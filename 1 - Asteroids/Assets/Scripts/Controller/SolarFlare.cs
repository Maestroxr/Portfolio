using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A solar flare of the dying star: first the edge of the sector it comes from glows for a moment (and the warning
    /// sounds), then a wall of fire sweeps across the whole playfield and out the other side. Every ship it passes takes
    /// a burn once (a dash goes through it unharmed, a shield takes it first), a freighter or station too. It is a body of
    /// its own, so a shared mission shows it everywhere; every client burns only its own ships, the simulator the allies.
    /// The side it comes from is its variant (<see cref="Side"/>), which a puppet needs before it starts to move.
    /// </summary>
    public class SolarFlare : SpaceBody
    {
        /// <summary>Where a flare comes from: it sweeps toward the opposite side.</summary>
        public enum Side
        {
            Left = 0,
            Right = 1,
            Bottom = 2,
            Top = 3
        }

        [SerializeField] internal float warningTime = 2.2f;
        [SerializeField] internal float speed = 13f;
        [Tooltip("Half the thickness of the wall of fire.")]
        [SerializeField] internal float halfWidth = 1.1f;
        [SerializeField] internal float damage = 22f;
        [SerializeField] internal float allyDamage = 14f;
        [Tooltip("The wall of fire: a quad one unit square, stretched across the playfield.")]
        [SerializeField] internal Transform wall;
        [Tooltip("The glow on the edge the flare comes from, during the warning.")]
        [SerializeField] internal Renderer edgeGlow;
        [SerializeField] internal ParticleSystem embers;
        [SerializeField] internal AudioClip roar;
        [SerializeField] internal Color tint = new Color(1f, 0.55f, 0.15f);

        private static MaterialPropertyBlock block;
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        /// <summary>Meters the drawn fire reaches back from its front, and the warning glow in from the edge.</summary>
        private const float WallDepth = 4.5f;
        private const float EdgeDepth = 4.5f;

        private readonly List<object> burned = new List<object>();
        private Side side;
        private bool sweeping;
        private Color edgeColor;

        /// <summary>The direction the wall sweeps in.</summary>
        public Vector2 Direction
        {
            get
            {
                switch (side)
                {
                    case Side.Right: return Vector2.left;
                    case Side.Bottom: return Vector2.up;
                    case Side.Top: return Vector2.down;
                    default: return Vector2.right;
                }
            }
        }

        /// <summary>Whether the wall of fire is on its way (after the warning).</summary>
        public bool IsSweeping => sweeping;


        public override void OnSpawned()
        {
            base.OnSpawned();
            wraps = false;
            pulledByGravity = false;
            sweeping = false;
            burned.Clear();
            Velocity = Vector2.zero;
            // A puppet comes from the side its variant names; the simulator sets it with Begin right after.
            Place((Side)Mathf.Clamp(NetVariant, 0, 3));
            if (edgeGlow != null)
            {
                edgeColor = edgeGlow.sharedMaterial != null && edgeGlow.sharedMaterial.HasProperty(ColorId) ? edgeGlow.sharedMaterial.GetColor(ColorId) : tint;
                edgeGlow.gameObject.SetActive(true);
            }
            if (wall != null)
            {
                wall.gameObject.SetActive(false);
            }
            if (embers != null)
            {
                embers.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            Field?.Sounds?.Warning();
        }


        /// <summary>The simulator's flare: it comes from <paramref name="from"/>.</summary>
        public void Begin(Side from)
        {
            NetVariant = (int)from;
            Place(from);
        }


        /// <summary>Puts the flare just outside the edge it comes from, its wall across the playfield.</summary>
        private void Place(Side from)
        {
            side = from;
            Playground playground = Field != null ? Field.Playground : null;
            Vector2 half = playground != null ? playground.HalfSize : new Vector2(17.8f, 10f);
            Vector2 direction = Direction;
            bool across = side == Side.Left || side == Side.Right;
            float reach = across ? half.x : half.y;
            Position = -direction * (reach + halfWidth + 0.5f);
            float length = (across ? half.y : half.x) * 2f + 4f;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            if (wall != null)
            {
                // The bright front of the fire is the leading edge of what burns; its tongues trail behind.
                wall.localPosition = new Vector3(0f, halfWidth - WallDepth * 0.5f, wall.localPosition.z);
                wall.localScale = new Vector3(length, WallDepth, 1f);
            }
            if (edgeGlow != null)
            {
                // The warning glows inside the edge the flare comes through, brightest at the edge.
                edgeGlow.transform.localPosition = new Vector3(0f, halfWidth + 0.5f + EdgeDepth * 0.5f - 0.3f, edgeGlow.transform.localPosition.z);
                edgeGlow.transform.localScale = new Vector3(length, EdgeDepth, 1f);
            }
            if (embers != null)
            {
                ParticleSystem.ShapeModule shape = embers.shape;
                shape.scale = new Vector3(length, 0.6f, 0.1f);
            }
        }


        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (!InPlay)
            {
                return;
            }
            if (!sweeping && Age >= warningTime)
            {
                Sweep();
            }
            if (!sweeping)
            {
                float urgency = Mathf.Clamp01(Age / Mathf.Max(0.1f, warningTime));
                GlowEdge(0.25f + 0.75f * urgency * (0.6f + 0.4f * Mathf.Sin(Age * Mathf.Lerp(8f, 30f, urgency))));
                return;
            }
            if (Field == null)
            {
                return;
            }
            for (int i = 0; i < Field.LocalShipCount; i++)
            {
                AsteroidsPlayer ship = Field.LocalShip(i);
                if (ship == null || !ship.IsAlive || burned.Contains(ship) || !Touches(ship.Position, ship.Radius))
                {
                    continue;
                }
                if (ship.TakeDamage(new DamageInfo(damage, Direction, ship.Position - Direction * ship.Radius, DamageSource.Hazard, false)))
                {
                    burned.Add(ship);
                    ship.Push(Direction * 5f);
                    Field.Effects?.Spark(ship.Position, tint, 1.6f);
                }
                else if (ship.IsDashing)
                {
                    // A dash goes through the wall unharmed.
                    burned.Add(ship);
                }
            }
            if (IsPuppet)
            {
                return;
            }
            for (int i = 0; i < Ally.Active.Count; i++)
            {
                Ally ally = Ally.Active[i];
                if (ally != null && ally.InPlay && ally.Field == Field && !burned.Contains(ally) && Touches(ally.Position, ally.Radius))
                {
                    burned.Add(ally);
                    ally.Damage(allyDamage);
                }
            }
        }


        /// <summary>Whether a circle of <paramref name="radius"/> at <paramref name="center"/> is inside the wall of fire.</summary>
        public bool Touches(Vector2 center, float radius)
        {
            return sweeping && Mathf.Abs(Vector2.Dot(center - Position, Direction)) <= halfWidth + radius;
        }


        private void Sweep()
        {
            sweeping = true;
            // The sweep is the same everywhere: a puppet sets off on its own and the simulator's reports only confirm it.
            Velocity = Direction * speed;
            if (edgeGlow != null)
            {
                edgeGlow.gameObject.SetActive(false);
            }
            if (wall != null)
            {
                wall.gameObject.SetActive(true);
            }
            if (embers != null)
            {
                embers.Play(true);
            }
            Field?.Sounds?.Play(roar, 0.9f, 1f, 0.05f, 0.3f);
            Field?.CameraRig?.Shake(0.35f);
        }


        private void GlowEdge(float amount)
        {
            if (edgeGlow == null)
            {
                return;
            }
            block ??= new MaterialPropertyBlock();
            block.Clear();
            block.SetColor(ColorId, new Color(edgeColor.r, edgeColor.g, edgeColor.b, edgeColor.a * amount));
            edgeGlow.SetPropertyBlock(block);
        }


        /// <summary>The wall is gone once it is past the far edge.</summary>
        protected override bool HasLeft(Playground playground)
        {
            if (!sweeping)
            {
                return false;
            }
            Vector2 half = playground.HalfSize;
            float reach = side == Side.Left || side == Side.Right ? half.x : half.y;
            return Vector2.Dot(Position, Direction) > reach + halfWidth + 1f;
        }


        protected override void OnDespawned()
        {
            base.OnDespawned();
            burned.Clear();
            sweeping = false;
            if (embers != null)
            {
                embers.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
