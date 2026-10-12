using System.Collections.Generic;
using Gamebox.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.Checkers.EditorTools
{
    /// <summary>
    /// Generates the art and sound of the game: the textures and materials of every look, the meshes the board and the
    /// pieces share, the interface sprites, the launcher icon and the six synthesised sounds. Everything is written in
    /// place through <see cref="CheckersAssets"/>, so a second run changes nothing.
    /// </summary>
    internal static class CheckersArtBuilder
    {
        public const string MeshesFolder = "Art/Meshes";
        public const string UIFolder = "Art/UI";
        public const string IconsFolder = "Art/Icons";
        public const string AudioFolder = "Audio/Generated";
        public const int SurfacePixels = 1024;

        /// <summary>Radius and height of a piece in board units (a square is one unit).</summary>
        public const float PieceRadius = 0.38f;
        public const float PieceHeight = 0.12f;

        private static Shader Lit => Shader.Find("Universal Render Pipeline/Lit");
        private static Shader Unlit => Shader.Find("Universal Render Pipeline/Unlit");

        private const string FontsFolder = "Prefabs/UI/MainMenu/Fonts";

        /// <summary>BaseGame's Poppins Black: titles and headers.</summary>
        public static TMP_FontAsset TitleFont => BaseFont("Poppins Black SDF");

        /// <summary>BaseGame's Poppins Bold: buttons and labels.</summary>
        public static TMP_FontAsset BoldFont => BaseFont("Poppins Bold SDF");

        /// <summary>BaseGame's Poppins SemiBold: body text, names and rows.</summary>
        public static TMP_FontAsset BodyFont => BaseFont("Poppins SemiBold SDF");

        private static TMP_FontAsset BaseFont(string name)
        {
            string path = GameMenuInstaller.PackagePath($"{FontsFolder}/{name}.asset");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null)
            {
                throw new System.InvalidOperationException($"BaseGame font {path} is missing");
            }
            return font;
        }

        public static void BuildAll()
        {
            Progress("Meshes", 0.05f);
            BuildMeshes();
            Progress("Interface sprites", 0.15f);
            BuildInterface();
            float step = 0.6f / CheckersThemeSpec.All.Length;
            for (int i = 0; i < CheckersThemeSpec.All.Length; i++)
            {
                CheckersThemeSpec spec = CheckersThemeSpec.All[i];
                Progress($"{spec.Name} textures", 0.2f + step * i);
                BuildTextures(spec);
                Progress($"{spec.Name} materials", 0.2f + step * i + step * 0.6f);
                BuildMaterials(spec);
            }
            Progress("Sounds", 0.85f);
            BuildSounds();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static Mesh Mesh(string name)
        {
            return CheckersAssets.Require<Mesh>($"{MeshesFolder}/{name}.asset");
        }

        public static Material Material(string name, CheckersThemeSpec spec)
        {
            return CheckersAssets.Require<Material>($"{spec.MaterialsFolder}/{name}.mat");
        }

        public static Texture2D Texture(string name, CheckersThemeSpec spec)
        {
            return CheckersAssets.Require<Texture2D>($"{spec.TexturesFolder}/{name}.png");
        }

        public static Sprite UI(string name)
        {
            return CheckersAssets.LoadSprite($"{UIFolder}/{name}.png");
        }

        public static Sprite Icon(string name)
        {
            return CheckersAssets.LoadSprite($"{IconsFolder}/{name}.png");
        }

        public static AudioClip Clip(string name)
        {
            return CheckersAssets.Require<AudioClip>($"{AudioFolder}/{name}.wav");
        }

        private static void BuildMeshes()
        {
            CheckersAssets.SaveMesh(PieceMesh().ToMesh("Piece"), $"{MeshesFolder}/Piece.asset");
            CheckersAssets.SaveMesh(CrownMesh().ToMesh("Crown"), $"{MeshesFolder}/Crown.asset");
            CheckersAssets.SaveMesh(MeshFactory.Plane(1f, 1f, new Rect(0f, 0f, 1f, 1f)).ToMesh("Square"), $"{MeshesFolder}/Square.asset");
            CheckersAssets.SaveMesh(MeshFactory.Box(Vector3.zero, new Vector3(1f, 0.3f, 1f)).ToMesh("Slab"), $"{MeshesFolder}/Slab.asset");
            CheckersAssets.SaveMesh(MeshFactory.Plane(40f, 40f, new Rect(0f, 0f, 20f, 20f)).ToMesh("Table"), $"{MeshesFolder}/Table.asset");
        }

        /// <summary>A bevelled disc standing on y = 0: flat bottom, rounded rim, a shallow dish on top.</summary>
        private static MeshBuilder PieceMesh()
        {
            float r = PieceRadius;
            float h = PieceHeight;
            float bevel = 0.03f;
            var profile = new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(r - bevel, 0f),
                new Vector2(r - bevel * 0.3f, bevel * 0.3f),
                new Vector2(r, bevel),
                new Vector2(r, h - bevel),
                new Vector2(r - bevel * 0.3f, h - bevel * 0.3f),
                new Vector2(r - bevel, h),
                new Vector2(r * 0.72f, h),
                new Vector2(r * 0.66f, h - 0.012f),
                new Vector2(r * 0.3f, h - 0.012f),
                new Vector2(0f, h - 0.012f)
            };
            return MeshFactory.Lathe(profile, 48);
        }

        /// <summary>The king's crown: a ring lying on top of the piece.</summary>
        private static MeshBuilder CrownMesh()
        {
            var ring = MeshFactory.Torus(PieceRadius * 0.5f, 0.022f, 40, 10);
            var hub = MeshFactory.Cylinder(PieceRadius * 0.16f, 0.03f, 0.006f, 24);
            return ring.Append(hub);
        }

        private static void BuildInterface()
        {
            CheckersAssets.SaveTexture(CheckersArt.Rounded(128, 40f), $"{UIFolder}/Rounded.png", GameAssets.SpriteTexture(new Vector4(48f, 48f, 48f, 48f)));
            CheckersAssets.SaveTexture(CheckersArt.Circle(256), $"{UIFolder}/Circle.png", GameAssets.SpriteTexture());
            CheckersAssets.SaveTexture(CheckersArt.Star(96), $"{UIFolder}/Star.png", GameAssets.SpriteTexture());
            CheckersAssets.SaveTexture(CheckersArt.Glow(256), $"{UIFolder}/Glow.png", GameAssets.SpriteTexture());
            CheckersAssets.SaveTexture(CheckersArt.Banner(512, 128), $"{UIFolder}/Banner.png", GameAssets.SpriteTexture(new Vector4(60f, 40f, 60f, 40f)));
            CheckersAssets.SaveTexture(CheckersArt.GameIcon(256, CheckersThemeSpec.Classic), $"{IconsFolder}/GameIcon.png", GameAssets.SpriteTexture());
        }

        /// <summary>The picture of a look in the theme chooser.</summary>
        public static Sprite Preview(CheckersThemeSpec spec)
        {
            return CheckersAssets.LoadSprite($"{spec.TexturesFolder}/Preview.png");
        }

        private static void BuildTextures(CheckersThemeSpec spec)
        {
            string textures = spec.TexturesFolder;
            CheckersAssets.SaveTexture(CheckersArt.Surface(SurfacePixels, spec.LightSquare, spec, 1), $"{textures}/LightSquare.png", GameAssets.SurfaceTexture(true, SurfacePixels));
            CheckersAssets.SaveTexture(CheckersArt.Surface(SurfacePixels, spec.DarkSquare, spec, 2), $"{textures}/DarkSquare.png", GameAssets.SurfaceTexture(true, SurfacePixels));
            CheckersAssets.SaveTexture(CheckersArt.Surface(SurfacePixels, spec.Slab, spec, 3), $"{textures}/Slab.png", GameAssets.SurfaceTexture(true, SurfacePixels));
            CheckersAssets.SaveTexture(CheckersArt.HighlightDisc(256), $"{textures}/Highlight.png", GameAssets.DecalTexture(256));
            CheckersAssets.SaveTexture(CheckersArt.ContactShadow(128), $"{textures}/ContactShadow.png", GameAssets.DecalTexture(128));
            CheckersAssets.SaveTexture(CheckersArt.GameIcon(256, spec), $"{textures}/Preview.png", GameAssets.SpriteTexture());
        }

        private static void BuildMaterials(CheckersThemeSpec spec)
        {
            LitMaterial(spec, "LightSquare", Color.white, 0f, spec.SquareSmoothness, Texture("LightSquare", spec));
            LitMaterial(spec, "DarkSquare", Color.white, 0f, spec.SquareSmoothness, Texture("DarkSquare", spec));
            LitMaterial(spec, "Slab", Color.white, spec.SlabMetallic, spec.SlabSmoothness, Texture("Slab", spec));
            LitMaterial(spec, "Table", spec.Table, 0f, spec.TableSmoothness, null);
            LitMaterial(spec, "LightPiece", spec.LightPiece, spec.PieceMetallic, spec.PieceSmoothness, null, spec.Emissive ? spec.LightGlow : default);
            LitMaterial(spec, "DarkPiece", spec.DarkPiece, spec.PieceMetallic, spec.PieceSmoothness, null, spec.Emissive ? spec.DarkGlow : default);
            LitMaterial(spec, "LightCrown", spec.LightCrown, spec.Emissive ? 0.3f : 0.85f, 0.8f, null, spec.Emissive ? spec.LightGlow * 0.6f : default);
            LitMaterial(spec, "DarkCrown", spec.DarkCrown, spec.Emissive ? 0.3f : 0.85f, 0.8f, null, spec.Emissive ? spec.DarkGlow * 0.6f : default);
            TransparentMaterial(spec, "Highlight", Color.white, Texture("Highlight", spec), false);
            TransparentMaterial(spec, "ContactShadow", Color.white, Texture("ContactShadow", spec), false);
        }

        /// <summary>A lit material; with an emission colour above black it glows.</summary>
        private static Material LitMaterial(CheckersThemeSpec spec, string name, Color color, float metallic, float smoothness, Texture2D texture,
            Color emissionColor = default)
        {
            bool glows = emissionColor.maxColorComponent > 0f;
            return CheckersAssets.SaveMaterial($"{spec.MaterialsFolder}/{name}.mat", Lit, m =>
            {
                m.SetColor("_BaseColor", color);
                m.SetTexture("_BaseMap", texture);
                m.SetFloat("_Metallic", metallic);
                m.SetFloat("_Smoothness", smoothness);
                m.SetFloat("_Surface", 0f);
                m.SetTexture("_EmissionMap", null);
                m.SetColor("_EmissionColor", glows ? emissionColor : Color.black);
                if (glows)
                {
                    m.EnableKeyword("_EMISSION");
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                else
                {
                    m.DisableKeyword("_EMISSION");
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                }
                m.enableInstancing = true;
            });
        }

        /// <summary>An unlit transparent material: alpha blended, or additive for glows.</summary>
        private static Material TransparentMaterial(CheckersThemeSpec spec, string name, Color color, Texture2D texture, bool additive)
        {
            return CheckersAssets.SaveMaterial($"{spec.MaterialsFolder}/{name}.mat", Unlit, m =>
            {
                m.SetColor("_BaseColor", color);
                m.SetTexture("_BaseMap", texture);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", additive ? 2f : 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
                if (m.HasProperty("_SrcBlendAlpha"))
                {
                    m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                    m.SetFloat("_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
                }
                m.SetFloat("_ZWrite", 0f);
                m.SetFloat("_Cull", (float)CullMode.Back);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)RenderQueue.Transparent;
                m.enableInstancing = true;
            });
        }

        private static void BuildSounds()
        {
            CheckersAssets.SaveAudio(CheckersSounds.Wav(CheckersSounds.Click()), $"{AudioFolder}/Click.wav", false);
            CheckersAssets.SaveAudio(CheckersSounds.Wav(CheckersSounds.Slide()), $"{AudioFolder}/Move.wav", false);
            CheckersAssets.SaveAudio(CheckersSounds.Wav(CheckersSounds.Capture()), $"{AudioFolder}/Capture.wav", false);
            CheckersAssets.SaveAudio(CheckersSounds.Wav(CheckersSounds.King()), $"{AudioFolder}/King.wav", false);
            CheckersAssets.SaveAudio(CheckersSounds.Wav(CheckersSounds.Win()), $"{AudioFolder}/Win.wav", false);
            CheckersAssets.SaveAudio(CheckersSounds.Wav(CheckersSounds.Lose()), $"{AudioFolder}/Lose.wav", false);
        }

        private static void Progress(string step, float fraction)
        {
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayProgressBar("Checkers art", step, fraction);
            }
        }
    }
}
