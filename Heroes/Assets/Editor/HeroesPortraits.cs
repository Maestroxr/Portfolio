using System;
using System.Collections.Generic;
using System.Linq;
using Gamebox.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// Paints the pictures the interface shows of the 3D models: a portrait of every creature, the head and shoulders
    /// of every class of hero and the hero on his mount, every town in every color, and the item icons the downloaded
    /// set lacks. Each model is posed in its idle clip (a third of the way in, so none stands in the rest pose it was
    /// rigged in), turned three quarters to the viewer and rendered on a transparent background in the
    /// <see cref="ModelStudio"/>.
    /// </summary>
    internal static class HeroesPortraits
    {
        private const string Folder = "Art/Generated/Portraits";
        private const int Size = 256;

        [MenuItem("Heroes/Art/Portraits", false, 41)]
        public static void BuildMenu()
        {
            HeroesArt art = HeroesAssets.Require<HeroesArt>("Art/HeroesArt.asset");
            Build(art);
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
        }

        public static void Build(HeroesArt art)
        {
            using (var studio = new ModelStudio())
            {
                foreach (HeroesArt.UnitArt unit in art.units)
                {
                    unit.portrait = Creature(studio, unit);
                }
                foreach (HeroesArt.HeroArt hero in art.heroes)
                {
                    hero.portrait = Bust(studio, hero);
                    hero.mounted = Mounted(studio, hero);
                }
                foreach (HeroesArt.TownArt town in art.towns)
                {
                    var portraits = new Sprite[5];
                    for (int color = 0; color < portraits.Length && color < town.byColor.Length; color++)
                    {
                        portraits[color] = Town(studio, town.byColor[color], $"Town_{town.faction}_{color}");
                    }
                    town.portraits = portraits;
                }
                Items(studio);
            }
        }

        // ------------------------------------------------------------------ poses

        /// <summary>
        /// Poses every animated model under <paramref name="root"/> in <paramref name="clip"/> (found by its full name or by
        /// the part after the armature's name), <paramref name="at"/> of the way through it. Returns whether it found one.
        /// </summary>
        public static bool Pose(GameObject root, string clip, float at = 0.3f)
        {
            bool posed = false;
            foreach (Animation animation in root.GetComponentsInChildren<Animation>(true))
            {
                AnimationClip found = Clip(animation, clip);
                if (found == null)
                {
                    continue;
                }
                found.SampleAnimation(animation.gameObject, found.length * at);
                posed = true;
            }
            return posed;
        }

        private static AnimationClip Clip(Animation animation, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            AnimationClip exact = animation.GetClip(name);
            if (exact != null)
            {
                return exact;
            }
            foreach (AnimationState state in animation)
            {
                string clip = state.clip != null ? state.clip.name : "";
                if (clip.EndsWith("|" + name, StringComparison.Ordinal))
                {
                    return state.clip;
                }
            }
            return null;
        }

        /// <summary>The first of the idle clips a model of one of the packs may have.</summary>
        private static void Rest(GameObject root, string idle)
        {
            if (!Pose(root, idle))
            {
                foreach (string name in new[] { "Idle", "CharacterArmature|Idle", "AnimalArmature|Idle" })
                {
                    if (Pose(root, name))
                    {
                        return;
                    }
                }
            }
        }

        // ------------------------------------------------------------------ creatures and heroes

        private static Sprite Creature(ModelStudio studio, HeroesArt.UnitArt unit)
        {
            if (unit.prefab == null)
            {
                return null;
            }
            var shot = new StudioShot { Width = Size, Height = Size, Yaw = 205f, Pitch = 10f, Padding = 0.05f };
            float at = 0.3f;
            switch (unit.creature)
            {
                case CreatureId.ArrowTower:
                    shot.Pitch = 18f;
                    shot.Yaw = 200f;
                    break;
                case CreatureId.GoldDragon:
                case CreatureId.BoneDragon:
                case CreatureId.RedDragon:
                case CreatureId.GreenDragon:
                    // From the side and a little above, so the wings and the long neck read.
                    shot.Yaw = 245f;
                    shot.Pitch = 16f;
                    at = 0.35f;
                    break;
                case CreatureId.VampireBat:
                    shot.Pitch = 6f;
                    at = 0.2f;
                    break;
                case CreatureId.Cavalier:
                case CreatureId.WarBull:
                case CreatureId.Wolf:
                case CreatureId.DireWolf:
                case CreatureId.FireFox:
                case CreatureId.ElderStag:
                    // Four legged beasts are seen more from the side, so their bodies read.
                    shot.Yaw = 235f;
                    break;
                case CreatureId.Militia:
                case CreatureId.Archer:
                case CreatureId.Priest:
                case CreatureId.Crusader:
                case CreatureId.Zombie:
                case CreatureId.Shaman:
                case CreatureId.Bandit:
                    // The tall adventurers from the knees up, or they come out as thin as a matchstick in a slot.
                    shot.Framing = StudioFraming.Bust;
                    shot.BustFraction = 0.72f;
                    shot.Padding = 0.04f;
                    break;
            }
            Texture2D picture = studio.Shoot(unit.prefab, shot, model => Pose(model, unit.idle, at));
            return Save(picture, $"Creature_{unit.creature}");
        }

        /// <summary>The rider alone, head and shoulders, standing at rest.</summary>
        private static Sprite Bust(ModelStudio studio, HeroesArt.HeroArt hero)
        {
            if (hero.rider == null)
            {
                return null;
            }
            // The adventurers of one pack have heads as large as their bodies, those of the other are tall.
            bool tall = IsTall(hero);
            var shot = new StudioShot
            {
                Width = Size,
                Height = Size,
                Yaw = 200f,
                Pitch = 4f,
                Framing = StudioFraming.Bust,
                BustFraction = tall ? 0.36f : 0.56f,
                Padding = 0.07f
            };
            Texture2D picture = studio.Shoot(hero.rider, shot, model => Rest(model, "Idle"));
            return Save(picture, $"Hero_{hero.heroClass}");
        }

        /// <summary>
        /// Whether the hero's rider is one of the tall Quaternius adventurers, whose clips are named after their armature
        /// (the art builder gives them a seated clip of their own, so their pose does not tell).
        /// </summary>
        private static bool IsTall(HeroesArt.HeroArt hero)
        {
            var animation = hero.rider != null ? hero.rider.GetComponentInChildren<Animation>(true) : null;
            return animation != null && animation.GetClip("CharacterArmature|Idle") != null;
        }

        /// <summary>The hero on his mount, as the map shows him: the rider seated on the saddle.</summary>
        private static Sprite Mounted(ModelStudio studio, HeroesArt.HeroArt hero)
        {
            if (hero.mount == null || hero.rider == null)
            {
                return null;
            }
            var shot = new StudioShot { Width = Size, Height = Size, Yaw = 225f, Pitch = 10f, Padding = 0.05f };
            GameObject mount = studio.Place(hero.mount);
            try
            {
                Rest(mount, hero.mountIdle);
                var seat = new GameObject("Seat");
                seat.transform.SetParent(mount.transform, false);
                seat.transform.localPosition = hero.seat;
                GameObject rider = Object.Instantiate(hero.rider, seat.transform, false);
                if (!Pose(rider, hero.riderPose))
                {
                    Rest(rider, "Idle");
                    Straddle(rider);
                }
                return Save(studio.ShootPlaced(mount, shot), $"Mounted_{hero.heroClass}");
            }
            finally
            {
                Object.DestroyImmediate(mount);
            }
        }

        /// <summary>
        /// Bends the legs of a rider whose rig has no seated clip (the Quaternius adventurers) so he sits the saddle
        /// instead of standing through the horse: thighs forward, shins back down.
        /// </summary>
        public static void Straddle(GameObject rider)
        {
            foreach (Transform bone in rider.GetComponentsInChildren<Transform>(true))
            {
                string name = bone.name.ToLowerInvariant();
                bool upper = name.StartsWith("upperleg", StringComparison.Ordinal) || name.StartsWith("thigh", StringComparison.Ordinal);
                bool lower = name.StartsWith("lowerleg", StringComparison.Ordinal) || name.StartsWith("shin", StringComparison.Ordinal)
                             || name.StartsWith("calf", StringComparison.Ordinal);
                if (upper)
                {
                    bone.rotation = Quaternion.AngleAxis(-80f, rider.transform.right) * bone.rotation;
                }
                else if (lower)
                {
                    bone.rotation = Quaternion.AngleAxis(75f, rider.transform.right) * bone.rotation;
                }
            }
        }

        // ------------------------------------------------------------------ towns and items

        private static Sprite Town(ModelStudio studio, GameObject prefab, string name)
        {
            if (prefab == null)
            {
                return null;
            }
            var shot = new StudioShot { Width = Size, Height = Size, Yaw = 200f, Pitch = 30f, Padding = 0.04f, Key = 1.45f };
            return Save(studio.Shoot(prefab, shot), name);
        }

        /// <summary>The item icons the downloaded set lacks, drawn from the item models in its manner.</summary>
        private static void Items(ModelStudio studio)
        {
            var wanted = new List<string>(Drawn);
            for (int i = 0; i < Artifacts.Count; i++)
            {
                ArtifactDef def = Artifacts.Get((ArtifactId)i);
                if (def != null && !string.IsNullOrEmpty(def.Item) && !wanted.Contains(def.Item)
                    && HeroesAssets.Load<Texture2D>($"Art/UI/Items/{def.Item}.png") == null)
                {
                    wanted.Add(def.Item);
                }
            }
            foreach (string item in wanted)
            {
                var holder = new GameObject(item);
                studio.Adopt(holder);
                GameObject piece = HeroesArtBuilder.Piece(holder.transform, $"Quaternius/Items/{item}", Vector3.zero, 0f, 1f, 0f);
                if (piece == null)
                {
                    Object.DestroyImmediate(holder);
                    continue;
                }
                if (Lying.TryGetValue(item, out Vector3 turn))
                {
                    piece.transform.rotation = Quaternion.Euler(turn) * piece.transform.rotation;
                }
                bool blade = item.StartsWith("Sword", StringComparison.Ordinal) || item.StartsWith("Dagger", StringComparison.Ordinal);
                if (blade)
                {
                    // The blade stands up along its length with its flat to the viewer, then leans from the lower left
                    // to the upper right, like the other blades of the set.
                    Vector3 size = HeroesArtBuilder.BoundsOf(piece).size;
                    if (size.z >= size.x && size.z >= size.y)
                    {
                        piece.transform.rotation = Quaternion.Euler(-90f, 0f, 0f) * piece.transform.rotation;
                    }
                    else if (size.x >= size.y && size.x >= size.z)
                    {
                        piece.transform.rotation = Quaternion.Euler(0f, 0f, 90f) * piece.transform.rotation;
                    }
                    size = HeroesArtBuilder.BoundsOf(piece).size;
                    if (size.x < size.z)
                    {
                        piece.transform.rotation = Quaternion.Euler(0f, 90f, 0f) * piece.transform.rotation;
                    }
                    Bounds bounds = HeroesArtBuilder.BoundsOf(piece);
                    piece.transform.position -= bounds.center - holder.transform.position;
                    var tilt = new GameObject("Tilt");
                    tilt.transform.SetParent(holder.transform, false);
                    piece.transform.SetParent(tilt.transform, true);
                    tilt.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
                }
                var shot = new StudioShot
                {
                    Width = 128,
                    Height = 128,
                    Yaw = blade ? 0f : 200f,
                    Pitch = blade ? 0f : 12f,
                    Padding = 0.04f,
                    Rim = 0.6f
                };
                var made = new List<Material>();
                Fill(piece, made);
                Texture2D picture = studio.ShootPlaced(holder, shot);
                Object.DestroyImmediate(holder);
                foreach (Material material in made)
                {
                    Object.DestroyImmediate(material);
                }
                HeroesAssets.SaveTexture(picture, $"Art/UI/Items/{item}.png", importer => HeroesAssets.SpriteImport(importer, Vector4.zero, 128));
            }
        }

        /// <summary>The colors of the liquids of the potions, by the name of their material.</summary>
        private static readonly (string name, Color color)[] Liquids =
        {
            ("Red", new Color(0.82f, 0.1f, 0.12f)), ("Yellow", new Color(0.96f, 0.76f, 0.16f)), ("Green", new Color(0.32f, 0.8f, 0.26f)),
            ("Blue", new Color(0.2f, 0.45f, 0.95f)), ("Purple", new Color(0.56f, 0.22f, 0.82f)), ("Orange", new Color(0.96f, 0.5f, 0.12f)),
            ("Pink", new Color(0.95f, 0.45f, 0.7f)), ("White", new Color(0.92f, 0.92f, 0.95f))
        };

        /// <summary>
        /// The potions of the item pack come without the colors of their glass and of what is in it (both import as plain
        /// grey, so a flask is drawn as a white ball): for their pictures the glass is made clear, and the liquid takes
        /// the color its material is named after. The materials are made for the picture only; they go in
        /// <paramref name="made"/> to be destroyed after it.
        /// </summary>
        private static void Fill(GameObject piece, List<Material> made)
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (Renderer renderer in piece.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    string name = materials[i] != null ? materials[i].name : "";
                    if (name.StartsWith("Glass", StringComparison.Ordinal))
                    {
                        var glass = new Material(lit) { hideFlags = HideFlags.HideAndDontSave };
                        glass.SetColor("_BaseColor", new Color(0.86f, 0.93f, 1f, 0.3f));
                        glass.SetFloat("_Smoothness", 0.92f);
                        glass.SetFloat("_Surface", 1f);
                        glass.SetFloat("_Blend", 0f);
                        glass.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        glass.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        glass.SetFloat("_ZWrite", 0f);
                        glass.SetOverrideTag("RenderType", "Transparent");
                        glass.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                        glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        materials[i] = glass;
                        made.Add(glass);
                        changed = true;
                    }
                    else if (name.StartsWith("Liquid_", StringComparison.Ordinal))
                    {
                        Color color = new Color(0.8f, 0.2f, 0.3f);
                        foreach ((string liquid, Color tone) in Liquids)
                        {
                            if (name.StartsWith("Liquid_" + liquid, StringComparison.Ordinal))
                            {
                                color = tone;
                            }
                        }
                        var fill = new Material(lit) { hideFlags = HideFlags.HideAndDontSave };
                        fill.SetColor("_BaseColor", color);
                        fill.SetFloat("_Smoothness", 0.6f);
                        materials[i] = fill;
                        made.Add(fill);
                        changed = true;
                    }
                }
                if (changed)
                {
                    renderer.sharedMaterials = materials;
                }
            }
        }

        /// <summary>The item icons this class draws itself: the downloaded set has no picture of them.</summary>
        private static readonly string[] Drawn = { "Sword_big_Golden", "Sword_big", "Potion1_Filled" };

        /// <summary>The item models that lie on their side, with the turn that stands them up (the flask's neck lies along +Z).</summary>
        private static readonly Dictionary<string, Vector3> Lying = new Dictionary<string, Vector3>
        {
            { "Potion1_Filled", new Vector3(-90f, 0f, 0f) }
        };

        private static Sprite Save(Texture2D picture, string name)
        {
            string relative = $"{Folder}/{name}.png";
            HeroesAssets.SaveTexture(picture, relative, importer => HeroesAssets.SpriteImport(importer, Vector4.zero, Size));
            return HeroesAssets.Sprite(relative);
        }
    }
}
