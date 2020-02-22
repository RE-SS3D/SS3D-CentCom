using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CentCom.Models;

namespace CentCom.Dtos
{
    public class ServerDto
    {
        public string Name { get; set; }

        /**
         * <summary>Convert to a server object, specifying extra inputs.</summary>
         * <param name="address">Address to fill in the new server object</param>
         * <param name="lastUpdate">Time of last update for the server. Defaults to DateTime.Now</param>
         */
        public Server ToServer(IPEndPoint address, DateTime lastUpdate = default)
        {
            return new Server { Address = address, Name = Name, LastUpdate = lastUpdate.Equals(DateTime.MinValue) ? DateTime.Now : lastUpdate };
        }
    }
}
