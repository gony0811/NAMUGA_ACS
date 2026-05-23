namespace ACS.Communication.Http.Models
{
    /// <summary>
    /// NA_X_APPLICATION 행을 UI로 노출하기 위한 DTO.
    /// State: active / inactive / hang / stanby, Type: trans / ei / daemon / control / host / query / report / emulator
    /// </summary>
    public class ApplicationDto
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string State { get; set; }
        public string RunningHardware { get; set; }
        public string StartTime { get; set; }
        public string CheckTime { get; set; }
        public string Description { get; set; }
    }
}
