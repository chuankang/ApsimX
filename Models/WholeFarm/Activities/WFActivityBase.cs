﻿using Models.Core;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.WholeFarm.Activities
{
	///<summary>
	/// WholeFarm Activity base model
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	public abstract class WFActivityBase: WFModel
	{
		[Link]
		private ResourcesHolder Resources = null;

		/// <summary>
		/// Current list of resources requested by this activity
		/// </summary>
		[XmlIgnore]
		public List<ResourceRequest> ResourceRequestList { get; set; }

		/// <summary>
		/// Current list of activities under this activity
		/// </summary>
		[XmlIgnore]
		public List<WFActivityBase> ActivityList { get; set; }

		/// <summary>
		/// Method to cascade calls for resources for all activities in the UI tree. 
		/// Responds to WFGetResourcesRequired in the Activity model holing top level list of activities
		/// </summary>
		public void GetResourcesForAllActivities()
		{
			// Get resources needed and use substitution if needed and provided, then move through children getting their resources.
			GetResourcesRequired();

			// get resources required for all dynamically created WFActivityBase activities
			if (ActivityList != null)
			{
				foreach (WFActivityBase activity in ActivityList)
				{
					activity.GetResourcesForAllActivities();
				}
			}
			// get resources required for all children of type WFActivityBase
			foreach (WFActivityBase activity in this.Children.Where(a => a.GetType().IsSubclassOf(typeof(WFActivityBase))).ToList())
			{
				activity.GetResourcesForAllActivities();
			}
		}

		/// <summary>
		/// Method to get this time steps current required resources for this activity. 
		/// </summary>
		public void GetResourcesRequired()
		{
			bool resourceAvailable = false;

			// determine what resources are needed
			ResourceRequestList = DetermineResourcesNeeded();

			// no resources required or this is an Activity folder.
			if (ResourceRequestList == null) return;

			Guid uniqueRequestID = Guid.NewGuid();
			// check resource amounts available
			foreach (ResourceRequest request in ResourceRequestList)
			{
				request.ActivityID = uniqueRequestID;
				request.Available = 0;
				// get resource
				IResourceType resource = Resources.GetResourceItem(request, out resourceAvailable) as IResourceType;
				if (resource != null)
				{
					// get amount available
					request.Available = Math.Min(resource.Amount, request.Required);
				}
				else
				{
					if (!resourceAvailable)
					{
						// if resource does not exist in simulation assume unlimited resource available
						// otherwise 0 will be assigned to available when no resouces match request
						request.Available = request.Required;
					}
				}
			}

			// are all resources available
			List<ResourceRequest> shortfallRequests = ResourceRequestList.Where(a => a.Required > a.Available).ToList();
			int countShortfallRequests = shortfallRequests.Count();
			if (countShortfallRequests > 0)
			{
				// check what transmutations can occur
				Resources.TransmutateShortfall(shortfallRequests, true);
			}

			// check if need to do transmutations
			int countTransmutationsSuccessful = shortfallRequests.Where(a => a.TransmutationPossible == true & a.AllowTransmutation).Count();
			bool allTransmutationsSuccessful = (shortfallRequests.Where(a => a.TransmutationPossible == false & a.AllowTransmutation).Count() == 0);

			// OR at least one transmutation successful and PerformWithPartialResources
			if (((countShortfallRequests > 0) & (countShortfallRequests == countTransmutationsSuccessful)) ^ (countTransmutationsSuccessful > 0 & PerformWithPartialResources))
			{
				// do transmutations.
				Resources.TransmutateShortfall(shortfallRequests, false);

				// recheck resource amounts now that resources have been topped up
				foreach (ResourceRequest request in ResourceRequestList)
				{
					resourceAvailable = false;
					// get resource
					request.Available = 0;
					IResourceType resource = Resources.GetResourceItem(request, out resourceAvailable) as IResourceType;
					if (resource != null)
					{
						// get amount available
						request.Available = Math.Max(resource.Amount, request.Required);
					}
				}
			}

			// report any resource defecits here
			foreach (var item in ResourceRequestList.Where(a => a.Required > a.Available))
			{
				ResourceRequestEventArgs rrEventArgs = new ResourceRequestEventArgs() { Request = item };
				OnShortfallOccurred(rrEventArgs);
			}

			// remove activity resources 
			// check if deficit and performWithPartial
			if ((ResourceRequestList.Where(a => a.Required > a.Available).Count() == 0) | PerformWithPartialResources)
			{
				foreach (ResourceRequest request in ResourceRequestList)
				{
					resourceAvailable = false;
					// get resource
					request.Provided = 0;
					IResourceType resource = Resources.GetResourceItem(request, out resourceAvailable) as IResourceType;
					if (resource != null)
					{
						// remove resource
						resource.Remove(request);
					}
				}
				PerformActivity();
			}
		}

		/// <summary>
		/// Perform Activity with partial resources available
		/// </summary>
		[Description("Perform Activity with partial resources available")]
		public bool PerformWithPartialResources { get; set; }

		/// <summary>
		/// Abstract method to determine list of resources and amounts needed. 
		/// </summary>
		public abstract List<ResourceRequest> DetermineResourcesNeeded();

		/// <summary>
		/// Method to perform activity tasks if expected as soon as resources are available
		/// </summary>
		public abstract void PerformActivity();

		/// <summary>
		/// Resource shortfall occured event handler
		/// </summary>
		public virtual event EventHandler ResourceShortfallOccurred;

		/// <summary>
		/// Shortfall occurred 
		/// </summary>
		/// <param name="e"></param>
		protected virtual void OnShortfallOccurred(EventArgs e)
		{
			if (ResourceShortfallOccurred != null)
				ResourceShortfallOccurred(this, e);
		}
	}

}
