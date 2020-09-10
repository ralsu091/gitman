using System;
using Octokit;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace gitman
{
    public abstract class BaseBranchAction : BaseAction
    {
        public abstract Task Check(List<Repository> all_repos, Repository repo);
        public abstract Task Action(Repository repo);
        public List<Repository> Change { get; set; } = new List<Repository>();
        public override async Task Do()
        {
            var repos = await Client.Repository.GetAllForOrg(Config.Github.Org, new ApiOptions
            {
                PageSize = 100
            });
            foreach (var repo in repos)
            {
                await Check(Change, repo);
            }

            if (Config.DryRun) return;

            if (Change.Count == 0) return;

            foreach (var repo in Change)
            {
                await Action(repo);
            }
        }
    }
}