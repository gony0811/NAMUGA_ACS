using System;

namespace ACS.Communication.Http.Models
{
    /// <summary>
    /// NA_T_VEHICLE_HISTORY 한 건을 표현하는 전송 DTO.
    /// <see cref="Time"/>은 항상 UTC(Kind=Utc)로 직렬화되며, 로컬 시간 변환은 클라이언트가 담당한다.
    /// </summary>
    public class VehicleHistoryDto
    {
        public string Id { get; set; }
        public DateTime? Time { get; set; }   // UTC
        public int PartitionId { get; set; }

        public string VehicleId { get; set; }
        public string BayId { get; set; }
        public string CarrierType { get; set; }
        public string ConnectionState { get; set; }
        public string AlarmState { get; set; }
        public string ProcessingState { get; set; }
        public string CurrentNodeId { get; set; }
        public string TransportCommandId { get; set; }
        public string Path { get; set; }
        public DateTime? NodeCheckTime { get; set; }
        public string State { get; set; }
        public string Installed { get; set; }
        public string TransferState { get; set; }
        public string RunState { get; set; }
        public string FullState { get; set; }
        public string MessageName { get; set; }
        public string AcsDestNodeId { get; set; }
        public string VehicleDestNodeId { get; set; }
    }
}
