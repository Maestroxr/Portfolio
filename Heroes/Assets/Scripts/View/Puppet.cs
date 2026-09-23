using System.Collections;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// Plays the animations of a model by name: the legacy <see cref="Animation"/> clips its importer made (idle and
    /// walk loop, the others play once and fall back to idle). A model without a clip for something moves by itself
    /// instead: it bobs, lunges, flinches and sinks.
    /// </summary>
    public sealed class Puppet : MonoBehaviour
    {
        private Animation animations;
        private Transform body;
        private string idle = "Idle";
        private string walk = "Walk";
        private string current;
        private Vector3 bodyPosition;
        private Quaternion bodyRotation;
        private Vector3 bodyScale;
        private float bob;
        private bool procedural;
        private bool dead;

        public Transform Body => body;

        /// <summary>
        /// Takes charge of a model and starts it idling. A <paramref name="still"/> model (a building) does not breathe
        /// when it has no idle clip of its own.
        /// </summary>
        public void Setup(Transform model, string idleClip, string walkClip, bool still = false)
        {
            body = model;
            bodyPosition = model.localPosition;
            bodyRotation = model.localRotation;
            bodyScale = model.localScale;
            idle = idleClip;
            walk = walkClip;
            animations = model.GetComponentInChildren<Animation>(true);
            if (animations != null)
            {
                animations.cullingType = AnimationCullingType.BasedOnRenderers;
                foreach (AnimationState state in animations)
                {
                    state.wrapMode = state.name == idle || state.name == walk ? WrapMode.Loop : WrapMode.Once;
                }
            }
            procedural = !Has(idle) && !still;
            bob = Random.value * 10f;
            Idle();
        }

        /// <summary>
        /// Takes the pose of the clip it plays at once (a model shows it only from the next frame on): for measuring the
        /// body as it stands rather than spread out as it was modelled.
        /// </summary>
        public void Pose()
        {
            if (animations == null || !Has(current))
            {
                return;
            }
            AnimationState state = animations[current];
            state.enabled = true;
            state.weight = 1f;
            animations.Sample();
        }

        public bool Has(string clip)
        {
            return animations != null && !string.IsNullOrEmpty(clip) && animations.GetClip(clip) != null;
        }

        public float Length(string clip)
        {
            if (!Has(clip))
            {
                return 0.5f;
            }
            return animations[clip].length / Mathf.Max(0.1f, animations[clip].speed);
        }

        public void Idle()
        {
            if (dead)
            {
                return;
            }
            Loop(idle);
        }

        public void Walk()
        {
            if (dead)
            {
                return;
            }
            Loop(Has(walk) ? walk : idle);
        }

        private void Loop(string clip)
        {
            if (!Has(clip) || current == clip)
            {
                return;
            }
            current = clip;
            animations[clip].wrapMode = WrapMode.Loop;
            animations.CrossFade(clip, 0.2f);
        }

        /// <summary>Plays a clip once and returns to idle; returns how long it lasts.</summary>
        public float Play(string clip, float speed = 1f)
        {
            if (dead)
            {
                return 0f;
            }
            if (!Has(clip))
            {
                return 0.45f;
            }
            AnimationState state = animations[clip];
            state.speed = speed;
            state.wrapMode = WrapMode.Once;
            animations.CrossFade(clip, 0.12f);
            current = clip;
            StopAllCoroutines();
            StartCoroutine(BackToIdle(state.length / Mathf.Max(0.1f, speed)));
            return state.length / Mathf.Max(0.1f, speed);
        }

        private IEnumerator BackToIdle(float after)
        {
            yield return new WaitForSeconds(Mathf.Max(0.05f, after - 0.15f));
            if (!dead)
            {
                current = null;
                Idle();
            }
        }

        /// <summary>Plays the death clip (or sinks down) and stays down.</summary>
        public float Die(string clip)
        {
            if (dead)
            {
                return 0f;
            }
            float length = Play(clip);
            dead = true;
            StopAllCoroutines();
            if (body != null)
            {
                // A flinch or a lunge cut short would leave the body squashed or out of place.
                body.localScale = bodyScale;
                body.localPosition = bodyPosition;
            }
            if (!Has(clip))
            {
                StartCoroutine(Sink());
                return 0.8f;
            }
            return length;
        }

        private IEnumerator Sink()
        {
            float t = 0f;
            Quaternion start = body.localRotation;
            Quaternion fallen = start * Quaternion.Euler(0f, 0f, 80f);
            while (t < 1f)
            {
                t += Time.deltaTime * 1.6f;
                body.localRotation = Quaternion.Slerp(start, fallen, t);
                body.localPosition = bodyPosition + Vector3.down * (t * 0.2f);
                yield return null;
            }
        }

        /// <summary>A procedural lunge toward <paramref name="direction"/> for models without an attack clip.</summary>
        public IEnumerator Lunge(Vector3 direction)
        {
            if (body == null)
            {
                yield break;
            }
            Vector3 start = body.position;
            Vector3 forward = direction.normalized * 0.5f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 4f;
                float k = Mathf.Sin(t * Mathf.PI);
                body.position = start + forward * k;
                yield return null;
            }
            body.position = start;
        }

        public void Flinch()
        {
            if (body != null && !dead && isActiveAndEnabled)
            {
                StartCoroutine(FlinchRoutine());
            }
        }

        private IEnumerator FlinchRoutine()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 5f;
                float k = Mathf.Sin(t * Mathf.PI) * 0.12f;
                body.localScale = bodyScale * (1f - k * 0.5f);
                yield return null;
            }
            body.localScale = bodyScale;
        }

        private void Update()
        {
            if (!procedural || dead || body == null)
            {
                return;
            }
            // Models without clips breathe a little.
            bob += Time.deltaTime;
            float k = Mathf.Sin(bob * 2.2f);
            body.localPosition = bodyPosition + Vector3.up * (k * 0.04f);
            body.localRotation = bodyRotation * Quaternion.Euler(k * 2f, 0f, 0f);
        }
    }
}
