namespace SCM.Application.Contracts.Branches.Dtos;

public class SwitchBranchResultDto
{
    public bool Success { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public bool IsAllBranches { get; set; }
    public string Message { get; set; } = string.Empty;
}
