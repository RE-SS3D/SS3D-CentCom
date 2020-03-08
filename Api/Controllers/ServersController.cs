using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Api.Models;
using Newtonsoft.Json;

namespace Api.Controllers
{
    /**
     * <summary>Maintains a list of servers that can be retrieved and queried</summary>
     */
    [Route("api/[controller]")]
    [ApiController]
    public class ServersController : ControllerBase
    {
        public class ConnectionResponseObject
        {
            public int challenge;
        }

        public ServersController(DataContext dataContext, IHttpClientFactory httpClientFactory, ILogger<ServersController> logger)
        {
            this.dataContext = dataContext;
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IAsyncEnumerable<GameServer> GetServers()
        {
            // Remove all servers outside of timeout
            // TODO: Should be an easier way that doesn't require a get, then set, then get of the entire list.
            var thresholdTime = DateTime.Now - SERVER_TIMEOUT_PERIOD;
            dataContext.Servers.RemoveRange(dataContext.Servers.Where(server => server.LastUpdate < thresholdTime));
            dataContext.SaveChanges();

            return dataContext.Servers;
        }

        /**
         * <summary>
         * Used to create a server. Most important for providing the route at which to update info and send heartbeat.
         * </summary>
         * <returns>The created object, along with the url to it in the Location header. Otherwise a 409 if already exists.</returns>
         */
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status424FailedDependency)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> PostNewServer(GameServer server)
        {
            // Fill in any missing server info
            if (server.Address == null)
                server.Address = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? IPAddress.Loopback.ToString();

            logger.LogInformation($"GameServer located at {server.Address}");

            // For now, ensure correct source to prevent DOS abuse.
            if (!CheckSourceIsServer(server.GetQueryEndPoint()))
                return Forbid();

            bool result = await ConnectToGameServer(server.GetQueryEndPoint()).ConfigureAwait(false);

            if (!result)
                return new StatusCodeResult(StatusCodes.Status424FailedDependency);

            server.LastUpdate = DateTime.Now;

            try {
                dataContext.Servers.Add(server);
                dataContext.SaveChanges();
            }
            catch(DbUpdateException) {
                return Conflict();
            }

            return CreatedAtAction("GetServerById", new { id = server.GetQueryEndPoint().ToString() }, server);
        }

        /**
         * <summary>Updates an existing server. Also updates heartbeat.</summary>
         */
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(GameServer), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(GameServer), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public IActionResult PutServer(string id, GameServer serverInput)
        {
            if(!IPEndPoint.TryParse(id, out IPEndPoint queryEndPoint))
                return BadRequest("Incorrect id");

            if (serverInput is null)
                throw new ArgumentNullException(nameof(serverInput));

            if (!CheckSourceIsServer(queryEndPoint))
                return Forbid();

            // Must preserve primary key values
            if (string.IsNullOrEmpty(serverInput.Address))
                serverInput.Address = queryEndPoint.Address.ToString();
            if (serverInput.QueryPort == 0)
                serverInput.QueryPort = queryEndPoint.Port;

            if (serverInput.Address != queryEndPoint.Address.ToString() || serverInput.QueryPort != queryEndPoint.Port)
                return BadRequest("Cannot modify endpoint or query point.");

            serverInput.LastUpdate = DateTime.Now;

            // Update the entry
            var foundServer = dataContext.Servers.Find(GameServer.ToKeyList(queryEndPoint));
            if (foundServer == null)
                return NotFound();

            dataContext.Entry(foundServer).CurrentValues.SetValues(serverInput);
            dataContext.SaveChanges();

            return Ok();
        }

        /**
         * <summary>Updates the timeout without changing any server information</summary>
         * <param name="id">A stringised version of IdToEndpoint, produced from <see cref="EndpointToId(IPEndPoint)"/></param>
         */
        [HttpPost("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult PostHeartBeatUpdate(string id)
        {
            // Get the endpoint referred to
            if (!IPEndPoint.TryParse(id, out IPEndPoint queryEndPoint))
                return BadRequest("Id is not of valid ip format");

            if (!CheckSourceIsServer(queryEndPoint))
                return Forbid();

            var serverObject = dataContext.Servers.Find(GameServer.ToKeyList(queryEndPoint));
            if (serverObject != null) {
                serverObject.LastUpdate = DateTime.Now;
                dataContext.SaveChanges();

                return Ok();
            }
            return NotFound();
        }

        /**
         * <summary>No real reason to use this, but helps in some cases anyway.</summary>
         */
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public ActionResult<GameServer> GetServerById(string id)
        {
            if (!IPEndPoint.TryParse(id, out IPEndPoint endPoint))
                return BadRequest("Id is not of valid ip format");

            var server = dataContext.Servers.Find(GameServer.ToKeyList(endPoint));

            if (server == null)
                return NotFound();

            return server;
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public IActionResult DeleteServer(string id)
        {
            if (!IPEndPoint.TryParse(id, out IPEndPoint endPoint))
                return BadRequest("Id is not of valid ip format");

            // TODO: Not performant. Should ideally be a single request.
            var server = dataContext.Servers.Find(GameServer.ToKeyList(endPoint));

            if (server == null)
                return NotFound();

            dataContext.Servers.Remove(server);
            dataContext.SaveChanges();

            return NoContent();
        }


        /**
         * <summary>Attempts to verify the connection to the given game server.</summary>
         * <param name="hostName">A DNS HostName or IP Address</param>
         * <param name="port">The query port of the game server</param>
         * <returns>Whether the connection was valid.</returns>
         */
        private async Task<bool> ConnectToGameServer(IPEndPoint queryEndPoint)
        {
            using HttpClient client = httpClientFactory.CreateClient();

            // Setup a request to the game server to verify that it exists
            client.BaseAddress = new UriBuilder("http", queryEndPoint.Address.ToString(), queryEndPoint.Port).Uri;

            int challenge = challengeSource.Next();

            var query = HttpUtility.ParseQueryString("");
            query["master"] = Request?.Host.Host ?? "unknown";
            query["version"] = "0.0.0"; // TODO
            query["challenge"] = challenge.ToString(CultureInfo.CurrentCulture);

            var uri = new Uri($"/connect?{query.ToString()}", UriKind.Relative);
            try {
                // Make the actual request
                var response = await client.PostAsync(uri, null).ConfigureAwait(false);

                if (response.StatusCode != HttpStatusCode.OK) {
                    logger.LogWarning($"Recieved non-good response code {response.StatusCode}. Not accepting connection.");
                    return false;
                }

                // Check the challenge in the body is correct
                var content = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                
                if(content["challenge"].ToObject<int>() != challenge) {
                    logger.LogWarning("Recieved incorrect challenge code?? Not accepting connection");
                    return false;
                }

                return true;
            }
            catch (HttpRequestException e) {
                logger.LogWarning(e, $"Failed to connect to game server at {queryEndPoint}. Dropping connection.");
                return false;
            }
            catch (JsonReaderException e) {
                logger.LogWarning(e, "Failed to get challenge object back. Dropping connection");
            }

            return true;
        }

        private bool CheckSourceIsServer(IPEndPoint queryEndpoint)
        {
            // We check the ip (but not port due to NAT) directly against the connection to at least somewhat prevent a malicious spoof.
            // TODO: To further prevent spoofing we need to issue a challenge back to the server.
            var clientAddress = HttpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback;

            logger.LogInformation($"Connection coming from {clientAddress}");
            return queryEndpoint.Address.Equals(clientAddress);
        }

        private static readonly TimeSpan SERVER_TIMEOUT_PERIOD = TimeSpan.FromMinutes(5);

        private readonly Random challengeSource = new Random();

        private readonly DataContext dataContext;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<ServersController> logger;
    }
}