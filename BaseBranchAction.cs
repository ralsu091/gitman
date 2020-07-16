using System;
using Octokit;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

public abstract class BaseBranchAction {
        public GitHubClient Client;
        public bool DryRun = true;

        public abstract Task Check(List<Repository> all_repos, Repository repo);
        public abstract Task Action(Repository repo);

        public async Task DoForAll() {
            var add_to_repos = new List<Repository>();

            var repos = await Client.Repository.GetAllForOrg("sectigo-eng", new ApiOptions {
                PageSize = 10000
            });
            foreach (var repo in repos)
            {
                await Check(add_to_repos, repo);
            }

            if (DryRun) return;

            if (add_to_repos.Count == 0) return;

            foreach (var repo in add_to_repos)
            {
                await Action(repo);
            }
        }
        protected void l(string msgs, int tab = 0) {
            Console.WriteLine(new String('\t', tab) + msgs);
        }
}