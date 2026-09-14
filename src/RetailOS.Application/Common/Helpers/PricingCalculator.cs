namespace RetailOS.Application.Common.Helpers;

public static class PricingCalculator
{
    public const decimal DefaultMarkupPercentage = 25.0m;

    /// <summary>
    /// Calculates the suggested retail selling price given the purchase cost and markup percentage.
    /// Handles edge cases:
    /// - Cost <= 0 returns null (signals UI to require manual entry and highlight in red).
    /// - Applies standard commercial rounding to 2 decimal places (MidpointRounding.AwayFromZero).
    /// </summary>
    public static decimal? CalculateSellingPrice(decimal? purchaseCost, decimal markupPercent = DefaultMarkupPercentage)
    {
        if (purchaseCost == null || purchaseCost.Value <= 0)
            return null;

        if (markupPercent < 0)
            markupPercent = 0;

        var rawPrice = purchaseCost.Value * (1m + (markupPercent / 100m));
        return Math.Round(rawPrice, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Checks if a manually overridden or calculated selling price is lower than the purchase cost.
    /// </summary>
    public static bool IsSellingBelowCost(decimal? sellingPrice, decimal? purchaseCost)
    {
        if (sellingPrice == null || purchaseCost == null)
            return false;

        return sellingPrice.Value > 0 && purchaseCost.Value > 0 && sellingPrice.Value < purchaseCost.Value;
    }
}
