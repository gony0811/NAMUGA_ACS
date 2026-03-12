using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using ACS.Extension.Framework.Resource;
using ACS.Extension.Framework.Resource.Model;
using ACS.Framework.Base;
using ACS.Framework.Resource;
using ACS.Manager.Resource;
using NHibernate.Criterion;
using ACS.Framework.Path;
using ACS.Framework.Path.Model;
using ACS.Framework.Resource.Model;
using ACS.Extension.Framework.Path.Model;
using ACS.Extension.Framework.Cache;
using ACS.Extension.Framework.Message.Model;
using ACS.Framework.Transfer.Model;
using ACS.Framework.Transfer;
using ACS.Framework.Alarm;
using ACS.Framework.Alarm.Model;
using ACS.Framework.Message;


namespace ACS.Extension.Manager
{
    public class ResourceManagerExsImplement : ResourceManagerExImplement, IResourceManagerExs
    {
        public ITransferManagerEx TransferManager { get; set; }
        public IAlarmManagerEx AlarmManager { get; set; }
        public IMessageManagerEx MessageManager { get; set; }
        public IPathManagerEx PathManager { get; set; }
        public ICacheManagerEx CacheManager { get; set; }


        public void CreateVehicleIdle(VehicleIdleEx vehicleIdle)
        {
            this.PersistentDao.Save(vehicleIdle);
        }

        public int DeleteVehicleIdle(VehicleIdleEx vehicleIdle)
        {
            StringBuilder sb = new StringBuilder(vehicleIdle.Id);
            return this.PersistentDao.Delete(typeof(VehicleIdleEx), (ISerializable)sb);
        }

        public int DeleteVehicleIdleByVehicleId(string vehicleId)
        {
            return this.PersistentDao.DeleteByAttribute(typeof(VehicleIdleEx), "VehicleId", vehicleId);
        }

        public VehicleIdleEx GetVehicleIdle(string vehicleId)
        {
            StringBuilder sb = new StringBuilder(vehicleId);
            return (VehicleIdleEx)this.PersistentDao.Find(typeof(VehicleIdleEx), (ISerializable)sb);
        }

        public VehicleIdleEx GetVehicleIdleACSByVehicleId(string vehicleId)
        {
            IList vehicleidle = this.PersistentDao.FindByAttribute(typeof(VehicleIdleEx), "VehicleId", vehicleId);
            if (vehicleidle.Count > 0)
            {
                return (VehicleIdleEx)vehicleidle[0];
            }
            return null;
        }
        public VehicleIdleEx GetVehicleIdleByBayID(string bayId)
        {
            DetachedCriteria criteria = DetachedCriteria.For(typeof(VehicleIdleEx));

            criteria.Add(Restrictions.Eq("BayId", bayId));
            criteria.AddOrder(Order.Asc("IdleTime"));

            IList VehicleIdles = this.PersistentDao.FindByCriteria(criteria);

            for (IEnumerator iterator = VehicleIdles.GetEnumerator(); iterator.MoveNext();)
            {
                VehicleIdleEx vehicleIdle = (VehicleIdleEx)iterator.Current;
                VehicleEx vehicle = GetVehicle(vehicleIdle.VehicleId);

                if (vehicle != null)
                {
                    if (vehicle.ConnectionState.Equals(VehicleEx.CONNECTIONSTATE_CONNECT) && vehicle.ProcessingState.Equals(VehicleEx.PROCESSINGSTATE_IDLE)
                        && vehicle.FullState.Equals(VehicleEx.FULLSTATE_EMPTY))
                    {
                        return vehicleIdle;
                    }

                }
            }

            return null;
        }




        public IList GetWaitPointByTypeAndBayId(string type, string bayId)
        {
            DetachedCriteria criteria = DetachedCriteria.For(typeof(WaitPointViewEx));

            criteria.Add(Restrictions.Eq("Type", type));
            criteria.Add(Restrictions.Eq("ZoneId", bayId));

            return this.PersistentDao.FindByCriteria(criteria);
        }

        public bool IsHaveVehicleGoToDestNode(string destNode)
        {
            DetachedCriteria criteria = DetachedCriteria.For(typeof(VehicleEx));

            criteria.Add(Restrictions.Eq("Installed", VehicleEx.INSTALL_INSTALLED));

            ICriterion currentnode = Restrictions.Eq("CurrentNodeId", destNode);
            ICriterion vehicleDestnode = Restrictions.Eq("VehicleDestNodeId", destNode);
            ICriterion acsDestnode = Restrictions.Eq("AcsDestNodeId", destNode);

            LogicalExpression orEx = (LogicalExpression)Restrictions.Or(currentnode, vehicleDestnode);
            LogicalExpression orAll = (LogicalExpression)Restrictions.Or(orEx, acsDestnode);

            criteria.Add(orAll);

            IList values = this.PersistentDao.FindByCriteria(criteria);
            if (values.Count > 0)
                return true;

            return false;
        }
        public Dictionary<string, List<VehicleEx>> MapVehicleByCurrentNode()
        {
            DetachedCriteria criteria = DetachedCriteria.For(typeof(VehicleEx));
            criteria.Add(Restrictions.Eq("Installed", VehicleEx.INSTALL_INSTALLED));
            IList vehicles = this.PersistentDao.FindByCriteria(criteria);

            if (vehicles.Count > 0)
            {
                Dictionary<string, List<VehicleEx>> mapVehicleByCurrentNode = new Dictionary<string, List<VehicleEx>>();

                for (IEnumerator iterator = vehicles.GetEnumerator(); iterator.MoveNext();)
                {
                    VehicleEx vehicle = (VehicleEx)iterator.Current;

                    if (vehicle.CurrentNodeId != null && !string.IsNullOrEmpty(vehicle.CurrentNodeId))
                    {
                        if (mapVehicleByCurrentNode.ContainsKey(vehicle.CurrentNodeId))
                        {
                            List<VehicleEx> ls = new List<VehicleEx>();
                            mapVehicleByCurrentNode.TryGetValue(vehicle.CurrentNodeId, out ls);
                            ls.Add(vehicle);
                        }
                        else
                        {
                            List<VehicleEx> Is = new List<VehicleEx>();
                            Is.Add(vehicle);
                            mapVehicleByCurrentNode.Add(vehicle.CurrentNodeId, Is);
                        }
                    }
                }
                return mapVehicleByCurrentNode;
            }

            return null;
        }

        public IList GetLinkZones()
        {
            return this.PersistentDao.FindAll(typeof(LinkZoneEx));
        }

        public String GetPortTypeByPortId(String portId, BayEx bay)
        {

            String portType = VehicleMessageExs.C_CODE_TYPE_UNDEFINED;

            LocationEx sourceLocation = this.GetLocationByPortId(portId);
            if (sourceLocation != null)
            {
                String direction = sourceLocation.Direction;
                String stationId = sourceLocation.StationId;
                StationEx station = this.CacheManager.GetStationById(stationId);
                String stationType = station.Type;

                if (sourceLocation.Type.Equals(LocationExs.LOCATION_TYPE_CHARGE))
                {
                    return VehicleMessageExs.C_CODE_TYPE_CHARGE;
                }
                else if (sourceLocation.Type.Equals(LocationExs.LOCATION_TYPE_VIRTUAL_BUFF))
                {
                    return VehicleMessageExs.C_CODE_TYPE_UNDEFINED;
                }
                else if (direction.Equals(LocationExs.DIRECTION_LEFT) && stationType.Equals(StationExs.TYPE_ACQUIRE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_LEFTLOAD;
                }
                else if (direction.Equals(LocationExs.DIRECTION_LEFT_BACK) && stationType.Equals(StationExs.TYPE_ACQUIRE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_LEFTLOAD_BACK;
                }
                else if (direction.Equals(LocationExs.DIRECTION_LEFT) && stationType.Equals(StationExs.TYPE_DEPOSITE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_LEFTUNLOAD;
                }
                else if (direction.Equals(LocationExs.DIRECTION_LEFT_BACK) && stationType.Equals(StationExs.TYPE_DEPOSITE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_LEFTUNLOAD_BACK;
                }
                else if (direction.Equals(LocationExs.DIRECTION_RIGHT) && stationType.Equals(StationExs.TYPE_ACQUIRE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_RIGHTLOAD;
                }
                else if (direction.Equals(LocationExs.DIRECTION_RIGHT_BACK) && stationType.Equals(StationExs.TYPE_ACQUIRE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_RIGHTLOAD_BACK;
                }
                else if (direction.Equals(LocationExs.DIRECTION_RIGHT) && stationType.Equals(StationExs.TYPE_DEPOSITE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_RIGHTUNLOAD;
                }
                else if (direction.Equals(LocationExs.DIRECTION_RIGHT_BACK) && stationType.Equals(StationExs.TYPE_DEPOSITE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_RIGHTUNLOAD_BACK;
                }
                else if (stationType.Equals(StationExs.TYPE_BOTH))
                {

                    String locationType = sourceLocation.Type;
                    if (locationType.Equals(LocationExs.LOCATION_TYPE_CHARGE))
                    {

                        portType = VehicleMessageExs.C_CODE_TYPE_CHARGE;
                    }
                    else
                    {

                        portType = VehicleMessageExs.C_CODE_TYPE_BOTH;
                    }
                }
                else
                {

                    logger.Error("please check stationType{" + stationType + "}, " + station);
                }
                // } else {
                //				
                // if(direction.equals(LocationACS.DIRECTION_LEFT) &&
                // stationType.equals(StationACS.TYPE_ACQUIRE)) {
                //					
                // portType = C_CODE_TYPE_LEFTLOAD;
                // } else if(direction.equals(LocationACS.DIRECTION_LEFT) &&
                // stationType.equals(StationACS.TYPE_DEPOSITE)) {
                //					
                // portType = C_CODE_TYPE_LEFTUNLOAD;
                // } else if(direction.equals(LocationACS.DIRECTION_RIGHT) &&
                // stationType.equals(StationACS.TYPE_ACQUIRE)) {
                //					
                // portType = C_CODE_TYPE_RIGHTLOAD;
                // } else if(direction.equals(LocationACS.DIRECTION_RIGHT) &&
                // stationType.equals(StationACS.TYPE_DEPOSITE)) {
                //					
                // portType = C_CODE_TYPE_RIGHTUNLOAD;
                // } else {
                //					
                // logger.error("please check direction{" + direction +
                // "} and stationType{" + stationType + "}, " + station);
                // }
                // }
            }
            else
            {
                logger.Error("location{" + portId + "} does not exist in repository");
            }

            return portType;
        }

        public IList GetVehicles(string bayId, bool containUnavailableVehicle)
        {
            float chargeVoltage = this.GetBayLimitVoltage(bayId);

            DetachedCriteria criteria = DetachedCriteria.For(typeof(VehicleEx));
            if (!containUnavailableVehicle)
            {
                criteria.Add(Restrictions.Eq("ConnectionState", VehicleEx.CONNECTIONSTATE_CONNECT));
                //criteria.Add(Restrictions.Eq("ProcessingState", VehicleEx.PROCESSINGSTATE_IDLE));

                ICriterion idlestate = Restrictions.Eq("ProcessingState", VehicleEx.PROCESSINGSTATE_IDLE);
                ICriterion parkstate = Restrictions.Eq("ProcessingState", VehicleEx.PROCESSINGSTATE_PARK); //20200612 LYS PARK Condition Add

                LogicalExpression orAll = (LogicalExpression)Restrictions.Or(idlestate, parkstate);
                criteria.Add(orAll);

                criteria.Add(Restrictions.Eq("State", VehicleEx.STATE_ALIVE));
                criteria.Add(Restrictions.Gt("BatteryVoltage", chargeVoltage));
                criteria.Add(Restrictions.Eq("BayId", bayId));
                criteria.Add(Restrictions.Eq("FullState", VehicleEx.FULLSTATE_EMPTY));
                criteria.Add(Restrictions.Eq("Installed", VehicleEx.INSTALL_INSTALLED));
                criteria.Add(Restrictions.Not(Restrictions.Eq("Vendor", VehicleEx.VENDOR_FIXMODE)));
                criteria.AddOrder(Order.Asc("NodeCheckTime"));
            }

            IList vehicleList = this.PersistentDao.FindByCriteria(criteria);
            IList returnList = new ArrayList();
            foreach (var item in vehicleList)
            {
                VehicleEx vehicle = (VehicleEx)item;
                TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicle.Id);
                if (transportCommand != null)
                {
                    continue;
                }
                string vehicleId = vehicle.Id;
                bool flag = true;
                IList alarms = this.AlarmManager.GetAlarmsByVehicleId(vehicleId);

                if (alarms != null && alarms.Count > 0)
                {
                    foreach (var alarmitem in alarms)
                    {
                        AlarmEx alarm = (AlarmEx)alarmitem;
                        string alarmId = alarm.AlarmId;
                        AlarmSpecEx alarmSpec = this.AlarmManager.GetAlarmSpecByAlarmId(alarmId);

                        if (string.Equals(alarmSpec.Severity, AlarmSpecEx.SEVERITY_HEAVY))
                        {
                            flag = false;
                            break;
                        }
                    }
                }

                if (flag)
                {
                    returnList.Add(vehicle);
                }
            }
            return returnList;
        }

        public float GetBayLimitVoltage(string bayId)
        {
            StringBuilder sbBayId = new StringBuilder(bayId);
            BayEx bay = (BayEx)PersistentDao.Find(typeof(BayEx), sbBayId, false);

            if (bay == null)
            {
                return (float)23.0;
            }
            return bay.LimitVoltage;
        }

        public bool IsRGV(VehicleEx vehicle)
        {

            bool result = false;
            if (!string.IsNullOrEmpty(vehicle.Vendor) && string.Equals(vehicle.Vendor, BayExs.AGVTYPE_RGV))
            {
                result = true;
            }

            return result;
        }

        public TransportCommandEx GetTransportCommandBySourcePortId(string sourcePortId)
        {
            DetachedCriteria crit = DetachedCriteria.For(typeof(TransportCommandEx));
            crit.Add(Restrictions.Eq("Source", sourcePortId));
            IList transportCommands = this.PersistentDao.FindByCriteria(crit);
            if (transportCommands.Count > 0)
                return (TransportCommandEx)transportCommands[0];
            else
                return null;
        }

        public void CreateVehicleOrder(VehicleOrderEx vehicleOrder)
        {
            PersistentDao.Save(vehicleOrder);
        }

        public int DeleteVehicleOrder(VehicleOrderEx vehicleOrder)
        {
            PersistentDao.Delete(vehicleOrder);

            return 0;
        }

        public int DeleteVehicleOrderByVehicleID(string vehicleId)
        {
            return PersistentDao.DeleteByAttribute(typeof(VehicleOrderEx), "VehicleId", vehicleId);
        }

        public VehicleOrderEx GetVehicleOrderByVehicleId(string vehicleId)
        {
            IList vehicleOrder = PersistentDao.FindByAttribute(typeof(VehicleOrderEx), "VehicleId", vehicleId);
            if (vehicleOrder.Count > 0)
            {
                return (VehicleOrderEx)vehicleOrder[0];
            }
            //List<VehicleOrderEx> vehicleOrder = (List<VehicleOrderEx>)PersistentDao.FindByAttribute(typeof(VehicleOrderEx), "VehicleId", vehicleId);
            //if (vehicleOrder.Count > 0)
            //{
            //    return vehicleOrder[0];
            //}
            return null;
        }

        public string GetPortTypeByPortIdRobot(string destPortId, string sourcePortId)
        {
            string portType = VehicleMessageExs.C_CODE_TYPE_ROBOT_UNDEFINED;

            LocationEx destLocation = this.CacheManager.GetLocationByPortId(destPortId);
            if (destLocation != null)
            {
                String direction = destLocation.Direction;
                String stationId = destLocation.StationId;
                StationEx station = this.CacheManager.GetStationById(stationId);
                String stationType = station.Type;

                // // 2018.07.16 key.han added
                if (direction.Equals(LocationExs.DIRECTION_LEFT) && stationType.Equals(StationExs.TYPE_DEPOSITE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_ROBOT_LEFTUNLOAD;
                }
                else if (direction.Equals(LocationExs.DIRECTION_RIGHT) && stationType.Equals(StationExs.TYPE_DEPOSITE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_ROBOT_RIGHUNLOAD;
                }
                else
                {
                    logger.Error("please check stationType{" + stationType + "}, " + station);
                }
            }
            else
            {
                logger.Error("location{" + destPortId + "} does not exist in repository");
            }
            char lotLoc = sourcePortId[sourcePortId.Length - 1];
            portType = lotLoc + portType;

            return portType;
        }

        public String GetPortTypeByStationId(String stationId, BayEx bay)
        {
            String portType = VehicleMessageExs.C_CODE_TYPE_UNDEFINED;

            LocationEx sourceLocation = this.CacheManager.GetLocationByStationId(stationId);
            if (sourceLocation != null)
            {

                String direction = sourceLocation.Direction;
                StationEx station = this.CacheManager.GetStationById(stationId);
                String stationType = station.Type;

                if (sourceLocation.Type.Equals(LocationExs.LOCATION_TYPE_CHARGE))
                {
                    return VehicleMessageExs.C_CODE_TYPE_CHARGE;
                }
                else if (sourceLocation.Type.Equals(LocationExs.LOCATION_TYPE_VIRTUAL_BUFF))
                {
                    return VehicleMessageExs.C_CODE_TYPE_UNDEFINED;
                }
                else if (direction.Equals(LocationExs.DIRECTION_LEFT) && stationType.Equals(StationExs.TYPE_ACQUIRE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_LEFTLOAD;
                }
                else if (direction.Equals(LocationExs.DIRECTION_LEFT_BACK) && stationType.Equals(StationExs.TYPE_ACQUIRE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_LEFTLOAD_BACK;
                }
                else if (direction.Equals(LocationExs.DIRECTION_LEFT) && stationType.Equals(StationExs.TYPE_DEPOSITE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_LEFTUNLOAD;
                }
                else if (direction.Equals(LocationExs.DIRECTION_LEFT_BACK) && stationType.Equals(StationExs.TYPE_DEPOSITE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_LEFTUNLOAD_BACK;
                }
                else if (direction.Equals(LocationExs.DIRECTION_RIGHT) && stationType.Equals(StationExs.TYPE_ACQUIRE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_RIGHTLOAD;
                }
                else if (direction.Equals(LocationExs.DIRECTION_RIGHT_BACK) && stationType.Equals(StationExs.TYPE_ACQUIRE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_RIGHTLOAD_BACK;
                }
                else if (direction.Equals(LocationExs.DIRECTION_RIGHT) && stationType.Equals(StationExs.TYPE_DEPOSITE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_RIGHTUNLOAD;
                }
                else if (direction.Equals(LocationExs.DIRECTION_RIGHT_BACK) && stationType.Equals(StationExs.TYPE_DEPOSITE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_RIGHTUNLOAD_BACK;
                }
                else if (stationType.Equals(StationExs.TYPE_BOTH))
                {

                    String locationType = sourceLocation.Type;
                    if (locationType.Equals(LocationExs.LOCATION_TYPE_CHARGE))
                    {

                        portType = VehicleMessageExs.C_CODE_TYPE_CHARGE;
                    }
                    else
                    {

                        portType = VehicleMessageExs.C_CODE_TYPE_BOTH;
                    }
                }
                else
                {

                    logger.Error("please check stationType{" + stationType + "}, " + station);
                }
            }
            else
            {
                logger.Error("location{" + stationId + "} does not exist in repository");
            }

            return portType;
        }

        public String GetPortTypeByStationId(String stationId)
        {
            String portType = VehicleMessageExs.C_CODE_TYPE_UNDEFINED;

            LocationEx sourceLocation = this.CacheManager.GetLocationByStationId(stationId);
            if (sourceLocation != null)
            {

                String direction = sourceLocation.Direction;

                StationEx station = this.CacheManager.GetStationById(stationId);
                String stationType = station.Type;
                if (sourceLocation.Type.Equals(LocationExs.LOCATION_TYPE_CHARGE))
                {
                    return VehicleMessageExs.C_CODE_TYPE_CHARGE;
                }
                else if (sourceLocation.Type.Equals(LocationExs.LOCATION_TYPE_VIRTUAL_BUFF))
                {
                    return VehicleMessageExs.C_CODE_TYPE_UNDEFINED;
                }
                else if (direction.Equals(LocationExs.DIRECTION_LEFT) && stationType.Equals(StationExs.TYPE_ACQUIRE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_LEFTLOAD;
                }
                else if (direction.Equals(LocationExs.DIRECTION_LEFT_BACK) && stationType.Equals(StationExs.TYPE_ACQUIRE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_LEFTLOAD_BACK;
                }
                else if (direction.Equals(LocationExs.DIRECTION_LEFT) && stationType.Equals(StationExs.TYPE_DEPOSITE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_LEFTUNLOAD;
                }
                else if (direction.Equals(LocationExs.DIRECTION_LEFT_BACK) && stationType.Equals(StationExs.TYPE_DEPOSITE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_LEFTUNLOAD_BACK;
                }
                else if (direction.Equals(LocationExs.DIRECTION_RIGHT) && stationType.Equals(StationExs.TYPE_ACQUIRE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_RIGHTLOAD;
                }
                else if (direction.Equals(LocationExs.DIRECTION_RIGHT_BACK) && stationType.Equals(StationExs.TYPE_ACQUIRE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_RIGHTLOAD_BACK;
                }
                else if (direction.Equals(LocationExs.DIRECTION_RIGHT) && stationType.Equals(StationExs.TYPE_DEPOSITE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_RIGHTUNLOAD;
                }
                else if (direction.Equals(LocationExs.DIRECTION_RIGHT_BACK) && stationType.Equals(StationExs.TYPE_DEPOSITE))
                {

                    portType = VehicleMessageExs.C_CODE_TYPE_RIGHTUNLOAD_BACK;
                }
                else if (stationType.Equals(StationExs.TYPE_BOTH))
                {

                    String locationType = sourceLocation.Type;
                    if (locationType.Equals(LocationExs.LOCATION_TYPE_CHARGE))
                    {

                        portType = VehicleMessageExs.C_CODE_TYPE_CHARGE;
                    }
                    else
                    {

                        portType = VehicleMessageExs.C_CODE_TYPE_BOTH;
                    }
                }
                else
                {
                    logger.Error("please check stationType{" + stationType + "}, " + station);
                }
            }
            else
            {
                logger.Error("location{" + stationId + "} does not exist in repository");
            }
            return portType;
        }

        public IList GetVehiclesForCharge(String bayId, bool containUnavailableVehicle)
        {
            DetachedCriteria criteria = DetachedCriteria.For(typeof(VehicleEx));

            // added RGV logic
            BayEx bay = this.GetBay(bayId);

            if (!containUnavailableVehicle)
            {
                criteria.Add(Restrictions.Eq("ConnectionState", VehicleEx.CONNECTIONSTATE_CONNECT));

                ICriterion processIdle = Restrictions.Eq("ProcessingState", VehicleEx.PROCESSINGSTATE_IDLE);
                ICriterion processPark = Restrictions.Eq("ProcessingState", VehicleEx.PROCESSINGSTATE_PARK);
                //ICriterion vendorFix = Restrictions.Eq("vendor", BayExs.AGVTYPE_FIXED_MODE);
                LogicalExpression orTransferState = Restrictions.Or(processIdle, processPark) as LogicalExpression;
                //LogicalExpression orVendor = Restrictions.Or(orTransferState, vendorFix) as LogicalExpression;
                criteria.Add(orTransferState);

                // criteria.add(Restrictions.eq("processingState",
                // VehicleACS.PROCESSINGSTATE_IDLE));

                // criteria.add(Restrictions.eq("state", VehicleACS.STATE_ALIVE));
                if (bay == null)
                {
                    logger.Warn("can not find Bay, bayId{" + bayId + "}");
                    return null;
                }
                if (!String.IsNullOrEmpty(bay.AgvType) && bay.AgvType.Equals(BayExs.AGVTYPE_RGV))
                {

                    criteria.Add(Restrictions.Le("BatteryVoltage", VehicleExs.AVAIALBE_VOLTAGE_RGV));
                }
                else
                {

                    criteria.Add(Restrictions.Le("BatteryVoltage", bay.ChargeVoltage));
                }
                criteria.Add(Restrictions.Eq("BayId", bayId));
                criteria.Add(Restrictions.Eq("FullState", VehicleEx.FULLSTATE_EMPTY));
                criteria.Add(Restrictions.Eq("Installed", VehicleEx.INSTALL_INSTALLED));
                criteria.AddOrder(Order.Asc("BatteryVoltage"));
            }

            IList vehicleList = this.PersistentDao.FindByCriteria(criteria);

            List<VehicleEx> returnList = new List<VehicleEx>();
            foreach (VehicleEx vehicle in vehicleList)
            {

                String vehicleId = vehicle.Id;
                bool flag = true;

                if (this.TransferManager.GetTransportCommandByVehicleId(vehicle.Id) == null)
                {
                    //List<AlarmEx> alarms = (List<AlarmEx>)this.AlarmManager.GetAlarmsByVehicleId(vehicleId);

                    IList alarms = this.AlarmManager.GetAlarmsByVehicleId(vehicleId);

                    if (alarms != null && alarms.Count > 0)
                    {
                        foreach (AlarmEx alarm in alarms)
                        {
                            String alarmId = alarm.AlarmId;
                            AlarmSpecEx alarmSpec = this.AlarmManager.GetAlarmSpecByAlarmId(alarmId);

                            if (alarmSpec.Severity.Equals(AlarmSpecEx.SEVERITY_HEAVY))
                            {
                                logger.Info("vehicle : " + vehicleId + " is Heavy Alarm.");
                                flag = false;
                                break;
                            }
                        }
                    }

                    // vehicle이 설비 port위에 위치하고 있을 경우 charge 대상에서 제외
                    // Add AGV have Transferstate =
                    // 'ASSIGNED_DEPOSITING' (This is fixed mode AGV)
                    LocationEx location = this.GetLocationByStationId(vehicle.CurrentNodeId);

                    if (location != null && (BayExs.AGVTYPE_FIXED_MODE.Equals(bay.AgvType) || vehicle.Vendor.Equals(VehicleEx.VENDOR_FIXMODE)))
                    {
                        logger.Info("vehicle : " + vehicleId + " located on the working station like a EQP port.");
                        flag = false;
                    }
                    if (flag)
                    {
                        returnList.Add(vehicle);
                    }
                }
            }

            return returnList;

        }

        public IList GetVehiclesByInterSectionStartNodes(IList startNodeIds)
        {
            // String hql = "from VehicleACS where connectionState = '" +
            // VehicleACS.CONNECTIONSTATE_CONNECT
            // + "' and installed = '" + VehicleACS.INSTALL_INSTALLED + "'";
            //
            // String addHql = StringUtils.EMPTY;
            if (startNodeIds.Count > 0)
            {
                // addHql = "and currentNodeId in (";
                // for (String startNodeId : startNodeIds) {
                // if(StringUtils.isNotEmpty(startNodeId)) {
                // // if(StringUtils.isEmpty(addHql)) {
                // // addHql = " and (currentNodeId = '" + startNodeId + "'";
                // // } else {
                // // addHql = addHql + " or currentNodeId = '" + startNodeId + "'";
                // // }
                // addHql = addHql + "'" + startNodeId + "',";
                // }
                // }
                // addHql = addHql + ")";
                // addHql = addHql.replace(",)", ")");

                DetachedCriteria criteria = DetachedCriteria.For(typeof(VehicleEx));
                criteria.Add(Restrictions.Eq("ConnectionState", VehicleEx.CONNECTIONSTATE_CONNECT));
                criteria.Add(Restrictions.Eq("Installed", VehicleEx.INSTALL_INSTALLED));
                criteria.Add(Restrictions.In("CurrentNodeId", startNodeIds));
                return this.PersistentDao.FindByCriteria(criteria);

                // return this.persistentDao.find(hql.concat(addHql));
            }
            else
            {
                logger.Info("startNodeId list is empty, can not find vehicles on interSectionStartNodes.");
                return new List<VehicleEx>();
            }	
        }


        public IList GetRunningVehiclesByNodeList(IList nodeIds)
        {
            DetachedCriteria criteria = DetachedCriteria.For(typeof(VehicleEx));
		    criteria.Add(Restrictions.In("CurrentNodeId", nodeIds));
		    criteria.Add(Restrictions.Eq("ConnectionState", VehicleEx.CONNECTIONSTATE_CONNECT));
		    criteria.Add(Restrictions.Eq("Installed", Entity.TRUE));
		    return this.PersistentDao.FindByCriteria(criteria);
        }

        public bool CheckNodeIsMonitoringNode(string nodeId)
        {
            int numA = int.Parse(nodeId[0] + "");
            int numB = int.Parse(nodeId[1] + "");
            string numC = nodeId[2] + "";
            string numD = nodeId[3] + "";
            int numCD = int.Parse(nodeId[2] + "" + nodeId[3]);
            // String[] listB = {"3","5","7","9"};
            // String[] listC = {"5","6","7","8"};
            if (numA == 0 && numB == 9 && numCD <= 49)
            {
                return true;
            }
            if (numA > 0 && numB % 2 == 0)
            {
                return true;
            }
            if (numA > 0 && numB % 2 == 1 && numCD <= 49)
            {
                return true;
            }
            //foreach (String bB in listB)
            //{
            //    for (String cC : listC)
            //   {
            //        if (bB.equals(numB) && cC.equals(numC))
            //        {
            //            return false;
            //        }
            //    }
            //}
            return false;
        }

        public string GetNodeTypeByNodeId(string nodeId)
        {
            NodeEx node = this.CacheManager.GetNode(nodeId);
            if (node != null)
            {
                return node.Type;
            }
            return null;
        }

        public int DeleteVehicleOrderACSByVehicleID(string vehicleID)
        {
            return PersistentDao.DeleteByAttribute(typeof(VehicleOrderEx), "vehicleId", vehicleID);
        }

        


        public void CreateVehicleOrderACS(VehicleOrderEx vehicleOrder)
        {
            PersistentDao.Save(vehicleOrder);
        }

        public OrderPairNodeEx SearchNextOrderNode(String path, string bayId)
        {
            string[] pathArr = path.Split('-')[0].Split(':')[0].Split(',');
            if (pathArr.Length > 2)
            {

                LocationEx location = GetLocationByStationId(pathArr[pathArr.Length - 1]);

                for (int i = 1; i < pathArr.Length - 1; i++)
                {
                    string nodeType = this.GetNodeTypeByNodeId(pathArr[i]);


                    if (!string.IsNullOrEmpty(nodeType) && nodeType != null)
                    {
                        if (nodeType.Equals(VehicleMessageExs.TYPE_ORDER, StringComparison.OrdinalIgnoreCase))
                        {
                            //20230314 ORDER_ONEPOINT
                            OrderPairNodeEx orderPair = GetOrderPairByGroup(pathArr[i]);
                            //OrderPairNodeEx orderPair = GetOrderPairByGroup(pathArr[i - 1] + "," + pathArr[i] + "," + pathArr[i + 1]);
                            //

                            //20230417 ORDER_THREE
                            if(orderPair == null)
                            {
                                orderPair = GetOrderPairByGroup(pathArr[i - 1] + "," + pathArr[i] + "," + pathArr[i + 1]);
                            }
                            //

                            if (orderPair != null && orderPair.Status.Equals("NOT USING", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            if (orderPair != null)
                            {
                                OrderPairNodeEx nextOrderPair = null;

                                for (int j = i + 1; j < pathArr.Length - 1; j++)
                                {
                                    string nextNodeType = GetNodeTypeByNodeId(pathArr[j]);
                                    if (!string.IsNullOrEmpty(nextNodeType) && nodeType != null)
                                    {
                                        //20230314 ORDER_ONEPOINT
                                        nextOrderPair = GetOrderPairByGroup(pathArr[j]);
                                        //nextOrderPair = GetOrderPairByGroup(pathArr[j - 1] + "," + pathArr[j] + "," + pathArr[j + 1]);
                                        //

                                        if (nextOrderPair != null)
                                        {
                                            break;
                                        }

                                        //20230417 ORDER_THREE
                                        if (nextOrderPair == null)
                                        {
                                            nextOrderPair = GetOrderPairByGroup(pathArr[j - 1] + "," + pathArr[j] + "," + pathArr[j + 1]);
                                        }

                                        if (nextOrderPair != null)
                                        {
                                            break;
                                        }
                                        //


                                    }
                                }

                                SpecialConfig spec = this.GetValuesBySpecialName("AUTOSWAPTURN");
                                if (spec != null && spec.ContainsValue(bayId))
                                {
                                    if (nextOrderPair != null)
                                    {
                                        if (location != null)
                                        {
                                            if (!string.IsNullOrEmpty(location.Direction) && location.Direction.Contains("BACK"))
                                            {
                                                string orderStatus = orderPair.Status;

                                                if (orderStatus.Equals("ORDER_RF", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    orderPair.Status = "ORDER_RB";
                                                }
                                                else if (orderStatus.Equals("ORDER_LF", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    orderPair.Status = "ORDER_LB";
                                                }
                                                else if (orderStatus.Equals("ORDER_LB", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    orderPair.Status = "ORDER_LF";
                                                }
                                                else if (orderStatus.Equals("ORDER_RB", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    orderPair.Status = "ORDER_RF";
                                                }
                                                //20230307 ORDER_VISION
                                                else if (orderStatus.Equals("ORDER_VISION", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    orderPair.Status = "ORDER_VISION";
                                                    logger.Fatal("20230307: TS : ORDER_VISION");
                                                }
                                                //
                                            }
                                        }
                                    }
                                }
                                return orderPair;

                            }
                        }
                    }
                }
            }
            return null;
        }

        public OrderPairNodeEx SearchNextOrderNode(String path)
        {
            //현재 위치를 포함한 잔여 Path
            string[] pathArr = path.Split('-')[0].Split(':')[0].Split(',');

            if (pathArr.Length > 1)
            {
                for (int i = 1; i < pathArr.Length - 1; i++)
                {
                    string nodeType = this.GetNodeTypeByNodeId(pathArr[i]);
                    if (!string.IsNullOrEmpty(nodeType) && nodeType != null)
                    {
                        if (nodeType.Equals(VehicleMessageExs.TYPE_ORDER))
                        {
                            //20230314 ORDER_ONEPOINT
                            OrderPairNodeEx orderPair = GetOrderPairByGroup(pathArr[i]);
                            if (orderPair != null)
                            {
                                return orderPair;
                            }
                            /*
                            OrderPairNodeEx orderPair = GetOrderPairByGroup(pathArr[i - 1] + "," + pathArr[i] + "," + pathArr[i + 1]);
                            if (orderPair != null)
                            {
                                return orderPair;
                            }
                            */
                            //

                            //20230417 ORDER_THREE
                            if(orderPair == null)
                            {
                                orderPair = GetOrderPairByGroup(pathArr[i - 1] + "," + pathArr[i] + "," + pathArr[i + 1]);
                            }

                            if (orderPair != null)
                            {
                                return orderPair;
                            }
                            //
                        }
                    }
                }
            }
            return null;
        }

        public OrderPairNodeEx GetOrderPairByGroup(String orderGroup)
        {
            //string st = "ORDER_RF";
            DetachedCriteria crit = DetachedCriteria.For(typeof(OrderPairNodeEx));
            crit.Add(Restrictions.Eq("OrderGroup", orderGroup));
            //crit.Add(Restrictions.Eq("Status", st));
            IList orderPair = this.PersistentDao.FindByCriteria(crit);
            if (orderPair.Count > 0)
            {
                return (OrderPairNodeEx)orderPair[0];
            }
            else
            {
                return null;
            }
        }
        public SpecialConfig GetValuesBySpecialName(String specialName)
        {
            DetachedCriteria criteria = DetachedCriteria.For(typeof(SpecialConfig));
		    criteria.Add(Restrictions.Eq("Name", specialName));
		    IList values = this.PersistentDao.FindByCriteria(criteria);
		    if (values.Count == 0)
            {
                return null;
        	}
            else
            {
                return (SpecialConfig)values[0];
            }
		}

        public bool CheckLinkViewByFromNodeAndBayId(String fromNode, String bayId)
        {
            List<LinkViewEx> linkViews = this.CacheManager.GetLinkViewsByFromNodeId(fromNode);
            if (linkViews != null)
            {
                foreach (LinkViewEx linkView in linkViews)
                {
                    if (linkView.BayId.Equals(bayId))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void UpdateVehicleLastChargeBattery(VehicleExs vehicle)
        {
            Dictionary<string, object> setAttributes = new Dictionary<string, object>();
            setAttributes.Add("LastChargeTime", vehicle.LastChargeTime);
            setAttributes.Add("LastChargeBattery", vehicle.LastChargeBattery);

            Dictionary<string, object> conditionAttributes = new Dictionary<string, object>();
            conditionAttributes.Add("Id", vehicle.Id);

            this.PersistentDao.UpdateByAttributes(typeof(VehicleExs), setAttributes, conditionAttributes);
        }

        public void UpdateVehicleIdleTime(VehicleIdleEx vehicleIdle)
        {
            Dictionary<string, object> setAttributes = new Dictionary<string, object>();
            setAttributes.Add("IdleTime", vehicleIdle.IdleTime);
            setAttributes.Add("BayId", vehicleIdle.BayId);

            Dictionary<string, object> conditionAttributes = new Dictionary<string, object>();
            conditionAttributes.Add("VehicleId", vehicleIdle.VehicleId);

            this.PersistentDao.UpdateByAttributes(typeof(VehicleIdleEx), setAttributes, conditionAttributes);
        }

        public void UpdateVehicleOrder(VehicleOrderEx vehicleOrder)
        {
            Dictionary<string, object> setAttributes = new Dictionary<string, object>();
            setAttributes.Add("OrderNode", vehicleOrder.OrderNode);
            setAttributes.Add("OrderTime", vehicleOrder.OrderTime);

            Dictionary<string, object> conditionAttributes = new Dictionary<string, object>();
            conditionAttributes.Add("VehicleId", vehicleOrder.VehicleId);

            PersistentDao.UpdateByAttributes(typeof(VehicleOrderEx), setAttributes, conditionAttributes);
        }

        public bool CheckValueBySpecialConfig(String specialName, string value)
        {
            SpecialConfig spec = GetValuesBySpecialName(specialName);

            bool contains = false;
            if (!String.IsNullOrEmpty(value) && spec != null)
            {
                if (spec.Values != null)
                {
                    if (spec.Values.Equals("ALL")) return true;
                    if (spec.Values.Contains(SpecialConfig.VALUE_DELIMITER))
                    {
                        String[] valueArr = spec.Values.Split(SpecialConfig.VALUE_DELIMITER);
                        foreach (String tempValue in valueArr)
                        {
                            if (value.Equals(tempValue.Trim()))
                            {
                                contains = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (value.Equals(spec.Values))
                        {
                            contains = true;
                        }
                    }
                }
            }
            return contains;
        }

        // 221212 copy
        public BayGroupCharegeEx GetBayGroupCharge(string id)
        {
            StringBuilder sb = new StringBuilder(id);

            try 
            {
                return (BayGroupCharegeEx)this.PersistentDao.Find(typeof(BayGroupCharegeEx), sb, false);
            }
            
            catch (Exception e)
            {
            
            }
            return null;
        }

    }
}

