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
            
            owner = CreateUser(orgName, orgName, $"{orgName}@foofighters.coffee");
            
            // Return all the teams
            team
                .Setup(t => t.GetAll(It.IsAny<string>()))
                .ReturnsAsync(teams.Select(CreateTeam).ToList().AsReadOnly());

            // return all the repos
            repo
                .Setup(r => r.GetAllForOrg(It.IsAny<string>(), It.IsAny<ApiOptions>()))
                .ReturnsAsync(repos.Select(name => CreateRepo(owner, name)).ToList().AsReadOnly());

            // for project 1 return all the teams
            repo
                .Setup(r => r.GetAllTeams(It.IsAny<string>(), repos.First()))
                .ReturnsAsync(teams.Select(CreateTeam).ToList().AsReadOnly());
            // for project 2 return none of the teams
            repo
                .Setup(r => r.GetAllTeams(It.IsAny<string>(), repos.Last()))
                .ReturnsAsync(new List<Team>().AsReadOnly());

            org.Setup(o => o.Team).Returns(team.Object);
            client.Setup(c => c.Organization).Returns(org.Object);
            client.Setup(c => c.Repository).Returns(repo.Object);

            this.git = client.Object;
            
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
            var collab = new Collaborators(git, "Alpha", Permission.Admin, only: repos.Take(1).ToList());
            await collab.Do();

            // We are only including the first project, which already has everybody setup
            Assert.Empty(collab.Change);
        }

        [Fact]
        public async Task Adding() {
            var collab = new Collaborators(git, "Alpha", Permission.Admin);
            await collab.Do();

            var a = new Mock<Team>();
            var push = new StringEnum<Permission>(Permission.Push);
            a.SetupGet(a => a.Permission).Returns(push as StringEnum<PermissionLevel>);

            
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


