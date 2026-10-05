using System.IO;
using System.Linq;
using AlleyStealth;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class IsometricAlleyUpgrade
{
    [MenuItem("Tools/Alley Stealth/Apply Isometric Presentation", priority = 20)]
    public static void Apply()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(StealthSceneBuilder.ScenePath);
        if (GameObject.Find("04 - Night watch") != null)
        {
            Debug.Log("Isometric upgrade is already applied. Edit the saved scene directly.");
            return;
        }
        Directory.CreateDirectory("Logs/Stealth/BeforeIsometric");
        File.Copy(StealthSceneBuilder.ScenePath, "Logs/Stealth/BeforeIsometric/ServiceAlley.unity", true);
        File.Copy("Assets/Stealth/Prefabs/PlayerCat.prefab", "Logs/Stealth/BeforeIsometric/RailCat.prefab", true);
        int blockerLayer = EnsureBlockerLayer();
        Material spriteMaterial = MaterialAt("Flat sprites", "AlleyStealth/FlatSprite");
        Material coneMaterial = MaterialAt("Red vision", "AlleyStealth/VisionOverlay");
        coneMaterial.SetColor("_Color", new Color(1f, 0.08f, 0.16f, 0.48f));
        Camera camera = Object.FindFirstObjectByType<Camera>();
        camera.transform.rotation = IsometricSpriteArt.ViewRotation;
        Vector3 viewOffset = new Vector3(0f, 0.8f, 1f) - camera.transform.forward * 18f;
        camera.transform.position = new Vector3(-11, 0, 0) + viewOffset;
        camera.orthographicSize = 5.3f;
        camera.backgroundColor = new Color(0.065f, 0.085f, 0.14f);
        camera.allowHDR = false;
        camera.allowMSAA = false;
        Set(camera.GetComponent<IsometricFollowCamera>(), "viewOffset", viewOffset);

        var player = Object.FindFirstObjectByType<FreeMovementController>();
        PrefabUtility.UnpackPrefabInstance(player.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        Transform oldVisual = player.transform.Find("Character Visual - replace with imported cat");
        if (oldVisual != null) Object.DestroyImmediate(oldVisual.gameObject);
        Sprite[] idle = Frames("Idle"), walk = Frames("Walk");
        if (idle.Length == 0 || walk.Length == 0) throw new System.InvalidOperationException("Cat sprite frames are missing.");
        SpriteRenderer cat = SpriteObject("Cat - existing pack sprites", player.transform,
            player.transform.position + Vector3.up * 0.35f, idle[0], spriteMaterial, camera, player.transform);
        cat.transform.localScale = Vector3.one * 4f;
        var animation = cat.gameObject.AddComponent<CatSpriteAnimator>();
        Set(animation, "player", player);
        SetArray(animation, "idleFrames", idle);
        SetArray(animation, "walkFrames", walk);
        PrefabUtility.SaveAsPrefabAssetAndConnect(player.gameObject, "Assets/Stealth/Prefabs/PlayerCat.prefab", InteractionMode.AutomatedAction);
        // Scene camera references cannot be stored in a prefab asset; keep this as a scene override.
        Set(cat.GetComponent<SpriteDepthOrder>(), "gameplayCamera", camera);

        int artIndex = 0;
        foreach (MeshRenderer renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (renderer.TryGetComponent(out TextMesh text))
            {
                text.transform.rotation = IsometricSpriteArt.ViewRotation;
                text.characterSize = text.text == "IN" ? 0.055f : 0.06f;
                renderer.sortingOrder = 3000;
                continue;
            }
            Vector3 size = renderer.bounds.size;
            renderer.enabled = false;
            if (renderer.name == "Cover top") continue;
            bool floor = renderer.name == "Walkway" || size.y < 0.5f;
            bool isCover = renderer.name.StartsWith("Solid cover");
            Color color = isCover ? new Color(0.25f, 0.48f, 0.48f) :
                renderer.name == "Walkway" ? new Color(0.25f, 0.3f, 0.38f) :
                renderer.name.Contains("wall") ? new Color(0.3f, 0.34f, 0.45f) :
                renderer.name.Contains("stripe") ? new Color(0.44f, 0.91f, 0.76f) :
                renderer.name.Contains("guide") ? new Color(0.91f, 0.72f, 0.43f) :
                new Color(0.21f, 0.26f, 0.34f);
            Sprite art = IsometricSpriteArt.Box("Alley_" + artIndex++, size, color, !floor);
            var sprite = SpriteObject(renderer.name + " - flat art", renderer.transform.parent,
                renderer.transform.position, art, spriteMaterial, camera, renderer.transform);
            if (floor || renderer.name == "Back wall")
            {
                Object.DestroyImmediate(sprite.GetComponent<SpriteDepthOrder>());
                sprite.sortingOrder = renderer.name == "Back wall" ? -8000 : renderer.name == "Walkway" ? -10000 : -9800;
            }
            if (renderer.TryGetComponent(out Collider collider) && !collider.isTrigger)
                renderer.gameObject.layer = blockerLayer;
        }
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) light.enabled = false;
        Transform watch = new GameObject("04 - Night watch").transform;
        Sprite guardArt = IsometricSpriteArt.Guard();
        var guards = new EnemyVision[3];
        for (int i = 0; i < guards.Length; i++)
        {
            var guard = new GameObject("Lookout " + (i + 1) + " - sweeping sight");
            guard.transform.SetParent(watch);
            guard.transform.position = new Vector3(-10 + i * 10, 0, -1.15f);
            guards[i] = guard.AddComponent<EnemyVision>();
            guard.GetComponent<MeshRenderer>().sharedMaterial = coneMaterial;
            SpriteRenderer figure = SpriteObject("Lookout sprite", guard.transform, guard.transform.position,
                guardArt, spriteMaterial, camera, guard.transform);
            Set(guards[i], "player", player);
            Set(guards[i], "sightBlockers", 1 << blockerLayer);
            Set(guards[i], "guardSprite", figure);
            Set(guards[i], "scanPhase", i * 0.19f);
        }
        SetArray(Object.FindFirstObjectByType<StealthStatusDisplay>(), "lookouts", guards);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Updated the existing alley: cat sprites, isometric scenery, and three raycast lookouts.");
    }

    private static Sprite[] Frames(string action) => AssetDatabase.LoadAllAssetsAtPath(
        "Assets/Pet Cats pack/Sprites/Cat-5/Cat-5-" + action + ".png").OfType<Sprite>()
        .OrderBy(sprite => sprite.rect.x).ToArray();

    public static void UseHighContrastCat()
    {
        var scene = EditorSceneManager.OpenScene(StealthSceneBuilder.ScenePath);
        var player = Object.FindFirstObjectByType<FreeMovementController>();
        var animation = player.GetComponentInChildren<CatSpriteAnimator>();
        Sprite[] idle = Frames("Idle");
        SetArray(animation, "idleFrames", idle);
        SetArray(animation, "walkFrames", Frames("Walk"));
        animation.GetComponent<SpriteRenderer>().sprite = idle[0];
        PrefabUtility.SaveAsPrefabAssetAndConnect(player.gameObject, "Assets/Stealth/Prefabs/PlayerCat.prefab", InteractionMode.AutomatedAction);
        Set(animation.GetComponent<SpriteDepthOrder>(), "gameplayCamera", Camera.main);
        EditorSceneManager.SaveScene(scene);
    }

    private static SpriteRenderer SpriteObject(string name, Transform parent, Vector3 position, Sprite sprite,
        Material material, Camera camera, Transform anchor)
    {
        var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
        renderer.transform.SetParent(parent);
        renderer.transform.position = position;
        renderer.transform.rotation = IsometricSpriteArt.ViewRotation;
        renderer.sprite = sprite;
        renderer.sharedMaterial = material;
        renderer.spriteSortPoint = SpriteSortPoint.Pivot;
        var sorting = renderer.gameObject.AddComponent<SpriteDepthOrder>();
        Set(sorting, "groundAnchor", anchor);
        Set(sorting, "gameplayCamera", camera);
        return renderer;
    }

    private static Material MaterialAt(string name, string shader)
    {
        string path = "Assets/Stealth/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find(shader));
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static int EnsureBlockerLayer()
    {
        int existing = LayerMask.NameToLayer("SightBlocker");
        if (existing >= 0) return existing;
        var manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = manager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue != "") continue;
            layers.GetArrayElementAtIndex(i).stringValue = "SightBlocker";
            manager.ApplyModifiedPropertiesWithoutUndo();
            return i;
        }
        throw new System.InvalidOperationException("No free physics layer for SightBlocker.");
    }

    private static void Set(Object target, string field, Object value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(field).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Set(Object target, string field, float value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(field).floatValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Set(Object target, string field, int value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(field).intValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Set(Object target, string field, Vector3 value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(field).vector3Value = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetArray(Object target, string field, Object[] values)
    {
        var data = new SerializedObject(target);
        SerializedProperty array = data.FindProperty(field);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
