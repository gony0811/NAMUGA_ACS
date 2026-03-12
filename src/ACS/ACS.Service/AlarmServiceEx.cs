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
using log4net;
using ACS.Extension.Framework.Base;
using ACS.Extension.Framework.History.Model;
using ACS.Framework.Path.Model;

namespace ACS.Service
{
    public class AlarmServiceEx : AbstractServiceEx
    {
        public static String ALARMCODE_LOWBATERRY = "280";
        public static String ALARMCODE_BUMPERERROR = "205";
        public static String ALARMCODE_FRONTSENSOR = "281";
        public static String ALARMCODE_REMOTESTOP = "285";
       
        public ILog logger = log4net.LogManager.GetLogger(typeof(AlarmServiceEx)); 
        public AlarmServiceEx()
            : base()
        {

        }
        public override void Init() 
        {
           base.Init();
        }
        public bool CreateAlarm(VehicleMessageEx vehicleMessage)
        {

            String errorId = vehicleMessage.ErrorId;
            //String errorCode = "128"; 
            String errorCode = "0000";
            String errorLevel = "";
            String errorText = "";
            String transportCommandId = "";


            VehicleEx vehicle = vehicleMessage.Vehicle;

            if (vehicle != null)
            {
                errorCode = vehicle.CurrentNodeId;

                if (vehicle.TransportCommandId != null)
                {
                    transportCommandId = vehicle.TransportCommandId;
                }
            }
            /*********************************/
            //20200626 SJP 동일 알람 보고가 연속적으로 들어올 때 이력 생성 Skip
            //이전 알람은 1개만 관리하므로,,, Agv Id & ErroId로 조회된 항목 삭제 후
            //이전 알람과 현재 보고 된 알람 비교 후 같으면 종료(이력 생성 Skip 목적)

            AlarmEx oldAlarm = this.AlarmManager.GetAlarmByVehicleIdAndAlarmId(vehicleMessage.VehicleId, errorId);

            if (oldAlarm != null)
            {
                return false;
            }
            else
            {
                //IList oldAlarms = this.AlarmManager.GetAlarmsByVehicleId(vehicleMessage.VehicleId);
                //if (oldAlarms.Count > 0)
                //{
                this.AlarmManager.DeleteAlarmByVehicleId(vehicleMessage.VehicleId);
                //}
            }
            /*********************************/

            //for (IEnumerator iterator = oldAlarms.GetEnumerator(); iterator.MoveNext(); )
            //{

            //    AlarmEx oldAlarm = (AlarmEx)iterator.Current;
            //    logger.Info("deleted alarm , " + oldAlarm);
            //}
            //if (oldAlarms.Count > 0)
            //{

            //    this.AlarmManager.DeleteAlarmByVehicleId(vehicleMessage.VehicleId);
            //}

            AlarmSpecEx alarmSpec = new AlarmSpecEx();
            if (this.AlarmManager.GetAlarmSpecByAlarmId(errorId) == null)
            {
                //alarmSpec.setAlarmId(errorId);
                alarmSpec.AlarmId = errorId;
                alarmSpec.Severity = "HEAVY";
                alarmSpec.AlarmText = "No defined alarm";
                this.AlarmManager.CreateAlarmSpec(alarmSpec);

            }

            alarmSpec = this.AlarmManager.GetAlarmSpecByAlarmId(errorId);

            errorLevel = alarmSpec.Severity;
            errorText = alarmSpec.AlarmText;

            logger.Info("errorId = " + errorId + " errorCode = " + errorCode + " errorText = " + errorText);
            vehicleMessage.ErrorId=errorId;
            vehicleMessage.ErrorCode=errorCode;
            vehicleMessage.ErrorLevel=errorLevel;
            vehicleMessage.ErrorText=errorText;

            AlarmEx alarm = new AlarmEx();
            //		alarm.setId(IdGeneratorUtils.randomNumeric(10)); // 20170801 keyhan deleted

            alarm.AlarmCode = vehicleMessage.ErrorCode;
            alarm.AlarmText = vehicleMessage.ErrorText;
            alarm.VehicleId = vehicleMessage.VehicleId;
            alarm.AlarmId = vehicleMessage.ErrorId;
            alarm.NearAgv = IsNearAgvExist(vehicle.CurrentNodeId);
            alarm.IsCross = IsCrossNode(vehicle.CurrentNodeId);
            alarm.CreateTime = DateTime.Now;
            
            if (transportCommandId != null && !string.IsNullOrEmpty(transportCommandId))
            {
                alarm.TransportCommandId = transportCommandId;
            }

            this.AlarmManager.CreateAlarm(alarm);
            return true;

        }

        public bool CheckAlarmExist(VehicleMessageEx vehicleMessage)
        {

            AlarmEx alarm = this.AlarmManager.GetAlarmByVehicleId(vehicleMessage.VehicleId);

            if (alarm != null)
            {
                vehicleMessage.ErrorId = alarm.AlarmId;
                vehicleMessage.ErrorText=alarm.AlarmText;
                vehicleMessage.ErrorCode=alarm.AlarmCode;
                return true;
            }
            else
                return false;
        }

        public bool CheckHeavyAlarm(VehicleMessageEx vehicleMessage)
        {
            //AlarmACS alarm = this.AlarmManager.GetAlarmByVehicleId(vehicleMessage.getVehicleId());

            AlarmSpecEx alarmSpec = this.AlarmManager.GetAlarmSpecByAlarmId(vehicleMessage.ErrorId);
            if (alarmSpec != null)
            {
                if (VehicleMessageEx.ALARM_SEVERITY_HEAVY.Equals(alarmSpec.Severity, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Info("AlarmCode " + alarmSpec.AlarmId + " is " + alarmSpec.Severity);
                    return true;
                }
                else
                    return false;

            }
            else
                return false;
        }

        public bool CheckAlarmClearMessage(VehicleMessageEx vehicleMessage)
        {

            bool result = true;
            String messageName = vehicleMessage.MessageName;

            if (messageName.Equals("RAIL-ALARMREPORT"))
            {
                return false;
            }

            return result;
        }

        public int DeleteAlarmByVehicle(VehicleMessageEx vehicleMessage)
        {

            return this.AlarmManager.DeleteAlarmByVehicleId(vehicleMessage.VehicleId);
        }


        public bool CheckVehicleHaveAlarm(VehicleMessageEx vehicleMessage)
        {
            AlarmEx alarm = this.AlarmManager.GetAlarmByVehicleId(vehicleMessage.VehicleId);
            VehicleEx vehicle = vehicleMessage.Vehicle;
            if ((alarm != null) || (vehicle.AlarmState.Equals("ALARM")))
            {
                return true;
            }
            return false;
        }

        private string IsCrossNode(string nodeId)
        {
            string result = "N";

            if(!string.IsNullOrEmpty(nodeId))
            {
                NodeEx node = ResourceManager.GetNode(nodeId);

                if(node != null && (node.Type.Equals("CROSS_S") || node.Type.Equals("CROSS_E")))
                {
                    result = "Y";
                }
                else
                {
                    result = "N";
                }
            }

            return result;
        }

        private string IsNearAgvExist(string nodeId)
        {
            string result = "N";

            if (!string.IsNullOrEmpty(nodeId))
            {
                IList linkList = ResourceManager.GetLinks();
                
                for (IEnumerator iterator = linkList.GetEnumerator(); iterator.MoveNext();)
                {
                    LinkEx link = iterator.Current as LinkEx;

                    if(link.FromNodeId.Equals(nodeId))
                    {
                        IList vehicles = ResourceManager.GetVehiclesByCurrentNode(link.ToNodeId);
                        if(vehicles != null && vehicles.Count > 0)
                        {
                            result = "Y";
                            break;
                        }                  
                        else
                        {
                            continue;
                        }
                    }
                    else if(link.ToNodeId.Equals(nodeId))
                    {
                        IList vehicles = ResourceManager.GetVehiclesByCurrentNode(link.FromNodeId);
                        if (vehicles != null && vehicles.Count > 0)
                        {
                            result = "Y";
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            return result;
        }

        public void ClearAlarmAndSetAlarmTimeHistory(VehicleMessageEx vehicleMessage)
        {
            String vehicleId = vehicleMessage.VehicleId;
            VehicleEx vehicle = vehicleMessage.Vehicle;

            if (vehicle == null)
            {
                vehicle = ResourceManager.GetVehicle(vehicleId);
            }

            string bayId = vehicle.BayId;
            string currentNodeId = vehicle.CurrentNodeId;
            bool needUpdateAlarm = true;

            IList alarms = this.AlarmManager.GetAlarmsByVehicleId(vehicleId);          

            if (alarms != null && alarms.Count > 0)
            {
                for (var iterator = alarms.GetEnumerator(); iterator.MoveNext();)
                {
                    AlarmEx alarm = (AlarmEx)iterator.Current;

                    if (ALARMCODE_LOWBATERRY.Equals(alarm.AlarmId))
                    {
                        needUpdateAlarm = false;
                        continue;
                    }

                    AlarmTimeHistoryEx alarmTimeHistory = new AlarmTimeHistoryEx();

                    alarmTimeHistory.AlarmId = alarm.AlarmId;
                    alarmTimeHistory.AlarmCode = alarm.AlarmCode;
                    alarmTimeHistory.AlarmText = alarm.AlarmText;
                    alarmTimeHistory.CreateTime = alarm.CreateTime;
                    alarmTimeHistory.ClearTime = DateTime.Now;
                    alarmTimeHistory.Time = DateTime.Now;
                    alarmTimeHistory.TransportCommandId = alarm.TransportCommandId;
                    alarmTimeHistory.VehicleId = alarm.VehicleId;
                    alarmTimeHistory.NearAgv = alarm.NearAgv;
                    alarmTimeHistory.IsCross = alarm.IsCross;
                    alarmTimeHistory.BayId = vehicle.BayId;


                    HistoryManager.CreateAlarmTimeHistory(alarmTimeHistory);
                    alarm.AlarmCode = "0";
                    HistoryManager.CreateAlarmReportHistory(alarm, "CLEAR");
                    AlarmManager.DeleteAlarm(alarm);
                }

                if (needUpdateAlarm)
                {
                    this.ResourceManager.UpdateVehicleAlarmState(vehicleMessage.VehicleId, VehicleEx.ALARMSTATE_NOALARM, vehicleMessage.MessageName);
                }
            }        
        }

        public bool ClearLowBatteryAlarm(VehicleMessageEx vehicleMessage)
        {
            VehicleEx vehicle = this.ResourceManager.GetVehicle(vehicleMessage.VehicleId);
            AlarmEx lowBatteryAlarm = this.AlarmManager.GetAlarmByVehicleIdAndAlarmId(vehicleMessage.VehicleId, "280");
            float vehicleVoltage = vehicleMessage.BatteryVoltage;
            if (lowBatteryAlarm != null)
            {
                //V3 : vehicleVoltage >= 24.5D)
                if (vehicleVoltage >= 23.5D)
                {
                    this.AlarmManager.DeleteAlarm(lowBatteryAlarm);
                    logger.Info("deleted lowbattery alarm, " + lowBatteryAlarm.ToString());

                    AlarmEx alarm = this.AlarmManager.GetAlarmByVehicleId(vehicleMessage.VehicleId);
                    if (alarm == null)
                    {
                        this.ResourceManager.UpdateVehicleAlarmState(vehicleMessage.VehicleId, "NOALARM", vehicleMessage.MessageName);
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
