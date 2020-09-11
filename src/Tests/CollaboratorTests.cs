using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;
using gitman;
using Octokit;
using Moq;
using System.Threading.Tasks;
using System.IO;
using Xunit.Abstractions;
using System.Text;

namespace Tests
{
    public class CollaboratorTests
    {
        // If we didn't find the team on the repo, 
        //  but it's in the :only list and it's an exclusive setting, then we need to remove it.  
        //  but if it's on the :only list and it's _not_ an exclusive setting, we can skip this action
        //  If it's not on the :only list then we're OK as long as we have the correct permissions.
        // If we didn't find the team on the repo, and they aren't on the :only list, then we should add them. If we 
        //  didn't find the team on the repo and they _are_ on the :only list, we should skip.
        
        IGitWrapper wrapper;
        IGitHubClient git;
        User owner;

        Mock<IGitHubClient> client;
        Mock<IOrganizationsClient> org;
        Mock<ITeamsClient> team;
        Mock<IRepositoriesClient> repo;
        
        List<string> teams = new List<string> {
            "Alpha",
            "Bravo", 
        };
        List<string> repos = new List<string> {
            "Project 1",
            "Project 2",
        };
        
        public CollaboratorTests(ITestOutputHelper output) 
        {
            Console.SetOut(new Loggy(output));

            var client = new Mock<IGitHubClient>();
            org = new Mock<IOrganizationsClient>();
            team = new Mock<ITeamsClient>();
            repo = new Mock<IRepositoriesClient>();
            
            var orgName = "FooFighters";
            Config.Github.Org = orgName;
            
            this.wrapper = new GitWrapper(git);
            
            var r = new Mock<IRepo>();
            r.Setup(a => a.GetTeamsAsync(repos.First()))
                .ReturnsAsync(teams.Select((t, i) => new GitTeam { Id = i + 1, Name = t, Permission = GitTeam.Perm.push }));
            r.Setup(a => a.GetTeamsAsync(repos.Last()))
                .ReturnsAsync(teams.Select((t, i) => new GitTeam { Id = i + 1, Name = t, Permission = GitTeam.Perm.push }));

            var w = new Mock<IGitWrapper>();
            w.SetupGet(a => a.Repo).Returns(r.Object);
            w.Setup(a => a.GetTeamAsync("Alpha"))
                .ReturnsAsync(new GitTeam { Id = 1, Name = "Alpha", Permission = GitTeam.Perm.push});
            w.Setup(a => a.GetTeamAsync("Bravo"))
                .ReturnsAsync(new GitTeam { Id = 1, Name = "Alpha", Permission = GitTeam.Perm.push});
            
            this.wrapper = w.Object;
        }

        /* 
            foo fighters
                project 1
                    alpha & brave
                project 2
                    nothing
        */

        [Fact]
        public async Task Only_Included()
        {
            var collab = new Collaborators(wrapper, "Alpha", Permission.Admin, only: repos.Take(1).ToList());
            await collab.Do();

            // We are only including the first project, which already has everybody setup
            Assert.Empty(collab.Change);
        }

        [Fact]
        public async Task Adding() {
            var collab = new Collaborators(wrapper, "Alpha", Permission.Admin);
            await collab.Do();

            Assert.Collection(collab.Change.Select(r => r.Name), 
                item => item.Equals(repos.Last())
            );
        }
        
        private Team CreateTeam(string name) => 
            new Team("", 1, "", "", name, "", TeamPrivacy.Closed, PermissionLevel.Admin, 0, 0, null, null, "");
        
        private Repository CreateRepo(User owner, string name) => 
            new Repository("", "", "", "", "", "", "", 1, "", owner, name, "FULLNAME", false, "", "","", false, false, 1, 1, "", 1, null, DateTimeOffset.Now, DateTimeOffset.Now, null, null, null, null, false, false, false, false, 1, 1, null, false, null, false, 1);
        
        private User CreateUser(string login, string name, string email) => 
            new User("", "", "", 1, "", DateTimeOffset.Now, DateTimeOffset.Now, 1, email, 1, 1, null, "", 1, 1, "", login, name, "", 1, null, 1, 1, 1, "", null, false, "", null);
    }

    public class Loggy : TextWriter
    {
        ITestOutputHelper _output;
        public Loggy(ITestOutputHelper output)
        {
            _output = output;
        }
        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
        public override void WriteLine(string message)
        {
            _output.WriteLine(message);
        }
        public override void WriteLine(string format, params object[] args)
        {
            _output.WriteLine(format, args);
        }
    }

}


