using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using ACS.Framework.Message.Model;
using ACS.Framework.Logging.Model;
using ACS.Framework.Resource.Model.Factory.Unit;
using ACS.Framework.Resource.Model.Factory.Machine;
using ACS.Framework.Resource.Model.Factory.Zone;
using ACS.Framework.Material.Model;
using ACS.Framework.Transfer.Model;
using ACS.Framework.History.Model;
using Serilog;
using Serilog.Events;

namespace ACS.Framework.Logging
{
    public class Logger
    {
        protected static string XPATH_NAME_CARRIER = "/MESSAGE/DATA/CARRIERID | /MESSAGE/DATA/CARRIERNAME | /MESSAGE/DATA//CARRIERID | /MESSAGE/DATA//CARRIERNAME";
        protected static string XPATH_NAME_COMMANDID = "/MESSAGE/DATA/COMMANDID | /MESSAGE/DATA//COMMANDID";
        protected static string XPATH_NAME_CARRIERLOC = "/MESSAGE/DATA/CARRIERLOC | /MESSAGE/DATA//CARRIERLOC";
        private static string FQCN = typeof(Logger).Name + ".";
        private ILogger log;
        private bool isConfigured = true; // Serilog는 항상 구성됨

        public string Name { get; set; }
        public ILogManager logManager { get; set; }
        public ILogger Log
        {
            get
            {
                if (log == null)
                {
                    log = Serilog.Log.ForContext("LoggerName", Name);
                }

                return log;
            }

            set
            {
                log = value;
            }
        }

        protected Logger(string name)
        {
            Name = name;
        }

        public static Logger GetLogger(string name)
        {
            Logger logger = new Logger(name)
            {
                Log = Serilog.Log.ForContext("LoggerName", name)
            };
            return logger;
        }

        public static Logger GetLogger(Type type)
        {
            Logger logger = new Logger(type.FullName)
            {
                Log = Serilog.Log.ForContext("LoggerName", type.FullName)
            };

            return logger;
        }

        public bool IsDebugEnabled => Log.IsEnabled(LogEventLevel.Debug);
        public bool IsInfoEnabled => Log.IsEnabled(LogEventLevel.Information);

        public void Debug(object message)
        {
            if (!isConfigured) return;

            if (Log.IsEnabled(LogEventLevel.Debug))
            {
                Log.Debug("{Message}", message);
            }
        }

        public void Debug(object message, Exception e)
        {
            if (!isConfigured) return;

            if (Log.IsEnabled(LogEventLevel.Debug))
            {
                Log.Debug(e, "{Message}", message);
            }
        }

        public void Info(object message)
        {
            if (!isConfigured) return;

            if (Log.IsEnabled(LogEventLevel.Information))
            {
                Log.Information("{Message}", message);
            }

            SaveMessageToDatabase(message, 20000, "INFO", null);
        }

        public void Info(object message, Exception e)
        {
            if (!isConfigured) return;

            if (Log.IsEnabled(LogEventLevel.Information))
            {
                Log.Information(e, "{Message}", message);
            }
            SaveMessageToDatabase(GetExceptionMessage(message, e), 20000, "INFO", null);
        }

        public void Fine(object message)
        {
            if (!isConfigured) return;

            // Fine은 Information과 Debug 사이의 레벨 (Verbose로 매핑)
            if (Log.IsEnabled(LogEventLevel.Verbose))
            {
                Log.Verbose("{Message}", message);
            }

            SaveMessageToDatabase(message, 20010, "FINE", null);
        }

        public void Fine(object message, bool saveToDatabase)
        {
            if (!isConfigured) return;

            if (Log.IsEnabled(LogEventLevel.Verbose))
            {
                Log.Verbose("{Message}", message);
            }

            if(saveToDatabase)
            {
                SaveMessageToDatabase(message, 20010, "FINE", null);
            }
        }

        public void Fine(object message, Exception ex)
        {
            if (!isConfigured) return;

            if (Log.IsEnabled(LogEventLevel.Verbose))
            {
                Log.Verbose(ex, "{Message}", message);
            }

            SaveMessageToDatabase(GetExceptionMessage(message, ex), 20010, "FINE", null);
        }

        public void Warn(object message)
        {
            if (!isConfigured) return;

            if (Log.IsEnabled(LogEventLevel.Warning))
            {
                Log.Warning("{Message}", message);
            }

            SaveMessageToDatabase(message, 30000, "WARN", null);
        }

        public void Warn(object message, Exception ex)
        {
            if (!isConfigured) return;

            if (Log.IsEnabled(LogEventLevel.Warning))
            {
                Log.Warning(ex, "{Message}", message);
            }
            SaveMessageToDatabase(GetExceptionMessage(message, ex), 30000, "WARN", null);
        }

        public void Error(object message)
        {
            if (!isConfigured) return;

            if (Log.IsEnabled(LogEventLevel.Error))
            {
                Log.Error("{Message}", message);
            }

            SaveMessageToDatabase(message, 40000, "ERROR", null);
        }

        public void Error(object message, Exception ex)
        {
            if (!isConfigured) return;

            if (Log.IsEnabled(LogEventLevel.Error))
            {
                Log.Error(ex, "{Message}", message);
            }
            SaveMessageToDatabase(GetExceptionMessage(message, ex), 40000, "ERROR", null);
        }

        public void Fatal(object message)
        {
            if (!isConfigured) return;

            if (Log.IsEnabled(LogEventLevel.Fatal))
            {
                Log.Fatal("{Message}", message);
            }

            SaveMessageToDatabase(message, 50000, "FATAL", null);
        }

        public void Fatal(object message, Exception ex)
        {
            if (!isConfigured) return;

            if (Log.IsEnabled(LogEventLevel.Fatal))
            {
                Log.Fatal(ex, "{Message}", message);
            }
            SaveMessageToDatabase(GetExceptionMessage(message, ex), 50000, "FATAL", null);
        }

        public void Debug(object message, string carrierName, string commandId, string machineName, string unitName)
        {
            Debug(message, "", "", carrierName, commandId, machineName, unitName);
        }

        public void Debug(object message, string messageName, string carrierName, string commandId, string machineName, string unitName)
        {
            Debug(message, "", messageName, carrierName, commandId, machineName, unitName);
        }

        public void Debug(object message, string transactionId, string messageName, string carrierName, string commandId, string machineName, string unitName)
        {
            LogMessage logMessage = CreateLogMessage(message, transactionId, messageName, carrierName, commandId, machineName, unitName);

            Debug(logMessage);
        }

        public void Debug(object message, string carrierName, string commandId, string machineName, string unitName, Exception ex)
        {
            Debug(message, "", "", carrierName, commandId, machineName, unitName, ex);
        }

        public void Debug(object message, string messageName, string carrierName, string commandId, string machineName, string unitName, Exception ex)
        {
            Debug(message, "", messageName, carrierName, commandId, machineName, unitName, ex);
        }

        public void Debug(object message, string transactionId, string messageName, string carrierName, string commandId, string machineName, string unitName, Exception ex)
        {
            LogMessage logMessage = CreateLogMessage(message, transactionId, messageName, carrierName, commandId, machineName, unitName);

            Debug(logMessage, ex);
        }

        public void Info(object message, TransferMessageEx transferMessage)
        {
            Info(message, "", transferMessage.MessageName, transferMessage.CarrierName, transferMessage.TransportCommandId, transferMessage.CurrentMachineName, transferMessage.CurrentUnitName);
        }

        public void Info(object message, AbstractMessage msg)
        {
            Info(message, msg.MessageName, "", "", msg.CurrentMachineName, "");
        }

        public void Info(object message, BaseMessage msg)
        {
            Info(message, msg.MessageName, msg.CarrierName, msg.TransportCommandId, msg.CurrentMachineName, msg.CurrentUnitName);
        }

        public void Info(object message, Unit unit)
        {
            Info(message, "", "", "", "", unit.MachineName, unit.Name);
        }

        public void Info(object message, Machine machine)
        {
            Info(message, "", "", "", "", machine.Name, "");
        }

        public void Info(object message, Carrier carrier)
        {
            Info(message, "", "", carrier.Name, "", carrier.MachineName, carrier.UnitName);
        }

        public void Info(object message, Substrate substrate)
        {
            Info(message, "", "", substrate.Name, "", substrate.MachineName, substrate.UnitName);
        }

        public void Info(object message, TransportJob transportJob)
        {
            Info(message, "", "", transportJob.CarrierName, transportJob.Id, transportJob.SourceMachineName, transportJob.SourceUnitName);
        }

        public void Info(object message, string machineName, string unitName)
        {
            Info(message, "", "", "", "", machineName, unitName);
        }

        public void Info(object message, Zone zone)
        {
            Info(message, "", "", "", "", zone.MachineName, "");
        }

        public void Info(object message, TransportCommandHistoryEx transportCommandHistory)
        {
            Info(message, "", transportCommandHistory.CarrierId, transportCommandHistory.Id, transportCommandHistory.Source, transportCommandHistory.PortId);
        }

        public void Info(object message, VehicleHistoryEx vehicleHistory)
        {
            Info(message, "", vehicleHistory.CarrierType, vehicleHistory.TransportCommandId, vehicleHistory.VehicleId, vehicleHistory.VehicleDestNodeId);
        }

        public void Info(object message, AlarmHistory alarmHistory)
        {
            Info(message, "", "", alarmHistory.TransportCommandId, alarmHistory.MachineName, alarmHistory.UnitName);
        }

        public void Info(object message, AlarmReportHistory alarmReportHistory)
        {
            Info(message, "", "", "", alarmReportHistory.MachineName, alarmReportHistory.UnitName);
        }

        public void Info(object message, String carrierName, String commandId, String machineName, String unitName)
        {
            Info(message, "", "", carrierName, commandId, machineName, unitName);
        }

        public void Info(Object message, String messageName, String carrierName, String commandId, String machineName, String unitName)
        {
            Info(message, "", messageName, carrierName, commandId, machineName, unitName);
        }

        public void Info(Object message, String transactionId, String messageName, String carrierName, String commandId, String machineName, String unitName)
        {
            LogMessage logMessage = CreateLogMessage(message, transactionId, messageName, carrierName, commandId, machineName, unitName);

            Info(logMessage);
        }

        public void Info(Object message, String carrierName, String commandId, String machineName, String unitName, Exception ex)
        {
            Info(message, "", "", carrierName, commandId, machineName, unitName, ex);
        }

        public void Info(Object message, String messageName, String carrierName, String commandId, String machineName, String unitName, Exception ex)
        {
            Info(message, "", messageName, carrierName, commandId, machineName, unitName, ex);
        }

        public void Info(Object message, String transactionId, String messageName, String carrierName, String commandId, String machineName, String unitName, Exception ex)
        {
            LogMessage logMessage = CreateLogMessage(message, transactionId, messageName, carrierName, commandId, machineName, unitName);

            Info(logMessage, ex);
        }
        public void Fine(Object message, String carrierName)
        {
            Fine(message, "", carrierName, "", "", "");
        }

        public void Fine(Object message, String communicationMessageName, bool useCommunicationMessageName)
        {
            Fine(message, "", "", "", "", "", "", communicationMessageName);
        }

        public void Fine(Object message, TransferMessageEx transferMessage)
        {
            String communicationMessageName = transferMessage.MessageName;
            Fine(message, transferMessage.TransactionId, transferMessage.MessageName, transferMessage.CarrierName, transferMessage.TransportCommandId,
              transferMessage.CurrentMachineName, transferMessage.CurrentUnitName, communicationMessageName);
        }

        public void Fine(Object message, BaseMessage msg)
        {
            String communicationMessageName = msg.MessageName;
            Fine(message, msg.TransactionId, msg.MessageName, msg.CarrierName, msg.TransportCommandId, msg.CurrentMachineName, msg.CurrentUnitName, communicationMessageName);
        }

        public void Fine(Object message, AbstractMessage abstractMessage)
        {
            String communicationMessageName = abstractMessage.MessageName;
            Fine(message, abstractMessage.TransactionId, abstractMessage.MessageName, "", "", abstractMessage.CurrentMachineName, "", communicationMessageName);
        }

        public void Fine(Object message, String machineName, String unitName)
        {
            Fine(message, "", "", "", machineName, unitName);
        }


        public void Fine(Object message, Carrier carrier)
        {
            Fine(message, "", carrier.Name, "", carrier.MachineName, carrier.UnitName);
        }

        public void Fine(Object message, Zone zone)
        {
            Fine(message, "", "", "", zone.MachineName, "");
        }

        public void Fine(Object message, VehicleHistoryEx vehicleHistory)
        {
            Fine(message, "", vehicleHistory.CarrierType, vehicleHistory.TransportCommandId, vehicleHistory.VehicleId, vehicleHistory.VehicleDestNodeId);
        }


        public void Fine(Object message, AlarmHistory alarmHistory)
        {
            Fine(message, "", "", alarmHistory.TransportCommandId, alarmHistory.MachineName, alarmHistory.UnitName);
        }

        public void Fine(Object message, AlarmReportHistory alarmReportHistory)
        {
            Fine(message, "", "", "", alarmReportHistory.MachineName, alarmReportHistory.UnitName);
        }
        public void Fine(Object message, String carrierName, String machineName, String unitName)
        {
            Fine(message, "", carrierName, "", machineName, unitName);
        }

        public void Fine(Object message, String carrierName, String commandId, String machineName, String unitName)
        {
            Fine(message, "", carrierName, commandId, machineName, unitName);
        }

        public void Fine(Object message, String commandId, Unit unit)
        {
            Fine(message, "", "", commandId, unit.MachineName, unit.Name);
        }

        public void Fine(Object message, String carrierName, String commandId, Unit unit)
        {
            Fine(message, "", carrierName, commandId, unit.MachineName, unit.Name);
        }

        public void Fine(Object message, String messageName, String carrierName, String commandId, String machineName, String unitName)
        {
            Fine(message, "", messageName, carrierName, commandId, machineName, unitName);
        }

        public void Fine(Object message, String transactionId, String messageName, String carrierName, String commandId, String machineName, String unitName)
        {
            LogMessage logMessage = CreateLogMessage(message, transactionId, messageName, carrierName, commandId, machineName, unitName);
            Fine(logMessage);
        }

        public void Fine(Object message, String transactionId, String messageName, String carrierName, String commandId, String machineName, String unitName, String communicationMessageName)
        {
            LogMessage logMessage = CreateLogMessage(message, "", transactionId, messageName, carrierName, commandId, machineName, unitName, communicationMessageName);
            Fine(logMessage);
        }

        public void Fine(Object message, String messageName, String carrierName, String machineName, String unitName, Exception ex)
        {
            Fine(message, "", messageName, carrierName, "", machineName, unitName, ex);
        }

        public void Fine(Object message, String messageName, String carrierName, String commandId, String machineName, String unitName, Exception ex)
        {
            Fine(message, "", messageName, carrierName, commandId, machineName, unitName, ex);
        }

        public void Fine(Object message, String transactionId, String messageName, String carrierName, String commandId, String machineName, String unitName, Exception ex)
        {
            LogMessage logMessage = CreateLogMessage(message, transactionId, messageName, carrierName, commandId, machineName, unitName);

            Fine(logMessage, ex);
        }



        public void Well(Object message, BaseMessage msg)
        {
            String communicationMessageName = msg.MessageName;
            Well(message, msg.TransactionId, msg.MessageName, msg.CarrierName, msg.TransportCommandId, msg.CurrentMachineName, msg.CurrentUnitName, communicationMessageName);
        }

        public void Well(Object message, BaseMessage msg, String communicationMessageName)
        {
            Well(message, msg.TransactionId, msg.MessageName, msg.CarrierName, msg.TransportCommandId, msg.CurrentMachineName, msg.CurrentUnitName, communicationMessageName);
        }

        public void Well(Object message, AbstractMessage msg)
        {
            String communicationMessageName = msg.MessageName;
            Well(message, msg.TransactionId, msg.MessageName, "", "", msg.CurrentMachineName, "", communicationMessageName);
        }

        public void Well(Object message, AbstractMessage msg, String communicationMessageName)
        {
            Well(message, msg.TransactionId, msg.MessageName, "", "", msg.CurrentMachineName, "", communicationMessageName);
        }

        public void Well(Object message, Unit unit)
        {
            if (unit != null)
            {
                Well(message, "", "", "", unit.MachineName, unit.Name);
            }
            else
            {
                Well(message, "", "", "", "", "");
            }
        }

        public void Well(Object message, Unit unit, String messageName)
        {
            if (unit != null)
            {
                Well(message, "", messageName, "", "", unit.MachineName, unit.Name, messageName);
            }
            else
            {
                Well(message, "", messageName, "", "", "", "", messageName);
            }
        }


        public void Well(Object message, TransportCommand transportCommand)
        {
            if (transportCommand != null)
            {
                Well(message, "", transportCommand.CarrierName, transportCommand.TransportCommandId, transportCommand.FromMachineName, transportCommand.FromUnitName);
            }
            else
            {
                Well(message, "", "", "", "", "");
            }
        }

        public void Well(Object message, TransportCommand transportCommand, String messageName)
        {
            if (transportCommand != null)
            {
                Well(message, "", messageName, transportCommand.CarrierName, transportCommand.TransportCommandId, transportCommand.FromMachineName, transportCommand.FromUnitName, messageName);
            }
            else
            {
                Well(message, "", messageName, "", "", "", "", messageName);
            }
        }

        public void Well(Object message, String carrierName, String commandId, String machineName, String unitName)
        {
            Well(message, "", "", carrierName, commandId, machineName, unitName);
        }

        public void Well(Object message, String messageName, String carrierName, String commandId, String machineName, String unitName)
        {
            Well(message, "", messageName, carrierName, commandId, machineName, unitName);
        }

        public void Well(Object message, String transactionId, String messageName, String carrierName, String commandId, String machineName, String unitName)
        {
            LogMessage logMessage = CreateLogMessage(message, transactionId, messageName, carrierName, commandId, machineName, unitName);
            Info(logMessage);
        }

        public void Well(Object message, String transactionId, String messageName, String carrierName, String commandId, String machineName, String unitName, String communicationMessageName)
        {
            LogMessage logMessage = CreateLogMessage(message, "", transactionId, messageName, carrierName, commandId, machineName, unitName, communicationMessageName);
            Info(logMessage);
        }



        public void Warn(Object message, TransferMessageEx transferMessage)
        {
            String communicationMessageName = transferMessage.MessageName;
            Warn(message, "", transferMessage.MessageName, transferMessage.CarrierName, transferMessage.TransportCommandId,
              transferMessage.CurrentMachineName, transferMessage.CurrentUnitName, communicationMessageName);
        }

        public void Warn(Object message, TransferMessageEx transferMessage, String communicationMessageName)
        {
            Warn(message, "", transferMessage.MessageName, transferMessage.CarrierName, transferMessage.TransportCommandId,
              transferMessage.CurrentMachineName, transferMessage.CurrentUnitName, communicationMessageName);
        }

        public void Warn(Object message, AbstractMessage msg)
        {
            String communicationMessageName = msg.MessageName;
            Warn(message, "", msg.MessageName, "", "", msg.CurrentMachineName, "", communicationMessageName);
        }

        public void Warn(Object message, BaseMessage msg)
        {
            String communicationMessageName = msg.MessageName;
            Warn(message, "", msg.MessageName, msg.CarrierName, msg.TransportCommandId, msg.CurrentMachineName, msg.CurrentUnitName, communicationMessageName);
        }

        public void Warn(Object message, String carrierName)
        {
            Warn(message, "", "", carrierName, "", "", "");
        }

        public void Warn(Object message, String machineName, String unitName)
        {
            Warn(message, "", "", "", "", machineName, unitName);
        }

        public void Warn(Object message, String carrierName, String machineName, String unitName)
        {
            Warn(message, "", "", carrierName, "", machineName, unitName);
        }

        public void Warn(Object message, String carrierName, String commandId, String machineName, String unitName)
        {
            Warn(message, "", "", carrierName, commandId, machineName, unitName);
        }

        public void warn(Object message, String messageName, String carrierName, String commandId, String machineName, String unitName)
        {
            Warn(message, "", messageName, carrierName, commandId, machineName, unitName);
        }

        public void Warn(Object message, String transactionId, String messageName, String carrierName, String commandId, String machineName, String unitName)
        {
            LogMessage logMessage = CreateLogMessage(message, transactionId, messageName, carrierName, commandId, machineName, unitName);
            Warn(logMessage);
        }

        public void Warn(Object message, String transactionId, String messageName, String carrierName, String commandId, String machineName, String unitName, String communicationMessageName)
        {
            LogMessage logMessage = CreateLogMessage(message, "", transactionId, messageName, carrierName, commandId, machineName, unitName, communicationMessageName);
            Warn(logMessage);
        }

        public void Warn(Object message, String carrierName, String commandId, String machineName, String unitName, Exception ex)
        {
            Warn(message, "", "", carrierName, commandId, machineName, unitName, ex);
        }

        public void Warn(Object message, String messageName, String carrierName, String commandId, String machineName, String unitName, Exception ex)
        {
            Warn(message, "", messageName, carrierName, commandId, machineName, unitName, ex);
        }

        public void Warn(Object message, String transactionId, String messageName, String carrierName, String commandId, String machineName, String unitName, Exception ex)
        {
            LogMessage logMessage = CreateLogMessage(message, transactionId, messageName, carrierName, commandId, machineName, unitName);

            Warn(logMessage, ex);
        }

        public void Warn(Object message, Unit unit)
        {
            Warn(message, "", "", "", "", unit.MachineName, unit.Name);
        }

        public void Error(Object message, TransferMessageEx transferMessage)
        {
            String communicationMessageName = transferMessage.MessageName;
            Error(message, "", transferMessage.MessageName, transferMessage.CarrierName,
              transferMessage.TransportCommandId, transferMessage.CurrentMachineName, transferMessage.CurrentUnitName, communicationMessageName);
        }

        public void Error(Object message, BaseMessage msg)
        {
            String communicationMessageName = msg.MessageName;
            Error(message, "", msg.MessageName, msg.CarrierName, msg.TransportCommandId, msg.CurrentMachineName, msg.CurrentUnitName, communicationMessageName);
        }

        public void Error(Object message, String carrierName, String commandId, String machineName, String unitName)
        {
            Error(message, "", "", carrierName, commandId, machineName, unitName);
        }

        public void Error(Object message, String messageName, String carrierName, String commandId, String machineName, String unitName)
        {
            Error(message, "", messageName, carrierName, commandId, machineName, unitName);
        }

        public void Error(Object message, String transactionId, String messageName, String carrierName, String commandId, String machineName, String unitName)
        {
            LogMessage logMessage = CreateLogMessage(message, transactionId, messageName, carrierName, commandId, machineName, unitName);
            Error(logMessage);
        }

        public void Error(Object message, String transactionId, String messageName, String carrierName, String commandId, String machineName, String unitName, String communicationMessageName)
        {
            LogMessage logMessage = CreateLogMessage(message, "", transactionId, messageName, carrierName, commandId, machineName, unitName, communicationMessageName);
            Error(logMessage);
        }

        public void Error(Object message, String carrierName, String commandId, String machineName, String unitName, Exception ex)
        {
            Error(message, "", "", carrierName, commandId, machineName, unitName, ex);
        }

        public void Error(Object message, String messageName, String carrierName, String commandId, String machineName, String unitName, Exception ex)
        {
            Error(message, "", messageName, carrierName, commandId, machineName, unitName, ex);
        }

        public void Error(Object message, String transactionId, String messageName, String carrierName, String commandId, String machineName, String unitName, Exception ex)
        {
            LogMessage logMessage = CreateLogMessage(message, transactionId, messageName, carrierName, commandId, machineName, unitName);

            Error(logMessage, ex);
        }
        public void Fatal(Object message, TransferMessageEx transferMessage)
        {
            String communicationMessageName = transferMessage.MessageName;
            Fatal(message, "", transferMessage.MessageName, transferMessage.CarrierName,
              transferMessage.TransportCommandId, transferMessage.CurrentMachineName, transferMessage.CurrentUnitName, communicationMessageName);
        }

        public void Fatal(Object message, String carrierName, String commandId, String machineName, String unitName)
        {
            Fatal(message, "", "", carrierName, commandId, machineName, unitName);
        }

        public void Fatal(Object message, String messageName, String carrierName, String commandId, String machineName, String unitName)
        {
            Fatal(message, "", messageName, carrierName, commandId, machineName, unitName);
        }

        public void Fatal(Object message, String transactionId, String messageName, String carrierName, String commandId, String machineName, String unitName)
        {
            LogMessage logMessage = CreateLogMessage(message, transactionId, messageName, carrierName, commandId, machineName, unitName);

            Fatal(logMessage);
        }

        public void Fatal(Object message, String transactionId, String messageName, String carrierName, String commandId, String machineName, String unitName, String communicationMessageName)
        {
            LogMessage logMessage = CreateLogMessage(message, "", transactionId, messageName, carrierName, commandId, machineName, unitName, communicationMessageName);

            Fatal(logMessage);
        }

        public void Fatal(Object message, String carrierName, String commandId, String machineName, String unitName, Exception ex)
        {
            Fatal(message, "", "", carrierName, commandId, machineName, unitName, ex);
        }

        public void Fatal(Object message, String messageName, String carrierName, String commandId, String machineName, String unitName, Exception ex)
        {
            Fatal(message, "", messageName, carrierName, commandId, machineName, unitName, ex);
        }

        public void Fatal(Object message, String transactionId, String messageName, String carrierName, String commandId, String machineName, String unitName, Exception ex)
        {
            LogMessage logMessage = CreateLogMessage(message, transactionId, messageName, carrierName, commandId, machineName, unitName);

            Fatal(logMessage, ex);
        }

        public LogMessage CreateLogMessage(Object message)
        {
            return CreateLogMessage(message, "");
        }

        public LogMessage CreateLogMessage(object message, string transactionId, string messageName, string carrierName, string commandId, string machineName, string unitName)
        {
            return CreateLogMessage(message, "", transactionId, messageName, carrierName, commandId, machineName, unitName, "");
        }
        public LogMessage CreateLogMessage(object message, string threadName, string transactionId, string messageName, string carrierName, string commandId, string machineName, string unitName)
        {
            return CreateLogMessage(message, threadName, transactionId, messageName, carrierName, commandId, machineName, unitName, "");
        }



        public LogMessage CreateLogMessage(Object message, string threadName, string transactionId, string messageName, string carrierName, string commandId, string machineName, string unitName, string communicationMessageName)
        {
            LogMessage logMessage = this.logManager != null ? this.logManager.CreateLogMessageInstance() : new LogMessage();

            logMessage.ThreadName = threadName;
            logMessage.TransactionId = transactionId;
            logMessage.MessageName = messageName;
            logMessage.TransportCommandId = commandId;
            logMessage.CarrierName = carrierName;
            logMessage.MachineName = machineName;
            logMessage.UnitName = unitName;
            logMessage.Text = message.ToString();
            logMessage.CommunicationMessageName = communicationMessageName;

            return logMessage;
        }



        private string GetExceptionMessage(object message, Exception ex)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(message);
            
            try
            {
                sb.Append(@"\n");
                sb.Append(ex.Message);
            }
            catch(Exception e)
            {
                Console.WriteLine("[" + DateTime.Now.ToShortTimeString() + "] failed to create exception log, " + e.Message);
            }

            return sb.ToString();
        }

        protected void SaveMessageToDatabase(object message, int currentLogLevelInt, string currentLogLevel, string communicationMessageName)
        {
            try
            {
                if ((this.logManager != null) && (this.logManager.IsGreaterOrEqual(currentLogLevelInt))) {
                    if(this.logManager.IsUseAdoDotNetAppender())
                    {
                        LogMessage logMessage = !(message is LogMessage) ? CreateLogMessage(message, communicationMessageName) : (LogMessage)message;

                        // StackTrace를 사용하여 호출자 정보 획득
                        StackFrame sf = new StackFrame(2, true); // 2 프레임 위로 올라가서 실제 호출자 찾기
                        var method = sf.GetMethod();
                        string className = method?.DeclaringType?.FullName ?? "Unknown";
                        string methodName = method?.Name ?? "Unknown";
                        string operationName = this.logManager.IsUseShortClassNameAtOperationName() ? className + "." + methodName : methodName;

                        this.logManager.CreateLogMessage(logMessage, Thread.CurrentThread.Name, operationName, currentLogLevel);
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("[" + DateTime.Now.ToShortTimeString() + "] failed to save log to database, " + e.Message);
            }
        }

        public LogMessage CreateLogMessage(Object message, String communicationMessageName)
        {
            LogMessage logMessage = this.logManager != null ? this.logManager.CreateLogMessageInstance() : new LogMessage();
            if (message != null)
            {
                logMessage.Text = message.ToString();
                logMessage.CommunicationMessageName = communicationMessageName;
            }
            return logMessage;
        }

        public LogMessage CreateWorkflowLogMessage(Object message, String transactionId, String messageName)
        {
            return CreateWorkflowLogMessage(message, transactionId, messageName, messageName);
        }

        public LogMessage CreateWorkflowLogMessage(Object message, String transactionId, String messageName, String communicationMessageName)
        {
            LogMessage logMessage = this.logManager != null ? this.logManager.CreateLogMessageInstance() : new LogMessage();

            logMessage.WorkflowLog = true;
            logMessage.TransactionId = transactionId;
            logMessage.MessageName = messageName;
            logMessage.Text = message.ToString();
            logMessage.CommunicationMessageName = communicationMessageName;

            return logMessage;
        }
    }
}
