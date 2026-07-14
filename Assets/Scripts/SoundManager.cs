using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Gameplay Clips")]
    [SerializeField] private AudioClip minePlaced;
    [SerializeField] private AudioClip mineReturned;
    [SerializeField] private AudioClip mineBlocked;
    [SerializeField] private AudioClip detonate;
    [SerializeField] private AudioClip debrisHit;
    [SerializeField] private AudioClip levelStart;
    [SerializeField] private AudioClip gameWon;
    [SerializeField] private AudioClip gameLost;

    [Header("UI Clips (usable in ANY scene)")]
    [SerializeField] private AudioClip uiClick;

    [Header("Mixing")]
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField] private int poolSize = 8;

    private List<AudioSource> pool;
    private int nextSource;

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
        src.PlayOneShot(clip, sfxVolume);
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
        src.PlayOneShot(debrisHit, sfxVolume);
    }

    // --- Usable from the main menu, pause menu, anywhere ---
    public void PlayUIClick() => Play(uiClick);
}
