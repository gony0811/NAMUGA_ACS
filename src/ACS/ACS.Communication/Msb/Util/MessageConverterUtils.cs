// Decompiled with JetBrains decompiler
// Type: ACS.Communication.Msb.Util.MessageConverterUtils
// Assembly: ACS.Communication, Version=1.0.0.1, Culture=neutral, PublicKeyToken=null
// MVID: 7F46AE0B-302E-44DB-A304-437AF99F9943
// Assembly location: D:\ACS\ACS\trunk\Bin\ACS.Communication.dll

using ACS.Communication.Msb.Highway101.Message;
using ACS.Core.Message.Model;
using System;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace ACS.Communication.Msb.Util
{
    public abstract class MessageConverterUtils
    {
        public static ExtendDocument GetMessage(com.miracom.transceiverx.message.Message highwayMessage)
        {
            XmlDocument document = new XmlDocument();
            document.LoadXml(Encoding.Default.GetString(highwayMessage.getData()));
            return MessageConverterUtils.ConvertToExtendDocument(document);
        }

        public static ExtendDocument GetMessage(byte[] message)
        {
            XmlDocument document = new XmlDocument();
            document.LoadXml(Encoding.Default.GetString(message));
            return MessageConverterUtils.ConvertToExtendDocument(document);
        }

        public static object GetMessage(com.miracom.transceiverx.message.Message highwayMessage, IMsbConverter msbConverter)
        {
            string messageStringFormat = MessageConverterUtils.GetMessageStringFormat(highwayMessage);
            if (messageStringFormat == null)
                return (object)null;
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(messageStringFormat);
            return (object)msbConverter.ConvertFromHost((object)xmlDocument);
        }

        public static object GetMessage(byte[] message, IMsbConverter msbConverter)
        {
            string messageStringFormat = MessageConverterUtils.GetMessageStringFormat(message);
            if (messageStringFormat == null)
                return (object)null;
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(messageStringFormat);
            return (object)msbConverter.ConvertFromHost((object)xmlDocument);
        }

        public static ExtendDocument GetMessageBasedOnXmlString(string message)
        {
            XmlDocument document = new XmlDocument();
            string messageStringFormat = message;
            if (messageStringFormat == null)
                return (ExtendDocument)null;
            document.LoadXml(messageStringFormat);
            return MessageConverterUtils.ConvertToExtendDocument(document);
        }

        public static ExtendDocument GetMessageBasedOnXmlString(com.miracom.transceiverx.message.Message highwayMessage)
        {
            XmlDocument document = new XmlDocument();
            string messageStringFormat = MessageConverterUtils.GetMessageStringFormat(highwayMessage);
            if (messageStringFormat == null)
                return (ExtendDocument)null;
            document.LoadXml(messageStringFormat);
            return MessageConverterUtils.ConvertToExtendDocument(document);
        }

        public static ExtendDocument GetMessageBasedOnXmlString(byte[] message)
        {
            XmlDocument document = new XmlDocument();
            string messageStringFormat = MessageConverterUtils.GetMessageStringFormat(message);
            if (messageStringFormat == null)
                return (ExtendDocument)null;
            document.LoadXml(messageStringFormat);
            return MessageConverterUtils.ConvertToExtendDocument(document);
        }

        public static object GetMessageBasedOnObject(com.miracom.transceiverx.message.Message highwayMessage)
        {
            if ((object)highwayMessage.getData() is ISerializable)
                return (object)highwayMessage.getData();
            return (object)null;
        }

        public static object GetMessageBasedOnObject(byte[] message)
        {
            if ((object)message is ISerializable)
                return (object)message;
            return (object)null;
        }

        public static string GetMessageStringFormat(com.miracom.transceiverx.message.Message highwayMessage)
        {
            return MessageConverterUtils.GetMessageAsString(highwayMessage);
        }


        public static string GetMessageStringFormat(byte[] message)
        {
            return MessageConverterUtils.GetMessageAsString(message);
        }

        public static void SetConversationId(XmlDocument document, string conversationId)
        {
            XmlElement xmlElement = document.DocumentElement["HEADER"];
            if (xmlElement == null)
                return;
            xmlElement["CONVERSATIONID"].InnerText = conversationId;
        }

        public static AbstractMessage GetMessageBasedOnXmlString(
          com.miracom.transceiverx.message.Message highwayMessage,
          IMsbConverter msbConverter)
        {
            string messageAsString = MessageConverterUtils.GetMessageAsString(highwayMessage);
            if (messageAsString == null)
                return (AbstractMessage)null;
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(messageAsString);
            return msbConverter.ConvertFromHost((object)xmlDocument);
        }

        public static AbstractMessage GetMessageBasedOnXmlString(
  String message,
  IMsbConverter msbConverter)
        {
            string messageAsString = message;
            if (messageAsString == null)
                return (AbstractMessage)null;
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(messageAsString);
            return msbConverter.ConvertFromHost((object)xmlDocument);
        }

        public static AbstractMessage GetMessageBasedOnXmlString(byte[] message, IMsbConverter msbConverter)
        {
            string messageAsString = MessageConverterUtils.GetMessageAsString(message);
            if (messageAsString == null)
                return (AbstractMessage)null;
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(messageAsString);
            return msbConverter.ConvertFromHost((object)xmlDocument);
        }

        public static ExtendDocument ConvertToExtendDocument(XmlDocument document)
        {
            ExtendDocument extendDocument = new ExtendDocument();
            extendDocument.LoadXml(document.InnerXml);
            return extendDocument;
        }

        public static AbstractMessage GetMessageBasedOnPlainString(
          com.miracom.transceiverx.message.Message highwayMessage,
          IMsbConverter msbConverter)
        {
            string messageAsString = MessageConverterUtils.GetMessageAsString(highwayMessage);
            if (messageAsString == null)
                return (AbstractMessage)null;
            return msbConverter.ConvertFromHost((object)messageAsString);
        }

        public static AbstractMessage GetMessageBasedOnPlainString(string message, IMsbConverter msbConverter)
        {
            string messageAsString = message;
            if (messageAsString == null)
                return (AbstractMessage)null;
            return msbConverter.ConvertFromHost((object)messageAsString);
        }

        public static AbstractMessage GetMessageBasedOnPlainString(byte[] message, IMsbConverter msbConverter)
        {
            string messageAsString = MessageConverterUtils.GetMessageAsString(message);
            if (messageAsString == null)
                return (AbstractMessage)null;
            return msbConverter.ConvertFromHost((object)messageAsString);
        }

        public static string GetMessageAsString(com.miracom.transceiverx.message.Message highwayMessage)
        {
            return highwayMessage.getDataAsString();
        }

        public static string GetMessageAsString(byte[] message)
        {
            return Encoding.UTF8.GetString(message);
        }


        public static void SetOriginatedName(XmlDocument document, string originatedName)
        {
            XmlElement xmlElement1 = document.DocumentElement["ORIGINATED"];
            if (xmlElement1 == null)
                return;
            XmlElement xmlElement2 = xmlElement1["ORIGINATEDNAME"];
            if (string.IsNullOrEmpty(xmlElement2.InnerText))
                xmlElement2.InnerText = originatedName;
        }

        public static string GetMessageBasedOnPlainString(com.miracom.transceiverx.message.Message highwayMessage)
        {
            return Encoding.Default.GetString(highwayMessage.getData());
        }

        public static string GetMessageBasedOnPlainString(byte[] message)
        {
            return Encoding.Default.GetString(message);
        }

        public static XmlDocument MakeDocument4Contents(string source)
        {
            if (string.IsNullOrEmpty(source))
                return (XmlDocument)null;
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(source);
                return xmlDocument;
            }
            catch (Exception ex)
            {
            }
            return (XmlDocument)null;
        }      
    }
}
