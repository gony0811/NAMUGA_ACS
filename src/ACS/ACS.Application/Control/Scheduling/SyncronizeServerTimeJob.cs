using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.Configuration;
using Quartz;
using ACS.Control;
using ACS.Framework.Scheduling.Model;
using ACS.Framework.Application;
using ACS.Framework.Application.Model;
using ACS.Framework.Logging;

namespace ACS.Control.Scheduling
{
    public class SyncronizeServerTimeJob : AbstractJob
    {
        public static string SCHEDULE_TIMESYNC = "SCHEDULE-TIMESYNC";
        
        public override void ExecuteJob(IJobExecutionContext context)
        {
            IControlServerManagerEx controlServerManager = (IControlServerManagerEx)context.MergedJobDataMap.Get("ControlServerManager");
            string applicationName = (string)context.MergedJobDataMap.Get("ApplicationName");

            XmlDocument document = (XmlDocument)context.MergedJobDataMap.Get("Document");

            bool useSecondAsTimeUnit = (bool)context.MergedJobDataMap.Get("UseSecondAsTimeUnit");

            string hardwareType = ConfigurationManager.AppSettings[Settings.SYSTEM_ENV_KEY_HARDWARE_TYPE];

            //if(string.IsNullOrEmpty(hardwareType))
            //{
            //    //
            //    hardwareType = "PRIMARY";
            //}

            ACS.Framework.Application.Model.Application application = controlServerManager.ApplicationManager.GetApplication(applicationName);

            if(!application.RunningHardware.Equals(hardwareType))
            {
                controlServerManager.UnScheduleServerTimeSync(applicationName);
            }
            else
            {
                long timeout = useSecondAsTimeUnit ? controlServerManager.ServerTimeSyncTimeout / 1000L : controlServerManager.HeartBeatTimeout;
                bool result = SyncronizeServerTime(controlServerManager, application, document, timeout);

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
                            controlServerManager.PauseServerTImeSync(applicationName);

                            bool retryResult = false;

                            for(int i = 0; i < controlServerManager.ServerTimeSyncRetryCount; i++)
                            {
                                long retryTimeout = useSecondAsTimeUnit ? controlServerManager.ServerTimeSyncTimeout : controlServerManager.ServerTimeSyncTimeout / 1000L;
                                retryResult = SyncronizeServerTime(controlServerManager, application, document, retryTimeout);

                                //Response X
                                if(!retryResult)
                                {
                                    processId = controlServerManager.GetProcessId(applicationName);

                                    //프로세스ID O
                                    if (!string.IsNullOrEmpty(processId))
                                    {
                                        logger.Fatal("failed to syncronize server time again, application{" + application.Name + "}, retry{" + (i + 1) + "}");
                                    }
                                    else
                                    {                              
                                        retryResult = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    //Response O
                                    controlServerManager.ResumeServerTimeSync(applicationName);
                                    break;
                                }
                            }

                            //Response X
                            if (!retryResult)
                            {
                                controlServerManager.ResumeServerTimeSync(applicationName);
                            }
                        }
                        else
                        {
                            //프로세스ID X
                            controlServerManager.UnScheduleServerTimeSync(applicationName);    
                        }
                    }
                    catch (Exception e)
                    {
                        // logger.Error("exitValue{" + e.ExitValue() + "}, " + e.Message + ", just use external script");
                    }
                }
            }
        }

        private bool SyncronizeServerTime(IControlServerManager controlServerManager, ACS.Framework.Application.Model.Application application, XmlDocument document, long timeout)
        {
            bool result = false;

            DateTime date = DateTime.Now;


            XmlElement data = document.DocumentElement["DATA"];
            XmlNode currentTime = document.CreateNode(XmlNodeType.Element, "CURRENTTIME", "");
            currentTime.InnerText = DateTime.Now.ToLongTimeString();
            data.AppendChild(currentTime);

            //logger.info("result timeout{" + timeout + sec}");
            XmlDocument replyDocument = controlServerManager.SynchronousMessageAgent.Request(document, controlServerManager.GetDestinationName(application.Name), timeout);

            if(replyDocument != null)
            {
                // logger.info("succeeded in checking heartBeat to application{" + application.Name + "}");
                result = true;
            }
            else
            {
                // logger.info("failed to check heartBeat, application{" + application.Name + "} does not reply");
            }
            return result;
        }
    }
}
