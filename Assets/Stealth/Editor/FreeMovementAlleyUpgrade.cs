using System.IO;
using System.Linq;
using AlleyStealth;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FreeMovementAlleyUpgrade
{
    [MenuItem("Tools/Alley Stealth/Apply Free Movement Layout", priority = 25)]
    public static void Apply()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(StealthSceneBuilder.ScenePath);
        if (GameObject.Find("02 - Courtyard obstacles") != null)
        {
            Debug.Log("Free movement layout already exists; edit the saved scene directly.");
            return;
        }
        Directory.CreateDirectory("Logs/Stealth/BeforeFreeMovement");
        File.Copy(StealthSceneBuilder.ScenePath, "Logs/Stealth/BeforeFreeMovement/ServiceAlley.unity", true);
        File.Copy("Assets/Stealth/Prefabs/PlayerCat.prefab", "Logs/Stealth/BeforeFreeMovement/PlayerCat.prefab", true);
        var pockets = GameObject.Find("02 - Hiding pockets");
        if (pockets != null) Object.DestroyImmediate(pockets);
        foreach (Transform item in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (item != null && (item.name.StartsWith("Front curb") || item.name.StartsWith("Walkway guide")))
                Object.DestroyImmediate(item.gameObject);

        var camera = Camera.main;
        camera.name = "Main Camera - Isometric follow";
        Vector3 viewOffset = Vector3.up * 0.8f - camera.transform.forward * 18f;
        SetVector(camera.GetComponent<IsometricFollowCamera>(), "viewOffset", viewOffset);
        camera.transform.position = new Vector3(-11f, 0f, -0.5f) + viewOffset;
        var player = Object.FindFirstObjectByType<FreeMovementController>();
        player.name = "Player - Cat";
        player.transform.position = new Vector3(-18f, 0.05f, -0.5f);
        SetReference(player, "movementCamera", camera);
        PrefabUtility.SaveAsPrefabAssetAndConnect(player.gameObject, "Assets/Stealth/Prefabs/PlayerCat.prefab", InteractionMode.AutomatedAction);
        SetReference(player, "movementCamera", camera);
        SetReference(player.GetComponentInChildren<SpriteDepthOrder>(), "gameplayCamera", camera);

        Transform architecture = GameObject.Find("01 - Alley architecture").transform;
        CreateBlock("Front boundary", architecture, new Vector3(0f, 0.3f, -3.25f),
            new Vector3(42f, 0.6f, 0.5f), new Color(0.2f, 0.25f, 0.34f), "panel", camera);
        Transform obstacles = new GameObject("02 - Courtyard obstacles").transform;
        Color wood = new Color(0.65f, 0.44f, 0.25f);
        Color brick = new Color(0.55f, 0.32f, 0.34f);
        Color stone = new Color(0.43f, 0.4f, 0.56f);
        Color metal = new Color(0.23f, 0.49f, 0.46f);
        Transform crates = Group("Loading crates", obstacles);
        CreateBlock("Loading crate base", crates, new Vector3(-12f, 0.6f, 0.2f), new Vector3(1.8f, 1.2f, 1.4f), wood, "crate", camera);
        CreateBlock("Stacked small crate", crates, new Vector3(-12.2f, 1.6f, 0.3f), new Vector3(1.05f, 0.8f, 1f), wood * 1.12f, "crate", camera);
        CreateBlock("Offset delivery crate", crates, new Vector3(-9.6f, 0.65f, 2.2f), new Vector3(1.3f, 1.3f, 1.2f), wood * 0.85f, "crate", camera);
        SetStackOrder();
        Transform wall = Group("L-shaped service wall", obstacles);
        CreateBlock("Brick wall long arm", wall, new Vector3(-3.8f, 0.9f, 1.6f), new Vector3(3.2f, 1.8f, 0.5f), brick, "brick", camera);
        CreateBlock("Brick wall return", wall, new Vector3(-2.45f, 0.9f, 2.65f), new Vector3(0.5f, 1.8f, 1.6f), brick, "brick", camera);
        Transform pillars = Group("Staggered columns", obstacles);
        CreateBlock("Square pillar", pillars, new Vector3(3.2f, 1.2f, -0.65f), new Vector3(0.85f, 2.4f, 0.85f), stone, "pillar", camera);
        CreateBlock("Short pillar", pillars, new Vector3(5.3f, 0.8f, 2.6f), new Vector3(1.1f, 1.6f, 1.1f), stone * 0.9f, "pillar", camera);
        Transform storage = Group("Utility storage", obstacles);
        CreateBlock("Tall locker", storage, new Vector3(10f, 1.25f, 1.5f), new Vector3(1.3f, 2.5f, 1.1f), metal, "panel", camera);
        CreateBlock("Low freight cart", storage, new Vector3(13.2f, 0.5f, -0.7f), new Vector3(2.3f, 0.65f, 1.2f), new Color(0.31f, 0.46f, 0.62f), "panel", camera);
        foreach (float x in new[] { 12.5f, 13.9f })
            CreateBlock("Cart wheel " + x, storage, new Vector3(x, 0.15f, -1.12f), new Vector3(0.28f, 0.3f, 0.25f), new Color(0.1f, 0.12f, 0.18f), "panel", camera);

        EnemyVision[] guards = Object.FindObjectsByType<EnemyVision>(FindObjectsSortMode.None).OrderBy(guard => guard.name).ToArray();
        Vector3[] positions = { new Vector3(-11f, 0f, -2.2f), new Vector3(1.5f, 0f, 3.8f), new Vector3(12f, 0f, -2.2f) };
        float[] angles = { 0f, 180f, -25f };
        for (int i = 0; i < guards.Length; i++)
        {
            guards[i].transform.position = positions[i];
            guards[i].transform.rotation = Quaternion.Euler(0f, angles[i], 0f);
            guards[i].GetComponentInChildren<SpriteRenderer>().transform.rotation = IsometricSpriteArt.ViewRotation;
            var body = guards[i].gameObject.AddComponent<CapsuleCollider>();
            body.height = 1.5f;
            body.radius = 0.25f;
            body.center = new Vector3(0f, 0.75f, 0f);
        }
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Updated ServiceAlley with unrestricted floor movement and varied courtyard obstacles.");
    }

    private static Transform Group(string name, Transform parent)
    {
        var group = new GameObject(name).transform;
        group.SetParent(parent, false);
        return group;
    }

    public static void RepairStackOrder()
    {
        var scene = EditorSceneManager.OpenScene(StealthSceneBuilder.ScenePath);
        SetStackOrder();
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetStackOrder()
    {
        var upperCrate = GameObject.Find("Stacked small crate").GetComponentInChildren<SpriteDepthOrder>();
        // Both pieces belong to one stack: draw the upper crate just after the base.
        SetReference(upperCrate, "groundAnchor", GameObject.Find("Loading crate base").transform);
        var data = new SerializedObject(upperCrate);
        data.FindProperty("orderOffset").intValue = 1;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateBlock(string name, Transform parent, Vector3 center, Vector3 size, Color color,
        string surfaceStyle, Camera camera)
    {
        var block = new GameObject(name);
        block.transform.SetParent(parent, false);
        block.transform.position = center;
        block.layer = LayerMask.NameToLayer("SightBlocker");
        block.AddComponent<BoxCollider>().size = size;
        var art = new GameObject("Flat art").AddComponent<SpriteRenderer>();
        art.transform.SetParent(block.transform, false);
        art.transform.rotation = IsometricSpriteArt.ViewRotation;
        art.sprite = IsometricSpriteArt.Box(name.Replace(" ", "_"), size, color, true, surfaceStyle);
        art.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Stealth/Materials/Flat sprites.mat");
        var sorting = art.gameObject.AddComponent<SpriteDepthOrder>();
        SetReference(sorting, "groundAnchor", block.transform);
        SetReference(sorting, "gameplayCamera", camera);
        if (name == "Front boundary")
        {
            Object.DestroyImmediate(sorting);
            art.sortingOrder = 7000;
        }
    }

    private static void SetReference(Object target, string field, Object value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(field).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetVector(Object target, string field, Vector3 value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(field).vector3Value = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
