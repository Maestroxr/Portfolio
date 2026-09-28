using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// An asteroid of one <see cref="AsteroidKind"/>. Large and medium ones break into smaller fragments; ore drops
    /// crystals, magma explodes and sets off everything around it, ice shatters into fast shards and void crystals
    /// release shards that hunt the ship. One prefab per kind serves every size: <see cref="Configure"/> picks the
    /// size, a shape and the numbers that go with them.
    /// </summary>
    public class Asteroid : Shootable
    {
        [SerializeField] internal AsteroidKind kind;
        [Tooltip("Shape variants; one is picked at random for every asteroid.")]
        [SerializeField] internal Mesh[] meshes = new Mesh[0];
        [SerializeField] internal MeshFilter meshFilter;
        [Tooltip("Radius of the models at scale 1, used to fit them to the asteroid's size.")]
        [SerializeField] internal float modelRadius = 1f;

        public AsteroidKind Kind => kind;

        public AsteroidSize Size { get; private set; } = AsteroidSize.Large;

        /// <summary>Speed multiplier of the mission, passed on to the fragments.</summary>
        public float SpeedMultiplier { get; private set; } = 1f;

        /// <summary>Which of the shape variants the asteroid has.</summary>
        public int Shape { get; private set; }


        /// <summary>
        /// Sets the size and everything that comes with it. Call before the asteroid is added to the field. A
        /// <paramref name="shape"/> below zero picks one at random; a puppet gets the shape of the rock it stands for.
        /// </summary>
        public void Configure(AsteroidSize size, float speedMultiplier, int shape = -1)
        {
            Size = size;
            SpeedMultiplier = speedMultiplier;
            radius = AsteroidRules.Radius(kind, size);
            maxHealth = AsteroidRules.Health(kind, size);
            score = AsteroidRules.Score(kind, size);
            contactDamage = AsteroidRules.ContactDamage(kind, size);
            randomSpin = size == AsteroidSize.Large ? 28f : size == AsteroidSize.Medium ? 50f : 90f;
            Mesh[] shapes = Shapes();
            Shape = shape >= 0 ? shape : Random.Range(0, shapes != null && shapes.Length > 0 ? shapes.Length : 1);
            ApplyTheme();
            if (visual != null)
            {
                visual.localScale = Vector3.one * (radius / Mathf.Max(0.01f, modelRadius) * Random.Range(0.94f, 1.08f));
                visual.localRotation = Random.rotation;
            }
        }


        /// <summary>
        /// Shows the shape variant <see cref="Shape"/> of the active theme's look of the kind, and its hit flash colour (the
        /// material follows through the ThemedRenderer of the model). Also called on the rocks in play when the theme changes.
        /// </summary>
        public void ApplyTheme()
        {
            Mesh[] shapes = Shapes();
            if (meshFilter != null && shapes != null && shapes.Length > 0)
            {
                Shape %= shapes.Length;
                meshFilter.sharedMesh = shapes[Shape];
            }
            AsteroidsTheme.AsteroidLook look = AsteroidsThemes.Asteroid(kind);
            if (look != null && look.flash.a > 0f)
            {
                flashColor = look.flash;
            }
        }


        /// <summary>The shape variants of the kind in the active theme; the prefab's own without a theme.</summary>
        private Mesh[] Shapes()
        {
            AsteroidsTheme.AsteroidLook look = AsteroidsThemes.Asteroid(kind);
            return look != null && look.meshes != null && look.meshes.Length > 0 ? look.meshes : meshes;
        }


        /// <summary>A drift in <paramref name="direction"/> at a random speed for the asteroid's size.</summary>
        public void Drift(Vector2 direction)
        {
            Vector2 range = AsteroidRules.SpeedRange(Size);
            Vector2 heading = direction.sqrMagnitude > 0.0001f ? direction.normalized : Random.insideUnitCircle.normalized;
            Velocity = heading * Random.Range(range.x, range.y) * SpeedMultiplier;
        }


        protected override void OnDestroyed(DamageInfo hit)
        {
            SpaceEffects effects = Field != null ? Field.Effects : null;
            if (effects != null)
            {
                effects.AsteroidBurst(Position, radius, kind);
            }
            if (Field != null && Field.Sounds != null)
            {
                Field.Sounds.RockBreak(kind, Size);
            }
            SpawnService spawner = Field != null ? Field.Spawner : null;
            if (spawner == null || IsPuppet)
            {
                // What a puppet breaks into comes from the simulator.
                return;
            }
            int fragments = AsteroidRules.SplitCount(kind, Size, Random.value);
            if (fragments > 0)
            {
                spawner.SpawnFragments(this, fragments, AsteroidRules.SplitSize(kind, Size), hit);
            }
            int crystals = AsteroidRules.CrystalDrops(kind, Size);
            for (int i = 0; i < crystals; i++)
            {
                spawner.SpawnCrystal(Position + Random.insideUnitCircle * radius * 0.6f, Velocity * 0.3f + Random.insideUnitCircle * 1.5f);
            }
            int shards = AsteroidRules.ShardCount(kind, Size);
            if (shards > 0)
            {
                spawner.ReleaseVoidShards(Position, shards, radius);
            }
            if (kind == AsteroidKind.Magma)
            {
                float scale = Size == AsteroidSize.Large ? 1f : Size == AsteroidSize.Medium ? 0.8f : 0.55f;
                Field.Explode(new Blast
                {
                    Center = Position,
                    Radius = spawner.ExplosionRadius * scale,
                    Damage = 3f,
                    PlayerDamage = 28f * scale,
                    Push = 5f,
                    ByPlayer = hit.ByPlayer,
                    Seat = hit.Seat,
                    Source = this,
                    Tint = AsteroidRules.Tint(kind)
                });
            }
        }
    }
}
