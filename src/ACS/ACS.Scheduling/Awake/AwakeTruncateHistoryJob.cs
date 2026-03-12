using Quartz;
using ACS.Framework.Scheduling.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Manager.History;
using ACS.Framework.Base.Interface;
using ACS.Framework.History.Model;
using System.Collections;
using System.Globalization;
using Spring.Scheduling.Quartz;
using ACS.Framework.Logging;


namespace ACS.Scheduling.Awake
{
    public class AwakeTruncateHistoryJob : QuartzJobObject
    {
        protected Logger logger = Logger.GetLogger("SCHEDULING_LOG");

        protected override void ExecuteInternal(JobExecutionContext context)
        {
            try
            {
                //logger.info("truncateHistoryJob will be invoked");

                HistoryManagerExImplement historyManager = (HistoryManagerExImplement)context.MergedJobDataMap.Get("HistoryManager");
                IPersistentDao persistentDao = (IPersistentDao)context.MergedJobDataMap.Get("PersistentDao");
                String tableName = (String)context.MergedJobDataMap.Get("TableName");

                TruncateParameterEx truncateParameterEx = historyManager.GetTruncateParameter(tableName);
                if (truncateParameterEx == null)
                {
                    // logger.error("truncateParameter does not exist in repository, tableName{" + tableName + "}");
                }
                else if (truncateParameterEx.PartitioningBase.Equals("DAY"))
                {
                    truncateHistoryByDayBasePartition(persistentDao, truncateParameterEx);
                }
                else if (truncateParameterEx.PartitioningBase.Equals("MONTH"))
                {
                    truncateHistoryByMonthBasePartition(persistentDao, truncateParameterEx);
                }
                else
                {
                    //logger.error("partitioning base value must be {DAY|MONTH}");
                }
            }
            catch (NullReferenceException nullEx)
            {
                logger.Error(nullEx.StackTrace, nullEx);
                return;
            }
            catch (Exception ex)
            {
                logger.Error(ex.StackTrace, ex);
                return;
            }
        }

       private void truncateHistoryByDayBasePartition(IPersistentDao persistentDao, String nativeSql, String partitioningBase, int savePeriod, String truncateSingleOrMulti)
        {
            DateTime calender = DateTime.Now; 
            //Calendar calendar = new GregorianCalendar();
            if (truncateSingleOrMulti.Equals("SINGLE"))
            {
                DateTime newcalender = calender.AddDays(-savePeriod - 1);
                int partitionId = newcalender.Day;
                String replcedSql = getNativeSqlWithReplace(nativeSql, partitionId);
                excuteTruncateQuery(persistentDao, replcedSql);
            }
            else
            {
                DateTime newcalender = calender.AddDays(-savePeriod);
                DateTime keepingFromTime = newcalender.Date;
                DateTime keepingToTime = DateTime.Now;

                int[] keepingPartitions = getPartitionsByDayBase(keepingFromTime, keepingToTime);
                for (int i = 1; i <= 31; i++)
                {
                    bool keeping = false;
                    for (int j = 0; j < keepingPartitions.Length; j++)
                    {
                        int partitionValue = keepingPartitions[j];
                        if (i == partitionValue)
                        {
                            keeping = true;
                            break;
                        }
                    }
                    if (!keeping)
                    {
                        String replcedSql = getNativeSqlWithReplace(nativeSql, i);
                        excuteTruncateQuery(persistentDao, replcedSql);
                        //Sleep(1000);
                    }
                }
            }
        }

        private void truncateHistoryByDayBasePartition(IPersistentDao persistentDao, TruncateParameterEx truncateParameter)
        {
            truncateHistoryByDayBasePartition(persistentDao, truncateParameter.NativeSql, truncateParameter.PartitioningBase, truncateParameter.SavePeriod, truncateParameter.TruncateSingleOrMulti);
        }

        private void truncateHistoryByMonthBasePartition(IPersistentDao persistentDao, TruncateParameterEx truncateParameter)
        {
            truncateHistoryByMonthBasePartition(persistentDao, truncateParameter.NativeSql, truncateParameter.PartitioningBase, truncateParameter.SavePeriod, truncateParameter.TruncateSingleOrMulti);
        }

        private void truncateHistoryByMonthBasePartition(IPersistentDao persistentDao, String nativeSql, String partitioningBase, int savePeriod, String truncateSingleOrMulti)
        {
            DateTime calendar = DateTime.Now;
            if (truncateSingleOrMulti.Equals("SINGLE"))
            {
                DateTime newcalender = calendar.AddMonths(-savePeriod - 1);
                int partitionId = newcalender.Month + 1;
                String replcedSql = getNativeSqlWithReplace(nativeSql, partitionId);
                excuteTruncateQuery(persistentDao, replcedSql);
            }
            else
            {
                DateTime newcalender = calendar.AddMonths(-savePeriod);
                DateTime keepingFromTime = newcalender.Date;
                DateTime keepingToTime = DateTime.Now;

                int[] keepingPartitions = getPartitionsByMonthBase(keepingFromTime, keepingToTime);
                for (int i = 1; i <= 12; i++)
                {
                    bool keeping = false;
                    for (int j = 0; j < keepingPartitions.Length; j++)
                    {
                        int partitionValue = keepingPartitions[j];
                        if (i == partitionValue)
                        {
                            keeping = true;
                            break;
                        }
                    }
                    if (!keeping)
                    {
                        String replcedSql = getNativeSqlWithReplace(nativeSql, i);
                        excuteTruncateQuery(persistentDao, replcedSql);
                        //Sleep(1000);
                    }
                }
            }
        }

        private String getNativeSqlWithReplace(String nativeSql, int partitionId)
        {
            string renativeSql = string.Empty;
            if (nativeSql.Contains("@{partitionId}"))
            {
                return renativeSql = nativeSql.Replace("@{partitionId}", formatTwoDigit(partitionId));
            }
            if (nativeSql.Contains("{partitionId}"))
            {
                return renativeSql = nativeSql.Replace("{partitionId}", partitionId.ToString());
            }
            //logger.error("property{nativeSql} must contains '@{partitionId}' or '{partitionId}'");
            return nativeSql;
        }

        private void excuteTruncateQuery(IPersistentDao persistentDao, String sql)
        {
            try
            {
                //logger.info("Native SQL{" + sql + "} will be excuted.");
                int result = persistentDao.ExecuteUpdate(sql);
                if (result == 0)
                {
                    //logger.fine("succeeded in truncating, Native SQL{" + sql + "}");
                }
                else
                {
                    //logger.error("failed to truncate, Native SQL{" + sql + "}");
                }
            }
            catch (Exception e)
            {
                //logger.error("failed to truncate, Native SQL{" + sql + "}", e);
            }
        }

        public static String formatTwoDigit(int i)
        {
            if (i < 10)
            {
                return "0" + i.ToString();
            }
            return i.ToString();
        }

        public static int[] getPartitionsByDayBase(DateTime fromtime, DateTime totime)
        {
            int fromMaximumDay = (new DateTime(fromtime.Year, fromtime.Month, 1).AddMonths(1).AddDays(-1)).Day; //fromtime 월의 마지막 날짜
            int fromYear = fromtime.Year;
            int fromMonth = fromtime.Month;
            int fromDay = fromtime.Day;

            //calendar.setTime(totime);
            int toYear = totime.Year;
            int toMonth = totime.Month;
            int toDay = totime.Day;

            List<int> partitions = new List<int>();
            if (fromMonth == toMonth)
            {
                int index = toDay - fromDay;
                for (int i = 0; i <= index; i++)
                {
                    int partition = fromDay + i;
                    partitions.Add(partition);
                }
                return partitions.ToArray();
            }
            if ((toYear - fromYear == 0) && (toMonth - fromMonth == 1) && (toDay - fromDay <= 0))
            {
                int index = fromMaximumDay - fromDay;
                for (int i = 0; i <= index; i++)
                {
                    int partition = fromDay + i;
                    partitions.Add(partition);
                }
                for (int i = 1; i <= toDay; i++)
                {
                    int partition = i;
                    partitions.Add(partition);
                }
                return partitions.ToArray();
            }
            if ((toYear - fromYear == 1) && (toMonth == 1) && (fromMonth == 12) && (toDay - fromDay <= 0))
            {
                int index = fromMaximumDay - fromDay;
                for (int i = 0; i <= index; i++)
                {
                    int partition = fromDay + i;
                    partitions.Add(partition);
                }
                for (int i = 1; i <= toDay; i++)
                {
                    int partition = fromDay + i;
                    partitions.Add(partition);
                }
                return partitions.ToArray();
            }
            return partitions.ToArray();
        }

        public static int[] getPartitionsByMonthBase(DateTime fromtime, DateTime totime)
        {
            int fromMaximumDay = (new DateTime(fromtime.Year, fromtime.Month, 1).AddMonths(1).AddDays(-1)).Day; //fromtime 월의 마지막 날짜
            int fromYear = fromtime.Year;
            int fromMonth = fromtime.Month;

            //calendar.setTime(totime);
            int toYear = totime.Year;
            int toMonth = totime.Month;

            List<int> partitions = new List<int>();
            if (toYear - fromYear == 0)
            {
                int index = toMonth - fromMonth;
                for (int i = 0; i <= index; i++)
                {
                    int partition = fromMonth + i;
                    partitions.Add(partition);
                }
                return partitions.ToArray();
            }
            if (toYear - fromYear == 1)
            {
                if (12 - fromMonth + toMonth > 11)
                {
                    return partitions.ToArray();
                }
                for (int i = fromMonth; i <= 12; i++)
                {
                    int partition = i;
                    partitions.Add(partition);
                }
                for (int i = 1; i <= toMonth; i++)
                {
                    int partition = i;
                    partitions.Add(partition);
                }
            }
            return partitions.ToArray();
        }
    }
}
