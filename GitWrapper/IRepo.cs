using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;

namespace gitman
{
    public interface IRepo {
        Task<IEnumerable<GitTeam>> GetTeamsAsync(string reponame);
        Task<bool> UpdateTeamPermissionAsync(string reponame, GitTeam target, Permission targetPermission);
    }
}
