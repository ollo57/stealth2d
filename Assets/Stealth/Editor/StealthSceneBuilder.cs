using System.IO;
using AlleyStealth;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public static class StealthSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/ServiceAlley.unity";
    private const string MaterialFolder = "Assets/Stealth/Materials";

    [MenuItem("Tools/Alley Stealth/Open Playable Level", priority = 0)]
    public static void OpenPlayableLevel()
    {
        if (!File.Exists(ScenePath))
        {
            Debug.LogError("The playable scene is missing: " + ScenePath);
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [MenuItem("Tools/Alley Stealth/Open Playable Level", true)]
    private static bool CanOpenPlayableLevel() => !EditorApplication.isPlayingOrWillChangePlaymode;

    // Deliberately not an automatic initializer: reopening Unity must never replace scene edits.
    [MenuItem("Tools/Alley Stealth/Create Fresh Test Alley")]
    public static void CreateFreshAlley()
    {
        if (File.Exists(ScenePath))
        {
            Debug.LogWarning("The alley already exists. Open it from Assets/Scenes. " +
                "To regenerate, first move your edited scene to a different path.");
            return;
        }
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory(MaterialFolder);
        Directory.CreateDirectory("Assets/Stealth/Prefabs");
        AssetDatabase.Refresh();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Material ground = MakeMaterial("Asphalt", new Color(0.12f, 0.17f, 0.21f));
        Material brick = MakeMaterial("Old masonry", new Color(0.29f, 0.35f, 0.39f));
        Material edge = MakeMaterial("Dark metal", new Color(0.085f, 0.12f, 0.16f));
        Material cover = MakeMaterial("Storage teal", new Color(0.12f, 0.38f, 0.38f));
        Material amber = MakeMaterial("Wayfinding amber", new Color(1f, 0.65f, 0.22f));
        Material mint = MakeMaterial("Pocket markings", new Color(0.3f, 0.8f, 0.67f));
        Material fur = MakeMaterial("Cat warm grey", new Color(0.7f, 0.64f, 0.53f));
        Material eyes = MakeMaterial("Cat eyes", new Color(0.75f, 0.95f, 0.45f));

        Transform architecture = new GameObject("01 - Alley architecture").transform;
        Box("Walkway", architecture, new Vector3(0, -0.25f, 1), new Vector3(42, 0.5f, 9), ground);
        Box("Back wall", architecture, new Vector3(0, 2.2f, 5), new Vector3(42, 4.4f, 0.5f), brick);
        Box("Front curb", architecture, new Vector3(0, 0.12f, -1.5f), new Vector3(42, 0.24f, 0.35f), edge);
        Box("Left boundary", architecture, new Vector3(-20.5f, 1.5f, 1), new Vector3(1, 3, 8), edge);
        Box("Far gate boundary", architecture, new Vector3(20.5f, 1.5f, 1), new Vector3(1, 3, 8), edge);
        for (int x = -20; x <= 20; x += 4)
        {
            Box("Masonry column", architecture, new Vector3(x, 2.2f, 4.6f), new Vector3(0.25f, 4.4f, 0.4f), edge);
            Box("Walkway guide", architecture, new Vector3(x, 0.006f, -0.85f), new Vector3(1.4f, 0.012f, 0.08f), amber, false);
        }
        Transform pockets = new GameObject("02 - Hiding pockets").transform;
        CreatePocket("01 - Delivery crates", -10f, 2.8f, pockets, cover, mint, edge);
        CreatePocket("02 - Service partition", 0f, 3.4f, pockets, brick, mint, edge);
        CreatePocket("03 - Storage locker", 10f, 2.6f, pockets, cover, mint, edge);
        Label("SERVICE ALLEY", architecture, new Vector3(-17.8f, 2.8f, 4.3f), 0.15f, Color.white);
        Label("FAR GATE", architecture, new Vector3(17.5f, 2.8f, 4.3f), 0.18f, Color.white);
        Box("Gate", architecture, new Vector3(18.6f, 1.3f, 4.4f), new Vector3(2.1f, 2.6f, 0.15f), edge);

        GameObject playerObject = new GameObject("Player - Cat");
        playerObject.transform.position = new Vector3(-18, 0.05f, 0);
        var capsule = playerObject.AddComponent<CharacterController>();
        capsule.height = 0.9f;
        capsule.radius = 0.3f;
        capsule.center = new Vector3(0, 0.45f, 0);
        capsule.skinWidth = 0.025f;
        capsule.stepOffset = 0.12f;
        capsule.minMoveDistance = 0f;
        var player = playerObject.AddComponent<FreeMovementController>();
        Transform visual = new GameObject("Character Visual - replace with imported cat").transform;
        visual.SetParent(playerObject.transform, false);
        BuildPlaceholderCat(visual, fur, eyes, edge);
        visual.localRotation = Quaternion.Euler(0, 90, 0);
        SetReference(player, "inputActions", AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions"));
        PrefabUtility.SaveAsPrefabAssetAndConnect(playerObject, "Assets/Stealth/Prefabs/PlayerCat.prefab", InteractionMode.AutomatedAction);

        GameObject cameraObject = new GameObject("Main Camera - Side view");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.7f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.045f, 0.075f, 0.11f);
        cameraObject.transform.position = new Vector3(-11f, 5.2f, -15f);
        cameraObject.transform.rotation = Quaternion.Euler(12, 0, 0);
        cameraObject.AddComponent<AudioListener>();
        SetReference(cameraObject.AddComponent<IsometricFollowCamera>(), "player", playerObject.transform);

        var sun = new GameObject("Moonlight").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(0.78f, 0.87f, 1f);
        sun.intensity = 1.4f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(45, -30, 0);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.38f, 0.43f, 0.5f);
        var display = new GameObject("03 - Player guidance").AddComponent<StealthStatusDisplay>();
        SetReference(display, "player", player);
        EditorSceneManager.SaveScene(scene, ScenePath);

        // Put the playable slice first while retaining existing build entries.
        var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        foreach (var existing in EditorBuildSettings.scenes)
            if (existing.path != ScenePath) buildScenes.Add(existing);
        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("Created playable alley: " + ScenePath);
    }

    private static void CreatePocket(string name, float x, float width, Transform parent,
        Material material, Material marking, Material trim)
    {
        Transform pocket = new GameObject(name).transform;
        pocket.SetParent(parent);
        pocket.position = new Vector3(x, 0, 0);
        GameObject cover = Box("Solid cover - walk around either end", pocket,
            new Vector3(x, 1.2f, 1.3f), new Vector3(width, 2.4f, 0.6f), material);
        Box("Cover top", pocket, new Vector3(x, 2.44f, 1.3f), new Vector3(width + 0.12f, 0.08f, 0.7f), trim, false);
        Box("Pocket floor", pocket, new Vector3(x, 0.008f, 2.6f), new Vector3(6.8f, 0.016f, 1.4f), trim, false);
        foreach (float side in new[] { -1f, 1f })
        {
            float openingX = x + side * 2.65f;
            Box("Depth entry stripe", pocket, new Vector3(openingX, 0.014f, 1.3f), new Vector3(0.14f, 0.028f, 3.3f), marking, false);
            Label("IN", pocket, new Vector3(openingX, 0.2f, -0.25f), 0.12f, new Color(0.4f, 1f, 0.8f));
        }
        Label(name.Substring(0, 2) + "  /  COVER", pocket, new Vector3(x, 1.6f, 0.985f), 0.12f, Color.white);
    }

    private static void BuildPlaceholderCat(Transform parent, Material fur, Material eyes, Material dark)
    {
        Shape("Body", PrimitiveType.Sphere, parent, new Vector3(0, 0.45f, 0), new Vector3(0.46f, 0.5f, 0.78f), fur);
        Shape("Head", PrimitiveType.Sphere, parent, new Vector3(0, 0.69f, 0.35f), new Vector3(0.43f, 0.39f, 0.37f), fur);
        foreach (float side in new[] { -1f, 1f })
        {
            Shape("Ear", PrimitiveType.Cube, parent, new Vector3(side * 0.14f, 0.89f, 0.32f), new Vector3(0.13f, 0.22f, 0.13f), fur);
            Shape("Eye", PrimitiveType.Sphere, parent, new Vector3(side * 0.105f, 0.74f, 0.513f), new Vector3(0.065f, 0.075f, 0.025f), eyes);
            foreach (float end in new[] { -1f, 1f })
                Shape("Paw", PrimitiveType.Capsule, parent, new Vector3(side * 0.16f, 0.17f, end * 0.25f), new Vector3(0.14f, 0.16f, 0.16f), fur);
        }
        Shape("Nose", PrimitiveType.Sphere, parent, new Vector3(0, 0.65f, 0.535f), new Vector3(0.065f, 0.045f, 0.045f), dark);
        var tail = Shape("Tail", PrimitiveType.Capsule, parent, new Vector3(0, 0.62f, -0.5f), new Vector3(0.09f, 0.3f, 0.09f), fur);
        tail.transform.localRotation = Quaternion.Euler(-30, 0, 0);
    }

    private static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 scale, Material material)
    {
        var shape = GameObject.CreatePrimitive(type);
        shape.name = name;
        shape.transform.SetParent(parent, false);
        shape.transform.localPosition = localPosition;
        shape.transform.localScale = scale;
        Object.DestroyImmediate(shape.GetComponent<Collider>());
        shape.GetComponent<Renderer>().sharedMaterial = material;
        return shape;
    }

    private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool solid = true)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent);
        box.transform.position = position;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        if (!solid) Object.DestroyImmediate(box.GetComponent<Collider>());
        return box;
    }

    private static void Label(string text, Transform parent, Vector3 position, float size, Color color)
    {
        var label = new GameObject(text).AddComponent<TextMesh>();
        label.transform.SetParent(parent);
        label.transform.position = position;
        label.text = text;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.characterSize = size;
        label.fontSize = 48;
        label.color = color;
    }

    private static Material MakeMaterial(string name, Color color)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        material.SetFloat("_Smoothness", 0.15f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void SetReference(Object component, string field, Object value)
    {
        var serialized = new SerializedObject(component);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
