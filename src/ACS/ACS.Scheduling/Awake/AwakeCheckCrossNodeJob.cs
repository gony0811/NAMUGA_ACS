using System;
using System.Xml;
using ACS.Framework.Message;
using ACS.Communication.Msb;
using ACS.Framework.Message.Model;

namespace ACS.Scheduling
{
    public class AwakeCheckCrossNodeJob : PeriodicBackgroundService
    {
        private readonly IMessageManagerEx _messageManager;
        private readonly IMessageAgent _messageAgent;

        protected override TimeSpan Interval => TimeSpan.FromSeconds(5);

        public AwakeCheckCrossNodeJob(
            IMessageManagerEx messageManager,
            IMessageAgent messageAgent)
        {
            _messageManager = messageManager;
            _messageAgent = messageAgent;
        }

        protected override void ExecuteOnce()
        {
            try
            {
                AbstractMessage message = new AbstractMessage();

                message.MessageName = "SCHEDULE-CHECKCROSSNODE";
                XmlDocument document = _messageManager.CreateDocument(message);

                XmlElement data = document.DocumentElement["DATA"];

                XmlNode element = document.CreateNode(XmlNodeType.Element, "NAME", "");
                element.InnerText = message.MessageName;
                data.AppendChild(element);

                _messageAgent.Send(document);
            }
            catch (NullReferenceException nullEx)
            {
                logger.Error(nullEx.StackTrace, nullEx);
                return;
            }
            catch (Exception ex)
            {
                logger.Error(ex.StackTrace, ex);
                return;
            }
        }
    }
}
