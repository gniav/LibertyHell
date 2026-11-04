using System.Collections;
using System.Reflection;
using LibertyHell.GameLoop;
using LibertyHell.SlotMachine;
using LibertyHell.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class RestartDisplayTests
{
    /// <summary>
    /// Проверяет сброс символов и отмену вращения при перезапуске забега.
    /// </summary>
    /// <returns></returns>
    [UnityTest]
    public IEnumerator RestartRestoresReelsAndCancelsSpin()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/RunFoundation.unity");
        yield return new EnterPlayMode();
        yield return null;

        var presentation = Object.FindFirstObjectByType<SlotPresentation>();
        var ui = Object.FindFirstObjectByType<GameUI>();
        var run = Object.FindFirstObjectByType<RunBootstrap>().State;
        var first = presentation.transform.Find("Reel 1").GetComponent<TextMesh>();
        var second = presentation.transform.Find("Reel 2").GetComponent<TextMesh>();
        var third = presentation.transform.Find("Reel 3").GetComponent<TextMesh>();
        string initialFirst = first.text;
        string initialSecond = second.text;
        string initialThird = third.text;

        first.text = "H";
        second.text = "Y";
        third.text = "X";
        Assert.IsTrue(presentation.StartSpin(new SlotResult(SlotSymbol.X, SlotSymbol.X, SlotSymbol.X, 100, 0)));
        typeof(GameUI).GetMethod("Restart", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ui, null);
        Assert.IsFalse(presentation.IsSpinning);
        Assert.AreEqual(initialFirst, first.text);
        Assert.AreEqual(initialSecond, second.text);
        Assert.AreEqual(initialThird, third.text);
        Assert.AreEqual(100, run.Money);
        yield return new WaitForSeconds(2f);
        Assert.AreEqual(100, run.Money);
        Assert.AreEqual(initialFirst, first.text);
        yield return new ExitPlayMode();
    }
}
