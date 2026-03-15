using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using ACS.Core.Base;
using ACS.Core.Application;
using ACS.Core.Message.Model.Control;
using ACS.Communication.Msb;
using ACS.Control;
using ACS.App;
using System.Configuration;
using ACS.Core.Base;

namespace ACS.Service
{
    public class ControlService : AbstractServiceEx
    {
        public IControlServerManager ControlServerManager { get; set; }
        public IMessageAgent MessageAgent { get; set; }
        public IApplicationControlManager ApplicationControlManager { get; set; }

        public override void Init()
        {
            base.Init();
        }

        public bool Control(ControlMessage controlMessage)
        {
            bool result = this.ControlServerManager.Control(controlMessage);

            if (!result)
            {
                string applicationName = controlMessage.ApplicationName;

                if (!applicationName.Equals(ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_ID_VALUE]))
                {
                    XmlDocument receivedMessage = (XmlDocument)controlMessage.ReceivedMessage;

                    if (applicationName.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                    {
                        IList applicationNames = this.ApplicationManager.GetApplicationNamesByType(controlMessage.ApplicationType, ConfigurationManager.AppSettings[Settings.SYSTEM_ENV_KEY_HARDWARE_TYPE]);

                        foreach (object obj in applicationNames)
                        {
                            applicationName = (string)obj;

                            if (receivedMessage is XmlDocument)
                            {
                                this.MessageAgent.Send(receivedMessage.InnerXml, this.ControlServerManager.GetDestinationName(applicationName));
                            }
                            else
                            {
                                this.MessageAgent.Send(receivedMessage.InnerXml, this.ControlServerManager.GetDestinationName(applicationName));
                            }
                        }
                    }
                    else if ((receivedMessage is XmlDocument))
                    {
                        this.MessageAgent.Send(receivedMessage.InnerXml, this.ControlServerManager.GetDestinationName(applicationName));
                    }
                    else
                    {
                        this.MessageAgent.Send(receivedMessage.InnerXml, this.ControlServerManager.GetDestinationName(applicationName));
                    }
                }
                else
                {
                    this.ApplicationControlManager.Control(controlMessage);
                }
            }

            return result;
        }
  }
}
