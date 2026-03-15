using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Material.Model;
using System.Collections;

namespace ACS.Core.Material
{
    public interface IMaterialManagerEx
    {
        void CreateCarrier(CarrierEx paramCarrier);

        CarrierEx CreateCarrier(String paramString1, String paramString2, String paramString3);

        IList GetCarriers();

        CarrierEx GetCarrier(String paramString);

        CarrierEx GetCarrierByVehicleId(String paramString);

        IList GetCarriersByCarrierLoc(String paramString);

        bool UpdateCarrier(CarrierEx paramCarrier);

        bool UpdateCarrierLoc(CarrierEx paramCarrier, String paramString);

        int UpdateCarrierLoc(String paramString1, String paramString2);

        int DeleteCarrier(String paramString);

        int DeleteCarrier(CarrierEx paramCarrier);

        int DeleteCarrierByCarrierLoc(String paramString);
    }
}
