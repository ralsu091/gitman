using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Octokit;

namespace gitman
{
    public class Protection : BaseBranchAction
    {
        private readonly IReadOnlyList<string> EmptyContexts;
        private int reviewers;
        private string branch;
        private const string UPDATE = "[UPDATE] ";
        private IList<string> cachedReviewers = new List<string>();
        private Dictionary<string, BranchProtectionRequiredStatusChecks> cachedStatusContexts = new Dictionary<string, BranchProtectionRequiredStatusChecks>();

        
        public Protection(string branch = "master", int reviewers = 2)
        {
            EmptyContexts = new List<string>().AsReadOnly();

            this.reviewers = reviewers;
            this.branch = branch;
        }

        public override async Task Check(List<Repository> all_repos, Repository repo)
        {
            BranchProtectionSettings requiredReviewers = null;
            var message = UPDATE;
            
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

            var doesNotHaveRequiredReviewers = true;
            try {
                requiredReviewers = await Client.Repository.Branch.GetBranchProtection(repo.Owner.Login, repo.Name, this.branch);
                doesNotHaveRequiredReviewers = requiredReviewers?.RequiredPullRequestReviews == null || requiredReviewers.RequiredPullRequestReviews.RequiredApprovingReviewCount < reviewers;
            } catch (Octokit.NotFoundException) {
                // no-op -- this usually means that it's a new repo
            }
            
            if (doesNotHaveRequiredReviewers)
            {
                if (!message.Equals(UPDATE)) message += " and ";
                message += $"will add {reviewers} review enforcement";
                this.cachedReviewers.Add(repo.Name);
            }

            if (message.Equals(UPDATE))
            {
                l($"[OK] {repo.Name} already has {this.branch} branch protection with the number of reviewers and non-strict", 1);
            }
            else
            {
                l($"{message} on {repo.Name}", 1);
                all_repos.Add(repo);
            }
        }

        public override async Task Action(Repository repo)
        {
            try {
                BranchProtectionRequiredStatusChecks statusChecks;
                BranchProtectionRequiredStatusChecksUpdate statusChecksUpdate;

                if (cachedStatusContexts.TryGetValue(repo.Name, out statusChecks)) {
                    statusChecksUpdate = new BranchProtectionRequiredStatusChecksUpdate(false, statusChecks.Contexts);
                } else {
                    statusChecksUpdate = new BranchProtectionRequiredStatusChecksUpdate(false, EmptyContexts);
                }

                l($"[MODIFING] Setting branch protections on {repo.Name} to unstrict and with contexts {string.Join(",", statusChecksUpdate.Contexts)} ", 1);
                await Client.Repository.Branch.UpdateBranchProtection(
                    repo.Owner.Login,
                    repo.Name, 
                    this.branch, 
                    new BranchProtectionSettingsUpdate(
                        statusChecksUpdate,
                        new BranchProtectionRequiredReviewsUpdate(false, false, reviewers),
                        false
                    )
                );
            } catch (Octokit.NotFoundException) {
                l($"[WARN] could not set anything on {repo.Name} because {this.branch} does not exist.", 1);
            }
        }
    }
}