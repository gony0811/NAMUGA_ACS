using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Core;

namespace ACS.Framework.Logging
{
    public class LogLevel
    {
        public Level Level { get; private set; }
        public int LogEquivalent { get; private set; }

        public LogLevel(int level, string levelString, int syslogEquivalent)
        {
            Level = new Level(level, levelString);
            LogEquivalent = syslogEquivalent;
        }

        public Level ToLevel(string sArg)
        {
            if ((sArg != null) && (sArg.ToUpper().Equals(Level.Name)))
            {
                return Level;
            }

            return ToLevel(sArg, Level.Info);
        }


        public Level ToLevel(int val)
        {
            if (val == LogEquivalent)
            {
                return Level;
            }
            return ToLevel(val, Level.Info);
        }

        public Level ToLevel(int val, Level defaultLevel)
        {
            if (val == LogEquivalent)
            {
                return Level;
            }
            return new Level(val, defaultLevel.Name, defaultLevel.DisplayName);
        }

        public Level ToLevel(String sArg, Level defaultLevel)
        {
            if ((sArg != null) && (sArg.ToUpper().Equals(Level.Name)))
            {
                return Level;
            }
            return new Level(defaultLevel.Value, defaultLevel.Name, defaultLevel.DisplayName);
        }

    }
}
