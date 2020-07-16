using System;
using Octokit;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace gitman
{
    class Program
    {
        private static GitHubClient client;
        private static bool DryRun = true;

        static async Task Main(string[] args)
        {
            client = new GitHubClient(new ProductHeaderValue("SuperMassiveCLI"));
            client.Credentials = new Credentials("joostschriek", "hahahahaha");
            
            // await AddTeamToAllRepos("bravo")
            l("Checking master branch protection");
            var revs = 2;
            await DoActionsPerBranch(
                async (add, repo) => {
                    BranchProtectionSettings prot = null;
                    try {
                        prot = await client.Repository.Branch.GetBranchProtection(repo.Owner.Login, repo.Name, "master");
                    } catch { }

                    if (prot?.RequiredPullRequestReviews == null || prot.RequiredPullRequestReviews.RequiredApprovingReviewCount < revs) {
                        l($"[UPDATE] will add {revs} review enforment to {repo.Name}", 1);
                        add.Add(repo);
                    } else {
                        l($"[OK] {repo.Name} already has master branch protection with {prot.RequiredPullRequestReviews.RequiredApprovingReviewCount} reviewers", 1);
                    }
                },
                async (repo) => {
                    l("etc etc " + repo.Name);
                }
            );
        }

        static async Task AddTeamToAllRepos(string team) {
            var add_to_repos = new List<Repository>();

            // Figure out the team id
            var all_teams = await client.Organization.Team.GetAll("sectigo-eng");
            var team_id = all_teams.Single(t => t.Name.Equals(team, StringComparison.OrdinalIgnoreCase)).Id;

            // Figure out where to add the team
            var repos = await client.Repository.GetAllForOrg("sectigo-eng");
            foreach (var repo in repos)
            {
                var teams = await client.Repository.GetAllTeams(repo.Owner.Login, repo.Name);
                if (!teams.Any(t => t.Name.Equals(team, StringComparison.OrdinalIgnoreCase))) {
                    l($"[UPDATE] will add {team} to {repo.Name}", 1);
                    add_to_repos.Add(repo);
                } else {
                    l($"[OK] {team} is already a collaborator of {repo.Name}", 1);
                }
            }

            if (DryRun) return;

            if (add_to_repos.Count == 0) return;

            foreach (var repo in add_to_repos)
            {
                var res = await client.Organization.Team.AddRepository(team_id, repo.Owner.Login, repo.Name);
                if (res) {
                    l($"[MODIFIED] Added {team} ({team_id}) as a collaborator to {repo.Name}", 1);
                } else {
                    l($"[ERROR] Failed to add {team} ({team_id}) as a collaborator to {repo.Name}", 1);
                }
            }
        }

        static async Task DoActionsPerBranch(Func<List<Repository>, Repository, Task> check, Func<Repository, Task> action) {
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

        static void l(string msgs, int tab = 0) {
            Console.WriteLine(new String('\t', tab) + msgs);
        }
    }
}