using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Configuration;
using ACS.Core.Application;

namespace ACS.Communication.Msb.Highway101
{
    public class ChannelDestination 
    {
        public string Name { get; set; }



        public ChannelDestination()
        {

        }

        public ChannelDestination(string name)
        {
            Name = name;
        }

        public virtual void Init()
        {
            string time = DateTime.UtcNow.ToLongDateString();
            
            //1PC 2ACS Test
            this.Name = this.Name.Replace("@{site}", ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_SITE_VALUE]);

            this.Name = this.Name.Replace(".", "/");

            if (!this.Name.StartsWith(@"/"))
            {
                this.Name = @"/" + this.Name;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("channelDestination{");
            sb.Append("name=").Append(this.Name);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
