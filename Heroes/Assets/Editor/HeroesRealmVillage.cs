using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>What the walls of one floor of a house are made of.</summary>
    internal enum Facade
    {
        /// <summary>Whitewashed plaster between timber trims.</summary>
        Plaster,
        /// <summary>Plaster over a footing of brick.</summary>
        Footed,
        /// <summary>Plaster crossed by a grid of timbers (the windows keep their plaster).</summary>
        Timbered,
        /// <summary>Rough stone of uneven bricks.</summary>
        Stone,
        /// <summary>No walls: timber posts on the corners, under a roof or around a lookout.</summary>
        Open
    }

    /// <summary>What covers a house.</summary>
    internal enum Roofing
    {
        /// <summary>A gable roof of round tiles with its gable ends.</summary>
        Tiles,
        /// <summary>A gable roof of planks.</summary>
        Planks,
        /// <summary>The pointed tiled spire of a tower.</summary>
        Spire,
        /// <summary>A flat top of flagstones behind a parapet of merlons: a castle's tower.</summary>
        Battlements,
        /// <summary>Nothing: a floor something else stands on.</summary>
        None
    }

    /// <summary>Sides of a house, as flags: which are windowed, which have shutters.</summary>
    [System.Flags]
    internal enum Sides
    {
        None = 0,
        /// <summary>The side toward -z, which the map's camera looks at.</summary>
        Front = 1,
        Right = 2,
        /// <summary>The side toward +z, which the portraits and the title look at.</summary>
        Back = 4,
        Left = 8,
        All = 15
    }

    /// <summary>How a house of the village kit is put together.</summary>
    internal sealed class Plan
    {
        /// <summary>Modules (2 m) across, along x.</summary>
        public int Wide = 2;
        /// <summary>Modules (2 m) deep, along z.</summary>
        public int Deep = 2;
        /// <summary>The facade of each floor, from the ground up.</summary>
        public Facade[] Floors = { Facade.Footed, Facade.Timbered };
        public Roofing Roof = Roofing.Tiles;
        /// <summary>The ridge runs along x (the gables face the sides) instead of along z (they face front and back).</summary>
        public bool RidgeAcross;
        /// <summary>The round tiles in other colours (slate, an owner's colour), or null for the kit's own red.</summary>
        public Material Tiles;
        /// <summary>The module of the front (-z) the door is in, or -1 for none.</summary>
        public int Door;
        /// <summary>The sides with windows; the other modules are plain walls, the cheapest pieces of the kit.</summary>
        public Sides Windows = Sides.Front | Sides.Back;
        /// <summary>The lowest floor with windows (a keep shows none on its ground floor).</summary>
        public int Sill;
        /// <summary>The sides whose windows have shutters beside them.</summary>
        public Sides Shutters = Sides.Front;
        /// <summary>Round windows among the square ones (they cost two to three times the triangles).</summary>
        public bool Fancy = true;
        /// <summary>Columns of stone on the corners of the floors that are not timbered.</summary>
        public bool Quoins = true;
        /// <summary>The glass of the windows in another material (a glow), or null for the kit's.</summary>
        public Material Glass;
        public bool Chimney;
        /// <summary>Vines hanging down the walls of the ground floor.</summary>
        public bool Vines;
        /// <summary>The floor (counted from the ground) with a balcony along its front, or -1 for none.</summary>
        public int Balcony = -1;
        /// <summary>Railings around the open floors, at the line of the walls, with planks to stand on.</summary>
        public bool Railings;
        /// <summary>The spire this much larger than the footprint asks (a spire wider than its tower).</summary>
        public float SpireScale = 1f;
        /// <summary>Picks the windows and doors: the same seed builds the same house.</summary>
        public int Seed;
        /// <summary>Every material under this tint (clear for none): the grey of a town nobody holds.</summary>
        public Color Tint;
    }

    /// <summary>
    /// Houses, towers and walls put together from the modular pieces of the village kit (Quaternius Medieval Village
    /// MegaKit), and the helpers that place its pieces and the props of the other kits. The kit is built on a grid of
    /// 2 m: a wall is one module wide and a floor 3 m high, its outer face toward +z as the file stands; a corner caps
    /// the joint of two walls; the roofs come sized for footprints with gable ends to match. A house is put together at
    /// the kit's own size with its footprint centred on its origin and its front toward -z (the map's camera), under a
    /// holder that scales the whole of it, so its pieces stay joined however small it is drawn.
    /// </summary>
    internal static class HeroesRealmVillage
    {
        public const string Kit = HeroesKitArt.Models + "/Village";
        public const string Props = HeroesKitArt.Models + "/Props";
        public const string Nature = HeroesKitArt.Models + "/Nature";

        /// <summary>How high a floor of the kit is; the walls of the top floor stand a beam higher.</summary>
        public const float Storey = 3f;

        private static HeroesThemeSpec Spec => HeroesThemeSpec.Current;

        // ------------------------------------------------------------------ placing pieces

        /// <summary>
        /// Adds a piece of the village kit (or any model, by its path under Art/Models) under <paramref name="parent"/>
        /// with its own origin at <paramref name="at"/>, turned by <paramref name="yaw"/> on top of the turn its file
        /// carries, dressed in the kit's materials.
        /// </summary>
        public static GameObject Put(Transform parent, string piece, Vector3 at, float yaw, Color tint = default)
        {
            string relative = piece.Contains("/") ? piece : $"{Kit}/{piece}";
            GameObject model = HeroesArtBuilder.Model(relative);
            if (model == null)
            {
                return null;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            instance.transform.localScale = model.transform.localScale;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * model.transform.localRotation;
            instance.transform.localPosition = at;
            HeroesArtBuilder.Dress(instance, relative, tint);
            return instance;
        }

        /// <summary>
        /// Adds a model standing on the ground: its middle over <paramref name="at"/>, its lowest point at the height of
        /// <paramref name="at"/>, turned by <paramref name="yaw"/>, scaled to <paramref name="height"/> when it is given
        /// (else by <paramref name="scale"/>).
        /// </summary>
        public static GameObject Prop(Transform parent, string relative, Vector3 at, float yaw = 0f, float height = 0f, float scale = 1f, Color tint = default)
        {
            return Prop(parent, relative, at, Quaternion.Euler(0f, yaw, 0f), Vector3.one * scale, height, tint);
        }

        /// <summary>
        /// Adds a model standing on the ground at <paramref name="at"/> (its middle over it, its lowest point at its
        /// height), stretched by <paramref name="stretch"/> along its own axes as it stands upright in its file (x across,
        /// y up, z along) and then turned by <paramref name="turn"/>, scaled as a whole to <paramref name="height"/> when it
        /// is given. Returns the holder that carries the stretch and the turn.
        /// </summary>
        public static GameObject Prop(Transform parent, string relative, Vector3 at, Quaternion turn, Vector3 stretch, float height = 0f, Color tint = default)
        {
            var holder = new GameObject(System.IO.Path.GetFileName(relative)).transform;
            holder.SetParent(parent, false);
            holder.localRotation = turn;
            holder.localScale = stretch;
            GameObject piece = Put(holder, relative, Vector3.zero, 0f, tint);
            if (piece == null)
            {
                Object.DestroyImmediate(holder.gameObject);
                return null;
            }
            Bounds bounds = LocalBounds(parent, holder.gameObject);
            if (height > 0f && bounds.size.y > 0.0001f)
            {
                holder.localScale *= height / bounds.size.y;
                bounds = LocalBounds(parent, holder.gameObject);
            }
            holder.localPosition = at - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            return holder.gameObject;
        }

        /// <summary>
        /// Readies a built object to be saved: the models in it become plain parts and every part gets a name of its own.
        /// Nested model instances and parts of one name are matched to the saved prefab's in a different order on every
        /// rebuild, which rewrites a prefab that has not changed.
        /// </summary>
        public static void Settle(GameObject root)
        {
            Transform[] parts = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform part in parts)
            {
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(part.gameObject))
                {
                    PrefabUtility.UnpackPrefabInstance(part.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }
            }
            var seen = new Dictionary<string, int>();
            foreach (Transform part in parts)
            {
                seen[part.name] = seen.TryGetValue(part.name, out int count) ? count + 1 : 1;
                if (seen[part.name] > 1)
                {
                    part.name = $"{part.name} ({seen[part.name]})";
                }
            }
        }

        /// <summary>The bounds of <paramref name="piece"/>'s renderers in the space of <paramref name="parent"/>.</summary>
        public static Bounds LocalBounds(Transform parent, GameObject piece)
        {
            Bounds world = HeroesArtBuilder.BoundsOf(piece);
            if (parent == null)
            {
                return world;
            }
            var local = new Bounds(parent.InverseTransformPoint(world.center), Vector3.zero);
            Vector3 min = world.min;
            Vector3 max = world.max;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
                local.Encapsulate(parent.InverseTransformPoint(corner));
            }
            return local;
        }

        /// <summary>An empty holder under <paramref name="parent"/>, where a building is put together before it is scaled.</summary>
        public static Transform Holder(Transform parent, string name, Vector3 at, float yaw, float scale)
        {
            var holder = new GameObject(name).transform;
            holder.SetParent(parent, false);
            holder.localPosition = at;
            holder.localRotation = Quaternion.Euler(0f, yaw, 0f);
            holder.localScale = Vector3.one * scale;
            return holder;
        }

        /// <summary>
        /// Gives every slot of the renderers under <paramref name="root"/> that wears the kit material
        /// <paramref name="name"/>, tinted or not (a tinted copy is named after it with the tint's colour), another one.
        /// </summary>
        public static void Swap(GameObject root, string name, Material material)
        {
            if (root == null || material == null)
            {
                return;
            }
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    string worn = materials[i] != null ? HeroesKitArt.BaseName(materials[i].name) : "";
                    bool tinted = worn.Length == name.Length + 7 && worn.StartsWith(name + "_", System.StringComparison.Ordinal)
                                  && ColorUtility.TryParseHtmlString("#" + worn.Substring(name.Length + 1), out _);
                    if (worn == name || tinted)
                    {
                        materials[i] = material;
                        changed = true;
                    }
                }
                if (changed)
                {
                    renderer.sharedMaterials = materials;
                }
            }
        }

        /// <summary>
        /// Scales a placed model down, about its foot, until it is no wider or deeper than <paramref name="widest"/> (an
        /// item lying flat, which its height alone would have made huge).
        /// </summary>
        public static void Fit(Transform parent, GameObject piece, float widest)
        {
            if (piece == null)
            {
                return;
            }
            Bounds bounds = LocalBounds(parent, piece);
            float wide = Mathf.Max(bounds.size.x, bounds.size.z);
            if (wide <= widest || wide < 0.0001f)
            {
                return;
            }
            var foot = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            float shrink = widest / wide;
            Transform t = piece.transform;
            t.localScale *= shrink;
            t.localPosition = foot + (t.localPosition - foot) * shrink;
        }

        /// <summary>Gives every slot of the renderers under <paramref name="root"/> one material.</summary>
        public static void Paint(GameObject root, Material material)
        {
            if (root == null || material == null)
            {
                return;
            }
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
            }
        }

        /// <summary>
        /// Pieces set end to end from <paramref name="from"/> to <paramref name="to"/> (a fence, a row of railings), each
        /// <paramref name="length"/> long along its own x, turned to run along the line.
        /// </summary>
        public static void Line(Transform parent, string piece, Vector3 from, Vector3 to, float length, Color tint = default)
        {
            Vector3 run = to - from;
            float distance = new Vector2(run.x, run.z).magnitude;
            int count = Mathf.Max(1, Mathf.RoundToInt(distance / length));
            float yaw = Mathf.Atan2(-run.z, run.x) * Mathf.Rad2Deg;
            for (int i = 0; i < count; i++)
            {
                Vector3 at = Vector3.Lerp(from, to, (i + 0.5f) / count);
                Prop(parent, piece, at, Quaternion.Euler(0f, yaw, 0f), new Vector3(distance / count / length, 1f, 1f), 0f, tint);
            }
        }

        // ------------------------------------------------------------------ materials

        /// <summary>
        /// The round tiles of the roofs dyed toward <paramref name="color"/>: the fired red of every tile takes its hue and
        /// half its saturation, and keeps its own light and shade, so a blue roof still reads as old clay tiles.
        /// </summary>
        public static Material Tiles(Color color, string tag)
        {
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            return HeroesKitArt.Repainted("Village", "MI_RoundTiles", -1, tag, pixel =>
            {
                Color.RGBToHSV(pixel, out float h, out float s, out float v);
                bool clay = (h >= 0.9f || h <= 0.16f) && s >= 0.16f;
                if (!clay)
                {
                    return pixel;
                }
                float lean = Mathf.Clamp01((s - 0.16f) / 0.12f);
                Color dyed = Color.HSVToRGB(hue, Mathf.Lerp(s, saturation, 0.45f) * 0.84f, Mathf.Clamp01(v * Mathf.Lerp(1f, value / 0.85f, 0.3f) * 0.94f));
                return Color.Lerp(pixel, dyed, lean);
            });
        }

        /// <summary>The round tiles graded to slate (the cold roofs of the dead, the bare ones of a town nobody holds).</summary>
        public static Material Slate(Color cast, float value, string tag)
        {
            return HeroesKitArt.Repainted("Village", "MI_RoundTiles", -1, tag, HeroesKitArt.Grade(0.2f, value, cast, 0.5f));
        }

        /// <summary>
        /// Cloth dyed <paramref name="color"/> for banners and awnings: the colour itself, unmuddied by the trim sheet the
        /// props kit paints its cloth from (whose patches would darken it), the sheet's folds kept in its normals, faintly
        /// lit from within like the team colours of a strategy game.
        /// </summary>
        public static Material Cloth(Color color)
        {
            var folds = HeroesAssets.Load<Texture2D>($"{HeroesKitArt.Folder}/Props/Textures/T_Trim_Cloth_Normal.png");
            return HeroesAssets.Material($"{Spec.Generated}/Kits/Cloth_{ColorUtility.ToHtmlStringRGB(color)}.mat", Shader.Find("Universal Render Pipeline/Lit"), m =>
            {
                m.SetColor("_BaseColor", color);
                m.SetTexture("_BumpMap", folds);
                m.SetFloat("_BumpScale", 0.8f);
                if (folds != null)
                {
                    m.EnableKeyword("_NORMALMAP");
                }
                m.SetFloat("_Metallic", 0f);
                m.SetFloat("_Smoothness", 0.22f);
                m.SetFloat("_Cull", 0f);
                m.doubleSidedGI = true;
                // A little light of its own, so the colours still read on the shaded side the map's camera looks at.
                m.SetColor("_EmissionColor", color * 0.28f);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            });
        }

        /// <summary>A glossy stone that glows from within in its own colour: crystals, gems, a star.</summary>
        public static Material Gem(Color color, float glow, string tag)
        {
            return HeroesAssets.Material($"{Spec.Generated}/Kits/Glow/Gem_{tag}.mat", Shader.Find("Universal Render Pipeline/Lit"), m =>
            {
                m.SetColor("_BaseColor", color);
                m.SetFloat("_Metallic", 0.1f);
                m.SetFloat("_Smoothness", 0.86f);
                m.SetColor("_EmissionColor", color * glow);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            });
        }

        /// <summary>Plain woven canvas of one colour: a tent's.</summary>
        public static Material Canvas(Color color, string tag)
        {
            return HeroesAssets.Material($"{Spec.Generated}/Kits/Canvas_{tag}.mat", Shader.Find("Universal Render Pipeline/Lit"), m =>
            {
                m.SetColor("_BaseColor", color);
                m.SetFloat("_Metallic", 0f);
                m.SetFloat("_Smoothness", 0.12f);
                m.SetFloat("_Cull", 0f);
                m.doubleSidedGI = true;
            });
        }

        /// <summary>A polished metal (quicksilver in a pot), mirroring the sky.</summary>
        public static Material Metal(Color color, string tag)
        {
            return HeroesAssets.Material($"{Spec.Generated}/Kits/Glow/Metal_{tag}.mat", Shader.Find("Universal Render Pipeline/Lit"), m =>
            {
                m.SetColor("_BaseColor", color);
                m.SetFloat("_Metallic", 0.92f);
                m.SetFloat("_Smoothness", 0.9f);
            });
        }

        /// <summary>The dark inside a doorway with nothing built behind it: a mine's adit.</summary>
        public static Material Darkness()
        {
            return HeroesAssets.Material($"{Spec.Generated}/Kits/Darkness.mat", Shader.Find("Universal Render Pipeline/Lit"), m =>
            {
                m.SetColor("_BaseColor", new Color(0.035f, 0.03f, 0.03f));
                m.SetFloat("_Metallic", 0f);
                m.SetFloat("_Smoothness", 0f);
            });
        }

        /// <summary>Clear water under the sky: a basin's, a stream's.</summary>
        public static Material Water()
        {
            return HeroesAssets.Material($"{Spec.Generated}/Kits/Water.mat", Shader.Find("Universal Render Pipeline/Lit"), m =>
            {
                m.SetColor("_BaseColor", new Color(0.16f, 0.42f, 0.56f, 0.82f));
                m.SetFloat("_Metallic", 0.05f);
                m.SetFloat("_Smoothness", 0.95f);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_ZWrite", 0f);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            });
        }

        /// <summary>A flat disc or slab of <paramref name="material"/> (water in a basin), made of a primitive with no collider.</summary>
        public static GameObject Slab(Transform parent, PrimitiveType shape, Vector3 at, Vector3 size, Material material)
        {
            GameObject slab = GameObject.CreatePrimitive(shape);
            Object.DestroyImmediate(slab.GetComponent<Collider>());
            slab.name = "Water";
            slab.transform.SetParent(parent, false);
            slab.transform.localPosition = at;
            slab.transform.localScale = size;
            slab.GetComponent<Renderer>().sharedMaterial = material;
            slab.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return slab;
        }

        // ------------------------------------------------------------------ dressing

        /// <summary>
        /// A banner on a timber pole <paramref name="height"/> high, its cloth dyed (the colours of whoever holds the place),
        /// as long as the pole allows and <paramref name="wide"/> times as wide as the kit's, hanging from its top toward -z.
        /// </summary>
        public static Transform Standard(Transform parent, Vector3 at, float yaw, Material cloth, float height = 3.4f, Color tint = default, float wide = 1f)
        {
            Transform standard = Holder(parent, "Standard", at, yaw, 1f);
            float thick = Mathf.Clamp(height * 0.045f, 0.08f, 0.18f) / 0.22f;
            Prop(standard, $"{Kit}/Corner_Exterior_Wood", Vector3.zero, Quaternion.identity, new Vector3(thick, height / 3f, thick), 0f, tint);
            float length = Mathf.Min(3.6f, height * 0.6f);
            GameObject banner = Put(standard, $"{Props}/Banner_1_Cloth", new Vector3(0f, height - 0.08f, -0.06f - thick * 0.11f), 0f, tint);
            if (banner != null)
            {
                Vector3 s = banner.transform.localScale;
                float across = Mathf.Clamp(length / 2.25f * 1.3f, 0.5f, 1f) * wide;
                banner.transform.localScale = new Vector3(s.x * across, s.y, s.z * length / 2.25f);
            }
            Swap(banner, "MI_Banner", cloth);
            return standard;
        }

        /// <summary>A long banner hung flat on a wall whose outer face is at <paramref name="at"/>, facing out along <paramref name="yaw"/>.</summary>
        public static void WallBanner(Transform parent, Vector3 at, float yaw, Material cloth, float length = 2.25f)
        {
            GameObject banner = Put(parent, $"{Props}/Banner_1_Cloth", at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, 0.06f), yaw);
            if (banner != null)
            {
                banner.transform.localScale = new Vector3(banner.transform.localScale.x * 1.15f, banner.transform.localScale.y, banner.transform.localScale.z * length / 2.25f);
            }
            Swap(banner, "MI_Banner", cloth);
        }

        /// <summary>
        /// Merlons from <paramref name="from"/> to <paramref name="to"/>, ends included: blocks of stone (the kit's stair
        /// platform drawn small, a few triangles each) along the top of a wall, the gaps between them as wide as they are;
        /// <paramref name="inner"/> leaves out the end ones (a side whose corners another row has).
        /// </summary>
        public static void Merlons(Transform parent, Vector3 from, Vector3 to, int count, Color tint = default, bool inner = false)
        {
            Vector3 run = to - from;
            float yaw = Mathf.Atan2(-run.z, run.x) * Mathf.Rad2Deg;
            int stones = Mathf.Max(2, count);
            for (int i = inner ? 1 : 0; i < (inner ? stones - 1 : stones); i++)
            {
                if (i % 2 == 1 && stones > 2)
                {
                    continue;
                }
                Vector3 at = Vector3.Lerp(from, to, i / (float)(stones - 1));
                Prop(parent, $"{Kit}/Stairs_Exterior_Platform", at, Quaternion.Euler(0f, yaw, 0f), new Vector3(0.24f, 0.42f, 0.2f), 0f, tint);
            }
        }

        // ------------------------------------------------------------------ houses

        /// <summary>The sides of a footprint.</summary>
        private enum Side
        {
            Front,
            Right,
            Back,
            Left
        }

        /// <summary>The turn that makes a wall's outer face (+z in its file) face out of <paramref name="side"/>.</summary>
        private static float YawOf(Side side)
        {
            switch (side)
            {
                case Side.Front:
                    return 180f;
                case Side.Right:
                    return 90f;
                case Side.Left:
                    return 270f;
                default:
                    return 0f;
            }
        }

        /// <summary>The middle of module <paramref name="i"/> of a side of a footprint <paramref name="wide"/> by <paramref name="deep"/> modules.</summary>
        private static Vector3 ModuleAt(Side side, int i, int wide, int deep)
        {
            switch (side)
            {
                case Side.Front:
                    return new Vector3(-wide + 1 + 2 * i, 0f, -deep);
                case Side.Back:
                    return new Vector3(wide - 1 - 2 * i, 0f, deep);
                case Side.Right:
                    return new Vector3(wide, 0f, -deep + 1 + 2 * i);
                default:
                    return new Vector3(-wide, 0f, deep - 1 - 2 * i);
            }
        }

        /// <summary>
        /// Builds a house of <paramref name="plan"/> under <paramref name="parent"/>, its footprint centred on
        /// <paramref name="at"/>, turned by <paramref name="yaw"/> and scaled by <paramref name="scale"/>. Returns its holder.
        /// </summary>
        public static Transform House(Transform parent, string name, Plan plan, Vector3 at, float yaw, float scale = 1f)
        {
            Transform house = Holder(parent, name, at, yaw, scale);
            int floors = plan.Floors.Length;
            for (int floor = 0; floor < floors; floor++)
            {
                Facade facade = plan.Floors[floor];
                float y = floor * Storey;
                if (facade == Facade.Open)
                {
                    Posts(house, plan, y);
                    continue;
                }
                foreach (Side side in new[] { Side.Front, Side.Right, Side.Back, Side.Left })
                {
                    int count = side == Side.Front || side == Side.Back ? plan.Wide : plan.Deep;
                    for (int i = 0; i < count; i++)
                    {
                        Vector3 module = ModuleAt(side, i, plan.Wide, plan.Deep) + Vector3.up * y;
                        bool door = floor == 0 && side == Side.Front && i == plan.Door;
                        Sides flag = (Sides)(1 << (int)side);
                        Opening opening = door ? Opening.Door : (plan.Windows & flag) != 0 && floor >= plan.Sill ? Pick(plan, floor, side, i) : Opening.None;
                        Wall(house, facade, opening, module, YawOf(side), plan, (plan.Shutters & flag) != 0, Hash(plan.Seed, floor, (int)side, i + 11));
                        if (floor == plan.Balcony && side == Side.Front)
                        {
                            Balcony(house, module, plan, i, count);
                        }
                    }
                }
            }
            Corners(house, plan);
            if (plan.Vines)
            {
                Vines(house, plan);
            }
            Cover(house, plan, floors * Storey);
            if (plan.Glass != null)
            {
                Swap(house.gameObject, "MI_WindowGlass", plan.Glass);
            }
            return house;
        }

        /// <summary>What a module of wall has in it.</summary>
        private enum Opening
        {
            None,
            Door,
            WideFlat,
            WideRound,
            ThinRound
        }

        private static Opening Pick(Plan plan, int floor, Side side, int i)
        {
            if (!plan.Fancy)
            {
                return Opening.WideFlat;
            }
            int roll = (int)(Hash(plan.Seed, floor, (int)side, i) % 100);
            return roll < 55 ? Opening.WideFlat : roll < 82 ? Opening.WideRound : Opening.ThinRound;
        }

        /// <summary>A number from the seed and the place, the same every build.</summary>
        public static uint Hash(int a, int b, int c, int d)
        {
            unchecked
            {
                uint h = 2166136261u;
                foreach (int value in new[] { a, b, c, d })
                {
                    h = (h ^ (uint)value) * 16777619u;
                    h ^= h >> 13;
                    h *= 0x5bd1e995u;
                    h ^= h >> 15;
                }
                return h;
            }
        }

        private static void Wall(Transform house, Facade facade, Opening opening, Vector3 at, float yaw, Plan plan, bool shuttered, uint hash)
        {
            string stem = facade == Facade.Stone ? "Wall_UnevenBrick_" : "Wall_Plaster_";
            string wall;
            switch (opening)
            {
                case Opening.Door:
                    wall = stem + "Door_Round";
                    break;
                case Opening.WideFlat:
                    wall = stem + "Window_Wide_Flat";
                    break;
                case Opening.WideRound:
                    wall = stem + "Window_Wide_Round";
                    break;
                case Opening.ThinRound:
                    wall = stem + "Window_Thin_Round";
                    break;
                default:
                    wall = facade == Facade.Stone ? "Wall_UnevenBrick_Straight"
                        : facade == Facade.Timbered ? "Wall_Plaster_WoodGrid"
                        : facade == Facade.Footed ? "Wall_Plaster_Straight_Base"
                        : "Wall_Plaster_Straight";
                    break;
            }
            Put(house, wall, at, yaw, plan.Tint);
            Quaternion turn = Quaternion.Euler(0f, yaw, 0f);
            string shutters = null;
            switch (opening)
            {
                case Opening.Door:
                    Put(house, "Door_1_Round", at + turn * new Vector3(0.53f, 0f, 0f), yaw, plan.Tint);
                    break;
                case Opening.WideFlat:
                    Put(house, "Window_Wide_Flat1", at, yaw, plan.Tint);
                    shutters = "WindowShutters_Wide_Flat";
                    break;
                case Opening.WideRound:
                    Put(house, "Window_Wide_Round1", at, yaw, plan.Tint);
                    shutters = "WindowShutters_Wide_Round";
                    break;
                case Opening.ThinRound:
                    Put(house, "Window_Thin_Round1", at, yaw, plan.Tint);
                    shutters = "WindowShutters_Thin_Round";
                    break;
            }
            if (shutters != null && shuttered)
            {
                Put(house, shutters + (hash % 4 == 0 ? "_Closed" : "_Open"), at, yaw, plan.Tint);
            }
        }

        /// <summary>
        /// The posts and quoins on the four corners (the file's corner wraps the +x +z one): a timber post on each timbered
        /// floor, and one slim column of stone (the kit's chimney drawn thin, a fraction of the triangles of its carved
        /// corner stones) up each run of floors that are not, the top one a beam higher.
        /// </summary>
        private static void Corners(Transform house, Plan plan)
        {
            int floors = plan.Floors.Length;
            int from = 0;
            while (from < floors)
            {
                Facade facade = plan.Floors[from];
                int to = from + 1;
                bool stone = facade != Facade.Timbered && facade != Facade.Open;
                while (stone && to < floors && plan.Floors[to] != Facade.Timbered && plan.Floors[to] != Facade.Open)
                {
                    to++;
                }
                for (int i = 0; i < 4 && facade != Facade.Open && (stone ? plan.Quoins : true); i++)
                {
                    float x = i == 0 || i == 1 ? plan.Wide : -plan.Wide;
                    float z = i == 0 || i == 3 ? plan.Deep : -plan.Deep;
                    if (!stone)
                    {
                        Put(house, "Corner_Exterior_Wood", new Vector3(x, from * Storey, z), i * 90f, plan.Tint);
                        continue;
                    }
                    float height = (to - from) * Storey + (to == floors ? 0.12f : 0f);
                    var at = new Vector3(x + Mathf.Sign(x) * 0.04f, from * Storey, z + Mathf.Sign(z) * 0.04f);
                    Prop(house, $"{Kit}/Prop_Chimney2", at, Quaternion.Euler(0f, i * 90f, 0f), new Vector3(0.38f, height / 3f, 0.38f), 0f, plan.Tint);
                }
                from = to;
            }
        }

        /// <summary>An open floor: timber posts on its corners and between its modules, railed and planked when asked.</summary>
        private static void Posts(Transform house, Plan plan, float y)
        {
            for (int x = -plan.Wide; x <= plan.Wide; x += 2)
            {
                for (int z = -plan.Deep; z <= plan.Deep; z += 2)
                {
                    bool edge = x == -plan.Wide || x == plan.Wide || z == -plan.Deep || z == plan.Deep;
                    if (edge)
                    {
                        Put(house, "Corner_ExteriorWide_Wood", new Vector3(x, y, z), 0f, plan.Tint);
                    }
                }
            }
            if (!plan.Railings)
            {
                return;
            }
            for (int ix = 0; ix < plan.Wide; ix++)
            {
                for (int iz = 0; iz < plan.Deep; iz++)
                {
                    Put(house, "Floor_WoodDark", new Vector3(-plan.Wide + 1 + 2 * ix, y + 0.02f, -plan.Deep + 1 + 2 * iz), 0f, plan.Tint);
                }
            }
            foreach (Side side in new[] { Side.Front, Side.Right, Side.Back, Side.Left })
            {
                int count = side == Side.Front || side == Side.Back ? plan.Wide : plan.Deep;
                for (int i = 0; i < count; i++)
                {
                    float yaw = YawOf(side);
                    Vector3 module = ModuleAt(side, i, plan.Wide, plan.Deep) + Vector3.up * y;
                    // The railing stands a module out from where it is placed; it is drawn back to the line of the walls.
                    Put(house, "Balcony_Cross_Straight", module - Quaternion.Euler(0f, yaw, 0f) * Vector3.forward, yaw, plan.Tint);
                }
            }
        }

        /// <summary>A balcony a module deep along one module of the front, on brackets.</summary>
        private static void Balcony(Transform house, Vector3 module, Plan plan, int i, int count)
        {
            Put(house, "Floor_WoodDark_Half3", module + Vector3.up * 0.02f, 0f, plan.Tint);
            Put(house, "Balcony_Cross_Straight", module, 180f, plan.Tint);
            // The ends are railed too, with railings drawn in to the balcony's depth.
            foreach (int end in new[] { -1, 1 })
            {
                if ((end < 0 && i == 0) || (end > 0 && i == count - 1))
                {
                    Prop(house, $"{Kit}/Balcony_Cross_Straight", module + new Vector3(end * 0.95f, -0.11f, -0.55f), Quaternion.Euler(0f, 90f, 0f),
                        new Vector3(0.5f, 1f, 1f), 0f, plan.Tint);
                }
            }
            Put(house, "Roof_Support2", module + new Vector3(-0.7f, 0f, 0f), 180f, plan.Tint);
            Put(house, "Roof_Support2", module + new Vector3(0.7f, 0f, 0f), 180f, plan.Tint);
        }

        private static readonly string[] VinePieces = { "Prop_Vine1", "Prop_Vine2", "Prop_Vine4" };

        /// <summary>Two vines climbing down the ground floor, on modules the seed picks.</summary>
        private static void Vines(Transform house, Plan plan)
        {
            for (int n = 0; n < 2; n++)
            {
                uint hash = Hash(plan.Seed, 77, n, 5);
                var side = (Side)(hash % 4);
                int count = side == Side.Front || side == Side.Back ? plan.Wide : plan.Deep;
                int i = (int)(hash / 4 % (uint)count);
                if (side == Side.Front && i == plan.Door)
                {
                    side = Side.Back;
                }
                float yaw = YawOf(side);
                Vector3 module = ModuleAt(side, i, plan.Wide, plan.Deep);
                Vector3 shift = Quaternion.Euler(0f, yaw, 0f) * new Vector3(n == 0 ? -0.55f : 0.55f, 0f, 0.1f);
                Put(house, VinePieces[hash / 16 % (uint)VinePieces.Length], module + shift + Vector3.up * 2.75f, yaw, plan.Tint);
            }
        }

        /// <summary>Roofs of round tiles that come in the kit, by span and length, with where their middle is along the ridge.</summary>
        private static readonly Dictionary<string, (string piece, float shift)> TiledRoofs = new Dictionary<string, (string, float)>
        {
            ["2x2"] = ("Roof_RoundTile_2x1", -0.5f),
            ["2x4"] = ("Roof_2x4_RoundTile", 0f),
            ["4x4"] = ("Roof_RoundTiles_4x4", 0f),
            ["4x6"] = ("Roof_RoundTiles_4x6", 0f),
            ["4x8"] = ("Roof_RoundTiles_4x8", 0f),
            ["6x4"] = ("Roof_RoundTiles_6x4", -2.35f),
            ["6x6"] = ("Roof_RoundTiles_6x6", 0f),
            ["6x8"] = ("Roof_RoundTiles_6x8", 0f),
            ["6x10"] = ("Roof_RoundTiles_6x10", 0f),
            ["8x8"] = ("Roof_RoundTiles_8x8", 0f),
            ["8x10"] = ("Roof_RoundTiles_8x10", 0f)
        };

        /// <summary>The roof of a house whose walls end at <paramref name="top"/>.</summary>
        private static void Cover(Transform house, Plan plan, float top)
        {
            switch (plan.Roof)
            {
                case Roofing.Tiles:
                {
                    int span = (plan.RidgeAcross ? plan.Deep : plan.Wide) * 2;
                    int length = (plan.RidgeAcross ? plan.Wide : plan.Deep) * 2;
                    float yaw = plan.RidgeAcross ? 90f : 0f;
                    Vector3 along = plan.RidgeAcross ? Vector3.right : Vector3.forward;
                    Vector3 across = plan.RidgeAcross ? Vector3.forward : Vector3.right;
                    if (TiledRoofs.TryGetValue($"{span}x{length}", out (string piece, float shift) roof))
                    {
                        GameObject tiles = Put(house, roof.piece, new Vector3(0f, top, 0f) + along * roof.shift, yaw, plan.Tint);
                        if (plan.Tiles != null)
                        {
                            Swap(tiles, "MI_RoundTiles", plan.Tiles);
                        }
                    }
                    float half = length * 0.5f;
                    string gable = $"Roof_Front_Brick{span}";
                    Put(house, gable, new Vector3(0f, top, 0f) + along * half, plan.RidgeAcross ? 90f : 0f, plan.Tint);
                    Put(house, gable, new Vector3(0f, top, 0f) - along * half, plan.RidgeAcross ? 270f : 180f, plan.Tint);
                    if (plan.Chimney)
                    {
                        Put(house, "Prop_Chimney", new Vector3(0f, top - 0.3f, 0f) + across * (span * 0.5f - 1.1f) + along * (half - 1.2f), 0f, plan.Tint);
                    }
                    break;
                }
                case Roofing.Spire:
                {
                    GameObject spire = Put(house, "Roof_Tower_RoundTiles", new Vector3(0f, top, 0f), 0f, plan.Tint);
                    if (spire != null)
                    {
                        spire.transform.localScale *= plan.Wide * 0.5f * plan.SpireScale;
                        if (plan.Tiles != null)
                        {
                            Swap(spire, "MI_RoundTiles", plan.Tiles);
                        }
                    }
                    break;
                }
                case Roofing.Planks:
                    Planks(house, plan, top);
                    break;
                case Roofing.Battlements:
                {
                    float y = top + 0.12f;
                    for (int ix = 0; ix < plan.Wide; ix++)
                    {
                        for (int iz = 0; iz < plan.Deep; iz++)
                        {
                            Put(house, "Floor_UnevenBrick", new Vector3(-plan.Wide + 1 + 2 * ix, y, -plan.Deep + 1 + 2 * iz), 0f, plan.Tint);
                        }
                    }
                    float w = plan.Wide + 0.05f;
                    float d = plan.Deep + 0.05f;
                    Merlons(house, new Vector3(-w, y, -d), new Vector3(w, y, -d), plan.Wide * 4 + 1, plan.Tint);
                    Merlons(house, new Vector3(-w, y, d), new Vector3(w, y, d), plan.Wide * 4 + 1, plan.Tint);
                    Merlons(house, new Vector3(-w, y, -d), new Vector3(-w, y, d), plan.Deep * 4 + 1, plan.Tint, true);
                    Merlons(house, new Vector3(w, y, -d), new Vector3(w, y, d), plan.Deep * 4 + 1, plan.Tint, true);
                    break;
                }
            }
        }

        /// <summary>
        /// A gable roof of planks: each module of the ridge two slopes (the end ones lipped), the slopes stretched for a
        /// house deeper than one module, with plaster gables under the ends of the ridge unless the floor below is open.
        /// </summary>
        private static void Planks(Transform house, Plan plan, float top)
        {
            // The pieces slope down from a ridge along their x: turned a quarter, the ridge runs along z.
            bool alongX = plan.RidgeAcross;
            float baseYaw = alongX ? 0f : 90f;
            int modules = alongX ? plan.Wide : plan.Deep;
            int across = alongX ? plan.Deep : plan.Wide;
            Quaternion frame = Quaternion.Euler(0f, baseYaw, 0f);
            for (int i = 0; i < modules; i++)
            {
                Vector3 at = new Vector3(0f, top, 0f) + frame * new Vector3(-modules + 1 + 2 * i, 0f, 0f);
                for (int slope = 0; slope < 2; slope++)
                {
                    bool first = i == 0;
                    bool last = i == modules - 1;
                    // On the far slope the piece is turned half round, so its own left lies to the right.
                    bool lipLeft = slope == 0 ? first : last;
                    bool lipRight = slope == 0 ? last : first;
                    string piece = lipLeft && lipRight ? "Roof_Wooden_2x1" : lipLeft ? "Roof_Wooden_2x1_R" : lipRight ? "Roof_Wooden_2x1_L" : "Roof_Wooden_2x1_Center";
                    GameObject half = Put(house, piece, at, baseYaw + slope * 180f, plan.Tint);
                    if (half != null && across > 1)
                    {
                        // The file stands on its side: its own y runs along the slope's fall and its own z up.
                        Vector3 s = half.transform.localScale;
                        half.transform.localScale = new Vector3(s.x, s.y * across, s.z * across);
                    }
                }
            }
            if (plan.Floors.Length > 0 && plan.Floors[plan.Floors.Length - 1] == Facade.Open)
            {
                return;
            }
            Vector3 ridge = frame * Vector3.right;
            float reach = modules;
            foreach (int end in new[] { -1, 1 })
            {
                GameObject gable = Put(house, "Roof_Front_Brick2", new Vector3(0f, top, 0f) + ridge * (end * reach), baseYaw + (end > 0 ? 90f : 270f), plan.Tint);
                if (gable != null)
                {
                    Vector3 s = gable.transform.localScale;
                    gable.transform.localScale = new Vector3(s.x * across * 0.78f, s.y, s.z * across * 0.66f);
                }
            }
        }
    }
}
