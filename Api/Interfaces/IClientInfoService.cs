using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CentCom.Interfaces
{
    /**
     * <summary>Helps to get information about the client</summary>
     */
    public interface IClientInfoService
    {
        IPEndPoint GetClientEndpoint();
    }
}
