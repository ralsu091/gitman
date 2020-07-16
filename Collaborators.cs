using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;
using System.Linq;
using System;

public class Collaborators : BaseBranchAction
{
    private Team team;
    private string teamname;
    
    public Collaborators(string teamname) {
        this.teamname = teamname;
    }

    public override async Task Check(List<Repository> all_repos, Repository repo)
    {
        // Figure out the team id
        if (team == null) {
            var all_teams = await Client.Organization.Team.GetAll("sectigo-eng");
            team = all_teams.Single(t => t.Name.Equals(teamname, StringComparison.OrdinalIgnoreCase));
        }

        var teams = await Client.Repository.GetAllTeams(repo.Owner.Login, repo.Name);
        if (teams.Any(t => t.Name.Equals(team.Name)))
        {
            l($"[OK] {team.Name} is already a collaborator of {repo.Name}", 1);
        } else {
            l($"[UPDATE] will add {team.Name} to {repo.Name}", 1);
            all_repos.Add(repo);
        }
    }

    public override async Task Action(Repository repo)
    {
        var res = await Client.Organization.Team.AddRepository(team.Id, repo.Owner.Login, repo.Name);
        if (res) {
           l($"[MODIFIED] Added {team.Name} ({team.Id}) as a collaborator to {repo.Name}", 1);
        } else {
            l($"[ERROR] Failed to add {team.Name} ({team.Id}) as a collaborator to {repo.Name}", 1);
        }
    }
}