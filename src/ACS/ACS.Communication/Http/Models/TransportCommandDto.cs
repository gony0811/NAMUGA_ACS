using System;

namespace ACS.Communication.Http.Models
{
    public class TransportCommandDto
    {
        public string Id { get; set; }
        public int Priority { get; set; }
        public string State { get; set; }
        public string VehicleId { get; set; }
        public string CarrierId { get; set; }
        public string Source { get; set; }
        public string Dest { get; set; }
        public string Path { get; set; }
        public DateTime? CreateTime { get; set; }
        public DateTime? AssignedTime { get; set; }
        public DateTime? CompletedTime { get; set; }
        public string BayId { get; set; }
        public string JobType { get; set; }
    }
}
