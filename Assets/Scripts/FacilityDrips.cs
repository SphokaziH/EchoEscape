using System.Collections.Generic;
using UnityEngine;


public class FacilityDrips : MonoBehaviour
{
    [System.Serializable]
    public class Drip
    {
        public Transform drop;
        public Vector3 head;     // where the drop forms 
        public float floorY;
        public float fall;       // seconds to fall
        public float period;
        public float phase;
    }

    public const float RippleLife = 1.8f;   //  match the floor 
    public List<Drip> drips = new List<Drip>();

    static readonly int DripDataId = Shader.PropertyToID("_DripData");
    static readonly int DripCountId = Shader.PropertyToID("_DripCount");
    static readonly int TimeId = Shader.PropertyToID("_FacilityTime");
    readonly Vector4[] data = new Vector4[16];

    void Update()
    {
        float t = Time.time;
        int n = Mathf.Min(drips.Count, 16);

        for (int i = 0; i < n; i++)
        {
            Drip d = drips[i];
            data[i] = new Vector4(d.head.x, d.head.z, d.period, d.phase);
            if (d.drop == null) continue;

            float a = (t + d.phase) % d.period;
            float landAt = d.period - RippleLife;
            float formEnd = landAt - d.fall;

            if (a < formEnd)
            {
                float s = Mathf.Clamp01(a / formEnd);
                float size = Mathf.Lerp(0.004f, 0.03f, s * s);
                d.drop.position = d.head + Vector3.down * (size * 0.5f);
                d.drop.localScale = new Vector3(size, size * 1.3f, size);
            }
            else if (a < landAt)
            {
                float s = a - formEnd;
                float y = Mathf.Max(d.floorY, d.head.y - 0.5f * 9.81f * s * s);
                float vel = 9.81f * s;
                d.drop.position = new Vector3(d.head.x, y, d.head.z);
                d.drop.localScale = new Vector3(0.022f, 0.035f + vel * 0.01f, 0.022f);
            }
            else
            {
                d.drop.localScale = Vector3.zero;
            }
        }

        Shader.SetGlobalVectorArray(DripDataId, data);
        Shader.SetGlobalFloat(DripCountId, n);
        Shader.SetGlobalFloat(TimeId, t);
    }

    void OnDisable()
    {
        Shader.SetGlobalFloat(DripCountId, 0f);
    }
}
