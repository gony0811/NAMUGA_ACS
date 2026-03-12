using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Extension.Framework.Resource.Model;
using ACS.Framework.Resource;
using ACS.Framework.Resource.Model;
using ACS.Framework.Transfer.Model;
using ACS.Extension.Framework.Resource.Model;

namespace ACS.Extension.Framework.Resource
{
    public interface IResourceManagerExs : IResourceManagerEx
    {
        void CreateVehicleIdle(VehicleIdleEx vehicleIdle);
        
        int DeleteVehicleIdle(VehicleIdleEx vehicleIdle);
        
        int DeleteVehicleIdleByVehicleId(string VehicleId);
        
        VehicleIdleEx GetVehicleIdleACSByVehicleId(string vehicleId);
        
        VehicleIdleEx GetVehicleIdle(string id);
        
        VehicleIdleEx GetVehicleIdleByBayID(string paramString);
        
        IList GetWaitPointByTypeAndBayId(string paramString1, string paramString2);
        
        bool IsHaveVehicleGoToDestNode(string destNode);
        
        IList GetLinkZones();
        
        String GetPortTypeByPortId(String portId, BayEx bay);
        
        IList GetVehicles(string bayId, bool containUnavailableVehicle);

       
        TransportCommandEx GetTransportCommandBySourcePortId(string sourcePortId);

        bool IsRGV(VehicleEx vehicle);

        float GetBayLimitVoltage(string bayId);

        void CreateVehicleOrder(VehicleOrderEx vehicleOrder);

        int DeleteVehicleOrder(VehicleOrderEx vehicleOrder);

        int DeleteVehicleOrderByVehicleID(String vehicleId);

        void UpdateVehicleOrder(VehicleOrderEx vehicleOrder);

        VehicleOrderEx GetVehicleOrderByVehicleId(String vehicleId);

        string GetPortTypeByPortIdRobot(string destPortId, string sourcePortId);

        String GetPortTypeByStationId(String stationId, BayEx bay);

        String GetPortTypeByStationId(String stationId);

        IList GetVehiclesForCharge(String bayId, bool containUnavailableVehicle);

        IList GetVehiclesByInterSectionStartNodes(IList startNodeIds);

        IList GetRunningVehiclesByNodeList(IList nodeIds);


        Dictionary<string, List<VehicleEx>> MapVehicleByCurrentNode();

        void UpdateVehicleIdleTime(VehicleIdleEx vehicleIdle);

        bool CheckNodeIsMonitoringNode(string nodeId);

        string GetNodeTypeByNodeId(string nodeId);

        void CreateVehicleOrderACS(VehicleOrderEx vehicleOrder);

        int DeleteVehicleOrderACSByVehicleID(string ID);

        OrderPairNodeEx SearchNextOrderNode(String path, string bayid);

        OrderPairNodeEx SearchNextOrderNode(String path);

        OrderPairNodeEx GetOrderPairByGroup(String orderGroup);
        SpecialConfig GetValuesBySpecialName(String specialName);

        bool CheckValueBySpecialConfig(String specialName, string value);
        bool CheckLinkViewByFromNodeAndBayId(String fromNode, String bayId);

        void UpdateVehicleLastChargeBattery(VehicleExs vehicle);

        //IList GetVehiclesForCharge(String bayId, bool containUnavailableVwhicle);


        // 221212 copy
        BayGroupCharegeEx GetBayGroupCharge(string id);
    }
}
