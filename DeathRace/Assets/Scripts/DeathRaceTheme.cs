using System;
using System.Collections.Generic;
using Gamebox;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.DeathRace
{
    /// <summary>The look of one kind of land in a theme: its surfaces, its scenery and its colours on the minimap.</summary>
    [Serializable]
    public sealed class BiomeLook
    {
        public Material ground;
        public Material road;
        public Material shoulder;
        public Material curb;
        public Material barrier;

        /// <summary>The start and finish line (a checker).</summary>
        public Material line;

        /// <summary>Scenery scattered outside the barriers.</summary>
        public GameObject[] props = new GameObject[0];

        /// <summary>Bigger set pieces, a few per track.</summary>
        public GameObject[] landmarks = new GameObject[0];

        /// <summary>A street lamp (its light is on at night).</summary>
        [ThemeOptional] public GameObject lamp;

        public Color minimapRoad = new Color(0.85f, 0.8f, 0.7f, 1f);
        public Color minimapEdge = new Color(0.1f, 0.08f, 0.06f, 1f);
        public Color groundTint = Color.white;
        public Color fogColor = new Color(0.8f, 0.7f, 0.55f, 1f);

        /// <summary>How thickly the scenery is scattered (1 = as designed).</summary>
        public float propDensity = 1f;
    }

    /// <summary>
    /// Everything Death Race shows that changes with its look: the light and the sky, the materials of the cars and of
    /// every kind of land, the colours of the effects and the pictures of the interface. Two looks: Scorched (day) and
    /// Nightfall (night, lights on). The cars, tracks and interface read it through the manager's <c>Look</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "DeathRaceTheme", menuName = "Death Race/Theme")]
    public class DeathRaceTheme : GameTheme
    {
        public override GameType Game => GameType.DeathRace;

        [Header("Light")]
        [Tooltip("Night: headlights, tail lights and street lamps are lit, and the barriers glow.")]
        public bool night;
        public Color sunColor = new Color(1f, 0.92f, 0.78f, 1f);
        public float sunIntensity = 1.3f;
        public Vector3 sunEuler = new Vector3(52f, -35f, 0f);
        public Color ambientSky = new Color(0.62f, 0.66f, 0.72f, 1f);
        public Color ambientEquator = new Color(0.55f, 0.48f, 0.4f, 1f);
        public Color ambientGround = new Color(0.3f, 0.24f, 0.18f, 1f);
        public Color background = new Color(0.55f, 0.47f, 0.36f, 1f);
        public bool fog = true;
        public float fogStart = 60f;
        public float fogEnd = 220f;
        [ThemeOptional] public VolumeProfile volume;

        [Header("Cars")]
        [Tooltip("The palette the car meshes are coloured with (emission lights them at night).")]
        public Material carPalette;
        [Tooltip("The painted parts of a car, tinted per car (_BaseColor through a property block).")]
        public Material carPaint;
        public Material carWreck;
        [Tooltip("The colours of the people of a split screen or online race.")]
        public Color[] seatColors = new Color[0];
        [Tooltip("The colours of the twenty drivers of the career (index = the driver's colour).")]
        public Color[] rivalColors = new Color[0];

        [Header("Land")]
        [Tooltip("Per Biome: Desert, Industrial, Snow, City.")]
        public BiomeLook[] biomes = new BiomeLook[0];
        [Tooltip("The palette the scenery meshes are coloured with.")]
        public Material propPalette;

        [Header("Effects")]
        public Color tracer = new Color(1f, 0.85f, 0.4f, 1f);
        public Color muzzle = new Color(1f, 0.75f, 0.3f, 1f);
        public Color explosion = new Color(1f, 0.55f, 0.15f, 1f);
        public Color sparks = new Color(1f, 0.8f, 0.45f, 1f);

        [Header("Interface")]
        public Sprite hudPanel;
        public Sprite barFrame;
        public Sprite barFill;
        public Sprite iconAmmo;
        public Sprite iconMine;
        public Sprite iconRocket;
        public Sprite iconNitro;
        public Sprite iconCash;
        public Sprite iconRepair;
        public Sprite iconHealth;
        public Sprite iconFlag;
        public Sprite iconSkull;
        [Tooltip("Per CarModel: the car seen from above.")]
        public Sprite[] carPictures = new Sprite[0];
        [Tooltip("Per UpgradeKind: engine, tyres, armour, nitro system, gun mount.")]
        public Sprite[] upgradePictures = new Sprite[0];
        [Tooltip("The twenty drivers' helmets (index = the driver's colour), then the champions: 20 The Warden, 21 The Iron Baron, 22 Nemesis.")]
        [ThemeOptional] public Sprite[] driverPortraits = new Sprite[0];
        public Color hudText = new Color(0.96f, 0.93f, 0.86f, 1f);
        public Color hudAccent = new Color(1f, 0.62f, 0.12f, 1f);
        public Color hudWarn = new Color(0.95f, 0.25f, 0.18f, 1f);

        public BiomeLook Biome(Biome biome)
        {
            int index = (int)biome;
            return biomes != null && index >= 0 && index < biomes.Length ? biomes[index] : null;
        }

        public Color SeatColor(int seat)
        {
            return seatColors != null && seatColors.Length > 0 ? seatColors[Mathf.Abs(seat) % seatColors.Length] : Color.white;
        }

        public Color RivalColor(int index)
        {
            return rivalColors != null && rivalColors.Length > 0 ? rivalColors[Mathf.Abs(index) % rivalColors.Length] : Color.gray;
        }

        public Sprite CarPicture(CarModel model)
        {
            int index = (int)model;
            return carPictures != null && index >= 0 && index < carPictures.Length ? carPictures[index] : null;
        }

        /// <summary>The picture of an upgrade, or null when the look has none (callers draw their own).</summary>
        public Sprite UpgradePicture(UpgradeKind kind)
        {
            int index = (int)kind;
            return upgradePictures != null && index >= 0 && index < upgradePictures.Length ? upgradePictures[index] : null;
        }

        /// <summary>A driver's portrait (0-19 by colour, 20-22 the champions), or null when the look has none.</summary>
        public Sprite DriverPortrait(int index)
        {
            return driverPortraits != null && index >= 0 && index < driverPortraits.Length ? driverPortraits[index] : null;
        }

        public Sprite PickupIcon(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.Cash: return iconCash;
                case PickupKind.Repair: return iconRepair;
                case PickupKind.Ammo: return iconAmmo;
                default: return iconNitro;
            }
        }

        public override bool Validate(List<string> problems)
        {
            bool valid = base.Validate(problems);
            int biomeCount = Enum.GetValues(typeof(Biome)).Length;
            if (biomes == null || biomes.Length != biomeCount)
            {
                problems.Add($"{name}: needs a look for each of the {biomeCount} biomes");
                valid = false;
            }
            if (carPictures == null || carPictures.Length != CarSpecs.Count)
            {
                problems.Add($"{name}: needs a picture of each of the {CarSpecs.Count} cars");
                valid = false;
            }
            int upgrades = Enum.GetValues(typeof(UpgradeKind)).Length;
            if (upgradePictures == null || upgradePictures.Length != upgrades)
            {
                problems.Add($"{name}: needs a picture of each of the {upgrades} upgrades");
                valid = false;
            }
            if (seatColors == null || seatColors.Length < 4)
            {
                problems.Add($"{name}: needs four seat colours");
                valid = false;
            }
            if (rivalColors == null || rivalColors.Length < 20)
            {
                problems.Add($"{name}: needs twenty driver colours");
                valid = false;
            }
            return valid;
        }
    }
}
