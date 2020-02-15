using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CentCom.Dtos;
using CentCom.Interfaces;
using CentCom.Models;
using Microsoft.EntityFrameworkCore;

namespace CentCom.Controllers
{
    /**
     * <summary>Maintains a list of servers that can be retrieved and queried</summary>
     */
    [Route("api/[controller]")]
    [ApiController]
    public class ServersController : ControllerBase
    {
        public ServersController(DataContext dataContext, IClientInfoService clientInfoService)
        {
            this.dataContext = dataContext;
            this.clientInfoService = clientInfoService;
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        public IAsyncEnumerable<Server> GetServers()
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
         * Note: If you already know how to generate an ID from the server info (namely endpoint), you can use <see cref="PutServer(string, ServerDto)"/>
         */
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesDefaultResponseType]
        public IActionResult PostNewServer(ServerDto serverInput)
        {
            if (serverInput is null)
                throw new ArgumentNullException(nameof(serverInput));

            var endPoint = clientInfoService.GetClientEndpoint();

            var server = serverInput.ToServer(endPoint);
            try {
                dataContext.Servers.Add(server);
                dataContext.SaveChanges();
            }
            catch(DbUpdateException) {
                return Conflict();
            }

            return CreatedAtAction("GetServerById", new { id = endPoint.ToString() }, server);
        }

        /**
         * <summary>Creates server at place, or updates existing server. Will return Create or OK.</summary>
         */
        [Route("{id}")]
        [ProducesResponseType(typeof(Server), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Server), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public IActionResult PutServer(string id, ServerDto serverInput)
        {
            if(!IPEndPoint.TryParse(id, out IPEndPoint endPoint))
                return BadRequest("Incorrect id");

            if (serverInput is null)
                throw new ArgumentNullException(nameof(serverInput));

            // We check the ip and port directly against the connection to at least somewhat prevent a malicious spoof.
            // TODO: To further prevent spoofing we need to issue a challenge back to the server.
            if (!endPoint.Equals(clientInfoService.GetClientEndpoint()))
                return Forbid();

            // Add or update the entry
            var newServer = dataContext.Servers.Find(endPoint.Address.GetAddressBytes(), endPoint.Port);
            bool isUpdated = newServer != null;
            if(isUpdated) {
                newServer.Name = serverInput.Name;
                newServer.LastUpdate = DateTime.Now;
            }
            else {
                var entry = dataContext.Servers.Add(serverInput.ToServer(endPoint));
                newServer = entry.Entity;
            }
            dataContext.SaveChanges();

            if (isUpdated)
                return Ok(newServer);
            else
                return CreatedAtAction("GetServerById", new { id }, newServer);
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
            if (!IPEndPoint.TryParse(id, out IPEndPoint endPoint))
                return BadRequest("Id is not of valid ip format");

            // For now assert that the given endpoint is equal to the source.
            // TODO: Determine whether to issue challenge instead of checking like this, or have no protections whatsoever
            if (!endPoint.Equals(clientInfoService.GetClientEndpoint()))
                return Forbid();

            var serverObject = dataContext.Servers.Find(endPoint.Address.GetAddressBytes(), endPoint.Port);
            if (serverObject != null) {
                serverObject.LastUpdate = DateTime.Now;
                dataContext.SaveChanges();

                return Ok();
            }
            return NotFound();
        }

        /**
         * <summary>No real reason to use this...</summary>
         */
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public ActionResult<Server> GetServerById(string id)
        {
            if (!IPEndPoint.TryParse(id, out IPEndPoint endPoint))
                return BadRequest("Id is not of valid ip format");

            var server = dataContext.Servers.Find(endPoint.Address.GetAddressBytes(), endPoint.Port);

            if (server == null)
                return NotFound();

            return server;
        }

        private static readonly TimeSpan SERVER_TIMEOUT_PERIOD = TimeSpan.FromMinutes(5);

        private readonly DataContext dataContext;
        private readonly IClientInfoService clientInfoService;
    }
}