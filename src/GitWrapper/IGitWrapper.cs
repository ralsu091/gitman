using System.Threading.Tasks;

namespace gitman
{
    public interface IGitWrapper {
        IRepo Repo { get; set; }
        Task<GitTeam> GetTeamAsync(string name);
    }
}
