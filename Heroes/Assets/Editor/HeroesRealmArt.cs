using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Gear = Portfolio.Heroes.EditorTools.HeroesRealmCharacters.Gear;
using Look = Portfolio.Heroes.EditorTools.HeroesRealmCharacters.Look;
using Outfit = Portfolio.Heroes.EditorTools.HeroesRealmCharacters.Outfit;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// The creatures, heroes and scenery of a theme made of the textured kits (<see cref="ModelSet.Kits"/>): every creature
    /// that is a person is put together by <see cref="HeroesRealmCharacters"/> in the colours of its faction (the blue of
    /// the Castle, the violet and grey of the Necropolis, the red and bare skin of the Stronghold), the beasts and dragons
    /// keep the models of the packs, the knights of the Castle ride, and the woods, rocks and reeds of the map are the
    /// nature kit's.
    /// </summary>
    internal static class HeroesRealmArt
    {
        private const string Nature = HeroesKitArt.Models + "/Nature";
        private const string Props = HeroesKitArt.Models + "/Props";

        private static HeroesThemeSpec Spec => HeroesThemeSpec.Current;
        private static string Generated => Spec.Generated;

        // The colours of the factions' cloth, and the look of the dead.
        internal static readonly Color CastleBlue = new Color(0.16f, 0.3f, 0.74f);
        internal static readonly Color CastleWhite = new Color(0.92f, 0.9f, 0.84f);
        internal static readonly Color NecroViolet = new Color(0.3f, 0.16f, 0.42f);
        internal static readonly Color StrongholdRed = new Color(0.66f, 0.14f, 0.1f);
        internal static readonly Color BanditGrey = new Color(0.26f, 0.26f, 0.28f);

        /// <summary>The skin of the risen dead: drained to bone white, or to an ashen grey with a cold cast.</summary>
        private static System.Func<Color, Color> Bone => HeroesKitArt.Grade(0.12f, 1.45f, new Color(0.92f, 0.9f, 0.82f), 0.35f);
        private static System.Func<Color, Color> Ashen => HeroesKitArt.Grade(0.2f, 1.2f, new Color(0.62f, 0.7f, 0.76f), 0.35f);
        private static System.Func<Color, Color> Rotten => HeroesKitArt.Grade(0.35f, 1.1f, new Color(0.56f, 0.66f, 0.48f), 0.4f);
        private static System.Func<Color, Color> Grave => HeroesKitArt.Grade(0.18f, 0.85f, new Color(0.55f, 0.55f, 0.6f), 0.3f);
        internal static readonly Color DeadEyes = new Color(0.35f, 1.6f, 1.2f);
        internal static readonly Color WitchEyes = new Color(1.4f, 0.5f, 1.8f);

        /// <summary>The ranger's green cloth repainted in a colour (its leather and metal stay).</summary>
        private static System.Func<Color, Color> Green(Color color, float brightness = 1f)
        {
            return HeroesKitArt.Hue(0.17f, 0.47f, 0.18f, color, brightness);
        }

        // ------------------------------------------------------------------ weapons

        private static Gear Sword => new Gear($"{Props}/Sword_Bronze", 0.95f, 0.1f);
        private static Gear Axe => new Gear($"{Props}/Axe_Bronze", 0.8f, 0.12f);
        private static Gear Pick => new Gear($"{Props}/Pickaxe_Bronze", 0.85f, 0.15f);
        private static Gear Torch => new Gear($"{Props}/Torch_Metal", 0.55f, 0.25f);
        private static Gear Shield => new Gear($"{Props}/Shield_Wooden", 0.62f) { Shield = true };
        private static Gear Staff => new Gear(HeroesRealmCharacters.StaffModel, 1.6f, 0.42f) { Upright = true };
        private static Gear FireStaff => new Gear(HeroesRealmCharacters.StaffModel, 1.6f, 0.42f) { Upright = true, Tint = new Color(2f, 0.9f, 0.3f) };
        private static Gear StormStaff => new Gear(HeroesRealmCharacters.StaffModel, 1.6f, 0.42f) { Upright = true, Tint = new Color(0.5f, 1.1f, 2f) };
        private static Gear HolyStaff => new Gear(HeroesRealmCharacters.StaffModel, 1.6f, 0.42f) { Upright = true, Tint = new Color(2f, 1.7f, 0.9f) };
        private static Gear BoneStaff => new Gear(HeroesRealmCharacters.StaffModel, 1.6f, 0.42f) { Upright = true, Tint = new Color(1.4f, 0.45f, 2f) };
        private static Gear Bow => new Gear("Quaternius/Items/Bow_Wooden", 1.15f, 0.5f) { Upright = true };
        private static Gear GreatSword => new Gear("Quaternius/Items/Sword_big", 1.25f, 0.1f);
        private static Gear Dagger => new Gear("Quaternius/Items/Dagger_Golden", 0.42f, 0.14f);

        // ------------------------------------------------------------------ creatures

        /// <summary>A creature that is a person: how it looks and which clips of the library it plays.</summary>
        private sealed class Person
        {
            public CreatureId Creature;
            public Look Look;
            public string Idle = "Idle_Loop";
            public string Walk = "Walk_Loop";
            public string Attack = "Sword_Attack";
            public string Shoot = "";
            public string Hit = "Hit_Chest";
            public string Death = "Death01";
            public string Cast = "";
            public ProjectileKind Projectile;
        }

        private static IEnumerable<Person> People()
        {
            // The Castle: men at arms in its blue, a priest in white, crusaders in white and gold.
            yield return new Person
            {
                Creature = CreatureId.Militia,
                Look = new Look { Outfit = Outfit.MalePeasant, Hair = "Hair_SimpleParted", Beard = "Hair_Beard", Right = Axe, Left = Shield },
                Attack = "Sword_Attack"
            };
            yield return new Person
            {
                Creature = CreatureId.Archer,
                Look = new Look { Outfit = Outfit.FemaleRanger, Cloth = Green(CastleBlue), ClothTag = "Castle", Hair = "Hair_Long", Right = Bow },
                Idle = "Pistol_Idle_Loop",
                Attack = "Punch_Cross",
                Shoot = "Pistol_Shoot",
                Projectile = ProjectileKind.Arrow
            };
            yield return new Person
            {
                Creature = CreatureId.Swordsman,
                Look = new Look { Outfit = Outfit.MaleRanger, Cloth = Green(CastleBlue), ClothTag = "Castle", Hood = false, Hair = "Hair_Buzzed", Beard = "Hair_Beard", Right = Sword, Left = Shield },
                Idle = "Sword_Idle",
                Attack = "Sword_Regular_A"
            };
            yield return new Person
            {
                Creature = CreatureId.Priest,
                Look = new Look { Outfit = Outfit.FemalePeasant, Variant = 0, Hair = "Hair_Buns", Light = true, HairTint = new Color(0.86f, 0.7f, 0.42f), Right = HolyStaff },
                Attack = "Spell_Simple_Shoot",
                Shoot = "Spell_Simple_Shoot",
                Cast = "Spell_Simple_Shoot",
                Projectile = ProjectileKind.HolyLight
            };
            yield return new Person
            {
                Creature = CreatureId.Crusader,
                Look = new Look { Outfit = Outfit.MaleRanger, Variant = 0, Hood = false, Hair = "Hair_Long", Beard = "Hair_Beard", Light = true, Right = GreatSword },
                Idle = "Sword_Idle",
                Attack = "Sword_Regular_C"
            };

            // The Necropolis: the dead get up in what they were buried in, bone white or ashen with their eyes alight.
            yield return new Person
            {
                Creature = CreatureId.Skeleton,
                Look = new Look
                {
                    Outfit = Outfit.MalePeasant, Cloth = Grave, ClothTag = "Grave", Skin = Bone, SkinTag = "Bone", EyeGlow = DeadEyes,
                    Right = Sword, Left = Shield
                },
                Idle = "Sword_Idle",
                Attack = "Sword_Regular_A"
            };
            yield return new Person
            {
                Creature = CreatureId.SkeletonArcher,
                Look = new Look
                {
                    Outfit = Outfit.MaleRanger, Cloth = Grave, ClothTag = "Grave", Skin = Bone, SkinTag = "Bone", EyeGlow = DeadEyes, Right = Bow
                },
                Idle = "Pistol_Idle_Loop",
                Attack = "Punch_Jab",
                Shoot = "Pistol_Shoot",
                Projectile = ProjectileKind.Arrow
            };
            yield return new Person
            {
                Creature = CreatureId.Zombie,
                Look = new Look
                {
                    Outfit = Outfit.MalePeasant, Variant = 0, Cloth = HeroesKitArt.Grade(0.4f, 0.95f, new Color(0.5f, 0.62f, 0.45f), 0.3f), ClothTag = "Rotten",
                    Hair = "Hair_Buzzed", Skin = Rotten, SkinTag = "Rotten", EyeGlow = DeadEyes * 0.6f
                },
                Idle = "Zombie_Idle_Loop",
                Walk = "Zombie_Walk_Fwd_Loop",
                Attack = "Zombie_Scratch"
            };
            yield return new Person
            {
                Creature = CreatureId.BoneGuard,
                Look = new Look
                {
                    Outfit = Outfit.MaleRanger, Cloth = Green(NecroViolet, 0.7f), ClothTag = "Necropolis", ClothTint = new Color(0.78f, 0.78f, 0.84f),
                    Skin = Ashen, SkinTag = "Ashen", EyeGlow = DeadEyes, Right = Sword, Left = Shield
                },
                Idle = "Sword_Idle",
                Attack = "Sword_Regular_B"
            };
            yield return new Person
            {
                Creature = CreatureId.Lich,
                Look = new Look
                {
                    Outfit = Outfit.MaleRanger, Cloth = Green(NecroViolet, 0.6f), ClothTag = "Lich", ClothTint = new Color(0.7f, 0.7f, 0.78f),
                    Skin = Ashen, SkinTag = "Ashen", EyeGlow = WitchEyes, Beard = "Hair_Beard", HairTint = new Color(0.9f, 0.9f, 0.95f), Right = BoneStaff
                },
                Attack = "Spell_Simple_Shoot",
                Shoot = "Spell_Simple_Shoot",
                Cast = "Spell_Simple_Shoot",
                Projectile = ProjectileKind.DeathCloud
            };

            // The Stronghold: brawlers bare to the waist, hunters in the woods' green, shamans and berserkers.
            yield return new Person
            {
                Creature = CreatureId.Brawler,
                Look = new Look { Outfit = Outfit.Male, Trousers = "Male_Peasant", Hair = "Hair_Buzzed", Beard = "Hair_Beard" },
                Idle = "Idle_Loop",
                Attack = "Punch_Cross"
            };
            yield return new Person
            {
                Creature = CreatureId.Hunter,
                Look = new Look { Outfit = Outfit.MaleRanger, Variant = -1, Hair = "Hair_SimpleParted", Right = Bow },
                Idle = "Pistol_Idle_Loop",
                Attack = "Punch_Jab",
                Shoot = "Pistol_Shoot",
                Projectile = ProjectileKind.Arrow
            };
            yield return new Person
            {
                Creature = CreatureId.Shaman,
                Look = new Look { Outfit = Outfit.FemalePeasant, Variant = 0, Hair = "Hair_Long", EyeGlow = new Color(0.6f, 1f, 1.6f), HairTint = new Color(0.16f, 0.13f, 0.12f), Right = StormStaff },
                Attack = "Spell_Simple_Shoot",
                Shoot = "Spell_Simple_Shoot",
                Cast = "Spell_Simple_Shoot",
                Projectile = ProjectileKind.Lightning
            };
            yield return new Person
            {
                Creature = CreatureId.Berserker,
                Look = new Look { Outfit = Outfit.Male, Trousers = "Male_Ranger", Hair = "Hair_Long", Beard = "Hair_Beard", HairTint = new Color(0.85f, 0.42f, 0.25f), SkinTint = new Color(1f, 0.9f, 0.86f), Right = Axe },
                Idle = "Sword_Idle",
                Attack = "Sword_Regular_Combo"
            };

            // The wilds: a bandit in grey, a hermit with a white beard who throws fire.
            yield return new Person
            {
                Creature = CreatureId.Bandit,
                Look = new Look { Outfit = Outfit.MaleRanger, Cloth = Green(BanditGrey), ClothTag = "Bandit", Hair = "Hair_Buzzed", Right = Dagger },
                Idle = "Sword_Idle",
                Attack = "Melee_Hook"
            };
            yield return new Person
            {
                Creature = CreatureId.Hermit,
                Look = new Look { Outfit = Outfit.MalePeasant, Variant = 0, Hair = "Hair_Long", Beard = "Hair_Beard", HairTint = new Color(1.6f, 1.6f, 1.6f), Light = true, Right = FireStaff },
                Attack = "Spell_Simple_Shoot",
                Shoot = "Spell_Simple_Shoot",
                Cast = "Spell_Simple_Shoot",
                Projectile = ProjectileKind.Fire
            };
        }

        public static void Units(HeroesArt art)
        {
            HeroesRealmCharacters.ForgetCaches();
            art.units.Clear();
            Dictionary<CreatureId, Person> people = People().ToDictionary(person => person.Creature);
            foreach (HeroesArtBuilder.UnitSpec spec in HeroesArtBuilder.UnitTable())
            {
                if (spec.Creature == CreatureId.Cavalier)
                {
                    art.units.Add(Cavalier(spec));
                }
                else if (spec.Creature == CreatureId.ArrowTower)
                {
                    art.units.Add(HeroesRealmObjects.ArrowTower(spec));
                }
                else if (people.TryGetValue(spec.Creature, out Person person))
                {
                    art.units.Add(PersonUnit(person));
                }
                else
                {
                    // The beasts, the dragons and the bones keep the models of the packs.
                    art.units.Add(HeroesArtBuilder.PackUnit(spec));
                }
            }
        }

        private static HeroesArt.UnitArt PersonUnit(Person person)
        {
            CreatureDef def = Creatures.Get(person.Creature);
            float height = HeroesArtBuilder.HeightOf(def, 0f);
            GameObject root = HeroesArtBuilder.Root(def.Name.Replace(" ", string.Empty));
            HeroesRealmCharacters.Build(root.transform, person.Look, height, person.Idle, person.Walk, person.Attack, person.Shoot, person.Hit,
                person.Death, person.Cast);
            GameObject prefab = HeroesArtBuilder.Save(root, $"{Generated}/Units/{person.Creature}.prefab");
            return new HeroesArt.UnitArt
            {
                creature = person.Creature,
                prefab = prefab,
                height = height,
                idle = HeroesRealmCharacters.ClipName(person.Idle),
                walk = HeroesRealmCharacters.ClipName(person.Walk),
                attack = HeroesRealmCharacters.ClipName(person.Attack),
                shoot = HeroesRealmCharacters.ClipName(person.Shoot),
                hit = HeroesRealmCharacters.ClipName(person.Hit),
                death = HeroesRealmCharacters.ClipName(person.Death),
                cast = HeroesRealmCharacters.ClipName(person.Cast),
                projectile = person.Projectile,
                flies = def.Abilities.HasFlag(Ability.Flying)
            };
        }

        /// <summary>
        /// The cavalier: a knight in white and gold seated on the white horse, held on its back so he rises and falls with
        /// its gallop and goes down with it. The horse's clips are the creature's; the knight holds his seat.
        /// </summary>
        private static HeroesArt.UnitArt Cavalier(HeroesArtBuilder.UnitSpec spec)
        {
            var knight = new Look { Outfit = Outfit.MaleRanger, Variant = 0, Hood = false, Hair = "Hair_SimpleParted", Beard = "Hair_Beard", Light = true, Right = Sword, Left = Shield };
            Mount(spec.Model, 1.85f, knight, 1.7f, "Units/Cavalier", out GameObject mount, out GameObject rider, out string seated, out Vector3 seat, null);
            CreatureDef def = Creatures.Get(CreatureId.Cavalier);
            GameObject root = HeroesArtBuilder.Root(def.Name.Replace(" ", string.Empty));
            var horse = (GameObject)PrefabUtility.InstantiatePrefab(mount, root.transform);
            Transform back = horse.GetComponentsInChildren<Transform>(true).FirstOrDefault(bone => bone.name == "Torso" || bone.name == "Body");
            var saddle = new GameObject("Seat");
            saddle.transform.SetParent(root.transform, false);
            saddle.transform.localPosition = seat;
            PrefabUtility.InstantiatePrefab(rider, saddle.transform);
            if (back != null)
            {
                saddle.transform.SetParent(back, true);
            }
            float height = HeroesArtBuilder.BoundsOf(root).size.y;
            GameObject prefab = HeroesArtBuilder.Save(root, $"{Generated}/Units/{CreatureId.Cavalier}.prefab");
            return new HeroesArt.UnitArt
            {
                creature = CreatureId.Cavalier,
                prefab = prefab,
                height = height,
                idle = spec.Idle,
                walk = spec.Walk,
                attack = spec.Attack,
                shoot = spec.Shoot,
                hit = spec.Hit,
                death = spec.Death,
                cast = spec.Cast,
                projectile = spec.Projectile,
                flies = false
            };
        }

        /// <summary>
        /// A mount and a person seated on it, as two prefabs (the mount's and the rider's, saved as <paramref name="name"/>
        /// Mount and Rider), the name of the rider's seated clip and where his feet go on the mount.
        /// </summary>
        private static void Mount(string mountModel, float mountHeight, Look look, float riderHeight, string name,
            out GameObject mount, out GameObject rider, out string seated, out Vector3 seat, string cast, Color mountTint = default)
        {
            GameObject mountRoot = HeroesArtBuilder.Root(System.IO.Path.GetFileName(name) + "Mount");
            HeroesArtBuilder.Piece(mountRoot.transform, mountModel, Vector3.zero, 0f, 1f, mountHeight, mountTint);
            mount = HeroesArtBuilder.Save(mountRoot, $"{Generated}/{name}Mount.prefab");

            GameObject riderRoot = HeroesArtBuilder.Root(System.IO.Path.GetFileName(name) + "Rider");
            GameObject person = HeroesRealmCharacters.Build(riderRoot.transform, look, riderHeight, "Idle_Loop", cast);
            seated = person != null ? HeroesArtBuilder.Saddle(person, $"{Generated}/{name}Saddle.anim") : "";
            var animation = person != null ? person.GetComponent<Animation>() : null;
            if (animation != null && animation.GetClip(seated) != null)
            {
                // A rider who is not driven by a puppet of his own (the cavalier's) holds his seat by himself.
                animation.clip = animation.GetClip(seated);
                animation.playAutomatically = true;
            }
            rider = HeroesArtBuilder.Save(riderRoot, $"{Generated}/{name}Rider.prefab");
            seat = HeroesArtBuilder.Seat(mount, rider, "AnimalArmature|Idle", seated);
        }

        // ------------------------------------------------------------------ heroes

        public static void Heroes(HeroesArt art)
        {
            art.heroes.Clear();
            // The dead ride horses as dark as night.
            var night = new Color(0.32f, 0.3f, 0.36f);
            (HeroClass heroClass, Look look, string mount, string cast, Color tint)[] table =
            {
                (HeroClass.Knight, new Look { Outfit = Outfit.MaleRanger, Variant = 0, Hood = false, Hair = "Hair_SimpleParted", Beard = "Hair_Beard", Light = true, Right = Sword, Left = Shield },
                    "Quaternius/Animals/Horse_White", "", default),
                (HeroClass.Cleric, new Look { Outfit = Outfit.FemalePeasant, Variant = 0, Hair = "Hair_Buns", Light = true, HairTint = new Color(0.86f, 0.7f, 0.42f), Right = HolyStaff },
                    "Quaternius/Animals/Horse_White", "Spell_Simple_Shoot", default),
                (HeroClass.DeathKnight, new Look
                    {
                        Outfit = Outfit.MaleRanger, Cloth = Green(NecroViolet, 0.6f), ClothTag = "DeathKnight", ClothTint = new Color(0.7f, 0.7f, 0.76f), Hood = false,
                        Hair = "Hair_Long", HairTint = new Color(1.5f, 1.5f, 1.55f), Skin = Ashen, SkinTag = "Ashen", EyeGlow = DeadEyes, Right = GreatSword
                    },
                    "Quaternius/Animals/Horse", "", night),
                (HeroClass.Necromancer, new Look
                    {
                        Outfit = Outfit.FemaleRanger, Cloth = Green(NecroViolet, 0.75f), ClothTag = "Necromancer", Skin = Ashen, SkinTag = "Ashen", EyeGlow = WitchEyes,
                        Hair = "Hair_Long", HairTint = new Color(0.3f, 0.3f, 0.34f), Right = BoneStaff
                    },
                    "Quaternius/Animals/Horse", "Spell_Simple_Shoot", night),
                (HeroClass.Barbarian, new Look { Outfit = Outfit.Male, Trousers = "Male_Ranger", Hair = "Hair_Long", Beard = "Hair_Beard", HairTint = new Color(0.55f, 0.35f, 0.22f), Right = Axe },
                    "Quaternius/Animals/Bull", "", default),
                (HeroClass.BattleMage, new Look { Outfit = Outfit.FemaleRanger, Cloth = Green(StrongholdRed), ClothTag = "Stronghold", Hair = "Hair_Long", HairTint = new Color(0.6f, 0.3f, 0.2f), Right = FireStaff },
                    "Quaternius/Animals/Horse", "Spell_Simple_Shoot", default)
            };
            foreach ((HeroClass heroClass, Look look, string mountModel, string cast, Color tint) in table)
            {
                Mount(mountModel, 1.85f, look, 1.7f, $"Heroes/{heroClass}", out GameObject mount, out GameObject rider, out string seated, out Vector3 seat, cast, tint);
                art.heroes.Add(new HeroesArt.HeroArt
                {
                    heroClass = heroClass,
                    rider = rider,
                    mount = mount,
                    riderPose = seated,
                    riderCast = HeroesRealmCharacters.ClipName(cast),
                    mountIdle = "AnimalArmature|Idle",
                    mountWalk = "AnimalArmature|Walk",
                    seat = seat
                });
            }
        }

        // ------------------------------------------------------------------ scenery

        public static void Scatter(HeroesArt art)
        {
            art.forestTrees = HeroesArtBuilder.Trees("Forest", new[]
            {
                ($"{Nature}/CommonTree_1", 3.8f), ($"{Nature}/CommonTree_2", 4.1f), ($"{Nature}/CommonTree_3", 4.6f),
                ($"{Nature}/CommonTree_4", 4.4f), ($"{Nature}/CommonTree_5", 3.6f)
            });
            art.pineTrees = HeroesArtBuilder.Trees("Pine", new[]
            {
                ($"{Nature}/Pine_1", 4.4f), ($"{Nature}/Pine_2", 4.2f), ($"{Nature}/Pine_3", 4.6f), ($"{Nature}/Pine_4", 5.0f), ($"{Nature}/Pine_5", 4.2f)
            });
            art.deadTrees = HeroesArtBuilder.Trees("Dead", new[]
            {
                ($"{Nature}/DeadTree_1", 3.6f), ($"{Nature}/DeadTree_2", 3.9f), ($"{Nature}/DeadTree_3", 4.2f), ($"{Nature}/DeadTree_4", 3.8f)
            });
            const float cell = HexLayout.DefaultRadius * 1.7320508f;
            art.mountains = HeroesArtBuilder.Group("Mountain", new[]
            {
                HeroesArtBuilder.Wide("KayKit/Nature/mountain_A", cell * 1.15f), HeroesArtBuilder.Wide("KayKit/Nature/mountain_B", cell * 1.15f),
                HeroesArtBuilder.Wide("KayKit/Nature/mountain_C", cell * 1.15f)
            });
            art.rocks = HeroesArtBuilder.Group("Rock", new[]
            {
                HeroesArtBuilder.Tall($"{Nature}/Rock_Medium_1", 1.1f), HeroesArtBuilder.Tall($"{Nature}/Rock_Medium_2", 0.9f),
                HeroesArtBuilder.Tall($"{Nature}/Rock_Medium_3", 1.25f), HeroesArtBuilder.Tall($"{Nature}/Rock_Medium_2", 0.65f)
            });
            art.waterPlants = HeroesArtBuilder.Group("Water", new[]
            {
                HeroesArtBuilder.Tall($"{Nature}/Plant_7", 0.1f), HeroesArtBuilder.Tall($"{Nature}/Plant_7_Big", 0.1f),
                HeroesArtBuilder.Tall($"{Nature}/Grass_Wispy_Tall", 0.9f), HeroesArtBuilder.Tall($"{Nature}/Grass_Common_Tall", 0.8f)
            });
            art.flags = new GameObject[5];
            for (int color = 0; color < 5; color++)
            {
                art.flags[color] = Banner(color);
            }
            art.towers = new GameObject[5];
            for (int color = 0; color < 5; color++)
            {
                art.towers[color] = HeroesRealmObjects.Tower(color);
            }
            art.cover = Cover();
        }

        /// <summary>
        /// What is strewn over the open ground of each land: grass, flowers, clover and bushes on the meadows, reeds, ferns
        /// and bracket fungus in the bogs, pebbles and tufts on rough and dry ground, sand-pale stones and dry grass in the
        /// desert, a few stones under the snow, ash-grey ones in the wastes. Small pieces cast no shadow.
        /// </summary>
        private static List<HeroesArt.CoverArt> Cover()
        {
            var made = new Dictionary<string, GameObject>();
            GameObject Piece(string name, string model, float height, Color tint = default, bool dry = false)
            {
                if (made.TryGetValue(name, out GameObject cached))
                {
                    return cached;
                }
                GameObject root = HeroesArtBuilder.Root(name);
                GameObject piece = HeroesArtBuilder.Piece(root.transform, $"{Nature}/{model}", Vector3.zero, 0f, 1f, height, tint);
                if (piece != null && dry)
                {
                    HeroesKitArt.Swap(piece, "Grass", HeroesKitArt.Repainted("Nature", "Grass", -1, "Dry",
                        HeroesKitArt.Hue(0.12f, 0.5f, 0.08f, new Color(0.8f, 0.68f, 0.4f)), tint));
                }
                if (piece != null && height < 0.5f)
                {
                    foreach (Renderer renderer in piece.GetComponentsInChildren<Renderer>(true))
                    {
                        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                }
                GameObject prefab = HeroesArtBuilder.Save(root, $"{Generated}/Scenery/Cover/{name}.prefab");
                made[name] = prefab;
                return prefab;
            }

            var sand = new Color(0.96f, 0.86f, 0.66f);
            var snow = new Color(0.9f, 0.94f, 1f);
            var ash = new Color(0.62f, 0.57f, 0.54f);
            GameObject grass = Piece("Grass", "Grass_Common_Short", 0.32f);
            GameObject tuft = Piece("Tuft", "Grass_Wispy_Short", 0.3f);
            GameObject flowers = Piece("Flowers", "Flower_3_Group", 0.24f);
            GameObject bluebells = Piece("Bluebells", "Flower_4_Group", 0.28f);
            GameObject clover = Piece("Clover", "Clover_1", 0.22f);
            GameObject bush = Piece("Bush", "Bush_Common", 0.5f);
            GameObject flowering = Piece("FloweringBush", "Bush_Common_Flowers", 0.5f);
            GameObject mushroom = Piece("Mushroom", "Mushroom_Common", 0.12f);
            GameObject pebble = Piece("Pebble", "Pebble_Round_1", 0.07f);
            GameObject pebbles = Piece("Pebbles", "Pebble_Round_3", 0.08f);
            GameObject stone = Piece("Stone", "Pebble_Square_1", 0.09f);
            GameObject stones = Piece("Stones", "Pebble_Square_3", 0.1f);
            GameObject boulder = Piece("SmallRock", "Rock_Medium_2", 0.28f);
            GameObject reeds = Piece("Reeds", "Grass_Wispy_Tall", 0.6f);
            GameObject sedge = Piece("Sedge", "Grass_Common_Tall", 0.5f);
            GameObject fern = Piece("Fern", "Fern_1", 0.4f);
            GameObject fungus = Piece("Fungus", "Mushroom_Laetiporus", 0.16f);
            GameObject dryGrass = Piece("DryGrass", "Grass_Wispy_Short", 0.3f, default, true);
            GameObject sandStone = Piece("SandStone", "Pebble_Square_2", 0.09f, sand);
            GameObject snowStone = Piece("SnowStone", "Pebble_Round_2", 0.08f, snow);
            GameObject ashStone = Piece("AshStone", "Pebble_Square_4", 0.09f, ash);
            GameObject ashGrass = Piece("AshGrass", "Grass_Wispy_Short", 0.26f, ash, true);

            // Low enough to strew over a battlefield's cells, under the troops.
            var low = new HashSet<GameObject> { grass, tuft, clover, pebble, pebbles, stone, stones, dryGrass, sandStone, snowStone, ashStone, ashGrass };
            HeroesArt.CoverArt Land(TerrainType terrain, float density, params GameObject[] prefabs)
            {
                GameObject[] all = prefabs.Where(prefab => prefab != null).ToArray();
                return new HeroesArt.CoverArt { terrain = terrain, density = density, prefabs = all, low = all.Where(low.Contains).Distinct().ToArray() };
            }
            return new List<HeroesArt.CoverArt>
            {
                Land(TerrainType.Grass, 1.5f, grass, tuft, grass, tuft, clover, flowers, bluebells, bush, flowering, grass, mushroom),
                Land(TerrainType.Dirt, 0.9f, tuft, pebble, pebbles, bush, clover, grass),
                Land(TerrainType.Rough, 1f, stone, stones, pebble, tuft, boulder),
                Land(TerrainType.Sand, 0.6f, dryGrass, sandStone, dryGrass),
                Land(TerrainType.Snow, 0.35f, snowStone),
                Land(TerrainType.Swamp, 1.3f, reeds, sedge, fungus, fern, reeds),
                Land(TerrainType.Wasteland, 0.7f, ashStone, ashGrass, ashStone),
                Land(TerrainType.Rock, 0.6f, stone, stones, pebble)
            };
        }

        /// <summary>A banner on its pole in a player's colour (white for nobody): the props kit's, its cloth dyed.</summary>
        internal static GameObject Banner(int color)
        {
            GameObject root = HeroesArtBuilder.Root($"Flag{color}");
            GameObject piece = HeroesKitArt.Stand(root.transform, $"{Props}/Banner_1_Cloth", Vector3.zero, 0f, 1.9f);
            Color dye = color < 4 ? HeroesTheme.Palette.Active.Player(color) : new Color(0.9f, 0.9f, 0.88f);
            if (Spec.Palette != null && Spec.Palette.players != null && color < Spec.Palette.players.Length)
            {
                dye = color < 4 ? Spec.Palette.players[color] : new Color(0.9f, 0.9f, 0.88f);
            }
            HeroesKitArt.Swap(piece, "MI_Banner", HeroesKitArt.Plain("Props", "MI_Banner", dye));
            return HeroesArtBuilder.Save(root, $"{Generated}/Scenery/Flag{color}.prefab");
        }
    }
}
