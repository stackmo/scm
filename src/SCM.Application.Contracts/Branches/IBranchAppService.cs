using SCM.Application.Contracts.Branches.Dtos;
using SCM.Domain.Branches;

namespace SCM.Application.Contracts.Branches;

public interface IBranchAppService
{
    Task<List<BranchDto>> GetListAsync();
    Task<BranchDto> GetByIdAsync(Guid id);
    Task<BranchDto?> GetByCodeAsync(string code);
    Task<BranchDto> CreateAsync(BranchCreateUpdateDto input);
    Task<BranchDto> UpdateAsync(Guid id, BranchCreateUpdateDto input);
    Task<SwitchBranchResultDto> SwitchBranchAsync(string branchCode);
    Task<SwitchBranchResultDto> SwitchBranchAsync(SwitchBranchInputDto input);
    void ValidateMasterDataModification(string role, string operationName);
    Task ValidateCrossBranchAccessAsync<TEntity>(TEntity? entity, string entityId, string entityName) where TEntity : class, IBranchScopedEntity;
}
