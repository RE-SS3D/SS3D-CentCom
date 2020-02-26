using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CentCom.Models
{
    public class GameServer
    {
        /// <summary>
        /// Name of the server
        /// </summary>
        [Required]
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }
        /// <summary>
        /// Optional. Custom text appearing below name.
        /// </summary>
        public string TagLine { get; set; }

        /// <summary>
        /// The ip, e.g. 127.0.0.1
        /// </summary>
        [Required]
        public string Address { get; set; } // As a hostname or ip

        /// <summary>
        /// Port used for connecting to the game.
        /// </summary>
        [Required]
        [JsonProperty(Required = Required.Always)]
        public int QueryPort { get; set; }

        /// <summary>
        /// Port used for querying game information. All http requests should go to here. Only has to be present in POST /api/servers.
        /// </summary>
        [Required]
        [JsonProperty(Required = Required.Always)]
        public int GamePort { get; set; }

        public int Players { get; set; }
        /// <summary>
        /// Optional. -1 implies infinite
        /// </summary>
        public int? MaxPlayers { get; set; }

        /// <summary>
        /// Ideally should be one of "restarting" | "lobby" | "playing"
        /// </summary>
        [Required]
        [JsonProperty(Required = Required.Always)]
        public string RoundStatus { get; set; }
        /// <summary>
        /// Time since last round status change
        /// </summary>
        [Required]
        [JsonProperty(Required = Required.Always)]
        public DateTime RoundStartTime { get; set; }

        public string Map { get; set; }
        public string Gamemode { get; set; }

        /// <summary>
        /// Should almost always be "SS3D". Different codebases should change 'branch' instead.
        /// </summary>
        [Required]
        [JsonProperty(Required = Required.Always)]
        public string Game { get; set; }

        /// <summary>
        /// Optional. The specific codebase this game is running. Default is "root"
        /// </summary>
        public string Branch { get; set; }

        /// <summary>
        /// Optional. Should be in format major.minor.patch, year-month-day, or GIT hash
        /// </summary>
        public string Version { get; set; }

        [Required]
        public DateTime LastUpdate { get; set; }

        public IPEndPoint GetQueryEndPoint()
        {
            return new IPEndPoint(IPAddress.Parse(Address), QueryPort);
        }
        public dynamic ToKey()
        {
            return new { Address, QueryPort };
        }
        public static object[] ToKeyList(IPEndPoint queryEndPoint)
        {
            return new object[] { queryEndPoint.Address.ToString(), queryEndPoint.Port };
        }
    }
}
