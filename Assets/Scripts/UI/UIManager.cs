using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages ALL UI in the game: HUD display, popups, buttons, win line, animations.
///
/// Scene Hierarchy expected:
/// [Canvas]
///  ├── [HUD]
///  │    ├── BalanceText (TMP)
///  │    ├── BetText (TMP)
///  │    └── LastWinText (TMP)
///  ├── [StatusText] (TMP)
///  ├── [WinLineImage] (Image)
///  ├── [BetButtonsGroup]
///  │    ├── Bet10Button
///  │    ├── Bet50Button
///  │    └── Bet100Button
///  ├── [SpinButton] (Button)
///  ├── [WinPopup] (GameObject)
///  │    ├── TitleText, MessageText, CoinsText (all TMP)
///  │    ├── YesButton, NoButton (Button)
///  ├── [JackpotPopup]
///  │    └── (same structure + ParticleSystem)
///  └── [GameOverPopup]
///       ├── SpinsText, CoinsText (TMP)
///       └── RestartButton
/// </summary>
public class UIManager : MonoBehaviour
{
    // ────────────────────────────────────────────────────────────────
    //  Inspector — HUD
    // ────────────────────────────────────────────────────────────────

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI balanceText;
    [SerializeField] private TextMeshProUGUI betText;
    [SerializeField] private TextMeshProUGUI lastWinText;
    [SerializeField] private TextMeshProUGUI statusText;

    // ────────────────────────────────────────────────────────────────
    //  Inspector — Buttons
    // ────────────────────────────────────────────────────────────────

    [Header("Buttons")]
    [SerializeField] private Button       spinButton;
    [SerializeField] private Button[]     betButtons;         // 3 buttons (10G, 50G, 100G)
    [SerializeField] private int[]        betButtonValues;    // matching values
    [SerializeField] private Color        activeBetColor   = new Color(1f, 0.7f, 0f);
    [SerializeField] private Color        inactiveBetColor = new Color(0.6f, 0.4f, 0f);

    // ────────────────────────────────────────────────────────────────
    //  Inspector — Win Line
    // ────────────────────────────────────────────────────────────────

    [Header("Win Line")]
    [SerializeField] private Image winLineImage;
    [SerializeField] private float winLineFlashSpeed = 3f;

    private Coroutine winLineCoroutine;

    // ────────────────────────────────────────────────────────────────
    //  Inspector — Popups
    // ────────────────────────────────────────────────────────────────

    [Header("Win Popup")]
    [SerializeField] private GameObject      winPopupRoot;
    [SerializeField] private TextMeshProUGUI winTitleText;
    [SerializeField] private TextMeshProUGUI winMessageText;
    [SerializeField] private TextMeshProUGUI winCoinsText;
    [SerializeField] private Button          winYesButton;
    [SerializeField] private Button          winNoButton;
    [SerializeField] private RectTransform   winPopupRect;

    [Header("Jackpot Popup")]
    [SerializeField] private GameObject      jackpotPopupRoot;
    [SerializeField] private TextMeshProUGUI jackpotCoinsText;
    [SerializeField] private Button          jackpotYesButton;
    [SerializeField] private Button          jackpotNoButton;
    [SerializeField] private ParticleSystem  jackpotParticles;

    [Header("Game Over Popup")]
    [SerializeField] private GameObject      gameOverPopupRoot;
    [SerializeField] private TextMeshProUGUI gameOverSpinsText;
    [SerializeField] private Button          restartButton;

    // ────────────────────────────────────────────────────────────────
    //  Inspector — Balance Animation
    // ────────────────────────────────────────────────────────────────

    [Header("Balance Counter")]
    [Tooltip("Time in seconds for balance to count up/down")]
    [SerializeField] private float balanceAnimDuration = 0.5f;

    private Coroutine balanceCoroutine;
    private int       displayedBalance = 0;

    // ────────────────────────────────────────────────────────────────
    //  Initialisation
    // ────────────────────────────────────────────────────────────────

    /// <summary>Call from SlotMachineManager.Start()</summary>
    public void Initialise(int[] betOptions)
    {
        CloseAllPopups();
        HideWinLine();

        // Sync bet button values from config
        betButtonValues = betOptions;
    }

    // ────────────────────────────────────────────────────────────────
    //  HUD Updates
    // ────────────────────────────────────────────────────────────────

    /// <summary>Animates balance from current displayed value to newBalance.</summary>
    public void UpdateBalance(int newBalance)
    {
        if (balanceCoroutine != null) StopCoroutine(balanceCoroutine);
        balanceCoroutine = StartCoroutine(AnimateBalance(displayedBalance, newBalance));
    }

    public void UpdateBet(int bet)
    {
        if (betText) betText.text = $"BET: {bet} G";
    }

    public void UpdateLastWin(int win)
    {
        if (lastWinText) lastWinText.text = $"WIN: {win} G";
    }

    public void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
    }

    // ────────────────────────────────────────────────────────────────
    //  Button State
    // ────────────────────────────────────────────────────────────────

    public void SetSpinButtonInteractable(bool interactable)
    {
        if (spinButton) spinButton.interactable = interactable;
    }

    public void SetBetButtonsInteractable(bool interactable)
    {
        foreach (Button btn in betButtons)
            if (btn) btn.interactable = interactable;
    }

    /// <summary>Highlights the currently selected bet button with active color.</summary>
    public void HighlightActiveBetButton(int activeBet)
    {
        for (int i = 0; i < betButtons.Length; i++)
        {
            if (betButtons[i] == null) continue;
            bool isActive = (i < betButtonValues.Length && betButtonValues[i] == activeBet);
            var  colors   = betButtons[i].colors;
            colors.normalColor = isActive ? activeBetColor : inactiveBetColor;
            betButtons[i].colors = colors;
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  Win Line
    // ────────────────────────────────────────────────────────────────

    public void ShowWinLine()
    {
        if (winLineImage == null) return;
        winLineImage.gameObject.SetActive(true);
        if (winLineCoroutine != null) StopCoroutine(winLineCoroutine);
        winLineCoroutine = StartCoroutine(FlashWinLine());
    }

    public void HideWinLine()
    {
        if (winLineCoroutine != null) StopCoroutine(winLineCoroutine);
        if (winLineImage) winLineImage.gameObject.SetActive(false);
    }

    private IEnumerator FlashWinLine()
    {
        while (true)
        {
            float t = Mathf.PingPong(Time.time * winLineFlashSpeed, 1f);
            winLineImage.color = Color.Lerp(
                new Color(1f, 0.84f, 0f, 0.2f),
                new Color(1f, 0.84f, 0f, 1f),
                t
            );
            yield return null;
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  Popups
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Shows the win popup with symbol info and payout.
    /// onClose(true) = YES clicked, onClose(false) = NO clicked.
    /// </summary>
    public void ShowWinPopup(SlotSymbolSO symbol, int amount, Action<bool> onClose)
    {
        if (winPopupRoot == null) return;

        winPopupRoot.SetActive(true);

        if (winTitleText)   winTitleText.text   = $"YOU WIN!";
        if (winTitleText)   winTitleText.color   = symbol.winColor;
        if (winMessageText) winMessageText.text  = $"Three {symbol.displayName}s! ({symbol.payoutMultiplier}x)";
        if (winCoinsText)   winCoinsText.text    = $"+{amount} G";

        // Animate popup scale-in
        if (winPopupRect) StartCoroutine(ScaleIn(winPopupRect));

        // Wire buttons
        winYesButton?.onClick.RemoveAllListeners();
        winNoButton?.onClick.RemoveAllListeners();
        winYesButton?.onClick.AddListener(() => { CloseAllPopups(); onClose?.Invoke(true); });
        winNoButton?.onClick.AddListener( () => { CloseAllPopups(); onClose?.Invoke(false); });
    }

    /// <summary>Shows the JACKPOT popup with particle burst.</summary>
    public void ShowJackpotPopup(int amount, Action<bool> onClose)
    {
        jackpotPopupRoot?.SetActive(true);
        if (jackpotCoinsText) jackpotCoinsText.text = $"+{amount} G";
        jackpotParticles?.Play();

        jackpotYesButton?.onClick.RemoveAllListeners();
        jackpotNoButton?.onClick.RemoveAllListeners();
        jackpotYesButton?.onClick.AddListener(() => { CloseAllPopups(); onClose?.Invoke(true); });
        jackpotNoButton?.onClick.AddListener( () => { CloseAllPopups(); onClose?.Invoke(false); });
    }

    /// <summary>Shows Game Over popup with stats.</summary>
    public void ShowGameOverPopup(int spinsPlayed, Action<bool> onRestart)
    {
        gameOverPopupRoot?.SetActive(true);
        if (gameOverSpinsText) gameOverSpinsText.text = $"Total Spins: {spinsPlayed}";

        restartButton?.onClick.RemoveAllListeners();
        restartButton?.onClick.AddListener(() => { CloseAllPopups(); onRestart?.Invoke(true); });
    }

    public void CloseAllPopups()
    {
        winPopupRoot?.SetActive(false);
        jackpotPopupRoot?.SetActive(false);
        gameOverPopupRoot?.SetActive(false);
        jackpotParticles?.Stop();
    }

    // ────────────────────────────────────────────────────────────────
    //  Animations (Coroutines)
    // ────────────────────────────────────────────────────────────────

    /// <summary>Smoothly counts balance text from startVal to endVal.</summary>
    private IEnumerator AnimateBalance(int startVal, int endVal)
    {
        float elapsed = 0f;

        while (elapsed < balanceAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / balanceAnimDuration);
            displayedBalance = Mathf.RoundToInt(Mathf.Lerp(startVal, endVal, t));
            if (balanceText) balanceText.text = $"BALANCE: {displayedBalance} G";
            yield return null;
        }

        displayedBalance = endVal;
        if (balanceText) balanceText.text = $"BALANCE: {endVal} G";
    }

    /// <summary>Bouncy scale-in animation for popups.</summary>
    private IEnumerator ScaleIn(RectTransform target)
    {
        float duration = 0.35f;
        float elapsed  = 0f;

        // Overshoot curve: scale goes 0 → 1.1 → 1.0
        AnimationCurve bounceCurve = new AnimationCurve(
            new Keyframe(0f,    0f,   0f,  5f),
            new Keyframe(0.7f,  1.1f, 0f,  0f),
            new Keyframe(1f,    1f,   0f,  0f)
        );

        target.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = bounceCurve.Evaluate(elapsed / duration);
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        target.localScale = Vector3.one;
    }
}
