using ACS.Framework.Resource.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Resource.Model
{
    public class VehicleExs : VehicleEx
    {
        public static float AVAIALBE_VOLTAGE_RGV = (float)23.5;
        public static float AVAIABLE_CHARGE_MAX_VOLTAGE_RGV = (float)26.0;

        public virtual DateTime LastChargeTime { get; set; }
        public virtual float LastChargeBattery { get; set; }

    }
}
