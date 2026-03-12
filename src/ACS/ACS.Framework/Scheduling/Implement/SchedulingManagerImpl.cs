using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;
using Quartz;
using Quartz.Collection;
using Quartz.Impl;
using Quartz.Listener;

namespace ACS.Framework.Scheduling
{
    public class SchedulingManagerImplement : AbstractManager, ISchedulingManager
    {
        private IScheduler scheduler;

        public IScheduler Scheduler
        {
            get { return scheduler; }
            set { scheduler = value; }
        }

        public bool DeleteJob(string jobName, string jobGroupName)
        {
            bool result = false;

            try
            {               
                this.scheduler.DeleteJob(jobName, jobGroupName);
                result = true;
            }
            catch(SchedulerException e)
            {

            }

            return result;
        }

        public void DisplayJobDetail(JobDetail paramJobDetail)
        {
            //throw new NotImplementedException();
        }

        public void DisplayJobGroup(string jobGroupName)
        {
            List<string> jobNames = GetJobNames(jobGroupName);
            for (int index = 0; index < jobNames.Count; index++)
            {
                String jobName = jobNames[index];

                JobDetail jobDetail = GetJobDetail(jobName, jobGroupName);
                if (jobDetail != null)
                {
                    DisplayJobDetail(jobDetail);
                }
            }
        }

        public void DisplayJobs()
        {
            try
            {
                List<string> jobGroupNames = this.scheduler.JobGroupNames.ToList();
                if (jobGroupNames.Count == 0)
                {
                    return;
                }

                foreach (string jobGroupName in jobGroupNames)
                {
                    DisplayJobGroup(jobGroupName);
                }
            }
            catch (SchedulerException e)
            {
            }
        }

        public void DisplayTrigger(Trigger trigger)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("trigger{" + trigger.JobName + "}, ");
            sb.Append("state{" + GetTriggerState(trigger.JobName, trigger.JobGroup) + "}, ");
            if (trigger is CronTrigger)
            {
                sb.Append("expression{" + ((CronTrigger)trigger).CronExpressionString + "}, ");
                sb.Append("start{" + ((CronTrigger)trigger).StartTimeUtc.ToString() + "}, ");
                sb.Append("end{" + ((CronTrigger)trigger).EndTimeUtc.ToString() + "}, ");
                sb.Append("final{" + ((CronTrigger)trigger).FinalFireTimeUtc.ToString() + "}, ");
                sb.Append("previous{" + ((CronTrigger)trigger).GetPreviousFireTimeUtc().ToString() + "}, ");
                sb.Append("next{" + ((CronTrigger)trigger).GetNextFireTimeUtc().ToString() + "}");
            }
            else if ((trigger is SimpleTrigger))
            {
                sb.Append("interval{" + ((SimpleTrigger)trigger).RepeatInterval + "}, ");
                sb.Append("repeat{" + ((SimpleTrigger)trigger).RepeatCount + "}, ");
                sb.Append("triggered{" + ((SimpleTrigger)trigger).TimesTriggered + "}, ");
                sb.Append("start{" + ((SimpleTrigger)trigger).StartTimeUtc + "}, ");
                sb.Append("end{" + ((SimpleTrigger)trigger).EndTimeUtc + "}, ");
                sb.Append("final{" + ((SimpleTrigger)trigger).FinalFireTimeUtc + "}, ");
                sb.Append("previous{" + ((SimpleTrigger)trigger).GetPreviousFireTimeUtc().ToString() + "}, ");
                sb.Append("next{" + ((SimpleTrigger)trigger).GetNextFireTimeUtc().ToString() + "}");
            }
        }

        public void DisplayTriggerGroup(string jobGroupName)
        {
            List<string> jobNames = GetJobNames(jobGroupName);

            foreach(string jobName in jobNames)
            {
                JobDetail jobDetail = GetJobDetail(jobName, jobGroupName);

                if(jobDetail != null)
                {
                    DisplayJobDetail(jobDetail);
                }
            }
        }

        public void DisplayTriggers()
        {
            try
            {
                List<string> triggerGroupNames = this.scheduler.TriggerGroupNames.ToList();
                if (triggerGroupNames.Count == 0)
                {
                    return;
                }
                for (int triggerGroupIndex = 0; triggerGroupIndex < triggerGroupNames.Count; triggerGroupIndex++)
                {
                    String triggerGroupName = triggerGroupNames[triggerGroupIndex];
                    DisplayTriggerGroup(triggerGroupName);
                }
            }
            catch (SchedulerException e)
            {
                
            }
        }

        public JobDetail GetJobDetail(string jobName, string jobGroupName)
        {
            JobDetail jobDetail = null;

            try
            {                
                jobDetail = this.scheduler.GetJobDetail(jobName, jobGroupName);
            }
            catch (SchedulerException e)
            {

            }

            return jobDetail;
        }

        public IList<string> GetJobGroupNames()
        {
            try
            {
                return this.scheduler.JobGroupNames;
            }
            catch (SchedulerException e)
            {
                //logger.Error("", e);
            }
            return new String[0];
        }

        public List<string> GetJobNames(string jobGroupName)
        {
            try
            {
                List<string> jobNames = this.scheduler.GetJobNames(jobGroupName).ToList();

                return jobNames;
            }
            catch (SchedulerException e)
            {
                //logger.Error("", e);
            }
            return new List<string>();          
        }

        public List<string> GetPausedTriggerGroups()
        {
            try
            {
                Quartz.Collection.ISet pausedTriggerGroups = this.scheduler.GetPausedTriggerGroups();
                List<string> retpausedTriggerGroups = new List<string>();
                foreach (var item in pausedTriggerGroups)
                {
                    retpausedTriggerGroups.Add(item.ToString());
                }
                return retpausedTriggerGroups;
            }
            catch(SchedulerException e)
            {

            }

            return new List<string>();
        }

        public string GetSchedulerInstanceId()
        {
            string schedulerInstanceId = "";

            try
            {
                schedulerInstanceId = this.scheduler.SchedulerInstanceId;
            }
            catch(SchedulerException e)
            {

                throw;    
            }

            return schedulerInstanceId;
        }

        public string GetSchedulerMetaDataInfo()
        {
            string schedulerMetaDataInfo = "";

            try
            {
                schedulerMetaDataInfo = scheduler.GetMetaData().ToString();
            }
            catch(SchedulerException e)
            {
                //LOG
            }

            return schedulerMetaDataInfo;
        }

        public string GetSchedulerName()
        {
            string schedulerName = "";

            try
            {
                schedulerName = this.scheduler.SchedulerName;
            }
            catch(SchedulerException e)
            {
                //LOG
            }

            return schedulerName;
        }

        public Trigger GetTrigger(string triggerName, string triggerGroupName)
        {
            Trigger trigger = null;

            try
            {              
                trigger = this.scheduler.GetTrigger(triggerName, triggerGroupName);
            }
            catch(SchedulerException e)
            {

            }

            return trigger;
        }

        public List<string> GetTriggerGroupNames()
        {
            try
            {
                return this.scheduler.TriggerGroupNames.ToList();
            }
            catch (SchedulerException e)
            {
                
            }
            return new List<string>();
        }

        public List<string> GetTriggerNames(string triggerGroupName)
        {
            try
            {
                List<string> triggerNames = this.scheduler.GetTriggerNames(triggerGroupName).ToList();

                return triggerNames;
            }
            catch (SchedulerException e)
            {

            }
            return new List<string>();
        }

        public string GetTriggerState(string triggerName, string triggerGroupName)
        {
            string triggerState = "none";

            try
            {
                triggerState = TriggerStateToString((int)this.scheduler.GetTriggerState(triggerName, triggerGroupName));
            }
            catch (SchedulerException e)
            {

                throw;
            }

            return triggerState;
        }

        public bool PauseAll()
        {
            bool result = false;

            try
            {
                this.scheduler.PauseAll();
                result = true;
            }
            catch(SchedulerException e)
            {

            }

            return result;
        }

        public bool PauseJob(string jobName, string jobGroupName)
        {
            bool result = false;
            try
            {
              
                this.scheduler.PauseJob(jobName, jobGroupName);
                result = true;
            }
            catch (SchedulerException e)
            {

            }
            return result;
        }

        public bool PauseJobGroup(string jobGroupName)
        {
            bool result = false;

            try
            {
                //다시
                string jobname = GetJobNames(jobGroupName)[0];
                this.scheduler.PauseJob(jobname, jobGroupName);
                result = true;
            }
            catch(SchedulerException e)
            {

            }

            return result;
        }

        public bool PauseTrigger(string triggerName, string triggerGroupName)
        {
            bool result = false;

            try
            {
                
                this.scheduler.PauseTrigger(triggerName, triggerGroupName);
                result = true;
            }
            catch (SchedulerException e)
            {

            }

            return result;
        }

        public bool PauseTriggerGroup(string triggerGroupName)
        {
            bool result = false;

            try
            {
                List<string> Trigger = GetTriggerNames(triggerGroupName);
                foreach(var triggerName in Trigger)
                {
                    this.scheduler.PauseTrigger(triggerName, triggerGroupName);
                }

               
                result = true;
            }
            catch (SchedulerException e)
            {

            }

            return result;
        }

        public bool ResumeAll()
        {
            bool result = false;

            try
            {
                this.scheduler.ResumeAll();
                result = true;
            }
            catch(SchedulerException e)
            {

            }
            return result;
        }

        public bool ResumeJob(string jobName, string jobGroupName)
        {
            bool result = false;

            try
            {
                this.scheduler.ResumeJob(jobName, jobGroupName);
                result = true;
            }
            catch(SchedulerException e)
            {

            }

            return result;
        }

        public bool ResumeJobGroup(string jobGroupName)
        {
            bool result = false;

            try
            {
                List<string> jobs = GetJobNames(jobGroupName);
                foreach (var jobName in jobs)
                {
                    this.scheduler.ResumeJob(jobName, jobGroupName);
                    result = true;
                }
            }
            catch (SchedulerException e)
            {

            }

            return result;
        }

        public bool ResumeTrigger(string jobName, string jobGroupName)
        {
            bool result = false;

            try
            {
                this.scheduler.ResumeTrigger(jobName, jobGroupName);
                result = true;
            }
            catch (SchedulerException e)
            {

            }

            return result;
        }

        public bool ResumeTriggerGroup(string jobGroupName)
        {
            bool result = false;

            try
            {
                  List<string> Trigger = GetTriggerNames(jobGroupName);
                  foreach (var triggerName in Trigger)
                  {
                      this.scheduler.ResumeTrigger(triggerName ,jobGroupName ); 
                  }
                result = true;
            }
            catch (SchedulerException e)
            {

            }

            return result;
        }

        public bool ScheduleJob(JobDetail jobDetail, Trigger trigger)
        {
            bool result = false;
            try
            {
                this.scheduler.ScheduleJob(jobDetail, trigger);
               
                result = true;
            }
            catch (SchedulerException e)
            {
                
            }
            return result;
        }

        public bool UnscheduleJob(string triggerName, string triggerGroupName)
        {
            bool result = false;
            try
            {
                this.scheduler.UnscheduleJob(triggerName, triggerGroupName);
                result = true;
            }
            catch (SchedulerException e)
            {
            }
            return result;
        }

        protected string TriggerStateToString(int triggerState)
        {
            string state = "none";
            switch (triggerState)
            {
                case 0:
                    state = "normal";
                    break;
                case 1:
                    state = "paused";
                    break;
                case 2:
                    state = "complete";
                    break;
                case 3:
                    state = "error";
                    break;
                case 4:
                    state = "blocked";
                    break;
            }
            return state;
        }

        List<string> ISchedulingManager.GetJobGroupNames()
        {
            try
            {
                return this.GetTriggerGroupNames();
            }
            catch(SchedulerException e)
            {

            }

            return new List<string>();
        }
    }
}
