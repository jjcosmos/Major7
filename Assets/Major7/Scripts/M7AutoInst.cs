using UnityEngine;

public static class M7AutoInst
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void InstM7Man()
    {
        if (Major7Man.Singleton != null) return;
        
        var config = Major7Man.GetInitConfig();
        if (!config.AutoInitialize) return;

        var manager = new GameObject();
        manager.AddComponent<Major7Man>();
        manager.name = "M7 Singleton";
    }
}
