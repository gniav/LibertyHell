using UnityEngine;

namespace LibertyHell.GameLoop
{
    [CreateAssetMenu(menuName = "Liberty Hell/Run Settings", fileName = "RunSettings")]
    public sealed class RunSettings : ScriptableObject
    {
        [SerializeField, Min(0)] private int startingMoney = 100;
        [SerializeField, Min(0)] private int startingBlood = 100;
        [SerializeField, Min(1)] private int maxBlood = 100;
        [SerializeField, Min(1)] private int moneyGoal = 500;
        [SerializeField, Min(0.01f)] private float durationSeconds = 480f;
        [SerializeField, Min(0)] private int bloodRegenAmount = 1;
        [SerializeField, Min(0.01f)] private float bloodRegenIntervalSeconds = 5f;

        public RunParameters CreateParameters() => new RunParameters(
            startingMoney, startingBlood, maxBlood, moneyGoal, durationSeconds, bloodRegenAmount, bloodRegenIntervalSeconds);

        private void OnValidate()
        {
            maxBlood = Mathf.Max(1, maxBlood);
            startingMoney = Mathf.Max(0, startingMoney);
            startingBlood = Mathf.Clamp(startingBlood, 0, maxBlood);
            moneyGoal = Mathf.Max(1, moneyGoal);
            if (float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds)) durationSeconds = 480f;
            durationSeconds = Mathf.Max(0.01f, durationSeconds);
            bloodRegenAmount = Mathf.Max(0, bloodRegenAmount);
            if (float.IsNaN(bloodRegenIntervalSeconds) || float.IsInfinity(bloodRegenIntervalSeconds)) bloodRegenIntervalSeconds = 5f;
            bloodRegenIntervalSeconds = Mathf.Max(0.01f, bloodRegenIntervalSeconds);
        }
    }
}
