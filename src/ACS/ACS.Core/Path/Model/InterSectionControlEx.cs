using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Path.Model
{
    public class InterSectionControlEx
    {

        public static String CROSS_WAIT_INTERSECTION = "INTERS";
        //private static final long serialVersionUID = -8457835685401807698L;

        public virtual string Id { get; set; }
        /**
        * @hibernate.property
        *  length="64"
        */
        public virtual string InterSectionId { get; set; }
        /**
        * @hibernate.property
        *  length="2"
        */
        public virtual string CheckPreviousNode { get; set; }
        /**
        * @hibernate.property
        *  length="64"
        */
        public virtual string StartNodeId { get; set; }
        /**
        * @hibernate.property
        *  length="64"
        */
        public virtual string EndNodeId { get; set; }
        /**
        * @hibernate.property
        *  length="5"
        */
        public virtual int Interval { get; set; }
        /**
        * @hibernate.property
        *  length="5"
        */
        public virtual int Sequence { get; set; }
        /**
        * @hibernate.property
        *  length="500"
        */
        public virtual string CheckNodeIds { get; set; }
        /**
        * @hibernate.property
        *  length="500"
        */
        public virtual string PreviousNodeIds { get; set; }
    }

}
