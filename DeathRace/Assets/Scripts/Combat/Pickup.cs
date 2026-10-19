using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// One pickup on the road: a crate of cash, a repair kit, rounds or a nitro bottle, spinning and bobbing over a
    /// glowing ring in its colour so it reads from far above. A car that drives through it collects it (its field decides
    /// what it gives); it is gone for a few seconds, then pops back.
    /// </summary>
    public class Pickup : MonoBehaviour
    {
        private const float SpinSpeed = 110f;
        private const float BobHeight = 0.25f;
        private const float PopSeconds = 0.35f;
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private PickupField field;
        private Transform visual;

        /// <summary>What spins and bobs: the model's "Item" (its "Base" glow stays on the ground), or the whole stand-in.</summary>
        private Transform item;
        private Vector3 itemRest;
        private Renderer ring;
        private Collider trigger;
        private MaterialPropertyBlock block;
        private float respawn;
        private float age;
        private float pop = 1f;
        private float phase;

        public PickupKind Kind { get; private set; }

        /// <summary>The number of the pickup in its field (online: what a claim names).</summary>
        public int Index { get; private set; }

        /// <summary>Whether it is there to be collected.</summary>
        public bool Available { get; private set; } = true;

        /// <summary>The colour each kind is shown in (its ring, its stand-in).</summary>
        public static Color ColorOf(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.Cash: return new Color(0.35f, 0.95f, 0.4f, 1f);
                case PickupKind.Repair: return new Color(1f, 0.32f, 0.3f, 1f);
                case PickupKind.Ammo: return new Color(1f, 0.78f, 0.2f, 1f);
                default: return new Color(0.3f, 0.72f, 1f, 1f);
            }
        }

        public static Pickup Create(PickupField field, PickupKind kind, int index, Vector3 position, DeathRaceContent content, DeathRaceTheme theme)
        {
            var root = new GameObject($"Pickup {index} {kind}");
            root.transform.SetParent(field.transform, false);
            root.transform.position = new Vector3(position.x, 0f, position.z);
            Pickup pickup = root.AddComponent<Pickup>();
            pickup.field = field;
            pickup.Kind = kind;
            pickup.Index = index;
            pickup.phase = index * 0.77f;
            GameObject prefab = content != null ? content.Pickup(kind) : null;
            GameObject visual = prefab != null ? Instantiate(prefab, root.transform, false) : Fallback(kind, root.transform, theme);
            visual.name = "Visual";
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            {
                DestroyImmediate(collider);
            }
            pickup.visual = visual.transform;
            Transform model = visual.transform.Find("Item");
            pickup.item = model != null ? model : visual.transform;
            pickup.itemRest = model != null ? model.localPosition : new Vector3(0f, 0.35f, 0f);
            // A model with a glowing base of its own needs no ring of ours.
            pickup.ring = visual.transform.Find("Base") == null ? Ring(root.transform, ColorOf(kind)) : null;
            Mine.SetLayer(root, DeathRaceLayers.Pickup);
            var sphere = root.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = CombatRules.PickupRadius;
            sphere.center = new Vector3(0f, 0.8f, 0f);
            pickup.trigger = sphere;
            return pickup;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (!Available)
            {
                respawn -= Time.deltaTime;
                if (respawn <= 0f)
                {
                    Show();
                }
                return;
            }
            if (pop < 1f)
            {
                pop = Mathf.Min(1f, pop + Time.deltaTime / PopSeconds);
            }
            float scale = Gamebox.Tween.OutBack(pop);
            visual.localScale = Vector3.one * Mathf.Max(0.01f, scale);
            item.localPosition = itemRest + new Vector3(0f, BobHeight * (0.5f + 0.5f * Mathf.Sin((age + phase) * 3.2f)), 0f);
            item.localRotation = Quaternion.Euler(0f, (age + phase) * SpinSpeed, 0f);
            if (ring != null)
            {
                block ??= new MaterialPropertyBlock();
                Color tint = ColorOf(Kind) * 1.4f;
                tint.a = (0.45f + 0.25f * Mathf.Sin((age + phase) * 4f)) * pop;
                ring.GetPropertyBlock(block);
                block.SetColor(ColorId, tint);
                ring.SetPropertyBlock(block);
                float size = CombatRules.PickupRadius * 1.9f * (0.95f + 0.05f * Mathf.Sin((age + phase) * 4f));
                ring.transform.localScale = new Vector3(size, size, 1f);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!Available || field == null)
            {
                return;
            }
            Car car = other.GetComponentInParent<Car>();
            if (car != null && !car.IsWrecked)
            {
                field.TryCollect(this, car);
            }
        }

        /// <summary>Taken: gone until it comes back after <paramref name="seconds"/>.</summary>
        public void Hide(float seconds)
        {
            Available = false;
            respawn = seconds;
            visual.gameObject.SetActive(false);
            if (ring != null)
            {
                ring.enabled = false;
            }
            trigger.enabled = false;
        }

        /// <summary>Back on the road, with a little pop.</summary>
        public void Show()
        {
            Available = true;
            pop = 0f;
            visual.gameObject.SetActive(true);
            visual.localScale = Vector3.one * 0.01f;
            if (ring != null)
            {
                ring.enabled = true;
            }
            trigger.enabled = true;
        }

        private static Renderer Ring(Transform parent, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Ring";
            DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, RaceEffects.GroundMarkHeight + 0.02f, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = CombatArt.Ring;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        /// <summary>A stand-in until the builders made the pickups: a box or a bottle in the kind's colour with a glowing band.</summary>
        private static GameObject Fallback(PickupKind kind, Transform parent, DeathRaceTheme theme)
        {
            var root = new GameObject("Visual");
            root.transform.SetParent(parent, false);
            Color color = ColorOf(kind);
            if (kind == PickupKind.Nitro)
            {
                var bottle = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                bottle.transform.SetParent(root.transform, false);
                bottle.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                bottle.transform.localScale = new Vector3(0.7f, 0.6f, 0.7f);
                CombatArt.Paint(bottle, color, false, theme);
            }
            else
            {
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.transform.SetParent(root.transform, false);
                box.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                box.transform.localScale = kind == PickupKind.Ammo ? new Vector3(1.3f, 0.8f, 0.9f) : new Vector3(1.05f, 1f, 1.05f);
                CombatArt.Paint(box, Color.Lerp(color, Color.white, kind == PickupKind.Repair ? 0.75f : 0f), false, theme);
            }
            var band = GameObject.CreatePrimitive(kind == PickupKind.Repair ? PrimitiveType.Cube : PrimitiveType.Cylinder);
            band.transform.SetParent(root.transform, false);
            band.transform.localPosition = new Vector3(0f, kind == PickupKind.Nitro ? 1.25f : 1.02f, 0f);
            band.transform.localScale = kind == PickupKind.Repair ? new Vector3(0.9f, 0.06f, 0.3f) : new Vector3(0.6f, 0.04f, 0.6f);
            CombatArt.Paint(band, color * 2.2f, true, theme);
            if (kind == PickupKind.Repair)
            {
                var cross = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cross.transform.SetParent(root.transform, false);
                cross.transform.localPosition = new Vector3(0f, 1.02f, 0f);
                cross.transform.localScale = new Vector3(0.3f, 0.06f, 0.9f);
                CombatArt.Paint(cross, color * 2.2f, true, theme);
            }
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                DestroyImmediate(collider);
            }
            return root;
        }
    }
}
