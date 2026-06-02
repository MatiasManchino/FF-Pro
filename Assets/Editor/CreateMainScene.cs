using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class CreateMainScene
{
    [MenuItem("Tools/Create Main Scene")]
// Crea escena
    static void CreateScene()
    {
        // Asegurar que la carpeta Scenes exista
        string scenesFolder = "Assets/Scenes";
        if (!Directory.Exists(scenesFolder))
        {
            Directory.CreateDirectory(scenesFolder);
            AssetDatabase.Refresh();
        }

        // Crear una nueva escena
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Crear el GameObject GameBootstrapper
        GameObject bootstrapperGO = new GameObject("GameBootstrapper");
        bootstrapperGO.AddComponent<GameBootstrapper>();

        // Guardar la escena
        string scenePath = scenesFolder + "/MainScene.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);

        AssetDatabase.Refresh();
        Debug.Log("Escena principal creada en " + scenePath);
    }
}