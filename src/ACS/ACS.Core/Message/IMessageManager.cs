using ACS.Core.Message.Model.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ACS.Core.Message
{
    public interface IMessageManager
    {
        ControlMessage CreateControlMessage(XmlDocument paramDocument);

        ControlMessage CreateControlMessage(String paramString1, String paramString2);

        XmlDocument CreateDocument(ControlMessage paramControlMessage);

    }
}
