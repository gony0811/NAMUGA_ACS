using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using ACS.Core.Logging;
using ACS.Core.Resource;
using Autofac;
using ACS.Core.Resource.Model;
using ACS.Core.Message.Model;
using ACS.Core.Alarm;
using ACS.Core.Alarm.Model;
using ACS.Core.Base;

namespace ACS.Communication.Socket.Checker
{
    //181127 Ignore E_Code Overlap 
    //public class DuplicatedCheckerImpl : DuplicateChecker, IApplicationContextAware
    public class DuplicatedCheckerImpl : DuplicateChecker
    {
        public Logger logger = Logger.GetLogger(typeof(DuplicatedCheckerImpl));

        //200423 Change Duplication Logic (Only AGV Number -> AGV Number and CommandCode)
        //protected IDictionary lastReceivedPackets = new Hashtable();
        protected IDictionary<string, IDictionary> commandLastReceivedPackets = new Dictionary<string, IDictionary>();
        //

        protected int maxMessageRepositoryCount = 3;
        protected int allowElapseTime = 5000;
        protected bool useNioNameForDuplicated = false;

        //181127 Ignore E_Code Overlap
        private ILifetimeScope lifetimeScope;
        public IResourceManagerEx ResourceManager { get; set; }
        public IAlarmManagerEx AlarmManager { get; set; }
        public virtual ILifetimeScope LifetimeScope
        {
            set { this.lifetimeScope = value; }
        }


        public virtual bool Duplicate(ReceivePacket receivePacket, string nioId)
        {
            bool result = false;

            //200423 Change Duplication Logic (Only AGV Number -> AGV Number and CommandCode)
            IDictionary lastReceivedPackets = new Hashtable();
            //

            string vehicleId = receivePacket.SendId;
            string vehicleMessage = receivePacket.ToString();       //{A001C123456}

            //181127 Ignore E_Code Overlap 
            string commandCode = receivePacket.Command;

            try
            {
                PacketCheckHolder packetCheckHolder = new PacketCheckHolder(nioId, receivePacket);

                //200423 Change Duplication Logic (Only AGV Number -> AGV Number and CommandCode)
                //IDictionary messageRepository = (IDictionary)this.lastReceivedPackets[vehicleId];
                IDictionary messageRepository = null;
                if (commandLastReceivedPackets.ContainsKey(vehicleId))
                {
                    lastReceivedPackets = this.commandLastReceivedPackets[vehicleId];          //vehicleId
                    messageRepository = (IDictionary)lastReceivedPackets[commandCode];                      //commandCode
                }
                //

                if (messageRepository != null)
                {
                    //messageRepository.Add(vehicleMessage, packetCheckHolder);
                    PacketCheckHolder previous = (PacketCheckHolder)messageRepository[vehicleMessage];

                    if (previous != null)
                    {
                        if (this.useNioNameForDuplicated)
                        {
                            if ((!nioId.Equals(previous.NioId)) &&
                                ((receivePacket.CreateTime - previous.ReceivePacket.CreateTime).TotalMilliseconds <= this.allowElapseTime))
                            {
                                result = true;
                                logger.Info("duplicate ReceivePacket, " + packetCheckHolder);
                            }
                        }
                        else if ((receivePacket.CreateTime - previous.ReceivePacket.CreateTime).TotalMilliseconds <= this.allowElapseTime)
                        {
                            result = true;
                            logger.Info("duplicate ReceivePacket, " + packetCheckHolder);
                        }
                    }
                    else if (messageRepository.Count > this.maxMessageRepositoryCount)
                    {
                        IEnumerator keys = messageRepository.Keys.GetEnumerator();
                        string oldestKey = "";
                        DateTime oldestDate = default(DateTime);
                        while (keys.MoveNext())
                        {
                            string key = (string)keys.Current;
                            PacketCheckHolder value = (PacketCheckHolder)messageRepository[key];
                            if (oldestKey.Length > 0)
                            {
                                if (value.ReceivePacket.CreateTime < oldestDate)
                                {
                                    oldestKey = key;
                                    oldestDate = value.ReceivePacket.CreateTime;
                                }
                            }
                            else
                            {
                                oldestKey = key;
                                oldestDate = value.ReceivePacket.CreateTime;
                            }
                        }
                        messageRepository.Remove(oldestKey);
                        logger.Info("remove oldest Message from messageRepository, key{" + oldestKey + "}");
                    }

                    messageRepository = new Hashtable();
                    messageRepository[vehicleMessage] = packetCheckHolder;

                    //200423 Change Duplication Logic (Only AGV Number -> AGV Number and CommandCode)
                    //this.lastReceivedPackets[vehicleId] = messageRepository;
                    if (commandLastReceivedPackets.ContainsKey(vehicleId))
                    {
                        commandLastReceivedPackets[vehicleId][commandCode] = messageRepository;
                    }
                    else
                    {
                        IDictionary receivedPacketsByCommandCode = new Hashtable();
                        receivedPacketsByCommandCode[commandCode] = messageRepository;
                        this.commandLastReceivedPackets.Add(vehicleId, receivedPacketsByCommandCode);
                    }
                    //

                    //200423 Change Duplication Logic (Only AGV Number -> AGV Number and CommandCode)
                    if (commandCode.Equals("E", StringComparison.OrdinalIgnoreCase))
                    {
                        //if E_CODE, Not duplication
                        result = false;
                    }
                    //

                    //181127 Ignore E_Code Overlap 
                    //E_CODE && Duplciation false
                    if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_E) && result == false)
                    {
                        this.AlarmManager = lifetimeScope.Resolve<IAlarmManagerEx>();
                        AlarmEx alarm = this.AlarmManager.GetAlarmByVehicleId(vehicleId);

                        //200423 Modify Ignore E_Code Overlap (Vehicle Null Exception)
                        this.ResourceManager = lifetimeScope.Resolve<IResourceManagerEx>();
                        VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleId);
                        //

                        //Vehicle Alarm Exist && Duplicate false
                        //200423 Modify Ignore E_Code Overlap (Vehicle Null Exception)
                        //if (alarm != null)
                        if (alarm != null && vehicle != null)
                        //
                        {
                            //200423 Modify Ignore E_Code Overlap (Vehicle Null Exception)
                            //this.ResourceManager = (IResourceManagerEx)applicationContext.GetObject("ResourceManager");
                            //VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleId);
                            //

                            //Same Tag && Same Alarm
                            //200423 Modify Ignore E_Code Overlap (Vehicle Null Exception)
                            //if (alarm.AlarmCode.Equals(vehicle.CurrentNodeId) && alarm.AlarmId.Equals(receivePacket.Data.Substring(1, 3)))
                            if (alarm.AlarmCode.Equals(vehicle.CurrentNodeId) && alarm.AlarmId.Equals(receivePacket.Data.Substring(0, 4)))
                            {
                                //Ignore
                                result = true;
                            }
                        }
                    }
                }
                else
                {
                    messageRepository = new Hashtable();
                    messageRepository[vehicleMessage] = packetCheckHolder;

                    //200423 Change Duplication Logic (Only AGV Number -> AGV Number and CommandCode)
                    //this.lastReceivedPackets[vehicleId] = messageRepository;
                    if (commandLastReceivedPackets.ContainsKey(vehicleId))
                    {
                        this.commandLastReceivedPackets[vehicleId][commandCode] = messageRepository;
                    }
                    else
                    {
                        IDictionary receivedPacketsByCommandCode = new Hashtable();
                        receivedPacketsByCommandCode[commandCode] = messageRepository;
                        this.commandLastReceivedPackets.Add(vehicleId, receivedPacketsByCommandCode);
                    }
                    //

                    logger.Info("created messageRepository with key{" + vehicleMessage + "}," + packetCheckHolder);
                    result = false;
                }
            }
            catch (Exception e)
            {
                logger.Fatal("Duplicate Check Error " + e.ToString());
            }
            //191010 Socket Duplicate Finally 주석처리
            //finally
            //{

            //}

            return result;
        }

        //200423 Ignore garbage Value in ES, Before Biz
        public virtual bool Validate(ReceivePacket rcvPacket)
        {
            bool result = true;

            string commandCode = rcvPacket.Command;

            //validate Command
            if (commandCode.Equals("X"))
            {
                logger.Fatal("NG: Ignore X_CODE - " + rcvPacket.ToString());
                result = false;
            }
            else if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_R))
            {
                string rType = rcvPacket.Data.Substring(0, 2);
                if (rType.Equals("03", StringComparison.OrdinalIgnoreCase))
                {
                    logger.Fatal("NG: Ignore R03_CODE - " + rcvPacket.ToString());
                    result = false;
                }
            }
            else if (commandCode.Equals(VehicleMessageEx.COMMAND_CODE_M))
            {
                string mType = rcvPacket.Data.Substring(0, 2);
                if (mType.Equals("00", StringComparison.OrdinalIgnoreCase))
                {
                    logger.Fatal("NG: Ignore M00_CODE - " + rcvPacket.ToString());
                    result = false;
                }
            }
            else
            {
                //OK
            }

            return result;
        }
        //
    }

}
