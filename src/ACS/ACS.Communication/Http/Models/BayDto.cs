namespace ACS.Communication.Http.Models
{
    public class BayDto
    {
        public string Id { get; set; }
        public int Floor { get; set; }
        public string Description { get; set; }
        public string AgvType { get; set; }
        public float ChargeVoltage { get; set; }
        public float LimitVoltage { get; set; }
        public int IdleTime { get; set; }
        public string ZoneMove { get; set; }
        public string Traffic { get; set; }
        public string StopOut { get; set; }
    }
}
