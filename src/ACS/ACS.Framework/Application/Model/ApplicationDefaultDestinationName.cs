using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using ACS.Framework.Application;

namespace ACS.Framework.Application.Model
{
    public class ApplicationDefaultDestinationName
    {
        public static string APPLICATION_NAME = "@{application}";

        private string destinationName;

        public string DestinationName { get { return destinationName; } set { destinationName = value; } }

        public void Init()
        {
            if (!destinationName.Contains("@{application}"))
            {
                //logger.Info("name does not have @{application}, just apply name{" + destinationName + "}");
                return;
            }

            string applicationName = null;

            try
            {
                applicationName = ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_ID_VALUE];
            }
            catch(Exception e)
            {
                //logger.Error("failed to get applicationName", e);
                return;
            }

            DestinationName = DestinationName.Replace(APPLICATION_NAME, applicationName);
        }
    }
}
