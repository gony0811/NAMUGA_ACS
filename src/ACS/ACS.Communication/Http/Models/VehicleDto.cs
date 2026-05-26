using System;

namespace ACS.Communication.Http.Models
{
    public class VehicleDto
    {
        public string VehicleId { get; set; }
        public string CommType { get; set; }
        public string CommId { get; set; }
        public string Vendor { get; set; }
        public string Version { get; set; }
        public string PlcVersion { get; set; }
        public string State { get; set; }
        public string Installed { get; set; }
        public string ConnectionState { get; set; }
        public string ProcessingState { get; set; }
        public string RunState { get; set; }
        public string FullState { get; set; }
        public string AlarmState { get; set; }
        public string TransferState { get; set; }
        public int BatteryRate { get; set; }
        public float BatteryVoltage { get; set; }
        public string CurrentNodeId { get; set; }
        public string AcsDestNodeId { get; set; }
        public string VehicleDestNodeId { get; set; }
        public string TransportCommandId { get; set; }
        public string Path { get; set; }
        public DateTime? NodeCheckTime { get; set; }
        public DateTime? EventTime { get; set; }
        public DateTime? LastChargeTime { get; set; }
        public float LastChargeBattery { get; set; }
        public string BayId { get; set; }
        public string CarrierType { get; set; }
    }
}
