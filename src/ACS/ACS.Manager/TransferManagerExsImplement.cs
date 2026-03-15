using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Manager.Transfer;
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;
using ACS.Core.Transfer.Model;

namespace ACS.Manager
{
    public class TransferManagerExsImplement : TransferManagerExImplement, ITransferManagerExs
    {


        public TransportCommandEx GetLastTransportCMDbyDest(string portID)
        {
            throw new NotImplementedException();
        }

        //200622 Change NIO Logic About ES.exe does not restart
        public IList GetEventUiCommand()
        {
            IList transportCommands = this.PersistentDao.FindAll(typeof(UiCommand));
            //logger.info("conut{" + transportCommands.size() + "}, " + transportCommands);
            return transportCommands;
        }
        //

        //200622 Change NIO Logic About ES.exe does not restart
        public int DeleteUiCommandById(string Id, string messageName, string applicationName)
        {
            Dictionary<string, object> conditionAttributes = new Dictionary<string, object>();

            conditionAttributes.Add("ID", Id);
            conditionAttributes.Add("MessageName", messageName);
            conditionAttributes.Add("ApplicationName", applicationName);

            return this.PersistentDao.DeleteByAttributes(typeof(UiCommand), conditionAttributes);

            //return this.PersistentDao.DeleteByAttribute(typeof(UiTransport), "ID", TransportId);
        }
        //

    }
}
