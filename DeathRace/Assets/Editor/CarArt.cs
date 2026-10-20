using Gamebox.Editor;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// The car side of a look: the palette and materials of the cars in each look (day and night), the shared glows of
    /// the cars (headlight beams, tail glow, shadow, nitro flame), the pictures of the twelve cars, and what
    /// <see cref="DeathRaceThemeBuilder"/> asks of them for each theme (<see cref="Dress"/>).
    /// </summary>
    internal static partial class CarArt
    {
        public const string Folder = "Art/Cars";
        public const string MeshFolder = Folder + "/Meshes";
        public const string TextureFolder = Folder + "/Textures";
        public const string MaterialFolder = Folder + "/Materials";
        public const string PictureFolder = Folder + "/Pictures";

        /// <summary>The folder of a look's car materials and pictures.</summary>
        public static string Look(bool night)
        {
            return night ? "Night" : "Day";
        }

        public static Material PaletteMaterial(bool night) => DeathRaceAssets.Load<Material>($"{MaterialFolder}/{Look(night)}/CarPalette.mat");

        public static Material PaintMaterial(bool night) => DeathRaceAssets.Load<Material>($"{MaterialFolder}/{Look(night)}/CarPaint.mat");

        public static Material WreckMaterial(bool night) => DeathRaceAssets.Load<Material>($"{MaterialFolder}/{Look(night)}/CarWreck.mat");

        public static Material Shared(string name) => DeathRaceAssets.Load<Material>($"{MaterialFolder}/{name}.mat");

        public static Texture2D PaletteMap => DeathRaceAssets.Load<Texture2D>($"{TextureFolder}/Palette.png");

        public static Texture2D PaletteGlow => DeathRaceAssets.Load<Texture2D>($"{TextureFolder}/PaletteGlow.png");

        /// <summary>
        /// The colour each car wears in the garage's pictures (in a race it wears its driver's): the rust of the
        /// Rustbucket, the Hornet's yellow, the Bulldog's green, the Viper's red, the Reaper's black, the Tyrant's crimson,
        /// the Jackal's beach teal, the Mauler's security blue, the Banshee's plum, the Spectre's slate, the Goliath's
        /// olive drab and the Apocalypse's pearl white.
        /// </summary>
        public static Color SignatureColor(CarModel model)
        {
            switch (model)
            {
                case CarModel.Rustbucket: return new Color(0.78f, 0.45f, 0.22f);
                case CarModel.Hornet: return new Color(0.98f, 0.76f, 0.1f);
                case CarModel.Bulldog: return new Color(0.24f, 0.52f, 0.3f);
                case CarModel.Viper: return new Color(0.84f, 0.12f, 0.1f);
                case CarModel.Reaper: return new Color(0.2f, 0.2f, 0.24f);
                case CarModel.Tyrant: return new Color(0.55f, 0.07f, 0.09f);
                case CarModel.Jackal: return new Color(0.1f, 0.62f, 0.66f);
                case CarModel.Mauler: return new Color(0.2f, 0.3f, 0.52f);
                case CarModel.Banshee: return new Color(0.44f, 0.14f, 0.62f);
                case CarModel.Spectre: return new Color(0.3f, 0.34f, 0.4f);
                case CarModel.Goliath: return new Color(0.42f, 0.43f, 0.22f);
                default: return new Color(0.9f, 0.9f, 0.92f);
            }
        }

        // ------------------------------------------------------------------ materials

        /// <summary>The palette textures, both looks' car materials and the shared glow materials of the cars.</summary>
        public static void BuildMaterials()
        {
            Texture2D map = DeathRaceAssets.SaveTexture(Palette.Albedo("CarPalette"), $"{TextureFolder}/Palette.png", PaletteImport);
            Texture2D glow = DeathRaceAssets.SaveTexture(Palette.Emission("CarPaletteGlow"), $"{TextureFolder}/PaletteGlow.png", PaletteImport);
            foreach (bool night in new[] { false, true })
            {
                string folder = $"{MaterialFolder}/{Look(night)}";
                // The palette: lamps barely lit by day, bright at night (the lamp renderers brighten their own further).
                DeathRaceAssets.SaveMaterial($"{folder}/CarPalette.mat", Lit, m =>
                    Surface(m, Color.white, map, night ? 0.45f : 0.34f, 0.08f, glow, Color.white * (night ? 1.6f : 0.4f)));
                // The paint: glossy (it catches the sun by day and the neon by night), tinted per car.
                DeathRaceAssets.SaveMaterial($"{folder}/CarPaint.mat", Lit, m =>
                    Surface(m, Color.white, map, night ? 0.86f : 0.8f, night ? 0.32f : 0.24f, null, null, true));
                // A wreck: the same palette, charred.
                DeathRaceAssets.SaveMaterial($"{folder}/CarWreck.mat", Lit, m =>
                    Surface(m, night ? new Color(0.15f, 0.13f, 0.12f) : new Color(0.2f, 0.18f, 0.16f), map, 0.08f, 0.15f));
            }

            Texture2D beam = DeathRaceAssets.SaveTexture(BeamTexture(), $"{TextureFolder}/Beam.png", SoftImport);
            Texture2D dot = DeathRaceAssets.SaveTexture(GlowTexture(), $"{TextureFolder}/Glow.png", SoftImport);
            Texture2D shadow = DeathRaceAssets.SaveTexture(ShadowTexture(), $"{TextureFolder}/Shadow.png", SoftImport);
            DeathRaceAssets.SaveMaterial($"{MaterialFolder}/Beam.mat", ParticlesUnlit, m => Transparent(m, beam, new Color(1f, 0.93f, 0.78f, 0.5f), Blending.Additive, false, -2));
            DeathRaceAssets.SaveMaterial($"{MaterialFolder}/TailGlow.mat", ParticlesUnlit, m => Transparent(m, dot, new Color(1f, 0.12f, 0.08f, 0.35f), Blending.Additive, false, -2));
            DeathRaceAssets.SaveMaterial($"{MaterialFolder}/Shadow.mat", ParticlesUnlit, m => Transparent(m, shadow, new Color(0f, 0f, 0f, 0.6f), Blending.Alpha, false, -5));
            DeathRaceAssets.SaveMaterial($"{MaterialFolder}/NitroFlame.mat", ParticlesUnlit, m => Transparent(m, dot, Color.white, Blending.Additive));

            DeathRaceAssets.SaveMesh(CarMeshes.BeamMesh(), $"{MeshFolder}/Beam.asset");
            DeathRaceAssets.SaveMesh(CarMeshes.GroundQuad(1f, 1f), $"{MeshFolder}/GroundQuad.asset");
            DeathRaceAssets.SaveMesh(CarMeshes.FlameMesh(), $"{MeshFolder}/NitroFlame.asset");
        }

        /// <summary>The light of a headlight on the road: bright near the car, fading with distance and to the edges.</summary>
        private static Texture2D BeamTexture()
        {
            return ToTexture(64, 128, (u, v) =>
            {
                float across = Mathf.Abs(u - 0.5f) * 2f;
                float edge = 1f - Smooth(0.45f, 1f, across);
                float near = Smooth(0f, 0.08f, v);
                float far = Mathf.Pow(1f - v, 1.6f);
                float core = 1f + 0.6f * (1f - Smooth(0f, 0.5f, across)) * (1f - v);
                float a = Mathf.Clamp01(edge * near * far * core * 0.9f);
                return new Color(1f, 1f, 1f, a);
            });
        }

        /// <summary>A soft round glow.</summary>
        private static Texture2D GlowTexture()
        {
            return ToTexture(64, 64, (u, v) =>
            {
                float r = Mathf.Clamp01(new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f);
                float a = Mathf.Pow(1f - r, 2.2f);
                return new Color(1f, 1f, 1f, a);
            });
        }

        /// <summary>A soft rounded rectangle: the contact shadow under a car.</summary>
        private static Texture2D ShadowTexture()
        {
            return ToTexture(64, 128, (u, v) =>
            {
                var p = new Vector2((u - 0.5f) * 2f, (v - 0.5f) * 2f);
                float d = Sd.Box(p, Vector2.zero, new Vector2(0.62f, 0.78f), 0.3f);
                float a = 1f - Smooth(-0.18f, 0.2f, d);
                return new Color(0f, 0f, 0f, a * a);
            });
        }

        // ------------------------------------------------------------------ the theme's car fields

        /// <summary>
        /// Dresses <paramref name="theme"/> with the car materials, car pictures and effect colours of its look. Call it
        /// from inside the theme's SaveScriptable setup, after the art and prefab builders ran (missing pictures stay
        /// null and a warning names them).
        /// </summary>
        public static void Dress(DeathRaceTheme theme, bool night)
        {
            if (theme == null)
            {
                return;
            }
            theme.carPalette = PaletteMaterial(night);
            theme.carPaint = PaintMaterial(night);
            theme.carWreck = WreckMaterial(night);
            var pictures = new Sprite[CarSpecs.Count];
            for (int i = 0; i < pictures.Length; i++)
            {
                pictures[i] = Picture((CarModel)i, night);
                if (pictures[i] == null)
                {
                    Debug.LogWarning($"Death Race: no picture of the {(CarModel)i} for the {Look(night)} look yet (run Build Everything).");
                }
            }
            theme.carPictures = pictures;
            theme.tracer = night ? new Color(1f, 0.74f, 0.32f, 1f) : new Color(1f, 0.88f, 0.5f, 1f);
            theme.muzzle = night ? new Color(1f, 0.68f, 0.28f, 1f) : new Color(1f, 0.8f, 0.4f, 1f);
            theme.explosion = night ? new Color(1f, 0.5f, 0.16f, 1f) : new Color(1f, 0.58f, 0.2f, 1f);
            theme.sparks = night ? new Color(1f, 0.78f, 0.4f, 1f) : new Color(1f, 0.86f, 0.52f, 1f);
        }

        public static Sprite Picture(CarModel model, bool night)
        {
            return DeathRaceAssets.LoadSprite($"{PictureFolder}/{Look(night)}/{model}.png");
        }

        /// <summary>A throwaway theme that carries one look's car materials (for pictures and previews).</summary>
        public static DeathRaceTheme PreviewTheme(bool night)
        {
            var theme = ScriptableObject.CreateInstance<DeathRaceTheme>();
            theme.hideFlags = HideFlags.HideAndDontSave;
            theme.night = night;
            theme.carPalette = PaletteMaterial(night);
            theme.carPaint = PaintMaterial(night);
            theme.carWreck = WreckMaterial(night);
            return theme;
        }

        // ------------------------------------------------------------------ pictures

        /// <summary>The cars seen from above at an angle, transparent, per look (the garage, the dealer, the HUD).</summary>
        public static void RenderPictures()
        {
            foreach (bool night in new[] { false, true })
            {
                DeathRaceTheme theme = PreviewTheme(night);
                try
                {
                    using (var studio = new ModelStudio())
                    {
                        for (int i = 0; i < CarSpecs.Count; i++)
                        {
                            var model = (CarModel)i;
                            GameObject prefab = DeathRacePrefabBuilder.CarPrefab(model);
                            if (prefab == null)
                            {
                                continue;
                            }
                            Texture2D picture = studio.Shoot(prefab, PictureShot(night), car => Pose(car, theme, SignatureColor(model)));
                            string path = $"{PictureFolder}/{Look(night)}/{model}.png";
                            if (SameAsSaved(picture, path))
                            {
                                Object.DestroyImmediate(picture);
                                continue;
                            }
                            DeathRaceAssets.SaveTexture(picture, path, GameAssets.SpriteTexture());
                        }
                    }
                }
                finally
                {
                    Object.DestroyImmediate(theme);
                }
            }
        }

        /// <summary>
        /// Whether a fresh render matches the picture already saved within a hair (the GPU's last bits vary from run to
        /// run), so a rebuild leaves the file, and the editors importing it, alone.
        /// </summary>
        private static bool SameAsSaved(Texture2D picture, string relative)
        {
            string file = GameAssets.FullPath(DeathRaceAssets.Path(relative));
            if (!System.IO.File.Exists(file))
            {
                return false;
            }
            var saved = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!saved.LoadImage(System.IO.File.ReadAllBytes(file)) || saved.width != picture.width || saved.height != picture.height)
                {
                    return false;
                }
                Color32[] a = saved.GetPixels32();
                Color32[] b = picture.GetPixels32();
                for (int i = 0; i < a.Length; i++)
                {
                    if (Mathf.Abs(a[i].r - b[i].r) > 3 || Mathf.Abs(a[i].g - b[i].g) > 3 || Mathf.Abs(a[i].b - b[i].b) > 3 || Mathf.Abs(a[i].a - b[i].a) > 3)
                    {
                        return false;
                    }
                }
                return true;
            }
            finally
            {
                Object.DestroyImmediate(saved);
            }
        }

        /// <summary>Paints a placed car for a picture: the look's materials, wheels turned a little, no beams on the road.</summary>
        public static void Pose(GameObject car, DeathRaceTheme theme, Color paint)
        {
            var view = car.GetComponent<CarModelView>();
            if (view == null)
            {
                return;
            }
            view.ApplyTheme(theme, paint);
            view.SetWheels(-14f, 0f);
            foreach (Renderer beam in view.beams)
            {
                beam.enabled = false;
            }
            foreach (Renderer glow in view.tailGlows)
            {
                glow.enabled = false;
            }
        }

        public static StudioShot PictureShot(bool night)
        {
            var shot = new StudioShot
            {
                Width = 512,
                Height = 384,
                Yaw = 215f,
                Pitch = 42f,
                Padding = 0.04f,
                Supersample = 3
            };
            if (night)
            {
                shot.KeyColor = new Color(0.78f, 0.84f, 1f);
                shot.Key = 1.15f;
                shot.FillColor = new Color(0.9f, 0.35f, 0.75f);
                shot.Fill = 0.55f;
                shot.RimColor = new Color(0.35f, 0.9f, 1f);
                shot.Rim = 1.5f;
                shot.AmbientSky = new Color(0.32f, 0.36f, 0.5f);
                shot.AmbientEquator = new Color(0.26f, 0.24f, 0.34f);
                shot.AmbientGround = new Color(0.12f, 0.1f, 0.14f);
            }
            else
            {
                shot.KeyColor = new Color(1f, 0.93f, 0.8f);
                shot.Key = 1.45f;
                shot.FillColor = new Color(0.7f, 0.76f, 0.92f);
                shot.Fill = 0.45f;
                shot.RimColor = new Color(1f, 0.85f, 0.6f);
                shot.Rim = 1f;
            }
            return shot;
        }
    }
}
