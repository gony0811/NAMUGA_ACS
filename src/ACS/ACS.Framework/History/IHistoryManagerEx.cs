using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Application.Model;
using ACS.Framework.Alarm.Model;
using ACS.Framework.History.Model;
using ACS.Framework.Message.Model;
using ACS.Framework.Resource.Model;
using ACS.Framework.Transfer.Model;
using ACS.Framework.Alarm;
using ACS.Framework.Message.Model.Server;
using ACS.Framework.Resource.Model.Factory.Machine;
using NHibernate.Criterion;
using System.Collections;
namespace ACS.Framework.History
{
    public interface IHistoryManagerEx
    {
        int DeleteHistoriesByTime(Type paramClass, DateTime paramDate, int paramInt);

        int DeleteHistoriesByTime(String paramString, DateTime paramDate, int paramInt);

        int GetHistoryCountByTime(Type paramClass, DateTime paramDate);

        int GetHistoryCountByTime(String paramString, DateTime paramDate);

        int GetHistoryCountByTime(Type paramClass, DateTime paramDate1, DateTime paramDate2);

        int GetHistoryCountByTime(String paramString, DateTime paramDate1, DateTime paramDate2);

        IList GetHistoriesBetweenTimes(Type paramClass, DateTime paramDate1, DateTime paramDate2, Dictionary<string, object> paramMap);

        IList GetHistoriesBetweenTimes(String paramString, DateTime paramDate1, DateTime paramDate2, Dictionary<string, object> paramMap);

        int GetHistoryCountByCriteria(DetachedCriteria paramDetachedCriteria);

        IList GetHistoriesByCriteria(DetachedCriteria paramDetachedCriteria, int paramInt1, int paramInt2);

        AbstractHistoryEx GetLatestHistoryBeforeTime(Type paramClass, DateTime paramDate, Dictionary<string, object> paramMap);

        TransportCommandHistoryEx CreateTransportCommandHistory(TransportCommandEx paramTransportCommand);

        TransportCommandHistoryEx CreateTransportCommandHistory(TransportCommandEx paramTransportCommand, String paramString1, String paramString2);

        TransportCommandHistoryEx CreateTransportCommandHistory(TransportCommandEx paramTransportCommand, String paramString);

        TransportCommandHistoryEx CreateTransportCommandHistory(TransferMessageEx paramTransferMessage);

        TransportCommandHistoryEx CreateTransportCommandHistory(String paramString1, String paramString2, String paramString3, String paramString4, String paramString5, String paramString6, String paramString7);

        void CreateTransportCommandHistory(TransportCommandHistoryEx paramTransportCommandHistory);

        int DeleteHistoriesBetweenTimes(Type paramClass, DateTime paramDate1, DateTime paramDate2);

        void DeleteTransportCommandHistory(TransportCommandHistoryEx paramTransportCommandHistory);

        int DeleteTransportCommandHistories(String paramString);

        int DeleteTransportCommandHistories(TransportCommandEx paramTransportCommand);

        int DeleteTransportCommandHistoriesByCarrierName(String paramString);

        int DeleteTransportCommandHistories();

        AlarmReportHistoryEx CreateAlarmReportHistory(AlarmReportMessage paramAlarmReportMessage);

        void CreateAlarmReportHistory(AlarmReportHistoryEx paramAlarmReportHistory);

        AlarmReportHistoryEx CreateAlarmReportHistory(AlarmEx paramAlarm, String paramString);

        AlarmReportHistoryEx CreateAlarmReportHistory(String paramString1, String paramString2, String paramString3, String paramString4, String paramString5);

        AlarmReportHistoryEx CreateAlarmReportHistory(String paramString1, String paramString2, String paramString3, String paramString4, String paramString5, String paramString6);

        AlarmReportHistoryEx CreateAlarmReportHistory(String paramString1, String paramString2, String paramString3);

        AlarmReportHistoryEx CreateAlarmReportHistory(String paramString1, String paramString2, String paramString3, String paramString4);

        AlarmReportHistoryEx CreateAlarmReportHistory(ACS.Framework.Alarm.Model.AlarmReport paramAlarmReport, String paramString);

        IList GetAlarmReportHistories(String paramString1, String paramString2);

        IList GetAlarmReportHistories(String paramString1, String paramString2, String paramString3);

        IList GetAlarmReportHistories(String paramString1, String paramString2, String paramString3, String paramString4);

        IList GetAlarmReportHistoriesByMachine(String paramString);

        IList GetAlarmReportHistoriesByMachine(Machine paramMachine);

        IList GetAlarmReportHistoriesByMachine(String paramString1, String paramString2);

        IList GetAlarmReportHistoriesByExample(AlarmReportHistoryEx paramAlarmReportHistory);

        IList GetAlarmReportHistoriesByMachineNames(String[] paramArrayOfString);

        void DeleteAlarmReportHistory(AlarmReportHistoryEx paramAlarmReportHistory);

        int DeleteAlarmReportHistories(String paramString1, String paramString2);

        int DeleteAlarmReportHistories(String paramString1, String paramString2, String paramString3);

        int DeleteAlarmReportHistories(String paramString1, String paramString2, String paramString3, String paramString4);

        int DeleteAlarmReportHistoriesByMachine(String paramString);

        int DeleteAlarmReportHistoriesByMachine(Machine paramMachine);

        int DeleteAlarmReportHistoriesByMachine(String paramString1, String paramString2);

        int DeleteAlarmReportHistories();

        VehicleHistoryEx CreateVehicleHistory(TransferMessageEx paramTransferMessage);

        VehicleHistoryEx CreateVehicleHistory(VehicleMessageEx paramVehicleMessageACS);

        VehicleHistoryEx CreateVehicleHistory(VehicleEx paramVehicleACS, String paramString);

        void CreateVehicleHistory(VehicleHistoryEx paramVehicleHistory);

        IList GetVehicleHistoriesByVehicleName(String paramString);

        IList GetVehicleHistoriesByTransportCommandId(String paramString);

        int DeleteVehicleHistories(String paramString);

        int DeleteVehicleHistories();

        VehicleBatteryHistoryEx CreateVehicleBatteryHistory(VehicleMessageEx paramVehicleMessageACS);

        VehicleBatteryHistoryEx CreateVehicleBatteryHistory(VehicleEx paramVehicleACS);

        void CreateVehicleBatteryHistory(VehicleBatteryHistoryEx paramVehicleBatteryHistoryACS);

        HeartBeatFailHistoryEx CreateHeartBeatFailHistory(ACS.Framework.Application.Model.Application paramApplication);

        void CreateHeartBeatFailHistory(HeartBeatFailHistoryEx paramHeartBeatFailHistoryEx);

        IList GetHeartBeatFailHistory(String paramString);

        void DeleteHeartBeatFailHistory(HeartBeatFailHistoryEx paramHeartBeatFailHistoryEx);

        int DeleteHeartBeatFailHistories(String paramString);

        int DeleteHeartBeatFailHistories();

        ApplicationHistoryEx CreateApplicationHistory(ACS.Framework.Application.Model.Application paramApplication);

        void CreateApplicationHistory(ApplicationHistoryEx paramApplicationHistoryEx);

        IList GetApplicationHistory(String paramString);

        void DeleteApplicationlHistory(ApplicationHistoryEx paramApplicationHistoryEx);

        int DeleteApplicationlHistories(String paramString);

        int DeleteApplicationlHistories();

        TruncateParameterEx CreateTruncateParameter(String paramString1, String paramString2, String paramString3, int paramInt, String paramString4);

        void CreateTruncateParameter(TruncateParameterEx paramTruncateParameterEx);

        int DeleteTruncateParameter(String paramString);

        void DeleteTruncateParameter(TruncateParameterEx paramTruncateParameterEx);

        TruncateParameterEx GetTruncateParameter(String paramString);

        IList GetTruncateParameters();

        void TruncateHistoryByDayBasePartition(TruncateParameterEx paramTruncateParameterEx);

        void TruncateHistoryByDayBasePartition(String paramString1, String paramString2, int paramInt, String paramString3);

        void TruncateHistoryByMonthBasePartition(TruncateParameterEx paramTruncateParameterEx);

        void TruncateHistoryByMonthBasePartition(String paramString1, String paramString2, int paramInt, String paramString3);

        String GetNativeSqlWithReplace(String paramString, int paramInt);

        String GetNativeSqlWithReplace(String paramString1, String paramString2);

        void ExcuteTruncateQuery(String paramString);

        TransactionHistoryEx CreateTransactionHistory(String paramString1, String paramString2, DateTime paramDate1, DateTime paramDate2, int paramInt1, int paramInt2, String paramString3);

        TransactionHistoryEx CreateTransactionHistory(String paramString1, String paramString2, DateTime paramDate1, DateTime paramDate2, int paramInt1, int paramInt2, String paramString3, String paramString4);

        TransactionHistoryEx CreateTransactionHistory(String paramString1, String paramString2, DateTime paramDate1, DateTime paramDate2, int paramInt1, int paramInt2, String paramString3, String paramString4, String paramString5);

        void CreateTransactionHistory(TransactionHistoryEx paramTransactionHistoryEx);

        AlarmHistoryEx CreateAlarmHistoryInstance();

        AlarmReportHistoryEx CreateAlarmReportHistoryInstance();

        ApplicationHistoryEx CreateApplicationHistoryInstance();

        HeartBeatFailHistoryEx CreateHeartBeatFailHistoryInstance();

        TransactionHistoryEx CreateTransactionHistoryInstance();

        TransportCommandHistoryEx CreateTransportCommandHistoryInstance();

        VehicleHistoryEx CreateVehicleHistoryInstance();

        TransportCommandHistoryEx CreateTransportCommandHistory(String paramString1, String paramString2, String paramString3, String paramString4, String paramString5, String paramString6, String paramString7, String paramString8);
    }
}
