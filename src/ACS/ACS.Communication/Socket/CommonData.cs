using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Socket
{
    public class CommonData
    {
        private static CommonData _instance;
        private short systembytes;

        public static CommonData GetInstance()
        {
            if (_instance == null)
            {
                _instance = new CommonData();
            }
            return _instance;
        }

        public virtual short Systembytes
        {
            get
            {
                if (this.systembytes == short.MaxValue)
                {
                    this.systembytes = 0;
                }
                return this.systembytes++;
            }
        }

        protected static readonly char[] hexArray = "0123456789ABCDEF".ToCharArray();
        private const string BUNDLE_SEP = " ";

        //public static string bytesToHex(Byte[] bytes)
        //{
        //    char[] hexChars = new char[bytes.Length * 2];
        //    for (int j = 0; j < bytes.Length; j++)
        //    {
        //        int v = bytes[j] & 0xFF;
        //        hexChars[(j * 2)] = hexArray[((int)((uint)v >> 4))];
        //        hexChars[(j * 2 + 1)] = hexArray[(v & 0xF)];
        //    }
        //    return new string(hexChars);
        //}

        public static string BytesToHex(byte[] bytes)
        {
            char[] hexChars = new char[bytes.Length * 2];
            for (int j = 0; j < bytes.Length; j++)
            {
                int v = bytes[j] & 0xFF;
                hexChars[(j * 2)] = hexArray[((int)((uint)v >> 4))];
                hexChars[(j * 2 + 1)] = hexArray[(v & 0xF)];
            }
            return new string(hexChars);
        }

        public static string HexString(string message)
        {
            int msgSize = message.Length;
            string msgData = null;
            string tocalData = "";
            int j = 0;
            for (int k = 1; j < msgSize; k++)
            {
                msgData = message.Substring(j, 1);
                if (k % 2 == 0)
                {
                    msgData = msgData + " ";
                }
                tocalData = tocalData + msgData;
                j++;
            }
            return tocalData.Trim();
        }
    }
}
