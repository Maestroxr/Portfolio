using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// What the autopilot knows of the field campaigns (AsteroidsAutopilot.FieldCampaigns.cs): it turns into an
    /// oncoming solar flare and dashes through it, keeps out of ion storms, lightning rings and the lines of beams (it
    /// crosses a sweep beam against its turn), keeps off a boss's hull, and picks its prey for the objective: what
    /// threatens the freighter or station first, the marked quarry of a hunt, a boss when no gunner is near, and never
    /// a phase stalker that is out of phase.
    /// </summary>
    public partial class AsteroidsAutopilot
    {
        /// <summary>How soon a beam counts as hitting: sooner than most shots, since a beam burns hardest.</summary>
        private const float BeamUrgency = 0.25f;

        /// <summary>Meters the autopilot keeps between the ship and a boss's hull.</summary>
        private const float BossClearance = 3f;

        /// <summary>Seconds ahead the autopilot watches a boss's course, and how soon a boss on that course counts as hitting.</summary>
        private const float BossLookAhead = 1.6f;
        private const float BossUrgency = 0.2f;

        /// <summary>
        /// The campaigns' threats to the ship at <paramref name="position"/>: like the field's own, the soonest wins and
        /// <paramref name="escape"/> is the way out of it (for a flare, the way into it: the dash goes through).
        /// </summary>
        private static void FieldThreats(SpaceField field, AsteroidsPlayer player, Vector2 position, ref float soonest, ref Vector2 escape)
        {
            Playground playground = field.Playground;
            var bodies = field.Bodies;
            for (int i = 0; i < bodies.Count; i++)
            {
                SpaceBody body = bodies[i];
                if (!body.InPlay)
                {
                    continue;
                }
                switch (body)
                {
                    case SolarFlare flare when flare.IsSweeping:
                    {
                        float ahead = Vector2.Dot(position - flare.Position, flare.Direction);
                        if (ahead > -0.5f && ahead < 6f)
                        {
                            float time = Mathf.Max(0f, ahead) / Mathf.Max(1f, flare.speed);
                            if (time < soonest)
                            {
                                soonest = Mathf.Min(time, 0.3f);
                                escape = -flare.Direction;
                            }
                        }
                        break;
                    }
                    case IonStorm storm when storm.Strength01 > 0.2f:
                    {
                        Vector2 offset = playground.Delta(storm.Position, position);
                        float reach = storm.Radius + player.Radius + 1.5f;
                        if (offset.sqrMagnitude < reach * reach && 0.6f < soonest)
                        {
                            soonest = 0.6f;
                            escape = offset.sqrMagnitude > 0.01f ? offset.normalized : Vector2.up;
                        }
                        break;
                    }
                }
            }
            // A boss rams what flies too close: keep a few meters off its hull, and get out of its way when it comes
            // on (an orbiting or charging boss moves faster than the ship turns). It comes before rocks and shots.
            var targets = field.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (!(targets[i] is Boss boss) || !boss.IsAlive)
                {
                    continue;
                }
                Vector2 offset = playground.Delta(boss.Position, position);
                Vector2 closing = boss.Velocity - player.Velocity;
                float keep = boss.Radius + player.Radius + BossClearance;
                float speed2 = closing.sqrMagnitude;
                float time = speed2 > 0.0001f ? Mathf.Clamp(Vector2.Dot(offset, closing) / speed2, 0f, BossLookAhead) : 0f;
                Vector2 nearest = offset - closing * time;
                if (nearest.sqrMagnitude < keep * keep && BossUrgency < soonest)
                {
                    soonest = BossUrgency;
                    // Away from where it will pass, else straight away from it.
                    escape = nearest.sqrMagnitude > 0.01f ? nearest.normalized : offset.sqrMagnitude > 0.01f ? offset.normalized : Vector2.down;
                }
            }
            var shots = field.EnemyShots;
            for (int i = 0; i < shots.Count; i++)
            {
                Shot shot = shots[i];
                if (!shot.InPlay)
                {
                    continue;
                }
                switch (shot)
                {
                    case ZoneShot zone:
                    {
                        Vector2 offset = position - zone.Position;
                        float reach = zone.Radius + player.Radius + 0.8f;
                        if (offset.sqrMagnitude < reach * reach && 0.4f < soonest)
                        {
                            soonest = 0.4f;
                            escape = offset.sqrMagnitude > 0.01f ? offset.normalized : Vector2.right;
                        }
                        break;
                    }
                    case FieldBeam beam:
                    {
                        Vector2 direction = beam.Direction;
                        float along = Vector2.Dot(position - beam.Position, direction);
                        if (along < -1f || along > beam.length + 1f)
                        {
                            break;
                        }
                        Vector2 side = new Vector2(-direction.y, direction.x);
                        float across = Vector2.Dot(position - beam.Position, side);
                        float danger = beam.halfWidth + player.Radius + (beam.sweep > 0f ? 4f : 1.6f);
                        if (Mathf.Abs(across) >= danger || BeamUrgency <= soonest)
                        {
                            break;
                        }
                        if (beam.sweep > 0f)
                        {
                            // A sweep beam cannot be outrun far from its origin: cross it against its turn, where it
                            // burns for the shortest moment (before it starts burning, for nothing). Once past, keep going.
                            Vector2 turning = beam.Clockwise ? -side : side;
                            if (Vector2.Dot(position - beam.Position, turning) < -(beam.halfWidth + player.Radius))
                            {
                                break;
                            }
                            soonest = BeamUrgency;
                            escape = -turning;
                            break;
                        }
                        soonest = BeamUrgency;
                        escape = across >= 0f ? side : -side;
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// The campaigns' choice of prey near <paramref name="position"/>, or null to hunt the nearest target: with a freighter
        /// or station in play, what comes for it; in a hunt, the marked quarry that shows; in a boss fight, the boss when
        /// nothing else is close.
        /// </summary>
        private static Shootable FieldPrey(SpaceField field, Vector2 position)
        {
            Ally ally = Ally.Nearest(field, position, 1000f);
            Vector2 at = ally != null ? ally.Position : Vector2.zero;
            // A mine or magma rock right next to the ally is left alone: its blast would hurt the ally too.
            float blastClear = ally != null ? ally.Radius + 3.5f : 0f;
            bool Spared(Shootable target) => ally != null && target is Explodable && (target.Position - at).sqrMagnitude < blastClear * blastClear;
            if (ally != null)
            {
                Shootable threat = field.NearestTarget(at, 11f, target => !target.Invulnerable && !Spared(target) && (target is Enemy || target is Explodable ||
                    Vector2.Dot(target.Velocity, at - target.Position) > 0f && (target.Position - at).sqrMagnitude < 36f));
                if (threat != null)
                {
                    return threat;
                }
            }
            Shootable marked = field.NearestTarget(position, 40f, target => target is CampaignEnemy enemy && enemy.Marked && !enemy.Invulnerable);
            if (marked != null)
            {
                return marked;
            }
            Shootable nearest = field.NearestTarget(position, 60f, target => !(target is PhaseStalker stalker && stalker.Invulnerable) && !Spared(target));
            // A boss that can be hurt comes first, unless something else is close enough to be in the way or an enemy
            // ship is near enough to shoot back.
            Shootable boss = field.NearestTarget(position, 60f, target => target is Boss candidate && candidate.HasEntered && !candidate.Invulnerable);
            if (boss == null || nearest == null || nearest == boss || (nearest.Position - position).sqrMagnitude <= 5f * 5f)
            {
                return nearest;
            }
            Shootable gunner = field.NearestTarget(position, 10f, target => target is Enemy && !(target is Boss) && !target.Invulnerable);
            return gunner ?? boss;
        }
    }
}
