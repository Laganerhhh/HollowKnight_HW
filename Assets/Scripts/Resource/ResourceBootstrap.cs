using UnityEngine;

public static class ResourceBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        ResourceManager.EnsureInstance();
        BundleManager.EnsureInstance();
    }
}