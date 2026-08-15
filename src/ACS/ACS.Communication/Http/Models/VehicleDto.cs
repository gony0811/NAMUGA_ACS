using System;
using System.Collections.Generic;

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

        /// <summary>EXCHANGE(v2): 차량 슬롯 상태 (NA_R_VEHICLE_SLOT, slotNo 오름차순 4행)</summary>
        public List<VehicleSlotDto> Slots { get; set; } = new List<VehicleSlotDto>();
    }

    /// <summary>차량 슬롯 1행 (NA_R_VEHICLE_SLOT)</summary>
    public class VehicleSlotDto
    {
        public int SlotNo { get; set; }
        /// <summary>INSERT(투입 1|2) / RETRIEVE(회수 3|4)</summary>
        public string Role { get; set; }
        /// <summary>EMPTY / OCCUPIED</summary>
        public string State { get; set; }
        /// <summary>예약·점유한 Job ID (빈값 = 미예약)</summary>
        public string JobId { get; set; }
        /// <summary>NEW(신자재) / OLD(구자재) — OCCUPIED 시</summary>
        public string Phase { get; set; }
        public DateTime? UpdatedTime { get; set; }
    }
}
