using System;
using Gamebox.Editor;
using UnityEditor;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// The pictures of the interface: plain white shapes (Art/UI), the sprites of each look's menus, windows and HUD
    /// (Art/UI/&lt;Look&gt;, drawn at twice their size and imported at 200 pixels per unit), each look's icons
    /// (Art/Icons/&lt;Look&gt;) with the bigger pictures of the three upgrades, the look's picture for the theme choice, and
    /// the launcher's emblem (Art/Icons/GameIcon.png).
    /// </summary>
    internal static partial class DeathRaceArtBuilder
    {
        /// <summary>The upgrades' pictures, per UpgradeKind (Engine, Tires, Armor), by icon name.</summary>
        public static readonly string[] UpgradeIcons = { "Engine", "Tyres", "Armour" };

        private const float SpritePixelsPerUnit = 100f * DeathRaceArtInterface.Detail;

        /// <summary>A sprite of a look's interface folder (e.g. "Window").</summary>
        public static Sprite UI(DeathRaceThemeSpec spec, string name)
        {
            return DeathRaceAssets.LoadSprite($"{spec.InterfaceFolder}/{name}.png");
        }

        /// <summary>An icon of a look (e.g. "Rocket").</summary>
        public static Sprite Icon(DeathRaceThemeSpec spec, string name)
        {
            return DeathRaceAssets.LoadSprite($"{spec.IconsFolder}/{name}.png");
        }

        public static Sprite UpgradePicture(DeathRaceThemeSpec spec, string icon)
        {
            return DeathRaceAssets.LoadSprite($"{spec.IconsFolder}/Upgrade{icon}.png");
        }

        /// <summary>The drivers' portraits of a look: twenty (index = the driver's colour) and The Warden's last.</summary>
        public static Sprite[] Portraits(DeathRaceThemeSpec spec)
        {
            var portraits = new Sprite[DeathRaceArtPortraits.Warden + 1];
            for (int i = 0; i < portraits.Length; i++)
            {
                portraits[i] = DeathRaceAssets.LoadSprite(PortraitPath(spec, i));
            }
            return portraits;
        }

        private static string PortraitPath(DeathRaceThemeSpec spec, int index)
        {
            return $"{spec.IconsFolder}/Portraits/Driver{index:00}.png";
        }

        private static Action<TextureImporter> SpriteImport(Vector4 border, float pixelsPerUnit = 100f)
        {
            return importer =>
            {
                GameAssets.SpriteTexture(border)(importer);
                importer.spritePixelsPerUnit = pixelsPerUnit;
            };
        }

        private static void SaveSprite(Texture2D texture, string relative, Vector4 border, float pixelsPerUnit = SpritePixelsPerUnit)
        {
            DeathRaceAssets.SaveTexture(texture, relative, SpriteImport(border, pixelsPerUnit));
        }

        private static void BuildInterface()
        {
            SaveSprite(DeathRaceArtInterface.Rounded(128, 40f), $"{InterfaceFolder}/Rounded.png", Vector4.one * 48f, 100f);
            SaveSprite(DeathRaceArtInterface.Circle(128), $"{InterfaceFolder}/Circle.png", Vector4.zero, 100f);
            SaveSprite(DeathRaceArtInterface.Glow(256), $"{InterfaceFolder}/Glow.png", Vector4.zero, 100f);
            SaveSprite(DeathRaceArtInterface.Pill(112), $"{InterfaceFolder}/Pill.png", Vector4.one * 55f, 100f);
            SaveSprite(DeathRaceArtInterface.Fade(), $"{InterfaceFolder}/Fade.png", Vector4.zero, 100f);
            foreach (DeathRaceThemeSpec spec in DeathRaceThemeSpec.All)
            {
                string folder = spec.InterfaceFolder;
                SaveSprite(DeathRaceArtInterface.Window(spec), $"{folder}/Window.png", Vector4.one * 80f);
                SaveSprite(DeathRaceArtInterface.Panel(spec), $"{folder}/Panel.png", Vector4.one * 40f);
                SaveSprite(DeathRaceArtInterface.Row(spec), $"{folder}/Row.png", Vector4.one * 24f);
                SaveSprite(DeathRaceArtInterface.Button(spec, DeathRaceArtInterface.ButtonState.Normal), $"{folder}/Button.png", Vector4.one * 36f);
                SaveSprite(DeathRaceArtInterface.Button(spec, DeathRaceArtInterface.ButtonState.Hover), $"{folder}/ButtonHover.png", Vector4.one * 36f);
                SaveSprite(DeathRaceArtInterface.Button(spec, DeathRaceArtInterface.ButtonState.Pressed), $"{folder}/ButtonPressed.png", Vector4.one * 36f);
                SaveSprite(DeathRaceArtInterface.Button(spec, DeathRaceArtInterface.ButtonState.Disabled), $"{folder}/ButtonDisabled.png", Vector4.one * 36f);
                SaveSprite(DeathRaceArtInterface.Button(spec, DeathRaceArtInterface.ButtonState.Normal, true), $"{folder}/Arrow.png", Vector4.one * 36f);
                SaveSprite(DeathRaceArtInterface.Banner(spec), $"{folder}/Banner.png", new Vector4(160f, 60f, 160f, 60f));
                SaveSprite(DeathRaceArtInterface.Track(spec), $"{folder}/Track.png", Vector4.one * 14f);
                SaveSprite(DeathRaceArtInterface.Fill(spec), $"{folder}/Fill.png", Vector4.one * 14f);
                SaveSprite(DeathRaceArtInterface.Knob(spec), $"{folder}/Knob.png", Vector4.zero);
                SaveSprite(DeathRaceArtInterface.CheckBox(spec), $"{folder}/CheckBox.png", Vector4.zero);
                SaveSprite(DeathRaceArtInterface.CheckMark(spec), $"{folder}/CheckMark.png", Vector4.zero);
                SaveSprite(DeathRaceArtInterface.InputBox(spec), $"{folder}/InputBox.png", Vector4.one * 24f);
                SaveSprite(DeathRaceArtInterface.BarFrame(spec), $"{folder}/BarFrame.png", new Vector4(24f, 20f, 24f, 20f));
                SaveSprite(DeathRaceArtInterface.BarFill(spec), $"{folder}/BarFill.png", new Vector4(24f, 20f, 24f, 20f));
                SaveSprite(DeathRaceArtInterface.Backdrop(spec), $"{folder}/Backdrop.png", Vector4.zero, 100f);
                SaveSprite(DeathRaceArtIcons.GameIcon(256, spec), $"{folder}/Preview.png", Vector4.zero, 100f);
                foreach (string icon in DeathRaceArtIcons.Names)
                {
                    SaveSprite(DeathRaceArtIcons.Draw(icon, 128, spec), $"{spec.IconsFolder}/{icon}.png", Vector4.zero, 100f);
                }
                foreach (string icon in UpgradeIcons)
                {
                    SaveSprite(DeathRaceArtIcons.Draw(icon, 256, spec), $"{spec.IconsFolder}/Upgrade{icon}.png", Vector4.zero, 100f);
                }
                for (int i = 0; i <= DeathRaceArtPortraits.Warden; i++)
                {
                    SaveSprite(DeathRaceArtPortraits.Draw(i, spec), PortraitPath(spec, i), Vector4.zero, 100f);
                }
            }
            SaveSprite(DeathRaceArtIcons.GameIcon(256, DeathRaceThemeSpec.Scorched), $"{IconsFolder}/GameIcon.png", Vector4.zero, 100f);
        }
    }
}
