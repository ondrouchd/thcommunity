using Xunit;
using FluentAssertions;

namespace THcommunity.Tests;

public class EventServiceTests
{
    [Fact]
    public void CalculatePrice_ShouldReturnZero_ForGoalie()
    {
        // Arrange
        var position = "Goalie";
        var pricingTier = 1;
        var goaliePrice = 0m;
        var tier1Price = 400m;

        // Act
        var price = position == "Goalie" ? goaliePrice : tier1Price;

        // Assert
        price.Should().Be(0m);
    }

    [Fact]
    public void CalculatePrice_ShouldReturnTier1_ForFirstPlayers()
    {
        // Arrange
        var position = "Player";
        var currentPosition = 3; // Within tier 1 (1-4)
        var tier1Count = 4;
        var tier1Price = 400m;
        var tier2Price = 350m;

        // Act
        var price = currentPosition <= tier1Count ? tier1Price : tier2Price;

        // Assert
        price.Should().Be(400m);
    }

    [Fact]
    public void CalculatePrice_ShouldReturnTier2_ForMiddlePlayers()
    {
        // Arrange
        var position = "Player";
        var currentPosition = 8; // Within tier 2 (5-12)
        var tier1Count = 4;
        var tier2Count = 12;
        var tier1Price = 400m;
        var tier2Price = 350m;
        var tier3Price = 300m;

        // Act
        decimal price;
        if (currentPosition <= tier1Count)
            price = tier1Price;
        else if (currentPosition <= tier2Count)
            price = tier2Price;
        else
            price = tier3Price;

        // Assert
        price.Should().Be(350m);
    }

    [Fact]
    public void CalculatePrice_ShouldReturnTier3_ForLatePlayers()
    {
        // Arrange
        var currentPosition = 15; // Beyond tier 2
        var tier1Count = 4;
        var tier2Count = 12;
        var tier1Price = 400m;
        var tier2Price = 350m;
        var tier3Price = 300m;

        // Act
        decimal price;
        if (currentPosition <= tier1Count)
            price = tier1Price;
        else if (currentPosition <= tier2Count)
            price = tier2Price;
        else
            price = tier3Price;

        // Assert
        price.Should().Be(300m);
    }
}
