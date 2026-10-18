using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The art of the armory (the new weapons of both modes and the new Supply Room items): the Ion Wave's crescent, the
    /// glows of the new shots and of the lightning, the icon materials of the new pickups, and the new sounds. The icons
    /// themselves are drawn with the others (<see cref="SpaceIcons.IconNames"/>, and every <see cref="StrikeItem"/> by the
    /// strike art), so the generated themes trace them like the rest. Everything lands in the Classic folders before the
    /// theme art runs.
    /// </summary>
    internal static partial class AsteroidsArtBuilder
    {
        /// <summary>The HUD icon of every <see cref="WeaponType"/>, in enum order (Art/Icons/{name}.png).</summary>
        public static readonly string[] WeaponIconNames = { "Blaster", "Laser", "Scatter", "Missile", "Arc", "Rail", "Flak", "Wave" };

        /// <summary>The icons of the armory's field pickups, shown inside the pickup frames (materials Icon{name}).</summary>
        private static readonly string[] ArmoryPickupIcons = { "Arc", "Rail", "Flak", "Wave", "Piercing", "Aegis" };


        static partial void BuildArmoryArt()
        {
            AsteroidsAssets.SaveTexture(CrescentTexture(), "Art/Textures/Crescent.png", AsteroidsAssets.ParticleTexture);

            Texture2D plasma = Texture("Plasma");
            Texture2D bolt = Texture("Bolt");
            GlowMaterial("ArcSpark", plasma, new Color(2.2f, 1.6f, 3.6f, 1f), true);
            GlowMaterial("RailSlug", bolt, new Color(2.6f, 3.1f, 3.8f, 1f), true);
            GlowMaterial("FlakShell", plasma, new Color(3.2f, 2.2f, 0.6f, 1f), true);
            GlowMaterial("FlakShrapnel", bolt, new Color(3.2f, 2f, 0.5f, 1f), true);
            GlowMaterial("IonWave", Texture("Crescent"), new Color(0.3f, 2.2f, 1.7f, 1f), true);
            // White: the lightning takes its colour from the line's own (the Glow shader multiplies the vertex colour). The
            // core is alpha blended, so a bolt still shows over bright sand and snow; its glow adds light.
            GlowMaterial("Lightning", StrikeTexture("Beam"), new Color(1.6f, 1.6f, 1.8f, 1f), true);
            GlowMaterial("LightningCore", StrikeTexture("Beam"), new Color(1.4f, 1.4f, 1.6f, 1f), false);
            StrikeGlow("NapalmFire", Texture("Glow"), new Color(3f, 1.1f, 0.25f, 0.6f), true);
            StrikeGlow("NapalmScorch", StrikeTexture("Crater"), new Color(0.45f, 0.32f, 0.25f, 0.85f), false);
            StrikeGlow("ClusterGlow", Texture("Glow"), new Color(2.6f, 1.2f, 0.35f, 0.4f), true);

            foreach (string icon in ArmoryPickupIcons)
            {
                GlowMaterial($"Icon{icon}", AsteroidsAssets.Load<Texture2D>($"Art/Icons/{icon}.png"), Color.white, false);
            }
            foreach (string name in SpaceSounds.ArmoryEffectNames)
            {
                AsteroidsAssets.SaveAudio(SpaceSounds.ArmoryEffect(name), $"Art/Audio/{name}.wav", false);
            }
        }


        /// <summary>
        /// The Ion Wave: a crescent bowed toward the top with a bright leading edge, a fainter trailing band and fine
        /// shimmer, fading to nothing at its horns.
        /// </summary>
        private static Texture2D CrescentTexture()
        {
            const int width = 128;
            const int height = 64;
            return SpaceTextures.Make(width, height, (x, y) =>
            {
                float u = (x + 0.5f) / width * 2f - 1f;
                float v = (y + 0.5f) / height * 2f - 1f;
                float r = Mathf.Sqrt(u * u + (v + 1.6f) * (v + 1.6f));
                float lead = Mathf.Clamp01(1f - Mathf.Abs(r - 2.22f) / 0.22f);
                float trail = Mathf.Clamp01(1f - Mathf.Abs(r - 1.88f) / 0.3f) * 0.55f;
                float body = r > 1.55f && r < 2.25f ? 0.18f : 0f;
                float horns = Mathf.Clamp01((1f - Mathf.Abs(u)) / 0.45f);
                float shimmer = 0.8f + 0.2f * Mathf.Sin(u * 23f + SpaceTextures.Hash(x, y, 313) * 2f);
                float a = Mathf.Clamp01(Mathf.Pow(lead, 1.5f) + trail + body) * horns * shimmer;
                return new Color(1f, 1f, 1f, a);
            });
        }
    }
}
