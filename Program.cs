using System;
using System.Collections.Generic;
using Octokit;
using System.Threading.Tasks;
using Mono.Options;
using System.Collections.Generic;
using System.IO;
using Jil;

namespace gitman
{
    class Program
    {
        private static GitHubClient client;
        private static List<string> devops_repos = new List<string> {
            "devops-chef"
            , "devops-saltstack"
            , "devops-jenkins"
            , "devops-terraform"
            , "devops-vault"
            , "devops-sdk"
            , "devops-sdk-pycert"
            , "devops-k8s"
            , "devops-ansible"
            , "devops-sdk-jcert"
            , "devops-sdk-gocert"
            , "devops-docker"
        };

        static async Task Main(string[] args)
        {
            var opts = new OptionSet() {
                {"u|user=", "(REQUIRED) A github user with admin access.", u => Config.Github.User = u}
                , {"t|token=", "(REQUIRED) A github token that has admin access to the org.", t => Config.Github.Token = t  }
                , {"org=", "The organisation we need to run the actions against (defaults to `sectigo-eng`)", o => Config.Github.Org = o}
                , {"teams=", "A file with the desired team structure.", ts => Config.TeamStructureFile = ts }
                , {"no-dryrun", "Do not change anything, just display changes.", d => Config.DryRun = false }
                , {"h|help", p => Config.Help = true}
            };

            try
            {
                opts.Parse(args);
                Console.WriteLine($"Current configuration: {Config.ToString()}");
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            if (!Config.Validate() || Config.Help)
            {
                opts.WriteOptionDescriptions(Console.Out);
                return;
            }

            if (!Config.DryRun)
            {
                Console.WriteLine("\n\n!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                Console.WriteLine("!!!                Non-DryRun mode - The actions will be DESTRUCTIVE                !!!");
                Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n\n");
            }

            client = new GitHubClient(new ProductHeaderValue("SuperMassiveCLI"));
            client.Credentials = new Credentials(Config.Github.User, Config.Github.Token);

            Console.WriteLine("\n\nChecking merge setting");
            await new Merging(squash: true) { Client = client }.Do();
            
            Console.WriteLine("\n\nChecking repo collaborators");
            await new Collaborators("developers") { Client = client }.Do();
            await new Collaborators("admins", Permission.Admin) { Client = client }.Do();
            await new Collaborators("devops-integrations", only: devops_repos) { Client = client }.Do();

            Console.WriteLine("\n\nChecking branch protections");
            await new Protection() { Client = client }.Do();

            Console.WriteLine("\n\nGenerating audit data on ./");
            var audit = new Audit(outputPath: "./") { Client = client };
            await audit.Do();

            if (Config.HasTeamsStructureFile)
            {
                await CheckTeamMemberships(audit.Data);
            }
        }

        private static async Task CheckTeamMemberships(Audit.AuditDto data) 
        {
                Console.WriteLine("\n\nChecking teams memberships");
                foreach (var team in GetTeams())
                {
                    await new Memberships(data, team.Key, team.Value) { Client = client }.Do();
                }
        }
        
        private static Dictionary<string, List<string>> GetTeams() 
        {
            using var reader = new StreamReader(Config.TeamStructureFile);
            return JSON.Deserialize<Dictionary<string, List<string>>>(reader);
        }
    }
}
