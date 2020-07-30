using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;
using System.Linq;
using System.IO;
using Jil;
using System.Text;

namespace gitman
{
    public class Memberships
    {
        public GitHubClient Client { get; set; }
        public Audit.AuditDto AuditData { get; set; }

        public async Task Do(string team_name, List<string> proposted_members) 
        {
            if (AuditData == null) {
                throw new Exception("Audit data has to be set!");
            }

            var team_id = AuditData.Teams.Single(t => t.Value.Equals(team_name)).Key;
            l($"Team mebers in {team_name} ({team_id})", 1);

            // figure out the modifications to the team
            var to_remove = AuditData.MembersByTeam[team_name].Where(m => !proposted_members.Contains(m));
            var to_add = proposted_members.Where(m => !AuditData.MembersByTeam[team_name].Any(gm => gm.Equals(m)));

            foreach (var m in to_add)
            {
                l($"[UPDATE] Will add {m} to {team_name} ({team_id})", 2);
            }

            foreach (var m in to_remove)
            {
                l($"[UPDATE] Will remove {m} from {team_name} ({team_id})", 2);
            }

            if (Config.DryRun) {
                return;
            }

            // Make our output pretty!
            l("");

            foreach (var member in to_add)
            {
                var res = await Client.Organization.Team.AddOrEditMembership(team_id, member, new UpdateTeamMembership(TeamRole.Member));
                l($"[MODIFIED] Added {member} to {team_name} ({team_id}", 2);
            }

            foreach (var member in to_remove)
            {
                var res = await Client.Organization.Team.RemoveMembership(team_id, member);
                if (res)
                    l($"[MODIFIED] Removed {member} from {team_name} ({team_id}", 2);
                else
                    l($"[ERROR] Could not remove {member} from {team_name} ({team_id}", 2);
            }
        }


        private string Dump(IEnumerable<string> list) => string.Join(", ", list);

        protected void l(string msgs, int tab = 0) => Console.WriteLine(new String('\t', tab) + msgs);
    }
}