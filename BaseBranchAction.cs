using System;
using Octokit;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace gitman
{
    public abstract class BaseBranchAction
    {
        public GitHubClient Client { get; set; }

        public abstract Task Check(List<Repository> all_repos, Repository repo);
        public abstract Task Action(Repository repo);

        public async Task DoForAll()
        {
            var add_to_repos = new List<Repository>();

            var repos = await Client.Repository.GetAllForOrg(Config.Github.Org, new ApiOptions
            {
                PageSize = 100
            });
            foreach (var repo in repos)
            {
                await Check(add_to_repos, repo);
            }

            if (Config.DryRun) return;

            if (add_to_repos.Count == 0) return;

            Console.WriteLine("\n");

            foreach (var repo in add_to_repos)
            {
                await Action(repo);
            }
        }
        protected void l(string msgs, int tab = 0)
        {
            Console.WriteLine(new String('\t', tab) + msgs);
        }
    }
}