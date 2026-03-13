using Autofac;
using ACS.Framework.Message.Model;
using ACS.Framework.Resource.Model;
using ACS.Framework.Transfer.Model;
using ACS.Service;
using ACS.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ACS.Biz.Trans
{
    public class RAIL_VEHICLEOCODESEPERATOR : BaseBizJob
    {
        public InterfaceServiceEx InterfaceService;
        public ResourceServiceEx ResourceService;
        public MaterialServiceEx MaterialService;
        public TransferServiceEx TransferService;
        public AlarmServiceEx AlarmService;
        private IWorkflowManager WorkflowManager;
        public Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        {
            get { return commandJobList; }
            set { commandJobList = value; }
        }

        public override int ExecuteJob(object[] args)
        {
            XmlDocument rail_vehicleocodeseperator = (XmlDocument)args[0];
            VehicleMessageEx vehicleMsg;

            InterfaceService = LifetimeScope.Resolve<InterfaceServiceEx>();
            ResourceService = LifetimeScope.Resolve<ResourceServiceEx>();
            MaterialService = LifetimeScope.Resolve<MaterialServiceEx>();
            TransferService = LifetimeScope.Resolve<TransferServiceEx>();
            AlarmService = LifetimeScope.Resolve<AlarmServiceEx>();
            WorkflowManager = LifetimeScope.Resolve<IWorkflowManager>();

            vehicleMsg = InterfaceService.CreateVehicleMessageFromES(rail_vehicleocodeseperator);

            if (ResourceService.CheckVehicle(vehicleMsg))
            {
                if (InterfaceService.CheckTransportCommand(vehicleMsg))
                {
                    //Loading 보고 진행
                    if (vehicleMsg.Vehicle.TransferState == VehicleEx.TRANSFERSTATE_ASSIGNED && vehicleMsg.TransportCommand.State == TransportCommandEx.STATE_ASSIGNED)
                    {
                        XmlElement header = rail_vehicleocodeseperator.DocumentElement["HEADER"];
                        foreach (XmlNode node in header.ChildNodes)
                        {
                            if (string.Equals(node.Name, "MESSAGENAME")) { node.InnerText = "RAIL-VEHICLEACQUIRECOMPLETED"; break; }                           
                        }

                        this.WorkflowManager.Execute("RAIL-VEHICLEACQUIRECOMPLETED", rail_vehicleocodeseperator);
                    }                                                                       
                    //Unloading 보고 진행
                    else if (vehicleMsg.Vehicle.TransferState == VehicleEx.TRANSFERSTATE_ACQUIRE_COMPLETE && vehicleMsg.TransportCommand.State == TransportCommandEx.STATE_TRANSFERRING_DEST)
                    {
                        XmlElement header = rail_vehicleocodeseperator.DocumentElement["HEADER"];
                        foreach (XmlNode node in header.ChildNodes)
                        {
                            if (string.Equals(node.Name, "MESSAGENAME")) { node.InnerText = "RAIL-VEHICLEDEPOSITCOMPLETED"; break; }
                        }

                        this.WorkflowManager.Execute("RAIL-VEHICLEDEPOSITCOMPLETED", rail_vehicleocodeseperator);
                    }
                    else
                    {
                        //O 코드의 조건에 부합하지 않음.
                        
                    }
                    return 0;

                }               
            }
            else
            {
                return 0;
            }
            return 0;
        }
    }
}
