using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Manager.History;
using ACS.Manager.Resource;
using ACS.Core.Base;
using ACS.Core.Application.Model;
using ACS.Core.Message.Model.Server;
using ACS.Core.Resource.Model.Factory.Unit;
using ACS.Core.Resource;
using ACS.Core.Material;
using ACS.Core.History;
using ACS.Core.History.Model;
using ACS.Core.Alarm.Model;
using ACS.Core.Transfer.Model;
using ACS.Core.Message.Model;
using ACS.Core.Resource.Model;
using ACS.Core.Resource.Model.Factory.Machine;
using System.Collections;
using ACS.Core.Path.Model;
using ACS.Core.Cache;
using ACS.Core.Path;

namespace ACS.Manager
{
    public class HistoryManagerExsImplement : HistoryManagerExImplement, IHistoryManagerExs
    {
	
        public ICacheManagerEx CacheManager { get; set; }
        public IPathManagerExs PathManager { get; set; }

        public IResourceManagerExs ResourceManager { get; set; }

        public void CreateVehicleCrossHistory(string vehicleId, string inNode, DateTime inDate, string endNode)
        {
            VehicleCrossHistory vCross = new VehicleCrossHistory();
            vCross.VehicleId = vehicleId;
            vCross.StartNodeId = inNode;
            vCross.StartTime = inDate.ToString();
            vCross.EndNodeId = endNode;
            vCross.EndTime = DateTime.Now.ToString();

            this.PersistentDao.Save(vCross);

        }


        public void CreateVehicleSearchPathHistory(VehicleSearchPathHistory vehicleSearchPath)
        {
            this.PersistentDao.Save(vehicleSearchPath);

        }
        
        public VehicleCrossWaitHistoryEx CreateVehicleCrossWaitHistory(VehicleCrossWaitEx vehicleCrossWait, DateTime permitTime)
        {
            VehicleCrossWaitHistoryEx crossWaitHistory = new VehicleCrossWaitHistoryEx();
            
            crossWaitHistory.VehicleId = vehicleCrossWait.VehicleId;

            crossWaitHistory.CreateTime = vehicleCrossWait.CreatedTime;
            crossWaitHistory.PermitTime = permitTime;
            crossWaitHistory.Time = DateTime.Now;
            crossWaitHistory.State = vehicleCrossWait.State;
            crossWaitHistory.NodeId = vehicleCrossWait.NodeId;
            crossWaitHistory.CrossWaitSeconds = (permitTime - vehicleCrossWait.CreatedTime).Seconds;

            CreateVehicleCrossWaitHistory(crossWaitHistory);
            return crossWaitHistory;
        }

        public VehicleCrossWaitHistoryEx CreateVehicleCrossWaitHistory(VehicleMessageEx vehicleMessage, DateTime permitTime)
        {
            var vehicleCrossWait = ResourceManager.GetVehicleCrossWait(vehicleMessage.VehicleId);

            if(vehicleCrossWait == null)
            {
                return null;
            }

            VehicleCrossWaitHistoryEx crossWaitHistory = new VehicleCrossWaitHistoryEx();

            crossWaitHistory.VehicleId = vehicleCrossWait.VehicleId;

            crossWaitHistory.CreateTime = vehicleCrossWait.CreatedTime;
            crossWaitHistory.PermitTime = permitTime;
            crossWaitHistory.Time = DateTime.Now;
            crossWaitHistory.State = vehicleCrossWait.State;
            crossWaitHistory.NodeId = vehicleCrossWait.NodeId;
            crossWaitHistory.CrossWaitSeconds = (int)(permitTime - vehicleCrossWait.CreatedTime).TotalSeconds;

            CreateVehicleCrossWaitHistory(crossWaitHistory);
            return crossWaitHistory;
        }

        public void CreateVehicleCrossWaitHistory(VehicleCrossWaitHistoryEx vehicleCrossWaitHistory)
        {
            PersistentDao.Save(vehicleCrossWaitHistory);
        }

        public MismatchAndFlyHistoryEx CreateMismatchAndFlyHistory(string vehicleId, string currentNodeId, string nodeNg, string mismatchOrFly)
        {
            MismatchAndFlyHistoryEx mismatchAndFlyHistory = new MismatchAndFlyHistoryEx()
            {
                VehicleId = vehicleId,
                CurrentNodeId = currentNodeId,
                NgNode = nodeNg,
                Type = mismatchOrFly,
                Time = DateTime.Now
            };

            CreateMismatchAndFlyHistory(mismatchAndFlyHistory);

            return mismatchAndFlyHistory;
        }

        public void CreateMismatchAndFlyHistory(MismatchAndFlyHistoryEx mismatchAndFlyHistory)
        {
            PersistentDao.Save(mismatchAndFlyHistory);
        }

        public void CreateAlarmTimeHistory(AlarmTimeHistoryEx alarmTimetHistory)
        {
            PersistentDao.Save(alarmTimetHistory);
        }

        public void CreateNioHistory(NioHistory nioHistory)
        {
            PersistentDao.Save(nioHistory);
        }

        public void AddVehiclePathHistory(VehicleEx vehicle, string path, string type, int distance)
        {
            VehicleSearchPathHistory vhs = new VehicleSearchPathHistory();
            vhs.VehicleId = vehicle.VehicleId;
            vhs.BayId = vehicle.BayId;
            vhs.Path = path;
            vhs.CurrentNodeId = vehicle.CurrentNodeId;
            vhs.TrCmd = vehicle.TransportCommandId;
            vhs.Type = type;

            if (distance == 0)
            {
                string[] pathArr = path.Split(',');
                for (int i = 0; i < pathArr.Length - 1; i++)
                {
                    LinkEx link = this.CacheManager.GetLinkById(pathArr[i] + "_" + pathArr[i + 1]);
                    if (link != null)
                    {
                        distance += link.Length;
                        if (i > 0)
                        {
                            NodeEx lastNode = this.CacheManager.GetNode(pathArr[i - 1]);
                            NodeEx currentNode = this.CacheManager.GetNode(pathArr[i]);
                            NodeEx nextNode = this.CacheManager.GetNode(pathArr[i + 1]);
                            if (lastNode != null && currentNode != null && nextNode != null)
                            {
                                if (lastNode.Xpos == currentNode.Xpos && currentNode.Xpos == nextNode.Xpos || lastNode.Ypos == currentNode.Ypos && currentNode.Ypos == nextNode.Ypos)
                                {

                                }
                                else
                                {
                                    int turnAngle = this.PathManager.CalculateAngle3Point(lastNode, currentNode, nextNode);
                                    distance += turnAngle * (int)3.3;
                                }
                            }
                        }

                    }
                }
            }

            vhs.Distance = distance;
            vhs.TransferState = vehicle.TransferState; ;
            vhs.Time = DateTime.Now;

            this.CreateVehicleSearchPathHistory(vhs);
        }

    }
}
