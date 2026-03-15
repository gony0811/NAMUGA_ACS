using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Message.Model.Server
{
    public class RetransportCommandMessage: BaseMessage
    {
        private IList transportCommandIds = new ArrayList();

        public IList TransportCommandIds { get { return transportCommandIds; } set { transportCommandIds = value; } }
    }
}
