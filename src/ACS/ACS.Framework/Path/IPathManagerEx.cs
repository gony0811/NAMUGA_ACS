using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Resource.Model;
using ACS.Framework.Path.Model;
using System.Collections;

namespace ACS.Framework.Path
{
    public interface IPathManagerEx
    {
        VehicleEx SearchSuitableVehicle(LocationEx paramLocation);

        VehicleEx SearchSuitableVehicle(LocationEx paramLocation, string paramString);
        VehicleEx SearchSuitableChargeVehicle(string paramString);
        VehicleEx SearchSuitableChargeVehicle(string paramString, LocationEx paramLocation);
        VehicleEx SearchSuitableVehicle(StationEx paramStation, bool paramboolean1, List<string> paramList, Boolean paramBoolean2);

        VehicleEx SearchSuitableVehicle(StationEx paramStation, string paramString, bool paramBoolean);

        PathInfoEx SearchPaths(LocationEx paramLocationACS1, LocationEx paramLocationACS2);
                 
        PathInfoEx SearchPaths(NodeEx paramNodeACS, LocationEx paramLocationACS);
                 
        PathInfoEx SearchPaths(StationEx paramStation1, StationEx paramStation2);

        PathInfoEx SearchDynamicPaths(StationEx paramStationACS, NodeEx paramNodeACS1, NodeEx paramNodeACS2, bool paramBoolean1, List<string> paramList, bool paramBoolean2);
        NodeEx SearchNodeByStationAsSource(StationEx paramStation);

        NodeEx SearchNodeByStationAsDest(StationEx paramStation);

        LinkEx GetLinkByStation(StationEx paramStation);

        LinkEx GetLink(string paramString);

        NodeEx GetNode(string paramString);

        StationEx GetStation(string paramString);

        StationViewEx GetStationView(string paramString);

        IList GetLinkViews(string paramString);

        IList GetLinkZoneByFromNodeId(string paramString);

        IList GetLinkViewsByFromNodeId(string paramString);
        IList GetLinkZoneByFromBayId(string paramString);

        LocationViewEx GetLocationView(string paramString);

        LocationViewEx GetLocationViewByStationId(string paramString);
        IList GetLocationViewsByBayId(string paramString);
        IList GetChargeLocationViewsByBayId(string paramString);
        IList GetStockLocationViewsByBayId(string paramString);
        LocationEx GetLocationByStationId(string paramString);
        LocationEx GetLocationByPortId(string paramString);
        string GetCommonUseBayIdBySourceDest(string paramString1, string paramString2, string paramString3);
        bool IsSameBayId(string paramString1, string paramString2, string paramString3);
        IList GetLinkViewsByBayId(bool flag, String s);
        IList GetVehicles(string paramString, bool paramBoolean);
        Dictionary<string, IList> ConvertLinkViewsToMapByToNode(IList paramList);

        Dictionary<string, IList> ConvertLinkViewsToMapByFromNode(IList paramList);


    }
}
