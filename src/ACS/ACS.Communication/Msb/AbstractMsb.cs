using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Msb
{
    public abstract class AbstractMsb
    {
        private AbstractMsb.eDataFormat dataFormat = AbstractMsb.eDataFormat.XML_string;
        private string name;

        public string Name 
        {
            get 
            {
                return name;
            }
            set 
            {
                name = value; 
            }
        }
        public eDataFormat DataFormat
        {
            get { return dataFormat; }
            set { dataFormat = value; }
        }

        protected AbstractMsb()
        {

        }

        public virtual void Init()
        {


        }

        public string GetDataFormatTostring()
        {
            return dataFormat.ToString();
        }

        public enum eDataFormat
        {
            XML_string,
            PLAIN_string,
            OBJECT,
        }
    }
}
