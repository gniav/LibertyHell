using System.Collections;
using LibertyHell.SlotMachine;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class SlotPresentationTests
{
    private static int completed;

    /// <summary>
    /// Запоминает завершение анимации.
    /// </summary>
    private static void OnFinished()
    {
        completed++;
    }

    /// <summary>
    /// Проверяет паузу, отмену и остановку барабанов на переданном результате.
    /// </summary>
    /// <returns></returns>
    [UnityTest]
    public IEnumerator ShowsResultAfterPauseAndCancellation()
    {
        EditorSceneManager.OpenScene("Assets/Tests/SlotPresentation/SlotPresentationDemo.unity");
        yield return new EnterPlayMode();

        var view = Object.FindFirstObjectByType<SlotPresentation>();
        Assert.IsNotNull(view);
        completed = 0;
        view.SpinFinished += OnFinished;
        var result = new SlotResult(SlotSymbol.Y, SlotSymbol.X, SlotSymbol.H, 0, 0);

        Assert.IsTrue(view.StartSpin(result));
        Assert.IsFalse(view.StartSpin(result));
        view.SetPaused(true);
        yield return new WaitForSecondsRealtime(1.6f);
        Assert.IsTrue(view.IsSpinning);
        view.CancelSpin();
        Assert.AreEqual(0, completed);

        Assert.IsTrue(view.StartSpin(result));
        float deadline = Time.realtimeSinceStartup + 10f;
        while (view.IsSpinning && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.IsFalse(view.IsSpinning);
        Assert.AreEqual(1, completed);
        Assert.AreEqual("Y", view.transform.Find("Reel 1").GetComponent<TextMesh>().text);
        Assert.AreEqual("X", view.transform.Find("Reel 2").GetComponent<TextMesh>().text);
        Assert.AreEqual("H", view.transform.Find("Reel 3").GetComponent<TextMesh>().text);
        yield return new ExitPlayMode();
    }
}
