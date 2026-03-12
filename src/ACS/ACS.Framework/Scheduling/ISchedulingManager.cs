using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;


namespace ACS.Framework.Scheduling
{
    public interface ISchedulingManager
    {
        String GetSchedulerMetaDataInfo();
        
        String GetSchedulerInstanceId();
        
        String GetSchedulerName();
        
        JobDetail GetJobDetail(String paramString1, String paramString2);
        
        bool PauseJob(String paramString1, String paramString2);
        
        bool ResumeJob(String paramString1, String paramString2);
        
        bool DeleteJob(String paramString1, String paramString2);
        
        bool PauseJobGroup(String paramString);
        
        bool ResumeJobGroup(String paramString);
        
        Trigger GetTrigger(String paramString1, String paramString2);
        
        bool PauseTrigger(String paramString1, String paramString2);
        
        bool ResumeTrigger(String paramString1, String paramString2);
        
        bool PauseTriggerGroup(String paramString);
        
        bool ResumeTriggerGroup(String paramString);
        
        List<string> GetPausedTriggerGroups();
        
        bool PauseAll();
        
        bool ResumeAll();
        
        bool ScheduleJob(JobDetail paramJobDetail, Trigger paramTrigger);
        
        bool UnscheduleJob(String paramString1, String paramString2);
        
        String GetTriggerState(String paramString1, String paramString2);

        List<string> GetJobGroupNames();

        List<string> GetJobNames(String paramString);

        List<string> GetTriggerGroupNames();

        List<string> GetTriggerNames(String paramString);
        
        void DisplayTriggers();
        
        void DisplayTriggerGroup(String paramString);
        
        void DisplayTrigger(Trigger paramTrigger);

        void DisplayJobs();
        
        void DisplayJobGroup(String paramString);
        
        void DisplayJobDetail(JobDetail paramJobDetail);
    }
}
