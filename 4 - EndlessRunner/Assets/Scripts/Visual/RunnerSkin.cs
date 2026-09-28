using Gamebox;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// Gives the runner (and every ghost made from its prefab) the look of the active <see cref="RunnerGameTheme"/>:
    /// the meshes and the material of the body parts, where the joints sit, and the materials of the auras and the
    /// dust. Applied when the runner wakes and again whenever the theme changes.
    /// </summary>
    public class RunnerSkin : ThemedElement
    {
        [SerializeField] internal RunnerAnimator animator;
        [SerializeField] internal GameObject shieldBubble;
        [SerializeField] internal GameObject magnetAura;
        [SerializeField] internal GameObject superJumpAura;
        [SerializeField] internal ParticleSystem runDust;

        protected override void Apply(GameTheme theme)
        {
            if (theme is RunnerGameTheme look)
            {
                Apply(look);
            }
        }

        public void Apply(RunnerGameTheme theme)
        {
            if (theme == null || animator == null)
            {
                return;
            }
            RunnerLook look = theme.Runner;
            PieceMaterials materials = theme.Materials;
            Part(animator.spine, look.torso, look.material);
            Part(animator.head, look.head, look.material);
            Part(animator.armLeft, look.upperArm, look.material);
            Part(animator.armRight, look.upperArm, look.material);
            Part(animator.forearmLeft, look.forearm, look.material);
            Part(animator.forearmRight, look.forearm, look.material);
            Part(animator.legLeft, look.thigh, look.material);
            Part(animator.legRight, look.thigh, look.material);
            Part(animator.shinLeft, look.shin, look.material);
            Part(animator.shinRight, look.shin, look.material);
            Place(animator.hips, new Vector3(0f, look.hipsHeight, 0f));
            Place(animator.head, look.neck);
            Place(animator.armLeft, new Vector3(-look.shoulder.x, look.shoulder.y, look.shoulder.z));
            Place(animator.armRight, look.shoulder);
            Place(animator.forearmLeft, new Vector3(0f, look.elbow, 0f));
            Place(animator.forearmRight, new Vector3(0f, look.elbow, 0f));
            Place(animator.legLeft, new Vector3(-look.hip, 0f, 0f));
            Place(animator.legRight, new Vector3(look.hip, 0f, 0f));
            Place(animator.shinLeft, new Vector3(0f, look.knee, 0f));
            Place(animator.shinRight, new Vector3(0f, look.knee, 0f));
            animator.RefreshRest();

            if (shieldBubble != null)
            {
                Halos(shieldBubble.transform, materials.shieldRing, materials.haloShield);
            }
            if (magnetAura != null)
            {
                Material(magnetAura.transform, look.material);
            }
            if (superJumpAura != null)
            {
                Material(superJumpAura.transform.Find("Ring"), look.material);
                Halos(superJumpAura.transform, materials.haloSuperJump, materials.haloSuperJump);
            }
            if (runDust != null && theme.Particles.smoke != null)
            {
                runDust.GetComponent<ParticleSystemRenderer>().sharedMaterial = theme.Particles.smoke;
            }
        }

        private static void Part(Transform joint, Mesh mesh, Material material)
        {
            if (joint == null)
            {
                return;
            }
            if (mesh != null && joint.TryGetComponent(out MeshFilter filter))
            {
                filter.sharedMesh = mesh;
            }
            Material(joint, material);
        }

        private static void Material(Transform part, Material material)
        {
            if (part != null && material != null && part.TryGetComponent(out MeshRenderer renderer))
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void Place(Transform joint, Vector3 position)
        {
            if (joint != null)
            {
                joint.localPosition = position;
            }
        }

        /// <summary>The halos under <paramref name="parent"/> in order: the first gets <paramref name="first"/>, the rest <paramref name="rest"/>.</summary>
        private static void Halos(Transform parent, Material first, Material rest)
        {
            int index = 0;
            foreach (Transform child in parent)
            {
                if (child.name != "Halo")
                {
                    continue;
                }
                Material(child, index == 0 ? first : rest);
                index++;
            }
        }
    }
}
