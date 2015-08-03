using System;
using System.Linq;
using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Server;

//todo: How to avoid triggering the template cloning code when you are creating the template item?
//todo: current version does not copy templates more than one level deep.

namespace TFS_EventHandlers
{
    /// <summary>
    /// This class represents an event handler that is subscribed to TFS event notifications
    /// </summary>
    public class WorkItemChangedEventHandler : ISubscriber
    {

        #region iSubscriber Implementation
        /// <summary>
        /// Specifies the TFS events that objects created from this class are subsxcribed to.
        /// </summary>
        /// <returns>Set of types this subscriber wants to be notified of.</returns>
        public Type[] SubscribedTypes() {return new Type[1] { typeof(WorkItemChangedEvent) };}

        /// <summary>
        /// Friendly name of the Subscriber used in messages about the subscriber
        /// </summary>
        public string Name   {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; }
        }

        /// <summary>
        /// Priority this subscriber is called on
        /// </summary>
        public SubscriberPriority Priority
        {
            get { return SubscriberPriority.Normal; }
        }

        /// <summary>
        /// TFS Main Event Handler
        /// </summary>
        /// <param name="requestContext">Event context passed in by TFS</param>
        /// <param name="notificationType">DecisionPoint or Notification</param>
        /// <param name="notificationEventArgs">Object that was published</param>
        /// <param name="statusCode">Code to return to the user when a decision point fails</param>
        /// <param name="statusMessage">Message to return to the user when a decision point fails</param>
        /// <param name="properties">Properties to return to the user when a decision point fails</param>
        /// <returns></returns>
        public EventNotificationStatus ProcessEvent(TeamFoundationRequestContext requestContext, NotificationType notificationType,
                                                    object notificationEventArgs, out int statusCode, out string statusMessage,
                                                    out ExceptionPropertyCollection properties) 
        {
            #region method outputs
            EventNotificationStatus returnStatus = EventNotificationStatus.ActionPermitted; //allows the action if no other subscribers reject
            statusCode = 0;
            properties = null;
            statusMessage = String.Empty;
            #endregion

            try {
                var ev = notificationEventArgs as WorkItemChangedEvent;
                if (notificationType == NotificationType.Notification && notificationEventArgs is WorkItemChangedEvent) {
                    int thisWorkItemID = getWorkItemID(ev);                                       
                    if (ev.ChangeType == ChangeTypes.New) { //new TFS Work Item created                        
                        returnStatus = TfsEvent_WorkItemCreated(requestContext, notificationEventArgs, ev.PortfolioProject, thisWorkItemID);
                    }
                }
            }
            catch (Exception exception) {
                TeamFoundationApplicationCore.LogException("Error processing event", exception);
            }

            return returnStatus;
        }
        #endregion

        protected EventNotificationStatus TfsEvent_WorkItemCreated(TeamFoundationRequestContext requestContext, object notificationEventArgs, string TFSProjectName,int TFSworkItemID)
        {
            TFSHelper tfs = new TFSHelper(requestContext, TFSProjectName);

            if (TFSworkItemID > 0) {
                Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem thisWorkItem = tfs.getWorkItem(TFSworkItemID);

                if (thisWorkItem != null) {
                    Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem templateWorkItem = tfs.getTemplateForWorkItem(thisWorkItem);
                    if (tfs.isWorkItemEligibleForClone(thisWorkItem)) tfs.createChildWorkItems(thisWorkItem, templateWorkItem);                    
                }
                else {
                    throw new System.Exception(string.Format(Resources.errorMessages.UNABLETOGET_WORKITEM, TFSworkItemID.ToString()));
                }
            }
            else {           
                throw new System.ArgumentException(String.Format(Resources.errorMessages.ERROR_INVALID_WORK_ITEMID, TFSworkItemID.ToString()));
            }
            return EventNotificationStatus.ActionPermitted; //allows the action if no other subscribers reject.
        }

        int getWorkItemID(WorkItemChangedEvent ev)        {
            try {
                IntegerField idField =  ev.CoreFields.IntegerFields.Where<IntegerField>(field => field.Name.Equals("ID")).FirstOrDefault<IntegerField>();
                return idField.NewValue;
            }
            catch (Exception) {
                return 0;
            }
        }
    }
}
