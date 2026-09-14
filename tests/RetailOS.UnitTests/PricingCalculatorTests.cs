using FluentAssertions;
using RetailOS.Application.Common.Helpers;
using Xunit;

namespace RetailOS.UnitTests;

public class PricingCalculatorTests
{
    [Fact]
    public void CalculateSellingPrice_WithDefault25PercentMarkup_ShouldReturnCorrectCommercialRoundedPrice()
    {
        // Arrange: Cost = 80, Markup = 25% -> 80 * 1.25 = 100.00
        decimal cost = 80m;

        // Act
        var result = PricingCalculator.CalculateSellingPrice(cost, 25m);

        // Assert
        result.Should().Be(100.00m);
    }

    [Theory]
    [InlineData(10.555, 25, 13.19)] // 10.555 * 1.25 = 13.19375 -> rounded to 13.19
    [InlineData(10.556, 25, 13.20)] // 10.556 * 1.25 = 13.195 -> rounded away from zero to 13.20
    [InlineData(100, 30, 130.00)]
    public void CalculateSellingPrice_ShouldApplyCommercialRoundingAwayFromZero(double costDouble, double markupDouble, double expectedDouble)
    {
        decimal cost = (decimal)costDouble;
        decimal markup = (decimal)markupDouble;
        decimal expected = (decimal)expectedDouble;

        // Act
        var result = PricingCalculator.CalculateSellingPrice(cost, markup);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void CalculateSellingPrice_WhenCostIsZero_ShouldReturnNull()
    {
        var result = PricingCalculator.CalculateSellingPrice(0m, 25m);
        result.Should().BeNull();
    }

    [Fact]
    public void CalculateSellingPrice_WhenCostIsNegative_ShouldReturnNull()
    {
        var result = PricingCalculator.CalculateSellingPrice(-5.5m, 25m);
        result.Should().BeNull();
    }

    [Fact]
    public void CalculateSellingPrice_WhenCostIsNull_ShouldReturnNull()
    {
        var result = PricingCalculator.CalculateSellingPrice(null, 25m);
        result.Should().BeNull();
    }

    [Fact]
    public void IsSellingBelowCost_WhenSellingPriceIsLowerThanCost_ShouldReturnTrue()
    {
        // Arrange
        decimal cost = 50m;
        decimal selling = 45m;

        // Act
        var isBelow = PricingCalculator.IsSellingBelowCost(selling, cost);

        // Assert
        isBelow.Should().BeTrue();
    }

    [Fact]
    public void IsSellingBelowCost_WhenSellingPriceIsHigherOrEqualToCost_ShouldReturnFalse()
    {
        // Arrange
        decimal cost = 50m;
        decimal selling = 62.5m;

        // Act
        var isBelow = PricingCalculator.IsSellingBelowCost(selling, cost);

        // Assert
        isBelow.Should().BeFalse();
    }
}
