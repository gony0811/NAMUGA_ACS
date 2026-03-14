using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Message.Model.Control;
using ACS.Framework.Message;
using ACS.Framework.History;
using ACS.Framework.Application;
using ACS.Communication.Msb;

namespace ACS.Control
{
    public interface IControlServerManager
    {
        IDictionary Scripts { get; set; }
        string StartConfigurationFilePath { get; set; }
        IApplicationManager ApplicationManager { get; set; }
        IMessageManagerEx MessageManager { get; set; }
        ISynchronousMessageAgent SynchronousMessageAgent { get; set; }
        IHistoryManagerEx HistoryManager { get; set; }
        long HeartBeatInterval { get; set; }
        long HeartBeatStartDelay { get; set; }
        long HeartBeatTimeout { get; set; }
        bool UseHeartBeat { get; set; }
        long SimpleHeartBeatInterval { get; set; }
        long SimpleHeartBeatStartDelay { get; set; }
        int HeartBeatRetryCount { get; set; }
        long HeartBeatRetryTimeout { get; set; }
        int HeartBeatFailWhenProcessDown { get; set; }
        int HeartBeatFailWhenProcessHang { get; set; }
        long RescheduleHeartBeatInterval { get; set; }
        long RescheduleHeartBeatStartDelay { get; set; }
        bool UseSystemKill { get; set; }
        bool UseSystemGetProcessId { get; set; }
        bool UseSecondAsTimeUnit { get; set; }
        bool Control(ControlMessage paramControlMessage);

        bool Start(ControlMessage paramControlMessage);

        bool Start(String paramString1, String paramString2);

        bool Kill(ControlMessage paramControlMessage);

        bool Kill(String paramString1, String paramString2);
               
        String GetProcessId(ControlMessage paramControlMessage);

        String GetProcessId(String paramString);

        bool ExecuteCoreDump(ControlMessage paramControlMessage);

        bool ExecuteCoreDump(String paramString1, String paramString2);

        void ScheduleHeartBeats();
        void ScheduleUiTransport();
        void ScheduleUiApplicationManager();

        void UnscheduleHeartBeats();

        void RescheduleHeartBeats();

        void DisplayTriggers();

        bool ScheduleHeartBeat(String paramString);

        bool UnscheduleHeartBeat(String paramString);

        bool ShedulingHeartBeat(String paramString);

        void PauseHeartBeat(String paramString);

        void ResumeHeartBeat(String paramString);

        String GetDestinationName(String paramString);

        //200622 Change NIO Logic About ES.exe does not restart
        void ScheduleUiCommand();
        //
    }
}
