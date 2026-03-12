using System;
using System.Threading.Tasks;
using ACS.Communication.Http.uHttpSharp.Clients;

namespace ACS.Communication.Http.uHttpSharp.Listeners
{
    public interface IHttpListener: IDisposable
    {

        Task<IClient> GetClient();

    }
}