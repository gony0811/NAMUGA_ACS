using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Message;

namespace ACS.Communication.Socket
{
    public abstract class CommonPacket : IPacket
    {
        protected DateTime createTime;
        protected string data;
        protected string sendId;
        protected string revId;
        protected string command;
        private string station;
        private string number;
        private string sub;
        protected string checkSum;
        protected byte[] rawData;
        private byte stx = 2;
        private byte etx = 3;
        public CommonPacket()
        {
            stx = 2;
            etx = 3;
        }

        public CommonPacket(byte[] packet)
         {
            //190524 Send Packet의 data Length 변경(Fix->Flexible) 에 따른 변경
            //#if BYTE12
            //            this.createTime = new DateTime();
            //            this.rawData = packet;

            //            this.revId = Encoding.UTF8.GetString(packet, 1, 2);
            //            this.sendId = Encoding.UTF8.GetString(packet, 3, 2);
            //            this.command = Encoding.UTF8.GetString(packet, 5, 1);
            //            this.data = Encoding.UTF8.GetString(packet, 6, 4);
            //            checkSum = Encoding.UTF8.GetString(packet, 10, 1);
            //#else
            //            this.createTime = new DateTime();
            //            this.rawData = packet;

            //            // A005L123400
            //            this.revId = Encoding.UTF8.GetString(packet, 1, 1); //A
            //            this.sendId = Encoding.UTF8.GetString(packet, 2, 3); //005
            //            this.command = Encoding.UTF8.GetString(packet, 5, 1); //L
            //            this.data = Encoding.UTF8.GetString(packet, 6, 6); //123400
            //            checkSum = Encoding.UTF8.GetString(packet, 12, 1);
            //#endif
        }

        public CommonPacket(string revId, string sendId, string command, string station, string number, string sub)
        {
            this.createTime = DateTime.Now;
            byte[] bRevID = Encoding.UTF8.GetBytes(revId);
            byte[] bSndID = Encoding.UTF8.GetBytes(sendId);
            byte[] bCMD = Encoding.UTF8.GetBytes(command);
            byte[] bStation = Encoding.UTF8.GetBytes(station);
            byte[] bSubNumber = Encoding.UTF8.GetBytes(number);
            byte[] bSpareNum = Encoding.UTF8.GetBytes(sub);

            this.rawData = MakeRawData(new byte[][] { bRevID, bSndID, bCMD, bStation, bSubNumber, bSpareNum });
        }

        public CommonPacket(string RevID, string SendID, string Command, string Data)
        {
            this.createTime = DateTime.Now;
            byte[] bRevID = Encoding.UTF8.GetBytes(RevID);
            byte[] bSndID = Encoding.UTF8.GetBytes(SendID);
            byte[] bCMD = Encoding.UTF8.GetBytes(Command);
            byte[] bData = Encoding.UTF8.GetBytes(Data);

            this.rawData = MakeRawData(new byte[][] { bRevID, bSndID,  bCMD, bData });

            this.sendId = SendID;
            this.revId = RevID;
        }

        /// <summary>
        /// SM01 전용
        /// </summary>
        /// <param name="RevID"></param>
        /// <param name="SendID"></param>
        /// <param name="Command"></param>
        /// <param name="Data"></param>
        public CommonPacket(string RevID, string SendID, string Command, string Data, string temp)
        {
            this.createTime = DateTime.Now;
            byte[] bRevID = Encoding.UTF8.GetBytes(RevID);
            byte[] bSndID = Encoding.UTF8.GetBytes(SendID);
            byte[] bCMD = Encoding.UTF8.GetBytes(Command);
            byte[] bData = Encoding.UTF8.GetBytes(Data);

            this.rawData = MakeRawData(new byte[][] { bSndID, bRevID, bCMD, bData });

            this.sendId = SendID;
            this.revId = RevID;
        }

        private byte[] MakeRawData(params byte[][] bs)
        {
            List<byte> mergebytelist = new List<byte>();
            byte[][] arrayOfByte;
            int j = (arrayOfByte = bs).Length;
            for (int i = 0; i < j; i++)
            {
                byte[] b = arrayOfByte[i];
                byte[] arrayOfByte1;
                int m = (arrayOfByte1 = b).Length;
                for (int k = 0; k < m; k++)
                {
                    byte c = arrayOfByte1[k];
                    mergebytelist.Add(Convert.ToByte(c));
                }
            }
            byte sumPayload = 0;

            for (System.Collections.IEnumerator localIterator1 = mergebytelist.GetEnumerator(); localIterator1.MoveNext(); )
            {
                //sbyte b = ((sbyte?)localIterator1.Current).Value;
                byte b = Convert.ToByte(localIterator1.Current);
                sumPayload = (byte)(sumPayload + b);
            }
            byte checksum = (byte)(sumPayload & 0xF);
            if (checksum >= 10)
            {
                checksum = (byte)(checksum + 87);
            }
            else
            {
                checksum = (byte)(checksum + 48);
            }
            mergebytelist.Add(Convert.ToByte(checksum));
            mergebytelist.Insert(0, Convert.ToByte(this.STX));
            mergebytelist.Add(Convert.ToByte(this.ETX));

            int index = 0;
            byte[] result = new byte[mergebytelist.Count];
            for (System.Collections.IEnumerator localIterator2 = mergebytelist.GetEnumerator(); localIterator2.MoveNext(); )
            {
                byte b = ((byte)localIterator2.Current);
                result[(index++)] = b;
            }
            return result;
        }

        public byte[] GetRawPayload()
        {
#if BYTE12
            return new byte[] { this.rawData[1], this.rawData[2], this.rawData[3], this.rawData[4], this.rawData[5], this.rawData[6], this.rawData[7], 
                    this.rawData[8], this.rawData[9]};
#else
            return new byte[] { this.rawData[1], this.rawData[2], this.rawData[3], this.rawData[4], this.rawData[5], this.rawData[6], this.rawData[7], 
                    this.rawData[8], this.rawData[9], this.rawData[10], this.rawData[11] };
#endif
        }

        public string Data
        {
            get
            {
                return data;
            }
            set
            {
                data = value;
            }
        }

        public string SendId
        {
            get
            {
                return sendId;
            }
            set
            {
                sendId = value;
            }
        }

        public string RevId
        {
            get
            {
                return revId;
            }
            set
            {
                revId = value;
            }
        }

        public string Command
        {
            get
            {
                return command;
            }
            set
            {
                command = value;
            }
        }

        public string Station
        {
            get
            {
                return station;
            }
            set
            {
                station = value;
            }
        }

        public string Number
        {
            get
            {
                return number;
            }
            set
            {
                number = value;
            }
        }

        public string Sub
        {
            get
            {
                return sub;
            }
            set
            {
                sub = value;
            }
        }

        public string CheckSum
        {
            get
            {
                return checkSum;
            }
            set
            {
                checkSum = value;
            }
        }

        public byte[] RawData
        {
            get
            {
                return rawData;
            }
            set
            {
                rawData = value;
            }
        }

        public byte STX
        {
            get
            {
                return stx;
            }
            set
            {
                stx = value;
            }
        }

        public byte ETX
        {
            get
            {
                return etx;
            }
            set
            {
                etx = value;
            }
        }

        public DateTime CreateTime
        {
            get
            {
                return createTime;
            }
            set
            {
                createTime = value;
            }
        }
    }
}
