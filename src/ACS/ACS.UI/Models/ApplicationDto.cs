namespace ACS.UI.Models;

/// <summary>
/// 백엔드 /api/applications 응답 (NA_X_APPLICATION 미러)
/// State: active / inactive / hang / stanby
/// Type: trans / ei / daemon / control / host / query / report / emulator
/// </summary>
public class ApplicationDto
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string State { get; set; } = "";
    public string RunningHardware { get; set; } = "";
    public string? StartTime { get; set; }
    public string? CheckTime { get; set; }
    public string? Description { get; set; }
}
