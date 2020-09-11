using Octokit;
using System;

namespace gitman
{
    public class GitTeam {
        public enum Perm { 
            admin = 0,
            push = 1,
            pull = 2
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public Perm Permission {get; set; }

        public GitTeam() { }

        public GitTeam(Team team) {
            Id = team.Id;
            Name = team.Name;
            Permission = Enum.Parse<Perm>(team.Permission.StringValue);
        }
    }
}
