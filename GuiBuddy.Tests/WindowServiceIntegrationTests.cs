using GuiBuddy.Infrastructure.Repositories;
using GuiBuddy.Services;
using Xunit;
using Xunit.Abstractions;

namespace GuiBuddy.Tests;

public class WindowServiceIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public WindowServiceIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GetWindows_ShouldReturnWindows()
    {
        // Arrange
        var repository = new AutomationWindowRepository();
        var service = new WindowService(repository);

        // Act
        var windows = service.GetWindows().ToList();

        // Assert
        Assert.NotNull(windows);
        Assert.NotEmpty(windows);

        _output.WriteLine($"Found {windows.Count} windows.");
        foreach (var window in windows.Take(10))
        {
            _output.WriteLine($"Handle: {window.Handle}, Title: {window.Title}, Rect: {window.Bounds}");
        }
    }
}
