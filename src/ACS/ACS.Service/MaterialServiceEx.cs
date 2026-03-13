using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;
using ACS.Framework.Alarm.Model;
using ACS.Framework.Message.Model;
using ACS.Framework.Message.Model.Ui;
using ACS.Framework.Resource.Model;
using ACS.Framework.Transfer.Model;
using ACS.Framework.Material.Model;
using ACS.Framework.Logging;
using ACS.Framework.Base;

namespace ACS.Service
{
    public class MaterialServiceEx : AbstractServiceEx
    {
        public Logger logger = Logger.GetLogger(typeof(MaterialServiceEx));
        
        public MaterialServiceEx()
            : base()
        {
        }

        public override void Init()
        {
            base.Init();
        }
        public bool CreateCarrier(TransferMessageEx transferMessage)
        {

            bool result = false;

            TransportCommandEx tansporCommand = this.TransferManager.GetTransportCommandByCarrierId(transferMessage.CarrierId);

            if (tansporCommand != null)
            {
                transferMessage.ReplyCode = AbstractManager.ID_RESULT_CARRIER_HAS_ANOTHER_TRANSPORTJOB.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_CARRIER_HAS_ANOTHER_TRANSPORTJOB.Item2;
                //logger.Warn("tansporCommand{" + tansporCommand.getId() + "} already exist related on carrier{" + transferMessage.CarrierId + "}.", transferMessage);
                return false;
            }
            else
            {
                this.MaterialManager.DeleteCarrier(transferMessage.CarrierId);

                CarrierEx carrier = this.MaterialManager.CreateCarrier(transferMessage.CarrierId, transferMessage.CarrierType, transferMessage.SourceMachine + ":" + transferMessage.SourceUnit);

                //logger.fine("carrier{" + carrier.getId() + "} was created", transferMessage);
                transferMessage.Carrier = carrier;
                result = true;
            }

            //		CarrierEx carrier = this.MaterialManager.GetCarrier(transferMessage.CarrierId);
            //		if (carrier == null) {
            //			
            //			carrier = this.MaterialManager.CreateCarrier(transferMessage.CarrierId, transferMessage.CarrierType, transferMessage.SourceMachine+":"+transferMessage.SourceUnit);
            //			
            //			//logger.fine("carrier{" + carrier.getId() + "} was created", transferMessage);
            //			transferMessage.setCarrier(carrier);
            //			result = true;
            //		} else {
            //			transferMessage.setCause(AbstractManager.RESULT_CARRIER_HAS_ANOTHER_TRANSPORTJOB);
            //			//logger.Warn("carrier{" + carrier.getId() + "} already exist in repository", transferMessage);
            //		}

            return result;
        }

        public bool CreateCarrier(VehicleMessageEx vehicleMessage) {

		bool result = false;
		
		TransportCommandEx tansporCommand = this.TransferManager.GetTransportCommandByCarrierId(vehicleMessage.CarrierId);
		
		if (tansporCommand != null) 
		{
                vehicleMessage.ResultCode = AbstractManager.ID_RESULT_CARRIER_HAS_ANOTHER_TRANSPORTJOB.Item1;
                vehicleMessage.Cause= AbstractManager.ID_RESULT_CARRIER_HAS_ANOTHER_TRANSPORTJOB.Item2;
                //logger.Warn("carrier{" + vehicleMessage.CarrierId + "} already exist in repository TransportCommand", vehicleMessage);
                return false;
		} 
		else 
		{
			this.MaterialManager.DeleteCarrier(vehicleMessage.CarrierId);
			
			CarrierEx carrier = this.MaterialManager.CreateCarrier(vehicleMessage.CarrierId, vehicleMessage.CarrierType, tansporCommand.Dest);
			
			//logger.fine("carrier{" + carrier.getId() + "} was created", vehicleMessage);
			vehicleMessage.CarrierId=carrier.Id;
			result = true;
		}
		
		
//		CarrierEx carrier = this.MaterialManager.GetCarrier(vehicleMessage.CarrierId);
//		if (carrier == null) {
//			
//			TransportCommandEx transportCommand = vehicleMessage.getTransportCommand();
//			carrier = this.MaterialManager.CreateCarrier(vehicleMessage.CarrierId, vehicleMessage.CarrierType, transportCommand.getDest());
//			
//			//logger.fine("carrier{" + carrier.getId() + "} was created", vehicleMessage);
//			result = true;
//		} else {
//			vehicleMessage.setCause(AbstractManager.RESULT_CARRIER_HAS_ANOTHER_TRANSPORTJOB);
//			//logger.Warn("carrier{" + carrier.getId() + "} already exist in repository", vehicleMessage);
//		}

		return result;
	}

        public bool ChangeCarrierLocationToVehicle(TransferMessageEx transferMessage)
        {

            return this.MaterialManager.UpdateCarrierLoc(transferMessage.Carrier, transferMessage.VehicleId);
        }

        public bool ChangeCarrierLocationToVehicle(VehicleMessageEx vehicleMessage)
        {


            TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            if (transportCommand == null)
            {

                CarrierEx orgCarrier = this.MaterialManager.GetCarrierByVehicleId(vehicleMessage.VehicleId);
                if (orgCarrier == null)
                {
                    //??? TimeUtils 확인 필요
                    DateTime currentTime = DateTime.Now;//TimeUtils.getCurrentTime();
                    String current = DateTime.Now.ToString("yyyy-MM-ddHH:mm:ss.fff");//TimeUtils.convertTimeToString(currentTime, TimeUtils.DEFAULTFORMAT_TO_MILLI);

                    String carrierId = CarrierEx.UNKNOWN_TYPE_UNK + current;
                    String carrierType = CarrierEx.UNKNOWN_TYPE_UNK;
                    String carrierLoc = vehicleMessage.VehicleId;
                    this.MaterialManager.CreateCarrier(carrierId, carrierType, carrierLoc);
                    return true;
                }
                else
                {
                    //logger.Info("This Vehicle {" + vehicleMessage.VehicleId + "} has Carrier !!" + vehicleMessage);
                    return true;
                }
            }

            if (string.IsNullOrEmpty(transportCommand.CarrierId))
            {
                //??? TimeUtils 확인 필요
                DateTime currentTime = DateTime.Now;//TimeUtils.getCurrentTime();
                String current = DateTime.Now.ToString("yyyy-MM-ddHH:mm:ss.fff");//TimeUtils.convertTimeToString(currentTime, TimeUtils.DEFAULTFORMAT_TO_MILLI);

                String carrierId = CarrierEx.UNKNOWN_TYPE_UNK + current;
                transportCommand.CarrierId = carrierId;
            }
            CarrierEx carrier = this.MaterialManager.GetCarrier(transportCommand.CarrierId);
            if (carrier == null)
            {
                String carrierType = CarrierEx.UNKNOWN_TYPE_UNK;
                String carrierLoc = vehicleMessage.VehicleId;
                this.MaterialManager.CreateCarrier(transportCommand.CarrierId, carrierType, carrierLoc);
                return true;
            }
            return this.MaterialManager.UpdateCarrierLoc(carrier, vehicleMessage.VehicleId);
        }

        public bool ChangeCarrierLocationToDest(VehicleMessageEx vehicleMessage)
        {

            TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            CarrierEx carrier = this.MaterialManager.GetCarrier(transportCommand.CarrierId);
            return this.MaterialManager.UpdateCarrierLoc(carrier, transportCommand.Dest);
        }

        public bool ChangeCarrierLocationToSourPort(TransferMessageEx transferMessage)
        {

            return this.MaterialManager.UpdateCarrierLoc(transferMessage.Carrier, transferMessage.SourceMachine + ":" + transferMessage.SourceUnit);
        }

        public bool ChangeCarrierLocationToDestPort(TransferMessageEx transferMessage)
        {

            return this.MaterialManager.UpdateCarrierLoc(transferMessage.Carrier, transferMessage.SourceMachine + ":" + transferMessage.SourceUnit);
        }

        public void DeleteCarrier(TransferMessageEx transferMessage)
        {
            CarrierEx carrier = this.MaterialManager.GetCarrier(transferMessage.CarrierId);
            if (carrier != null)
            {
                int deletedCount = this.MaterialManager.DeleteCarrier(transferMessage.CarrierId);
                if (deletedCount > 0)
                {

                    //logger.fine("carrier{" + transferMessage.CarrierId + "} was deleted", transferMessage);
                }
            }
            else
            {
                //logger.Warn("Can not Find Carrier : " + transferMessage.TransportCommandId);
            }
        }

        public void DeleteCarrier(VehicleMessageEx vehicleMessage)
        {
            String carrierId = vehicleMessage.CarrierId;
            if (string.IsNullOrEmpty(carrierId))
            {
                TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);

                if (transportCommand != null)
                {
                    carrierId = transportCommand.CarrierId;
                    vehicleMessage.CarrierId = carrierId;
                }
            }
            CarrierEx carrier = this.MaterialManager.GetCarrier(vehicleMessage.CarrierId);
            if (carrier != null)
            {
                int deletedCount = this.MaterialManager.DeleteCarrier(vehicleMessage.CarrierId);
                if (deletedCount > 0)
                {

                    //logger.fine("carrier{" + vehicleMessage.CarrierId + "} was deleted", vehicleMessage);
                }
            }
            else
            {
                logger.Warn("Can not Find Carrier : " + vehicleMessage.CarrierId);
            }
        }

        public bool DxistCarrier(TransferMessageEx transferMessage)
        {

            bool result = false;

            CarrierEx carrier = this.MaterialManager.GetCarrier(transferMessage.CarrierId);
            if (carrier != null)
            {
                //logger.Warn("carrier{" + carrier.getId() + "} already exist in repository", transferMessage);
                result = true;
            }
            return result;
        }

        public void DeleteCarrierByCommand(TransferMessageEx transferMessage)
        {

            TransportCommandEx transportCommand = transferMessage.TransportCommand;
            if (transportCommand == null)
            {

                transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {

                int deletedCount = this.MaterialManager.DeleteCarrier(transportCommand.CarrierId);
                if (deletedCount > 0)
                {

                    //logger.fine("carrier{" + transferMessage.CarrierId + "} was deleted", transferMessage);
                }
            }
            else
            {

                //logger.Warn("can't delete carrier, transportCommandId{" + transferMessage.TransportCommandId + "}");
            }
        }
    }
}