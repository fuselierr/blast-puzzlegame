using UnityEngine;

public static class LeanTweenInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        LeanTween.init(2000);
    }
}