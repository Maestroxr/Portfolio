using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// Puts the people of the kits together: an outfit of the character kit (or a bare body), the head, eyes and brows
    /// of the base body of the same sex bound to the outfit's skeleton, a hairstyle and a beard, a weapon in a hand and a
    /// shield on the other arm, and the clips of the animation library the puppets play. Every outfit, body and clip of the
    /// kits shares one rig, so a part of one file is moved onto another by the names of its bones. The clips are copies of
    /// the library's that turn the bones but leave their lengths alone (only the root and the pelvis move), so the shorter
    /// women of the kit keep their own proportions under clips recorded on a man.
    /// </summary>
    internal static class HeroesRealmCharacters
    {
        private const string Kit = HeroesKitArt.Models + "/Characters";
        private static readonly string[] Libraries = { HeroesKitArt.Models + "/Animation/UAL1_Standard", HeroesKitArt.Models + "/Animation/UAL2_Standard" };

        private static HeroesThemeSpec Spec => HeroesThemeSpec.Current;

        /// <summary>The hair of a person whose look names no colour for it.</summary>
        private static readonly Color DarkBrown = new Color(0.36f, 0.25f, 0.17f);

        /// <summary>The model name of a staff the builder turns itself: a pole of twisted bark with a glowing crystal on top.</summary>
        public const string StaffModel = "staff";

        /// <summary>What a person of the kits wears.</summary>
        internal enum Outfit
        {
            MalePeasant,
            FemalePeasant,
            MaleRanger,
            FemaleRanger,
            /// <summary>The base body itself, bare to the waist: a brawler, a berserker.</summary>
            Male,
            Female
        }

        /// <summary>A weapon or a shield, by its model, held in a hand or worn on a forearm.</summary>
        internal sealed class Gear
        {
            public string Model;
            /// <summary>How long it is, in meters.</summary>
            public float Length = 1f;
            /// <summary>Where along its length the hand closes, from its butt (0) to its tip (1).</summary>
            public float Grip = 0.12f;
            /// <summary>Turns the model end for end when its file has the tip where the butt is guessed.</summary>
            public bool Reversed;
            /// <summary>Turns it about its own length, in degrees (the edge of a blade, the face of a bow).</summary>
            public float Roll;
            public Color Tint;
            /// <summary>Worn on the forearm, facing out (a shield), rather than held in the fist.</summary>
            public bool Shield;
            /// <summary>Held upright while the person idles (a staff planted at the side, a bow), rather than across the fist.</summary>
            public bool Upright;

            public Gear(string model, float length, float grip = 0.12f)
            {
                Model = model;
                Length = length;
                Grip = grip;
            }
        }

        /// <summary>The look of one person: the outfit and its colours, the head, the hair, what the hands hold.</summary>
        internal sealed class Look
        {
            public Outfit Outfit;
            /// <summary>The outfit's alternate texture (the second peasant, the white ranger), or -1 for its first.</summary>
            public int Variant = -1;
            /// <summary>The cloth of the outfit repainted (null to keep it), filed under <see cref="ClothTag"/>.</summary>
            public Func<Color, Color> Cloth;
            public string ClothTag;
            public Color ClothTint;
            public bool Hood = true;
            public bool Pauldron = true;
            /// <summary>Hair_Long, Hair_SimpleParted, Hair_Buns, Hair_Buzzed, Hair_BuzzedFemale or null; the beard alike.</summary>
            public string Hair;
            public string Beard;
            /// <summary>The colour of the hair (the kit's is grey, to be dyed); clear for dark brown.</summary>
            public Color HairTint;
            /// <summary>For a body bare to the waist: whose trousers and boots it wears (Male_Peasant, Male_Ranger...).</summary>
            public string Trousers;
            /// <summary>The lighter of the two skins of the base body.</summary>
            public bool Light;
            /// <summary>The skin under a tint, and repainted (the bone white and ashen grey of the dead), filed under <see cref="SkinTag"/>.</summary>
            public Color SkinTint;
            public Func<Color, Color> Skin;
            public string SkinTag;
            /// <summary>Eyes that glow with this colour (the dead and the witches), clear for plain eyes.</summary>
            public Color EyeGlow;
            public Gear Right;
            public Gear Left;
        }

        // ------------------------------------------------------------------ building a person

        /// <summary>
        /// Builds a person under <paramref name="parent"/> standing on its origin, <paramref name="height"/> meters tall
        /// (0 for as the kit made it), with an Animation holding <paramref name="clips"/> (names of the library's clips; the
        /// first is played at once). Returns the person's root, which carries the Animation.
        /// </summary>
        public static GameObject Build(Transform parent, Look look, float height, params string[] clips)
        {
            bool female = look.Outfit == Outfit.FemalePeasant || look.Outfit == Outfit.FemaleRanger || look.Outfit == Outfit.Female;
            string sex = female ? "Female" : "Male";
            bool bare = look.Outfit == Outfit.Male || look.Outfit == Outfit.Female;
            string file = look.Outfit switch
            {
                Outfit.MalePeasant => "Male_Peasant",
                Outfit.FemalePeasant => "Female_Peasant",
                Outfit.MaleRanger => "Male_Ranger",
                Outfit.FemaleRanger => "Female_Ranger",
                _ => $"Superhero_{sex}_FullBody"
            };
            GameObject model = HeroesArtBuilder.Model($"{Kit}/{file}");
            if (model == null)
            {
                return null;
            }
            var person = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            PrefabUtility.UnpackPrefabInstance(person, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            person.name = file;
            Dictionary<string, Transform> bones = Bones(person);

            // The outfit's own pieces, in the kit's materials (the cloth repainted, the hood and pauldron as asked).
            foreach (Renderer renderer in person.GetComponentsInChildren<Renderer>(true).ToArray())
            {
                string name = renderer.gameObject.name;
                if ((!look.Hood && name.EndsWith("_Head_Hood", StringComparison.Ordinal)) || (!look.Pauldron && name.Contains("_Acc_Pauldron")))
                {
                    Object.DestroyImmediate(renderer.gameObject);
                    continue;
                }
                if (bare && !string.IsNullOrEmpty(look.Trousers) && renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null
                    && skinned.sharedMesh.vertexCount > 3000)
                {
                    // A body bare to the waist wears the trousers and boots of an outfit; its own legs would show through.
                    skinned.sharedMesh = Cut(skinned, $"Upper_{sex}", Legs, false);
                }
                renderer.sharedMaterials = renderer.sharedMaterials.Select(material => Dress(material, look, sex)).ToArray();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            if (bare && !string.IsNullOrEmpty(look.Trousers))
            {
                foreach (string part in new[] { "Legs", "Feet", "Feet_Boots" })
                {
                    GameObject partModel = AssetDatabase.LoadAssetAtPath<GameObject>(HeroesAssets.Path($"Art/Models/{Kit}/Parts/{look.Trousers}_{part}.fbx"));
                    if (partModel == null)
                    {
                        continue;
                    }
                    foreach (SkinnedMeshRenderer source in partModel.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        Bind(person.transform, $"{look.Trousers}_{part}", source.sharedMesh, source, bones,
                            source.sharedMaterials.Select(material => Dress(material, look, sex)).ToArray());
                    }
                }
            }

            // The head of the base body (an outfit has none of its own) with its eyes and brows, then the hair.
            if (!bare)
            {
                GameObject body = HeroesArtBuilder.Model($"{Kit}/Superhero_{sex}_FullBody");
                foreach (SkinnedMeshRenderer source in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    bool skin = source.sharedMesh != null && source.sharedMesh.vertexCount > 3000;
                    Mesh mesh = skin ? Cut(source, $"Head_{sex}", Head, true) : source.sharedMesh;
                    // Not "Head": that is the name of a bone, and two transforms of one name confuse the prefab's matching on a rebuild.
                    string part = skin ? "Face" : source.name;
                    Bind(person.transform, part, mesh, source, bones, source.sharedMaterials.Select(material => Dress(material, look, sex)).ToArray());
                }
            }
            foreach (string hair in new[] { look.Hair, look.Beard })
            {
                if (string.IsNullOrEmpty(hair))
                {
                    continue;
                }
                GameObject hairModel = HeroesArtBuilder.Model($"{Kit}/Hair/{hair}");
                if (hairModel == null)
                {
                    continue;
                }
                foreach (SkinnedMeshRenderer source in hairModel.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    Bind(person.transform, hair, source.sharedMesh, source, bones, source.sharedMaterials.Select(material => Dress(material, look, sex)).ToArray());
                }
            }

            // How tall the person stands, before anything is put in the hands (a spear over the head is no part of it).
            float standing = HeroesArtBuilder.BoundsOf(person).size.y;

            // The importer gives a legacy rig an Animation of its own (with no clips), which takes the library's.
            var animation = person.GetComponent<Animation>();
            if (animation == null)
            {
                animation = person.AddComponent<Animation>();
            }
            foreach (string clip in clips.Where(name => !string.IsNullOrEmpty(name)).Distinct())
            {
                AnimationClip made = Clip(clip);
                if (made != null && animation.GetClip(made.name) == null)
                {
                    animation.AddClip(made, made.name);
                }
            }
            AnimationClip first = clips.Length > 0 ? animation.GetClip(ClipName(clips[0])) : null;
            animation.clip = first;
            animation.playAutomatically = first != null;
            animation.cullingType = AnimationCullingType.BasedOnRenderers;

            // What the hands hold: across the fist as rigged, or upright as the person stands idle (the pose is put back).
            Transform[] skeleton = person.GetComponentsInChildren<Transform>(true);
            var rest = skeleton.Select(bone => (bone.localPosition, bone.localRotation)).ToArray();
            bool upright = (look.Right != null && look.Right.Upright) || (look.Left != null && look.Left.Upright);
            if (upright && first != null)
            {
                first.SampleAnimation(person, first.length * 0.3f);
            }
            Hold(person, bones, look.Right, false);
            Hold(person, bones, look.Left, true);
            for (int i = 0; i < skeleton.Length; i++)
            {
                skeleton[i].localPosition = rest[i].localPosition;
                skeleton[i].localRotation = rest[i].localRotation;
            }

            if (height > 0f && standing > 0.01f)
            {
                person.transform.localScale *= height / standing;
            }
            person.transform.localPosition = Vector3.zero;
            // The kit faces its people down -Z; everything of the game looks along +Z.
            person.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            return person;
        }

        /// <summary>Every transform of a person by name (the first of a name wins).</summary>
        private static Dictionary<string, Transform> Bones(GameObject person)
        {
            var bones = new Dictionary<string, Transform>();
            foreach (Transform bone in person.GetComponentsInChildren<Transform>(true))
            {
                if (!bones.ContainsKey(bone.name))
                {
                    bones[bone.name] = bone;
                }
            }
            return bones;
        }

        /// <summary>
        /// A skinned mesh of another file of the rig bound to <paramref name="person"/>'s skeleton: the same bones by name,
        /// the bind poses the mesh came with.
        /// </summary>
        private static void Bind(Transform person, string name, Mesh mesh, SkinnedMeshRenderer source, Dictionary<string, Transform> bones, Material[] materials)
        {
            if (mesh == null)
            {
                return;
            }
            var holder = new GameObject(name);
            holder.transform.SetParent(person, false);
            holder.transform.localPosition = source.transform.localPosition;
            holder.transform.localRotation = source.transform.localRotation;
            holder.transform.localScale = source.transform.localScale;
            var renderer = holder.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = mesh;
            renderer.bones = source.bones.Select(bone => bone != null && bones.TryGetValue(bone.name, out Transform found) ? found : null).ToArray();
            renderer.rootBone = source.rootBone != null && bones.TryGetValue(source.rootBone.name, out Transform root) ? root : null;
            renderer.sharedMaterials = materials;
            renderer.updateWhenOffscreen = false;
            renderer.localBounds = source.localBounds;
        }

        // ------------------------------------------------------------------ parts of a body

        private static readonly Dictionary<string, Mesh> Cuts = new Dictionary<string, Mesh>();

        /// <summary>The bones a head hangs on.</summary>
        private static readonly HashSet<string> Head = new HashSet<string> { "Head", "neck_01" };

        /// <summary>The bones of the hips and legs, which trousers cover.</summary>
        private static readonly HashSet<string> Legs = new HashSet<string>
        {
            "pelvis", "thigh_l", "thigh_r", "calf_l", "calf_r", "foot_l", "foot_r", "ball_l", "ball_r", "ball_leaf_l", "ball_leaf_r"
        };

        /// <summary>
        /// A part of a base body as a mesh of its own: the triangles whose corners all hang mostly on <paramref name="bones"/>
        /// (<paramref name="inside"/>), or mostly off them, saved among the theme's meshes as <paramref name="name"/>. The head
        /// of a body goes on an outfit (the rest would show through it); the upper half goes over a pair of trousers.
        /// </summary>
        private static Mesh Cut(SkinnedMeshRenderer body, string name, HashSet<string> bones, bool inside)
        {
            string relative = $"{Spec.Generated}/Kits/Characters/{name}.asset";
            if (Cuts.TryGetValue(relative, out Mesh cached) && cached != null)
            {
                return cached;
            }
            Mesh source = body.sharedMesh;
            Transform[] skeleton = body.bones;
            var keep = new HashSet<int>();
            for (int i = 0; i < skeleton.Length; i++)
            {
                if (skeleton[i] != null && bones.Contains(skeleton[i].name))
                {
                    keep.Add(i);
                }
            }
            BoneWeight[] weights = source.boneWeights;
            var kept = new bool[weights.Length];
            for (int v = 0; v < weights.Length; v++)
            {
                BoneWeight w = weights[v];
                float on = (keep.Contains(w.boneIndex0) ? w.weight0 : 0f) + (keep.Contains(w.boneIndex1) ? w.weight1 : 0f)
                           + (keep.Contains(w.boneIndex2) ? w.weight2 : 0f) + (keep.Contains(w.boneIndex3) ? w.weight3 : 0f);
                kept[v] = inside ? on > 0.55f : on < 0.45f;
            }
            var map = new Dictionary<int, int>();
            var triangles = new List<int>();
            int[] all = source.triangles;
            for (int t = 0; t + 2 < all.Length; t += 3)
            {
                if (!kept[all[t]] || !kept[all[t + 1]] || !kept[all[t + 2]])
                {
                    continue;
                }
                for (int k = 0; k < 3; k++)
                {
                    int v = all[t + k];
                    if (!map.TryGetValue(v, out int index))
                    {
                        index = map.Count;
                        map[v] = index;
                    }
                    triangles.Add(index);
                }
            }
            var order = new int[map.Count];
            foreach (KeyValuePair<int, int> pair in map)
            {
                order[pair.Value] = pair.Key;
            }
            Vector3[] vertices = source.vertices;
            Vector3[] normals = source.normals;
            Vector4[] tangents = source.tangents;
            Vector2[] uv = source.uv;
            var mesh = new Mesh { name = name };
            mesh.vertices = order.Select(v => vertices[v]).ToArray();
            if (normals.Length == vertices.Length)
            {
                mesh.normals = order.Select(v => normals[v]).ToArray();
            }
            if (tangents.Length == vertices.Length)
            {
                mesh.tangents = order.Select(v => tangents[v]).ToArray();
            }
            if (uv.Length == vertices.Length)
            {
                mesh.uv = order.Select(v => uv[v]).ToArray();
            }
            mesh.boneWeights = order.Select(v => weights[v]).ToArray();
            mesh.bindposes = source.bindposes;
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh = HeroesAssets.CreateOrReplace(mesh, relative);
            Cuts[relative] = mesh;
            return mesh;
        }

        // ------------------------------------------------------------------ materials

        /// <summary>The kit's material for a slot of a person, as the look paints it.</summary>
        private static Material Dress(Material source, Look look, string sex)
        {
            if (source == null)
            {
                return null;
            }
            string name = HeroesKitArt.BaseName(source.name);
            string kit = "Characters";
            switch (name)
            {
                case "MI_Peasant":
                case "MI_Ranger":
                    return look.Cloth != null
                        ? HeroesKitArt.Repainted(kit, name, look.Variant, look.ClothTag ?? "Cloth", look.Cloth, look.ClothTint)
                        : HeroesKitArt.Material(kit, name, look.Variant, look.ClothTint);
                case "MI_Regular_Male":
                case "MI_Regular_Female":
                    return look.Skin != null
                        ? HeroesKitArt.Repainted(kit, name, -1, look.SkinTag ?? "Skin", look.Skin, look.SkinTint)
                        : HeroesKitArt.Material(kit, name, -1, look.SkinTint);
                case "MI_Superhero_Male":
                case "MI_Superhero_Female":
                    return look.Skin != null
                        ? HeroesKitArt.Repainted(kit, name, look.Light ? 0 : -1, look.SkinTag ?? "Skin", look.Skin, look.SkinTint)
                        : HeroesKitArt.Material(kit, name, look.Light ? 0 : -1, look.SkinTint);
                case "MI_Eyes":
                    Material eyes = HeroesKitArt.Material(kit, name);
                    return look.EyeGlow.a > 0f ? HeroesKitArt.Glowing(eyes, look.EyeGlow, ColorUtility.ToHtmlStringRGB(look.EyeGlow)) : eyes;
                case "MI_Hair_1":
                case "MI_Hair_2":
                    return HeroesKitArt.Material(kit, name, -1, look.HairTint.a > 0f ? look.HairTint : DarkBrown);
                default:
                    return HeroesKitArt.Material(kit, name) ?? source;
            }
        }

        // ------------------------------------------------------------------ what the hands hold

        /// <summary>
        /// Puts a weapon in the right (or left) fist, or a shield on the forearm. The person stands as rigged, so the place
        /// is worked out from the bones of the hand: the fist closes around an axis across the palm (from the little
        /// finger's knuckle to the forefinger's), and a weapon runs along it with its grip in the middle of the fist.
        /// </summary>
        private static void Hold(GameObject person, Dictionary<string, Transform> bones, Gear gear, bool left)
        {
            if (gear == null)
            {
                return;
            }
            string side = left ? "_l" : "_r";
            if (!bones.TryGetValue("hand" + side, out Transform hand) || !bones.TryGetValue("index_01" + side, out Transform index)
                || !bones.TryGetValue("pinky_01" + side, out Transform pinky) || !bones.TryGetValue("middle_01" + side, out Transform middle))
            {
                return;
            }
            bool staff = gear.Model == StaffModel;
            if (!staff && HeroesArtBuilder.Model(gear.Model) == null)
            {
                return;
            }
            // Named apart from the model inside it, so a rebuild matches every part to its saved self.
            var holder = new GameObject((staff ? "Staff" : System.IO.Path.GetFileName(gear.Model)) + " Grip");
            holder.transform.SetParent(person.transform, false);
            GameObject piece = staff ? Staff(holder.transform, gear.Length, gear.Tint) : HeroesArtBuilder.Piece(holder.transform, gear.Model, Vector3.zero, 0f, 1f, 0f, gear.Tint);
            if (piece == null)
            {
                Object.DestroyImmediate(holder);
                return;
            }
            // Plain objects, as the body is: a model's prefab nested deep in the skeleton would be saved anew on every rebuild.
            foreach (Transform part in piece.GetComponentsInChildren<Transform>(true))
            {
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(part.gameObject))
                {
                    PrefabUtility.UnpackPrefabInstance(part.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }
            }
            // The model's long axis, measured in the holder's space, from its butt to its tip.
            Bounds bounds = HeroesArtBuilder.BoundsOf(piece);
            Vector3 size = bounds.size;
            Vector3 along = size.x >= size.y && size.x >= size.z ? Vector3.right : size.y >= size.z ? Vector3.up : Vector3.forward;
            float length = Vector3.Scale(size, along).magnitude;
            if (length < 0.001f)
            {
                Object.DestroyImmediate(holder);
                return;
            }
            float scale = gear.Length / length;
            piece.transform.localScale *= scale;
            piece.transform.localPosition *= scale;
            bounds = HeroesArtBuilder.BoundsOf(piece);
            Vector3 butt = bounds.center - Vector3.Scale(bounds.extents, along);
            Vector3 tip = bounds.center + Vector3.Scale(bounds.extents, along);
            if (gear.Reversed)
            {
                (butt, tip) = (tip, butt);
            }
            // Grip point and axis in the holder's space; the holder is then turned and moved so they meet the fist's.
            Vector3 grip = Vector3.Lerp(butt, tip, gear.Grip);
            Vector3 axis = (tip - butt).normalized;

            Vector3 palmCenter = Vector3.Lerp(hand.position, middle.position, 0.55f);
            Vector3 across = (index.position - pinky.position).normalized;
            Vector3 fingers = (middle.position - hand.position).normalized;
            Vector3 palm = Vector3.Cross(fingers, across).normalized * (left ? -1f : 1f);
            Quaternion turn;
            Vector3 at;
            if (gear.Shield)
            {
                // A shield's face looks out from the back of the forearm, its length along the arm.
                Vector3 outward = -palm;
                turn = Quaternion.LookRotation(outward, fingers) * Quaternion.Inverse(Quaternion.LookRotation(Face(bounds, along), axis));
                at = hand.position - fingers * 0.08f + outward * 0.06f;
                grip = bounds.center;
            }
            else
            {
                // Upright, the staff or bow stands straight through the fist; otherwise it runs along the fist's axis.
                Vector3 toward = gear.Upright ? Vector3.up : across;
                turn = Quaternion.FromToRotation(axis, toward);
                turn = Quaternion.AngleAxis(gear.Roll, toward) * turn;
                at = palmCenter + palm * 0.025f;
            }
            holder.transform.rotation = person.transform.rotation * turn;
            holder.transform.position = at - holder.transform.TransformVector(grip);
            holder.transform.SetParent(hand, true);
        }

        /// <summary>
        /// A staff of <paramref name="length"/> meters standing on its foot: a slender pole in the twisted trees' bark, and a
        /// crystal at its head lit by <paramref name="glow"/>.
        /// </summary>
        private static GameObject Staff(Transform parent, float length, Color glow)
        {
            var staff = new GameObject("Pole and crystal");
            staff.transform.SetParent(parent, false);
            var pole = new GameObject("Pole");
            pole.transform.SetParent(staff.transform, false);
            pole.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
            // The built-in cylinder is two units tall and one across.
            pole.transform.localScale = new Vector3(0.038f, length * 0.5f, 0.038f);
            pole.transform.localPosition = new Vector3(0f, length * 0.5f, 0f);
            pole.AddComponent<MeshRenderer>().sharedMaterial = HeroesKitArt.Material("Nature", "Bark_TwistedTree");
            GameObject crystal = HeroesArtBuilder.Piece(staff.transform, "Quaternius/Items/Crystal2", new Vector3(0f, length - 0.02f, 0f), 30f, 1f, 0.2f);
            Color light = glow.a > 0f ? glow : new Color(0.5f, 0.8f, 1.6f);
            if (crystal != null)
            {
                // The crystal takes the colour of its light (the pack paints it pink), and glows with it.
                float brightest = Mathf.Max(light.r, Mathf.Max(light.g, light.b), 0.01f);
                var body = new Color(light.r / brightest, light.g / brightest, light.b / brightest, 1f);
                Material gem = HeroesAssets.Material($"{Spec.Generated}/Kits/Glow/Crystal_{ColorUtility.ToHtmlStringRGB(light)}.mat",
                    Shader.Find("Universal Render Pipeline/Lit"), m =>
                    {
                        m.SetColor("_BaseColor", body);
                        m.SetFloat("_Smoothness", 0.85f);
                        m.SetFloat("_Metallic", 0.1f);
                        m.SetColor("_EmissionColor", light);
                        m.EnableKeyword("_EMISSION");
                        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    });
                foreach (Renderer renderer in crystal.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(material => gem).ToArray();
                }
            }
            return staff;
        }

        /// <summary>The direction a flat model faces: its thinnest axis that is not its length.</summary>
        private static Vector3 Face(Bounds bounds, Vector3 along)
        {
            Vector3 size = bounds.size;
            var axes = new[] { Vector3.right, Vector3.up, Vector3.forward }.Where(a => a != along).ToArray();
            return Vector3.Scale(size, axes[0]).magnitude <= Vector3.Scale(size, axes[1]).magnitude ? axes[0] : axes[1];
        }

        // ------------------------------------------------------------------ clips

        private static readonly Dictionary<string, AnimationClip> Clips = new Dictionary<string, AnimationClip>();

        /// <summary>The name a clip of the library plays by: Idle and Walk for the looping idle and walk, else its own.</summary>
        public static string ClipName(string library)
        {
            return library == "Idle_Loop" ? "Idle" : library == "Walk_Loop" ? "Walk" : library;
        }

        /// <summary>
        /// The library's clip <paramref name="library"/> as the people of the theme play it: a legacy clip of the bones'
        /// turns (and the root and pelvis moving), saved among the theme's clips under <see cref="ClipName"/>.
        /// </summary>
        public static AnimationClip Clip(string library)
        {
            string name = ClipName(library);
            if (Clips.TryGetValue(name, out AnimationClip cached) && cached != null)
            {
                return cached;
            }
            AnimationClip source = null;
            foreach (string file in Libraries)
            {
                source = AssetDatabase.LoadAllAssetsAtPath(HeroesAssets.Path($"Art/Models/{file}.fbx")).OfType<AnimationClip>()
                    .FirstOrDefault(clip => clip.name == library || clip.name.EndsWith("|" + library, StringComparison.Ordinal));
                if (source != null)
                {
                    break;
                }
            }
            if (source == null)
            {
                Debug.LogWarning($"Heroes: the animation library has no clip {library}.");
                return null;
            }
            bool loops = library.EndsWith("_Loop", StringComparison.Ordinal) || library == "Sword_Idle";
            var clip = new AnimationClip { name = name, legacy = true, wrapMode = loops ? WrapMode.Loop : WrapMode.Once, frameRate = source.frameRate };
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(source))
            {
                if (binding.propertyName.StartsWith("m_LocalScale", StringComparison.Ordinal))
                {
                    continue;
                }
                bool moves = binding.propertyName.StartsWith("m_LocalPosition", StringComparison.Ordinal);
                if (moves && !(binding.path.EndsWith("/pelvis", StringComparison.Ordinal) || binding.path.EndsWith("/root", StringComparison.Ordinal)))
                {
                    continue;
                }
                AnimationUtility.SetEditorCurve(clip, binding, AnimationUtility.GetEditorCurve(source, binding));
            }
            clip.EnsureQuaternionContinuity();
            clip = HeroesAssets.CreateOrReplace(clip, $"{Spec.Generated}/Kits/Clips/{name}.anim");
            Clips[name] = clip;
            return clip;
        }

        /// <summary>Forgets the meshes and clips of the theme built before.</summary>
        public static void ForgetCaches()
        {
            Cuts.Clear();
            Clips.Clear();
        }
    }
}
