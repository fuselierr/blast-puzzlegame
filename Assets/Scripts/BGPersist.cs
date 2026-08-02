using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BGPersist : MonoBehaviour
{
    private static BGPersist instance;
    private const float DefaultExitDuration = 2f;
    private const float DefaultExitDistance = 12f;

    public static BGPersist Instance => instance;

    private bool hasInitializedMainMenu = false;
    private readonly Dictionary<int, Vector3> originalPositions = new Dictionary<int, Vector3>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool shouldShowBackground = scene.name != "Game" && scene.name != "Classic" && scene.name != "LevelEditor";
        gameObject.SetActive(shouldShowBackground);

        if (scene.name == "MainMenu")
        {
            RestoreMainMenuBackground();
        }
    }

    public void AnimateExitThenLoad(string sceneName)
    {
        if (!gameObject.activeInHierarchy)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        SlowRotate[] backgroundTiles = GetComponentsInChildren<SlowRotate>(includeInactive: false);
        if (backgroundTiles.Length == 0)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        SaveOriginalPositions(backgroundTiles);

        for (int i = 0; i < backgroundTiles.Length; i++)
        {
            Transform tileTransform = backgroundTiles[i].transform;
            Vector3 exitTarget = tileTransform.position + new Vector3(Random.Range(-2f, 2f), -DefaultExitDistance, 0f);
            LeanTween.move(tileTransform.gameObject, exitTarget, DefaultExitDuration)
                .setEase(LeanTweenType.easeInQuad);
        }

        LeanTween.delayedCall(DefaultExitDuration, () => SceneManager.LoadScene(sceneName));
    }

    private void SaveOriginalPositions(SlowRotate[] backgroundTiles)
    {
        originalPositions.Clear();
        for (int i = 0; i < backgroundTiles.Length; i++)
        {
            GameObject tileObject = backgroundTiles[i].gameObject;
            originalPositions[tileObject.GetInstanceID()] = tileObject.transform.position;
        }
    }

    private void RestoreMainMenuBackground()
    {
        if (originalPositions.Count == 0)
        {
            return;
        }

        SlowRotate[] backgroundTiles = GetComponentsInChildren<SlowRotate>(includeInactive: false);
        for (int i = 0; i < backgroundTiles.Length; i++)
        {
            Transform tileTransform = backgroundTiles[i].transform;
            if (!originalPositions.TryGetValue(tileTransform.gameObject.GetInstanceID(), out Vector3 originalPosition))
            {
                continue;
            }

            tileTransform.position = originalPosition + new Vector3(0f, DefaultExitDistance, 0f);
            LeanTween.move(tileTransform.gameObject, originalPosition, DefaultExitDuration)
                .setEase(LeanTweenType.easeInQuad);
        }

        originalPositions.Clear();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}