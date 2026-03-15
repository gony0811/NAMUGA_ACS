using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;

namespace ACS.Core.Scheduling
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
                scheduler.DeleteJob(new JobKey(jobName, jobGroupName)).GetAwaiter().GetResult();
                result = true;
            }
            catch (SchedulerException)
            {
            }

            return result;
        }

        public void DisplayJobDetail(IJobDetail paramJobDetail)
        {
        }

        public void DisplayJobGroup(string jobGroupName)
        {
            List<string> jobNames = GetJobNames(jobGroupName);
            for (int index = 0; index < jobNames.Count; index++)
            {
                String jobName = jobNames[index];

                IJobDetail jobDetail = GetJobDetail(jobName, jobGroupName);
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
                var jobGroupNames = scheduler.GetJobGroupNames().GetAwaiter().GetResult();
                if (jobGroupNames.Count == 0)
                {
                    return;
                }

                foreach (string jobGroupName in jobGroupNames)
                {
                    DisplayJobGroup(jobGroupName);
                }
            }
            catch (SchedulerException)
            {
            }
        }

        public void DisplayTrigger(ITrigger trigger)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("trigger{" + trigger.Key.Name + "}, ");
            sb.Append("state{" + GetTriggerState(trigger.Key.Name, trigger.Key.Group) + "}, ");
            if (trigger is ICronTrigger cronTrigger)
            {
                sb.Append("expression{" + cronTrigger.CronExpressionString + "}, ");
                sb.Append("start{" + cronTrigger.StartTimeUtc.ToString() + "}, ");
                sb.Append("end{" + cronTrigger.EndTimeUtc?.ToString() + "}, ");
                sb.Append("final{" + cronTrigger.FinalFireTimeUtc?.ToString() + "}, ");
                sb.Append("previous{" + cronTrigger.GetPreviousFireTimeUtc()?.ToString() + "}, ");
                sb.Append("next{" + cronTrigger.GetNextFireTimeUtc()?.ToString() + "}");
            }
            else if (trigger is ISimpleTrigger simpleTrigger)
            {
                sb.Append("interval{" + simpleTrigger.RepeatInterval + "}, ");
                sb.Append("repeat{" + simpleTrigger.RepeatCount + "}, ");
                sb.Append("triggered{" + simpleTrigger.TimesTriggered + "}, ");
                sb.Append("start{" + simpleTrigger.StartTimeUtc + "}, ");
                sb.Append("end{" + simpleTrigger.EndTimeUtc + "}, ");
                sb.Append("final{" + simpleTrigger.FinalFireTimeUtc + "}, ");
                sb.Append("previous{" + simpleTrigger.GetPreviousFireTimeUtc()?.ToString() + "}, ");
                sb.Append("next{" + simpleTrigger.GetNextFireTimeUtc()?.ToString() + "}");
            }
        }

        public void DisplayTriggerGroup(string jobGroupName)
        {
            List<string> jobNames = GetJobNames(jobGroupName);

            foreach (string jobName in jobNames)
            {
                IJobDetail jobDetail = GetJobDetail(jobName, jobGroupName);

                if (jobDetail != null)
                {
                    DisplayJobDetail(jobDetail);
                }
            }
        }

        public void DisplayTriggers()
        {
            try
            {
                var triggerGroupNames = scheduler.GetTriggerGroupNames().GetAwaiter().GetResult();
                if (triggerGroupNames.Count == 0)
                {
                    return;
                }
                foreach (string triggerGroupName in triggerGroupNames)
                {
                    DisplayTriggerGroup(triggerGroupName);
                }
            }
            catch (SchedulerException)
            {
            }
        }

        public IJobDetail GetJobDetail(string jobName, string jobGroupName)
        {
            IJobDetail jobDetail = null;

            try
            {
                jobDetail = scheduler.GetJobDetail(new JobKey(jobName, jobGroupName)).GetAwaiter().GetResult();
            }
            catch (SchedulerException)
            {
            }

            return jobDetail;
        }

        public List<string> GetJobGroupNames()
        {
            try
            {
                return scheduler.GetJobGroupNames().GetAwaiter().GetResult().ToList();
            }
            catch (SchedulerException)
            {
            }
            return new List<string>();
        }

        public List<string> GetJobNames(string jobGroupName)
        {
            try
            {
                var jobKeys = scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(jobGroupName)).GetAwaiter().GetResult();
                return jobKeys.Select(k => k.Name).ToList();
            }
            catch (SchedulerException)
            {
            }
            return new List<string>();
        }

        public List<string> GetPausedTriggerGroups()
        {
            try
            {
                var pausedTriggerGroups = scheduler.GetPausedTriggerGroups().GetAwaiter().GetResult();
                return pausedTriggerGroups.ToList();
            }
            catch (SchedulerException)
            {
            }

            return new List<string>();
        }

        public string GetSchedulerInstanceId()
        {
            string schedulerInstanceId = "";

            try
            {
                schedulerInstanceId = scheduler.SchedulerInstanceId;
            }
            catch (SchedulerException)
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
                schedulerMetaDataInfo = scheduler.GetMetaData().GetAwaiter().GetResult().ToString();
            }
            catch (SchedulerException)
            {
            }

            return schedulerMetaDataInfo;
        }

        public string GetSchedulerName()
        {
            string schedulerName = "";

            try
            {
                schedulerName = scheduler.SchedulerName;
            }
            catch (SchedulerException)
            {
            }

            return schedulerName;
        }

        public ITrigger GetTrigger(string triggerName, string triggerGroupName)
        {
            ITrigger trigger = null;

            try
            {
                trigger = scheduler.GetTrigger(new TriggerKey(triggerName, triggerGroupName)).GetAwaiter().GetResult();
            }
            catch (SchedulerException)
            {
            }

            return trigger;
        }

        public List<string> GetTriggerGroupNames()
        {
            try
            {
                return scheduler.GetTriggerGroupNames().GetAwaiter().GetResult().ToList();
            }
            catch (SchedulerException)
            {
            }
            return new List<string>();
        }

        public List<string> GetTriggerNames(string triggerGroupName)
        {
            try
            {
                var triggerKeys = scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.GroupEquals(triggerGroupName)).GetAwaiter().GetResult();
                return triggerKeys.Select(k => k.Name).ToList();
            }
            catch (SchedulerException)
            {
            }
            return new List<string>();
        }

        public string GetTriggerState(string triggerName, string triggerGroupName)
        {
            string triggerState = "none";

            try
            {
                var state = scheduler.GetTriggerState(new TriggerKey(triggerName, triggerGroupName)).GetAwaiter().GetResult();
                triggerState = TriggerStateToString(state);
            }
            catch (SchedulerException)
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
                scheduler.PauseAll().GetAwaiter().GetResult();
                result = true;
            }
            catch (SchedulerException)
            {
            }

            return result;
        }

        public bool PauseJob(string jobName, string jobGroupName)
        {
            bool result = false;
            try
            {
                scheduler.PauseJob(new JobKey(jobName, jobGroupName)).GetAwaiter().GetResult();
                result = true;
            }
            catch (SchedulerException)
            {
            }
            return result;
        }

        public bool PauseJobGroup(string jobGroupName)
        {
            bool result = false;

            try
            {
                scheduler.PauseJobs(GroupMatcher<JobKey>.GroupEquals(jobGroupName)).GetAwaiter().GetResult();
                result = true;
            }
            catch (SchedulerException)
            {
            }

            return result;
        }

        public bool PauseTrigger(string triggerName, string triggerGroupName)
        {
            bool result = false;

            try
            {
                scheduler.PauseTrigger(new TriggerKey(triggerName, triggerGroupName)).GetAwaiter().GetResult();
                result = true;
            }
            catch (SchedulerException)
            {
            }

            return result;
        }

        public bool PauseTriggerGroup(string triggerGroupName)
        {
            bool result = false;

            try
            {
                scheduler.PauseTriggers(GroupMatcher<TriggerKey>.GroupEquals(triggerGroupName)).GetAwaiter().GetResult();
                result = true;
            }
            catch (SchedulerException)
            {
            }

            return result;
        }

        public bool ResumeAll()
        {
            bool result = false;

            try
            {
                scheduler.ResumeAll().GetAwaiter().GetResult();
                result = true;
            }
            catch (SchedulerException)
            {
            }
            return result;
        }

        public bool ResumeJob(string jobName, string jobGroupName)
        {
            bool result = false;

            try
            {
                scheduler.ResumeJob(new JobKey(jobName, jobGroupName)).GetAwaiter().GetResult();
                result = true;
            }
            catch (SchedulerException)
            {
            }

            return result;
        }

        public bool ResumeJobGroup(string jobGroupName)
        {
            bool result = false;

            try
            {
                scheduler.ResumeJobs(GroupMatcher<JobKey>.GroupEquals(jobGroupName)).GetAwaiter().GetResult();
                result = true;
            }
            catch (SchedulerException)
            {
            }

            return result;
        }

        public bool ResumeTrigger(string triggerName, string triggerGroupName)
        {
            bool result = false;

            try
            {
                scheduler.ResumeTrigger(new TriggerKey(triggerName, triggerGroupName)).GetAwaiter().GetResult();
                result = true;
            }
            catch (SchedulerException)
            {
            }

            return result;
        }

        public bool ResumeTriggerGroup(string triggerGroupName)
        {
            bool result = false;

            try
            {
                scheduler.ResumeTriggers(GroupMatcher<TriggerKey>.GroupEquals(triggerGroupName)).GetAwaiter().GetResult();
                result = true;
            }
            catch (SchedulerException)
            {
            }

            return result;
        }

        public bool ScheduleJob(IJobDetail jobDetail, ITrigger trigger)
        {
            bool result = false;
            try
            {
                scheduler.ScheduleJob(jobDetail, trigger).GetAwaiter().GetResult();
                result = true;
            }
            catch (SchedulerException)
            {
            }
            return result;
        }

        public bool UnscheduleJob(string triggerName, string triggerGroupName)
        {
            bool result = false;
            try
            {
                scheduler.UnscheduleJob(new TriggerKey(triggerName, triggerGroupName)).GetAwaiter().GetResult();
                result = true;
            }
            catch (SchedulerException)
            {
            }
            return result;
        }

        protected string TriggerStateToString(TriggerState triggerState)
        {
            switch (triggerState)
            {
                case TriggerState.Normal:
                    return "normal";
                case TriggerState.Paused:
                    return "paused";
                case TriggerState.Complete:
                    return "complete";
                case TriggerState.Error:
                    return "error";
                case TriggerState.Blocked:
                    return "blocked";
                default:
                    return "none";
            }
        }
    }
}
