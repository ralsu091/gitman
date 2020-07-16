using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;

public class Protection : BaseBranchAction {   

    private int reviewers;
    private string branch;

    public Protection(string branch = "master", int reviewers = 2) {
        this.reviewers = reviewers;
        this.branch = branch;
    }

    public override async Task Check(List<Repository> all_repos, Repository repo)
    {
        BranchProtectionSettings prot = null;
        try {
            prot = await Client.Repository.Branch.GetBranchProtection(repo.Owner.Login, repo.Name, this.branch);
        } catch { }

        if (prot?.RequiredPullRequestReviews == null || prot.RequiredPullRequestReviews.RequiredApprovingReviewCount < reviewers) {
            l($"[UPDATE] will add {reviewers} review enforcement to {repo.Name}", 1);
            all_repos.Add(repo);
        } else {
            l($"[OK] {repo.Name} already has {this.branch} branch protection with {prot.RequiredPullRequestReviews.RequiredApprovingReviewCount} reviewers", 1);
        }
    }

    public override async Task Action(Repository repo)
    {
        var update = new BranchProtectionSettingsUpdate(new BranchProtectionRequiredReviewsUpdate(false, false, reviewers));
        var prot = await Client.Repository.Branch.UpdateBranchProtection(repo.Owner.Login, repo.Name, this.branch, update);
        l($"[MODIFIED] {repo.Name} is set to {reviewers} reviewers", 1);
    }
}