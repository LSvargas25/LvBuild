namespace LvTest.Common;

/// <summary>
/// Shared acting-role arrays for service calls, so tests don't allocate a new constant
/// array per call (CA1861). Never mutate them.
/// </summary>
public static class TestRoles
{
    public static readonly string[] GeneralManager = ["GeneralManager"];
    public static readonly string[] OperationsDirector = ["OperationsDirector"];
    public static readonly string[] ProjectAdmin = ["ProjectAdmin"];
    public static readonly string[] BranchAdmin = ["BranchAdmin"];
    public static readonly string[] BusinessManager = ["BusinessManager"];
}
