using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;
using ACS.Framework.Application.Model;
using ACS.Framework.Resource;
using ACS.Framework.Material;
using ACS.Framework.History;
using ACS.Framework.History.Model;
using System.Collections;
using ACS.Framework.Application.Model;
using ACS.Extension.Framework.History.Model;
using ACS.Framework.Resource.Model;
using ACS.Framework.Message.Model;

namespace ACS.Extension.Framework.History
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
