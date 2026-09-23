using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// Something that stands on a cell and can be told to look somewhere, walk somewhere and play a move: a creature of
    /// an army, a hero with his mount, a building. The model underneath is driven by a <see cref="Puppet"/>.
    /// </summary>
    public class Actor : MonoBehaviour
    {
        [SerializeField] protected Puppet puppet;

        /// <summary>The cell it stands on, in the rules' numbering.</summary>
        public int Cell { get; set; } = -1;

        public Puppet Puppet => puppet;

        /// <summary>Where a blow lands or a shot is aimed: the middle of the model, not its feet.</summary>
        public virtual Vector3 Middle => transform.position + Vector3.up * 0.8f;

        public void Face(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        public IEnumerator Turn(Vector3 direction, float speed = 12f)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                yield break;
            }
            Quaternion target = Quaternion.LookRotation(direction);
            while (Quaternion.Angle(transform.rotation, target) > 2f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, speed * 60f * Time.deltaTime);
                yield return null;
            }
            transform.rotation = target;
        }

        /// <summary>Walks to a point, turning into the step first and going back to standing at the end.</summary>
        public IEnumerator WalkTo(Vector3 point, float speed, bool turn = true)
        {
            Vector3 direction = point - transform.position;
            if (turn)
            {
                yield return Turn(direction);
            }
            puppet?.Walk();
            while ((point - transform.position).sqrMagnitude > 0.0004f)
            {
                transform.position = Vector3.MoveTowards(transform.position, point, speed * Time.deltaTime);
                yield return null;
            }
            transform.position = point;
            puppet?.Idle();
        }

        /// <summary>
        /// Walks along a path in one go: the walk keeps looping from the first step to the last, and the body turns into
        /// each new step while it moves, so a path of many cells reads as one walk rather than a string of starts and stops.
        /// </summary>
        public IEnumerator WalkPath(IReadOnlyList<Vector3> points, float speed)
        {
            if (points == null || points.Count == 0)
            {
                yield break;
            }
            // A walk that starts back the way it faces turns on the spot first; the rest turns on the move.
            Vector3 first = points[0] - transform.position;
            first.y = 0f;
            if (first.sqrMagnitude > 0.0001f && Vector3.Angle(transform.forward, first) > 100f)
            {
                yield return Turn(first, 16f);
            }
            puppet?.Walk();
            foreach (Vector3 point in points)
            {
                while ((point - transform.position).sqrMagnitude > 0.0004f)
                {
                    Vector3 step = point - transform.position;
                    step.y = 0f;
                    if (step.sqrMagnitude > 0.0001f)
                    {
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(step),
                            540f * Time.deltaTime);
                    }
                    transform.position = Vector3.MoveTowards(transform.position, point, speed * Time.deltaTime);
                    yield return null;
                }
                transform.position = point;
            }
            puppet?.Idle();
        }

        /// <summary>Plays a clip once, or a lunge toward <paramref name="at"/> when the model has no such clip.</summary>
        public IEnumerator Perform(string clip, Vector3 at)
        {
            if (puppet == null)
            {
                yield return new WaitForSeconds(0.3f);
            }
            else if (puppet.Has(clip))
            {
                float length = puppet.Play(clip);
                yield return new WaitForSeconds(length * 0.9f);
            }
            else
            {
                yield return puppet.Lunge(at - transform.position);
            }
        }
    }

    /// <summary>A stack of one kind of creature, on the map or on the battlefield.</summary>
    public sealed class UnitView : Actor
    {
        private HeroesArt.UnitArt art;
        private float height = 1.6f;

        public CreatureId Creature { get; private set; } = CreatureId.None;

        public HeroesArt.UnitArt Art => art;

        public override Vector3 Middle => transform.position + Vector3.up * (Height * 0.55f);

        /// <summary>
        /// Builds the model of a creature under this actor: the creature's own, or <paramref name="look"/> in its place
        /// (an arrow tower in the colors of the town it defends). A <paramref name="still"/> model without an idle clip
        /// stands without breathing: a building.
        /// </summary>
        public void Setup(HeroesArt catalog, CreatureId creature, GameObject look = null, bool still = false)
        {
            Creature = creature;
            art = catalog.Unit(creature);
            height = art != null ? art.height : 1.6f;
            GameObject prefab = look != null ? look : art != null ? art.prefab : null;
            if (prefab == null)
            {
                return;
            }
            GameObject model = Instantiate(prefab, transform);
            model.transform.localPosition = art != null && art.flies ? Vector3.up * (height * 0.35f) : Vector3.zero;
            puppet = gameObject.AddComponent<Puppet>();
            puppet.Setup(model.transform, art != null ? art.idle : "Idle", art != null ? art.walk : "Walk", still);
        }

        /// <summary>How tall the creature stands (as big as it is shown), for what floats over it.</summary>
        public float Height => height * transform.lossyScale.y;

        /// <summary>Whether it flies (it is lifted off the ground, and crosses the field in the air).</summary>
        public bool Flies => art != null && art.flies;

        public string AttackClip => art != null ? art.attack : "";

        public string ShootClip => art != null ? art.shoot : "";

        public string HitClip => art != null ? art.hit : "";

        public string DeathClip => art != null ? art.death : "";

        public ProjectileKind Projectile => art != null ? art.projectile : ProjectileKind.None;
    }

    /// <summary>A hero on the map: a rider sitting on a mount, with the banner of his color behind him.</summary>
    public sealed class HeroView : Actor
    {
        private Puppet rider;
        private HeroesArt.HeroArt art;

        public int HeroId { get; private set; } = -1;

        public override Vector3 Middle => transform.position + Vector3.up * (1.6f * transform.lossyScale.y);

        /// <summary>
        /// Builds the rider on his mount, with a banner of <paramref name="color"/> (a player's color, as a number; the
        /// owner's seat when none is given, which is the same on a map laid out by the generator).
        /// </summary>
        public void Setup(HeroesArt catalog, HeroState hero, int color = -1)
        {
            HeroId = hero.id;
            Cell = hero.cell;
            art = catalog.Hero(hero.Def != null ? hero.Def.Class : HeroClass.Knight);
            if (art == null)
            {
                return;
            }
            if (art.mount != null)
            {
                GameObject mount = Instantiate(art.mount, transform);
                puppet = gameObject.AddComponent<Puppet>();
                puppet.Setup(mount.transform, art.mountIdle, art.mountWalk);
            }
            if (art.rider != null)
            {
                GameObject seat = new GameObject("Seat");
                seat.transform.SetParent(transform, false);
                seat.transform.localPosition = art.seat;
                GameObject model = Instantiate(art.rider, seat.transform);
                rider = seat.AddComponent<Puppet>();
                // The rider holds the pose of a man in the saddle while the mount does the moving.
                rider.Setup(model.transform, string.IsNullOrEmpty(art.riderPose) ? "Idle" : art.riderPose, "");
            }
            if (catalog.Flag(color >= 0 ? color : hero.owner) is GameObject flag && flag != null)
            {
                GameObject banner = Instantiate(flag, transform);
                banner.transform.localPosition = new Vector3(0.35f, 0f, -0.55f);
                banner.transform.localScale = Vector3.one * 0.75f;
            }
        }

        /// <summary>The spell the hero casts from the saddle, if his model knows how.</summary>
        public IEnumerator Cast()
        {
            if (rider != null && art != null && rider.Has(art.riderCast))
            {
                yield return new WaitForSeconds(rider.Play(art.riderCast) * 0.8f);
            }
            else
            {
                yield return new WaitForSeconds(0.4f);
            }
        }
    }

    /// <summary>A thing standing on the map: a mine, a chest, a shrine, a town.</summary>
    public sealed class ObjectView : MonoBehaviour
    {
        private GameObject flag;
        private Transform spin;

        public int ObjectId { get; private set; } = -1;

        public ObjectKind Kind { get; private set; }

        /// <summary>The owner the banner is showing, or -1.</summary>
        public int Owner { get; private set; } = -2;

        public void Setup(HeroesArt catalog, MapObject what, GameObject prefab, Vector3 flagAt)
        {
            ObjectId = what.id;
            Kind = what.kind;
            if (prefab != null)
            {
                GameObject model = Instantiate(prefab, transform);
                model.transform.localPosition = Vector3.zero;
                if (MapObjects.IsPickup(what.kind))
                {
                    // Treasure turns slowly on the spot so the eye catches it.
                    spin = model.transform;
                }
            }
            FlagPost = flagAt;
            SetOwner(catalog, what.owner);
        }

        public Vector3 FlagPost { get; private set; }

        /// <summary>Plants (or moves, or takes away) the banner that shows who holds the place.</summary>
        public void SetOwner(HeroesArt catalog, int owner)
        {
            if (Owner == owner)
            {
                return;
            }
            Owner = owner;
            if (flag != null)
            {
                Destroy(flag);
                flag = null;
            }
            if (!MapObjects.IsBuilding(Kind) || Kind == ObjectKind.Town)
            {
                return;
            }
            GameObject prefab = catalog.Flag(owner);
            if (prefab == null)
            {
                return;
            }
            flag = Instantiate(prefab, transform);
            flag.transform.localPosition = FlagPost;
        }

        private void Update()
        {
            if (spin != null)
            {
                spin.Rotate(Vector3.up, 28f * Time.deltaTime, Space.World);
            }
        }
    }
}
