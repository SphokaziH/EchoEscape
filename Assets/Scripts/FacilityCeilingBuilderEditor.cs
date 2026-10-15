#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Adds Build / Clear buttons to the bottom of the Facility Ceiling Builder inspector.
// MUST live in a folder named "Editor" (e.g. Assets/Scripts/Editor).
[CustomEditor(typeof(FacilityCeilingBuilder))]
public class FacilityCeilingBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FacilityCeilingBuilder b = (FacilityCeilingBuilder)target;
        EditorGUILayout.Space(10);

        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("Build Ceiling", GUILayout.Height(34)))
        {
            b.Build();
            EditorUtility.SetDirty(b);
            EditorSceneManager.MarkSceneDirty(b.gameObject.scene);
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("Clear Ceiling"))
        {
            b.Clear();
            EditorUtility.SetDirty(b);
            EditorSceneManager.MarkSceneDirty(b.gameObject.scene);
        }
    }
}
#endif
