using Mina.Core.Session;
using Mina.Filter.Codec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Socket.Emulator
{
    /// <summary>
    /// //190723 AGV 시뮬레이터(SM01_P.exe) 개발
    /// </summary>
    public class CommonPacketEmulatorFactory : IProtocolCodecFactory
    {
        private IProtocolEncoder encoder;
        private IProtocolDecoder decoder;

        public CommonPacketEmulatorFactory()
        {
            this.encoder = new CommonPacketEncoder();
            this.decoder = new CommonPackerEmulatorDecoder();
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
