using Gamebox.Racing;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// The line drawing of a layout, drawn pixel by pixel (no scene, no art): the verges and the road with their real
    /// widths, the barriers, the centre line, the racing line from green (flat out) to red (slowest), the grid boxes, the
    /// pickup spots, the start line, the set pieces, a dot every 50 m with the distance every 100 m, and a 50 m grid.
    /// North is up.
    /// </summary>
    internal static partial class TrackPreview
    {
        private const float LineScale = 3f;
        private const int LineMargin = 60;

        private static readonly Color32 Background = new Color32(43, 42, 38, 255);
        private static readonly Color32 GridColor = new Color32(58, 56, 51, 255);
        private static readonly Color32 VergeColor = new Color32(122, 106, 80, 255);
        private static readonly Color32 RoadColor = new Color32(85, 82, 76, 255);
        private static readonly Color32 BarrierColor = new Color32(232, 224, 208, 255);
        private static readonly Color32 CentreColor = new Color32(154, 150, 140, 255);
        private static readonly Color32 SlotColor = new Color32(64, 160, 255, 255);
        private static readonly Color32 PickupColor = new Color32(64, 255, 128, 255);
        private static readonly Color32 PieceColor = new Color32(255, 140, 40, 255);
        private static readonly Color32 TextColor = new Color32(240, 236, 224, 255);

        /// <summary>Digits 0-9 in a 3 x 5 pixel font, a row per string, top first.</summary>
        private static readonly string[] Digits =
        {
            "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001",
            "111100111001111", "111100111101111", "111001010010010", "111101111101111", "111101111001111"
        };

        private static byte[] Draw(TrackGeometry geometry)
        {
            RacePath path = geometry.Path;
            var canvas = new Canvas(geometry);
            canvas.Grid(50f);
            float step = 0.5f;
            for (float d = 0f; d < path.Length; d += step)
            {
                canvas.Disc(path.PointAt(d), geometry.BarrierOffset(d), VergeColor);
            }
            for (float d = 0f; d < path.Length; d += step)
            {
                canvas.Disc(path.PointAt(d), geometry.HalfWidthAt(d), RoadColor);
            }
            for (float d = 0f; d < path.Length; d += 0.25f)
            {
                Vector2 centre = path.PointAt(d);
                Vector2 normal = path.NormalAt(d);
                float face = geometry.BarrierOffset(d);
                canvas.Dot(centre + normal * face, 1, BarrierColor);
                canvas.Dot(centre - normal * face, 1, BarrierColor);
                if (Mathf.Repeat(d, 6f) < 3f)
                {
                    canvas.Dot(centre, 0, CentreColor);
                }
                float speed = geometry.SpeedHint(d);
                var line = new Color32((byte)(255 * Mathf.Clamp01(2f * (1f - speed))), (byte)(255 * Mathf.Clamp01(2f * speed)), 40, 255);
                canvas.Dot(geometry.RacingLinePoint(d), 1, line);
            }
            Vector2 lineCentre = path.PointAt(geometry.LineDistance);
            Vector2 lineNormal = path.NormalAt(geometry.LineDistance);
            float lineHalf = geometry.HalfWidthAt(geometry.LineDistance);
            for (float x = -lineHalf; x <= lineHalf; x += 0.2f)
            {
                bool white = Mathf.Repeat(x, 2f) < 1f;
                canvas.Dot(lineCentre + lineNormal * x, 2, white ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 255));
            }
            for (int slot = 0; slot < 8; slot++)
            {
                Pose pose = geometry.GridSlot(slot);
                Vector3 forward = pose.rotation * Vector3.forward;
                Vector3 right = pose.rotation * Vector3.right;
                Vector2 centre = RacePath.ToPlane(pose.position);
                for (float along = -2.2f; along <= 2.2f; along += 0.25f)
                {
                    for (float across = -0.95f; across <= 0.95f; across += 0.25f)
                    {
                        canvas.Dot(centre + new Vector2(forward.x, forward.z) * along + new Vector2(right.x, right.z) * across, 0, SlotColor);
                    }
                }
            }
            foreach (Vector3 spot in geometry.PickupSpots)
            {
                canvas.Disc(RacePath.ToPlane(spot), 1.2f, PickupColor);
            }
            foreach (TrackLayout.SetPiece piece in geometry.Layout.SetPieces)
            {
                float side = piece.Side >= 0f ? 1f : -1f;
                Vector2 spot = path.PointAt(piece.Distance) + path.NormalAt(piece.Distance) * (side * (geometry.BarrierOffset(piece.Distance) + 4f));
                canvas.Disc(spot, 2.5f, PieceColor);
            }
            for (float d = 0f; d < path.Length; d += 50f)
            {
                Vector2 centre = path.PointAt(d);
                canvas.Disc(centre, 1f, TextColor);
                if (Mathf.RoundToInt(d) % 100 == 0)
                {
                    Vector2 label = centre + path.NormalAt(d) * (geometry.BarrierOffset(d) + 9f);
                    canvas.Number(label, Mathf.RoundToInt(d));
                }
            }
            return canvas.ToPng();
        }

        /// <summary>A picture of the ground plane at a fixed scale, fitted round a layout.</summary>
        private sealed class Canvas
        {
            private readonly Color32[] pixels;
            private readonly int width;
            private readonly int height;
            private readonly float minX;
            private readonly float maxZ;

            public Canvas(TrackGeometry geometry)
            {
                RacePath path = geometry.Path;
                float lowX = float.MaxValue, highX = float.MinValue, lowZ = float.MaxValue, highZ = float.MinValue;
                for (int i = 0; i < path.Count; i++)
                {
                    float reach = geometry.BarrierOffset(i * path.Spacing) + 20f;
                    Vector2 point = path.Samples[i];
                    lowX = Mathf.Min(lowX, point.x - reach);
                    highX = Mathf.Max(highX, point.x + reach);
                    lowZ = Mathf.Min(lowZ, point.y - reach);
                    highZ = Mathf.Max(highZ, point.y + reach);
                }
                minX = lowX;
                maxZ = highZ;
                width = Mathf.CeilToInt((highX - lowX) * LineScale) + LineMargin * 2;
                height = Mathf.CeilToInt((highZ - lowZ) * LineScale) + LineMargin * 2;
                pixels = new Color32[width * height];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Background;
                }
            }

            public void Grid(float spacing)
            {
                for (int x = 0; x < width; x++)
                {
                    float world = minX + (x - LineMargin) / LineScale;
                    if (Mathf.Repeat(world, spacing) < 1f / LineScale)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            Set(x, y, GridColor);
                        }
                    }
                }
                for (int y = 0; y < height; y++)
                {
                    float world = maxZ - (y - LineMargin) / LineScale;
                    if (Mathf.Repeat(world, spacing) < 1f / LineScale)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            Set(x, y, GridColor);
                        }
                    }
                }
            }

            public void Disc(Vector2 centre, float radius, Color32 color)
            {
                Vector2 pixel = ToPixel(centre);
                float r = radius * LineScale;
                int x0 = Mathf.FloorToInt(pixel.x - r), x1 = Mathf.CeilToInt(pixel.x + r);
                int y0 = Mathf.FloorToInt(pixel.y - r), y1 = Mathf.CeilToInt(pixel.y + r);
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        if ((x - pixel.x) * (x - pixel.x) + (y - pixel.y) * (y - pixel.y) <= r * r)
                        {
                            Set(x, y, color);
                        }
                    }
                }
            }

            public void Dot(Vector2 point, int radius, Color32 color)
            {
                Vector2 pixel = ToPixel(point);
                int cx = Mathf.RoundToInt(pixel.x);
                int cy = Mathf.RoundToInt(pixel.y);
                for (int y = cy - radius; y <= cy + radius; y++)
                {
                    for (int x = cx - radius; x <= cx + radius; x++)
                    {
                        Set(x, y, color);
                    }
                }
            }

            /// <summary>A number in the little digit font, two pixels per dot, centred on a point.</summary>
            public void Number(Vector2 point, int value)
            {
                string text = value.ToString();
                Vector2 pixel = ToPixel(point);
                int left = Mathf.RoundToInt(pixel.x) - text.Length * 4;
                int top = Mathf.RoundToInt(pixel.y) - 5;
                for (int c = 0; c < text.Length; c++)
                {
                    string glyph = Digits[text[c] - '0'];
                    for (int row = 0; row < 5; row++)
                    {
                        for (int column = 0; column < 3; column++)
                        {
                            if (glyph[row * 3 + column] != '1')
                            {
                                continue;
                            }
                            int x = left + c * 8 + column * 2;
                            int y = top + row * 2;
                            Set(x, y, TextColor);
                            Set(x + 1, y, TextColor);
                            Set(x, y + 1, TextColor);
                            Set(x + 1, y + 1, TextColor);
                        }
                    }
                }
            }

            public byte[] ToPng()
            {
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                // Rows of the picture run top down; a texture's run bottom up.
                var flipped = new Color32[pixels.Length];
                for (int y = 0; y < height; y++)
                {
                    System.Array.Copy(pixels, y * width, flipped, (height - 1 - y) * width, width);
                }
                texture.SetPixels32(flipped);
                texture.Apply();
                byte[] png = texture.EncodeToPNG();
                Object.DestroyImmediate(texture);
                return png;
            }

            private Vector2 ToPixel(Vector2 world)
            {
                return new Vector2((world.x - minX) * LineScale + LineMargin, (maxZ - world.y) * LineScale + LineMargin);
            }

            private void Set(int x, int y, Color32 color)
            {
                if (x >= 0 && y >= 0 && x < width && y < height)
                {
                    pixels[y * width + x] = color;
                }
            }
        }
    }
}
