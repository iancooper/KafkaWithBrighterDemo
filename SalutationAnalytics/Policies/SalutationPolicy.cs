﻿using Paramore.Brighter;

namespace SalutationAnalytics.Policies
{
    public class SalutationPolicy : DefaultPolicy 
    {
        public SalutationPolicy()
        {
            AddSalutationPolicies();
        }

        private void AddSalutationPolicies()
        {
            Add(Retry.RETRYPOLICYASYNC, Retry.GetSimpleHandlerRetryPolicyAsync());
            Add(Retry.EXPONENTIAL_RETRYPOLICYASYNC, Retry.GetExponentialHandlerRetryPolicyAsync());
        }
    }
}
