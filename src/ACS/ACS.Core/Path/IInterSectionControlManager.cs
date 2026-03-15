using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;
using ACS.Core.Resource.Model;
using ACS.Core.Resource.Model;
using ACS.Core.Path.Model;
using System.Collections;


namespace ACS.Core.Path
{
    public interface IInterSectionControlManager
    {
        // reference information (InterSectionControl)
        /**
         * create referenceInformation.
         * 
         * @param interSectionControl
         */

        void CreateInterSectionControl(InterSectionControlEx interSectionControl);

        IList GetAllInterSectionControls();
        /**
         * <p>return interSectionControl data list by param interSectionId</p>
         * return value will be list because intersection can has startNode more than one.
         * 
         * @param interSectionId
         * @return
         */
        IList GetInterSectionControls(String interSectionId);
        /**
         * <p>return interSectionControl data by param interSectionId and startNodeId</p>
         * It will be return only one value because it can be only one endNode in startNode without two directions.
         * 
         * @param interSectionId
         * @param startNodeId
         * @return
         */
        InterSectionControlEx GetInterSectionControls(String interSectionId, String startNodeId);

        InterSectionControlEx GetInterSectionControl(String interSectionId, int sequence);
        /**
         * return interSectionControl data by param startNodeId and endNodeId
         * 
         * @param startNodeId
         * @param endNodeId
         * @return
         */
        InterSectionControlEx GetInterSectionControl(String startNodeId, String endNodeId);
        /**
         * Only one interSectionControl returned by startNodeId
         * In SDV, only one startNodeId exist in one interSection.
         * 
         * @param startNodeId
         * @return
         */
        InterSectionControlEx GetInterSectionControlByStartNode(String startNodeId);
        /**
         * endNodeId can be more than 2.
         * search InterSection list by 'LIKE' searching
         * 
         * @param endNodeId
         * @return
         */
        IList GetInterSectionControlByEndNode(String endNodeId);
        /**
         * Only one interSectionControl returned by endNodeId
         * In SDV, only one endNodeId exist in one interSection.
         * 
         * @param endNodeId
         * @return
         */
        String GetInterSectionIdControlByEndNode(String endNodeId);

        /**
         * update whole properties of interSectionControl object to database.
         * 
         * @param interSectionControl
         */
        void UpdateInterSectionControl(InterSectionControlEx interSectionControl);
        /**
         * update only interval value of interSectionControl
         * @param interSectionId
         * @param interval
         * @return
         */
        int UpdateInterSectionControlInterval(String interSectionId, int interval);
        /**
         * update only sequence value of interSectionControl
         * @param interSectionId
         * @param sequence
         * @return
         */
        int UpdateInterSectionControlSequence(String interSectionId, int sequence);
        /**
         * update only previousNode check flag of interSectionControl
         * @param interSectionId
         * @param checkFlag
         * @return
         */
        int UpdateInterSectionControlCheckPrevNode(String interSectionId, String checkFlag);

        /**
         * delete interSectionControl by param object
         * @param interSectionControl
         */
        void DeleteInterSectionControl(InterSectionControlEx interSectionControl);
        /**
         * delete all interSectionControl data.
         * @return
         */
        int DeleteAllInterSectionControl();
        /**
         * get InterSection's startNodeIds by param interSectionId
         * 
         * @param interSectionId
         * @return
         */
        IList GetStartNodesByInterSectionId(String interSectionId);
        /**
         * get all InterSection's startNodeIds
         * @return
         */
        IList GetAllStartNodesByInterSectionId();

        // runtime data(CurrentInterSectionInfo)
        /**
         * create runtime data of interSectionControl
         * currentInterSectionInfo data can be created and deleted in realTime
         * 
         * @param currInterSectionInfo
         */
        void CreateCurrentInterSectionInfo(CurrentInterSectionInfoEx currInterSectionInfo);
        /**
         * get all CurrentInterSections
         * 
         * @return
         */
        IList GetAllCurrentInterSections();
        /**
         * get runtime data of interSectionControl by param interSectionId
         * 
         * @param interSectionId
         * @return
         */
        CurrentInterSectionInfoEx GetCurrentInterSectionInfoById(String interSectionId);
        /**
         * get runtime data of interSectionControl by param startNodeId
         * 
         * @param startNodeId
         * @return
         */
        CurrentInterSectionInfoEx GetCurrentInterSectionInfo(String startNodeId);
        /**
         * get runtime data list of interSectionControl by param state
         * 
         * @param state
         * @return
         */
        IList GetCurrentInterSectionInfoByState(String state);
        /**
         * update whole interSectionControl's property 
         * 
         * @param currInterSectionInfo
         */
        void UpdateCurrentInterSectionInfo(CurrentInterSectionInfoEx currInterSectionInfo);
        /**
         * update only currentInterSectionControl's direction property
         * 
         * @param currInterSectionInfo
         * @param direction
         * @return
         */
        int UpdateCurrentInterSectionInfoDirection(CurrentInterSectionInfoEx currInterSectionInfo, String direction);
        /**
         * 
         * @param currInterSectionInfo
         * @param direction
         * @param state
         * @return
         */
        int UpdateCurrentInterSectionInfoDirection(CurrentInterSectionInfoEx currInterSectionInfo, String direction, String state);
        /**
         * 
         * @param interSectionId
         * @param directionNode
         * @return
         */
        int UpdateCurrentInterSectionInfoDirection(String interSectionId, String directionNode);
        int UpdateCurrentInterSectionInfoDirection(String interSectionId, String directionNode, String state);
        /**
         * 
         * @param interSectionId
         * @param state
         * @return
         */
        int UpdateCurrentInterSectionInfoState(String interSectionId, String state);

        /**
         * delete currentInterSectionInfo
         * 
         * @param currInterSectionInfo
         */
        void DeleteCurrentInterSectionInfo(CurrentInterSectionInfoEx currInterSectionInfo);
        /**
         * delete currentInterSectionInfo by param interSectionId
         * 
         * @param interSectionId
         */
        void DeleteCurrentInterSectionInfo(String interSectionId);

        /**
         * <p>judge that can change direction now.</p>
         * 1. check schedule time is over.
         * 2. check currentInterSection's state (can change only 'CHANGED' state) 
         * 
         * @param interSectionControl
         * @return
         */
        bool PossibleToChangeDirection(InterSectionControlEx interSectionControl);
        /**
         * <p>judge that can change direction now.</p>
         * 1. check schedule time is over.
         * 2. check currentInterSection's state (can change only 'CHANGED' state) 
         * 
         * @param interSectionControl
         * @param currInterSection
         * @return
         */
        bool PossibleToChangeDirection(InterSectionControlEx interSectionControl, CurrentInterSectionInfoEx currInterSection);
        /**
         * 
         * 
         * @param interSectionList
         * @see ChangeInterSectionDirectionJob
         * @return
         */
        bool ChangeDirection(List<InterSectionControlEx> interSectionList);
        /**
         * 
         * @param vehicle
         * @return
         */
        bool PossibleToGo(VehicleEx vehicle);
        /**
         * <p>If CurrentInterSectionInfo's currentDirectionNodeId is agv's nodeId,</p>
         * <p>And even though CurrentInterSectionInfo's currentDirectionNodeId is not equal agv's nodeId, 
         * <p>other waiting AGVs does not exist in current interSection</p>
         * AGV can go into interSection, this method check AGV can go or not.
         * 
         * !! During check the logic, vehicle location should be needed.
         * Currently vehicle location did not changed in cross_start, end node.
         * Consider it, need to meeting with SDV.  
         * 
         * @param interSectionId
         * @param vehicle
         * @return
         */
        bool PossibleToGo(String interSectionId, VehicleEx vehicle);

        void ChangeNextSequent(CurrentInterSectionInfoEx currIsInfo, String interSectionId);


        bool HaveRunningAGVInStartNode(String interSectionId);

        /**
         * check exist agv on the checkNode in interSection
         * checkNodeIds will be registered in InterSection table.
         * 
         * @param interSectionId
         * @return
         */
        bool ExistAGVinInterSection(String interSectionId);
    }

}
