using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Resource.Model
{
    public class VehicleEx : Entity
    {
        public static string STATE_REMOVED = "REMOVED";
        public static string STATE_INSTALLED = "INSTALLED";
        public static string STATUS_DOWN = "DOWN";
        public static string STATUS_IDLE = "IDLE";
        public static string STATUS_RUN = "RUN";
        public static string STATUS_CHARGE = "CHARGE";
        public static string STATUS_MANUAL = "MANUAL";
        public static string STATUS_EXITMAP = "EXITMAP";
        public static string STATUS_OFF = "OFF";
        public static string CARRIERTYPE_CARRIER = "CARRIER";
        public static string CARRIERTYPE_TRAY = "TRAY";
        public static string CARRIERTYPE_FOUP = "FOUP";
        public static string CONNECTIONSTATE_CONNECT = "CONNECT";
        public static string CONNECTIONSTATE_DISCONNECT = "DISCONNECT";
        public static string ALARMSTATE_ALARM = "ALARM";
        public static string ALARMSTATE_NOALARM = "NOALARM";
        public static string PROCESSINGSTATE_IDLE = "IDLE";
        public static string PROCESSINGSTATE_RUN = "RUN";
        public static string PROCESSINGSTATE_CHARGE = "CHARGE";
        public static string PROCESSINGSTATE_PARK = "PARK";
        public static string STATE_ALIVE = "ALIVE";
        public static string STATE_BANNED = "BANNED";
        public static string RUNSTATE_RUN = "RUN";
        public static string RUNSTATE_STOP = "STOP";
        public static string FULLSTATE_FULL = "FULL";
        public static string FULLSTATE_EMPTY = "EMPTY";
        public static string INSTALL_INSTALLED = "T";
        public static string INSTALL_REMOVE = "F";
        public static float AVAIALBE_VOLTAGE = 25.0F;
        public static float AVAIALBE_VOLTAGE_LIMIT = 23.0F;
        public static float AVAIALBE_CAPACITY = 40.0F;
        public static string TRANSFERSTATE_ASSIGNED = "ASSIGNED";
        public static string TRANSFERSTATE_ASSIGNED_ENROUTE = "ASSIGNED_ENROUTE";
        public static string TRANSFERSTATE_ASSIGNED_PARKED = "ASSIGNED_PARKED";
        public static string TRANSFERSTATE_ASSIGNED_ACQUIRING = "ASSIGNED_ACQUIRING";
        public static string TRANSFERSTATE_ASSIGNED_DEPOSITING = "ASSIGNED_DEPOSITING";
        public static string TRANSFERSTATE_ACQUIRE_COMPLETE = "ACQUIRE_COMPLETE";
        public static string TRANSFERSTATE_DEPOSIT_COMPLETE = "DEPOSIT_COMPLETE";
        public static string TRANSFERSTATE_NOTASSIGNED = "NOTASSIGNED";

        public static string VENDOR_FIXMODE = "FIXMODE";
	    public static string  VENDOR_SPECIAL = "SPECIAL";
        public static string VENDOR_COMMON = "COMMON"; //221212 copy


        public virtual string VehicleId { get; set; }
        public virtual string CommType { get; set; } = "NIO";
        public virtual string CommId { get; set; }
        public virtual string Vendor { get; set; }
        public virtual string Version { get; set; }
        public virtual string BayId { get; set; }
        public virtual string CarrierType { get; set; }
        public virtual string ConnectionState { get; set; }
        public virtual string AlarmState { get; set; }
        public virtual string ProcessingState { get; set; }
        public virtual string RunState { get; set; }
        public virtual string FullState { get; set; }
        public virtual string State { get; set; }
        public virtual int BatteryRate { get; set; }
        public virtual float BatteryVoltage { get; set; }
        public virtual string CurrentNodeId { get; set; }
        public virtual string AcsDestNodeId { get; set; }
        public virtual string VehicleDestNodeId { get; set; }
        public virtual string TransportCommandId { get; set; }
        public virtual string Path { get; set; }
        public virtual DateTime NodeCheckTime { get; set; }
        public virtual string Installed { get; set; }
        public virtual string TransferState { get; set; }
        public virtual DateTime EventTime { get; set; }
        public virtual string PlcVersion { get; set; }
        ////181109 if Wifi, Try to Restart NIO
        //public virtual string IP { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("vehicle{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", vehicleId=").Append(this.VehicleId);
            sb.Append(", commType=").Append(this.CommType);
            sb.Append(", commId=").Append(this.CommId);
            sb.Append(", vendor=").Append(this.Vendor);
            sb.Append(", version=").Append(this.Version);
            sb.Append(", plcVersion=").Append(this.PlcVersion);
            sb.Append(", bayId=").Append(this.BayId);
            sb.Append(", carrierType=").Append(this.CarrierType);
            sb.Append(", connectionState=").Append(this.ConnectionState);
            sb.Append(", alarmState=").Append(this.AlarmState);
            sb.Append(", processingState=").Append(this.ProcessingState);
            sb.Append(", state=").Append(this.State);
            sb.Append(", batteryRate=").Append(this.BatteryRate);
            sb.Append(", batteryVoltage=").Append(this.BatteryVoltage);
            sb.Append(", currentNodeId=").Append(this.CurrentNodeId);
            sb.Append(", transportCommandId=").Append(this.TransportCommandId);
            sb.Append(", path=").Append(this.Path);
            sb.Append(", nodeCheckTime=").Append(this.NodeCheckTime);
            sb.Append(", eventTime=").Append(this.EventTime);
            sb.Append(", installed=").Append(this.Installed);
            sb.Append(", transferState=").Append(this.TransferState);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
