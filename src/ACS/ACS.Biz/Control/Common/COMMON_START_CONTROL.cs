using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using Spring.Context;

namespace ACS.Biz
{
    public class COMMON_START_CONTROL : BaseBizJob
    {
        private Dictionary<string, Tuple<Type, object>> commandJobList;

        public Dictionary<string, Tuple<Type, object>> CommandJobList
        { 
            get { return commandJobList;}  
            set { commandJobList = value;} 
        }

        public override int ExecuteJob(object[] args)
        {
           
            return 0;
        }
    }
}
