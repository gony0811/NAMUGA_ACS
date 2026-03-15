using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ACS.Communication;
using ACS.Communication.Msb;
using ACS.Core.Message;
using ACS.Core.Message.Model;
using ACS.Manager.Message;
using ACS.Utility;

namespace ACS.Manager
{
    public class MessageManagerExsImplement : MessageManagerExImplement, IMessageManagerExs
    {
        public XmlDocument CreateVehicleOrderCommandDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-VEHICLEORDERCOMMAND");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "KEY", "");
            element.InnerText = vehicleMessage.KeyData;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateVehiclePermit0031CommandDocument(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateDocument(vehicleMessage);
            document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText = "RAIL-VEHICLELGOCOMMAND";
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = "0031";
            data.AppendChild(element);

            return document;
        }


        public void SendVehicleOrderCommand(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateVehicleOrderCommandDocument(vehicleMessage);
            SendMessageToAcsEs(document);
        }

        public void SendVehiclePermit0031Command(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateVehiclePermit0031CommandDocument(vehicleMessage);
            SendMessageToAcsEs(document);
        }

        public void SendVehicleMessageMCodeAgvChargingFail(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeAgvChargingFailDocument(vehicleMessage);
            base.SendVehicleMessage(document);
        }
        public void SendVehicleMessageMCodeRecoverMissMagTag(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeRecoverMissMagTagDocument(vehicleMessage);          
            base.SendVehicleMessage(document);
        }
        public void SendVehicleMessageMCodeRecoverMissMagTagFail(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeRecoverMissMagTagFailDocument(vehicleMessage);
            base.SendVehicleMessage(document);
        }
        public void SendVehicleMessageMCodeRecoverMissMagTagSuccess(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeRecoverMissMagTagSuccessDocument(vehicleMessage);
            base.SendVehicleMessage(document);
        }

        public void SendVehicleMessageMCodeRecoverAgvOutRail(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeRecoverAgvOutRailDocument(vehicleMessage);
            base.SendVehicleMessage(document);
        }

        public void SendVehicleMessageMCodeRecoverAgvOutRailSuccess(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeRecoverAgvOutRailSuccessDocument(vehicleMessage);
            base.SendVehicleMessage(document);
        }

        public void SendVehicleMessageMCodeRecoverAgvOutRailFail(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeRecoverAgvOutRailFailDocument(vehicleMessage);
            base.SendVehicleMessage(document);
        }

        public void SendVehicleMessageMCodeAgvAutoStart(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeAgvAutoStartDocument(vehicleMessage);
            base.SendVehicleMessage(document);
        }

        public void SendVehicleMessageMCodeAgvAutoStartSuccess(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeAgvAutoStartSuccessDocument(vehicleMessage);
            base.SendVehicleMessage(document);
        }

        public void SendVehicleMessageMCodeAgvAutoStartFail(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeAgvAutoStartFailDocument(vehicleMessage);
            base.SendVehicleMessage(document);
        }

        public void SendVehicleMessageMCodeAgvTurnPbsOff(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeAgvTurnPbsOffDocument(vehicleMessage);
            base.SendVehicleMessage(document);
        }

        public void SendVehicleMessageMCodeAgvTurnPbsOffSuccess(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeAgvTurnPbsOffSuccessDocument(vehicleMessage);
            base.SendVehicleMessage(document);
        }

        public void SendVehicleMessageMCodeAgvTurnPbsOffFail(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeAgvTurnPbsOffFailDocument(vehicleMessage);
            base.SendVehicleMessage(document);
        }

        public void SendVehicleMessageMCodeRecoveryAgvBack(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeRecoveryAgvBackDocument(vehicleMessage);
            base.SendVehicleMessage(document);
        }

        public void SendVehicleMessageMCodeRecoveryAgvBackSuccess(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeRecoveryAgvBackSuccessDocument(vehicleMessage);
            base.SendVehicleMessage(document);
        }

        public void SendVehicleMessageMCodeRecoveryAgvBackFail(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeRecoveryAgvBackFailDocument(vehicleMessage);
            base.SendVehicleMessage(document);
        }

        public void SendVehicleMessageMCodeAgvSensorSonic(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeAgvSensorSonicDocument(vehicleMessage);
            base.SendVehicleMessage(document);
        }

        public void SendVehicleMessageMCodeAgvSensorSonicSuccess(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeAgvSensorSonicSuccessDocument(vehicleMessage);
            base.SendVehicleMessage(document);
        }

        public void SendVehicleMessageMCodeAgvSensorSonicFail(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeAgvSensorSonicFailDocument(vehicleMessage);
            base.SendVehicleMessage(document);
        }

        public void SendVehicleMessageMCodeHmiVersion(String messageName, VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateMCodeHmiVersionDocument(vehicleMessage);
            base.SendVehicleMessage(document);
        }



        public XmlDocument CreateMCodeAgvChargingFailDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-AGVCHARGINGFAIL");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeRecoverMissMagTagDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-RECOVERMISSMAGTAG");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeRecoverMissMagTagFailDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-RECOVERMISSMAGTAGFAIL");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeRecoverMissMagTagSuccessDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-RECOVERMISSMAGTAGSUCCESS");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeRecoverAgvOutRailDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-RECOVERAGVOUTRAIL");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }
        public XmlDocument CreateMCodeRecoverAgvOutRailSuccessDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-RECOVERAGVOUTRAILSUCCESS");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }
        public XmlDocument CreateMCodeRecoverAgvOutRailFailDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-RECOVERAGVOUTRAILFAIL");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeAgvAutoStartDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-AGVAUTOSTART");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeAgvAutoStartSuccessDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-AGVAUTOSTARTSUCCESS");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeAgvAutoStartFailDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-AGVAUTOSTARTFAIL");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeAgvTurnPbsOffDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-AGVTURNPBSOFF");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeAgvTurnPbsOffSuccessDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-AGVTURNPBSOFFSUCCESS");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;

            data.AppendChild(element);

            return document;
        }

        public override VehicleMessageEx CreateVehicleMessageFromTrans(XmlDocument document)
        {
            VehicleMessageEx vehicleMessage = base.CreateVehicleMessageFromTrans(document);
            
            String messageName = XmlUtility.GetDataFromXml(document, "//MESSAGENAME");
            String nodeId = "";
            String keyData = "";
            if (messageName.Equals("RAIL-VEHICLEORDERCOMMAND"))
            {
                messageName = "O_CODE";
                nodeId = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/CURRENTNODEID");
                keyData = XmlUtility.GetDataFromXml(document, "/MESSAGE/DATA/KEY");
                vehicleMessage.MessageName = messageName;
                vehicleMessage.NodeId = nodeId;
                vehicleMessage.KeyData = keyData;
            }
            return vehicleMessage;
        }


        public XmlDocument CreateMCodeAgvTurnPbsOffFailDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-AGVTURNPBSOFFFAIL");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeRecoveryAgvBackDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-RECOVERYAGVBACK");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeRecoveryAgvBackSuccessDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-RECOVERYAGVBACKSUCCESS");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeRecoveryAgvBackFailDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-RECOVERYAGVBACKFAIL");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeAgvSensorSonicDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-AGVSENSORSONIC");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeAgvSensorSonicSuccessDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-AGVSENSORSONICSUCCESS");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }
		

        public XmlDocument CreateMCodeAgvSensorSonicFailDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-AGVSENSORSONICFAIL");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

        public XmlDocument CreateMCodeHmiVersionDocument(VehicleMessageEx vehicleMessage)
        {
            vehicleMessage.MessageName = ("RAIL-HMIVERSION");
            XmlDocument document = CreateDocument(vehicleMessage);
            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }


        public void SendVehicleStopCommand(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateVehicleStopCommandDocument(vehicleMessage);
            SendMessageToAcsEs(document);
        }
        public XmlDocument CreateVehicleStopCommandDocument(VehicleMessageEx vehicleMessage)
        {
            XmlDocument document = CreateDocument(vehicleMessage);
            document.SelectSingleNode("/MESSAGE/HEADER/MESSAGENAME").InnerText = "RAIL-VEHICLESTOPCOMMAND";

            XmlElement data = document.DocumentElement["DATA"];

            XmlNode element = document.CreateNode(XmlNodeType.Element, this.messageNode.NodeNameOfVehicleId, "");
            element.InnerText = vehicleMessage.VehicleId;
            data.AppendChild(element);

            
            element = document.CreateNode(XmlNodeType.Element, "CURRENTNODEID", "");
            element.InnerText = vehicleMessage.NodeId;
            data.AppendChild(element);

            return document;
        }

    }
}
