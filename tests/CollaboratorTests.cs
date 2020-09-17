using System.Collections.Generic;
using System.Linq;

using Xunit;
using gitman;
using Octokit;


namespace Tests
{
    public class CollaboratorTests
    {
        public class Exclusion : CollaboratorTests {
            IEnumerable<string> repos = new List<string>{ "portal", "agent", "stuff" };
            IEnumerable<GitTeam> existingTeamsOnRpos = new [] { "bravo", "charlie", "delta" }.Select(t => new GitTeam(1, t));
            
            public class OnlyTests : Exclusion {
                Collaborators collab =  new Collaborators(null, "Alpha", only: new [] { "portal" });
                
                [Fact]
                public void should_do_nothing_with_notportal() 
                {
                    var action = collab.Should("NotPortal", existingTeamsOnRpos, new GitTeam(1, "Alpha"), Permission.Pull);
                    Assert.Equal(Collaborators.Update.Skip, action);            
                }

                [Fact]
                public void should_add_to_portal() 
                {
                    var action = collab.Should("Portal", existingTeamsOnRpos, new GitTeam(1, "Alpha"), Permission.Pull);
                    Assert.Equal(Collaborators.Update.Add, action);            
                }

                [Fact]
                public void should_update_permission() 
                {
                    existingTeamsOnRpos = new [] { "Alpha" }.Select(t => new GitTeam(1, t, GitTeam.Perm.pull));
                    var action = collab.Should("Portal", existingTeamsOnRpos, new GitTeam(1, "Alpha"), Permission.Admin);
                    Assert.Equal(Collaborators.Update.UpdatePermission, action);  
                }

                
                [Fact]
                public void should_not_downgrade_permission() 
                {
                    existingTeamsOnRpos = new [] { "Alpha" }.Select(t => new GitTeam(1, t, GitTeam.Perm.push));
                    var action = collab.Should("Portal", existingTeamsOnRpos, new GitTeam(1, "Alpha"), Permission.Pull);
                    Assert.Equal(Collaborators.Update.Nothing, action);  
                }
            }
        
            public  class NotTests : Exclusion {
                Collaborators collab =  new Collaborators(null, "Alpha", not: new [] { "portal" });

                [Fact]
                public void should_not_add_portal() {
                    var action = collab.Should("portal", existingTeamsOnRpos, new GitTeam(1, "Alpha"), Permission.Pull);
                    Assert.Equal(Collaborators.Update.Skip, action);
                }

                
                [Fact]
                public void should_add_to_notportal() {
                    var action = collab.Should("notportal", existingTeamsOnRpos, new GitTeam(1, "Echo"), Permission.Pull);
                    Assert.Equal(Collaborators.Update.Add, action);
                }
            }
        }        
    }
}
