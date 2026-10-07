using UnityEngine;

/// <summary>
/// Global wall-material power state.
/// FacilityPower calls Set(0), then PowerOn(2f) when the generator is restored.
/// The wall shader reads the global _WallPower value.
/// </summary>
public static class WallPower
{
    const string ShaderProperty = "_WallPower";

    static float currentPower = 0f;
    static float startPower = 0f;
    static float targetPower = 0f;
    static float transitionStart = -1f;
    static float transitionDuration = 0f;

    static bool initialized;

    static void EnsureInitialized()
    {
        if (initialized) return;

        initialized = true;
        currentPower = 0f;
        targetPower = 0f;
        Shader.SetGlobalFloat(ShaderProperty, 0f);
    }

    public static void Set(float value)
    {
        EnsureInitialized();

        currentPower = Mathf.Clamp01(value);
        startPower = currentPower;
        targetPower = currentPower;
        transitionStart = -1f;
        transitionDuration = 0f;

        Shader.SetGlobalFloat(ShaderProperty, currentPower);
    }

    public static void PowerOn(float duration)
    {
        EnsureInitialized();

        startPower = currentPower;
        targetPower = 1f;

        transitionStart = Time.time;
        transitionDuration = Mathf.Max(0.01f, duration);

        Shader.SetGlobalFloat(ShaderProperty, currentPower);
    }

    public static float Current => currentPower;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitializeOnLoad()
    {
        initialized = false;
        EnsureInitialized();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartUpdater()
    {
        WallPowerUpdater.Create();
    }

    sealed class WallPowerUpdater : MonoBehaviour
    {
        static WallPowerUpdater instance;

        public static void Create()
        {
            if (instance != null) return;

            GameObject go = new GameObject("WallPowerUpdater");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<WallPowerUpdater>();
        }

        void Update()
        {
            EnsureInitialized();

            if (transitionStart >= 0f)
            {
                float t =
                    Mathf.Clamp01(
                        (Time.time - transitionStart) /
                        transitionDuration
                    );

                // Smooth electrical fade.
                float eased =
                    t * t * (3f - 2f * t);

                currentPower =
                    Mathf.Lerp(
                        startPower,
                        targetPower,
                        eased
                    );

                Shader.SetGlobalFloat(
                    ShaderProperty,
                    currentPower
                );

                if (t >= 1f)
                {
                    currentPower = targetPower;
                    transitionStart = -1f;

                    Shader.SetGlobalFloat(
                        ShaderProperty,
                        currentPower
                    );
                }
            }
        }
    }
}
