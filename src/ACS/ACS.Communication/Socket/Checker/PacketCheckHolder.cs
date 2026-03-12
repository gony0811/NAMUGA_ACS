using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Communication.Socket;

namespace ACS.Communication.Socket.Checker
{
    public class PacketCheckHolder
    {
        private string nioId;
        private ReceivePacket receivePacket;

        public PacketCheckHolder(string nioId, ReceivePacket receivePacket)
        {
            this.nioId = nioId;
            this.receivePacket = receivePacket;
        }

        public virtual string NioId
        {
            get { return this.nioId; }
            set { this.nioId = value; }
        }

        public virtual ReceivePacket ReceivePacket
        {
            get { return this.receivePacket; }
            set { this.receivePacket = value; }
        }

        public string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("packetCheckHolder{");
            sb.Append("nioId=").Append(this.nioId);
            sb.Append(", receivePacket=").Append(this.receivePacket);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
