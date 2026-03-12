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
using ACS.Framework.Transfer;
using ACS.Framework.Material.Model;
using ACS.Framework.Path;
using ACS.Framework.Path.Model;
using ACS.Framework.Resource;
using ACS.Framework.Logging;
using System.Collections;
using ACS.Utility;
using log4net;
using ACS.Extension.Framework.Base;
using ACS.Extension.Framework.Path;
using ACS.Extension.Framework.Resource.Model;
using ACS.Extension.Framework.History.Model;
using ACS.Extension.Framework.History;
using ACS.Framework.History;
using ACS.Extension.Framework.Path.Model;

namespace ACS.Service
{
    public class TransferServiceEx : AbstractServiceEx
    {
        public Logger logger = Logger.GetLogger("EventLogger");
        public ILog OCodeLog = log4net.LogManager.GetLogger("OCODELog");
        //private IPathManagerEx pathManager;
        //private IResourceManagerEx resourceManager;
        //private IRequestManagerEx requestManager;
        //	protected LogManager logManager;

        public IRequestManagerEx RequestManagerExImplement { get; set; }
        //public VehicleOrderEx VehicleOrderManager { get; set; }


        //public LogManager getLogManager()
        //{
        //    return logManager;
        //}

        //public void SetLogManager(LogManager logManager)
        //{
        //    //this.logManager = logManager;
        //}

        public TransferServiceEx()
            : base()
        { }
        public override void Init()
        {
            base.Init();
        }

        //////////////////////////////////////////////////////////////////////////////////////
        //


        public bool CreateTransportCommand(TransferMessageEx transferMessage)
        {

            bool result = false;
            if (!this.TransferManager.ExistTransportCommand(transferMessage.TransportCommandId))
            {

                transferMessage.JobType = (TransportCommandEx.JOBTYPE_AUTOCALL);
                TransportCommandEx transportCommand = this.TransferManager.CreateTransportCommand(transferMessage);
                if (transportCommand != null)
                {

                    transferMessage.TransportCommand = (transportCommand);
                    //logger.fine(transportCommand);
                    result = true;
                }
                else
                {
                    transferMessage.ReplyCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTABLETOEXECUTE.Item1;
                    transferMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTABLETOEXECUTE.Item2;
                    //logger.Error("failed to create transportCommand, " + transferMessage);
                }

            }
            else
            {
                transferMessage.ReplyCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item2;
                //logger.Error("transportCommand{" + transferMessage.TransportCommandId + "} already exist in repository, " + transferMessage);
            }

            return result;
        }

        public bool CreateTransportCommandAndVehicleAssign(TransferMessageEx transferMessage)
        {

            bool result = false;
            if (!this.TransferManager.ExistTransportCommand(transferMessage.TransportCommandId))
            {
                string vehicleId = transferMessage.VehicleId;


                transferMessage.JobType = (TransportCommandEx.JOBTYPE_AUTOCALL);
                TransportCommandEx transportCommand = this.TransferManager.CreateTransportCommand(transferMessage);
                this.TransferManager.UpdateTransportCommandVehicleId(transportCommand, vehicleId);
                if (transportCommand != null)
                {

                    transferMessage.TransportCommand = (transportCommand);
                    //logger.fine(transportCommand);
                    result = true;
                }
                else
                {
                    transferMessage.ReplyCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTABLETOEXECUTE.Item1;
                    transferMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTABLETOEXECUTE.Item2;
                    //logger.Error("failed to create transportCommand, " + transferMessage);
                }

            }
            else
            {
                transferMessage.ReplyCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item2;
                //logger.Error("transportCommand{" + transferMessage.TransportCommandId + "} already exist in repository, " + transferMessage);
            }

            return result;
        }

        public bool CreateUITransportCommand(TransferMessageEx transferMessage)
        {

            bool result = false;
            if (!this.TransferManager.ExistTransportCommand(transferMessage.TransportCommandId))
            {

                transferMessage.JobType = (TransportCommandEx.JOBTYPE_ACSMOVE);
                TransportCommandEx transportCommand = this.TransferManager.CreateTransportCommand(transferMessage);
                if (transportCommand != null)
                {

                    transferMessage.TransportCommand = (transportCommand);
                    //logger.fine(transportCommand);
                    result = true;
                }
                else
                {
                    transferMessage.ReplyCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTABLETOEXECUTE.Item1;
                    transferMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTABLETOEXECUTE.Item2;
                    //logger.Error("failed to create transportCommand, " + transferMessage);
                }

            }
            else
            {
                transferMessage.ReplyCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item2;
                //logger.Error("transportCommand{" + transferMessage.TransportCommandId + "} already exist in repository, " + transferMessage);
            }

            return result;
        }

        public bool CreateRechargeTransportCommand(VehicleMessageEx vehicleMessage)
        {

            bool result = false;
            bool exist = false;
            DateTime currentTime = DateTime.Now;
            //String current = TimeUtils.convertTimeToString(currentTime, TimeUtils.DEFAULTFORMAT_TO_SECOND);
            String current = DateTime.Now.ToString("yyyy-MM-ddHH:mm:ss");
            String commandId = "R" + vehicleMessage.VehicleId + current;
            //20190829 added by KSB
            String oldtransportCommand = "";
            TransportCommandEx transportCommand = new TransportCommandEx();

            if (vehicleMessage.TransportCommand != null)
            {
                oldtransportCommand = vehicleMessage.TransportCommand.Id;
            }

            //20190829 added by KSB
            //20200208 LSJ exist 추가로 기존CMD UPDATE 진행
            if (oldtransportCommand != null && oldtransportCommand.Length > 0)
            {
                transportCommand.Id = (oldtransportCommand);
                transportCommand.CarrierId = (oldtransportCommand);
                exist = true;
            }
            else
            {
                transportCommand.Id = (commandId);
                transportCommand.CarrierId = (commandId);

            }

            transportCommand.State = (TransportCommandEx.STATE_CREATED);
            transportCommand.Dest = (vehicleMessage.DestPortId);
            transportCommand.VehicleId = (vehicleMessage.VehicleId);
            transportCommand.CreateTime = (DateTime.Now);
            transportCommand.AssignedTime = (DateTime.Now);
            transportCommand.JobType = (TransportCommandEx.JOBTYPE_CHARGEMOVE);

            transportCommand.BayId = (vehicleMessage.BayId);


            transportCommand.LoadedTime = null;
            transportCommand.UnloadArrivedTime = null;
            transportCommand.UnloadedTime = null;
            transportCommand.LoadingTime = null;
            transportCommand.UnloadingTime = null;
            transportCommand.CompletedTime = null;
            transportCommand.StartedTime = null;



            //20.02.08 LSJ 기존 CMD UPDATE 처리
            TransportCommandEx rechargeTransportCommand = null;

            if (exist)
            {
                this.TransferManager.UpdateTransportCommand(transportCommand);
                rechargeTransportCommand = TransferManager.GetTransportCommand(transportCommand.Id);
            }
            else
            {
                rechargeTransportCommand = this.TransferManager.CreateRechargeTransportCommand(transportCommand);
            }

            if (rechargeTransportCommand != null)
            {

                vehicleMessage.TransportCommand = (transportCommand);
                vehicleMessage.TransportCommandId = (transportCommand.Id);
                vehicleMessage.CarrierId = (transportCommand.CarrierId);
                //logger.fine(transportCommand);
                result = true;
            }
            else
            {
                vehicleMessage.ResultCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTABLETOEXECUTE.Item1;
                vehicleMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTABLETOEXECUTE.Item2;
                //logger.Error("failed to create rechargeTransportCommand, " + vehicleMessage);
            }

            return result;
        }

        public bool CreateStockStationTransportCommand(VehicleMessageEx vehicleMessage)
        {

            bool result = false;
            DateTime currentTime = DateTime.Now;
            //String current = TimeUtils.convertTimeToString(currentTime, TimeUtils.DEFAULTFORMAT_TO_SECOND);
            String current = DateTime.Now.ToString("yyyy-MM-ddHH:mm:ss");
            String commandId = "S" + vehicleMessage.VehicleId + current;

            TransportCommandEx transportCommand = new TransportCommandEx();

            transportCommand.Id = (commandId);
            transportCommand.CarrierId = (vehicleMessage.TransportCommand.CarrierId);
            transportCommand.State = (TransportCommandEx.STATE_CREATED);
            //wook 20170629
            //STOCK�쑝濡� 蹂대궡�뒗 JOB�쓽 Source瑜� M_CODE媛� 諛쒖깮�븳 DESTPort濡� 諛붽퓞
            //VEHICLE OCCUPIED�씪寃쎌슦�뿉�뒗 怨좊�쇱씠 醫� �븘�슂
            if (vehicleMessage.TransportCommand != null)
            {
                transportCommand.Source = (vehicleMessage.TransportCommand.Dest);
            }
            transportCommand.Dest = (vehicleMessage.DestPortId);
            transportCommand.VehicleId = (vehicleMessage.VehicleId);
            transportCommand.CreateTime = (DateTime.Now);
            transportCommand.JobType = (TransportCommandEx.JOBTYPE_STOCK_STATION);

            //wook 20170619 BayID愿��젴 Bug Fix
            if (vehicleMessage.BayId != null && !string.IsNullOrEmpty(vehicleMessage.BayId))
            {
                transportCommand.BayId = (vehicleMessage.BayId);
            }
            else
            {
                transportCommand.BayId = (vehicleMessage.TransportCommand.BayId);
            }



            TransportCommandEx rechargeTransportCommand = this.TransferManager.CreateStockStationTransportCommand(transportCommand);

            if (rechargeTransportCommand != null)
            {

                vehicleMessage.TransportCommand = (transportCommand);
                vehicleMessage.TransportCommandId = (transportCommand.Id);
                vehicleMessage.CarrierId = (transportCommand.CarrierId);
                //logger.fine(transportCommand);
                result = true;
            }
            else
            {
                vehicleMessage.ResultCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTABLETOEXECUTE.Item1;
                vehicleMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTABLETOEXECUTE.Item2;
                //logger.Error("failed to create stockStationTransportCommand, " + vehicleMessage);
            }

            return result;
        }

        public bool DeleteTransportCommand(TransferMessageEx transferMessage)
        {

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (transferMessage.TransportCommand != null)
            {
                transportCommand = transferMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {
                //logger.fine("transportCommand{" + transportCommand.Id + "} was deleted"]=transferMessage);
                this.HistoryManager.CreateTransportCommandHistory(transportCommand, "", transferMessage.Cause);
                int resultCount = this.TransferManager.DeleteTransportCommand(transportCommand);

                if (resultCount > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                //logger.Warn("transportCommand{" + transferMessage.TransportCommandId + "} is not exist");
                return false;
            }
        }

        public bool DeleteTransportCommand(VehicleMessageEx vehicleMessage)
        {
            TransportCommandEx transportCommand = new TransportCommandEx();

            if (string.IsNullOrEmpty(vehicleMessage.TransportCommandId))
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {
                int resultCount = this.TransferManager.DeleteTransportCommand(transportCommand);
                this.HistoryManager.CreateTransportCommandHistory(transportCommand, "", vehicleMessage.Cause);

                if (resultCount > 0)
                {
                    //logger.fine("transportCommand{" + vehicleMessage.TransportCommandId + "} was deleted.");
                    return true;
                }
                logger.Warn("transportCommand{" + vehicleMessage.TransportCommandId + "} is not exist");
                return false;
            }
            logger.Warn("transportCommand{" + vehicleMessage.TransportCommandId + "} does not exist repository");
            return false;
        }

        public bool DeleteTransportCommandByCarrier(TransferMessageEx transferMessage)
        {

            int resultCount = this.TransferManager.DeleteTransportCommandsByCarrierId(transferMessage.CarrierId);

            if (resultCount > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public TransportCommandEx GetTransportCommandsByCarrier(TransferMessageEx transferMessage)
        {

            return this.TransferManager.GetTransportCommandByCarrierId(transferMessage.CarrierId);
        }

        public TransportCommandEx GetTransportCommandsByVehicle(TransferMessageEx transferMessage)
        {

            return this.TransferManager.GetTransportCommandByVehicleId(transferMessage.VehicleId);
        }

        //	public VehicleMessageEx getQueueTransportCommandFIFO(VehicleMessageEx vehicleMessage) {
        //		
        //		TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByQueueStateFIFO(vehicleMessage.VehicleId);
        //		vehicleMessage.TransportCommand=(transportCommand);
        //		vehicleMessage.TransportCommandId=(transportCommand.Id);
        //		
        //		return vehicleMessage;
        //	}

        public bool CheckTransportCommandsByVehicle(TransferMessageEx transferMessage)
        {

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (transferMessage.TransportCommand != null)
            {
                transportCommand = transferMessage.TransportCommand;
            }
            else
            {
                if (string.IsNullOrEmpty(transferMessage.TransportCommandId))
                {
                    transportCommand = this.TransferManager.GetTransportCommandByVehicleId(transferMessage.VehicleId);
                }
                else
                {
                    transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
                }
            }

            if (transportCommand == null)
            {
                return false;
            }
            return true;
        }

        public bool CheckTransportCommandsByVehicle(VehicleMessageEx vehicleMessage)
        {

            TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            if (transportCommand == null)
            {
                ////logger.Warn("can not find TransportCommand, " + vehicleMessage.toString());
                return false;
            }
            return true;
        }

        public bool CheckTransportCommandDestNodeByVehicleCurrentNode(VehicleMessageEx vehicleMessage)
        {

            TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            if (transportCommand != null)
            {
                String destPortId = transportCommand.Dest;
                LocationEx destLocation = this.PathManager.GetLocationByPortId(destPortId);
                String destNodeId = destLocation.StationId;
                String vehicleCurrentNodeId = "";

                if (vehicleMessage.NodeId != null && !string.IsNullOrEmpty(vehicleMessage.NodeId))
                {
                    vehicleCurrentNodeId = vehicleMessage.NodeId;
                }
                else
                {
                    vehicleMessage.DestNodeId = (vehicleMessage.Vehicle.CurrentNodeId);
                }

                if (vehicleMessage.NodeId.Equals(destNodeId))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                ////logger.Warn("can not find TransportCommand, " + vehicleMessage.toString());
                return false;
            }
        }


        public void DeleteChargeTransportCommandsByVehicle(VehicleMessageEx vehicleMessage)
        {

            TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            if (transportCommand != null)
            {
                if (transportCommand.JobType.Equals(TransportCommandEx.JOBTYPE_CHARGEMOVE))
                {
                    vehicleMessage.TransportCommand = (transportCommand);
                    vehicleMessage.TransportCommandId = (transportCommand.Id);
                    vehicleMessage.CarrierId = (transportCommand.CarrierId);
                    vehicleMessage.Cause = (TransportCommandEx.STATE_CHARGE_COMPLETED);

                    this.HistoryManager.CreateTransportCommandHistory(transportCommand, "", vehicleMessage.Cause);
                    this.TransferManager.DeleteTransportCommand(transportCommand);
                }
                else
                {
                    ////logger.Warn("can not find TransportCommand, " + vehicleMessage.toString());
                }
            }
            return;
        }

        public bool SearchPaths(TransferMessageEx transferMessage)
        {

            bool result = false;
            bool find = false;
            String sameBayId = "";
            String transferFlag = "Y";

            LocationEx sourceLocation = this.PathManager.GetLocationByPortId(transferMessage.SourceMachine + ":" + transferMessage.SourceUnit);

            // 요거로 변경해야,,,
            // LocationEx sourceLocation =  this.PathManager.GetLocationViewByStationId(transferMessage.SourceMachine + ":" + transferMessage.SourceUnit);

            LocationEx destLocation = this.PathManager.GetLocationByPortId(transferMessage.DestMachine + ":" + transferMessage.DestUnit);

            // 요거로 변경해야,,,
            // LocationEx destLocation =  this.PathManager.GetLocationViewByStationId(transferMessage.DestMachine + ":" + transferMessage.DestUnit);

            sameBayId = this.PathManager.GetCommonUseBayIdBySourceDest(sourceLocation.StationId, destLocation.StationId, transferFlag);

            if (sameBayId == null)
            {
                transferMessage.ReplyCode = AbstractManager.ID_RESULT_NOTSAMEBAY.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_NOTSAMEBAY.Item2;
                return false;
            }
            //		transferMessage.BayId=(sameBayId);
            //		
            //		PathInfoEx pathInfo = this.PathManager.SearchPaths(sourceLocation, destLocation);
            //		if(pathInfo != null) {
            //			
            //			if(pathInfo.PathsAvailable.size() > 0) {
            //				
            //				transferMessage.setPathInfo(pathInfo);
            //				return true;
            //			}
            //		}
            //		transferMessage.Cause=(AbstractManager.RESULT_ROUTE_NOTFOUND);
            //		//logger.Warn("can not find Path, " + sourceLocation.StationId + " and " + destLocation.StationId, transferMessage);
            //		return result;
            else
            {
                transferMessage.BayId = sameBayId;
                return true;
            }
        }

        public bool SearchPathsAsSourceVehicle(TransferMessageEx transferMessage)
        {

            bool result = false;

            String sameBayId = "";
            String transferFlag = "Y";

            VehicleEx vehicle = transferMessage.Vehicle;
            if (vehicle == null)
            {
                //String vehicleId = transferMessage.VehicleId;

                //JOB ID로 VEHICLE ID 가져오는걸로 임시 반영

                TransportCommandEx transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
                //SJP 1013
                if (transportCommand != null)
                {
                    String vehicleId = transportCommand.VehicleId;
                    if (string.IsNullOrEmpty(vehicleId))
                    {
                        vehicleId = transferMessage.SourceUnit;
                    }
                    vehicle = this.ResourceManager.GetVehicle(vehicleId);

                    //20.02.06 LSJ
                    //transCommand를 갖은 Vehicle 이 없을 경우 ADS Nack
                    if (vehicle == null)
                    {
                        return false;
                    }
                    else
                    {
                        sameBayId = vehicle.BayId;
                        transferMessage.Vehicle = vehicle;
                        transferMessage.VehicleId = vehicleId;
                    }
                }
            }
            if (vehicle != null)
            {

                NodeEx sourceNode = this.PathManager.GetNode(vehicle.CurrentNodeId);
                LocationEx destLocation = this.PathManager.GetLocationByPortId(transferMessage.DestMachine + ":" + transferMessage.DestUnit);

                //181017 MOVECMD NotSameBay
                sameBayId = vehicle.BayId;

                bool IsSameBayId = this.PathManager.IsSameBayId(sameBayId, destLocation.StationId, transferFlag);

                if (IsSameBayId)
                {
                    //SJP
                    transferMessage.BayId = sameBayId;
                    //transferMessage.BayId = (sameBayId); //이게 무슨 의미람.... 이해할 수 없다. LSJ
                    result = true;

                    //				transferMessage.BayId=(sameBayId);
                    //				
                    //				PathInfoEx pathInfo = this.PathManager.SearchPaths(sourceNode, destLocation);
                    //				if(pathInfo != null) {
                    //					
                    //					if(pathInfo.PathsAvailable.size() > 0) {
                    //						
                    //						transferMessage.setPathInfo(pathInfo);
                    //						result = true;
                    //					}
                    //				}
                    //				if(!result) {
                    //					
                    //					transferMessage.Cause=(AbstractManager.RESULT_ROUTE_NOTFOUND);
                    //					//logger.Warn("can not find Path sourceNode{" + sourceNode.Id + "}, destLocation{" + destLocation.StationId + "}"]=transferMessage);
                    //				}
                }
                else
                {

                    transferMessage.ReplyCode = AbstractManager.ID_RESULT_NOTSAMEBAY.Item1;
                    transferMessage.Cause = AbstractManager.ID_RESULT_NOTSAMEBAY.Item2;
                }
            }

            return result;
        }
        public bool DeleteTransportCommandsameVehicleID(VehicleMessageEx vehiclemessage)
        {
            bool result = false;
            TransportCommandEx transportCommand = new TransportCommandEx();
            transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehiclemessage.VehicleId);

            if (transportCommand != null)
            {
                DeleteTransportCommand(vehiclemessage);
                result = true;
            }

            return result;
        }
        public int ChangeTransportCommandStateToAssigned(TransferMessageEx transferMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (transferMessage.TransportCommand != null)
            {
                transportCommand = transferMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.State = (TransportCommandEx.STATE_ASSIGNED);
                transportCommand.AssignedTime = (DateTime.Now);
                transportCommand.VehicleId = (transferMessage.VehicleId);

                setAttributes["State"] = transportCommand.State;
                setAttributes["AssignedTime"] = transportCommand.AssignedTime;
                setAttributes["VehicleId"] = transportCommand.VehicleId;

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }
        //KSB PREASSIGN 추가
        public int ChangeTransportCommandStateToPreAssigned(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (vehicleMessage.TransportCommand != null)
            {
                transportCommand = vehicleMessage.TransportCommand;
            }
            else
            {
                if (string.IsNullOrEmpty(vehicleMessage.TransportCommandId))
                {
                    transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
                }
                else
                {
                    transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
                }
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.State = (TransportCommandEx.STATE_PREASSIGNED);
                transportCommand.AssignedTime = (DateTime.Now);
                transportCommand.VehicleId = (vehicleMessage.VehicleId);

                setAttributes["State"] = transportCommand.State;
                setAttributes["AssignedTime"] = transportCommand.AssignedTime;
                setAttributes["VehicleId"] = transportCommand.VehicleId;

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int ChangeTransportCommandStateToAssigned(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (vehicleMessage.TransportCommand != null)
            {
                transportCommand = vehicleMessage.TransportCommand;
            }
            else
            {
                if (string.IsNullOrEmpty(vehicleMessage.TransportCommandId))
                {
                    transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
                }
                else
                {
                    transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
                }
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.State = (TransportCommandEx.STATE_ASSIGNED);
                transportCommand.AssignedTime = (DateTime.Now);
                transportCommand.VehicleId = (vehicleMessage.VehicleId);

                setAttributes["State"] = transportCommand.State;
                setAttributes["AssignedTime"] = transportCommand.AssignedTime;
                setAttributes["VehicleId"] = transportCommand.VehicleId;

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int ChangeTransportCommandStateToQueued(TransferMessageEx transferMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (transferMessage.TransportCommand != null)
            {
                transportCommand = transferMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.State = (TransportCommandEx.STATE_QUEUED);
                transportCommand.QueuedTime = (DateTime.Now);
                transportCommand.VehicleId = (transferMessage.VehicleId);

                setAttributes["State"] = transportCommand.State;
                setAttributes["QueuedTime"] = transportCommand.QueuedTime;
                //			setAttributes["vehicleId"]=transportCommand.VehicleId);	

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int ChangeTransportCommandStateToQueued(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (vehicleMessage.TransportCommand != null)
            {
                transportCommand = vehicleMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.State = (TransportCommandEx.STATE_QUEUED);
                transportCommand.QueuedTime = (DateTime.Now);
                transportCommand.VehicleId = (vehicleMessage.VehicleId);

                setAttributes["State"] = transportCommand.State;
                setAttributes["QueuedTime"] = transportCommand.QueuedTime;
                //			setAttributes["vehicleId"]=transportCommand.VehicleId);

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int ChangeTransportCommandStateToTransferring(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (vehicleMessage.TransportCommand != null)
            {
                transportCommand = vehicleMessage.TransportCommand;
            }
            else
            {
                if (string.IsNullOrEmpty(vehicleMessage.TransportCommandId))
                {
                    transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
                }
                else
                {
                    transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
                }
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();
                transportCommand.State = (TransportCommandEx.STATE_TRANSFERRING_SOURCE);
                setAttributes["State"] = transportCommand.State;

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int ChangeTransportCommandStateToWating(TransferMessageEx transferMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (transferMessage.TransportCommand != null)
            {
                transportCommand = transferMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.State = (TransportCommandEx.STATE_WAITING);
                //			transportCommand.AssignedTime=(new Date());
                setAttributes["State"] = transportCommand.State;

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int ChangeTransportCommandStateToArrivedSource(TransferMessageEx transferMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (transferMessage.TransportCommand != null)
            {
                transportCommand = transferMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.State = (TransportCommandEx.STATE_ARRIVED_SOURCE);
                transportCommand.LoadArrivedTime = DateTime.Now;
                setAttributes["State"] = transportCommand.State;
                setAttributes["LoadArrivedTime"] = transportCommand.LoadArrivedTime;

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int ChangeTransportCommandStateToArrivedDest(TransferMessageEx transferMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (transferMessage.TransportCommand != null)
            {
                transportCommand = transferMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.State = (TransportCommandEx.STATE_ARRIVED_DEST);
                transportCommand.UnloadArrivedTime = DateTime.Now;
                setAttributes["State"] = transportCommand.State;
                setAttributes["UnloadArrivedTime"] = transportCommand.UnloadArrivedTime;

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int ChangeTransportCommandStateToTransferingSource(TransferMessageEx transferMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (transferMessage.TransportCommand != null)
            {
                transportCommand = transferMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.State = (TransportCommandEx.STATE_TRANSFERRING_SOURCE);
                //			transportCommand.LoadedTime=(new Date());
                setAttributes["State"] = transportCommand.State;

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int ChangeTransportCommandStateToTransferingDest(TransferMessageEx transferMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (transferMessage.TransportCommand != null)
            {
                transportCommand = transferMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.State = (TransportCommandEx.STATE_TRANSFERRING_DEST);
                transportCommand.LoadedTime = DateTime.Now;
                setAttributes["State"] = transportCommand.State;
                setAttributes["LoadedTime"] = transportCommand.LoadedTime;

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int ChangeTransportCommandStateToTransferingDest(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (vehicleMessage.TransportCommand != null)
            {
                transportCommand = vehicleMessage.TransportCommand;
            }
            else
            {
                if (string.IsNullOrEmpty(vehicleMessage.TransportCommandId))
                {
                    transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
                }
                else
                {
                    transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
                }
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.State = (TransportCommandEx.STATE_TRANSFERRING_DEST);
                transportCommand.LoadedTime = DateTime.Now;
                setAttributes["State"] = transportCommand.State;
                setAttributes["LoadedTime"] = transportCommand.LoadedTime;

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int ChangeTransportCommandStateToComplete(TransferMessageEx transferMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (transferMessage.TransportCommand != null)
            {
                transportCommand = transferMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.State = (TransportCommandEx.STATE_COMPLETED);
                transportCommand.UnloadedTime = DateTime.Now;
                transportCommand.CompletedTime = DateTime.Now;
                setAttributes["State"] = transportCommand.State;
                setAttributes["UnloadedTime"] = transportCommand.UnloadedTime;
                setAttributes["CompletedTime"] = transportCommand.CompletedTime;

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int ChangeTransportCommandStateToComplete(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (vehicleMessage.TransportCommand != null)
            {
                transportCommand = vehicleMessage.TransportCommand;
            }
            else
            {
                if (string.IsNullOrEmpty(vehicleMessage.TransportCommandId))
                {
                    transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
                }
                else
                {
                    transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
                }
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.State = (TransportCommandEx.STATE_COMPLETED);
                transportCommand.UnloadedTime = DateTime.Now;
                transportCommand.CompletedTime = DateTime.Now;
                setAttributes["State"] = transportCommand.State;
                setAttributes["UnloadedTime"] = transportCommand.UnloadedTime;
                setAttributes["CompletedTime"] = transportCommand.CompletedTime;

                //TransportCommandEx�쓽 �긽�깭 'complete' �떆 reason �뿉�룄 'complete'�쑝濡� 湲곕줉�븯�룄濡� 異붽�
                vehicleMessage.Cause = (TransportCommandEx.CAUSE_COMPLETE);
                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int ChangeTransferCommandVehicleId(TransferMessageEx transferMessage)
        {

            return this.TransferManager.UpdateTransportCommandVehicleId(transferMessage.TransportCommand, transferMessage.VehicleId);
        }

        public int ChangeTransferCommandVehicleId(VehicleMessageEx vehicleMessage)
        {

            return this.TransferManager.UpdateTransportCommandVehicleId(vehicleMessage.TransportCommand, vehicleMessage.VehicleId);
        }

        public int ChangeTransferCommandVehicleIdEmpty(TransferMessageEx transferMessage)
        {

            return this.TransferManager.UpdateTransportCommandVehicleId(transferMessage.TransportCommand, "");
        }

        public int ChangeTransferCommandVehicleIdEmpty(VehicleMessageEx vehicleMessage)
        {

            return this.TransferManager.UpdateTransportCommandVehicleId(vehicleMessage.TransportCommand, "");
        }

        //200518 LYS Path Create for O-code 
        public void UpdateJobExpectedTimeWhenCreate(TransferMessageEx transferMessage)
        {
            TransportCommandEx transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);

            if (transportCommand != null)
            {
                LocationEx sourceLocation = this.ResourceManager.GetLocationByPortId(transportCommand.Source);
                LocationEx destLocation = this.ResourceManager.GetLocationByPortId(transportCommand.Dest);

                PathEx sourceDestPath = this.PathManager.SearchDynamicPathsDijkstra(sourceLocation.StationId, destLocation.StationId);

                if (sourceDestPath != null)
                {
                    //String path = this.extensionManager.listToString(entry.getValue());
                    int totalTime = sourceDestPath.Cost * 60 / 2500;

                    Hashtable pathInfoMap = MapUtility.StringToMap(transferMessage.TransportCommand.Path);

                    String previousValue = "expectedSourceDest=" + totalTime;
                    pathInfoMap.Add("expectedSourceDest=", totalTime);
                    //logger.fine("expectedSourceDest{" + StringUtils.defaultString(previousValue) + "->" + oldTransport.getVehicleId() + "}, " + transferMessage, transferMessage);
                    //transferMessage.TransportCommand.Path = MapUtility.MapToString(pathInfoMap);
                    transferMessage.TransportCommand.Path = previousValue;

                    this.PathManager.UpdateTransportCommandPathMap(transferMessage.TransportCommand);
                }
            }
        }

        public void UpdateJobExpectedTimeWhenQueue(VehicleMessageEx vehicleMessage)
        {
            TransportCommandEx transportCmd = vehicleMessage.TransportCommand;
            if (transportCmd != null)
            {
                VehicleEx vehicle = vehicleMessage.Vehicle;
                if (vehicle.Path != null)
                {
                    if (vehicle.Path.Contains(":"))
                    {
                        String path = vehicle.Path.Split(':')[0];
                        int totalTime = Int32.Parse(vehicle.Path.Split(':')[1]);

                        Hashtable pathInfoMap = MapUtility.StringToMap(transportCmd.Path);
                        String previousValue = "expectedAGVSource=" + totalTime;
                        pathInfoMap.Add("expectedAGVSource=", totalTime);

                        //transportCmd.Path = MapUtility.MapToString(pathInfoMap);
                        transportCmd.Path = previousValue;

                        this.PathManager.UpdateTransportCommandPathMap(transportCmd);
                        //this.resourceManager.updateVehicle(vehicle, "path", vehicle.getPath());
                        this.UpdateVehiclePath(vehicle, vehicle.Path);
                    }
                }
            }
        }

        public int UpdateTransportCommandStateToTransferringSource(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (vehicleMessage.TransportCommand != null)
            {
                transportCommand = vehicleMessage.TransportCommand;
            }
            else
            {
                if (string.IsNullOrEmpty(vehicleMessage.TransportCommandId))
                {
                    transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
                }
                else
                {
                    transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
                }
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.State = (TransportCommandEx.STATE_TRANSFERRING_SOURCE);
                //transportCommand.AssignedTime=(new Date());
                setAttributes["State"] = transportCommand.State;
                //setAttributes["assignedTime"]=transportCommand.AssignedTime);

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int UpdateTransportCommandStateToTransferringDest(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (vehicleMessage.TransportCommand != null)
            {
                transportCommand = vehicleMessage.TransportCommand;
            }
            else
            {
                if (string.IsNullOrEmpty(vehicleMessage.TransportCommandId))
                {
                    transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
                }
                else
                {
                    transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
                }
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.State = (TransportCommandEx.STATE_TRANSFERRING_DEST);
                //transportCommand.AssignedTime=(new Date());
                setAttributes["State"] = transportCommand.State;
                //setAttributes["assignedTime"]=transportCommand.AssignedTime);

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int UpdateTransportCommandStateToArrivedSource(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (vehicleMessage.TransportCommand != null)
            {
                transportCommand = vehicleMessage.TransportCommand;
            }
            else
            {
                if (string.IsNullOrEmpty(vehicleMessage.TransportCommandId))
                {
                    transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
                }
                else
                {
                    transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
                }
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.State = (TransportCommandEx.STATE_ARRIVED_SOURCE);
                //transportCommand.AssignedTime=(new Date());
                setAttributes["State"] = transportCommand.State;
                //setAttributes["assignedTime"]=transportCommand.AssignedTime);

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int UpdateTransportCommandStateToArrivedDest(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (vehicleMessage.TransportCommand != null)
            {
                transportCommand = vehicleMessage.TransportCommand;
            }
            else
            {
                if (string.IsNullOrEmpty(vehicleMessage.TransportCommandId))
                {
                    transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
                }
                else
                {
                    transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
                }
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.State = (TransportCommandEx.STATE_ARRIVED_DEST);
                //transportCommand.AssignedTime=(new Date());
                setAttributes["State"] = transportCommand.State;
                //setAttributes["assignedTime"]=transportCommand.AssignedTime);

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }


        public VehicleMessageEx CalculatePathAndTimeFirstTime(VehicleMessageEx vehicleMessage)
        {
            VehicleEx vhc = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);

            NodeEx currentNode = this.CacheManager.GetNode(vhc.CurrentNodeId);
            string remainPath = string.Empty;

            if (vehicleMessage.CurrentPath != null && vehicleMessage.CurrentPath.Count > 2)
            {
                currentNode = this.CacheManager.GetNode((string)vehicleMessage.CurrentPath[2]);
                if (currentNode.Type.Equals(NodeEx.TYPE_WAIT_P, StringComparison.OrdinalIgnoreCase) ||
                    currentNode.Type.Equals(WaitPointViewEx.TYPE_SWAIT_P, StringComparison.OrdinalIgnoreCase) ||
                    currentNode.Type.Equals(WaitPointViewEx.TYPE_AWAIT_P, StringComparison.OrdinalIgnoreCase))
                {
                    currentNode = this.CacheManager.GetNode(vhc.CurrentNodeId);
                }
                else
                {
                    remainPath = vehicleMessage.CurrentPath[0] + "," + vehicleMessage.CurrentPath[1] + ",";
                }
            }

            NodeEx destNode = this.CacheManager.GetNode(vehicleMessage.NodeId);

            if (vhc != null && currentNode != null && destNode != null)
            {
                string startNode = currentNode.Id;

                if (vehicleMessage.CurrentPath != null && vehicleMessage.CurrentPath.Count > 2 && vehicleMessage.CurrentPath[2].Equals(destNode.Id))
                {
                    startNode = (string)vehicleMessage.CurrentPath[0];
                    remainPath = string.Empty;
                }
                else if (vehicleMessage.CurrentPath != null && vehicleMessage.CurrentPath[0].Equals(destNode.Id))
                {
                    IList links = this.CacheManager.GetLinkByFromNodeId((string)(vehicleMessage.CurrentPath[0]));
                    if (links != null && links.Count > 0)
                    {
                        startNode = ((LinkEx)links[0]).ToNodeId;
                        remainPath = vehicleMessage.CurrentPath[0] + ",";
                    }
                }

                Dictionary<String, IList> linkMap = null;

                BayEx bay = this.ResourceManager.GetBay(vhc.BayId);

                //if (bay != null && !string.IsNullOrEmpty(bay.ZoneMove) && bay.ZoneMove.Equals("Y", StringComparison.OrdinalIgnoreCase) && !vhc.Vendor.Equals(BayExs.AGVTYPE_RGV, StringComparison.OrdinalIgnoreCase))
                if (bay != null && !vhc.Vendor.Equals(BayExs.AGVTYPE_RGV, StringComparison.OrdinalIgnoreCase))
                {
                    IList linkViews = this.CacheManager.GetLinkViewByBayId(vhc.BayId);

                    if (linkViews != null && linkViews.Count > 0)
                    {
                        linkMap = this.PathManager.ConvertLinkViewsToMapByFromNode(linkViews);
                    }
                }


                PathEx originalPath = this.PathManager.SearchDynamicPathsDijkstra(startNode, destNode.Id, linkMap, true, false);

                PathEx foundPath;

                //if (bay != null && !string.IsNullOrEmpty(bay.Traffic) && bay.Traffic.Equals("Y", StringComparison.OrdinalIgnoreCase))
                if (bay != null)
                {
                    foundPath = this.PathManager.SearchDynamicPathsDijkstraDivide(startNode, destNode.Id, linkMap);
                }
                else
                {
                    foundPath = this.PathManager.SearchDynamicPathsDijkstra(startNode, destNode.Id, linkMap, true, true);
                }


                if (foundPath == null && originalPath == null)
                {
                    if (bay.ZoneMove != null && bay.ZoneMove.Equals("Y", StringComparison.OrdinalIgnoreCase) &&
                        bay.StopOut != null && bay.StopOut.Equals("Y", StringComparison.OrdinalIgnoreCase) &&
                        vhc.Vendor != null && !vhc.Vendor.Equals(BayExs.AGVTYPE_RGV, StringComparison.OrdinalIgnoreCase))
                    {

                        startNode = vhc.CurrentNodeId;
                        foundPath = this.PathManager.SearchDynamicPathsDijkstra(startNode, destNode.Id, linkMap, true, true);
                        if (foundPath == null)
                        {

                            this.MessageManager.SendVehicleStopCommand(vehicleMessage);

                        }
                    }
                }


                if (foundPath != null & originalPath != null)
                {
                    if (foundPath.Cost > originalPath.Cost * 1.3)
                    {
                        foundPath = originalPath;
                    }


                    String path = remainPath + this.PathManager.ListToString(foundPath.NodeIds);
                    int totalTime = foundPath.Cost * 60 / 2500;

                    vhc.Path = path + "," + totalTime;

                    //20230425 Modify ORDER_THREE
                    //에러나서 주석처리함
                    //this.HistoryManager.AddVehiclePathHistory(vhc, path, VehicleSearchPathHistory.TYPE_ORIGINAL, foundPath.Cost);
                    //

                    this.UpdateVehiclePath(vhc, path + ":" + totalTime);
                    this.ResourceManager.UpdateVehicle(vhc, "VehicleDestNodeId", destNode.Id);

                    vehicleMessage.Vehicle = vhc;


                    OrderPairNodeEx orderPair = this.ResourceManager.SearchNextOrderNode(path + ":" + totalTime, vhc.BayId);

                    if (orderPair != null)
                    {
                        VehicleOrderEx vehicleOrder = this.ResourceManager.GetVehicleOrderByVehicleId(vehicleMessage.VehicleId);

                        if (vehicleOrder != null)
                        {
                            //20230425 Modify ORDER_THREE
                            //항상 C->O 하게 변경함
                            //if (!vehicleOrder.OrderNode.Equals(orderPair.OrderGroup))
                            //
                            {
                                VehicleMessageEx newVehicleMessage = new VehicleMessageEx();
                                newVehicleMessage.Vehicle = vhc;
                                newVehicleMessage.VehicleId = vhc.Id;
                                newVehicleMessage.NodeId = orderPair.OrderGroup;
                                newVehicleMessage.KeyData = orderPair.Status;

                                vehicleOrder.OrderNode = orderPair.OrderGroup;
                                vehicleOrder.OrderTime = DateTime.Now;
                                vehicleOrder.Reply = "N";

                                //20230425 Modify ORDER_THREE
                                //this.ResourceManager.UpdateVehicleOrderACS(vehicleOrder);
                                this.ResourceManager.UpdateVehicleOrder(vehicleOrder);
                                //

                                //this.HistoryManager.CreateVehicleOrderACSHistory(vehicleOrder);

                                return newVehicleMessage;

                            }
                        }
                        else
                        {
                            VehicleMessageEx newVehicleMessage = new VehicleMessageEx();
                            newVehicleMessage.Vehicle = vhc;
                            newVehicleMessage.VehicleId = vhc.Id;
                            newVehicleMessage.NodeId = orderPair.OrderGroup;
                            newVehicleMessage.KeyData = orderPair.Status;


                            vehicleOrder = new VehicleOrderEx();

                            vehicleOrder.OrderNode = orderPair.OrderGroup;
                            vehicleOrder.VehicleId = vehicleMessage.VehicleId;
                            vehicleOrder.Reply = "N";

                            //20230425 Modify ORDER_THREE
                            vehicleOrder.OrderTime = DateTime.Now;
                            //


                            this.ResourceManager.CreateVehicleOrderACS(vehicleOrder);


                            return newVehicleMessage;

                        }

                    }
                    else
                    {
                        VehicleOrderEx vehicleOrder = this.ResourceManager.GetVehicleOrderByVehicleId(vhc.Id);

                        if (vehicleOrder != null)
                        {
                            this.ResourceManager.DeleteVehicleOrderACSByVehicleID(vhc.Id);
                            VehicleMessageEx newVehicleMessage = vehicleMessage;
                            newVehicleMessage.Vehicle = vhc;
                            newVehicleMessage.VehicleId = vhc.Id;
                            newVehicleMessage.NodeId = "0000,0000,0000";
                            newVehicleMessage.KeyData = "ORDER_LF";

                            //this.ResourceManager.CreateVehicleOrderACSHistory(vehicleOrder);

                            return newVehicleMessage;
                        }
                        return null;
                    }
                }
            }


            return null;
        }




        public int UpdateTransportCommandVehicleEvent(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;
            String vehicleEvent = vehicleMessage.MessageName;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (vehicleMessage.TransportCommand != null)
            {
                transportCommand = vehicleMessage.TransportCommand;
            }
            else
            {
                if (string.IsNullOrEmpty(vehicleMessage.TransportCommandId))
                {
                    transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
                }
                else
                {
                    transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
                }
            }

            if (transportCommand != null)
            {
                updateCount = this.UpdateTransportCommandVehicleEvent(transportCommand, vehicleEvent);
                return updateCount;
            }

            return updateCount;
        }

        public int UpdateTransportCommandVehicleEvent(TransportCommandEx transportCommand, String vehicleEvent)
        {

            int updateCount = -1;
            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.VehicleEvent = (vehicleEvent);
                //transportCommand.AssignedTime=(new Date());
                setAttributes["VehicleEvent"] = vehicleEvent;
                //setAttributes["assignedTime"]=transportCommand.AssignedTime);

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
                return updateCount;
            }
            ////logger.Warn("transportCommand does not exist.");
            return updateCount;
        }

        public int UpdateTransportCommandCreateTime(TransferMessageEx transferMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (transferMessage.TransportCommand != null)
            {
                transportCommand = transferMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.CreateTime = (DateTime.Now);
                transportCommand.VehicleId = (transferMessage.VehicleId);

                setAttributes["CreateTime"] = transportCommand.CreateTime;
                setAttributes["VehicleId"] = transportCommand.VehicleId;

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int UpdateTransportCommandQueuedTime(TransferMessageEx transferMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (transferMessage.TransportCommand != null)
            {
                transportCommand = transferMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.QueuedTime = (DateTime.Now);
                transportCommand.VehicleId = (transferMessage.VehicleId);

                setAttributes["QueuedTime"] = transportCommand.QueuedTime;
                setAttributes["VehicleId"] = transportCommand.VehicleId;

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int UpdateTransportCommandAssignedTime(TransferMessageEx transferMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (transferMessage.TransportCommand != null)
            {
                transportCommand = transferMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.AssignedTime = (DateTime.Now);
                transportCommand.VehicleId = (transferMessage.VehicleId);

                setAttributes["AssignedTime"] = transportCommand.AssignedTime;
                setAttributes["VehicleId"] = transportCommand.VehicleId;

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int UpdateTransportCommandStartedTime(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (vehicleMessage.TransportCommand != null)
            {
                transportCommand = vehicleMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.StartedTime = (DateTime.Now);

                setAttributes["StartedTime"] = transportCommand.StartedTime;

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int UpdateTransportCommandLoadArrivedTime(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (vehicleMessage.TransportCommand != null)
            {
                transportCommand = vehicleMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.LoadArrivedTime = (DateTime.Now);

                setAttributes["LoadArrivedTime"] = transportCommand.LoadArrivedTime;

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int UpdateTransportCommandLoadedTime(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (vehicleMessage.TransportCommand != null)
            {
                transportCommand = vehicleMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.LoadedTime = (DateTime.Now);
                // 2017.09.28 keyhan added. loadArrivedTime�씠 null�씤寃쎌슦 loadedTime�쑝濡� 媛숈씠 update�븯�룄濡� logic 異붽�
                if (transportCommand.LoadArrivedTime == null)
                {
                    transportCommand.LoadArrivedTime = (transportCommand.LoadedTime);
                }
                setAttributes["LoadedTime"] = transportCommand.LoadedTime;
                setAttributes["LoadArrivedTime"] = transportCommand.LoadArrivedTime;
                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int UpdateTransportCommandUnloadArrivedTime(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (vehicleMessage.TransportCommand != null)
            {
                transportCommand = vehicleMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }

            if (transportCommand != null)
            {

                if (transportCommand.LoadArrivedTime == null)
                {
                    return updateCount;
                }

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.UnloadArrivedTime = (DateTime.Now);

                setAttributes["UnloadArrivedTime"] = transportCommand.UnloadArrivedTime;

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int UpdateTransportCommandUnloadedTime(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (vehicleMessage.TransportCommand != null)
            {
                transportCommand = vehicleMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.UnloadedTime = (DateTime.Now);

                setAttributes["UnloadedTime"] = transportCommand.UnloadedTime;

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int UpdateTransportCommandCompletedTime(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (vehicleMessage.TransportCommand != null)
            {
                transportCommand = vehicleMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.CompletedTime = (DateTime.Now);

                setAttributes["CompletedTime"] = transportCommand.CompletedTime;
                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int UpdateTransportCommandUnloadingTime(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (vehicleMessage.TransportCommand != null)
            {
                transportCommand = vehicleMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.UnloadingTime = (DateTime.Now);

                setAttributes["UnloadingTime"] = transportCommand.UnloadingTime;

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int UpdateTransportCommandLoadingTime(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (vehicleMessage.TransportCommand != null)
            {
                transportCommand = vehicleMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.LoadingTime = (DateTime.Now);
                transportCommand.VehicleId = (vehicleMessage.VehicleId);

                setAttributes["LoadingTime"] = transportCommand.LoadingTime;
                setAttributes["VehicleId"] = transportCommand.VehicleId;

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public int UpdateTransportCommandPath(TransferMessageEx transferMessage)
        {

            int updateCount = -1;
            bool boolExe = false;

            //wook 20170731 searchPath logic �궘�젣�뿉 �뵲瑜� 蹂�寃�
            if (boolExe)
            {
                TransportCommandEx transportCommand = new TransportCommandEx();
                if (transferMessage.TransportCommand != null)
                {
                    transportCommand = transferMessage.TransportCommand;
                }
                else
                {
                    transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
                }

                if (transportCommand != null)
                {

                    LocationEx currentLocation = this.ResourceManager.GetLocation(transportCommand.Source);
                    LocationEx destLocation = this.ResourceManager.GetLocation(transportCommand.Dest);
                    PathInfoEx pathInfo = this.PathManager.SearchPaths(currentLocation, destLocation);

                    PathEx paths = (PathEx)pathInfo.PathsAvailable[0];
                    String path = currentLocation.StationId;

                    if (path != null && !string.IsNullOrEmpty(path))
                    {
                        path += ",";
                    }

                    IList listPaths = paths.NodeIds;

                    for (IEnumerator iterator = listPaths.GetEnumerator(); iterator.MoveNext();)
                    {
                        String appPath = (String)iterator.Current;
                        path += appPath;
                        if (iterator.MoveNext())
                        {
                            path += ",";
                        }
                    }
                    this.TransferManager.UpdateTransportCommandPath(transportCommand, path);
                }
            }
            else
            {

            }

            return updateCount;
        }

        public int UpdateTransportCommandPathEmpty(TransferMessageEx transferMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (transferMessage.TransportCommand != null)
            {
                transportCommand = transferMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {

                String path = "";

                this.TransferManager.UpdateTransportCommandPath(transportCommand, path);

            }

            return updateCount;
        }

        public int UpdateTransportCommandPathEmpty(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = new TransportCommandEx();
            if (vehicleMessage.TransportCommand != null)
            {
                transportCommand = vehicleMessage.TransportCommand;
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {

                String path = "";

                this.TransferManager.UpdateTransportCommandPath(transportCommand, path);

            }

            return updateCount;
        }

        public int UpdateTransportCommandPath(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            bool boolExe = false;

            //wook 20170731 searchPath logic �궘�젣�뿉 �뵲瑜� 蹂�寃�
            if (boolExe)
            {
                TransportCommandEx transportCommand = new TransportCommandEx();
                if (vehicleMessage.TransportCommand != null)
                {
                    transportCommand = vehicleMessage.TransportCommand;
                }
                else
                {
                    transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
                }

                if (transportCommand != null)
                {

                    LocationEx currentLocation = this.ResourceManager.GetLocation(transportCommand.Source);
                    LocationEx destLocation = this.ResourceManager.GetLocation(transportCommand.Dest);
                    PathInfoEx pathInfo = this.PathManager.SearchPaths(currentLocation, destLocation);

                    PathEx paths = (PathEx)pathInfo.PathsAvailable[0];
                    String path = currentLocation.StationId;

                    if (path != null && !string.IsNullOrEmpty(path))
                    {
                        path += ",";
                    }

                    IList listPaths = paths.NodeIds;

                    for (IEnumerator iterator = listPaths.GetEnumerator(); iterator.MoveNext();)
                    {
                        String appPath = (String)iterator.Current;
                        path += appPath;
                        if (iterator.MoveNext())
                        {
                            path += ",";
                        }
                    }
                    this.TransferManager.UpdateTransportCommandPath(transportCommand, path);
                }
            }
            else
            {

            }

            return updateCount;
        }

        public void UpdateVehicleTransportCommand(TransferMessageEx transferMessage)
        {

            VehicleEx vehicle;
            if (transferMessage.Vehicle != null)
            {
                vehicle = transferMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(transferMessage.VehicleId);
                transferMessage.Vehicle = (vehicle);
            }

            this.ResourceManager.UpdateVehicleTransportCommandId(vehicle, transferMessage.TransportCommandId,
                    transferMessage.MessageName);
        }

        public void UpdateVehicleTransportCommandEmpty(TransferMessageEx transferMessage)
        {

            VehicleEx vehicle;
            if (transferMessage.Vehicle != null)
            {
                vehicle = transferMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(transferMessage.VehicleId);
                transferMessage.Vehicle = (vehicle);
            }

            this.ResourceManager.UpdateVehicleTransportCommandId(vehicle, "", transferMessage.MessageName);
        }

        public void UpdateVehicleTransportCommandEmpty(VehicleMessageEx vehicleMessage)
        {
            VehicleEx vehicle;
            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                vehicleMessage.Vehicle = (vehicle);
            }

            this.ResourceManager.UpdateVehicleTransportCommandId(vehicle, "", vehicleMessage.MessageName);
        }

        public void UpdateVehicleTransportCommand(VehicleMessageEx vehicleMessage)
        {
            VehicleEx vehicle;
            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                vehicleMessage.Vehicle = (vehicle);
            }

            this.ResourceManager.UpdateVehicleTransportCommandId(vehicle, vehicleMessage.TransportCommandId,
                    vehicleMessage.MessageName);
        }

        public void UpdateVehiclePath(TransferMessageEx transferMessage)
        {

            //TransportCommandEx transportCommand = new TransportCommandEx();
            //if (transferMessage.TransportCommand != null)
            //{
            //    transportCommand = transferMessage.TransportCommand;
            //}
            //else
            //{
            //    transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
            //}

            //String path = "";

            //if (transportCommand != null)
            //{
            //    path = transportCommand.Path;
            //}

            //		PathInfoEx pathInfo;
            //		LocationEx currentLocation;
            //		
            //		if (transferMessage.PathInfo != null) {
            //			pathInfo = transferMessage.PathInfo;
            //			currentLocation = this.PathManager.GetLocationByPortId(transferMessage.SourceMachine+":"+transferMessage.SourceUnit);
            //		} else {
            //			currentLocation = this.PathManager.GetLocationByPortId(transferMessage.SourceMachine+":"+transferMessage.SourceUnit);
            //			LocationEx destLocation = this.PathManager.GetLocationByPortId(transferMessage.DestMachine+":"+transferMessage.DestUnit);
            //			pathInfo = this.PathManager.SearchPaths(currentLocation, destLocation);
            //
            //		}
            //		
            //		PathEx paths = (PathEx) pathInfo.PathsAvailable.get(0);
            //		String path = currentLocation.StationId;
            //		
            //		if (path != null && !path.isEmpty()) 
            //		{
            //			path += ",";
            //		}
            //		
            //		List listPaths = paths.NodeIds;
            //		
            //		for (Iterator iterator = listPaths.iterator(); iterator.MoveNext();) {
            //			String appPath = (String) iterator.Current;
            //			path += appPath;
            //			if (iterator.hasNext()) 
            //			{
            //				path += ",";
            //			}
            //		}

            //VehicleEx vehicle;
            //if (transferMessage.Vehicle != null)
            //{
            //    vehicle = transferMessage.Vehicle;
            //}
            //else
            //{
            //    vehicle = this.ResourceManager.GetVehicle(transferMessage.VehicleId);
            //    transferMessage.Vehicle = (vehicle);
            //}

            ////2020.07.17 KSG
            ////Commnets : CacheVersion Only
            //this.ResourceManager.UpdateVehicle(vehicle, "Path", path);
        }

        public void UpdateVehiclePath(VehicleEx vehicle, String path)
        {
            this.ResourceManager.UpdateVehicle(vehicle, "path", path);
        }

        public void UpdateVehicleAcsDestNodeId(TransferMessageEx transferMessage)
        {

            VehicleEx vehicle;
            if (transferMessage.Vehicle != null)
            {
                vehicle = transferMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(transferMessage.VehicleId);
            }
            String source = transferMessage.SourceMachine + ":" + transferMessage.SourceUnit;
            String destNodeId = this.ResourceManager.GetLocation(source).StationId;

            this.ResourceManager.UpdateVehicleAcsDestNodeId(vehicle, destNodeId, transferMessage.MessageName);
        }

        public void UpdateVehicleAcsDestNodeId(VehicleMessageEx vehicleMessage)
        {

            VehicleEx vehicle;
            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
            }
            TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);

            if (transportCommand != null)
            {
                String dest = transportCommand.Source;
                if (dest == null)
                {
                    dest = transportCommand.Dest;
                }
                String destNodeId = this.ResourceManager.GetLocation(dest).StationId;
                this.ResourceManager.UpdateVehicleAcsDestNodeId(vehicle, destNodeId, vehicleMessage.MessageName);
            }
        }

        public void UpdateVehicleAcsDestNodeIdByVehicleCurrentNodeId(VehicleMessageEx vehicleMessage)
        {

            VehicleEx vehicle;
            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
            }

            String dest = "";
            if (vehicleMessage.DestNodeId != null && !string.IsNullOrEmpty(vehicleMessage.DestNodeId))
            {
                dest = vehicleMessage.DestNodeId;
            }
            else
            {
                dest = vehicleMessage.DestPortId;
            }

            this.ResourceManager.UpdateVehicleAcsDestNodeId(vehicle, dest, vehicleMessage.MessageName);
        }

        public void UpdateVehicleAcsDestNodeIdToDest(VehicleMessageEx vehicleMessage)
        {

            VehicleEx vehicle;
            //suji
            vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
            //if (vehicleMessage.Vehicle != null)
            //{
            //    vehicle = vehicleMessage.Vehicle;
            //}
            //else
            //{
            //    vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
            //}

            if (vehicle.BayId.Contains("S0")) return;

            TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);

            if (transportCommand != null)
            {
                String dest = transportCommand.Dest;
                String destNodeId = this.ResourceManager.GetLocation(dest).StationId;

                this.ResourceManager.UpdateVehicleAcsDestNodeId(vehicle, destNodeId, vehicleMessage.MessageName);
            }
        }

        public void UpdateVehicleAcsDestNodeId(UiMoveVehicleMessageEx uiMessage)
        {

            VehicleEx vehicle = this.ResourceManager.GetVehicle(uiMessage.VehicleId);
            if (vehicle != null)
            {
                String destNodeId = uiMessage.NodeId;
                this.ResourceManager.UpdateVehicleAcsDestNodeId(vehicle, destNodeId, uiMessage.MessageName);
            }

        }

        public void UpdateVehicleAcsDestNodeIdToWaitPoint(VehicleMessageEx vehicleMessage)
        {

            VehicleEx vehicle;

            vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);

            if (vehicleMessage.Vehicle != null && vehicleMessage.Vehicle != vehicle)
            {
                vehicleMessage.Vehicle = vehicle;
            }

            String destNodeId = vehicleMessage.DestPortId;

            if (destNodeId != null)
            {
                this.ResourceManager.UpdateVehicleAcsDestNodeId(vehicle, destNodeId, vehicleMessage.MessageName);
            }
        }

        public void UpdateVehicleAcsDestNodeIdToWaitPoint(TransferMessageEx transferMessage)
        {

            VehicleEx vehicle;
            if (transferMessage.Vehicle != null)
            {
                vehicle = transferMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(transferMessage.VehicleId);
            }


            String destNodeId = transferMessage.DestNodeId;

            if (destNodeId != null)
            {
                this.ResourceManager.UpdateVehicleAcsDestNodeId(vehicle, destNodeId, transferMessage.MessageName);
            }
        }

        public void UpdateVehicleAcsDestNodeIdTo0000(VehicleMessageEx vehicleMessage)
        {

            VehicleEx vehicle;
            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                // 20170801 keyhan added. bugfixed
                if (vehicle == null)
                {

                    //logger.Error("vehicle{" + vehicleMessage.VehicleId + "} does not exist in repository");
                    return;
                }
            }
            this.ResourceManager.UpdateVehicleAcsDestNodeId(vehicle, "9816", vehicleMessage.MessageName);
        }

        //KSB RGV에 작업자가 강제로 Tray 올려 놓았을때 Panel AB Buffer 강제 이동
        public void UpdateVehicleAcsDestNodeIdTo0000_RGV(VehicleMessageEx vehicleMessage)
        {

            VehicleEx vehicle;
            String dest = "9000";

            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                // 20170801 keyhan added. bugfixed
                if (vehicle == null)
                {

                    //logger.Error("vehicle{" + vehicleMessage.VehicleId + "} does not exist in repository");
                    return;
                }
            }

            // 작업자가 강제적으로 RGV에 Tray를 올려 놓았을때 AB Buffer로 보내기 위함
            // AB Buffer TAGID를 Vehicle의 Path 컬럼에 값을 넣고 사용함
            if (vehicleMessage.Vehicle.Path != null)
            {
                dest = vehicleMessage.Vehicle.Path.ToString().Trim();
            }

            this.ResourceManager.UpdateVehicleAcsDestNodeId(vehicle, dest, vehicleMessage.MessageName);
        }


        public void UpdateVehicleAcsDestNodeIdTo0000(TransferMessageEx transferMessage)
        {

            VehicleEx vehicle;
            if (transferMessage.Vehicle != null)
            {
                vehicle = transferMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(transferMessage.VehicleId);
                // 20170801 keyhan added. bugfixed
                if (vehicle == null)
                {

                    //logger.Error("vehicle{" + transferMessage.VehicleId + "} does not exist in repository");
                    return;
                }
            }
            this.ResourceManager.UpdateVehicleAcsDestNodeId(vehicle, "9816", transferMessage.MessageName);
        }

        public void UpdateVehicleAcsDestNodeIdEmpty(TransferMessageEx transferMessage)
        {

            VehicleEx vehicle;
            if (transferMessage.Vehicle != null)
            {
                vehicle = transferMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(transferMessage.VehicleId);
                // 20170801 keyhan added. bugfixed
                if (vehicle == null)
                {

                    //logger.Error("vehicle{" + transferMessage.VehicleId + "} does not exist in repository");
                    return;
                }
            }
            this.ResourceManager.UpdateVehicleAcsDestNodeId(vehicle, "", transferMessage.MessageName);
        }

        public void UpdateVehicleAcsDestNodeIdEmpty(VehicleMessageEx vehicleMessage)
        {

            VehicleEx vehicle;
            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                // 20170801 keyhan added. bugfixed
                if (vehicle == null)
                {
                    //logger.Error("vehicle{" + vehicleMessage.VehicleId + "} does not exist in repository");
                    return;
                }
            }
            this.ResourceManager.UpdateVehicleAcsDestNodeId(vehicle, "", vehicleMessage.MessageName);
        }


        public void UpdateVehiclePathEmpty(TransferMessageEx transferMessage)
        {
            VehicleEx vehicle;
            if (transferMessage.Vehicle != null)
            {
                vehicle = transferMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(transferMessage.VehicleId);
                // 20170801 keyhan added. bugfixed
                if (vehicle == null)
                {

                    //logger.Error("vehicle{" + transferMessage.VehicleId + "} does not exist in repository");
                    return;
                }
                transferMessage.Vehicle = (vehicle);
            }

            this.ResourceManager.UpdateVehicle(vehicle, "Path", "");
        }


        public void UpdateVehiclePathEmpty(VehicleMessageEx vehicleMessage)
        {

            VehicleEx vehicle;
            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                // 20170801 keyhan added. bugfixed
                if (vehicle == null)
                {

                    //logger.Error("vehicle{" + vehicleMessage.VehicleId + "} does not exist in repository");
                    return;
                }
                vehicleMessage.Vehicle = (vehicle);
            }

            this.ResourceManager.UpdateVehicle(vehicle, "Path", "");
        }

        public void UpdateVehiclePath(VehicleMessageEx vehicleMessage)
        {
            //wook 20170705. VehiclePath瑜� �떎�떆 李얜뒗寃껋씠 �븘�땶 �깮�꽦�릺�뼱�엳�뒗 TransportCommand�쓽 Path瑜� �궗�슜
            TransportCommandEx transportCommand = new TransportCommandEx();
            if (vehicleMessage.TransportCommandId != null && !string.IsNullOrEmpty(vehicleMessage.TransportCommandId))
            {
                transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
            }
            else
            {
                transportCommand = vehicleMessage.TransportCommand;
            }
            String path = "";

            if (transportCommand != null)
            {
                path = transportCommand.Path;
            }
            ////		TransportCommandEx transportCommand = vehicleMessage.TransportCommand;
            //		LocationEx currentLocation = this.ResourceManager.GetLocation(transportCommand.Source);
            //		LocationEx destLocation = this.ResourceManager.GetLocation(transportCommand.Dest);
            //		PathInfoEx pathInfo = new PathInfoEx();
            //		
            //		if (currentLocation == null) 
            //		{
            //			StationACS sourceStation = new StationACS();
            //			NodeEx currentNode = this.ResourceManager.GetNode(vehicleMessage.Vehicle.CurrentNodeId);
            //			NodeEx destNode = this.ResourceManager.GetNode(destLocation.StationId);
            //			
            //			pathInfo = this.PathManager.searchDynamicPaths(sourceStation, currentNode, destNode, false, null, false);
            //		}
            //		else
            //		{
            //			pathInfo = this.PathManager.SearchPaths(currentLocation, destLocation);
            //		}
            //		
            //		PathEx paths = (PathEx) pathInfo.PathsAvailable.get(0);
            //		String path = "";
            //		if (currentLocation != null) 
            //		{
            //			path = currentLocation.StationId;
            //		}
            //		
            //		if (path != null && !path.isEmpty()) 
            //		{
            //			path += ","; 
            //		}
            //		
            //		List listPaths = paths.NodeIds;
            //		
            //		for (Iterator iterator = listPaths.iterator(); iterator.MoveNext();) {
            //			String appPath = (String) iterator.Current;
            //			path += appPath;
            //			if (iterator.hasNext()) 
            //			{
            //				path += ",";
            //			}
            //		}

            VehicleEx vehicle;
            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
                path = vehicleMessage.Vehicle.Path;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                vehicleMessage.Vehicle = (vehicle);
                path = vehicleMessage.Vehicle.Path;
            }

            this.ResourceManager.UpdateVehicle(vehicle, "Path", path);
        }


        public void CreateTransportCommandRequest(TransferMessageEx transferMessage)
        {

            String jobId = transferMessage.TransportCommandId;
            String vehicleId = transferMessage.VehicleId;

            this.RequestManagerExImplement.CreateTransportCommandRequest(jobId, vehicleId);
        }

        public void CreateTransportCommandRequest(VehicleMessageEx vehicleMessage)
        {

            String jobId = vehicleMessage.TransportCommandId;
            String vehicleId = vehicleMessage.VehicleId;

            this.RequestManagerExImplement.CreateTransportCommandRequest(jobId, vehicleId);
        }


        public void DeleteTransportCommandRequest(VehicleMessageEx VehicleMessageEx)
        {

            String jobId = VehicleMessageEx.CarrierId;
            String vehicleId = VehicleMessageEx.VehicleId;

            this.RequestManagerExImplement.DeleteTransportCommandRequest(jobId, vehicleId);
        }


        public bool IsTransportCommandRequestReplied(TransferMessageEx transferMessage)
        {

            String jobId = transferMessage.TransportCommandId;
            String vehicleId = transferMessage.VehicleId;
            TransportCommandRequestEx transportCommandRequestACS = this.RequestManagerExImplement.GetTransportCommandRequest(jobId, vehicleId);

            if (transportCommandRequestACS == null)
            {
                // RAIL-CARRIERTRANSFER�썑 RAIL-CARRIERTRANSFERREPLY媛� �젙�긽�쟻�쑝濡� �닔�떊 �릺�뼱 transportCommandRequest媛� �씠誘� �궘�젣 �맂 Case
                return true;
            }
            else
            {
                transferMessage.Cause = (TransportCommandEx.CAUSE_REQ_TIMEOUT);
                // request data �궘�젣
                this.RequestManagerExImplement.DeleteTransportCommandRequest(transportCommandRequestACS.JobId, transportCommandRequestACS.VehicleId);
                //logger.Error("TransportCommandRequest(sended 5 second ago) is not replied " + transferMessage);
                return false;
            }
        }

        public bool IsTransportCommandRequestReplied(VehicleMessageEx vehicleMessage)
        {

            String jobId = vehicleMessage.TransportCommandId;
            //		//wook 20170620 carrierId Set�븞�맆�븣 蹂댁셿濡쒖쭅
            //		if (jobId == null || jobId.isEmpty()) 
            //		{
            //			jobId = vehicleMessage.TransportCommandId;
            //		}
            String vehicleId = vehicleMessage.VehicleId;
            TransportCommandRequestEx transportCommandRequest = this.RequestManagerExImplement.GetTransportCommandRequest(jobId, vehicleId);

            if (transportCommandRequest == null)
            {
                // RAIL-CARRIERTRANSFER�썑 RAIL-CARRIERTRANSFERREPLY媛� �젙�긽�쟻�쑝濡� �닔�떊 �릺�뼱 transportCommandRequest媛� �씠誘� �궘�젣 �맂 Case
                return true;
            }
            else
            {
                vehicleMessage.Cause = (TransportCommandEx.CAUSE_REQ_TIMEOUT);
                // request data �궘�젣
                this.RequestManagerExImplement.DeleteTransportCommandRequest(transportCommandRequest.JobId, transportCommandRequest.VehicleId);
                //logger.Error("TransportCommandRequest(sended 5 second ago) is not replied " + vehicleMessage);
                return false;
            }
        }


        public bool CheckIfReportAGVJOBREPORTOrNot(VehicleMessageEx vehicleMessage)
        {

            TransportCommandEx transportCommand = new TransportCommandEx();

            if (string.IsNullOrEmpty(vehicleMessage.TransportCommandId))
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
            }

            if (transportCommand == null)
            {
                return false;
            }

            vehicleMessage.TransportCommand = transportCommand;
            vehicleMessage.TransportCommandId = transportCommand.Id;
            vehicleMessage.CarrierId = transportCommand.CarrierId;

            if (!string.IsNullOrEmpty(transportCommand.Source))
            {
                LocationEx location = this.ResourceManager.GetLocation(transportCommand.Source);
                if (location != null)
                {
                    if (location.StationId.Equals(vehicleMessage.DestNodeId, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;

        }


        public bool CheckAndUpdateLoadArrivedTimeOrUnloadArrivedTime(VehicleMessageEx vehicleMessage)
        {

            TransportCommandEx transportCommand = new TransportCommandEx();

            if (string.IsNullOrEmpty(vehicleMessage.TransportCommandId))
            {
                transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
            }
            else
            {
                transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
            }

            if (transportCommand == null)
            {
                return false;
            }
            else
            {
                logger.Info(transportCommand);
            }


            vehicleMessage.TransportCommand = transportCommand;
            vehicleMessage.CarrierId = transportCommand.CarrierId;

            if (!string.IsNullOrEmpty(transportCommand.Source))
            {

                LocationEx location = this.ResourceManager.GetLocation(transportCommand.Source);
                if (location != null)
                {
                    if (location.StationId.Equals(vehicleMessage.NodeId, StringComparison.OrdinalIgnoreCase))
                    {

                        if (transportCommand.LoadedTime == null)
                        {
                            if (transportCommand.LoadArrivedTime == null)
                            {
                                this.UpdateTransportCommandLoadArrivedTime(vehicleMessage);
                            }
                            return true;
                        }
                    }
                }
            }
            else
            {
                logger.Warn("source{" + transportCommand.Source + "} is null in transportCommand, " + transportCommand);
            }

            if (!string.IsNullOrEmpty(transportCommand.Dest))
            {

                LocationEx location = this.ResourceManager.GetLocation(transportCommand.Dest);
                if (location != null)
                {
                    if (location.StationId.Equals(vehicleMessage.NodeId, StringComparison.OrdinalIgnoreCase))
                    {
                        this.UpdateTransportCommandUnloadArrivedTime(vehicleMessage);
                        return false;
                    }
                }
            }
            else
            {
                logger.Warn("dest{" + transportCommand.Source + "} is null in transportCommand, " + transportCommand);
            }
            return false;

        }


        public bool CreateTransportCommandAsSourceVehicle(TransferMessageEx transferMessage)
        {

            bool result = false;
            if (!this.TransferManager.ExistTransportCommand(transferMessage.TransportCommandId))
            {

                transferMessage.JobType = (TransportCommandEx.JOBTYPE_AUTOCALL);
                TransportCommandEx transportCommand = this.TransferManager.CreateTransportCommand(transferMessage);
                if (transportCommand != null)
                {

                    PathInfoEx pathInfo = transferMessage.PathInfo;

                    transportCommand.QueuedTime = null;
                    transportCommand.AssignedTime = null;
                    transportCommand.StartedTime = null;
                    transportCommand.LoadArrivedTime = null;
                    transportCommand.LoadingTime = null;
                    transportCommand.LoadedTime = null;


                    transportCommand.State = (TransportCommandEx.STATE_TRANSFERRING_DEST);

                    if (transportCommand.BayId != null && !string.IsNullOrEmpty(transportCommand.BayId))
                    {
                        // do nothing.
                        // already has bayid.
                    }
                    else
                    {
                        VehicleEx vehicle = this.ResourceManager.GetVehicle(transferMessage.VehicleId);
                        if (vehicle != null)
                        {
                            transportCommand.BayId = (vehicle.BayId);
                        }
                    }


                    this.TransferManager.UpdateTransportCommand(transportCommand);
                    transferMessage.TransportCommand = (transportCommand);
                    logger.Info(transportCommand);
                    result = true;
                }
                else
                {
                    transferMessage.ReplyCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTABLETOEXECUTE.Item1;
                    transferMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_NOTABLETOEXECUTE.Item2;
                    logger.Error("failed to create transportCommand, " + transferMessage);
                }

            }
            else
            {
                transferMessage.ReplyCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item2;
                logger.Error("transportCommand{" + transferMessage.TransportCommandId + "} already exist in repository, " + transferMessage);
            }

            return result;
        }

        public void UpdateVehicleAcsDestNodeIdToDest(TransferMessageEx transferMessage)
        {

            VehicleEx vehicle;
            if (transferMessage.Vehicle != null)
            {
                vehicle = transferMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(transferMessage.VehicleId);
            }
            TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(transferMessage.VehicleId);

            if (transportCommand != null)
            {
                String dest = transportCommand.Dest;
                String destNodeId = this.ResourceManager.GetLocation(dest).StationId;

                this.ResourceManager.UpdateVehicleAcsDestNodeId(vehicle, destNodeId, transferMessage.MessageName);
            }
        }

        public TransportCommandEx GetPossibleStealTransportCommand(VehicleMessageEx vehicleMessage)
        {

            try
            {
                VehicleEx vehicle = vehicleMessage.Vehicle;
                if (vehicle == null)
                {
                    vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
                }
                if (vehicle != null)
                {

                    String bayId = vehicle.BayId;
                    IList assigndTransportCommands = this.TransferManager.GetTransportCommandsByStateAndBayId(TransportCommandEx.STATE_ASSIGNED, bayId);

                    //Assigned 된 transpotCommand가 없을 경우.
                    if (assigndTransportCommands.Count == 0) return null;

                    int depthLimit = 20;
                    String currentNodeId = vehicle.CurrentNodeId;
                    Hashtable transportCommandMap = this.ConvertTransportCommandsBySourceNodeId(assigndTransportCommands);
                    IList linkViews = this.PathManager.GetLinkViewsByBayId(false, vehicle.BayId);
                    Dictionary<string, IList> linksMap = this.PathManager.ConvertLinkViewsToMapByFromNode(linkViews);
                    IList nearTransportCommands = this.SortNearTransortCommandByCurrentNodeId(currentNodeId, transportCommandMap, linksMap, depthLimit);

                    foreach (var item1 in nearTransportCommands)
                    //for (IEnumerator iterator = nearTransportCommands.GetEnumerator(); iterator.MoveNext(); )
                    {
                        IList nearTransportCommand = (IList)item1;
                        foreach (var item2 in nearTransportCommand)
                        {
                            TransportCommandEx transportCommand = (TransportCommandEx)item2;

                            DateTime currentTime = DateTime.Now;//new DateTime();

                            if (transportCommand.AssignedTime < currentTime.AddSeconds(-5))//(TimeUtils.addSeconds(currentTime, -5)))
                            {

                                if (this.NeedToStillTransportCommand(transportCommand, vehicle, linksMap, depthLimit))
                                {

                                    if (this.TransferManager.UpdateTransportCommandStateByChangeVehicle(transportCommand) > 0)
                                    {

                                        VehicleEx originalVehicle = this.ResourceManager.GetVehicle(transportCommand.VehicleId);
                                        logger.Warn("stealTransportCommand{" + transportCommand.Id + "}, changeVehicle{" + originalVehicle.Id + " -> " + vehicle.Id + "}, " + transportCommand, transportCommand.CarrierId, transportCommand.Id, "]=" + vehicle.Id);
                                        return transportCommand;
                                    }
                                    else
                                    {
                                        logger.Warn("already changedVehicle, " + transportCommand, transportCommand.CarrierId, transportCommand.Id, "]=" + vehicle.Id);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    logger.Warn("vehicle{" + vehicleMessage.VehicleId + "} does not exist, " + vehicleMessage, vehicleMessage);
                }
            }
            catch (Exception e)
            {
                logger.Error(e, e);
            }

            return null;
        }

        public bool NeedToStillTransportCommand(TransportCommandEx transportCommand, VehicleEx vehicle, Dictionary<string, IList> linksMap, int depthLimit)
        {

            bool result = false;

            VehicleEx commandVehicle = this.ResourceManager.GetVehicle(transportCommand.VehicleId);
            String commandVehicleNodeId = commandVehicle.CurrentNodeId;
            String currentVehicleNodeId = vehicle.CurrentNodeId;
            LocationEx sourceLocation = this.PathManager.GetLocationByPortId(transportCommand.Source);
            if (sourceLocation != null)
            {

                StationEx sourceStation = this.PathManager.GetStation(sourceLocation.StationId);
                NodeEx sourceNode = this.PathManager.SearchNodeByStationAsDest(sourceStation);
                if (!sourceNode.Id.Equals(commandVehicleNodeId))
                {

                    int commandVehicleLength = this.GetLength(commandVehicleNodeId, sourceNode.Id, linksMap, depthLimit);
                    int currentVehicleLength = this.GetLength(currentVehicleNodeId, sourceNode.Id, linksMap, depthLimit);
                    if (currentVehicleLength < commandVehicleLength)
                    {

                        //					//logger.Warn("orgVehicle{" + commandVehicle.Id + "}, length{" + commandVehicleLength+ "}"]=""]=""]=transportCommand.Id, ""]=commandVehicle.Id);
                        //					//logger.Warn("newVehicle{" + vehicle.Id + "}, length{" + currentVehicleLength+ "}"]=""]=""]=transportCommand.Id, ""]=vehicle.Id);
                        //logger.Warn("commandVehicle{" + commandVehicle.Id + "}, length{" + commandVehicleLength + "}" + "\n" + "currentVehicle{" + vehicle.Id + "}, length{" + currentVehicleLength + "}"]=""]=""]=transportCommand.Id, ""]=vehicle.Id);
                        result = true;
                    }
                }
            }

            return result;
        }

        public int GetLength(String sourceNodeId, String destNodeId, Dictionary<string, IList> linksMap, int depthLimit)
        {

            IList currentNodeIds = new ArrayList();
            currentNodeIds.Add(sourceNodeId);

            for (int i = 0; i < depthLimit; i++)
            {

                IList nextNodeIds = new ArrayList();
                for (IEnumerator iterator = currentNodeIds.GetEnumerator(); iterator.MoveNext();)
                {
                    IList links = null;
                    String nodeId = (String)iterator.Current;

                    if (linksMap.ContainsKey(nodeId)) // AGV의 현재 no
                    {
                        links = (IList)linksMap[nodeId];  //***//
                    }

                    if (links != null)
                    {

                        for (IEnumerator iterator2 = links.GetEnumerator(); iterator2.MoveNext();)
                        {

                            LinkViewEx link = (LinkViewEx)iterator2.Current;
                            String nextNodeId = link.ToNodeId;
                            if (destNodeId.Equals(nextNodeId))
                            {
                                return i;
                            }
                            nextNodeIds.Add(nextNodeId);
                        }
                    }
                }
                currentNodeIds.Clear();
                currentNodeIds = nextNodeIds;

            }
            return depthLimit;
        }
        public IList SortNearTransortCommandByCurrentNodeId(string currentNodeId, Hashtable transportCommandMap, Dictionary<string, IList> linksMap, int limitDepth)
        {

            IList nearTransportCommands = new ArrayList();
            IList currentNodeIds = new ArrayList();
            currentNodeIds.Add(currentNodeId);

            for (int i = 0; i < limitDepth; i++)
            {

                IList nextNodeIds = new ArrayList();
                for (IEnumerator iterator = currentNodeIds.GetEnumerator(); iterator.MoveNext();)
                {

                    String nodeId = (String)iterator.Current;
                    IList links = null;

                    if (linksMap.ContainsKey(nodeId))
                    {
                        links = (IList)linksMap[nodeId];
                    }

                    if (links != null)
                    {

                        for (IEnumerator iterator2 = links.GetEnumerator(); iterator2.MoveNext();)
                        {

                            LinkViewEx link = (LinkViewEx)iterator2.Current;
                            String nextNodeId = link.ToNodeId;
                            if (transportCommandMap.ContainsKey(nextNodeId))
                            {
                                nearTransportCommands.Add((IList)transportCommandMap[nextNodeId]);
                            }
                            nextNodeIds.Add(nextNodeId);
                        }
                    }
                }
                currentNodeIds.Clear();
                currentNodeIds = nextNodeIds;
            }
            return nearTransportCommands;
        }

        public Hashtable ConvertTransportCommandsBySourceNodeId(IList transportCommands)
        {

            Hashtable transportCommandMap = new Hashtable();

            foreach (object obj in transportCommands)
            {
                TransportCommandEx transportCommand = (TransportCommandEx)obj;
                LocationEx sourceLocation = this.PathManager.GetLocationByPortId(transportCommand.Source);
                if (sourceLocation != null)
                {

                    StationEx sourceStation = this.PathManager.GetStation(sourceLocation.StationId);
                    NodeEx sourceNode = this.PathManager.SearchNodeByStationAsDest(sourceStation);
                    if (transportCommandMap.ContainsKey(sourceNode.Id))
                    {
                        IList transportCommandsSameNode = (IList)transportCommandMap[sourceNode.Id];
                        transportCommandsSameNode.Add(transportCommand);
                    }
                    else
                    {

                        IList transportCommandsSameNode = new ArrayList();
                        transportCommandsSameNode.Add(transportCommand);
                        transportCommandMap[sourceNode.Id] = transportCommandsSameNode;
                    }
                }
                else
                {
                    //KSB ChargeJob 일경우 Source는 없고, Dest만 존재함
                    //logger.Warn("can not find sourceLocation, source{" + transportCommand.Source + "}");
                    logger.Warn("can not find sourceLocation, source{" + transportCommand.Id + "}");
                }
            }

            return transportCommandMap;
        }

        public bool IsSameTransportCommandVehicleId(VehicleMessageEx vehicleMessage)
        {

            bool result = false;

            TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);

            if (transportCommand != null)
            {

                result = true;
            }
            else
            {
                logger.Warn("Can not Find Vehicle's Job, Vehicle: " + vehicleMessage.VehicleId + ". Already Still Job");
            }

            return result;
        }

        public bool UpdateTransportCommandDest(TransferMessageEx transferMessage)
        {

            bool result = false;

            TransportCommandEx transportCommand = transferMessage.TransportCommand;
            if (transportCommand == null)
            {
                transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {

                transportCommand.Dest = (transferMessage.DestMachine + ":" + transferMessage.DestUnit);
                this.TransferManager.UpdateTransportCommand(transportCommand);
                transferMessage.TransportCommand = (transportCommand);
                logger.Info(transportCommand);
                result = true;
            }
            else
            {
                transferMessage.ReplyCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item2;
                logger.Error("transportCommand{" + transferMessage.TransportCommandId + "} already exist in repository, " + transferMessage);
            }

            return result;
        }

        public bool UpdateTransportCommandNewDest(TransferMessageEx transferMessage)
        {

            bool result = false;

            String newDest = transferMessage.DestMachine + ":" + transferMessage.DestUnit;
            TransportCommandEx transportCommand = transferMessage.TransportCommand;
            if (transportCommand == null)
            {
                transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {
                //Hashtable additionalInfoMap = MapUtils.stringToMap(transportCommand.AdditionalInfo);
                //String previousValue = (String)additionalInfoMap.put("newDest", newDest);
                Hashtable additionalInfoMap = MapUtility.StringToMap(transportCommand.AdditionalInfo);

                additionalInfoMap["newDest"] = newDest;
                //logger.fine("newDest{" + StringUtils.defaultString(previousValue) + "->" + newDest + "}, " + transferMessage, transferMessage);

                //transportCommand.AdditionalInfo = (MapUtils.mapToString(additionalInfoMap));
                transportCommand.AdditionalInfo = (MapUtility.MapToString(additionalInfoMap));//(mapToString(additionalInfoMap));
                this.TransferManager.UpdateTransportCommandAdditionalInfo(transportCommand);
                transferMessage.TransportCommand = (transportCommand);
                //logger.fine(transportCommand);
                result = true;
            }
            else
            {
                transferMessage.ReplyCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item2;
                //logger.Error("transportCommand{" + transferMessage.TransportCommandId + "} already exist in repository, " + transferMessage);
            }

            return result;
        }

        public bool DeleteTransportCommandNewDest(TransferMessageEx transferMessage)
        {

            bool result = false;

            String newDest = "";
            TransportCommandEx transportCommand = transferMessage.TransportCommand;
            if (transportCommand == null)
            {
                transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {

                // Hashtable additionalInfoMap = MapUtils.stringToMap(transportCommand.AdditionalInfo);
                //String previousValue = (String)additionalInfoMap.put("newDest", newDest);
                Hashtable additionalInfoMap = MapUtility.StringToMap(transportCommand.AdditionalInfo);
                additionalInfoMap["newDest"] = newDest;
                //logger.fine("newDest{" + StringUtils.defaultString(previousValue) + "->" + newDest + "}, " + transferMessage, transferMessage);

                //transportCommand.AdditionalInfo = (MapUtils.mapToString(additionalInfoMap));
                transportCommand.AdditionalInfo = MapUtility.MapToString(additionalInfoMap);
                this.TransferManager.UpdateTransportCommandAdditionalInfo(transportCommand);
                transferMessage.TransportCommand = (transportCommand);
                //logger.fine(transportCommand);
                result = true;
            }
            else
            {
                transferMessage.ReplyCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item2;
                //logger.Error("transportCommand{" + transferMessage.TransportCommandId + "} already exist in repository, " + transferMessage);
            }

            return result;
        }

        public bool IsNewDestTransferReply(VehicleMessageEx vehicleMessage)
        {

            bool result = false;

            TransportCommandEx transportCommand = vehicleMessage.TransportCommand;
            if (transportCommand == null)
            {
                transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {

                String newDest = this.TransferManager.GetAdditionalInfo(transportCommand, "newDest");
                if (!string.IsNullOrEmpty(newDest))
                {

                    String destNodeId = this.ResourceManager.GetLocation(newDest).StationId;
                    if (vehicleMessage.DestNodeId.Equals(destNodeId))
                    {
                        result = true;
                    }
                }
                else
                {
                    //logger.fine("newDest{" + newDest + "} does not exist in repository, " + vehicleMessage, vehicleMessage);
                }
            }
            return result;
        }

        public bool ChangeTransportCommandDestByNewDest(TransferMessageEx transferMessage)
        {

            bool result = false;

            TransportCommandEx transportCommand = transferMessage.TransportCommand;
            if (transportCommand == null)
            {
                transportCommand = this.TransferManager.GetTransportCommand(transferMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {

                String newDest = this.TransferManager.GetAdditionalInfo(transportCommand, "newDest");
                this.DeleteTransportCommandNewDest(transferMessage);
                transportCommand.Dest = (newDest);
                this.TransferManager.UpdateTransportCommand(transportCommand);
                transferMessage.TransportCommand = (transportCommand);
                //logger.fine(transportCommand);
                result = true;
            }
            else
            {
                transferMessage.ReplyCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item1;
                transferMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item2;
                //logger.Error("transportCommand{" + transferMessage.TransportCommandId + "} already exist in repository, " + transferMessage);
            }

            return result;
        }

        public bool ChangeTransportCommandDestByNewDest(VehicleMessageEx vehicleMessage)
        {

            bool result = false;

            TransportCommandEx transportCommand = vehicleMessage.TransportCommand;
            if (transportCommand == null)
            {
                transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {

                String newDest = this.TransferManager.GetAdditionalInfo(transportCommand, "newDest");

                //Hashtable additionalInfoMap = MapUtils.stringToMap(transportCommand.AdditionalInfo);
                //String previousValue = (String)additionalInfoMap.put("newDest", newDest);

                Hashtable additionalInfoMap = MapUtility.StringToMap(transportCommand.AdditionalInfo);
                additionalInfoMap["newDest"] = newDest;

                String newDestInfo = "newDest{" + transportCommand.Dest + "->" + newDest + "}";
                //logger.fine(newDestInfo + ", " + vehicleMessage, vehicleMessage);

                // [SJP님께서 테스트 진행하실겁니다.] 
                // Oracle Database에서 null을 진짜 null로 return 해서...Null exception 예외처리.
                if (string.IsNullOrEmpty(transportCommand.Description))
                {
                    transportCommand.Description = string.Empty;
                }

                if (transportCommand.Description.Contains(", newDest{"))
                {
                    string[] split = transportCommand.Description.Split(',');
                    String originalDescription = split[0];
                    String description = originalDescription + ", newDest{" + transportCommand.Dest + "->" + newDest + "}";
                    transportCommand.Description = description;
                }
                else
                {
                    String description = transportCommand.Description + ", newDest{" + transportCommand.Dest + "->" + newDest + "}";
                    transportCommand.Description = description;
                }
                //transportCommand.AdditionalInfo = (MapUtils.mapToString(additionalInfoMap));
                transportCommand.AdditionalInfo = (MapUtility.MapToString(additionalInfoMap));
                this.TransferManager.UpdateTransportCommandAdditionalInfo(transportCommand);

                transportCommand.Dest = (newDest);
                this.TransferManager.UpdateTransportCommand(transportCommand);
                vehicleMessage.TransportCommand = (transportCommand);
                //logger.fine(transportCommand);
                result = true;
            }
            else
            {
                vehicleMessage.ResultCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item1;
                vehicleMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item2;
                //logger.Error("transportCommand{" + vehicleMessage.TransportCommandId + "} already exist in repository, " + vehicleMessage);
            }

            return result;
        }
        public bool DeleteTransportCommandNewDest(VehicleMessageEx vehicleMessage)
        {

            bool result = false;

            String newDest = "";
            TransportCommandEx transportCommand = vehicleMessage.TransportCommand;
            if (transportCommand == null)
            {
                transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
            }

            if (transportCommand != null)
            {

                //Map additionalInfoMap = MapUtils.stringToMap(transportCommand.AdditionalInfo);
                //String previousValue = (String)additionalInfoMap.put("newDest", newDest);
                Hashtable additionalInfoMap = MapUtility.StringToMap(transportCommand.AdditionalInfo);
                additionalInfoMap["newDest"] = newDest;
                //logger.fine("newDest{" + StringUtils.defaultString(previousValue) + "->" + newDest + "}, " + vehicleMessage, vehicleMessage);

                //transportCommand.AdditionalInfo=(MapUtils.mapToString(additionalInfoMap));
                transportCommand.AdditionalInfo = (MapUtility.MapToString(additionalInfoMap));
                this.TransferManager.UpdateTransportCommandAdditionalInfo(transportCommand);
                vehicleMessage.TransportCommand = (transportCommand);
                //logger.fine(transportCommand);
                result = true;
            }
            else
            {
                vehicleMessage.ResultCode = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item1;
                vehicleMessage.Cause = AbstractManager.ID_RESULT_TRANSPORTCOMMAND_ALREADYREQUESTED.Item2;
                //logger.Error("transportCommand{" + vehicleMessage.TransportCommandId + "} already exist in repository, " + vehicleMessage);
            }

            return result;
        }


        public void UpdateVehicleAcsDestNodeIdByDest(VehicleMessageEx vehicleMessage)
        {

            VehicleEx vehicle;
            if (vehicleMessage.Vehicle != null)
            {
                vehicle = vehicleMessage.Vehicle;
            }
            else
            {
                vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
            }
            TransportCommandEx transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);

            if (transportCommand != null)
            {

                String dest = transportCommand.Dest;
                String destNodeId = this.ResourceManager.GetLocation(dest).StationId;
                this.ResourceManager.UpdateVehicleAcsDestNodeId(vehicle, destNodeId, vehicleMessage.MessageName);
            }
        }

        public int ChangeTransportCommandStateAndVehicleIdWhenStealCommand(VehicleMessageEx vehicleMessage)
        {

            int updateCount = -1;

            TransportCommandEx transportCommand = vehicleMessage.TransportCommand;
            if (vehicleMessage.TransportCommand == null)
            {

                if (string.IsNullOrEmpty(vehicleMessage.TransportCommandId))
                {
                    transportCommand = this.TransferManager.GetTransportCommandByVehicleId(vehicleMessage.VehicleId);
                }
                else
                {
                    transportCommand = this.TransferManager.GetTransportCommand(vehicleMessage.TransportCommandId);
                }
            }

            if (transportCommand != null)
            {

                Dictionary<string, object> setAttributes = new Dictionary<string, object>();

                transportCommand.State = (TransportCommandEx.STATE_ASSIGNED);
                transportCommand.VehicleId = (vehicleMessage.VehicleId);

                setAttributes["State"] = transportCommand.State;
                setAttributes["VehicleId"] = transportCommand.VehicleId;

                updateCount = this.TransferManager.UpdateTransportCommand(transportCommand, setAttributes);
            }

            return updateCount;
        }

        public bool ExistTransportCommand(TransferMessageEx transferMessage)
        {

            bool result = false;

            String transportCommandId = transferMessage.TransportCommandId;
            if (!string.IsNullOrEmpty(transportCommandId))
            {

                result = this.TransferManager.ExistTransportCommand(transportCommandId);
            }
            return result;
        }

        public VehicleMessageEx CheckPathAndReCalculate(VehicleMessageEx vehicleMessage)
        {
            VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
            // NodeEx node = this.ResourceManager.GetNode(vehicleMessage.NodeId);
            NodeEx node = this.CacheManager.GetNode(vehicleMessage.NodeId);
            //	Map<String, List<VehicleACS>> vehicleMap = this.extensionManager.mapVehicleByCurrentNode(vehicleMessage.getVehicleId());
            //	vehicleMessage.setAdditionalInfoMap(vehicleMap);

            // exist vehicle and node 
            if (vehicle != null && node != null)
            {
                string vehiclePath = vehicle.Path;

                // vehiclePath isn't null or empty
                if (!string.IsNullOrEmpty(vehiclePath))
                {
                    // path : expected moving time
                    string fullpath = vehiclePath.Split(':')[0];
                    string time = vehiclePath.Split(':')[1];

                    // vehicle past path history
                    string pastPath = "";
                    string path = "";

                    // exist char '-'
                    if (fullpath.Split('-').Length > 1)
                    {
                        pastPath = fullpath.Split('-')[1];
                        path = fullpath.Split('-')[0];
                    }
                    else
                    {
                        path = fullpath;
                    }

                    string currentNode = vehicleMessage.NodeId;

                    // vehicle move to follow path 
                    if (currentNode.Equals(path.Split(',')[0]))
                    {
                        // TTTTTTTTTTTTTTTTT

                        //20230425 Modify ORDER_THREE
                        //OrderPairNodeEx orderPair = this.ResourceManager.SearchNextOrderNode(path + ":" + totalTime);
                        OrderPairNodeEx orderPair = this.ResourceManager.SearchNextOrderNode(vehicle.Path);
                        if (orderPair != null)
                        {
                            //logger.info("Find order node: " + orderPair.getOrderGroup());
                            VehicleOrderEx vehicleOrder = this.ResourceManager.GetVehicleOrderByVehicleId(vehicleMessage.VehicleId);
                            if (vehicleOrder != null)
                            {
                                if (!vehicleOrder.OrderNode.Equals(orderPair.OrderGroup))
                                {
                                    VehicleMessageEx newVehicleMessage = new VehicleMessageEx();
                                    newVehicleMessage.Vehicle = vehicle;
                                    newVehicleMessage.VehicleId = vehicle.Id;
                                    newVehicleMessage.NodeId = orderPair.OrderGroup;
                                    newVehicleMessage.KeyData = orderPair.Status;

                                    vehicleOrder.OrderNode = orderPair.OrderGroup;

                                    //20230417 ORDER_THREE
                                    //vehicleOrder.OrderTime = new DateTime();
                                    vehicleOrder.OrderTime = DateTime.Now;
                                    //

                                    this.ResourceManager.UpdateVehicleOrder(vehicleOrder);
                                    return newVehicleMessage;
                                }
                            }
                            else
                            {
                                VehicleMessageEx newVehicleMessage = new VehicleMessageEx();
                                newVehicleMessage.Vehicle = vehicle;
                                newVehicleMessage.VehicleId = vehicle.Id;
                                newVehicleMessage.NodeId = orderPair.OrderGroup;
                                newVehicleMessage.KeyData = orderPair.Status;

                                vehicleOrder = new VehicleOrderEx();

                                vehicleOrder.OrderNode = orderPair.OrderGroup;
                                vehicleOrder.VehicleId = vehicleMessage.VehicleId;
                                vehicleOrder.OrderTime = DateTime.Now;
                                this.ResourceManager.CreateVehicleOrder(vehicleOrder);
                                return newVehicleMessage;
                            }
                        }
                        /*
                        VehicleOrderEx vehicleOrder = this.ResourceManager.GetVehicleOrderByVehicleId(vehicle.Id);
                        if (vehicleOrder == null)
                        {
                            OrderPairNodeEx orderPair = this.ResourceManager.SearchNextOrderNode(vehicle.Path);
                            if (orderPair != null)
                            {
                                logger.Info("Find order node : " + orderPair.OrderGroup);

                                VehicleMessageEx newVehicleMessage = new VehicleMessageEx();
                                newVehicleMessage.Vehicle = vehicle;
                                newVehicleMessage.VehicleId = vehicle.Id;
                                newVehicleMessage.NodeId = orderPair.OrderGroup;
                                newVehicleMessage.KeyData = orderPair.Status;

                                vehicleOrder = new VehicleOrderEx();

                                vehicleOrder.OrderNode = orderPair.OrderGroup;
                                vehicleOrder.OrderTime = DateTime.Now;

                                this.ResourceManager.UpdateVehicleOrder(vehicleOrder);
                                return newVehicleMessage;

                            }
                            else
                            {
                                //20230314 ORDER_ONEPOINT
                                //주석처리 동일포지션 
                                //VehicleMessageEx newVehicleMessage = new VehicleMessageEx();
                                //newVehicleMessage.VehicleId = vehicle.Id;
                                //newVehicleMessage.Vehicle = vehicle;
                                //newVehicleMessage.NodeId = "0000," + vehicle.AcsDestNodeId + ",0000";
                                //newVehicleMessage.KeyData = "GO_DEST";
                                //return newVehicleMessage;
                                //return null;
                                //
                            }
                        }
                        */
                        //


                        // if would vehicle move to follow path, return null (no recalculation path)
                        return null;
                    }
            

                    string destNode = path.Split(',').Last();

                    // Check vehicle move to follow path, remove missing path in path string...
                    // if newPath is null, vehicle doesn't move to follow the path
                    OCodeLog.Info(string.Format("SearchNodeInPathAndReCalculate() - vehicle id:{0} | currentNode:{1} | path:{2} | pastPath:{3}", vehicle.Id, currentNode, path, pastPath));
                    string newPath = this.PathManager.SearchNodeInPathAndReCalculate(vehicle.Id, currentNode, path, pastPath);

                    logger.Info("New path: " + ((newPath == null) ? "" : newPath));

                    // Current Node of vehicle doesn't exist in vehicle path.
                    if (newPath != null && destNode != null && destNode.Equals(vehicle.AcsDestNodeId))
                    {
                        vehicle.Path = newPath;
                        this.UpdateVehiclePath(vehicle, newPath);

                        vehicleMessage.Vehicle = vehicle;

                        OrderPairNodeEx orderPair = this.ResourceManager.SearchNextOrderNode(newPath);
                        if (orderPair != null)
                        {
                            logger.Info("Find order node : " + orderPair.OrderGroup);
                            VehicleOrderEx vehicleOrder = this.ResourceManager.GetVehicleOrderByVehicleId(vehicleMessage.VehicleId);
                            if (vehicleOrder != null)
                            {
                                if (!vehicleOrder.OrderNode.Equals(orderPair.OrderGroup))
                                {
                                    VehicleMessageEx newVehicleMessage = new VehicleMessageEx();
                                    newVehicleMessage.Vehicle = vehicle;
                                    newVehicleMessage.VehicleId = vehicle.Id;
                                    newVehicleMessage.NodeId = orderPair.OrderGroup;
                                    newVehicleMessage.KeyData = orderPair.Status;

                                    vehicleOrder.OrderNode = orderPair.OrderGroup;
                                    vehicleOrder.OrderTime = DateTime.Now;

                                    this.ResourceManager.UpdateVehicleOrder(vehicleOrder);
                                    return newVehicleMessage;

                                }
                                return null;
                            }
                            else
                            {
                                VehicleMessageEx newVehicleMessage = new VehicleMessageEx();
                                newVehicleMessage.Vehicle = vehicle;
                                newVehicleMessage.VehicleId = vehicle.Id;
                                newVehicleMessage.NodeId = orderPair.OrderGroup;
                                newVehicleMessage.KeyData = orderPair.Status;

                                vehicleOrder = new VehicleOrderEx();

                                vehicleOrder.OrderNode = orderPair.OrderGroup;
                                vehicleOrder.VehicleId = vehicleMessage.VehicleId;
                                vehicleOrder.OrderTime = DateTime.Now;
                                this.ResourceManager.CreateVehicleOrder(vehicleOrder);
                                return newVehicleMessage;
                            }
                        }
                        else
                        {
                            VehicleOrderEx vehicleOrder = this.ResourceManager.GetVehicleOrderByVehicleId(vehicle.Id);

                            //20230314 ORDER_ONEPOINT
                            if (vehicleOrder != null)
                            {
                                this.ResourceManager.DeleteVehicleOrderByVehicleID(vehicle.Id);
                            }
                            //목적지 n-1 T_CODE시 O_CODE 전송하지 않게 처리
                            /*
                            if (vehicleOrder != null)
                            {
                                this.ResourceManager.DeleteVehicleOrderByVehicleID(vehicle.Id);
                                VehicleMessageEx newVehicleMessage = new VehicleMessageEx();
                                newVehicleMessage.VehicleId = vehicle.Id;
                                newVehicleMessage.Vehicle = vehicle;
                                newVehicleMessage.NodeId = "0000,0000,0000";
                                newVehicleMessage.KeyData = "ORDER_LF";
                                return newVehicleMessage;
                            }
                            */
                            //

                            return null;
                        }
                    }
                    else
                    {
                        // In case of Vehicle escape in path (derailment)
                        UpdateVehiclePathEmpty(vehicleMessage);
                        return CalculatePathAndTimeToDestDijkstra(vehicleMessage);
                    }
                }
                else
                {
                    // Doesn't exist vehicle path
                    return CalculatePathAndTimeToDestDijkstra(vehicleMessage);
                }
            }
            return null;
        }

        public VehicleMessageEx CalculatePathAndTimeToDestDijkstra(VehicleMessageEx vehicleMessage)
       {
            VehicleEx vhc = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);

            NodeEx currentNode = this.CacheManager.GetNode(vehicleMessage.NodeId);

            // check exist node
            if (currentNode != null)
            {
                // It's deferent vehicle Current Node and reporting vehicle message T-code node 
                if (!vehicleMessage.NodeId.Equals(vhc.CurrentNodeId))
                {
                    // Calculate missing path from vehicle current node to reporting T-Code node
                    PathEx missingPath = this.PathManager.SearchDynamicPathsDijkstraEasy(vhc.CurrentNodeId, currentNode.Id);

                    // Calculate back path from reporting T-Code node to vehicle current node
                    PathEx backPath = this.PathManager.SearchDynamicPathsDijkstraEasy(currentNode.Id, vhc.CurrentNodeId);
                    //			List<String> originalPath = new ArrayList<String>();
                    List<String> missingTag = new List<String>();
                    if (missingPath != null)
                    {
                        if (backPath != null)
                        {
                            // Vehicle go past reporting T-Code node... vehicle move faster than reporting message vehicle current node   
                            if (missingPath.NodeIds.Count > 0 && missingPath.NodeIds.Count <= 10 && missingPath.NodeIds.Count < backPath.NodeIds.Count)
                            {
                                VehicleSearchPathHistory vhs = new VehicleSearchPathHistory();
                                vhs.VehicleId = vehicleMessage.VehicleId;
                                vhs.Path = this.PathManager.ListToString(missingPath.NodeIds);
                                vhs.CurrentNodeId = vhc.CurrentNodeId;
                                vhs.Time = DateTime.Now;
                                this.HistoryManager.CreateVehicleSearchPathHistory(vhs);
                                for (int i = 1; i < missingPath.NodeIds.Count - 2; i++)
                                {
                                    string missNode = (string)missingPath.NodeIds[i];
                                    if (missNode.Length == 4)
                                    {
                                        NodeEx checkNode = this.CacheManager.GetNode(missNode);
                                        if (checkNode != null && !checkNode.Type.Equals(VehicleOrderEx.TYPE_ORDER))
                                            missingTag.Add(missNode);
                                    }
                                }

                                if (missingTag.Count > 0)
                                {
                                    StringBuilder missingNodeString = new StringBuilder();

                                    foreach (string nodeStr in missingTag)
                                    {
                                        missingNodeString.Append(nodeStr + ", ");
                                    }

                                    logger.Error("AGV report fly location: (" + vhs.Id + ") missing node: " + missingNodeString.ToString().TrimEnd(','));
                                }
                            }
                            else
                            {
                                logger.Error("AGV report fly location: (" + vhc.Id + ") fly from: " + vhc.CurrentNodeId + " to: " + currentNode.Id);
                            }
                        }
                        else
                        {
                            if (missingPath.NodeIds.Count > 0 && missingPath.NodeIds.Count <= 10)
                            {
                                VehicleSearchPathHistory vhs = new VehicleSearchPathHistory();
                                vhs.VehicleId = vehicleMessage.VehicleId;
                                vhs.Path = this.PathManager.ListToString(missingPath.NodeIds);
                                vhs.CurrentNodeId = vhc.CurrentNodeId;
                                vhs.Time = DateTime.Now;
                                this.HistoryManager.CreateVehicleSearchPathHistory(vhs);
                                for (int i = 1; i < missingPath.NodeIds.Count - 2; i++)
                                {
                                    string missNode = (string)missingPath.NodeIds[i];
                                    if (missNode.Length == 4)
                                    {
                                        missingTag.Add(missNode);
                                    }
                                }
                                if (missingTag.Count > 0)
                                {
                                    StringBuilder missingNodeString = new StringBuilder();

                                    foreach (string nodeStr in missingTag)
                                    {
                                        missingNodeString.Append(nodeStr + ", ");

                                    }

                                    logger.Error("AGV report fly location: (" + vhs.Id + ") missing node: " + missingNodeString.ToString().TrimEnd(','));
                                }
                            }
                            else
                            {
                                logger.Error("AGV report fly location: (" + vhc.Id + ") fly from: " + vhc.CurrentNodeId + " to: " + currentNode.Id);
                            }
                        }

                        //				if (backPath != null) {
                        //					if (missingPath.getNodeIds().size() > backPath.getNodeIds().size()) {
                        //						originalPath = backPath.getNodeIds();
                        //					} else {
                        //						originalPath = missingPath.getNodeIds();
                        //					}
                        //				}
                    }
                }
            }
            else
            {
                currentNode = this.CacheManager.GetNode(vhc.CurrentNodeId);
            }


            NodeEx destNode = null;
            if (vhc.AcsDestNodeId != null && string.IsNullOrEmpty(vhc.AcsDestNodeId) && vhc.AcsDestNodeId.Equals(vhc.VehicleDestNodeId))
            {
                destNode = this.CacheManager.GetNode(vhc.AcsDestNodeId);
            }
            else
            {
                destNode = this.CacheManager.GetNode(vhc.VehicleDestNodeId);
            }

            if (currentNode != null && destNode != null)
            {

                PathEx originalPath = this.PathManager.SearchDynamicPathsDijkstra(currentNode.Id, destNode.Id, true, false);

                PathEx foundPath = new PathEx();
                BayEx bay = this.ResourceManager.GetBay(vhc.BayId);
                if (bay != null && bay.AgvType != null && bay.AgvType.Equals("HIGH"))
                {
                    if (this.ResourceManager.CheckLinkViewByFromNodeAndBayId(vhc.CurrentNodeId, vhc.BayId))
                    {
                        IList linkViews = this.CacheManager.GetLinkViewByBayId(vhc.BayId);
                        Dictionary<string, IList> linkViewMap = this.PathManager.ConvertLinkViewsToMapByFromNode(linkViews);
                        foundPath = this.PathManager.SearchDynamicPathsDijkstra(currentNode.Id, destNode.Id, linkViewMap, true, false);
                        originalPath = foundPath;
                    }
                    else
                    {
                        foundPath = this.PathManager.SearchDynamicPathsDijkstra(currentNode.Id, destNode.Id, true, true);
                    }
                }
                else
                {
                    SpecialConfig sp = this.ResourceManager.GetValuesBySpecialName(SpecialConfig.SPECIAL_DIVIDEPATH);
                    //			PathACS foundPath = ((PathManagerACSImplEx) this.pathManager).searchDynamicPathsDijkstra(currentNode.getId(), destNode.getId());
                    if (sp != null && ResourceManager.CheckValueBySpecialConfig(sp.Values, vhc.BayId))
                    {
                        foundPath = this.PathManager.SearchDynamicPathsDijkstraDivide(currentNode.Id, destNode.Id);
                    }
                    else
                        foundPath = this.PathManager.SearchDynamicPathsDijkstra(currentNode.Id, destNode.Id, true, true);
                }

                if (foundPath != null && originalPath != null)
                {
                    if (foundPath.Cost > originalPath.Cost * 1.3)
                    {
                        foundPath = originalPath;
                    }

                    string path = this.PathManager.ListToString(foundPath.NodeIds);
                    int totalTime = foundPath.Cost * 60 / 2500;

                    vhc.Path = path + ":" + totalTime;

                    this.UpdateVehiclePath(vhc, path + ":" + totalTime);

                    vehicleMessage.Vehicle = vhc;

                    //logger.info(path + " - need total time: " + totalTime + " seconds");

                    OrderPairNodeEx orderPair = this.ResourceManager.SearchNextOrderNode(path + ":" + totalTime);
                    if (orderPair != null)
                    {
                        //logger.info("Find order node: " + orderPair.getOrderGroup());
                        VehicleOrderEx vehicleOrder = this.ResourceManager.GetVehicleOrderByVehicleId(vehicleMessage.VehicleId);
                        if (vehicleOrder != null)
                        {
                            if (!vehicleOrder.OrderNode.Equals(orderPair.OrderGroup))
                            {
                                VehicleMessageEx newVehicleMessage = new VehicleMessageEx();
                                newVehicleMessage.Vehicle = vhc;
                                newVehicleMessage.VehicleId = vhc.Id;
                                newVehicleMessage.NodeId = orderPair.OrderGroup;
                                newVehicleMessage.KeyData = orderPair.Status;

                                vehicleOrder.OrderNode = orderPair.OrderGroup;

                                //20230417 ORDER_THREE
                                //vehicleOrder.OrderTime = new DateTime();
                                vehicleOrder.OrderTime = DateTime.Now;
                                //

                                this.ResourceManager.UpdateVehicleOrder(vehicleOrder);
                                return newVehicleMessage;
                            }
                        }
                        else
                        {
                            VehicleMessageEx newVehicleMessage = new VehicleMessageEx();
                            newVehicleMessage.Vehicle = vhc;
                            newVehicleMessage.VehicleId = vhc.Id;
                            newVehicleMessage.NodeId = orderPair.OrderGroup;
                            newVehicleMessage.KeyData = orderPair.Status;

                            vehicleOrder = new VehicleOrderEx();

                            vehicleOrder.OrderNode = orderPair.OrderGroup;
                            vehicleOrder.VehicleId = vehicleMessage.VehicleId;
                            vehicleOrder.OrderTime = DateTime.Now;
                            this.ResourceManager.CreateVehicleOrder(vehicleOrder);
                            return newVehicleMessage;
                        }
                    }
                    else
                    {
                        VehicleOrderEx vehicleOrder = this.ResourceManager.GetVehicleOrderByVehicleId(vhc.Id);
                        if (vehicleOrder != null)
                        {
                            this.ResourceManager.DeleteVehicleOrderByVehicleID(vhc.Id);
                            VehicleMessageEx newVehicleMessage = vehicleMessage;
                            newVehicleMessage.VehicleId = vhc.Id;
                            newVehicleMessage.Vehicle = vhc;
                            // TTTTTTTTTT
                            newVehicleMessage.NodeId = "0000," + vhc.AcsDestNodeId + ",0000";
                            newVehicleMessage.KeyData = "GO_DEST";

                            //newVehicleMessage.NodeId = "0000,0000,0000";
                            //newVehicleMessage.KeyData = "ORDER_LF";
                            //
                            return newVehicleMessage;
                        }
                        return null;
                    }
                }
                else
                {
                    //logger.info("Can not find way to come back -.-!");
                    this.CalculatePathAndTimeToDest(vehicleMessage);
                }
            }
            return null;
        }

        public VehicleMessageEx CalculatePathAndTimeToDest(VehicleMessageEx vehicleMessage)
        {
            VehicleEx vhc = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);

            //NodeACS currentNode = this.pathManager.getNode(vehicleMessage.getNodeId());
            NodeEx currentNode = this.CacheManager.GetNode(vehicleMessage.NodeId);

            if (currentNode == null)
            {
                //currentNode = this.pathManager.getNode(vhc.getCurrentNodeId());
                currentNode = this.CacheManager.GetNode(vhc.CurrentNodeId);
            }
            NodeEx destNode = null;
            //LocationACS destLocation = null;
            //If ACS dest node is null or difference with vehicle dest node -> Choice vehicle destnode
            if (vhc.AcsDestNodeId != null && String.IsNullOrEmpty(vhc.AcsDestNodeId) && vhc.AcsDestNodeId.Equals(vhc.VehicleDestNodeId))
            {
                //destNode = this.pathManager.getNode(vhc.getAcsDestNodeId());
                destNode = this.CacheManager.GetNode(vhc.AcsDestNodeId);
            }
            else
            {
                //destNode = this.pathManager.getNode(vhc.getVehicleDestNodeId());
                destNode = this.CacheManager.GetNode(vhc.VehicleDestNodeId);
            }

            //List links = this.extensionManager.getLinkACS(false);
            //Map linkMap = this.extensionManager.convertLinksToMapByToNode(links);
            Dictionary<string, List<LinkEx>> linksMap = this.CacheManager.ConvertLinksToMapByToNode();

            if (currentNode != null && destNode != null)
            {
                List<string> pathed = new List<string>();
                pathed.Add(destNode.Id);

                IList paths = new ArrayList();
                paths.Add(pathed);

                Dictionary<List<string>, int> pathMap = new Dictionary<List<string>, int>();
                pathMap.Add(pathed, 0);

                Dictionary<string, IList> checkedPaths = new Dictionary<string, IList>();
                checkedPaths.Add(currentNode.Id, paths);

                Dictionary<int, List<string>> foundPath = this.PathManager.SearchPathFromVehicleToDest(pathMap, linksMap, currentNode.Id, checkedPaths, 0);

                if (foundPath != null && foundPath.Count > 0)
                {
                    //			if (foundPath != null) {
                    KeyValuePair<int, List<string>> entry = foundPath.FirstOrDefault();

                    string path = this.PathManager.ListToString(entry.Value);
                    int totalTime = entry.Key * 60 / 2500;
                    vhc.Path = path + ":" + totalTime;
                    // this.resourceManager.updateVehicle(vhc, "path", path + ":" +
                    // totalTime);

                    this.UpdateVehiclePath(vhc, path + ":" + totalTime);

                    vehicleMessage.Vehicle = vhc;

                    logger.Info(path + " - need total time: " + totalTime + " seconds");

                    OrderPairNodeEx orderPair = this.ResourceManager.SearchNextOrderNode(path + ":" + totalTime);
                    if (orderPair != null)
                    {
                        logger.Info("Find order node: " + orderPair.OrderGroup);
                        VehicleOrderEx vehicleOrder = this.ResourceManager.GetVehicleOrderByVehicleId(vehicleMessage.VehicleId);
                        if (vehicleOrder != null)
                        {
                            if (!vehicleOrder.OrderNode.Equals(orderPair.OrderGroup))
                            {
                                VehicleMessageEx newVehicleMessage = new VehicleMessageEx();
                                newVehicleMessage.Vehicle = vhc;
                                newVehicleMessage.VehicleId = vhc.Id;
                                newVehicleMessage.NodeId = orderPair.OrderGroup;
                                newVehicleMessage.KeyData = orderPair.Status;

                                vehicleOrder.OrderNode = orderPair.OrderGroup;
                                vehicleOrder.OrderTime = new DateTime();

                                this.ResourceManager.UpdateVehicleOrder(vehicleOrder);
                                return newVehicleMessage;
                            }
                        }
                        else
                        {
                            VehicleMessageEx newVehicleMessage = new VehicleMessageEx();
                            newVehicleMessage.Vehicle = vhc;
                            newVehicleMessage.VehicleId = vhc.Id;
                            newVehicleMessage.NodeId = orderPair.OrderGroup;
                            newVehicleMessage.KeyData = orderPair.Status;

                            vehicleOrder = new VehicleOrderEx();

                            vehicleOrder.OrderNode = orderPair.OrderGroup;
                            vehicleOrder.VehicleId = vehicleMessage.VehicleId;
                            vehicleOrder.OrderTime = DateTime.Now;
                            this.ResourceManager.CreateVehicleOrder(vehicleOrder);
                            return newVehicleMessage;
                        }
                    }
                    else
                    {
                        VehicleOrderEx vehicleOrder = this.ResourceManager.GetVehicleOrderByVehicleId(vhc.Id);
                        if (vehicleOrder != null)
                        {
                            this.ResourceManager.DeleteVehicleOrderByVehicleID(vhc.Id);
                            VehicleMessageEx newVehicleMessage = vehicleMessage;
                            newVehicleMessage.VehicleId = vhc.Id;
                            newVehicleMessage.Vehicle = vhc;
                            newVehicleMessage.NodeId = "0000,0000,0000";
                            newVehicleMessage.KeyData = "ORDER_LF";
                            return newVehicleMessage;
                        }
                        return null;
                    }
                }
                else
                {
                    logger.Info("Can not find way to come back -.-!");
                }
            }
            return null;
        }
    }
}