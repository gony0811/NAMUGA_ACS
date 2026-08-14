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

            transportCommand.JobId = transferMessage.TransportCommandId;
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
            return this.PersistentDao.DeleteByAttribute(typeof(TransportCommandEx), "JobId", transportCommandId);
        }

        public int DeleteTransportCommand(TransportCommandEx transportCommand)
        {
            return this.PersistentDao.DeleteByAttribute(typeof(TransportCommandEx), "JobId", transportCommand.JobId);
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

        // 새 MOVECMD 의 SourceLoc/DestLoc 가 기존 비-종료 TC 의 Source/Dest 의 location 부분(":" split 첫 토큰) 과
        // 하나라도 일치하면 그 TC 를 반환. 매칭 후보가 여러 건이면 CreateTime 오름차순 첫 건.
        public TransportCommandEx FindActiveTransportCommandByLocationMatch(String newSourceLoc, String newDestLoc)
        {
            var terminalStates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                TransportCommandEx.STATE_COMPLETED, TransportCommandEx.STATE_CANCELED,
                TransportCommandEx.STATE_CANCELING, TransportCommandEx.STATE_ABORTED,
                TransportCommandEx.STATE_ABORTING,  TransportCommandEx.STATE_COMPLETEFAILED,
                TransportCommandEx.STATE_CHARGE_COMPLETED, TransportCommandEx.STATE_CHANGE_VEHICLE
            };

            string LocPart(string s) =>
                string.IsNullOrWhiteSpace(s) ? null : s.Split(':')[0];

            bool Eq(string a, string b) =>
                !string.IsNullOrWhiteSpace(a) && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

            IList all = this.PersistentDao.FindAll(typeof(TransportCommandEx));
            if (all == null || all.Count == 0) return null;

            return all
                .Cast<TransportCommandEx>()
                .Where(tc => tc.State == null || !terminalStates.Contains(tc.State))
                .Where(tc =>
                {
                    var sLoc = LocPart(tc.Source);
                    var dLoc = LocPart(tc.Dest);
                    return Eq(newSourceLoc, sLoc) || Eq(newSourceLoc, dLoc)
                        || Eq(newDestLoc,   sLoc) || Eq(newDestLoc,   dLoc);
                })
                .OrderBy(tc => tc.CreateTime ?? DateTime.MaxValue)
                .FirstOrDefault();
        }

        public TransportCommandEx GetTransportCommand(String transportCommandId)
        {
            IList results = this.PersistentDao.FindByAttribute(typeof(TransportCommandEx), "JobId", transportCommandId);
            if (results != null && results.Count > 0)
            {
                return (TransportCommandEx)results[0];
            }
            return null;
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
            return FilterUnassignedTransportCommands(transportCommands, excludeExchange: true);
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
            return FilterUnassignedTransportCommands(transportCommands, excludeExchange: true);
        }

        // EXCHANGE(v2) S4: 배차 대기 EXCHANGE TC 조회 — 기존 QUEUED 조회와 상태 격리 (D5).
        public IList GetExchangeQueuedTransportCommandsByBayId(String bayId)
        {
            var attributes = new Dictionary<string, object>();
            attributes.Add("State", TransportCommandEx.STATE_EXCHANGE_QUEUED);
            attributes.Add("BayId", bayId);

            IList transportCommands = this.PersistentDao.FindByAttributes(typeof(TransportCommandEx), attributes);
            return FilterUnassignedTransportCommands(transportCommands);
        }

        // Rollback 잘못된 발동/EF silent drop 등으로 만들어진 좀비 TC (state=QUEUED 이지만 VehicleId 가
        // 남아있는 경우) 가 다음 사이클에서 다시 잡혀 잘못된 재할당을 일으키지 않도록 메모리에서 한 번 더 필터.
        // excludeExchange: 일반 배차 조회용 이중 방어 — 상태가 어떤 경로로 QUEUED 로 오염되든
        // JobType=EXCHANGE TC 는 일반 스케줄러가 절대 집지 못하게 한다 (D5 격리).
        private IList FilterUnassignedTransportCommands(IList transportCommands, bool excludeExchange = false)
        {
            if (transportCommands == null || transportCommands.Count == 0) return transportCommands;
            var filtered = new List<TransportCommandEx>(transportCommands.Count);
            foreach (var item in transportCommands)
            {
                if (item is not TransportCommandEx tc || !string.IsNullOrEmpty(tc.VehicleId))
                    continue;
                if (excludeExchange
                    && TransportCommandEx.JOBTYPE_EXCHANGE.Equals(tc.JobType, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Warn($"FilterUnassignedTransportCommands: EXCHANGE TC 가 일반 QUEUED 조회에 잡힘 — 제외 " +
                                $"(state 오염 의심) jobId={tc.JobId}, state={tc.State}");
                    continue;
                }
                filtered.Add(tc);
            }
            return filtered;
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
            return this.PersistentDao.UpdateByAttributes(typeof(TransportCommandEx), setAttributes, "JobId", transportCommand.JobId);
        }

        public int UpdateTransportCommandVehicleId(TransportCommandEx transportCommand, String vehicleId)
        {
            return this.PersistentDao.UpdateByAttribute(typeof(TransportCommandEx), "VehicleId", vehicleId, "JobId", transportCommand.JobId);
        }

        public int UpdateTransportCommandPath(TransportCommandEx transportCommand, String path)
        {
            return this.PersistentDao.UpdateByAttribute(typeof(TransportCommandEx), "Path", path, "JobId", transportCommand.JobId);
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

            transportCommand.JobId = transportCommandId;
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
            conditionAttributes.Add("JobId", transportCommand.JobId);
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
