using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Core;

namespace ACS.Framework.Logging
{
    public class FineLevel
    {
        public static int FINE_INT = 20010;
        public Level FINE { get; private set; }
        public FineLevel(int level, string levelString, int syslogEquivalent)
        {
            FINE = new Level(level, levelString);
        }

        public Level ToLevel(string sArg)
        {
            if((sArg != null) && (sArg.ToUpper().Equals("FINE")))
            {
                return FINE;
            }

            return ToLevel(sArg, Level.Info);
        }


        public Level ToLevel(int val)
        {
            if (val == FINE_INT)
            {
                return FINE;
            }
            return ToLevel(val, Level.Info);
        }

        public Level ToLevel(int val, Level defaultLevel)
        {
            if (val == FINE_INT)
            {
                return FINE;
            }
            return new Level(val, defaultLevel.Name, defaultLevel.DisplayName);
        }

        public Level ToLevel(String sArg, Level defaultLevel)
        {
            if ((sArg != null) && (sArg.ToUpper().Equals("FINE")))
            {
                return FINE;
            }
            return new Level(defaultLevel.Value, defaultLevel.Name, defaultLevel.DisplayName);
        }

    }
}
