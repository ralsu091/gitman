using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;
using System.Linq;
using System;

public class Collaborators : BaseBranchAction
{
    private Team team;
    
    public Collaborators(string username, string token, string teamname) {
        // Figure out the team id
        var all_teams = client.Organization.Team.GetAll("sectigo-eng").Result;
        team = all_teams.Single(t => t.Name.Equals(teamname, StringComparison.OrdinalIgnoreCase));
    }

    public override async Task Check(List<Repository> all_repos, Repository repo)
    {
        var teams = await client.Repository.GetAllTeams(repo.Owner.Login, repo.Name);
        if (teams.Any(t => t.Name.Equals(team)))
        {
            l($"[OK] {team} is already a collaborator of {repo.Name}", 1);
        } else {
            l($"[UPDATE] will add {team} to {repo.Name}", 1);
            all_repos.Add(repo);
        }
    }

    public override async Task Action(Repository repo)
    {
        var res = await client.Organization.Team.AddRepository(team.Id, repo.Owner.Login, repo.Name);
        if (res) {
           l($"[MODIFIED] Added {team} ({team.Id}) as a collaborator to {repo.Name}", 1);
        } else {
            l($"[ERROR] Failed to add {team} ({team.Id}) as a collaborator to {repo.Name}", 1);
        }
    }
}