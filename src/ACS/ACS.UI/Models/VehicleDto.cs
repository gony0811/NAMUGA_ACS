namespace ACS.UI.Models;

public class VehicleDto
{
    public string VehicleId { get; set; }
    public string State { get; set; }
    public string ConnectionState { get; set; }
    public string ProcessingState { get; set; }
    public string RunState { get; set; }
    public string AlarmState { get; set; }
    public string TransferState { get; set; }
    public int BatteryRate { get; set; }
    public float BatteryVoltage { get; set; }
    public string CurrentNodeId { get; set; }
    public string AcsDestNodeId { get; set; }
    public string VehicleDestNodeId { get; set; }
    public string TransportCommandId { get; set; }
    public string BayId { get; set; }
    public string CarrierType { get; set; }
}
