using System.Collections;
using Gamebox;
using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// One backgammon checker on the 3D board: a disc in its side's material with a soft contact shadow under it while
    /// it lies flat on the felt. The board places it and flies it from place to place; the coroutine here is one flight.
    /// </summary>
    public sealed class CheckerView : MonoBehaviour
    {
        [SerializeField] private MeshRenderer disc;
        [SerializeField] private Transform shadow;

        /// <summary>The side the checker belongs to (the board sets it once).</summary>
        public Side Side { get; set; }

        /// <summary>Whether the checker is in the air.</summary>
        public bool Flying { get; private set; }

        public MeshRenderer Disc => disc;

        /// <summary>The disc's mesh and material (each kept when null).</summary>
        public void Dress(Mesh mesh, Material material)
        {
            if (disc == null)
            {
                return;
            }
            if (material != null)
            {
                disc.sharedMaterial = material;
            }
            var filter = disc.GetComponent<MeshFilter>();
            if (filter != null && mesh != null)
            {
                filter.sharedMesh = mesh;
            }
        }

        /// <summary>The material of the contact shadow (kept when null).</summary>
        public void SetShadowMaterial(Material material)
        {
            var renderer = shadow != null ? shadow.GetComponent<MeshRenderer>() : null;
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        /// <summary>Puts the checker at <paramref name="local"/> on the board (its parent), with or without its shadow.</summary>
        public void Place(Pose local, bool withShadow)
        {
            Flying = false;
            transform.SetLocalPositionAndRotation(local.position, local.rotation);
            SetShadow(withShadow);
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Flies to <paramref name="local"/> over <paramref name="seconds"/> after <paramref name="delay"/>, hopping
        /// <paramref name="hop"/> high on the way and turning to the new rotation; the shadow shows again on landing
        /// when <paramref name="withShadow"/>.
        /// </summary>
        public IEnumerator Fly(Pose local, float seconds, float hop, float delay, bool withShadow)
        {
            if (delay > 0f)
            {
                yield return Tween.Wait(delay);
            }
            Flying = true;
            SetShadow(false);
            Vector3 from = transform.localPosition;
            Quaternion fromRotation = transform.localRotation;
            yield return Tween.Run(seconds, t =>
            {
                float eased = Tween.InOutCubic(t);
                transform.SetLocalPositionAndRotation(Tween.Arc(from, local.position, hop, eased), Quaternion.Slerp(fromRotation, local.rotation, eased));
            });
            Place(local, withShadow);
        }

        private void SetShadow(bool on)
        {
            if (shadow != null && shadow.gameObject.activeSelf != on)
            {
                shadow.gameObject.SetActive(on);
            }
        }
    }
}
