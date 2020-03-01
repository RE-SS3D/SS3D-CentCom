using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Api.SystemTests.Helpers
{
    /**
     * <summary>
     * Simulates a GameServer.
     * Listens at the given query port for connections.
     * Only responds to /connect http requests.
     * </summary>
     */
    class GameServerEmulator : IDisposable
    {
        public GameServerEmulator(int port)
        {
            this.port = port;
        }

        public void Start()
        {
            mainThread = new Thread(StartListening);
            mainThread.Start();

            serverReady.WaitOne();
        }

        public void Stop()
        {
            keepRunning = false;
            listenerUse.Set();
            mainThread.Join();
        }

        public void Dispose()
        {
            Stop();
        }

        private void StartListening()
        {
            IPAddress ipAddress = IPAddress.Loopback;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEndPoint);
            listener.Listen(100);

            keepRunning = true;
            serverReady.Set();
            Console.WriteLine("GameServer listening for incoming requests...");

            while(keepRunning)
            {
                listenerUse.Reset();

                listener.BeginAccept(new AsyncCallback(HandleSocket), listener);

                // Wait until the accept has ended
                listenerUse.WaitOne();
            }

            listener.Close();
        }

        private void HandleSocket(IAsyncResult ar)
        {
            Console.WriteLine("Recieved request. Processing...");

            Socket listener = ar.AsyncState as Socket;
            Socket handler;
            try {
                handler = listener.EndAccept(ar);
            }
            catch(ObjectDisposedException) { // Connection has been closed
                return;
            }

            // Listener may now continue processing new requests.
            listenerUse.Set();

            byte[] buffer = new byte[2048];
            int bytesRead = handler.Receive(buffer);

            string text = System.Text.Encoding.UTF8.GetString(buffer.AsSpan(0, bytesRead));
            // Assumes bytesRead < 2048

            if (!text.StartsWith("POST /connect"))
                return;

            var match = Regex.Match(text, @"challenge=(\d+)");

            if (!match.Success)
                return;

            string challenge = match.Groups[1].Value;

            string body = $"{{ \"challenge\": {challenge} }}";
            // Now respond
            handler.Send(Encoding.UTF8.GetBytes(
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: application/json; charset=utf-8\r\n" +
                $"Content-Length: {body.Length}\r\n" +
                "\r\n\r\n" +
                body
            ));

            handler.Close();
        }

        private int port;

        private bool keepRunning;
        private Thread mainThread;
        private ManualResetEvent listenerUse = new ManualResetEvent(false);
        private AutoResetEvent serverReady = new AutoResetEvent(false);
    }
}
