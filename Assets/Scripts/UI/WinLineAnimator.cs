using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animates the payline (horizontal line across middle of all 3 reels).
/// Shows a flashing golden line on win, hidden on loss.
///
/// Setup: Attach to the WinLine Image GameObject
/// </summary>
public class WinLineAnimator : MonoBehaviour
{
    [Header("Win Line Image")]
    [SerializeField] private Image winLineImage;

    [Header("Animation")]
    [SerializeField] private float flashSpeed   = 4f;
    [SerializeField] private Color glowColorMin = new Color(1f, 0.84f, 0f, 0.2f);
    [SerializeField] private Color glowColorMax = new Color(1f, 0.84f, 0f, 1f);

    [Header("Symbol Flash (optional)")]
    [Tooltip("The 3 center-row symbol Images to highlight on win")]
    [SerializeField] private Image[] centerSymbolImages = new Image[3];

    private Coroutine flashRoutine;
    private bool      isFlashing = false;

    private void Awake()
    {
        // Hide on start
        if (winLineImage) winLineImage.gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────

    public void ShowAndFlash()
    {
        if (winLineImage) winLineImage.gameObject.SetActive(true);

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());

        // Also flash center symbols
        StartCoroutine(FlashSymbols());
    }

    public void Hide()
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        isFlashing = false;

        if (winLineImage) winLineImage.gameObject.SetActive(false);
        ResetSymbols();
    }

    // ── Coroutines ────────────────────────────────────────────────────

    private IEnumerator FlashRoutine()
    {
        isFlashing = true;
        while (isFlashing)
        {
            float t = Mathf.PingPong(Time.time * flashSpeed, 1f);
            if (winLineImage)
                winLineImage.color = Color.Lerp(glowColorMin, glowColorMax, t);
            yield return null;
        }
    }

    private IEnumerator FlashSymbols()
    {
        float duration  = 2f;  // flash for 2 seconds
        float elapsed   = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.PingPong(elapsed * flashSpeed, 1f);
            Color c  = Color.Lerp(Color.white, Color.yellow, t);

            foreach (Image img in centerSymbolImages)
            {
                if (img) img.color = c;
            }
            yield return null;
        }

        ResetSymbols();
    }

    private void ResetSymbols()
    {
        foreach (Image img in centerSymbolImages)
            if (img) img.color = Color.white;
    }
}
