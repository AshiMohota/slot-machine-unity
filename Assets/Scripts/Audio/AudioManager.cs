using UnityEngine;

/// <summary>
/// Singleton audio manager — persists across scene loads.
///
/// Setup:
///   1. Create empty GameObject "AudioManager"
///   2. Add this script + 2 AudioSource components
///   3. Assign SFX AudioSource and BGM AudioSource in Inspector
///   4. Drag all AudioClips from Assets/Sounds/ into the slots
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────
    public static AudioManager Instance { get; private set; }

    // ── Inspector: Audio Sources ──────────────────────────────────────
    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;   // Plays one-shot SFX
    [SerializeField] private AudioSource bgmSource;   // Loops background music

    // ── Inspector: SFX Clips ──────────────────────────────────────────
    [Header("Sound Effects")]
    [SerializeField] private AudioClip spinStartClip;    // Pull lever / start spin
    [SerializeField] private AudioClip reelStopClip;     // Each reel landing click
    [SerializeField] private AudioClip winClip;          // Normal win jingle
    [SerializeField] private AudioClip jackpotClip;      // Jackpot big fanfare
    [SerializeField] private AudioClip buttonClickClip;  // UI button press
    [SerializeField] private AudioClip loseClip;         // Sad trombone / lose
    [SerializeField] private AudioClip errorClip;        // Not enough G error

    [Header("Background Music")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] [Range(0f, 1f)] private float bgmVolume = 0.4f;

    // ────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Singleton enforcement — only one AudioManager in scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);   // Persist through scene loads
    }

    private void Start()
    {
        PlayBGM();
    }

    // ────────────────────────────────────────────────────────────────
    //  SFX Methods
    // ────────────────────────────────────────────────────────────────

    public void PlaySpinStart()    => Play(spinStartClip);
    public void PlayReelStop()     => Play(reelStopClip);
    public void PlayWin()          => Play(winClip);
    public void PlayJackpot()      => Play(jackpotClip);
    public void PlayButtonClick()  => Play(buttonClickClip);
    public void PlayLose()         => Play(loseClip);
    public void PlayError()        => Play(errorClip);

    private void Play(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }

    // ────────────────────────────────────────────────────────────────
    //  BGM Methods
    // ────────────────────────────────────────────────────────────────

    public void PlayBGM()
    {
        if (bgmClip == null || bgmSource == null) return;
        bgmSource.clip   = bgmClip;
        bgmSource.loop   = true;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource?.Stop();
    }

    // ────────────────────────────────────────────────────────────────
    //  Volume Control (call from Settings UI)
    // ────────────────────────────────────────────────────────────────

    public void SetSFXVolume(float v)
    {
        if (sfxSource) sfxSource.volume = Mathf.Clamp01(v);
    }

    public void SetBGMVolume(float v)
    {
        if (bgmSource) bgmSource.volume = Mathf.Clamp01(v);
    }

    public void ToggleMute()
    {
        if (sfxSource) sfxSource.mute = !sfxSource.mute;
        if (bgmSource) bgmSource.mute = !bgmSource.mute;
    }
}
