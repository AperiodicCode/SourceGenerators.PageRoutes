namespace AperiodicCode.SourceGenerators.PageRoutes.IntegrationTests;

public class IntegrationTests
{
    [Fact]
    public void Generated_TestRoute()
    {
        Assert.Equal("/test", Pages.Test);
        Assert.Equal("/test-with-parameters/hello/42", Pages.TestWithParameters("hello", 42));
    }
}
