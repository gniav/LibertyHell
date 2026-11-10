using System.Collections;
using System.Reflection;
using LibertyHell.GameLoop;
using LibertyHell.Interaction;
using LibertyHell.Items;
using LibertyHell.SceneIntegration;
using LibertyHell.SlotMachine;
using LibertyHell.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class RunSceneIntegrationTests
{
    [Test]
    public void FirstEnabledBuildSceneIsRunFoundation()
    {
        var first = System.Array.Find(EditorBuildSettings.scenes, scene => scene.enabled);
        Assert.IsNotNull(first);
        Assert.AreEqual("Assets/Scenes/RunFoundation.unity", first.path);
        Assert.AreEqual(AssetDatabase.AssetPathToGUID(first.path), first.guid.ToString());
    }

    [UnityTest]
    public IEnumerator SceneConnectsSpinPauseRestartAndShop()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/RunFoundation.unity");
        yield return new EnterPlayMode();
        yield return null;
        var run = Object.FindFirstObjectByType<RunBootstrap>().State;
        var player = Object.FindFirstObjectByType<PlayerInteraction>();
        var slot = Object.FindFirstObjectByType<SlotSceneInteraction>();
        var spin = Object.FindFirstObjectByType<SlotSpinBehaviour>();
        var view = Object.FindFirstObjectByType<SlotPresentation>();
        var shop = Object.FindFirstObjectByType<ShopSceneInteraction>();
        var ui = Object.FindFirstObjectByType<GameUI>();
        Assert.IsNotNull(slot); Assert.IsNotNull(spin); Assert.IsNotNull(view);
        Assert.IsNotNull(shop); Assert.IsNotNull(ui); Assert.IsNotNull(player);
        // Batch mode has no focused game window; simulate focus for input checks.
        typeof(PlayerInteraction).GetMethod("OnApplicationFocus", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(player, new object[] { true });
        Physics.SyncTransforms();
        player.RefreshTarget();
        Assert.AreSame(slot, player.CurrentTarget);
        Assert.IsTrue(player.TryInteract());
        Assert.AreEqual(90, run.Money);
        Assert.IsTrue(run.IsSpinInProgress);
        Assert.IsFalse(shop.IsAvailable);
        Assert.IsTrue(view.IsSpinning);
        var result = spin.PendingResult.Value;
        ui.Pause();
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsFalse(player.InputEnabled);
        float time = run.RemainingSeconds;
        yield return new WaitForSecondsRealtime(.2f);
        Assert.AreEqual(time, run.RemainingSeconds);
        Assert.IsTrue(view.IsSpinning);
        Assert.IsFalse(player.TryInteract());
        ui.Resume();
        Assert.AreEqual(1f, Time.timeScale);
        float deadline = Time.realtimeSinceStartup + 10;
        while (view.IsSpinning && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsFalse(view.IsSpinning);
        Assert.IsFalse(run.IsSpinInProgress);
        Assert.AreEqual(result.First.ToString(), view.transform.Find("Reel 1").GetComponent<TextMesh>().text);
        Assert.AreEqual(result.Second.ToString(), view.transform.Find("Reel 2").GetComponent<TextMesh>().text);
        Assert.AreEqual(result.Third.ToString(), view.transform.Find("Reel 3").GetComponent<TextMesh>().text);
        Assert.IsTrue(player.InputEnabled);
        int balance = run.Money;
        view.enabled = false;
        Assert.IsFalse(slot.IsAvailable);
        Assert.IsFalse(slot.Interact());
        Assert.AreEqual(balance, run.Money);
        view.enabled = true;
        Assert.IsTrue(slot.Interact());
        ui.Pause();
        typeof(GameUI).GetMethod("Restart", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ui, null);
        Assert.AreEqual(1f, Time.timeScale);
        Assert.IsFalse(view.IsSpinning);
        Assert.IsFalse(run.IsSpinInProgress);
        Assert.AreEqual(100, run.Money);
        Assert.AreEqual(100, run.Blood);
        Assert.IsTrue(player.InputEnabled);
        typeof(GameUI).GetMethod("Restart", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ui, null);
        Assert.AreEqual(100, run.Money);
        player.transform.rotation = Quaternion.LookRotation(shop.transform.position - player.transform.position);
        Physics.SyncTransforms();
        player.RefreshTarget();
        Assert.AreSame(shop, player.CurrentTarget);
        Assert.IsTrue(player.TryInteract());
        Assert.IsTrue(ui.IsShopOpen);
        Assert.IsFalse(player.InputEnabled);
        Assert.IsFalse(player.TryInteract());
        Assert.IsTrue(run.Shop.TryPurchase(ItemId.Discount).Success);
        Assert.AreEqual(75, run.Blood);
        ui.CloseShop();
        Assert.IsTrue(player.InputEnabled);
        Assert.IsTrue(run.Inventory.CurrentEffects.HasDiscount);
        player.transform.rotation = Quaternion.Euler(-80, 0, 0);
        player.RefreshTarget();
        Assert.IsNull(player.CurrentTarget);
        Assert.IsFalse(player.TryInteract());
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator OldSceneMaterialIsReplacedBeforeShowingResults()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/RunFoundation.unity");
        var root = GameObject.Find("Slot Machine Model").transform.Find("drums");
        var oldMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/TeamAssets/Materials/textures drums.mat");
        Assert.IsNotNull(oldMaterial);
        foreach (Transform drum in root) drum.GetComponent<Renderer>().sharedMaterial = oldMaterial;
        yield return new EnterPlayMode();
        yield return null;
        var view = Object.FindFirstObjectByType<SlotPresentation>();
        root = GameObject.Find("Slot Machine Model").transform.Find("drums");
        foreach (Transform drum in root)
            Assert.AreEqual("Liberty Hell/Reel Symbols", drum.GetComponent<Renderer>().sharedMaterial.shader.name);
        var initialRotation = root.Find("Cylinder").localRotation;
        typeof(SlotPresentation).GetField("spinDuration", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(view, .2f);
        Assert.IsTrue(view.StartSpin(new SlotResult(SlotSymbol.Y, SlotSymbol.A, SlotSymbol.A, 0, 0)));
        float deadline = Time.realtimeSinceStartup + 5f;
        while (view.IsSpinning && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsFalse(view.IsSpinning);
        var expected = initialRotation * Quaternion.AngleAxis(185.6f, Vector3.forward);
        Assert.Less(Quaternion.Angle(expected.normalized, root.Find("Cylinder").localRotation.normalized), .1f);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator ReelsStopOnResultAndReset()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/RunFoundation.unity");
        yield return new EnterPlayMode();
        var view = Object.FindFirstObjectByType<SlotPresentation>();
        var root = GameObject.Find("Slot Machine Model").transform.Find("drums");
        var drums = new[] { root.Find("Cylinder"), root.Find("Cylinder.001"), root.Find("Cylinder.002") };
        var initial = new Quaternion[3];
        for (int i = 0; i < 3; i++)
        {
            initial[i] = drums[i].localRotation;
            Assert.IsNotNull(drums[i].GetComponent<Renderer>().sharedMaterial.mainTexture);
            Assert.IsNull(view.transform.Find("Reel Image " + (i + 1)));
        }
        Assert.IsTrue(view.StartSpin(new SlotResult(SlotSymbol.A, SlotSymbol.B, SlotSymbol.X, 0, 0)));
        yield return new WaitForSeconds(.15f);
        Assert.AreNotEqual(initial[0], drums[0].localRotation);
        var ui = Object.FindFirstObjectByType<GameUI>();
        ui.Pause();
        var pausedRotation = drums[0].localRotation;
        yield return new WaitForSecondsRealtime(.1f);
        Assert.AreEqual(pausedRotation, drums[0].localRotation);
        ui.Resume();
        view.CancelSpin();
        view.ResetDisplay();
        Assert.AreEqual(initial[0], drums[0].localRotation);
        var floorMaterial = GameObject.Find("Floor").GetComponent<Renderer>().sharedMaterial;
        Assert.IsNotNull(floorMaterial.mainTexture);
        Assert.AreEqual(new Vector2(8, 8), floorMaterial.GetTextureScale("_BaseMap"));
        Assert.IsNotNull(GameObject.Find("Back Wall").GetComponent<Renderer>().sharedMaterial.mainTexture);
        typeof(SlotPresentation).GetField("spinDuration", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(view, .2f);
        var resultAngles = new[] { 0f, 302.7f, 244.6f, 185.6f, 128.3f };
        for (int i = 0; i < 5; i++)
        {
            var symbol = (SlotSymbol)i;
            Assert.IsTrue(view.StartSpin(new SlotResult(symbol, symbol, symbol, 0, 0)));
            float deadline = Time.realtimeSinceStartup + 5f;
            while (view.IsSpinning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsFalse(view.IsSpinning);
            for (int reel = 0; reel < 3; reel++)
            {
                var expected = initial[reel] * Quaternion.AngleAxis(resultAngles[i], Vector3.forward);
                Assert.Less(Quaternion.Angle(expected.normalized, drums[reel].localRotation.normalized), .1f);
            }
        }
        view.ResetDisplay();
        Assert.AreEqual(initial[0], drums[0].localRotation);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator FailedPresentationStillCompletesThePaidSpin()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/RunFoundation.unity");
        yield return new EnterPlayMode();
        yield return null;
        var run = Object.FindFirstObjectByType<RunBootstrap>().State;
        var slot = Object.FindFirstObjectByType<SlotSceneInteraction>();
        var view = Object.FindFirstObjectByType<SlotPresentation>();
        typeof(SlotPresentation).GetField("firstReel", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(view, null);
        LogAssert.Expect(LogType.Warning, "Slot animation could not start; applying the paid result immediately.");
        Assert.IsTrue(slot.Interact());
        Assert.IsFalse(run.IsSpinInProgress);
        Assert.IsFalse(view.IsSpinning);
        yield return new ExitPlayMode();
    }
}
