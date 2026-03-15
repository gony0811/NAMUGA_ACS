using ACS.Communication.Msb;
using ACS.Communication.Msb.Util;
using ACS.Core.Message.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TIBCO.Rendezvous;

namespace ACS.Communication.Msb.Util
{
    public abstract class MessageConverterUtilsEx : MessageConverterUtils
    {
        public static object GetMessage(TIBCO.Rendezvous.Message tibrvMsg)
        {
            return tibrvMsg;
        }

        public static AbstractMessage GetMessage(TIBCO.Rendezvous.Message tibrvMsg, IMsbConverter msbConverter)
        {
            AbstractMessage abstractMessage = msbConverter.ConvertFromHost(tibrvMsg);

            return abstractMessage;
        }

        public static XmlDocument GetMessageBasedOnXmlString(TIBCO.Rendezvous.Message tibrvMsg, String dataFieldName)
        {
            String message = GetMessageAsString(tibrvMsg, dataFieldName);
            if (message == null)
            {
                return null;
            }

            XmlDocument document = new XmlDocument();

            try
            {

                if (string.ReferenceEquals(message, null))
                {

                }
                else
                {
                    document.LoadXml(message);
                }
            }
            catch (Exception e)
            {

            }

            return document;
        }

        public static object GetMessageBasedOnObject(TIBCO.Rendezvous.Message tibrvMsg, String dataFieldName)
        {
            return GetMessageAsObject(tibrvMsg, dataFieldName);
        }

        public static string GetMessageBasedOnPlainString(TIBCO.Rendezvous.Message tibrvMsg, String dataFieldName)
        {
            string message = GetMessageAsString(tibrvMsg, dataFieldName);
            if (message == null)
            {
                return null;
            }
            return message;
        }

        public static AbstractMessage GetMessageBasedOnPlainString(TIBCO.Rendezvous.Message tibrvMsg, String dataFieldName, IMsbConverter msbConverter)
        {
            String message = GetMessageAsString(tibrvMsg, dataFieldName);
            if (message == null)
            {
                return null;
            }
            AbstractMessage abstractMessage = msbConverter.ConvertFromHost(message);

            return abstractMessage;
        }

        public static AbstractMessage GetMessageBasedOnXmlString(TIBCO.Rendezvous.Message tibrvMsg, string dataFieldName, IMsbConverter msbConverter)
        {
            String message = GetMessageAsString(tibrvMsg, dataFieldName);
            if (message == null)
            {
                return null;
            }
            XmlDocument document = null;

            if (string.ReferenceEquals(message, null))
            {

            }
            else
            {
                document.LoadXml(message);
            }

            AbstractMessage abstractMessage = msbConverter.ConvertFromHost(document);

            return abstractMessage;
        }

        public static String GetMessageAsString(TIBCO.Rendezvous.Message tibrvMsg, string dataFieldName)
        {
            MessageField tibrvMsgField = tibrvMsg.GetField(dataFieldName);

            if (tibrvMsgField == null)
            {
                //logger.error("there is no data related to {" + dataFieldName + "}, please check message format");
                return null;
            }
            //String message = tibrvMsgField.GetType != 7 ? (String)tibrvMsgField.Value : new String((byte[])tibrvMsgField.Value);
            String message = (String)tibrvMsgField.Value;

            return message;
        }


        public static object GetMessageAsObject(TIBCO.Rendezvous.Message tibrvMsg, String dataFieldName)
        {
            MessageField tibrvMsgField = tibrvMsg.GetField(dataFieldName);
            if (tibrvMsgField == null)
            {
                //logger.error("there is no data related to {" + dataFieldName + "}, please check message format");
                return null;
            }
            object message = null;
            //if (tibrvMsgField.type == 7)
            //{
            byte[] array = (byte[])tibrvMsgField.Value;
            if (array != null)
            {
                try
                {
                    //ByteArrayInputStream byteArrayInputStream = new ByteArrayInputStream(array);
                    //ObjectInputStream objectInputStream = new ObjectInputStream(byteArrayInputStream);
                    //message = objectInputStream.readObject();
                    //objectInputStream.close();

                    message = array;
                }
                catch (IOException e)
                {
                    //logger.error("failed to unmarshal");
                }
                //catch (ClassNotFoundException e)
                //{
                //    logger.error("failed to unmarshal");
                //}
                finally
                {

                }
            }
            //}
            //else
            //{
            //    //logger.error("failed to unmarshal, object should be transferred as byte array");
            //}
            return message;
        }


    }
}
