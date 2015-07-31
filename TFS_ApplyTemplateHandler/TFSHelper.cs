using System;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace TFS_EventHandlers {
    public class TFSHelper {
        public TfsTeamProjectCollection TFSCollection { get; private set; }
        public Project teamProject { get; private set; }
        public WorkItemStore workItemStore { get; private set; }

        public TFSHelper(TeamFoundationRequestContext requestContext, string projectName) {
            TFSCollection = getTeamProjectCollectionFromRequestContext(requestContext);
           // TFSCollection.EnsureAuthenticated();
            workItemStore = TFSCollection.GetService<WorkItemStore>();
            teamProject = workItemStore.Projects[projectName];                                
        }

        public WorkItem getWorkItem(int WorkItemID) {
            return workItemStore.GetWorkItem(WorkItemID);
        }

        public void createChildWorkItems(WorkItem parentWorkItem) {
          
            WorkItem templateItem = getWorkItem(17472); //template of parent (used to get children to create)
            WorkItemLinkTypeEnd childLinkType =  workItemStore.WorkItemLinkTypes.LinkTypeEnds["Child"];
            WorkItemLinkTypeEnd parentLinkType =  workItemStore.WorkItemLinkTypes.LinkTypeEnds["Parent"];

            foreach(WorkItemLink itemLInk in templateItem.WorkItemLinks) {
                if ((itemLInk.BaseType == BaseLinkType.WorkItemLink) && (itemLInk.LinkTypeEnd == childLinkType)) {
                    WorkItem copyWorkItem = getWorkItem(itemLInk.TargetId);
                   WorkItem newWorkItem =  copyWorkItem.Copy();

                    newWorkItem.Title = string.Format("{0}:{1}",newWorkItem.Title,parentWorkItem.Title);
                    newWorkItem.IterationId = parentWorkItem.IterationId; 
                    newWorkItem.Links.Clear();

                    try { //clear history from new item
                        foreach (Revision rev in newWorkItem.Revisions) {                            
                            //todo: add check that History field exists
                            rev.Fields["History"].Value = null;
                        }
                    }
                    catch (Exception) {
                        //ignore errors, in case history field doesn't exist. 
                    }
                    WorkItemLinkTypeEnd linkTypeEnd = parentLinkType;
                    newWorkItem.Links.Add(new RelatedLink(linkTypeEnd, parentWorkItem.Id));
                    newWorkItem.Save();
                }
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

        //public LinkTypeItem(WorkItemLinkType type) {
        //    this.CanDelete = type.CanDelete;
        //    this.CanEdit = type.CanEdit;
        //    this.ForwardEnd = type.ForwardEnd;
        //    this.IsActive = type.IsActive;
        //    this.IsDirectional = type.IsDirectional;
        //    this.IsNonCircular = type.IsNonCircular;
        //    this.IsOneToMany = type.IsOneToMany;
        //    this.LinkTopology = type.LinkTopology;
        //    this.ReferenceName = type.ReferenceName;
        //    this.ReverseEnd = type.ReverseEnd;
        //}

    }
}
