using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Manager.History;
using ACS.Manager.Resource;
using ACS.Framework.Base;
using ACS.Framework.Application.Model;
using ACS.Framework.Message.Model.Server;
using ACS.Framework.Resource.Model.Factory.Unit;
using ACS.Framework.Resource;
using ACS.Framework.Material;
using ACS.Framework.History;
using ACS.Framework.History.Model;
using ACS.Framework.Alarm.Model;
using ACS.Framework.Transfer.Model;
using ACS.Framework.Message.Model;
using ACS.Framework.Resource.Model;
using ACS.Framework.Resource.Model.Factory.Machine;
using NHibernate.Mapping;
using NHibernate.Criterion;
using NHibernate;
using System.Collections;
using ACS.Extension.Framework.Resource;
using ACS.Extension.Framework.History;
using ACS.Extension.Framework.History.Model;
using ACS.Framework.Path.Model;
using ACS.Extension.Framework.Cache;
using ACS.Extension.Framework.Path;

namespace ACS.Extension.Manager
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
            vhs.VehicleId = vehicle.Id;
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
