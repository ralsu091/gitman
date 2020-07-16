using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;

public class Protection : BaseBranchAction {   

    public int Reviewers { get; set; } = 2;

    public override async Task Check(List<Repository> all_repos, Repository repo)
    {
        BranchProtectionSettings prot = null;
        try {
            prot = await Client.Repository.Branch.GetBranchProtection(repo.Owner.Login, repo.Name, "master");
        } catch { }

        if (prot?.RequiredPullRequestReviews == null || prot.RequiredPullRequestReviews.RequiredApprovingReviewCount < Reviewers) {
            l($"[UPDATE] will add {Reviewers} review enforment to {repo.Name}", 1);
            all_repos.Add(repo);
        } else {
            l($"[OK] {repo.Name} already has master branch protection with {prot.RequiredPullRequestReviews.RequiredApprovingReviewCount} reviewers", 1);
        }
    }

    public override async Task Action(Repository repo)
    {
        l("etc etc " + repo.Name);
    }
}