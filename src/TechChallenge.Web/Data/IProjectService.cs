using TechChallenge.Web.Models;
using TechChallenge.Web.Domain;

namespace TechChallenge.Web.Data;

public interface IProjectService
{
    Task<PagedResult<Project>> ListAsync(
        bool  includeArchived = false,
        int   page            = 1,
        int   pageSize        = 15,
    CancellationToken ct  = default);    Task<Project?> GetAsync(int id, CancellationToken ct = default);
    Task<(bool Success, string? Error)> CreateAsync(Project input, CancellationToken ct = default);
    Task<(bool Success, string? Error)> UpdateAsync(Project input, CancellationToken ct = default);
    Task<bool> ArchiveAsync(int id, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}