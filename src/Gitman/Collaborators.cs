using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;
using System.Linq;
using System;

namespace gitman
{
    /// <remarks>
    /// For some reason, I can't find the `update repo permisions for teams` call in 
    /// octokit. This means, that in order for the permission check to properly work
    /// we have to remove the team, and re-add it with the correct permissions. Super 
    /// annoying.
    /// </remarks>
    public class Collaborators : BaseBranchAction
    {
        private GitTeam team;
        private string teamname;
        private Permission permission;
        private List<Repository> update_perms = new List<Repository>();
        private IEnumerable<string> only, not;
        private bool exclusive;

        public IGitWrapper Wrapper { get; set; } 

        public Collaborators(IGitWrapper wrapper, string teamname, Permission permission = Permission.Push, IEnumerable<string> only = null, IEnumerable<string> not = null, bool exclusive = true)
        {
            this.teamname = teamname;
            this.permission = permission;
            this.only = only ?? new List<string>();
            this.not = not ?? new List<String>();
           
            this.Wrapper = wrapper;
        }

        public override async Task Check(List<Repository> all_repos, Repository repo)
        {
            // Figure out the team id, this only has to happen once (and this function get repeated)
            if (team == null)
            {
                team = await Wrapper.GetTeamAsync(teamname);
            }

            Func<string, bool> isExcluded = (string tm) => only.Any() && only.All(r => !repo.Name.Equals(r, StringComparison.CurrentCultureIgnoreCase));

            var repo_teams = await Wrapper.Repo.GetTeamsAsync(repo.Name);
            var repo_team = repo_teams.SingleOrDefault(t => t.Name.Equals(team.Name));

            // If we didn't find the team on the repo, 
            //  but it's in the :only list and it's an exclusive setting, then we need to remove it.  
            //  but if it's on the :only list and it's _not_ an exclusive setting, we can skip this action
            //  If it's not on the :only list then we're OK as long as we have the correct permissions.
            // If we didn't find the team on the repo, and they aren't on the :only list, then we should add them. If we 
            //  didn't find the team on the repo and they _are_ on the :only list, we should skip.
            if (repo_team != null)
            {
                var excluded = isExcluded(repo_team.Name);
                if (excluded && exclusive)
                {
                    l($"[UPDATE] Will remove {team.Name} from {repo.Name}", 1);
                }
                else if (excluded && !exclusive) 
                {
                    l($"[SKIP] {repo.Name} doesn't need this action applied.", 1);
                }
                else
                {
                    l($"postition wanted={(int)this.permission} have={(int)repo_team.Permission}");
                    if (repo_team.Permission.ToString().Equals(this.permission.ToString(), StringComparison.CurrentCultureIgnoreCase))
                    {
                        l($"[OK] {team.Name} is already a collaborator of {repo.Name}", 1);
                    } 
                    else 
                    {
                        l($"[UPDATE] {team.Name} is not at {this.permission} (but is {repo_team.Permission}) of {repo.Name}", 1);
                        all_repos.Add(repo);
                    }
                }
            }
            else if (isExcluded(repo.Name))
            {
                l($"[SKIP] {repo.Name} does not need {team.Name} as a collaborator", 1);
            } 
            else 
            {
                l($"[UPDATE] will add {team.Name} to {repo.Name} as {this.permission}", 1);
                all_repos.Add(repo);
            }
        }

        public enum Update {
            Nothing,
            Add,
            UpdatePermission
        }

        public Update Should(string repo, IEnumerable<GitTeam> repoTeams, GitTeam targetTeam, Permission targetPermission) {
            var isIncluded = this.only.Any() ? this.only.Any(t => t.Equals(repo, StringComparison.CurrentCultureIgnoreCase)) : true;
            var isExcluded = this.not.Any(t => t.Equals(repo, StringComparison.CurrentCultureIgnoreCase));
            
            if (!isIncluded || isExcluded) {
                return Update.Nothing;
            }

            var repoTeam = repoTeams.SingleOrDefault(t => t.Name.Equals(targetTeam.Name));
            if (repoTeam == null) {
                return Update.Add;
            } else {
                if ((int) repoTeam.Permission > (int) targetPermission) {
                    return Update.UpdatePermission;
                }
            }

            return Update.Nothing;

            // if (repo_team != null)
            // {
            //     var excluded = isExcluded(repo_team.Name);
            //     if (excluded && exclusive)
            //     {
            //         l($"[UPDATE] Will remove {team.Name} from {repo.Name}", 1);
            //     }
            //     else if (excluded && !exclusive) 
            //     {
            //         l($"[SKIP] {repo.Name} doesn't need this action applied.", 1);
            //     }
            //     else
            //     {
            //         l($"postition wanted={(int)this.permission} have={(int)repo_team.Permission}");
            //         if (repo_team.Permission.ToString().Equals(this.permission.ToString(), StringComparison.CurrentCultureIgnoreCase))
            //         {
            //             l($"[OK] {team.Name} is already a collaborator of {repo.Name}", 1);
            //         } 
            //         else 
            //         {
            //             l($"[UPDATE] {team.Name} is not at {this.permission} (but is {repo_team.Permission}) of {repo.Name}", 1);
            //             all_repos.Add(repo);
            //         }
            //     }
            // }
            // else if (isExcluded(repo.Name))
            // {
            //     l($"[SKIP] {repo.Name} does not need {team.Name} as a collaborator", 1);
            // } 
            // else 
            // {
            //     l($"[UPDATE] will add {team.Name} to {repo.Name} as {this.permission}", 1);
            //     all_repos.Add(repo);
            // }
        }

        public override async Task Action(Repository repo)
        {
            var res = await Wrapper.Repo.UpdateTeamPermissionAsync(repo.Name, team, permission);
            if (res)
            {
                l($"[MODIFIED] Added {team.Name} ({team.Id}) as a collaborator to {repo.Name}", 1);
            }
            else
            {
                l($"[ERROR] Failed to add {team.Name} ({team.Id}) as a collaborator to {repo.Name}", 1);
            }
        }
    }
}
