using UnityEngine;

/// <summary>
/// Evaluates spin results and calculates payouts.
/// Pure logic class — no MonoBehaviour, no Unity lifecycle.
/// </summary>
public class WinEvaluator
{
    // ────────────────────────────────────────────────────────────────
    //  Result Data
    // ────────────────────────────────────────────────────────────────

    public enum WinType { None, BAR, Bell, Cherry, Jackpot }

    public struct SpinResult
    {
        public bool        isWin;
        public WinType     winType;
        public int         winAmount;
        public SlotSymbolSO winSymbol;
        public string      message;
    }

    // ────────────────────────────────────────────────────────────────
    //  Evaluation
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Checks if all 3 reel results are the same symbol (3-of-a-kind).
    /// This is the only win condition for a classic 3-reel slot.
    /// </summary>
    /// <param name="results">Array of 3 landed symbols (left, center, right)</param>
    /// <param name="betAmount">Current player bet in G</param>
    /// <returns>SpinResult with win data filled in</returns>
    public SpinResult Evaluate(SlotSymbolSO[] results, int betAmount)
    {
        // Validate input
        if (results == null || results.Length != 3)
        {
            Debug.LogError("[WinEvaluator] Results array must have exactly 3 elements.");
            return new SpinResult { isWin = false };
        }

        // Win condition: all three symbols are identical
        bool threeOfAKind = (results[0].symbolID == results[1].symbolID)
                         && (results[1].symbolID == results[2].symbolID);

        if (!threeOfAKind)
        {
            return new SpinResult
            {
                isWin     = false,
                winType   = WinType.None,
                winAmount = 0,
                winSymbol = null,
                message   = "Try Again!"
            };
        }

        // ── Win! Calculate payout ─────────────────────────────────────
        SlotSymbolSO symbol    = results[0];
        int          winAmount = betAmount * symbol.payoutMultiplier;
        WinType      winType   = GetWinType(symbol);

        return new SpinResult
        {
            isWin     = true,
            winType   = winType,
            winAmount = winAmount,
            winSymbol = symbol,
            message   = BuildWinMessage(symbol, betAmount, winAmount)
        };
    }

    // ────────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────────

    private WinType GetWinType(SlotSymbolSO symbol)
    {
        if (symbol.isJackpot)            return WinType.Jackpot;

        switch (symbol.symbolID)
        {
            case "cherry": return WinType.Cherry;
            case "bell":   return WinType.Bell;
            case "bar":    return WinType.BAR;
            default:       return WinType.None;
        }
    }

    private string BuildWinMessage(SlotSymbolSO symbol, int bet, int totalWin)
    {
        string prefix = symbol.isJackpot ? "JACKPOT!!! " : "Three " + symbol.displayName + "s! ";
        return $"{prefix} ({symbol.payoutMultiplier}x) = +{totalWin} G";
    }
}
