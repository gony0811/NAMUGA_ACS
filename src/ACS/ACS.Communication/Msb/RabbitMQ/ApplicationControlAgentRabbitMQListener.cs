using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using ACS.Communication.Msb.Highway101.Message;
using ACS.Core.Application;
using ACS.Core.Logging;
using ACS.Core.Message;
using ACS.Core.Message.Model;
using ACS.Core.Message.Model.Control;

namespace ACS.Communication.Msb.RabbitMQ
{
    public class ApplicationControlAgentRabbitMQListener : AbstractRabbitMQListener
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(ApplicationControlAgentRabbitMQListener));

        public IApplicationControlManager ApplicationControlManager { get; set; }
        public IMessageManagerEx MessageManager { get; set; }

        public override void OnMessage(XmlDocument document, string dest)
        {
            ControlMessageEx controlMessage = MessageManager.CreateControlMessage(document);

            this.ApplicationControlManager.Control(controlMessage);

            if (controlMessage.MessageName.Equals("CONTROL-HEARTBEAT"))
            {
                string message = document.InnerXml;

                if ((document is ExtendDocument))
                {

                }
            }
        }

        public override void OnJsonMessage(string jsonMessage, string dest)
        {
            try
            {
                string messageName = null;

                using (var doc = JsonDocument.Parse(jsonMessage))
                {
                    if (doc.RootElement.TryGetProperty("messageName", out var nameEl))
                        messageName = nameEl.GetString();
                }

                if ("CONTROL-HEARTBEAT".Equals(messageName, StringComparison.OrdinalIgnoreCase))
                {
                    this.ApplicationControlManager.InvokeHeartBeat();
                    _logger.Debug($"JSON CONTROL-HEARTBEAT 처리 완료");
                }
                else
                {
                    _logger.Warn($"알 수 없는 JSON control 메시지: {messageName}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"OnJsonMessage 처리 실패: {ex.Message}", ex);
            }
        }

        public override void OnMessage(AbstractMessage abstractMessage)
        {

        }
    }
}
