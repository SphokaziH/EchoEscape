using System.Collections;
using UnityEngine;


/// 0 = power off (dark walls), 1 = power on (bright walls).
/// Walls start dark every time you press Play.
/// </summary>
public static class WallPower
{
    static readonly int PowerId = Shader.PropertyToID("_FacilityPower");

    public static float Current { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ResetOnPlay() { Set(0f); }

    public static void Set(float value)
    {
        Current = Mathf.Clamp01(value);
        Shader.SetGlobalFloat(PowerId, Current);
    }

 
    public static void PowerOn(float duration = 2.5f)
    {
        Runner.Run(Flicker(duration, true));
    }

   
    public static void PowerOff(float duration = 1.2f)
    {
        Runner.Run(Flicker(duration, false));
    }

    static IEnumerator Flicker(float duration, bool turningOn)
    {
        float t = 0f;
        while (t < duration)
        {
            float k = t / duration;
            float chanceOn = turningOn ? k : 1f - k; // more "on" flashes as time passes
            Set(Random.value < chanceOn ? Random.Range(0.8f, 1f) : Random.Range(0f, 0.15f));
            float hold = Random.Range(0.03f, 0.14f);
            yield return new WaitForSeconds(hold);
            t += hold;
        }
        Set(turningOn ? 1f : 0f);
    }

   
    class Runner : MonoBehaviour
    {
        static Runner instance;

        public static void Run(IEnumerator routine)
        {
            if (instance == null)
            {
                var go = new GameObject("WallPowerRunner");
                UnityEngine.Object.DontDestroyOnLoad(go);
                instance = go.AddComponent<Runner>();
            }
            instance.StopAllCoroutines();
            instance.StartCoroutine(routine);
        }
    }
}
