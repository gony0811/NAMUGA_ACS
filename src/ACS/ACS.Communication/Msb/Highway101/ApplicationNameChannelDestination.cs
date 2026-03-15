using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using ACS.Core.Application;

namespace ACS.Communication.Msb.Highway101
{
    public class ApplicationNameChannelDestination : ChannelDestination
    {

        public override void Init()
        {
            base.Init();

            if (!Name.Contains("@{application}"))
            {
                return;
            }

            string applicationName = null;

            try
            {
                applicationName = ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_ID_VALUE];
            }
            catch(Exception e)
            {
                return;
            }

            Name = Name.Replace("@{application}", applicationName);
           
        }
    }
}
