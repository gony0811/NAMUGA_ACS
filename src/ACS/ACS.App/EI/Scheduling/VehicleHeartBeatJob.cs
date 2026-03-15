using Quartz;
using ACS.Communication.Socket;
using ACS.Core.Application;
using ACS.Core.Message.Model;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Scheduling.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ACS.Core.Logging;
using ACS.Communication.Socket.Model;

namespace ACS.EI.Scheduling
{
    public class VehicleHeartBeatJob : AbstractJob
    {
        public const string NIO_TYPE_REPEATER = "REPEATER";
        public const string NIO_TYPE_WIFI = "WIFI";

        protected Logger logger = Logger.GetLogger(typeof(VehicleHeartBeatJob));
        protected IResourceManagerEx resourceManager;
        protected NioInterfaceManager nioInterfaceManager;
        protected IApplicationManager applicationManager;
        protected IList vehicleList;

        public override void ExecuteJob(IJobExecutionContext context)
        {
            logger.Debug("VehicleHeartBeatJob will be invoked");

            this.nioInterfaceManager = ((NioInterfaceManager)context.MergedJobDataMap.Get("NioInterfaceManager"));
            this.applicationManager = ((IApplicationManager)context.MergedJobDataMap.Get("ApplicationManager"));
            //this.resourceManager = ((IResourceManagerExs)context.MergedJobDataMap.Get("ResourceManager"));

            IConfiguration configuration = (IConfiguration)context.MergedJobDataMap.Get("Configuration");
            string applicationName = configuration["Acs:Process:Name"];

            //if(vehicleList == null)
            //{
            //    vehicleList = resourceManager.GetVehicle
            //}

            //181213 WIFI RF ZIGBEE available
            bool isWifiMode = this.applicationManager.IsWifiMode(applicationName);
            SendHCode(isWifiMode);

            //SendHCode(vehicleList);

            //181026 H Code Send Logic변경
            //bool isWifiMode = this.applicationManager.IsWifiMode(applicationName);
            //SendHCode(vehicleList, isWifiMode);
        }

        private void SendHCode(bool isWifiMode)
        {
            try
            {
                string applicationName = applicationManager.GetApplicationName();

                IList nioList = this.nioInterfaceManager.GetNioesByApplicationName(applicationName);

                foreach(Nio nio in nioList)
                {
                    string commandCode = VehicleMessageEx.COMMAND_CODE_H;

                    var str = DateTime.Now.ToString("HHmmss");

                    string transactionId = str;
                    string vehicleId = null;

                    if(!string.IsNullOrEmpty(nio.Name))
                    {
                        //CAUTION : NIONAME = VEHICLE_ID
                        vehicleId = nio.Name.PadLeft(3, '0');

                        SendPacket sendPacket = new SendPacket(vehicleId, "A", commandCode, transactionId);

                        if (isWifiMode)
                        {
                            //Wifi or Zigbee
                            //181026 H Code Send Logic변경
                            //AbstractSocketService abstractSocketService = this.nioInterfaceManager.GetNioInterface(vehicleId);
                            AbstractSocketService abstractSocketService = this.nioInterfaceManager.GetNioInterfacebyVehicleId(vehicleId, applicationName);

                            if (abstractSocketService != null)
                            {
                                if (abstractSocketService.SessionOpened)
                                {
                                    abstractSocketService.Send(sendPacket);
                                }
                                else
                                {
                                    logger.Warn("Not Open Socket, Fail to Send Packet : " + sendPacket.ToString());
                                }
                            }
                            else
                            {
                                //Skip
                            }
                        }
                        else
                        {
                            ////2019.05.29 KSB RF 데이터양이 많아서 RF에서는 일단 skip
                            ////181213 WIFI RF ZIGBEE available
                            ////RF
                            //IDictionary nioes = this.nioInterfaceManager.NioInterfaces;

                            //for (IEnumerator iterator = nioes.Values.GetEnumerator(); iterator.MoveNext();)
                            //{
                            //    AbstractSocketService abstractSocketService = (AbstractSocketService)iterator.Current;

                            //    if (abstractSocketService != null)
                            //    {
                            //        if (abstractSocketService.SessionOpened)
                            //        {
                            //            abstractSocketService.Send(sendPacket);
                            //        }
                            //        else
                            //        {
                            //            logger.Warn("Not Open Socket, Fail to Send Packet : " + sendPacket.ToString());
                            //        }
                            //    }
                            //    else
                            //    {
                            //        //Skip
                            //    }
                            //}
                        }
                    }
                }
            }
            catch(Exception e)
            {

            }
        }

        private void SendHCode(IList vehicleList, bool isWifiMode)
        {
            try
            {
                string applicationName = applicationManager.GetApplicationName();

                if (vehicleList == null) return;

                foreach (VehicleEx vehicle in vehicleList)
                {
                    string commandCode = VehicleMessageEx.COMMAND_CODE_H;

                    var str = DateTime.Now.ToString("HHmmss");

                    string transactionId = str;
                    string vehicleId = vehicle.Id.PadLeft(3, '0');

                    SendPacket sendPacket = new SendPacket(vehicleId, "A", commandCode, transactionId);

                    if (isWifiMode)
                    {                       
                        //Wifi or Zigbee
                        //181026 H Code Send Logic변경
                        //AbstractSocketService abstractSocketService = this.nioInterfaceManager.GetNioInterface(vehicleId);
                        AbstractSocketService abstractSocketService = this.nioInterfaceManager.GetNioInterfacebyVehicleId(vehicleId, applicationName);

                        if (abstractSocketService != null)
                        {
                            if (abstractSocketService.SessionOpened)
                            {
                                abstractSocketService.Send(sendPacket);
                            }
                            else
                            {
                                logger.Warn("Not Open Socket, Fail to Send Packet : " + sendPacket.ToString());
                            }
                        }
                        else
                        {
                            //Skip
                        }
                    }
                    else
                    {
                        ////2019.05.29 KSB RF 데이터양이 많아서 RF에서는 일단 skip
                        ////181213 WIFI RF ZIGBEE available
                        ////RF
                        //IDictionary nioes = this.nioInterfaceManager.NioInterfaces;

                        //for (IEnumerator iterator = nioes.Values.GetEnumerator(); iterator.MoveNext();)
                        //{
                        //    AbstractSocketService abstractSocketService = (AbstractSocketService)iterator.Current;

                        //    if (abstractSocketService != null)
                        //    {
                        //        if (abstractSocketService.SessionOpened)
                        //        {
                        //            abstractSocketService.Send(sendPacket);
                        //        }
                        //        else
                        //        {
                        //            logger.Warn("Not Open Socket, Fail to Send Packet : " + sendPacket.ToString());
                        //        }
                        //    }
                        //    else
                        //    {
                        //        //Skip
                        //    }
                        //}
                    }

                }
            }
            catch (Exception e)
            {

            }
            finally
            {

            }
        }

        //        private void SendHCode(IList vehicleList, bool isWifiMode)
        //        {
        //#if BYTE12
        //            //try
        //            //{
        //            //    foreach (VehicleEx vehicle in vehicleList)
        //            //    {
        //            //        string commandCode = VehicleMessageEx.COMMAND_CODE_H;

        //            //        var str = DateTime.Now.ToString("mmss");

        //            //        string transactionId = str;
        //            //        string vehicleId = vehicle.Id.PadLeft(2,'0');

        //            //        SendPacket sendPacket = new SendPacket(vehicle.Id, "A", commandCode, transactionId);

        //            //        if (isWifiMode)
        //            //        {
        //            //            AbstractSocketService abstractSocketService = this.nioInterfaceManager.GetNioInterface(vehicle.Id);
        //            //            if (abstractSocketService != null)
        //            //            {
        //            //                if (abstractSocketService.SessionOpened)
        //            //                {
        //            //                    abstractSocketService.Send(sendPacket);
        //            //                }
        //            //                else
        //            //                {
        //            //                    logger.Warn("Not Open Socket, Fail to Send Packet : " + sendPacket.ToString());
        //            //                }
        //            //            }
        //            //        }
        //            //        else
        //            //        {
        //            //            IDictionary nioes = this.nioInterfaceManager.NioInterfaces;
        //            //            for (IEnumerator iterator = nioes.Values.GetEnumerator(); iterator.MoveNext(); )
        //            //            {
        //            //                AbstractSocketService abstractSocketService = (AbstractSocketService)iterator.Current;
        //            //                if (abstractSocketService != null)
        //            //                {
        //            //                    if (abstractSocketService.SessionOpened)
        //            //                    {
        //            //                        abstractSocketService.Send(sendPacket);
        //            //                    }
        //            //                    else
        //            //                    {
        //            //                        logger.Warn("Not Open Socket, Fail to Send Packet : " + sendPacket.ToString());
        //            //                    }
        //            //                }
        //            //            }

        //            //        }
        //            //    }
        //            //}
        //            //catch (Exception e)
        //            //{

        //            //}
        //            //finally
        //            //{

        //            //}    
        //#else
        //            try
        //            {
        //                foreach (VehicleEx vehicle in vehicleList)
        //                {
        //                    string commandCode = VehicleMessageEx.COMMAND_CODE_H;

        //                    var str = DateTime.Now.ToString("HHmmss");

        //                    string transactionId = str;
        //                    string vehicleId = vehicle.Id.PadLeft(3,'0');

        //                    SendPacket sendPacket = new SendPacket(vehicleId, "A", commandCode, transactionId);

        //                    if (isWifiMode)
        //                    {
        //                        AbstractSocketService abstractSocketService = this.nioInterfaceManager.GetNioInterface(vehicleId);
        //                        if (abstractSocketService != null)
        //                        {
        //                            if (abstractSocketService.SessionOpened)
        //                            {
        //                                abstractSocketService.Send(sendPacket);
        //                            }
        //                            else
        //                            {
        //                                logger.Warn("Not Open Socket, Fail to Send Packet : " + sendPacket.ToString());
        //                            }
        //                        }
        //                    }
        //                    else
        //                    {
        //                        IDictionary nioes = this.nioInterfaceManager.NioInterfaces;
        //                        for (IEnumerator iterator = nioes.Values.GetEnumerator(); iterator.MoveNext(); )
        //                        {
        //                            AbstractSocketService abstractSocketService = (AbstractSocketService)iterator.Current;
        //                            if (abstractSocketService != null)
        //                            {
        //                                if (abstractSocketService.SessionOpened)
        //                                {
        //                                    abstractSocketService.Send(sendPacket);
        //                                }
        //                                else
        //                                {
        //                                    logger.Warn("Not Open Socket, Fail to Send Packet : " + sendPacket.ToString());
        //                                }
        //                            }
        //                        }

        //                    }
        //                }
        //            }
        //            catch (Exception e)
        //            {

        //            }
        //            finally
        //            {

        //            }
        //#endif
        //        }
    }
}
