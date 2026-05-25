using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Threading.Channels;
using Serilog.Events;
using ACS.Core.Base.Interface;
using ACS.Core.Logging;
using ACS.Core.Logging.Model;
using ACS.Core.Message;

namespace ACS.Core.Logging.Implement
{
    public class LogManagerImpl : ILogManager
    {
        private string loglevel;

        public IPersistentDao PersistentDao { get; set; }
        public ThreadLocal<object> ThreadLocal { get; set; }
        public MessageNode MessageNode { get; set; }

        public int TextSizeForInsert { get; set; }
        public int LargeTextSizeForInsert { get; set; }
        public int LogLevelInt { get; set; }
        public bool UseShortClassNameAtOperationName { get; set; }
        public bool UseAdoDotNetAppender { get; set; }
        public IList SkipLoggingMessages { get; set; }
        public IDictionary UseFriendlyCommunicationMessageNames { get; set; }
        public bool UsePhysicalPartitioningTable { get; set; }
        public bool UseFirstIterationLargeLogMessage { get; set; }

        // 프로세스명(Acs:Process:Name). LogMessage 생성자는 레거시 ConfigurationManager.AppSettings에서
        // 읽으려 하나 .NET 8 appsettings.json 환경에선 항상 null이므로 여기서 주입받아 채운다.
        public string ProcessName { get; set; }

        // 비동기 백그라운드 큐 설정/상태
        public int QueueCapacity { get; set; } = 10000;
        public int BatchSize { get; set; } = 200;

        private Channel<LogMessage> _channel;
        private Task _consumerTask;
        private CancellationTokenSource _cts;

        public string LogLevel {
            get { return loglevel; }
            set
            {
                loglevel = value;

                if (loglevel.Equals("DEBUG")) this.LogLevelInt = 10000;
                else if (loglevel.Equals("INFO")) this.LogLevelInt = 20000;
                else if (loglevel.Equals("FINE")) this.LogLevelInt = 20010;
                else if (loglevel.Equals("WELL")) this.LogLevelInt = 20020;
                else if (loglevel.Equals("WARN")) this.LogLevelInt = 30000;
                else if (loglevel.Equals("ERROR")) this.LogLevelInt = 40000;
                else if (loglevel.Equals("FATAL")) this.LogLevelInt = 50000;
                else
                {
                    loglevel = "WELL";
                    LogLevelInt = 20020;
                }
            }
        }

        public LogManagerImpl()
        {
            ThreadLocal = new ThreadLocal<object>();
            UseFriendlyCommunicationMessageNames = new Dictionary<string, string>();
            TextSizeForInsert = 3000;
            LargeTextSizeForInsert = 4000;
        }

        public LargeLogMessage CreateLargeLogMessageInstance(int date)
        {
            if (!this.UsePhysicalPartitioningTable)
            {
                return new LargeLogMessage();
            }
            return new LargeLogMessage(date);
        }

        public void CreateLogMessage(LogEvent logEvent)
        {
            try
            {
                if (logEvent.Properties.TryGetValue("LogMessage", out var logMessageValue) &&
                    logMessageValue is ScalarValue scalarValue &&
                    scalarValue.Value is LogMessage logMessage)
                {
                    if (this.SkipLoggingMessages != null && this.SkipLoggingMessages.Contains(logMessage.MessageName))
                    {
                        return;
                    }

                    string transactionId = logMessage.TransactionId;

                    if (string.IsNullOrEmpty(transactionId) && this.ThreadLocal != null)
                    {
                        transactionId = this.ThreadLocal.Value != null ? this.ThreadLocal.Value.ToString() : "";
                        logMessage.TransactionId = transactionId;
                    }
                    // UTC로 명시 변환: DateTimeOffset.DateTime은 Kind=Unspecified(로컬 벽시계)라
                    // DAO가 변환 없이 UTC로 라벨링 → 로컬 시각이 UTC로 오기록되는 문제를 방지.
                    logMessage.Time = logEvent.Timestamp.UtcDateTime;
                    logMessage.ThreadName = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString();

                    // Extract method name from source context if available
                    if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
                    {
                        logMessage.OperationName = sourceContext.ToString().Trim('"');
                    }

                    logMessage.LogLevel = logEvent.Level.ToString();
                    EnrichFromAmbientContext(logMessage);
                    ChangeCommunicationMessageName(logMessage);

                    Enqueue(logMessage);
                }
            }
            catch(Exception e)
            {
                LogInternalError(e, "CreateLogMessage(LogEvent) 실패");
            }
        }

        protected void ChangeCommunicationMessageName(LogMessage logMessage)
        {
            string communicationMessageName = logMessage.CommunicationMessageName;
            // 통신 메시지명이 없는 일반 로그는 null/빈 문자열 → Dictionary.Contains(null)이 ArgumentNullException을
            // 던지므로 반드시 먼저 걸러낸다(이 가드 누락이 모든 일반 로그의 DB 적재를 막던 원인).
            if (string.IsNullOrEmpty(communicationMessageName) || this.UseFriendlyCommunicationMessageNames == null)
            {
                return;
            }
            if (this.UseFriendlyCommunicationMessageNames.Contains(communicationMessageName))
            {
                logMessage.CommunicationMessageName = (string)UseFriendlyCommunicationMessageNames[communicationMessageName];
            }
        }

        public void CreateLogMessage(LogMessage logMessage, string threadName, string operationName, string logLevel)
        {
            try
            {
                if(this.SkipLoggingMessages != null && this.SkipLoggingMessages.Contains(logMessage.MessageName))
                {
                    return;
                }

                string transactionId = logMessage.TransactionId;
                if(string.IsNullOrEmpty(transactionId) && this.ThreadLocal != null)
                {
                    transactionId = this.ThreadLocal.Value != null ? this.ThreadLocal.Value.ToString() : "";

                    logMessage.TransactionId = transactionId;
                }

                logMessage.Time = DateTime.Now;
                logMessage.ThreadName = threadName;
                logMessage.OperationName = operationName;
                logMessage.LogLevel = logLevel;
                EnrichFromAmbientContext(logMessage);
                ChangeCommunicationMessageName(logMessage);

                Enqueue(logMessage);
            }
            catch (Exception e)
            {
                LogInternalError(e, "CreateLogMessage 실패");
            }
        }

        /// <summary>
        /// 백그라운드 소비자 Task를 기동한다. UseAdoDotNetAppender가 false면 동작하지 않는다.
        /// Start 호출 전에 들어온 로그는 Enqueue가 동기 저장으로 폴백한다.
        /// </summary>
        public void Start()
        {
            if (!this.UseAdoDotNetAppender)
            {
                return;
            }
            if (_channel != null)
            {
                return; // 이미 기동됨
            }

            int capacity = QueueCapacity <= 0 ? 10000 : QueueCapacity;
            _channel = Channel.CreateBounded<LogMessage>(new BoundedChannelOptions(capacity)
            {
                // 로깅 폭주가 시스템을 죽이지 않도록 큐가 가득 차면 새 로그를 버린다.
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false
            });
            _cts = new CancellationTokenSource();
            _consumerTask = Task.Run(() => ConsumeAsync(_cts.Token));
        }

        /// <summary>
        /// 큐를 닫고 소비자가 잔여 로그를 모두 적재할 때까지 대기한다(타임아웃 포함).
        /// </summary>
        public void Flush()
        {
            try
            {
                _channel?.Writer.TryComplete();
                _consumerTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception e)
            {
                LogInternalError(e, "Flush 실패");
            }
        }

        /// <summary>
        /// LogManager 내부 오류를 기록한다. 반드시 Serilog(파일/콘솔 sink)로만 남긴다 —
        /// ACS <see cref="Logger"/> 래퍼를 쓰면 다시 LogManager로 들어가 무한 재귀하므로 금지.
        /// </summary>
        private static void LogInternalError(Exception e, string context)
        {
            try
            {
                Serilog.Log.ForContext("LoggerName", "ACS.Core.Logging.LogManager")
                    .Error(e, "[LogManager] {Context}", context);
            }
            catch { /* 진단 로깅 실패는 무시 */ }
        }

        private static string Trunc(string s, int max)
        {
            return (s != null && s.Length > max) ? s.Substring(0, max) : s;
        }

        /// <summary>
        /// 주변(ambient) 로그 컨텍스트(LogContext.Current)로 빈 필드만 보강한다.
        /// 명시적으로 채워진 값은 덮어쓰지 않는다.
        /// 반드시 로그를 생성하는 비즈니스 스레드(CreateLogMessage)에서 호출해야 한다 —
        /// AsyncLocal은 백그라운드 소비자 스레드에서는 무효이기 때문.
        /// </summary>
        private void EnrichFromAmbientContext(LogMessage m)
        {
            var ctx = LogContext.Current;
            if (ctx == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(m.TransactionId)) m.TransactionId = ctx.TransactionId;
            if (string.IsNullOrEmpty(m.MessageName)) m.MessageName = ctx.MessageName;
            if (string.IsNullOrEmpty(m.CommunicationMessageName)) m.CommunicationMessageName = ctx.CommunicationMessageName;
            if (string.IsNullOrEmpty(m.TransportCommandId)) m.TransportCommandId = ctx.TransportCommandId;
            if (string.IsNullOrEmpty(m.CarrierName)) m.CarrierName = ctx.CarrierName;
            if (string.IsNullOrEmpty(m.MachineName)) m.MachineName = ctx.MachineName;
            if (string.IsNullOrEmpty(m.UnitName)) m.UnitName = ctx.UnitName;
        }

        // NA_L_LOGMESSAGE 컬럼 한도에 맞춰 문자열을 잘라 varchar 초과로 배치 전체가 롤백되는 것을 막는다.
        private void NormalizeLengths(LogMessage m)
        {
            m.TransactionId = Trunc(m.TransactionId, 64);
            m.ThreadName = Trunc(m.ThreadName, 64);
            m.OperationName = Trunc(m.OperationName, 128);
            m.ProcessName = Trunc(m.ProcessName, 64);
            m.MessageName = Trunc(m.MessageName, 64);
            m.CommunicationMessageName = Trunc(m.CommunicationMessageName, 64);
            m.TransportCommandId = Trunc(m.TransportCommandId, 64);
            m.CarrierName = Trunc(m.CarrierName, 64);
            m.MachineName = Trunc(m.MachineName, 64);
            m.UnitName = Trunc(m.UnitName, 64);
            m.LogLevel = Trunc(m.LogLevel, 20);
        }

        private void Enqueue(LogMessage logMessage)
        {
            var channel = _channel;
            if (channel != null)
            {
                channel.Writer.TryWrite(logMessage); // 큐가 가득 차면 DropWrite로 즉시 버려짐
            }
            else
            {
                // Start() 이전에 들어온 초기 로그는 동기 저장으로 폴백한다.
                var batch = new List<object>();
                PrepareForPersist(logMessage, batch);
                try { this.PersistentDao.SaveAll(batch); }
                catch (Exception e) { LogInternalError(e, "동기 폴백 저장 실패"); }
            }
        }

        private async Task ConsumeAsync(CancellationToken ct)
        {
            var reader = _channel.Reader;
            try
            {
                // 채널이 Complete되고 비워질 때까지 반복 — Flush 시 잔여 로그까지 모두 드레인된다.
                while (await reader.WaitToReadAsync(ct).ConfigureAwait(false))
                {
                    var batch = new List<object>();
                    while (batch.Count < BatchSize && reader.TryRead(out var msg))
                    {
                        try { PrepareForPersist(msg, batch); }
                        catch (Exception e) { LogInternalError(e, "PrepareForPersist 실패"); }
                    }

                    if (batch.Count > 0)
                    {
                        try { this.PersistentDao.SaveAll(batch); }
                        catch (Exception e) { LogInternalError(e, "배치 저장 실패 (batch save failed)"); }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { LogInternalError(e, "소비자 루프 오류"); }
        }

        /// <summary>
        /// LogMessage를 저장 대상 배치에 추가한다. text가 컬럼 한도를 넘으면 LargeLogMessage로 분할한다.
        /// </summary>
        private void PrepareForPersist(LogMessage logMessage, List<object> batch)
        {
            // 레거시 ConfigurationManager.AppSettings로는 .NET 8에서 못 채워지므로 여기서 보강.
            if (string.IsNullOrEmpty(logMessage.ProcessName))
            {
                logMessage.ProcessName = this.ProcessName;
            }

            NormalizeLengths(logMessage);

            string text = logMessage.Text;
            if (string.IsNullOrEmpty(text) || text.Length <= this.TextSizeForInsert)
            {
                batch.Add(logMessage);
                return;
            }

            int size = text.Length;
            int fieldSize = this.LargeTextSizeForInsert;
            int quotient = size / fieldSize;

            for (int index = 0; index <= quotient; index++)
            {
                int startIndex = index * fieldSize;
                if (startIndex >= size)
                {
                    break;
                }
                int length = Math.Min(fieldSize, size - startIndex);
                string largeText = text.Substring(startIndex, length);

                if (index == 0)
                {
                    logMessage.Text = this.UseFirstIterationLargeLogMessage ? largeText : "";
                    batch.Add(logMessage);
                }

                LargeLogMessage largeLogMessage = CreateLargeLogMessageInstance(logMessage.PartitionId);
                largeLogMessage.Sequence = index;
                largeLogMessage.LogMessageId = logMessage.Id;
                largeLogMessage.LargeText = largeText;
                batch.Add(largeLogMessage);
            }
        }

        public LogMessage CreateLogMessageInstance()
        {
            if (!this.UsePhysicalPartitioningTable)
            {
                return new LogMessage();
            }
            int date = DateTime.Now.Day;
            switch (date)
            {
                case 1:
                    return new LogMessage(date);
                case 2:
                    return new LogMessage(date);
                case 3:
                    return new LogMessage(date);
                case 4:
                    return new LogMessage(date);
                case 5:
                    return new LogMessage(date);
                case 6:
                    return new LogMessage(date);
                case 7:
                    return new LogMessage(date);
                case 8:
                    return new LogMessage(date);
                case 9:
                    return new LogMessage(date);
                case 10:
                    return new LogMessage(date);
                case 11:
                    return new LogMessage(date);
                case 12:
                    return new LogMessage(date);
                case 13:
                    return new LogMessage(date);
                case 14:
                    return new LogMessage(date);
                case 15:
                    return new LogMessage(date);
                case 16:
                    return new LogMessage(date);
                case 17:
                    return new LogMessage(date);
                case 18:
                    return new LogMessage(date);
                case 19:
                    return new LogMessage(date);
                case 20:
                    return new LogMessage(date);
                case 21:
                    return new LogMessage(date);
                case 22:
                    return new LogMessage(date);
                case 23:
                    return new LogMessage(date);
                case 24:
                    return new LogMessage(date);
                case 25:
                    return new LogMessage(date);
                case 26:
                    return new LogMessage(date);
                case 27:
                    return new LogMessage(date);
                case 28:
                    return new LogMessage(date);
                case 29:
                    return new LogMessage(date);
                case 30:
                    return new LogMessage(date);
                case 31:
                    return new LogMessage(date);
            }
            return new LogMessage(date);
        }

        public int GetLargeTextSizeForInsert()
        {
            return LargeTextSizeForInsert;
        }

        public MessageNode GetMessageNode()
        {
            return MessageNode;
        }

        public IPersistentDao GetPersistentDao()
        {
            return PersistentDao;
        }

        public bool IsGreaterOrEqual(int logLevel)
        {
            return logLevel >= this.LogLevelInt;
        }

        public bool IsUseAdoDotNetAppender()
        {
            return this.UseAdoDotNetAppender;
        }

        public bool IsUseShortClassNameAtOperationName()
        {
            return UseShortClassNameAtOperationName;
        }
    }
}
