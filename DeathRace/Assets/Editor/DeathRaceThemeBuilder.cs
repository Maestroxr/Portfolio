using System;
using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// Writes the two looks (Scorched, the starting one, and Nightfall) from the generated art and
    /// <see cref="DeathRaceThemeSpec"/>: the light, sky, fog and post processing; the land of every biome (surfaces,
    /// scenery, lamp, minimap colours); the colours of the people; the car side through ART-CARS'
    /// <see cref="CarArt.Dress"/>; the interface sprites and icons; the fonts; the skin of the shared menu and of every
    /// window built in code (<see cref="Gamebox.UI.OverlayLook"/>: the lobby, the garage, the results, the HUD's panels).
    /// Extra pictures the game's windows may ask for by key (theme.SpriteOf) are in the theme's slots: the icons (e.g.
    /// "Spikes", "Trophy") and the interface sprites (e.g. "Window", "Row", "Button"); the slot texture "reflection" is the
    /// sky cube map the scene reflects (DeathRaceManager.Look.cs) and the slot colours "Second", "Good", "Muted", "Plate"
    /// and "PlateDark" round off the palette of the interface.
    /// </summary>
    internal static class DeathRaceThemeBuilder
    {
        public const string ScorchedPath = "Config/Themes/Scorched.asset";
        public const string NightfallPath = "Config/Themes/Nightfall.asset";

        /// <summary>The key of the reflected sky in a theme's texture slots.</summary>
        public const string ReflectionSlot = "reflection";

        /// <summary>The interface sprites also handed out by key.</summary>
        private static readonly string[] SpriteSlots =
        {
            "Window", "Panel", "Row", "Button", "ButtonHover", "ButtonPressed", "ButtonDisabled", "Arrow", "Banner", "Track", "Fill", "Knob",
            "CheckBox", "CheckMark", "InputBox", "BarFrame", "BarFill", "Backdrop"
        };

        /// <summary>The fields CarArt.Dress fills; while ART-CARS' art is not there their problems are warnings.</summary>
        private static readonly string[] CarFields = { "carPalette", "carPaint", "carWreck", "carPictures" };

        /// <summary>Builds (or updates in place) every theme, the starting one first.</summary>
        public static List<DeathRaceTheme> BuildThemes()
        {
            var themes = new List<DeathRaceTheme>();
            foreach (DeathRaceThemeSpec spec in DeathRaceThemeSpec.All)
            {
                themes.Add(BuildTheme(spec));
            }
            return themes;
        }

        /// <summary>The theme the game starts with.</summary>
        public static DeathRaceTheme Starting => DeathRaceAssets.Load<DeathRaceTheme>(ScorchedPath);

        private static DeathRaceTheme BuildTheme(DeathRaceThemeSpec spec)
        {
            DeathRaceTheme theme = DeathRaceAssets.SaveScriptable<DeathRaceTheme>(spec.AssetPath, t =>
            {
                DeathRaceAssets.SetString(t, "displayName", spec.Name);
                DeathRaceAssets.SetString(t, "description", spec.Description);
                DeathRaceAssets.SetObject(t, "preview", DeathRaceArtBuilder.UI(spec, "Preview"));
                FillLight(t, spec);
                FillLand(t, spec);
                FillPeople(t, spec);
                FillInterface(t, spec);
                FillFonts(t, spec);
                FillMenu(t.Menu, spec);
                FillLobby(t.Lobby, spec);
                FillSlots(t, spec);
                CarArt.Dress(t, spec.Night);
            });
            var problems = new List<string>();
            if (!theme.Validate(problems))
            {
                var mine = new List<string>();
                foreach (string problem in problems)
                {
                    if (Array.Exists(CarFields, field => problem.Contains("." + field)))
                    {
                        Debug.LogWarning($"Death Race theme {spec.Name}: {problem} (the car art of ART-CARS)");
                    }
                    else
                    {
                        mine.Add(problem);
                    }
                }
                if (mine.Count > 0)
                {
                    throw new InvalidOperationException($"Theme {spec.Name} is not complete: {string.Join("; ", mine)}");
                }
            }
            return theme;
        }

        private static void FillLight(DeathRaceTheme t, DeathRaceThemeSpec spec)
        {
            t.night = spec.Night;
            t.sunColor = spec.SunColor;
            t.sunIntensity = spec.SunIntensity;
            t.sunEuler = spec.SunEuler;
            t.ambientSky = spec.AmbientSky;
            t.ambientEquator = spec.AmbientEquator;
            t.ambientGround = spec.AmbientGround;
            t.background = spec.Background;
            t.fog = spec.Fog;
            t.fogStart = spec.FogStart;
            t.fogEnd = spec.FogEnd;
            t.volume = DeathRaceArtBuilder.Volume(spec);
        }

        private static void FillLand(DeathRaceTheme t, DeathRaceThemeSpec spec)
        {
            var biomes = (Biome[])Enum.GetValues(typeof(Biome));
            t.biomes = new BiomeLook[biomes.Length];
            foreach (Biome biome in biomes)
            {
                BiomeSpec look = spec.Of(biome);
                t.biomes[(int)biome] = new BiomeLook
                {
                    ground = DeathRaceArtBuilder.SurfaceMaterial(spec, biome, DeathRaceArtBuilder.Ground),
                    road = DeathRaceArtBuilder.SurfaceMaterial(spec, biome, DeathRaceArtBuilder.Road),
                    shoulder = DeathRaceArtBuilder.SurfaceMaterial(spec, biome, DeathRaceArtBuilder.Shoulder),
                    curb = DeathRaceArtBuilder.SurfaceMaterial(spec, biome, DeathRaceArtBuilder.Curb),
                    barrier = DeathRaceArtBuilder.SurfaceMaterial(spec, biome, DeathRaceArtBuilder.Barrier),
                    line = DeathRaceArtBuilder.SurfaceMaterial(spec, biome, DeathRaceArtBuilder.Line),
                    props = DeathRaceArtBuilder.Props(spec, biome),
                    landmarks = DeathRaceArtBuilder.Landmarks(spec, biome),
                    lamp = DeathRaceArtBuilder.Lamp(spec, biome),
                    minimapRoad = look.MinimapRoad,
                    minimapEdge = look.MinimapEdge,
                    groundTint = look.GroundTint,
                    fogColor = look.Fog,
                    propDensity = look.PropDensity
                };
            }
            t.propPalette = DeathRaceArtBuilder.PropMaterial(spec);
        }

        private static void FillPeople(DeathRaceTheme t, DeathRaceThemeSpec spec)
        {
            t.seatColors = (Color[])spec.SeatColors.Clone();
            t.rivalColors = (Color[])spec.RivalColors.Clone();
        }

        private static void FillInterface(DeathRaceTheme t, DeathRaceThemeSpec spec)
        {
            t.hudPanel = DeathRaceArtBuilder.UI(spec, "Panel");
            t.barFrame = DeathRaceArtBuilder.UI(spec, "BarFrame");
            t.barFill = DeathRaceArtBuilder.UI(spec, "BarFill");
            t.iconAmmo = DeathRaceArtBuilder.Icon(spec, "Ammo");
            t.iconMine = DeathRaceArtBuilder.Icon(spec, "Mine");
            t.iconRocket = DeathRaceArtBuilder.Icon(spec, "Rocket");
            t.iconNitro = DeathRaceArtBuilder.Icon(spec, "Nitro");
            t.iconCash = DeathRaceArtBuilder.Icon(spec, "Cash");
            t.iconRepair = DeathRaceArtBuilder.Icon(spec, "Repair");
            t.iconHealth = DeathRaceArtBuilder.Icon(spec, "Health");
            t.iconFlag = DeathRaceArtBuilder.Icon(spec, "Flag");
            t.iconSkull = DeathRaceArtBuilder.Icon(spec, "Skull");
            t.upgradePictures = new Sprite[DeathRaceArtBuilder.UpgradeIcons.Length];
            for (int i = 0; i < t.upgradePictures.Length; i++)
            {
                t.upgradePictures[i] = DeathRaceArtBuilder.UpgradePicture(spec, DeathRaceArtBuilder.UpgradeIcons[i]);
            }
            t.driverPortraits = DeathRaceArtBuilder.Portraits(spec);
            t.hudText = spec.HudText;
            t.hudAccent = spec.HudAccent;
            t.hudWarn = spec.HudWarn;
        }

        private static void FillFonts(DeathRaceTheme t, DeathRaceThemeSpec spec)
        {
            t.Fonts.body = DeathRaceArtBuilder.BoldFont;
            t.Fonts.bodyMaterial = DeathRaceArtBuilder.BoldFont.material;
            t.Fonts.title = DeathRaceArtBuilder.TitleFont;
            t.Fonts.titleMaterial = DeathRaceArtBuilder.TitleMaterial(spec);
        }

        /// <summary>
        /// The shared menu as a combat racer's: dark steel (Scorched) or dark glass (Nightfall) windows, the title on a
        /// hazard striped or neon banner, buttons that light up orange or cyan when chosen.
        /// </summary>
        private static void FillMenu(MenuSkin menu, DeathRaceThemeSpec spec)
        {
            Sprite Ui(string name)
            {
                return DeathRaceArtBuilder.UI(spec, name);
            }

            menu.backdrop = Ui("Backdrop");
            menu.backdropColor = Color.white;
            menu.window = Ui("Window");
            menu.windowColor = Color.white;
            menu.tileWindow = false;
            menu.settingsWindow = Ui("Window");
            menu.headerBanner = Ui("Banner");
            menu.rowsBackground = Ui("Panel");
            menu.rowsColor = new Color(1f, 1f, 1f, 0.9f);
            menu.rowBackground = Ui("Row");
            menu.rowColor = Color.white;
            menu.button = Ui("Button");
            menu.buttonHover = Ui("ButtonHover");
            menu.buttonPressed = Ui("ButtonPressed");
            menu.buttonDisabled = Ui("ButtonDisabled");
            menu.arrowButton = Ui("Arrow");
            menu.inputBox = Ui("InputBox");
            menu.inputColor = Color.white;
            menu.inputTextColor = spec.Ink;
            menu.track = Ui("Track");
            menu.fill = Ui("Fill");
            menu.fillColor = spec.Accent;
            menu.knob = Ui("Knob");
            menu.checkBox = Ui("CheckBox");
            menu.checkMark = Ui("CheckMark");
            menu.headerColor = spec.Night ? spec.Ink : spec.Second;
            menu.textColor = spec.Ink;
            menu.valueColor = spec.Night ? spec.Accent : spec.Second;
            menu.buttonTextColor = spec.ButtonInk;
            menu.errorColor = spec.Warn;
        }

        /// <summary>
        /// The windows built in code (the online lobby, local play, the garage, the results, the HUD's panels): the
        /// look's window, row and field sprites as drawn (no tint), buttons as solid shapes in their colours (accent, good,
        /// warn) so a Buy and a Sell differ, light words on them.
        /// </summary>
        private static void FillLobby(LobbySkin lobby, DeathRaceThemeSpec spec)
        {
            lobby.font = DeathRaceArtBuilder.BoldFont;
            lobby.bodyFont = DeathRaceArtBuilder.BodyFont;
            lobby.dim = spec.Night ? DeathRaceThemeSpec.Hex(0x02030A, 0.8f) : DeathRaceThemeSpec.Hex(0x0A0604, 0.78f);
            lobby.window = DeathRaceArtInterface.WithAlpha(spec.Plate, 0.97f);
            lobby.row = spec.Night ? spec.PlateLight : DeathRaceArtInterface.Shade(spec.PlateLight, 0.85f);
            lobby.accent = spec.Night ? DeathRaceThemeSpec.Hex(0x0E8FB0) : DeathRaceThemeSpec.Hex(0xC8641A);
            lobby.good = spec.Night ? DeathRaceThemeSpec.Hex(0x1FA36A) : DeathRaceThemeSpec.Hex(0x4F9A2C);
            lobby.warn = spec.Night ? DeathRaceThemeSpec.Hex(0xD6334F) : DeathRaceThemeSpec.Hex(0xC83A2A);
            lobby.text = spec.Ink;
            lobby.muted = spec.Muted;
            lobby.buttonText = spec.ButtonInk;
            lobby.windowSprite = DeathRaceArtBuilder.UI(spec, "Window");
            lobby.rowSprite = DeathRaceArtBuilder.UI(spec, "Row");
            lobby.buttonSprite = null;
            lobby.inputSprite = DeathRaceArtBuilder.UI(spec, "InputBox");
            lobby.tileSprites = false;
            lobby.replaceSprites = true;
            lobby.spriteTint = 0f;
        }

        private static void FillSlots(DeathRaceTheme t, DeathRaceThemeSpec spec)
        {
            ThemeSlots slots = t.Slots;
            foreach (string icon in DeathRaceArtIcons.Names)
            {
                ThemeSlots.Set(slots.sprites, icon, DeathRaceArtBuilder.Icon(spec, icon));
            }
            foreach (string sprite in SpriteSlots)
            {
                ThemeSlots.Set(slots.sprites, sprite, DeathRaceArtBuilder.UI(spec, sprite));
            }
            ThemeSlots.Set<Texture>(slots.textures, ReflectionSlot, DeathRaceArtBuilder.Sky(spec));
            slots.SetColor("Second", spec.Second);
            slots.SetColor("Good", spec.Good);
            slots.SetColor("Muted", spec.Muted);
            slots.SetColor("Plate", spec.Plate);
            slots.SetColor("PlateDark", spec.PlateDark);
        }
    }
}
