using System;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace TFS_EventHandlers {
    public class TFSHelper {

        #region Member Properties
        public TfsTeamProjectCollection TFSCollection { get; private set; }
        public Project teamProject { get; private set; }
        public WorkItemStore workItemStore { get; private set; }
        #endregion

        public TFSHelper(TeamFoundationRequestContext requestContext, string projectName) {
            TFSCollection = getTeamProjectCollectionFromRequestContext(requestContext);
           // TFSCollection.EnsureAuthenticated();
            workItemStore = TFSCollection.GetService<WorkItemStore>();
            teamProject = workItemStore.Projects[projectName];                                
        }

        public WorkItem getWorkItem(int WorkItemID) {
            try {
                return workItemStore.GetWorkItem(WorkItemID);
            }
            catch {
                throw new System.Exception(string.Format(Resources.errorMessages.UNABLETOGET_WORKITEM, WorkItemID.ToString()));
            }
        }

        public WorkItem getTemplateForWorkItem(WorkItem workItem)
        {
            //todo: implement some kind of system to query the correct template.
            return getWorkItem(17472);
        }
    
        public string getWorkItemType(WorkItem workItem) {
            if (workItem != null) {
                return workItem.Fields["Work Item Type"].Value.ToString();
            }
            else {
                return string.Empty;
            }           
        }

        public bool isWorkItemEligibleForClone(WorkItem workItem) {
            return
                   !isClonedWorkItem(workItem) &&
                   !getWorkItemType(workItem).Equals("Task");
        }

        /// <summary>
        /// Used to test if a work item was created by cloning template work items using this event handler
        /// </summary>
        /// <returns>Boolean representing whether the passed in work item was cloned using this event handlers templating system</returns>
        public bool isClonedWorkItem(WorkItem workItemtoCheck){

            foreach (Revision revision in workItemtoCheck.Revisions) {
                String history = (String)revision.Fields["History"].Value;

                if (!String.IsNullOrEmpty(history)) {
                    if (history.Equals(Resources.strings.HISTORYTEXT_CLONED)) return true; 
                }                                             
            }
            return false;
        }

        /// <summary>
        /// Copies the child work items from the templat work item, under parentWorkItem.
        /// Currently there is no support for multiple nesting in the template. This will only copy one level deep.
        /// </summary>
        /// <param name="parentWorkItem"></param>
        public void createChildWorkItems(WorkItem parentWorkItem, WorkItem templateWorkItem) {                      
            WorkItemLinkTypeEnd childLinkType =  workItemStore.WorkItemLinkTypes.LinkTypeEnds["Child"];
            WorkItemLinkTypeEnd parentLinkType =  workItemStore.WorkItemLinkTypes.LinkTypeEnds["Parent"];

            foreach(WorkItemLink itemLInk in templateWorkItem.WorkItemLinks) {
                if ((itemLInk.BaseType == BaseLinkType.WorkItemLink) && (itemLInk.LinkTypeEnd == childLinkType)) {
                    WorkItem copyWorkItem = getWorkItem(itemLInk.TargetId);
                   WorkItem newWorkItem =  copyWorkItem.Copy();

                    newWorkItem.Title = string.Format("{0}:{1}",newWorkItem.Title,parentWorkItem.Title);
                    newWorkItem.IterationId = parentWorkItem.IterationId; 
                    newWorkItem.Links.Clear();
                    
                    clearHistoryFromWorkItem(newWorkItem);

                    //This history entry is added to the new items to prevent recursion on newly created items.
                    newWorkItem.History=Resources.strings.HISTORYTEXT_CLONED;
                    
                    WorkItemLinkTypeEnd linkTypeEnd = parentLinkType;
                    newWorkItem.Links.Add(new RelatedLink(linkTypeEnd, parentWorkItem.Id));
                    newWorkItem.Save();
                }
            }
        }

        public void clearHistoryFromWorkItem(WorkItem workItem)
        {
            try
            {
                foreach (Revision rev in workItem.Revisions)
                {
                    //todo: add check that History field exists
                    rev.Fields["History"].Value = null;
                }
            }
            catch (Exception)
            {
                //todo: use a more specific exception handler
                //ignore errors, in case history field doesn't exist. 
            }
        }

        TfsTeamProjectCollection getTeamProjectCollectionFromRequestContext(TeamFoundationRequestContext requestContext)  {

            //todo: Avoid hardcoding credentials.
            System.Net.ICredentials cred = new System.Net.NetworkCredential("John", "cheese");

            IdentityDescriptor id; requestContext.GetAuthenticatedIdentity(out id);
            //ICredentialsProvider c = requestContext.GetAuthenticatedIdentity

            TeamFoundationLocationService service = requestContext.GetService<TeamFoundationLocationService>();
            Uri selfReferenceUri = service.GetSelfReferenceUri(requestContext, service.GetDefaultAccessMapping(requestContext));
            return new TfsTeamProjectCollection(selfReferenceUri, cred);
        }

        public void GetLinkTypes() {
            foreach (WorkItemLinkType type in workItemStore.WorkItemLinkTypes)  {
                //combo_types.Items.Add(new LinkTypeItem(type));
            }
        }
    }
}
