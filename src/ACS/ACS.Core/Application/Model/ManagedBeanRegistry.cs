using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Application.Model
{
    public class ManagedBeanRegistry
    {
        private IDictionary managedBeans;
        private IList managedMethods;

        public IDictionary ManagedBeans { get { return managedBeans; } set { managedBeans = value; } }
        public IList ManagedMethods { get { return managedMethods; } set { managedMethods = value; } }
    }
}
