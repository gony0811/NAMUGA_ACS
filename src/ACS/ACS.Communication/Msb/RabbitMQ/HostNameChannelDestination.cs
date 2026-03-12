using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace ACS.Communication.Msb.RabbitMQ
{
    public class HostNameChannelDestination : ChannelDestination
    {
        public static string HOST_NAME = "@{host}";

        public override void Init()
        {
            base.Init();

            if (Name.Contains("@{host}"))
            {
                return;
            }

            string hostName = null;

            try
            {
                hostName = Dns.GetHostName();
                hostName = hostName.Replace("-", "");
            }
            catch (Exception e)
            {
                hostName = "unknown" + Guid.NewGuid().ToString();
                hostName = hostName.Replace("-", "");
            }

            Name = Name.Replace(HOST_NAME, hostName);
        }
    }
}
