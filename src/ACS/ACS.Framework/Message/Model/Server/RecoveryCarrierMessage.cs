using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Message.Model.Server
{
    public class RecoveryCarrierMessage: BaseMessage
    {
        private IList carrierNames = new ArrayList();

        public IList CarrierNames { get { return carrierNames; } set { carrierNames = value; } }
    }
}
