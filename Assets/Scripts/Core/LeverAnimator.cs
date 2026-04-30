using System.Collections;
using UnityEngine;

/// <summary>
/// Animates the lever on the right side of the slot machine.
/// Attach to the LeverHandle GameObject.
/// </summary>
public class LeverAnimator : MonoBehaviour
{
    [Header("Lever Parts")]
    [Tooltip("The entire lever arm RectTransform that moves up/down")]
    [SerializeField] private RectTransform leverArm;

    [Header("Animation Settings")]
    [Tooltip("How far down the lever moves (pixels)")]
    [SerializeField] private float pullDistance = 80f;

    [Tooltip("Time to pull the lever down (seconds)")]
    [SerializeField] private float pullDuration = 0.2f;

    [Tooltip("Time to spring back up")]
    [SerializeField] private float returnDuration = 0.4f;

    // ── Private ───────────────────────────────────────────────────────
    private bool  isAnimating = false;
    private float restPosY    = 0f;

    private void Awake()
    {
        if (leverArm) restPosY = leverArm.anchoredPosition.y;
    }

    // ─────────────────────────────────────────────────────────────────
    // ✅ THIS METHOD shows in Unity Inspector Button onClick dropdown
    // ─────────────────────────────────────────────────────────────────
    /// <summary>
    /// No-parameter version — assign this in SpinButton onClick Inspector.
    /// </summary>
    public void PullLever()
    {
        if (isAnimating) return;
        StartCoroutine(LeverRoutine(null));
    }

    /// <summary>
    /// With callback — call this from code only.
    /// </summary>
    public void PullLeverWithCallback(System.Action onPullComplete)
    {
        if (isAnimating) return;
        StartCoroutine(LeverRoutine(onPullComplete));
    }

    // ── COROUTINE ─────────────────────────────────────────────────────
    private IEnumerator LeverRoutine(System.Action onPullComplete)
    {
        isAnimating = true;

        // Phase 1: Pull DOWN
        yield return StartCoroutine(MoveLeverTo(restPosY - pullDistance, pullDuration, easeIn: true));

        onPullComplete?.Invoke();

        // Phase 2: Spring BACK with slight overshoot
        yield return StartCoroutine(MoveLeverTo(restPosY + 10f, returnDuration * 0.6f, easeIn: false));

        // Phase 3: Settle to rest
        yield return StartCoroutine(MoveLeverTo(restPosY, returnDuration * 0.4f, easeIn: true));

        isAnimating = false;
    }

    private IEnumerator MoveLeverTo(float targetY, float duration, bool easeIn)
    {
        if (leverArm == null) yield break;

        float startY  = leverArm.anchoredPosition.y;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t      = Mathf.Clamp01(elapsed / duration);
            float curved = easeIn ? t * t : 1f - (1f - t) * (1f - t);
            float newY   = Mathf.Lerp(startY, targetY, curved);
            leverArm.anchoredPosition = new Vector2(leverArm.anchoredPosition.x, newY);
            yield return null;
        }

        leverArm.anchoredPosition = new Vector2(leverArm.anchoredPosition.x, targetY);
    }
}