using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Server;


namespace TFS_EventHandlers
{
    public class WorkItemChangedEventHandler : ISubscriber
    {
        public Type[] SubscribedTypes()
        {
            return new Type[1] { typeof(WorkItemChangedEvent) };
        }
        
        public string Name
        {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; }
        }

        public SubscriberPriority Priority
        {
            get { return SubscriberPriority.Normal; }
        }

        public EventNotificationStatus ProcessEvent(TeamFoundationRequestContext requestContext, NotificationType notificationType,
                                                    object notificationEventArgs, out int statusCode, out string statusMessage,
                                                    out ExceptionPropertyCollection properties)
        {
            statusCode = 0;
            properties = null;
            statusMessage = String.Empty;

            try
            { 
                if (notificationType == NotificationType.Notification && notificationEventArgs is WorkItemChangedEvent)
                {
                    var ev = notificationEventArgs as WorkItemChangedEvent;

                    switch (ev.ChangeType) {
                        case ChangeTypes.New: 
                            
                            TFSHelper tfs = new TFSHelper(requestContext, ev.PortfolioProject);
                            int thisWorkItemID = getWorkItemID(ev);

                            if (thisWorkItemID > 0) {
                               Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem thisWorkItem= tfs.getWorkItem(thisWorkItemID);

                                if (!thisWorkItem.Fields["Work Item Type"].Value.Equals("Task")) 
                                {
                                    //todo: This will get into infinite loop. find way to stop that.
                                    tfs.createChildWorkItems(thisWorkItem);
                                }
                            }
                            return EventNotificationStatus.ActionPermitted;
                            
                        case ChangeTypes.Change: //do nothing
                            break;

                        default: //do nothing
                            break;
                    }
                    
                    // Do what you want
                }
            }
            catch (Exception exception) {
                TeamFoundationApplicationCore.LogException("Error processing event", exception);
            }
            return EventNotificationStatus.ActionPermitted;
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
