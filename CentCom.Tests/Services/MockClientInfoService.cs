using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using CentCom.Interfaces;

namespace CentCom.Tests.Services
{
    public class MockClientInfoService : IClientInfoService
    {
        public MockClientInfoService(long ipAddress, int port)
            : this(new IPEndPoint(new IPAddress(ipAddress), port))
        {
        }
        public MockClientInfoService(IPEndPoint endPoint)
        {
            this.endPoint = endPoint;
        }
        public MockClientInfoService()
        {
        }

        public IPEndPoint GetClientEndpoint() => endPoint;

        private readonly IPEndPoint endPoint;
    }
}
