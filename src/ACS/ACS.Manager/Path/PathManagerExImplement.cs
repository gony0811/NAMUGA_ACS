using ACS.Core.Base;
using ACS.Core.Base.Interface;
using ACS.Core.Alarm;
using ACS.Core.Alarm.Model;
using ACS.Core.Path.Model;
using ACS.Core.Resource.Model;
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;
using ACS.Core.Route.Model;
using ACS.Core.Path;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace ACS.Manager.Path
{

    public class PathManagerExImplement : AbstractManager, IPathManagerEx
    {
        //protected static final Logger logger = //logger.getLogger(PathManagerACSImpl.class);
        public int MaxHoppingCount { get; set; }
        public int MaxTotalCost { get; set; }
        public bool UseDynamicLoad { get; set; }
        public bool UseHeuristicDelay { get; set; }
        public IComparator Comparator { get; set; }
        public IComparator NodeCheckTimeComparator { get; set; }
        public IAlarmManagerEx AlarmManager { get; set; }
        public ITransferManagerEx TransferManager { get; set; }

        public PathManagerExImplement()
        {
            MaxHoppingCount = 100;
            MaxTotalCost = int.MaxValue;
            UseDynamicLoad = false;
        }

        public VehicleEx SearchSuitableVehicle(LocationEx sourceLocation)
        {
            StationEx sourceStation = GetStation(sourceLocation.StationId);
            return SearchSuitableVehicle(sourceStation, false, null, false);
        }

        public VehicleEx SearchSuitableVehicle(LocationEx sourceLocation, String bayId)
        {
            StationEx sourceStation = GetStation(sourceLocation.StationId);
            return SearchSuitableVehicle(sourceStation, bayId, false);
        }

        public VehicleEx SearchSuitableChargeVehicle(String bayId)
        {
            List<VehicleEx> listVehicle = GetChargeVehicles(bayId, false);
            if ((listVehicle != null) && (listVehicle.Count > 0))
            {
                return (VehicleEx)listVehicle[0];
            }
            return null;
        }

        public VehicleEx SearchSuitableVehicle(StationEx sourceStation, bool useCarrierProcessType, List<string> carrierProcessTypeNames, bool isContainDeadNode)
        {
            VehicleEx suitableVehicle = null;

            NodeEx sourceNode = SearchNodeByStationAsDest(sourceStation);
            if (sourceNode != null)
            {
                IList links = GetLinks(useCarrierProcessType, carrierProcessTypeNames, isContainDeadNode);
                Dictionary<string, IList> linksMap = ConvertLinksToMapByToNode(links);

                IList vehicles = GetVehicles(useCarrierProcessType, carrierProcessTypeNames, isContainDeadNode);
                foreach (var item in vehicles)
                {
                    VehicleEx obj = (VehicleEx)item;
                    //logger.info("Logging Target(" + sourceNode.getId() + ") Vehicle : " + object.toString());
                }
                Dictionary<string, IList> vehicleMap = ConvertVehiclesToMap(vehicles);

                PathEx path = new PathEx();
                path.NodeIds.Add(sourceNode.NodeId);

                IList paths = new List<PathEx>();
                paths.Add(path);

                List<string> checkedNodeIds = new List<string>();
                checkedNodeIds.Add(sourceNode.NodeId);

                suitableVehicle = SearchSuitableVehicle(paths, linksMap, vehicleMap, checkedNodeIds);
            }
            return suitableVehicle;
        }

        public VehicleEx SearchSuitableVehicle(StationEx sourceStation, String bayId, bool isContainDeadNode)
        {
            VehicleEx suitableVehicle = null;

            NodeEx sourceNode = SearchNodeByStationAsDest(sourceStation);
            if (sourceNode != null)
            {
                try
                {
                    IList linkViews = GetLinkViewsByBayId(isContainDeadNode, bayId);

                    Dictionary<string, IList> linksMap = ConvertLinkViewsToMapByToNode(linkViews);

                    IList vehicles = GetVehicles(bayId, isContainDeadNode);
                    if ((vehicles == null) || (vehicles.Count < 1))
                    {
                        logger.Warn("can not fined available vehicles, bayId{" + bayId + "}");
                        return null;
                    }
                    foreach (var item in vehicles)
                    {
                        VehicleEx obj = (VehicleEx)item;
                        logger.Info("Logging Target(" + sourceNode.NodeId + ") Vehicle : " + obj.ToString());
                    }

                    Dictionary<string, IList> vehicleMap = ConvertVehiclesToMap(vehicles);

                    List<string> pathed = new List<string>();

                    pathed.Add(sourceNode.NodeId);


                    IList paths = new List<IList>();

                    paths.Add(pathed);


                    Dictionary<string, IList> checkedPaths = new Dictionary<string, IList>();

                    checkedPaths.Add(sourceNode.NodeId, paths);


                    VehicleEx vehicle = SearchSuitableVehicleDijkstra(paths, linksMap, vehicleMap, checkedPaths);
                    logger.Info("Find Vehicle : " + vehicle.ToString());

                    return vehicle;
                }
                catch (Exception e)
                {
                    logger.Error("failed to Suitable Vehicle ", e);
                }
            }
            //logger.warn("can not find Source : " + sourceStation);

            //logger.warn("can not find Node By Source Station, " + sourceStation.toString());
            return suitableVehicle;
        }

        public VehicleEx SearchSuitableVehicle(IList paths, Dictionary<string, IList> linksMap, Dictionary<string, IList> vehiclesMap, List<string> checkedNodeIds)
        {
            VehicleEx suitableVehicle = null;
            List<string> tempNodeList = new List<string>();
            foreach (var value in paths)
            {
                PathEx path = value as PathEx;

                String nodeId = (String)path.NodeIds[0];
                if (checkedNodeIds.Count < 2)
                {
                    tempNodeList.Add(nodeId);
                }
                else
                {
                    IList sameNodeVehicles = new List<VehicleEx>();
                    vehiclesMap.TryGetValue(nodeId, out sameNodeVehicles);
                    if (sameNodeVehicles != null)
                    {
                        foreach (var value3 in sameNodeVehicles)
                        {
                            VehicleEx obj = value3 as VehicleEx;
                            logger.Info("logging This Node : " + nodeId + " In Vehicle : " + obj.ToString());
                        }
                        IEnumerator iterator2 = sameNodeVehicles.GetEnumerator();
                        if (iterator2.MoveNext())
                        {
                            VehicleEx vehicle = (VehicleEx)iterator2.Current;

                            suitableVehicle = vehicle;
                            logger.Info("Find Vehicle : " + vehicle.ToString());
                        }
                        if (suitableVehicle != null)
                        {
                            break;
                        }
                    }
                }
            }
            if (suitableVehicle != null)
            {
                return suitableVehicle;
            }
            bool isFinished = true;
            List<PathEx> newPaths = new List<PathEx>();
            foreach (var value in paths)
            {
                PathEx path = value as PathEx;

                IList sameToNodeLinks = new List<LinkEx>();
                linksMap.TryGetValue((String)path.NodeIds[0], out sameToNodeLinks);
                if (sameToNodeLinks != null)
                {
                    foreach (var value2 in sameToNodeLinks)
                    {
                        LinkEx link = value2 as LinkEx;
                        if ((!path.NodeIds.Contains(link.FromNodeId)) &&
                          (!checkedNodeIds.Contains(link.FromNodeId)))
                        {
                            PathEx newPath = (PathEx)path.Clone();
                            newPath.NodeIds.Insert(0, link.FromNodeId);
                            newPaths.Add(newPath);
                            checkedNodeIds.Add(link.FromNodeId);
                            isFinished = false;
                        }
                    }
                }
            }
            if (!isFinished)
            {
                suitableVehicle = SearchSuitableVehicle(newPaths, linksMap, vehiclesMap, checkedNodeIds);
            }
            if (suitableVehicle == null)
            {
                foreach (var value in tempNodeList)
                {
                    String nodeId = value;

                    IList sameNodeVehicles = new List<VehicleEx>();
                    vehiclesMap.TryGetValue(nodeId, out sameNodeVehicles);
                    if (sameNodeVehicles != null)
                    {
                        IEnumerator iterator2 = sameNodeVehicles.GetEnumerator();
                        if (iterator2.MoveNext())
                        {
                            VehicleEx vehicle = (VehicleEx)iterator2.Current;

                            suitableVehicle = vehicle;
                        }
                        if (suitableVehicle != null)
                        {
                            break;
                        }
                    }
                }
            }
            return suitableVehicle;
        }

        public PathInfoEx SearchPaths(LocationEx sourceLocation, LocationEx destLocation)
        {
            StationEx sourceStation = GetStation(sourceLocation.StationId);
            StationEx destStation = GetStation(destLocation.StationId);

            return SearchPaths(sourceStation, destStation);
        }

        public PathInfoEx SearchPaths(StationEx sourceStation, StationEx destStation)
        {
            PathInfoEx pathInfo = null;

            NodeEx sourceNode = SearchNodeByStationAsSource(sourceStation);
            NodeEx destNode = SearchNodeByStationAsDest(destStation);
            if ((sourceNode != null) &&
              (destNode != null))
            {
                pathInfo = SearchDynamicPaths(sourceStation, sourceNode, destNode, false, null, false);
            }
            else
            {
                //logger.warn("can not find Nodes " + sourceStation.toString() + " and " + destStation.toString());
            }
            return pathInfo;
        }

        public PathInfoEx SearchDynamicPaths(StationEx sourceStation, NodeEx sourceNode, NodeEx destNode, bool useCarrierProcessType, List<string> carrierProcessTypeNames, bool isContainDeadNode)
        {
            //logger.fine("searching dynamic paths");

            List<PathEx> paths = null;
            PathInfoEx pathInfo = new PathInfoEx();

            pathInfo.CurrentNode = sourceNode;
            pathInfo.CurrentStation = sourceStation;

            PathEx startPath = new PathEx();

            startPath.CurrentNode = pathInfo.CurrentNode;
            startPath.CurrentStation = pathInfo.CurrentStation;

            startPath.AddNodeId(sourceNode.NodeId);

            bool isSameNode = false;
            if (!sourceNode.NodeId.Equals(destNode.NodeId))
            {
                startPath.CurrentNodeId = sourceNode.NodeId;
                startPath.DestNodeId = destNode.NodeId;

                //logger.info(" before getLinks");
                IList links = GetLinks(useCarrierProcessType, carrierProcessTypeNames, isContainDeadNode);
                //logger.info(" before convertLinksToMapByFromNode");
                Dictionary<string, List<LinkEx>> linksMap = ConvertLinksToMapByFromNode(links);

                //logger.info("available links{" + links.size() + "}");
                //long startSearching = System.currentTimeMillis();
                long startSearching = DateTime.Now.Millisecond;

                int maxHoppingCount = this.MaxHoppingCount;
                int maxTotalCost = this.MaxTotalCost;

                pathInfo.MaxHoppingCount = this.MaxHoppingCount;
                pathInfo.MaxTotalCost = this.MaxTotalCost;

                pathInfo.IsUseDynamicLoad = this.UseDynamicLoad;
                if (this.UseDynamicLoad)
                {
                    pathInfo.SingleNodes = GetSingleNodesAsMap();
                }
                pathInfo.IsUseHeuristicDelay = this.UseHeuristicDelay;
                if (this.UseHeuristicDelay)
                {
                    pathInfo.TripleNodes = GetTripleNodesAsMap();
                }
                paths = SearchDynamicPaths(pathInfo, startPath, destNode, linksMap);

                long elpased = DateTime.Now.Millisecond - startSearching;
                //logger.fine("total dynamic paths{" + paths.size() + "}, elapsed{" + elpased + "}");

                pathInfo = MakePathInfo(paths, true);
            }
            else
            {
                startPath.CurrentNodeId = sourceNode.NodeId;
                startPath.DestNodeId = destNode.NodeId;
                if (paths == null)
                {
                    paths = new List<PathEx>();
                }
                paths.Add(startPath);

                pathInfo = MakePathInfo(paths, true);
            }
            return pathInfo;
        }

        public List<PathEx> SearchDynamicPaths(PathInfoEx pathInfo, PathEx currentPath, NodeEx destNode, Dictionary<string, List<LinkEx>> linksMap)
        {
            List<PathEx> newPaths = new List<PathEx>();
            String lastNodeName = currentPath.LastNodeId();

            List<LinkEx> nextNodes = GetNextLinks(linksMap, lastNodeName);
            foreach (var value in nextNodes)
            {
                LinkEx link = value;
                if ((!currentPath.ContainNodeId(link.ToNodeId)) ||
                  (link.ToNodeId.Equals(destNode.NodeId)))
                {
                    PathEx newPath = (PathEx)currentPath.Clone();

                    int dynamicLoad = pathInfo.IsUseDynamicLoad ? CalculateDynamicLoad(pathInfo.SingleNodes, link) : 0;
                    int heuristicDelay = pathInfo.IsUseHeuristicDelay ? CalculateHeuristicDelay(pathInfo.TripleNodes, newPath, link) : 0;

                    newPath.AddCost(dynamicLoad, link.Length, link.Load, heuristicDelay);
                    newPath.AddNodeId(link.ToNodeId);
                    if ((!ExceedMaxHoppingCount(newPath, pathInfo)) &&
                      (!ExceedMaxTotalCost(newPath, pathInfo)))
                    {
                        SetPathAvailability(newPath, link);
                        if (!link.ToNodeId.Equals(destNode.NodeId))
                        {
                            List<PathEx> nextPaths = SearchDynamicPaths(pathInfo, newPath, destNode, linksMap);
                            if (nextPaths.Count > 0)
                            {
                                newPaths.AddRange(nextPaths);
                            }
                        }
                        else
                        {
                            //if (//logger.isDebugEnabled())
                            //{
                            //    //logger.debug(newPath);
                            //}
                            newPaths.Add(newPath);
                            pathInfo.MaxHoppingCount = newPath.NodeIds.Count;
                            break;
                        }
                    }
                }
            }
            return newPaths;
        }

        protected IList GetLinks(bool useCarrierProcessType, List<string> carrierProcessTypeNames, bool isContainDeadNode)
        {
            if (useCarrierProcessType)
            {
                if (!isContainDeadNode)
                {
                    return this.PersistentDao.FindByAttribute(typeof(LinkEx), "Availability", "0");
                }
                return this.PersistentDao.FindAll(typeof(LinkEx));
            }
            return GetLinks(isContainDeadNode);
        }

        protected IList GetLinks(bool containDeadNode)
        {
            if (!containDeadNode)
            {
                return this.PersistentDao.FindByAttribute(typeof(LinkEx), "Availability", "0");
            }
            return this.PersistentDao.FindAll(typeof(LinkEx));
        }

        public IList GetLinkViewsByBayId(bool containDeadNode, String bayId)
        {
            if (!containDeadNode)
            {
                var attributes = new Dictionary<string, object> { { "Availability", "0" }, { "BayId", bayId } };
                return this.PersistentDao.FindByAttributes(typeof(LinkViewEx), attributes);
            }
            return this.PersistentDao.FindByAttribute(typeof(LinkViewEx), "BayId", bayId);
        }

        protected Dictionary<string, List<LinkEx>> ConvertLinksToMapByFromNode(IList links)
        {
            Dictionary<string, List<LinkEx>> linkMap = new Dictionary<string, List<LinkEx>>();
            foreach (var value in links)
            {
                LinkEx link = (LinkEx)value;
                if (linkMap.ContainsKey(link.FromNodeId))
                {
                    List<LinkEx> nextNodes = new List<LinkEx>();
                    linkMap.TryGetValue(link.FromNodeId, out nextNodes);
                    nextNodes.Add(link);
                }
                else
                {
                    List<LinkEx> newNextNodes = new List<LinkEx>();
                    newNextNodes.Add(link);
                    linkMap.Add(link.FromNodeId, newNextNodes);
                }
            }
            return linkMap;
        }

        protected Dictionary<string, IList> ConvertLinksToMapByToNode(IList links)
        {
            Dictionary<string, IList> linkMap = new Dictionary<string, IList>();
            foreach (var value in links)
            {
                LinkEx link = (LinkEx)value;
                if (linkMap.ContainsKey(link.ToNodeId))
                {
                    IList previousNodes = new List<LinkEx>();
                    linkMap.TryGetValue(link.ToNodeId, out previousNodes);
                    previousNodes.Add(link);
                }
                else
                {
                    IList newPreviousNodes = new List<LinkEx>();
                    newPreviousNodes.Add(link);
                    linkMap.Add(link.ToNodeId, newPreviousNodes);
                }
            }
            return linkMap;
        }

        public Dictionary<string, IList> ConvertLinkViewsToMapByToNode(IList linkViews)
        {
            Dictionary<string, IList> linkViewMap = new Dictionary<string, IList>();
            foreach (var value in linkViews)
            {
                LinkViewEx linkView = (LinkViewEx)value;
                if (linkViewMap.ContainsKey(linkView.ToNodeId))
                {
                    IList previousNodes = new List<LinkViewEx>();
                    linkViewMap.TryGetValue(linkView.ToNodeId, out previousNodes);
                    previousNodes.Add(linkView);
                }
                else
                {
                    List<LinkViewEx> newPreviousNodes = new List<LinkViewEx>();
                    newPreviousNodes.Add(linkView);
                    linkViewMap.Add(linkView.ToNodeId, newPreviousNodes);
                }
            }
            return linkViewMap;
        }

        public Dictionary<string, IList> ConvertLinkViewsToMapByFromNode(IList linkViews)
        {
            Dictionary<string, IList> linkViewMap = new Dictionary<string, IList>();
            foreach (var value in linkViews)
            {
                LinkViewEx linkView = (LinkViewEx)value;
                if (linkViewMap.ContainsKey(linkView.FromNodeId))
                {
                    IList previousNodes = new List<LinkViewEx>();
                    linkViewMap.TryGetValue(linkView.FromNodeId, out previousNodes);
                    previousNodes.Add(linkView);
                }
                else
                {
                    List<LinkViewEx> newPreviousNodes = new List<LinkViewEx>();
                    newPreviousNodes.Add(linkView);
                    linkViewMap.Add(linkView.FromNodeId, newPreviousNodes);
                }
            }
            return linkViewMap;
        }

        protected Dictionary<string, SingleNode> GetSingleNodesAsMap()
        {
            //Map singleNodeMap = new HashMap();
            Dictionary<string, SingleNode> singleNodeMap = new Dictionary<string, SingleNode>();
            return singleNodeMap;
        }

        protected Dictionary<string, TripleNode> GetTripleNodesAsMap()
        {
            //Map tripleNodeMap = new HashMap();
            Dictionary<string, TripleNode> tripleNodeMap = new Dictionary<string, TripleNode>();
            return tripleNodeMap;
        }

        protected List<LinkEx> GetNextLinks(Dictionary<string, List<LinkEx>> linksMap, String currentNodeName)
        {
            List<LinkEx> ret = new List<LinkEx>();
            if (linksMap.ContainsKey(currentNodeName))
            {
                linksMap.TryGetValue(currentNodeName, out ret);
                return ret;
            }
            return ret;
        }

        protected IList GetNextLinks(Dictionary<string, IList> linksMap, String currentNodeName)
        {
            IList ret = new List<LinkViewEx>();
            if (linksMap.ContainsKey(currentNodeName))
            {
                linksMap.TryGetValue(currentNodeName, out ret);
                return ret;
            }
            return ret;
        }

        protected int CalculateDynamicLoad(Dictionary<string, SingleNode> singleNodes, LinkEx link)
        {
            int dynamicLoad = 0;

            String singleNodeId = link.ToNodeId;
            SingleNode singleNode = new SingleNode();
            singleNodes.TryGetValue(singleNodeId, out singleNode);
            if (singleNode != null)
            {
                dynamicLoad = singleNode.Load;
                //    if (//logger.isDebugEnabled())
                //    {
                //        //logger.debug("dynamicLoad will be applied, " + singleNode);
                //    }
            }
            return dynamicLoad;
        }

        protected int CalculateHeuristicDelay(Dictionary<string, TripleNode> tripleNodes, PathEx path, LinkEx link)
        {
            int heuristicDelay = 0;
            if (path.NodeIds.Count > 1)
            {
                String firstNodeId = (String)path.NodeIds[path.NodeIds.Count - 2];
                String secondNodeId = link.FromNodeId;
                String thirdNodeId = link.ToNodeId;

                TripleNode tripleNode = new TripleNode();
                tripleNodes.TryGetValue(secondNodeId + firstNodeId + thirdNodeId, out tripleNode);
                if (tripleNode != null)
                {
                    heuristicDelay = tripleNode.Delay;
                    //if (//logger.isDebugEnabled())
                    //{
                    //    //logger.debug("heuristicDelay will be applied, " + tripleNode);
                    //}
                }
            }
            return heuristicDelay;
        }

        protected bool ExceedMaxHoppingCount(PathEx path, PathInfoEx pathInfo)
        {
            int maxHoppingCount = 0;
            if (path.IsAvailable)
            {
                maxHoppingCount = pathInfo.MaxHoppingCount;
            }
            else if ((!path.IsAvailable) && (!path.IsBanned))
            {
                maxHoppingCount = pathInfo.MaxHoppingCount;
            }
            if (path.NodeIds.Count > maxHoppingCount)
            {
                //if (//logger.isDebugEnabled())
                //{
                //    //logger.debug("currentHoppoingCount will exceed maxHoppingCount{" + maxHoppingCount + "}, " + path);
                //}
                return true;
            }
            return false;
        }

        protected bool ExceedMaxTotalCost(PathEx path, PathInfoEx pathInfo)
        {
            int maxTotalCost = 0;
            if (path.IsAvailable)
            {
                maxTotalCost = pathInfo.MaxTotalCost;
            }
            else if ((!path.IsAvailable) && (!path.IsBanned))
            {
                maxTotalCost = pathInfo.MaxTotalCost;
            }
            if (path.Cost > maxTotalCost)
            {
                //if (//logger.isDebugEnabled())
                //{
                //    //logger.debug("currentTotalWeight will exceed maxTotalCost{" + maxTotalCost + "}, " + path);
                //}
                return true;
            }
            return false;
        }

        protected void SetPathAvailability(PathEx path, LinkEx link)
        {
            String availability = GetLinkAvailability(link);
            if (!availability.Equals("0"))
            {
                path.IsAvailable = false;
                if (!path.UnavailableLinks.Contains(link))
                {
                    path.UnavailableLinks.Add(link);
                }
            }
        }

        protected String GetLinkAvailability(LinkEx link)
        {
            return link.Availability;
        }

        protected PathInfoEx MakePathInfo(List<PathEx> paths, bool sort)
        {
            PathInfoEx pathInfo = new PathInfoEx();
            if (paths.Count == 0)
            {
                pathInfo.IsExistPath = false;
                return pathInfo;
            }
            foreach (var item in paths)
            {
                PathEx path = item;
                if (path.IsAvailable)
                {
                    pathInfo.AddPathAvailable(path);
                }
                else
                {
                    pathInfo.AddPathUnavailable(path);
                }
            }


            if (sort)
            {
                Sort(pathInfo);
            }
            return pathInfo;
        }

        protected void Sort(PathInfoEx pathInfo)
        {
            pathInfo.PathsAvailable.Sort();
            pathInfo.PathsUnavailable.Sort();
            //Collections.sort(pathInfo.PathsAvailable, this.comparator);
            //Collections.sort(pathInfo.PathsUnavailable, this.comparator);
        }

        protected void SortVehicleByNodeCheckTime(List<VehicleEx> vehicleList)
        {
            vehicleList.Sort();
            //Collections.sort(vehicleList, this.nodeCheckTimecomparator);
        }

        public NodeEx SearchNodeByStationAsSource(StationEx station)
        {
            LinkEx link = GetLinkByStation(station);
            if (link != null)
            {
                return GetNode(link.ToNodeId);
            }
            return null;
        }

        public NodeEx SearchNodeByStationAsDest(StationEx station)
        {
            LinkEx link = GetLinkByStation(station);
            if (link != null)
            {
                return GetNode(link.FromNodeId);
            }
            return null;
        }

        public LinkEx GetLink(String id)
        {
            StringBuilder sbId = new StringBuilder(id);
            return (LinkEx)this.PersistentDao.Find(typeof(LinkEx), sbId, false);
        }

        public LinkEx GetLinkByStation(StationEx station)
        {
            StringBuilder sbStationLinkId = new StringBuilder(station.LinkId);
            return (LinkEx)this.PersistentDao.Find(typeof(LinkEx), sbStationLinkId, false);
        }
        
        public NodeEx GetNode(String id)
        {
            IList results = this.PersistentDao.FindByAttribute(typeof(NodeEx), "NodeId", id, false);
            if (results != null && results.Count > 0)
                return (NodeEx)results[0];
            return null;
        }

        public StationEx GetStation(String id)
        {
            StringBuilder sbId = new StringBuilder(id);
            return (StationEx)this.PersistentDao.Find(typeof(StationEx), sbId, false);
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

        public LocationEx GetLocationByLocationId(String portId)
        {
            IList locations = this.PersistentDao.FindByAttribute(typeof(LocationEx), "LocationId", portId);
            if (locations.Count > 0)
            {
                return (LocationEx)locations[0];
            }
            return null;
        }

        protected IList GetVehicles(bool useCarrierProcessType, List<string> carrierProcessTypeNames, bool isContainUnavailableVehicle)
        {
            if (useCarrierProcessType)
            {
                return this.PersistentDao.FindAll(typeof(VehicleEx));
            }
            return GetVehicles(isContainUnavailableVehicle);
        }

        public IList GetVehicles(String bayId, bool containUnavailableVehicle)
        {
            IList vehicleList;

            if (!containUnavailableVehicle)
            {
                var attributes = new Dictionary<string, object>
                {
                    { "ConnectionState", "CONNECT" },
                    { "State", "ALIVE" },
                    { "BayId", bayId },
                    { "FullState", "EMPTY" },
                    { "Installed", "T" }
                };
                IList allMatches = this.PersistentDao.FindByAttributes(typeof(VehicleEx), attributes);
                // OR condition: ProcessingState == "IDLE" || ProcessingState == "PARK"
                // Gt: BatteryRate > 10%
                // Order: NodeCheckTime ASC
                vehicleList = allMatches.Cast<VehicleEx>()
                    .Where(v => (v.ProcessingState == "IDLE" || v.ProcessingState == "PARK") && v.BatteryRate > 5.0F)
                    .OrderBy(v => v.NodeCheckTime)
                    .ToList<VehicleEx>();
            }
            else
            {
                vehicleList = this.PersistentDao.FindAll(typeof(VehicleEx));
            }

            IList returnList = new List<VehicleEx>();

            foreach (var item in vehicleList)
            {
                VehicleEx vehicle = (VehicleEx)item;
                TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicle.VehicleId);
                if (transportCommand != null)
                {
                    //logger.warn("vehicle{" + vehicle.getId() + "} has a transportCommand{" + transportCommand.getId() + "}. it will be not designated.");
                }
                else
                {
                    String vehicleId = vehicle.VehicleId;
                    bool flag = true;
                    IList alarms = this.AlarmManager.GetAlarmsByVehicleId(vehicleId);
                    if ((alarms != null) && (alarms.Count > 0))
                    {
                        foreach (var item2 in alarms)
                        {
                            AlarmEx alarm = (AlarmEx)item2;
                            String alarmId = alarm.AlarmId;
                            AlarmSpecEx alarmSpec = this.AlarmManager.GetAlarmSpecByAlarmId(alarmId);
                            if (alarmSpec.Severity.Equals("HEAVY"))
                            {
                                //logger.info("vehicle : " + vehicleId + " is Heavy Alarm.");
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

            }
            return returnList;           
        }

        protected List<VehicleEx> GetChargeVehicles(String bayId, bool containUnavailableVehicle)
        {
            IList vehicleList;

            //KSB
            //RGV구간의 사용하는 BAYID 확인?
            // - RGV 적용 BAYID
            if (bayId.Equals("AMT-LFC(A)", StringComparison.OrdinalIgnoreCase)    ||
                bayId.Equals("AMT(A)-LFC(A)", StringComparison.OrdinalIgnoreCase) ||
                bayId.Equals("AMT-LFC(B)", StringComparison.OrdinalIgnoreCase)    ||
                bayId.Equals("AMT(B)-LFC(B)", StringComparison.OrdinalIgnoreCase))
            {
                if (!containUnavailableVehicle)
                {
                    var attributes = new Dictionary<string, object>
                    {
                        { "ConnectionState", "CONNECT" },
                        { "ProcessingState", "IDLE" },
                        { "State", "ALIVE" },
                        //BatteryVoltage 조건 사용하지 않으면 항상 충전으로 보낼수 있음
                        { "BayId", bayId },
                        { "FullState", "EMPTY" },
                        { "Installed", "T" }
                    };
                    vehicleList = this.PersistentDao.FindByAttributesOrderBy(typeof(VehicleEx), attributes, "BatteryVoltage");
                }
                else
                {
                    vehicleList = this.PersistentDao.FindAll(typeof(VehicleEx));
                }
            }
            //AGV 일경우
            else
            {
                if (!containUnavailableVehicle)
                {
                    var attributes = new Dictionary<string, object>
                    {
                        { "ConnectionState", "CONNECT" },
                        { "State", "ALIVE" },
                        //20190928 KSB 원래 기준은24V 미만인데, V1은 밧데리 노후로 임시로 25V미만일경우 변경함
                        { "BayId", bayId },
                        { "FullState", "EMPTY" },
                        { "Installed", "T" }
                    };
                    IList allMatches = this.PersistentDao.FindByAttributesOrderBy(typeof(VehicleEx), attributes, "BatteryVoltage");
                    // OR condition: ProcessingState == "IDLE" || ProcessingState == "PARK" (20200525 KKH 충전조건 Park상태도 추가)
                    // Le: BatteryVoltage <= 25.0F
                    vehicleList = allMatches.Cast<VehicleEx>()
                        .Where(v => (v.ProcessingState == "IDLE" || v.ProcessingState == "PARK") && v.BatteryVoltage <= 25.0F)
                        .ToList<VehicleEx>();
                }
                else
                {
                    vehicleList = this.PersistentDao.FindAll(typeof(VehicleEx));
                }
            }

            List<VehicleEx> returnList = new List<VehicleEx>();
            foreach (var item in vehicleList)
            {
                VehicleEx vehicle = (VehicleEx)item;
                String vehicleId = vehicle.VehicleId;
                bool flag = true;
                IList alarms = this.AlarmManager.GetAlarmsByVehicleId(vehicleId);
                if ((alarms != null) && (alarms.Count > 0))
                {
                    foreach (var item2 in alarms)
                    {
                        AlarmEx alarm = (AlarmEx)item2;
                        string alarmId = alarm.AlarmId;
                        AlarmSpecEx alarmSpec = this.AlarmManager.GetAlarmSpecByAlarmId(alarmId);
                        if (alarmSpec.Severity.Equals("HEAVY"))
                        {
                            //logger.info("vehicle : " + vehicleId + " is Heavy Alarm.");
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

        protected IList GetVehicles(bool containUnavailableVehicle)
        {
            IList vehicleList;
            if (!containUnavailableVehicle)
            {
                var attributes = new Dictionary<string, object>
                {
                    { "ConnectionState", "CONNECT" },
                    { "ProcessingState", "IDLE" },
                    { "State", "ALIVE" },
                    { "FullState", "EMPTY" },
                    { "Installed", "T" }
                };
                IList allMatches = this.PersistentDao.FindByAttributes(typeof(VehicleEx), attributes);
                // Gt: BatteryVoltage > 23.0F
                vehicleList = allMatches.Cast<VehicleEx>()
                    .Where(v => v.BatteryVoltage > 23.0F)
                    .ToList<VehicleEx>();
            }
            else
            {
                vehicleList = this.PersistentDao.FindAll(typeof(VehicleEx));
            }

            IList returnList = new List<VehicleEx>();
            foreach (var item in vehicleList)
            {
                VehicleEx vehicle = (VehicleEx)item;
                String vehicleId = vehicle.VehicleId;
                bool flag = true;
                IList alarms = this.AlarmManager.GetAlarmsByVehicleId(vehicleId);
                if ((alarms != null) && (alarms.Count > 0))
                {
                    foreach (var item2 in alarms)
                    {
                        AlarmEx alarm = (AlarmEx)item2;
                        String alarmId = alarm.AlarmId;
                        AlarmSpecEx alarmSpec = this.AlarmManager.GetAlarmSpecByAlarmId(alarmId);
                        if (alarmSpec.Severity.Equals("HEAVY"))
                        {
                            //logger.info("vehicle : " + vehicleId + " is Heavy Alarm.");
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
        protected Dictionary<string, IList> ConvertVehiclesToMap(IList vehicles)
        {
            Dictionary<string, IList> vehicleMap = new Dictionary<string, IList>(); //HashMap();
            foreach (var item in vehicles)
            {
                VehicleEx vehicle = (VehicleEx)item;
                if (vehicleMap.ContainsKey(vehicle.CurrentNodeId))
                {
                    IList sameNodeVehicles = new List<VehicleEx>();
                    vehicleMap.TryGetValue(vehicle.CurrentNodeId, out sameNodeVehicles);
                    sameNodeVehicles.Add(vehicle);
                }
                else
                {
                    List<VehicleEx> sameNodeVehicles = new List<VehicleEx>();
                    sameNodeVehicles.Add(vehicle);
                    vehicleMap.Add(vehicle.CurrentNodeId, sameNodeVehicles);
                }
            }
            return vehicleMap;
        }

        public StationViewEx GetStationView(String id)
        {
            StringBuilder sbId = new StringBuilder(id);
            return (StationViewEx)this.PersistentDao.Find(typeof(StationViewEx), sbId, false);
        }

        public IList GetLinkViews(String id)
        {
            StringBuilder sbId = new StringBuilder(id);
            return (IList)this.PersistentDao.Find(typeof(LinkViewEx), sbId, false);
        }

        public IList GetLinkZoneByFromNodeId(String fromNodeId) //(List<LinkZoneEx>)
        {
            IList returnList = null;

            StationEx station = GetStation(fromNodeId);
            if (station != null)
            {
                String linkId = station.LinkId;
                returnList = this.PersistentDao.FindByAttribute(typeof(LinkZoneEx), "LinkId", linkId);
                return returnList;
            }
            //logger.error("can not find Station. so cannot get LinkId!, fromNodeId : " + fromNodeId);
            if (this.PersistentDao.FindByAttribute(typeof(LinkEx), "FromNodeId", fromNodeId) != null)
            {
                IList linkList = this.PersistentDao.FindByAttribute(typeof(LinkEx), "FromNodeId", fromNodeId);
                if ((linkList != null) && (linkList.Count > 0))
                {
                    LinkEx link = (LinkEx)linkList[0];
                    if (link != null)
                    {
                        String linkId = link.Id;
                        returnList = this.PersistentDao.FindByAttribute(typeof(LinkZoneEx), "LinkId", linkId);
                    }
                }
                else
                {
                    //logger.error("can not find Link, fromNodeId : " + fromNodeId);
                }
            }
            return returnList;
        }

        public IList GetLinkViewsByFromNodeId(String fromNodeId)
        {
            return this.PersistentDao.FindByAttribute(typeof(LinkViewEx), "FromNodeId", fromNodeId);
        }

        public IList GetLinkZoneByFromBayId(String bayId)
        {
            IList returnList = this.PersistentDao.FindByAttribute(typeof(LinkZoneEx), "ZoneId", bayId);

            return returnList;
        }

        public LocationViewEx GetLocationView(String id)
        {
            StringBuilder sbId = new StringBuilder(id);
            return (LocationViewEx)this.PersistentDao.Find(typeof(LocationViewEx), sbId, false);
        }

        public LocationViewEx GetLocationViewByStationId(String stationId)
        {
            IList locations = this.PersistentDao.FindByAttribute(typeof(LocationViewEx), "StationId", stationId);
            if (locations.Count > 0)
            {
                return (LocationViewEx)locations[0];
            }
            return null;
        }

        public IList GetChargeLocationViewsByBayId(String bayId)
        {
            var attributes = new Dictionary<string, object>
            {
                { "Location_Type", "CHARGE" },
                { "BayId", bayId },
                { "TransferFlag", "Y" }
            };
            return this.PersistentDao.FindByAttributes(typeof(LocationViewEx), attributes);
        }

        public IList GetStockLocationViewsByBayId(String bayId)
        {
            var attributes = new Dictionary<string, object>
            {
                { "Location_Type", "STOCK" },
                { "BayId", bayId },
                { "TransferFlag", "Y" }
            };
            return this.PersistentDao.FindByAttributes(typeof(LocationViewEx), attributes);
        }

        public IList GetLocationViewsByBayId(String bayId)
        {
            return this.PersistentDao.FindByAttribute(typeof(LocationViewEx), "BayId", bayId);
        }

        public string GetCommonUseBayIdBySourceDest(string sourceNodeId, string destNodeId, string transferFlag)
        {
            IList listLinkZonesSource = GetLinkZoneByFromNodeId(sourceNodeId);
            IList listLinkZonesDest = GetLinkZoneByFromNodeId(destNodeId);
            if (listLinkZonesSource != null)
            {
                foreach (var item in listLinkZonesSource)
                {

                    LinkZoneEx linkZoneSource = (LinkZoneEx)item;
                    if (linkZoneSource.TransferFlag.Equals(transferFlag))
                    {
                        foreach (var item2 in listLinkZonesDest)
                        {

                            LinkZoneEx linkZoneDest = (LinkZoneEx)item2;
                            if (linkZoneDest.TransferFlag.Equals(transferFlag))
                            {
                                if (linkZoneSource.ZoneId.Equals(linkZoneDest.ZoneId))
                                {
                                    return linkZoneSource.ZoneId;
                                }
                            }
                        }
                    }
                }
                //logger.warn("Source BayId and Dest BayId is not same, " + sourceNodeId + " and " + destNodeId);
            }
            else
            {
                //logger.warn("This Source [" + sourceNodeId + "] is not exist in NA_R_LINK_ZONE");
            }
            return null;
        }

        public PathInfoEx SearchPaths(NodeEx sourceNode, StationEx destStation)
        {
            PathInfoEx pathInfo = null;

            NodeEx destNode = SearchNodeByStationAsDest(destStation);
            if ((sourceNode != null) &&
              (destNode != null))
            {
                StationEx sourceStation = new StationEx();
                pathInfo = SearchDynamicPaths(sourceStation, sourceNode, destNode, false, null, false);
            }
            else
            {
                //logger.warn("can not find dest node{" + destStation.toString() + "}");
            }
            return pathInfo;
        }

        public PathInfoEx SearchPaths(NodeEx currentNode, LocationEx destLocation)
        {
            PathInfoEx pathInfo = null;

            StationEx destStation = GetStation(destLocation.StationId);
            NodeEx destNode = SearchNodeByStationAsDest(destStation);
            StationEx sourceStation = new StationEx();
            if ((currentNode != null) &&
              (destNode != null))
            {
                pathInfo = SearchDynamicPaths(sourceStation, currentNode, destNode, false, null, false);
            }
            else
            {
                //logger.warn("can not find destNode {" + destStation.toString() + "}");
            }
            return pathInfo;
        }

        public bool IsSameBayId(String vehicleBayId, String destNodeId, String transferFlag)
        {
            bool result = false;

            IList listLinkZonesDest = GetLinkZoneByFromNodeId(destNodeId);
            if (vehicleBayId != null)
            {
                foreach (var item in listLinkZonesDest)
                {
                    LinkZoneEx linkZoneDest = (LinkZoneEx)item;
                    if (linkZoneDest.TransferFlag.Equals(transferFlag))
                    {
                        if (linkZoneDest.ZoneId.Equals(vehicleBayId))
                        {
                            result = true;
                        }
                    }
                }
            }
            return result;
        }

        public VehicleEx SearchSuitableVehicleDijkstra(IList paths, Dictionary<string, IList> linkMap, Dictionary<string, IList> vehicleMap, IDictionary checkedPaths)
        {
            IList newPaths = new List<List<string>>();
          
            foreach (var value in paths)
            {
                List<string> path = value as List<string>;
                IList links = new List<LinkViewEx>();

                if (path.Count <= 0) return null;

                linkMap.TryGetValue(path[path.Count - 1], out links);

                if (links != null)
                {
                    foreach (var value2 in links)
                    {
                        LinkViewEx link = value2 as LinkViewEx;
                        String nextNodeId = link.FromNodeId;

                        IList vehicles = new List<VehicleEx>();
                        vehicleMap.TryGetValue(nextNodeId, out vehicles);
                        if (vehicles != null)
                        {
                            VehicleEx vehicle = (VehicleEx)vehicles[0];
                            //logger.info(vehicle + ", " + path);
                            return vehicle;
                        }
                        if (checkedPaths.Contains(nextNodeId))
                        {
                            IList oldPath = new List<List<string>>();
                            (checkedPaths as Dictionary<string, IList>).TryGetValue(nextNodeId, out oldPath);
                            if (oldPath.Count > path.Count)
                            {
                                List<string> newPath = new List<string>();
                                if (path.Count > 0)
                                {
                                    newPath.AddRange(path);
                                }
                                newPath.Add(nextNodeId);
                                checkedPaths[nextNodeId] = path;
                                newPaths.Add(newPath);
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
                            newPaths.Add(newPath);
                        }
                    }
                }
                else
                {
                    //logger.warn("links does not exist in repository, link{" + path.get(path.size() - 1) + "}");
                }
            }
            if (newPaths.Count > 0)
            {
                return SearchSuitableVehicleDijkstra(newPaths, linkMap, vehicleMap, checkedPaths);
            }
            return null;
        }

        public VehicleEx SearchSuitableChargeVehicle(String bayId, LocationEx destLocation)
        {
            StationEx destStation = GetStation(destLocation.StationId);
            NodeEx destNode = SearchNodeByStationAsDest(destStation);
            if (destNode != null)
            {
                try
                {
                    IList linkViews = GetLinkViewsByBayId(true, bayId);
                    Dictionary<string, IList> linksMap = ConvertLinkViewsToMapByToNode(linkViews);

                    List<VehicleEx> nominateVehicles = GetChargeVehicles(bayId, false);
                    if ((nominateVehicles == null) || (nominateVehicles.Count < 1))
                    {
                        //logger.warn("can not find available charge vehicles, bayId{" + bayId + "}");
                        return null;
                    }
                    Dictionary<float, List<VehicleEx>> vehicleVoltageMap = new Dictionary<float, List<VehicleEx>>();
                    //Map vehicleVoltageMap = new HashMap();
                    //...
                    //List<List<VehicleEx>> treeMap = new List<List<VehicleEx>>(); //확인필요
                    foreach (var value in nominateVehicles)
                    {
                        VehicleEx vehicle = value;
                        if (vehicleVoltageMap.ContainsKey(vehicle.BatteryVoltage))
                        {
                            List<VehicleEx> sameNodeVehicles = new List<VehicleEx>();
                            vehicleVoltageMap.TryGetValue(vehicle.BatteryVoltage, out sameNodeVehicles);
                            sameNodeVehicles.Add(vehicle);
                            //treeMap.Add(sameNodeVehicles);
                        }
                        else
                        {
                            List<VehicleEx> sameNodeVehicles = new List<VehicleEx>();
                            sameNodeVehicles.Add(vehicle);
                            vehicleVoltageMap.Add(vehicle.BatteryVoltage, sameNodeVehicles);
                            //treeMap.Add(sameNodeVehicles);
                        }
                        if (vehicle != null)
                        {
                            //logger.info("Find Suitable Charge Vehicle : " + vehicle);

                            return vehicle;
                        }
                    }
                    //////TreeMap treeMap = new TreeMap(vehicleVoltageMap);
                    ////List<List<VehicleEx>> treeMap = new List<List<VehicleEx>>(); //확인필요
                    //foreach (var value in treeMap)
                    //  //  for (Iterator iterator = treeMap.values().iterator(); iterator.hasNext();)
                    //{
                    //    List<VehicleEx> listVehicles = value;
                    //    Dictionary<string, IList> vehicleMap = ConvertVehiclesToMap(listVehicles);

                    //    List<string> pathed = new List<string>();                        
                    //    pathed.Add(destNode.NodeId);

                    //    List<List<string>> paths = new List<List<string>>();
                    //    paths.Add(pathed);

                    //    Dictionary<string, List<List<string>>> checkedPaths = new Dictionary<string, List<List<string>>>();
                    //    checkedPaths.Add(destNode.NodeId, paths);

                    //    VehicleEx vehicle = SearchSuitableVehicleDijkstra(paths, linksMap, vehicleMap, checkedPaths);
                    //    if (vehicle != null)
                    //    {
                    //        //logger.info("Find Suitable Charge Vehicle : " + vehicle);

                    //        return vehicle;
                    //    }
                    //}
                }
                catch (Exception e)
                {
                    //logger.error("failed to Suitable Charge Vehicle ", e);
                }
            }
            //logger.warn("can not find Source : " + destStation);

            //logger.warn("can not find Suitable Charge Vehicle, from " + bayId);

            return null;
        }
    }
}
