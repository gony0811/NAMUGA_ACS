using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Application.Model
{
    public class Sequence : Entity
    {
        public static String PREFIX_RECOVERY_INDEX = "RECOVERY-INDEX-";
        private string name;
        private int currentValue = 1;
        private int limitedValue = 100;
        private int initialValue = 1;
        private int stepValue = 1;
        private string positive = "T";

        public string Name { get { return name; } set { name = value; } }
        public int CurrentValue { get { return currentValue; } set { currentValue = value; } }
        public int LimitedValue { get { return limitedValue; } set { limitedValue = value; } }
        public int InitialValue { get { return initialValue; } set { initialValue = value; } }
        public int StepValue { get { return stepValue; } set { stepValue = value; } }
        public string Positive { get { return positive; } set { positive = value; } }

    }
}
