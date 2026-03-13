using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;
using ACS.Framework.Alarm.Model;
using ACS.Framework.Message.Model;
using ACS.Framework.Resource.Model;
using System.Collections;
using ACS.Framework.Message.Model.Ui;
using ACS.Framework.History.Model;
using ACS.Framework.Logging;
using ACS.Framework.Base;
using ACS.Communication.Socket.Model;
using ACS.Framework.History.Model;
using ACS.Framework.Application;
using System.Configuration;

namespace ACS.Service
{
    public class HistoryServiceEx : AbstractServiceEx
    {
        public Logger logger = Logger.GetLogger(typeof(HistoryServiceEx));

        public void CreateNioHistory(Nio nio)
        {
            if (nio == null) return;

            string currentNode = (ResourceManager.GetVehicle(nio.Name)).CurrentNodeId;

            NioHistory nioHistory = new NioHistory()
            {
                ApplicationName = nio.ApplicationName,
                MachineName = nio.MachineName,
                Location = currentNode,
                Name = nio.Name,
                State = nio.State,
                RemoteIp = nio.RemoteIp,
                Port = nio.Port,
                Time = DateTime.Now
            };

            HistoryManager.CreateNioHistory(nioHistory);
        }

        public void CreateVehicleCrossWaitHistory(VehicleMessageEx vehicleMessage)
        {
            HistoryManager.CreateVehicleCrossWaitHistory(vehicleMessage, DateTime.Now);
        }

        public void TruncatePartitionTable(UiTruncateMessageEx uiTruncateMessage)
        {

            String tableName = uiTruncateMessage.TableName;

            if (string.IsNullOrEmpty(tableName))
            {

                logger.Error("tableName does not exist in message, " + uiTruncateMessage);
            }
            else
            {

                TruncateParameterEx truncateParameter = this.HistoryManager.GetTruncateParameter(uiTruncateMessage.TableName);

                if (truncateParameter == null)
                {
                    logger.Error("truncateParameter does not exist in repository, tableName{" + tableName + "}");
                }
                else
                {

                    String partitionId = uiTruncateMessage.PartitionId;
                    String replcedSql = this.HistoryManager.GetNativeSqlWithReplace(truncateParameter.NativeSql, partitionId);
                    this.HistoryManager.ExcuteTruncateQuery(replcedSql);

                }
            }
        }
    }
}
