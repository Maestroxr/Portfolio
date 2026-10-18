using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The slug of the Rail Driver (asteroid field) and of the rail gun (strike): so fast that it would skip past a small
    /// rock between two frames, so every step it checks the whole stretch it flew (a swept circle) and hits what lies
    /// along it in order, nearest first, until its pierce is spent. The field's own collision test still runs at the end
    /// of the stretch; a target is never hit twice.
    /// </summary>
    public class RailShot : Shot
    {
        private static readonly List<(Shootable target, float along)> Along = new List<(Shootable, float)>(16);


        public override void Tick(float deltaTime)
        {
            Vector2 before = Position;
            base.Tick(deltaTime);
            if (InPlay && Field != null)
            {
                Sweep(before, Position);
            }
        }


        /// <summary>
        /// Where along the segment from <paramref name="from"/> to <paramref name="to"/> a circle of
        /// <paramref name="reach"/> around <paramref name="center"/> is first touched (meters from the start), or -1 when the
        /// segment passes it by.
        /// </summary>
        public static float Touch(Vector2 from, Vector2 to, Vector2 center, float reach)
        {
            Vector2 path = to - from;
            float length = path.magnitude;
            if (length < 0.0001f)
            {
                return (center - from).sqrMagnitude <= reach * reach ? 0f : -1f;
            }
            Vector2 direction = path / length;
            Vector2 offset = center - from;
            float along = Vector2.Dot(offset, direction);
            float side = (offset - direction * along).sqrMagnitude;
            if (side > reach * reach || along < -reach || along > length + reach)
            {
                return -1f;
            }
            return Mathf.Max(0f, along - Mathf.Sqrt(Mathf.Max(0f, reach * reach - side)));
        }


        private void Sweep(Vector2 from, Vector2 to)
        {
            SpaceField field = Field;
            IReadOnlyList<Shootable> targets = field.Targets;
            Along.Clear();
            for (int i = 0; i < targets.Count; i++)
            {
                Shootable target = targets[i];
                if (target == null || !target.IsAlive)
                {
                    continue;
                }
                float along = Touch(from, to, target.Position, radius + target.Radius);
                if (along < 0f || !field.IsTargetable(target) || !CanHit(target))
                {
                    continue;
                }
                Along.Add((target, along));
            }
            if (Along.Count == 0)
            {
                return;
            }
            Along.Sort((a, b) => a.along.CompareTo(b.along));
            foreach ((Shootable target, float _) in Along)
            {
                if (!InPlay)
                {
                    break;
                }
                bool first = HitCount == 0;
                Hit(target);
                if (first)
                {
                    field.NoteLanded(this);
                }
            }
            Along.Clear();
        }
    }
}
