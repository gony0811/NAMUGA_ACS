using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Path.Model;
using ACS.Framework.Resource.Model;

namespace ACS.Framework.Cache
{
    public interface ICacheManagerEx
    {
        bool Synchronize();

        bool SynchronizeLinkACS();
        bool SynchronizeLinkViewACS();
        bool SynchronizeNodeACS();
        bool SynchronizeLocationACS();
        bool SynchronizeStationACS();
        bool SynchronizeLocationViewACS();
        bool SynchronizeLinkZoneACS();
        bool SynchronizeDynamicLink();

        List<LinkEx> GetLinkACS();
        List<LinkEx> GetLinkByFromNodeId(string nodeId);
        List<LinkEx> GetLinkByToNodeId(string nodeId);
        List<LinkViewEx> GetLinkViewACS();
        List<LinkViewEx> GetLinkViewByBayId(string bayId);
        List<LinkViewEx> GetLinkViewsByFromNodeId(string fromNodeId);
        List<LinkViewEx> GetLinkViewsByFromNodeId(string fromNodeId, string bayId);
        List<NodeEx> GetNodeACS();
        List<LocationEx> GetLocationACS();
        List<StationEx> GetStationACS();
        List<LocationViewEx> GetLocationViewACS();

        Dictionary<string, List<LinkEx>> ConvertLinksToMapByToNode();
        Dictionary<string, List<LinkEx>> ConvertLinksToMapByFromNode();

        LinkEx GetLinkById(string linkId);
        LinkViewEx GetLinkViewById(string linkViewId);

        NodeEx GetNode(string nodeId);
        NodeEx GetNodeByStation(StationEx station);
        LocationEx GetLocationByPortId(string portId);
        LocationEx GetLocation(string portId);
        LocationEx GetLocationByStationId(string stationId);
        StationEx GetStationById(string stationId);
        List<LocationViewEx> GetLocationViewByPortId(string portId);
        LocationViewEx GetBayByLocationViewByPortId(string portId);
        LocationViewEx GetBayByLocationViewByStationId(string stationId);
        List<LocationViewEx> GetLocationViewByStationId(string stationId);
        LocationViewEx GetLocationViewByStationIdAndBayId(string stationId, string bayId);
        LocationViewEx GetLocationViewChargeByStationIdAndBayId(string stationId, string bayId);

        List<LocationViewEx> GetChargeLocationViewsByBayId(string bayId);

        List<PathEx> GetGroupPathByNode(string nodeId);

        bool CheckLocation(string locationId);
    }
}
