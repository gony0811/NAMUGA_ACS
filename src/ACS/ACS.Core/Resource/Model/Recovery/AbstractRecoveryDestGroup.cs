using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Resource.Model.Recovery
{
    public class AbstractRecoveryDestGroup : TimedEntity
    {
        private string groupName = "";
        private string machineName;
        private string selectionRule = "FIXEDORDER";
        private int lastIndex = 0;

        protected string GroupName { get { return groupName; } set { groupName = value; } }
        protected string MachineName { get { return machineName; } set { machineName = value; } }
        protected string SelectionRule { get { return selectionRule; } set { selectionRule = value; } }
        protected int LastIndex { get { return lastIndex; } set { lastIndex = value; } }
    }
}
