using System.Collections;
using UnityEngine;

/// <summary>
/// ╔══════════════════════════════════════════════════════════════╗
/// ║              SLOT MACHINE MANAGER — MAIN CONTROLLER          ║
/// ╠══════════════════════════════════════════════════════════════╣
/// ║  Orchestrates: balance, bets, spin flow, win evaluation,     ║
/// ║  and cross-communication between all other components.       ║
/// ╚══════════════════════════════════════════════════════════════╝
///
/// Scene Setup:
///   1. Create empty GameObject "SlotMachineManager"
///   2. Attach this script
///   3. Drag in all 3 ReelControllers, UIManager, AudioManager
///   4. Assign a GameConfigSO asset in the Inspector
/// </summary>
public class SlotMachineManager : MonoBehaviour
{
    // ────────────────────────────────────────────────────────────────
    //  Inspector References
    // ────────────────────────────────────────────────────────────────

    [Header("Configuration (ScriptableObject)")]
    [SerializeField] private GameConfigSO gameConfig;

    [Header("Reel Controllers (Left, Center, Right)")]
    [SerializeField] private ReelController[] reels = new ReelController[3];

    [Header("Managers")]
    [SerializeField] private UIManager     uiManager;
    [SerializeField] private AudioManager  audioManager;

    // ────────────────────────────────────────────────────────────────
    //  Private State
    // ────────────────────────────────────────────────────────────────

    // Balance & Bet
    private int balance;
    private int currentBet;
    private int lastWin;
    private int totalSpins;

    // Spin state tracking
    private bool           isSpinning    = false;
    private SlotSymbolSO[] reelResults   = new SlotSymbolSO[3];
    private int            reelsDoneCount = 0;

    // Evaluator (pure logic, no MonoBehaviour)
    private WinEvaluator winEvaluator = new WinEvaluator();

    // ────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        ValidateSetup();
    }

    private void Start()
    {
        InitialiseGame();
    }

    private void Update()
    {
        HandleKeyboardInput();
    }

    // ────────────────────────────────────────────────────────────────
    //  Initialisation
    // ────────────────────────────────────────────────────────────────

    private void InitialiseGame()
    {
        // Set starting values
        balance    = gameConfig.startingBalance;
        currentBet = gameConfig.betOptions[0];
        lastWin    = 0;
        totalSpins = 0;

        // Initialise each reel with config
        for (int i = 0; i < reels.Length; i++)
        {
            reels[i].Initialise(gameConfig);
            int reelIndex = i;  // capture for lambda
            reels[i].OnReelStopped += (symbol) => OnSingleReelStopped(reelIndex, symbol);
        }

        // Set initial UI state
        uiManager.Initialise(gameConfig.betOptions);
        uiManager.UpdateBalance(balance);
        uiManager.UpdateBet(currentBet);
        uiManager.UpdateLastWin(0);
        uiManager.SetStatus("PRESS SPIN");
        uiManager.SetSpinButtonInteractable(true);
    }

    // ────────────────────────────────────────────────────────────────
    //  Keyboard Input
    // ────────────────────────────────────────────────────────────────

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            TrySpin();

        if (Input.GetKeyDown(KeyCode.Alpha1)) SetBet(gameConfig.betOptions[0]); // Bet 10
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetBet(gameConfig.betOptions[1]); // Bet 50
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetBet(gameConfig.betOptions[2]); // Bet 100
    }

    // ────────────────────────────────────────────────────────────────
    //  Bet Management (called by UI buttons)
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets current bet amount. Called from UI bet buttons via Inspector event.
    /// </summary>
    public void SetBet(int amount)
    {
        if (isSpinning) return;
        currentBet = amount;
        uiManager.UpdateBet(currentBet);
        uiManager.HighlightActiveBetButton(currentBet);
        audioManager.PlayButtonClick();
    }

    // ────────────────────────────────────────────────────────────────
    //  Spin Flow
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Entry point for spin. Called by the SPIN button's onClick event.
    /// Validates balance, deducts bet, starts reel animations.
    /// </summary>
    public void TrySpin()
    {
        if (isSpinning) return;

        if (balance < currentBet)
        {
            uiManager.SetStatus("NOT ENOUGH G!");
            audioManager.PlayError();
            return;
        }

        StartCoroutine(SpinSequence());
    }

    private IEnumerator SpinSequence()
    {
        // ── 1. Pre-spin setup ──────────────────────────────────────
        isSpinning    = true;
        reelsDoneCount = 0;
        balance      -= currentBet;
        lastWin       = 0;
        totalSpins++;

        uiManager.SetSpinButtonInteractable(false);
        uiManager.SetBetButtonsInteractable(false);
        uiManager.HideWinLine();
        uiManager.UpdateBalance(balance);
        uiManager.SetStatus("SPINNING...");

        // Clear old results
        for (int i = 0; i < 3; i++) reelResults[i] = null;

        // ── 2. Determine outcome via RNG ───────────────────────────
        SlotSymbolSO[] outcome = RNG.GetSpinOutcome(gameConfig.symbols);

        // ── 3. Play spin sound & start all reels ──────────────────
        audioManager.PlaySpinStart();

        for (int i = 0; i < reels.Length; i++)
        {
            reels[i].StartSpin(outcome[i], gameConfig.reelSpinDurations[i]);
        }

        // ── 4. Wait for all reels to stop ─────────────────────────
        yield return new WaitUntil(() => reelsDoneCount >= 3);

        // ── 5. Evaluate result ─────────────────────────────────────
        EvaluateResult();
    }

    // ────────────────────────────────────────────────────────────────
    //  Reel Callback
    // ────────────────────────────────────────────────────────────────

    private void OnSingleReelStopped(int reelIndex, SlotSymbolSO symbol)
    {
        reelResults[reelIndex] = symbol;
        reelsDoneCount++;
        audioManager.PlayReelStop();
    }

    // ────────────────────────────────────────────────────────────────
    //  Win Evaluation
    // ────────────────────────────────────────────────────────────────

    private void EvaluateResult()
    {
        WinEvaluator.SpinResult result = winEvaluator.Evaluate(reelResults, currentBet);

        if (result.isWin)
        {
            // ── Win path ───────────────────────────────────────────
            balance += result.winAmount;
            lastWin  = result.winAmount;

            uiManager.UpdateBalance(balance);
            uiManager.UpdateLastWin(lastWin);
            uiManager.ShowWinLine();
            uiManager.SetStatus(result.winType == WinEvaluator.WinType.Jackpot
                ? $"JACKPOT! +{result.winAmount}G"
                : $"WIN! +{result.winAmount}G");

            // Highlight all 3 winning reels
            foreach (var reel in reels) reel.HighlightWinSymbol(true);

            if (result.winType == WinEvaluator.WinType.Jackpot)
            {
                audioManager.PlayJackpot();
                uiManager.ShowJackpotPopup(result.winAmount, OnPopupClosed);
            }
            else
            {
                audioManager.PlayWin();
                uiManager.ShowWinPopup(result.winSymbol, result.winAmount, OnPopupClosed);
            }
        }
        else
        {
            // ── Lose path ──────────────────────────────────────────
            audioManager.PlayLose();
            uiManager.UpdateBalance(balance);

            if (balance <= 0)
            {
                uiManager.SetStatus("GAME OVER");
                StartCoroutine(DelayThen(0.8f, () =>
                    uiManager.ShowGameOverPopup(totalSpins, OnGameOverClosed)));
            }
            else
            {
                uiManager.SetStatus("TRY AGAIN");
                EndSpin();
            }
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  Popup Callbacks
    // ────────────────────────────────────────────────────────────────

    /// <summary>Called when player clicks YES (Play Again) on the win popup.</summary>
    private void OnPopupClosed(bool playAgain)
    {
        foreach (var reel in reels) reel.HighlightWinSymbol(false);

        if (!playAgain)
        {
            ResetGame();
            return;
        }

        EndSpin();
    }

    private void OnGameOverClosed(bool restart)
    {
        ResetGame();
    }

    // ────────────────────────────────────────────────────────────────
    //  Reset & Helpers
    // ────────────────────────────────────────────────────────────────

    private void EndSpin()
    {
        isSpinning = false;
        uiManager.SetSpinButtonInteractable(true);
        uiManager.SetBetButtonsInteractable(true);
    }

    private void ResetGame()
    {
        balance    = gameConfig.startingBalance;
        lastWin    = 0;
        totalSpins = 0;

        uiManager.UpdateBalance(balance);
        uiManager.UpdateLastWin(0);
        uiManager.HideWinLine();
        uiManager.SetStatus("PRESS SPIN");
        uiManager.CloseAllPopups();

        foreach (var reel in reels)
        {
            reel.HighlightWinSymbol(false);
            reel.Initialise(gameConfig);
        }

        EndSpin();
    }

    private IEnumerator DelayThen(float seconds, System.Action callback)
    {
        yield return new WaitForSeconds(seconds);
        callback?.Invoke();
    }

    private void ValidateSetup()
    {
        if (gameConfig == null)
            Debug.LogError("[SlotMachineManager] GameConfigSO not assigned!");
        if (reels.Length != 3)
            Debug.LogError("[SlotMachineManager] Exactly 3 ReelControllers needed!");
        if (uiManager == null)
            Debug.LogError("[SlotMachineManager] UIManager not assigned!");
    }
}
