using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The effects of a race, pooled from the content's prefabs and coloured by the theme: explosions, sparks, smoke,
    /// muzzle flashes, tracers, scorch marks, dust, the screen shake of a blast. One per race, on the race's root
    /// (<see cref="Race.Effects"/>); the pools die with the race. Until the builders made a prefab, a stand-in drawn in
    /// code (<see cref="EffectFallbacks"/>) plays instead, so nothing is ever missing. A few uses per frame at most of
    /// each kind, so a pile-up of blasts cannot flood the frame.
    /// </summary>
    public class RaceEffects : MonoBehaviour
    {
        /// <summary>The height marks on the ground are drawn at (just above the road).</summary>
        public const float GroundMarkHeight = 0.05f;

        /// <summary>Metres from the middle of a view at which a blast no longer shakes it.</summary>
        public const float ShakeReach = 55f;

        private const int MaxScorches = 40;
        private const int MaxTracers = 64;
        private const float ScorchSeconds = 22f;
        private const float ScorchFade = 4f;
        private const int BudgetPerFrame = 40;

        private static readonly int EffectKinds = Enum.GetValues(typeof(EffectKind)).Length;

        private readonly List<TracerLine> tracers = new List<TracerLine>();
        private readonly List<ScorchMark> scorches = new List<ScorchMark>();
        private EffectPool[] pools;
        private Transform templates;
        private Transform home;
        private DeathRaceContent content;
        private DeathRaceTheme theme;
        private Material tracerMaterial;
        private Material skidMaterial;
        private int frame;
        private int playedThisFrame;
        private float scorchLift;
        private Track track;
        private bool trackLooked;
        private RaceCamera[] views = new RaceCamera[0];
        private float viewsFound = float.NegativeInfinity;

        public DeathRaceTheme Theme => theme;

        /// <summary>The content the effects come from (the mines and rockets take their models from it too).</summary>
        public DeathRaceContent Content => content;

        /// <summary>Something was played: its kind ("explosion", "big explosion", "sparks", "muzzle", "scorch") and where (development checks).</summary>
        public event Action<string, Vector3> Played;

        public Color TracerColor => theme != null ? theme.tracer : new Color(1f, 0.85f, 0.4f, 1f);
        public Color MuzzleColor => theme != null ? theme.muzzle : new Color(1f, 0.75f, 0.3f, 1f);
        public Color ExplosionColor => theme != null ? theme.explosion : new Color(1f, 0.55f, 0.15f, 1f);
        public Color SparkColor => theme != null ? theme.sparks : new Color(1f, 0.8f, 0.45f, 1f);

        /// <summary>Whether the race is at night (flashes light more).</summary>
        public bool Night => theme != null && theme.night;

        /// <summary>The material of tracer lines (the content's, or a glowing streak).</summary>
        public Material TracerMaterial => tracerMaterial != null ? tracerMaterial : (tracerMaterial = content != null && content.tracer != null ? content.tracer : CombatArt.Tracer);

        /// <summary>The material of skid marks (the content's, or dark rubber).</summary>
        public Material SkidMaterial => skidMaterial != null ? skidMaterial : (skidMaterial = content != null && content.skid != null ? content.skid : CombatArt.Skid);

        public virtual void Setup(DeathRaceContent content, DeathRaceTheme theme)
        {
            bool sameContent = this.content == content && pools != null;
            this.content = content;
            this.theme = theme;
            tracerMaterial = null;
            skidMaterial = null;
            if (home == null)
            {
                home = new GameObject("Effects").transform;
                home.SetParent(transform, false);
                templates = new GameObject("Templates").transform;
                templates.SetParent(home, false);
                templates.gameObject.SetActive(false);
            }
            if (!sameContent)
            {
                pools = new EffectPool[EffectKinds];
            }
            foreach (TracerLine tracer in tracers)
            {
                if (tracer != null)
                {
                    tracer.GetComponent<LineRenderer>().sharedMaterial = TracerMaterial;
                }
            }
        }

        /// <summary>An explosion of <paramref name="size"/> (1 = a mine or a rocket; 2 and more = a car blowing up).</summary>
        public virtual void Explosion(Vector3 position, float size = 1f)
        {
            bool big = size >= 1.5f;
            RaceEffect effect = Play(big ? EffectKind.BigExplosion : EffectKind.Explosion, position, Quaternion.identity, big ? size / 1.6f : size, ExplosionColor, Night ? 1.8f : 1f, true);
            if (effect != null)
            {
                Played?.Invoke(big ? "big explosion" : "explosion", position);
            }
        }

        /// <summary>Sparks flying off along <paramref name="direction"/> (a hit, a crash, a scrape); <paramref name="amount"/> 0 to 2.</summary>
        public virtual void Sparks(Vector3 position, Vector3 direction, float amount = 1f)
        {
            Vector3 flat = new Vector3(direction.x, 0.25f, direction.z);
            Quaternion facing = flat.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(flat.normalized, Vector3.up) : Quaternion.LookRotation(Vector3.up);
            if (Play(EffectKind.Sparks, position, facing, Mathf.Lerp(0.6f, 1.3f, Mathf.Clamp01(amount * 0.5f)), SparkColor, 0f) != null)
            {
                Played?.Invoke("sparks", position);
            }
        }

        /// <summary>A flash at the muzzle of a gun, riding with it while it lasts.</summary>
        public virtual void Muzzle(Transform muzzle)
        {
            if (muzzle == null)
            {
                return;
            }
            RaceEffect effect = Play(EffectKind.Muzzle, muzzle.position, muzzle.rotation, 1f, MuzzleColor, 0f);
            if (effect != null)
            {
                effect.Follow(muzzle, Vector3.zero, Quaternion.identity);
            }
        }

        /// <summary>The streak of a round from <paramref name="from"/> to where it stopped.</summary>
        public virtual void Tracer(Vector3 from, Vector3 to)
        {
            TracerLine line = FreeTracer();
            if (line != null)
            {
                line.Play(from, to, TracerColor, 0.09f, 0.34f);
            }
        }

        /// <summary>A burnt patch on the ground (a blast was here); <paramref name="size"/> 1 = a mine's.</summary>
        public virtual void Scorch(Vector3 position, float size = 1f)
        {
            ScorchMark mark = FreeScorch();
            if (mark == null)
            {
                return;
            }
            // Each mark a hair above the last, so overlapping ones never flicker.
            scorchLift = (scorchLift + 0.0007f) % 0.02f;
            float yaw = (position.x * 37.1f + position.z * 17.3f) % 360f;
            mark.Place(new Vector3(position.x, GroundMarkHeight + scorchLift, position.z), 5.5f * Mathf.Max(0.2f, size), yaw, new Color(1f, 1f, 1f, 0.9f), ScorchSeconds, ScorchFade);
            Played?.Invoke("scorch", position);
        }

        /// <summary>A puff of dust (or snow, or tyre smoke on asphalt: the colour of the land) at a wheel.</summary>
        public virtual void Dust(Vector3 position, float amount = 1f)
        {
            Play(EffectKind.Dust, new Vector3(position.x, Mathf.Max(position.y, 0.2f), position.z), Quaternion.identity, Mathf.Lerp(0.6f, 1.3f, Mathf.Clamp01(amount * 0.5f)), DustColor, 0f);
        }

        /// <summary>
        /// Shakes the views of the people near <paramref name="position"/> (their cameras' CameraShake; the settings' screen
        /// shake switches every one of them through <see cref="Gamebox.CameraShake.Enabled"/>).
        /// </summary>
        public virtual void Shake(Vector3 position, float trauma)
        {
            if (trauma > 0f)
            {
                ShakeViews(position, trauma);
            }
        }

        /// <summary>
        /// Starts a loop (smoke, black smoke, fire, a nitro flame) riding on <paramref name="parent"/>; it burns until its
        /// <see cref="RaceEffect.Stop"/>. Null when the pool is full.
        /// </summary>
        public RaceEffect Loop(EffectKind kind, Transform parent, Vector3 localPosition, Quaternion localRotation, float size = 1f)
        {
            if (parent == null)
            {
                return null;
            }
            EffectPool pool = PoolOf(kind);
            RaceEffect effect = pool?.Take();
            if (effect == null)
            {
                return null;
            }
            effect.Follow(parent, localPosition, localRotation);
            effect.Play(effect.transform.position, effect.transform.rotation, size, ExplosionColor, 0f);
            return effect;
        }

        /// <summary>The colour of the dust of this race's land.</summary>
        private Color DustColor
        {
            get
            {
                if (!trackLooked)
                {
                    trackLooked = true;
                    track = GetComponentInChildren<Track>();
                    if (track == null && transform.parent != null)
                    {
                        track = transform.parent.GetComponentInChildren<Track>();
                    }
                }
                Biome biome = track != null && track.Layout != null ? track.Layout.Biome : Biome.Desert;
                BiomeLook look = theme != null ? theme.Biome(biome) : null;
                Color color;
                switch (biome)
                {
                    case Biome.Snow:
                        color = new Color(0.95f, 0.97f, 1f, 1f);
                        break;
                    case Biome.Desert:
                        color = look != null ? Color.Lerp(look.fogColor, new Color(0.78f, 0.66f, 0.5f), 0.5f) : new Color(0.78f, 0.66f, 0.5f);
                        break;
                    default:
                        color = new Color(0.62f, 0.62f, 0.62f, 1f);
                        break;
                }
                color.a = 1f;
                return Night ? color * 0.55f : color;
            }
        }

        private RaceEffect Play(EffectKind kind, Vector3 position, Quaternion rotation, float size, Color tint, float lightScale, bool important = false)
        {
            if (frame != Time.frameCount)
            {
                frame = Time.frameCount;
                playedThisFrame = 0;
            }
            if (!important && playedThisFrame >= BudgetPerFrame)
            {
                return null;
            }
            EffectPool pool = PoolOf(kind);
            RaceEffect effect = pool?.Take();
            if (effect == null)
            {
                return null;
            }
            playedThisFrame++;
            effect.Play(position, rotation, size, tint, lightScale);
            return effect;
        }

        private EffectPool PoolOf(EffectKind kind)
        {
            if (home == null)
            {
                Setup(content, theme);
            }
            int index = (int)kind;
            if (pools[index] == null)
            {
                GameObject prefab = PrefabOf(kind);
                if (prefab == null)
                {
                    prefab = EffectFallbacks.Build(kind, templates);
                }
                pools[index] = new EffectPool(prefab, home, CapOf(kind));
            }
            return pools[index];
        }

        private GameObject PrefabOf(EffectKind kind)
        {
            if (content == null)
            {
                return null;
            }
            switch (kind)
            {
                case EffectKind.Explosion: return content.explosion;
                case EffectKind.BigExplosion: return content.bigExplosion != null ? content.bigExplosion : content.explosion;
                case EffectKind.Sparks: return content.sparks;
                case EffectKind.Smoke: return content.smoke;
                case EffectKind.BlackSmoke: return content.blackSmoke;
                case EffectKind.Muzzle: return content.muzzleFlash;
                case EffectKind.Fire: return content.fire;
                case EffectKind.NitroFlame: return content.nitroFlame;
                default: return content.dust;
            }
        }

        private static int CapOf(EffectKind kind)
        {
            switch (kind)
            {
                case EffectKind.Sparks: return 48;
                case EffectKind.Muzzle: return 16;
                case EffectKind.Dust: return 40;
                case EffectKind.Explosion: return 12;
                case EffectKind.BigExplosion: return 6;
                default: return 16;
            }
        }

        private TracerLine FreeTracer()
        {
            if (home == null)
            {
                Setup(content, theme);
            }
            foreach (TracerLine line in tracers)
            {
                if (line != null && !line.Busy)
                {
                    return line;
                }
            }
            if (tracers.Count >= MaxTracers)
            {
                return null;
            }
            TracerLine made = TracerLine.Create(home, TracerMaterial);
            tracers.Add(made);
            return made;
        }

        private ScorchMark FreeScorch()
        {
            if (home == null)
            {
                Setup(content, theme);
            }
            ScorchMark oldest = null;
            foreach (ScorchMark mark in scorches)
            {
                if (mark == null)
                {
                    continue;
                }
                if (!mark.Busy)
                {
                    return mark;
                }
                if (oldest == null || mark.Placed < oldest.Placed)
                {
                    oldest = mark;
                }
            }
            if (scorches.Count >= MaxScorches)
            {
                return oldest;
            }
            ScorchMark made = ScorchMark.Create(home, content != null ? content.scorch : null, CombatArt.Scorch);
            scorches.Add(made);
            return made;
        }

        /// <summary>Adds the trauma of a blast to the views of this race, by how far it is from the middle of each.</summary>
        private void ShakeViews(Vector3 position, float trauma)
        {
            foreach (RaceCamera view in Views())
            {
                if (view == null || view.Shake == null)
                {
                    continue;
                }
                Vector3 focus = view.Focus;
                float distance = new Vector2(position.x - focus.x, position.z - focus.z).magnitude;
                view.Shake.AddTrauma(Mathf.Clamp01(trauma), distance, ShakeReach);
            }
        }

        /// <summary>The cameras looking at this race (looked up again now and then: split screen adds and drops some).</summary>
        internal RaceCamera[] Views()
        {
            if (Time.unscaledTime - viewsFound > 1f)
            {
                viewsFound = Time.unscaledTime;
                RaceCamera[] all = FindObjectsByType<RaceCamera>(FindObjectsSortMode.None);
                var mine = new List<RaceCamera>(all.Length);
                foreach (RaceCamera view in all)
                {
                    if (view != null && view.isActiveAndEnabled && view.Race != null && view.Race.Effects == this)
                    {
                        mine.Add(view);
                    }
                }
                views = mine.ToArray();
            }
            return views;
        }
    }
}
