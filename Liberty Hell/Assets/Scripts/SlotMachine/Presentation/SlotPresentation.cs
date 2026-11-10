using System;
using UnityEngine;

namespace LibertyHell.SlotMachine
{
    public sealed class SlotPresentation : MonoBehaviour
    {
        [SerializeField] private TextMesh firstReel;
        [SerializeField] private TextMesh secondReel;
        [SerializeField] private TextMesh thirdReel;
        [SerializeField] private Transform firstDrum;
        [SerializeField] private Transform secondDrum;
        [SerializeField] private Transform thirdDrum;
        [SerializeField, Min(0.1f)] private float spinDuration = 1.5f;

        private Transform[] drums;
        private Renderer[] drumRenderers;
        private Quaternion[] initialRotations;
        private readonly float[] angles = new float[3];
        private readonly float[] startAngles = new float[3];
        private readonly float[] targetAngles = new float[3];
        private SlotResult result;
        private float elapsedTime;
        private bool paused;
        private bool spinning;
        private string firstInitial;
        private string secondInitial;
        private string thirdInitial;
        private const string Symbols = "ABXYH";

        // Each outcome has a fixed location on the textured belt. The two
        // repeated icons are recoloured by the reel shader throughout the spin.
        private static readonly float[] ResultAngles = { 0f, 302.7f, 244.6f, 185.6f, 128.3f };

        public bool IsSpinning => spinning;
        public event Action SpinFinished;

        private void Awake()
        {
            if (firstReel != null) firstInitial = firstReel.text;
            if (secondReel != null) secondInitial = secondReel.text;
            if (thirdReel != null) thirdInitial = thirdReel.text;
            // A scene already open in the editor can still hold the old imported
            // drum material after assets change on disk. Resolve the visual setup
            // when entering play mode so the result is never shown with that material.
            if (firstDrum == null || secondDrum == null || thirdDrum == null)
            {
                var model = GameObject.Find("Slot Machine Model");
                var root = model != null ? model.transform.Find("drums") : null;
                if (root != null)
                {
                    firstDrum = root.Find("Cylinder");
                    secondDrum = root.Find("Cylinder.001");
                    thirdDrum = root.Find("Cylinder.002");
                }
            }
            if (firstDrum == null || secondDrum == null || thirdDrum == null) return;
            drums = new[] { firstDrum, secondDrum, thirdDrum };
            drumRenderers = new Renderer[3];
            initialRotations = new Quaternion[3];
            var reelMaterial = Resources.Load<Material>("SlotMachine/ReelSymbols");
            for (int i = 0; i < drums.Length; i++)
            {
                initialRotations[i] = drums[i].localRotation;
                drumRenderers[i] = drums[i].GetComponent<Renderer>();
                if (reelMaterial != null && drumRenderers[i] != null &&
                    (drumRenderers[i].sharedMaterial == null ||
                     drumRenderers[i].sharedMaterial.shader != reelMaterial.shader))
                    drumRenderers[i].sharedMaterial = reelMaterial;
            }
        }

        public void ResetDisplay()
        {
            CancelSpin();
            if (firstReel != null) firstReel.text = firstInitial;
            if (secondReel != null) secondReel.text = secondInitial;
            if (thirdReel != null) thirdReel.text = thirdInitial;
            if (drums == null) return;
            for (int i = 0; i < drums.Length; i++)
            {
                angles[i] = 0f;
                ApplyDrum(i, 0f);
            }
        }

        public bool StartSpin(SlotResult spinResult)
        {
            if (spinning || firstReel == null || secondReel == null || thirdReel == null)
                return false;
            result = spinResult;
            elapsedTime = 0f;
            paused = false;
            spinning = true;
            if (drums != null)
            {
                var values = new[] { result.First, result.Second, result.Third };
                for (int i = 0; i < drums.Length; i++)
                {
                    startAngles[i] = angles[i];
                    float destination = ResultAngles[(int)values[i]];
                    targetAngles[i] = angles[i] + 720f + i * 360f + Mathf.Repeat(destination - angles[i], 360f);
                }
            }
            return true;
        }

        public void SetPaused(bool isPaused) => paused = isPaused;

        public void CancelSpin()
        {
            spinning = false;
            paused = false;
        }

        private void Update()
        {
            if (!spinning || paused) return;
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / spinDuration);
            float easing = 1f - Mathf.Pow(1f - progress, 3f);
            if (drums != null)
                for (int i = 0; i < drums.Length; i++)
                {
                    angles[i] = Mathf.Lerp(startAngles[i], targetAngles[i], easing);
                    ApplyDrum(i, angles[i]);
                }
            if (progress >= 1f)
            {
                firstReel.text = result.First.ToString();
                secondReel.text = result.Second.ToString();
                thirdReel.text = result.Third.ToString();
                if (drums != null)
                {
                    for (int i = 0; i < drums.Length; i++)
                    {
                        angles[i] = Mathf.Repeat(targetAngles[i], 360f);
                        ApplyDrum(i, angles[i]);
                    }
                }
                spinning = false;
                SpinFinished?.Invoke();
                return;
            }
            int frame = (int)(elapsedTime * 12f);
            firstReel.text = Symbols[frame % Symbols.Length].ToString();
            secondReel.text = Symbols[(frame + 1) % Symbols.Length].ToString();
            thirdReel.text = Symbols[(frame + 2) % Symbols.Length].ToString();
        }

        private void ApplyDrum(int index, float angle) =>
            drums[index].localRotation = initialRotations[index] * Quaternion.AngleAxis(angle, Vector3.forward);

        private void OnDisable() => CancelSpin();
    }
}
