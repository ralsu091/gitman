using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;
using System.Linq;

namespace gitman
{
    public class Memberships
    {
        public GitHubClient Client { get; set; }
        
        private string team_name;
        private List<string> proposted_members;

        public Memberships(string team_name, List<string> proposted_members) {
            this.team_name = team_name;
            this.proposted_members = proposted_members;
        }

        public async Task Do() 
        {
            // get the team
            var teams = await Client.Organization.Team.GetAll(Config.Github.Org);
            var team = teams.Single(t => t.Name.Equals(team_name));

            // get all the members in the team
            var members = await Client.Organization.Team.GetAllMembers(team.Id);

            // figure out the modifications to the team
            var to_remove = members.Where(m => !this.proposted_members.Contains(m.Login)).Select(m => m.Login);
            var to_add = proposted_members.Where(m => !members.Any(gm => gm.Login.Equals(m)));

            Console.WriteLine("To Add: " + Dump(to_add));
            Console.WriteLine("To Remove: " + Dump(to_remove));

            // commit!
        }

        public async Task Audit() 
        {
            var membersByTeams = new Dictionary<string, IEnumerable<string>>();
            IEnumerable<string> members;
            
            // Get all the teams and members
            var teams = await Client.Organization.Team.GetAll(Config.Github.Org);
            foreach (var team in teams)
            {
                var mbs = await Client.Organization.Team.GetAllMembers(team.Id);
                membersByTeams.Add(team.Name, mbs.Select(m => m.Login));
            }           
            
            // Get all our members
            members = (await Client.Organization.Member.GetAll(Config.Github.Org, new ApiOptions { PageSize = 1000 })).Select(m => m.Login);
            
            
        }           

        private string Dump(IEnumerable<string> list) => string.Join(", ", list);
    }
}