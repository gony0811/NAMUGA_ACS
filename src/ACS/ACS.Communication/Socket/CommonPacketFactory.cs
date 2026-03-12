using Mina.Core.Session;
using Mina.Filter.Codec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Socket
{
    public class CommonPacketFactory : IProtocolCodecFactory
    {
        private IProtocolEncoder encoder;
        private IProtocolDecoder decoder;

        public CommonPacketFactory()
        {
            this.encoder = new CommonPacketEncoder();
            this.decoder = new CommonPacketDecoder();
        }

        public virtual IProtocolDecoder GetDecoder(IoSession arg0)
        {
            return this.decoder;
        }

        public virtual IProtocolEncoder GetEncoder(IoSession arg0)
        {
            return this.encoder;
        }
    }

}
