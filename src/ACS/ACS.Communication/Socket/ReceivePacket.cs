using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Message;

namespace ACS.Communication.Socket
{
    public class ReceivePacket : CommonPacket, IPacket
    {
        public ReceivePacket(byte[] bytes)
            : base(bytes)
        {
            //190524 Send Packet의 data Length 변경(Fix->Flexible) 에 따른 변경
            // A005L123400 (수신Packet은 14Byte Fix)
            this.revId = Encoding.UTF8.GetString(bytes, 1, 1); //A
            this.sendId = Encoding.UTF8.GetString(bytes, 2, 3); //005
            this.command = Encoding.UTF8.GetString(bytes, 5, 1); //L
            this.data = Encoding.UTF8.GetString(bytes, 6, 6); //123400
            checkSum = Encoding.UTF8.GetString(bytes, 12, 1);
            this.rawData = bytes;
            CreateTime = DateTime.Now;
        }

        public virtual bool GetChecksumResult()
        {
            byte[] unsigned = base.GetRawPayload();
            sbyte[] payload = unsigned.Select(b => (sbyte)b).ToArray();

            sbyte sumPayload = 0;
            sbyte[] arrayOfByte1;
            int j = (arrayOfByte1 = payload).Length;
            for (int i = 0; i < j; i++)
            {
                sbyte b = arrayOfByte1[i];
                sumPayload = (sbyte)(sumPayload + b);
            }
            
            sbyte recvChecksum = (sbyte)(Encoding.UTF8.GetBytes(base.CheckSum)[0]);
            sumPayload = (sbyte)(sumPayload & 0xF);
            if ((sumPayload >= 10) && (sumPayload <= 15))
            {
                sumPayload = (sbyte)(sumPayload + 87);
            }
            else
            {
                recvChecksum = (sbyte)(recvChecksum & 0xF);
            }
            return recvChecksum == sumPayload;
        }

        public virtual string ToDataCheck()
        {
            string prettyMessage = string.Format("ChecksumResult({0}),ReVID({1}),SendID({2}),CMD({3}),Data({4})", new object[] { Convert.ToBoolean(GetChecksumResult()), base.RevId, base.SendId, base.Command, base.Data, base.CheckSum });
            return prettyMessage;
        }

        public virtual string ToData()
        {
            string prettyMessage = string.Format("{0} {1} {2} {3}", new object[] { base.RevId, base.SendId, base.Command, base.Data });
            return prettyMessage;
        }

        public override string ToString()
        {
            string prettyMessage = string.Format("{0}{1}{2}{3}", new object[] { base.RevId, base.SendId, base.Command, base.Data });
            return prettyMessage;
        }

        //200527 When CheckSum NG, Write Checksum Character
        public virtual string ToDataCheckSum()
        {
            string prettyMessage = string.Format("{0} {1} {2} {3} {4}", new object[] { base.RevId, base.SendId, base.Command, base.Data, base.CheckSum });
            return prettyMessage;
        }
        //
    }
}
