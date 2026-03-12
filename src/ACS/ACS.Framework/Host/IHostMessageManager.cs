using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Message.Model;

namespace ACS.Framework.Host
{
    public interface IHostMessageManager
    {
        Object Convert(String paramString, AbstractMessage paramAbstractMessage);

        void SendMessageToHost(AbstractMessage paramAbstractMessage, String paramString);

        void SendMessageToHost(AbstractMessage paramAbstractMessage, String paramString1, String paramString2);

        Object RequestMessageToHost(AbstractMessage paramAbstractMessage, String paramString);

        Object RequestMessageToHost(AbstractMessage paramAbstractMessage, String paramString1, String paramString2);

        bool UseSend(String paramString);

        bool UseReceive(String paramString);

        String GetSendHostMessageName(String paramString);

        String GetReceiveHostMessageName(String paramString);
    }
}
