#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Authors the sample with Unity 2022's scene serializer and imported script GUID.</summary>
public static class PongSampleScene
{
    [MenuItem("Tools/Pipeline Samples/Create Pong Scene")]
    public static void CreateScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var pong = new GameObject("Pong").AddComponent<PongScript>();
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(pong.gameObject, scene);
        var directory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(pong)));
        var path = AssetDatabase.GenerateUniqueAssetPath(directory.Replace('\\', '/') + "/PongScene.unity");
        if (!EditorSceneManager.SaveScene(scene, path))
            throw new IOException("Could not save Pong sample scene: " + path);
        Selection.activeObject = pong.gameObject;
    }
}
#endif
