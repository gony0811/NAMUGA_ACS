using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Material.Model;
using ACS.Framework.Message.Model.Server;
using ACS.Framework.Resource.Model.Factory.Machine;
using ACS.Framework.Resource.Model.Factory.Unit;
using ACS.Framework.Resource.Model.Factory.Zone;
using ACS.Framework.Route.Model;
using ACS.Framework.Route.Model.Dynamic;
using ACS.Framework.Route.Model.ProcessType;
using ACS.Framework.Transfer.Model;

namespace ACS.Framework.Route
{
    public interface IRouteManager
    {
        void SearchTransportMachine(TransferMessage paramTransferMessage);

        RouteInfo SearchRoutes(TransferMessage paramTransferMessage);

        RouteInfo SearchDynamicRoutes(TransferMessage paramTransferMessage);

        RouteInfo SearchRoutes(Machine paramMachine1, Unit paramUnit1, Machine paramMachine2, Zone paramZone, Unit paramUnit2, Carrier paramCarrier);

        RouteInfo SearchRoutes(String paramString1, String paramString2, String paramString3, String paramString4, String paramString5, String paramString6);

        RouteInfo SearchFixedRoutes(TransferMessage paramTransferMessage);

        RouteInfo SearchInternalRoutes(TransferMessage paramTransferMessage);

        RouteInfo SearchAlternateRoutes(TransferMessage paramTransferMessage);

        Model.Route SearchForeTransferBestRoute(TransferMessage paramTransferMessage);

        Model.Route GetBestRoute(TransferMessage paramTransferMessage);

        Model.Route GetBestRoute(RouteInfo paramRouteInfo, bool paramBoolean);

        InternalRoute GetBestInternalRoute(TransferMessage paramTransferMessage);

        void SetBestRoute(TransferMessage paramTransferMessage, Model.Route paramRoute);

        Machine GetTransportMachine(String paramString);

        Machine GetTransportMachine(Unit paramUnit);

        Machine GetTransportMachine(String paramString1, String paramString2);

        Machine GetTransportMachine(Unit paramUnit, String paramString);

        TransportMachine GetTransportMachine(Machine paramMachine, Unit paramUnit);

        TransportMachine GetTransportMachine(String paramString, Machine paramMachine, Unit paramUnit);

        TransportMachine GetTransportMachineByUnit(String paramString1, String paramString2);

        TransportMachine GetTransportMachineByUnit(Unit paramUnit);

        TransportMachine GetTransportMachineByUnit(String paramString1, String paramString2, String paramString3);

        TransportMachine GetTransportMachineByUnit(String paramString, Unit paramUnit);

        TransportMachine GetTransportMachineByPort(String paramString1, String paramString2);

        TransportMachine GetTransportMachineByPort(Port paramPort);

        TransportMachine GetTransportMachineByPort(String paramString1, String paramString2, String paramString3);

        TransportMachine GetTransportMachineByPort(String paramString, Port paramPort);

        //TransportMachine GetTransportMachineByIntraNode(String paramString1, String paramString2);

        //TransportMachine GetTransportMachineByIntraNode(Port paramPort);

        //ArrayList GetTransportMachinesByIntraNode(String paramString1, String paramString2);

        //ArrayList GetTransportMachinesByIntraNode(Port paramPort);

        //ArrayList GetTransportMachinesByIntraNode(Machine paramMachine);

        //ArrayList GetTransportMachinesByIntraNode(String paramString);
        
        TransportMachine GetTransportMachineAsSource(String paramString, Machine paramMachine, Unit paramUnit);

        TransportMachine GetTransportMachineAsSource(String paramString, Machine paramMachine, Unit paramUnit, Carrier paramCarrier);

        TransportMachine GetTransportMachineAsCurrent(Machine paramMachine, Unit paramUnit, Carrier paramCarrier);
                   
        TransportMachine GetTransportMachineAsDest(Machine paramMachine, Unit paramUnit);

        //TransportMachine GetTransportMachineAsDest(Machine paramMachine, Zone paramZone, Unit paramUnit, String paramString);

        //TransportMachine GetTransportMachineAsDest(Machine paramMachine, Zone paramZone);        
        
        void CalculateCurrentExpectedProcessedRoutes(TransportJob paramTransportJob, Model.Route paramRoute);

        void CalculateCurrentExpectedRoutes(TransportJob paramTransportJob, Model.Route paramRoute);

        void CalculateProcessedRoutes(TransportJob paramTransportJob, TransportCommand paramTransportCommand);

        String ToPrettyFormat(RouteInfo paramRouteInfo);

        String ChangeMessageNameToGetCurrentTransportMachineTemporarily(TransferMessage paramTransferMessage);

        void CreateSingleNode(SingleNode paramSingleNode);

        SingleNode CreateSingleNode(String paramString, int paramInt);

        SingleNode CreateSingleNode(String paramString);

        void CreateSingleNodes(ArrayList paramList);

        void CreateDefaultSingleNodes();

        ArrayList GetSingleNodes();

        int GetSingleNodeCount();

        ArrayList GetSingleNodesByExample(SingleNode paramSingleNode);

        SingleNode GetSingleNode(String paramString);

        SingleNode GetSingleNode(TransportMachine paramTransportMachine);

        bool ExistSingleNode(String paramString);

        bool ExistSingleNode(TransportMachine paramTransportMachine);

        void UpdateSingleNode(SingleNode paramSingleNode);

        void DeleteSingleNode(SingleNode paramSingleNode);

        int DeleteSingleNodes();

        void CreateInterNode(InterNode paramInterNode);

        InterNode CreateInterNode(String paramString1, String paramString2, String paramString3, String paramString4, String paramString5, int paramInt1, int paramInt2, int paramInt3);

        void CreateInterNodes(ArrayList paramList);

        ArrayList GetInterNodes();

        int GetInterNodeCount();

        ArrayList GetInterNodesByExample(InterNode paramInterNode);

        ArrayList GetInterNodesByFromMachine(String paramString);

        ArrayList GetInterNodesByToMachine(String paramString);

        InterNode GetInterNode(String paramString1, String paramString2);

        void UpdateInterNode(InterNode paramInterNode);

        void UpdateInterNode(InterNode paramInterNode, Dictionary<string, object> paramMap);

        int DecoupleInterNodesByFromMachine(String paramString);

        int DecoupleInterNodesByToMachine(String paramString);

        int DecoupleInterNodes(String paramString);

        int DecoupleInterNodes(String paramString1, String paramString2);

        int CoupleInterNodesByFromMachine(String paramString);

        int CoupleInterNodesByToMachine(String paramString);

        int CoupleInterNodes(String paramString);

        int CoupleInterNodes(String paramString1, String paramString2);

        void ChangeInterNodeByMachine(Machine paramMachine);

        void ChangeInterNodeByPort(Port paramPort);

        void DeleteInterNode(InterNode paramInterNode);

        int DeleteInterNodes();

        void CreateInterZoneNode(InterZoneNode paramInterZoneNode);

        InterZoneNode CreateInterZoneNode(String paramString1, String paramString2, String paramString3, String paramString4);

        void CreateInterZoneNodes(ArrayList paramList);

        ArrayList GetInterZoneNodes();

        int GetInterZoneNodeCount();

        ArrayList GetInterZoneNodesByFromZone(String paramString);

        ArrayList GetInterZoneNodesByFromZoneAndTransportMachine(String paramString1, String paramString2);

        ArrayList GetInterZoneNodesByToZone(String paramString);

        ArrayList GetInterZoneNodesByToZoneAndTransportMachine(String paramString1, String paramString2);

        ArrayList GetInterZoneNodesByTransportMachine(String paramString);

        InterZoneNode GetInterZoneNode(String paramString1, String paramString2, String paramString3);

        void UpdateInterZoneNode(InterZoneNode paramInterZoneNode);

        void UpdateInterZoneNode(InterZoneNode paramInterZoneNode, IDictionary paramMap);

        int UpdateInterZoneNodes(String paramString1, String paramString2);

        int UpdateLinkedZoneNodesByZoneName(String paramString1, String paramString2);

        int UpdateLinkedZoneNodesByLinkedZoneName(String paramString1, String paramString2);

        void DeleteInterZoneNode(InterZoneNode paramInterZoneNode);

        int DeleteInterZoneNodes();

        void InitInterZoneNodes();

        void ChangeInterZoneNodeByPort(Port paramPort);

        void ChangeInterZoneNodeByTransportUnit(Unit paramUnit);

        void ChangeInterZoneNodeByZone(Zone paramZone);

        void CreateLinkedZoneNode(LinkedZoneNode paramLinkedZoneNode);

        LinkedZoneNode CreateLinkedZoneNode(String paramString1, String paramString2, String paramString3, String paramString4);

        void CreateLinkedZoneNodes(ArrayList paramList);

        ArrayList GetLinkedZoneNodes();

        ArrayList GetLinkedZoneNodesByMachine(String paramString);

        ArrayList GetLinkedZoneNodesByMachineAndLinkedMachine(String paramString1, String paramString2);

        LinkedZoneNode GetLinkedZoneNodesByMachineAndZoneAndLinkedMachine(String paramString1, String paramString2, String paramString3, bool paramBoolean);

        LinkedZoneNode GetLinkedZoneNodeByLinkedMachineAndLinkedZone(String paramString1, String paramString2, String paramString3, bool paramBoolean);

        void UpdateLinkedZoneNode(LinkedZoneNode paramLinkedZoneNode);

        void UpdateLinkedZoneNode(LinkedZoneNode paramLinkedZoneNode, IDictionary paramMap);

        void DeleteLinkedZoneNode(LinkedZoneNode paramLinkedZoneNode);

        int DeleteLinkedZoneNodes();

        void InitLinkedZoneNodes();

        void CreateTripleNode(TripleNode paramTripleNode);

        TripleNode CreateTripleNode(String paramString1, String paramString2, String paramString3, int paramInt);

        void CreateTripleNodes(ArrayList paramList);

        void CreateDefaultTripleNodes();

        TripleNode GetTripleNode(String paramString);

        ArrayList GetTripleNodes();

        int GetTripleNodeCount();

        ArrayList GetTripleNodesByExample(TripleNode paramTripleNode);

        ArrayList GetTripleNodesByFromMachine(String paramString);

        ArrayList GetTripleNodesByToMachine(String paramString);

        ArrayList GetTripleNodesByTransportMachine(String paramString);

        TripleNode GetTripleNode(String paramString1, String paramString2, String paramString3);

        bool ExistTripleNode(String paramString);

        void UpdateTripleNode(TripleNode paramTripleNode);

        void DeleteTripleNode(TripleNode paramTripleNode);

        int DeleteTripleNodes();

        void CreateIntraNode(IntraNode paramIntraNode);

        ArrayList GetIntraNodes();

        int GetIntraNodeCount();

        ArrayList GetIntraNodesByExample(IntraNode paramIntraNode);

        ArrayList GetIntraNodesByMachine(String paramString);




        ArrayList GetIntraNodesByUnit(String paramString);

        ArrayList GetIntraNodesByUnit(String paramString1, String paramString2);

        ArrayList GetIntraNodeTransportMachineNamesByUnit(String paramString1, String paramString2);

        ArrayList GetIntraNodesByTransportMachine(String paramString);

        ArrayList GetIntraNodes(String paramString1, String paramString2);

        ArrayList GetIntraNodes(String paramString);

        ArrayList GetIntraNodes(TransportMachine paramTransportMachine);

        ArrayList GetIntraNodesByTransportMachineNames(ArrayList paramList);

        IntraNode GetIntraNodeByUnitNameAndTransportMachineName(String paramString1, String paramString2);

        IntraNode GetIntraNodeByUnitNameAndTransportMachineNames(String paramString, ArrayList paramList);

        IntraNode GetIntraNodeByUnitName(String paramString, ArrayList paramList);

        Dictionary<string, string> TransformIntraNodesByUnitName(ArrayList paramList);

        void UpdateIntraNode(IntraNode paramIntraNode);

        void DeleteIntraNode(IntraNode paramIntraNode);

        int DeleteIntraNodes();

        void CoupleInterNodeByPort(Port paramPort);

        void DecoupleInterNodeByPort(Port paramPort);

        void DecoupleInterNodesByMachine(Machine paramMachine);

        void CoupleInterNodesByMachine(Machine paramMachine);

        void CreateFixedRoute(FixedRoute paramFixedRoute);

        ArrayList GetFixedRoutes();

        int GetFixedRouteCount();

        ArrayList GetFixedRoutes(String paramString1, String paramString2);

        ArrayList GetFixedRoutes(Machine paramMachine1, Machine paramMachine2);

        bool IsTransportJobFixedRoute(TransportJob paramTransportJob);

        void UpdateFixedRoute(FixedRoute paramFixedRoute);

        void DeleteFixedRoute(FixedRoute paramFixedRoute);

        int DeleteFixedRoutes(String paramString1, String paramString2);

        int DeleteFixedRoutes(Machine paramMachine1, Machine paramMachine2);

        int DeleteFixedRoutes();

        void CreateBalanceLoad(BalanceLoad paramBalanceLoad);

        BalanceLoad GetBalanceLoad(String paramString1, String paramString2, String paramString3);

        BalanceLoad GetBalanceLoadByTransportMachine(TransportMachine paramTransportMachine, Unit paramUnit);

        BalanceLoad GetBalanceLoadByTransportMachine(String paramString1, String paramString2);

        BalanceLoad GetBalanceLoadByNextTransportMachine(TransportMachine paramTransportMachine, Unit paramUnit);

        BalanceLoad GetBalanceLoadByNextTransportMachine(String paramString1, String paramString2);

        void UpdateBalanceLoad(BalanceLoad paramBalanceLoad);

        ArrayList GetBalanceLoads();

        void DeleteBalanceLoad(BalanceLoad paramBalanceLoad);

        int DeleteBalanceLoads();

        bool BalanceLoad(TransportCommand paramTransportCommand);

        void CreateDynamicLoad(DynamicLoad paramDynamicLoad);

        DynamicLoad GetDynamicLoad(String paramString);

        DynamicLoad GetDynamicLoad(TransportMachine paramTransportMachine);

        void UpdateDynamicLoad(DynamicLoad paramDynamicLoad);

        ArrayList GetDynamicLoads();

        void DeleteDynamicLoad(DynamicLoad paramDynamicLoad);

        int DeleteDynamicLoads();

        bool DynamicLoad(TransportCommand paramTransportCommand);

        void CreateHeuristicDelay(HeuristicDelay paramHeuristicDelay);

        HeuristicDelay GetHeuristicDelay(String paramString1, String paramString2, String paramString3);

        ArrayList GetHeuristicDelaysByTransportMachine(TransportMachine paramTransportMachine);

        ArrayList GetHeuristicDelaysByTransportMachine(String paramString);

        void UpdateHeuristicDelay(HeuristicDelay paramHeuristicDelay);

        ArrayList GetHeuristicDelays();

        void DeleteHeuristicDelay(HeuristicDelay paramHeuristicDelay);

        int DeleteHeuristicDelays();

        bool HeuristicDelay(TransportCommand paramTransportCommand);

        bool HeuristicDelay(String paramString1, String paramString2, String paramString3);

        bool HeuristicDelay(String paramString);

        bool HeuristicDelay(HeuristicDelay paramHeuristicDelay);

        bool HeuristicDelay();

        void CreateDefaultProcessTypes();

        void CreateProcessType(ProcessType paramProcessType);

        ProcessType CreateProcessType(String paramString);

        ProcessType GetProcessType(String paramString);

        ArrayList GetProcessTypes();

        ArrayList GetProcessTypeNames();

        bool ExistProcessType(String paramString);

        void DeleteProcessType(ProcessType paramProcessType);

        int DeleteProcessType(String paramString);

        void CreateCarrierProcessType(CarrierProcessType paramCarrierProcessType);

        CarrierProcessType CreateCarrierProcessType(String paramString1, String paramString2);

        CarrierProcessType CreateCarrierProcessType(Carrier paramCarrier, ProcessType paramProcessType);

        CarrierProcessType CreateCarrierProcessType(String paramString1, String paramString2, String paramString3);

        CarrierProcessType CreateCarrierProcessType(String paramString, Carrier paramCarrier, ProcessType paramProcessType);

        ArrayList CreateCarrierProcessTypeNamesByLocation(Carrier paramCarrier);

        ArrayList CreateCarrierProcessTypesByCurrentLocation(Carrier paramCarrier);

        ArrayList CreateCarrierProcessTypesByCarrierName(Carrier paramCarrier);

        ArrayList CreateCarrierProcessType(Carrier paramCarrier);

        ArrayList GetCarrierProcessTypes(String paramString);

        ArrayList GetCarrierProcessTypes(Carrier paramCarrier);

        ArrayList GetCarrierProcessTypes();

        ArrayList GetCarrierProcessTypesByCarrierNames(String[] paramArrayOfString);

        ArrayList GetCarrierProcessTypeNames(String paramString);

        ArrayList GetCarrierProcessTypeNames(Carrier paramCarrier);

        ArrayList GetCarrierProcessTypeNames();

        bool IsCarrierProcessTypeAll(ArrayList paramList);

        void DeleteCarrierProcessType(CarrierProcessType paramCarrierProcessType);

        int DeleteCarrierProcessType(String paramString1, String paramString2);

        int DeleteCarrierProcessType(Carrier paramCarrier, ProcessType paramProcessType);

        int DeleteCarrierProcessTypes(String paramString);

        int DeleteCarrierProcessTypes(Carrier paramCarrier);

        int DeleteCarrierProcessTypesByProcessTypeName(String paramString);

        void CreateMachineProcessType(MachineProcessType paramMachineProcessType);

        MachineProcessType CreateMachineProcessType(String paramString1, String paramString2);

        MachineProcessType CreateMachineProcessType(Machine paramMachine, ProcessType paramProcessType);

        MachineProcessType CreateMachineProcessType(Machine paramMachine, String paramString);

        MachineProcessType CreateMachineProcessType(String paramString1, String paramString2, String paramString3);

        MachineProcessType CreateMachineProcessType(String paramString, Machine paramMachine, ProcessType paramProcessType);

        MachineProcessType CreateMachineProcessType(String paramString1, Machine paramMachine, String paramString2);

        ArrayList GetMachineProcessTypes(String paramString);

        ArrayList GetMachineProcessTypes(Machine paramMachine);

        ArrayList GetMachineProcessTypes();

        ArrayList GetMachineProcessTypeNames(String paramString);

        ArrayList GetMachineProcessTypeNames(Machine paramMachine);

        ArrayList GetMachineProcessTypeNames();

        ArrayList GetMachineProcessTypesByMachineNames(String[] paramArrayOfString);

        bool IsProcessTypeAcceptable(ArrayList paramList, Machine paramMachine);

        void DeleteMachineProcessType(MachineProcessType paramMachineProcessType);

        int DeleteMachineProcessType(String paramString1, String paramString2);

        int DeleteMachineProcessType(Machine paramMachine, ProcessType paramProcessType);

        int DeleteMachineProcessTypes(String paramString);

        int DeleteMachineProcessTypes(Machine paramMachine);

        int DeleteMachineProcessTypesByProcessTypeName(String paramString);

        void CreateUnitProcessType(UnitProcessType paramUnitProcessType);

        UnitProcessType CreateUnitProcessType(String paramString1, String paramString2, String paramString3);

        UnitProcessType CreateUnitProcessType(Unit paramUnit, ProcessType paramProcessType);

        UnitProcessType CreateUnitProcessType(Unit paramUnit, String paramString);

        UnitProcessType CreateUnitProcessType(String paramString1, String paramString2, String paramString3, String paramString4);

        UnitProcessType CreateUnitProcessType(String paramString, Unit paramUnit, ProcessType paramProcessType);

        UnitProcessType CreateUnitProcessType(String paramString1, Unit paramUnit, String paramString2);

        ArrayList GetUnitProcessTypes(String paramString1, String paramString2);

        ArrayList GetUnitProcessTypes(Unit paramUnit);

        ArrayList GetUnitProcessTypes();

        ArrayList GetUnitProcessTypeNames(String paramString1, String paramString2);

        ArrayList GetUnitProcessTypeNames(Unit paramUnit);

        ArrayList GetUnitProcessTypesByUnitNames(String[] paramArrayOfString, String paramString);

        ArrayList GetUnitProcessTypeNames(String paramString1, String paramString2, bool paramBoolean);

        ArrayList GetUnitProcessTypeNames(Unit paramUnit, bool paramBoolean);

        bool IsProcessTypeAcceptable(ArrayList paramList, Unit paramUnit, bool paramBoolean);

        ArrayList GetUnitProcessTypeNames();

        void DeleteUnitProcessType(UnitProcessType paramUnitProcessType);

        int DeleteUnitProcessType(String paramString1, String paramString2, String paramString3);

        int DeleteUnitProcessType(Unit paramUnit, ProcessType paramProcessType);

        int DeleteUnitProcessTypes(String paramString1, String paramString2);

        int DeleteUnitProcessTypes(Unit paramUnit);

        int DeleteUnitProcessTypesByProcessTypeName(String paramString);

        void CreateZoneProcessType(ZoneProcessType paramZoneProcessType);

        ZoneProcessType CreateZoneProcessType(String paramString1, String paramString2, String paramString3);

        ZoneProcessType CreateZoneProcessType(Zone paramZone, ProcessType paramProcessType);

        ZoneProcessType CreateZoneProcessType(Zone paramZone, String paramString);

        ZoneProcessType CreateZoneProcessType(String paramString1, String paramString2, String paramString3, String paramString4);

        ZoneProcessType CreateZoneProcessType(String paramString, Zone paramZone, ProcessType paramProcessType);

        ZoneProcessType CreateZoneProcessType(String paramString1, Zone paramZone, String paramString2);

        ArrayList GetZoneProcessTypes(String paramString1, String paramString2);

        ArrayList GetZoneProcessTypes(Zone paramZone);

        ArrayList GetZoneProcessTypes();

        ArrayList GetZoneProcessTypesByZoneNames(String[] paramArrayOfString, String paramString);

        ArrayList GetZoneProcessTypeNames(String paramString1, String paramString2);

        ArrayList GetZoneProcessTypeNames(Zone paramZone);

        ArrayList GetZoneProcessTypeNames(String paramString1, String paramString2, bool paramBoolean);

        ArrayList GetZoneProcessTypeNames(Zone paramZone, bool paramBoolean);

        bool IsProcessTypeAcceptable(ArrayList paramList, Zone paramZone, bool paramBoolean);

        bool IsProcessTypeAcceptable(ArrayList paramList, String paramString1, String paramString2, bool paramBoolean);

        ArrayList getZoneProcessTypeNames();

        void deleteZoneProcessType(ZoneProcessType paramZoneProcessType);

        int DeleteZoneProcessType(String paramString1, String paramString2, String paramString3);

        int DeleteZoneProcessType(Zone paramZone, ProcessType paramProcessType);

        int DeleteZoneProcessTypes(String paramString1, String paramString2);

        int DeleteZoneProcessTypes(Zone paramZone);

        int DeleteZoneProcessTypesByProcessTypeName(String paramString);

        void UpdateHeuristicBestRoute(Model.Route paramRoute);

        void UpdateHeuristicBestRoute(String paramString1, String paramString2, ArrayList paramList, int paramInt);
    }
}
