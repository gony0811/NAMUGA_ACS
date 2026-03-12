using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Framework.Base;

namespace ACS.Framework.Path.Model
{
    public class CurrentInterSectionInfoEx : Entity
    {
        //private static final long serialVersionUID = 5000157935190468876L;

        public static string STATE_CHANGED	= "CHANGED";
	    public static string STATE_CHANGING	= "CHANGING";
        private string state = STATE_CHANGED;

        public virtual string Id { get; set; }
        public virtual string CurrentDirectionNode { get; set; }
        public virtual DateTime? ChangedTime { get; set; }
        public virtual string State { get; set; }
    
        public override string ToString()
        {
            return Id + ":" + CurrentDirectionNode + ":" + ChangedTime + ":" + State;
        }
    }

}
