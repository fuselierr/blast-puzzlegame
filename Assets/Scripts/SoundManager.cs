using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    [Header("Music")]
    [SerializeField] private AudioClip musicClip;

    [Header("Gameplay Clips")]
    [SerializeField] private AudioClip minePlaced;
    [SerializeField] private AudioClip mineReturned;
    [SerializeField] private AudioClip mineBlocked;
    [SerializeField] private AudioClip flagPlaced;
    [SerializeField] private AudioClip detonate;
    [SerializeField] private AudioClip debrisHit;
    [SerializeField] private AudioClip levelStart;
    [SerializeField] private AudioClip timerTick;
    [SerializeField] private AudioClip gameWon;
    [SerializeField] private AudioClip gameLost;

    [Header("UI Clips (usable in ANY scene)")]
    [SerializeField] private AudioClip uiClick;

    [Header("Mixing")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField] private bool musicEnabled = true;
    [SerializeField] private int poolSize = 8;

    private List<AudioSource> pool;
    private AudioSource musicSource;
    private int nextSource;

    public float CurrentMasterVolume => masterVolume;
    public float CurrentMusicVolume => musicVolume;
    public float CurrentSfxVolume => sfxVolume;
    public bool CurrentMusicEnabled => musicEnabled;

    private float EffectiveSfxVolume => masterVolume * sfxVolume;
    private float EffectiveMusicVolume => musicEnabled ? masterVolume * musicVolume : 0f;

    // Creates itself the first time anything asks for it — no need to
    // manually drop a prefab into every scene, including your menu.
    public static SoundManager GetOrCreate()
    {
        if (Instance != null) return Instance;

        var prefab = Resources.Load<SoundManager>("SoundManager");
        SoundManager instance = prefab != null
            ? Instantiate(prefab)
            : new GameObject("SoundManager").AddComponent<SoundManager>();

        return instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        pool = new List<AudioSource>(poolSize);
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            pool.Add(src);
        }

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = musicClip;
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        LoadAudioSettings();
        ApplyAudioSettings();
        musicSource.Play();

        SceneManager.sceneLoaded += OnSceneLoaded;
        TryHookGameManager(); // in case gameplay scene is already loaded
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryHookGameManager();
        HookAllButtons();
    }

    // Re-subscribes every time the gameplay scene (re)loads, since
    // GameManager is a fresh instance each time (Play Again, restart, etc.)
    private void TryHookGameManager()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnLevelStart -= HandleLevelStart;
        GameManager.Instance.OnGameWon -= HandleGameWon;
        GameManager.Instance.OnGameLost -= HandleGameLost;
        GameManager.Instance.OnDetonateComplete -= HandleDetonateComplete;

        GameManager.Instance.OnLevelStart += HandleLevelStart;
        GameManager.Instance.OnGameWon += HandleGameWon;
        GameManager.Instance.OnGameLost += HandleGameLost;
        GameManager.Instance.OnDetonateComplete += HandleDetonateComplete;
    }

    private void HandleLevelStart(int level, int total) => Play(levelStart);
    private void HandleGameWon() => Play(gameWon);
    private void HandleGameLost() => Play(gameLost);
    private void HandleDetonateComplete(bool debrisCleared) => Play(detonate);

    private void Play(AudioClip clip)
    {
        if (clip == null || pool.Count == 0) return;
        AudioSource src = pool[nextSource];
        nextSource = (nextSource + 1) % pool.Count;
        src.pitch = 1f;
        src.volume = EffectiveSfxVolume;
        src.PlayOneShot(clip, 1f);
    }

    // --- Gameplay one-offs without a GameManager event ---
    public void PlayMinePlaced() => Play(minePlaced);
    public void PlayMineReturned() => Play(mineReturned);
    public void PlayMineBlocked() => Play(mineBlocked);
    public void PlayDebrisHit()
    {
        if (debrisHit == null || pool.Count == 0) return;
        AudioSource src = pool[nextSource];
        nextSource = (nextSource + 1) % pool.Count;
        src.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
        src.volume = EffectiveSfxVolume;
        src.PlayOneShot(debrisHit, 1f);
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        SaveManager.GetOrCreate()?.SaveMasterVolume(masterVolume);
        ApplyAudioSettings();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        SaveManager.GetOrCreate()?.SaveMusicVolume(musicVolume);
        ApplyAudioSettings();
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        SaveManager.GetOrCreate()?.SaveSfxVolume(sfxVolume);
    }

    public void SetMusicEnabled(bool enabled)
    {
        musicEnabled = enabled;
        SaveManager.GetOrCreate()?.SaveMusicEnabled(enabled);
        ApplyAudioSettings();
    }

    // --- Usable from the main menu, pause menu, anywhere ---
    public void PlayUIClick() => Play(uiClick);
    public void PlayFlagPlaced() => Play(flagPlaced);
    public void PlayTimerTick() => Play(timerTick);

    private void LoadAudioSettings()
    {
        SaveManager save = SaveManager.GetOrCreate();
        if (save == null) return;

        masterVolume = save.LoadMasterVolume();
        musicVolume = save.LoadMusicVolume();
        sfxVolume = save.LoadSfxVolume();
        musicEnabled = save.LoadMusicEnabled();
    }

    private void ApplyAudioSettings()
    {
        if (musicSource != null)
            musicSource.volume = EffectiveMusicVolume;
    }

    // Wires PlayUIClick() to every UI Button in the newly loaded scene,
    // so you never have to manually assign it in the Inspector.
    private void HookAllButtons()
    {
        Button[] buttons = FindObjectsOfType<Button>(includeInactive: true);
        foreach (Button btn in buttons)
        {
            if (btn.GetComponent<NoAutoSfx>() != null) continue; // opt-out escape hatch

            btn.onClick.RemoveListener(PlayUIClick); // avoid double-adding
            btn.onClick.AddListener(PlayUIClick);
        }
    }
}
