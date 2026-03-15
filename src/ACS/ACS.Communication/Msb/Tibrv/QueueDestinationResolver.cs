using ACS.Communication.Msb.Highway101;
using ACS.Core.Logging;
using ACS.Communication.Msb;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TIBCO.Rendezvous;

namespace ACS.Communication.Msb.Tibrv
{
    public class QueueDestinationResolver : DestinationResolver
    {
        //protected internal static readonly Logger logger = Logger.getLogger(typeof(QueueDestinationResolver));
        public Logger logger = Logger.GetLogger(typeof(QueueDestinationResolver));
        private System.Collections.IDictionary destinations = new Hashtable();

        public IDictionary Destinations
        {
            get
            {
                return this.destinations;
            }
            set
            {
                this.destinations = value;
            }
        }


        //public IDestination getDestination(string name)
        public ChannelDestination getDestination(string name)
        {
            object @object = this.destinations[name];
            if (@object == null)
            {
                logger.Info("there is no destination in " + this.destinations + ", so destination{" + name + "} will be created");
                try
                {
                    ChannelDestination destination = new QueueDestination(name);
                    this.destinations[name] = destination;
                    @object = destination;
                }
                catch (RendezvousException e)
                {
                    logger.Error("failed to create destination{" + name + "}", e);
                }
            }
            //return (IDestination)@object;
            return (ChannelDestination)@object;
        }
    }

}

