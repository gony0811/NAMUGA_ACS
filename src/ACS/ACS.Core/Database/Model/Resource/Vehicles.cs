namespace ACS.Core.Database.Model.Resource;

public class Vehicles : Vehicle
{
    public static float AVAIALBE_VOLTAGE_RGV = (float)23.5;
    public static float AVAIABLE_CHARGE_MAX_VOLTAGE_RGV = (float)26.0;

    public virtual DateTime LastChargeTime { get; set; }
    public virtual float LastChargeBattery { get; set; }
}
