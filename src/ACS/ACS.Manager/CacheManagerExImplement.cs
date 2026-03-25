using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Path;
using ACS.Core.Cache;
using ACS.Core.Path.Model;
using ACS.Core.Resource.Model;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Base;

namespace ACS.Manager
{
    public class CacheManagerExImplement : AbstractManager, ICacheManagerEx
    {
        #region Private member variables
        private List<LinkEx> links;
        private List<LinkViewEx> linkViews = new List<LinkViewEx>();
        private List<NodeEx> nodes;
        private List<LocationEx> locations;
        private List<StationEx> stations;
        private List<LocationViewEx> locationViews = new List<LocationViewEx>();
        List<LinkZoneEx> linkZones;

        private IList nodeEx;
        private IList linkEx;
        private IList LinkZoneEx;
        private IList locationEx;
        private IList stationEx;

        Dictionary<string, LinkEx> linkMap = new Dictionary<string, LinkEx>();
        Dictionary<string, List<LinkEx>> linkFromNodeMap = new Dictionary<string, List<LinkEx>>();
        Dictionary<string, List<LinkEx>> linkToNodeMap = new Dictionary<string, List<LinkEx>>();

        Dictionary<string, ZoneEx> zoneMap = new Dictionary<string, ZoneEx>();
        Dictionary<string, List<LinkViewEx>> linkViewMap = new Dictionary<string, List<LinkViewEx>>();
        Dictionary<string, List<LinkViewEx>> linkViewMapByBayId = new Dictionary<string, List<LinkViewEx>>();
        Dictionary<string, List<LinkViewEx>> linkViewMapByFromNode = new Dictionary<string, List<LinkViewEx>>();
        Dictionary<string, NodeEx> nodeExMap = new Dictionary<string, NodeEx>();
        Dictionary<string, LocationEx> locationMapByLocationId = new Dictionary<string, LocationEx>();
        Dictionary<string, LocationEx> locationMapByStation = new Dictionary<string, LocationEx>();
        Dictionary<string, StationEx> stationMap = new Dictionary<string, StationEx>();
        Dictionary<string, List<LocationViewEx>> locationViewMapByLocationId = new Dictionary<string, List<LocationViewEx>>();
        Dictionary<string, List<LocationViewEx>> locationViewMapByStation = new Dictionary<string, List<LocationViewEx>>();
        Dictionary<string, List<LinkEx>> convertLinksToMapByToNode = new Dictionary<string, List<LinkEx>>();
        Dictionary<string, LinkViewEx> convertLinkViewToMapByFromNodeByBay = new Dictionary<string, LinkViewEx>();
        Dictionary<string, List<LinkZoneEx>> linkZoneMapByLink = new Dictionary<string, List<LinkZoneEx>>();

        Dictionary<String, List<LocationViewEx>> locationViewCharge = new Dictionary<string, List<LocationViewEx>>();
        Dictionary<string, List<PathEx>> dynamicLink = new Dictionary<string, List<PathEx>>();
        #endregion

        public Lazy<IResourceManagerEx> ResourceManagerLazy { get; set; }
        private IResourceManagerEx ResourceManager => ResourceManagerLazy?.Value;
 
        public Dictionary<string, LinkEx> LinkMap { get { return linkMap; } set { value = linkMap; } }
        public Dictionary<string, ZoneEx> ZoneMap { get { return zoneMap; } set { value = zoneMap; } }

        public Dictionary<string, List<LinkViewEx>> LinkViewMap { get { return linkViewMap; } set { value = linkViewMap; } }

        //public Dictionary<string, NodeEx> NodeMap { get { return nodeMap; } set { value = nodeMap; } }

        public bool CheckLocation(string locationId)
        {
            if (locationMapByLocationId.ContainsKey(locationId))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public Dictionary<string, List<LinkEx>> ConvertLinksToMapByFromNode()
        {
            return this.linkFromNodeMap;
        }

        public Dictionary<string, List<LinkEx>> ConvertLinksToMapByToNode()
        {
            return convertLinksToMapByToNode;
        }

        public LocationViewEx GetBayByLocationViewByPortId(string portId)
        {
            List<LocationViewEx> locationView = (List<LocationViewEx>)locationViewMapByLocationId[portId];
            foreach (LocationViewEx loc in locationView)
            {
                if (loc.TransferFlag.Equals("Y"))
                {
                    return loc;
                }
            }

            return null;
        }

        public LocationViewEx GetBayByLocationViewByStationId(string stationId)
        {
            if (!locationViewMapByStation.ContainsKey(stationId))
            {
                return null;
            }
            else
            {
                List<LocationViewEx> locationViews = (List<LocationViewEx>)locationViewMapByStation[stationId];

                foreach (LocationViewEx loc in locationViews)
                {
                    if (loc.TransferFlag.Equals("Y"))
                    {
                        return loc;
                    }
                }
                return null;
            }
        }

        public List<LocationViewEx> GetChargeLocationViewsByBayId(string bayId)
        {
            if (locationViewCharge.ContainsKey(bayId))
            {
                return locationViewCharge[bayId];
            }
            else
            {
                return null;
            }
        }

        public List<PathEx> GetGroupPathByNode(string nodeId)
        {
            if (dynamicLink.ContainsKey(nodeId))
            {
                return dynamicLink[nodeId];
            }
            else
            {
                return null;
            }
        }

        public List<LinkEx> GetLinkACS()
        {
            return this.links;
        }

        public List<LinkEx> GetLinkByFromNodeId(string nodeId)
        {
            return (List<LinkEx>)this.linkFromNodeMap[nodeId];
        }

        public LinkEx GetLinkById(string linkId)
        {
            if (linkMap.ContainsKey(linkId))
            {
                return this.linkMap[linkId];
            }
            else
            {
                return null;
            }

        }

        public List<LinkEx> GetLinkByToNodeId(string nodeId)
        {
            if (convertLinksToMapByToNode.ContainsKey(nodeId))
            {
                return convertLinksToMapByToNode[nodeId];
            }
            else
            {
                return null;
            }
        }

        public List<LinkViewEx> GetLinkViewACS()
        {
            return this.linkViews;
        }

        public List<LinkViewEx> GetLinkViewByBayId(string bayId)
        {
            if (linkViewMapByBayId.ContainsKey(bayId))
            {
                return linkViewMapByBayId[bayId];
            }
            else
            {
                return null;
            }
        }

        public LinkViewEx GetLinkViewById(string linkViewId)
        {
            if (linkViewMap.ContainsKey(linkViewId))
            {
                return linkViewMap[linkViewId][0];
            }
            else
            {
                return null;
            }
        }

        public List<LinkViewEx> GetLinkViewsByFromNodeId(string fromNodeId)
        {
            if (linkViewMapByFromNode.ContainsKey(fromNodeId))
            {
                return linkViewMapByFromNode[fromNodeId];
            }
            else
            {
                return null;
            }
        }

        public List<LinkViewEx> GetLinkViewsByFromNodeId(string fromNodeId, string bayId)
        {
            List<LinkViewEx> linkList = new List<LinkViewEx>();

            if (linkViewMapByFromNode.ContainsKey(fromNodeId))
            {
                foreach (LinkViewEx link in linkViewMapByFromNode[fromNodeId])
                {
                    if (link.BayId.Equals(bayId)) linkList.Add(link);
                }

                return linkList;
            }
            else
            {
                return linkList;
            }
        }

        public LocationEx GetLocation(string portId)
        {
            if (locationMapByLocationId.ContainsKey(portId))
            {
                return locationMapByLocationId[portId];
            }
            else
            {
                return null;
            }
        }

        public List<LocationEx> GetLocationACS()
        {
            return locations;
        }

        public LocationEx GetLocationByLocationId(string portId)
        {

            if (!string.IsNullOrEmpty(portId) && locationMapByLocationId.ContainsKey(portId))
            {
                LocationEx returnvalue;
                locationMapByLocationId.TryGetValue(portId, out returnvalue);
                return returnvalue;
            }
            else
            {
                return null;
            }
        }

        public LocationEx GetLocationByStationId(string stationId)
        {
            if (locationMapByStation.ContainsKey(stationId))
            {
                return locationMapByStation[stationId];
            }
            else
            {
                return null;
            }
        }

        public List<LocationViewEx> GetLocationViewACS()
        {
            return locationViews;
        }

        public List<LocationViewEx> GetLocationViewByLocationId(string portId)
        {
            if (locationViewMapByLocationId.ContainsKey(portId))
            {
                return locationViewMapByLocationId[portId];
            }
            else
            {
                return null;
            }
        }

        public List<LocationViewEx> GetLocationViewByStationId(string stationId)
        {
            if (locationViewMapByStation.ContainsKey(stationId))
            {
                return locationViewMapByStation[stationId];
            }
            else
            {
                return null;
            }
        }

        public LocationViewEx GetLocationViewByStationIdAndBayId(string stationId, string bayId)
        {
            if (locationViewMapByStation.ContainsKey(stationId))
            {
                List<LocationViewEx> locations = (List<LocationViewEx>)locationViewMapByStation[stationId];


                if (locations != null && locations.Count > 0)
                {
                    foreach (LocationViewEx location in locations)
                    {
                        if (location.BayId.Equals(bayId))
                        {
                            return location;
                        }
                    }
                }
                else
                {
                    return null;
                }
            }

            return null;
        }

        public LocationViewEx GetLocationViewChargeByStationIdAndBayId(string stationId, string bayId)
        {
            if (locationViewCharge.ContainsKey(bayId))
            {
                List<LocationViewEx> locations = (List<LocationViewEx>)locationViewCharge[bayId];


                if (locations != null && locations.Count > 0)
                {
                    foreach (LocationViewEx location in locations)
                    {
                        if (location.StationId.Equals(stationId))
                        {
                            return location;
                        }
                    }
                }
                else
                {
                    return null;
                }
            }

            return null;
        }

        public NodeEx GetNode(string nodeId)
        {
            if (nodeExMap.ContainsKey(nodeId))
            {
                return nodeExMap[nodeId];
            }
            else
            {
                return null;
            }
        }

        public List<NodeEx> GetNodeACS()
        {
            return nodes;
        }

        public NodeEx GetNodeByStation(StationEx station)
        {
            if (nodeExMap.ContainsKey(station.Id))
            {
                return nodeExMap[station.Id];
            }
            else
            {
                return null;
            }
        }

        public List<StationEx> GetStationACS()
        {
            return stations;
        }

        public StationEx GetStationById(string stationId)
        {
            if (stationMap.ContainsKey(stationId))
            {
                return stationMap[stationId];
            }
            else
            {
                return null;
            }
        }

        public bool Synchronize()
        {
            this.SynchronizeNodeACS();
            this.SynchronizeLinkACS();
            this.SynchronizeLinkZoneACS();
            this.SynchronizeLocationACS();
            this.SynchronizeStationACS();
            this.SynchronizeLinkViewACS();
            this.SynchronizeLocationViewACS();

            return true;
        }

        protected List<LinkEx> GetNextLinks(Dictionary<string, List<LinkEx>> linksMap, String currentNodeName)
        {
            if (linksMap.ContainsKey(currentNodeName))
            {

                return linksMap[currentNodeName];
            }
            else
            {

                return new List<LinkEx>();
            }
        }

        public PathEx SearchDynamicLinkSimple(LinkEx startLink)
        {
            PathEx tempPath = new PathEx();
            List<String> path = new List<String>();
            path.Add(startLink.FromNodeId);
            String checkNode = startLink.ToNodeId;
            String endNode = "";
            int cost = startLink.Length + startLink.Load;
            bool stoped = false;
            while (!stoped)
            {
                List<LinkEx> nextNodes = this.GetNextLinks(linkFromNodeMap, checkNode);
                path.Add(checkNode);
                if (nextNodes.Count == 0 || nextNodes.Count > 1)
                {
                    endNode = checkNode;
                    stoped = true;
                }
                else
                {
                    checkNode = nextNodes[0].ToNodeId;
                    if (path.Contains(checkNode))
                    {
                        stoped = true;
                        //endNode = checkNode;
                        //path.add(checkNode);
                        logger.Error("Round path:" + path);
                    }
                    cost += nextNodes[0].Length + nextNodes[0].Load;
                }
            }
            tempPath.Id = startLink.FromNodeId + "_" + endNode;
            tempPath.Cost = cost;
            tempPath.NodeIds = path;
            return tempPath;
        }

        public bool SynchronizeDynamicLink()
        {
            PathEx dynamicLink = new PathEx();
            Dictionary<String, List<PathEx>> tempDynamicLink = new Dictionary<string, List<PathEx>>();
            foreach (LinkEx firstLink in links)
            {
                List<LinkEx> nextNodes = this.GetNextLinks(linkFromNodeMap, firstLink.ToNodeId);
                List<LinkEx> lastNodes = this.GetNextLinks(this.linkToNodeMap, firstLink.FromNodeId);
                if (lastNodes.Count == 0)
                {
                    dynamicLink = SearchDynamicLinkSimple(firstLink);
                    if (dynamicLink != null)
                    {
                        String lastNode = dynamicLink.NodeIds[dynamicLink.NodeIds.Count - 1];
                        foreach (string nodeId in dynamicLink.NodeIds)
                        {
                            ;
                            if (!nodeId.Equals(lastNode))
                            {
                                if (!tempDynamicLink.ContainsKey(nodeId))
                                {
                                    List<PathEx> addPath = new List<PathEx>();
                                    addPath.Add(dynamicLink);
                                    tempDynamicLink.Add(nodeId, addPath);
                                }
                                else
                                {
                                    List<PathEx> addPath = tempDynamicLink[nodeId];
                                    bool alreadyAdd = false;
                                    foreach (PathEx path in addPath)
                                    {
                                        if (path.NodeIds.Equals(dynamicLink.NodeIds))
                                        {
                                            alreadyAdd = true;
                                            break;
                                        }
                                    }
                                    if (!alreadyAdd)
                                    {
                                        addPath.Add(dynamicLink);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (nextNodes.Count > 1)
                {//|| lastNodes.size()==0) {
                    foreach (LinkEx link in nextNodes)
                    {

                        //					String nextNodeId = link.getToNodeId();
                        //					List pathed = new ArrayList();
                        //					pathed.add(link.getFromNodeId());
                        //
                        //					List paths = new ArrayList();
                        //					paths.add(pathed);
                        //
                        //					Map<List<String>, Integer> pathMap = new HashMap<List<String>, Integer>();
                        //					pathMap.put(pathed, 0);
                        //
                        //					Map checkedPaths = new HashMap();
                        //					checkedPaths.put(nextNodeId, paths);
                        //dynamicLink = searchDynamicLink(link.getFromNodeId(), pathMap, linkFromNodeMap, checkedPaths);
                        dynamicLink = SearchDynamicLinkSimple(link);
                        //logger.warn(dynamicLink);
                        if (dynamicLink != null)
                        {
                            String lastNode = (String)dynamicLink.NodeIds[dynamicLink.NodeIds.Count - 1];
                            foreach (string nodeId in dynamicLink.NodeIds)
                            {
                                if (!nodeId.Equals(lastNode))
                                {
                                    if (!tempDynamicLink.ContainsKey(nodeId))
                                    {
                                        List<PathEx> addPath = new List<PathEx>();
                                        addPath.Add(dynamicLink);
                                        tempDynamicLink.Add(nodeId, addPath);
                                    }
                                    else
                                    {
                                        List<PathEx> addPath = tempDynamicLink[nodeId];
                                        bool alreadyAdd = false;
                                        foreach (PathEx path in addPath)
                                        {
                                            if (path.NodeIds.Equals(dynamicLink.NodeIds))
                                            {
                                                alreadyAdd = true;
                                                break;
                                            }
                                        }
                                        if (!alreadyAdd)
                                        {
                                            addPath.Add(dynamicLink);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            this.dynamicLink = tempDynamicLink;
            return true;
        }

        public bool SynchronizeLinkACS()
        {
            Dictionary<string, LinkEx> linkTemp = new Dictionary<string, LinkEx>();
            Dictionary<string, List<LinkEx>> linkMap = new Dictionary<string, List<LinkEx>>();
            Dictionary<string, List<LinkEx>> linkFromNodeMap = new Dictionary<string, List<LinkEx>>();
            Dictionary<string, List<LinkEx>> linkToNodeMap = new Dictionary<string, List<LinkEx>>();

            linkEx = this.ResourceManager.GetLinks();

            foreach (LinkEx link in linkEx)
            {
                //lys20200605 link.Id Duplicate Check 
                if (linkTemp.ContainsKey(link.Id))
                {
                    continue;
                }

                linkTemp.Add(link.Id, link);

                if (linkMap.ContainsKey(link.ToNodeId))
                {

                    List<LinkEx> previousNodes = (List<LinkEx>)linkMap[link.ToNodeId];
                    previousNodes.Add(link);
                }
                else
                {

                    List<LinkEx> newPreviousNodes = new List<LinkEx>();
                    newPreviousNodes.Add(link);
                    linkMap.Add(link.ToNodeId, newPreviousNodes);
                }

                if (linkFromNodeMap.ContainsKey(link.FromNodeId))
                {

                    List<LinkEx> previousNodes = linkFromNodeMap[link.FromNodeId];
                    previousNodes.Add(link);
                }
                else
                {

                    List<LinkEx> newPreviousNodes = new List<LinkEx>();
                    newPreviousNodes.Add(link);
                    linkFromNodeMap.Add(link.FromNodeId, newPreviousNodes);
                }

                if (linkToNodeMap.ContainsKey(link.ToNodeId))
                {

                    List<LinkEx> previousNodes = linkToNodeMap[link.ToNodeId];
                    previousNodes.Add(link);
                }
                else
                {

                    List<LinkEx> newPreviousNodes = new List<LinkEx>();
                    newPreviousNodes.Add(link);
                    linkToNodeMap.Add(link.ToNodeId, newPreviousNodes);
                }       
            }
            this.linkMap = linkTemp;
            this.convertLinksToMapByToNode = linkMap;
            this.linkFromNodeMap = linkFromNodeMap;
            this.linkToNodeMap = linkToNodeMap;
            return true;
        }

        public bool SynchronizeLinkViewACS()
        {
            Dictionary<string, List<LinkViewEx>> linkViewMapTemp = new Dictionary<string, List<LinkViewEx>>();
            Dictionary<string, List<LinkViewEx>> linkViewMapBayId = new Dictionary<string, List<LinkViewEx>>();
            Dictionary<string, List<LinkViewEx>> linkViewByFromNode = new Dictionary<string, List<LinkViewEx>>();
            List<LinkViewEx> linkViews = new List<LinkViewEx>();

            foreach (LinkZoneEx lz in LinkZoneEx)
            {
                LinkEx link = null;

                if (linkMap.ContainsKey(lz.LinkId))
                {
                    link = (LinkEx)this.linkMap[lz.LinkId];
                }

                //logger.warn("LinkZone:" + lz + ". Link: " + link);
                if (link != null)
                {
                    LinkViewEx lv = new LinkViewEx();

                    lv.Id = lz.LinkId;
                    lv.BayId = lz.ZoneId;
                    lv.TransferFlag = lz.TransferFlag;
                    lv.FromNodeId = link.FromNodeId;
                    lv.ToNodeId = link.ToNodeId;
                    lv.Length = link.Length;
                    lv.Speed = link.Speed;
                    lv.Availability = link.Availability;
                    lv.Load = link.Load;

                    linkViews.Add(lv);
                }
            }
            //		linkViewACS = this.resourceManager.getLinkViewACS();
            //		for(LinkViewACS linkView:linkViewACS){
            //			logger.warn(linkView);
            //		}
            this.linkViews = linkViews;
            foreach (LinkViewEx linkView in linkViews)
            {
                if (linkViewMapTemp.ContainsKey(linkView.Id))
                {
                    //linkViewMapTemp에 이미 추가된 linkView.Id 이면 해당 value에 linkView 추가 
                    List<LinkViewEx> linkViewInMap = linkViewMapTemp[linkView.Id];
                    linkViewInMap.Add(linkView);
                }
                else
                {
                    //linkViewMapTemp에 linkView.Id 없으면 key, value 모두 추가 
                    List<LinkViewEx> linkViewInMap = new List<LinkViewEx>();
                    linkViewInMap.Add(linkView);
                    linkViewMapTemp.Add(linkView.Id, linkViewInMap);
                }

                if (linkViewMapBayId.ContainsKey(linkView.BayId))
                {
                    List<LinkViewEx> linkViewInMap = linkViewMapBayId[linkView.BayId];
                    linkViewInMap.Add(linkView);
                }
                else
                {
                    List<LinkViewEx> linkViewInMap = new List<LinkViewEx>();
                    linkViewInMap.Add(linkView);
                    linkViewMapBayId.Add(linkView.BayId, linkViewInMap);
                }

                if (linkViewByFromNode.ContainsKey(linkView.FromNodeId))
                {
                    List<LinkViewEx> linkViewInMap = linkViewByFromNode[linkView.FromNodeId];
                    linkViewInMap.Add(linkView);
                }
                else
                {
                    List<LinkViewEx> linkViewInMap = new List<LinkViewEx>();
                    linkViewInMap.Add(linkView);
                    linkViewByFromNode.Add(linkView.FromNodeId, linkViewInMap);
                }
            }

            this.linkViewMap = linkViewMapTemp;
            this.linkViewMapByBayId = linkViewMapBayId;
            this.linkViewMapByFromNode = linkViewByFromNode;
            return true;

        }

        public bool SynchronizeLinkZoneACS()
        {
            Dictionary<string, List<LinkZoneEx>> linkZone = new Dictionary<string, List<LinkZoneEx>>();

            this.LinkZoneEx = this.ResourceManager.GetLinkZones();

            foreach (LinkZoneEx lz in LinkZoneEx)
            {
                if (linkZone.ContainsKey(lz.LinkId))
                {
                    List<LinkZoneEx> linkZoneMap = linkZone[lz.LinkId];
                    linkZoneMap.Add(lz);
                }
                else
                {
                    List<LinkZoneEx> linkZoneMap = new List<LinkZoneEx>();
                    linkZoneMap.Add(lz);
                    linkZone.Add(lz.LinkId, linkZoneMap);
                }
            }
            this.linkZoneMapByLink = linkZone;

            return true;
        }

        public bool SynchronizeLocationACS()
        {
            Dictionary<string, LocationEx> locById = new Dictionary<string, LocationEx>();
            Dictionary<string, LocationEx> locStation = new Dictionary<string, LocationEx>();

            locationEx = this.ResourceManager.GetLocations();

            foreach (LocationEx loc in locationEx)
            {
                try
                {
                    if (!locById.ContainsKey(loc.LocationId))
                        locById.Add(loc.LocationId, loc);

                    if (!locStation.ContainsKey(loc.StationId))
                        locStation.Add(loc.StationId, loc);
                }
                catch (Exception e)
                {

                }
            }

            this.locationMapByLocationId = locById;
            this.locationMapByStation = locStation;
            return true;
        }

        public bool SynchronizeLocationViewACS()
        {
            List<LocationViewEx> listLocView = new List<LocationViewEx>();

            Dictionary<string, List<LocationViewEx>> locViewPort = new Dictionary<string, List<LocationViewEx>>();
            Dictionary<string, List<LocationViewEx>> locViewStation = new Dictionary<string, List<LocationViewEx>>();
            Dictionary<string, List<LocationViewEx>> locViewCharge = new Dictionary<string, List<LocationViewEx>>();

            stationEx = this.ResourceManager.GetStations();

            foreach (StationEx st in stationEx)
            {
                LocationEx loc = null; List<LinkZoneEx> link_Zone = null;

                if (locationMapByStation.ContainsKey(st.Id))
                {
                    loc = (LocationEx)this.locationMapByStation[st.Id];
                }
                else
                {
                    loc = null;
                }

                if (linkZoneMapByLink.ContainsKey(st.LinkId))
                {
                    link_Zone = linkZoneMapByLink[st.LinkId];
                }
                else
                {
                    link_Zone = null;
                }


                if (loc != null && link_Zone != null)
                {
                    foreach (LinkZoneEx lz in link_Zone)
                    {
                        LocationViewEx locView = new LocationViewEx();
                        LinkEx link = null;

                        if (this.LinkMap.ContainsKey(lz.LinkId))
                        {
                            link = (LinkEx)this.linkMap[lz.LinkId];
                        }

                        if (link != null)
                        {
                            locView.LocationId = loc.LocationId;
                            locView.StationId = loc.StationId;
                            locView.BayId = lz.ZoneId;
                            locView.TransferFlag = lz.TransferFlag;
                            locView.Location_Type = loc.Type;
                            locView.CarrierType = loc.CarrierType;
                            locView.Direction = loc.Direction;
                            locView.State = loc.State;
                            locView.LinkId = lz.LinkId;
                            locView.ParentNode = link.FromNodeId;
                            locView.NextNode = link.ToNodeId;
                            locView.Station_type = st.Type;

                            if (!listLocView.Contains(locView))
                                listLocView.Add(locView);
                        }
                    }
                }
            }
            this.locationViews = listLocView;

            //locationViewACS = this.resourceManager.getLocationView();
            foreach (LocationViewEx loc in locationViews)
            {
                if (locViewPort.ContainsKey(loc.LocationId))
                {
                    List<LocationViewEx> locationViewInMap = locViewPort[loc.LocationId];
                    locationViewInMap.Add(loc);
                }
                else
                {
                    List<LocationViewEx> locationViewInMap = new List<LocationViewEx>();
                    locationViewInMap.Add(loc);
                    locViewPort.Add(loc.LocationId, locationViewInMap);
                }

                if (locViewStation.ContainsKey(loc.StationId))
                {
                    List<LocationViewEx> locationViewInMap = locViewStation[loc.StationId];
                    locationViewInMap.Add(loc);
                }
                else
                {
                    List<LocationViewEx> locationViewInMap = new List<LocationViewEx>();
                    locationViewInMap.Add(loc);
                    locViewStation.Add(loc.StationId, locationViewInMap);
                }

                if (locViewCharge.ContainsKey(loc.BayId))
                {
                    if (loc.TransferFlag.Equals("Y") && loc.Location_Type.Equals(LocationEx.TYPE_CHARGE))
                    {
                        List<LocationViewEx> locationViewInMap = locViewCharge[loc.BayId];
                        locationViewInMap.Add(loc);
                    }
                }
                else
                {
                    if (loc.TransferFlag.Equals("Y") && loc.Location_Type.Equals(LocationEx.TYPE_CHARGE))
                    {
                        List<LocationViewEx> locationViewInMap = new List<LocationViewEx>();
                        locationViewInMap.Add(loc);
                        locViewCharge.Add(loc.BayId, locationViewInMap);
                    }
                }
            }

            this.locationViewMapByLocationId = locViewPort;
            this.locationViewMapByStation = locViewStation;
            this.locationViewCharge = locViewCharge;
            //		for(Map.Entry<String, List> a : locationViewCharge.entrySet()){
            //			logger.warn("Bayid " + a.getKey() + ". Charge:" + a.getValue());
            //		}
            return true;
        }

        public bool SynchronizeNodeACS()
        {
            Dictionary<String, NodeEx> nodeMap = new Dictionary<string, NodeEx>();

            this.nodeEx = this.ResourceManager.GetNodes();

            foreach (NodeEx node in this.nodeEx)
            {
                nodeMap.Add(node.Id, node);
            }

            this.nodeExMap = nodeMap;
            return true;
        }

        public bool SynchronizeStationACS()
        {
            Dictionary<string, StationEx> stationMap = new Dictionary<string, StationEx>();

            this.stationEx = this.ResourceManager.GetStations();

            try
            {
                foreach (StationEx station in stationEx)
                {
                    if(!stationMap.ContainsKey(station.Id))
                        stationMap.Add(station.Id, station);
                }
            }
            catch(Exception e)
            {

            }
            this.stationMap = stationMap;

            return true;
        }
    }
}
