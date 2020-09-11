using Octokit;
using System;

namespace gitman
{
    public class GitTeam {
        public enum Perm { 
            Admin = 0,
            Push = 1,
            Pull = 2
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
