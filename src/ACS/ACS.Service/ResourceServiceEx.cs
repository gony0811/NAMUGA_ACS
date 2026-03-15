using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;
using ACS.Core.Alarm.Model;
using ACS.Core.Message.Model;
using ACS.Core.Message.Model.Ui;
using ACS.Core.Resource.Model;
using ACS.Core.Transfer.Model;
using ACS.Core.Material.Model;
using ACS.Core.Path;
using ACS.Core.Path.Model;
using ACS.Core.Alarm;
using ACS.Core.History;
using ACS.Communication.Socket;
using ACS.Utility;
using System.Collections;
using ACS.Communication.Socket.Model;
using ACS.Core.Resource.Model;
using ACS.Core.Path.Model;
using ACS.Manager;
using ACS.Core.Resource;
using ACS.Core.Base;
using ACS.Core.History;
using ACS.Core.History.Model;

namespace ACS.Service
{
    public class ResourceServiceEx : AbstractServiceEx
    {
        public ResourceServiceEx()
            : base()
        { }

        public override void Init()
        {
            base.Init();
        }
        //private IPathManagerEx PathManager;
        //private IAlarmManagerEx AlarmManager;
        private int crossWaitTimeout = 60;

        //public LogManager LogManager{ get { return logManager; } set { this.logManager = value; } }

        public NioInterfaceManager NioInterfaceManager { get; set; }
        public IntersectionControlManagerExImplement IntersectionControlManagerExImplement { get; set; }

        //  public IVehicleIdleManagerEx VehicleIdleManager { get; set; }

        //public void SetLogManager(LogManager logManager)
        //{
        //    //this.logManager = logManager;
        //}

        public int CrossWaitTimeout { get { return crossWaitTimeout; } set { this.crossWaitTimeout = value; } }

        public bool AvailableToTransfer(TransferMessageEx transferMessage)
        {

            bool result = false;
            // TODO 
            // source & dest�뜝�룞�삕 �뜝�룞�삕�뜝�룞�삕 check

            if (!result)
            {
                //			//logger.fine("transportJob will be alternated or rejected, " + transferMessage.getTransportJob(), transferMessage);
            }

            return result;

        }


        public bool CheckVehicleStateIsAssigned(VehicleMessageEx vehicleMessage)
        {
            bool result = false;
            VehicleEx vehicle = new VehicleEx();

            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                vehicleMessage.Vehicle = vehicle;
            }

            TransportCommandEx transportCommand = new TransportCommandEx();

            if (string.IsNullOrEmpty(vehicleMessage.TransportCommandId))
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
            }

            if (transportCommand == null)
            {
                return false;
            }

            if (transportCommand.State == TransportCommandEx.STATE_ASSIGNED)
            {
                return true;
            }

            return false;
        }

        public bool CheckVehicleNodeMismatchAndFly(VehicleMessageEx vehicleMessage)
        {
            bool result = false;
            VehicleEx vehicle = null;
            string beforeReportNodeId;
            string currentReportNodeId;
            NodeEx node = ResourceManager.GetNode(vehicleMessage.NodeId);

            if (vehicleMessage.Vehicle == null)
            {
                vehicle = ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                vehicleMessage.Vehicle = vehicle;
                beforeReportNodeId = vehicleMessage.Vehicle.CurrentNodeId;
            }
            else
            {
                beforeReportNodeId = vehicleMessage.Vehicle.CurrentNodeId;
            }




            if (node == null)
            {
                MismatchAndFlyHistoryEx mismatchAndFlyHistory = new MismatchAndFlyHistoryEx()
                {
                    VehicleId = vehicleMessage.VehicleId,
                    CurrentNodeId = beforeReportNodeId,
                    NgNode = vehicleMessage.NodeId,
                    Type = "MISMATCH",
                    Time = DateTime.Now
                };

                HistoryManager.CreateMismatchAndFlyHistory(mismatchAndFlyHistory);
            }
            else
            {
                currentReportNodeId = vehicleMessage.NodeId;

                String linkId = beforeReportNodeId + "_" + currentReportNodeId;

                LinkEx link = ResourceManager.GetLink(linkId);

                if (link == null)
                {

                    MismatchAndFlyHistoryEx mismatchAndFlyHistory = new MismatchAndFlyHistoryEx()
                    {
                        VehicleId = vehicleMessage.VehicleId,
                        CurrentNodeId = beforeReportNodeId,
                        NgNode = vehicleMessage.NodeId,
                        Type = "FLY",
                        Time = DateTime.Now
                    };

                    HistoryManager.CreateMismatchAndFlyHistory(mismatchAndFlyHistory);

                    result = true;
                }
                else
                {
                    result = false;
                }
            }

            return result;
        }

        public bool CheckVehicleStateIsTransferDest(VehicleMessageEx vehicleMessage)
        {
            bool result = false;
            VehicleEx vehicle = new VehicleEx();

            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                vehicleMessage.Vehicle = vehicle;
            }

            TransportCommandEx transportCommand = new TransportCommandEx();

            if (string.IsNullOrEmpty(vehicleMessage.TransportCommandId))
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
            }

            if (transportCommand == null)
            {
                return false;
            }

            if (transportCommand.State == TransportCommandEx.STATE_TRANSFERRING_DEST)
            {
                return true;
            }

            return false;
        }

        public bool CheckSpecialIntersectionBay(VehicleMessageEx vehicleMessage)
        {

            VehicleEx vehicle = new VehicleEx();
            vehicle = vehicleMessage.Vehicle;
            if (vehicle != null)
            {
                if (ResourceManager.CheckValueBySpecialConfig(SpecialConfig.SPECIAL_INTERSECTION_BAY, vehicle.BayId))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsCarrierLocatedOnSource(TransferMessageEx transferMessage)
        {

            bool result = false;

            CarrierEx carrier = transferMessage.Carrier;
            if (carrier != null)
            {

                if (carrier.CarrierLoc.Equals(transferMessage.SourceMachine + ":" + transferMessage.SourceUnit))
                {
                    //logger.Info("carrier(" + carrier.Id + ").location is equal to source", transferMessage);
                    result = true;

                }
                else
                {
                    //logger.Info("carrier(" + carrier.Id + ").location is not equal to source", transferMessage);
                }
            }
            else
            {

                //logger.Info("can not check source location because carrier is empty", transferMessage);
            }

            return result;
        }

        public VehicleEx SearchSuitableVehicle(TransportCommandEx transportCommand)
        {

            LocationEx sourceLocation = this.PathManager.GetLocationByPortId(transportCommand.Source);
            String stationId = "";
            String bayId = "";
            if (sourceLocation != null)
            {
                stationId = sourceLocation.StationId;

                //String bayId = this.ResourceManager.GetBayIdByStationId(stationId);
                bayId = transportCommand.BayId;

                VehicleEx vehicle = this.PathManager.SearchSuitableVehicle(sourceLocation, bayId);
                if (vehicle != null)
                {
                    return vehicle;
                }
            }
            else
            {
                ////logger.Warn(message, messageName, carrierName, commandId, machineName, unitName);
                //logger.Warn("location does't exist in repository, portId{" + transportCommand.Source + "}", "", transportCommand.CarrierId,transportCommand.Id, bayId, stationId);
            }
            ////logger.Warn(message, messageName, carrierName, commandId, machineName, unitName);
            //		//logger.Warn("can not find Suitable Vehicle, " + sourceLocation.toString(), "", transportCommand.CarrierId, 
            //				transportCommand.Id, bayId, stationId); 
            return null;
        }

        public bool SearchSuitableVehicle(TransferMessageEx transferMessage)
        {

            bool result = false;

            LocationEx sourceLocation = this.PathManager.GetLocationByPortId(transferMessage.SourceMachine + ":" + transferMessage.SourceUnit);
            if (sourceLocation != null)
            {
                //			String stationId = sourceLocation.StationId;
                //			String bayId = this.ResourceManager.GetBayIdByStationId(stationId);

                String bayId = transferMessage.BayId;
                VehicleEx vehicle = this.PathManager.SearchSuitableVehicle(sourceLocation, bayId);
                if (vehicle != null)
                {

                    transferMessage.VehicleId = vehicle.Id;
                    transferMessage.Vehicle = vehicle;
                    return true;
                }
            }
            ////logger.Warn("can not find Suitable Vehicle, " + sourceLocation.toString());
            return result;
        }

        public bool SearchSuitableVehicle(VehicleMessageEx vehicleMessage)
        {

            bool result = false;

            LocationEx sourceLocation = this.PathManager.GetLocationByPortId(vehicleMessage.TransportCommand.Source);
            if (sourceLocation != null)
            {
                String stationId = sourceLocation.StationId;
                String bayId = this.ResourceManager.GetBayIdByStationId(stationId);
                VehicleEx vehicle = this.PathManager.SearchSuitableVehicle(sourceLocation, bayId);
                if (vehicle != null)
                {

                    vehicleMessage.VehicleId = vehicle.Id;
                    vehicleMessage.Vehicle = vehicle;
                    return true;
                }
            }
            ////logger.Warn("can not find Suitable Vehicle, " + sourceLocation.toString());
            return result;
        }

        public bool SearchSuitableChargeVehicle(VehicleMessageEx vehicleMessage)
        {
            bool result = false;

            String bayId = vehicleMessage.BayId;
            LocationEx destLocation = this.PathManager.GetLocationByPortId(vehicleMessage.DestPortId);

            //20171006 wook BCS Rule change
            //		VehicleEx vehicle = this.PathManager.SearchSuitableChargeVehicle(bayId);
            VehicleEx vehicle = this.PathManager.SearchSuitableChargeVehicle(bayId, destLocation);

            if (vehicle != null)
            {

                vehicleMessage.VehicleId = vehicle.Id;
                vehicleMessage.Vehicle = vehicle;
                return true;
            }
            ////logger.Warn("can not find Suitable Vehicle, " + destLocation.toString());
            return result;
        }

        // 221212 copy
        public bool SearchSuitableRechargeStationWithAGV(VehicleMessageEx vehicleMessage)
        {
            String bayId = vehicleMessage.BayId;

            List<LocationViewEx> SearchChargeLocationViews = new List<LocationViewEx>();

            //1.Check Charge Group
            BayGroupCharegeEx ChargeGroupList = this.ResourceManager.GetBayGroupCharge(bayId);

            if (ChargeGroupList != null)
            {
                String[] ChargeGroup = ChargeGroupList.Bays.Split(',');

                for (int i = 0; i < ChargeGroup.Length; i++)
                {
                    String ChargeBayId = ChargeGroup[i];
                    IList ChargeGroupView = this.CacheManager.GetChargeLocationViewsByBayId(ChargeBayId);

                    if (ChargeGroupView != null && ChargeGroupView.Count > 0)
                    {
                        for (IEnumerator iterator = ChargeGroupView.GetEnumerator(); iterator.MoveNext();)
                        {
                            LocationViewEx locationView = (LocationViewEx)iterator.Current;
                            if (!SearchChargeLocationViews.Contains(locationView))
                                SearchChargeLocationViews.Add(locationView);   //Charge Location ("Type" column is "Charge")
                        }
                    }
                }
            }
            else
            {
                SearchChargeLocationViews = this.CacheManager.GetChargeLocationViewsByBayId(bayId);
            }
            //2.Check station and Add
            if (SearchChargeLocationViews != null && SearchChargeLocationViews.Count > 0)
            {
                for (IEnumerator iterator = SearchChargeLocationViews.GetEnumerator(); iterator.MoveNext();)
                {
                    LocationViewEx locationView = (LocationViewEx)iterator.Current;
                    IList OccupiedVehicles = this.ResourceManager.GetVehiclesByCurrentNode(locationView.StationId);

                    bool boolTransportCommand = this.TransferManager.CheckTransportCommandByDestLocationId(locationView.PortId);

                    //이미 할당 된 Station 이거나 위치에 타 AGV 있는지 확인
                    //다른 AGV가 해당 위치를 해당 순간에 지나가거나, 알람 발생으로 멈춰있는데... 배제해야하나????
                    if (!boolTransportCommand && (OccupiedVehicles == null || OccupiedVehicles.Count <= 0))
                    {
                        VehicleEx vehicle = new VehicleEx();

                        vehicleMessage.DestPortId = locationView.PortId;
                        LocationEx destLocation = this.CacheManager.GetLocationByStationId(locationView.StationId);

                        if (destLocation != null)
                        {
                            if (ChargeGroupList != null)
                            {
                                String[] bay = ChargeGroupList.Bays.Split(',');
                                vehicle = this.PathManager.SearchSuitableChargeGroupVehicleDijkstraCache(bay, destLocation);
                            }
                            else
                            {
                                //QD
                                vehicle = this.PathManager.SearchSuitableChargeVehicleDijkstraCache(bayId, destLocation);
                                                                    //
                                //BayEx bay = this.ResourceManager.GetBay(bayId);
                                //vehicle = this.PathManager.SearchSuitableVehicleDijkstraCache(bay, destLocation.StationId);

                            }

                            //220822 1 Charge Station, 2 AGV Charge Assign NG
                            bool boolTransportCmd = false;
                            TransportCommandEx transportCommand = null;
                            if (vehicle != null)
                            {
                                boolTransportCmd = this.TransferManager.CheckTransportCommandByDestLocationId(locationView.PortId);
                                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicle.Id);

                                if (boolTransportCmd == true || transportCommand != null)
                                {
                                    logger.Fatal("public bool SearchSuitableRechargeStationWithAGV(vehicleMessageEx vehicleMessage) : Skip Vehicle - " + locationView.PortId + "," + vehicle.Id + " - " + boolTransportCmd);
                                    continue;
                                }
                            }

                            if (vehicle != null)
                            {
                                vehicleMessage.VehicleId = vehicle.Id;
                                vehicleMessage.Vehicle = vehicle;
                                vehicleMessage.BayId = vehicle.BayId;
                                return true;
                            }
                            else
                            {
                            }
                        }
                    }
                }
            }
            return false; 
        }


        //	public bool SearchSuitableVehicleFromRechargeStation(VehicleMessageEx vehicleMessage) {
        //		
        //		bool result = false;
        //		
        //		if (vehicleMessage.Vehicles == null) 
        //		{
        //			//logger.Info("Target Vehicles is empty" + vehicleMessage.toString());
        //			return result;
        //		} 
        //		
        ////		List listVehicles = vehicleMessage.Vehicles;
        ////		LocationEx destLocation = this.PathManager.GetLocationByPortId(vehicleMessage.DestPortId);
        ////		LocationEx sourceLocation = null;
        ////		int distance = 0;
        ////		Map<String, Integer> mapVehicleDistance = new HashMap();
        ////		String suitableVehicle = "";
        ////		
        ////		for (Iterator iterator = listVehicles.GetEnumerator(); iterator.MoveNext();) {
        ////			VehicleEx vehicle = (VehicleEx) iterator.Current;
        ////			
        ////			String vehicleId = vehicle.Id;
        ////			//logger.Info("Check distance  Vehicle: " + vehicle.Id + ", RechargeStation: " + destLocation.PortId);
        ////			
        ////			if (vehicle.CurrentNodeId == null || vehicle.CurrentNodeId.isEmpty()) 
        ////			{
        ////				//logger.Info("Vehicle Currnt Node is Empty : " +vehicleId);
        ////			}
        ////			//wook
        ////			//?�눖而� TAG??Location???袁⑤빍疫�??�슢揆??pathManager ?�꼷�젟 ?袁⑹뒄
        //////			sourceLocation = this.PathManager.GetLocationByPortId(this.PathManager.getLocationViewByStationId(vehicle.CurrentNodeId).PortId);
        ////			String currentNodeId = vehicle.CurrentNodeId;
        ////			NodeEx sourceNode = this.PathManager.getNode(currentNodeId);
        ////			NodeEx destNode = this.PathManager.getNode(destLocation.StationId);
        ////			
        ////			//logger.Info("Check distance source: " + currentNodeId + ", dest: " + destLocation.PortId);
        ////			//wook
        //////			PathInfoACS pathInfo = this.PathManager.searchPaths(sourceLocation, destLocation);
        ////			PathInfoACS pathInfo = this.PathManager.searchDynamicPaths(null, sourceNode, destNode, false, null, true);
        ////			
        ////			if (pathInfo == null || pathInfo.getPathsAvailable().size() < 1) 
        ////			{
        ////				//logger.Info("can not find Path source: " + sourceNode.Id + ", dest: " + destLocation.PortId);
        ////				break;
        ////			}
        ////			
        ////			PathACS paths = (PathACS) pathInfo.getPathsAvailable().get(0);
        ////			distance = paths.getNodeIds().size();
        ////			//logger.Info("Distance " + vehicle.Id + " AND " + destLocation.PortId + "is " + distance);
        ////			mapVehicleDistance.put(vehicleId, distance);
        ////		}
        ////		
        ////		for (Iterator iterator = mapVehicleDistance.keySet().GetEnumerator(); iterator.MoveNext();) 
        ////		{
        ////			String vehicleId = (String) iterator.Current;
        ////			//logger.Info("Start calculate Vehicle: " + vehicleId + " 's Distance = " + mapVehicleDistance.get(vehicleId));
        ////			
        ////			if (suitableVehicle.isEmpty()) 
        ////			{
        ////				suitableVehicle = vehicleId;
        ////				result = true;
        ////				continue;
        ////			}
        ////			
        ////			if (mapVehicleDistance.get(suitableVehicle) > mapVehicleDistance.get(vehicleId)) 
        ////			{
        ////				suitableVehicle = vehicleId;
        ////			}
        ////			
        ////		}
        ////		//logger.Info("Complete SearchSuiableVehicle Vehicle: " + suitableVehicle + " 's Distance = " + mapVehicleDistance.get(suitableVehicle));
        ////		
        ////		VehicleEx vehicle = this.ResourceManager.GetVehicle(suitableVehicle);
        //		
        //		LocationEx chargeLocation = this.PathManager.GetLocationByPortId(vehicleMessage.DestPortId);
        //		StationACS chargeStation = this.ResourceManager.GetStation(chargeLocation.StationId);
        //		VehicleEx vehicle = this.PathManager.SearchSuitableVehicleFromRecharge(chargeStation, vehicleMessage.BayId, vehicleMessage.Vehicles, false);
        //		
        //		if (vehicle != null) 
        //		{
        //			result = true;
        //			vehicleMessage.VehicleId=(vehicle.Id);
        //			vehicleMessage.Vehicle=vehicle;
        //		}
        //		
        //		return result;
        //	}

        public bool SearchSuitableStockStationFromVehicle(VehicleMessageEx vehicleMessage)
        {

            bool result = false;
            //		VehicleEx vehicle = new VehicleEx();
            //		
            //		if (vehicleMessage.Vehicle != null) 
            //		{
            //			vehicle = vehicleMessage.Vehicle;
            //		} 
            //		else 
            //		{
            //			vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
            //			vehicleMessage.Vehicle=vehicle;
            //		}
            //		
            //		try {
            //			
            //			//揶쏆늿? Bay??StockStation ??筌≪뼐由�
            //			List listStockLocation = this.PathManager.getStockLocationViewsByBayId(vehicle.BayId);
            //			
            //			//Vehicle嚥�?筌�?�닊 Node?類ｋ궖 �뤃�뗫릭疫�?
            //			StationACS sourceStation = this.PathManager.getStation(vehicle.CurrentNodeId);
            //			NodeEx currentNode = this.ResourceManager.GetNode(vehicle.CurrentNodeId);
            //			
            //			if (sourceStation == null) 
            //			{
            //				sourceStation = new StationACS();
            //			}
            //			
            //			//筌≪뼚? StockStation��??袁⑹삺 Vehicle??PaathInfo �뤃�뗫퉸??Map???節딅┛
            //			Map<LocationEx, PathInfoACS> map = new HashMap<LocationEx, PathInfoACS>();
            //			if (listStockLocation == null || listStockLocation.isEmpty()) 
            //			{
            //				return false;
            //			}
            //			else 
            //			{
            //				for (Iterator iterator = listStockLocation.GetEnumerator(); iterator.MoveNext();) 
            //				{
            //					LocationViewEx destLocationView = (LocationViewEx) iterator.Current;
            //					String destLocationPortId = destLocationView.PortId;
            //					LocationEx destLocation = this.ResourceManager.GetLocationByPortId(destLocationPortId);
            //					NodeEx destNode = this.ResourceManager.GetNode(destLocation.StationId);
            //					
            //					PathInfoACS pathInfo = this.PathManager.searchDynamicPaths(sourceStation, currentNode, destNode, false, null, false);
            //					map.put(destLocation, pathInfo);
            //				}
            //				
            //			}
            //			
            //			//筌≪뼚? Map?�눖以� ?�뮇�뵬 揶�?�돱??StockStation 野껉퀣�젟?�꼵由�
            //			LocationEx destLocation = new LocationEx();
            //			PathInfoACS compPathInfo = new PathInfoACS();
            //			
            //			for (Entry<LocationEx, PathInfoACS> keyValue : map.entrySet()) 
            //			{
            //				PathInfoACS currentPathInfo = keyValue.getValue();
            //				
            //				if (compPathInfo == null || compPathInfo.getPathsAvailable().size() == 0) 
            //				{
            //					compPathInfo = currentPathInfo;
            //					destLocation = keyValue.getKey();
            //					result = true;
            //					continue;
            //				}
            //				
            //				//wook 20170626
            //				//STOCK STATION??Link?類ｋ궖�몴??源낆쨯?�뜇六�??野껋럩�뒭????釉� Logic�빊遺�?
            //				if (currentPathInfo.getPathsAvailable() == null || currentPathInfo.getPathsAvailable().size() < 1) 
            //				{
            //					continue;
            //				}
            //				PathACS compPath = (PathACS) compPathInfo.getPathsAvailable().get(0);
            //				PathACS currentPath = (PathACS) currentPathInfo.getPathsAvailable().get(0);
            //				if (compPath.getCost() > currentPath.getCost()) 
            //				{
            //					destLocation = keyValue.getKey();
            //				}
            //			}
            //			
            //			vehicleMessage.setDestPortId(destLocation.PortId);
            //		} 
            //		catch (Exception e) 
            //		{
            //			//logger.Warn("failed to searchSuitableStockStationFromVehicle{" + vehicleMessage.toString() + "}", e);
            //			result = false;
            //		}

            return result;
        }

        public bool GetQueuedTransportCommandbyVehicle(VehicleMessageEx vehicleMessage)
        {

            VehicleEx vehicle = vehicleMessage.Vehicle;
            if (vehicle == null)
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                if (vehicle == null)
                {
                    return false;
                }
            }

            if (this.TransferManager.GetTransportCommandByVehicleId(vehicle.Id) == null)
            {
                IList queuedJob = this.TransferManager.GetQueuedTransportCommandsByBayId(vehicle.BayId);

                if (queuedJob.Count > 0)
                {
                    for (IEnumerator iter = queuedJob.GetEnumerator(); iter.MoveNext();)
                    {

                        TransportCommandEx transportCommand = (TransportCommandEx)iter.Current;
                        ////logger.Info("Try to Find Vehicle for Queued TransportCommand : " + transportCommand.Id);

                        //SDV-057 soonhan.lee

                        if (transportCommand.BayId.Equals(vehicle.BayId))
                        {
                            //logger.Info("Find Queued TransportCommand : " + transportCommand.Id + " Suitable Vehicle : " + vehicle.Id);
                            transportCommand.VehicleId = vehicleMessage.VehicleId;
                            vehicleMessage.TransportCommand = transportCommand;
                            vehicleMessage.TransportCommandId = transportCommand.Id;
                            //vehicleMessage.setCarrierId(transportCommand.Id);
                            vehicleMessage.CarrierId = transportCommand.CarrierId; // 2017.17.18 soonhan.lee
                            return true;
                        }
                        //			    VehicleEx vehicle = this.SearchSuitableVehicle(transportCommand);

                        //			    if(vehicle.Id.EqualsIgnoreCase(vehicleMessage.VehicleId)){
                        //			    	
                        //			    	//logger.Info("Find Queued TransportCommand : " + transportCommand.Id + "Suitable Vehicle : " + vehicleMessage.VehicleId);
                        //			    	transportCommand.VehicleId=(vehicle.Id);
                        //				    vehicleMessage.setTransportCommand(transportCommand);
                        //				    vehicleMessage.setTransportCommandId(transportCommand.Id);
                        //				     return true;
                        //			    }

                    }
                }
                else
                {
                    //logger.Warn("Can Not Find Queued TransportCommand, Vehicle : " + vehicle.toString());
                }
            }
            else
            {
                //logger.Warn("Still have TransportCommand, Vehicle : " + vehicle.Id);
            }
            return false;
        }
        //KSB - 해당 Bay의 Queue에 대기 Job 이 있는지 확인 함수
        public bool GetQueuedTransportCommandbyBayId_RGV(VehicleMessageEx vehicleMessage)
        {

            IList queuedJob = this.TransferManager.GetQueuedTransportCommandsByBayId(vehicleMessage.BayId);

            if (queuedJob.Count > 0)
            {
                return true;
            }
            return false;
        }

        public bool GetQueuedTransportCommandbyBayIdForRGV(VehicleMessageEx vehicleMessage)
        {
            IList queuedJob = this.TransferManager.GetQueuedTransportCommandsByBayId(vehicleMessage.BayId);

            TransportCommandEx oldestTransportCommand = null;

            for (IEnumerator iter = queuedJob.GetEnumerator(); iter.MoveNext();)
            {
                TransportCommandEx transportCommand = iter.Current as TransportCommandEx;

                if (oldestTransportCommand == null)
                {
                    oldestTransportCommand = transportCommand;
                    continue;
                }
                else if (oldestTransportCommand.CreateTime > transportCommand.CreateTime)
                {
                    oldestTransportCommand = transportCommand;
                    continue;
                }
                else
                {
                    continue;
                }
            }

            String bayId = oldestTransportCommand.BayId;
            LocationEx sourceLocation = this.PathManager.GetLocationByPortId(oldestTransportCommand.Source);
            String stationId = "";
            if (sourceLocation != null)
            {
                stationId = sourceLocation.StationId;
                IList vehicles = this.PathManager.GetVehicles(bayId, false);
                if ((vehicles == null) || vehicles.Count < 1)
                {
                    ////logger.Warn(message, messageName, carrierName, commandId, machineName, unitName);
                    //logger.Warn("can not fined available vehicles, bayId{" + bayId + "}", vehicleMessage.MessageName, transportCommand.CarrierId, transportCommand.Id, bayId, transportCommand.Source); 
                    return false;
                }

                //for (IEnumerator iterator = vehicles.GetEnumerator(); iterator.MoveNext(); ) //20200318 LYS Response speed up of TS Logic
                //{
                //    VehicleEx obj = (VehicleEx)iterator.Current;
                //    //logger.Warn(message, messageName, carrierName, commandId, machineName, unitName);
                //    //logger.Warn("Logging Target(" + stationId + ") Candidate-Vehicle : " + object.toString());
                //    logger.Warn("Logging Target(" + stationId + ") Candidate-Vehicle : " + object.toString(), vehicleMessage.MessageName, transportCommand.CarrierId, transportCommand.Id, object.CurrentNodeId, object.Id); 
                //}

                //VehicleEx vehicle = (VehicleEx)vehicles[0];// $$$$$
                VehicleEx vehicle = this.SearchSuitableVehicle(oldestTransportCommand);

                if (vehicle != null)
                {
                    if (this.TransferManager.GetTransportCommandByVehicleId(vehicle.Id) == null)
                    {
                        ////logger.Warn(message, messageName, carrierName, commandId, machineName, unitName);
                        ////logger.Warn("Logging Target(" + stationId + ") Selected-Vehicle : " + vehicle.toString());
                        //logger.Warn("Logging Target(" + stationId + ") Selected-Vehicle : " + vehicle.toString(), vehicleMessage.MessageName, transportCommand.CarrierId, transportCommand.Id, vehicle.CurrentNodeId, vehicle.Id); 

                        ////logger.Info("Find Queued TransportCommand : " + transportCommand.Id + " Suitable Vehicle : " + vehicle.Id);
                        oldestTransportCommand.VehicleId = vehicle.Id;
                        //vehicleMessage.setCarrierId(transportCommand.Id);
                        vehicleMessage.CarrierId = oldestTransportCommand.CarrierId;
                        vehicleMessage.TransportCommand = oldestTransportCommand;
                        vehicleMessage.TransportCommandId = oldestTransportCommand.Id;
                        vehicleMessage.Vehicle = vehicle;
                        vehicleMessage.VehicleId = vehicle.Id;
                        return true;
                    }
                    else
                    {
                        ////logger.Warn(message, messageName, carrierName, commandId, machineName, unitName);
                        //logger.Warn("This Vehicle have already Job - " + transportCommand.Id + ", " + vehicle.Id, vehicleMessage.MessageName, transportCommand.CarrierId, transportCommand.Id, vehicle.CurrentNodeId, vehicle.Id);
                    }
                }
                else
                {
                    ////logger.Warn(message, messageName, carrierName, commandId, machineName, unitName);
                    //logger.Warn("Can not Find Suitable Vehicle, Queued TransportCommand : " + transportCommand.Id, vehicleMessage.MessageName, transportCommand.CarrierId, transportCommand.Id, bayId, transportCommand.Source);
                }
            }

            return false;
        }

        public bool GetQueuedTransportCommandbyBayId(VehicleMessageEx vehicleMessage)
        {
            String agvType = ResourceManager.GetBay(vehicleMessage.BayId).AgvType;

            if (agvType != null && agvType.Equals("RGV"))
            {
                return GetQueuedTransportCommandbyBayIdForRGV(vehicleMessage);
            }
            else
            {

                IList queuedJob = this.TransferManager.GetQueuedTransportCommandsByBayId(vehicleMessage.BayId);

                if (queuedJob.Count > 0)
                {

                    ////logger.Warn("Queued JOB is exist, bayId{" + vehicleMessage.BayId + "}", vehicleMessage.MessageName, "", 
                    //		"", vehicleMessage.BayId, ""); 

                    for (IEnumerator iter = queuedJob.GetEnumerator(); iter.MoveNext();)
                    {
                        TransportCommandEx transportCommand = (TransportCommandEx)iter.Current;
                        ////logger.fine(transportCommand);

                        String bayId = transportCommand.BayId;
                        //LocationEx sourceLocation = this.PathManager.GetLocationByPortId(transportCommand.Source);
                        LocationEx sourceLocation = this.CacheManager.GetLocationByPortId(transportCommand.Source);
                        String stationId = "";
                        if (sourceLocation != null)
                        {
                            stationId = sourceLocation.StationId;
                            BayEx bay = this.ResourceManager.GetBay(bayId);
                            //IList vehicles = this.PathManager.GetVehicles(bayId, false);
                            //if ((vehicles == null) || vehicles.Count < 1)
                            //{
                            //    ////logger.Warn(message, messageName, carrierName, commandId, machineName, unitName);
                            //    //logger.Warn("can not fined available vehicles, bayId{" + bayId + "}", vehicleMessage.MessageName, transportCommand.CarrierId, transportCommand.Id, bayId, transportCommand.Source); 
                            //    return false;
                            //}

                            //for (IEnumerator iterator = vehicles.GetEnumerator(); iterator.MoveNext(); ) //20200318 LYS Response speed up of TS Logic
                            //{
                            //    VehicleEx obj = (VehicleEx)iterator.Current;
                            //    //logger.Warn(message, messageName, carrierName, commandId, machineName, unitName);
                            //    //logger.Warn("Logging Target(" + stationId + ") Candidate-Vehicle : " + object.toString());
                            //    logger.Warn("Logging Target(" + stationId + ") Candidate-Vehicle : " + object.toString(), vehicleMessage.MessageName, transportCommand.CarrierId, transportCommand.Id, object.CurrentNodeId, object.Id); 
                            //}

                            VehicleEx vehicle = null;
                            //20200819 KSG SPECIALCONFIG CHECK ORDER BAY
                            if (ResourceManager.CheckValueBySpecialConfig(SpecialConfig.SPECIAL_ODER_BAY, bayId))
                            {
                                vehicle = this.PathManager.SearchSuitableVehicleDijkstraCache(transportCommand, bay, stationId);
                            }
                            else
                            {
                                vehicle = this.SearchSuitableVehicle(transportCommand);
                            }
                            //VehicleEx vehicle = (VehicleEx)vehicles[0];// $$$$$
                            //VehicleEx vehicle = this.SearchSuitableVehicle(transportCommand);


                            if (vehicle != null)
                            {
                                if (this.TransferManager.GetTransportCommandByVehicleId(vehicle.Id) == null)
                                {
                                    ////logger.Warn(message, messageName, carrierName, commandId, machineName, unitName);
                                    ////logger.Warn("Logging Target(" + stationId + ") Selected-Vehicle : " + vehicle.toString());
                                    //logger.Warn("Logging Target(" + stationId + ") Selected-Vehicle : " + vehicle.toString(), vehicleMessage.MessageName, transportCommand.CarrierId, transportCommand.Id, vehicle.CurrentNodeId, vehicle.Id); 

                                    ////logger.Info("Find Queued TransportCommand : " + transportCommand.Id + " Suitable Vehicle : " + vehicle.Id);
                                    transportCommand.VehicleId = vehicle.Id;
                                    //vehicleMessage.setCarrierId(transportCommand.Id);
                                    vehicleMessage.CarrierId = transportCommand.CarrierId;
                                    vehicleMessage.TransportCommand = transportCommand;
                                    vehicleMessage.TransportCommandId = transportCommand.Id;
                                    vehicleMessage.Vehicle = vehicle;
                                    vehicleMessage.VehicleId = vehicle.Id;
                                    return true;
                                }
                                else
                                {
                                    ////logger.Warn(message, messageName, carrierName, commandId, machineName, unitName);
                                    //logger.Warn("This Vehicle have already Job - " + transportCommand.Id + ", " + vehicle.Id, vehicleMessage.MessageName, transportCommand.CarrierId, transportCommand.Id, vehicle.CurrentNodeId, vehicle.Id);
                                }
                            }
                            else
                            {
                                ////logger.Warn(message, messageName, carrierName, commandId, machineName, unitName);
                                //logger.Warn("Can not Find Suitable Vehicle, Queued TransportCommand : " + transportCommand.Id, vehicleMessage.MessageName, transportCommand.CarrierId, transportCommand.Id, bayId, transportCommand.Source);
                            }
                        }
                    }
                }
                return false;
            }
        }

        //20200310 LYS Add on Function for Parking Station 
        public bool GetSuitableVehicleToSWaitP(VehicleMessageEx vehicleMessage)
        {
            VehicleIdleEx vehicleIdle = ResourceManager.GetVehicleIdleByBayID(vehicleMessage.BayId);

            int idleOverTime = 60; //minutes
            BayEx bay = this.ResourceManager.GetBay(vehicleMessage.BayId);

            try
            {
                if (bay != null)
                {
                    idleOverTime = bay.IdleTime * 60;
                }
            }
            catch (Exception e)
            {
            }

            if (vehicleIdle != null)
            {
                DateTime eventime = vehicleIdle.IdleTime;
                DateTime currentTime = DateTime.Now;

                TimeSpan DateResult = currentTime - eventime;
                double TotalSecond = DateResult.TotalSeconds;

                if (TotalSecond >= idleOverTime)
                {
                    //logger.info("Vehicle [" + vehicleIdle.VehicleId + "] have idle over " + idleOverTime / 60000 + " minutes");

                    TransportCommandEx transportCommand = vehicleMessage.TransportCommand;
                    transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleIdle.VehicleId);

                    if (transportCommand == null)
                    {


                        VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleIdle.VehicleId);
                        if (vehicle != null)
                        {
                            vehicleMessage.Vehicle = vehicle;
                            vehicleMessage.VehicleId = vehicleIdle.VehicleId;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool SearchSuitableRechargeStation(VehicleMessageEx vehicleMessage)
        {

            String destRechargeStation = "";
            VehicleEx vehicle = new VehicleEx();

            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                vehicleMessage.Vehicle = vehicle;
            }

            //20170719 
            String bayId = vehicleMessage.BayId;
            IList listChargeLocationViews = this.PathManager.GetChargeLocationViewsByBayId(bayId);

            if (listChargeLocationViews.Count > 0)
            {
                for (IEnumerator iterator = listChargeLocationViews.GetEnumerator(); iterator.MoveNext();)
                {
                    LocationViewEx locationView = (LocationViewEx)iterator.Current;

                    String stationId = locationView.StationId;
                    //logger.Info("Start to Check RechargeStation : " + locationView.toString());

                    bool boolTransportCommand = this.TransferManager.CheckTransportCommandByDestLocationId(locationView.PortId);

                    if (!boolTransportCommand)
                    {
                        String tagId = locationView.StationId;
                        IList listVehicles = this.ResourceManager.GetVehiclesByCurrentNode(tagId);

                        if (listVehicles == null || listVehicles.Count <= 0)
                        {
                            //logger.Info("Find Empty RechargeStation : " + locationView.PortId);
                            vehicleMessage.DestPortId = locationView.PortId;
                            return true;
                        }
                        else
                        {
                            //logger.Info("This RechargeStation (" + locationView.PortId + ") has Vehicle : " + ((VehicleEx)listVehicles.get(0)).toString());
                        }
                    }
                    else
                    {
                        TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByDestPortId(locationView.PortId);
                        //logger.Info("This RechargeStation (" + locationView.PortId + ") has TransPortCommand : " + transportCommand.toString());

                        if (transportCommand.State.Equals("QUEUED", StringComparison.OrdinalIgnoreCase))
                        {
                            //20190829 added by KSB
                            vehicleMessage.TransportCommand = transportCommand;
                            String tagId = locationView.StationId;
                            IList listVehicles = this.ResourceManager.GetVehiclesByCurrentNode(tagId);

                            if (listVehicles == null || listVehicles.Count <= 0)
                            {
                                //logger.Info("Find Empty RechargeStation : " + locationView.PortId);
                                vehicleMessage.DestPortId = locationView.PortId;
                                return true;
                            }
                            else
                            {
                                //logger.Info("This RechargeStation (" + locationView.PortId + ") has Vehicle : " + ((VehicleEx)listVehicles.get(0)).toString());
                            }
                        }
                    }
                }
            }

            //		if (vehicle != null) 
            //		{
            //			//?�끉�젫嚥≪뮆�뮉 ?�딆뒠?�뜄由�??嚥≪뮇彛�
            //			String bayId = vehicle.BayId;
            //			List listLocationViews = this.PathManager.getLocationViewsByBayId(bayId);
            //			
            //			if (listLocationViews.size() > 0) 
            //			{
            //				for (Iterator iterator = listLocationViews.GetEnumerator(); iterator.MoveNext();) 
            //				{
            //					LocationViewEx locationView = (LocationViewEx) iterator.Current;
            //					
            //					String stationId = locationView.StationId;
            //					
            //					NodeEx node = this.ResourceManager.GetNode(stationId);
            //					
            //					if (node.Type.Equals(NodeEx.TYPE_CHARGE)) 
            //					{
            //						bool boolTransportCommand = this.TransferManager.CheckTransportCommandByDestLocationId(locationView.PortId);
            //						if (!boolTransportCommand) 
            //						{
            //							//20170503 wook ?袁⑹삺 �빊�뫗�읈餓λ쵐�뵥 �빊�뫗�읈??嚥≪뮇彛� �빊遺�?
            //							String tagId = locationView.StationId;
            //							List listVehicles = this.ResourceManager.GetVehiclesByCurrentNode(tagId);
            //							
            ////							if (listVehicles == null) 
            //							if (listVehicles == null || listVehicles.size() <= 0) 
            //							{
            //								//logger.Info("Find Empty RechargeStation : " + locationView.PortId);
            //								vehicleMessage.setDestPortId(locationView.PortId);
            //								return true;
            //							}
            //							
            //						}
            //						//logger.Info("RechargeStation is busy, " + locationView.PortId);
            //					}
            //				}
            //			}
            //			else
            //			{
            //				//logger.Info("can not find Recharge Location with BAY= " + bayId);
            //			}
            //		}
            //		else
            //		{
            //			if (vehicleMessage.Vehicles != null && vehicleMessage.Vehicles.size() > 0)
            //			{
            ////				vehicle = (VehicleEx) vehicleMessage.Vehicles.get(0);
            ////				String bayId = vehicle.BayId;
            //				String bayId = vehicleMessage.BayId;
            //				List listLocationViews = this.PathManager.getLocationViewsByBayId(bayId);
            //				
            //				if (listLocationViews.size() > 0) 
            //				{
            //					for (Iterator iterator = listLocationViews.GetEnumerator(); iterator.MoveNext();) 
            //					{
            //						LocationViewEx locationView = (LocationViewEx) iterator.Current;
            //						
            //						String stationId = locationView.StationId;
            //						
            //						NodeEx node = this.ResourceManager.GetNode(stationId);
            //						
            //						if (node.Type.Equals(NodeEx.TYPE_CHARGE)) 
            //						{
            //							bool boolTransportCommand = this.TransferManager.CheckTransportCommandByDestLocationId(locationView.PortId);
            //							if (!boolTransportCommand) 
            //							{
            //								//20170503 wook ?袁⑹삺 �빊�뫗�읈餓λ쵐�뵥 �빊�뫗�읈??嚥≪뮇彛� �빊遺�?
            //								String tagId = locationView.StationId;
            //								List listVehicles = this.ResourceManager.GetVehiclesByCurrentNode(tagId);
            //								
            //								if (listVehicles == null || listVehicles.size() <= 0) 
            //								{
            //									//logger.Info("Find Empty RechargeStation : " + locationView.PortId);
            //									vehicleMessage.setDestPortId(locationView.PortId);
            //									return true;
            //								}
            //								else
            //								{
            //									//logger.Info("This RechargeStation (" + locationView.PortId + ") has Vehicle : " + ((VehicleEx)listVehicles.get(0)).toString());
            //								}
            //							}
            //							//logger.Info("RechargeStation is busy, " + locationView.PortId);
            //						}
            //					}
            //				}
            //				else
            //				{
            //					//logger.Info("can not find Recharge Location with BAY= " + bayId);
            //				}
            //				
            //			}
            //		}
            return false;
        }

        public bool CheckTransportCommandDestNodeByVehicleCurrentNode(VehicleMessageEx vehicleMessage)
        {

            TransportCommandEx transportCommand = vehicleMessage.TransportCommand;
            if (transportCommand == null)
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }

            if (transportCommand != null)
            {
                String destPortId = transportCommand.Dest;
                LocationEx destLocation = this.PathManager.GetLocationByPortId(destPortId);
                String destNodeId = destLocation.StationId;
                String vehicleCurrentNodeId = "";

                if (vehicleMessage.NodeId != null && !string.IsNullOrEmpty(vehicleMessage.NodeId))
                {
                    vehicleCurrentNodeId = vehicleMessage.NodeId;
                }
                else
                {
                    vehicleCurrentNodeId = vehicleMessage.Vehicle.CurrentNodeId;
                }

                if (vehicleCurrentNodeId.Equals(destNodeId))
                {
                    vehicleMessage.DestNodeId = destNodeId;
                    return true;
                }
                else
                {
                    String message = vehicleMessage.MessageName + " at NG LOCATION, VEHICLE [" + vehicleMessage.VehicleId + "] CURRENT NODE=" + vehicleCurrentNodeId;
                    String desc = "Change the properties of the Tag[" + vehicleCurrentNodeId + "] To ACQUIRE in AGV[" + vehicleMessage.VehicleId + "].";

                    this.CreateUIInform(Inform.INFORM_TYPE_EMERGENCY, message, vehicleMessage.VehicleId, desc);
                    //logger.Error(message + "::" + vehicleMessage.toString(), "", vehicleMessage.MessageName, "", transportCommand.Id, "AGV NG", vehicleMessage.VehicleId, vehicleMessage.MessageName);
                    return false;
                }
            }
            else
            {
                //logger.Warn("can not find TransportCommand, " + vehicleMessage.toString());
                return false;
            }
        }

        public bool CheckTransportCommandSourceNodeByVehicleCurrentNode(VehicleMessageEx vehicleMessage)
        {

            TransportCommandEx transportCommand = vehicleMessage.TransportCommand;
            if (transportCommand == null)
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }

            if (transportCommand != null)
            {
                String sourcePortId = transportCommand.Source;
                LocationEx sourceLocation = this.PathManager.GetLocationByPortId(sourcePortId);

                if(sourceLocation == null)
                {
                    return false;
                }

                String sourceNodeId = sourceLocation.StationId;
                String vehicleCurrentNodeId = "";

                if (vehicleMessage.NodeId != null && !string.IsNullOrEmpty(vehicleMessage.NodeId))
                {
                    vehicleCurrentNodeId = vehicleMessage.NodeId;
                }
                else
                {
                    vehicleCurrentNodeId = vehicleMessage.Vehicle.CurrentNodeId;
                }

                if (vehicleCurrentNodeId.Equals(sourceNodeId))
                {
                    return true;
                }
                else
                {
                    String message = vehicleMessage.MessageName + " at NG LOCATION, VEHICLE [" + vehicleMessage.VehicleId + "] CURRENT NODE=" + vehicleCurrentNodeId;
                    String desc = "Change the properties of the Tag[" + vehicleCurrentNodeId + "] To DEPOSIT in AGV[" + vehicleMessage.VehicleId + "].";

                    this.CreateUIInform(Inform.INFORM_TYPE_EMERGENCY, message, vehicleMessage.VehicleId, desc);

                    //logger.Error(message + "::" + vehicleMessage.toString(), "", vehicleMessage.MessageName, "", transportCommand.Id, "AGV NG", vehicleMessage.VehicleId, vehicleMessage.MessageName);
                    return false;
                }
            }
            else
            {
                //logger.Warn("can not find TransportCommand, " + vehicleMessage.toString());
                return false;
            }
        }
        //20200310 LYS Add on Function for Parking Station 
        public bool SearchSWaitPoint(VehicleMessageEx vehicleMessage)
        {
            String bayId = vehicleMessage.BayId;

            IList listSWaitNodes = ResourceManager.GetWaitPointByTypeAndBayId(WaitPointViewEx.TYPE_SWAIT_P, bayId);

            if (listSWaitNodes.Count > 0)
            {
                for (IEnumerator iterator = listSWaitNodes.GetEnumerator(); iterator.MoveNext();)
                {
                    WaitPointViewEx waitPointView = (WaitPointViewEx)iterator.Current;
                    String waitPointId = waitPointView.Id;
                    if (!ResourceManager.IsHaveVehicleGoToDestNode(waitPointId))
                    {
                        vehicleMessage.DestPortId = waitPointId;
                        return true;
                    }
                }
            }
            //logger.Warn("can not find SWaitPoint, " + vehicle);
            return SearchAWaitPoint(vehicleMessage);
        }
        public bool SearchAWaitPoint(VehicleMessageEx vehicleMessage)
        {
            String bayId = vehicleMessage.BayId;
            //Find Wait Point by view
            IList listSWaitNodes = this.ResourceManager.GetWaitPointByTypeAndBayId(WaitPointViewEx.TYPE_AWAIT_P, bayId);
            if (listSWaitNodes.Count > 0)
            {
                WaitPointViewEx waitPointView = (WaitPointViewEx)listSWaitNodes;
                vehicleMessage.DestPortId = waitPointView.Id;
                return true;
            }
            // return searchWaitPoint(vehicleMessage);
            return false;
        }

        public bool SearchWaitPointForOCode(VehicleMessageEx vehicleMessage)
        {
            VehicleEx vehicle = new VehicleEx();
            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                vehicleMessage.Vehicle = vehicle;
            }
            if (vehicle != null)
            {
                String bayId = vehicle.BayId;

                IList listWp = this.ResourceManager.GetWaitPointByTypeAndBayId("WAIT_P", bayId);

                // Exist wait point in bay of vehicle
                if (listWp.Count > 0)
                {
                    // exist wait point only one
                    if (listWp.Count == 1 && listWp[0] is WaitPointViewEx)
                    {
                        vehicleMessage.DestPortId = (listWp[0] as WaitPointViewEx).Id;
                        return true;
                    }
                    // exist wait points more than one
                    else
                    {
                        String currentNode = vehicle.CurrentNodeId;

                        Dictionary<int, List<String>> nearWp = new Dictionary<int, List<string>>();

                        foreach (WaitPointViewEx wp in listWp)
                        {
                            PathEx vehiclePath = this.PathManager.SearchDynamicPathsDijkstra(currentNode, wp.Id);
                            if (vehiclePath != null)
                            {
                                List<String> lsTr = null;
                                if (nearWp.TryGetValue(vehiclePath.Cost, out lsTr))
                                {
                                    lsTr.Add(wp.Id);
                                }
                                else
                                {
                                    lsTr = new List<string>();
                                    lsTr.Add(wp.Id);
                                    nearWp.Add(vehiclePath.Cost, lsTr);
                                }
                            }
                        }
                                           
                        if (nearWp != null && nearWp.Count > 0)
                        {
                            int minCost = nearWp.Keys.Min();                           
                            vehicleMessage.DestPortId = nearWp[minCost].FirstOrDefault();
                            return true;
                        }
                        else
                        {
                            vehicleMessage.DestPortId = ((WaitPointViewEx)listWp[0]).Id;
                            return true;
                        }
                    }
                }
            }

            logger.Info("can not find WaitPoint, " + vehicle);
            return false;
        }

        public bool SearchWaitPoint(VehicleMessageEx vehicleMessage)
        {
            if(CheckSpecialOrderBay(vehicleMessage))
            {
                return SearchWaitPointForOCode(vehicleMessage);
            }

            VehicleEx vehicle = new VehicleEx();

            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                vehicleMessage.Vehicle = vehicle;
            }

            if (vehicle != null)
            {
                String bayId = vehicle.BayId;
                //			String zoneId = this.ResourceManager.GetZoneByBayId(bayId).Id;
                //			List listLinks = this.ResourceManager.GetLinkZonesByZoneIdTransferFlag(zoneId, "Y");
                //			
                //			if (listLinks != null) 
                //			{
                //				for (Iterator iterator = listLinks.GetEnumerator(); iterator.MoveNext();) 
                //				{
                //					LinkZoneEx linkZone = (LinkZoneEx) iterator.Current;
                //					
                //					LinkEx link = this.ResourceManager.GetLink(linkZone.LinkId);
                //					if(link == null) 
                //						continue;
                //					
                //					String fromNodeId = link.FromNodeId;
                //					
                //					NodeEx node = this.ResourceManager.GetNode(fromNodeId);
                //					if(node == null) 
                //						continue;
                //					
                //					if (node.Type.Equals(NodeEx.TYPE_WAIT_P)) 
                //					{
                //						vehicleMessage.setDestPortId(node.Id);
                //						return true;
                //					}
                //				}
                //			}
                //			else
                //			{
                //				//logger.Warn("can not find Station, " + vehicle.toString());
                //			}

                IList listWaitNodes = this.ResourceManager.GetNodesByType(NodeEx.TYPE_WAIT_P);

                for (IEnumerator iterator = listWaitNodes.GetEnumerator(); iterator.MoveNext();)
                {
                    NodeEx node = (NodeEx)iterator.Current;
                    String nodeId = node.Id;

                    IList listLinkZones = this.PathManager.GetLinkZoneByFromNodeId(nodeId);
                    if (listLinkZones == null)
                    {

                        return false;
                    }
                    for (IEnumerator iterator2 = listLinkZones.GetEnumerator(); iterator2.MoveNext();)
                    {
                        LinkZoneEx linkZone = (LinkZoneEx)iterator2.Current;

                        if (linkZone.ZoneId.Equals(bayId) && linkZone.TransferFlag.Equals("Y"))
                        {
                            vehicleMessage.DestPortId = node.Id;
                            return true;
                        }
                    }
                }
            }
            logger.Warn("can not find WaitPoint, " + vehicle);
            return false;
        }

        public bool SearchWaitPoint(TransferMessageEx transferMessage)
        {

            VehicleEx vehicle = new VehicleEx();

            if (transferMessage.Vehicle != null)
            {
                vehicle = transferMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(transferMessage.VehicleId);
                transferMessage.Vehicle = vehicle;
            }

            if (vehicle != null)
            {
                String bayId = vehicle.BayId;
                //			String zoneId = this.ResourceManager.GetZoneByBayId(bayId).Id;
                //			List listLinks = this.ResourceManager.GetLinkZonesByZoneId(zoneId);
                //			
                //			if (listLinks != null) 
                //			{
                //				for (Iterator iterator = listLinks.GetEnumerator(); iterator.MoveNext();) 
                //				{
                //					LinkZoneEx linkZone = (LinkZoneEx) iterator.Current;
                //					
                //					LinkEx link = this.ResourceManager.GetLink(linkZone.LinkId);
                //					if(link == null) // soonhan.lee 17.06.14
                //						continue;
                //					
                //					String fromNodeId = link.FromNodeId;
                //					
                //					NodeEx node = this.ResourceManager.GetNode(fromNodeId);
                //					if(node == null) // soonhan.lee 17.06.14
                //						continue;
                //					
                //					if (node.Type.Equals(NodeEx.TYPE_WAIT_P)) 
                //					{
                //						transferMessage.DestNodeId=(node.Id);
                //						return true;
                //					}
                //				}
                //			}
                //			else
                //			{
                //				//logger.Warn("can not find Station, " + vehicle.toString());
                //			}

                IList listWaitNodes = this.ResourceManager.GetNodesByType(NodeEx.TYPE_WAIT_P);

                for (IEnumerator iterator = listWaitNodes.GetEnumerator(); iterator.MoveNext();)
                {
                    NodeEx node = (NodeEx)iterator.Current;
                    String nodeId = node.Id;

                    IList listLinkZones = this.PathManager.GetLinkZoneByFromNodeId(nodeId);
                    if (listLinkZones == null)
                    {  // soonhan.lee 17.06.14
                        return false;
                    }
                    for (IEnumerator iterator2 = listLinkZones.GetEnumerator(); iterator2.MoveNext();)
                    {
                        LinkZoneEx linkZone = (LinkZoneEx)iterator2.Current;

                        if (linkZone.ZoneId.Equals(bayId) && linkZone.TransferFlag.Equals("Y"))
                        {
                            transferMessage.DestNodeId = node.Id;
                            return true;
                        }
                    }
                }
            }
            //logger.Warn("can not find WaitPoint, " + vehicle.toString());
            return false;
        }

        public bool SearchStockStation(VehicleMessageEx vehicleMessage)
        {

            VehicleEx vehicle = new VehicleEx();

            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                vehicleMessage.Vehicle = vehicle;
            }

            if (vehicle != null)
            {
                String bayId = vehicle.BayId;
                String zoneId = this.ResourceManager.GetZoneByBayId(bayId).Id;
                IList listLinks = this.ResourceManager.GetLinkZonesByZoneId(zoneId);

                if (listLinks != null)
                {
                    for (IEnumerator iterator = listLinks.GetEnumerator(); iterator.MoveNext();)
                    {
                        LinkZoneEx linkZone = (LinkZoneEx)iterator.Current;

                        LinkEx link = this.ResourceManager.GetLink(linkZone.LinkId);

                        String fromNodeId = link.FromNodeId;

                        NodeEx node = this.ResourceManager.GetNode(fromNodeId);

                        if (node.Type.Equals(NodeEx.TYPE_STOCK_STATION))
                        {
                            vehicleMessage.DestNodeId = node.Id;
                            return true;
                        }
                    }
                }
                else
                {
                    //logger.Warn("can not find Station, " + vehicle.toString());
                }
            }
            //logger.Warn("can not find Stock Station, " + vehicle.toString());
            return false;
        }

        public void ChangeVehicleTransportCommandId(VehicleMessageEx vehicleMessage)
        {

            TransportCommandEx transportCommand = vehicleMessage.TransportCommand;
            if (transportCommand == null)
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }
            if (transportCommand == null)
            {
                //logger.Error("Can Not Find TransportCommand in DB. Vehicle: " + vehicleMessage.VehicleId);
            }
            this.ResourceManager.UpdateVehicleTransportCommandId(vehicleMessage.VehicleId, transportCommand.Id,
                    vehicleMessage.MessageName);
        }

        public void ChangeVehicleTransportCommandId(TransferMessageEx transferMessage)
        {

            TransportCommandEx transportCommand = transferMessage.TransportCommand;
            if (transportCommand == null)
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(transferMessage.VehicleId);
            }
            if (transportCommand == null)
            {
                //logger.Error("Can Not Find TransportCommand in DB. Vehicle: " + transferMessage.VehicleId);
            }

            this.ResourceManager.UpdateVehicleTransportCommandId(transferMessage.VehicleId, transportCommand.Id,
                    transferMessage.MessageName);
        }

        public void ChangeVehicleTransportCommandIdEmpty(VehicleMessageEx vehicleMessage)
        {
            this.ResourceManager.UpdateVehicleTransportCommandId(vehicleMessage.VehicleId, "", vehicleMessage.MessageName);
        }

        public void UpdateVehicleDestNodeId(VehicleMessageEx vehicleMessage)
        {

            VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);

            if (vehicle != null)
            {
                this.ResourceManager.UpdateVehicleVehicleDestNodeId(vehicle, vehicleMessage.StationId, vehicleMessage.MessageName);

                String station2 = vehicleMessage.StationId.Substring(0, 2);
                //if (station2.EqualsIgnoreCase("06") || station2.EqualsIgnoreCase("08"))
                if (station2.Equals("06", StringComparison.OrdinalIgnoreCase) || station2.Equals("08", StringComparison.OrdinalIgnoreCase))
                {

                    TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicle.Id);
                    if (transportCommand != null && !transportCommand.JobType.Equals(TransportCommandEx.JOBTYPE_CHARGEMOVE))
                    {
                        if (vehicle.FullState.Equals(VehicleEx.FULLSTATE_EMPTY))
                        {
                            if (transportCommand.State.Equals(TransportCommandEx.STATE_ASSIGNED))
                            {
                                //vehicle �룯�뜃由�??
                                vehicle.TransportCommandId = "";
                                vehicle.Path = "";
                                vehicle.AcsDestNodeId = "";
                                vehicle.ProcessingState = VehicleEx.PROCESSINGSTATE_IDLE;

                                //TransportCommand �룯�뜃由�??
                                transportCommand.VehicleId = "";
                                transportCommand.Path = "";
                                transportCommand.State = TransportCommandEx.STATE_QUEUED;

                                this.TransferManager.UpdateTransportCommand(transportCommand);
                                //logger.Info("This Vehicle's TransportCommand change Queued. Vehicle: " + vehicle.Id + "JOB: " + transportCommand.Id);
                            }
                        }
                    }

                    ChangeVehicleProcessStateToCharge(vehicleMessage);
                }
            }
            else
            {
                //logger.Error("Can not find Vehicle : " + vehicleMessage.VehicleId);
            }

        }

        public bool UpdateVehicleRunState(VehicleMessageEx vehicleMessage)
        {
            int count = this.ResourceManager.UpdateVehicleRunState(vehicleMessage.VehicleId, vehicleMessage.RunState,
                    vehicleMessage.MessageName);
            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool UpdateVehicleFullState(VehicleMessageEx vehicleMessage)
        {
            int count = this.ResourceManager.UpdateVehicleFullState(vehicleMessage.VehicleId, vehicleMessage.FullState,
                    vehicleMessage.MessageName);
            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void UpdateVehicleNodeCheckTime(VehicleMessageEx vehicleMessage)
        {
            this.ResourceManager.UpdateVehicleNodeCheckTime(vehicleMessage.VehicleId);
        }

        public bool UpdateVehicleAlarmStateToNoAlarm(VehicleMessageEx vehicleMessage)
        {
            int count = this.ResourceManager.UpdateVehicleAlarmState(vehicleMessage.VehicleId,
                    VehicleEx.ALARMSTATE_NOALARM, vehicleMessage.MessageName);
            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool UpdateVehicleAlarmStateToAlarm(VehicleMessageEx vehicleMessage)
        {

            int count = this.ResourceManager.UpdateVehicleAlarmState(vehicleMessage.VehicleId, VehicleEx.ALARMSTATE_ALARM, vehicleMessage.MessageName);
            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool UpdateVehicleVoltage(VehicleMessageEx vehicleMessage)
        {

            if (vehicleMessage.BatteryVoltage < 1)
            {
                return false;
            }
            int count = this.ResourceManager.UpdateVehicleBatteryVoltage(vehicleMessage.VehicleId, vehicleMessage.BatteryVoltage);
            //		if(count > 0){
            //			this.historyManager.createVehicleBatteryHistory(vehicleMessage);
            //		}
            return true;
        }

        public void UpdateVehicleCurrentNodeByCharge(VehicleMessageEx vehicleMessage)
        {

            TransportCommandEx transportCommand = vehicleMessage.TransportCommand;

            if (transportCommand == null)
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }

            String destPortId = transportCommand.Dest;
            String destNodeId = this.PathManager.GetLocationByPortId(destPortId).StationId;

            this.ChangeVehicleLocation(vehicleMessage.VehicleId, destNodeId, vehicleMessage.MessageName);
        }

        public bool ChangeVehicleProcessStateToRun(VehicleMessageEx vehicleMessage)
        {

            int count = this.ResourceManager.UpdateVehicleProcessingState(vehicleMessage.VehicleId,
                    VehicleEx.PROCESSINGSTATE_RUN, vehicleMessage.MessageName);

            this.DeleteVehicleIdleByVehicleId(vehicleMessage.VehicleId); //200320 LYS
            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ChangeVehicleProcessStateToRun(TransferMessageEx transferMessage)
        {

            int count = this.ResourceManager.UpdateVehicleProcessingState(transferMessage.VehicleId,
                    VehicleEx.PROCESSINGSTATE_RUN, transferMessage.MessageName);

            this.DeleteVehicleIdleByVehicleId(transferMessage.VehicleId); //200320 LYS
            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ChangeVehicleProcessStateToIdle(VehicleMessageEx vehicleMessage)
        {

            int count = this.ResourceManager.UpdateVehicleProcessingState(vehicleMessage.VehicleId,
                    VehicleEx.PROCESSINGSTATE_IDLE, vehicleMessage.MessageName);

            if (!checkVehicleIdle(vehicleMessage.VehicleId))
            {
                createVehicleIdle(vehicleMessage);
            }
            else
            {
                // changeVehicleStateToAlive(uiMoveVehicleMessage.getVehicleId());
                if (!string.Equals(vehicleMessage.MessageName, "RAIL-VEHICLEINFOREPORT"))
                    updateVehicleIdle(vehicleMessage);
            }

            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ChangeVehicleProcessStateToIdle(TransferMessageEx transferMessage)
        {

            if (transferMessage.VehicleId != null && !string.IsNullOrEmpty(transferMessage.VehicleId))
            {
                int count = this.ResourceManager.UpdateVehicleProcessingState(transferMessage.VehicleId,
                        VehicleEx.PROCESSINGSTATE_IDLE, transferMessage.MessageName);

                if (!checkVehicleIdle(transferMessage.VehicleId))
                    createVehicleIdle(transferMessage);
                else
                {
                    // changeVehicleStateToAlive(uiMoveVehicleMessage.getVehicleId());
                    updateVehicleIdle(transferMessage);
                }

                if (count == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return false;

        }

        public bool ChangeVehicleProcessStateToCharge(VehicleMessageEx vehicleMessage)
        {

            int count = this.ResourceManager.UpdateVehicleProcessingState(vehicleMessage.VehicleId,
                    VehicleEx.PROCESSINGSTATE_CHARGE, vehicleMessage.MessageName);
            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        //20200310 LYS Add on Function for Parking Station 
        public bool ChangeVehicleProcessingStateToParking(VehicleMessageEx vehicleMessage)
        {
            DeleteVehicleIdleByVehicleId(vehicleMessage.VehicleId);
            int count = this.ResourceManager.UpdateVehicleProcessingState(vehicleMessage.VehicleId,
                    VehicleEx.PROCESSINGSTATE_PARK);
            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ChangeVehicleConnectionStateToConnect(TransferMessageEx transferMessage)
        {

            int count = this.ResourceManager.UpdateVehicleConnectionState(transferMessage.VehicleId,
                    VehicleEx.CONNECTIONSTATE_CONNECT, transferMessage.MessageName);
            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ChangeVehicleConnectionStateToDisconnect(TransferMessageEx transferMessage)
        {
            int count = this.ResourceManager.UpdateVehicleConnectionState(transferMessage.VehicleId,
                    VehicleEx.CONNECTIONSTATE_DISCONNECT, transferMessage.MessageName);
            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ChangeVehicleConnectionStateToConnect(VehicleMessageEx vehicleMessage)
        {
            int count = this.ResourceManager.UpdateVehicleConnectionState(vehicleMessage.VehicleId,
                    VehicleEx.CONNECTIONSTATE_CONNECT, vehicleMessage.MessageName);
            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ChangeVehicleConnectionStateToDisconnect(VehicleMessageEx vehicleMessage)
        {
            int count = this.ResourceManager.UpdateVehicleConnectionState(vehicleMessage.VehicleId,
                    VehicleEx.CONNECTIONSTATE_DISCONNECT, vehicleMessage.MessageName);
            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ChangeVehiclesConnectionStateToDisconnect(VehicleMessageEx vehicleMessage)
        {

            IList vehicleList = vehicleMessage.Vehicles;
            int count = 0;

            if (vehicleList != null && vehicleList.Count > 0)
            {
                for (IEnumerator iterator = vehicleList.GetEnumerator(); iterator.MoveNext();)
                {
                    VehicleEx vehicle = (VehicleEx)iterator.Current;

                    if (!"DISCONNECT".Equals(vehicle.ConnectionState, StringComparison.OrdinalIgnoreCase))
                    {
                        //only WIFI Mode AGV
                        IList nios = this.NioInterfaceManager.GetNioes(vehicle.NioId);
                        if (nios == null || nios.Count < 1)
                        {
                            continue;
                        }

                        //					//20170815 wook JOB??揶�??�⑥쥙�뿳?袁⑸뻻 Queue?怨밴묶嚥�??�떯?
                        //					TransportCommandEx transportCommand =  this.TransferManager.GetTransportCommandByVehicleId(vehicle.Id);
                        //					if (transportCommand != null && !transportCommand.JobType.Equals(TransportCommandEx.JOBTYPE_CHARGEMOVE)) 
                        //					{
                        //						if (vehicle.FullState.Equals(VehicleEx.FULLSTATE_EMPTY) && transportCommand.getLoadedTime() == null) 
                        //						{
                        //							//vehicle �룯�뜃由�??
                        //							vehicle.setTransportCommandId("");
                        //							vehicle.setPath("");
                        //							vehicle.setAcsDestNodeId("");
                        //							vehicle.setProcessingState(VehicleEx.PROCESSINGSTATE_IDLE);
                        //							vehicle.setTransferState(VehicleEx.TRANSFERSTATE_NOTASSIGNED);
                        //							
                        //							//TransportCommand �룯�뜃由�??
                        //							transportCommand.VehicleId=("");
                        //							transportCommand.setPath("");
                        //							transportCommand.State=(TransportCommandEx.STATE_QUEUED);
                        //							
                        //							this.TransferManager.updateTransportCommand(transportCommand);
                        //							this.ResourceManager.UpdateVehicle(vehicle);
                        //							//logger.Info("This Vehicle's TransportCommand change Queued. Vehicle: " + vehicle.Id + "JOB: " + transportCommand.Id);
                        //						}
                        //					}
                        this.ResourceManager.UpdateVehicleConnectionState(vehicle.Id, VehicleEx.CONNECTIONSTATE_DISCONNECT,
                                vehicleMessage.MessageName);
                        //					this.historyManager.createVehicleHistory(vehicle);
                        logger.Debug("This Vehicle is disconnected. Vehicle: [" + vehicle.Id + "]");

                        ////SJP
                        ////this.MessageManager.SendRestartNio(vehicle);

                        ////181109 if Wifi, Try to Restart NIO
                        ////20190905 KSB 이상해서 다시 사용
                        //Nio nio = this.NioInterfaceManager.GetNio(vehicle.NioId);

                        //if (nio.MachineName.Equals("WIFI"))
                        //{
                        //    this.MessageManager.SendRestartNio(vehicle);
                        //}
                    }
                    else
                    {
                        IList nios = this.NioInterfaceManager.GetNioes(vehicle.NioId);
                        for (IEnumerator iterator2 = nios.GetEnumerator(); iterator2.MoveNext();)
                        {

                            Nio nio = (Nio)iterator2.Current;
                            if (nio.State.Equals(Nio.NIO_STATE_CONNECED))
                            {
                                ////20190905 KSB 이상해서 다시 사용
                                //if (nio.MachineName.Equals("WIFI"))
                                //{
                                //    this.MessageManager.SendRestartNio(vehicle);
                                //}
                            }
                        }
                        //logger.Info("This Vehicle is already disconnected. Vehicle: [" + vehicle.Id + "]");
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ChangeVehicleTransferStateToAssigned(VehicleMessageEx vehicleMessage)
        {

            int count = this.ResourceManager.UpdateVehicleTransferState(vehicleMessage.VehicleId,
                    VehicleEx.TRANSFERSTATE_ASSIGNED, vehicleMessage.MessageName);
            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ChangeVehicleTransferStateToParked(VehicleMessageEx vehicleMessage)
        {
            int count = this.ResourceManager.UpdateVehicleTransferState(vehicleMessage.VehicleId,
                    VehicleEx.TRANSFERSTATE_ASSIGNED_PARKED, vehicleMessage.MessageName);
            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ChangeVehicleTransferStateToAcquireComplete(VehicleMessageEx vehicleMessage)
        {
            int count = this.ResourceManager.UpdateVehicleTransferState(vehicleMessage.VehicleId,
                    VehicleEx.TRANSFERSTATE_ACQUIRE_COMPLETE, vehicleMessage.MessageName);
            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ChangeVehicleTransferStateToAcquireComplete(TransferMessageEx TransferMessageEx)
        {

            int count = this.ResourceManager.UpdateVehicleTransferState(TransferMessageEx.VehicleId,
                    VehicleEx.TRANSFERSTATE_ACQUIRE_COMPLETE, TransferMessageEx.MessageName);
            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ChangeVehicleTransferStateToDepositComplete(VehicleMessageEx vehicleMessage)
        {

            int count = this.ResourceManager.UpdateVehicleTransferState(vehicleMessage.VehicleId,
                    VehicleEx.TRANSFERSTATE_DEPOSIT_COMPLETE, vehicleMessage.MessageName);
            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ChangeVehicleTransferStateToNotAssigned(VehicleMessageEx vehicleMessage)
        {
            int count = this.ResourceManager.UpdateVehicleTransferState(vehicleMessage.VehicleId,
                    VehicleEx.TRANSFERSTATE_NOTASSIGNED, vehicleMessage.MessageName);
            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool ChangeVehicleTransferStateToNotAssigned(TransferMessageEx transferMessage)
        {
            int count = this.ResourceManager.UpdateVehicleTransferState(transferMessage.VehicleId,
                    VehicleEx.TRANSFERSTATE_NOTASSIGNED, transferMessage.MessageName);
            if (count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        //	public bool ChangeVehicleBatteryVoltage(VehicleMessageEx vehicleMessage) {
        //
        //		int count = this.ResourceManager.UpdateVehicleBatteryVoltage(vehicleMessage.Vehicle, vehicleMessage.getBatteryVoltage());
        //		if(count > 0){
        //			this.historyManager.createVehicleBatteryHistory(vehicleMessage);
        //		}
        //		if (count == 0) 
        //		{
        //			return false;
        //		} 
        //		else 
        //		{
        //			return true;
        //		}
        //	}

        //	public bool ChangeVehicleBatteryRate(VehicleMessageEx vehicleMessage) {
        //
        //		int count = this.ResourceManager.UpdateVehicleBatteryRate(vehicleMessage.VehicleId, vehicleMessage.getBatteryRate());
        //		if(count > 0){
        //			this.historyManager.createVehicleBatteryHistory(vehicleMessage);
        //		}
        //		if (count == 0) 
        //		{
        //			return false;
        //		} 
        //		else 
        //		{
        //			return true;
        //		}
        //	}

        //	public bool ChangeVehicleBatteryState(VehicleMessageEx vehicleMessage) {
        //		String bType = vehicleMessage.KeyData.Substring(1, 2);
        //		if(bType.EqualsIgnoreCase("1")){
        //			bool count = this.changeVehicleBatteryVoltage(vehicleMessage);
        //			this.historyManager.createVehicleBatteryHistory(vehicleMessage);
        //			return count;
        //		}else if(bType.EqualsIgnoreCase("2")){
        //			bool count = this.changeVehicleBatteryRate(vehicleMessage);
        //			this.historyManager.createVehicleBatteryHistory(vehicleMessage);
        //			return count;
        //		}else{
        //			//logger.Error("Battery Type must be 1 or 2 !!  data = [" + vehicleMessage.KeyData + "]");
        //			return false;
        //		}
        //	}

        public void ChangeVehicleLocation(VehicleMessageEx vehicleMessage)
        {
            this.ChangeVehicleLocation(vehicleMessage.VehicleId, vehicleMessage.NodeId, vehicleMessage.MessageName);
            // 필요여부??
            VehicleEx vehicle = vehicleMessage.Vehicle;
            vehicle.CurrentNodeId = vehicleMessage.NodeId;
            vehicleMessage.Vehicle = vehicle;
        }


        public void ChangeVehicleProcessingState(VehicleMessageEx vehicleMessage)
        {
            VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
            if (vehicle != null)
            {

                if (string.IsNullOrEmpty(vehicle.VehicleDestNodeId)) return;
                String st2 = vehicle.VehicleDestNodeId.Substring(0, 2);
                //if (VEHICLE.STATUS_CHARGE.EqualsIgnoreCase(vehicle.ProcessingState)
                //    && !st2.EqualsIgnoreCase("06")
                //    && !st2.EqualsIgnoreCase("08"))
                if (VehicleEx.STATUS_CHARGE.Equals(vehicle.ProcessingState, StringComparison.OrdinalIgnoreCase)
                 && !st2.Equals("06", StringComparison.OrdinalIgnoreCase)
                 && !st2.Equals("08", StringComparison.OrdinalIgnoreCase))
                {
                    this.ResourceManager.UpdateVehicleProcessingState(vehicleMessage.VehicleId, VehicleEx.PROCESSINGSTATE_IDLE,
                            vehicleMessage.MessageName);
                    //logger.Info("changed vehicle(" + vehicleMessage.VehicleId + ") processing state to " + VehicleEx.PROCESSINGSTATE_IDLE);

                    TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicle.Id);
                    if (transportCommand != null && transportCommand.JobType.Equals(TransportCommandEx.JOBTYPE_CHARGEMOVE))
                    {
                        logger.Info("delete Charge TransportCommand (" + transportCommand.Id + ")");
                        this.TransferManager.DeleteTransportCommand(transportCommand.Id);
                    }
                }
            }
        }

        public void ChangeVehicleLocation(String vehicleId, String nodeId)
        {
            VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleId);
            //NodeEx node = this.ResourceManager.GetNode(nodeId);
            NodeEx node = this.CacheManager.GetNode(nodeId);

            if (vehicle != null)
            {
                if (node != null)
                {
                    //logger.Info("changed vehicle.node{" + nodeId + "}");
                    this.ResourceManager.UpdateVehicleLocation(vehicle, nodeId);
                }
                else
                {
                    //logger.Error("node{" + nodeId + "} does not exist in repository. Vehicle now on : " + vehicle.CurrentNodeId);
                }
            }
            else
            {
                //logger.Warn("vehicle{" + vehicleId + "} does not exist in repository");
            }
        }

        public void ChangeVehicleLocation(String vehicleId, String nodeId, String messageName)
        {

            VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleId);
            //NodeEx node = this.ResourceManager.GetNode(nodeId);
            NodeEx node = this.CacheManager.GetNode(nodeId);
            if (vehicle != null)
            {
                if (node != null)
                {
                    //logger.Info("changed vehicle.node{" + nodeId + "}");
                    this.ResourceManager.UpdateVehicleLocation(vehicle, nodeId, messageName);
                }
                else
                {
                    if (this.ResourceManager.CheckNodeIsMonitoringNode(nodeId))
                    {
                        //logger.Error("node{" + nodeId + "} does not exist in repository. Vehicle now on : " + vehicle.CurrentNodeId)
                    }
                    //logger.Warn("node{" + nodeId + "} does not exist in repository");
                }
            }
        }

        public String GetTCodeType(VehicleMessageEx vehicleMessage)
        {
            String TCodeType = vehicleMessage.KeyData.Substring(1, 2);
            return TCodeType;
        }


        public String GetRunState(VehicleMessageEx vehicleMessage)
        {
            return vehicleMessage.RunState;
        }

        public String GetFullState(VehicleMessageEx vehicleMessage)
        {
            return vehicleMessage.FullState;
        }

        public bool CheckVehicleFullStateEmpty(VehicleMessageEx vehicleMessage)
        {
            bool result = false;
            String fullState = "";

            if (vehicleMessage.Vehicle != null)
            {
                fullState = vehicleMessage.Vehicle.FullState;
            }
            else
            {
                VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                vehicleMessage.Vehicle = vehicle;
                fullState = vehicle.FullState;
            }

            if (fullState.Equals(VehicleEx.FULLSTATE_EMPTY))
            {
                result = true;
            }
            else
            {
                result = false;
            }

            return result;
        }

        public bool CheckVehicleChargeState(VehicleMessageEx vehicleMessage)
        {
            bool result = false;
            String processingState = "";

            if (vehicleMessage.Vehicle != null)
            {
                processingState = vehicleMessage.Vehicle.ProcessingState;
            }
            else
            {
                VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                vehicleMessage.Vehicle = vehicle;
                processingState = vehicle.ProcessingState;
            }

            if (processingState.Equals(VehicleEx.PROCESSINGSTATE_CHARGE))
            {
                result = true;
            }
            else
            {
                result = false;
            }

            return result;
        }


        public bool IsCrossStartNode(VehicleMessageEx vehicleMessage)
        {

            if (vehicleMessage.Node != null)
            {
                if (vehicleMessage.Node.Type.Equals(NodeEx.TYPE_CROSS_S))
                {
                    //logger.Info("This node{" + vehicleMessage.NodeId + "}, is Cross Start Node!!");
                    return true;
                }
                else
                    return false;
            }
            else
            {
                //NodeEx node = this.ResourceManager.GetNode(vehicleMessage.NodeId);
                NodeEx node = this.CacheManager.GetNode(vehicleMessage.NodeId); //20200612 LYS CacheManager Change

                if (node != null)
                {
                    vehicleMessage.Node = node;
                    if (node.Type.Equals(NodeEx.TYPE_CROSS_S))
                    {
                        //logger.Info("This node{" + vehicleMessage.NodeId + "}, is Cross Start Node!!");
                        return true;
                    }
                    else
                        return false;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsCrossEndNode(VehicleMessageEx vehicleMessage)
        {

            if (vehicleMessage.Node != null)
            {
                if (vehicleMessage.Node.Type.Equals(NodeEx.TYPE_CROSS_E))
                {
                    //logger.Info("This node{" + vehicleMessage.NodeId + "}, is Cross End Node!!");
                    return true;
                }
                else
                    return false;
            }
            else
            {
                NodeEx node = this.ResourceManager.GetNode(vehicleMessage.NodeId);

                if (node != null)
                {
                    if (node.Type.Equals(NodeEx.TYPE_CROSS_E))
                    {
                        //logger.Info("This node{" + vehicleMessage.NodeId + "}, is Cross End Node!!");
                        return true;
                    }
                    else
                        return false;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsVehicleInCrossWaitByStartNodeId(VehicleMessageEx vehicleMessage)
        {

            VehicleCrossWaitEx vehicleCrossWait = this.ResourceManager.GetVehicleCrossWaitAllByNodeId(vehicleMessage.NodeId);

            if (vehicleCrossWait != null)
            {
                if (vehicleCrossWait.VehicleId.Equals(vehicleMessage.VehicleId, StringComparison.OrdinalIgnoreCase))
                {
                    //logger.Info("This node{" + vehicleMessage.NodeId + "} is not Busy!!!");
                    return false;
                }
                //logger.Info("This node{" + vehicleMessage.NodeId + "} is Busy!");
                return true;
            }
            else
            {
                //logger.Info("This node{" + vehicleMessage.NodeId + "} is not Busy!!");
                return false;
            }
        }

        public void AddVehicleCrossWaitToWAIT(VehicleMessageEx vehicleMessage)
        {

            VehicleCrossWaitEx checkVehicleCrossWait = this.ResourceManager.GetVehicleCrossWait(vehicleMessage.VehicleId);
            if (checkVehicleCrossWait != null)
            {
                this.ResourceManager.DeleteVehicleCrossWait(vehicleMessage.VehicleId);
                //logger.Warn("Delete Garbage CrossWait Data about Vehicle{" + vehicleMessage.VehicleId);
            }

            VehicleCrossWaitEx vehicleCrossWait = new VehicleCrossWaitEx();
            vehicleCrossWait.VehicleId = (vehicleMessage.VehicleId);
            vehicleCrossWait.NodeId = (vehicleMessage.NodeId);
            vehicleCrossWait.State = (VehicleCrossWaitEx.STATE_WAIT);
            vehicleCrossWait.CreatedTime = DateTime.Now;//(TimeUtils.getCurrentTime());

            //this.ResourceManager.CreateVehicleCrossWait(vehicleCrossWait);
            try
            {
                this.ResourceManager.CreateVehicleCrossWait(vehicleCrossWait);
            }
            catch (Exception e)
            {
                //logger.Warn("failed to addVehicleCrossWaitToWAIT{" + vehicleMessage.toString() + "}", e);
            }
            //logger.Info("createVehicleCrossWait to WAIT {" + vehicleCrossWait.toString() + "}");

        }

        public void AddVehicleCrossWaitToGOING(VehicleMessageEx vehicleMessage)
        {


            VehicleCrossWaitEx vehicleCrossWait = new VehicleCrossWaitEx();
            vehicleCrossWait.VehicleId = vehicleMessage.VehicleId;
            vehicleCrossWait.NodeId = vehicleMessage.NodeId;
            vehicleCrossWait.State = VehicleCrossWaitEx.STATE_GOING;
            vehicleCrossWait.CreatedTime = DateTime.Now;//TimeUtils.getCurrentTime();

            this.ResourceManager.CreateVehicleCrossWait(vehicleCrossWait);

            //logger.Warn("createVehicleCrossWait to GOING {" + vehicleCrossWait.toString() + "}");

        }

        public void AddVehiclesCrossWaitToGOING(VehicleMessageEx vehicleMessage)
        {

            for (IEnumerator iterator = vehicleMessage.Vehicles.GetEnumerator(); iterator.MoveNext();)
            {
                VehicleEx vehicle = (VehicleEx)iterator.Current;

                VehicleCrossWaitEx vehicleCrossWait = new VehicleCrossWaitEx();
                vehicleCrossWait.VehicleId = (vehicle.Id);
                vehicleCrossWait.NodeId = (vehicle.CurrentNodeId);
                vehicleCrossWait.State = (VehicleCrossWaitEx.STATE_GOING);
                vehicleCrossWait.CreatedTime = DateTime.Now;//(TimeUtils.getCurrentTime());

                this.ResourceManager.CreateVehicleCrossWait(vehicleCrossWait);

                //logger.Warn("createVehicleCrossWait to GOING {" + vehicleCrossWait.toString() + "}");

            }


        }

        public int DeleteVehicleCrossWaitByVehicleId(VehicleMessageEx vehicleMessage)
        {
            int deleteCount = this.ResourceManager.DeleteVehicleCrossWait(vehicleMessage.VehicleId);

            //logger.Info("Deleted VehicleCrossWait VehicleId {" + vehicleMessage.VehicleId + "} Count=[" + deleteCount + "]");
            return deleteCount;
        }

        //20200310 LYS Add on Function for Parking Station
        public int DeleteVehicleIdleByVehicleId(string vehicleId)
        {
            return this.ResourceManager.DeleteVehicleIdleByVehicleId(vehicleId);
        }

        public bool CheckVehicleCrossWaitToGO(VehicleMessageEx vehicleMessage)
        {
            bool result = false;
            IList listVehicles = this.ResourceManager.GetVehiclesCrossWaitAllOrderbyCreateTime();

            if (listVehicles == null || listVehicles.Count == 0)
            {
                return false;
            }

            IList newListVehicles = new ArrayList();
            IList checkedNodes = new ArrayList();
            String waitNodeId = "";
            String vehicleId = "";
            bool isCheck = false;

            for (IEnumerator iterator = listVehicles.GetEnumerator(); iterator.MoveNext();)
            {
                VehicleCrossWaitEx VehicleCrossWaitEx = (VehicleCrossWaitEx)iterator.Current;
                waitNodeId = VehicleCrossWaitEx.NodeId;

                //중복체크 Validate
                isCheck = false;
                for (IEnumerator iterator1 = checkedNodes.GetEnumerator(); iterator1.MoveNext();)
                {
                    String nodeId = (String)iterator1.Current;

                    if (waitNodeId.Equals(nodeId, StringComparison.OrdinalIgnoreCase))
                    {
                        isCheck = true;
                        break;
                    }
                }

                if (isCheck == true)
                {
                    //already go in this nodeid. so pass this node..
                    continue;
                }
                //중복체크 Validate End
                checkedNodes.Add(waitNodeId);
            }

            for (IEnumerator iterator = checkedNodes.GetEnumerator(); iterator.MoveNext();)
            {
                String checkNodeId = (String)iterator.Current;

                IList listCrossNodeGoing = this.ResourceManager.GetVehiclesCrossWaitByNodeIdAndState(checkNodeId, VehicleCrossWaitEx.STATE_GOING);

                if (listCrossNodeGoing == null || listCrossNodeGoing.Count < 1)
                {
                    //WAIT중 오래 된 AGV 보내기
                    IList listCrossNodeWait = this.ResourceManager.GetVehiclesCrossWaitByNodeIdAndState(checkNodeId, VehicleCrossWaitEx.STATE_WAIT);

                    if (listCrossNodeWait != null && listCrossNodeWait.Count > 0)
                    {
                        VehicleCrossWaitEx vehicleCrossWait = (VehicleCrossWaitEx)listCrossNodeWait[0];

                        VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleCrossWait.VehicleId);
                        if (vehicle != null)
                        {
                            vehicle.CurrentNodeId = vehicleCrossWait.NodeId;
                            newListVehicles.Add(vehicle);
                        }
                    }
                }
                else
                {
                    //Going인 가장 최근 AGV 찾아서, 시간이 30초 지난 AGV 보내기
                    VehicleCrossWaitEx vehicleCrossWait = (VehicleCrossWaitEx)listCrossNodeGoing[listCrossNodeGoing.Count - 1];

                    DateTime vehicleDate = vehicleCrossWait.CreatedTime;
                    DateTime currentDate = DateTime.Now;

                    //				if(vehicleDate.before(TimeUtils.addSeconds(currentDate, -120))){
                    if (vehicleDate < (currentDate.AddSeconds(-this.crossWaitTimeout))) //if(vehicleDate.before(TimeUtils.addSeconds(currentDate, -this.crossWaitTimeout))){
                    {
                        IList listCrossNodeWait = this.ResourceManager.GetVehiclesCrossWaitByNodeIdAndState(checkNodeId, VehicleCrossWaitEx.STATE_WAIT);

                        if (listCrossNodeWait != null && listCrossNodeWait.Count > 0)
                        {
                            VehicleCrossWaitEx vehicleCrossWait2 = (VehicleCrossWaitEx)listCrossNodeWait[0];

                            VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleCrossWait2.VehicleId);
                            if (vehicle != null)
                            {
                                vehicle.CurrentNodeId = vehicleCrossWait2.NodeId;
                                newListVehicles.Add(vehicle);
                            }
                        }
                    }
                }
            }

            //vehicleMessage.Vehicles.clear();
            if (newListVehicles.Count > 0) //20200318 LYS Response speed up of TS Logic
            {
                vehicleMessage.Vehicles = newListVehicles;
                result = true;
            }

            return result;
        }

        public bool CheckVehicleAndGoByEndNodeId(VehicleMessageEx vehicleMessage)
        {

            String nodeId = vehicleMessage.NodeId;
            String startNodeId = this.getStartNodeId(nodeId);

            IList listGoingVehicles = this.ResourceManager.GetVehiclesCrossWaitByNodeIdAndState(startNodeId, VehicleCrossWaitEx.STATE_GOING);

            if (listGoingVehicles != null && listGoingVehicles.Count > 0)
            {
                //logger.Warn("There is a GOING Vehicle at nodeId {" + startNodeId + "} so, Do nothing.");
                return false;
            }

            IList listWaitVehicles = this.ResourceManager.GetVehiclesCrossWaitByNodeIdAndState(startNodeId, VehicleCrossWaitEx.STATE_WAIT);
            if (listWaitVehicles != null)
            {

                for (IEnumerator iterator = listWaitVehicles.GetEnumerator(); iterator.MoveNext();)
                {
                    VehicleCrossWaitEx vehicleCrossWaitEx = (VehicleCrossWaitEx)iterator.Current;
                    String waitNodeId = vehicleCrossWaitEx.NodeId;
                    String vehicleId = vehicleCrossWaitEx.VehicleId;

                    //logger.Info("This is no GOING Vehicle, and this is first WATING Vehicle at nodeId {" + startNodeId + "}, so, send to GO command to Vehicle[" + vehicleId + "]");
                    vehicleMessage.NodeId = startNodeId;
                    vehicleMessage.Node = this.ResourceManager.GetNode(startNodeId);
                    vehicleMessage.VehicleId = vehicleId;
                    vehicleMessage.Vehicle = this.ResourceManager.GetVehicle(vehicleId);
                    return true;

                }
                //logger.Warn("There is no WAITING Vehicle at nodeId {" + startNodeId + "} so, Do nothing.");
                return false;
            }
            else
            {
                //logger.Info("This is no GOING, no WAITING Vehicle at nodeId {" + startNodeId + "}, so, Do nothing.");
                return false;
            }

            //		VehicleCrossWaitEx VehicleCrossWaitEx = this.ResourceManager.GetVehicleCrossWaitByNodeId(startNodeId);
            //		if(VehicleCrossWaitEx != null){
            //			vehicleMessage.NodeId=(VehicleCrossWaitEx.NodeId);
            //			vehicleMessage.Node=(this.ResourceManager.GetNode(startNodeId));
            //			vehicleMessage.VehicleId=(VehicleCrossWaitEx.VehicleId);
            //			vehicleMessage.Vehicle=(this.ResourceManager.GetVehicle(VehicleCrossWaitEx.VehicleId));
            //			
            //			//logger.Info("There is a Vehicle at nodeId {" + startNodeId + "}, VehicleCrossWaitEx{" + VehicleCrossWaitEx.toString()+"}");
            //			
            //			return true;
            //		}else{
            //			//logger.Info("There is no Vehicle at nodeId {" + startNodeId + "}. so do nothing!!");
            //			return false;
            //		}

        }

        public bool CheckVehicleAndGoByStartNodeId(VehicleMessageEx vehicleMessage)
        {

            String startNodeId = vehicleMessage.NodeId;

            VehicleCrossWaitEx VehicleCrossWaitEx = this.ResourceManager.GetVehicleCrossWaitByNodeId(startNodeId);
            if (VehicleCrossWaitEx != null)
            {
                vehicleMessage.NodeId = VehicleCrossWaitEx.NodeId;
                vehicleMessage.Node = this.ResourceManager.GetNode(startNodeId);
                vehicleMessage.VehicleId = VehicleCrossWaitEx.VehicleId;
                vehicleMessage.Vehicle = this.ResourceManager.GetVehicle(VehicleCrossWaitEx.VehicleId);

                //logger.Info("There is a Vehicle at nodeId {" + startNodeId + "}, VehicleCrossWaitEx{" + VehicleCrossWaitEx.toString() + "}");

                return true;
            }
            else
            {
                //logger.Info("There is no Vehicle at nodeId {" + startNodeId + "}. so do nothing!!");
                return false;
            }

        }

        public bool CheckVehicleCrossWaitByStartNodeId(VehicleMessageEx vehicleMessage)
        {

            String startNodeId = vehicleMessage.NodeId;

            IList listGoingVehicles = this.ResourceManager.GetVehiclesCrossWaitByNodeIdAndState(startNodeId, VehicleCrossWaitEx.STATE_GOING);

            if (listGoingVehicles != null && listGoingVehicles.Count > 0)
            {
                //logger.fine("There is a GOING Vehicle at nodeId {" + startNodeId + "} so, WAIT. Vehicle[" + vehicleMessage.VehicleId + "]");
                return true;
            }

            //vehicleMessage.NodeId=(VehicleCrossWaitEx.NodeId);
            vehicleMessage.Node = this.ResourceManager.GetNode(startNodeId);
            //vehicleMessage.VehicleId=(VehicleCrossWaitEx.VehicleId);
            vehicleMessage.Vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
            return false;

        }


        public bool CheckVehicleBatteryVoltage(VehicleMessageEx vehicleMessage)
        {

            float vehicleVoltage = 0;

            if (vehicleMessage.Vehicle == null)
            {
                vehicleMessage.Vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                vehicleMessage.Vehicle.BatteryVoltage = vehicleVoltage;
            }

            vehicleVoltage = vehicleMessage.BatteryVoltage;

            if (vehicleVoltage <= VehicleEx.AVAIALBE_VOLTAGE)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool CheckVehiclesBatteryVoltage(VehicleMessageEx vehicleMessage)
        {

            bool result = false;

            if (vehicleMessage.Vehicles == null)
            {
                vehicleMessage.Vehicles = this.ResourceManager.GetVehiclesByBayId(vehicleMessage.BayId);
            }

            IList listVehicles = vehicleMessage.Vehicles;
            List<VehicleEx> newListVehicles = new List<VehicleEx>();

            for (IEnumerator iterator = listVehicles.GetEnumerator(); iterator.MoveNext();)
            {
                VehicleEx vehicle = (VehicleEx)iterator.Current;

                float vehicleVoltage = vehicle.BatteryVoltage;
                //logger.Info("Check Vehicle voltage= " + vehicle.toString());
                if (vehicleVoltage <= VehicleEx.AVAIALBE_VOLTAGE)
                {
                    //logger.Info("Need charge Vehicle add : {" + vehicle.toString());
                    newListVehicles.Add(vehicle);
                    result = true;
                }

            }
            vehicleMessage.Vehicles.Clear();
            vehicleMessage.Vehicles = newListVehicles;
            return result;
        }

        public bool CheckVehicleBatteryCapacity(VehicleMessageEx vehicleMessage)
        {

            float vehicleCapa = 0;
            if (vehicleMessage.Vehicle != null)
            {
                vehicleCapa = vehicleMessage.Vehicle.BatteryRate;
            }
            else
            {
                vehicleMessage.Vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                vehicleCapa = vehicleMessage.BatteryRate;
            }

            if (vehicleCapa <= VehicleEx.AVAIALBE_CAPACITY)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool CheckVehicleSCodeStationIs0000(VehicleMessageEx vehicleMessage)
        {

            String stationId = vehicleMessage.StationId;

            if (stationId.Equals("0000"))
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public bool CheckVehicleSCodeStationIs9999(VehicleMessageEx vehicleMessage)
        {

            String stationId = vehicleMessage.StationId;

            if (stationId.Equals("9999"))
            {
                return true;
            }
            else
            {
                return false;
            }

        }


        public bool CheckVehicleByTransportCommand(TransferMessageEx transferMessage)
        {

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (transferMessage != null && transferMessage.TransportCommand != null)
            {
                transportCommand = transferMessage.TransportCommand;
            }
            else
            {
                if (string.IsNullOrEmpty(transferMessage.TransportCommandId))
                {
                    transportCommand = this.TransferManager.GetTransportCommandByVehicleId(transferMessage.VehicleId);
                }
                else
                {
                    transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
                }
            }

            if (transportCommand == null)
                return false;
            transferMessage.TransportCommand = transportCommand;
            if (transportCommand.VehicleId == null || string.IsNullOrEmpty(transportCommand.VehicleId))
            {
                return false;
            }
            else
            {
                //transferMessage.setTransportCommand(transportCommand); 
                transferMessage.VehicleId = transportCommand.VehicleId;
                transferMessage.CarrierId = transportCommand.CarrierId;
                return true;
            }
        }

        public bool CheckCCodeTypeByNodeId(VehicleMessageEx vehicleMessage)
        {
            bool result = false;

            if (string.IsNullOrEmpty(vehicleMessage.NodeId)) return result;

            StationEx station = this.ResourceManager.GetStation(vehicleMessage.NodeId);
            //KSB
            LocationEx location = this.ResourceManager.GetLocationByStationId(vehicleMessage.NodeId);
            VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);

            //1. Vehicle (RGV, AGV/SLIM) 확인?
            // - RGV
            if (vehicle.Vendor.Equals("RGV", StringComparison.OrdinalIgnoreCase))
            {
                //2. RGV구간의 사용하는 BAYID 확인?
                // - RGV 적용 BAYID 
                if (vehicle.BayId.Equals("AMT-LFC(A)", StringComparison.OrdinalIgnoreCase) ||
                    vehicle.BayId.Equals("AMT(A)-LFC(A)", StringComparison.OrdinalIgnoreCase) ||
                    vehicle.BayId.Equals("AMT-LFC(B)", StringComparison.OrdinalIgnoreCase) ||
                    vehicle.BayId.Equals("AMT(B)-LFC(B)", StringComparison.OrdinalIgnoreCase))
                {
                    //3. Location Type(EQP, BUFFER) 확인?
                    // - EQP
                    if (location.Type.Equals("EQP", StringComparison.OrdinalIgnoreCase))
                    {
                        if (station == null || string.IsNullOrEmpty(station.Type))
                        {
                            vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_MANUAL;
                            result = true;
                        }
                        //Source
                        else if (station.Type.Equals("ACQUIRE", StringComparison.OrdinalIgnoreCase))
                        {
                            if (location.Direction.Equals("LEFT", StringComparison.OrdinalIgnoreCase))
                                vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_LEFTLOAD_TURN;  //06
                            else
                                vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_RIGHTLOAD_TURN; //08
                            result = true;
                        }
                        //Dest
                        else if (station.Type.Equals("DEPOSIT", StringComparison.OrdinalIgnoreCase))
                        {
                            if (location.Direction.Equals("LEFT", StringComparison.OrdinalIgnoreCase))
                                vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_LEFTUNLOAD;     //02
                            else
                                vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_RIGHTUNLOAD;    //04
                            result = true;
                        }
                        else
                        {
                            vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_MANUAL;
                            result = true;
                        }
                    }
                    else if (location.Type.Equals("BUFFER", StringComparison.OrdinalIgnoreCase)) //BUFFER 
                    {
                        if (station == null || string.IsNullOrEmpty(station.Type))
                        {
                            vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_MANUAL;
                            result = true;
                        }
                        //Source
                        else if (station.Type.Equals("ACQUIRE", StringComparison.OrdinalIgnoreCase))
                        {
                            if (location.Direction.Equals("LEFT", StringComparison.OrdinalIgnoreCase))
                                vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_LEFTLOAD;
                            else
                                vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_RIGHTLOAD;
                            result = true;
                        }
                        //Dest
                        else if (station.Type.Equals("DEPOSIT", StringComparison.OrdinalIgnoreCase))
                        {
                            if (location.Direction.Equals("LEFT", StringComparison.OrdinalIgnoreCase))
                                vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_LEFTUNLOAD;
                            else
                                vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_RIGHTUNLOAD;
                            result = true;
                        }
                        else
                        {
                            vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_MANUAL;
                            result = true;
                        }
                    }
                    else if (location.Type.Equals("CHARGE", StringComparison.OrdinalIgnoreCase))  //CHARGE
                    {
                        vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_MANUAL;
                        result = true;
                    }
                    else  //
                    {
                        vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_MANUAL;
                        result = true;
                    }
                }
            }
            else //그외 AGV, SLIM ...
            {
                if (station == null || string.IsNullOrEmpty(station.Type))
                {
                    vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_MANUAL;
                    result = true;
                }
                //Source
                else if (station.Type.Equals("ACQUIRE", StringComparison.OrdinalIgnoreCase))
                {
                    if (location.Direction.Equals("LEFT", StringComparison.OrdinalIgnoreCase))
                        vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_LEFTLOAD;
                    else
                        vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_RIGHTLOAD;
                    result = true;
                }
                //Dest
                else if (station.Type.Equals("DEPOSIT", StringComparison.OrdinalIgnoreCase))
                {
                    if (location.Type.Equals("CHARGE", StringComparison.OrdinalIgnoreCase))
                    {
                        vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_MANUAL;
                        result = true;
                    }
                    else
                    {
                        if (location.Direction.Equals("LEFT", StringComparison.OrdinalIgnoreCase))
                            vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_LEFTUNLOAD;
                        else
                            vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_RIGHTUNLOAD;
                        result = true;
                    }
                }
                else
                {
                    vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_MANUAL;
                    result = true;
                }
            }

            #region <이전코드>
            /*
                        if (station == null || string.IsNullOrEmpty(station.Type))
                        {
                            vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_MANUAL;
                            result = true;
                        }
                        //Source
                        else if (station.Type.Equals("ACQUIRE", StringComparison.OrdinalIgnoreCase))
                        {
                            if (location.Direction.Equals("LEFT", StringComparison.OrdinalIgnoreCase))
                                vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_LEFTLOAD;
                            else //if (location.Direction.Equals("RIGHT", StringComparison.OrdinalIgnoreCase)))
                                vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_RIGHTLOAD;
                            result = true;
                        }
                        //Dest
                        else if (station.Type.Equals("DEPOSIT", StringComparison.OrdinalIgnoreCase))
                        {
                            if (location.Direction.Equals("LEFT", StringComparison.OrdinalIgnoreCase))
                                vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_LEFTUNLOAD;
                            else //if (location.Direction.Equals("RIGHT", StringComparison.OrdinalIgnoreCase)))
                                vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_RIGHTUNLOAD;
                            result = true;
                        }
                        else
                        {
                            vehicleMessage.CCodeType = VehicleMessageEx.C_CODE_TYPE_MANUAL;
                            result = true;
                        }
            */
            #endregion
            return result;
        }

        public bool CheckVehiclesTransportCommand(VehicleMessageEx vehicleMessage)
        {

            bool result = false;

            if (vehicleMessage.Vehicles == null)
            {
                //logger.Info("Target Vehicles is empty" + vehicleMessage.toString());
                return result;
            }

            IList listVehicles = vehicleMessage.Vehicles;
            List<VehicleEx> newListVehicles = new List<VehicleEx>();

            for (IEnumerator iterator = listVehicles.GetEnumerator(); iterator.MoveNext();)
            {
                VehicleEx vehicle = (VehicleEx)iterator.Current;

                String vehicleId = vehicle.Id;
                //logger.Info("Check Vehicle TransportCommand= " + vehicle.toString());

                TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleId);
                if (transportCommand == null)
                {
                    //logger.Info("Need charge Vehicle add : {" + vehicle.toString());
                    newListVehicles.Add(vehicle);
                    result = true;
                }

            }
            vehicleMessage.Vehicles.Clear();
            vehicleMessage.Vehicles = newListVehicles;
            return result;
        }


        public bool CheckVehiclesNodeCheckTime(VehicleMessageEx vehicleMessage)
        {

            bool result = false;

            if (vehicleMessage.Vehicles == null)
            {
                //logger.Info("Target Vehicles is empty" + vehicleMessage.toString());
                return result;
            }

            IList listVehicles = vehicleMessage.Vehicles;
            List<VehicleEx> newListVehicles = new List<VehicleEx>();

            for (IEnumerator iterator = listVehicles.GetEnumerator(); iterator.MoveNext();)
            {
                VehicleEx vehicle = (VehicleEx)iterator.Current;

                String vehicleId = vehicle.Id;
                DateTime nodeCheckTime = vehicle.NodeCheckTime;
                DateTime currentTime = new DateTime();

                //logger.Info("Check Vehicle NodeCheckTime= " + vehicle.toString());
                //SJP
                TimeSpan DateResult = currentTime - nodeCheckTime;
                if (DateResult.TotalSeconds > 30)
                {
                    //logger.Info("Need disconnect Vehicle add : {" + vehicle.toString());
                    newListVehicles.Add(vehicle);
                }
            }
            vehicleMessage.Vehicles.Clear();
            vehicleMessage.Vehicles = newListVehicles;
            return result;
        }

        public int ChangeVehicleCrossWaitToGOING(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            VehicleCrossWaitEx VehicleCrossWaitEx = this.ResourceManager.GetVehicleCrossWait(vehicleMessage.VehicleId);
            if (VehicleCrossWaitEx != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();
                VehicleCrossWaitEx.State = (VehicleCrossWaitEx.STATE_GOING);

                setAttributes["State"] = VehicleCrossWaitEx.State;

                updateCount = this.ResourceManager.UpdateVehicleCrossWait(VehicleCrossWaitEx, setAttributes);
                //			try {
                //				updateCount = this.ResourceManager.UpdateVehicleCrossWait(VehicleCrossWaitEx, setAttributes);
                //			} 
                //			catch (Exception e) 
                //			{
                //				//logger.Warn("failed to searchSuitableStockStationFromVehicle{" + vehicleMessage.toString() + "}", e);
                //				updateCount = 0;
                //			}
            }

            return updateCount;



        }

        public int ChangeVehiclesCrossWaitToGOING(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            for (IEnumerator iterator = vehicleMessage.Vehicles.GetEnumerator(); iterator.MoveNext();)
            {
                VehicleEx vehicle = (VehicleEx)iterator.Current;

                VehicleCrossWaitEx VehicleCrossWaitEx = this.ResourceManager.GetVehicleCrossWait(vehicle.Id);
                if (VehicleCrossWaitEx != null)
                {

                    Dictionary<string, object> setAttributes = new Dictionary<string, object>();
                    VehicleCrossWaitEx.State = (VehicleCrossWaitEx.STATE_GOING);

                    setAttributes["State"] = VehicleCrossWaitEx.State;
                    setAttributes["CreatedTime"] = DateTime.Now;

                    updateCount = this.ResourceManager.UpdateVehicleCrossWait(VehicleCrossWaitEx, setAttributes);
                }
            }

            return updateCount;



        }

        public String getStartNodeId(String nodeId)
        {

            String startNodeId = extractNodenumber(nodeId);
            int startNodeNumber;

            if (!int.TryParse(startNodeId, out startNodeNumber))
            {
                startNodeNumber = 0;
            }
            else
            {
                startNodeNumber -= 1;
            }


            String startNodeIdToString = startNodeNumber.ToString();

            int gab = nodeId.Length - startNodeIdToString.Length;

            for (int i = 0; i < gab; i++)
            {
                startNodeIdToString = "0" + startNodeIdToString;
            }

            return startNodeIdToString;
        }

        public String extractNodenumber(String nodeId)
        {

            String nodeNumber = nodeId;
            if (nodeId.StartsWith("0"))
            {
                nodeNumber = extractNodenumber(nodeId.Substring(1, nodeId.Length));
            }

            return nodeNumber;
        }

        public bool CheckVehicleBayByNodeId(VehicleMessageEx vehicleMessage)
        {
            VehicleEx vehicle = new VehicleEx();

            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                if (vehicle == null)
                {
                    return false;
                }
                vehicleMessage.Vehicle = vehicle;
            }

            String tagNodeId = vehicleMessage.NodeId;

            NodeEx node = this.ResourceManager.GetNode(tagNodeId);
            if (node == null)
            {
                return false;
            }

            //LinkViewEx linkViews = this.PathManager.getLinkViewByFromNodeId(tagNodeId);

            //List listLinkViews = this.PathManager.GetLinkViewsByFromNodeId(tagNodeId);
            IList listLinkZones = this.PathManager.GetLinkZoneByFromNodeId(tagNodeId);

            if (listLinkZones != null)
            {
                for (IEnumerator iterator = listLinkZones.GetEnumerator(); iterator.MoveNext();)
                {
                    LinkZoneEx linkZone = (LinkZoneEx)iterator.Current;

                    if (vehicle.BayId.Equals(linkZone.ZoneId))
                    {
                        return true;
                    }
                }

                String vehicleBayId = vehicle.BayId;
                if (this.PathManager.GetLinkZoneByFromBayId(vehicleBayId) == null || this.PathManager.GetLinkZoneByFromBayId(vehicleBayId).Count <= 0)
                {
                    //logger.Info("This AGV is Fix Mode, " + vehicle.Id + " " + vehicle.BayId);
                    return false;
                }


                String message = "VEHICLE [" + vehicle.Id + "] TAG=" + tagNodeId + " ORIGINAL ROUTE=" + vehicle.BayId;
                String desc = "Dear AGV member. Move this AGV to original route[" + vehicle.BayId + "]";

                ////logger.Warn(message,"",vehicleMessage.MessageName,vehicleMessage.CarrierId,vehicleMessage.getCommandId(),vehicle.CurrentNodeId,vehicleMessage.VehicleId);


                IList linkZones = this.PathManager.GetLinkZoneByFromBayId(vehicle.BayId);
                if (linkZones != null)
                {

                    IList informList = this.ResourceManager.GetUIInformByDescription(vehicleMessage.VehicleId, desc);
                    if (informList.Count > 0)
                    {

                        Inform info = (Inform)informList[0];
                        info.Time = DateTime.Now;//(TimeUtils.getCurrentTime());
                        info.Message = message;
                        this.ResourceManager.UpdateInform(info);
                        //logger.Info(message);

                    }
                    else
                    {

                        this.CreateUIInform(Inform.INFORM_TYPE_EMERGENCY, message, vehicleMessage.VehicleId, desc);
                        //logger.Info(message);
                    }

                }
                return false;
            }
            else
            {
                //			//logger.Error("Can not find LinkZone, tag: " + tagNodeId);
                return false;
            }
        }

        public bool CheckStokStionByNodeId(VehicleMessageEx vehicleMessage)
        {
            VehicleEx vehicle = new VehicleEx();

            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                vehicleMessage.Vehicle = vehicle;
            }

            if (vehicle != null)
            {
                NodeEx node = this.ResourceManager.GetNode(vehicleMessage.NodeId);
                if (node.Type.Equals(NodeEx.TYPE_STOCK_STATION))
                {
                    return true;
                }
            }
            //logger.Warn("can not find WaitPoint, " + vehicle.toString());
            return false;
        }

        public bool CheckVehicleDestNodeByStationId(VehicleMessageEx vehicleMessage)
        {
            VehicleEx vehicle = new VehicleEx();

            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                vehicleMessage.Vehicle = vehicle;
            }

            String currentStationId = vehicleMessage.StationId;
            String vehicleDestNodeId = vehicle.AcsDestNodeId;

            //logger.Info("S_Code StationId = " + currentStationId + ", ACSDestNodeId = " + vehicleDestNodeId);
            //BSW
            if (!string.IsNullOrEmpty(vehicleDestNodeId) && !currentStationId.Equals(vehicleDestNodeId, StringComparison.OrdinalIgnoreCase)) //Equals->EqualsIngnoreCase嚥�?癰�?瑗�
            {
                vehicleMessage.DestNodeId = vehicleDestNodeId;
                //logger.Info("This Vehicle (" + vehicle.Id + ") send to " + vehicleDestNodeId + " by S_CODE");
                return false;
            }

            return true;

        }

        public bool CheckVehicleCurrentNodeIsSource(VehicleMessageEx vehicleMessage)
        {

            bool result = false;
            VehicleEx vehicle = new VehicleEx();

            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                vehicleMessage.Vehicle = vehicle;
            }

            TransportCommandEx transportCommand = new TransportCommandEx();

            if (string.IsNullOrEmpty(vehicleMessage.TransportCommandId))
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
            }

            if (transportCommand == null)
            {
                return false;
            }

            vehicleMessage.TransportCommand = transportCommand;
            vehicleMessage.CarrierId = transportCommand.CarrierId;

            if (!string.IsNullOrEmpty(transportCommand.Source))
            {

                LocationEx location = this.ResourceManager.GetLocation(transportCommand.Source);
                if (location != null)
                {
                    if (location.StationId.Equals(vehicleMessage.NodeId, StringComparison.OrdinalIgnoreCase))
                    {
                        //Assign상태에서Source간것이면
                        if (transportCommand.State.Equals(TransportCommandEx.STATE_ASSIGNED))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                return false;
            }
            return result;
        }

        public bool CheckVehicleCurrentNodeIsDest(VehicleMessageEx vehicleMessage)
        {

            bool result = false;
            VehicleEx vehicle = new VehicleEx();

            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                vehicleMessage.Vehicle = vehicle;
            }

            TransportCommandEx transportCommand = new TransportCommandEx();

            if (string.IsNullOrEmpty(vehicleMessage.TransportCommandId))
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
            }

            if (transportCommand == null)
            {
                return false;
            }

            vehicleMessage.TransportCommand = transportCommand;
            vehicleMessage.CarrierId = transportCommand.CarrierId;

            if (!string.IsNullOrEmpty(transportCommand.Dest))
            {

                LocationEx location = this.ResourceManager.GetLocation(transportCommand.Dest);
                if (location != null)
                {
                    if (location.StationId.Equals(vehicleMessage.NodeId, StringComparison.OrdinalIgnoreCase))
                    {

                        //Load상태에서 Dest 갔다면
                        if (transportCommand.State.Equals(TransportCommandEx.STATE_TRANSFERRING_DEST))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                return false;
            }
            return result;
        }


        public bool CheckVehicleCurrentNodeByChargeJob(VehicleMessageEx vehicleMessage)
        {

            VehicleEx vehicle = new VehicleEx();

            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                vehicleMessage.Vehicle = vehicle;
            }

            String currentNodeId = vehicleMessage.NodeId;

            TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.Vehicle.Id);

            if (transportCommand == null || !transportCommand.JobType.Equals(TransportCommandEx.JOBTYPE_CHARGEMOVE))
            {
                return true;
            }

            String destPortId = transportCommand.Dest;
            String destNodeId = this.PathManager.GetLocationByPortId(destPortId).StationId;

            if (!currentNodeId.Equals(destNodeId))
            {
                return false;
            }

            return true;

        }

        public bool CheckVehicleCurrentNodeByChargeJob(TransferMessageEx transferMessage)
        {

            VehicleEx vehicle = new VehicleEx();

            if (transferMessage.Vehicle != null)
            {
                vehicle = transferMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(transferMessage.VehicleId);
                transferMessage.Vehicle = vehicle;
            }

            String currentNodeId = vehicle.CurrentNodeId;

            TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(transferMessage.Vehicle.Id);

            if (transportCommand == null || !transportCommand.JobType.Equals(TransportCommandEx.JOBTYPE_CHARGEMOVE))
            {
                return false;
            }

            String destPortId = transportCommand.Dest;
            String destNodeId = this.PathManager.GetLocationByPortId(destPortId).StationId;

            if (!currentNodeId.Equals(destNodeId))
            {
                return true;
            }

            return false;

        }

        public void CreateVehicleHistory(VehicleMessageEx vehicleMessage)
        {
            VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
            if (vehicle != null)
            {
                vehicleMessage.Vehicle = vehicle;
                this.HistoryManager.CreateVehicleHistory(vehicleMessage);
            }
            return;
        }

        public void CreateVehicleHistory(TransferMessageEx transferMessage)
        {
            VehicleEx vehicle = this.ResourceManager.GetVehicle(transferMessage.VehicleId);
            if (vehicle != null)
            {
                transferMessage.Vehicle = vehicle;
                this.HistoryManager.CreateVehicleHistory(transferMessage);
            }
            return;
        }

        public void CreateCarrierInform(VehicleMessageEx vehicleMessage)
        {
            if ("RAIL-CARRIERLOADED".Equals(vehicleMessage.MessageName, StringComparison.OrdinalIgnoreCase))
            {

                IList informCarrierLoadedList = this.ResourceManager.GetUIInformByMessage(vehicleMessage.VehicleId, "CARRIER LOADED");
                if (informCarrierLoadedList.Count == 0)
                {
                    Random rand = new Random();
                    Inform inform = new Inform();
                    inform.Id = Guid.NewGuid().ToString();
                    inform.Time = DateTime.Now;//(TimeUtils.getCurrentTime());
                    inform.Type = Inform.INFORM_TYPE_IMPORTANT;
                    inform.Source = vehicleMessage.VehicleId;
                    inform.Message = "CARRIER LOADED";
                    inform.Description = "Check and Reset AGV(" + vehicleMessage.VehicleId + "). Current location is [" + vehicleMessage.NodeId + "]";

                    this.ResourceManager.CreateInform(inform);
                }

            }
            else if ("RAIL-CARRIERREMOVED".Equals(vehicleMessage.MessageName, StringComparison.OrdinalIgnoreCase))
            {

                IList informCarrierRemoveList = this.ResourceManager.GetUIInformByMessage(vehicleMessage.VehicleId, "CARRIER REMOVED");
                if (informCarrierRemoveList.Count == 0)
                {
                    Random rand = new Random();
                    Inform inform = new Inform();
                    inform.Id = Guid.NewGuid().ToString();
                    inform.Time = DateTime.Now;//(TimeUtils.getCurrentTime());
                    inform.Type = Inform.INFORM_TYPE_IMPORTANT;
                    inform.Source = vehicleMessage.VehicleId;
                    inform.Message = "CARRIER REMOVED";
                    inform.Description = "Check and Reset AGV(" + vehicleMessage.VehicleId + "). Current location is [" + vehicleMessage.NodeId + "]";

                    this.ResourceManager.CreateInform(inform);
                }
            }
            else if ("RAIL-VEHICLEOCCUPIED".Equals(vehicleMessage.MessageName, StringComparison.OrdinalIgnoreCase))
            {
                IList informCarrierRemoveList = this.ResourceManager.GetUIInformByMessage(vehicleMessage.VehicleId, "VEHICLE OCCUPIED");
                if (informCarrierRemoveList.Count == 0)
                {
                    Random rand = new Random();
                    Inform inform = new Inform();
                    inform.Id = Guid.NewGuid().ToString();
                    inform.Time = DateTime.Now;//(TimeUtils.getCurrentTime());
                    inform.Type = Inform.INFORM_TYPE_IMPORTANT;
                    inform.Source = vehicleMessage.VehicleId;
                    inform.Message = "VEHICLE OCCUPIED";
                    inform.Description = "Check and Reset AGV(" + vehicleMessage.VehicleId + "). Current location is [" + vehicleMessage.NodeId + "]";

                    this.ResourceManager.CreateInform(inform);
                }
            }
        }

        public void CreateHeavyAlarmInform(VehicleMessageEx vehicleMessage)
        {


            Inform inform = new Inform();
            inform.Id = Guid.NewGuid().ToString();
            inform.Time = DateTime.Now;//(TimeUtils.getCurrentTime());
            inform.Type = (Inform.INFORM_TYPE_IMPORTANT);
            inform.Message = (vehicleMessage.ErrorText);
            inform.Source = (vehicleMessage.VehicleId);

            AlarmSpecEx alarmSpec = this.AlarmManager.GetAlarmSpecByAlarmId(vehicleMessage.ErrorId);
            if (alarmSpec != null)
            {
                inform.Description = (alarmSpec.Description);
            }
            else
            {
                inform.Description = ("Call AGV engineer.");
            }

            this.ResourceManager.CreateInform(inform);

        }

        public int DeleteUIInform(VehicleMessageEx vehicleMessage)
        {

            return this.ResourceManager.DeleteUIInform(vehicleMessage.VehicleId);
        }

        public void CreateRailOutInform(VehicleMessageEx vehicleMessage)
        {


            Inform inform = new Inform();
            inform.Id = Guid.NewGuid().ToString();
            inform.Time = DateTime.Now;//(TimeUtils.getCurrentTime());
            inform.Type = (Inform.INFORM_TYPE_EMERGENCY);
            inform.Message = (vehicleMessage.ErrorText);
            inform.Source = (vehicleMessage.VehicleId);
            inform.Description = ("Dear AGV member. Move this AGV to original route!");

            this.ResourceManager.CreateInform(inform);

        }

        public void CreateUIInform(String type, String message, String source, String description)
        {
            Inform inform = new Inform();
            inform.Id = Guid.NewGuid().ToString();
            inform.Time = DateTime.Now;//(TimeUtils.getCurrentTime());
            inform.Type = (type);
            inform.Message = (message);
            inform.Source = (source);
            inform.Description = (description);

            this.ResourceManager.CreateInform(inform);

        }

        public void UpdateUIInform(String type, String message, String source, String desc)
        {

            Inform inform = new Inform();
            inform.Id = Guid.NewGuid().ToString();
            inform.Time = DateTime.Now;
            inform.Type = (type);
            inform.Message = (message);
            inform.Source = (source);
            inform.Description = (desc);

            this.ResourceManager.UpdateInform(inform);
        }


        public bool CheckVehicle(VehicleMessageEx vehicleMessage)
        {

            bool result = false;

            VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);

            if (vehicle != null)
            {
                vehicleMessage.Vehicle = vehicle;
                result = true;
            }
            else
            {
                String message = vehicleMessage.MessageName + " VEHICLE [" + vehicleMessage.VehicleId + "] is not exist in ACS!!";
                String desc = " Add VEHICLE [" + vehicleMessage.VehicleId + "] to ACS.";

                logger.Error(message + desc);

            }
            return result;
        }

        public bool CheckVehicleExDestNodeIdByVehicle(VehicleMessageEx vehicleMessage)
        {
            //2018.10.04 KSG
            bool result = false;

            VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);

            if (vehicle != null)
            {
                String acsDestNodeId = vehicle.AcsDestNodeId;
                String vehicleDestNodeId = vehicleMessage.DestNodeId;

                if ((!string.IsNullOrEmpty(acsDestNodeId)) && acsDestNodeId.Equals(vehicleDestNodeId))
                {
                    result = true;
                }
                // acsDestNodeId가 null or empty인 경우
                else if (!string.IsNullOrEmpty(acsDestNodeId))
                {
                    vehicleMessage.DestNodeId = acsDestNodeId;
                    result = false;
                }
                else
                {
                    // 이전에 setting된 destNodeId를 그대로 유지
                    return true;
                }

            }
            return result;
        }

        // when ACS received MOVECANCEL.
        // if agv is full, or transerstate is ACQUIRE_COMPLETE then do not cancel!
        public bool CheckDoMoveCancel(TransferMessageEx transferMessage)
        {

            bool result = true;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (transferMessage.TransportCommand != null)
            {
                transportCommand = transferMessage.TransportCommand;
            }
            else
            {
                if (string.IsNullOrEmpty(transferMessage.TransportCommandId))
                {
                    transportCommand = this.TransferManager.GetTransportCommandByVehicleId(transferMessage.VehicleId);
                }
                else
                {
                    transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
                }
            }

            if (transportCommand != null)
            {
                if (transportCommand.VehicleId == null || string.IsNullOrEmpty(transportCommand.VehicleId))
                {
                    // job exist, and not assign AGV. -> cancel OK
                    return true;
                }
                else
                {
                    transferMessage.VehicleId = (transportCommand.VehicleId);
                    VehicleEx vehicle = this.ResourceManager.GetVehicle(transferMessage.VehicleId);

                    if (vehicle != null)
                    {
                        if (vehicle.FullState.Equals("FULL", StringComparison.OrdinalIgnoreCase))
                        {
                            // job exist, but AGV is FULL. -> cancel NG
                            return false;
                        }

                        if (vehicle.TransferState.Equals("ACQUIRE_COMPLETE", StringComparison.OrdinalIgnoreCase))
                        {
                            // job exist, but AGV state is ACQUIRE_COMPLETE. -> cancel NG
                            result = false;
                        }

                        //Doing Still Job -> cancel NG
                        if (transportCommand.State.Equals(TransportCommandEx.STATE_CHANGE_VEHICLE))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                // no transportcmd! -> cancel NG
                result = false;
            }
            return true;
        }

        public IList GetWifiNioes()
        {
            IList temp = new List<object>();
            return temp;
        }

        public void ChangeVehicleTransportCommandIdEmpty(TransferMessageEx transferMessage)
        {
            this.ResourceManager.UpdateVehicleTransportCommandId(transferMessage.VehicleId, "", transferMessage.MessageName);
        }

        public bool IsCurrentNodeDest(VehicleMessageEx vehicleMessage)
        {

            bool result = false;
            TransportCommandEx transportCommand = vehicleMessage.TransportCommand;
            if (transportCommand == null)
            {

                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }

            if (transportCommand != null)
            {

                String destPortId = transportCommand.Dest;
                LocationEx destLocation = this.PathManager.GetLocationByPortId(destPortId);
                String destNodeId = destLocation.StationId;
                String vehicleCurrentNodeId = "";

                if (vehicleMessage.NodeId != null && !string.IsNullOrEmpty(vehicleMessage.NodeId))
                {

                    vehicleCurrentNodeId = vehicleMessage.NodeId;
                }
                else
                {

                    vehicleCurrentNodeId = vehicleMessage.Vehicle.CurrentNodeId;
                }

                logger.Info("vehicleCurrentNodeId{" + vehicleCurrentNodeId + "}, destNodeId{" + destNodeId + "}");
                if (vehicleCurrentNodeId.Equals(destNodeId))
                {
                    result = true;
                }
            }
            else
            {

                logger.Warn("can not find TransportCommand, " + vehicleMessage.ToString());
            }

            return result;
        }

        public bool ChangeVehicleProcessWhenDeletedTransportCommand(TransferMessageEx transferMessage)
        {

            if (transferMessage.VehicleId != null && !string.IsNullOrEmpty(transferMessage.VehicleId))
            {

                String updateState = VehicleEx.PROCESSINGSTATE_IDLE;

                VehicleEx vehicle = this.ResourceManager.GetVehicle(transferMessage.VehicleId);
                if ((vehicle != null) && vehicle.ProcessingState.Equals(VehicleEx.PROCESSINGSTATE_CHARGE))
                {

                    TransportCommandEx transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
                    if ((transportCommand != null) && transportCommand.JobType.Equals(TransportCommandEx.JOBTYPE_CHARGEMOVE))
                    {
                        return false;
                    }
                }
                int count = this.ResourceManager.UpdateVehicleProcessingState(transferMessage.VehicleId, updateState,
                        transferMessage.MessageName);

                if (count == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return false;

        }

        public bool IsVehicleCrossWaitByStartNodeId(VehicleMessageEx vehicleMessage)
        {

            bool result = false;

            String startNodeId = vehicleMessage.NodeId;

            IList listWaitVehicles = this.ResourceManager.GetVehiclesCrossWaitByNodeIdAndState(startNodeId, VehicleCrossWaitEx.STATE_WAIT);
            if (listWaitVehicles != null)
            {

                for (IEnumerator iterator = listWaitVehicles.GetEnumerator(); iterator.MoveNext();)
                {

                    VehicleCrossWaitEx vehicleCrossWaitEx = (VehicleCrossWaitEx)iterator.Current;
                    String vehicleId = vehicleCrossWaitEx.VehicleId;

                    if (vehicleId.Equals(vehicleMessage.VehicleId, StringComparison.OrdinalIgnoreCase))
                    {

                        logger.Info("continue wait vehicle{" + vehicleId + "}, nodeId {" + startNodeId + "}, so, send to GO command to Vehicle[" + vehicleMessage.VehicleId + "]");
                        vehicleMessage.Node = (this.ResourceManager.GetNode(startNodeId));
                        vehicleMessage.Vehicle = (this.ResourceManager.GetVehicle(vehicleMessage.VehicleId));
                        result = true;
                    }
                }
            }

            return result;
        }

        public bool IsAlreadyGoingVehicleCrossWait(VehicleMessageEx vehicleMessage)
        {

            bool result = false;

            String startNodeId = vehicleMessage.NodeId;

            IList listGoingVehicles = this.ResourceManager.GetVehiclesCrossWaitByNodeIdAndState(startNodeId, VehicleCrossWaitEx.STATE_GOING);

            if (listGoingVehicles != null && listGoingVehicles.Count > 0)
            {

                for (IEnumerator iterator = listGoingVehicles.GetEnumerator(); iterator.MoveNext();)
                {

                    VehicleCrossWaitEx vehicleCrossWaitEx = (VehicleCrossWaitEx)iterator.Current;
                    String vehicleId = vehicleCrossWaitEx.VehicleId;

                    if (vehicleId.Equals(vehicleMessage.VehicleId, StringComparison.OrdinalIgnoreCase))
                    {

                        logger.Info("already going vehicle{" + vehicleId + "}, nodeId{" + startNodeId + "}, so, send to GO command to Vehicle[" + vehicleMessage.VehicleId + "]");
                        vehicleMessage.Node = (this.ResourceManager.GetNode(startNodeId));
                        vehicleMessage.Vehicle = (this.ResourceManager.GetVehicle(vehicleMessage.VehicleId));

                        result = true;
                    }
                }
            }
            return result;
        }


        public String SearchWaitPoint(TransportCommandEx transportCommand)
        {

            VehicleEx vehicle = this.ResourceManager.GetVehicle(transportCommand.VehicleId);

            if (vehicle != null)
            {

                String bayId = vehicle.BayId;

                IList listWaitNodes = this.ResourceManager.GetNodesByType(NodeEx.TYPE_WAIT_P);

                for (IEnumerator iterator = listWaitNodes.GetEnumerator(); iterator.MoveNext();)
                {
                    NodeEx node = (NodeEx)iterator.Current;
                    String nodeId = node.Id;

                    IList listLinkZones = this.PathManager.GetLinkZoneByFromNodeId(nodeId);
                    if (listLinkZones == null)
                    {
                        return null;
                    }
                    for (IEnumerator iterator2 = listLinkZones.GetEnumerator(); iterator2.MoveNext();)
                    {
                        LinkZoneEx linkZone = (LinkZoneEx)iterator2.Current;

                        if (linkZone.ZoneId.Equals(bayId) && linkZone.TransferFlag.Equals("Y"))
                        {
                            return node.Id;
                        }
                    }
                }
            }
            logger.Warn("can not find WaitPoint, " + vehicle);
            return null;
        }

        public bool IsPossibleChangeDestLocation(TransferMessageEx transferMessage)
        {

            bool result = true;

            TransportCommandEx transportCommand = transferMessage.TransportCommand;
            if (transportCommand == null)
            {
                transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
            }
            if (transportCommand != null)
            {
                // Test 진행 시 ADS에서 내려오는 타이밍과 정보를 보고 수정이 필요함.
                // 왜 Vehicle의 현재 Node가 Source이거나 source 전일 경우 update를 하지 못하는지..?
                // 왠지 Source가 아닌 Dest가 맞는거 같음. 확인 필요 LSJ
                String destLocationId = "";
                if (transferMessage.MessageName == "MOVEUPDATE")
                { //Update시 command의 source가 아니라 dest여야 함
                    destLocationId = transportCommand.Dest;
                }
                if (transferMessage.MessageName == "MOVECANCEL")
                { //Cancel시 command의 dest가 아니라 source여야 함
                    destLocationId = transportCommand.Source;
                }

                String destNodeId = string.Empty;
                LocationEx location = this.ResourceManager.GetLocation(destLocationId);

                if (location != null)
                {   // transportCommand의 source가 AGV일 수 있어 location이 null이 됨.
                    destNodeId = this.ResourceManager.GetLocation(destLocationId).StationId;
                }

                if (!string.IsNullOrEmpty(destNodeId))
                {
                    VehicleEx vehicle = transferMessage.Vehicle;
                    if (vehicle == null)
                    {
                        vehicle = this.ResourceManager.GetVehicle(transportCommand.VehicleId);
                    }
                    if (vehicle != null)
                    {

                        logger.Info("vehicle.currentNodeId{" + vehicle.CurrentNodeId + "}, destNodeId{" + destNodeId + "}");
                        if (destNodeId.Equals(vehicle.CurrentNodeId))
                        {
                            result = false;
                        }
                        else
                        {

                            IList newLinkViews = this.PathManager.GetLinkViewsByFromNodeId(vehicle.CurrentNodeId);
                            for (IEnumerator iterator = newLinkViews.GetEnumerator(); iterator.MoveNext();)
                            {
                                LinkViewEx linkView = (LinkViewEx)iterator.Current;

                                if (linkView.ToNodeId.Equals(destNodeId))
                                {
                                    result = false;
                                    logger.Info("linkView.ToNodeId{" + linkView.ToNodeId + "}, destNodeId{" + destNodeId + "}");
                                }
                            }
                        }
                    }
                }
                else
                {
                    logger.Warn("destNode does not exist in repository, locationId{" + destLocationId + "}" + transferMessage);
                }
            }
            else
            {
                logger.Warn("transportCommand does not exist in message, " + transferMessage);
            }
            return result;
        }

        // Add 20190813
        public bool CheckVehicleTransferStateIsAccqureComplete(TransferMessageEx transferMessage)
        {
            bool result = false;

            TransportCommandEx transportCommand = transferMessage.TransportCommand;

            if (transportCommand == null)
            {
                transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
            }

            //20.02.08 LSJ transportCommand 확인 

            if (transportCommand != null)
            {
                if (transportCommand.State.Equals(TransportCommandEx.STATE_TRANSFERRING_DEST))
                {

                    result = true;
                }
            }

            return result;
        }

        public bool CheckVehiclesEventTime(VehicleMessageEx vehicleMessage)
        {

            bool result = false;

            if (vehicleMessage.Vehicles == null)
            {
                logger.Info("Target Vehicles is empty" + vehicleMessage.ToString());
                return result;
            }

            IList listVehicles = vehicleMessage.Vehicles;
            List<VehicleEx> newListVehicles = new List<VehicleEx>();

            for (IEnumerator iterator = listVehicles.GetEnumerator(); iterator.MoveNext();)
            {
                VehicleEx vehicle = (VehicleEx)iterator.Current;

                String vehicleId = vehicle.Id;
                DateTime eventTime = vehicle.EventTime;
                DateTime currentTime = DateTime.Now;

                logger.Info("Check Vehicle EventTime= " + eventTime.ToString());

                if ((vehicle.ProcessingState.Equals("PARK")) || vehicle.ProcessingState.Equals("CHARGE"))
                {
                    continue;
                }

                if (vehicle.EventTime == null)
                {
                    logger.Info("Need disconnect Vehicle add : {" + vehicle.ToString());
                    newListVehicles.Add(vehicle);
                    continue;
                }
                //SJP
                TimeSpan DateResult = currentTime - eventTime;
                if (DateResult.TotalSeconds > 60)
                {
                    logger.Info("Need disconnect Vehicle add : {" + vehicle.ToString());
                    newListVehicles.Add(vehicle);

                }
            }
            vehicleMessage.Vehicles.Clear();
            vehicleMessage.Vehicles = (newListVehicles);
            return result;
        }

        public void UpdateVehicleEventTime(VehicleMessageEx vehicleMessage)
        {
            this.ResourceManager.UpdateVehicleEventTime(vehicleMessage.VehicleId);
        }

        public void UpdateVehicleMainboardVersion(VehicleMessageEx vehicleMessage)
        {
            this.ResourceManager.UpdateVehicleMainboardVersion(vehicleMessage.VehicleId, vehicleMessage.NodeId);
        }

        public void UpdateVehiclePlcVersion(VehicleMessageEx vehicleMessage)
        {
            this.ResourceManager.UpdateVehiclePlcVersion(vehicleMessage.VehicleId, vehicleMessage.NodeId);
        }

        public bool createVehicleIdle(VehicleMessageEx vehicleMessage)
        {
            VehicleIdleEx vehicleIdle = new VehicleIdleEx();
            vehicleIdle.VehicleId = vehicleMessage.VehicleId;
            VehicleEx vehicle = vehicleMessage.Vehicle;
            vehicleIdle.BayId = vehicle.BayId;
            this.ResourceManager.CreateVehicleIdle(vehicleIdle);
            return true;
        }

        public bool createVehicleIdle(UiMoveVehicleMessageEx vehicleMessage)
        {
            VehicleIdleEx vehicleIdle = new VehicleIdleEx();
            vehicleIdle.VehicleId = vehicleMessage.VehicleId;
            VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
            vehicleIdle.BayId = vehicle.BayId;
            this.ResourceManager.CreateVehicleIdle(vehicleIdle);
            return true;
        }

        public void updateVehicleIdle(UiMoveVehicleMessageEx vehicleMessage)
        {
            VehicleIdleEx vehicleIdle = new VehicleIdleEx();
            vehicleIdle.VehicleId = vehicleMessage.VehicleId;
            VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
            this.ResourceManager.UpdateVehicleIdleTime(vehicleIdle);
        }

        public void updateVehicleIdle(TransferMessageEx vehicleMessage)
        {
            VehicleIdleEx vehicleIdle = new VehicleIdleEx();
            vehicleIdle.VehicleId = vehicleMessage.VehicleId;
            VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
            this.ResourceManager.UpdateVehicleIdleTime(vehicleIdle);
        }

        public void updateVehicleIdle(VehicleMessageEx vehicleMessage)
        {
            VehicleIdleEx vehicleIdle = new VehicleIdleEx();
            vehicleIdle.VehicleId = vehicleMessage.VehicleId;
            VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
            vehicleIdle.BayId = vehicle.BayId;
            this.ResourceManager.UpdateVehicleIdleTime(vehicleIdle);
        }

        public void UpdateVehicleLastCharge(VehicleMessageEx vehicleMessage)
        {
            VehicleExs vehicle = new VehicleExs();

            if(vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle as VehicleExs;
            }
            else
            {
                vehicle = ResourceManager.GetVehicle(vehicleMessage.VehicleId) as VehicleExs;                          
            }

            vehicle.LastChargeTime = DateTime.Now;
            vehicle.LastChargeBattery = vehicle.BatteryVoltage;

            this.ResourceManager.UpdateVehicleLastChargeBattery(vehicle);
        }

        public void updateVehicleIdle(VehicleEx vehicle)
        {
            VehicleIdleEx vehicleIdle = new VehicleIdleEx();
            vehicleIdle.VehicleId = vehicle.Id;
            vehicleIdle.BayId = vehicle.BayId;
            this.ResourceManager.UpdateVehicleIdleTime(vehicleIdle);
        }

        public bool createVehicleIdle(TransferMessageEx transferMessage)
        {
            VehicleIdleEx vehicleIdle = new VehicleIdleEx();
            vehicleIdle.VehicleId = transferMessage.VehicleId;
            VehicleEx vehicle = transferMessage.Vehicle;
            vehicleIdle.BayId = vehicle.BayId;
            this.ResourceManager.CreateVehicleIdle(vehicleIdle);
            return true;
        }

        public bool checkVehicleIdle(string vehicleId)
        {
            VehicleIdleEx vehicleIdle = this.ResourceManager.GetVehicleIdleACSByVehicleId(vehicleId);
            VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleId);
            if (vehicleIdle != null || (vehicle.Vendor == VehicleEx.VENDOR_FIXMODE))
                return true;
            else
                return false;
        }


        public bool IsRegistedNode(VehicleMessageEx vehicleMessage)
        {
            string nodeId = vehicleMessage.NodeId;
            NodeEx node = this.CacheManager.GetNode(nodeId);
            if (node != null)
            {
                return true;
            }
            if (this.ResourceManager.CheckNodeIsMonitoringNode(nodeId))
            {
                VehicleEx vehicle = vehicleMessage.Vehicle;
                //logger.Error("node{" + nodeId + "} does not exist in repository. Vehicle now on : " + vehicle.CurrentNodeId);
            }
            return false;
        }

        public bool ExistInterSectionInfo(VehicleMessageEx vehicleMessage)
        {
            string nodeId = vehicleMessage.NodeId;
            if (string.IsNullOrEmpty(nodeId))
            {
                if (vehicleMessage.Vehicle != null)
                {
                    nodeId = vehicleMessage.Vehicle.CurrentNodeId;
                    vehicleMessage.NodeId = nodeId;
                }
                else
                {
                    VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                    if (vehicle != null)
                    {
                        nodeId = vehicle.CurrentNodeId;
                        vehicleMessage.NodeId = nodeId;
                    }
                    else
                    {
                        logger.Warn("Can not check possibility to go because vehicle{" + vehicleMessage.VehicleId + "} does not exist in ACS Repository.");
                    }
                }
            }
            if (!string.IsNullOrEmpty(nodeId))
            {
                string interSectionId = "";
                InterSectionControlEx interSectionControl = this.IntersectionControlManagerExImplement.GetInterSectionControlByStartNode(nodeId);
                if (interSectionControl == null)
                {
                    interSectionId = this.IntersectionControlManagerExImplement.GetInterSectionIdControlByEndNode(nodeId);
                }
                else
                {
                    interSectionId = interSectionControl.InterSectionId;
                }
                if (!string.IsNullOrEmpty(interSectionId))
                {
                    CurrentInterSectionInfoEx currIsInfo = this.IntersectionControlManagerExImplement.GetCurrentInterSectionInfoById(interSectionId);
                    if (currIsInfo != null)
                    {
                        return true;
                    }
                }
                else
                {
                    logger.Info("Can't find interSection by nodeId{" + nodeId + "}.");
                }
            }
            return false;
        }


        public bool CheckSpecialOrderBay(VehicleMessageEx vehicleMessage)
        {
            VehicleEx vehicle = ResourceManager.GetVehicle(vehicleMessage.VehicleId);

            if(vehicle == null)
            {
                return false;
            }
            else if (ResourceManager.CheckValueBySpecialConfig(SpecialConfig.SPECIAL_ODER_BAY, "ALL") || ResourceManager.CheckValueBySpecialConfig(SpecialConfig.SPECIAL_ODER_BAY, vehicle.BayId))
            {
                return true;
            }
            else
            {
                return false;
            }         
        }

        public bool CheckSpecialOrderBay(TransferMessageEx transferMessage)
        {
            if (ResourceManager.CheckValueBySpecialConfig(SpecialConfig.SPECIAL_ODER_BAY, "ALL") || ResourceManager.CheckValueBySpecialConfig(SpecialConfig.SPECIAL_ODER_BAY, transferMessage.BayId))
            {
                return true;
            }

            return false;
        }

        public bool CheckSpecialMismatchAndFly(VehicleMessageEx vehicleMessage)
        {

            if (vehicleMessage.Vehicle == null)
            {
                VehicleEx vehicle = ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                vehicleMessage.Vehicle = vehicle;
            }

            if (ResourceManager.CheckValueBySpecialConfig(SpecialConfig.SPECIAL_MISMATCHANDFLY, "ALL") || ResourceManager.CheckValueBySpecialConfig(SpecialConfig.SPECIAL_MISMATCHANDFLY, vehicleMessage.Vehicle.BayId))
            {
                return true;
            }

            return false;
        }

        public bool CheckSpecialCrossWaitHistory(VehicleMessageEx vehicleMessage)
        {
            if(vehicleMessage.Vehicle == null)
            {
                VehicleEx vehicle = ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                vehicleMessage.Vehicle = vehicle;
            }          

            if (ResourceManager.CheckValueBySpecialConfig(SpecialConfig.SPECIAL_CROSSWAITHISTORY, "ALL") || ResourceManager.CheckValueBySpecialConfig(SpecialConfig.SPECIAL_CROSSWAITHISTORY, vehicleMessage.Vehicle.BayId))
            {
                return true;
            }

            return false;
        }

        public bool CheckSpecialMismatchAndFly(TransferMessageEx transferMessage)
        {
            if (ResourceManager.CheckValueBySpecialConfig(SpecialConfig.SPECIAL_MISMATCHANDFLY, "ALL") || ResourceManager.CheckValueBySpecialConfig(SpecialConfig.SPECIAL_MISMATCHANDFLY, transferMessage.BayId))
            {
                return true;
            }

            return false;
        }

        public bool CheckSpecialOrderBay(UiMoveVehicleMessageEx vehicleMessage)
        {
            VehicleEx vehicle = ResourceManager.GetVehicle(vehicleMessage.VehicleId);
            if (ResourceManager.CheckValueBySpecialConfig(SpecialConfig.SPECIAL_ODER_BAY, "ALL") || ResourceManager.CheckValueBySpecialConfig(SpecialConfig.SPECIAL_ODER_BAY, vehicle.BayId))
            {
                return true;
            }

            return false;
        }

        public void AddVehicleCrossWaitToINTERS(VehicleMessageEx vehicleMessage)
        {
            VehicleCrossWaitEx checkVehicleCrossWait = this.ResourceManager.GetVehicleCrossWait(vehicleMessage.VehicleId);
            if (checkVehicleCrossWait != null)
            {
                //this.HistoryManager.CreateVehicleCrossHistory(vehicleMessage.VehicleId, checkVehicleCrossWait.NodeId, checkVehicleCrossWait.CreatedTime, vehicleMessage.NodeId);
                this.ResourceManager.DeleteVehicleCrossWait(vehicleMessage.VehicleId);
                logger.Info("Delete Garbage CrossWait Data about Vehicle{" + vehicleMessage.VehicleId);
            }
            VehicleCrossWaitEx vehicleCrossWait = new VehicleCrossWaitEx();
            vehicleCrossWait.VehicleId = vehicleMessage.VehicleId;
            vehicleCrossWait.NodeId = vehicleMessage.NodeId;
            vehicleCrossWait.State = IntersectionControlManagerExImplement.CROSS_WAIT_INTERSECTION;
            vehicleCrossWait.CreatedTime = DateTime.Now;

            try
            {
                this.ResourceManager.CreateVehicleCrossWait(vehicleCrossWait);
            }
            catch (Exception e)
            {
                logger.Info("failed to addVehicleCrossWaitToINTERS{" + vehicleMessage.ToString() + "}", e);
            }
            logger.Info("createVehicleCrossWait to INTERS {" + vehicleCrossWait.ToString() + "}");
        }

        //When ACS received T-code, check possibility to go.
        public bool possibleToGo(VehicleMessageEx vehicleMessage)
        {

            // VehicleACS vehicle = vehicleMessage.getVehicle();
            // if(vehicle == null) {
            // vehicle =
            // this.resourceManager.getVehicle(vehicleMessage.getVehicleId());
            // }

            VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);

            if (vehicle != null)
            {
                return this.IntersectionControlManagerExImplement.PossibleToGo(vehicle);
            }
            return false;
        }
    }
}

