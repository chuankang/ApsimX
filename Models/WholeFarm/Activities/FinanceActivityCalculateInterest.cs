﻿using Models.Core;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm.Activities
{
	/// <summary>manage enterprise activity</summary>
	/// <summary>This activity undertakes the overheads of running the enterprise.</summary>
	/// <version>1.0</version>
	/// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(WFActivityBase))]
	[ValidParent(ParentType = typeof(ActivitiesHolder))]
	[ValidParent(ParentType = typeof(ActivityFolder))]
	public class FinanceActivityCalculateInterest : WFActivityBase
	{
		/// <summary>
		/// Get the resources.
		/// </summary>
		[Link]
		private ResourcesHolder Resources = null;

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns></returns>
		public override List<ResourceRequest> DetermineResourcesNeeded()
		{
			return null;
		}

		/// <summary>
		/// Method used to perform activity if it can occur as soon as resources are available.
		/// </summary>
		public override void PerformActivity()
		{
			return;
		}

		/// <summary>
		/// test for whether finances are included.
		/// </summary>
		private bool financesExist = false;

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			financesExist = ((Resources.FinanceResource() != null));
		}

		/// <summary>An event handler to allow us to make all payments when needed</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("EndOfMonth")]
		private void OnEndOfMonth(object sender, EventArgs e)
		{
			if (financesExist)
			{
				// make interest payments on bank accounts
				foreach (FinanceType accnt in Resources.FinanceResource().Children.Where(a => a.GetType() == typeof(FinanceType)))
				{
					if (accnt.Balance > 0)
					{
						accnt.Add(accnt.Balance * accnt.InterestRatePaid / 1200, this.Name, "Interest earned");
					}
					else
					{
						if (Math.Abs(accnt.Balance) * accnt.InterestRateCharged / 1200 != 0)
						{
							ResourceRequest interestRequest = new ResourceRequest();
							interestRequest.ActivityName = this.Name;
							interestRequest.Required = Math.Abs(accnt.Balance) * accnt.InterestRateCharged / 1200;
							interestRequest.AllowTransmutation = false;
							interestRequest.Reason = "Interest charged";
							accnt.Remove(interestRequest);
						}
					}
				}
			}
		}

		/// <summary>
		/// Resource shortfall event handler
		/// </summary>
		public override event EventHandler ResourceShortfallOccurred;

		/// <summary>
		/// Shortfall occurred 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnShortfallOccurred(EventArgs e)
		{
			if (ResourceShortfallOccurred != null)
				ResourceShortfallOccurred(this, e);
		}
	}
}
