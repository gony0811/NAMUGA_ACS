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
    public class ReceivePacketEmulator : ReceivePacket
    {
        public ReceivePacketEmulator(byte[] bytes)              
            : base(bytes)
        {
            this.sendId = Encoding.UTF8.GetString(bytes, 1, 3);              //002
            this.revId = Encoding.UTF8.GetString(bytes, 4, 1);               //A
            this.command = Encoding.UTF8.GetString(bytes, 5, 1);             //H
            this.data = Encoding.UTF8.GetString(bytes, 6, bytes.Length - 8); //151520
            checkSum = Encoding.UTF8.GetString(bytes, bytes.Length - 2, 1);  //9
            this.rawData = bytes;                                            //
            CreateTime = DateTime.Now;              
        }

        public override bool GetChecksumResult()
        {
            byte[] unsigned = new byte[RawData.Length - 3];
            Buffer.BlockCopy(RawData, 1, unsigned, 0, RawData.Length-3);
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

        public override string ToString()
        {
            string prettyMessage = string.Format("{0}{1}{2}{3}", new object[] { base.SendId, base.RevId, base.Command, base.Data });
            return prettyMessage;
        }

    }
}
