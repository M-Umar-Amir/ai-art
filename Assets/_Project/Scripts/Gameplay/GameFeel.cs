using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Run feedback in one place: burger pickup bursts, stumble dust and shake, speed lines,
    /// and the synthesised sound effects. Listens to the track and rider events.
    public class GameFeel : MonoBehaviour
    {
        public FollowCamera cam;
        public RiderController rider;
        /// URP Particles/Unlit material, referenced from the scene so the shader survives build stripping.
        public Material particleMaterial;

        ParticleSystem burst, dust, speedLines;
        AudioSource sfx, siren;
        AudioClip coin, jump, drop, thud, whoosh;
        int combo;
        float comboTimer;

        void Start()
        {
            burst = Particles("PickupBurst", 0.35f, 0.12f, new Color(1f, 0.85f, 0.3f), 9f, -3f);
            dust = Particles("Dust", 0.6f, 0.35f, new Color(0.85f, 0.8f, 0.72f), 4f, 0.5f);
            speedLines = SpeedLines();

            sfx = gameObject.AddComponent<AudioSource>();
            siren = gameObject.AddComponent<AudioSource>();
            siren.loop = true;
            siren.volume = 0f;
            siren.clip = Synth.Siren();
            siren.Play();
            // Background music under the menus and the run (an original synthesised loop).
            music = gameObject.AddComponent<AudioSource>();
            music.clip = Synth.Music();
            music.loop = true;
            music.volume = 0.32f;
            music.Play();
            coin = Synth.Coin();
            jump = Synth.Sweep(320f, 760f, 0.16f, 0.35f);
            drop = Synth.Sweep(500f, 180f, 0.14f, 0.3f);
            whoosh = Synth.Noise(0.12f, 0.18f);
            thud = Synth.Thud();

            TrackSpawner.Collected += OnCollected;
            TrackSpawner.Struck += OnStruck;
            TrackSpawner.NearMiss += OnNearMiss;
            TrackSpawner.Smashed += OnSmashed;
            PowerUps.Activated += OnPowerUp;
            powerUp = Synth.Sweep(400f, 1200f, 0.35f, 0.4f);
            rider.Jumped += () => { sfx.PlayOneShot(jump); cam.Kick(0.6f); };
            rider.Dropped += () => sfx.PlayOneShot(drop);
            rider.Landed += () => {
                float weight = Mathf.InverseLerp(4f, 16f, rider.LandingImpact);
                dust.Emit(Params(rider.transform.position), 4 + Mathf.RoundToInt(weight * 8f));
                sfx.PlayOneShot(thud, .12f + weight * .25f);
                cam.Kick(weight * .35f);
            };
            rider.LaneChanged += () => sfx.PlayOneShot(whoosh, 0.6f);
            GameManager.Instance.ComboMilestone += (c, pts) =>
            {
                sfx.pitch = 1.15f;
                sfx.PlayOneShot(powerUp, 0.5f);
                burst.Emit(Params(rider.transform.position + Vector3.up * 1.2f), 24);
                Haptics.Play(Haptics.Strength.Medium);
            };
            GameManager.Instance.StateChanged += s => { if (s == RunState.Busted) { sfx.PlayOneShot(thud); cam.Shake(0.5f); } };
        }

        void OnDestroy()
        {
            TrackSpawner.Collected -= OnCollected;
            TrackSpawner.Struck -= OnStruck;
            TrackSpawner.NearMiss -= OnNearMiss;
            TrackSpawner.Smashed -= OnSmashed;
            PowerUps.Activated -= OnPowerUp;
        }

        AudioClip powerUp;
        AudioSource music;

        void OnPowerUp(PowerUp p)
        {
            sfx.pitch = 1f;
            sfx.PlayOneShot(powerUp, 0.8f);
            Haptics.Play(Haptics.Strength.Medium);
            var main = burst.main;
            var was = main.startColor;
            main.startColor = new ParticleSystem.MinMaxGradient(PowerUps.Colors[(int)p], Color.white);
            burst.Emit(Params(rider.transform.position + Vector3.up), 40);
            main.startColor = was;
            cam.Kick(0.8f);
        }

        /// Sugar Rush ploughing through an obstacle: a pop of dust and a light knock, no stumble.
        void OnSmashed(TrackObject o)
        {
            sfx.pitch = 1.3f;
            sfx.PlayOneShot(thud, 0.7f);
            dust.Emit(Params(o.transform.position + Vector3.up * 0.5f), 24);
            cam.Shake(0.15f);
        }

        void OnNearMiss(TrackObject o)
        {
            sfx.PlayOneShot(whoosh, 1f);
            Haptics.Play(Haptics.Strength.Light);
            cam.Kick(1.2f); // Slightly stronger camera kick for better feedback
            StartCoroutine(SlowMo());
        }

        System.Collections.IEnumerator HitStop()
        {
            if (GameManager.Instance.Paused) yield break;
            // A brief slow-down, not a freeze: a near-zero time scale read as the game lagging.
            Time.timeScale = 0.25f;
            for (float t = 0f; t < 0.06f; t += Time.unscaledDeltaTime) yield return null;
            if (!GameManager.Instance.Paused) Time.timeScale = 1f;
        }

        System.Collections.IEnumerator SlowMo()
        {
            if (GameManager.Instance.Paused) yield break;
            Time.timeScale = 0.55f;
            // Unscaled game time rather than the wall clock, so a frame-locked showcase recording keeps the beat.
            for (float t = 0f; t < 0.22f; t += Time.unscaledDeltaTime) yield return null;
            if (!GameManager.Instance.Paused) Time.timeScale = 1f;
        }

        void OnCollected(TrackObject o)
        {
            combo = comboTimer > 0f ? combo + 1 : 0;
            comboTimer = 0.6f;
            sfx.pitch = 1f + Mathf.Min(combo, 8) * 0.06f;
            sfx.PlayOneShot(coin, 0.75f); // Slightly louder for better feedback
            Haptics.Play(Haptics.Strength.Light);
            burst.Emit(Params(o.transform.position), 16); // More particles for better visual feedback
        }

        void OnStruck(TrackObject o)
        {
            sfx.pitch = 1f;
            sfx.PlayOneShot(thud);
            dust.Emit(Params(o.transform.position + Vector3.up * 0.5f), 22); // More dust for better impact feedback
            cam.Shake(0.55f); // Stronger shake for better feedback
            Haptics.Play(Haptics.Strength.Medium);
            if (GameManager.Instance.State == RunState.Running) StartCoroutine(HitStop());
        }

        void Update()
        {
            var gm = GameManager.Instance;
            comboTimer -= Time.deltaTime;
            if (comboTimer < 0f) sfx.pitch = 1f;

            bool running = gm.State == RunState.Running;
            float speed01 = Mathf.InverseLerp(Tuning.SpeedBase, Tuning.SpeedMax, gm.Speed);
            var em = speedLines.emission;
            em.rateOverTime = running && !PlayerProfile.ReduceMotion ? Mathf.Lerp(8f, 60f, speed01) * (PowerUps.Active(PowerUp.Rush) ? 3f : 1f) : 0f;
            speedLines.transform.position = cam.transform.position + cam.transform.forward * 14f;
            speedLines.transform.rotation = cam.transform.rotation;

            // Siren swells while the officer is close behind.
            var chase = FindChase();
            float want = running && chase && chase.Close ? 0.35f * PlayerProfile.SfxVolume : 0f;
            sfx.volume = PlayerProfile.SfxVolume;
            siren.volume = Mathf.MoveTowards(siren.volume, want, Time.deltaTime * 0.8f);
            // Music ducks under the siren and drops away on the busted screen; slightly faster under Sugar Rush.
            if (music)
            {
                float mv = (gm.State == RunState.Busted ? 0.12f : running && chase && chase.Close ? 0.2f : 0.32f) * PlayerProfile.MusicVolume;
                music.volume = Mathf.MoveTowards(music.volume, mv, Time.unscaledDeltaTime * 0.6f);
                float districtPitch = DashRun.District == 1 ? 0.88f : DashRun.District == 2 ? 1.13f : 1f;
                music.pitch = Mathf.MoveTowards(music.pitch, (running && PowerUps.Active(PowerUp.Rush) ? 1.08f : 1f) * districtPitch, Time.unscaledDeltaTime * 0.5f);
            }
        }

        ChaseController chaseCache;
        ChaseController FindChase() => chaseCache ? chaseCache : chaseCache = FindFirstObjectByType<ChaseController>();

        static ParticleSystem.EmitParams Params(Vector3 pos) => new() { position = pos, applyShapeToPosition = true };

        ParticleSystem Particles(string name, float life, float size, Color c, float speed, float gravity)
        {
            var ps = NewSystem(name);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startColor = new ParticleSystem.MinMaxGradient(c, Color.white);
            main.gravityModifier = -gravity / 9.81f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var em = ps.emission;
            em.rateOverTime = 0f;
            return ps;
        }

        ParticleSystem SpeedLines()
        {
            var ps = NewSystem("SpeedLines");
            var main = ps.main;
            main.startLifetime = 0.35f;
            main.startSpeed = -40f;
            main.startSize = 0.05f;
            main.startColor = new Color(1f, 1f, 1f, 0.55f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 7f;
            shape.radiusThickness = 0.35f;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 6f;
            r.velocityScale = 0.08f;
            return ps;
        }

        ParticleSystem NewSystem(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = particleMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }
    }
}
