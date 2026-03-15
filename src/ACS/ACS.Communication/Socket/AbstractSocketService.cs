using Mina.Core.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ACS.Core.Logging;

namespace ACS.Communication.Socket
{
    public abstract class AbstractSocketService
    {
        public Logger logger = Logger.GetLogger(typeof(AbstractSocketService));
        public Logger commLogger = Logger.GetLogger("AGVCommLogger"); 
        protected IoSession ioSession;
        protected bool sessionOpened;

        public virtual IoSession IoSession
        {
            get { return this.ioSession; }
            set { this.ioSession = value; }
        }

        public virtual bool SessionOpened
        {
            get { return this.sessionOpened; }
            set { this.sessionOpened = value; }
        }

        public virtual int Request(string primaryMessage)
        {
            this.ioSession.Write(primaryMessage);
            logger.Info("request{" + primaryMessage + "}");
            return 0;
        }

        public virtual int Reply(string secondaryMessage)
        {
            this.ioSession.Write(secondaryMessage);
            logger.Info("reply{" + secondaryMessage + "}");
            return 0;
        }

        public virtual void Sleep(long millis)
        {
            try
            {
                Thread.Sleep((int)millis);
            }
            catch (Exception e)
            {
                logger.Warn("failed to sleep", e);
            }
            finally
            {

            }
        }

        public virtual int Send(string Message)
        {
            if (Message.Length == 11)
            {
                SendPacket sendPacket = new SendPacket(Message.Substring(0, 3), Message.Substring(3, 1), Message.Substring(4, 1), Message.Substring(5, 6));

                byte[] rawData = sendPacket.GetRawData();

                this.ioSession.Write(rawData);

                commLogger.Info(sendPacket.ToString());
            }
            else
            {
                logger.Info("messageSend Data invaild");
            }
            return 0;
        }

        public virtual int Send(SendPacket sendPacket)
        {
            //190524 Send Packet의 data Length 변경(Fix->Flexible) 에 따른 변경
            byte[] rawData = sendPacket.GetRawData();

            this.ioSession.Write(rawData);

            string sendstring = Encoding.UTF8.GetString(rawData);

            commLogger.Info(sendstring.Substring(1, rawData.Length - 3));
            return 0;

            //
            //            byte[] rawData = sendPacket.GetRawData();

            //            this.ioSession.Write(rawData);

            //            string sendstring = Encoding.UTF8.GetString(rawData);

            //#if BYTE12
            //            commLogger.Info(sendstring.Substring(1, 9));
            //#else
            //            commLogger.Info(sendstring.Substring(1, 11));
            //#endif
            //            return 0;
        }

        public abstract bool Connect();

        public abstract void OnDisconnected();

        //191101 if ioSession.Connected, Send "H"
        public virtual bool IsConnected()
        {
            return ioSession.Connected;
        }
        //
    }
}
