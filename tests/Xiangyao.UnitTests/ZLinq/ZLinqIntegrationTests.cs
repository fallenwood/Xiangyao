using ZLinq;
using Xiangyao.Docker;

namespace Xiangyao.UnitTests.ZLinq;

public class ZLinqIntegrationTests
{
    [Fact]
    public void FirstOrDefault_WithPredicate_ShouldWorkWithLabels()
    {
        // Arrange
        var labels = new Label[]
        {
            new() { Name = "xiangyao.enable", Value = "true" },
            new() { Name = "xiangyao.cluster.port", Value = "8080" },
            new() { Name = "other.label", Value = "value" }
        };

        // Act
        var enableLabel = labels.AsValueEnumerable().FirstOrDefault(e => e.Name == "xiangyao.enable");
        var portLabel = labels.AsValueEnumerable().FirstOrDefault(e => e.Name == "xiangyao.cluster.port");
        var nonExistentLabel = labels.AsValueEnumerable().FirstOrDefault(e => e.Name == "non.existent");

        // Assert
        enableLabel.Should().NotBeNull();
        enableLabel!.Value.Should().Be("true");
        
        portLabel.Should().NotBeNull();
        portLabel!.Value.Should().Be("8080");
        
        nonExistentLabel.Should().BeNull();
    }

    [Fact]
    public void FirstOrDefault_WithoutPredicate_ShouldReturnFirstElement()
    {
        // Arrange
        var labels = new Label[]
        {
            new() { Name = "first", Value = "value1" },
            new() { Name = "second", Value = "value2" }
        };

        // Act
        var firstLabel = labels.AsValueEnumerable().FirstOrDefault();

        // Assert
        firstLabel.Should().NotBeNull();
        firstLabel!.Name.Should().Be("first");
    }

    [Fact]
    public void FirstOrDefault_EmptyArray_ShouldReturnNull()
    {
        // Arrange
        var labels = Array.Empty<Label>();

        // Act
        var result = labels.AsValueEnumerable().FirstOrDefault();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Any_WithPredicate_ShouldWorkCorrectly()
    {
        // Arrange
        var labels = new Label[]
        {
            new() { Name = "xiangyao.enable", Value = "true" },
            new() { Name = "other.label", Value = "value" }
        };

        // Act
        var hasEnableLabel = labels.AsValueEnumerable().Any(e => e.Name == "xiangyao.enable");
        var hasNonExistentLabel = labels.AsValueEnumerable().Any(e => e.Name == "non.existent");

        // Assert
        hasEnableLabel.Should().BeTrue();
        hasNonExistentLabel.Should().BeFalse();
    }

    [Fact]
    public void Count_WithPredicate_ShouldWorkCorrectly()
    {
        // Arrange
        var labels = new Label[]
        {
            new() { Name = "xiangyao.enable", Value = "true" },
            new() { Name = "xiangyao.cluster.port", Value = "8080" },
            new() { Name = "other.label", Value = "value" }
        };

        // Act
        var xiangyaoLabelsCount = labels.AsValueEnumerable().Count(e => e.Name.StartsWith("xiangyao"));
        var allLabelsCount = labels.AsValueEnumerable().Count(e => true);

        // Assert
        xiangyaoLabelsCount.Should().Be(2);
        allLabelsCount.Should().Be(3);
    }
}