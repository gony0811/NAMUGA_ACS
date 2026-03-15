using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;
using ACS.Core.Resource;
using ACS.Core.Resource.Model.Factory.Machine;
using ACS.Core.Resource.Model.Factory.Zone;
using ACS.Core.Resource.Model.Factory.Unit;
using ACS.Core.Application;
using ACS.Core.Material.Model;
using ACS.Core.Message.Model;
using ACS.Core.Logging;

namespace ACS.Communication.Msb.Convert.Implement
{
    public class MsbValueConverterImpl : IMsbValueConverter
    {
        protected static Logger logger = Logger.GetLogger(typeof(MsbValueConverterImpl));
        public const string TRUE = "T";
        public const string FALSE = "F";
        public const string YES = "Y";
        public const string NO = "N";
        public const string PROPERTYNAME_PRIORITY = "PRIORITY";
        public ITransferManagerEx TransferManager { get; set; }
        //public CacheManager cacheManager;
        public IResourceManagerEx ResourceManager { get; set; }


        public void Init()
        {
            //if (this.CacheManager == null)
            //{
            //    logger.Warn("cacheManager should be set properly, please check configuration");
            //}
            //if (this.ResourceManager == null)
            //{
            //    logger.Warn("resourceManager should be set properly, please check configuration");
            //}
        }

        public string ChangeValue(string propertyName, string objectPropertyName, string objectPropertyValue, object classObject, string methodName)
        {
            String result = objectPropertyValue;
            if (propertyName.Equals("PRIORITY"))
            {
                result = ChangePriorityValue(objectPropertyValue);
            }
            logger.Warn("{" + objectPropertyName + "." + objectPropertyValue + "} should be changed for {" + propertyName + "}, please extend class and write down your own logic. default is " + objectPropertyValue);
            return result;
        }

        private string ChangePriorityValue(string priority)
        {
            return this.TransferManager.ConvertPriorityToMES(priority);
        }

        public string ComposeValue(string propertyName, object classObject, string methodName)
        {
            logger.Warn("propertyName{" + propertyName + "} does not exist in " + classObject + ", please extend class and write down your own logic. default is whiteString");
            return "";
        }

        protected string GetServerName()
        {
            return ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_ID_VALUE];
        }

        protected Carrier GetCarrier(Object classObject)
        {
            Carrier carrier = null;
            if ((classObject is BaseMessage)) {
              carrier = ((BaseMessage)classObject).Carrier;
            } else if ((classObject is Carrier)) {
              carrier = (Carrier)classObject;
            } else {
              logger.Warn("failed to getCarrier from {" + classObject + "}");
            }
            return carrier;
        }

        protected TransportJob GetTransportJob(object classObject)
        {
            TransportJob transportJob = null;

            if(classObject is BaseMessage)
            {
                transportJob = ((BaseMessage)classObject).TransportJob;
            }
            else if(classObject is TransportJob)
            {
                transportJob = (TransportJob)classObject;
            }
            else
            {
                logger.Warn("failed to GetTransportJob from {" + classObject + "}");
            }

            return transportJob;
        }

        protected TransportCommand GetTransportCommand(object classObject)
        {
            TransportCommand transportCommand = null;

            if (classObject is BaseMessage)
            {
                transportCommand = ((BaseMessage)classObject).TransportCommand;
            }
            else if (classObject is TransportCommand)
            {
                transportCommand = (TransportCommand)classObject;
            }
            else
            {
                logger.Warn("failed to GetTransportJob from {" + classObject + "}");
            }

            return transportCommand;
        }

        protected Machine GetCurrentMachine(object classObject)
        {
            Machine currentMachine = null;

            if(classObject is AbstractMessage)
            {
                currentMachine = ((AbstractMessage)classObject).CurrentMachine;

                if (currentMachine == null)
                {
                    string currentMachineName = ((AbstractMessage)classObject).CurrentMachineName;
                    if ((!string.IsNullOrEmpty(currentMachineName)) && (!currentMachineName.Equals("NA")))
                    {
                        currentMachine = this.ResourceManager.GetMachineByName(currentMachineName);
                    }
                }
            }
            else if((classObject is Machine))
            {
                currentMachine = (Machine)classObject;
            }
            else if(classObject is Carrier)
            {
                string machineName = ((Carrier)classObject).MachineName;

                if(!string.IsNullOrEmpty(machineName) && (!machineName.Equals("NA")))
                {
                    currentMachine = this.ResourceManager.GetMachineByName(machineName);
                }
            }
            else if (classObject is Zone)
            {
                string machineName = ((Zone)classObject).MachineName;
                currentMachine = this.ResourceManager.GetMachineByName(machineName);
            }
            else if (classObject is Unit)
            {
                string machineName = ((Unit)classObject).MachineName;
                currentMachine = this.ResourceManager.GetMachineByName(machineName);
            }
            else
            {
                logger.Warn("failed to GetCurrentMachine from {" + classObject + "}");
            }

            return currentMachine;
        }

        protected TransportMachine GetCurrentTransportMachine(object classObject)
        {
            TransportMachine currentTransportMachine = null;

            if (classObject is BaseMessage)
            {
                currentTransportMachine = (TransportMachine)(((BaseMessage)classObject).CurrentTransportMachine);
            }
            else if (classObject is TransportMachine)
            {
                currentTransportMachine = (TransportMachine)classObject;
            }
            else
            {
                logger.Warn("failed to GetTransportJob from {" + classObject + "}");
            }

            return currentTransportMachine;
        }

        protected Unit GetCurrentUnit(object classObject)
        {
            Unit currentUnit = null;

            if (classObject is BaseMessage)
            {
                currentUnit = ((BaseMessage)classObject).CurrentUnit;

                if (currentUnit == null)
                {
                    Carrier carrier = ((BaseMessage)classObject).Carrier;


                    if (carrier != null)
                    {
                        string carrierMachineName = carrier.MachineName;

                        if(!string.IsNullOrEmpty(carrierMachineName) && (!carrierMachineName.Equals("NA")))
                        {
                            string carrierUnitName = carrier.UnitName;
                            if((!string.IsNullOrEmpty(carrierUnitName))&&(!carrierUnitName.Equals("NA")))
                            {
                                currentUnit = ResourceManager.GetUnitByName(carrierUnitName, carrierMachineName);
                            }
                        }
                    }
                }
            }
            else if ((classObject is Unit))
            {
                currentUnit = (Unit)classObject;
            }
            else if (classObject is Carrier)
            {
                string carrierMachineName = ((Carrier)classObject).MachineName;

                if (!string.IsNullOrEmpty(carrierMachineName) && (!carrierMachineName.Equals("NA")))
                {
                    string carrierUnitName = ((Carrier)classObject).UnitName;
                    if(!string.IsNullOrEmpty(carrierUnitName) && !carrierUnitName.Equals("NA"))
                    {
                        currentUnit = this.ResourceManager.GetUnitByName(carrierUnitName, carrierMachineName);
                    }
                }
            }
            else
            {
                logger.Warn("failed to GetCurrentUnit from {" + classObject + "}");
            }

            return currentUnit;
        }

        protected Zone GetZone(object classObject)
        {
            Zone zone = null;

            if(classObject is Zone)
            {
                zone = (Zone)classObject;
            }
            else
            {
                logger.Warn("failed to GetZone from {" + classObject + "}");
            }

            return zone;
        }

        protected Machine GetCachedMachine(string machineName)
        {
            return null;
        }

        protected Unit GetCachedUnit(string unitName, string machineName)
        {
            return null;
        }

        protected Zone GetCachedZone(string zoneName, string machineName)
        {
            return null;
        }
    }
}
