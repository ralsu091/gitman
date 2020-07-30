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
    public class Audit
    {
        public GitHubClient Client { get; set; }
        public AuditDto Data { get; private set; } = new AuditDto();

        private string outputPath;
        private string fileName;

        public Audit(string outputPath = null, string fileName = "audit_report.json") {
            this.outputPath = outputPath;
            this.fileName = fileName;
        }

        public class AuditDto { 
            public Dictionary<int, string> Teams {get; set;} = new Dictionary<int, string>();
            public List<string>  Members {get; set;} = new List<String>();
            public Dictionary<string, IEnumerable<string>> MembersByTeam { get; set; } = new Dictionary<string, IEnumerable<string>>();

            public override string  ToString(){ 
                var s = new StringBuilder();
                s.AppendLine($"Member ({Members.Count})=[{string.Join(", ", Members)}]");
                s.AppendLine($"Teams ({Teams.Count})=[{string.Join(", ", Teams.Select(t => t.Value))}]");
                foreach (var team in MembersByTeam)
                {
                    s.AppendLine($"{team.Key} ({team.Value.Count()})=[{string.Join(", ", team.Value)}]");
                }

                return s.ToString();
            }
        }

        public async Task Do() 
        {            
            // Get all the teams and members
            var teams = await Client.Organization.Team.GetAll(Config.Github.Org);
            foreach (var team in teams)
            {
                Data.Teams.Add(team.Id, team.Name);
                var mbs = await Client.Organization.Team.GetAllMembers(team.Id);
                Data.MembersByTeam.Add(team.Name, mbs.Select(m => m.Login));
            }           
            
            // Get all our members
            Data.Members.AddRange((await Client.Organization.Member.GetAll(Config.Github.Org, new ApiOptions { PageSize = 1000 })).Select(m => m.Login));

            if (!string.IsNullOrEmpty(outputPath))
            {
                using var writer = new StreamWriter(Path.Combine(outputPath, fileName));
                JSON.Serialize(Data, writer, Jil.Options.PrettyPrintCamelCase);
            }
        }

        private string Dump(IEnumerable<string> list) => string.Join(", ", list);

        protected void l(string msgs, int tab = 0) => Console.WriteLine(new String('\t', tab) + msgs);
    }
}