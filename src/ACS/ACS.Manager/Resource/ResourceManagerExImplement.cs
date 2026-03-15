using ACS.Core.Application;
using ACS.Core.Base;
using ACS.Core.History;
using ACS.Core.Material;
using ACS.Core.Path.Model;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Resource.Model.Factory;
using ACS.Core.Resource.Model.Factory.Machine;
using ACS.Core.Resource.Model.Factory.Zone;
using ACS.Core.Resource.Model.Factory.Unit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Configuration;

namespace ACS.Manager.Resource
{

    public class ResourceManagerExImplement : AbstractManager, IResourceManagerEx
    {
        public IApplicationManager ApplicationManager { get; set; }
        public IMaterialManagerEx MaterialManager { get; set; }
        public IHistoryManagerEx HistoryManager { get; set; }

        public void CreateBay(BayEx bay)
        {
            this.PersistentDao.Save(bay);
        }

        public void DeleteBay(BayEx bay)
        {
            this.PersistentDao.Delete(bay);
        }

        public int DeleteBay(String bayId)
        {
            StringBuilder sbBayId = new StringBuilder(bayId);
            return this.PersistentDao.Delete(typeof(BayEx), sbBayId);
        }

        public int DeleteFactories()
        {
            return this.PersistentDao.DeleteAll(typeof(Factory));
        }

        public BayEx GetBay(String bayId)
        {
            StringBuilder sbBayId = new StringBuilder(bayId);
            return (BayEx)this.PersistentDao.Find(typeof(BayEx), sbBayId, false);
        }

        public IList GetBays()
        {            
            return this.PersistentDao.FindAll(typeof(BayEx)); ;
        }

        public void UpdateBay(BayEx bay)
        {
            this.PersistentDao.Update(bay);
        }

        public void CreateLink(LinkEx link)
        {
            this.PersistentDao.Save(link);
        }

        public void CreateLocation(LocationEx location)
        {
            this.PersistentDao.Save(location);
        }

        public void CreateNode(NodeEx node)
        {
            this.PersistentDao.Save(node);
        }

        public void CreateStation(StationEx station)
        {
            this.PersistentDao.Save(station);
        }

        public void CreateVehicle(VehicleEx vehicle)
        {
            this.PersistentDao.Save(vehicle);
        }

        public void CreateZone(ZoneEx zone)
        {
            this.PersistentDao.Save(zone);
        }

        public void DeleteLink(LinkEx link)
        {
            this.PersistentDao.Delete(link, false);
        }

        public int DeleteLink(String linkId)
        {
            StringBuilder sbLinkId = new StringBuilder(linkId);
            return this.PersistentDao.Delete(typeof(LinkEx), sbLinkId);
        }

        public void DeleteLocation(LocationEx location)
        {
            this.PersistentDao.Delete(location, false);
        }

        public int DeleteLocation(String locationId)
        {
            StringBuilder sbLocationId = new StringBuilder(locationId);
            return this.PersistentDao.Delete(typeof(LocationEx), sbLocationId);
        }

        public void DeleteNode(NodeEx node)
        {
            this.PersistentDao.Delete(node, false);
        }

        public int DeleteNode(String nodeId)
        {
            StringBuilder sbNodeId = new StringBuilder(nodeId);
            return this.PersistentDao.Delete(typeof(NodeEx), sbNodeId);
        }

        public void DeleteStation(StationEx station)
        {
            this.PersistentDao.Delete(station, false);
        }

        public int DeleteStation(String stationId)
        {
            StringBuilder sbStationId = new StringBuilder(stationId);
            return this.PersistentDao.Delete(typeof(StationEx), sbStationId);
        }

        public void DeleteVehicle(VehicleEx vehicle)
        {
            this.PersistentDao.Delete(vehicle, false);
        }

        public int DeleteVehicle(String vehicleId)
        {
            StringBuilder sbVehicleId = new StringBuilder(vehicleId);
            return this.PersistentDao.Delete(typeof(VehicleEx), sbVehicleId);
        }

        public void DeleteZone(ZoneEx zone)
        {
            this.PersistentDao.Delete(zone, false);
        }

        public int DeleteZone(String zoneId)
        {
            StringBuilder sbZoneId = new StringBuilder(zoneId);
            return this.PersistentDao.Delete(typeof(ZoneEx), sbZoneId);
        }

        public LinkEx GetLink(String linkId)
        {
            StringBuilder sbLinkId = new StringBuilder(linkId);
            return (LinkEx)this.PersistentDao.Find(typeof(LinkEx), sbLinkId, false);
        }

        public IList GetLinks()
        {
            return this.PersistentDao.FindAll(typeof(LinkEx));
        }

        public LocationEx GetLocation(String locationId)
        {
            StringBuilder sbLocationId = new StringBuilder(locationId);
            return (LocationEx)this.PersistentDao.Find(typeof(LocationEx), sbLocationId, false);
        }

        public LocationEx GetLocationByPortId(String portId)
        {
            IList locations = this.PersistentDao.FindByAttribute(typeof(LocationEx), "PortId", portId);
            if (locations.Count > 0)
            {
                return (LocationEx)locations[0];
            }
            return null;
        }

        public LocationEx GetLocationByStationId(String stationId)
        {
            IList locations = this.PersistentDao.FindByAttribute(typeof(LocationEx), "StationId", stationId);
            if (locations.Count > 0)
            {
                return (LocationEx)locations[0];
            }
            return null;
        }

        public bool CheckLocation(String locationId)
        {
            StringBuilder sbLocationId = new StringBuilder(locationId);
            LocationEx location = (LocationEx)this.PersistentDao.Find(typeof(LocationEx), sbLocationId, false);
            if (location == null)
            {
                return false;
            }
            return true;
        }

        public IList GetLocations()
        {
            return this.PersistentDao.FindAll(typeof(LocationEx));
        }

        public IList GetNodes()
        {
            return this.PersistentDao.FindAll(typeof(NodeEx));
        }

        public IList GetNodesByType(String type)
        {
            return this.PersistentDao.FindByAttribute(typeof(NodeEx), "Type", type, false);
        }

        public NodeEx GetNode(String nodeId)
        {
            StringBuilder sbNodeId = new StringBuilder(nodeId);
            return (NodeEx)this.PersistentDao.Find(typeof(NodeEx), sbNodeId, false);
        }

        public StationEx GetStation(String stationId)
        {
            StringBuilder sbStationId = new StringBuilder(stationId);
            return (StationEx)this.PersistentDao.Find(typeof(StationEx), sbStationId, false);
        }

        public StationEx GetStationByLinkId(String linkId)
        {
            return (StationEx)this.PersistentDao.FindByAttribute(typeof(StationEx), "LinkId", linkId, false);
        }

        public IList GetStations()
        {
            return this.PersistentDao.FindAll(typeof(StationEx));
        }

        public VehicleEx GetVehicle(String vehicleId)
        {
            StringBuilder sbVehicleId = new StringBuilder(vehicleId);
            return (VehicleEx)this.PersistentDao.Find(typeof(VehicleEx), sbVehicleId, false);
        }

        private VehicleEx Refresh(String vehicleId)
        {
            return GetVehicle(vehicleId);
        }

        public VehicleEx GetVehicleByCurrentNode(String currentNodeId)
        {
            return (VehicleEx)this.PersistentDao.FindByAttribute(typeof(VehicleEx), "CurrentNodeId", currentNodeId, false);
        }

        public IList GetVehiclesByCurrentNode(String currentNodeId)
        {
            return this.PersistentDao.FindByAttribute(typeof(VehicleEx), "CurrentNodeId", currentNodeId);
        }

        public IList GetVehicles()
        {
            return this.PersistentDao.FindAll(typeof(VehicleEx));
        }

        public IList GetVehiclesByBayId(String bayId)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("BayId", bayId);

            return this.PersistentDao.FindByAttributes(typeof(VehicleEx), attributes);
        }

        public ZoneEx GetZone(String zoneId)
        {
            StringBuilder sbZoneId = new StringBuilder(zoneId);
            return (ZoneEx)this.PersistentDao.Find(typeof(ZoneEx), sbZoneId, false);
        }

        public ZoneEx GetZoneByBayId(String bayId)
        {
            IList listZone = this.PersistentDao.FindByAttribute(typeof(ZoneEx), "BayId", bayId, false);
            if (listZone.Count == 0)
            {
                //logger.info("This BayId {" + bayId + "} dose not in Zone.");
                return null;
            }
            return (ZoneEx)listZone[0];
        }

        public IList GetZones()
        {
            return this.PersistentDao.FindAll(typeof(ZoneEx));
        }

        public void UpdateLink(LinkEx link)
        {
            this.PersistentDao.Update(link, false);
        }

        public void UpdateLocation(LocationEx location)
        {
            this.PersistentDao.Update(location, false);
        }

        public void UpdateNode(NodeEx node)
        {
            this.PersistentDao.Update(node, false);
        }

        public void UpdateStation(StationEx station)
        {
            this.PersistentDao.Update(station, false);
        }

        public void UpdateVehicle(VehicleEx vehicle)
        {
            this.PersistentDao.Update(vehicle, false);
        }

        public void UpdateZone(ZoneEx zone)
        {
            this.PersistentDao.Update(zone, false);
        }

        public int UpdateVehicle(VehicleEx vehicle, String propertyName, Object propertyValue)
        {
            return this.PersistentDao.Update(vehicle.GetType(), propertyName, propertyValue, vehicle.Id);
        }

        public int UpdateVehicleTransferState(VehicleEx vehicle, String transferState, String messageName)
        {
            int updateCount = 0;
            if (vehicle != null)
            {
                bool needToUpdate = false;
                if (transferState == null)
                {
                    if (vehicle.TransferState == null)
                    {
                        //logger.fine("not necessary to update it, because transferState is already {" + transferState + "}");
                    }
                    else
                    {
                        needToUpdate = true;
                    }
                }
                else if (transferState.Equals(vehicle.TransferState))
                {
                    //logger.fine("not necessary to update it, because transferState is already {" + transferState + "}");
                }
                else
                {
                    needToUpdate = true;
                }
                if (needToUpdate)
                {
                    updateCount = UpdateVehicle(vehicle, "TransferState", transferState);
                    if (updateCount > 0)
                    {
                        //logger.fine("vehicle{" + vehicle.getId() + "}.transferState was changed to {" + transferState + "}.");
                        VehicleEx newVehicle = Refresh(vehicle.Id);
                        this.HistoryManager.CreateVehicleHistory(newVehicle, messageName);
                    }
                }
            }
            else
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table.");
                return 0;
            }
            return updateCount;
        }

        public int UpdateVehicleTransferState(VehicleEx vehicle, String transferState)
        {
            return UpdateVehicleTransferState(vehicle, transferState, "");
        }

        public int UpdateVehicleTransferState(String vehicleId, String transferState, String messageName)
        {
            VehicleEx vehicle = GetVehicle(vehicleId);
            if (vehicle == null)
            {
                logger.Warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table. {" + vehicleId + "}");
                return 0;
            }
            return UpdateVehicleTransferState(vehicle, transferState, messageName);
        }

        public int UpdateVehicleTransferState(String vehicleId, String transferState)
        {
            return UpdateVehicleTransferState(vehicleId, transferState, "");
        }

        public int UpdateVehicleBatteryRate(VehicleEx vehicle, int batteryRate)
        {
            int updateCount = 0;
            if (vehicle != null)
            {
                if (vehicle.BatteryRate == batteryRate)
                {
                    //logger.fine("not necessary to update it, because batteryRate is already {" + batteryRate + "}");
                }
                else
                {
                    updateCount = UpdateVehicle(vehicle, "BatteryRate", batteryRate);
                    if (updateCount > 0)
                    {
                        //logger.fine("vehicle{" + vehicle.getId() + "}.batteryRate was changed to {" + batteryRate + "}");
                        VehicleEx newVehicle = Refresh(vehicle.Id);
                        this.HistoryManager.CreateVehicleBatteryHistory(newVehicle);
                    }
                }
            }
            else
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table.");
            }
            return updateCount;
        }

        public int UpdateVehicleBatteryRate(String vehicleId, int batteryRate)
        {
            VehicleEx vehicle = GetVehicle(vehicleId);
            if (vehicle == null)
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table. {" + vehicleId + "}");
                return 0;
            }
            return UpdateVehicleBatteryRate(vehicle, batteryRate);
        }

        public int UpdateVehicleTransportCommandId(VehicleEx vehicle, String transportCommandId, String messageName)
        {
            int updateCount = 0;
            if (vehicle != null)
            {
                bool needToUpdate = false;
                if (transportCommandId == null)
                {
                    if (vehicle.TransportCommandId == null)
                    {
                        //logger.fine("not necessary to update it, because transportCommandId is already {" + transportCommandId + "}");
                    }
                    else
                    {
                        needToUpdate = true;
                    }
                }
                else if (transportCommandId.Equals(vehicle.TransportCommandId))
                {
                    //logger.fine("not necessary to update it, because transportCommandId is already {" + transportCommandId + "}");
                }
                else
                {
                    needToUpdate = true;
                }
                if (needToUpdate)
                {
                    updateCount = UpdateVehicle(vehicle, "TransportCommandId", transportCommandId);
                    if (updateCount > 0)
                    {
                        //logger.fine("vehicle{" + vehicle.getId() + "}.transportCommandId was changed to{" + transportCommandId + "}.");
                        VehicleEx newVehicle = Refresh(vehicle.Id);
                        this.HistoryManager.CreateVehicleHistory(newVehicle, messageName);
                    }
                }
            }
            else
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table.");
            }
            return updateCount;
        }

        public int UpdateVehicleTransportCommandId(VehicleEx vehicle, String transportCommandId)
        {
            return UpdateVehicleTransportCommandId(vehicle, transportCommandId, "");
        }

        public int UpdateVehicleTransportCommandId(String vehicleId, String transportCommandId)
        {
            VehicleEx vehicle = GetVehicle(vehicleId);
            if (vehicle == null)
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table. {" + vehicleId + "}");
                return 0;
            }
            return UpdateVehicleTransportCommandId(vehicle, transportCommandId);
        }

        public int UpdateVehicleLocation(VehicleEx vehicle, String nodeId, String messageName)
        {
            int updateCount = 0;
            if (vehicle != null)
            {
                if (nodeId.Equals(vehicle.CurrentNodeId))
                {
                    //logger.fine("not necessary to update it, because nodeId is already {" + nodeId + "}");
                }
                else
                {
                    updateCount = UpdateVehicle(vehicle, "CurrentNodeId", nodeId);
                    if (updateCount > 0)
                    {
                        UpdateVehicle(vehicle, "NodeCheckTime",DateTime.Now);
                        //logger.fine("vehicle{" + vehicle.getId() + "}.currentNodeId was changed to {" + nodeId + "}");
                        VehicleEx newVehicle = Refresh(vehicle.Id);
                        this.HistoryManager.CreateVehicleHistory(newVehicle, messageName);
                    }
                }
            }
            else
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table.");
            }
            return updateCount;
        }

        public int UpdateVehicleLocation(VehicleEx vehicle, String nodeId)
        {
            return UpdateVehicleLocation(vehicle, nodeId, "");
        }

        public int UpdateVehicleLocation(String vehicleId, String nodeId, String messageName)
        {
            VehicleEx vehicle = GetVehicle(vehicleId);
            if (vehicle == null)
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table. {" + vehicleId + "}");
                return 0;
            }
            return UpdateVehicleLocation(vehicle, nodeId, messageName);
        }

        public int UpdateVehicleLocation(String vehicleId, String nodeId)
        {
            return UpdateVehicleLocation(vehicleId, nodeId, "");
        }

        public int UpdateVehicleProcessingState(VehicleEx vehicle, String processingState, String messageName)
        {
            int updateCount = 0;
            if (vehicle != null)
            {
                String prevoiusState = vehicle.ProcessingState;
                if (prevoiusState.Equals(processingState))
                {
                    //logger.fine("not necessary to update it, because state is already {" + processingState + "}");
                }
                else
                {
                    updateCount = UpdateVehicle(vehicle, "ProcessingState", processingState);
                    if (updateCount > 0)
                    {
                        //logger.fine("vehicle{" + vehicle.getId() + "}.processingState was changed to {" + processingState + "}");
                        VehicleEx newVehicle = Refresh(vehicle.Id);
                        this.HistoryManager.CreateVehicleHistory(newVehicle, messageName);
                    }
                }
            }
            else
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table.");
            }
            return updateCount;
        }

        public int UpdateVehicleProcessingState(VehicleEx vehicle, String processingState)
        {
            return UpdateVehicleProcessingState(vehicle, processingState, "");
        }

        public int UpdateVehicleProcessingState(String vehicleId, String processingState, String messageName)
        {
            VehicleEx vehicle = GetVehicle(vehicleId);
            if (vehicle == null)
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table. {" + vehicleId + "}");
                return 0;
            }
            return UpdateVehicleProcessingState(vehicle, processingState, messageName);
        }

        public int UpdateVehicleProcessingState(String vehicleId, String processingState)
        {
            return UpdateVehicleProcessingState(vehicleId, processingState, "");
        }

        public int UpdateVehicleConnectionState(VehicleEx vehicle, String connectionState, String messageName)
        {
            int updateCount = 0;
            if (vehicle != null)
            {
                String prevoiusState = vehicle.ConnectionState;
                if (prevoiusState.Equals(connectionState))
                {
                    //logger.fine("not necessary to update it, because state is already {" + connectionState + "}");
                }
                else
                {
                    updateCount = UpdateVehicle(vehicle, "ConnectionState", connectionState);
                    if (updateCount > 0)
                    {
                        //logger.fine("vehicle{" + vehicle.getId() + "}.connectionState was changed to {" + connectionState + "}");
                        VehicleEx newVehicle = Refresh(vehicle.Id);
                        this.HistoryManager.CreateVehicleHistory(newVehicle, messageName);
                    }
                }
            }
            else
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table.");
            }
            return updateCount;
        }

        public int UpdateVehicleConnectionState(VehicleEx vehicle, String connectionState)
        {
            return UpdateVehicleConnectionState(vehicle, connectionState, "");
        }

        public int UpdateVehicleConnectionState(String vehicleId, String connectionState, String messageName)
        {
            VehicleEx vehicle = GetVehicle(vehicleId);
            if (vehicle == null)
            {
                logger.Warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table. {" + vehicleId + "}");
                return 0;
            }
            return UpdateVehicleConnectionState(vehicle, connectionState, messageName);
        }

        public int UpdateVehicleConnectionState(String vehicleId, String connectionState)
        {
            return UpdateVehicleConnectionState(vehicleId, connectionState, "");
        }

        public int UpdateVehicleRunState(VehicleEx vehicle, String runState, String messageName)
        {
            int updateCount = 0;
            if (vehicle != null)
            {
                String prevoiusState = vehicle.RunState;
                if (prevoiusState.Equals(runState))
                {
                    //logger.fine("not necessary to update it, because runState is already {" + runState + "}");
                }
                else
                {
                    updateCount = UpdateVehicle(vehicle, "RunState", runState);
                    if (updateCount > 0)
                    {
                        //logger.fine("vehicle{" + vehicle.getId() + "}.runState was changed to {" + runState + "}");
                        VehicleEx newVehicle = Refresh(vehicle.Id);
                        this.HistoryManager.CreateVehicleHistory(newVehicle, messageName);
                    }
                }
            }
            else
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table.");
                return 0;
            }
            return updateCount;
        }

        public int UpdateVehicleRunState(VehicleEx vehicle, String runState)
        {
            return UpdateVehicleRunState(vehicle, runState, "");
        }

        public int UpdateVehicleRunState(String vehicleId, String runState, String messageName)
        {
            VehicleEx vehicle = GetVehicle(vehicleId);
            if (vehicle == null)
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table. {" + vehicleId + "}");
                return 0;
            }
            return UpdateVehicleRunState(vehicle, runState, messageName);
        }

        public int UpdateVehicleRunState(String vehicleId, String runState)
        {
            return UpdateVehicleRunState(vehicleId, runState, "");
        }

        public int UpdateVehicleFullState(VehicleEx vehicle, String fullState, String messageName)
        {
            int updateCount = 0;
            if (vehicle != null)
            {
                String prevoiusState = vehicle.FullState;
                if ((prevoiusState != null) && (prevoiusState.Equals(fullState)))
                {
                    //logger.fine("not necessary to update it, because state is already {" + fullState + "}");
                }
                else
                {
                    updateCount = UpdateVehicle(vehicle, "FullState", fullState);
                    if (updateCount > 0)
                    {
                        //logger.fine("vehicle{" + vehicle.getId() + "}.fullState was changed to {" + fullState + "}");
                        VehicleEx newVehicle = Refresh(vehicle.Id);
                        this.HistoryManager.CreateVehicleHistory(newVehicle, messageName);
                    }
                }
            }
            else
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table.");
            }
            return updateCount;
        }

        public int UpdateVehicleFullState(VehicleEx vehicle, String fullState)
        {
            return UpdateVehicleFullState(vehicle, fullState, "");
        }

        public int UpdateVehicleFullState(String vehicleId, String fullState, String messageName)
        {
            VehicleEx vehicle = GetVehicle(vehicleId);
            if (vehicle == null)
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table. {" + vehicleId + "}");
                return 0;
            }
            return UpdateVehicleFullState(vehicle, fullState, messageName);
        }

        public int UpdateVehicleFullState(String vehicleId, String fullState)
        {
            return UpdateVehicleFullState(vehicleId, fullState, "");
        }

        public int UpdateVehicleNodeCheckTime(VehicleEx vehicle)
        {
            int updateCount = 0;
            if (vehicle != null)
            {
                vehicle.NodeCheckTime = DateTime.Now;
                updateCount = UpdateVehicle(vehicle, "NodeCheckTime", vehicle.NodeCheckTime);
                if (updateCount > 0)
                {
                    //logger.fine("vehicle{" + vehicle.getId() + "}.nodeCheckTime was changed.");
                }
            }
            else
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table.");
            }
            return updateCount;
        }

        public int UpdateVehicleNodeCheckTime(String vehicleId)
        {
            VehicleEx vehicle = GetVehicle(vehicleId);
            if (vehicle == null)
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table. {" + vehicleId + "}");
                return 0;
            }
            return UpdateVehicleNodeCheckTime(vehicle);
        }

        public int UpdateVehicleAlarmState(VehicleEx vehicle, String state, String messageName)
        {
            int updateCount = 0;
            if (vehicle != null)
            {
                String prevoiusState = vehicle.AlarmState;
                if (prevoiusState.Equals(state))
                {
                    //logger.fine("not necessary to update it, because alarmState is already {" + state + "}");
                }
                else
                {
                    updateCount = UpdateVehicle(vehicle, "AlarmState", state);
                    if (updateCount > 0)
                    {
                        //logger.fine("vehicle{" + vehicle.getId() + "}.alarmState was changed to {" + state + "}");
                        VehicleEx newVehicle = Refresh(vehicle.Id);
                        this.HistoryManager.CreateVehicleHistory(newVehicle, messageName);
                    }
                }
            }
            else
            {
                //logger.error("vehicle is not exist. check NA_R_VEHICLE table.");
            }
            return updateCount;
        }

        public int UpdateVehicleAlarmState(VehicleEx vehicle, String state)
        {
            return UpdateVehicleAlarmState(vehicle, state, "");
        }

        public int UpdateVehicleAlarmState(String vehicleId, String state, String messageName)
        {
            VehicleEx vehicle = GetVehicle(vehicleId);
            if (vehicle == null)
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table. {" + vehicleId + "}");
                return 0;
            }
            return UpdateVehicleAlarmState(vehicle, state, messageName);
        }

        public int UpdateVehicleAlarmState(String vehicleId, String state)
        {
            return UpdateVehicleAlarmState(vehicleId, state, "");
        }

        public int UpdateVehicleBatteryVoltage(VehicleEx vehicle, float voltage)
        {
            int updateCount = 0;
            if (vehicle != null)
            {
                if (vehicle.BatteryVoltage == voltage)
                {
                    //logger.fine("not necessary to update it, because voltage is already {" + voltage + "}");
                }
                else
                {
                    updateCount = UpdateVehicle(vehicle, "BatteryVoltage", voltage);
                    if (updateCount > 0)
                    {
                        //logger.fine("vehicle{" + vehicle.getId() + "}.batteryVoltage was changed to {" + voltage + "}");
                        VehicleEx newVehicle = Refresh(vehicle.Id);
                        this.HistoryManager.CreateVehicleBatteryHistory(newVehicle);
                    }
                }
            }
            else
            {
                //logger.error("vehicle is not exist. check Vehicle table.");
            }
            return updateCount;
        }

        public int UpdateVehicleBatteryVoltage(String vehicleId, float voltage)
        {
            VehicleEx vehicle = GetVehicle(vehicleId);
            if (vehicle == null)
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table. {" + vehicleId + "}");
                return 0;
            }
            return UpdateVehicleBatteryVoltage(vehicle, voltage);
        }

        public int UpdateVehicleEventTime(VehicleEx vehicle)
        {
            int updateCount = 0;
            if (vehicle != null)
            {

                vehicle.EventTime = DateTime.Now;

                updateCount = UpdateVehicle(vehicle, "EventTime", vehicle.EventTime);
                if (updateCount > 0)
                {
                    //logger.fine("vehicle{" + vehicle.getId() + "}.eventTime was changed.");
                }
            }
            else
            {
                //logger.warn("vehicle is not exist. check NA_R_VEHICLE table.");
            }
            return updateCount;
        }

        public int UpdateVehicleEventTime(String vehicleId)
        {
            VehicleEx vehicle = GetVehicle(vehicleId);
            if (vehicle == null)
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table. {" + vehicleId + "}");
                return 0;
            }
            return UpdateVehicleEventTime(vehicle);
        }

        public int UpdateVehiclePlcVersion(String vehicleId, String version)
        {
            int updateCount = 0;
            VehicleEx vehicle = GetVehicle(vehicleId);
            if (vehicle != null)
            {
                if (version.Equals(vehicle.PlcVersion))
                {
                    //logger.fine("not necessary to update it, because plcVersion is already {" + version + "}");
                }
                else
                {
                    vehicle.PlcVersion = version;
                    updateCount = UpdateVehicle(vehicle, "PlcVersion", vehicle.PlcVersion);
                    if (updateCount > 0)
                    {
                        //logger.fine("vehicle{" + vehicle.getId() + "}.plcVersion was changed.");
                    }
                }
            }
            else
            {
                //logger.warn("vehicle id : " + vehicleId + " . could not found.");
            }
            return updateCount;
        }

        public int UpdateVehicleMainboardVersion(String vehicleId, String version)
        {
            int updateCount = 0;
            VehicleEx vehicle = GetVehicle(vehicleId);
            if (vehicle != null)
            {
                if (version.Equals(vehicle.Version))
                {
                    //logger.fine("not necessary to update it, because version is already {" + version + "}");
                }
                else
                {
                    vehicle.Version = version;
                    updateCount = UpdateVehicle(vehicle, "Version", vehicle.Version);
                    if (updateCount > 0)
                    {
                        //logger.fine("vehicle{" + vehicle.getId() + "}.version was changed.");
                    }
                }
            }
            else
            {
                //logger.warn("vehicle id : " + vehicleId + " . could not found.");
            }
            return updateCount;
        }

        public int UpdateVehicleAcsDestNodeId(VehicleEx vehicle, String destNodeId, String messageName)
        {
            int updateCount = 0;
            if (vehicle != null)
            {
                bool needToUpdate = false;
                if (destNodeId == null)
                {
                    if (vehicle.AcsDestNodeId == null)
                    {
                        //logger.fine("not necessary to update it, because acsDestNodeId is already {" + destNodeId + "}");
                    }
                    else
                    {
                        needToUpdate = true;
                    }
                }
                else if (destNodeId.Equals(vehicle.AcsDestNodeId))
                {
                    //logger.fine("not necessary to update it, because acsDestNodeId is already {" + destNodeId + "}");
                }
                else
                {
                    needToUpdate = true;
                }
                if (needToUpdate)
                {
                    updateCount = UpdateVehicle(vehicle, "AcsDestNodeId", destNodeId);
                    if (updateCount > 0)
                    {
                        //logger.fine("vehicle{" + vehicle.getId() + "}.acsDestNodeId was changed from{" + vehicle.getAcsDestNodeId() +
                        // "} to{" + destNodeId + "}.");
                        VehicleEx newVehicle = Refresh(vehicle.Id);
                        this.HistoryManager.CreateVehicleHistory(newVehicle, messageName);
                    }
                }
            }
            else
            {
                //logger.warn("vehicle is not exist. check NA_R_VEHICLE table.");
            }
            return updateCount;
        }

        public int UpdateVehicleAcsDestNodeId(VehicleEx vehicle, String destNodeId)
        {
            return UpdateVehicleAcsDestNodeId(vehicle, destNodeId, "");
        }

        public int UpdateVehicleAcsDestNodeId(String vehicleId, String destNodeId, String messageName)
        {
            VehicleEx vehicle = GetVehicle(vehicleId);
            if (vehicle == null)
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table. {" + vehicleId + "}");
                return 0;
            }
            return UpdateVehicleAcsDestNodeId(vehicle, destNodeId, messageName);
        }

        public int UpdateVehicleAcsDestNodeId(String vehicleId, String destNodeId)
        {
            return UpdateVehicleAcsDestNodeId(vehicleId, destNodeId, "");
        }

        public int UpdateVehicleVehicleDestNodeId(VehicleEx vehicle, String destNodeId, String messageName)
        {
            int updateCount = 0;
            if (vehicle != null)
            {
                bool needToUpdate = false;
                if (destNodeId == null)
                {
                    if (vehicle.VehicleDestNodeId == null)
                    {
                        //logger.fine("not necessary to update it, because destNodeId is already {" + destNodeId + "}");
                    }
                    else
                    {
                        needToUpdate = true;
                    }
                }
                else if (destNodeId.Equals(vehicle.VehicleDestNodeId))
                {
                    //logger.fine("not necessary to update it, because destNodeId is already {" + destNodeId + "}");
                }
                else
                {
                    needToUpdate = true;
                }
                if (needToUpdate)
                {
                    updateCount = UpdateVehicle(vehicle, "VehicleDestNodeId", destNodeId);
                    if (updateCount > 0)
                    {
                        //logger.fine("vehicle{" + vehicle.getId() + "}.vehicleDestNodeId was changed from{" + vehicle.getVehicleDestNodeId() +
                        // "} to{" + destNodeId + "}.");
                        VehicleEx newVehicle = Refresh(vehicle.Id);
                        this.HistoryManager.CreateVehicleHistory(newVehicle, messageName);
                    }
                }
            }
            else
            {
                //logger.warn("vehicle is not exist. check NA_R_VEHICLE table.");
            }
            return updateCount;
        }

        public int UpdateVehicleVehicleDestNodeId(VehicleEx vehicle, String destNodeId)
        {
            return UpdateVehicleVehicleDestNodeId(vehicle, destNodeId, "");
        }

        public int UpdateVehicleVehicleDestNodeId(String vehicleId, String destNodeId, String messageName)
        {
            VehicleEx vehicle = GetVehicle(vehicleId);
            if (vehicle == null)
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table. {" + vehicleId + "}");
                return 0;
            }
            return UpdateVehicleVehicleDestNodeId(vehicle, destNodeId, messageName);
        }

        public int UpdateVehicleVehicleDestNodeId(String vehicleId, String destNodeId)
        {
            return UpdateVehicleVehicleDestNodeId(vehicleId, destNodeId, "");
        }

        public void CreateVehicleCrossWait(VehicleCrossWaitEx vehicleCrossWait)
        {
            try
            {
                this.PersistentDao.Save(vehicleCrossWait);
            }
            catch (Exception e)
            {
                //logger.warn("failed to createVehicleCrossWait{" + vehicleCrossWait.toString() + "}", e);
            }
        }

        public void DeleteVehicleCrossWait(VehicleCrossWaitEx vehicleCrossWait)
        {
            this.PersistentDao.Delete(vehicleCrossWait, false);
        }

        public int DeleteVehicleCrossWait(String vehicleId)
        {
            return this.PersistentDao.DeleteByAttribute(typeof(VehicleCrossWaitEx), "VehicleId", vehicleId);
        }

        public VehicleCrossWaitEx GetVehicleCrossWait(String vehicleId)
        {
            IList vehicleCrossWait = this.PersistentDao.FindByAttribute(typeof(VehicleCrossWaitEx), "VehicleId", vehicleId);
            if (vehicleCrossWait == null || vehicleCrossWait.Count <= 0)
            {
                //logger.info("This vehicle {" + vehicleId + "} dose not in vehicleCrossWait.");
                return null;
            }
            return (VehicleCrossWaitEx)vehicleCrossWait[0];
        }

        public IList GetVehiclesCrossWaitByNodeId(String nodeId)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("NodeId", nodeId);
            attributes.Add("State", "WAIT");

            return this.PersistentDao.FindByAttributesOrderBy(typeof(VehicleCrossWaitEx), attributes, "CreatedTime");
        }

        public IList GetVehiclesCrossWaitByNodeIdAndState(String nodeId, String state)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("NodeId", nodeId);
            attributes.Add("State", state);

            return this.PersistentDao.FindByAttributesOrderBy(typeof(VehicleCrossWaitEx), attributes, "CreatedTime");
        }

        public IList GetVehiclesCrossWaitByStateOrderbyCreateTime(String state)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("State", state);

            return this.PersistentDao.FindByAttributesOrderBy(typeof(VehicleCrossWaitEx), attributes, "CreatedTime");
        }

        public IList GetVehiclesCrossWaitAllOrderbyCreateTime()
        {
            return this.PersistentDao.FindAllOrderBy(typeof(VehicleCrossWaitEx), "CreatedTime");
        }

        public List<VehicleCrossWaitEx> GetVehiclesCrossWaitByStateAndBeforeTimeOrderbyCreateTime(String state, int delayedTime)
        {
            String hql = "select t.vehicleId from VehicleCrossWaitEx t where state=" + state +
              " and (sysdate - cast((t.createdTime) as date))*24*60*60 > " + delayedTime;

            IList<VehicleCrossWaitEx> vehicles = this.PersistentDao.Find<VehicleCrossWaitEx>(hql);
            List<VehicleCrossWaitEx> newListVehicles = new List<VehicleCrossWaitEx>();
            if ((vehicles != null) && (vehicles.Count > 0))
            {
                foreach (var val in vehicles)
                {
                    String vehicleId = val.VehicleId;
                    VehicleCrossWaitEx crossNodeWait = GetVehicleCrossWait(vehicleId);

                    newListVehicles.Add(crossNodeWait);
                }
            }
            return newListVehicles;
        }

        public VehicleCrossWaitEx GetVehicleCrossWaitByNodeId(String nodeId)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("NodeId", nodeId);
            attributes.Add("State", "WAIT");

            IList vehicleCrossWait = this.PersistentDao.FindByAttributesOrderBy(typeof(VehicleCrossWaitEx), attributes, "CreatedTime");
            if (vehicleCrossWait.Count == 0)
            {
                //logger.info("This Node {" + nodeId + "} dose not have Wait Vehicle.");
                return null;
            }
            return (VehicleCrossWaitEx)vehicleCrossWait[0];
        }

        public VehicleCrossWaitEx GetVehicleCrossWaitAllByNodeId(String nodeId)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("NodeId", nodeId);

            IList vehicleCrossWait = this.PersistentDao.FindByAttributesOrderBy(typeof(VehicleCrossWaitEx), attributes,
              "CreatedTime");
            if (vehicleCrossWait.Count == 0)
            {
                //logger.info("This Node {" + nodeId + "} dose not have Wait Vehicle.");
                return null;
            }
            return (VehicleCrossWaitEx)vehicleCrossWait[0];
        }

        public void UpdateVehicleCrossWait(VehicleCrossWaitEx vehicleCrossWait)
        {
            this.PersistentDao.Update(vehicleCrossWait, false);
        }

        public int UpdateVehicleCrossWait(VehicleCrossWaitEx vehicleCrossWait, Dictionary<string, object> setAttributes)
        {
            return this.PersistentDao.UpdateByAttributes(typeof(VehicleCrossWaitEx).Name, setAttributes, "VehicleId", vehicleCrossWait.VehicleId);

        }

        public String GetBayIdByStationId(String stationId)
        {
            StationEx station = GetStation(stationId);
            LinkEx link = GetLink(station.LinkId);

            LinkZoneEx linkZone = GetLinkZoneByLinkId(station.LinkId);

            String zoneId = linkZone.ZoneId;

            ZoneEx zone = GetZone(zoneId);

            return zone.BayId;
        }

        public String GetBayIdByLocationId(String locationId)
        {
            LocationEx location = GetLocation(locationId);
            if (location == null)
            {
                return null;
            }
            StationEx station = GetStation(location.StationId);
            LinkEx link = GetLink(station.LinkId);

            LinkZoneEx linkZone = GetLinkZoneByLinkId(station.LinkId);

            String zoneId = linkZone.ZoneId;

            ZoneEx zone = GetZone(zoneId);

            return zone.BayId;
        }

        public LinkZoneEx GetLinkZoneByLinkId(String linkId)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("LinkId", linkId);

            IList linkZone = this.PersistentDao.FindByAttributes(typeof(LinkZoneEx), attributes);
            if (linkZone.Count == 0)
            {
                //logger.info("This LinkId {" + linkId + "} dose not exist LinkZone.");
                return null;
            }
            return (LinkZoneEx)linkZone[0];
        }

        public IList GetLinkZonesByZoneId(String zoneId)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("ZoneId", zoneId);

            return this.PersistentDao.FindByAttributes(typeof(LinkZoneEx), attributes);
        }

        public IList GetLinkZonesByZoneIdTransferFlag(String zoneId, String transferFlag)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("ZoneId", zoneId);
            attributes.Add("TransferFlag", transferFlag);

            return this.PersistentDao.FindByAttributes(typeof(LinkZoneEx), attributes);
        }

        public IList GetUIInformBySource(String source)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("Source", source);

            return this.PersistentDao.FindByAttributes(typeof(Inform), attributes);
        }

        public void CreateInform(Inform inform)
        {
            //20190912 KSB No use
            //this.PersistentDao.Save(inform);
        }

        public void UpdateInform(Inform inform)
        {
            //20190912 KSB No use
            //this.PersistentDao.Update(inform, false);
        }

        public int DeleteUIInform(DateTime toDate)
        {
            return this.PersistentDao.DeleteByTime(typeof(Inform), toDate);
        }

        public int DeleteUIInform(String vehicleId)
        {
            //20190906 KSB DB 이상요인으로..
            //String hql = "DELETE " + typeof(Inform).Name + " WHERE SOURCE ='" + vehicleId + "' AND MESSAGE NOT LIKE 'VEHICLE%'";

            //return this.PersistentDao.DeleteByHql(hql);
            return 0;
        }

        public int DeleteVehicleCrossWait(DateTime toDate)
        {
            //190329 Oracle
            ////String to = toDate.ToString("yyyy-MM-dd HH:mm:ss.mmm");
            //String to = toDate.ToString("dd-MMM-yy HH.mm.ss.ffff tt");

            //String hql = "delete " + typeof(VehicleCrossWaitEx).Name + " where STATE='GOING' AND createdTime < '" + to + "'";

            //return this.PersistentDao.DeleteByHql(hql);

            String to = string.Empty;
            String hql = string.Empty;

            //KSB 대소문자 구분해서.오류.. (Oracle != oracle)
            //if (ConfigurationManager.AppSettings[Settings.SYSTEM_DATABASE_TYPE] == Settings.DB_ORACLE)
            if (string.Equals(ConfigurationManager.AppSettings[Settings.SYSTEM_DATABASE_TYPE], Settings.DB_ORACLE, StringComparison.CurrentCultureIgnoreCase))
            {
                //to = toDate.ToString("dd-MMM-yy hh.mm.ss.ffff tt");
                to = toDate.ToString("yy/MM/dd HH:mm:ss.mmmm");
                hql = "delete " + typeof(VehicleCrossWaitEx).Name + " where STATE='GOING' AND createdTime < to_timestamp('" + to + "'" + ",'yy/mm/dd hh24:mi:ss.ff')";
            }
            else
            {
                to = toDate.ToString("yyyy-MM-dd HH:mm:ss.mmm");
                hql = "delete " + typeof(VehicleCrossWaitEx).Name + " where STATE='GOING' AND createdTime < '" + to + "'";
            }

            return this.PersistentDao.DeleteByHql(hql);
        }

        public IList GetUIInformByMessage(String source, String message)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("Source", source);
            attributes.Add("Message", message);

            return this.PersistentDao.FindByAttributes(typeof(Inform), attributes);
        }

        public IList GetUIInformByDescription(String source, String description)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("Source", source);
            attributes.Add("Description", description);

            return this.PersistentDao.FindByAttributes(typeof(Inform), attributes);
        }

        public int DeleteVehicleCrossWait(DateTime toDate, String nodeId)
        {
            //190329 Oracle
            ////String to = toDate.ToString("yyyy-MM-dd HH:mm:ss.mmm");
            //String to = toDate.ToString("dd-MMM-yy HH.mm.ss.ffff tt");
            //String hql = "delete " + typeof(VehicleCrossWaitEx).Name + " where nodeId = '" + nodeId +
            //  "' and state='GOING' AND createdTime < '" + to + "'";

            //return this.PersistentDao.DeleteByHql(hql);

            String to = string.Empty;
            String hql = string.Empty;

            //KSB 대소문자 구분해서.오류.. (Oracle != oracle)
            //if (ConfigurationManager.AppSettings[Settings.SYSTEM_DATABASE_TYPE] == Settings.DB_ORACLE)
            if (string.Equals(ConfigurationManager.AppSettings[Settings.SYSTEM_DATABASE_TYPE], Settings.DB_ORACLE, StringComparison.CurrentCultureIgnoreCase))
            {
                //to = toDate.ToString("dd-MMM-yy hh.mm.ss.ffff tt");
                to = toDate.ToString("yy/MM/dd HH:mm:ss.mmmm");
                hql = "delete " + typeof(VehicleCrossWaitEx).Name + " where nodeId = '" + nodeId +
                  "' and state='GOING' AND createdTime < to_timestamp('" + to + "'" + ", 'yy/mm/dd hh24:mi:ss.ff')";
            }
            else
            {
                to = toDate.ToString("yyyy-MM-dd HH:mm:ss.mmm");
                hql = "delete " + typeof(VehicleCrossWaitEx).Name + " where nodeId = '" + nodeId +
                  "' and state='GOING' AND createdTime < '" + to + "'";
            }

            return this.PersistentDao.DeleteByHql(hql);
        }

        public int UpdateVehicleTransportCommandId(String vehicleId, String transportCommandId, String messageName)
        {
            int updateCount = 0;
            VehicleEx vehicle = GetVehicle(vehicleId);
            if (vehicle != null)
            {
                bool needToUpdate = false;
                if (transportCommandId == null)
                {
                    if (vehicle.TransportCommandId == null)
                    {
                        //logger.fine("not necessary to update it, because transportCommandId is already {" + transportCommandId + "}");
                    }
                    else
                    {
                        needToUpdate = true;
                    }
                }
                else if (transportCommandId.Equals(vehicle.TransportCommandId))
                {
                    //logger.fine("not necessary to update it, because transportCommandId is already {" + transportCommandId + "}");
                }
                else
                {
                    needToUpdate = true;
                }
                if (needToUpdate)
                {
                    updateCount = UpdateVehicle(vehicle, "TransportCommandId", transportCommandId);
                    if (updateCount > 0)
                    {
                        //logger.fine("vehicle{" + vehicle.getId() + "}.transportCommandId was changed to{" + transportCommandId + "}.");
                        VehicleEx newVehicle = Refresh(vehicle.Id);
                        this.HistoryManager.CreateVehicleHistory(newVehicle, messageName);
                    }
                }
            }
            else
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table.");
            }
            return updateCount;
        }


        public Machine GetMachineByName(string machineName)
        {
            return (Machine)this.PersistentDao.FindByName(typeof(Machine), machineName, false);
        }


        public Unit GetUnitByName(string unitName, string machineName)
        {
            if(IsNameValid(unitName) && IsNameValid(machineName))
            {
                var attributes = new Dictionary<string, object>
                {
                    { "Name", unitName },
                    { "MachineName", machineName }
                };
                IList units = this.PersistentDao.FindByAttributes(typeof(Unit), attributes);
                if(units.Count > 0)
                {
                    return (Unit)units[0];
                }
                logger.Info("unit{" + unitName + "} of machine{" + machineName + "} does not exist in Repository");
            }

            return null;
        }

        public int UpdateVehicleState(VehicleEx vehicle, String State, String messageName)
        {
            int updateCount = 0;
            if (vehicle != null)
            {
                String prevoiusState = vehicle.State;
                if (prevoiusState.Equals(State))
                {
                    //logger.fine("not necessary to update it, because state is already {" + processingState + "}");
                }
                else
                {
                    updateCount = UpdateVehicle(vehicle, "State", State);
                    if (updateCount > 0)
                    {
                        //logger.fine("vehicle{" + vehicle.getId() + "}.processingState was changed to {" + processingState + "}");
                        VehicleEx newVehicle = Refresh(vehicle.Id);
                        this.HistoryManager.CreateVehicleHistory(newVehicle, messageName);
                    }
                }
            }
            else
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table.");
            }
            return updateCount;
        }
        public int UpdateVehicleState(String vehicleId, String State, String messageName)
        {
            VehicleEx vehicle = GetVehicle(vehicleId);
            if (vehicle == null)
            {
                //logger.warn("This vehicleId is not in ACS DB. check NA_R_VEHICLE table. {" + vehicleId + "}");
                return 0;
            }
            return UpdateVehicleState(vehicle, State, messageName);
        }

    }

}
