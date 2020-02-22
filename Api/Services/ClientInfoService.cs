using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CentCom.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CentCom.Services
{
    public class ClientInfoService : IClientInfoService
    {
        public ClientInfoService(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        /**
         * <summary>Must be called within controller api method</summary>
         */
        public IPEndPoint GetClientEndpoint()
        {
            var context = httpContextAccessor.HttpContext;

            return new IPEndPoint(context.Connection.RemoteIpAddress ?? IPAddress.Loopback, context.Connection.RemotePort);
        }

        private readonly IHttpContextAccessor httpContextAccessor;
    }
}
