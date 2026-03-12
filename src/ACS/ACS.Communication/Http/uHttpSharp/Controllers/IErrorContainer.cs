using System.Collections.Generic;
using System.Threading.Tasks;
using ACS.Communication.Http.uHttpSharp.Handlers;

namespace ACS.Communication.Http.uHttpSharp.Controllers
{
    public interface IErrorContainer
    {

        void Log(string description);

        IEnumerable<string> Errors { get; }

        bool Any { get; }

        Task<IControllerResponse> GetResponse();

    }
}