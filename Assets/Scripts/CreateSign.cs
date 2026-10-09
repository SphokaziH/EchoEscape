// Put this file in a folder named "Editor" (Assets/Editor/CreateSign.cs).
// Select your Safe (or DoorPivot / Door) in the Hierarchy, then use Tools > Add Emergency Sign.
using UnityEngine;
using UnityEditor;
using TMPro;

public static class CreateSign
{
    [MenuItem("Tools/Add Emergency Sign")]
    static void Create()
    {
        var sel = Selection.activeGameObject;
        if (sel == null)
        {
            Debug.LogError("Select your Safe, DoorPivot or Door in the Hierarchy first.");
            return;
        }

        Transform pivot = FindPivot(sel.transform);
        if (pivot == null)
        {
            Debug.LogError("Couldn't find an object named 'DoorPivot'. Select the Safe, DoorPivot or Door.");
            return;
        }

        if (pivot.Find("SignPlate") != null)
        {
            Debug.LogWarning("DoorPivot already has a SignPlate. Delete it first if you want to recreate the sign.");
            return;
        }

        Material red = GetRedMaterial();

        // ---------- Plate ----------
        var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(plate, "Add Emergency Sign");
        plate.name = "SignPlate";
        plate.transform.SetParent(pivot, false);
        plate.transform.localPosition = new Vector3(0.5f, 0.3f, -0.055f);
        plate.transform.localScale = new Vector3(0.5f, 0.15f, 0.01f);
        plate.GetComponent<Renderer>().sharedMaterial = red;
        Object.DestroyImmediate(plate.GetComponent<Collider>());

        // ---------- Text ----------
        var textGO = new GameObject("SignText");
        Undo.RegisterCreatedObjectUndo(textGO, "Add Emergency Sign");
        textGO.transform.SetParent(pivot, false);
        textGO.transform.localPosition = new Vector3(0.5f, 0.3f, -0.062f);
        textGO.transform.localScale = Vector3.one * 0.1f;

        var tmp = textGO.AddComponent<TextMeshPro>();
        tmp.text = "IN CASE OF EMERGENCY";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 1;
        tmp.fontSizeMax = 10;
        tmp.rectTransform.sizeDelta = new Vector2(5f, 1.5f);

        Selection.activeGameObject = plate;
        Debug.Log("Emergency sign added under " + pivot.name + ".");
    }

    static Transform FindPivot(Transform t)
    {
        if (t.name == "DoorPivot") return t;
        if (t.name == "Door" && t.parent != null && t.parent.name == "DoorPivot") return t.parent;
        foreach (var child in t.GetComponentsInChildren<Transform>(true))
            if (child.name == "DoorPivot") return child;
        return null;
    }

    static Material GetRedMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>("Assets/M_SignRed.mat");
        if (existing != null) return existing;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var m = new Material(shader);
        m.color = new Color(0.78f, 0.08f, 0.08f);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Smoothness", 0.3f);
        m.SetFloat("_Glossiness", 0.3f);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", new Color(0.25f, 0f, 0f));
        AssetDatabase.CreateAsset(m, "Assets/M_SignRed.mat");
        return m;
    }
}
