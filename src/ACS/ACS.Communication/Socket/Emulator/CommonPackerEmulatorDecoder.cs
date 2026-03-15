using ACS.Core.Logging;
using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Filter.Codec;
using Mina.Filter.Codec.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Communication.Socket.Emulator
{
    /// <summary>
    /// //190723 AGV 시뮬레이터(SM01_P.exe) 개발
    /// socket first receiving part (ES <- AGV)
    /// </summary>
    public class CommonPackerEmulatorDecoder : ObjectSerializationDecoder
    {
        public Logger logger = Logger.GetLogger(typeof(CommonPacketDecoder));
        protected override bool DoDecode(IoSession session, IoBuffer @in, IProtocolDecoderOutput @out)
        {
            ////BSW Test
            //int LENGTH_DATA = 14;
            //byte[] bodyBytes2 = new byte[LENGTH_DATA];
            //@in.Get(bodyBytes2, 0, LENGTH_DATA);
            //string temp = Encoding.UTF8.GetString(bodyBytes2);
            //Console.WriteLine("Receive: " + temp);// +" "+ DateTime.Now.ToString());

            int LENGTH_DATA = 14;

            int RECEIVEBYTELENGTH = @in.Capacity;

            try
            {
                if (@in.Remaining < LENGTH_DATA)
                {
                    //If it is less than 14 digits, ignore it
                    return false;
                }

                @in.Mark();

                MemoryStream byteArrayOutputStream = new MemoryStream();

                byte[] StxBytes = new byte[1];
                byte[] bodyBytes = new byte[RECEIVEBYTELENGTH - 1];
                @in.Get(StxBytes, 0, 1);
                byteArrayOutputStream.Write(StxBytes, 0, 1);
                if (StxBytes[0] != 2)
                {
                    return false;
                }
                @in.Get(bodyBytes, 0, RECEIVEBYTELENGTH - 1);
                byteArrayOutputStream.Write(bodyBytes, 0, bodyBytes.Length);

                if (byteArrayOutputStream.ToArray()[(RECEIVEBYTELENGTH - 1)] != 3)
                {
                    //If the last character is not ascii 3, ignore it !!
                    logger.Error("ES DATA's Last Character is not ETX!!" + CommonData.BytesToHex(byteArrayOutputStream.ToArray()));
                    return false;
                }

                @out.Write(byteArrayOutputStream.ToArray());
            }
            catch (Exception e)
            {
                logger.Error(e.ToString());
            }
            finally
            {

            }
            return true;
        }
    }
}
