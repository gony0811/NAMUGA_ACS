using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Resource.Model.Alternate
{
    public class AbstractAlternateStorageGroup : TimedEntity
    {
        protected string machineName;
        protected string groupName;
        protected string selectionRule = "FIXED_ORDER";
        protected int lastIndex = 0;

        public string MachineName
        {
            get { return machineName; }
            set { machineName = value; }
        }

        public string GroupName
        {
            get { return groupName; }
            set { groupName = value; }
        }

        public string SelectionRule
        {
            get { return selectionRule; }
            set { selectionRule = value; }
        }

        public int LastIndex
        {
            get { return lastIndex; }
            set { lastIndex = value; }
        }
    }
}
