using ACS.Communication.Msb;
using ACS.Core.Logging;
using ACS.Communication.Msb.Tibrv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Msb.Tibrv
{
    public abstract class AbstractTibrv : AbstractMsb
    {
        public Logger logger = Logger.GetLogger(typeof(AbstractMsb));
        protected internal Transport transport;
        protected internal TransportParameter transportParameter;
        /// <summary>
        /// @deprecated
        /// </summary>
        public const string DEFAULT_DATA_FIELD_NAME = "DATA";
        protected internal bool useDataField = true;
        protected internal string dataFieldName = "DATA";

        public bool UseDataField
        {
            get
            {
                return this.useDataField;
            }
            set
            {
                this.useDataField = value;
            }
        }
        public TransportParameter TransportParameter
        {
            get
            {
                return this.transportParameter;
            }
            set
            {
                this.transportParameter = value;
            }
        }
        public Transport Transport
        {
            get
            {
                return this.transport;
            }
            set
            {
                this.transport = value;
            }
        }

        public string DataFieldName
        {
            get
            {
                return this.dataFieldName;
            }
            set
            {
                this.dataFieldName = value;
            }
        }

        public override void Init()
        {
            base.Init();
        }
    }
}
