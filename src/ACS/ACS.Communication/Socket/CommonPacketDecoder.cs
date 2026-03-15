using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Filter.Codec;
using Mina.Filter.Codec.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Logging;
using System.IO;
namespace ACS.Communication.Socket
{
    public class CommonPacketDecoder : ObjectSerializationDecoder
    {
        public Logger logger = Logger.GetLogger(typeof(CommonPacketDecoder));
        protected override bool DoDecode(IoSession session, IoBuffer @in, IProtocolDecoderOutput @out)
        {
#if BYTE12
             int LENGTH_DATA = 12;

            try
            {
                if (@in.Remaining < LENGTH_DATA)
                {
                    return false;
                }

                @in.Mark();

                MemoryStream byteArrayOutputStream = new MemoryStream();

                byte[] StxBytes = new byte[1];
                byte[] bodyBytes = new byte[LENGTH_DATA - 1];
                @in.Get(StxBytes, 0, 1);
                byteArrayOutputStream.Write(StxBytes, 0, 1);
                if (StxBytes[0] != 2)
                {
                    return false;
                }
                @in.Get(bodyBytes, 0, LENGTH_DATA - 1);
                byteArrayOutputStream.Write(bodyBytes, 0, bodyBytes.Length);

                if (byteArrayOutputStream.ToArray()[(LENGTH_DATA - 1)] != 3)
                {
                    logger.Error("DATA[12] is not ETX!!" + CommonData.BytesToHex(byteArrayOutputStream.ToArray()));
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
#else
            ////BSW Test
            //int LENGTH_DATA = 14;
            //byte[] bodyBytes2 = new byte[LENGTH_DATA];
            //@in.Get(bodyBytes2, 0, LENGTH_DATA);
            //string temp = Encoding.UTF8.GetString(bodyBytes2);
            //Console.WriteLine("Receive: " + temp);// +" "+ DateTime.Now.ToString());

            /*
                if (@in.HasArray)
                {
                    //Get Buffer Value(= Byte Array)
                    ArraySegment<Byte> bytes = @in.GetRemaining();
                    readBuffer = bytes.Array;
                }
            */

            //200417 Modify Socket Decoder, Because AGV S_CODE Format NG

            if (@in.Capacity > 100)
            {
                //수신버퍼 Char 100개 초과
                logger.Fatal("protected override bool DoDecode(IoSession session, IoBuffer @in, IProtocolDecoderOutput @out) :@in.Capacity > 100 : " + session.RemoteEndPoint + ", " + @in.Capacity);
            }

            int LENGTH_DATA = 14;

            try
            {
                //버퍼길이 14미만 False (DoDecode Function 호출 안함)
                if (@in.Remaining < LENGTH_DATA)
                {
                    return false;                                               //DoDecode 호출 안함
                }

                @in.Mark();

                MemoryStream byteArrayOutputStream = new MemoryStream();

                byte[] StxBytes = new byte[1];                                  //1 Digit (Stx)
                byte[] bodyBytes = new byte[LENGTH_DATA - 1];                   //13 Digit (Body + Etx)

                bool CheckStx = false;                                          //Check Stx

                while (@in.HasRemaining)                                        //Search 수신Buffer 
                {
                    //Get First Digit
                    @in.Get(StxBytes, 0, 1);

                    //OK Stx
                    if (StxBytes[0] == 2)
                    {
                        //OK: 남은 Length가 OK
                        if (@in.Remaining > LENGTH_DATA - 2)                    //13 Digit 이상
                        {
                            //OK: STX Ok, 남은 Length OK
                            byteArrayOutputStream.Write(StxBytes, 0, 1);
                            CheckStx = true;
                            break;
                        }
                        else
                        {
                            //200417 Modify Socket Decoder, Because AGV S_CODE Format NG
                            //NG: Stx OK, But 남은 Length가 NG
                            return true;                                        //DoDecode 재호출
                            //
                        }
                    }
                    else
                    {
                        //NG stx
                    }
                }

                if (!CheckStx)
                {
                    //NG stx 
                    return true;                                                //DoDecode 재호출
                }

                @in.Get(bodyBytes, 0, LENGTH_DATA - 1);             //Get 13Byte 
                byteArrayOutputStream.Write(bodyBytes, 0, bodyBytes.Length);

                if (byteArrayOutputStream.ToArray()[(LENGTH_DATA - 1)] != 3)
                {
                    logger.Error("DATA[14] is not ETX!!" + CommonData.BytesToHex(byteArrayOutputStream.ToArray()));
                    //200417 Modify Socket Decoder, Because AGV S_CODE Format NG
                    //return false;
                    return true;                                                //DoDecode 재호출
                    //
                }

                @out.Write(byteArrayOutputStream.ToArray());                    //Call MessageReceived Function
            }
            catch (Exception e)
            {
                logger.Error("protected override bool DoDecode(IoSession session, IoBuffer @in, IProtocolDecoderOutput @out) : " + e.ToString());
            }

            //최종OK시 True처리
            return true;                                            //DoDecode 재호출
                                                                    //

            #region Backup
            /* backUp
            int LENGTH_DATA = 14;

            try
            {
                if (@in.Remaining < LENGTH_DATA)
                {
                    return false;
                }

                @in.Mark();

                MemoryStream byteArrayOutputStream = new MemoryStream();

                byte[] StxBytes = new byte[1];
                byte[] bodyBytes = new byte[LENGTH_DATA - 1];
                @in.Get(StxBytes, 0, 1);
                byteArrayOutputStream.Write(StxBytes, 0, 1);
                if (StxBytes[0] != 2)
                {
                    //200417 Modify Socket Decoder, Because AGV S_CODE Format NG
                    return true;
                    //return false;
                    //
                }
                @in.Get(bodyBytes, 0, LENGTH_DATA - 1);
                byteArrayOutputStream.Write(bodyBytes, 0, bodyBytes.Length);

                if (byteArrayOutputStream.ToArray()[(LENGTH_DATA - 1)] != 3)
                {
                    logger.Error("DATA[14] is not ETX!!" + CommonData.BytesToHex(byteArrayOutputStream.ToArray()));
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
            */
            #endregion Backup
#endif
        }
    }
}
