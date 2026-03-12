using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Socket
{
    public class SendPacket : CommonPacket
    {
        public SendPacket(byte[] rawbytes)
            : base(rawbytes)
        {
            //190524 Send Packet의 data Length 변경(Fix->Flexible) 에 따른 변경
            //송신 Packet의 data는 유동적
            this.createTime = new DateTime();
            this.rawData = rawbytes;

            this.revId = Encoding.UTF8.GetString(rawbytes, 1, 1);
            this.sendId = Encoding.UTF8.GetString(rawbytes, 2, 3);
            this.command = Encoding.UTF8.GetString(rawbytes, 5, 1);
            this.data = Encoding.UTF8.GetString(rawbytes, 6, rawbytes.Length - 8);
            checkSum = Encoding.UTF8.GetString(rawbytes, rawbytes.Length - 2, 1);
        }

        public SendPacket(string RevID, string SendID, string Command, string Station, string SubNumber, string SpareNum, ReceivePacket receivepacket)
            : base(RevID, SendID, Command, Station, SubNumber, SpareNum)
        {
        }

        public SendPacket(string RevID, string SendID, string Command, string Station, string SubNumber, string SpareNum)
            : base(RevID, SendID, Command, Station, SubNumber, SpareNum)
        {
        }

        public SendPacket(string RevID, string SendID, string Command, string Data)
            : base(RevID, SendID, Command, Data)
        {
        }

        /// <summary>
        /// SM01 전용
        /// //190723 AGV 시뮬레이터(SM01_P.exe) 개발
        /// </summary>
        /// <param name="RevID"></param>
        /// <param name="SendID"></param>
        /// <param name="Command"></param>
        /// <param name="Data"></param>
        /// <param name="temp"></param>
        public SendPacket(string RevID, string SendID, string Command, string Data, string temp)
            : base(RevID, SendID, Command, Data, temp)
        {
        }

        public virtual byte[] GetRawData()
        {
            return base.RawData;
        }

        public new string ToString()
        {
            string prettyMessage = string.Format("{0}{1}{2}{3}", new object[] { base.RevId, base.SendId, base.Command, base.Data });
            return prettyMessage;
        }
    }
}
