using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using ACS.Extension.Framework.Path;
using ACS.Extension.Framework.Resource;
using ACS.Extension.Framework.Path.Model;
using ACS.Framework.Base;
using ACS.Framework.Resource;
using ACS.Framework.Resource.Model;
using ACS.Framework.Alarm;
using ACS.Framework.Alarm.Model;
using ACS.Framework.Path.Model;
using ACS.Manager.Resource;
using NHibernate.Criterion;
using ACS.Extension.Framework.Path.Comparator;
using NHibernate.Engine;
using ACS.Extension.Framework.Alarm.Model;

namespace ACS.Extension.Manager
{
    public class IntersectionControlManagerExImplement : AbstractManager, IInterSectionControlManager
    {

        public static char DELIMITER_NODEID = ',';
        public static string CROSS_WAIT_INTERSECTION = "INTERS";
        private IResourceManagerExs ResourceManager { get; set; }
        private IAlarmManagerEx AlarmManager { get; set; }    

        private IDictionary<String, List<LinkEx>> ToNodelinksMap { get; set; }

        private int IgnoreNodeCheckTime = 60;   // If some agv located in startNode, nodeCheckTime is older than this time, ignore
        private int MaxSearchNextSeqCnt = 20;
        private int MaxSearchNodeCnt = 20;



        public int GetIgnoreNodeCheckTime { get; set; }

        public int GetMaxSearchNodeCnt { get; set; }

        public int GetMaxSearchNextSeqCnt { get; set; }

        public IComparer<VehicleEx> NodeCheckTimeComparator { get; set; }

        public void CreateCurrentInterSectionInfo(CurrentInterSectionInfoEx currInterSectionInfo)
        {
            logger.Info("currentInterSectionInfo will be inserted, " + currInterSectionInfo.ToString());
            this.PersistentDao.Save(currInterSectionInfo);
        }

        public void CreateInterSectionControl(InterSectionControlEx interSectionControl)
        {
            this.PersistentDao.Save(interSectionControl);
        }

        public int DeleteAllInterSectionControl()
        {
            return this.PersistentDao.DeleteAll(typeof(InterSectionControlEx));
        }


        public void DeleteCurrentInterSectionInfo(CurrentInterSectionInfoEx currInterSectionInfo)
        {
            logger.Info("delete currentInterSectionInfo.");
            this.PersistentDao.Delete(currInterSectionInfo);
        }


        public void DeleteCurrentInterSectionInfo(string interSectionId)
        {
            logger.Info("delete currentInterSectionInfo{" + interSectionId + "}");
            StringBuilder sbInterSectionId = new StringBuilder(interSectionId);
            this.PersistentDao.Delete(typeof(CurrentInterSectionInfoEx), sbInterSectionId);
        }


        public void DeleteInterSectionControl(InterSectionControlEx interSectionControl)
        {
            this.PersistentDao.Delete(interSectionControl);
        }

        //@SuppressWarnings("unchecked")


        public IList GetAllCurrentInterSections()
        {
            return this.PersistentDao.FindAll(typeof(CurrentInterSectionInfoEx));
        }

        //@SuppressWarnings("unchecked")


        public CurrentInterSectionInfoEx GetCurrentInterSectionInfo(string startNodeId)
        {
            IList result = this.PersistentDao.FindByAttribute(typeof(CurrentInterSectionInfoEx), "currentDirectionNode", startNodeId);
            if (result == null)
            {
                return null;
            }
            return (CurrentInterSectionInfoEx)result[0];
        }


        public CurrentInterSectionInfoEx GetCurrentInterSectionInfoById(string interSectionId)
        {
            StringBuilder sbInterSectionId = new StringBuilder(interSectionId);
            return (CurrentInterSectionInfoEx)this.PersistentDao.Find(typeof(CurrentInterSectionInfoEx), sbInterSectionId, false);
        }

        //@SuppressWarnings("unchecked")

        public IList GetCurrentInterSectionInfoByState(string state)
        {
            return this.PersistentDao.FindByAttribute(typeof(CurrentInterSectionInfoEx), "state", state);
        }

        //@SuppressWarnings("unchecked")


        public IList GetAllInterSectionControls()
        {
            return this.PersistentDao.FindAll(typeof(InterSectionControlEx));
        }

        //@SuppressWarnings("unchecked")


        public InterSectionControlEx GetInterSectionControl(string startNodeId, string endNodeId)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("startNodeId", startNodeId);
            attributes.Add("endNodeId", endNodeId);

            IList result = this.PersistentDao.FindByAttributes(typeof(InterSectionControlEx), attributes);
            if (result.Count == 0)
            {
                return null;
            }
            return (InterSectionControlEx)result[0];
        }

        //@SuppressWarnings("unchecked")


        public InterSectionControlEx GetInterSectionControlByStartNode(string startNodeId)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("startNodeId", startNodeId);

            IList result = this.PersistentDao.FindByAttributes(typeof(InterSectionControlEx), attributes);
            if (result.Count == 0)
            {
                return null;
            }
            return (InterSectionControlEx)result[0];
        }

        //@SuppressWarnings("unchecked")


        public IList GetInterSectionControlByEndNode(string endNodeId)
        {
            DetachedCriteria criteria = DetachedCriteria.For(typeof(InterSectionControlEx));
            criteria.Add(Restrictions.Like("endNodeId", "%" + endNodeId + "%"));
            return this.PersistentDao.FindByCriteria(criteria);

        }

        //@SuppressWarnings("unchecked")


        public string GetInterSectionIdControlByEndNode(string endNodeId)
        {

            string interSectionId = "";

            DetachedCriteria criteria = DetachedCriteria.For(typeof(InterSectionControlEx));
            criteria.SetProjection(Projections.Property("interSectionId"));
            criteria.Add(Restrictions.Like("endNodeId", "%" + endNodeId + "%"));
            IList result = this.PersistentDao.FindByCriteria(criteria);

            if (result.Count > 0)
            {
                if (result.Count == 1)
                {
                    interSectionId = (string)result[0];

                }
                else
                {
                    logger.Error("More than two intersections cannot be selected for one endnode{" + endNodeId + "}.");
                }
            }
            return interSectionId;
        }

        //@SuppressWarnings("unchecked")


        public IList GetInterSectionControls(string interSectionId)
        {
            IList result = this.PersistentDao.FindByAttribute(typeof(InterSectionControlEx), "interSectionId", interSectionId);
            if (result.Count == 0)
            {
                return new List<InterSectionControlEx>();
            }
            return result;
        }

        //@SuppressWarnings("unchecked")


        public InterSectionControlEx GetInterSectionControls(string interSectionId, string startNodeId)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("interSectionId", interSectionId);
            attributes.Add("startNodeId", startNodeId);

            IList result = this.PersistentDao.FindByAttributes(typeof(InterSectionControlEx), attributes);
            if (result.Count == 0)
            {
                return null;
            }
            return (InterSectionControlEx)result[0];
        }

        //@SuppressWarnings("unchecked")


        public InterSectionControlEx GetInterSectionControl(string interSectionId, int sequence)
        {
            Dictionary<string, object> attributes = new Dictionary<string, object>();
            attributes.Add("interSectionId", interSectionId);
            attributes.Add("sequence", sequence);

            IList result = this.PersistentDao.FindByAttributes(typeof(InterSectionControlEx), attributes);
            if (result.Count == 0)
            {
                return null;
            }
            return (InterSectionControlEx)result[0];
        }


        public int UpdateCurrentInterSectionInfoDirection(string interSectionId, string directionNode)
        {
            return this.UpdateCurrentInterSectionInfoDirection(interSectionId, directionNode, CurrentInterSectionInfoEx.STATE_CHANGED);
        }

        //@SuppressWarnings("unchecked")

        public int UpdateCurrentInterSectionInfoDirection(string interSectionId, string directionNode, string state)
        {
            logger.Info("CurrentInterSection{" + interSectionId + "}'s directionNode will be changed to : " + directionNode + ", state : " + state);
            Dictionary<string, object> setAttributes = new Dictionary<string, object>();
            setAttributes.Add("currentDirectionNode", directionNode);
            setAttributes.Add("state", state);
            setAttributes.Add("changedTime", DateTime.Now);

            Dictionary<string, object> conditionAttributes = new Dictionary<string, object>();
            conditionAttributes.Add("id", interSectionId);

            return this.PersistentDao.UpdateByAttributes(typeof(CurrentInterSectionInfoEx), setAttributes, conditionAttributes);
        }


        public int UpdateCurrentInterSectionInfoDirection(CurrentInterSectionInfoEx currInterSectionInfo, string direction)
        {
            return this.UpdateCurrentInterSectionInfoDirection(currInterSectionInfo, direction, CurrentInterSectionInfoEx.STATE_CHANGED);
        }


        public int UpdateCurrentInterSectionInfoDirection(CurrentInterSectionInfoEx currInterSectionInfo, string direction, string state)
        {
            string interSectionId = currInterSectionInfo.Id;
            return this.UpdateCurrentInterSectionInfoDirection(interSectionId, direction, state);
        }


        //@SuppressWarnings("unchecked")

        public int UpdateCurrentInterSectionInfoState(string interSectionId, string state)
        {
            logger.Info("CurrentInterSection{" + interSectionId + "}'s state will be changed to : " + state);
            Dictionary<string, object> setAttributes = new Dictionary<string, object>();
            setAttributes.Add("state", state);
            setAttributes.Add("changedTime", DateTime.Now);

            Dictionary<string, object> conditionAttributes = new Dictionary<string, object>();
            conditionAttributes.Add("id", interSectionId);

            return this.PersistentDao.UpdateByAttributes(typeof(CurrentInterSectionInfoEx), setAttributes, conditionAttributes);
        }


        public void UpdateCurrentInterSectionInfo(CurrentInterSectionInfoEx currInterSectionInfo)
        {
            this.PersistentDao.Update(currInterSectionInfo);
        }


        public void UpdateInterSectionControl(InterSectionControlEx interSectionControl)
        {
            this.PersistentDao.Update(interSectionControl);
        }


        public int UpdateInterSectionControlCheckPrevNode(string interSectionId, string checkFlag)
        {
            logger.Info("CurrentInterSectionInfo{" + interSectionId + "}'s prevCheckFlag will be changed to : " + checkFlag);
            return this.PersistentDao.Update(typeof(InterSectionControlEx), "checkPreviousNode", checkFlag, interSectionId);
        }


        public int UpdateInterSectionControlInterval(string interSectionId, int interval)
        {
            logger.Info("CurrentInterSectionInfo{" + interSectionId + "}'s changeInterval will be changed to : " + interval);
            return this.PersistentDao.Update(typeof(InterSectionControlEx), "interval", interval, interSectionId);
        }


        public int UpdateInterSectionControlSequence(string interSectionId, int sequence)
        {
            logger.Info("CurrentInterSectionInfo{" + interSectionId + "}'s sequence will be changed to : " + sequence);
            return this.PersistentDao.Update(typeof(InterSectionControlEx), "sequence", sequence, interSectionId);
        }

        //@SuppressWarnings("unchecked")

        public IList GetStartNodesByInterSectionId(string interSectionId)
        {
            DetachedCriteria criteria = DetachedCriteria.For(typeof(InterSectionControlEx));
            criteria.SetProjection(Projections.Property("startNodeId"));
            criteria.Add(Restrictions.Eq("interSectionId", interSectionId));
            return this.PersistentDao.FindByCriteria(criteria);
        }

        //@SuppressWarnings("unchecked")

        public IList GetAllStartNodesByInterSectionId()
        {
            DetachedCriteria criteria = DetachedCriteria.For(typeof(InterSectionControlEx));
            criteria.SetProjection(Projections.Property("startNodeId"));
            return this.PersistentDao.FindByCriteria(criteria);
        }

        /**
        * update currentInterSection's direction to nextInterSection, default state is 'CHANGED'
        * 
        * @param currInterSectionInfo
        * @param interSectionControl
        */
        public void UpdateCurrentInterSectionToNextSequence(CurrentInterSectionInfoEx currInterSectionInfo, InterSectionControlEx interSectionControl)
        {
            this.UpdateCurrentInterSectionToNextSequence(currInterSectionInfo, interSectionControl, CurrentInterSectionInfoEx.STATE_CHANGED);
        }

        /**
        * update currentInterSection's direction to nextInterSection by parameter state('CHANGED' or 'CHANGING')
        * 
        * @param currInterSectionInfo
        * @param interSectionControl
        * @param state
        */
        public void UpdateCurrentInterSectionToNextSequence(CurrentInterSectionInfoEx currInterSectionInfo, InterSectionControlEx interSectionControl, string state)
        {
            if (currInterSectionInfo != null)
            {
                string interSectionId = currInterSectionInfo.Id;
                string currentNodeId = currInterSectionInfo.CurrentDirectionNode;
                //  InterSectionControl nextInterSection = this.getInterSectionControl(interSectionId, interSectionControl.getSequence()+1);
                InterSectionControlEx nextInterSection = this.GetNextSequenceInterSection(interSectionControl);
                if (nextInterSection == null)
                {
                    nextInterSection = this.GetInterSectionControl(interSectionId, 1);
                }
                if (nextInterSection == null)
                {
                    logger.Error("Can not find next node of InterSection{" + interSectionId + "}. current : " + currentNodeId);
                    return;
                }

                currInterSectionInfo.CurrentDirectionNode = nextInterSection.StartNodeId;
                currInterSectionInfo.State = state;
                currInterSectionInfo.ChangedTime = DateTime.Now;
                logger.Info("CurrentInterSection will be updated, Node : " + nextInterSection.StartNodeId + ", State : " + state + ", ChangedTime : " + DateTime.Now);
                this.UpdateCurrentInterSectionInfo(currInterSectionInfo);
            }
        }


        private InterSectionControlEx GetNextSequenceInterSection(InterSectionControlEx interSectionControl)
        {
            InterSectionControlEx nextInterSection = null;
            string interSectionId = interSectionControl.InterSectionId;
            int searchSeq = interSectionControl.Sequence + 1;
            while (GetMaxSearchNextSeqCnt >= searchSeq)
            {
                nextInterSection = this.GetInterSectionControl(interSectionId, searchSeq);
                if (nextInterSection != null)
                {
                    break;
                }
                searchSeq++;
            }
            return nextInterSection;
        }


        public bool PossibleToChangeDirection(InterSectionControlEx interSectionControl)
        {
            CurrentInterSectionInfoEx currInterSection = this.GetCurrentInterSectionInfoById(interSectionControl.InterSectionId);
            return PossibleToChangeDirection(interSectionControl, currInterSection);
        }


        public bool PossibleToChangeDirection(InterSectionControlEx interSectionControl, CurrentInterSectionInfoEx currInterSection)
        {
            // default change condition
            // 1. current stateChangedTime��interSectionControl���깅줉��interval��吏�궃 寃쎌슦
            // 
            DateTime datetime = new DateTime();
            datetime = DateTime.Now;
            datetime.AddSeconds(-interSectionControl.Interval);
            DateTime currentTime = datetime;

            // 1. interSectionControl��startNode��AGV媛��녿뒗 寃쎌슦 time留뚯쑝濡�changeDirection
            if (currInterSection != null && currentTime.CompareTo(currInterSection.ChangedTime) > 0)
            {
                // �쒓컙留�check. �ㅻⅨ validation��諛뽰뿉��
                return true;
            }
            return false;
        }

        /**
        * change direction at one time for reduing db transaction.
        * 
        * @param interSectionList
        * @return
        * @see ChangeInterSectionDirectionJob
        */
        //@SuppressWarnings("unchecked")

        public bool ChangeDirection(IList allInterSectionList)
        {
            long startTime = DateTime.Now.Millisecond;
            NodeCheckTimeComparator compare = new NodeCheckTimeComparator();
            // 1. getCurrentInterSections
            IList currIsList = GetAllCurrentInterSections();

            // 2. get AGV list by all startNodeIds of InterSectionControls.
            IList startNodeIds = this.GetAllStartNodesByInterSectionId();
            IList vehicles = this.ResourceManager.GetVehiclesByInterSectionStartNodes(startNodeIds);
            Dictionary<string, VehicleEx> nodeExistAgvs = new Dictionary<string, VehicleEx>();
            foreach (VehicleEx vehicle in vehicles)
            {
                logger.Info("Found " + vehicle);
                nodeExistAgvs.Add(vehicle.CurrentNodeId, vehicle);
            }

            Dictionary<string, InterSectionControlEx> isMap = new Dictionary<string, InterSectionControlEx>();
            foreach (InterSectionControlEx interSection in allInterSectionList)
            {
                string key = interSection.InterSectionId + ":" + interSection.StartNodeId;
                isMap.Add(key, interSection);
            }

            foreach (CurrentInterSectionInfoEx currInterSection in currIsList)
            {

                InterSectionControlEx interSection = new InterSectionControlEx();
                isMap.TryGetValue(currInterSection.Id + ":" + currInterSection.CurrentDirectionNode, out interSection);
                string startNodeId = currInterSection.CurrentDirectionNode;
                if (interSection != null)
                {
                    if (this.PossibleToChangeDirection(interSection, currInterSection))
                    {
                        if (this.ExistAGVinInterSection(interSection.InterSectionId))
                        {
                            if (CurrentInterSectionInfoEx.STATE_CHANGED.Equals(currInterSection.State))
                            {
                                logger.Info("Other AGV is running in InterSection{" + currInterSection.Id + "}, state will be changed to '" + CurrentInterSectionInfoEx.STATE_CHANGING + "'");
                                IList crossStartNodes = this.GetStartNodesByInterSectionId(interSection.InterSectionId);
                                List<VehicleEx> otherWaitAgvs = (List<VehicleEx>)this.GetOtherVehiclesOnStartNode(startNodeId, crossStartNodes, nodeExistAgvs);
                                if (otherWaitAgvs.Count == 0)
                                {
                                    logger.Info("Waiting AGV does not exist in {" + currInterSection.Id + "}, direction will be changed to nextSequence.");
                                    //this.updateCurrentInterSectionToNextSequence(currInterSection, interSection);
                                    this.UpdateCurrentInterSectionToNextSequence(currInterSection, interSection, CurrentInterSectionInfoEx.STATE_CHANGING);
                                }
                                else
                                {
                                    otherWaitAgvs.Sort(NodeCheckTimeComparator);
                                    VehicleEx oldestAGV = (VehicleEx)otherWaitAgvs[0];
                                    if (oldestAGV != null)
                                    {
                                        logger.Info("Oldest AGV[" + oldestAGV.Id + "] in " + currInterSection.Id);
                                        this.UpdateCurrentInterSectionInfoDirection(currInterSection, oldestAGV.CurrentNodeId, CurrentInterSectionInfoEx.STATE_CHANGING);
                                    }
                                    else
                                    {
                                        logger.Warn("Can not find AGV which has oldest nodeCheckTime, " + currInterSection.Id + "'s direction will be changed to nextSequence.");
                                        //this.updateCurrentInterSectionToNextSequence(currInterSection, interSection);
                                        this.UpdateCurrentInterSectionToNextSequence(currInterSection, interSection, CurrentInterSectionInfoEx.STATE_CHANGING);
                                    }
                                }
                            }
                            else
                            {
                                logger.Warn("Other AGV is running in InterSection{" + currInterSection.Id + "} " + "can not change state to " + CurrentInterSectionInfoEx.STATE_CHANGED);
                            }
                        }
                        else
                        {
                            if (CurrentInterSectionInfoEx.STATE_CHANGED.Equals(currInterSection.State))
                            {
                                VehicleEx existAGV = null;
                                nodeExistAgvs.TryGetValue(startNodeId, out existAGV);
                                if (existAGV != null)
                                {
                                    DateTime datetime = new DateTime();
                                    datetime = DateTime.Now;
                                    datetime.AddSeconds(-IgnoreNodeCheckTime);
                                    DateTime currentTime = datetime;

                                    if (currentTime.CompareTo(existAGV.NodeCheckTime) > 0)
                                    {
                                        logger.Info("AGV{" + existAGV.Id + "} exist in startNode, but nodeCheckTime is old. CurrentInterSection{" + currInterSection.Id + "}'s direction will be changed.");
                                        this.UpdateCurrentInterSectionToNextSequence(currInterSection, interSection);
                                    }
                                    else
                                    {
                                        if (VehicleEx.RUNSTATE_RUN.Equals(existAGV.RunState))
                                        {
                                            logger.Info("AGV running on the startNode{" + startNodeId + "}. CurrentInterSection{" + currInterSection.Id + "} state will be changed to '" + CurrentInterSectionInfoEx.STATE_CHANGING + "'");
                                            //this.updateCurrentInterSectionToNextSequence(currInterSection, interSection, CurrentInterSectionInfo.STATE_CHANGING);
                                            IList crossStartNodes = this.GetStartNodesByInterSectionId(interSection.InterSectionId);

                                            List<VehicleEx> otherWaitAgvs = (List<VehicleEx>)this.GetOtherVehiclesOnStartNode(startNodeId, crossStartNodes, nodeExistAgvs);
                                            if (otherWaitAgvs.Count == 0)
                                            {
                                                logger.Info("Waiting AGV does not exist in {" + currInterSection.Id + "}, direction will be changed to nextSequence.");
                                                //this.updateCurrentInterSectionToNextSequence(currInterSection, interSection);
                                                this.UpdateCurrentInterSectionToNextSequence(currInterSection, interSection, CurrentInterSectionInfoEx.STATE_CHANGING);
                                            }
                                            else
                                            {
                                                otherWaitAgvs.Sort(NodeCheckTimeComparator);
                                                VehicleEx oldestAGV = (VehicleEx)otherWaitAgvs[0];
                                                if (oldestAGV != null)
                                                {
                                                    logger.Info("Oldest AGV[" + oldestAGV.Id + "] in " + currInterSection.Id);
                                                    this.UpdateCurrentInterSectionInfoDirection(currInterSection, oldestAGV.CurrentNodeId, CurrentInterSectionInfoEx.STATE_CHANGING);
                                                }
                                                else
                                                {
                                                    logger.Warn("Can not find AGV which has oldest nodeCheckTime, " + currInterSection.Id + "'s direction will be changed to nextSequence.");
                                                    //this.updateCurrentInterSectionToNextSequence(currInterSection, interSection);
                                                    this.UpdateCurrentInterSectionToNextSequence(currInterSection, interSection, CurrentInterSectionInfoEx.STATE_CHANGING);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            this.UpdateCurrentInterSectionToNextSequence(currInterSection, interSection);
                                        }
                                    }
                                }
                                else
                                {
                                    IList crossStartNodes = this.GetStartNodesByInterSectionId(interSection.InterSectionId);
                                    List<VehicleEx> otherWaitAgvs = (List<VehicleEx>)this.GetOtherVehiclesOnStartNode(startNodeId, crossStartNodes, nodeExistAgvs);
                                    if (otherWaitAgvs.Count == 0)
                                    {
                                        logger.Info("Waiting AGV does not exist in {" + currInterSection.Id + "}, direction will be changed to nextSequence.");
                                        this.UpdateCurrentInterSectionToNextSequence(currInterSection, interSection);
                                    }
                                    else
                                    {
                                        otherWaitAgvs.Sort(NodeCheckTimeComparator);
                                        VehicleEx oldestAGV = (VehicleEx)otherWaitAgvs[0];
                                        if (oldestAGV != null)
                                        {
                                            logger.Info("Oldest AGV[" + oldestAGV.Id + "] in " + currInterSection.Id);
                                            this.UpdateCurrentInterSectionInfoDirection(currInterSection, oldestAGV.CurrentNodeId, CurrentInterSectionInfoEx.STATE_CHANGED);
                                        }
                                        else
                                        {
                                            logger.Warn("Can not find AGV which has oldest nodeCheckTime, " + currInterSection.Id + "'s direction will be changed to nextSequence.");
                                            this.UpdateCurrentInterSectionToNextSequence(currInterSection, interSection);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                IList crossStartNodes = this.GetStartNodesByInterSectionId(interSection.InterSectionId);
                                List<VehicleEx> otherWaitAgvs = (List<VehicleEx>)this.GetOtherVehiclesOnStartNode(startNodeId, crossStartNodes, nodeExistAgvs);
                                if (otherWaitAgvs.Count == 0)
                                {
                                    this.UpdateCurrentInterSectionInfoState(currInterSection.Id, CurrentInterSectionInfoEx.STATE_CHANGED);
                                }
                                else
                                {
                                    bool existRunAgv = false;
                                    foreach (VehicleEx otherStartAgv in otherWaitAgvs)
                                    {
                                        if (VehicleEx.RUNSTATE_RUN.Equals(otherStartAgv.RunState))
                                        {
                                            existRunAgv = true;
                                            break;
                                        }
                                    }
                                    if (!existRunAgv)
                                    {
                                        this.UpdateCurrentInterSectionInfoState(currInterSection.Id, CurrentInterSectionInfoEx.STATE_CHANGED);
                                    }
                                }
                            }
                        }
                    }
                    else
                    { //Ch튼a qu찼 gi沼�
                        logger.Info("InterSection{" + currInterSection.Id + "} has not yet been changed.");
                    }
                }
                else
                {
                    logger.Error("Can't find interSection info by " + currInterSection.Id + ":" + currInterSection.CurrentDirectionNode + ". Now delete " + currInterSection);
                    this.DeleteCurrentInterSectionInfo(currInterSection);
                }
            }

            long endTime = System.DateTime.Now.Ticks;
            logger.Info("ChangeDirection elapsedTime = " + (endTime - startTime).ToString());
            return true;
        }



        public void ChangeNextSequent(CurrentInterSectionInfoEx currIsInfo, String interSectionId)
        {

            InterSectionControlEx interSection = this.GetInterSectionControls(interSectionId, currIsInfo.CurrentDirectionNode);
            if (interSection != null)
            {
                List<VehicleEx> vehicles = this.GetOtherWaitAGVsInIntersection(interSectionId);
                Dictionary<String, VehicleEx> nodeExistAgvs = new Dictionary<string, VehicleEx>();
                foreach (VehicleEx vehicle in vehicles)
                {
                    logger.Info("Found " + vehicle);
                    nodeExistAgvs.Add(vehicle.CurrentNodeId, vehicle);
                }

                List<String> crossStartNodes = (List<String>)this.GetStartNodesByInterSectionId(interSectionId);

                List<VehicleEx> otherWaitAgvs = (List<VehicleEx>)this.GetOtherVehiclesOnStartNode(currIsInfo.CurrentDirectionNode, crossStartNodes, nodeExistAgvs);
                if (otherWaitAgvs == null || otherWaitAgvs.Count <= 0)
                {
                    logger.Info("Waiting AGV does not exist in {" + interSectionId + "}, direction will be changed to nextSequence.");
                    this.UpdateCurrentInterSectionToNextSequence(currIsInfo, interSection);
                }
                else
                {
                    otherWaitAgvs.Sort(NodeCheckTimeComparator);
                    VehicleEx oldestAGV = otherWaitAgvs[0];
                    if (oldestAGV != null)
                    {
                        logger.Info("Oldest AGV[" + oldestAGV.Id + "] in " + interSectionId);
                        this.UpdateCurrentInterSectionInfoDirection(currIsInfo, oldestAGV.CurrentNodeId, CurrentInterSectionInfoEx.STATE_CHANGED);
                    }
                    else
                    {
                        logger.Info("Can not find AGV which has oldest nodeCheckTime, " + interSectionId + "'s direction will be changed to nextSequence.");
                        this.UpdateCurrentInterSectionToNextSequence(currIsInfo, interSection);
                    }
                }
            }
        }



        public List<VehicleEx> GetOtherWaitAGVsInIntersection(String intersectionId)
        {
            DetachedCriteria criteria = DetachedCriteria.For(typeof(InterSectionControlEx));
            criteria.SetProjection(Projections.Property("StartNodeId"));
            criteria.Add(Restrictions.Eq("InterSectionId", intersectionId));
            List<String> listNode = (List<String>)this.PersistentDao.FindByCriteria(criteria);
            if (listNode.Count > 0)
            {
                DetachedCriteria criteria2 = DetachedCriteria.For(typeof(VehicleEx));
                criteria2.Add(Restrictions.In("currentNodeId", listNode));
                return (List<VehicleEx>)this.PersistentDao.FindByCriteria(criteria2);

            }
            return null;
        }

        public IList GetOtherVehiclesOnStartNode(string currentStartNodeId, IList crossStartNodes, Dictionary<string, VehicleEx> nodeExistAgvs)
        {
            IList otherWaitAgvs = new List<VehicleEx>();
            foreach (string otherStartNode in crossStartNodes)
            {
                if (otherStartNode.Equals(currentStartNodeId))
                {
                    continue;
                }
                VehicleEx vehicle = nodeExistAgvs[otherStartNode];
                if (vehicle != null)
                {
                    otherWaitAgvs.Add(vehicle);
                }
            }
            return otherWaitAgvs;
        }
        public bool PossibleToGo(VehicleEx vehicle)
        {

            InterSectionControlEx interSection = this.GetInterSectionControlByStartNode(vehicle.CurrentNodeId);
            if (interSection != null)
            {
                return this.PossibleToGo(interSection.InterSectionId, vehicle);
            }
            return false;
        }


        public bool PossibleToGo(String interSectionId, VehicleEx vehicle)
        {

            String startNodeId = vehicle.CurrentNodeId;
            CurrentInterSectionInfoEx cis = GetCurrentInterSectionInfoById(interSectionId);
            // 1. AGV's location(startNodeId) equal currentInterSection's directionNodeId
            //    and currentInterSection's state was 'CHANGED', if state is 'CHANGING', some AGV might be still exist in interSection. 
            if (startNodeId.Equals(cis.CurrentDirectionNode))
            {
                if (CurrentInterSectionInfoEx.STATE_CHANGED.Equals(cis.State))
                {
                    logger.Info("InterSection's directionNode is currentNode{" + startNodeId + "}, AGV {" + vehicle.Id + "} will be entered to interSection.");
                    return true;
                }
                else
                {
                    logger.Warn("AGV can not be endtered because interSection's state is " + CurrentInterSectionInfoEx.STATE_CHANGING + "}");
                    return false;
                }
            }

            // 2. AGV's location != currentInterSection's directionNodeId
            //    check AGVs has waiting in other startNode.
            List<String> crossStartNodes = (List<string>)this.GetStartNodesByInterSectionId(interSectionId);

            if (!ExistAGVinInterSection(interSectionId))
            {
                List<VehicleEx> startNodeAGVs = (List<VehicleEx>)this.ResourceManager.GetVehiclesByInterSectionStartNodes(crossStartNodes);
                foreach (VehicleEx waitVehicle in startNodeAGVs)
                {
                    if (vehicle.Id.Equals(waitVehicle.Id))
                    {
                        continue;
                    }
                    if (VehicleEx.RUNSTATE_RUN.Equals(waitVehicle.RunState))
                    { //N梳퓎 c처 AGV kh찼c 휃ang RUN t梳죍 휃i沼긩 startnode kh찼c th챙 d沼쳌g
                        logger.Info("Other AGV{" + waitVehicle.Id + "} is running, can't go into interSection.");
                        return false;
                    }
                    else
                    {
                        AlarmEx alarm = this.AlarmManager.GetAlarmByVehicleId(waitVehicle.Id);
                        if (alarm != null && alarm.AlarmId.Equals(AlarmExs.ALARMCODE_FRONTSENSOR))
                            return false;
                    }
                }
                logger.Info("No AGV exist in InterSection " + interSectionId + ", CurrentInterSection state will be changed.");
                this.UpdateCurrentInterSectionInfoDirection(interSectionId, startNodeId, CurrentInterSectionInfoEx.STATE_CHANGED);
                return true;
            }
            else
            {
                logger.Info("Other AGV is running in InterSection " + interSectionId + " now, CurrentInterSection state can't be updated.");
            }

            return false;
        }


        public bool ExistAGVinInterSection(String interSectionId)
        {
            List<String> checkNodeIds = this.GetAllCheckNodeIdList(interSectionId);

            if (checkNodeIds.Count <= 0)
            {
                return false;
            }
            else
            {
                List<VehicleEx> vehicles = (List<VehicleEx>)this.ResourceManager.GetRunningVehiclesByNodeList(checkNodeIds);
                //logger.info("List vehicle in check node: [" + vehicles + "]");
                return (vehicles == null || vehicles.Count <= 0) ? false : true;
            }
        }

        public bool HaveRunningAGVInStartNode(String interSectionId)
        {
            CurrentInterSectionInfoEx cis = this.GetCurrentInterSectionInfoById(interSectionId);
            String startNode = cis.CurrentDirectionNode;
            List<String> crossStartNodes = new List<String>();
            crossStartNodes.Add(startNode);
            if (!ExistAGVinInterSection(interSectionId))
            {
                List<VehicleEx> startNodeAGVs = (List<VehicleEx>)this.ResourceManager.GetVehiclesByInterSectionStartNodes(crossStartNodes);
                foreach (VehicleEx waitVehicle in startNodeAGVs)
                {
                    if (VehicleEx.RUNSTATE_RUN.Equals(waitVehicle.RunState))
                    {
                        return true;
                    }
                    else
                    {
                        AlarmEx alarm = this.AlarmManager.GetAlarmByVehicleId(waitVehicle.Id);
                        if (alarm != null && alarm.AlarmId.Equals(AlarmExs.ALARMCODE_FRONTSENSOR))
                            return true;
                    }
                }
                return false;
            }
            else
            {
                logger.Info("Other AGV is running in InterSection " + interSectionId + " now, CurrentInterSection state can't be updated.");
                return true;
            }
        }

        private List<String> GetAllCheckNodeIdList(String interSectionId)
        {
            List<String> checkNodeIds = new List<String>();
            DetachedCriteria criteria = DetachedCriteria.For(typeof(InterSectionControlEx));
            criteria.SetProjection(Projections.Property("CheckNodeIds"));
            criteria.Add(Restrictions.Eq("InterSectionId", interSectionId));
            List<String> result = (List<String>)this.PersistentDao.FindByCriteria(criteria);
            foreach (String checkNodes in result)
            {
                if (!string.IsNullOrEmpty(checkNodes))
                {
                    checkNodeIds.AddRange(MakeNodeList(checkNodes));
                }
            }
            return new List<String>(checkNodeIds);
        }

        private List<String> MakeNodeList(String endNodeIds)
        {
            List<String> endNodeIdList = new List<String>();
            if (!string.IsNullOrEmpty(endNodeIds))
            {
                if (endNodeIds.Contains(DELIMITER_NODEID))
                {
                    // multi value
                    String[] endNodeIdArray = endNodeIds.Split(DELIMITER_NODEID);
                    foreach (String endNodeId in endNodeIdArray)
                    {
                        endNodeIdList.Add(endNodeId);
                    }
                }
                else
                {
                    // single value
                    endNodeIdList.Add(endNodeIds);
                }
            }
            return new List<String>(endNodeIdList);
        }

        public bool ChangeDirection(List<InterSectionControlEx> allInterSectionList)
        {

            long startTime = System.DateTime.Now.Ticks;
            // 1. getCurrentInterSections
            List<CurrentInterSectionInfoEx> currIsList = (List<CurrentInterSectionInfoEx>)GetAllCurrentInterSections();

            // 2. get AGV list by all startNodeIds of InterSectionControls.
            List<String> startNodeIds = (List<String>)this.GetAllStartNodesByInterSectionId();
            List<VehicleEx> vehicles = (List<VehicleEx>)this.ResourceManager.GetVehiclesByInterSectionStartNodes(startNodeIds);
            Dictionary<String, VehicleEx> nodeExistAgvs = new Dictionary<string, VehicleEx>();
            foreach (VehicleEx vehicle in vehicles)
            {
                logger.Info("Found " + vehicle);
                nodeExistAgvs.Add(vehicle.CurrentNodeId, vehicle);
            }

            Dictionary<String, InterSectionControlEx> isMap = new Dictionary<String, InterSectionControlEx>();
            foreach (InterSectionControlEx interSection in allInterSectionList)
            {
                String key = interSection.InterSectionId + ":" + interSection.StartNodeId;
                isMap.Add(key, interSection);
            }

            foreach (CurrentInterSectionInfoEx currInterSection in currIsList)
            {
                InterSectionControlEx interSection = isMap[currInterSection.Id + ":" + currInterSection.CurrentDirectionNode];
                String startNodeId = currInterSection.CurrentDirectionNode;
                if (interSection != null)
                {
                    if (this.PossibleToChangeDirection(interSection, currInterSection))
                    {
                        // 1. changed면?
                        // 방향을 바꿀 수 있는데 agv가 교차로내에 있는지 check해서 없으면 변경, 있으면 changing
                        // 변경할 때 아무곳에도 agv가 없다면 다음 sequence로, 대기중인 agv들이 있다면 대기시간이 가장 긴 agv로
                        // 2. changing이면?
                        // 변경중이므로 agv가 교차로 내에 있는지 check해서 없으면 changed로 상태만 변경
                        // 있는 경우 대기
                        // 시간이 지난 것 중 startNode를 제외한 구간에 agv 여부 check
                        if (this.ExistAGVinInterSection(interSection.InterSectionId))
                        { //Có AGV trong vùng va chạm
                          // start, end가 아닌 interSection안에 AGV가 존재 => changing
                            if (CurrentInterSectionInfoEx.STATE_CHANGED.Equals(currInterSection.State))
                            { //Nếu current intersection đang Changed thì chuyển thành Changing
                                logger.Info("Other AGV is running in InterSection{" + currInterSection.Id + "}, state will be changed to '"
                                        + CurrentInterSectionInfoEx.STATE_CHANGING + "'");
                                List<String> crossStartNodes = (List<String>)this.GetStartNodesByInterSectionId(interSection.InterSectionId);
                                // current외에 다른 startNode에 대기중이 있는 AGV가 있는지 check해서 있다면  가장 오래된 AGV로 변경
                                List<VehicleEx> otherWaitAgvs = (List<VehicleEx>)this.GetOtherVehiclesOnStartNode(startNodeId, crossStartNodes, nodeExistAgvs);
                                if (otherWaitAgvs == null || otherWaitAgvs.Count <= 0)
                                {
                                    logger.Info("Waiting AGV does not exist in {" + currInterSection.Id + "}, direction will be changed to nextSequence.");
                                    //this.updateCurrentInterSectionToNextSequence(currInterSection, interSection);
                                    this.UpdateCurrentInterSectionToNextSequence(currInterSection, interSection, CurrentInterSectionInfoEx.STATE_CHANGING);
                                }
                                else
                                {
                                    otherWaitAgvs.Sort(NodeCheckTimeComparator);
                                    VehicleEx oldestAGV = otherWaitAgvs[0];
                                    if (oldestAGV != null)
                                    {
                                        logger.Info("Oldest AGV[" + oldestAGV.Id + "] in " + currInterSection.Id);
                                        this.UpdateCurrentInterSectionInfoDirection(currInterSection, oldestAGV.CurrentNodeId,
                                                CurrentInterSectionInfoEx.STATE_CHANGING);
                                    }
                                    else
                                    {
                                        logger.Warn("Can not find AGV which has oldest nodeCheckTime, " + currInterSection.Id
                                                + "'s direction will be changed to nextSequence.");
                                        //this.updateCurrentInterSectionToNextSequence(currInterSection, interSection);
                                        this.UpdateCurrentInterSectionToNextSequence(currInterSection, interSection, CurrentInterSectionInfoEx.STATE_CHANGING);
                                    }
                                }
                            }
                            else
                            {
                                logger.Warn("Other AGV is running in InterSection{" + currInterSection.Id + "} '"
                                        + "can not change state to " + CurrentInterSectionInfoEx.STATE_CHANGED);
                            }
                        }
                        else
                        {
                            if (CurrentInterSectionInfoEx.STATE_CHANGED.Equals(currInterSection.State))
                            {
                                // 현재 start의 agv가 run이라면 changing, 아니라면 변경
                                VehicleEx existAGV = nodeExistAgvs[startNodeId];
                                if (existAGV != null)
                                {
                                    System.Globalization.GregorianCalendar calendar = new System.Globalization.GregorianCalendar();

                                    System.DateTime currentTime = new System.DateTime(System.DateTime.Now.Second - IgnoreNodeCheckTime);

                                    if (currentTime.CompareTo(existAGV.NodeCheckTime) > 0)
                                    {
                                        logger.Info("AGV{" + existAGV.Id + "} exist in startNode, but nodeCheckTime is old. CurrentInterSection{" + currInterSection.Id + "}'s direction will be changed.");
                                        this.UpdateCurrentInterSectionToNextSequence(currInterSection, interSection);
                                    }
                                    else
                                    {
                                        if (VehicleEx.RUNSTATE_RUN.Equals(existAGV.RunState))
                                        {
                                            logger.Info("AGV running on the startNode{" + startNodeId + "}. CurrentInterSection{" +
                                                    currInterSection.Id + "} state will be changed to '" + CurrentInterSectionInfoEx.STATE_CHANGING + "'");

                                            List<String> crossStartNodes = (List<String>)this.GetStartNodesByInterSectionId(interSection.InterSectionId);
                                            // current외에 다른 startNode에 대기중이 있는 AGV가 있는지 check해서 있다면  가장 오래된 AGV로 변경
                                            List<VehicleEx> otherWaitAgvs = (List<VehicleEx>)this.GetOtherVehiclesOnStartNode(startNodeId, crossStartNodes, nodeExistAgvs);
                                            if (otherWaitAgvs == null || otherWaitAgvs.Count <= 0)
                                            {
                                                logger.Info("Waiting AGV does not exist in {" + currInterSection.Id + "}, direction will be changed to nextSequence.");
                                                //this.updateCurrentInterSectionToNextSequence(currInterSection, interSection);
                                                this.UpdateCurrentInterSectionToNextSequence(currInterSection, interSection, CurrentInterSectionInfoEx.STATE_CHANGING);
                                            }
                                            else
                                            {
                                                otherWaitAgvs.Sort(NodeCheckTimeComparator);
                                                VehicleEx oldestAGV = otherWaitAgvs[0];
                                                if (oldestAGV != null)
                                                {
                                                    logger.Info("Oldest AGV[" + oldestAGV.Id + "] in " + currInterSection.Id);
                                                    this.UpdateCurrentInterSectionInfoDirection(currInterSection, oldestAGV.CurrentNodeId,
                                                            CurrentInterSectionInfoEx.STATE_CHANGING);
                                                }
                                                else
                                                {
                                                    logger.Warn("Can not find AGV which has oldest nodeCheckTime, " + currInterSection.Id
                                                            + "'s direction will be changed to nextSequence.");
                                                    //this.updateCurrentInterSectionToNextSequence(currInterSection, interSection);
                                                    this.UpdateCurrentInterSectionToNextSequence(currInterSection, interSection, CurrentInterSectionInfoEx.STATE_CHANGING);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            this.UpdateCurrentInterSectionToNextSequence(currInterSection, interSection);
                                        }
                                    }
                                }
                                else
                                {
                                    List<String> crossStartNodes = (List<String>)this.GetStartNodesByInterSectionId(interSection.InterSectionId);
                                    // current외에 다른 startNode에 대기중이 있는 AGV가 있는지 check해서 있다면  가장 오래된 AGV로 변경
                                    List<VehicleEx> otherWaitAgvs = (List<VehicleEx>)this.GetOtherVehiclesOnStartNode(startNodeId, crossStartNodes, nodeExistAgvs);
                                    if (otherWaitAgvs == null || otherWaitAgvs.Count <= 0)
                                    {
                                        logger.Info("Waiting AGV does not exist in {" + currInterSection.Id + "}, direction will be changed to nextSequence.");
                                        this.UpdateCurrentInterSectionToNextSequence(currInterSection, interSection);
                                    }
                                    else
                                    {
                                        otherWaitAgvs.Sort(NodeCheckTimeComparator);
                                        VehicleEx oldestAGV = otherWaitAgvs[0];
                                        if (oldestAGV != null)
                                        {
                                            logger.Info("Oldest AGV[" + oldestAGV.Id + "] in " + currInterSection.Id);
                                            this.UpdateCurrentInterSectionInfoDirection(currInterSection, oldestAGV.CurrentNodeId,
                                                    CurrentInterSectionInfoEx.STATE_CHANGED);
                                        }
                                        else
                                        {
                                            logger.Warn("Can not find AGV which has oldest nodeCheckTime, " + currInterSection.Id
                                                    + "'s direction will be changed to nextSequence.");
                                            this.UpdateCurrentInterSectionToNextSequence(currInterSection, interSection);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                List<String> crossStartNodes = (List<String>)this.GetStartNodesByInterSectionId(interSection.InterSectionId);
                                // 다른 start의 agv가 run이라면 대기, 아니라면 changed
                                List<VehicleEx> otherWaitAgvs = (List<VehicleEx>)this.GetOtherVehiclesOnStartNode(startNodeId, crossStartNodes, nodeExistAgvs);
                                if (otherWaitAgvs == null || otherWaitAgvs.Count <= 0)
                                {
                                    this.UpdateCurrentInterSectionInfoState(currInterSection.Id, CurrentInterSectionInfoEx.STATE_CHANGED);
                                }
                                else
                                {
                                    bool existRunAgv = false;
                                    foreach (VehicleEx otherStartAgv in otherWaitAgvs)
                                    {
                                        if (VehicleEx.RUNSTATE_RUN.Equals(otherStartAgv.RunState))
                                        {
                                            existRunAgv = true;
                                            break;
                                        }
                                    }
                                    if (!existRunAgv)
                                    {
                                        this.UpdateCurrentInterSectionInfoState(currInterSection.Id, CurrentInterSectionInfoEx.STATE_CHANGED);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        logger.Info("InterSection{" + currInterSection.Id + "} has not yet been changed.");
                    }
                }
                else
                {
                    logger.Error("Can't find interSection info by " + currInterSection.Id + ":" + currInterSection.CurrentDirectionNode + ". Now delete " + currInterSection);
                    this.DeleteCurrentInterSectionInfo(currInterSection);
                }
            }

            long endTime = System.DateTime.Now.Ticks;
            logger.Info("ChangeDirection elapsedTime = " + (endTime - startTime).ToString());
            return true;
        }
    }
}

