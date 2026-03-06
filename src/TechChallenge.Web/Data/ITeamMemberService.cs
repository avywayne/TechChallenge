using TechChallenge.Web.Models;
using TechChallenge.Web.Domain;

namespace TechChallenge.Web.Data;

public interface ITeamMemberService
{
    Task<PagedResult<TeamMember>> ListAsync(
        bool  onlyActive     = false,
        int   page           = 1,
        int   pageSize       = 15,
        CancellationToken ct = default);    
    Task<TeamMember?> GetAsync(int id, CancellationToken ct = default);
    Task<(bool Success, string? Error)> CreateAsync(TeamMember input, CancellationToken ct = default);
    Task<(bool Success, string? Error)> UpdateAsync(TeamMember input, CancellationToken ct = default);
    Task<bool> ToggleActiveAsync(int id, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}