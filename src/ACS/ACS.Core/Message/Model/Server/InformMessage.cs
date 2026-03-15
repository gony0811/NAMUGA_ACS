using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Message.Model.Server
{
    public class InformMessage: BaseMessage
    {
        private string terminalId;
        private string information;

        public string TerminalId { get {return terminalId;} set {terminalId = value;}}
        public string Information { get { return information; } set { information = value; } }
    }
}
