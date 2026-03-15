using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Message;
using ACS.Core.Logging.Model;
using ACS.Core.Base.Interface;
using Serilog.Events;

namespace ACS.Core.Logging
{
    public interface ILogManager
    {
        MessageNode GetMessageNode();

        void CreateLogMessage(LogEvent logEvent);

        void CreateLogMessage(LogMessage paramLogMessage, String paramString1, String paramString2, String paramString3);

        bool IsGreaterOrEqual(int paramInt);

        bool IsUseShortClassNameAtOperationName();

        bool IsUseAdoDotNetAppender();

        int GetLargeTextSizeForInsert();

        IPersistentDao GetPersistentDao();

        LogMessage CreateLogMessageInstance();

        LargeLogMessage CreateLargeLogMessageInstance(int paramInt);
    }
}
