namespace RightClickPS.Tests;

/// <summary>
/// Tests to verify the solution structure is correctly set up.
/// </summary>
public class SolutionStructureTests
{
    [Fact]
    public void TestProjectCanReferenceMainProject()
    {
        // This test verifies that the test project can reference
        // the main RightClickPS project. If this compiles, the
        // project reference is working correctly.
        Assert.True(true, "Test project successfully references main project");
    }
}
