using UnityEngine;

namespace LibertyHell.GameLoop
{
    [CreateAssetMenu(menuName = "Liberty Hell/Run Settings", fileName = "RunSettings")]
    public sealed class RunSettings : ScriptableObject
    {
        [SerializeField, Min(0)] private int startingMoney = 100;
        [SerializeField, Min(0)] private int startingBlood = 5;
        [SerializeField, Min(1)] private int maxBlood = 10;
        [SerializeField, Min(1)] private int moneyGoal = 1000;
        [SerializeField, Min(0.01f)] private float durationSeconds = 300f;

        public RunParameters CreateParameters() => new RunParameters(
            startingMoney, startingBlood, maxBlood, moneyGoal, durationSeconds);

        private void OnValidate()
        {
            maxBlood = Mathf.Max(1, maxBlood);
            startingMoney = Mathf.Max(0, startingMoney);
            startingBlood = Mathf.Clamp(startingBlood, 0, maxBlood);
            moneyGoal = Mathf.Max(1, moneyGoal);
            if (float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds)) durationSeconds = 300f;
            durationSeconds = Mathf.Max(0.01f, durationSeconds);
        }
    }
}
