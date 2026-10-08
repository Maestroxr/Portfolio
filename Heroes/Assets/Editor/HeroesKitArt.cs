using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// The materials of the textured kits in Art/Models/Quaternius/Kits (the outfits and bodies of one rig, the animation
    /// library, the props, the village and the woods). Each kit has a Materials.json written when it was downloaded: by the
    /// name of a material its models use, the base colour, normal and "MOS" maps (metallic in red, occlusion in green,
    /// smoothness in alpha, which URP Lit reads as both its metallic and its occlusion map), the alpha mode and a colour.
    /// The URP Lit materials are made from it once per theme build, in the theme's own folder, and the kit models are dressed
    /// in them by the names of the materials embedded in the files. A material can be had tinted, with one of its alternate
    /// base colours, or with its base colour repainted (the cloth of an outfit in a faction's colours).
    /// </summary>
    internal static class HeroesKitArt
    {
        /// <summary>The kits, under the game's folder.</summary>
        public const string Folder = "Art/Models/Quaternius/Kits";

        /// <summary>The kits' models, relative to Art/Models (as <see cref="HeroesArtBuilder.Model"/> takes them).</summary>
        public const string Models = "Quaternius/Kits";

        [Serializable]
        private sealed class Manifest
        {
            public Entry[] materials = new Entry[0];
        }

        [Serializable]
        private sealed class Entry
        {
            public string name;
            public string @base;
            public string normal;
            public string mos;
            public string alpha;
            public string[] alternates;
            public float[] color;
            public float roughness;
            public bool doubleSided;
        }

        /// <summary>The kits whose models are painted per vertex as well, drawn with Heroes/Kit Lit (URP Lit with vertex colours).</summary>
        private static readonly HashSet<string> VertexColored = new HashSet<string> { "Props" };

        private static readonly Dictionary<string, Dictionary<string, Entry>> Manifests = new Dictionary<string, Dictionary<string, Entry>>();
        private static readonly Dictionary<string, Material> Made = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Texture2D> Painted = new Dictionary<string, Texture2D>();

        private static HeroesThemeSpec Spec => HeroesThemeSpec.Current;

        /// <summary>Forgets what the theme built before made (it belongs to that theme's folder).</summary>
        public static void ForgetCaches()
        {
            Manifests.Clear();
            Made.Clear();
            Painted.Clear();
        }

        /// <summary>Whether a model (relative to Art/Models) is one of the kits'.</summary>
        public static bool IsKit(string relative)
        {
            return relative != null && relative.StartsWith(Models + "/", StringComparison.Ordinal);
        }

        /// <summary>The kit a model belongs to: the folder under Kits.</summary>
        public static string KitOf(string relative)
        {
            string rest = relative.Substring(Models.Length + 1);
            int slash = rest.IndexOf('/');
            return slash > 0 ? rest.Substring(0, slash) : rest;
        }

        // ------------------------------------------------------------------ dressing models

        /// <summary>
        /// Dresses an instance of a kit model in the kit's materials, by the names of the materials the file embeds, under
        /// <paramref name="tint"/> (clear or white for none). Materials the kit does not know are left as they are.
        /// </summary>
        public static void Dress(GameObject instance, string relative, Color tint)
        {
            string kit = KitOf(relative);
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                    {
                        continue;
                    }
                    Material made = Material(kit, materials[i].name, -1, tint);
                    if (made != null && made != materials[i])
                    {
                        materials[i] = made;
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
        /// A kit model standing as it was made, <paramref name="height"/> meters tall (0 for its own size), its foot on the
        /// ground at <paramref name="offset"/> under a parent at the origin. <see cref="HeroesArtBuilder.Piece"/> turns a
        /// model's root to the yaw alone, which lays the props and village pieces on their side (their files stand them up
        /// on their root); this keeps that turn.
        /// </summary>
        public static GameObject Stand(Transform parent, string relative, Vector3 offset, float yaw, float height, Color tint = default)
        {
            GameObject piece = HeroesArtBuilder.Piece(parent, relative, Vector3.zero, yaw, 1f, 0f, tint);
            if (piece == null)
            {
                return null;
            }
            piece.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * HeroesArtBuilder.Model(relative).transform.localRotation;
            piece.transform.localPosition = Vector3.zero;
            Bounds bounds = HeroesArtBuilder.BoundsOf(piece);
            if (height > 0f && bounds.size.y > 0.0001f)
            {
                piece.transform.localScale *= height / bounds.size.y;
                bounds = HeroesArtBuilder.BoundsOf(piece);
            }
            piece.transform.localPosition = offset - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            return piece;
        }

        /// <summary>Gives every slot of the renderers under <paramref name="root"/> that wears <paramref name="name"/> another material.</summary>
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
                    if (materials[i] != null && BaseName(materials[i].name) == name)
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

        /// <summary>The name of a kit material without what Unity or the builders put after it.</summary>
        public static string BaseName(string name)
        {
            int instance = name.IndexOf(" (", StringComparison.Ordinal);
            return instance > 0 ? name.Substring(0, instance) : name;
        }

        // ------------------------------------------------------------------ materials

        /// <summary>
        /// The kit's material called <paramref name="name"/> in URP Lit: with the alternate base colour of that index (or its
        /// own for -1), under <paramref name="tint"/>. Null when the kit has no such material.
        /// </summary>
        public static Material Material(string kit, string name, int alternate = -1, Color tint = default)
        {
            return Make(kit, name, alternate, tint, null, null, false);
        }

        /// <summary>
        /// The kit's material without the colours of the vertices, under <paramref name="tint"/>: the white cloth of a banner
        /// dyed in a player's colour, which the vertices of the file would have kept red or blue.
        /// </summary>
        public static Material Plain(string kit, string name, Color tint = default)
        {
            return Make(kit, name, -1, tint, null, null, true);
        }

        /// <summary>
        /// The kit's material with its base colour repainted by <paramref name="paint"/> (every pixel through it), saved as
        /// <paramref name="tag"/>: the cloth of an outfit in other colours, a roof in slate.
        /// </summary>
        public static Material Repainted(string kit, string name, int alternate, string tag, Func<Color, Color> paint, Color tint = default)
        {
            return Make(kit, name, alternate, tint, tag, paint, false);
        }

        private static Material Make(string kit, string name, int alternate, Color tint, string tag, Func<Color, Color> paint, bool plain)
        {
            Entry entry = Find(kit, BaseName(name));
            if (entry == null)
            {
                return null;
            }
            bool tinted = tint.a > 0f && tint != Color.white;
            string baseFile = alternate >= 0 && entry.alternates != null && alternate < entry.alternates.Length ? entry.alternates[alternate] : entry.@base;
            string key = entry.name + (alternate >= 0 ? $"_{alternate + 2}" : "") + (tag != null ? $"_{tag}" : "") + (plain ? "_Plain" : "")
                         + (tinted ? $"_{ColorUtility.ToHtmlStringRGB(tint)}" : "");
            string id = $"{kit}/{key}";
            if (Made.TryGetValue(id, out Material cached) && cached != null)
            {
                return cached;
            }
            bool clipped = entry.alpha == "MASK";
            bool blended = entry.alpha == "BLEND";
            Texture2D baseMap = string.IsNullOrEmpty(baseFile) ? null : ColorMap(kit, baseFile, clipped);
            if (baseMap != null && paint != null)
            {
                baseMap = Repaint(kit, baseMap, baseFile, tag, paint, clipped);
            }
            Texture2D normal = string.IsNullOrEmpty(entry.normal) ? null : NormalMap(kit, entry.normal);
            Texture2D mos = string.IsNullOrEmpty(entry.mos) ? null : MaskMap(kit, entry.mos);
            Color color = entry.color != null && entry.color.Length >= 3
                ? new Color(entry.color[0], entry.color[1], entry.color[2], entry.color.Length > 3 ? entry.color[3] : 1f)
                : Color.white;
            if (tinted)
            {
                color = new Color(color.r * tint.r, color.g * tint.g, color.b * tint.b, color.a);
            }
            // The props tint their trim sheets per vertex (gold coins, red apples, dark books), which plain URP Lit ignores.
            Shader shader = VertexColored.Contains(kit) && !plain ? Shader.Find("Heroes/Kit Lit") : null;
            Material material = HeroesAssets.Material($"{Spec.Generated}/Kits/{kit}/{key}.mat", shader != null ? shader : Shader.Find("Universal Render Pipeline/Lit"), m =>
            {
                m.SetTexture("_BaseMap", baseMap);
                m.SetColor("_BaseColor", color);
                m.SetTexture("_BumpMap", normal);
                m.SetFloat("_BumpScale", 1f);
                if (normal != null)
                {
                    m.EnableKeyword("_NORMALMAP");
                }
                else
                {
                    m.DisableKeyword("_NORMALMAP");
                }
                m.SetFloat("_WorkflowMode", 1f);
                if (mos != null)
                {
                    m.SetTexture("_MetallicGlossMap", mos);
                    m.SetTexture("_OcclusionMap", mos);
                    m.SetFloat("_OcclusionStrength", 1f);
                    m.SetFloat("_Smoothness", 1f);
                    m.SetFloat("_SmoothnessTextureChannel", 0f);
                    m.EnableKeyword("_METALLICSPECGLOSSMAP");
                    m.EnableKeyword("_OCCLUSIONMAP");
                }
                else
                {
                    m.SetTexture("_MetallicGlossMap", null);
                    m.SetTexture("_OcclusionMap", null);
                    m.SetFloat("_Metallic", 0f);
                    m.SetFloat("_Smoothness", entry.roughness > 0f ? 1f - entry.roughness : 0.25f);
                    m.DisableKeyword("_METALLICSPECGLOSSMAP");
                    m.DisableKeyword("_OCCLUSIONMAP");
                }
                m.SetFloat("_Cull", entry.doubleSided ? 0f : 2f);
                m.doubleSidedGI = entry.doubleSided;
                // The woods are drawn by the terrain, many at once.
                m.enableInstancing = true;
                if (blended)
                {
                    m.SetFloat("_Surface", 1f);
                    m.SetFloat("_Blend", 0f);
                    m.SetFloat("_AlphaClip", 0f);
                    m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    m.SetFloat("_ZWrite", 0f);
                    m.SetOverrideTag("RenderType", "Transparent");
                    m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    m.DisableKeyword("_ALPHATEST_ON");
                }
                else
                {
                    m.SetFloat("_Surface", 0f);
                    m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                    m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                    m.SetFloat("_ZWrite", 1f);
                    m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    m.SetFloat("_AlphaClip", clipped ? 1f : 0f);
                    m.SetFloat("_Cutoff", 0.5f);
                    if (clipped)
                    {
                        m.EnableKeyword("_ALPHATEST_ON");
                        m.SetOverrideTag("RenderType", "TransparentCutout");
                        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    }
                    else
                    {
                        m.DisableKeyword("_ALPHATEST_ON");
                        m.SetOverrideTag("RenderType", "Opaque");
                        m.renderQueue = -1;
                    }
                }
            });
            material.name = key;
            Made[id] = material;
            return material;
        }

        /// <summary>
        /// A copy of <paramref name="material"/> that glows: its base colour lit by <paramref name="emission"/> (the eyes of
        /// the dead, the crystal of a staff), saved among the theme's glowing materials.
        /// </summary>
        public static Material Glowing(Material material, Color emission, string tag)
        {
            if (material == null)
            {
                return null;
            }
            string id = $"glow/{material.name}_{tag}";
            if (Made.TryGetValue(id, out Material cached) && cached != null)
            {
                return cached;
            }
            string relative = $"{Spec.Generated}/Kits/Glow/{material.name}_{tag}.mat";
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            bool sameShader = material.shader == lit;
            // A material of the packs may be one the importer made with a shader of its own: the glow is plain URP Lit.
            Texture map = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : null;
            Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            Material glow = HeroesAssets.Material(relative, lit, m =>
            {
                if (sameShader)
                {
                    m.CopyPropertiesFromMaterial(material);
                    m.shaderKeywords = material.shaderKeywords;
                }
                else
                {
                    m.SetTexture("_BaseMap", map);
                    m.SetColor("_BaseColor", new Color(color.r, color.g, color.b, 1f));
                    m.SetFloat("_Smoothness", 0.7f);
                }
                m.SetColor("_EmissionColor", emission);
                m.SetTexture("_EmissionMap", map);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            });
            Made[id] = glow;
            return glow;
        }

        // ------------------------------------------------------------------ the manifests and the textures

        private static Entry Find(string kit, string name)
        {
            if (!Manifests.TryGetValue(kit, out Dictionary<string, Entry> entries))
            {
                entries = new Dictionary<string, Entry>();
                string file = HeroesAssets.FullPath(HeroesAssets.Path($"{Folder}/{kit}/Materials.json"));
                if (File.Exists(file))
                {
                    var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(file));
                    foreach (Entry entry in manifest.materials)
                    {
                        entries[entry.name] = entry;
                    }
                }
                else
                {
                    Debug.LogWarning($"Heroes: the kit {kit} has no Materials.json.");
                }
                Manifests[kit] = entries;
            }
            return entries.TryGetValue(name, out Entry found) ? found : null;
        }

        private static string TexturePath(string kit, string file)
        {
            return $"{Folder}/{kit}/Textures/{file}";
        }

        private static Texture2D ColorMap(string kit, string file, bool cutout)
        {
            return HeroesAssets.ConfigureTexture(TexturePath(kit, file), importer =>
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 2048;
                importer.alphaIsTransparency = cutout;
                importer.alphaSource = cutout ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
                // Leaves and vines keep their cover in the distance instead of thinning out to nothing.
                importer.mipMapsPreserveCoverage = cutout;
                importer.alphaTestReferenceValue = 0.5f;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.wrapMode = TextureWrapMode.Repeat;
            });
        }

        private static Texture2D NormalMap(string kit, string file)
        {
            return HeroesAssets.ConfigureTexture(TexturePath(kit, file), importer =>
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.wrapMode = TextureWrapMode.Repeat;
            });
        }

        private static Texture2D MaskMap(string kit, string file)
        {
            return HeroesAssets.ConfigureTexture(TexturePath(kit, file), importer =>
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = false;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 2048;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = false;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.wrapMode = TextureWrapMode.Repeat;
            });
        }

        /// <summary>A copy of a kit's base colour with every pixel repainted, at most 1024 across, in the theme's folder.</summary>
        private static Texture2D Repaint(string kit, Texture2D source, string file, string tag, Func<Color, Color> paint, bool cutout)
        {
            string id = $"{kit}/{file}/{tag}";
            if (Painted.TryGetValue(id, out Texture2D cached) && cached != null)
            {
                return cached;
            }
            var pixels = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            pixels.LoadImage(File.ReadAllBytes(HeroesAssets.FullPath(AssetDatabase.GetAssetPath(source))));
            Texture2D sized = Shrink(pixels, 1024);
            Color[] colors = sized.GetPixels();
            for (int i = 0; i < colors.Length; i++)
            {
                float alpha = colors[i].a;
                colors[i] = paint(colors[i]);
                colors[i].a = alpha;
            }
            sized.SetPixels(colors);
            sized.Apply();
            string name = $"{Path.GetFileNameWithoutExtension(file)}_{tag}";
            string relative = $"{Spec.Generated}/Kits/{kit}/Textures/{name}{(cutout ? ".png" : ".jpg")}";
            HeroesAssets.EnsureFolderOf(relative);
            string path = HeroesAssets.Path(relative);
            bool written = HeroesAssets.WriteIfChanged(path, cutout ? sized.EncodeToPNG() : sized.EncodeToJPG(92));
            Object.DestroyImmediate(sized);
            Texture2D saved = HeroesAssets.ConfigureTexture(relative, importer =>
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 1024;
                importer.alphaIsTransparency = cutout;
                importer.alphaSource = cutout ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.wrapMode = TextureWrapMode.Repeat;
            }, written);
            Painted[id] = saved;
            return saved;
        }

        /// <summary>The picture scaled down (by halving) until it is at most <paramref name="max"/> across; the source is destroyed when replaced.</summary>
        private static Texture2D Shrink(Texture2D texture, int max)
        {
            while (texture.width > max || texture.height > max)
            {
                int width = Mathf.Max(1, texture.width / 2);
                int height = Mathf.Max(1, texture.height / 2);
                var half = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Color32[] source = texture.GetPixels32();
                var target = new Color32[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int r = 0, g = 0, b = 0, a = 0;
                        for (int dy = 0; dy < 2; dy++)
                        {
                            for (int dx = 0; dx < 2; dx++)
                            {
                                Color32 c = source[Mathf.Min(texture.height - 1, y * 2 + dy) * texture.width + Mathf.Min(texture.width - 1, x * 2 + dx)];
                                r += c.r;
                                g += c.g;
                                b += c.b;
                                a += c.a;
                            }
                        }
                        target[y * width + x] = new Color32((byte)(r / 4), (byte)(g / 4), (byte)(b / 4), (byte)(a / 4));
                    }
                }
                half.SetPixels32(target);
                half.Apply();
                Object.DestroyImmediate(texture);
                texture = half;
            }
            return texture;
        }

        // ------------------------------------------------------------------ painting

        /// <summary>
        /// A repaint of the pixels whose hue lies within <paramref name="from"/> .. <paramref name="to"/> (0..1, wrapping
        /// through red when from &gt; to) and that are saturated at least <paramref name="saturated"/>: they take the hue and
        /// saturation of <paramref name="color"/>, their brightness scaled toward its brightness. Everything else stays.
        /// </summary>
        public static Func<Color, Color> Hue(float from, float to, float saturated, Color color, float brightness = 1f)
        {
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            return pixel =>
            {
                Color.RGBToHSV(pixel, out float h, out float s, out float v);
                bool inside = from <= to ? h >= from && h <= to : h >= from || h <= to;
                if (!inside || s < saturated)
                {
                    return pixel;
                }
                float lean = Mathf.Clamp01((s - saturated) / 0.12f);
                Color painted = Color.HSVToRGB(hue, Mathf.Lerp(s, saturation, 0.85f), Mathf.Clamp01(v * Mathf.Lerp(1f, value * 1.6f, 0.6f) * brightness));
                return Color.Lerp(pixel, painted, lean);
            };
        }

        /// <summary>Every pixel graded: saturation and brightness multiplied, and a cast mixed in by its brightness.</summary>
        public static Func<Color, Color> Grade(float saturation, float value, Color cast, float mix)
        {
            return pixel =>
            {
                Color.RGBToHSV(pixel, out float h, out float s, out float v);
                Color graded = Color.HSVToRGB(h, Mathf.Clamp01(s * saturation), Mathf.Clamp01(v * value));
                float luma = graded.r * 0.3f + graded.g * 0.59f + graded.b * 0.11f;
                return Color.Lerp(graded, cast * luma * 1.6f, mix);
            };
        }
    }
}
