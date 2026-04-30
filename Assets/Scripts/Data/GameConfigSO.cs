using UnityEngine;

/// <summary>
/// ScriptableObject holding all game-wide configuration.
/// Right-click > Create > SlotMachine > GameConfig
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "SlotMachine/GameConfig")]
public class GameConfigSO : ScriptableObject
{
    [Header("Starting Balance")]
    public int startingBalance = 500;

    [Header("Bet Amounts")]
    public int[] betOptions = { 10, 50, 100 };

    [Header("Reel Settings")]
    [Tooltip("Height of each symbol cell in pixels (UI units)")]
    public float symbolCellHeight = 150f;

    [Tooltip("Number of symbols visible per reel (viewport)")]
    public int visibleSymbolCount = 3;

    [Tooltip("Total symbols in the virtual reel strip")]
    public int reelStripLength = 30;

    [Header("Spin Durations (seconds)")]
    [Tooltip("Each reel stops slightly later — creates suspense")]
    public float[] reelSpinDurations = { 1.8f, 2.3f, 2.8f };

    [Tooltip("Peak scroll speed during spin (pixels/second)")]
    public float spinScrollSpeed = 2500f;

    [Header("All Symbols")]
    [Tooltip("Drag all SlotSymbolSO assets here")]
    public SlotSymbolSO[] symbols;
}
