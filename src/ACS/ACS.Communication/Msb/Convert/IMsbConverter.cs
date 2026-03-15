using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using ACS.Core.Message.Model;
using ACS.Communication;

namespace ACS.Communication.Msb
{
    public interface IMsbConverter : IConverter
    {
        IMsbValueConverter GetMsbValueConverter();

        Object ConvertToHost(string paramstring, AbstractMessage paramAbstractMessage);

        AbstractMessage ConvertFromHost(Object paramObject);

        bool UseReceive(string paramstring);

        bool UseSend(string paramstring);

        string GetReceiveHostMessageName(string paramstring);

        string GetSendHostMessageName(string paramstring);

        XmlDocument GetReceiveMessageTemplate(string paramstring);

        XmlDocument GetSendMessageTemplate(string paramstring);

        void Reload();
    }
}
