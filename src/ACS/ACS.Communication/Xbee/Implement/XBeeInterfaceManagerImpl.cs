using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using ACS.Core.Base;
//using ACS.Core.Extension.Resource.Model;

namespace ACS.Communication.Xbee.Implement
{
    public class XBeeInterfaceManagerImpl : AbstractManager
    {
        private Dictionary<string, List<XBeeInterfaceManagerImpl>> xbeeInterfaces = new Dictionary<string, List<XBeeInterfaceManagerImpl>>();
        //private ApplicationContext applicationContext;

        public Dictionary<string, List<XBeeInterfaceManagerImpl>> getXBeeInterfaces()
        {
            return this.xbeeInterfaces;
        }

        public void setXBeeInterfaces(Dictionary<string, List<XBeeInterfaceManagerImpl>> xbeeInterfaces)
        {
            this.xbeeInterfaces = xbeeInterfaces;
        }

        //public void setApplicationContext(ApplicationContext applicationContext)
        //throws BeansException
        //{
        //    this.applicationContext = applicationContext;
        //}

        public void displayAll() { }

        public string getXBeeInterfaceNames()
        {
            return this.xbeeInterfaces.Keys.ToString(); //lys20180719 keySet().tostring();
        }

        public int getXBeeInterfaceCount()
        {
            return this.xbeeInterfaces.Count;
        }

        //public AbstractXBeeDriver getXBeeDriver(string name)
        //{
            //AbstractXBeeDriver abstractXBeeDriver = (AbstractXBeeDriver)this.xbeeInterfaces.ContainsKey(name);
            //if (abstractXBeeDriver == null)
            //{
            //    throw new XBeeDriverDoesNotExistException("secsDriver{" + name + "} does not exist"); //lys20180720 상위 class 호출
            //}
            //return abstractXBeeDriver;
        //}

        //public AbstractXBeeDriver getXBeeDriverByVehicle(VehicleEx vehicle)
        //{
            //if (vehicle != null)
            //{
            //    for (Iterator iterator = this.xbeeInterfaces.values().iterator(); iterator.hasNext();)
            //    {
            //        AbstractXBeeDriver xbeeDriver = (AbstractXBeeDriver).iterator.next();
            //        if (xbeeDriver.getVehicles().containsKey(vehicle.getId()))
            //        {
            //            return xbeeDriver;
            //        }
            //    }
            //}
            //throw new XBeeDriverDoesNotExistException("xbeeDriver does not exist related to vehicle{" + vehicle.getId() + "}");
        //}

        public void restartAll()
        {
            stopAll();
            startAll();
        }

        public void startAll() { }

        public void stopAll() { }

        public bool startXBeeInterface(string name)
        {
            return false;
        }

        public bool stopXBeeInterface(string name)
        {
            return false;
        }

        public bool isConnected(string name)
        {
            return false;
        }
    }
}
