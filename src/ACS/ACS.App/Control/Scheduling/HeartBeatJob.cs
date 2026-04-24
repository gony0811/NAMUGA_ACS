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

            string jsonMessage = (string)context.MergedJobDataMap.Get("JsonMessage");

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
                bool result = CheckHeartBeat(controlServerManager, application, jsonMessage, timeout);

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
                                retryResult = CheckHeartBeat(controlServerManager, application, jsonMessage, retryTimeout);

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
                        logger.Error($"HeartBeat exception for {applicationName}: {e.Message}", e);
                    }
                }
            }
        }

        private bool CheckHeartBeat(IControlServerManager controlServerManager, ACS.Core.Application.Model.Application application, string jsonMessage, long timeout)
        {
            bool result = false;

            DateTime date = DateTime.UtcNow;

            string destinationName = controlServerManager.GetDestinationName(application.Name);
            string replyMessage = controlServerManager.SynchronousMessageAgent.Request(jsonMessage, destinationName, timeout);

            if (!string.IsNullOrEmpty(replyMessage))
            {
                if (!application.State.Equals("active"))
                {
                    controlServerManager.ApplicationManager.UpdateApplicationState(application.Name, "active", date);
                    logger.Info($"HeartBeat: {application.Name} state → active");
                }
                else
                {
                    controlServerManager.ApplicationManager.UpdateApplicationCheckTime(application.Name, date);
                }
                result = true;
            }
            else
            {
                logger.Warn($"HeartBeat: {application.Name} 응답 없음 (타임아웃 {timeout}ms)");
                controlServerManager.ApplicationManager.UpdateApplicationCheckTime(application.Name, date);
                controlServerManager.HistoryManager.CreateHeartBeatFailHistory(application);
            }
            return result;
        }
    }
}
