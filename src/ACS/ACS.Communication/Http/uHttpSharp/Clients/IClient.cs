using System.IO;
using System.Net;

namespace ACS.Communication.Http.uHttpSharp.Clients
{
    public interface IClient
    {

        Stream Stream { get; }

        bool Connected { get; }

        void Close();

        EndPoint RemoteEndPoint { get; }



    }
}
