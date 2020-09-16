using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;
using gitman;
using Octokit;
using Moq;
using System.Threading.Tasks;
using Xunit.Abstractions;

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

        public CollaboratorTests(ITestOutputHelper output) 
        {
            Console.SetOut(new Loggy(output));
        }

        public class Exclusion : CollaboratorTests {
            IEnumerable<string> repos = new List<string>{ "portal", "agent", "stuff" };
            IEnumerable<GitTeam> existingTeamsOnRpos = new [] { "bravo", "charlie", "delta" }.Select(t => new GitTeam(1, t));
            
            public Exclusion(ITestOutputHelper output) : base(output) { }

            public class OnlyTests : Exclusion {
                Collaborators collab =  new Collaborators(null, "Alpha", only: new [] { "portal" });
                
                public OnlyTests(ITestOutputHelper output) : base(output) { }

                [Fact]
                public void should_do_nothing_with_notportal() 
                {
                    var action = collab.Should("NotPortal", existingTeamsOnRpos, new GitTeam(1, "Alpha"), Permission.Pull);
                    Assert.Equal(Collaborators.Update.Nothing, action);            
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
            }
        
            public  class NotTests : Exclusion {
                Collaborators collab =  new Collaborators(null, "Alpha", not: new [] { "portal" });

                public NotTests(ITestOutputHelper output) : base(output) { }

                [Fact]
                public void should_not_add_portal() {
                    var action = collab.Should("portal", existingTeamsOnRpos, new GitTeam(1, "Alpha"), Permission.Pull);
                    Assert.Equal(Collaborators.Update.Nothing, action);
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
