using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;
using ACS.Core.Application.Model;
using ACS.Core.Message.Model.Server;
using ACS.Core.Resource.Model.Factory.Unit;
using ACS.Core.Resource;
using ACS.Core.Material;
using ACS.Core.History;
using ACS.Core.History.Model;
using ACS.Core.Alarm.Model;
using ACS.Core.Transfer.Model;
using ACS.Core.Message.Model;
using ACS.Core.Resource.Model;
using ACS.Core.Resource.Model.Factory.Machine;
using System.Collections;
namespace ACS.Manager.History
{
    public class HistoryManagerExImplement : AbstractManager, IHistoryManagerEx
    {
        protected IResourceManagerEx resourceManager;
        protected IMaterialManagerEx materialManager;
        private bool usePhysicalPartitioningTable;
        private String transactionHistoryPartitionType = "MONTH";

        public bool IsUsePhysicalPartitioningTable()
        {
            return this.usePhysicalPartitioningTable;
        }

        public void SetUsePhysicalPartitioningTable(bool usePhysicalPartitioningTable)
        {
            this.usePhysicalPartitioningTable = usePhysicalPartitioningTable;
        }

        public String GetTransactionHistoryPartitionType()
        {
            return this.transactionHistoryPartitionType;
        }

        public void SetTransactionHistoryPartitionType(String transactionHistoryPartitionType)
        {
            this.transactionHistoryPartitionType = transactionHistoryPartitionType;
        }

        public IMaterialManagerEx GetMaterialManager()
        {
            return this.materialManager;
        }

        public void SetMaterialManager(IMaterialManagerEx materialManager)
        {
            this.materialManager = materialManager;
        }

        public IResourceManagerEx GetResourceManager()
        {
            return this.resourceManager;
        }

        public void SetResourceManager(IResourceManagerEx resourceManager)
        {
            this.resourceManager = resourceManager;
        }

        public AlarmHistoryEx CreateAlarmHistoryInstance()
        {
            return null;
        }

        public AlarmReportHistoryEx CreateAlarmReportHistory(AlarmReportMessage alarmReportMessage)
        {
            AlarmReportHistoryEx alarmReportHistory = CreateAlarmReportHistoryInstance();

            alarmReportHistory.AlarmCode = alarmReportMessage.AlarmCode;
            alarmReportHistory.AlarmId = alarmReportMessage.AlarmId;
            alarmReportHistory.AlarmText = alarmReportMessage.AlarmText;

            CreateAlarmReportHistory(alarmReportMessage);
            return alarmReportHistory;
        }

        public void CreateAlarmReportHistory(AlarmReportHistoryEx alarmReportHistory)
        {
            PersistentDao.Save(alarmReportHistory, true);
        }

        public AlarmReportHistoryEx CreateAlarmReportHistory(AlarmEx alarm, String state)
        {
            AlarmReportHistoryEx alarmReportHistory = new AlarmReportHistoryEx();

            alarmReportHistory.Id = Guid.NewGuid().ToString();
            alarmReportHistory.AlarmCode = alarm.AlarmCode;
            alarmReportHistory.AlarmId = alarm.AlarmId;
            alarmReportHistory.AlarmText = alarm.AlarmText;

            alarmReportHistory.Time = DateTime.Now; //lys20180710 TimeUtils.getCurrentTimestamp()
            alarmReportHistory.VehicleId = alarm.VehicleId;
            alarmReportHistory.State = state;
            if (alarm.TransportCommandId != null)
            {
                alarmReportHistory.TransportCommandId = alarm.TransportCommandId;
            }
            PersistentDao.Save(alarmReportHistory);
            return alarmReportHistory;
        }

        public AlarmReportHistoryEx CreateAlarmReportHistory(String alarmId, String alarmCode, String alarmText, String machineName, String state)
        {
            return CreateAlarmReportHistory(alarmId, alarmCode, alarmText, machineName, "", state);
        }

        public AlarmReportHistoryEx CreateAlarmReportHistory(String alarmId, String alarmCode, String alarmText, String machineName, String unitName, String state)
        {
            return null;
        }

        public AlarmReportHistoryEx CreateAlarmReportHistory(String alarmId, String machineName, String state)
        {
            return null;
        }

        public AlarmReportHistoryEx CreateAlarmReportHistory(String alarmId, String alarmCode, String machineName, String state)
        {
            return null;
        }

        public AlarmReportHistoryEx CreateAlarmReportHistory(AlarmReport alarmReport, String state)
        {
            return null;
        }

        public AlarmReportHistoryEx CreateAlarmReportHistoryInstance()
        {
            return null;
        }

        public ApplicationHistoryEx CreateApplicationHistory(ACS.Core.Application.Model.Application application) //lys20180713 Application??
        {
            return null;
        }

        public void CreateApplicationHistory(ApplicationHistoryEx applicationHistory) { }

        public ApplicationHistoryEx CreateApplicationHistoryInstance()
        {
            return null;
        }

        public HeartBeatFailHistoryEx CreateHeartBeatFailHistory(ACS.Core.Application.Model.Application application) //lys20180713 Application??
        {
            return null;
        }

        public void CreateHeartBeatFailHistory(HeartBeatFailHistoryEx heartBeatFailHistory) { }

        public HeartBeatFailHistoryEx CreateHeartBeatFailHistoryInstance()
        {
            return null;
        }

        public TransactionHistoryEx CreateTransactionHistory(String transactionId, String transactionName, DateTime startTime, DateTime endTime, int elapsedTime, int runningBpelProcessCount, String applicationName)
        {
            return null;
        }

        public TransactionHistoryEx CreateTransactionHistory(String transactionId, String transactionName, DateTime startTime, DateTime endTime, int elapsedTime, int runningBpelProcessCount, String applicationName, String cause)
        {
            return null;
        }

        public TransactionHistoryEx CreateTransactionHistory(String transactionId, String transactionName, DateTime startTime, DateTime endTime, int elapsedTime, int runningBpelProcessCount, String applicationName, String cause, String originatedType)
        {
            return null;
        }

        public void CreateTransactionHistory(TransactionHistoryEx transactionHistory) { }

        public TransactionHistoryEx CreateTransactionHistoryInstance()
        {
            return null;
        }

        public TransportCommandHistoryEx CreateTransportCommandHistory(TransportCommandEx transportCommand)
        {
            TransportCommandHistoryEx transportCommandHistoryEx = new TransportCommandHistoryEx();

            transportCommandHistoryEx.State = transportCommand.State;
            transportCommandHistoryEx.JobId = transportCommand.JobType;
            transportCommandHistoryEx.CarrierId = transportCommand.CarrierId;
            transportCommandHistoryEx.Source = transportCommand.Source;
            transportCommandHistoryEx.Dest = transportCommand.Dest;
            transportCommandHistoryEx.Priority = transportCommand.Priority;
            transportCommandHistoryEx.Path = transportCommand.Path;

            transportCommandHistoryEx.EqpId = transportCommand.EqpId;
            transportCommandHistoryEx.PortId = transportCommand.PortId;
            transportCommandHistoryEx.AgvName = transportCommand.AgvName;
            transportCommandHistoryEx.VehicleId = transportCommand.VehicleId;
            transportCommandHistoryEx.JobType = transportCommand.JobType;
            transportCommandHistoryEx.MidLoc = transportCommand.MidLoc;
            transportCommandHistoryEx.MidPortId = transportCommand.MidPortId;
            transportCommandHistoryEx.OriginLoc = transportCommand.OriginLoc;
            transportCommandHistoryEx.Description = transportCommand.Description;
            transportCommandHistoryEx.BayId = transportCommand.BayId;

            transportCommandHistoryEx.CreateTime = transportCommand.CreateTime;
            transportCommandHistoryEx.QueuedTime = transportCommand.QueuedTime;
            transportCommandHistoryEx.AssignedTime = transportCommand.AssignedTime;

            transportCommandHistoryEx.StartedTime = transportCommand.StartedTime;

            transportCommandHistoryEx.LoadArrivedTime = transportCommand.LoadArrivedTime;
            transportCommandHistoryEx.LoadedTime = transportCommand.LoadedTime;
            transportCommandHistoryEx.UnloadArrivedTime = transportCommand.UnloadArrivedTime;
            transportCommandHistoryEx.UnloadedTime = transportCommand.UnloadedTime;

            transportCommandHistoryEx.Time = DateTime.Now; //lys20180710 SetTime(TimeUtils.getCurrentTimestamp());

            PersistentDao.Save(transportCommandHistoryEx);
            return transportCommandHistoryEx;
        }

        public TransportCommandHistoryEx CreateTransportCommandHistory(TransportCommandEx transportCommand, String code, String reason)
        {
            TransportCommandHistoryEx transportCommandHistoryEx = new TransportCommandHistoryEx();
            transportCommandHistoryEx.State = transportCommand.State;
            transportCommandHistoryEx.JobId = transportCommand.JobId;
            transportCommandHistoryEx.CarrierId = transportCommand.CarrierId;
            transportCommandHistoryEx.Source = transportCommand.Source;
            transportCommandHistoryEx.Dest = transportCommand.Dest;
            transportCommandHistoryEx.Priority = transportCommand.Priority;
            transportCommandHistoryEx.Path = transportCommand.Path;

            transportCommandHistoryEx.EqpId = transportCommand.EqpId;
            transportCommandHistoryEx.PortId = transportCommand.PortId;
            transportCommandHistoryEx.AgvName = transportCommand.AgvName;
            transportCommandHistoryEx.VehicleId = transportCommand.VehicleId;
            transportCommandHistoryEx.JobType = transportCommand.JobType;
            transportCommandHistoryEx.MidLoc = transportCommand.MidLoc;
            transportCommandHistoryEx.MidPortId = transportCommand.MidPortId;
            transportCommandHistoryEx.OriginLoc = transportCommand.OriginLoc;
            transportCommandHistoryEx.Description = transportCommand.Description;
            transportCommandHistoryEx.BayId = transportCommand.BayId;

            transportCommandHistoryEx.CreateTime = transportCommand.CreateTime;
            transportCommandHistoryEx.QueuedTime = transportCommand.QueuedTime;
            transportCommandHistoryEx.AssignedTime = transportCommand.AssignedTime;

            transportCommandHistoryEx.StartedTime = transportCommand.StartedTime;

            transportCommandHistoryEx.LoadArrivedTime = transportCommand.LoadArrivedTime;
            transportCommandHistoryEx.LoadedTime = transportCommand.LoadedTime;
            transportCommandHistoryEx.UnloadArrivedTime = transportCommand.UnloadArrivedTime;
            transportCommandHistoryEx.UnloadedTime = transportCommand.UnloadedTime;

            transportCommandHistoryEx.Time = DateTime.Now; //lys20180710(TimeUtils.getCurrentTimestamp());
            transportCommandHistoryEx.Code = code;
            transportCommandHistoryEx.Reason = reason;

            PersistentDao.Save(transportCommandHistoryEx);
            return transportCommandHistoryEx;
        }

        public TransportCommandHistoryEx CreateTransportCommandHistory(String jobId, String carrierId, String source, String dest, String jobType, String code, String cause)
        {
            TransportCommandHistoryEx transportCommandHistoryEx = new TransportCommandHistoryEx();
            transportCommandHistoryEx.State = "ABORTED";
            transportCommandHistoryEx.JobId = jobId;

            transportCommandHistoryEx.CarrierId = carrierId;
            transportCommandHistoryEx.Source = source;
            transportCommandHistoryEx.Dest = dest;

            transportCommandHistoryEx.JobType = jobType;

            transportCommandHistoryEx.Time = DateTime.Now; //(TimeUtils.getCurrentTimestamp());
            transportCommandHistoryEx.Code = code;
            transportCommandHistoryEx.Reason = cause;

            PersistentDao.Save(transportCommandHistoryEx);
            return transportCommandHistoryEx;
        }

        public TransportCommandHistoryEx CreateTransportCommandHistory(TransportCommandEx transportCommand, String fixedRoute)
        {
            return null;
        }

        public TransportCommandHistoryEx CreateTransportCommandHistory(TransferMessageEx transferMessage)
        {
            return null;
        }

        public void CreateTransportCommandHistory(TransportCommandHistoryEx transportCommandHistory) { }

        public TransportCommandHistoryEx CreateTransportCommandHistoryInstance()
        {
            return null;
        }
        public TruncateParameterEx CreateTruncateParameter(string paramString1, string paramString2, string paramString3, int paramInt, string paramString4)
        {
            throw new NotImplementedException();
        }

        public TruncateParameterEx createTruncateParameter(String tableName, String nativeSql, String partitioningBase, int savePeriod, String truncateSingleOrMulti)
        {
            return null;
        }

        public void CreateTruncateParameter(TruncateParameterEx truncateParameter) { }

        public VehicleBatteryHistoryEx CreateVehicleBatteryHistory(VehicleMessageEx vehicleMessage)
        {
            VehicleBatteryHistoryEx vehicleBatteryHistory = new VehicleBatteryHistoryEx();

            vehicleBatteryHistory.VehicleId = vehicleMessage.VehicleId;
            if (vehicleMessage.Vehicle != null)
            {
                vehicleBatteryHistory.BatteryRate = vehicleMessage.Vehicle.BatteryRate;
                vehicleBatteryHistory.BatteryVoltage = vehicleMessage.Vehicle.BatteryVoltage;
                vehicleBatteryHistory.Time = DateTime.Now; //TimeUtils.getCurrentTimestamp());
            }
            CreateVehicleBatteryHistory(vehicleBatteryHistory);
            return vehicleBatteryHistory;
        }

        public VehicleBatteryHistoryEx CreateVehicleBatteryHistory(VehicleEx vehicle)
        {
            VehicleBatteryHistoryEx vehicleBatteryHistory = new VehicleBatteryHistoryEx();

            vehicleBatteryHistory.VehicleId = vehicle.VehicleId;
            vehicleBatteryHistory.BatteryRate = vehicle.BatteryRate;
            vehicleBatteryHistory.BatteryVoltage = vehicle.BatteryVoltage;
            vehicleBatteryHistory.Time = DateTime.Now; //TimeUtils.getCurrentTimestamp());
            vehicleBatteryHistory.ProcessingState = vehicle.ProcessingState;

            CreateVehicleBatteryHistory(vehicleBatteryHistory);
            return vehicleBatteryHistory;
        }

        public void CreateVehicleBatteryHistory(VehicleBatteryHistoryEx vehicleBatteryHistory)
        {
            PersistentDao.Save(vehicleBatteryHistory);
        }

        public VehicleHistoryEx CreateVehicleHistory(VehicleMessageEx vehicleMessage)
        {
            VehicleHistoryEx vehicleHistory = new VehicleHistoryEx();

            vehicleHistory.VehicleId = vehicleMessage.VehicleId;
            if (vehicleMessage.Vehicle != null)
            {
                vehicleHistory.BayId = vehicleMessage.Vehicle.BayId;
                vehicleHistory.CarrierType = vehicleMessage.Vehicle.CarrierType;
                vehicleHistory.ConnectionState = vehicleMessage.Vehicle.ConnectionState;
                vehicleHistory.AlarmState = vehicleMessage.Vehicle.AlarmState;
                vehicleHistory.ProcessingState = vehicleMessage.Vehicle.ProcessingState;
                vehicleHistory.CurrentNodeId = vehicleMessage.Vehicle.CurrentNodeId;
                vehicleHistory.TransportCommandId = vehicleMessage.Vehicle.TransportCommandId;
                vehicleHistory.Path = vehicleMessage.Vehicle.Path;
                vehicleHistory.NodeCheckTime = vehicleMessage.Vehicle.NodeCheckTime;
                vehicleHistory.State = vehicleMessage.Vehicle.State;
                vehicleHistory.Installed = vehicleMessage.Vehicle.Installed;
                vehicleHistory.TransferState = vehicleMessage.Vehicle.TransferState;
                vehicleHistory.RunState = vehicleMessage.Vehicle.RunState;
                vehicleHistory.FullState = vehicleMessage.Vehicle.FullState;
                vehicleHistory.Time = DateTime.Now; //TimeUtils.getCurrentTimestamp());
                vehicleHistory.MessageName = vehicleMessage.MessageName;
                vehicleHistory.AcsDestNodeId = vehicleMessage.Vehicle.AcsDestNodeId;
                vehicleHistory.VehicleDestNodeId = vehicleMessage.Vehicle.VehicleDestNodeId;
            }
            CreateVehicleHistory(vehicleHistory);
            return vehicleHistory;
        }

        public VehicleHistoryEx CreateVehicleHistory(TransferMessageEx transferMessage)
        {
            VehicleHistoryEx vehicleHistory = new VehicleHistoryEx();

            vehicleHistory.VehicleId = transferMessage.VehicleId;
            if (transferMessage.Vehicle != null)
            {
                vehicleHistory.BayId = transferMessage.Vehicle.BayId;
                vehicleHistory.CarrierType = transferMessage.Vehicle.CarrierType;
                vehicleHistory.ConnectionState = transferMessage.Vehicle.ConnectionState;
                vehicleHistory.AlarmState = transferMessage.Vehicle.AlarmState;
                vehicleHistory.ProcessingState = transferMessage.Vehicle.ProcessingState;
                vehicleHistory.CurrentNodeId = transferMessage.Vehicle.CurrentNodeId;
                vehicleHistory.TransportCommandId = transferMessage.Vehicle.TransportCommandId;
                vehicleHistory.Path = transferMessage.Vehicle.Path;
                vehicleHistory.NodeCheckTime = transferMessage.Vehicle.NodeCheckTime;
                vehicleHistory.State = transferMessage.Vehicle.State;
                vehicleHistory.Installed = transferMessage.Vehicle.Installed;
                vehicleHistory.TransferState = transferMessage.Vehicle.TransferState;
                vehicleHistory.RunState = transferMessage.Vehicle.RunState;
                vehicleHistory.FullState = transferMessage.Vehicle.FullState;
                vehicleHistory.Time = DateTime.Now; // (TimeUtils.getCurrentTimestamp());
                vehicleHistory.MessageName = transferMessage.MessageName;
                vehicleHistory.AcsDestNodeId = transferMessage.Vehicle.AcsDestNodeId;
                vehicleHistory.VehicleDestNodeId = transferMessage.Vehicle.VehicleDestNodeId;
            }
            CreateVehicleHistory(vehicleHistory);
            return vehicleHistory;
        }

        public VehicleHistoryEx CreateVehicleHistory(VehicleEx vehicle, String messageName)
        {
            VehicleHistoryEx vehicleHistory = new VehicleHistoryEx();

            vehicleHistory.VehicleId = vehicle.VehicleId;
            vehicleHistory.BayId = vehicle.BayId;
            vehicleHistory.CarrierType = vehicle.CarrierType;
            vehicleHistory.ConnectionState = vehicle.ConnectionState;
            vehicleHistory.AlarmState = vehicle.AlarmState;
            vehicleHistory.ProcessingState = vehicle.ProcessingState;
            vehicleHistory.CurrentNodeId = vehicle.CurrentNodeId;
            vehicleHistory.TransportCommandId = vehicle.TransportCommandId;
            vehicleHistory.Path = vehicle.Path;
            vehicleHistory.NodeCheckTime = vehicle.NodeCheckTime;
            vehicleHistory.State = vehicle.State;
            vehicleHistory.Installed = vehicle.Installed;
            vehicleHistory.TransferState = vehicle.TransferState;
            vehicleHistory.RunState = vehicle.RunState;
            vehicleHistory.FullState = vehicle.FullState;
            vehicleHistory.AcsDestNodeId = vehicle.AcsDestNodeId;
            vehicleHistory.VehicleDestNodeId = vehicle.VehicleDestNodeId;
            vehicleHistory.Time = DateTime.Now; //Time(TimeUtils.getCurrentTimestamp());
            vehicleHistory.MessageName = messageName;

            CreateVehicleHistory(vehicleHistory);
            return vehicleHistory;
        }

        public void CreateVehicleHistory(VehicleHistoryEx vehicleHistory)
        {
            PersistentDao.Save(vehicleHistory);
        }

        public VehicleHistoryEx CreateVehicleHistoryInstance()
        {
            return null;
        }

        public int DeleteAlarmReportHistories(String alarmId, String machineName)
        {
            return 0;
        }

        public int DeleteAlarmReportHistories(String alarmId, String alarmCode, String machineName)
        {
            return 0;
        }

        public int DeleteAlarmReportHistories(String alarmId, String alarmCode, String alarmText, String machineName)
        {
            return 0;
        }

        public int DeleteAlarmReportHistories()
        {
            return 0;
        }

        public int DeleteAlarmReportHistoriesByMachine(String machineName)
        {
            return 0;
        }

        public int DeleteAlarmReportHistoriesByMachine(Machine machine)
        {
            return 0;
        }

        public int DeleteAlarmReportHistoriesByMachine(String machineName, String unitName)
        {
            return 0;
        }

        public void DeleteAlarmReportHistory(AlarmReportHistoryEx alarmReportHistory) { }

        public int DeleteApplicationlHistories(String applicationName)
        {
            return 0;
        }

        public int DeleteApplicationlHistories()
        {
            return 0;
        }

        public void DeleteApplicationlHistory(ApplicationHistoryEx applicationlHistory) { }

        public int DeleteHeartBeatFailHistories(String applicationName)
        {
            return 0;
        }

        public int DeleteHeartBeatFailHistories()
        {
            return 0;
        }

        public void DeleteHeartBeatFailHistory(HeartBeatFailHistoryEx heartBeatFailHistory) { }

        public int DeleteHistoriesBetweenTimes(Type claz, DateTime from, DateTime to)
        {
            return 0;
        }

        public int DeleteHistoriesByTime(Type clazz, DateTime to, int count)
        {
            return 0;
        }


        public int DeleteTransportCommandHistories(String transportCommandId)
        {
            return 0;
        }

        public int DeleteTransportCommandHistories(TransportCommandEx transportCommand)
        {
            return 0;
        }

        public int DeleteTransportCommandHistories()
        {
            return 0;
        }

        public int DeleteTransportCommandHistoriesByCarrierName(String carrierName)
        {
            return 0;
        }

        public void DeleteTransportCommandHistory(TransportCommandHistoryEx transportCommandHistory) { }

        public int DeleteTruncateParameter(String tableName)
        {
            return 0;
        }

        public void DeleteTruncateParameter(TruncateParameterEx truncateParameter) { }

        public int DeleteVehicleHistories(String vehicleName)
        {
            return 0;
        }

        public int DeleteVehicleHistories()
        {
            return 0;
        }

        public void ExcuteTruncateQuery(String sql) { }

        public IList GetAlarmReportHistories(String alarmId, String machineName)
        {
            return null;
        }

        public IList GetAlarmReportHistories(String alarmId, String alarmCode, String machineName)
        {
            return null;
        }

        public IList GetAlarmReportHistories(String alarmId, String alarmCode, String alarmText, String machineName)
        {
            return null;
        }

        public IList GetAlarmReportHistoriesByExample(AlarmReportHistoryEx example)
        {
            return null;
        }

        public IList GetAlarmReportHistoriesByMachine(String machineName)
        {
            return null;
        }

        public IList GetAlarmReportHistoriesByMachine(Machine machine)
        {
            return null;
        }

        public IList GetAlarmReportHistoriesByMachine(String machineName, String unitName)
        {
            return null;
        }

        public IList GetAlarmReportHistoriesByMachineNames(String[] machineNames)
        {
            return null;
        }

        public IList GetApplicationHistory(String applicationName)
        {
            return null;
        }

        public IList GetHeartBeatFailHistory(String applicationName)
        {
            return null;
        }
        public int GetHistoryCountByCriteria(Dictionary<string, object> criteria)
        {
            return 0;
        }

        public int GetHistoryCountByTime(Type clazz, DateTime toDate)
        {
            return 0;
        }

        public int GetHistoryCountByTime(String className, DateTime toDate)
        {
            return 0;
        }

        public int GetHistoryCountByTime(Type clazz, DateTime from, DateTime to)
        {
            return 0;
        }

        public int GetHistoryCountByTime(String className, DateTime from, DateTime to)
        {
            return 0;
        }

        public AbstractHistoryEx GetLatestHistoryBeforeTime(Type clazz, DateTime time, Dictionary<string, object> conditions)
        {
            return null;
        }

        public String GetNativeSqlWithReplace(String nativeSql, int partitionId)
        {
            return null;
        }

        public String GetNativeSqlWithReplace(String nativeSql, String partitionId)
        {
            return null;
        }

        public TruncateParameterEx GetTruncateParameter(String tableName)
        {
            var attributes = new Dictionary<string, object> { { "TableName", tableName } };
            IList list = PersistentDao.FindByAttributes(typeof(TruncateParameterEx), attributes);
            if (list.Count > 0)
            {
                return (TruncateParameterEx)list[0];
            }

            return null;
        }

        public IList GetTruncateParameters()
        {
            return PersistentDao.FindAll(typeof(TruncateParameterEx));
        }

        public IList GetVehicleHistoriesByTransportCommandId(String transportCommandId)
        {
            return null;
        }

        public IList GetVehicleHistoriesByVehicleName(String vehicleName)
        {
            return null;
        }

        public void TruncateHistoryByDayBasePartition(TruncateParameterEx truncateParameter) { }

        public void TruncateHistoryByDayBasePartition(String nativeSql, String partitioningBase, int savePeriod, String truncateSingleOrMulti) { }

        public void TruncateHistoryByMonthBasePartition(TruncateParameterEx truncateParameter) { }

        public void TruncateHistoryByMonthBasePartition(String nativeSql, String partitioningBase, int savePeriod, String truncateSingleOrMulti) { }

        public TransportCommandHistoryEx CreateTransportCommandHistory(String jobId, String carrierId, String source, String dest, String jobType, String code, String cause, String bayId)
        {
            TransportCommandHistoryEx transportCommandHistoryEx = new TransportCommandHistoryEx();
            transportCommandHistoryEx.State = "ABORTED";
            transportCommandHistoryEx.JobId = jobId;
            transportCommandHistoryEx.CarrierId = carrierId;
            transportCommandHistoryEx.Source = source;
            transportCommandHistoryEx.Dest = dest;
            transportCommandHistoryEx.JobType = jobType;
            transportCommandHistoryEx.Time = DateTime.Now; //(TimeUtils.getCurrentTimestamp());
            transportCommandHistoryEx.Code = code;
            transportCommandHistoryEx.Reason = cause;
            transportCommandHistoryEx.BayId = bayId;

            PersistentDao.Save(transportCommandHistoryEx);
            return transportCommandHistoryEx;
        }
        public IList GetHistoriesBetweenTimes(Type paramClass, DateTime paramDate1, DateTime paramDate2, Dictionary<string, object> paramMap)
        {
            return null;
        }

        public IList GetHistoriesBetweenTimes(string paramString, DateTime paramDate1, DateTime paramDate2, Dictionary<string, object> paramMap)
        {
            return null;
        }

        public int DeleteHistoriesByTime(string paramString, DateTime paramDate, int paramInt)
        {
            return 0;
        }

        public IList GetHistoriesByCriteria(Dictionary<string, object> paramCriteria, int paramInt1, int paramInt2)
        {
            return null;
        }

        public AbstractHistoryEx GetLatestHistoryBeforeTime(object paramClass, DateTime paramDate, Dictionary<string, object> paramMap)
        {
            return null;
        }



    }
}
