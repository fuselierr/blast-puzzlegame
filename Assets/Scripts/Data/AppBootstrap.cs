using UnityEngine;

public static class AppBootstrap
{
    private const string PersistentSystemsPath = "PersistentSystems";

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        // Prevent duplicate bootstrap prefabs.
        if (Object.FindObjectOfType<PersistentSystemsMarker>() != null)
        {
            return;
        }

        GameObject prefab =
            Resources.Load<GameObject>(PersistentSystemsPath);

        if (prefab == null)
        {
            Debug.LogError(
                "Could not find Resources/PersistentSystems.prefab.");
            return;
        }

        GameObject instance = Object.Instantiate(prefab);
        instance.name = "PersistentSystems";

        Object.DontDestroyOnLoad(instance);
    }
}