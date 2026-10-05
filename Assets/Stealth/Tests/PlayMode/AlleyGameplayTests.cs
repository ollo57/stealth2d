using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using AlleyStealth;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class AlleyGameplayTests
{
    private FreeMovementController player;
    private Keyboard keyboard;
    private InputSettings.BackgroundBehavior previousBackgroundBehavior;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior;
#endif
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        SceneManager.LoadScene("ServiceAlley");
        yield return null;
        player = Object.FindFirstObjectByType<FreeMovementController>();
        foreach (EnemyVision guard in Object.FindObjectsByType<EnemyVision>(FindObjectsSortMode.None))
            typeof(EnemyVision).GetField("scanHalfAngle", Fields).SetValue(guard, 0f);
        keyboard = InputSystem.AddDevice<Keyboard>();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior;
#endif
        yield return null;
    }

    private IEnumerator PlacePlayer(float x, float z)
    {
        var controller = player.GetComponent<CharacterController>();
        controller.enabled = false;
        player.transform.position = new Vector3(x, 0.03f, z);
        controller.enabled = true;
        Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return null;
    }

    private IEnumerator Hold(float seconds, params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        yield return null;
        Assert.IsTrue(keyboard[keys[0]].isPressed, "Virtual keyboard input must reach the game.");
        yield return new WaitForSeconds(seconds);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
    }

    [UnityTest]
    public IEnumerator MovementIsFreeOutsideOldPocketsAndNeverSnapsBack()
    {
        yield return PlacePlayer(-17f, 0f);
        Vector3 start = player.transform.position;
        yield return Hold(0.4f, Key.W);
        Assert.That(player.transform.position.z, Is.GreaterThan(start.z + 1f));
        Vector3 stopped = player.transform.position;
        yield return new WaitForSeconds(0.3f);
        Assert.That(player.transform.position.z, Is.EqualTo(stopped.z).Within(0.01f));
        yield return Hold(0.6f, Key.S);
        Assert.That(player.transform.position.z, Is.LessThan(0f));
        Assert.IsNull(GameObject.Find("02 - Hiding pockets"));
    }

    [UnityTest]
    public IEnumerator CameraRelativeInputAndDiagonalSpeedAreConsistent()
    {
        yield return PlacePlayer(-17f, 0f);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
        yield return null;
        yield return null;
        Vector3 right = Camera.main.transform.right;
        Assert.That(Vector3.Angle(player.ActualMovement, right), Is.LessThan(1f));
        float straightSpeed = player.ActualMovement.magnitude / Time.deltaTime;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.W));
        yield return null;
        yield return null;
        float diagonalSpeed = player.ActualMovement.magnitude / Time.deltaTime;
        Assert.That(diagonalSpeed, Is.EqualTo(straightSpeed).Within(0.1f));
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
    }

    [UnityTest]
    public IEnumerator CratesAndOuterBoundariesStillBlockMovement()
    {
        yield return PlacePlayer(-12f, -1.2f);
        yield return Hold(0.25f, Key.W);
        Assert.That(player.transform.position.z, Is.LessThan(-0.74f), "The crate must block the capsule.");
        yield return PlacePlayer(-17f, -2.65f);
        yield return Hold(0.6f, Key.S);
        Assert.That(player.transform.position.z, Is.GreaterThan(-2.76f), "Front boundary must keep the cat on the floor.");
        yield return PlacePlayer(18.9f, 0f);
        yield return Hold(0.7f, Key.D);
        Assert.That(player.transform.position.x, Is.LessThan(19.8f));
        yield return PlacePlayer(-17f, 4.2f);
        yield return Hold(0.5f, Key.W);
        Assert.That(player.transform.position.z, Is.LessThan(4.5f));
    }

    [UnityTest]
    public IEnumerator PhysicalCoverBlocksSightAndRemovingItExposesTheSamePoint()
    {
        EnemyVision guard = FirstGuard();
        Vector3 behindCrate = new Vector3(-12f, 0f, 2f);
        Assert.IsTrue(guard.IsInViewSector(behindCrate));
        Assert.IsFalse(guard.CanSeePosition(behindCrate));
        GameObject.Find("Loading crate base").GetComponent<Collider>().enabled = false;
        Physics.SyncTransforms();
        Assert.IsTrue(guard.CanSeePosition(behindCrate));
        Assert.IsTrue(guard.CanSeePosition(new Vector3(-11f, 0f, -0.8f)));
        Assert.IsFalse(guard.CanSeePosition(new Vector3(-11f, 0f, -4f)));
        Assert.IsFalse(guard.CanSeePosition(new Vector3(-11f, 0f, 8f)));
        yield return null;
    }

    [UnityTest]
    public IEnumerator ConeEndpointsMatchSightRaysAndSweepTogether()
    {
        EnemyVision guard = FirstGuard();
        yield return null;
        Vector3[] points = guard.GetComponent<MeshFilter>().sharedMesh.vertices;
        for (int i = 1; i < points.Length; i++)
        {
            Vector3 direction = guard.transform.TransformPoint(points[i]) - guard.transform.position;
            direction.y = 0f;
            Assert.That(direction.magnitude, Is.EqualTo(guard.VisibleDistance(direction.normalized)).Within(0.005f));
        }
        typeof(EnemyVision).GetField("scanHalfAngle", Fields).SetValue(guard, 55f);
        typeof(EnemyVision).GetField("scanPhase", Fields).SetValue(guard, -Time.time / 10f);
        yield return null;
        Vector3 before = guard.ViewDirection;
        yield return new WaitForSeconds(0.6f);
        Assert.That(Vector3.Angle(before, guard.ViewDirection), Is.GreaterThan(5f));
        points = guard.GetComponent<MeshFilter>().sharedMesh.vertices;
        Vector3 center = guard.transform.TransformPoint(points[points.Length / 2]) - guard.transform.position;
        center.y = 0f;
        Assert.That(Vector3.Angle(center, guard.ViewDirection), Is.LessThan(0.6f));
    }

    [UnityTest]
    public IEnumerator CameraFollowsBothFloorAxesWithoutRotating()
    {
        yield return PlacePlayer(-2f, -1f);
        yield return new WaitForSeconds(0.9f);
        Camera camera = Camera.main;
        Vector3 before = camera.transform.position;
        Quaternion rotation = camera.transform.rotation;
        yield return PlacePlayer(1f, 2f);
        yield return new WaitForSeconds(0.9f);
        Vector3 movement = camera.transform.position - before;
        Assert.That(movement.x, Is.EqualTo(3f).Within(0.12f));
        Assert.That(movement.z, Is.EqualTo(3f).Within(0.12f));
        Assert.That(movement.y, Is.EqualTo(0f).Within(0.01f));
        Assert.That(Quaternion.Angle(rotation, camera.transform.rotation), Is.LessThan(0.01f));
    }

    [UnityTest]
    public IEnumerator CatUsesPackSpritesAndFacesScreenDirection()
    {
        yield return PlacePlayer(-17f, 0f);
        SpriteRenderer sprite = player.GetComponentInChildren<SpriteRenderer>();
        Assert.That(sprite.sprite.name, Does.StartWith("Cat-5-Idle"));
        yield return Hold(0.15f, Key.D);
        Assert.IsFalse(sprite.flipX);
        yield return Hold(0.15f, Key.A);
        Assert.IsTrue(sprite.flipX);
        Assert.That(GameObject.Find("Walkway - flat art").GetComponent<SpriteRenderer>().sortingOrder, Is.EqualTo(-10000));
    }

    [UnityTest]
    public IEnumerator CaptureVariedObstacleLayout()
    {
        yield return PlacePlayer(-14f, 0f);
        yield return new WaitForSeconds(0.8f);
        int lowerOrder = GameObject.Find("Loading crate base").GetComponentInChildren<SpriteRenderer>().sortingOrder;
        int upperOrder = GameObject.Find("Stacked small crate").GetComponentInChildren<SpriteRenderer>().sortingOrder;
        Assert.That(upperOrder, Is.EqualTo(lowerOrder + 1));
        SaveCameraPreview("FreeMovementCrates");
        yield return PlacePlayer(-1f, -0.5f);
        yield return new WaitForSeconds(0.8f);
        SaveCameraPreview("FreeMovementCourtyard");
        yield return PlacePlayer(11.5f, 2.8f);
        yield return new WaitForSeconds(0.8f);
        SaveCameraPreview("FreeMovementStorage");
    }

    private EnemyVision FirstGuard() => Object.FindObjectsByType<EnemyVision>(FindObjectsSortMode.None).OrderBy(guard => guard.name).First();

    private void SaveCameraPreview(string fileName)
    {
        Camera camera = Camera.main;
        var target = new RenderTexture(1280, 720, 24);
        camera.targetTexture = target;
        camera.Render();
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        image.Apply();
        File.WriteAllBytes("Logs/Stealth/" + fileName + ".png", image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = previous;
        Object.Destroy(image);
        Object.Destroy(target);
    }
}
