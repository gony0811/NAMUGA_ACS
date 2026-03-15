using System;
using System.Collections;
using System.Collections.Generic;
using ACS.Core.Base.Interface;
using ACS.Core.History.Model;
using ACS.Core.Logging;
using ACS.Core.History;

namespace ACS.Scheduling.Awake
{
    public class AwakeTruncateHistoryJob : DailyBackgroundService
    {
        private readonly IHistoryManagerEx _historyManager;
        private readonly IPersistentDao _persistentDao;

        public AwakeTruncateHistoryJob(IHistoryManagerEx historyManager, IPersistentDao persistentDao)
        {
            _historyManager = historyManager;
            _persistentDao = persistentDao;
        }

        /// <summary>실행 시각: 매일 02:00 (새벽).</summary>
        protected override TimeSpan TimeOfDay => new TimeSpan(2, 0, 0);

        protected override void ExecuteOnce()
        {
            try
            {
                IList allParams = _historyManager.GetTruncateParameters();
                if (allParams == null || allParams.Count == 0)
                {
                    logger.Info("No TruncateParameter found in repository — skipping truncation.");
                    return;
                }

                logger.Info($"TruncateHistoryJob started — {allParams.Count} table(s) to process.");

                foreach (TruncateParameterEx param in allParams)
                {
                    try
                    {
                        if (param.PartitioningBase.Equals("DAY"))
                        {
                            truncateHistoryByDayBasePartition(_persistentDao, param);
                        }
                        else if (param.PartitioningBase.Equals("MONTH"))
                        {
                            truncateHistoryByMonthBasePartition(_persistentDao, param);
                        }
                        else
                        {
                            logger.Error($"Unknown partitioning base '{param.PartitioningBase}' for table {param.TableName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Failed to truncate table {param.TableName}: {ex.Message}", ex);
                    }
                }

                logger.Info("TruncateHistoryJob completed.");
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
        }

       private void truncateHistoryByDayBasePartition(IPersistentDao persistentDao, String nativeSql, String partitioningBase, int savePeriod, String truncateSingleOrMulti)
        {
            DateTime calender = DateTime.Now;
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
                    }
                }
            }
        }

        private String getNativeSqlWithReplace(String nativeSql, int partitionId)
        {
            if (nativeSql.Contains("@{partitionId}"))
            {
                return nativeSql.Replace("@{partitionId}", formatTwoDigit(partitionId));
            }
            if (nativeSql.Contains("{partitionId}"))
            {
                return nativeSql.Replace("{partitionId}", partitionId.ToString());
            }
            return nativeSql;
        }

        private void excuteTruncateQuery(IPersistentDao persistentDao, String sql)
        {
            try
            {
                logger.Info("Truncate SQL: " + sql);
                int result = persistentDao.ExecuteUpdate(sql);
                if (result == 0)
                {
                    logger.Info("Truncate succeeded: " + sql);
                }
                else
                {
                    logger.Error("Truncate failed: " + sql);
                }
            }
            catch (Exception e)
            {
                logger.Error("Truncate exception: " + sql, e);
            }
        }

        public static String formatTwoDigit(int i)
        {
            return i.ToString("D2");
        }

        public static int[] getPartitionsByDayBase(DateTime fromtime, DateTime totime)
        {
            int fromMaximumDay = (new DateTime(fromtime.Year, fromtime.Month, 1).AddMonths(1).AddDays(-1)).Day;
            int fromYear = fromtime.Year;
            int fromMonth = fromtime.Month;
            int fromDay = fromtime.Day;

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
            int fromYear = fromtime.Year;
            int fromMonth = fromtime.Month;

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
