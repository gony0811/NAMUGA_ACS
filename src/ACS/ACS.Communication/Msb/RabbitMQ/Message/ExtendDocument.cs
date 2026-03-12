using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;

namespace ACS.Communication.Msb.RabbitMQ.Message
{
    public class ExtendDocument : XmlDocument
    {
        Dictionary<object, object> extendData = new Dictionary<object, object>();

        public Dictionary<object, object> ExtendData
        {
            get { return extendData; }
            set { extendData = value; }
        }

        public void putExtendDataElement(object key, object obj)
        {
            if (!this.extendData.ContainsKey(key))
                this.extendData.Add(key, obj);
        }

        public void RemoveExtedDataElement(object key)
        {
            if (this.extendData.ContainsKey(key))
                this.ExtendData.Remove(key);
        }

        public object ExtendDataElement(object key)
        {
            if (this.extendData.ContainsKey(key))
            {
                return this.extendData[key];
            }

            return true;
        }
    }
}
