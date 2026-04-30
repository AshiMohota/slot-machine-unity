using UnityEngine;

/// <summary>
/// Utility class for Weighted Random Number Generation.
/// Keeps RNG logic separate from game logic (Single Responsibility).
/// </summary>
public static class RNG
{
    /// <summary>
    /// Picks a random symbol using weighted probability.
    /// Symbol with higher weight appears more often.
    /// Example: BAR(50) is 10x more likely than Seven(5).
    /// </summary>
    public static SlotSymbolSO PickWeightedSymbol(SlotSymbolSO[] symbols)
    {
        // Step 1: sum all weights
        int totalWeight = 0;
        foreach (SlotSymbolSO sym in symbols)
            totalWeight += sym.weight;

        // Step 2: roll a random number in [0, totalWeight)
        int roll = Random.Range(0, totalWeight);

        // Step 3: walk through symbols, subtracting weight until we overshoot
        foreach (SlotSymbolSO sym in symbols)
        {
            roll -= sym.weight;
            if (roll < 0)
                return sym;
        }

        // Fallback (should never hit)
        return symbols[symbols.Length - 1];
    }

    /// <summary>
    /// Returns the RNG outcome for all 3 reels independently.
    /// Each reel is a completely independent random pick.
    /// </summary>
    public static SlotSymbolSO[] GetSpinOutcome(SlotSymbolSO[] symbols)
    {
        return new SlotSymbolSO[]
        {
            PickWeightedSymbol(symbols),
            PickWeightedSymbol(symbols),
            PickWeightedSymbol(symbols)
        };
    }

    /// <summary>
    /// Calculates theoretical RTP (Return To Player) percentage.
    /// Useful for tuning symbol weights.
    /// </summary>
    public static float CalculateTheoreticalRTP(SlotSymbolSO[] symbols, int betAmount)
    {
        int totalWeight = 0;
        foreach (var sym in symbols) totalWeight += sym.weight;

        float rtp = 0f;
        foreach (var sym in symbols)
        {
            float prob = (float)sym.weight / totalWeight;
            float probThreeOfAKind = prob * prob * prob;
            float payout = betAmount * sym.payoutMultiplier;
            rtp += probThreeOfAKind * payout;
        }

        return (rtp / betAmount) * 100f;
    }
}
