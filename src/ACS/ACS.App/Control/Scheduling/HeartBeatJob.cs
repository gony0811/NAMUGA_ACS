using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Quartz;
using ACS.Control;
using ACS.Core.Scheduling.Model;
using ACS.Core.Application;
using ACS.Core.Application.Model;
using ACS.Core.Logging;

namespace ACS.Control.Scheduling
{
    public class HeartBeatJob : AbstractJob
    {
        public static string GROUP_HEARTBEAT = "GROUP-HEARTBEAT";
        
        public override void ExecuteJob(IJobExecutionContext context)
        {
            IControlServerManager controlServerManager = (IControlServerManager)context.MergedJobDataMap.Get("ControlServerManager");
            string applicationName = (string)context.MergedJobDataMap.Get("ApplicationName");

            XmlDocument document = (XmlDocument)context.MergedJobDataMap.Get("Document");

            bool useSecondAsTimeUnit = (bool)context.MergedJobDataMap.Get("UseSecondAsTimeUnit");

            IConfiguration configuration = (IConfiguration)context.MergedJobDataMap.Get("Configuration");
            string hardwareType = configuration["Acs:Process:HardwareType"];

            if(string.IsNullOrEmpty(hardwareType))
            {
                //
                hardwareType = "PRIMARY";
            }

            ACS.Core.Application.Model.Application application = controlServerManager.ApplicationManager.GetApplication(applicationName);

            if(!application.RunningHardware.Equals(hardwareType))
            {
                controlServerManager.UnscheduleHeartBeat(applicationName);
            }
            else
            {
                long timeout = useSecondAsTimeUnit ? controlServerManager.HeartBeatTimeout / 1000L : controlServerManager.HeartBeatTimeout;
                bool result = CheckHeartBeat(controlServerManager, application, document, timeout);

                //Response X
                if (!result)
                {
                    try
                    {
                        string processId = controlServerManager.GetProcessId(applicationName);

                        //프로세스ID O
                        if (!string.IsNullOrEmpty(processId))
                        {
                            //HeartBeat Check Logic Off
                            controlServerManager.PauseHeartBeat(applicationName);

                            bool retryResult = false;

                            for(int i = 0; i < controlServerManager.HeartBeatRetryCount; i++)
                            {
                                long retryTimeout = useSecondAsTimeUnit ? controlServerManager.HeartBeatRetryTimeout : controlServerManager.HeartBeatRetryTimeout / 1000L;
                                retryResult = CheckHeartBeat(controlServerManager, application, document, retryTimeout);

                                //Response X
                                if(!retryResult)
                                {
                                    processId = controlServerManager.GetProcessId(applicationName);

                                    //프로세스ID O
                                    if (!string.IsNullOrEmpty(processId))
                                    {
                                        logger.Fatal("failed to check heartHeat again, application{" + application.Name + "}, retry{" + (i + 1) + "}");
                                    }
                                    else
                                    {
                                        //프로세스ID X
                                        switch (controlServerManager.HeartBeatFailWhenProcessDown)
                                        {
                                            case 1:
                                                logger.Info("HEARTBEATFAIL_PROCESSDOWN_STATE_INACTIVE");
                                                controlServerManager.UnscheduleHeartBeat(applicationName);
                                                controlServerManager.ApplicationManager.UpdateApplicationState(applicationName, "inactive");
                                                break;
                                            case 2:
                                                logger.Info("HEARTBEATFAIL_PROCESSDOWN_STATE_RESTART");
                                                controlServerManager.UnscheduleHeartBeat(applicationName);
                                                controlServerManager.Start(applicationName, application.Type);
                                                break;
                                            default:
                                                logger.Info("HEARTBEATFAIL_PROCESSDOWN_NOTHING");
                                                
                                                break;
                                        }
                                        retryResult = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    //Response O
                                    controlServerManager.ResumeHeartBeat(applicationName);
                                    break;
                                }
                            }

                            //Response X
                            if (!retryResult)
                            {
                                switch(controlServerManager.HeartBeatFailWhenProcessHang)
                                {
                                    case 1:
                                        logger.Info("HEARTBEATFAIL_PROCESSHANG_STATE_HANG");
                                        controlServerManager.ApplicationManager.UpdateApplicationState(applicationName, "hang");
                                        controlServerManager.ResumeHeartBeat(applicationName);
                                        break;
                                    case 2:
                                        logger.Info("HEARTBEATFAIL_PROCESSHANG_STATE_RESTART");
                                        controlServerManager.Kill(applicationName, application.Type);
                                        controlServerManager.Start(applicationName, application.Type);
                                        break;
                                    default:
                                        logger.Info("HEARTBEATFAIL_PROCESSHANG_STATE_NOTHING");
                                        controlServerManager.ResumeHeartBeat(applicationName);
                                        break;
                                }
                            }
                        }
                        else
                        {
                            //프로세스ID X
                            switch (controlServerManager.HeartBeatFailWhenProcessDown)
                            {
                                case 1:
                                    controlServerManager.UnscheduleHeartBeat(applicationName);
                                    controlServerManager.ApplicationManager.UpdateApplicationState(applicationName, "inactive");
                                    break;
                                case 2:
                                    controlServerManager.UnscheduleHeartBeat(applicationName);
                                    controlServerManager.Start(applicationName, application.Type);
                                    break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // logger.Error("exitValue{" + e.ExitValue() + "}, " + e.Message + ", just use external script");
                    }
                }
            }
        }

        private bool CheckHeartBeat(IControlServerManager controlServerManager, ACS.Core.Application.Model.Application application, XmlDocument document, long timeout)
        {
            bool result = false;

            DateTime date = DateTime.UtcNow;

            //logger.info("result timeout{" + timeout + sec}");
            XmlDocument replyDocument = controlServerManager.SynchronousMessageAgent.Request(document, controlServerManager.GetDestinationName(application.Name), timeout);

            if(replyDocument != null)
            {
                // logger.info("succeeded in checking heartBeat to application{" + application.Name + "}");

                if(!application.State.Equals("active"))
                {
                    // logger.info("application.state{" + application.State + "} is not active, state will be updated to active, {" + application.Name + "}");
                    controlServerManager.ApplicationManager.UpdateApplicationState(application.Name, "active", date);
                }
                else
                {
                    controlServerManager.ApplicationManager.UpdateApplicationCheckTime(application.Name, date);
                }
                result = true;
            }
            else
            {
                // logger.info("failed to check heartBeat, application{" + application.Name + "} does not reply");
                controlServerManager.ApplicationManager.UpdateApplicationCheckTime(application.Name, date);
                controlServerManager.HistoryManager.CreateHeartBeatFailHistory(application);
            }
            return result;
        }
    }
}
