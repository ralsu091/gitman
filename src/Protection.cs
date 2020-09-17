using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;

namespace gitman
{
    public class Protection : BaseBranchAction
    {
        private int reviewers;
        private string branch;
        private const string UPDATE = "[UPDATE] ";
        private Dictionary<string, BranchProtectionRequiredReviews> cachedReviewers = new Dictionary<string, BranchProtectionRequiredReviews>();
        private Dictionary<string, BranchProtectionRequiredStatusChecks> cachedStatusContexts = new Dictionary<string, BranchProtectionRequiredStatusChecks>();

        public Protection(string branch = "master", int reviewers = 2)
        {
            this.reviewers = reviewers;
            this.branch = branch;
        }

        public override async Task Check(List<Repository> all_repos, Repository repo)
        {
            BranchProtectionSettings requiredReviewers = null;
            var message = UPDATE;
            
            try
            {
                try {
                    var statusChecks = await Client.Repository.Branch.GetRequiredStatusChecks(repo.Owner.Login, repo.Name, this.branch);
                    if (statusChecks.Strict) 
                    {
                        message += "will remove strict reviewers";
                        this.cachedStatusContexts.Add(repo.Name, statusChecks);
                    }
                } catch (Octokit.NotFoundException) { 
                    // no-op -- we didn't find any restrictions so that is good.            
                }

                requiredReviewers = await Client.Repository.Branch.GetBranchProtection(repo.Owner.Login, repo.Name, this.branch);
                var hasReviewers = requiredReviewers?.RequiredPullRequestReviews == null || requiredReviewers.RequiredPullRequestReviews.RequiredApprovingReviewCount >= reviewers;
                if (!hasReviewers)
                {
                    if (message.Equals(UPDATE)) message += " and ";
                    message = $"will add {reviewers} review enforcement";
                    this.cachedReviewers.Add(repo.Name, requiredReviewers.RequiredPullRequestReviews);
                }

                if (message.Equals(UPDATE))
                {
                    l($"[OK] {repo.Name} already has {this.branch} branch protection with {requiredReviewers.RequiredPullRequestReviews.RequiredApprovingReviewCount} reviewers and non-strict", 1);
                }
                else
                {
                    l($"{message} on {repo.Name}", 1);
                    all_repos.Add(repo);
                }
            }
            catch (Exception ex)
            { 
                l($"[ERROR] Something went wrong tryping to check the branch protection for {repo.Name}. {ex.Message}", 1);
            }
        }

        public override async Task Action(Repository repo)
        {
            BranchProtectionSettingsUpdate update;
            BranchProtectionRequiredReviewsUpdate reviewersEnforcement = null;
            BranchProtectionRequiredStatusChecksUpdate notSoStrict = null;

            if (cachedReviewers.TryGetValue(repo.Name, out var protection)) {
                reviewersEnforcement = new BranchProtectionRequiredReviewsUpdate(false, false, reviewers);
            }
            if (cachedStatusContexts.TryGetValue(repo.Name, out var statusChecks)) {
                notSoStrict = new BranchProtectionRequiredStatusChecksUpdate(false, statusChecks.Contexts);
            }
            
            update = new BranchProtectionSettingsUpdate(
                requiredStatusChecks: notSoStrict,
                requiredPullRequestReviews: reviewersEnforcement,
                false
            );

            var prot = await Client.Repository.Branch.UpdateBranchProtection(repo.Owner.Login, repo.Name, this.branch, update);
            l($"[MODIFIED] {repo.Name} is set to {reviewers} reviewers and not strict", 1);
        }
    }
}