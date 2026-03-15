using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Path;
using ACS.Core.Path.Model;
using ACS.Core.Transfer.Model;
using ACS.Core.Path.Model;
using ACS.Core.Resource.Model;

namespace ACS.Core.Path
{
    public interface IPathManagerExs : IPathManagerEx
    {
        VehicleEx SearchSuitableVehicleDijkstraCache(BayEx bay, string sourceNode);

        VehicleEx SearchSuitableVehicleDijkstraCache(TransportCommandEx transportCmd, BayEx bay, string sourceNode);
        VehicleEx SearchSuitableVehicleDijkstraCache(TransportCommandEx transportCmd, BayEx bay, string sourceNode, Dictionary<string, IList> linkMap);
        VehicleEx SearchSuitableParkingVehicleDijkstraCache(BayEx bay, string sourceNode);

        PathEx SearchDynamicPathsDijkstra(string startNode, string destNode);
        //PathEx SearchDynamicPathsDijkstra(string startNode, string destNode, Dictionary<string, IList> linksMap);
        PathEx SearchDynamicPathsDijkstra(string startNode, string destNode, bool needTurnCost, bool needTraffic);
        PathEx SearchDynamicPathsDijkstra(string startNode, string destNode, Dictionary<string, IList> linksMap, bool needTurnCost, bool needTraffic);
        PathEx SearchDynamicPathsDijkstraDivide(String startNode, String destNode);

        PathEx SearchDynamicPathsDijkstraDivide(String startNode, String id, Dictionary<String,IList> linkMap);


        PathEx SearchDynamicPathsDijkstraEasy(string startNode, string destNode);

        IList GetLinksByFromNodeId(String fromNodeId);
        IList GetLinksByToNodeId(String toNodeId);
        Dictionary<int, VehicleEx> SearchSuitableVehicleDijkstra(Dictionary<int, VehicleEx> vehicleCost, Dictionary<List<String>, int> paths, IDictionary linkMap, IDictionary vehicleMap, Dictionary<string, IList> checkedPaths);

        Dictionary<int, VehicleEx> SearchSuitableVehicleDijkstraLink(Dictionary<int, VehicleEx> vehicleCost, Dictionary<List<String>, int> paths, IDictionary linkMap, IDictionary vehicleMap, Dictionary<string, IList> checkedPaths);

        VehicleEx SearchSuitableVehicle(StationEx sourceStation, String bayId, bool isContainDeadNode, bool isContainChargeAGV);

        VehicleEx SearchSuitableVehicle(IList linkViews, IDictionary linksMap, StationEx sourceStation, String bayId, bool isContainDeadNode, bool isContainChargeAGV, TransportCommandEx transportCommand);
        VehicleEx SearchSuitableChargeGroupVehicleDijkstraCache(String[] bayGroupCharge, LocationEx destLocation); //221212 copy
        VehicleEx SearchSuitableChargeVehicleDijkstraCache(string bayid, LocationEx destLocation); //221212 copy
        VehicleEx SearchSuitableParkingVehicle(StationEx sourceStation, String bayId, bool isContainDeadNode, bool isContainChargeAGV);

        VehicleEx SearchSuitableParkingVehicle(IList linkViews, IDictionary linksMap, StationEx sourceStation, String bayId, bool isContainDeadNode, bool isContainChargeAGV);

        void UpdateTransportCommandPathMap(TransportCommandEx transportCommand);

        string SearchNodeInPathAndReCalculate(string vehicleId, string node, string path, string pastPath);

        Dictionary<int, List<string>> SearchPathFromVehicleToDest(Dictionary<List<string>, int> paths, IDictionary linkMap, string endNode, Dictionary<string, IList> checkedPaths, int maxloop);

        string ListToString(List<string> path);

        int CalculateAngle3Point(NodeEx lastNode, NodeEx currentNode, NodeEx nextNode);
        int ToDegress(double d);


    }
}
