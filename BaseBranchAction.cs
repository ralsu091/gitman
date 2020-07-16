using System;
using Octokit;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

public abstract class BaseBranchAction {
        protected GitHubClient client;
        protected bool DryRun = true;

        public BaseBranchAction() {
            client = new GitHubClient(new ProductHeaderValue("SuperMassiveCLI"));
            client.Credentials = new Credentials("joostschriek", "hahahahaha");            
        }

        public abstract Task Check(List<Repository> all_repos, Repository repo);
        public abstract Task Action(Repository repo);

        public async Task DoActionsPerBranch(Func<List<Repository>, Repository, Task> check, Func<Repository, Task> action) {
            var add_to_repos = new List<Repository>();

            var repos = await client.Repository.GetAllForOrg("sectigo-eng", new ApiOptions {
                PageSize = 10000
            });
            foreach (var repo in repos)
            {
                await check(add_to_repos, repo);
            }

            if (DryRun) return;

            if (add_to_repos.Count == 0) return;

            foreach (var repo in add_to_repos)
            {
                await action(repo);
            }
        }
        protected void l(string msgs, int tab = 0) {
            Console.WriteLine(new String('\t', tab) + msgs);
        }
}