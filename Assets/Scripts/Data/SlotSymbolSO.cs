using UnityEngine;

/// <summary>
/// ScriptableObject for each slot symbol.
/// Right-click > Create > SlotMachine > Symbol to create a new symbol asset.
/// </summary>
[CreateAssetMenu(fileName = "NewSymbol", menuName = "SlotMachine/Symbol")]
public class SlotSymbolSO : ScriptableObject
{
    [Header("Identity")]
    public string symbolID;           // "seven", "cherry", "bell", "bar"
    public string displayName;        // Shown in UI: "Seven", "Cherry" etc.
    public Sprite sprite;             // Assign in Inspector

    [Header("Payout")]
    [Tooltip("Win amount = currentBet * payoutMultiplier")]
    public int payoutMultiplier = 5;

    [Header("RNG Weight")]
    [Tooltip("Higher = more common. Seven=5, Cherry=20, Bell=25, BAR=50")]
    [Range(1, 100)]
    public int weight = 10;

    [Header("Visual FX")]
    [Tooltip("Color used for win highlight and popup text")]
    public Color winColor = Color.yellow;
    public bool isJackpot = false;    // True only for Seven
}