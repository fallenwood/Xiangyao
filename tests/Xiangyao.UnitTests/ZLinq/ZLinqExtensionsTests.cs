using Xiangyao.ZLinq;

namespace Xiangyao.UnitTests.ZLinq;

public class ZLinqExtensionsTests
{
    [Fact]
    public void FirstOrDefault_WithPredicate_ShouldReturnFirstMatch()
    {
        // Arrange
        var items = new[] { "apple", "banana", "cherry" };

        // Act
        var result = items.FirstOrDefault(x => x.StartsWith("b"));

        // Assert
        result.Should().Be("banana");
    }

    [Fact]
    public void FirstOrDefault_WithPredicate_ShouldReturnDefaultWhenNoMatch()
    {
        // Arrange
        var items = new[] { "apple", "banana", "cherry" };

        // Act
        var result = items.FirstOrDefault(x => x.StartsWith("z"));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FirstOrDefault_WithoutPredicate_ShouldReturnFirstElement()
    {
        // Arrange
        var items = new[] { "apple", "banana", "cherry" };

        // Act
        var result = items.FirstOrDefault();

        // Assert
        result.Should().Be("apple");
    }

    [Fact]
    public void FirstOrDefault_EmptyArray_ShouldReturnDefault()
    {
        // Arrange
        var items = Array.Empty<string>();

        // Act
        var result = items.FirstOrDefault();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Any_WithPredicate_ShouldReturnTrueWhenMatch()
    {
        // Arrange
        var items = new[] { 1, 2, 3, 4, 5 };

        // Act
        var result = items.Any(x => x > 3);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Any_WithPredicate_ShouldReturnFalseWhenNoMatch()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };

        // Act
        var result = items.Any(x => x > 5);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Count_WithPredicate_ShouldReturnCorrectCount()
    {
        // Arrange
        var items = new[] { 1, 2, 3, 4, 5 };

        // Act
        var result = items.Count(x => x > 3);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void WhereInPlace_ShouldFilterCorrectly()
    {
        // Arrange
        var items = new[] { 1, 2, 3, 4, 5 };
        var buffer = new int[3];

        // Act
        var count = items.WhereInPlace(x => x > 2, buffer);

        // Assert
        count.Should().Be(3);
        buffer[0].Should().Be(3);
        buffer[1].Should().Be(4);
        buffer[2].Should().Be(5);
    }

    [Fact]
    public void SelectInPlace_ShouldTransformCorrectly()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        var buffer = new string[3];

        // Act
        var count = items.SelectInPlace(x => x.ToString(), buffer);

        // Assert
        count.Should().Be(3);
        buffer[0].Should().Be("1");
        buffer[1].Should().Be("2");
        buffer[2].Should().Be("3");
    }
}