using System.Reflection;

namespace Plugin.Maui.SignaturePad.Tests;

public class SmokeTests
{
    [Fact]
    public void LibraryAssemblyLoads()
    {
        var assembly = Assembly.Load("Plugin.Maui.SignaturePad");

        Assert.Equal("Plugin.Maui.SignaturePad", assembly.GetName().Name);
    }
}
