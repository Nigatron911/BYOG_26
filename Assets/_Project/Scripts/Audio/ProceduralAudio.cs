using UnityEngine;

namespace Project.Audio
{
    public class ProceduralAudio : MonoBehaviour
    {
        public static ProceduralAudio Instance { get; private set; }

        private AudioSource audioSource;

        private AudioClip jumpClip;
        private AudioClip doubleJumpClip;
        private AudioClip transformClip;
        private AudioClip failClip;
        private AudioClip floorCrackClip;
        private AudioClip floorBreakClip;
        private AudioClip rubberBounceClip;
        private AudioClip checkpointClip;
        private AudioClip victoryClip;
        private AudioClip windHumClip;
        private AudioClip rewriteFireClip;
        private AudioClip rewriteRestoreClip;
        private AudioClip springBounceClip;
        private AudioClip cooldownReadyClip;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            GenerateClips();
        }

        private void GenerateClips()
        {
            jumpClip = CreateToneClip("Jump", 0.12f, 260f, 520f, 0.4f, WaveType.Sine);
            doubleJumpClip = CreateToneClip("DoubleJump", 0.18f, 500f, 1000f, 0.5f, WaveType.Sine);
            transformClip = CreateHarmonicClip("Transform", 0.25f, 330f, 660f, 0.5f);
            failClip = CreateToneClip("Fail", 0.15f, 160f, 120f, 0.35f, WaveType.Sawtooth);
            floorCrackClip = CreateNoiseClip("Crack", 0.18f, 0.6f);
            floorBreakClip = CreateNoiseClip("Break", 0.35f, 0.9f);
            rubberBounceClip = CreateBounceClip("RubberBounce", 0.22f);
            checkpointClip = CreateChimeClip("Checkpoint", 0.35f);
            victoryClip = CreateFanfareClip("Victory", 0.9f);

            // Level 2 Reality Rewrite clips
            rewriteFireClip = CreateToneClip("RewriteFire", 0.28f, 320f, 980f, 0.55f, WaveType.Sine);
            rewriteRestoreClip = CreateToneClip("RewriteRestore", 0.22f, 750f, 280f, 0.45f, WaveType.Sine);
            springBounceClip = CreateToneClip("SpringBounce", 0.26f, 240f, 850f, 0.6f, WaveType.Sine);
            cooldownReadyClip = CreateToneClip("CooldownReady", 0.15f, 880f, 1200f, 0.4f, WaveType.Sine);

            // Level 3 Blank Canvas clips
            paperPickupClip = CreateChimeClip("PaperPickup", 0.25f);
            canvasOpenClip = CreateToneClip("CanvasOpen", 0.2f, 400f, 800f, 0.4f, WaveType.Sine);
            canvasCloseClip = CreateToneClip("CanvasClose", 0.15f, 600f, 300f, 0.35f, WaveType.Sine);
            creationSpawnClip = CreateHarmonicClip("CreationSpawn", 0.3f, 440f, 880f, 0.5f);
            creationWarningClip = CreateToneClip("CreationWarning", 0.18f, 220f, 180f, 0.6f, WaveType.Square);
            creationDissolveClip = CreateNoiseClip("CreationDissolve", 0.25f, 0.5f);
            anvilImpactClip = CreateToneClip("AnvilImpact", 0.3f, 180f, 60f, 0.8f, WaveType.Sawtooth);
            switchActivateClip = CreateChimeClip("SwitchActivate", 0.3f);
        }

        private AudioClip paperPickupClip;
        private AudioClip canvasOpenClip;
        private AudioClip canvasCloseClip;
        private AudioClip creationSpawnClip;
        private AudioClip creationWarningClip;
        private AudioClip creationDissolveClip;
        private AudioClip anvilImpactClip;
        private AudioClip switchActivateClip;

        public void PlayJump() => Play(jumpClip);
        public void PlayDoubleJump() => Play(doubleJumpClip);
        public void PlayTransform() => Play(transformClip);
        public void PlayFail() => Play(failClip);
        public void PlayFloorCrack() => Play(floorCrackClip);
        public void PlayFloorBreak() => Play(floorBreakClip);
        public void PlayRubberBounce() => Play(rubberBounceClip);
        public void PlayCheckpoint() => Play(checkpointClip);
        public void PlayVictory() => Play(victoryClip);
        public void PlayRewriteFire() => Play(rewriteFireClip);
        public void PlayRewriteRestore() => Play(rewriteRestoreClip);
        public void PlaySpringBounce() => Play(springBounceClip);
        public void PlayCooldownReady() => Play(cooldownReadyClip);

        // Level 3 audio methods
        public void PlayPaperPickup() => Play(paperPickupClip);
        public void PlayCanvasOpen() => Play(canvasOpenClip);
        public void PlayCanvasClose() => Play(canvasCloseClip);
        public void PlayCreationSpawn() => Play(creationSpawnClip);
        public void PlayCreationWarning() => Play(creationWarningClip);
        public void PlayCreationDissolve() => Play(creationDissolveClip);
        public void PlayAnvilImpact() => Play(anvilImpactClip);
        public void PlaySwitchActivate() => Play(switchActivateClip);

        private void Play(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private enum WaveType { Sine, Square, Sawtooth }

        private AudioClip CreateToneClip(string name, float duration, float startFreq, float endFreq, float volume, WaveType wave)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[sampleCount];

            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += 2f * Mathf.PI * freq / sampleRate;

                float sample = 0f;
                switch (wave)
                {
                    case WaveType.Sine:
                        sample = Mathf.Sin(phase);
                        break;
                    case WaveType.Square:
                        sample = Mathf.Sin(phase) >= 0 ? 0.5f : -0.5f;
                        break;
                    case WaveType.Sawtooth:
                        sample = (float)(phase / (2 * Mathf.PI) - Mathf.Floor(phase / (2 * Mathf.PI) + 0.5f));
                        break;
                }

                float envelope = (1f - t) * volume;
                samples[i] = sample * envelope;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateBounceClip(string name, float duration)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[sampleCount];

            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = Mathf.Lerp(180f, 480f, Mathf.Sin(t * Mathf.PI * 0.9f));
                phase += 2f * Mathf.PI * freq / sampleRate;
                float env = Mathf.Sin(t * Mathf.PI) * (1f - t * 0.5f) * 0.6f;
                samples[i] = Mathf.Sin(phase) * env;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateNoiseClip(string name, float duration, float volume)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[sampleCount];

            var rand = new System.Random(42);
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float white = (float)(rand.NextDouble() * 2.0 - 1.0);
                float env = Mathf.Pow(1f - t, 2f) * volume;
                samples[i] = white * env;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateHarmonicClip(string name, float duration, float f1, float f2, float volume)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[sampleCount];

            float p1 = 0f, p2 = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                p1 += 2f * Mathf.PI * f1 / sampleRate;
                p2 += 2f * Mathf.PI * f2 / sampleRate;
                float env = (1f - t) * volume;
                samples[i] = (Mathf.Sin(p1) * 0.6f + Mathf.Sin(p2) * 0.4f) * env;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateChimeClip(string name, float duration)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[sampleCount];

            float fA = 523.25f; // C5
            float fE = 659.25f; // E5
            float fG = 783.99f; // G5
            float pA = 0f, pE = 0f, pG = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                pA += 2f * Mathf.PI * fA / sampleRate;
                pE += 2f * Mathf.PI * fE / sampleRate;
                pG += 2f * Mathf.PI * fG / sampleRate;
                float env = Mathf.Pow(1f - t, 1.5f) * 0.5f;
                samples[i] = (Mathf.Sin(pA) * 0.4f + Mathf.Sin(pE) * 0.35f + Mathf.Sin(pG) * 0.35f) * env;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateFanfareClip(string name, float duration)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float chordT = t * 4f; // 4 notes progression
                float freq = 440f;
                if (chordT < 1f) freq = 523.25f; // C5
                else if (chordT < 2f) freq = 659.25f; // E5
                else if (chordT < 3f) freq = 783.99f; // G5
                else freq = 1046.50f; // C6

                float env = (1f - (chordT % 1f)) * 0.5f;
                if (chordT >= 3f) env = (1f - (t - 0.75f) / 0.25f) * 0.6f;

                float sample = Mathf.Sin(2f * Mathf.PI * freq * i / sampleRate);
                samples[i] = sample * env;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
