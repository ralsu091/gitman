using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;
using System.Linq;
using gitman;
using System;

namespace gitman
{
    public class GitWrapper {
        private Dictionary<string, Team> teamsByIds = new Dictionary<string, Team>();
        private GitHubClient client;
        
        public Repository Repo { get; private set; }

        public GitWrapper(GitHubClient client) {
            this.client = client;
            this.Repo = new Repository(this);
        }

        public async Task<GitTeam> GetTeamAsync(string name) {
            await CacheTeamsAsync();
            var team = teamsByIds.SingleOrDefault(t => t.Key.Equals(name));
            if (team.Equals(default(KeyValuePair<int, Team>))) {
                return null;
            }

            return new GitTeam(team.Value);
        }

        public class Repository {
            private Dictionary<string, IEnumerable<GitTeam>> teamsByRepo = new Dictionary<string, IEnumerable<GitTeam>>();
            private GitWrapper wrapper;

            public Repository(GitWrapper wrapper) {
                this.wrapper = wrapper;
            }

            public async Task<IEnumerable<GitTeam>> GetTeamsAsync(string reponame) {
                if (!teamsByRepo.ContainsKey(reponame)) {
                    var repoTeams = await wrapper.client.Repository.GetAllTeams(Config.Github.Org, reponame);
                    teamsByRepo.Add(reponame, repoTeams.Select(t => new GitTeam(t)));
                }

                return teamsByRepo[reponame];
            }

            public async Task<bool> UpdateTeamPermissionAsync(string reponame, GitTeam target, Permission targetPermission) {
                bool updated = false;
                var removed = await wrapper.client.Organization.Team.RemoveRepository(target.Id, Config.Github.Org, reponame);
                if (removed) {
                    updated = await wrapper.client.Organization.Team.AddRepository(target.Id, Config.Github.Org, target.Name, new RepositoryPermissionRequest(targetPermission));
                }

                return updated;
            } 
        }

        private async Task CacheTeamsAsync() {
            if (teamsByIds.Count() != 0) {
                return;
            }
            var teams = await client.Organization.Team.GetAll(Config.Github.Org);
            teamsByIds = teams.ToDictionary(t => t.Name, t => t);
        }
    }
}
