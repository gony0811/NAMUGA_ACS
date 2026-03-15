using ACS.Core.Base;
using ACS.Core.Material.Model;
using ACS.Core.Material;
using ACS.Core.Resource;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Manager.Material
{

    public class MaterialManagerExImplement : AbstractManager, IMaterialManagerEx
    {
        //private static SimpleDateFormat simpleDateFormat = new SimpleDateFormat("yyyyMMddHHmmss");
        protected IResourceManagerEx resourceManager;


        public IResourceManagerEx GetResourceManager()
        {
            return this.resourceManager;
        }

        public void SetResourceManager(IResourceManagerEx resourceManager)
        {
            this.resourceManager = resourceManager;
        }

        public void CreateCarrier(CarrierEx carrier)
        {
            this.PersistentDao.Save(carrier);
            logger.Info("carrier{" + carrier.Id + "} was created " + "{" + carrier.Id + " : " + carrier.CarrierLoc + "}");
        }

        public CarrierEx CreateCarrier(String id, String type, String carrierLoc)
        {
            CarrierEx carrier = new CarrierEx();

            carrier.Id = id;
            carrier.CarrierLoc = carrierLoc;
            carrier.Type = type;
            carrier.CreateTime = DateTime.Now;
            CreateCarrier(carrier);
            return carrier;
        }

        public CarrierEx GetCarrier(String carrierId)
        {
            StringBuilder sbCarrierId = new StringBuilder(carrierId);
            return (CarrierEx)this.PersistentDao.Find(typeof(CarrierEx), sbCarrierId, false);
        }

        public IList GetCarriersByCarrierLoc(String carrierLoc)
        {
            var attributes = new Dictionary<string, object> { { "CarrierLoc", carrierLoc } };
            return this.PersistentDao.FindByAttributes(typeof(CarrierEx), attributes);
        }

        public IList GetCarriers()
        {
            return this.PersistentDao.FindAll(typeof(CarrierEx));
        }

        public bool UpdateCarrier(CarrierEx carrier)
        {
            this.PersistentDao.Update(carrier);
            return true;
        }

        public bool UpdateCarrierLoc(CarrierEx carrier, String carrierLoc)
        {
            String previousCarrierLoc = carrier.CarrierLoc;
            if (previousCarrierLoc.Equals(carrierLoc))
            {
                //logger.fine("not necessary to update it, because occupied is already {" + previousCarrierLoc + "}", "", carrierLoc);
                return false;
            }
            carrier.CarrierLoc = carrierLoc;

            return UpdateCarrier(carrier);
        }

        public int DeleteCarrier(String carrierId)
        {
            //logger.fine("carrier{" + carrierId + "} was deleted");
            StringBuilder sbCarrierId = new StringBuilder(carrierId);
            return this.PersistentDao.Delete(typeof(CarrierEx), sbCarrierId);
        }

        public int DeleteCarrierByCarrierLoc(String carrierLoc)
        {
            return this.PersistentDao.DeleteByAttribute(typeof(CarrierEx), "CarrierLoc", carrierLoc);
        }

        public int UpdateCarrierLoc(String carrierId, String carrierLoc)
        {
            return this.PersistentDao.UpdateByAttribute(typeof(CarrierEx), "CarrierLoc", carrierLoc, "CarrierId", carrierId);
        }

        public int DeleteCarrier(CarrierEx carrier)
        {
            StringBuilder sbCarrierId = new StringBuilder(carrier.Id);
            return this.PersistentDao.Delete(typeof(CarrierEx), sbCarrierId);
        }

        public CarrierEx GetCarrierByVehicleId(String vehicleId)
        {
            var attributes = new Dictionary<string, object> { { "CarrierLoc", vehicleId } };
            IList carriers = this.PersistentDao.FindByAttributes(typeof(CarrierEx), attributes);
            if (carriers.Count == 0)
            {
                //logger.info("carriers in {" + vehicleId + "} does not exist");
                return null;
            }
            return (CarrierEx)carriers[0];
        }
    }

}
