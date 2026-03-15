using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Runtime.Serialization;
using ACS.Core.Base;
using ACS.Core.History;
using ACS.Core.Material;
using ACS.Core.Application;
using ACS.Core.Transfer.Model;
using ACS.Core.Resource;
using ACS.Core.Message.Model;
using ACS.Core.Transfer;
using ACS.Utility;
using ACS.Core.Transfer.Model;

namespace ACS.Manager.Transfer
{
    public class TransferManagerExImplement : AbstractManager, ITransferManagerEx
    {
        public Lazy<IResourceManagerEx> ResourceManager { get; set; }
        public IApplicationManager ApplicationManager { get; set; }
        public IMaterialManagerEx MaterialManager { get; set; }

     
        public IHistoryManagerEx HistoryManager { get; set; }

        private DateTime CreateTime = DateTime.Now;

        public void CreateTransportCommand(TransportCommandEx transportCommand)
        {
            this.PersistentDao.Save(transportCommand);
            //logger.info("transportCommand{" + transportCommand.getId() + "} was created, " + transportCommand);
        }

        public TransportCommandEx CreateTransportCommand(TransferMessageEx transferMessage)
        {
            TransportCommandEx transportCommand = new TransportCommandEx();

            transportCommand.Id = transferMessage.TransportCommandId;
            transportCommand.CarrierId = transferMessage.CarrierId;
            transportCommand.Source = (transferMessage.SourceMachine + ":" + transferMessage.SourceUnit);
            transportCommand.Dest = (transferMessage.DestMachine + ":" + transferMessage.DestUnit);
            transportCommand.Priority = transferMessage.Priority;

            transportCommand.EqpId = (transferMessage.EqpId);
            transportCommand.PortId = (transferMessage.PortId);
            transportCommand.AgvName = (transferMessage.AgvName);
            transportCommand.JobType = (transferMessage.JobType);
            transportCommand.MidLoc = (transferMessage.MidLoc);
            transportCommand.MidPortId = (transferMessage.MidPortId);
            transportCommand.OriginLoc = (transferMessage.OriginLoc);
            transportCommand.Description = (transferMessage.Description);
            transportCommand.CreateTime = DateTime.Now;
            transportCommand.CompletedTime = null;


            transportCommand.AssignedTime = null;
            transportCommand.CompletedTime = null;
            transportCommand.LoadArrivedTime = null;
            transportCommand.LoadingTime = null;
            transportCommand.QueuedTime = null;
            transportCommand.StartedTime = null;
            transportCommand.UnloadArrivedTime = null;
            transportCommand.UnloadedTime = null;
            transportCommand.UnloadingTime = null;
            transportCommand.LoadedTime = null;





            transportCommand.BayId = transferMessage.BayId;

            CreateTransportCommand(transportCommand);

            return transportCommand;
        }

        public TransportCommandEx CreateRechargeTransportCommand(TransportCommandEx transportCommand)
        {
            CreateTransportCommand(transportCommand);

            return transportCommand;
        }

        public TransportCommandEx CreateStockStationTransportCommand(TransportCommandEx transportCommand)
        {
            CreateTransportCommand(transportCommand);

            return transportCommand;
        }

        // yslee Hybernate delete 확인 필요 
        public int DeleteTransportCommand(String transportCommandId)
        {
            StringBuilder sb = new StringBuilder(transportCommandId);
            return this.PersistentDao.Delete(typeof(TransportCommandEx), sb );
        }

        public int DeleteTransportCommand(TransportCommandEx transportCommand)
        {
            StringBuilder sb = new StringBuilder(transportCommand.Id);
            return this.PersistentDao.Delete(typeof(TransportCommandEx), sb);
        }

        public int DeleteTransportCommands()
        {
            return this.PersistentDao.DeleteAll(typeof(TransportCommandEx));
        }
  
        public int DeleteTransportCommandsByCarrierId(String carrierId)
        {
            return this.PersistentDao.DeleteByAttribute(typeof(TransportCommandEx), "CarrierId", carrierId);
        }
        public int DeleteUiTransportById(String TransportId)
        {
            return this.PersistentDao.DeleteByAttribute(typeof(UiTransport), "ID", TransportId);
        }

        public bool ExistTransportCommand(String transportCommandId)
        {
            TransportCommandEx transportCommand = GetTransportCommand(transportCommandId);
            if (transportCommand != null)
            {
                //logger.fine("transportCommand exists", transportCommand.getCarrierId(), transportCommandId, transportCommand.getSource(),transportCommand.Dest);
                return true;
            }
            //logger.fine("transportCommand does not exist", "", transportCommandId, "", "");
            return false;
        }

        public TransportCommandEx GetTransportCommand(String transportCommandId)
        {
            StringBuilder sbTransportCommandId = new StringBuilder(transportCommandId);
            return (TransportCommandEx)this.PersistentDao.Find(typeof(TransportCommandEx), sbTransportCommandId, false);
        }
  
        public TransportCommandEx GetTransportCommandByCarrierId(String carrierId)
        {
            IList transportCommands = this.PersistentDao.FindByAttribute(typeof(TransportCommandEx), "CarrierId", carrierId);
            if (transportCommands.Count > 0) {
              return (TransportCommandEx) transportCommands[0];
            }
            return null;
        }
  
        public TransportCommandEx GetTransportCommandByQueueStateFIFO(String vehicleId)
        {
            TransportCommandEx transportCommand = (TransportCommandEx)this.PersistentDao.FindByAttributeOrderByDesc(typeof(TransportCommandEx), "State", "QUEUED", "CreateTime")[0];
    
            return transportCommand;
        }
  
        public TransportCommandEx GetTransportCommandByVehicleId(String vehicleId)
        {
            IList transportCommands = this.PersistentDao.FindByAttribute(typeof(TransportCommandEx), "VehicleId", vehicleId);
            if (transportCommands.Count > 0) {
              return (TransportCommandEx) transportCommands[0];
            }
            return null;
        }
  
        public TransportCommandEx GetTransportCommandByDestPortId(String destPortId)
        {
            IList transportCommands = this.PersistentDao.FindByAttribute(typeof(TransportCommandEx), "Dest", destPortId);
            if (transportCommands.Count > 0) {
              return (TransportCommandEx) transportCommands[0];
            }
            return null;
        }
  
        public bool CheckTransportCommandBySourceLocationId(String sourceLocationId)
        {
            IList transportCommands = this.PersistentDao.FindByAttribute(typeof(TransportCommandEx), "Source", sourceLocationId);
            if (transportCommands.Count > 0) {
              return true;
            }
            return false;
        }
  
        public bool CheckTransportCommandByDestLocationId(String destLocationId)
        {
            IList transportCommands = this.PersistentDao.FindByAttribute(typeof(TransportCommandEx), "Dest", destLocationId);
            if (transportCommands.Count > 0) {
              return true;
            }
            return false;
        }
  
        public String ConvertPriorityToMES(String priority)
        {
            return ConvertPriority("MCS", "MES", priority);
        }

        public int GetTransportCommandCount()
        {
            return this.PersistentDao.FindAll(typeof(TransportCommandEx)).Count;
        }
  
        public int GetTransportCommandCountByDestPortId(String destPortId)
        {
            return this.PersistentDao.FindByAttribute(typeof(TransportCommandEx), "DestPortId", destPortId).Count;
        }

        public int GetTransportCommandCountBySourcePortId(String sourcePortId)
        {
            return this.PersistentDao.FindByAttribute(typeof(TransportCommandEx), "SourcePortId", sourcePortId).Count;
        }

        public IList GetQueuedTransportCommands()
        {
            IList transportCommands = this.PersistentDao.FindByAttributeOrderBy(typeof(TransportCommandEx), "State", "QUEUED", "CreateTime");
            //logger.info("conut{" + transportCommands.size() + "}, " + transportCommands);
            return transportCommands;
        }

        public IList GetQueuedUiTransportCommands()
        {
            IList transportCommands = this.PersistentDao.FindAll(typeof(UiTransport));
            //logger.info("conut{" + transportCommands.size() + "}, " + transportCommands);
            return transportCommands;
        }


        public IList GetQueuedTransportCommandsByBayId(String bayId)
        {
            var attributes = new Dictionary<string, object>();
            attributes.Add("State", "QUEUED");
            attributes.Add("BayId", bayId);

            IList transportCommands = this.PersistentDao.FindByAttributes(typeof(TransportCommandEx), attributes);

            //logger.info("conut{" + transportCommands.size() + "}, " + transportCommands);
    
            return transportCommands;
        }

        public IList GetTransportCommands()
        {
            return this.PersistentDao.FindAll(typeof(TransportCommandEx));
        }

        public IList GetTransportCommandsByStateAndBayId(String state, String bayId)
        {
            var attributes = new Dictionary<string, object>();
            attributes.Add("State", state);
            attributes.Add("BayId", bayId);

            return this.PersistentDao.FindByAttributes(typeof(TransportCommandEx), attributes);
        }

        public void UpdateTransportCommand(TransportCommandEx transportCommand)
        {
            this.PersistentDao.Update(transportCommand);
        }

        public int UpdateTransportCommand(TransportCommandEx transportCommand, Dictionary<string, object> setAttributes)
        {
            return this.PersistentDao.Update(typeof(TransportCommandEx), setAttributes, transportCommand.Id);
        }
  
        public int UpdateTransportCommandVehicleId(TransportCommandEx transportCommand, String vehicleId)
        {
            return this.PersistentDao.Update(typeof(TransportCommandEx), "VehicleId", vehicleId, transportCommand.Id);
        }
  
        public int UpdateTransportCommandPath(TransportCommandEx transportCommand, String path)
        {
            return this.PersistentDao.Update(typeof(TransportCommandEx), "Path", path, transportCommand.Id);
        }
  
        public void UpdateTransportCommandState(TransportCommandEx transportCommand)
        {
            this.PersistentDao.Update(transportCommand);
        }

        public TransportCommandEx CreateTransportCommand(String transportCommandId, String carrierId, String sourcePortId, String destPortId, int priority)
        {
            return CreateTransportCommand(transportCommandId, carrierId, sourcePortId, destPortId, priority, "", "", "", "", "", "", "", "");
        }

        public TransportCommandEx CreateTransportCommand(String transportCommandId, String carrierId, String sourcePortId, String destPortId, int priority, String eqpId, String portId, String agvName, String jobType, String midLoc, String midPortId, String originLoc, String description)
        {
            TransportCommandEx transportCommand = new TransportCommandEx();

            transportCommand.Id = transportCommandId;
            transportCommand.CarrierId = carrierId;
            transportCommand.Source = sourcePortId;
            transportCommand.Dest = destPortId;
            transportCommand.Priority = priority;

            transportCommand.EqpId = eqpId;
            transportCommand.PortId = portId;
            transportCommand.AgvName = agvName;
            transportCommand.JobType = jobType;
            transportCommand.MidLoc = midLoc;
            transportCommand.MidPortId = midPortId;
            transportCommand.OriginLoc = originLoc;
            transportCommand.Description = description;

            CreateTransportCommand(transportCommand);
            return transportCommand;
        }

        public String ConvertPriority(String fromSystemName, String toSystemName, String strFromPriority)
        {
            PriorityRange fromPriorityRange = GetPriorityRange(fromSystemName);
            PriorityRange toPriorityRange = GetPriorityRange(toSystemName);
            if (fromPriorityRange == null)
            {
                //logger.fine("priorityRange does not exist, systemName{" + fromSystemName + "}");
                return strFromPriority;
            }
            if (toPriorityRange == null)
            {
                //logger.fine("priorityRange does not exist, systemName{" + toSystemName + "}");
                return strFromPriority;
            }
            int fromPriority = 0;

            int.TryParse(strFromPriority, out fromPriority);

            if (fromPriorityRange.getDirection().Equals("ASCENDING"))
            {
                if (fromPriority > fromPriorityRange.getMax())
                {
                    fromPriority = fromPriorityRange.getMax();
                }
                else if (fromPriority < fromPriorityRange.getMin())
                {
                    fromPriority = fromPriorityRange.getMin();
                }
            }
            else if (fromPriority > fromPriorityRange.getMin())
            {
                fromPriority = fromPriorityRange.getMin();
            }
            else if (fromPriority < fromPriorityRange.getMax())
            {
                fromPriority = fromPriorityRange.getMax();
            }
            float magnification = (toPriorityRange.getMax() - toPriorityRange.getMin()) / (
              fromPriorityRange.getMax() - fromPriorityRange.getMin());

            int toPriority = fromPriority;
            if (fromPriorityRange.getDirection().Equals(toPriorityRange.getDirection()))
            {
                toPriority = (int)Math.Round((fromPriority - fromPriorityRange.getMin()) * magnification + toPriorityRange.getMin());
            }
            else if (toPriorityRange.getDirection().Equals("ASCENDING"))
            {
                toPriority = toPriorityRange.getMin() + (int)Math.Round((fromPriority - fromPriorityRange.getMin()) * magnification);
            }
            else
            {
                toPriority = toPriorityRange.getMin() + (int)Math.Round((fromPriority - fromPriorityRange.getMin()) * magnification);
            }
            return toPriority.ToString();
        }

        public PriorityRange GetPriorityRange(String systemName)
        {
            IList priorityRanges = this.PersistentDao.FindByAttribute(typeof(PriorityRange), "SystemName", systemName);
            if ((priorityRanges != null) && (priorityRanges.Count > 0)) 
            {
              return (PriorityRange)priorityRanges[0];
            }
            return null;
        }
  
        public int UpdateTransportCommandStateByChangeVehicle(TransportCommandEx transportCommand)
        {
            Dictionary<string, object> setAttributes = new Dictionary<string, object>();
            setAttributes.Add("State", "CHANGEVEHICLE");

            Dictionary<string, object> conditionAttributes = new Dictionary<string, object>();
            conditionAttributes.Add("Id", transportCommand.Id);
            conditionAttributes.Add("State", "ASSIGNED");

            return this.PersistentDao.UpdateByAttributes(typeof(TransportCommandEx), setAttributes, conditionAttributes);
         }
  
        public void UpdateTransportCommandAdditionalInfo(TransportCommandEx transportCommand)
        {
            Dictionary<string, object> setAttributes = new Dictionary<string, object>();
            setAttributes.Add("AdditionalInfo", transportCommand.AdditionalInfo );
            int result = UpdateTransportCommand(transportCommand, setAttributes);
            if (result > 0)
            {
                //logger.fine("transportCommand{" + transportCommand.getId() + "}.additionalInfo was changed to {" + transportCommand.getAdditionalInfo() + "}" + transportCommand);
            }
        }

        public String GetAdditionalInfo(TransportCommandEx transportCommand, String key)
        {
            //StringBuilder sbAdditionalInfo = new StringBuilder(); //lys20180709 차후 사용 시
            //sbAdditionalInfo.Append(transportCommand.AdditionalInfo);

            //Dictionary<string, object> additionalInfoMap = new Dictionary<string, object>();
            Hashtable additionalInfoMap = new Hashtable();
            additionalInfoMap = MapUtility.StringToMap(transportCommand.AdditionalInfo);

            if (additionalInfoMap.ContainsKey(key))
            {
                return (String)additionalInfoMap[key];
            }
            return "";
        }

        //200622 Change NIO Logic About ES.exe does not restart
        public IList GetEventUiCommands()
        {
            IList transportCommands = this.PersistentDao.FindAll(typeof(UiCommand));
            //logger.info("conut{" + transportCommands.size() + "}, " + transportCommands);
            return transportCommands;
        }
        //

        //200622 Change NIO Logic About ES.exe does not restart
        public int DeleteUiCommandById(string Id, string messageName, string applicationName)
        {
            Dictionary<string, object> conditionAttributes = new Dictionary<string, object>();

            conditionAttributes.Add("Id", Id);
            conditionAttributes.Add("MessageName", messageName);
            conditionAttributes.Add("ApplicationName", applicationName);

            return this.PersistentDao.DeleteByAttributes(typeof(UiCommand), conditionAttributes);

            //return this.PersistentDao.DeleteByAttribute(typeof(UiTransport), "ID", TransportId);
        }
        //
    }
}
