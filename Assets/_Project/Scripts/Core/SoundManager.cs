using UnityEngine;
using Tilcayo.Core;

namespace Tilcayo.Core
{
    /// <summary>
    /// Gestor de sonido integral.
    /// Soporta clips asignados o sintetiza efectos de sonido procedimentales retro
    /// para asegurar audio funcional inmediato sin depender de archivos de audio externos.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Fuentes de Audio")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;

        public bool IsMuted { get; private set; } = false;

        [Header("Clips Opcionales (Si se dejan vacíos, se sintetizan proceduralmente)")]
        [SerializeField] private AudioClip jumpClip;
        [SerializeField] private AudioClip slimeDropClip;
        [SerializeField] private AudioClip teleportClip;
        [SerializeField] private AudioClip hitClip;
        [SerializeField] private AudioClip checkpointClip;
        [SerializeField] private AudioClip diskPickupClip;
        [SerializeField] private AudioClip bossHitClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.volume = 0.5f;
            }

            GenerateProceduralClipsIfNeeded();
        }

        private void OnEnable()
        {
            GameEvents.OnTeleportUsed += PlayTeleportSound;
            GameEvents.OnPlayerDamaged += PlayHitSound;
            GameEvents.OnCheckpointReached += PlayCheckpointSound;
            GameEvents.OnDiskCollected += PlayDiskSound;
            GameEvents.OnSlimeSpawned += PlaySlimeDropSound;
            GameEvents.OnBossDamaged += PlayBossHitSound;
        }

        private void OnDisable()
        {
            GameEvents.OnTeleportUsed -= PlayTeleportSound;
            GameEvents.OnPlayerDamaged -= PlayHitSound;
            GameEvents.OnCheckpointReached -= PlayCheckpointSound;
            GameEvents.OnDiskCollected -= PlayDiskSound;
            GameEvents.OnSlimeSpawned -= PlaySlimeDropSound;
            GameEvents.OnBossDamaged -= PlayBossHitSound;
        }

        private void GenerateProceduralClipsIfNeeded()
        {
            if (jumpClip == null) jumpClip = CreateProceduralTone(300f, 600f, 0.12f, ToneType.Sine);
            if (slimeDropClip == null) slimeDropClip = CreateProceduralTone(450f, 200f, 0.15f, ToneType.Triangle);
            if (teleportClip == null) teleportClip = CreateProceduralTone(250f, 950f, 0.2f, ToneType.Square);
            if (hitClip == null) hitClip = CreateProceduralTone(180f, 60f, 0.22f, ToneType.Noise);
            if (checkpointClip == null) checkpointClip = CreateProceduralTone(520f, 780f, 0.25f, ToneType.Sine);
            if (diskPickupClip == null) diskPickupClip = CreateProceduralTone(700f, 1050f, 0.12f, ToneType.Sine);
            if (bossHitClip == null) bossHitClip = CreateProceduralTone(120f, 50f, 0.3f, ToneType.Square);
        }

        public void PlayJump() => PlayClip(jumpClip, 0.7f);
        public void PlaySlimeDrop() => PlayClip(slimeDropClip, 0.8f);
        public void PlayTeleport() => PlayClip(teleportClip, 0.9f);
        public void PlayHit() => PlayClip(hitClip, 1.0f);
        public void PlayCheckpoint() => PlayClip(checkpointClip, 0.85f);
        public void PlayDiskPickup() => PlayClip(diskPickupClip, 0.75f);

        private void PlayTeleportSound(Vector3 from, Vector3 to) => PlayTeleport();
        private void PlayHitSound(int cur, int max) => PlayHit();
        private void PlayCheckpointSound(Vector3 pos) => PlayCheckpoint();
        private void PlayDiskSound(int count) => PlayDiskPickup();
        private void PlaySlimeDropSound() => PlaySlimeDrop();
        private void PlayBossHitSound(int cur, int max) => PlayClip(bossHitClip, 0.9f);

        public void ToggleMute()
        {
            SetMute(!IsMuted);
        }

        public void SetMute(bool mute)
        {
            IsMuted = mute;
            AudioListener.volume = IsMuted ? 0f : 1f;
            AudioListener.pause = IsMuted;
            if (sfxSource != null) sfxSource.mute = IsMuted;
            if (musicSource != null) musicSource.mute = IsMuted;
        }

        private void PlayClip(AudioClip clip, float volume = 1f)
        {
            if (IsMuted) return;
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }

        private enum ToneType { Sine, Square, Triangle, Noise }

        private AudioClip CreateProceduralTone(float startFreq, float endFreq, float duration, ToneType type)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                float phase = (float)i / sampleRate * freq * Mathf.PI * 2f;
                float envelope = 1f - t; // fade out lineal

                float sample = 0f;
                switch (type)
                {
                    case ToneType.Sine:
                        sample = Mathf.Sin(phase);
                        break;
                    case ToneType.Square:
                        sample = Mathf.Sin(phase) >= 0f ? 0.6f : -0.6f;
                        break;
                    case ToneType.Triangle:
                        sample = Mathf.PingPong(phase / Mathf.PI, 2f) - 1f;
                        break;
                    case ToneType.Noise:
                        sample = (Random.value * 2f - 1f) * 0.7f;
                        break;
                }

                data[i] = sample * envelope;
            }

            AudioClip clip = AudioClip.Create("SynthSFX_" + type, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
