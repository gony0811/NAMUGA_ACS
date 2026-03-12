using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Cache;
using ACS.Framework.Path;
using ACS.Framework.Path.Model;
using ACS.Framework.Resource;
using ACS.Framework.Resource.Model;
using ACS.Framework.History.Model;
using ACS.Framework.Path.Model;
using ACS.Framework.Resource.Model;
using ACS.Framework.Transfer.Model;
using ACS.Framework.Alarm.Model;
using ACS.Framework.Base;
using ACS.Manager.Path;
using Spring.Collections;
using NHibernate.Criterion;
using ACS.Framework.History;
using ACS.Framework.Logging;

namespace ACS.Manager
{
    public class PathManagerExsImplement : PathManagerExImplement, IPathManagerExs
    {
        #region Private Member Variable

        public static int MAX_TIMES_USED_LINK = 100;

        private int maxFindLength = 100000;
        private int agvStopCost = 1500;
        private int agvRunCost = 300;
        private int waitPointCost = 1000;
        private double turnCostRate = 3.3;

        private List<LinkEx> listLink = new List<LinkEx>();
        private Dictionary<string, List<LinkViewEx>> mapLinkViewEx = new Dictionary<string, List<LinkViewEx>>();
        private Dictionary<string, int> mapLinkViewExCount = new Dictionary<string, int>();

        #endregion

        protected Logger oCodelogger = Logger.GetLogger("OCODELog");

        #region Public Properties
        public List<LinkEx> ListLink { get { return listLink; } set { value = listLink; } }
        public ICacheManagerEx CacheManager { get; set; }
        public IResourceManagerExs ResourceManager { get; set; }
        public new IComparer<VehicleEx> NodeCheckTimeComparator { get; set; }
        public new IComparer Comparator { get; set; }
        public IHistoryManagerExs HistoryManager { get; set; }

        #endregion

        public void CreateSuitableVehicleHistory(String bayId, String transportCommandId, String sourceNodeId, String sourcePortId, String selectedVehicleId, String vehicleId, String currentNodeId, String cost)
        {

            SuitableVehicleHistory suitableVehicleHistory = new SuitableVehicleHistory();

            suitableVehicleHistory.BayId = bayId;
            suitableVehicleHistory.TransportCommandId = transportCommandId;
            suitableVehicleHistory.SourceNodeId = sourceNodeId;
            suitableVehicleHistory.SourcePortId = sourcePortId;
            suitableVehicleHistory.SelectedVehicleId = selectedVehicleId;
            suitableVehicleHistory.VehicleId = vehicleId;
            suitableVehicleHistory.CurrentNodeId = currentNodeId;
            suitableVehicleHistory.Cost = cost;

            this.CreateSuitableVehicleHistory(suitableVehicleHistory);
        }

        public void CreateSuitableVehicleHistory(SuitableVehicleHistory suitableVehicleHistory)
        {
            this.PersistentDao.Save(suitableVehicleHistory);
        }

        public int CalculateAngle3Point(NodeEx lastNode, NodeEx currentNode, NodeEx nextNode)
        {
            long fi = DateTime.Now.Millisecond;
            int fiAngle = ToDegress(Math.Atan2(lastNode.Ypos - currentNode.Ypos, lastNode.Xpos - currentNode.Xpos));
            int secAngle = ToDegress(Math.Atan2(nextNode.Ypos - currentNode.Ypos, nextNode.Xpos - currentNode.Xpos));
            int totalAngle = fiAngle - secAngle > 0 ? fiAngle - secAngle : fiAngle - secAngle + 360;
            //logger.warn("Duong re: " + lastNode.getId() + " " + currentNode.getId() + " " + nextNode.getId() + ":" + (360-totalAngle) + ":" + (System.currentTimeMillis() - fi) + " ms");
            if (totalAngle > 180)
            {
                return totalAngle - 180;
            }
            return 180 - totalAngle;
        }


        public VehicleEx SearchSuitableVehicleDijkstraCache(BayEx bay, string sourceNode)
        {
            string bayId = bay.Id;
            IList vehicles = ResourceManager.GetVehicles(bayId, false);

            if ((vehicles == null) || vehicles.Count < 1)
            {
                return null;
                //return SearchSuitableParkingVehicleDijkstraCache(bay, sourceNode);
            }

            if ((!string.IsNullOrEmpty(bay.AgvType)) && string.Equals(bay.AgvType, BayExs.AGVTYPE_RGV))
            {
                if (vehicles.Count > 0)
                {
                    return (VehicleEx)vehicles[0];
                }
            }

            Dictionary<int, VehicleEx> vehicleCost = new Dictionary<int, VehicleEx>();

            IList linkViews = this.CacheManager.GetLinkViewByBayId(bay.Id);

            if (linkViews != null)
            {
                //IList => LinkViewList
                Dictionary<string, IList> linkViewMap = this.ConvertLinkViewsToMapByFromNode(linkViews);

                foreach (var itemvehicle in vehicles)
                {
                    VehicleEx vehicle = (VehicleEx)itemvehicle;

                    //PathACS vehiclePath = this.searchDynamicPathsDijkstraV2(vehicle.getCurrentNodeId(), sourceNode);
                    PathEx vehiclePath = this.SearchDynamicPathsDijkstra(vehicle.CurrentNodeId, sourceNode, linkViewMap, true, false);
                    if (vehiclePath != null)
                    {
                        String path = this.ListToString(vehiclePath.NodeIds);
                        vehicle.Path = string.Format(path + ":" + (int)(vehiclePath.Cost * 60 / 2500));

                        if (!vehicleCost.ContainsKey(vehiclePath.Cost))
                            vehicleCost.Add(vehiclePath.Cost, vehicle);
                    }
                }
                VehicleEx foundVehicle = null;
                if (!(vehicleCost.Count == 0))
                {
                    foreach (var entry in vehicleCost)
                    {
                        foundVehicle = entry.Value;
                        break;
                    }
                }
                else
                {
                    //20200612 LYS  Send QueueJob to everything Vehicle
                    //foundVehicle = SearchSuitableParkingVehicleDijkstraCache(bay, sourceNode, linkMap); ;
                    foundVehicle = null;
                }

                if (foundVehicle != null)
                {
                    logger.Info("Find Vehicle : " + foundVehicle);
                    try
                    {
                        //LocationACS sourceLocation = this.pathManager.getLocationByPortId(transportCommand.getSource());
                        LocationEx sourceLocation = this.CacheManager.GetLocationByStationId(sourceNode);
                        String stationId = sourceLocation.StationId;
                        //for (Entry<Integer, VehicleACS> entry : vehicleCost.entrySet())
                        //{
                        //    VehicleEx nominatedVehicle = entry.getValue();
                        //    this.ExtensionManager.CreateSuitableVehicleHistory(bayId, transportCmd.Id, stationId, transportCmd.Source, foundVehicle.getId(), nominatedVehicle.getId(), nominatedVehicle.getCurrentNodeId(), entry.getKey().toString());
                        //}
                    }
                    catch (Exception e)
                    {
                        logger.Error(e);
                    }
                }

                return foundVehicle;
            }
            else
                return null;
        }


        public VehicleEx SearchSuitableVehicleDijkstraCache(TransportCommandEx transportCmd, BayEx bay, string sourceNode)
        {
            string bayId = bay.Id;
            IList vehicles = ResourceManager.GetVehicles(bayId, false);

            if ((vehicles == null) || vehicles.Count < 1)
            {
                return null;
                //return SearchSuitableParkingVehicleDijkstraCache(bay, sourceNode);
            }

            if ((!string.IsNullOrEmpty(bay.AgvType)) && string.Equals(bay.AgvType, BayExs.AGVTYPE_RGV))
            {
                if (vehicles.Count > 0)
                {
                    return (VehicleEx)vehicles[0];
                }
            }

            Dictionary<int, VehicleEx> vehicleCost = new Dictionary<int, VehicleEx>();

            IList linkViews = this.CacheManager.GetLinkViewByBayId(bay.Id);

            if (linkViews != null)
            {
                //IList => LinkViewList
                Dictionary<string, IList> linkViewMap = this.ConvertLinkViewsToMapByFromNode(linkViews);

                foreach (var itemvehicle in vehicles)
                {
                    VehicleEx vehicle = (VehicleEx)itemvehicle;

                    //PathACS vehiclePath = this.searchDynamicPathsDijkstraV2(vehicle.getCurrentNodeId(), sourceNode);
                    PathEx vehiclePath = this.SearchDynamicPathsDijkstra(vehicle.CurrentNodeId, sourceNode, linkViewMap, true, false);
                    if (vehiclePath != null)
                    {
                        String path = this.ListToString(vehiclePath.NodeIds);
                        vehicle.Path = string.Format(path + ":" + (int)(vehiclePath.Cost * 60 / 2500));

                        if (!vehicleCost.ContainsKey(vehiclePath.Cost))
                            vehicleCost.Add(vehiclePath.Cost, vehicle);
                    }
                }
                VehicleEx foundVehicle = null;
                if (!(vehicleCost.Count == 0))
                {
                    foreach (var entry in vehicleCost)
                    {
                        foundVehicle = entry.Value;
                        break;
                    }
                }
                else
                {
                    //20200612 LYS  Send QueueJob to everything Vehicle
                    //foundVehicle = SearchSuitableParkingVehicleDijkstraCache(bay, sourceNode, linkMap); ;
                    foundVehicle = null;
                }

                if (foundVehicle != null)
                {
                    logger.Info("Find Vehicle : " + foundVehicle);
                    try
                    {
                        //LocationACS sourceLocation = this.pathManager.getLocationByPortId(transportCommand.getSource());
                        LocationEx sourceLocation = this.CacheManager.GetLocationByStationId(sourceNode);
                        String stationId = sourceLocation.StationId;
                        //for (Entry<Integer, VehicleACS> entry : vehicleCost.entrySet())
                        //{
                        //    VehicleEx nominatedVehicle = entry.getValue();
                        //    this.ExtensionManager.CreateSuitableVehicleHistory(bayId, transportCmd.Id, stationId, transportCmd.Source, foundVehicle.getId(), nominatedVehicle.getId(), nominatedVehicle.getCurrentNodeId(), entry.getKey().toString());
                        //}
                    }
                    catch (Exception e)
                    {
                        logger.Error(e);
                    }
                }

                return foundVehicle;
            }
            else
                return null;
        }

        public VehicleEx SearchSuitableVehicleDijkstraCache(TransportCommandEx transportCmd, BayEx bay, string sourceNode, Dictionary<string, IList> linkViewMap)
        {
            string bayId = bay.Id;
            IList vehicles = ResourceManager.GetVehicles(bayId, false);

            //if ((vehicles == null) || vehicles.Count < 1)
            //{
            //    return SearchSuitableParkingVehicleDijkstraCache(bay, sourceNode);
            //}

            if ((!string.IsNullOrEmpty(bay.AgvType)) && string.Equals(bay.AgvType, BayExs.AGVTYPE_RGV))
            {
                return (VehicleEx)vehicles[0];
            }

            Dictionary<int, VehicleEx> vehicleCost = new Dictionary<int, VehicleEx>();

            foreach (var itemvehicle in vehicles)
            {
                VehicleEx vehicle = (VehicleEx)itemvehicle;

                //List linkViews = this.cacheManager.getLinkViewByBayId(bay.getId());
                //Map linkMap = this.convertLinkViewsToMapByFromNode(linkViews);

                //PathACS vehiclePath = this.searchDynamicPathsDijkstraV2(vehicle.getCurrentNodeId(), sourceNode);
                PathEx vehiclePath = this.SearchDynamicPathsDijkstra(vehicle.CurrentNodeId, sourceNode, linkViewMap, true, false);
                if (vehiclePath != null)
                {
                    vehicle.Path = string.Format(vehiclePath.NodeIds.ToArray().ToString() + ":" + (int)(vehiclePath.Cost * 60 / 2500));

                    vehicleCost.Add(vehiclePath.Cost, vehicle);
                }
            }
            VehicleEx foundVehicle = null;
            if (!(vehicleCost.Count == 0))
            {
                foreach (var entry in vehicleCost)
                {
                    foundVehicle = entry.Value;
                    break;
                }
            }
            else
            {
                foundVehicle = SearchSuitableParkingVehicleDijkstraCache(bay, sourceNode); ;
            }

            if (foundVehicle != null)
            {
                logger.Info("Find Vehicle : " + foundVehicle);
                try
                {
                    //LocationACS sourceLocation = this.pathManager.getLocationByPortId(transportCommand.getSource());
                    LocationEx sourceLocation = this.CacheManager.GetLocationByStationId(sourceNode);
                    String stationId = sourceLocation.StationId;
                    //for (Entry<Integer, VehicleACS> entry : vehicleCost.entrySet())
                    //{
                    //    VehicleEx nominatedVehicle = entry.getValue();
                    //    this.ExtensionManager.CreateSuitableVehicleHistory(bayId, transportCmd.Id, stationId, transportCmd.Source, foundVehicle.getId(), nominatedVehicle.getId(), nominatedVehicle.getCurrentNodeId(), entry.getKey().toString());
                    //}
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            }

            return foundVehicle;
        }

        public VehicleEx SearchSuitableParkingVehicleDijkstraCache(BayEx bay, string sourceNode)
        {
            string bayId = bay.Id;
            IList vehicles = this.GetParkingVehicles(bayId, false);

            if ((vehicles == null) || vehicles.Count < 1)
            {
                return null;
            }

            if ((!string.IsNullOrEmpty(bay.AgvType)) && string.Equals(bay.AgvType, BayExs.AGVTYPE_RGV))
            {
                return (VehicleEx)vehicles[0];
            }

            Dictionary<int, VehicleEx> vehicleCost = new Dictionary<int, VehicleEx>();

            foreach (var item in vehicles)
            {
                VehicleEx vehicle = (VehicleEx)item;
                //			PathACS vehiclePath = this.searchDynamicPathsDijkstraV2(vehicle.getCurrentNodeId(), sourceNode);
                PathEx vehiclePath = this.SearchDynamicPathsDijkstra(vehicle.CurrentNodeId, sourceNode);
                if (vehiclePath != null)
                {
                    vehicle.Path = string.Format(vehiclePath.NodeIds.ToArray().ToString() + ":" + (int)(vehiclePath.Cost * 60 / 2500));
                    vehicleCost.Add(vehiclePath.Cost, vehicle);
                }
            }
            VehicleEx foundVehicle = null;
            if (!(vehicleCost.Count == 0))
            {
                foreach (var entry in vehicleCost)
                {
                    foundVehicle = entry.Value;
                    break;
                }
            }
            return foundVehicle;
        }

        public VehicleEx SearchSuitableParkingVehicleDijkstraCache(BayEx bay, string sourceNode, Dictionary<string, IList> linkViewMap)
        {
            string bayId = bay.Id;
            IList vehicles = this.GetParkingVehicles(bayId, false);

            if ((vehicles == null) || vehicles.Count < 1)
            {
                return null;
            }
             
            if ((!string.IsNullOrEmpty(bay.AgvType)) && string.Equals(bay.AgvType, BayExs.AGVTYPE_RGV))
            {
                return (VehicleEx)vehicles[0];
            }

            Dictionary<int, VehicleEx> vehicleCost = new Dictionary<int, VehicleEx>();

            foreach (var item in vehicles)
            {
                VehicleEx vehicle = (VehicleEx)item;
                //			PathACS vehiclePath = this.searchDynamicPathsDijkstraV2(vehicle.getCurrentNodeId(), sourceNode);
                PathEx vehiclePath = this.SearchDynamicPathsDijkstra(vehicle.CurrentNodeId, sourceNode, linkViewMap, true, false);
                if (vehiclePath != null)
                {
                    vehicle.Path = string.Format(vehiclePath.NodeIds.ToArray().ToString() + ":" + (int)(vehiclePath.Cost * 60 / 2500));
                    vehicleCost.Add(vehiclePath.Cost, vehicle);
                }
            }
            VehicleEx foundVehicle = null;
            if (!(vehicleCost.Count == 0))
            {
                foreach (var entry in vehicleCost)
                {
                    foundVehicle = entry.Value;
                    break;
                }
            }
            return foundVehicle;
        }

        public PathEx SearchDynamicPathsDijkstraEasy(string startNode, string destNode)
        {
            int minimumCost = maxFindLength;

            PriorityQueue queue = new PriorityQueue(2, new PathExs());
        
            Dictionary<string, PathEx> visitNodeMap = new Dictionary<string, PathEx>();
            PathEx path = new PathEx();

            path.Id = startNode;
            path.NodeIds.Add(startNode);
            visitNodeMap.Add(startNode, path);

            if (startNode.Equals(destNode))
            {
                return path;
            }

            queue.Offer(path);
            while (!queue.IsEmpty)
            {
                //maxHoping--;
                PathEx nextPath = (PathEx)queue.Peek();
                queue.Poll();
                String nodeId = nextPath.Id;

                IList nextNodes = this.GetNextLinks(this.CacheManager.ConvertLinksToMapByFromNode(), nodeId);

                foreach (var nextnode in nextNodes)
                {
                    LinkEx link = (LinkEx)nextnode;
                    string nextNodeId = link.ToNodeId;

                    int cost = nextPath.Cost + link.Length + link.Load;

                    if (!nextNodeId.Equals(destNode))
                    {
                        NodeEx nextNode = this.CacheManager.GetNode(nextNodeId);
                        LocationEx loc = this.CacheManager.GetLocationByStationId(nextNodeId);
                        if (nextNode != null)
                        {
                            if (nextNode.Type.Equals("S_WAIT_P") || nextNode.Type.Equals("A_WAIT_P"))
                            {
                                cost += this.waitPointCost;
                                if (this.ResourceManager.IsHaveVehicleGoToDestNode(nextNode.Id))
                                {
                                    continue;
                                }
                            }
                            else if (nextNode.Type.Equals("WAIT_P"))
                            {
                                nextPath.SingleNodeLoad = path.SingleNodeLoad + 1;
                            }
                            cost += this.waitPointCost;
                        }
                        if (loc != null && loc.Type.Equals("CHARGE"))
                        {
                            cost += this.waitPointCost;
                            if (this.ResourceManager.IsHaveVehicleGoToDestNode(loc.StationId))
                            {
                                continue;
                            }
                        }
                    }
                    if (cost < minimumCost)
                    {
                        if (nextNodeId.Equals(destNode))
                        {
                            minimumCost = cost;
                        }

                        PathEx existPath = null;

                        if (visitNodeMap.ContainsKey(nextNodeId))
                        {
                            existPath = visitNodeMap[nextNodeId];
                        }

                        if (existPath != null)
                        {
                            if (existPath.Cost > cost)
                            {
                                existPath.Cost = cost;
                                existPath.NodeIds.Clear();
                                existPath.NodeIds.AddRange(nextPath.NodeIds);
                                existPath.NodeIds.Add(nextNodeId);
                                queue.Offer(existPath);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            PathEx newPath = new PathEx();
                            newPath.Id = link.ToNodeId;
                            newPath.NodeIds.AddRange(nextPath.NodeIds);
                            newPath.NodeIds.Add(nextNodeId);
                            newPath.Cost = cost;
                            visitNodeMap.Add(link.ToNodeId, newPath);
                            queue.Offer(newPath);
                        }
                    }
                }
            }
            PathEx result = null;

            if (visitNodeMap.ContainsKey(destNode))
            {
                result = (PathEx)visitNodeMap[destNode];
            }
            return result;
        }
    
        public PathEx SearchDynamicPathsDijkstra(string startNode, string destNode)
        {
            long startSearching = DateTime.Now.Millisecond;
            int minimumCost = 100000;
            //int maxHoping = 500;

            PathExs compare = new PathExs();
            PriorityQueue queue = new PriorityQueue(2, compare);          
            //{
            //int compare(PathEx arg0, PathEx arg1)
            //{
            //    if (arg0.Cost > arg1.Cost)
            //    {
            //        return 1;
            //    }
            //    else
            //        return -1;
            //}
            //}

            Dictionary<string, PathEx> visitNodeMap = new Dictionary<string, PathEx>();
            PathEx path = new PathEx();

            path.Id = startNode;
            path.NodeIds.Add(startNode);
            visitNodeMap.Add(startNode, path);      //������������

            if (startNode.Equals(destNode))
            {
                return path;
            }

            Dictionary<string, List<VehicleEx>> vehicleMap = this.ResourceManager.MapVehicleByCurrentNode();

            queue.Offer(path);
            while (!queue.IsEmpty)
            {
                //maxHoping--;
                PathEx nextPath = (PathEx)queue.Peek();
                queue.Poll();
                String nodeId = nextPath.Id;

                IList nextNodes = this.GetNextLinks(CacheManager.ConvertLinksToMapByFromNode(), nodeId);

                foreach (var nextnode in nextNodes)
                {
                    LinkEx link = (LinkEx)nextnode;
                    String nextNodeId = link.ToNodeId;

                    int cost = nextPath.Cost + link.Length + link.Load;

                    NodeEx nextNode = this.CacheManager.GetNode(nextNodeId);
                    LocationEx loc = this.CacheManager.GetLocationByStationId(nextNodeId);
                    if (nextNode != null && nextNode.Type.Contains("WAIT"))
                    {
                        cost += 500;
                    }

                    if (loc != null && string.Equals(loc.Type, "CHARGE"))
                    {
                        cost += 500;
                    }

                    //Calculate turn link - Not checked
                    string lastNodeId = (string)nextPath.NodeIds[nextPath.NodeIds.Count - 1];
                    NodeEx lastNode = this.CacheManager.GetNode(lastNodeId);
                    NodeEx currentNode = this.CacheManager.GetNode(nodeId);
                    if (lastNode != null && currentNode != null && nextNode != null)
                    {
                        if (lastNode.Xpos == currentNode.Xpos && currentNode.Xpos == nextNode.Xpos || lastNode.Ypos == currentNode.Ypos && currentNode.Ypos == nextNode.Ypos)
                        {
                            //��i th���ng
                        }
                        else
                        { //�������ng r��?
                            cost += 1000;
                        }
                    }

                    //--------------------------------------------------		
                    if (cost < minimumCost)
                    {
                        if (string.Equals(nextNodeId, destNode))
                        {
                            minimumCost = cost;
                        }

                        PathEx existPath = null;

                        if (visitNodeMap.ContainsKey(nextNodeId))
                        {
                            existPath = visitNodeMap[nextNodeId];
                        }

                        if (existPath != null)
                        {
                            if (existPath.Cost > cost)
                            {
                                existPath.Cost = cost;
                                existPath.NodeIds.Clear();
                                existPath.NodeIds.AddRange(nextPath.NodeIds);
                                existPath.NodeIds.Add(nextNodeId);
                                queue.Offer(existPath);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            //if(visitNodeMap.Count == 11)
                            //{
                            //    cost = 1;
                            //}
                            PathEx newPath = new PathEx();
                            newPath.Id = link.ToNodeId;
                            newPath.NodeIds.AddRange(nextPath.NodeIds);
                            newPath.NodeIds.Add(nextNodeId);
                            newPath.Cost = cost;
                            visitNodeMap.Add(link.ToNodeId, newPath);
                            queue.Offer(newPath);
                        }
                    }
                }
            }//end of while
            long elpased = DateTime.Now.Millisecond - startSearching;
            //		for (Map.Entry<String, PathACS> entry : visitNodeMap.entrySet()) {
            //			logger.warn(entry.getKey() + ".Path: " + entry.getValue());
            //		}
            PathEx result = null;

            if (visitNodeMap.ContainsKey(destNode))
            {
                result = (PathEx)visitNodeMap[destNode];
            }

            if (result != null)
            {
                logger.Info("Search Route from " + startNode + " to " + destNode + " : " + elpased + "ms. Path: " + result.NodeIds + ". Cost: " + result.Cost);
            }
            else
            {
                logger.Info("Search Route from " + startNode + " to " + destNode + " : " + elpased + "ms. No path found!");
            }
            return result;
        }

        // 221212 copy
        public PathEx SearchDynamicPathsDijkstra(string startNode, string destNode, string agvType)
        {
            long startSearching = DateTime.Now.Millisecond;
            int minimumCost = 100000;

            PathExs compare = new PathExs();
            PriorityQueue queue = new PriorityQueue(2, compare);
            Dictionary<string, PathEx> visitNodeMap = new Dictionary<string, PathEx>();
            PathEx path = new PathEx();

            path.Id = startNode;
            path.NodeIds.Add(startNode);
            visitNodeMap.Add(startNode, path);
            
            if (startNode.Equals(destNode))
            {
                return path;
            }
            Dictionary<string, List<VehicleEx>> vehicleMap = this.ResourceManager.MapVehicleByCurrentNode();
            queue.Offer(path);
            while (!queue.IsEmpty)
            {
                //maxHoping--;
                PathEx nextPath = (PathEx)queue.Peek();
                queue.Poll();
                String nodeId = nextPath.Id;

                IList nextNodes = this.GetNextLinks(this.CacheManager.ConvertLinksToMapByFromNode(), nodeId);

                foreach (var nextnode in nextNodes)
                {
                    LinkEx link = (LinkEx)nextnode;
                    string nextNodeId = link.ToNodeId;

                    if (!link.AgvType.Equals(VehicleEx.VENDOR_COMMON) && !agvType.Contains(link.AgvType))
                    {
                        continue;
                    }

                    int cost = nextPath.Cost + link.Length + link.Load;

                    NodeEx nextNode = this.CacheManager.GetNode(nextNodeId);
                    LocationEx loc = this.CacheManager.GetLocationByStationId(nextNodeId);
                    if (nextNodeId != null && nextNode.Type.Contains("WAIT"))
                    {
                        cost += 500;
                    }
                    if (loc != null && string.Equals(loc.Type, "CHARGE"))
                    {
                        cost += 500;
                    }

                    string lastNodeId = (string)nextPath.NodeIds[nextPath.NodeIds.Count - 1];
                    NodeEx lastNode = this.CacheManager.GetNode(lastNodeId);
                    NodeEx currentNode = this.CacheManager.GetNode(nodeId);
                    if (lastNode != null && currentNode != null && nextNode != null)
                    {
                        if (lastNode.Xpos == currentNode.Xpos && currentNode.Xpos == nextNode.Xpos || lastNode.Ypos == currentNode.Ypos && currentNode.Ypos == nextNode.Ypos)
                        {

                        }
                        else
                        {
                            cost += 1000;
                        }
                    }
                    if (cost < minimumCost)
                    {
                        if (string.Equals(nextNodeId, destNode))
                        {
                            minimumCost = cost;
                        }
                        PathEx existPath = null;
                        
                        if (visitNodeMap.ContainsKey(nextNodeId))
                        {
                            existPath = visitNodeMap[nextNodeId];
                        }
                        
                        
                        if (existPath != null)
                        {
                            if (existPath.Cost > cost)
                            {
                                existPath.Cost = cost;
                                existPath.NodeIds.Clear();
                                existPath.NodeIds.AddRange(nextPath.NodeIds);
                                existPath.NodeIds.Add(nextNodeId);
                                queue.Offer(existPath);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            PathEx newPath = new PathEx();
                            newPath.Id = link.ToNodeId;
                            newPath.NodeIds.AddRange(nextPath.NodeIds);
                            newPath.NodeIds.Add(nextNodeId);
                            newPath.Cost = cost;
                            visitNodeMap.Add(link.ToNodeId, newPath);
                            queue.Offer(newPath);
                        }
                    }
                }
            }

            long elapsed = DateTime.Now.Millisecond - startSearching;
            PathEx result = null;

            if (visitNodeMap.ContainsKey(destNode))
            {
                result = (PathEx)visitNodeMap[destNode];
            }
            if (result != null)
            {
                logger.Info("Search Route from " + startNode + " to " + destNode + " : " + elapsed + "ms. Path: " + result.NodeIds + ". Cost: " + result.Cost + ". Vehicle cost: " + result.SingleNodeLoad);
            }
            else
            {
                logger.Info("Search Route from " + startNode + " to " + destNode + " : " + elapsed + "ms. No path found!");
            }
            return result;
        }


        public PathEx SearchDynamicPathsDijkstra(string startNode, string destNode, bool needTurnCost, bool needTraffic)
        {
            long startSearching = DateTime.Now.Millisecond;
            int minimumCost = maxFindLength;
            PriorityQueue queue = new PriorityQueue(2, new PathExs());

            Dictionary<string, PathEx> visitNodeMap = new Dictionary<string, PathEx>();
            PathEx path = new PathEx();

            path.Id = startNode;
            path.NodeIds.Add(startNode);
            visitNodeMap.Add(startNode, path);
            if (startNode.Equals(destNode))
            {
                return path;
            }
            Dictionary<string, List<VehicleEx>> vehicleMap = this.ResourceManager.MapVehicleByCurrentNode();
            queue.Offer(path);
            while (!queue.IsEmpty)
            {
                //maxHoping--;
                PathEx nextPath = (PathEx)queue.Peek();
                queue.Poll();
                String nodeId = nextPath.Id;

                IList nextNodes = this.GetNextLinks(this.CacheManager.ConvertLinksToMapByFromNode(), nodeId);

                foreach (var nextnode in nextNodes)
                {
                    LinkEx link = (LinkEx)nextnode;
                    string nextNodeId = link.ToNodeId;
                    int cost = nextPath.Cost + link.Length + link.Load;
                    int vehicleCost = nextPath.SingleNodeLoad;

                    if (!nextNodeId.Equals(destNode))
                    {
                        NodeEx nextNode = this.CacheManager.GetNode(nextNodeId);
                        LocationEx loc = this.CacheManager.GetLocationByStationId(nextNodeId);
                        if (nextNode != null)
                        {
                            if (nextNode.Type.Equals("S_WAIT_P") || nextNode.Type.Equals("A_WAIT_P"))
                            {
                                cost += this.waitPointCost;
                                if (this.ResourceManager.IsHaveVehicleGoToDestNode(nextNode.Id))
                                {
                                    continue;
                                }
                            }
                            else if (nextNode.Type.Equals("WAIT_P"))
                            {
                                cost += this.waitPointCost;
                            }
                        }
                        if (loc != null && loc.Type.Equals("CHARGE"))
                        {
                            continue;
                        }
                        // TurnCost 미반영시 ↓ 하단 죽이면됨
                        if (needTurnCost)
                        {
                            if (nextPath.NodeIds.Count > 2)
                            {
                                string lastNodeId = (string)nextPath.NodeIds[nextPath.NodeIds.Count - 2];
                                NodeEx lastNode = this.CacheManager.GetNode(lastNodeId);
                                NodeEx currentNode = this.CacheManager.GetNode(nodeId);
                                if (lastNode != null && currentNode != null && nextNode != null)
                                {
                                    if (lastNode.Xpos == currentNode.Xpos && currentNode.Xpos == nextNode.Xpos || lastNode.Ypos == currentNode.Ypos && currentNode.Ypos == nextNode.Ypos)
                                    {

                                    }
                                    else
                                    {
                                        int turnAngle = this.CalculateAngle3Point(lastNode, currentNode, nextNode);
                                        cost += (int)(turnAngle * this.turnCostRate);
                                    }
                                }
                            }
                        }

                        // Traffic Control 미반영시 ↓ 하단 죽이면됨
                        if (needTraffic)
                        {
                            List<VehicleEx> vehiclesInNode = null;
                            if (vehicleMap.ContainsKey(nextNodeId))
                            {
                                vehiclesInNode = vehicleMap[nextNodeId];
                            }
                            //List<VehicleEx> vehiclesInNode = vehicleMap[nextNodeId];
                            if (vehiclesInNode != null)
                            {
                                foreach (VehicleEx vehicle in vehiclesInNode)
                                {
                                    if (vehicle.AlarmState.Equals(VehicleEx.ALARMSTATE_ALARM) || vehicle.RunState.Equals(VehicleEx.RUNSTATE_STOP))
                                    {
                                        vehicleCost += this.agvStopCost;
                                    }
                                    else
                                    {
                                        vehicleCost += this.agvRunCost;
                                    }
                                }
                            }
                        }
                    }
                    if (cost + vehicleCost < minimumCost)
                    {
                        if (nextNodeId.Equals(destNode))
                        {
                            minimumCost = cost + vehicleCost;
                        }
                        PathEx existPath = null;
                        if (visitNodeMap.ContainsKey(nextNodeId))
                        {                            
                            existPath = visitNodeMap[nextNodeId];
                        }
                        if (existPath != null)
                        {
                            if (existPath.Cost + existPath.SingleNodeLoad > cost + vehicleCost)
                            {
                                existPath.Cost = cost;
                                existPath.SingleNodeLoad = vehicleCost;
                                existPath.NodeIds.Clear();
                                existPath.NodeIds.AddRange(nextPath.NodeIds);
                                existPath.NodeIds.Add(nextNodeId);
                                queue.Offer(existPath);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            PathEx newPath = new PathEx();
                            newPath.Id = link.ToNodeId;
                            newPath.NodeIds.AddRange(nextPath.NodeIds);
                            newPath.NodeIds.Add(nextNodeId);
                            newPath.Cost = cost;
                            newPath.SingleNodeLoad = vehicleCost;
                            visitNodeMap.Add(link.ToNodeId, newPath);
                            queue.Offer(newPath);
                        }
                    }
                }
            }
            long elapsed = DateTime.Now.Millisecond - startSearching;
            PathEx result = null;

            if (visitNodeMap.ContainsKey(destNode))
            {
                result = (PathEx)visitNodeMap[destNode];
            }
            if (result != null)
            {
                logger.Info("Search Route from " + startNode + " to " + destNode + " : " + elapsed + "ms. Path: " + result.NodeIds + ". Cost: " + result.Cost + ". Vehicle cost: " + result.SingleNodeLoad);
            }
            else
            {
                logger.Info("Search Route from " + startNode + " to " + destNode + " : " + elapsed + "ms. No path found!");
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startNode"></param>
        /// <param name="destNode"></param>
        /// <param name="linksMap">Dictionary Value => LinkViewEx Type </param>
        /// <param name="needTurnCost"></param>
        /// <param name="needTraffic"></param>
        /// <returns></returns>
        public PathEx SearchDynamicPathsDijkstra(string startNode, string destNode, Dictionary<string, IList> linksMap, bool needTurnCost, bool needTraffic)
        {
            //linksMap IList => LinkViewList
            if (linksMap != null)
            {
                long startSearching = DateTime.Now.Millisecond;
                int minimumCost = 100000;

                PriorityQueue queue = new PriorityQueue(2, new PathExs() as IComparer);

                Dictionary<string, PathEx> visitNodeMap = new Dictionary<string, PathEx>();
                PathEx path = new PathEx();

                Dictionary<string, List<VehicleEx>> vehicleMap = this.ResourceManager.MapVehicleByCurrentNode();

                path.Id = startNode;
                path.NodeIds.Add(startNode);
                visitNodeMap.Add(startNode, path);

                if (startNode.Equals(destNode))
                {
                    return path;
                }

                queue.Offer(path);

                try
                {
                    while (!queue.IsEmpty)
                    {
                        //maxHoping--;
                        PathEx nextPath = (PathEx)queue.Peek();
                        queue.Poll();
                        String nodeId = nextPath.Id;

                        IList nextNodes = this.GetNextLinks(linksMap, nodeId);
                        //logger.info(nextNodes);
                        foreach (var nextnode in nextNodes)
                        {
                            LinkViewEx link = (LinkViewEx)nextnode;
                            String nextNodeId = link.ToNodeId;

                            //if (link.FromNodeId.Equals(link.ToNodeId)) continue;

                            int cost = nextPath.Cost + link.Length + link.Load;
                            int vehicleCost = nextPath.SingleNodeLoad;

                            if (!nextNodeId.Equals(destNode))
                            {
                                NodeEx nextNode = this.CacheManager.GetNode(nextNodeId);
                                LocationEx loc = this.CacheManager.GetLocationByStationId(nextNodeId);
                                if (nextNode != null)
                                {
                                    if (nextNode.Type.Equals("S_WAIT_P") || nextNode.Type.Equals("A_WAIT_P"))
                                    {
                                        cost += this.waitPointCost;
                                        if (this.ResourceManager.IsHaveVehicleGoToDestNode(nextNode.Id))
                                        {
                                            continue;
                                        }
                                    }
                                    else if (nextNode.Type.Equals("WAIT_P"))
                                    {
                                        cost += this.waitPointCost;
                                    }
                                }
                                // ttttttttttttt
                                //if (loc != null && loc.Type.Equals("CHARGE"))
                                //{
                                //    continue;
                                //}
                            }

                            if (cost + vehicleCost < minimumCost)
                            {
                                if (string.Equals(nextNodeId, destNode))
                                {
                                    minimumCost = cost + vehicleCost;
                                }

                                //PathEx existPath = null;
                                //if (visitNodeMap.ContainsKey(nextNodeId))
                                //{
                                //    existPath = visitNodeMap[nextNodeId];
                                //}

                                PathEx existPath = null;
                                visitNodeMap.TryGetValue(nextNodeId, out existPath);

                                if (existPath != null)
                                {
                                    if (existPath.Cost + existPath.SingleNodeLoad > cost + vehicleCost)
                                    {

                                        existPath.Cost = cost;
                                        existPath.SingleNodeLoad = vehicleCost;
                                        existPath.NodeIds.Clear();
                                        existPath.NodeIds.AddRange(nextPath.NodeIds);
                                        existPath.NodeIds.Add(nextNodeId);
                                        queue.Offer(existPath);
                                        // logger.info("ExistPath: " + existPath);
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                else
                                {
                                    PathEx newPath = new PathEx();
                                    newPath.Id = link.ToNodeId;
                                    newPath.NodeIds.AddRange(nextPath.NodeIds);
                                    newPath.NodeIds.Add(nextNodeId);
                                    newPath.Cost = cost;
                                    newPath.SingleNodeLoad = vehicleCost;
                                    visitNodeMap.Add(link.ToNodeId, newPath);
                                    queue.Offer(newPath);
                                    // logger.info("newPath: " + newPath);
                                }
                            }
                        }
                    }//end of while
                }
                catch(Exception e)
                {

                }
                long elpased = DateTime.Now.Millisecond - startSearching;
                //        for(Map.Entry<String, PathACS> entry : visitNodeMap.entrySet()){
                //        	logger.info(entry.getKey() + ".Path: " + entry.getValue());
                //        }

                //if (visitNodeMap.ContainsKey(destNode)) return null;
                //else
                //{
                    PathEx result;
                    visitNodeMap.TryGetValue(destNode, out result);

                    if (result != null)
                    {
                        logger.Info("Search Route from " + startNode + " to " + destNode + " : " + elpased + "ms. Cost: " + result.Cost + ". Path: " + result.NodeIds);
                    }
                    else
                    {
                        logger.Info("Search Route from " + startNode + " to " + destNode + " : " + elpased + "ms. No path found!");
                    }
                    return result;
               // }
            }
            else
                return this.SearchDynamicPathsDijkstra(startNode, destNode, needTurnCost, needTraffic);
        }        

        //public new VehicleEx SearchSuitableChargeVehicle(String bayId, LocationEx destLocation)
        //{

        //    StationEx destStation = this.CacheManager.GetStationById(destLocation.StationId);
        //    NodeEx destNode = this.CacheManager.GetNodeByStation(destStation);

        //    if (destNode != null)
        //    {

        //        try
        //        {

        //            List<LinkViewEx> linkViews = (List<LinkViewEx>)this.GetLinkViewEx(bayId);
        //            Dictionary<string, IList> linksMap = this.ConvertLinkViewsToMapByToNode(linkViews);
        //            //
        //            //				List linkViews = this.getLinkACS(true);
        //            //				Map linksMap = this.convertLinksToMapByToNode(linkViews);

        //            IList nominateVehicles = ResourceManager.GetVehiclesForCharge(bayId, false);
        //            if ((nominateVehicles == null) || nominateVehicles.Count < 1)
        //            {

        //                logger.Warn("can not find available charge vehicles, bayId{" + bayId + "}");
        //                return null;
        //            }


        //            Dictionary<float, IList> vehicleVoltageMap = new Dictionary<float, IList>();
        //            foreach (VehicleEx vehicle in nominateVehicles)
        //            {
        //                if (vehicleVoltageMap.ContainsKey(vehicle.BatteryVoltage))
        //                {

        //                    IList sameNodeVehicles = vehicleVoltageMap[vehicle.BatteryVoltage];
        //                    sameNodeVehicles.Add(vehicle);
        //                }
        //                else
        //                {

        //                    IList sameNodeVehicles = new List<VehicleEx>();
        //                    sameNodeVehicles.Add(vehicle);
        //                    vehicleVoltageMap.Add(vehicle.BatteryVoltage, sameNodeVehicles);
        //                }
        //            }


        //            foreach (IList listVehicles in vehicleVoltageMap.Values)
        //            {
        //                Dictionary<string, IList> vehicleMap = this.ConvertVehiclesToMap(listVehicles);

        //                List<string> pathed = new List<string>();
        //                pathed.Add(destNode.Id);

        //                List<List<string>> paths = new List<List<string>>();
        //                paths.Add(pathed);

        //                Dictionary<List<String>, int> pathMap = new Dictionary<List<string>, int>();
        //                pathMap.Add(pathed, 0);

        //                Dictionary<string, IList> checkedPaths = new Dictionary<string, IList>();
        //                checkedPaths.Add(destNode.Id, paths);

        //                Dictionary<int, VehicleEx> vehicleCost = new Dictionary<int, VehicleEx>();
        //                vehicleCost = this.SearchSuitableVehicleDijkstra(vehicleCost, pathMap, linksMap, vehicleMap, checkedPaths);
        //                VehicleEx foundVehicle = null;

        //                if (vehicleCost != null && vehicleCost.Count > 0)
        //                {
        //                    foreach (KeyValuePair<int, VehicleEx> entry in vehicleCost)
        //                    {
        //                        foundVehicle = entry.Value;
        //                        break;
        //                    }
        //                }
        //                if (foundVehicle != null)
        //                {
        //                    logger.Info("Find Suitable Charge Vehicle : " + foundVehicle);
        //                    return foundVehicle;
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            logger.Error("failed to Suitable Charge Vehicle ", e);
        //        }

        //    }
        //    else
        //    {
        //        logger.Warn("can not find Source : " + destStation);
        //    }
        //    logger.Warn("can not find Suitable Charge Vehicle, from " + bayId);

        //    return null;
        //}

        public Dictionary<int, VehicleEx> SearchSuitableVehicleDijkstra(Dictionary<int, VehicleEx> vehicleCost, Dictionary<List<String>, int> paths, IDictionary linkMap, IDictionary vehicleMap, Dictionary<string, IList> checkedPaths)
        {

            Dictionary<List<String>, int> newPaths = new Dictionary<List<String>, int>();

            foreach (List<String> path in paths.Keys)
            {
                IList links = (IList)linkMap[path[path.Count - 1]];
                if (links != null)
                {

                    Dictionary<string, VehicleEx> checkedVehicles = new Dictionary<string, VehicleEx>();

                    foreach (LinkViewEx link in links)
                    {
                        String nextNodeId = link.FromNodeId;

                        IList vehicles = (IList)vehicleMap[nextNodeId];
                        if (vehicles != null)
                        {

                            VehicleEx vehicle = (VehicleEx)vehicles[0];

                            if (checkedVehicles.ContainsKey(vehicle.Id))
                            {
                                continue;
                            }
                            else
                            {
                                checkedVehicles.Add(vehicle.Id, vehicle);
                            }

                            int totalCost = paths[path] + link.Length + link.Load;
                            List<string> foundPath = new List<string>();
                            foundPath.AddRange(path);
                            foundPath.Add(vehicle.CurrentNodeId);
                            logger.Info(vehicle.Id + ", cost[" + totalCost + "], " + foundPath);
                            int totalTime = totalCost * 60 / 2500;

                            foundPath.Reverse();

                            vehicle.Path = ListToString(foundPath) + ":" + totalTime;
                            vehicleCost.Add(totalCost, vehicle);

                            continue;
                        }

                        int currentCost = (paths.ContainsKey(path)) ? paths[path] : 0;
                        int length = link.Length;
                        if (checkedPaths.ContainsKey(nextNodeId))
                        {
                            IList oldPath = checkedPaths[nextNodeId];
                            if (oldPath.Count > path.Count)
                            {
                                List<string> newPath = new List<string>();
                                if (path.Count > 0)
                                {
                                    newPath.AddRange(path);
                                }
                                newPath.Add(nextNodeId);
                                checkedPaths.Add(nextNodeId, path);
                                newPaths.Add(newPath, currentCost + length + link.Load);
                            }
                        }
                        else
                        {
                            List<string> newPath = new List<string>();
                            if (path.Count > 0)
                            {
                                newPath.AddRange(path);
                            }
                            newPath.Add(nextNodeId);
                            checkedPaths.Add(nextNodeId, path);
                            newPaths.Add(newPath, currentCost + length + link.Load);
                        }
                    }
                }
                else
                {
                    logger.Warn("links does not exist in repository, link{" + path[path.Count - 1] + "}");
                }
            }
            if (newPaths.Count > 0)
            {
                return SearchSuitableVehicleDijkstra(vehicleCost, newPaths, linkMap, vehicleMap, checkedPaths);
            }
            else
            {
                return vehicleCost;
            }
        }

        public Dictionary<int, VehicleEx> SearchSuitableVehicleDijkstraLink(Dictionary<int, VehicleEx> vehicleCost, Dictionary<List<String>, int> paths, IDictionary linkMap, IDictionary vehicleMap, Dictionary<string, IList> checkedPaths)
        {
            Dictionary<List<String>, int> newPaths = new Dictionary<List<String>, int>();

            foreach (List<string> path in paths.Keys)
            {
                List<LinkEx> links = (List<LinkEx>)linkMap[path[path.Count - 1]];
                if (links != null)
                {

                    Dictionary<string, VehicleEx> checkedVehicles = new Dictionary<string, VehicleEx>();

                    foreach (LinkEx link in links)
                    {
                        String nextNodeId = link.FromNodeId;

                        List<VehicleEx> vehicles = (List<VehicleEx>)vehicleMap[nextNodeId];
                        if (vehicles != null)
                        {

                            VehicleEx vehicle = (VehicleEx)vehicles[0];

                            if (checkedVehicles.ContainsKey(vehicle.Id))
                            {
                                continue;
                            }
                            else
                            {
                                checkedVehicles.Add(vehicle.Id, vehicle);
                            }

                            int totalCost = paths[path] + link.Length + link.Load;
                            List<string> foundPath = new List<string>();
                            foundPath.AddRange(path);
                            foundPath.Add(vehicle.CurrentNodeId);
                            logger.Info(vehicle.Id + ", cost[" + totalCost + "], " + foundPath);
                            int totalTime = totalCost * 60 / 2500;
                            foundPath.Reverse();



                            vehicle.Path = ListToString(foundPath) + ":" + totalTime;
                            vehicleCost.Add(totalCost, vehicle);

                            continue;
                        }

                        int currentCost = (paths.ContainsKey(path)) ? paths[path] : 0;
                        int length = link.Length;
                        if (checkedPaths.ContainsKey(nextNodeId))
                        {
                            List<string> oldPath = (List<string>)checkedPaths[nextNodeId];
                            if (oldPath.Count > path.Count)
                            {
                                List<string> newPath = new List<string>();
                                if (path.Count > 0)
                                {
                                    newPath.AddRange(path);
                                }
                                newPath.Add(nextNodeId);
                                checkedPaths.Add(nextNodeId, path);
                                newPaths.Add(newPath, currentCost + length + link.Load);
                            }
                        }
                        else
                        {
                            List<string> newPath = new List<string>();
                            if (path.Count > 0)
                            {
                                newPath.AddRange(path);
                            }
                            newPath.Add(nextNodeId);
                            checkedPaths.Add(nextNodeId, path);
                            newPaths.Add(newPath, currentCost + length + link.Load);
                        }
                    }
                }
                else
                {
                    logger.Info("links does not exist in repository, link{" + path[path.Count - 1] + "}");
                }
            }
            if (newPaths.Count > 0)
            {
                return SearchSuitableVehicleDijkstraLink(vehicleCost, newPaths, linkMap, vehicleMap, checkedPaths);
            }
            else
            {
                return vehicleCost;
            }

        }

        public VehicleEx SearchSuitableVehicle(StationEx sourceStation, String bayId, bool isContainDeadNode, bool isContainChargeAGV)
        {
            VehicleEx suitableVehicle = null;

            NodeEx sourceNode = this.CacheManager.GetNodeByStation(sourceStation);
            if (sourceNode != null)
            {

                try
                {
                    List<LinkViewEx> linkViews = (List<LinkViewEx>)this.GetLinkViews(bayId);
                    Dictionary<string, IList> linksMap = this.ConvertLinkViewsToMapByToNode(linkViews);

                    //				List linkViews = this.getLinkACS(isContainDeadNode);
                    //				Map linksMap = this.convertLinksToMapByToNode(linkViews);

                    List<VehicleEx> vehicles = (List<VehicleEx>)this.GetVehicles(bayId, isContainDeadNode);
                    if (isContainChargeAGV)
                    {
                        vehicles.AddRange(this.GetChargeVehicles(bayId, isContainDeadNode));
                    }

                    if ((vehicles == null) || vehicles.Count < 1)
                    {
                        return SearchSuitableParkingVehicle(sourceStation, bayId, isContainDeadNode, isContainChargeAGV);
                    }

                    for (IEnumerator iterator = vehicles.GetEnumerator(); iterator.MoveNext();)
                    {
                        VehicleEx vehicle = (VehicleEx)iterator.Current;
                        logger.Info("Logging Target(" + sourceNode.Id + ") Vehicle : " + vehicle.ToString());
                    }

                    BayEx bay = this.ResourceManager.GetBay(bayId);

                    if (!String.IsNullOrEmpty(bay.AgvType) && bay.AgvType.Equals(BayExs.AGVTYPE_RGV))
                    {

                        VehicleEx vehicle = vehicles[0];
                        logger.Info("Find Vehicle : " + vehicle.ToString());
                        return vehicle;
                    }

                    Dictionary<string, IList> vehicleMap = this.ConvertVehiclesToMap(vehicles);

                    List<string> pathed = new List<string>();
                    pathed.Add(sourceNode.Id);

                    List<List<string>> paths = new List<List<string>>();
                    paths.Add(pathed);

                    Dictionary<List<String>, int> pathMap = new Dictionary<List<string>, int>();
                    pathMap.Add(pathed, 0);

                    Dictionary<string, IList> checkedPaths = new Dictionary<string, IList>();
                    checkedPaths.Add(sourceNode.Id, paths);

                    Dictionary<int, VehicleEx> vehicleCost = new Dictionary<int, VehicleEx>();
                    long startTime = System.DateTime.Now.Ticks;
                    vehicleCost = this.SearchSuitableVehicleDijkstra(vehicleCost, pathMap, linksMap, vehicleMap, checkedPaths);
                    logger.Info("Search SuitableVehicle Elapsedtime = " + (System.DateTime.Now.Ticks - startTime).ToString() + "ms");
                    VehicleEx foundVehicle = null;
                    if (vehicleCost.Count > 0)
                    {
                        foreach (KeyValuePair<int, VehicleEx> entry in vehicleCost)
                        {
                            foundVehicle = entry.Value;
                            break;
                        }
                    }
                    if (foundVehicle != null)
                    {
                        logger.Info("Find Vehicle : " + foundVehicle.ToString());
                        return foundVehicle;
                    }
                    else
                        return SearchSuitableParkingVehicle(sourceStation, bayId, isContainDeadNode, isContainChargeAGV);
                }
                catch (Exception e)
                {
                    logger.Error("failed to Suitable Vehicle ", e);
                }

            }
            else
            {
                logger.Warn("can not find Source : " + sourceStation);
            }
            logger.Warn("can not find Node By Source Station, " + sourceStation.ToString());

            return suitableVehicle;
        }

        public VehicleEx SearchSuitableVehicle(IList linkViews, IDictionary linksMap, StationEx sourceStation, String bayId, bool isContainDeadNode, bool isContainChargeAGV, TransportCommandEx transportCommand)
        {
            VehicleEx suitableVehicle = null;

            //NodeACS sourceNode = this.pathManager.searchNodeByStationAsDest(sourceStation);
            NodeEx sourceNode = this.CacheManager.GetNodeByStation(sourceStation);
            if (sourceNode != null)
            {

                try
                {
                    List<VehicleEx> vehicles = (List<VehicleEx>)this.GetVehicles(bayId, isContainDeadNode);
                    if (isContainChargeAGV)
                    {
                        vehicles.AddRange(this.GetChargeVehicles(bayId, isContainDeadNode));
                    }
                    if ((vehicles == null) || vehicles.Count < 1)
                    {

                        // 2019.07.31 dv.kien check parking AGV if can not find
                        // available agv
                        logger.Warn("can not fined available running vehicles, bayId{" + bayId + "}. Checking parking AGV");
                        return SearchSuitableParkingVehicle(linkViews, linksMap, sourceStation, bayId, isContainDeadNode, isContainChargeAGV);
                    }

                    foreach (VehicleEx vehicle in vehicles)
                    {
                        logger.Info("Logging Target(" + sourceNode.Id + ") Candidate-Vehicle : " + vehicle.ToString());
                    }

                    BayEx bay = this.ResourceManager.GetBay(bayId);
                    if (!String.IsNullOrEmpty(bay.AgvType) && bay.AgvType.Equals(BayExs.AGVTYPE_RGV))
                    {

                        VehicleEx vehicle = (VehicleEx)vehicles[0];
                        logger.Info("Find Vehicle : " + vehicle.ToString());
                        return vehicle;
                    }

                    IDictionary vehicleMap = this.ConvertVehiclesToMap(vehicles);

                    List<string> pathed = new List<string>();
                    pathed.Add(sourceNode.Id);

                    List<List<string>> paths = new List<List<string>>();
                    paths.Add(pathed);

                    Dictionary<List<String>, int> pathMap = new Dictionary<List<string>, int>();
                    pathMap.Add(pathed, 0);

                    Dictionary<string, IList> checkedPaths = new Dictionary<string, IList>();
                    checkedPaths.Add(sourceNode.Id, paths);

                    Dictionary<int, VehicleEx> vehicleCost = new Dictionary<int, VehicleEx>();
                    long startTime = System.DateTime.Now.Ticks;
                    vehicleCost = this.SearchSuitableVehicleDijkstraLink(vehicleCost, pathMap, linksMap, vehicleMap, checkedPaths);
                    logger.Info("Search SuitableVehicle Elapsedtime = " + (System.DateTime.Now.Ticks - startTime).ToString() + "ms");
                    VehicleEx foundVehicle = null;
                    if (vehicleCost.Count > 0)
                    {
                        foreach (KeyValuePair<int, VehicleEx> entry in vehicleCost)
                        {
                            foundVehicle = entry.Value;
                            break;
                        }
                    }
                    else
                    {
                        foundVehicle = SearchSuitableParkingVehicle(linkViews, linksMap, sourceStation, bayId, isContainDeadNode, isContainChargeAGV);
                    }

                    // dv.kien 2018.12.07 add Suitable Vehicle History in here,
                    // include total cost of nominate vehicle
                    if (foundVehicle != null)
                    {
                        logger.Info("Find Vehicle : " + foundVehicle);
                        try
                        {
                            LocationEx sourceLocation = this.GetLocationByPortId(transportCommand.Source);
                            String stationId = sourceLocation.StationId;
                            foreach (KeyValuePair<int, VehicleEx> entry in vehicleCost)
                            {
                                VehicleEx nominatedVehicle = entry.Value;
                                CreateSuitableVehicleHistory(bayId, transportCommand.Id, stationId, transportCommand.Source, foundVehicle.Id, nominatedVehicle.Id, nominatedVehicle.CurrentNodeId, entry.Key.ToString());
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Error(e);
                        }
                    }

                    return foundVehicle;
                }
                catch (Exception e)
                {
                    logger.Error("failed to Suitable Vehicle ", e);
                }

            }
            else
            {
                logger.Warn("can not find Source : " + sourceStation);
            }
            logger.Warn("can not find Node By Source Station, " + sourceStation.ToString());
            return suitableVehicle;
        }

        public VehicleEx SearchSuitableParkingVehicle(StationEx sourceStation, String bayId, bool isContainDeadNode, bool isContainChargeAGV)
        {
            VehicleEx suitableVehicle = null;

            //NodeACS sourceNode = this.pathManager.searchNodeByStationAsDest(sourceStation);
            NodeEx sourceNode = this.CacheManager.GetNodeByStation(sourceStation);
            if (sourceNode != null)
            {

                try
                {
                    IList linkViews = this.GetLinkViews(bayId);
                    IDictionary linksMap = this.ConvertLinkViewsToMapByToNode(linkViews);
                    //				List linkViews = this.getLinkACS(isContainDeadNode);
                    //				Map linksMap = this.convertLinksToMapByToNode(linkViews);

                    List<VehicleEx> vehicles = (List<VehicleEx>)this.GetParkingVehicles(bayId, isContainDeadNode);
                    if (isContainChargeAGV)
                    {
                        vehicles.AddRange(this.GetChargeVehicles(bayId, isContainDeadNode));
                    }
                    if ((vehicles == null) || vehicles.Count < 1)
                    {
                        logger.Warn("can not fined available vehicles, bayId{" + bayId + "}");
                        return null;
                    }
                    foreach (VehicleEx vehicle in vehicles)
                    {
                        logger.Info("Logging Target(" + sourceNode.Id + ") Parking Vehicle : " + vehicle.ToString());
                    }

                    IDictionary vehicleMap = this.ConvertVehiclesToMap(vehicles);

                    List<string> pathed = new List<string>();
                    pathed.Add(sourceNode.Id);

                    List<List<string>> paths = new List<List<string>>();
                    paths.Add(pathed);

                    Dictionary<List<String>, int> pathMap = new Dictionary<List<string>, int>();
                    pathMap.Add(pathed, 0);

                    Dictionary<string, IList> checkedPaths = new Dictionary<string, IList>();
                    checkedPaths.Add(sourceNode.Id, paths);

                    Dictionary<int, VehicleEx> vehicleCost = new Dictionary<int, VehicleEx>();
                    long startTime = System.DateTime.Now.Ticks;
                    vehicleCost = this.SearchSuitableVehicleDijkstra(vehicleCost, pathMap, linksMap, vehicleMap, checkedPaths);
                    logger.Info("Search Parking SuitableVehicle Elapsedtime = " + (System.DateTime.Now.Ticks - startTime).ToString() + "ms");
                    VehicleEx foundVehicle = null;
                    if (vehicleCost.Count > 0)
                    {
                        foreach (KeyValuePair<int, VehicleEx> entry in vehicleCost)
                        {
                            foundVehicle = entry.Value;
                            break;
                        }
                    }
                    if (foundVehicle != null)
                    {
                        logger.Info("Find Vehicle : " + foundVehicle);
                    }

                    return foundVehicle;
                }
                catch (Exception e)
                {
                    logger.Error("failed to Suitable Parking Vehicle ", e);
                }

            }
            else
            {
                logger.Warn("can not find Source : " + sourceStation);
            }
            logger.Warn("can not find Node By Source Station, " + sourceStation.ToString());
            return suitableVehicle;
        }

        public VehicleEx SearchSuitableParkingVehicle(IList linkViews, IDictionary linksMap, StationEx sourceStation, String bayId, bool isContainDeadNode, bool isContainChargeAGV)
        {
            VehicleEx suitableVehicle = null;

            //NodeACS sourceNode = this.pathManager.searchNodeByStationAsDest(sourceStation);
            NodeEx sourceNode = this.CacheManager.GetNodeByStation(sourceStation);
            if (sourceNode != null)
            {

                try
                {

                    List<VehicleEx> vehicles = (List<VehicleEx>)this.GetParkingVehicles(bayId, isContainDeadNode);
                    if (isContainChargeAGV)
                    {
                        vehicles.AddRange(this.GetChargeVehicles(bayId, isContainDeadNode));
                    }
                    if ((vehicles == null) || vehicles.Count < 1)
                    {

                        logger.Warn("can not fined available vehicles, bayId{" + bayId + "}");
                        return null;
                    }
                    foreach (VehicleEx vehicle in vehicles)
                    {
                        logger.Info("Logging Target(" + sourceNode.Id + ") Parking Vehicle : " + vehicle.ToString());
                    }

                    Dictionary<string, IList> vehicleMap = this.ConvertVehiclesToMap(vehicles);

                    List<string> pathed = new List<string>();
                    pathed.Add(sourceNode.Id);

                    List<List<string>> paths = new List<List<string>>();
                    paths.Add(pathed);

                    Dictionary<List<String>, int> pathMap = new Dictionary<List<string>, int>();
                    pathMap.Add(pathed, 0);

                    Dictionary<string, IList> checkedPaths = new Dictionary<string, IList>();
                    checkedPaths.Add(sourceNode.Id, paths);

                    Dictionary<int, VehicleEx> vehicleCost = new Dictionary<int, VehicleEx>();
                    long startTime = System.DateTime.Now.Ticks;
                    vehicleCost = this.SearchSuitableVehicleDijkstraLink(vehicleCost, pathMap, linksMap, vehicleMap, checkedPaths);
                    logger.Info("Search Parking SuitableVehicle Elapsedtime = " + (System.DateTime.Now.Ticks - startTime).ToString() + "ms");
                    VehicleEx foundVehicle = null;
                    if (vehicleCost.Count > 0)
                    {
                        foreach (KeyValuePair<int, VehicleEx> entry in vehicleCost)
                        {
                            foundVehicle = entry.Value;
                            break;
                        }
                    }
                    if (foundVehicle != null)
                    {
                        logger.Info("Find Vehicle : " + foundVehicle);
                    }

                    return foundVehicle;
                }
                catch (Exception e)
                {
                    logger.Error("failed to Suitable Parking Vehicle ", e);
                }

            }
            else
            {
                logger.Warn("can not find Source : " + sourceStation);
            }
            logger.Warn("can not find Node By Source Station, " + sourceStation.ToString());
            return suitableVehicle;
        }

        public VehicleEx SearchSuitableVehicle(StationEx sourceStation, String bayId, bool isContainDeadNode, bool isContainChargeAGV, TransportCommandEx transportCommand)
        {
            VehicleEx suitableVehicle = null;

            NodeEx sourceNode = this.CacheManager.GetNodeByStation(sourceStation);
            if (sourceNode != null)
            {

                try
                {
                    IList linkViews = this.CacheManager.GetLinkViewByBayId(bayId);
                    IDictionary linksMap = this.ConvertLinkViewsToMapByToNode(linkViews);

                    List<VehicleEx> vehicles = (List<VehicleEx>)this.GetVehicles(bayId, isContainDeadNode);
                    if (isContainChargeAGV)
                    {
                        vehicles.AddRange(this.GetChargeVehicles(bayId, isContainDeadNode));
                    }
                    if ((vehicles == null) || vehicles.Count < 1)
                    {

                        // check parking AGV if can not find
                        // available agv
                        logger.Warn("can not fined available running vehicles, bayId{" + bayId + "}. Checking parking AGV");
                        return SearchSuitableParkingVehicle(sourceStation, bayId, isContainDeadNode, isContainChargeAGV);
                    }

                    foreach (VehicleEx vehicle in vehicles)
                    {
                        logger.Info("Logging Target(" + sourceNode.Id + ") Candidate-Vehicle : " + vehicle.ToString());
                    }

                    BayEx bay = this.ResourceManager.GetBay(bayId);
                    if (!String.IsNullOrEmpty(bay.AgvType) && bay.AgvType.Equals(BayExs.AGVTYPE_RGV))
                    {

                        VehicleEx vehicle = (VehicleEx)vehicles[0];
                        logger.Info("Find Vehicle : " + vehicle);
                        return vehicle;
                    }

                    IDictionary vehicleMap = this.ConvertVehiclesToMap(vehicles);

                    List<string> pathed = new List<string>();
                    pathed.Add(sourceNode.Id);

                    List<List<string>> paths = new List<List<string>>();
                    paths.Add(pathed);

                    Dictionary<List<String>, int> pathMap = new Dictionary<List<string>, int>();
                    pathMap.Add(pathed, 0);

                    Dictionary<string, IList> checkedPaths = new Dictionary<string, IList>();
                    checkedPaths.Add(sourceNode.Id, paths);

                    Dictionary<int, VehicleEx> vehicleCost = new Dictionary<int, VehicleEx>();
                    long startTime = System.DateTime.Now.Ticks;
                    // vehicleCost = this.searchSuitableVehicleDijkstra(vehicleCost,
                    // pathMap, linksMap, vehicleMap, checkedPaths);
                    vehicleCost = this.SearchSuitableVehicleDijkstra(vehicleCost, pathMap, linksMap, vehicleMap, checkedPaths);
                    logger.Info("Search SuitableVehicle Elapsedtime = " + (System.DateTime.Now.Ticks - startTime).ToString() + "ms");
                    VehicleEx foundVehicle = null;
                    if (vehicleCost.Count > 0)
                    {
                        foreach (KeyValuePair<int, VehicleEx> entry in vehicleCost)
                        {
                            foundVehicle = entry.Value;
                            break;
                        }
                    }
                    else
                    {
                        // check parking AGV if available agv can
                        // not find path to source
                        foundVehicle = SearchSuitableParkingVehicle(sourceStation, bayId, isContainDeadNode, isContainChargeAGV);
                    }

                    // add Suitable Vehicle History in here,
                    // include total cost of nominate vehicle
                    if (foundVehicle != null)
                    {
                        logger.Info("Find Vehicle : " + foundVehicle);
                        try
                        {
                            LocationEx sourceLocation = this.CacheManager.GetLocationByPortId(transportCommand.Source);
                            String stationId = sourceLocation.StationId;
                            foreach (KeyValuePair<int, VehicleEx> entry in vehicleCost)
                            {
                                VehicleEx nominatedVehicle = entry.Value;
                                CreateSuitableVehicleHistory(bayId, transportCommand.Id, stationId, transportCommand.Source, foundVehicle.Id, nominatedVehicle.Id, nominatedVehicle.CurrentNodeId, entry.Key.ToString());
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Error(e);
                        }
                    }

                    return foundVehicle;
                }
                catch (Exception e)
                {
                    logger.Error("failed to Suitable Vehicle ", e);
                }

            }
            else
            {
                logger.Warn("can not find Source : " + sourceStation);
            }
            logger.Warn("can not find Node By Source Station, " + sourceStation.ToString());
            return suitableVehicle;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="vehicleId"></param>
        /// <param name="node">vehicle current node</param>
        /// <param name="path">vehicle expected path</param>
        /// <param name="pastPath">vehicle past path history</param>
        /// <returns></returns>
        public string SearchNodeInPathAndReCalculate(string vehicleId, string node, string path, string pastPath)
        {
            try
            {
                bool findNode = false;
                int totalTIme = 0;
                string newPath = "";
                string[] pathArr = path.Split(',');
                IList missingNode = new ArrayList();

                string tempOldPath = "";
                if (string.IsNullOrEmpty(pastPath))
                {
                    // if past path is null or empty, path[0] is missing node
                    tempOldPath = pathArr[0];
                }

                // search that currentnode exist in pathArr and if currentnode exist in pathArr, From path[0] to currentNode are missing node.
                for (int i = 1; i < pathArr.Length; i++)
                {
                    if (node.Equals(pathArr[i]))
                    {
                        if (tempOldPath.Equals(""))
                        {
                            tempOldPath = pathArr[i];
                        }
                        else
                        {
                            tempOldPath += "," + pathArr[i];
                        }
                        findNode = true;
                    }

                    if (findNode)
                    {
                        newPath += pathArr[i] + ",";
                        if (i + 1 < pathArr.Length)
                        {
                            LinkEx link = this.CacheManager.GetLinkById(pathArr[i] + "_" + pathArr[i + 1]);
                            if (link != null)
                            {
                                totalTIme += link.Length * 60 / 2500;
                            }
                        }
                    }
                    else
                    {
                        if (pathArr[i].Length == 4)
                        {
                            NodeEx checkCom = this.CacheManager.GetNode(pathArr[i]);
                            if (checkCom != null && !checkCom.Type.Equals(VehicleOrderEx.TYPE_ORDER))
                            {
                                missingNode.Add(pathArr[i]);
                            }
                        }
                        if (tempOldPath.Equals(""))
                        {
                            tempOldPath = pathArr[i];
                        }
                        else
                        {
                            tempOldPath += "," + pathArr[i];
                        }
                    }
                }


                if (findNode)
                {
                    if (missingNode.Count > 0)
                    {
                        StringBuilder missingNodeString = new StringBuilder();

                        foreach (string nodeStr in missingNode)
                        {
                            missingNodeString.Append(nodeStr + ", ");

                        }

                        oCodelogger.Error("AGV report fly location: (" + vehicleId + ") missing node: " + missingNodeString.ToString().TrimEnd(','));
                    }
                    newPath += "-" + pastPath + "," + tempOldPath + ":" + totalTIme;
                    return newPath.Replace(",-", "-").Replace("-,", "-");
                }
                else
                {
                    if (!pastPath.Equals(""))
                    {
                        VehicleSearchPathHistory vhs = new VehicleSearchPathHistory();
                        vhs.VehicleId = vehicleId;
                        vhs.Path = pastPath;
                        vhs.CurrentNodeId = pathArr[0];
                        vhs.Time = DateTime.Now;
                        this.HistoryManager.CreateVehicleSearchPathHistory(vhs);
                    }
                }
                return null;
            }
            catch(NullReferenceException ex)
            {               
                oCodelogger.Error(ex.ToString() + " -:- " + ex.StackTrace);
                return null;
            }
        }

        public PathEx SearchDynamicPathsDijkstraDivide(String startNode, String destNode, Dictionary<String, IList> linksMap)
        {
            //2020.02.12 dv.kien test using divide 3 path:
            //0. Distance but not using turn cost
            //1. Distance with turn cost
            //2. Distance with turn cost and traffic check
            Random r = new Random();
            int i = r.Next(3);

            if (i == 0)
            {
                return SearchDynamicPathsDijkstra(startNode, destNode, linksMap, false, false);
            }
            else if (i == 2)
            {
                return SearchDynamicPathsDijkstra(startNode, destNode, linksMap, true, false);
            }
            else
            {
                return SearchDynamicPathsDijkstra(startNode, destNode, linksMap, true, true);
            }
        }

        public PathEx SearchDynamicPathsDijkstraDivide(String startNode, String destNode)
        {
            //2020.02.12 dv.kien test using divide 3 path:
            //0. Distance but not using turn cost
            //1. Distance with turn cost
            //2. Distance with turn cost and traffic check
            Random r = new Random();
            int i = r.Next(3);

            if (i == 0)
            {
                return SearchDynamicPathsDijkstra(startNode, destNode, false, false);
            }
            else if (i == 2)
            {
                return SearchDynamicPathsDijkstra(startNode, destNode, true, false);
            }
            else
            {
                return SearchDynamicPathsDijkstra(startNode, destNode, true, true);
            }
        }

        public Dictionary<int, List<string>> SearchPathFromVehicleToDest(Dictionary<List<string>, int> paths, IDictionary linkMap, string endNode, Dictionary<string, IList> checkedPaths, int maxloop)
        {
            string returnPath = "";
            maxloop++;
            Dictionary<List<String>, int> newPaths = new Dictionary<List<String>, int>();

            foreach (List<string> path in paths.Keys)
            {
                //logger.warn("Checked path: " + path + ". Total cost: " + paths.get(path));
                IList links = (IList)linkMap[path[path.Count - 1]];
                if (links != null)
                {
                    foreach (LinkEx link in links)
                    {
                        int extendCost = 0;
                        string nextNodeId = link.FromNodeId;

                        if (nextNodeId.Equals(endNode))
                        {

                            int totalCost = paths[path] + link.Length + link.Load + extendCost;
                            List<string> foundPath = new List<string>();
                            foundPath.AddRange(path);
                            foundPath.Add(endNode);

                            foundPath.Reverse();

                            Dictionary<int, List<string>> returnMap = new Dictionary<int, List<string>>();
                            returnMap.Add(totalCost, foundPath);

                            return returnMap;
                        }

                        //2019.10.02 dv.kien added: Not check path have wait point, or charge port
                        //NodeACS nextNode = this.pathManager.getNode(nextNodeId);
                        NodeEx nextNode = this.CacheManager.GetNode(nextNodeId);
                        if (nextNode.Type.Equals(NodeEx.TYPE_WAIT_P) || nextNode.Type.Equals("S_WAIT_P") || nextNode.Type.Equals("A_WAIT_P"))
                        {
                            continue;
                        }
                        //LocationACS loc = this.resourceManager.getLocationByStationId(nextNodeId);
                        LocationEx loc = this.CacheManager.GetLocationByStationId(nextNodeId);
                        if (loc != null && loc.Type.Equals(LocationEx.TYPE_CHARGE))
                        {
                            continue;
                        }

                        int currentCost = (paths[path] != null) ? paths[path] : 0;
                        int length = link.Length;
                        if (checkedPaths.ContainsKey(nextNodeId))
                        {
                            List<string> oldPath = (List<string>)checkedPaths[nextNodeId];
                            if (oldPath.Count > path.Count)
                            {
                                List<string> newPath = new List<string>();
                                if (path.Count > 0)
                                {
                                    newPath.AddRange(path);
                                }
                                newPath.Add(nextNodeId);
                                checkedPaths.Add(nextNodeId, path);
                                newPaths.Add(newPath, currentCost + length + link.Load + extendCost);
                            }
                        }
                        else
                        {
                            List<string> newPath = new List<string>();
                            if (path.Count > 0)
                            {
                                newPath.AddRange(path);
                            }
                            newPath.Add(nextNodeId);
                            checkedPaths.Add(nextNodeId, path);
                            newPaths.Add(newPath, currentCost + length + link.Load + extendCost);
                        }
                    }
                }
                else
                {
                    logger.Warn("links does not exist in repository, link{" + path[path.Count - 1] + "}");
                }
            }
            if (newPaths.Count > 0)
            {
                return SearchPathFromVehicleToDest(newPaths, linkMap, endNode, checkedPaths, maxloop);
            }
            else
            {
                return null;
            }
        }

        public IList GetParkingVehicles(string bayId, bool containUnavailableVehicle)
        {
            //logger.warn("No have Idle AGV. Try to find other PARKING agv!");

            float chargeVoltage = ResourceManager.GetBayLimitVoltage(bayId);

            DetachedCriteria criteria = DetachedCriteria.For(typeof(VehicleEx));
            if (!containUnavailableVehicle)
            {
                criteria = DetachedCriteria.For(typeof(VehicleEx));
                criteria.Add(Restrictions.Eq("ConnectionState", VehicleEx.CONNECTIONSTATE_CONNECT));
                criteria.Add(Restrictions.Eq("State", VehicleEx.STATE_ALIVE));
                criteria.Add(Restrictions.Eq("ProcessingState", VehicleEx.PROCESSINGSTATE_PARK));
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

        public IList GetAvailableRGVVehicles(String bayId, bool containUnavailableVehicle)
        {
            DetachedCriteria criteria = DetachedCriteria.For(typeof(VehicleEx));


            if (!containUnavailableVehicle)
            {
                criteria.Add(Restrictions.Eq("connectionState", VehicleEx.CONNECTIONSTATE_CONNECT));
                criteria.Add(Restrictions.Eq("processingState", VehicleEx.PROCESSINGSTATE_IDLE));
                criteria.Add(Restrictions.Eq("state", VehicleEx.STATE_ALIVE));
                criteria.Add(Restrictions.Gt("batteryVoltage", VehicleExs.AVAIALBE_VOLTAGE_RGV));
                criteria.Add(Restrictions.Eq("bayId", bayId));
                criteria.Add(Restrictions.Eq("fullState", VehicleEx.FULLSTATE_EMPTY));
                criteria.Add(Restrictions.Eq("installed", VehicleEx.INSTALL_INSTALLED));
                criteria.Add(Restrictions.NotEqProperty("vendor", BayExs.AGVTYPE_FIXED_MODE));
                criteria.AddOrder(Order.Asc("nodeCheckTime"));
            }


            List<VehicleEx> vehicleList = (List<VehicleEx>)this.PersistentDao.FindByCriteria(criteria);
            List<VehicleEx> returnList = new List<VehicleEx>();

            foreach (VehicleEx vehicle in vehicleList)
            {
                TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicle.Id);
                if (transportCommand != null)
                {
                    logger.Warn("vehicle{" + vehicle.Id + "} has a transportCommand{" + transportCommand.Id + "}. it will be not designated.");
                    continue;
                }
                String vehicleId = vehicle.Id;
                bool flag = true;
                List<AlarmEx> alarms = (List<AlarmEx>)this.AlarmManager.GetAlarmsByVehicleId(vehicleId);

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

                if (flag)
                {
                    returnList.Add(vehicle);
                }
            }

            return returnList;
        }

        public IList GetLinksByToNodeId(String toNodeId)
        {
            DetachedCriteria criteria = DetachedCriteria.For(typeof(LinkEx));
            criteria.Add(Restrictions.Eq("ToNodeId", toNodeId));
            return this.PersistentDao.FindByCriteria(criteria);
        }

        public IList GetLinksByFromNodeId(String fromNodeId)
        {
            DetachedCriteria criteria = DetachedCriteria.For(typeof(LinkEx));
            criteria.Add(Restrictions.Eq("FromNodeId", fromNodeId));
            return this.PersistentDao.FindByCriteria(criteria);
        }

        public List<LinkViewEx> GetLinkViewEx(String bayId)
        {
            if (mapLinkViewExCount.ContainsKey(bayId))
            {
                int Retvalue;

                mapLinkViewExCount.TryGetValue(bayId, out Retvalue);

                if ( Retvalue >= MAX_TIMES_USED_LINK)
                {
                    mapLinkViewExCount.Add(bayId, 1);
                    List<LinkViewEx> linkView = this.GetLinkViewByBayId(bayId);
                    mapLinkViewEx.Add(bayId, linkView);
                    logger.Info("Reload linkViews of bay: " + bayId);
                }
                else
                {
                    int totalCount = Retvalue;
                    totalCount++;
                    mapLinkViewExCount.Add(bayId, totalCount);
                    logger.Info("Used linkViews of bay: " + bayId + ". Total used: " + totalCount);
                }
            }
            else
            {
                mapLinkViewExCount.Add(bayId, 1);
                List<LinkViewEx> linkView = this.GetLinkViewByBayId(bayId);
                mapLinkViewEx.Add(bayId, linkView);
                logger.Info("Start add linkViews of bay: " + bayId);
            }

            List<LinkViewEx> ret = null; 
            mapLinkViewEx.TryGetValue(bayId, out ret); 
            return ret; 
        }

        public List<LinkViewEx> GetLinkViewByBayId(String bayID)
        {
            DetachedCriteria crit = DetachedCriteria.For(typeof(LinkViewEx));
		    crit.Add(Restrictions.Eq("bayId", bayID));
		    List<LinkViewEx> values = (List < LinkViewEx >) this.PersistentDao.FindByCriteria(crit);
		    if (values.Count > 0)
			    return values;
		    return null;
	    }

        public String ListToString(List<String> path)
        {
            String newPath = "";

            for (IEnumerator iterator = path.GetEnumerator(); iterator.MoveNext();)
            {
                String str = (String)iterator.Current;
                newPath += str;
                newPath += ",";             
            }

            newPath = newPath.TrimEnd(',');

            return newPath;
        }

        public int ToDegress(double d)
        {           
            return (int)(d * 180 / Math.PI);
        }

        public void UpdateTransportCommandPathMap(TransportCommandEx transportCommand)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();

            attributes.Add("path", transportCommand.Path);
            int result = this.TransferManager.UpdateTransportCommand(transportCommand, attributes);

            if (result > 0)
            {
                logger.Debug("transportCommand{" + transportCommand.Id + "}.path was changed to {" + transportCommand.Path + "}" + transportCommand);
            }
        }

        public VehicleEx SearchSuitableChargeGroupVehicleDijkstraCache(string[] bayGroupCharge, LocationEx destLocation)
        {
            StationEx destStation = this.CacheManager.GetStationById(destLocation.StationId);
            NodeEx destnode = this.CacheManager.GetNodeByStation(destStation);

            if (destnode != null)
            {
                try
                {
                    IList nominateVehicles = new ArrayList();
                    for (int i = 0; i < bayGroupCharge.Length; i++)
                    {
                        string singleBay = (String)bayGroupCharge[i];

                        LocationViewEx loc = this.CacheManager.GetLocationViewChargeByStationIdAndBayId(destnode.Id, singleBay);
                        if (loc != null)
                        {
                            logger.Info("Find vehicle for location" + destnode + " in bay " + singleBay);
                            IList chargeVehicles = this.ResourceManager.GetVehiclesForCharge(singleBay, false);
                            if (chargeVehicles != null && chargeVehicles.Count > 0)
                            {
                                for (IEnumerator iterator = chargeVehicles.GetEnumerator(); iterator.MoveNext();)
                                {
                                    VehicleEx vehicle = (VehicleEx)iterator.Current;
                                    nominateVehicles.Add(vehicle);
                                }
                            }
                        }
                    }
                    if ((nominateVehicles == null) || nominateVehicles.Count < 1)
                    {
                        logger.Warn("Can not find available charge vehicles, Group {" + bayGroupCharge.ToString() + "}");
                        return null;
                    }
                    for (IEnumerator iterator = nominateVehicles.GetEnumerator(); iterator.MoveNext();)
                    {
                        VehicleEx vehicle = (VehicleEx)iterator.Current;
                        logger.Info("Find vehicle to charge [" + vehicle.Id + "]. Bay [" + vehicle.BayId + "]. Charge point: " + destLocation);
                    }
                    // Check lowest Voltage AGV
                    Dictionary<string, IList> vehicleVoltageMap = new Dictionary<string, IList>();
                    for (IEnumerator iterator = nominateVehicles.GetEnumerator(); iterator.MoveNext();)
                    {
                        VehicleEx vehicle = (VehicleEx)iterator.Current;

                        if (vehicleVoltageMap.ContainsKey(vehicle.BatteryVoltage.ToString()))
                        {
                            IList samenodeVehicles = new List<VehicleEx>();
                            vehicleVoltageMap.TryGetValue(vehicle.BatteryVoltage.ToString(), out samenodeVehicles);
                            samenodeVehicles.Add(vehicle);
                        }
                        else
                        {
                            IList samenodeVehicles = new ArrayList();
                            samenodeVehicles.Add(vehicle);
                            vehicleVoltageMap.Add(vehicle.BatteryVoltage.ToString(), samenodeVehicles);
                        }
                    }

                    IDictionary treemap = new Hashtable(vehicleVoltageMap);

                    for (IEnumerator iterator = treemap.GetEnumerator(); iterator.MoveNext();)
                    {
                        IList listVehicles = (IList)iterator.Current;

                        SortedList<int, VehicleEx> vehicleCost = new SortedList<int, VehicleEx>();

                        for (IEnumerator iterator2 = listVehicles.GetEnumerator(); iterator2.MoveNext();)
                        {
                            VehicleEx vehicle2 = (VehicleEx)iterator2.Current;
                            PathEx vehiclePath = this.SearchDynamicPathsDijkstra(vehicle2.CurrentNodeId, destLocation.StationId, vehicle2.Vendor);
                            if (vehiclePath != null)
                            {
                                vehicle2.Path = string.Join(",", vehiclePath.NodeIds) + ":" + (int)(vehiclePath.Cost * 60 / 2500);
                                vehicleCost.Add(vehiclePath.Cost, vehicle2);
                            }
                            VehicleEx foundVehicle = null;
                            if (!(vehicleCost.Count == 0))
                            {
                                var vehicleCostList = vehicleCost.OrderBy(x => x.Value.BatteryVoltage);
                                foreach (var entry in vehicleCostList)
                                {
                                    foundVehicle = entry.Value;
                                    break;
                                }
                            }
                            if (foundVehicle != null)
                            {
                                logger.Info("Find Suitable Charge vehicles : " + foundVehicle);
                                return foundVehicle;
                            }
                        }
                    }


                }
                catch (Exception e)
                {
                    logger.Error("Failed to Suitable Charge vehicle ", e);
                }
            }
            else
            {
                logger.Warn("Can not find Source : " + destStation);
            }
            logger.Warn("Can not find Suitable Charge vehicle, from " + bayGroupCharge);

            return null;
        }




        public VehicleEx SearchSuitableChargeVehicleDijkstraCache(string bayId, LocationEx destLocation)
        {
            StationEx destStation = this.CacheManager.GetStationById(destLocation.StationId);
            NodeEx destnode = this.CacheManager.GetNodeByStation(destStation);

            if (destnode != null)
            {
                try
                {
                    IList nominateVehicles = this.ResourceManager.GetVehiclesForCharge(bayId, false);

                    if ((nominateVehicles == null) || nominateVehicles.Count < 1)
                    {
                        logger.Warn("Can not find available charge vehicles, bayId {" + bayId + "}" + " - false");
                        return null;
                    }

                    IDictionary vehicleVoltageMap = new Hashtable();

                    for (IEnumerator iterator = nominateVehicles.GetEnumerator(); iterator.MoveNext();)
                    {
                        VehicleEx vehicle = (VehicleEx)iterator.Current;

                        if (vehicleVoltageMap.Contains(vehicle.BatteryVoltage))
                        {
                            IList sameNodeVehicles = (IList)vehicleVoltageMap[vehicle.BatteryVoltage];
                            sameNodeVehicles.Add(vehicle);
                        }
                        else
                        {
                            IList sameNodeVehicles = new ArrayList();
                            sameNodeVehicles.Add(vehicle);
                            vehicleVoltageMap[vehicle.BatteryVoltage] = sameNodeVehicles;
                        }
                    }

                    IDictionary treemap = new Hashtable(vehicleVoltageMap);

                    for (IEnumerator iterator = treemap.Values.GetEnumerator(); iterator.MoveNext();)
                    {
                        IList listVehicles = (IList)iterator.Current;

                        SortedList<int, VehicleEx> vehicleCost = new SortedList<int, VehicleEx>();

                        for (IEnumerator iterator2 = listVehicles.GetEnumerator(); iterator2.MoveNext();)
                        {
                            VehicleEx vehicle2 = (VehicleEx)iterator2.Current;
                            logger.Info("Check vehicle " + vehicle2.Id + " to charge! ");

                            PathEx vehiclePath = this.SearchDynamicPathsDijkstra(vehicle2.CurrentNodeId, destLocation.StationId, vehicle2.Vendor);
                            if (vehiclePath != null)
                            {
                                vehicle2.Path = string.Join(",", vehiclePath.NodeIds) + ":" + (int)(vehiclePath.Cost * 60 / 2500);
                                vehicleCost[vehiclePath.Cost] = vehicle2;
                            }
                        }

                        VehicleEx foundVehicle = null;
                        if (vehicleCost.Count > 0)
                        {
                            var vehicleCostList = vehicleCost.OrderBy(x => x.Value.BatteryVoltage);
                            foreach (var entry in vehicleCostList)
                            {
                                foundVehicle = entry.Value;
                                break;
                            }
                        }
                        if (foundVehicle != null)
                        {
                            logger.Info("Find Suitable Charge vehicles : " + foundVehicle);
                            return foundVehicle;
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.Error("Failed to Suitable Charge vehicle ", e);
                }
            }
            else
            {
                logger.Warn("Can not find Source : " + destStation + " - false");
            }
            logger.Warn("Can not find Suitable Charge vehicle, from " + bayId + " - false");

            return null;
        }

    }
}
