using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Text.Json.Serialization;

namespace CentCom.Models
{
    public class Server
    {
        /**
         * <summary>The endpoint at which the server is located</summary>
         * <remarks>To serialize this into the database, it is privately translated into an Ip and Port.</remarks>
         */
        [NotMapped]
        [JsonIgnore]
        public IPEndPoint Address
        {
            get => new IPEndPoint(new IPAddress(Ip), Port);
            set
            {
                Ip = value?.Address.GetAddressBytes();
                Port = value?.Port ?? 0;
            }
        }

        [Required]
        public string Name { get; set; }

        [Required]
        public DateTime LastUpdate { get; set; }

        // Private serialized model info
        [Required, MinLength(4), MaxLength(16)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Required for correct serialization of model")]
        public byte[] Ip { get; set; }
        [Required]
        public int Port { get; set; }
    }
}
