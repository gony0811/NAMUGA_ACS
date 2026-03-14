using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.Threading;
using ACS.Communication.Msb;
using ACS.Framework.Application;
using ACS.Framework.Application.Model;
using ACS.Framework.Base;
using ACS.Framework.History;
using ACS.Framework.Message.Model.Control;
using ACS.Framework.Message;
using ACS.Framework.Scheduling;
using ACS.Control.Scheduling;
using ACS.Utility;
using Quartz;
using System.Configuration;
using System.Diagnostics;
using ACS.Framework.Transfer;
using ACS.Workflow;

namespace ACS.Control
{
    public class ControlServerManagerImplement : AbstractManager, IControlServerManager
    {

        public static String SCRIPT_TS_START = "TS-START";
        public static String SCRIPT_TS_KILL = "TS-KILL";
        public static String SCRIPT_ES_START = "ES-START";
        public static String SCRIPT_ES_KILL = "ES-KILL";
        public static String SCRIPT_DS_START = "DS-START";
        public static String SCRIPT_DS_KILL = "DS-KILL";
        public static String SCRIPT_MS_START = "MS-START";
        public static String SCRIPT_MS_KILL = "MS-KILL";
        public static String SCRIPT_RS_START = "RS-START";
        public static String SCRIPT_RS_KILL = "RS-KILL";
        public static String SCRIPT_QS_START = "QS-START";
        public static String SCRIPT_QS_KILL = "QS-KILL";
        public static String SCRIPT_COREDUMP = "COREDUMP";
        public static String SCRIPT_SYSTEMCHECK = "SYSTEMCHECK";
        public static String SCRIPT_GETPROCESSID = "GETPROCESSID";
        public static String OS_TYPE_WINDOWS = "Windows";
        public static String OS_TYPE_LINUX = "Linux";
        public static String OS_TYPE_UNIX = "Unix";

        public IDictionary Scripts { get; set; }
        [Obsolete("startup.xml 제거됨 — Scripts 딕셔너리와 ApplicationManager로 대체")]
        public string StartConfigurationFilePath { get; set; }
        public IApplicationManager ApplicationManager { get; set; }
        public ITransferManagerEx TransferManager { get; set; }
        public IMessageManagerEx MessageManager { get; set; }
        public ISynchronousMessageAgent SynchronousMessageAgent { get; set; }
        public ISchedulingManager SchedulingManager { get; set; }
        public IHistoryManagerEx HistoryManager { get; set; }
        public IMessageAgent MessageAgent { get; set; }
        public IWorkflowManager WorkflowManager { get; set; }
        public long HeartBeatInterval { get; set; }
        public long HeartBeatStartDelay { get; set; }
        public long HeartBeatTimeout { get; set; }
        public bool UseHeartBeat { get; set; }
        public bool UseUiTransport { get; set; }
        public bool UseUiApplicationManager { get; set; }
        public long SimpleHeartBeatInterval { get; set; }
        public long SimpleHeartBeatStartDelay { get; set; }
        public int HeartBeatRetryCount { get; set; }
        public long HeartBeatRetryTimeout { get; set; }
        public int HeartBeatFailWhenProcessDown { get; set; }
        public int HeartBeatFailWhenProcessHang { get; set; }
        public long RescheduleHeartBeatInterval { get { return HeartBeatInterval; } set { HeartBeatInterval = value; } }
        public long RescheduleHeartBeatStartDelay { get { return HeartBeatStartDelay; } set { HeartBeatStartDelay = value; } }
        public bool UseSystemKill { get; set; }
        public bool UseSystemGetProcessId { get; set; }
        public bool UseSecondAsTimeUnit { get; set; }
        protected Type JobType { get; set; }
        protected Type HeartBeatJobType { get; set; }
        protected Type SimpleHeartBeatJobType { get; set; }
        protected Type RescheduleHeartBeatJobType { get; set; }
        protected string DestinationNamePrefix { get; set; }
        protected string WindowRedirectFilePath { get; set; }

        protected Type UiTransportJobType { get; set; }
        protected Type UiApplicationManagerJobType { get; set; }
        public long UiTransportInterval { get; set; }
        public long UiTransportStartDelay { get; set; }
        public long UiApplicationManagerInterval { get; set; }
        public long UiApplicationManagerStartDelay { get; set; }

        //200622 Change NIO Logic About ES.exe does not restart
        protected Type UiCommandJobType { get; set; }
        public bool UseUiCommand { get; set; }
        public long UiCommandInterval { get; set; }
        //

        public override void Init()
        {
            base.Init();

            HeartBeatInterval = 20000L;
            HeartBeatStartDelay = 10000L;
            UseHeartBeat = true;
            UseUiTransport = true;
            UseUiApplicationManager = true;
            SimpleHeartBeatInterval = 5000L;
            SimpleHeartBeatStartDelay = 2000L;
            HeartBeatRetryCount = 3;
            HeartBeatRetryTimeout = 10000L;
            HeartBeatFailWhenProcessDown = 0;
            HeartBeatFailWhenProcessHang = 0;
            RescheduleHeartBeatInterval = this.HeartBeatInterval;
            RescheduleHeartBeatStartDelay = this.HeartBeatStartDelay;
            UseSystemKill = false;
            UseSystemGetProcessId = false;

            UseSecondAsTimeUnit = false;
            UiTransportInterval = 20000L;
            UiTransportStartDelay = 10000L;
            UiApplicationManagerInterval = 20000L;
            UiApplicationManagerStartDelay = 10000L;

            HeartBeatJobType = typeof(HeartBeatJob);
            SimpleHeartBeatJobType = typeof(SimpleHeartBeatJob);
            RescheduleHeartBeatJobType = typeof(RescheduleHeartBeatJob);
            UiTransportJobType = typeof(UiTransportJob);
            UiApplicationManagerJobType = typeof(UiApplicationManagerJob);

            //200622 Change NIO Logic About ES.exe does not restart
            UiCommandJobType = typeof(UiCommandJob);
            //
        }



        public bool Control(ControlMessage controlMessage)
        {
            bool controlByControlServer = false;

            string messageName = controlMessage.MessageName;
            string applicationType = controlMessage.ApplicationType;

            if (!applicationType.Equals("control"))
            {
                if (messageName.Equals("CONTROL-START"))
                {
                    Start(controlMessage);
                    controlByControlServer = true;
                }
                else if (messageName.Equals("CONTROL-KILL"))
                {
                    Kill(controlMessage);
                    controlByControlServer = true;
                }
                else if (messageName.Equals("CONTROL-GETPROCESSID"))
                {
                    controlByControlServer = true;
                }
                else if (messageName.Equals("CONTROL-SYSTEMCHECK"))
                {
                    //SystemCheck(controlMessage);
                }
                else if (messageName.Equals("CONTROL-COREDUMP"))
                {
                    ExecuteCoreDump(controlMessage);
                    controlByControlServer = true;
                }
                else
                {
                    logger.Info("messageName{" + messageName + "} can not be accepted, it will be sent to each server");
                }
            }

            return controlByControlServer;

        }


        public void DisplayTriggers()
        {
            this.SchedulingManager.DisplayTriggers();
        }

        public bool ExecuteCoreDump(ControlMessage controlMessage)
        {
            //logger.info("executeCoreDump asked");
            string applicationName = controlMessage.ApplicationName;
            string applicationType = controlMessage.ApplicationType;

            return ExecuteCoreDump(applicationName, applicationType);
        }

        public bool ExecuteCoreDump(string applicationName, string applicationType)
        {
            bool result = false;

            String script = (String)this.Scripts["COREDUMP"];

            //logger.info(script);

            result = true;
            if (result)
            {
                List<string> lines = SystemUtility.PerformCommand(new String[] { script, applicationName }, 2);
                //logger.info(lines);
            }
            else
            {
                //logger.error("failed to executeCoreDump");
            }

            return result;
        }

        public IApplicationManager GetApplicationManager()
        {
            throw new NotImplementedException();
        }

        public string GetDestinationName(string applicationName)
        {
            string destinationName = DestinationNamePrefix + "." + applicationName;
            ACS.Framework.Application.Model.Application application = this.ApplicationManager.GetApplication(applicationName);

            if(application != null)
            {
                if(application.Msb.Equals("highway101"))
                {
                    destinationName = destinationName.Replace(".", "/");
                    if (!destinationName.StartsWith("/"))
                    {
                        destinationName = "/" + destinationName;
                    }
                    return destinationName;
                }
            }

            return destinationName;
        }


        public string GetProcessId(ControlMessage controlMessage)
        {
            //logger.info("GetProcessId asked");

            string applicationName = controlMessage.ApplicationName;

            return GetProcessId(applicationName);
        }

        public string GetProcessId(string applicationName)
        {
            string processId = "";

            if(this.UseSystemGetProcessId)
            {
                processId = SystemUtility.GetProcessId(applicationName);
            }
            else
            {
                string script = (string)this.Scripts["GETPROCESSID"];
                //logger.info(script);

                try
                {
                    List<string> lines = SystemUtility.PerformCommand(new string[] { script, applicationName }, 2);

                    if(lines.Count > 0)
                    {
                        processId = lines[0];
                    }
                }
                catch(PerformCommandException pce)
                {
                    if (pce.ExitValue == 1)
                    {
                        //logger.error("failed to perform command, arguments are not correct, " + pce.Message);
                    }
                    else if(pce.ExitValue == 2)
                    {
                        //logger.error("failed to perform command, arguments are not correct, " + pce.Message);
                    }
                    else if(pce.ExitValue == 4)
                    {
                        //logger.error("failed to perform command, already killed, " + pce.Message);
                    }
                    else
                    {
                        //logger.error("ExitCode{" + pce.ExitValue :}, " + pce.Message);
                    }
                }
                catch (Exception e)
                {
                    // logger.error(e.Message);
                    throw;
                }
            }

            return processId;
        }

        public bool Kill(ControlMessage controlMessage)
        {
            //logger.info("kill asked");

            string applicationName = controlMessage.ApplicationName;
            string applicationType = controlMessage.ApplicationType;

            return Kill(applicationName, applicationType);
        }

        public bool Kill(string applicationName, string applicationType)
        {
            bool result = false;

            try
            {
                if(UseSystemKill)
                {
                    result = SystemUtility.KillProcess(applicationName);
                }
                else
                {
                    string script = GetKillScript(applicationType);
                    //logger.info(script);

                    List<string> lines = SystemUtility.PerformCommand(new string[] { script, applicationName }, 2);
                    //logger.info(lines);

                    result = true;
                }

                if(result)
                {
                    this.ApplicationManager.UpdateApplicationState(applicationName, "inactive");
                    UnscheduleHeartBeat(applicationName);
                }
                else
                {
                    //logger.error("failed to kill");
                }
            }
            catch (PerformCommandException pce)
            {
                if (pce.ExitValue == 1)
                {
                    //logger.error("failed to perform command, arguments are not correct, " + pce.Message);
                }
                else if (pce.ExitValue == 2)
                {
                    //logger.error("failed to perform command, arguments are not correct, " + pce.Message);
                }
                else if (pce.ExitValue == 4)
                {
                    //logger.fine("failed to perform command, already killed, " + pce.Message);
                    this.ApplicationManager.UpdateApplicationState(applicationName, "inactive");
                    UnscheduleHeartBeat(applicationName);
                }
                else
                {
                    //logger.error("ExitCode{" + pce.ExitValue :}, " + pce.Message);

                }
            }

            return result;
        }

        private string GetKillScript(string applicationType)
        {
            string script = "";

            if (applicationType.Equals("trans"))
            {
                script = (string)this.Scripts[SCRIPT_TS_KILL];
            }
            else if(applicationType.Equals("ei"))
            {
                script = (string)this.Scripts[SCRIPT_ES_KILL];
            }
            else if(applicationType.Equals("daemon"))
            {
                script = (string)this.Scripts[SCRIPT_DS_KILL];
            }
            else if(applicationType.Equals("emulator"))
            {
                script = (string)this.Scripts[SCRIPT_MS_KILL];
            }
            else if(applicationType.Equals("report"))
            {
                script = (string)this.Scripts[SCRIPT_RS_KILL];
            }
            else if(applicationType.Equals("query"))
            {
                script = (string)this.Scripts[SCRIPT_QS_KILL];
            }
            else
            {
                // logger.error("failed to get kill script, please check applicationType{" + applicationType + "}");
            }

            return script;
        }

        public void PauseHeartBeat(string applicationName)
        {
            ITrigger trigger = this.SchedulingManager.GetTrigger(applicationName, HeartBeatJob.GROUP_HEARTBEAT);

            if(trigger != null)
            {
                //logger.info("heartBeat{" + applicationName + "} will be paused");
                this.SchedulingManager.PauseTrigger(applicationName, HeartBeatJob.GROUP_HEARTBEAT);
            }

            DisplayTriggers();
        }

        public void RescheduleHeartBeats()
        {
            if (this.UseHeartBeat)
            {
                //logger.fine("rescheduleHeartBeat functionality will be running");
                string hardwareType = ConfigurationManager.AppSettings[Settings.SYSTEM_ENV_KEY_HARDWARE_TYPE];
                if (string.IsNullOrEmpty(hardwareType))
                {
                    //logger.fine("hardwareType is not designated, {PRIMARY} will be applied");
                    hardwareType = "PRIMARY";
                }
                IList applicationNames = this.ApplicationManager.GetApplicationNamesByRunningHardware(hardwareType, true, true);
                if (applicationNames.Count == 0)
                {
                    //logger.info("there is no application running at " + hardwareType);
                    return;
                }
                foreach (object obj in applicationNames)
                {
                    String applicationName = (string)obj;
                    if (!SchedulingHeartBeat(applicationName))
                    {
                        ScheduleHeartBeat(applicationName);
                    }
                }
            }
            else
            {
                //logger.fine("heartBeat functionality is not used");
            }
        }

        public bool SchedulingHeartBeat(string applicationName)
        {
            ITrigger trigger = this.SchedulingManager.GetTrigger(applicationName, HeartBeatJob.GROUP_HEARTBEAT);
            if (trigger != null)
            {
                return true;
            }
            return false;
        }

        public void ResumeHeartBeat(string applicationName)
        {
            ITrigger trigger = this.SchedulingManager.GetTrigger(applicationName, HeartBeatJob.GROUP_HEARTBEAT);

            if(trigger != null)
            {
                //logger.info("heartBeat{" + applicationName + "} will be resumed");
                this.SchedulingManager.ResumeTrigger(applicationName, HeartBeatJob.GROUP_HEARTBEAT);
            }

            DisplayTriggers();
        }

        public bool ScheduleHeartBeat(string applicationName)
        {
            try
            {
                if(this.UseHeartBeat)
                {
                    ITrigger trigger = this.SchedulingManager.GetTrigger(applicationName, HeartBeatJob.GROUP_HEARTBEAT);

                    if (trigger != null)
                    {
                        this.SchedulingManager.DeleteJob(applicationName, HeartBeatJob.GROUP_HEARTBEAT);
                    }

                    IJobDetail jobDetail = CreateHeartBeatJobDetail(applicationName);

                    ITrigger simpleTrigger = CreateHeartBeatTrigger(applicationName, jobDetail);

                    //logger.info("heartBeat{" + applicationName + "} will be scheduled");

                    this.SchedulingManager.ScheduleJob(jobDetail, simpleTrigger);

                    DisplayTriggers();

                    return true;
                }

                // logger.fine("heartBeat functionality is not used");
            }
            catch (Exception e)
            {
                // logger.error("failed to parse when create trigger", e);
                
            }

            return false;
        }

        protected ITrigger CreateHeartBeatTrigger(string applicationName, IJobDetail jobDetail)
        {
            TimeSpan startDelay;
            TimeSpan repeatInterval;

            if(this.UseSecondAsTimeUnit)
            {
                startDelay = TimeSpan.FromSeconds(this.HeartBeatStartDelay);
                repeatInterval = TimeSpan.FromSeconds(this.HeartBeatInterval);
            }
            else
            {
                startDelay = TimeSpan.FromMilliseconds(this.HeartBeatStartDelay);
                repeatInterval = TimeSpan.FromMilliseconds(this.HeartBeatInterval);
            }

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(applicationName, HeartBeatJob.GROUP_HEARTBEAT)
                .StartAt(DateTimeOffset.UtcNow.Add(startDelay))
                .WithSimpleSchedule(x => x
                    .WithInterval(repeatInterval)
                    .RepeatForever())
                .ForJob(jobDetail)
                .Build();

            return trigger;
        }

        protected IJobDetail CreateHeartBeatJobDetail(string applicationName)
        {
            Type jobType = HeartBeatJobType ?? this.JobType;

            ControlMessageEx controlMessage = this.MessageManager.CreateControlMessage("CONTROL-HEARTBEAT", applicationName);
            XmlDocument document = this.MessageManager.CreateDocument(controlMessage);

            JobDataMap jobData = new JobDataMap();
            jobData.Put("ControlServerManager", this);
            jobData.Put("ApplicationName", applicationName);
            jobData.Put("Document", document);
            jobData.Put("UseSecondAsTimeUnit", this.UseSecondAsTimeUnit);

            IJobDetail jobDetail = JobBuilder.Create(jobType)
                .WithIdentity(applicationName, HeartBeatJob.GROUP_HEARTBEAT)
                .UsingJobData(jobData)
                .Build();

            return jobDetail;
        }

        public void ScheduleHeartBeats()
        {
            if(this.UseHeartBeat)
            {
                //logger.fine("heartbeat functionality will be running");
                string hardwareType = ConfigurationManager.AppSettings[Settings.SYSTEM_ENV_KEY_HARDWARE_TYPE];

                if(string.IsNullOrEmpty(hardwareType))
                {
                    //logger.fine("hardwareType is not designated, {PRIMARY} will be applied");
                    hardwareType = "PRIMARY";
                }

                IList applicationNames = this.ApplicationManager.GetApplicationNamesByRunningHardware(hardwareType, true, true);

                foreach(object obj in applicationNames)
                {
                    string applicationName = (string)obj;

                    ScheduleHeartBeat(applicationName);
                }
            }
            else
            {
                //logger.fine("heartbeat functionality is not used");
            }

            string controlServerApplicationName = ConfigurationManager.AppSettings[Settings.SYSTEM_PROPERTY_KEY_ID_VALUE];

            ScheduleSimpleHeartBeat(controlServerApplicationName);

            ScheduleReschedulingHeartBeat(controlServerApplicationName);
        }

        protected bool ScheduleReschedulingHeartBeat(string applicationName)
        {
            if (this.RescheduleHeartBeatJobType == null)
            {
                //logger.warn("failed to schedule rescheduleHeartBeat, can not reschedule heartBeat, please check {rescheduleHeartBeatJobClass}");
                return false;
            }
            try
            {
                ITrigger trigger = this.SchedulingManager.GetTrigger(applicationName, RescheduleHeartBeatJob.GROUP_RESCHEDULE_HEARTBEAT);
                if (trigger != null)
                {
                    this.SchedulingManager.DeleteJob(applicationName, RescheduleHeartBeatJob.GROUP_RESCHEDULE_HEARTBEAT);
                }
                IJobDetail jobDetail = CreateReschedulingHeartBeatJobDetail(applicationName);

                ITrigger simpleTrigger = CreateReschedulingHeartBeatTrigger(applicationName, jobDetail);

                this.SchedulingManager.ScheduleJob(jobDetail, simpleTrigger);
            }
            catch (Exception e)
            {
                //logger.error("failed to parse when create trigger", e);
            }
            return false;
        }

        protected ITrigger CreateReschedulingHeartBeatTrigger(string applicationName, IJobDetail jobDetail)
        {
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(applicationName, RescheduleHeartBeatJob.GROUP_RESCHEDULE_HEARTBEAT)
                .StartAt(DateTimeOffset.UtcNow.Add(TimeSpan.FromMilliseconds(this.RescheduleHeartBeatStartDelay)))
                .WithSimpleSchedule(x => x
                    .WithInterval(TimeSpan.FromMilliseconds(this.RescheduleHeartBeatInterval))
                    .RepeatForever())
                .ForJob(jobDetail)
                .Build();

            return trigger;
        }

        protected IJobDetail CreateReschedulingHeartBeatJobDetail(string applicationName)
        {
            JobDataMap jobData = new JobDataMap();
            jobData.Put("ControlServerManager", this);

            IJobDetail jobDetail = JobBuilder.Create(this.RescheduleHeartBeatJobType)
                .WithIdentity(applicationName, RescheduleHeartBeatJob.GROUP_RESCHEDULE_HEARTBEAT)
                .UsingJobData(jobData)
                .Build();

            return jobDetail;
        }

        protected bool ScheduleSimpleHeartBeat(string applicationName)
        {
            if(this.SimpleHeartBeatJobType == null)
            {
                // logger.warn("failed to schedule, can not check myself, please check {simpleHeartBeatJobType}");
                return false;
            }

            try
            {
                ITrigger trigger = this.SchedulingManager.GetTrigger(applicationName, SimpleHeartBeatJob.GROUP_SIMPLE_HEARTBEAT);
                if (trigger != null)
                {
                    this.SchedulingManager.DeleteJob(applicationName, SimpleHeartBeatJob.GROUP_SIMPLE_HEARTBEAT);
                }

                IJobDetail jobDetail = CreateHeartSimpleHeartBeatJobDetail(applicationName);

                ITrigger simpleTrigger = CreateSimpleHeartBeatTrigger(applicationName, jobDetail);

                this.SchedulingManager.ScheduleJob(jobDetail, simpleTrigger);
            }
            catch (Exception e)
            {
                // logger.error("failed to parse when create trigger", e);
                
            }

            return false;
        }

        protected ITrigger CreateSimpleHeartBeatTrigger(string applicationName, IJobDetail jobDetail)
        {
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(applicationName, SimpleHeartBeatJob.GROUP_SIMPLE_HEARTBEAT)
                .StartAt(DateTimeOffset.UtcNow.Add(TimeSpan.FromMilliseconds(this.SimpleHeartBeatStartDelay)))
                .WithSimpleSchedule(x => x
                    .WithInterval(TimeSpan.FromMilliseconds(this.SimpleHeartBeatInterval))
                    .RepeatForever())
                .ForJob(jobDetail)
                .Build();

            return trigger;
        }

        protected IJobDetail CreateHeartSimpleHeartBeatJobDetail(string applicationName)
        {
            JobDataMap jobData = new JobDataMap();
            jobData.Put("ControlServerManager", this);
            jobData.Put("ApplicationName", applicationName);

            IJobDetail jobDetail = JobBuilder.Create(this.SimpleHeartBeatJobType)
                .WithIdentity(applicationName, SimpleHeartBeatJob.GROUP_SIMPLE_HEARTBEAT)
                .UsingJobData(jobData)
                .Build();

            return jobDetail;
        }

        public bool ShedulingHeartBeat(string applicationName)
        {
            ITrigger trigger = this.SchedulingManager.GetTrigger(applicationName, HeartBeatJob.GROUP_HEARTBEAT);
            if (trigger != null)
            {
                return true;
            }
            return false;
        }

        public bool Start(ControlMessage controlMessage)
        {
            //logger.info("start asked");

            string applicationName = controlMessage.ApplicationName;
            string applicationType = controlMessage.ApplicationType;

            return Start(applicationName, applicationType);
        }

        public bool Start(string applicationName, string applicationType)
        {
            string script = GetStartScript(applicationType);

            if (string.IsNullOrEmpty(script))
            {
                logger.Error("Start script not found for applicationType: " + applicationType);
                return false;
            }

            List<string> lines = null;
            try
            {
                string os = ConfigurationManager.AppSettings[Settings.SYSTEM_ENV_OPERATION_SYSTEM];

                if ((os != null) && (os.StartsWith("Windows")))
                {
                    //logger.info("Window os case, execute by process builder.");
                    lines = SystemUtility.PerformCommand(new string[] { script }, 2);            
                }
                else
                {
                    lines = SystemUtility.PerformCommand(new String[] { script }, 2);
                    //logger.info(lines);
                }
                ScheduleHeartBeat(applicationName);

                return true;
            }
            catch (PerformCommandException pce)
            {
                if (pce.ExitValue == 1)
                {
                    //logger.error("failed to perform command, arguments are not correct, " + pce.getMessage());
                }
                else if (pce.ExitValue == 2)
                {
                    //logger.error("failed to perform command, arguments are not correct, " + pce.getMessage());
                }
                else if (pce.ExitValue == 3)
                {
                    //logger.fine("failed to perform command, already running, " + pce.getMessage());
                    this.ApplicationManager.UpdateApplicationState(applicationName, "active");
                    ScheduleHeartBeat(applicationName);
                }
                else if (pce.ExitValue == 4)
                {
                    //logger.fine("failed to perform command, already killed, " + pce.getMessage());
                }
                else
                {
                    //logger.error("exitValue{" + pce.getExitValue() + "}, " + pce.getMessage());
                }
            }
            catch (Exception ie)
            {
                //logger.error("failed to execute " + script + ", " + ie.getMessage());
            }
            return false;
        }

        public bool UnscheduleHeartBeat(string applicationName)
        {
            bool result = false;

            //logger.info("heartBeat{" + applicationName + "} will be unscheduled");

            result = this.SchedulingManager.UnscheduleJob(applicationName, HeartBeatJob.GROUP_HEARTBEAT);

            DisplayTriggers();

            return result;
        }

        public void UnscheduleHeartBeats()
        {
            string hardwareType = ConfigurationManager.AppSettings[Settings.SYSTEM_ENV_KEY_HARDWARE_TYPE];
            if(string.IsNullOrEmpty(hardwareType))
            {
                //logger.fine("hardwareType is not designated, {PRIMARY} will be applied");
                hardwareType = "PRIMARY";
            }

            IList applicationNames = this.ApplicationManager.GetApplicationNamesByRunningHardware(hardwareType, true);

            foreach(object obj in applicationNames)
            {
                string applicationName = (string)obj;

                UnscheduleHeartBeat(applicationName);
            }
        }

        protected void Sleep(long millis)
        {
            try
            {
                Thread.Sleep(new TimeSpan(millis));
            }
            catch (Exception e)
            {
                // logger.error("", e);
                throw;
            }
        }

        protected string GetStartScript(ControlMessage controlMessage)
        {
            string applicationType = controlMessage.ApplicationType;
            return GetStartScript(applicationType);
        }
  

        protected string GetStartScript(string applicationType)
        {
            string script = "";
            if (applicationType.Equals("trans"))
            {
                script = (string)this.Scripts[SCRIPT_TS_START];
            }
            else if (applicationType.Equals("ei"))
            {
                script = (string)this.Scripts[SCRIPT_ES_START];
            }
            else if (applicationType.Equals("daemon"))
            {
                script = (string)this.Scripts[SCRIPT_DS_START];
            }
            else if (applicationType.Equals("emulator"))
            {
                script = (string)this.Scripts[SCRIPT_MS_START];
            }
            else if (applicationType.Equals("report"))
            {
                script = (string)this.Scripts[SCRIPT_RS_START];
            }
            else if (applicationType.Equals("query"))
            {
                script = (string)this.Scripts[SCRIPT_QS_START];
            }
            else
            {
                //logger.error("failed to get start script, please check applicationType{" + applicationType + "}");
            }
            return script;
        }

        public bool SystemCheck(ControlMessage controlMessage)
        {
            //logger.info("Unix System check asked");
            string script = (string)this.Scripts[SCRIPT_SYSTEMCHECK];
            if (string.IsNullOrEmpty(script))
            {
                return false;
            }
            List<string> lines = SystemUtility.PerformCommand(new String[] { script }, 13);
            //logger.info(lines);
            return true;
        }

        //0924 LSJ 추가
        //UI Transport Schedule 처리
        public void ScheduleUiTransport()
        {
            try
            {
                if (this.UseUiTransport)
                {
                    ITrigger trigger = this.SchedulingManager.GetTrigger(UiTransportJob.TRIGGER_UITRNASPROT, UiTransportJob.GROUP_UITRANSPORT);

                    if (trigger != null)
                    {
                        this.SchedulingManager.DeleteJob(UiTransportJob.TRIGGER_UITRNASPROT, UiTransportJob.GROUP_UITRANSPORT);
                    }

                    IJobDetail jobDetail = CreateUiTransportJobDetail(UiTransportJob.TRIGGER_UITRNASPROT);

                    ITrigger simpleTrigger = CreateUiTranportTrigger(UiTransportJob.TRIGGER_UITRNASPROT, jobDetail);

                    //logger.info("heartBeat{" + applicationName + "} will be scheduled");

                    this.SchedulingManager.ScheduleJob(jobDetail, simpleTrigger);

                   // DisplayTriggers();

                    return;
                }

                // logger.fine("heartBeat functionality is not used");
            }
            catch (Exception e)
            {
                // logger.error("failed to parse when create trigger", e);

            }
            return;
        }


        //1030 LSJ 추가
        //UI ApplicationManager Schedule 처리
        public void ScheduleUiApplicationManager()
        {
            try
            {
                if (this.UseUiApplicationManager)
                {
                    ITrigger trigger = this.SchedulingManager.GetTrigger(UiApplicationManagerJob.TRIGGER_UIAPPLICATIONMANAGER, UiApplicationManagerJob.GROUP_UIAPPLICATIONMANAGER);

                    if (trigger != null)
                    {
                        this.SchedulingManager.DeleteJob(UiApplicationManagerJob.TRIGGER_UIAPPLICATIONMANAGER, UiApplicationManagerJob.GROUP_UIAPPLICATIONMANAGER);
                    }

                    IJobDetail jobDetail = CreateUiApplicationManagerJobDetail(UiApplicationManagerJob.TRIGGER_UIAPPLICATIONMANAGER);

                    ITrigger simpleTrigger = CreateUiApplicationManagerTrigger(UiApplicationManagerJob.TRIGGER_UIAPPLICATIONMANAGER, jobDetail);

                    //logger.info("heartBeat{" + applicationName + "} will be scheduled");

                    this.SchedulingManager.ScheduleJob(jobDetail, simpleTrigger);

                    // DisplayTriggers();

                    return;
                }

                // logger.fine("heartBeat functionality is not used");
            }
            catch (Exception e)
            {
                // logger.error("failed to parse when create trigger", e);

            }
            return;
        }

        //200622 Change NIO Logic About ES.exe does not restart
        public void ScheduleUiCommand()
        {
            try
            {
                if (this.UseUiCommand)
                {
                    ITrigger trigger = this.SchedulingManager.GetTrigger(UiCommandJob.TRIGGER_UICOMMAND, UiCommandJob.GROUP_UICOMMAND);

                    if (trigger != null)
                    {
                        this.SchedulingManager.DeleteJob(UiCommandJob.TRIGGER_UICOMMAND, UiCommandJob.GROUP_UICOMMAND);
                    }

                    IJobDetail jobDetail = CreateUiCommandJobDetail(UiCommandJob.TRIGGER_UICOMMAND);

                    ITrigger simpleTrigger = CreateUiCommandTrigger(UiCommandJob.TRIGGER_UICOMMAND, jobDetail);

                    //logger.info("heartBeat{" + applicationName + "} will be scheduled");

                    this.SchedulingManager.ScheduleJob(jobDetail, simpleTrigger);

                    // DisplayTriggers();

                    return;
                }

                // logger.fine("heartBeat functionality is not used");
            }
            catch (Exception e)
            {
                // logger.error("failed to parse when create trigger", e);

            }
            return;
        }
        //

        protected IJobDetail CreateUiTransportJobDetail(string TriggerName)
        {
            Type jobType = UiTransportJobType ?? this.JobType;

            //ControlMessageEx controlMessage = this.MessageManager.CreateControlMessage("CONTROL-HEARTBEAT", applicationName);
            //XmlDocument document = this.MessageManager.CreateDocument(controlMessage);

            JobDataMap jobData = new JobDataMap();

            //나중에 채우기
            jobData.Put("transferManager", TransferManager);
            jobData.Put("messageManager", MessageManager);
            jobData.Put("messageAgent", MessageAgent);
            //jobData.Put("Document", document);
            //jobData.Put("UseSecondAsTimeUnit", this.UseSecondAsTimeUnit);

            IJobDetail jobDetail = JobBuilder.Create(jobType)
                .WithIdentity(TriggerName, UiTransportJob.GROUP_UITRANSPORT)
                .UsingJobData(jobData)
                .Build();

            return jobDetail;
        }

        protected ITrigger CreateUiTranportTrigger(string TriggerName, IJobDetail jobDetail)
        {
            //수정필요
            //startDelay = TimeSpan.FromSeconds(this.UiTransportStartDelay);

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(TriggerName, UiTransportJob.GROUP_UITRANSPORT)
                .WithSimpleSchedule(x => x
                    .WithInterval(TimeSpan.FromSeconds(this.UiTransportInterval))
                    .RepeatForever())
                .ForJob(jobDetail)
                .Build();

            return trigger;
        }

        //200622 Change NIO Logic About ES.exe does not restart
        protected ITrigger CreateUiCommandTrigger(string TriggerName, IJobDetail jobDetail)
        {
            //수정필요
            //startDelay = TimeSpan.FromSeconds(this.UiTransportStartDelay);

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(TriggerName, UiCommandJob.GROUP_UICOMMAND)
                .WithSimpleSchedule(x => x
                    .WithInterval(TimeSpan.FromSeconds(this.UiCommandInterval))
                    .RepeatForever())
                .ForJob(jobDetail)
                .Build();

            return trigger;
        }
        //

        protected IJobDetail CreateUiApplicationManagerJobDetail(string TriggerName)
        {
            Type jobType = UiApplicationManagerJobType ?? this.JobType;

            //ControlMessageEx controlMessage = this.MessageManager.CreateControlMessage("CONTROL-HEARTBEAT", applicationName);
            //XmlDocument document = this.MessageManager.CreateDocument(controlMessage);

            JobDataMap jobData = new JobDataMap();

            //나중에 채우기
            jobData.Put("applicationManager", ApplicationManager);
            jobData.Put("messageManager", MessageManager);
            jobData.Put("workflowManager", WorkflowManager);
            //jobData.Put("Document", document);
            //jobData.Put("UseSecondAsTimeUnit", this.UseSecondAsTimeUnit);

            IJobDetail jobDetail = JobBuilder.Create(jobType)
                .WithIdentity(TriggerName, UiApplicationManagerJob.GROUP_UIAPPLICATIONMANAGER)
                .UsingJobData(jobData)
                .Build();

            return jobDetail;
        }

        //200622 Change NIO Logic About ES.exe does not restart
        protected IJobDetail CreateUiCommandJobDetail(string TriggerName)
        {
            Type jobType = UiCommandJobType ?? this.JobType;

            //ControlMessageEx controlMessage = this.MessageManager.CreateControlMessage("CONTROL-HEARTBEAT", applicationName);
            //XmlDocument document = this.MessageManager.CreateDocument(controlMessage);

            JobDataMap jobData = new JobDataMap();

            //나중에 채우기
            jobData.Put("transferManager", TransferManager);
            jobData.Put("messageManager", MessageManager);
            jobData.Put("messageAgent", MessageAgent);

            jobData.Put("workflowManager", WorkflowManager);
            //jobData.Put("nioInterfaceManager", NioInterfaceManager);

            //jobData.Put("Document", document);
            //jobData.Put("UseSecondAsTimeUnit", this.UseSecondAsTimeUnit);

            IJobDetail jobDetail = JobBuilder.Create(jobType)
                .WithIdentity(TriggerName, UiCommandJob.GROUP_UICOMMAND)
                .UsingJobData(jobData)
                .Build();

            return jobDetail;
        }
        //

        protected ITrigger CreateUiApplicationManagerTrigger(string TriggerName, IJobDetail jobDetail)
        {
            //수정필요
            //startDelay = TimeSpan.FromSeconds(this.UiTransportStartDelay);

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(TriggerName, UiApplicationManagerJob.GROUP_UIAPPLICATIONMANAGER)
                .WithSimpleSchedule(x => x
                    .WithInterval(TimeSpan.FromSeconds(this.UiApplicationManagerInterval))
                    .RepeatForever())
                .ForJob(jobDetail)
                .Build();

            return trigger;
        }
    }
}
