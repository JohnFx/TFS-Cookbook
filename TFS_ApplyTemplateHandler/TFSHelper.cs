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
            // Run a query.
            WorkItemCollection queryResults = workItemStore.Query(string.Format(
               "Select [System.ID], [System.Tags] " +
               "From WorkItems " +
               "Where ([Work Item Type] = '{0}') " +
               "and ([System.TeamProject] = '{1}') " +  
               "and ([Tags] contains '" + Resources.strings.TEMPLATE_TAG + "')", 
               getWorkItemType(workItem), teamProject.Name)); 
            
            if (queryResults.Count > 0) {
                return queryResults[0];                 
            }
            else {                
                return null; //no template found
            }            
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
                   !workItem["Tags"].ToString().Contains(Resources.strings.TEMPLATE_TAG);
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

                    if (!copyWorkItem["State"].Equals("Removed")) {
                        WorkItem newWorkItem = copyWorkItem.Copy();

                        newWorkItem.Title =  newWorkItem.Title;
                        newWorkItem.IterationId = parentWorkItem.IterationId;
                        newWorkItem.Links.Clear();

                        clearHistoryFromWorkItem(newWorkItem);

                        //This history entry is added to the new items to prevent recursion on newly created items.
                        newWorkItem.History = Resources.strings.HISTORYTEXT_CLONED;

                        WorkItemLinkTypeEnd linkTypeEnd = parentLinkType;
                        newWorkItem.Links.Add(new RelatedLink(linkTypeEnd, parentWorkItem.Id));
                        newWorkItem.Save();
                    }
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
    }
}
