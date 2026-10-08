namespace ComputerRepairSystem.company.Context;

public class CurrentBranchContext
{
    public int? BranchId { get; private set; }

    public event Action? BranchChanged;

    public void SetBranch(int? branchId)
    {
        BranchId = branchId;
        BranchChanged?.Invoke();
    }

    public void ClearBranch()
    {
        BranchId = null;
        BranchChanged?.Invoke();
    }
}