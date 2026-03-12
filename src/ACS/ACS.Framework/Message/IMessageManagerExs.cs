using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Message;
using ACS.Framework.Message.Model;
using System.Xml;

namespace ACS.Framework.Message
{
    public interface IMessageManagerExs : IMessageManagerEx
    {
        XmlDocument CreateVehicleOrderCommandDocument(VehicleMessageEx vehicleMessage);
        XmlDocument CreateVehiclePermit0031CommandDocument(VehicleMessageEx vehicleMessage);

        void SendVehicleOrderCommand(VehicleMessageEx vehicleMessage);

        void SendVehiclePermit0031Command(VehicleMessageEx vehicleMessage);
        void SendVehicleMessageMCodeAgvChargingFail(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeRecoverMissMagTag(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeRecoverMissMagTagFail(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeRecoverMissMagTagSuccess(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeRecoverAgvOutRail(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeRecoverAgvOutRailSuccess(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeRecoverAgvOutRailFail(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeAgvAutoStart(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeAgvAutoStartSuccess(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeAgvAutoStartFail(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeAgvTurnPbsOff(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeAgvTurnPbsOffSuccess(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeAgvTurnPbsOffFail(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeRecoveryAgvBack(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeRecoveryAgvBackSuccess(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeRecoveryAgvBackFail(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeAgvSensorSonic(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeAgvSensorSonicSuccess(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeAgvSensorSonicFail(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeHmiVersion(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleStopCommand(VehicleMessageEx vehicleMessage);
    }
}
