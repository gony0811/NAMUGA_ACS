using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;
using ACS.Core.Application.Model;
using ACS.Core.Resource;
using ACS.Core.Material;
using ACS.Core.History;
using ACS.Core.History.Model;
using System.Collections;
using ACS.Core.Application.Model;
using ACS.Core.History.Model;
using ACS.Core.Resource.Model;
using ACS.Core.Message.Model;

namespace ACS.Core.History
{
    public interface IHistoryManagerExs : IHistoryManagerEx
    {
        void CreateVehicleCrossHistory(string vehicleId, string inNode, DateTime indate, string endNode);
        void CreateVehicleSearchPathHistory(VehicleSearchPathHistory vehicleSearchPath);

        VehicleCrossWaitHistoryEx CreateVehicleCrossWaitHistory(VehicleCrossWaitEx vehicleCrossWait, DateTime permitTime);

        VehicleCrossWaitHistoryEx CreateVehicleCrossWaitHistory(VehicleMessageEx vehicleMessage, DateTime permitTime);

        void CreateVehicleCrossWaitHistory(VehicleCrossWaitHistoryEx vehicleCrossWaitHistory);
        void CreateMismatchAndFlyHistory(MismatchAndFlyHistoryEx mismatchAndFlyHistory);

        void CreateAlarmTimeHistory(AlarmTimeHistoryEx alarmTimetHistory);

        void CreateNioHistory(NioHistory nioHistory);

        void AddVehiclePathHistory(VehicleEx vehicle, string path, string type, int distance);
    }
}
