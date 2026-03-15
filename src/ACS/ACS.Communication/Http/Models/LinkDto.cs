namespace ACS.Communication.Http.Models
{
    public class LinkDto
    {
        public string Id { get; set; }
        public string FromNodeId { get; set; }
        public string ToNodeId { get; set; }
        public string Availability { get; set; }
        public int Length { get; set; }
        public int Speed { get; set; }
    }
}
