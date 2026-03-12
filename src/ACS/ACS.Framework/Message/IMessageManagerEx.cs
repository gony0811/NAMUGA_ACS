using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using ACS.Framework.Message.Model;
using ACS.Framework.Message.Model.Control;
using ACS.Framework.Message.Model.Server;
using ACS.Framework.Resource.Model.Factory.Unit;
using ACS.Framework.Message.Model.Ui;
using ACS.Framework.Resource.Model;

namespace ACS.Framework.Message
{
    public interface IMessageManagerEx
    {

        string MessageTemplatePath { get; set; }
        XmlDocument TemplateDocument { get; set; }

        MessageNode MessageNode { get; set; }

        bool Validate(string paramString);

        bool ValidateCarrier(string paramString);

        bool ValidateCarrier(TransferMessageEx paramTransferMessage);

        bool ValidateAndPopulateCarrier(TransferMessageEx paramTransferMessage);

        bool ValidateAndPopulateCarrierByTransportCommand(TransferMessageEx paramTransferMessage);

        bool PopulateCarrier(TransferMessageEx paramTransferMessage);

        bool ValidateTransportCommand(String paramString);

        bool ValidateTransportCommand(TransferMessageEx paramTransferMessage);

        bool ValidateAndPopulateTransportCommand(TransferMessageEx paramTransferMessage);

        bool ValidateAndPopulateTransportCommandByVehicle(VehicleMessageEx paramVehicleMessage);

        bool PopulateTransportCommand(TransferMessageEx paramTransferMessage);

        void PopulateTransportCommandInfo(TransferMessageEx paramTransferMessage);

        TransferMessageEx CreateTransferMessage(XmlDocument paramDocument);

        TransferMessageEx CreateTransferMessage(UiTransportMessageEx paramUiTransportMessage);

        TransferMessageEx CreateTransferMessage(UiTransportCancelMessageEx paramUiTransportCancelMessage);

        TransferMessageEx CreateTransferMessage(UiTransportDeleteMessageEx paramUiTransportDeleteMessage);

        VehicleMessageEx CreateVehicleMessage(String paramString);

        VehicleMessageEx CreateVehicleMessage(IPacket paramReceivePacket);

        VehicleMessageEx CreateVehicleMessageFromES(XmlDocument paramDocument);

        VehicleMessageEx CreateVehicleMessageFromTrans(XmlDocument paramDocument);

        VehicleMessageEx CreateVehicleMessageFromDaemon(XmlDocument paramDocument);
        AlarmMessage CreateAlarmMessage(XmlDocument paramDocument);

        XmlDocument CreateDocument();

        XmlDocument CreateDocument(ControlMessage controlMessage);

        XmlDocument CreateDocument(AbstractMessage paramAbstractMessage);

        XmlDocument CreateDocument(String paramString);

        XmlDocument CreateDocument(VehicleMessageEx paramVehicleMessage);

        XmlDocument CreateTransportCommandSourceDocument(TransferMessageEx paramTransferMessage);

        void SetHeaderInfoToMessage(XmlDocument paramDocument, AbstractMessage paramAbstractMessage);

        void SetOriginatedInfoToMessage(XmlDocument paramDocument, AbstractMessage paramAbstractMessage);

        void SetHeaderInfoToMessage(AbstractMessage paramAbstractMessage1, AbstractMessage paramAbstractMessage2);

        void SetOriginatedInfoToMessage(AbstractMessage paramAbstractMessage1, AbstractMessage paramAbstractMessage2);

        void SetHeaderInfoToDocument(AbstractMessage paramAbstractMessage, XmlDocument paramDocument);

        void SetDefaultHeaderInfoToDocument(String paramString, XmlDocument paramDocument);

        void SetDefaultHeaderInfoToDocument(XmlDocument paramDocument);

        void SetOriginatedInfoToDocument(AbstractMessage paramAbstractMessage, XmlDocument paramDocument);

        void SetDefaultOriginatedInfoToDocument(String paramString, XmlDocument paramDocument);

        void SetDefaultOriginatedInfoToDocument(String paramString1, String paramString2, XmlDocument paramDocument);

        void SetDefaultOriginatedInfoToDocument(XmlDocument paramDocument);

        bool ValidateVehicle(VehicleMessageEx paramVehicleMessage);

        bool PopulateVehicle(VehicleMessageEx paramVehicleMessage);

        ControlMessageEx CreateControlMessage(XmlDocument paramDocument);

        ControlMessageEx CreateControlMessage(String paramString1, String paramString2);

        void SendVehicleMessage(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageCCode(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageSCode(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageTCodeEnter(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageTCodePermission(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageUCode(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageOCode(String paramString, VehicleMessageEx paramVehicleMessage);
        void SendVehicleMessageLCode(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageRCodeVoltage(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageRCodeCapacity(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeChargeStart(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeChargeComplete(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeDestOccupied(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeSourceEmpty(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeVehicleEmpty(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeVehicleOccupied(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageECode(String paramString, VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageInform(VehicleMessageEx paramVehicleMessage, String paramString);

        void SendVehicleMessageInform(VehicleMessageEx paramVehicleMessage, String paramString1, String paramString2);

        void SendTransportCommandSource(TransferMessageEx paramTransferMessage);

        void SendTransportCommandSource(VehicleMessageEx paramVehicleMessage);

        void SendTransportCommandDest(VehicleMessageEx paramVehicleMessage);

        void SendTransportCommandDest(TransferMessageEx paramTransferMessage);

        void SendTransportCommandNewDest(TransferMessageEx paramTransferMessage);

        void SendTransportCommandWaitpoint(TransferMessageEx paramTransferMessage);

        void SendTransportCommandWaitpoint0000(TransferMessageEx paramTransferMessage);

        void SendTransportCommandWaitpoint(VehicleMessageEx paramVehicleMessage);

        void SendTransportCommandWaitpoint0000(VehicleMessageEx paramVehicleMessage);
        //KSB RGV에 작업자가 강제로 Tray 올려 놓았을때 Panel AB Buffer 강제 이동
        void SendTransportCommandWaitpoint0000_RGV(VehicleMessageEx paramVehicleMessage);

        void SendTransportCommandVehicleDestNode(VehicleMessageEx paramVehicleMessage);

        void SendMoveCommandTarget(UiMoveVehicleMessageEx paramUiMoveVehicleMessage);

        void SendVehiclePermitCommand(VehicleMessageEx paramVehicleMessage);

        void SendOtherVehiclePermitCommand(VehicleMessageEx paramVehicleMessage);

        void SendBatteryVoltageReq(VehicleMessageEx paramVehicleMessage);

        void SendBatteryCapacityReq(VehicleMessageEx paramVehicleMessage);

        UiTransportMessageEx CreateUiTransportMessage(XmlDocument paramDocument);

        UiTransportCancelMessageEx CreateUiTransportCancelMessage(XmlDocument paramDocument);

        UiTransportDeleteMessageEx CreateUiTransportDeleteMessage(XmlDocument paramDocument);

        UiMoveVehicleMessageEx CreateUiMoveVehicleMessage(XmlDocument paramDocument);

        void ReplyMessageToUi(AbstractMessage paramAbstractMessage);

        XmlDocument CreateReplyDocument(AbstractMessage paramAbstractMessage);

        void ReplyTransportMessageValidationNGToUi(AbstractMessage paramAbstractMessage);

        void ReplyTransportStationValidationNGToUi(AbstractMessage paramAbstractMessage);

        void ReplyTransportExistJobNGToUi(AbstractMessage paramAbstractMessage);

        void ReplyTransportCarrierCreateNGToUi(AbstractMessage paramAbstractMessage);

        void ReplyTransportJobCreateNGToUi(AbstractMessage paramAbstractMessage);

        void ReplyTransportPathValidationNGToUi(AbstractMessage paramAbstractMessage);

        void SendMessageToAcsEs(XmlDocument paramDocument);

        void SendVehicleMessageMCodeDestPIOConnectError(VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeDestPIORequestError(VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeDestPIORunError(VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeDestPIOPortCheckError(VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeSourcePIOConnectError(VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeSourcePIORequestError(VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeSourcePIORunError(VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeSourcePIOPortCheckError(VehicleMessageEx paramVehicleMessage);

        XmlDocument CreateMCodePIOErrorDocument(VehicleMessageEx paramVehicleMessage, String paramString);

        VehicleMessageEx CreateVehicleMessage(VehicleEx paramVehicle);

        void SendVehicleMessageMCodeCarrierLoaded(VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeCarrierRemoved(VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodePortError(VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeConveyorLoadingTimeout(VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeConveyorUnloadingTimeout(VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeStart(VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeStop(VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodeMainboardVersion(VehicleMessageEx paramVehicleMessage);

        void SendVehicleMessageMCodePLCVersion(VehicleMessageEx paramVehicleMessage);

        XmlDocument CreateMCodePortErrorDocument(VehicleMessageEx paramVehicleMessage);

        XmlDocument CreateMCodeCarrierLoaded(VehicleMessageEx paramVehicleMessage);

        XmlDocument CreateMCodeCarrierRemoved(VehicleMessageEx paramVehicleMessage);

        XmlDocument CreateTransportCommandNewDestDocument(TransferMessageEx paramTransferMessage);

        void SendRestartNio(VehicleMessageEx paramVehicleMessage);

        void SendRestartNio(VehicleEx paramVehicleACS);

        UiTruncateMessageEx CreateUiTruncateMessage(XmlDocument paramDocument);
    }

}
