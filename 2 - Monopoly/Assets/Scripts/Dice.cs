using System;
using System.Collections;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The two white dice and the red speed die on the board. <see cref="Roll"/> throws them from a player's side of the
    /// table: they fly in, tumble and bounce, and come to rest showing the result the rules engine rolled. Faces follow
    /// the atlas of the die meshes: +Y shows 1, +Z 2, +X 3, -X 4, -Z 5 and -Y 6 (opposite faces add up to seven); the
    /// speed die has 1, 2 and 3 there, the bus on -X and Mr. Monopoly on -Z and -Y.
    /// </summary>
    public class Dice : MonoBehaviour
    {
        [SerializeField] private Transform dieA;
        [SerializeField] private Transform dieB;
        [SerializeField] private Transform speedDie;
        [Tooltip("Where the dice come to rest, relative to this transform (the board centre).")]
        [SerializeField] private Vector3 restA = new Vector3(-0.5f, 0f, 0.15f);
        [SerializeField] private Vector3 restB = new Vector3(0.45f, 0f, -0.1f);
        [SerializeField] private Vector3 restSpeed = new Vector3(0f, 0f, -0.75f);
        [SerializeField] private float halfSize = 0.25f;
        [Tooltip("Soft shadows on the board under the white dice and the speed die, in that order.")]
        [SerializeField] private Transform[] shadows = new Transform[0];

        public event Action Bounced;

        private void LateUpdate()
        {
            Transform[] dice = { dieA, dieB, speedDie };
            for (int i = 0; i < shadows.Length && i < dice.Length; i++)
            {
                if (shadows[i] == null || dice[i] == null)
                {
                    continue;
                }
                shadows[i].gameObject.SetActive(dice[i].gameObject.activeInHierarchy);
                Vector3 p = dice[i].position;
                float ground = transform.position.y;
                float height = Mathf.Max(0f, p.y - ground - halfSize);
                shadows[i].position = new Vector3(p.x, ground + 0.004f, p.z);
                shadows[i].rotation = transform.rotation;
                float size = Mathf.Lerp(0.95f, 0.45f, Mathf.Clamp01(height / 1.5f));
                shadows[i].localScale = new Vector3(size, 1f, size);
            }
        }

        private static readonly Vector3[] FaceNormals =
        {
            Vector3.up, Vector3.forward, Vector3.right, Vector3.left, Vector3.back, Vector3.down
        };

        public bool SpeedDieVisible => speedDie != null && speedDie.gameObject.activeSelf;

        /// <summary>The rotation that puts face <paramref name="value"/> (1 to 6) on top, turned by <paramref name="yaw"/>.</summary>
        public static Quaternion FaceUp(int value, float yaw)
        {
            Vector3 normal = FaceNormals[Mathf.Clamp(value, 1, 6) - 1];
            return Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.FromToRotation(normal, Vector3.up);
        }

        /// <summary>The atlas face of the speed die that shows <paramref name="face"/>.</summary>
        public static int SpeedFaceValue(SpeedFace face, bool alternate)
        {
            switch (face)
            {
                case SpeedFace.One: return 1;
                case SpeedFace.Two: return 2;
                case SpeedFace.Three: return 3;
                case SpeedFace.Bus: return 4;
                default: return alternate ? 5 : 6;
            }
        }

        /// <summary>Puts the dice at rest showing <paramref name="roll"/> without animating (a loaded game).</summary>
        public void Show(DiceRoll roll)
        {
            Place(dieA, restA, FaceUp(Mathf.Max(1, roll.a), -12f));
            Place(dieB, restB, FaceUp(Mathf.Max(1, roll.b), 17f));
            if (speedDie != null)
            {
                speedDie.gameObject.SetActive(roll.HasSpeed);
                if (roll.HasSpeed)
                {
                    Place(speedDie, restSpeed, FaceUp(SpeedFaceValue(roll.speed, false), 5f));
                }
            }
        }

        private void Place(Transform die, Vector3 rest, Quaternion rotation)
        {
            if (die == null)
            {
                return;
            }
            die.gameObject.SetActive(true);
            die.position = transform.TransformPoint(rest + Vector3.up * halfSize);
            die.rotation = transform.rotation * rotation;
        }

        /// <summary>Throws the dice from <paramref name="from"/> (world) and lands them on <paramref name="roll"/>.</summary>
        public IEnumerator Roll(DiceRoll roll, Vector3 from, float duration)
        {
            var random = new System.Random(Environment.TickCount);
            bool speed = roll.HasSpeed && speedDie != null;
            if (speedDie != null)
            {
                speedDie.gameObject.SetActive(speed);
            }
            Coroutine a = StartCoroutine(Tumble(dieA, from + Jitter(random, 0.3f), restA + Jitter(random, 0.08f), roll.a, duration, random, true));
            Coroutine b = StartCoroutine(Tumble(dieB, from + Jitter(random, 0.3f), restB + Jitter(random, 0.08f), roll.b, duration * 1.05f, random, false));
            Coroutine c = speed
                ? StartCoroutine(Tumble(speedDie, from + Jitter(random, 0.3f), restSpeed + Jitter(random, 0.06f), SpeedFaceValue(roll.speed, random.Next(2) == 0), duration * 0.95f, random, false))
                : null;
            yield return a;
            yield return b;
            if (c != null)
            {
                yield return c;
            }
        }

        private static Vector3 Jitter(System.Random random, float amount)
        {
            return new Vector3((float)(random.NextDouble() * 2 - 1) * amount, 0f, (float)(random.NextDouble() * 2 - 1) * amount);
        }

        private IEnumerator Tumble(Transform die, Vector3 fromWorld, Vector3 restLocal, int value, float duration, System.Random random, bool reportBounces)
        {
            if (die == null)
            {
                yield break;
            }
            die.gameObject.SetActive(true);
            float yaw = (float)random.NextDouble() * 360f;
            Quaternion final = transform.rotation * FaceUp(value, yaw);
            Vector3 axis = new Vector3((float)(random.NextDouble() * 2 - 1), (float)(random.NextDouble() * 0.6 - 0.3), (float)(random.NextDouble() * 2 - 1)).normalized;
            if (axis.sqrMagnitude < 0.01f)
            {
                axis = Vector3.right;
            }
            float spin = 720f + (float)random.NextDouble() * 540f;
            Vector3 start = fromWorld + Vector3.up * 1.4f;
            Vector3 end = transform.TransformPoint(restLocal + Vector3.up * halfSize);
            Vector3 first = Vector3.Lerp(start, end, 0.62f);
            first.y = end.y;
            Vector3 second = Vector3.Lerp(start, end, 0.9f);
            second.y = end.y;
            // Three hops: the throw, a bounce and a little skip.
            float[] ends = { 0.55f, 0.85f, 1f };
            Vector3[] points = { start, first, second, end };
            float[] heights = { 0.9f, 0.35f, 0.08f };
            float time = 0f;
            int hop = 0;
            while (time < duration)
            {
                float t = time / duration;
                while (hop < 2 && t > ends[hop])
                {
                    hop++;
                    if (reportBounces)
                    {
                        Bounced?.Invoke();
                    }
                }
                float hopStart = hop == 0 ? 0f : ends[hop - 1];
                float local = Mathf.InverseLerp(hopStart, ends[hop], t);
                Vector3 position = Tween.Arc(points[hop], points[hop + 1], heights[hop], local);
                if (hop == 0)
                {
                    // The throw comes down from above.
                    position.y = Mathf.Lerp(start.y, end.y, Tween.InQuad(local)) + 4f * heights[0] * local * (1f - local);
                }
                die.position = position;
                float remaining = 1f - Tween.OutCubic(t);
                die.rotation = Quaternion.AngleAxis(spin * remaining, axis) * final;
                yield return null;
                time += Time.deltaTime;
            }
            if (reportBounces)
            {
                Bounced?.Invoke();
            }
            die.position = end;
            die.rotation = final;
        }
    }
}
