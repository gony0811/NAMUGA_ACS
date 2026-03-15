using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Message.Model.Server
{
    public class TransportBatchMessage : BaseMessage
    {
        private string batchTransportJobId;
        private IList batchTransportJobs = new ArrayList();
        private IList batchTransportMessages = new ArrayList();

        protected internal string BatchTransportJobId { get { return batchTransportJobId; } set { batchTransportJobId = value; } }
        protected internal IList BatchTransportJobs { get { return batchTransportJobs; } set { batchTransportJobs = value; } }
        protected internal IList BatchTransportMessages { get { return batchTransportMessages; } set { batchTransportMessages = value; } }
    }
}
