using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls a single reel strip — spin animation + symbol display.
/// 
/// Scene Hierarchy:
///   [ReelViewport]  ← Mask + Image (white) + THIS SCRIPT
///      └── [ReelStrip]  ← RectTransform + VerticalLayoutGroup
///            ├── SymbolCell (Image)
///            ├── SymbolCell (Image)
///            └── ... (STRIP_SIZE total)
/// </summary>
public class ReelController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────
    [Header("Required References")]
    [SerializeField] private RectTransform reelStrip;        // The moving strip
    [SerializeField] private GameObject    symbolCellPrefab; // Prefab with Image component

    [Header("Reel Settings")]
    [SerializeField] private float symbolHeight  = 150f;  // Height of ONE symbol cell
    [SerializeField] private int   visibleCount  = 3;     // How many symbols visible
    [SerializeField] private int   stripSize     = 24;    // Total symbols in strip

    [Header("Spin Speed")]
    [SerializeField] private float fastSpeed     = 3000f; // px/sec at peak

    // ── Events ────────────────────────────────────────────────────────
    public event Action<SlotSymbolSO> OnReelStopped;

    // ── Private ───────────────────────────────────────────────────────
    private List<Image>        cellImages   = new List<Image>();
    private List<SlotSymbolSO> cellSymbols  = new List<SlotSymbolSO>();
    private SlotSymbolSO[]     symbolPool;

    private bool isSpinning = false;
    public  bool IsSpinning => isSpinning;

    // ─────────────────────────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────────────────────────

    public void Initialise(GameConfigSO config)
    {
        symbolPool   = config.symbols;
        symbolHeight = config.symbolCellHeight;
        visibleCount = config.visibleSymbolCount;
        stripSize    = config.reelStripLength;
        fastSpeed    = config.spinScrollSpeed;

        BuildStrip();
        ShowIdlePosition(); // Show symbols at start
    }

    // ─────────────────────────────────────────────────────────────────
    //  BUILD STRIP
    // ─────────────────────────────────────────────────────────────────

    private void BuildStrip()
    {
        // Destroy old cells
        foreach (Transform child in reelStrip)
            Destroy(child.gameObject);

        cellImages.Clear();
        cellSymbols.Clear();

        // Create cells
        for (int i = 0; i < stripSize; i++)
        {
            SlotSymbolSO sym  = PickRandom();
            GameObject   cell = Instantiate(symbolCellPrefab, reelStrip);

            // Force correct size
            RectTransform rt = cell.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(symbolHeight, symbolHeight);

            Image img = cell.GetComponent<Image>();
            img.sprite           = sym.sprite;
            img.preserveAspect   = true;
            img.color            = Color.white;

            cellSymbols.Add(sym);
            cellImages.Add(img);
        }

        // Set strip total height
        float totalHeight = stripSize * symbolHeight;
        reelStrip.sizeDelta = new Vector2(reelStrip.sizeDelta.x, totalHeight);
    }

    // ─────────────────────────────────────────────────────────────────
    //  IDLE POSITION  (called on Start + Reset)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>Show 3 symbols starting from index 0 (top of strip).</summary>
    private void ShowIdlePosition()
    {
        // Anchor strip so index 0 is at the top of the viewport
        // anchoredPosition.y = 0 means top of strip aligns with top of viewport
        reelStrip.anchoredPosition = new Vector2(0, 0);
    }

    // ─────────────────────────────────────────────────────────────────
    //  SPIN
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Spin the reel. Will stop showing landingSymbol in the CENTER row.
    /// </summary>
    public void StartSpin(SlotSymbolSO landingSymbol, float duration)
    {
        if (isSpinning) return;

        BuildStrip(); // Randomize fresh

        // Place landing symbol at a guaranteed center index
        // Center index = the index that ends up in the MIDDLE visible row (row index 1 of 0,1,2)
        // We pick an index in the mid-range of the strip
        int landingIndex = stripSize / 2;

        // Overwrite that cell with our desired symbol
        cellSymbols[landingIndex]       = landingSymbol;
        cellImages[landingIndex].sprite = landingSymbol.sprite;

        StartCoroutine(SpinCoroutine(landingIndex, duration));
    }

    // ─────────────────────────────────────────────────────────────────
    //  SPIN COROUTINE
    // ─────────────────────────────────────────────────────────────────

    private IEnumerator SpinCoroutine(int landingIndex, float totalDuration)
    {
        isSpinning = true;

        // ── Calculate target Y ────────────────────────────────────────
        // We want landingIndex to appear in the MIDDLE row of the viewport.
        // Middle row = row 1 (0-based) of 3 visible rows.
        // 
        // ReelStrip anchor is TOP-LEFT of strip.
        // anchoredPosition.y moves strip DOWN when POSITIVE (in Unity UI).
        //
        // To show index N in the middle:
        //   targetY = N * symbolHeight  - (1 * symbolHeight)
        //           = (N - 1) * symbolHeight
        //
        // This puts:
        //   row 0 (top)    → index N-1
        //   row 1 (middle) → index N      ← payline
        //   row 2 (bottom) → index N+1

        float targetY = (landingIndex - 1) * symbolHeight;

        // ── Phase 1: ACCELERATE (first 15%) ───────────────────────────
        float accelTime = totalDuration * 0.15f;
        float elapsed   = 0f;
        float currentY  = reelStrip.anchoredPosition.y;

        while (elapsed < accelTime)
        {
            elapsed   += Time.deltaTime;
            float t    = elapsed / accelTime;          // 0→1
            float spd  = Mathf.Lerp(0, fastSpeed, t); // ease-in
            currentY  += spd * Time.deltaTime;
            WrapY(ref currentY);
            reelStrip.anchoredPosition = new Vector2(0, currentY);
            yield return null;
        }

        // ── Phase 2: FAST SPIN (middle 55%) ───────────────────────────
        float fastTime = totalDuration * 0.55f;
        elapsed        = 0f;

        while (elapsed < fastTime)
        {
            elapsed  += Time.deltaTime;
            currentY += fastSpeed * Time.deltaTime;
            WrapY(ref currentY);
            reelStrip.anchoredPosition = new Vector2(0, currentY);
            yield return null;
        }

        // ── Phase 3: DECELERATE + SNAP to target (final 30%) ─────────
        float decelTime = totalDuration * 0.30f;
        elapsed         = 0f;
        float startY    = currentY;

        // Make sure targetY is reachable going forward from currentY
        float stripTotalHeight = stripSize * symbolHeight;
        while (targetY < startY)
            targetY += stripTotalHeight;

        // If target is too far ahead, bring it one strip-length back
        while (targetY - startY > stripTotalHeight)
            targetY -= stripTotalHeight;

        while (elapsed < decelTime)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / decelTime);

            // Ease-out cubic
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float y     = Mathf.Lerp(startY, targetY, eased);
            reelStrip.anchoredPosition = new Vector2(0, y);
            yield return null;
        }

        // ── SNAP exactly ──────────────────────────────────────────────
        // Reset position using modulo so symbols align perfectly
        float finalY = (landingIndex - 1) * symbolHeight;
        reelStrip.anchoredPosition = new Vector2(0, finalY);

        isSpinning = false;
        OnReelStopped?.Invoke(cellSymbols[landingIndex]);
    }

    // ─────────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────────

    /// <summary>Keep Y within strip bounds so it loops seamlessly.</summary>
    private void WrapY(ref float y)
    {
        float max = stripSize * symbolHeight;
        if (y > max) y -= max;
        if (y < 0)   y += max;
    }

    private SlotSymbolSO PickRandom() => RNG.PickWeightedSymbol(symbolPool);

    // ─────────────────────────────────────────────────────────────────
    //  WIN HIGHLIGHT
    // ─────────────────────────────────────────────────────────────────

    private Coroutine highlightCoroutine;

    public void HighlightWinSymbol(bool enable)
    {
        // Find center cell index currently shown
        float y           = reelStrip.anchoredPosition.y;
        int   centerIndex = Mathf.RoundToInt(y / symbolHeight) + 1;
        centerIndex       = Mathf.Clamp(centerIndex, 0, cellImages.Count - 1);

        Image img = cellImages[centerIndex];

        if (enable)
        {
            if (highlightCoroutine != null) StopCoroutine(highlightCoroutine);
            highlightCoroutine = StartCoroutine(PulseSymbol(img));
        }
        else
        {
            if (highlightCoroutine != null)
            {
                StopCoroutine(highlightCoroutine);
                highlightCoroutine = null;
            }
            if (img != null)
            {
                img.color                = Color.white;
                img.transform.localScale = Vector3.one;
            }
        }
    }

    private IEnumerator PulseSymbol(Image img)
    {
        while (true)
        {
            float t     = Mathf.PingPong(Time.time * 3f, 1f);
            float scale = Mathf.Lerp(1f, 1.15f, t);
            if (img != null)
            {
                img.color                = Color.Lerp(Color.white, Color.yellow, t);
                img.transform.localScale = Vector3.one * scale;
            }
            yield return null;
        }
    }
}