using Gamebox.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Portfolio.Monopoly.EditorTools
{
    /// <summary>
    /// Draws the generated textures of a look (<see cref="MonopolyThemeSpec"/>): the board (tracks, colour bars, jail
    /// cell, card spots and the globe in the middle; names, prices and icons are TextMesh Pro on top of it), the table,
    /// the dice faces, the card backs, the interface sprites and the token badges. The classic style draws the mint board
    /// and the wooden table; the galactic style a dark station floor with neon rims and lanes, a starfield table and
    /// holographic cards, with a second texture of what glows for the emission of the materials. Everything is
    /// deterministic, so a rebuild writes the same bytes and changes nothing.
    /// </summary>
    internal static class MonopolyArt
    {
        // ------------------------------------------------------------------ layers

        /// <summary>A drawing and, for the emissive looks, the layer of what glows on it (null when nothing does).</summary>
        private sealed class Layers
        {
            public readonly Raster Main;
            public readonly Raster Glow;

            public Layers(int width, int height, Color background, bool glows)
            {
                Main = new Raster(width, height, background);
                Glow = glows ? new Raster(width, height, Color.black) : null;
            }

            public void Fill(Sdf shape, Color main, Color glow, Rect bounds)
            {
                Main.Fill(shape, main, bounds);
                Glow?.Fill(shape, glow, bounds);
            }

            public void FillRect(Rect rect, Color main, Color glow)
            {
                Main.FillRect(rect, main);
                Glow?.FillRect(rect, glow);
            }

            public void Outline(Rect rect, float width, Color main, Color glow)
            {
                MonopolyArt.Outline(Main, rect, width, main);
                if (Glow != null)
                {
                    MonopolyArt.Outline(Glow, rect, width, glow);
                }
            }
        }

        // ------------------------------------------------------------------ board

        /// <summary>
        /// The board surface: <paramref name="size"/> pixels for the whole square of <paramref name="geometry"/>, in the
        /// style of <paramref name="spec"/>. <paramref name="glow"/> gets the emission texture of an emissive look, null otherwise.
        /// </summary>
        public static Texture2D Board(BoardGeometry geometry, IList<SpaceData> spaces, int size, MonopolyThemeSpec spec, out Texture2D glow)
        {
            var layers = new Layers(size, size, spec.BoardField, spec.Emissive);
            float ppu = size / geometry.Side;
            Func<Vector2, Vector2> px = p => (p + new Vector2(geometry.Half, geometry.Half)) * ppu;
            Func<Rect, Rect> pxRect = r => Rect.MinMaxRect(px(r.min).x, px(r.min).y, px(r.max).x, px(r.max).y);
            float line = Mathf.Max(2f, ppu * 0.016f);
            float inner = geometry.Half - geometry.Corner;
            if (spec.Style == ArtStyle.Galactic)
            {
                StationBoard(layers, geometry, spaces, size, spec, px, pxRect, line, inner);
            }
            else
            {
                ClassicBoard(layers.Main, geometry, spaces, size, spec, px, pxRect, line, inner);
            }
            glow = layers.Glow?.ToTexture(false);
            return layers.Main.ToTexture(false);
        }

        private static void ClassicBoard(Raster raster, BoardGeometry geometry, IList<SpaceData> spaces, int size, MonopolyThemeSpec spec,
            Func<Vector2, Vector2> px, Func<Rect, Rect> pxRect, float line, float inner)
        {
            Color lineColor = spec.BoardLine;
            // The globe in the middle: meridians and parallels in a darker mint.
            Vector2 middle = new Vector2(size * 0.5f, size * 0.5f);
            float globe = ppuOf(size, geometry) * 3.3f;
            Color faint = new Color(0.2f, 0.42f, 0.3f, 0.16f);
            raster.Fill(p => Sd.Ring(p, middle, globe, line * 1.6f), faint, Sd.Around(middle, globe + line * 2f));
            for (int i = 1; i <= 3; i++)
            {
                float rx = globe * (i / 4f);
                raster.Fill(p => Sd.Outline(Sd.Ellipse(p, middle, new Vector2(rx, globe)), line), faint, Sd.Around(middle, globe + line));
            }
            for (int i = -2; i <= 2; i++)
            {
                float y = globe * i / 3.2f;
                float half = Mathf.Sqrt(Mathf.Max(0f, globe * globe - y * y));
                Vector2 a = middle + new Vector2(-half, y);
                Vector2 b = middle + new Vector2(half, y);
                raster.Fill(p => Sd.Segment(p, a, b, line * 0.5f), faint, Rect.MinMaxRect(a.x - line, a.y - line, b.x + line, b.y + line));
            }

            // The card spots: Community Chest up left, Chance down right, turned like on the classic board.
            float ppu = ppuOf(size, geometry);
            CardSpot(raster, px(new Vector2(-2.35f, 2.35f)), ppu * 0.9f, ppu * 1.3f, 45f, spec.Palette.chestBlue, line);
            CardSpot(raster, px(new Vector2(2.35f, -2.35f)), ppu * 0.9f, ppu * 1.3f, 45f, spec.Palette.chanceOrange, line);

            // Spaces: colour bars, the jail cell and the outlines.
            for (int space = 0; space < spaces.Count; space++)
            {
                SpaceData data = spaces[space];
                if (data.kind == SpaceKind.Street)
                {
                    Rect bar = pxRect(geometry.Part(space, 1f - 0.24f, 1f));
                    raster.FillRect(bar, spec.GroupColor(data.group));
                    // A soft sheen on the bar.
                    raster.FillRect(new Rect(bar.x, bar.y, bar.width, bar.height), new Color(1f, 1f, 1f, 0.06f));
                    Rect under = pxRect(geometry.Part(space, 1f - 0.24f, 1f - 0.24f));
                    raster.FillRect(Grow(under, line * 0.5f), lineColor);
                }
                if (data.kind == SpaceKind.Jail)
                {
                    Rect cell = pxRect(geometry.Part(space, 0.3f, 1f, 0f, 0.7f));
                    raster.FillRect(cell, MonopolyStyle.Hex(0xF7941D));
                    for (int bar = 1; bar <= 5; bar++)
                    {
                        float x = Mathf.Lerp(cell.xMin, cell.xMax, bar / 6f);
                        raster.FillRect(new Rect(x - line * 0.9f, cell.yMin + cell.height * 0.1f, line * 1.8f, cell.height * 0.8f), new Color(0.12f, 0.12f, 0.12f, 0.85f));
                    }
                    Outline(raster, cell, line, lineColor);
                }
                if (data.kind == SpaceKind.Go)
                {
                    // A red flash behind the GO arrow.
                    Rect go = pxRect(geometry.Bounds(space));
                    Vector2 c = go.center;
                    raster.Fill(p => Sd.Circle(p, c, go.width * 0.36f), MonopolyStyle.WithAlpha(spec.Print.go, 0.12f), Sd.Around(c, go.width * 0.4f));
                }
                Outline(raster, pxRect(geometry.Bounds(space)), line, lineColor);
            }

            // The border between the track and the middle, and the board's edge.
            Outline(raster, pxRect(Rect.MinMaxRect(-inner, -inner, inner, inner)), line * 1.6f, lineColor);
            Outline(raster, new Rect(0, 0, size, size), line * 2.2f, lineColor);
        }

        private static float ppuOf(int size, BoardGeometry geometry)
        {
            return size / geometry.Side;
        }

        /// <summary>
        /// The station floor: every space a dark panel with a cyan rim and a violet inner line, neon colour bars, lanes
        /// along the edges of the track with a node at every space, circuit traces and a holographic emblem over a
        /// starfield in the middle, and the card spots as holographic panels.
        /// </summary>
        private static void StationBoard(Layers layers, BoardGeometry geometry, IList<SpaceData> spaces, int size, MonopolyThemeSpec spec,
            Func<Vector2, Vector2> px, Func<Rect, Rect> pxRect, float line, float inner)
        {
            float ppu = ppuOf(size, geometry);
            Color cyan = spec.BoardLine;
            Color violet = MonopolyStyle.Hex(0x8A6BFF);
            Color panel = MonopolyStyle.Hex(0x0E1626);
            Vector2 middle = new Vector2(size * 0.5f, size * 0.5f);
            Rect centre = pxRect(Rect.MinMaxRect(-inner, -inner, inner, inner));

            // The starfield of the middle.
            Starfield(layers.Main, centre, 900, 31, false);
            Starfield(layers.Glow, centre, 900, 31, true);

            // Circuit traces from the corners of the middle towards the emblem.
            float trace = line * 0.7f;
            foreach (float sx in new[] { -1f, 1f })
            {
                foreach (float sy in new[] { -1f, 1f })
                {
                    var start = px(new Vector2(sx * (inner - 0.5f), sy * (inner - 0.5f)));
                    var bend = px(new Vector2(sx * (inner - 0.5f), sy * 2.9f));
                    var end = px(new Vector2(sx * 3.4f, sy * 2.9f));
                    var bend2 = px(new Vector2(sx * (inner - 1.4f), sy * (inner - 0.5f)));
                    var end2 = px(new Vector2(sx * (inner - 1.4f), sy * 4.1f));
                    Trace(layers, start, bend, end, trace, violet, cyan);
                    Trace(layers, start, bend2, end2, trace, violet, cyan);
                }
            }

            // The holographic emblem: rings, a hexagon, a crosshair and ticks.
            float globe = ppu * 3.3f;
            Rect around = Sd.Around(middle, globe + line * 3f);
            foreach (float r in new[] { 1f, 0.82f, 0.56f })
            {
                float radius = globe * r;
                layers.Fill(p => Sd.Ring(p, middle, radius, line * (r == 1f ? 1.4f : 0.8f)), MonopolyStyle.WithAlpha(cyan, 0.32f), MonopolyStyle.WithAlpha(cyan, 0.42f), around);
            }
            var hexagon = new List<Vector2>();
            for (int i = 0; i < 6; i++)
            {
                float angle = (60f * i + 30f) * Mathf.Deg2Rad;
                hexagon.Add(middle + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * globe * 0.68f);
            }
            layers.Fill(p => Sd.Outline(Sd.Polygon(p, hexagon), line * 0.9f), MonopolyStyle.WithAlpha(violet, 0.38f), MonopolyStyle.WithAlpha(violet, 0.45f), around);
            for (int i = 0; i < 4; i++)
            {
                float angle = 90f * i * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 a = middle + direction * globe * 0.86f;
                Vector2 b = middle + direction * globe * 1.08f;
                layers.Fill(p => Sd.Segment(p, a, b, line * 0.7f), MonopolyStyle.WithAlpha(cyan, 0.5f), MonopolyStyle.WithAlpha(cyan, 0.6f), around);
            }
            for (int i = 0; i < 36; i++)
            {
                float angle = 10f * i * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 a = middle + direction * globe * 0.97f;
                Vector2 b = middle + direction * globe * (i % 3 == 0 ? 1.04f : 1.01f);
                layers.Fill(p => Sd.Segment(p, a, b, line * 0.4f), MonopolyStyle.WithAlpha(cyan, 0.35f), MonopolyStyle.WithAlpha(cyan, 0.4f), around);
            }

            // The card spots: holographic panels in the deck colours.
            HoloSpot(layers, px(new Vector2(-2.35f, 2.35f)), ppu * 0.9f, ppu * 1.3f, 45f, spec.Palette.chestBlue, line, panel);
            HoloSpot(layers, px(new Vector2(2.35f, -2.35f)), ppu * 0.9f, ppu * 1.3f, 45f, spec.Palette.chanceOrange, line, panel);

            // Spaces: dark panels, rims, colour bars, the jail cell, the GO flash.
            for (int space = 0; space < spaces.Count; space++)
            {
                SpaceData data = spaces[space];
                Rect bounds = pxRect(geometry.Bounds(space));
                layers.Main.FillRect(Grow(bounds, -line * 0.5f), panel);
                Rect innerRect = Grow(bounds, -line * 3.2f);
                layers.Outline(innerRect, line * 0.55f, MonopolyStyle.WithAlpha(violet, 0.32f), MonopolyStyle.WithAlpha(violet, 0.28f));
                if (data.kind == SpaceKind.Street)
                {
                    Color group = spec.GroupColor(data.group);
                    Rect bar = pxRect(geometry.Part(space, 1f - 0.24f, 1f));
                    bar = Grow(bar, -line * 0.5f);
                    layers.FillRect(bar, group, MonopolyStyle.Shade(group, 0.85f));
                    layers.Main.FillRect(new Rect(bar.x, bar.y + bar.height * 0.55f, bar.width, bar.height * 0.45f), new Color(1f, 1f, 1f, 0.08f));
                    Rect under = pxRect(geometry.Part(space, 1f - 0.24f, 1f - 0.24f));
                    layers.FillRect(Grow(under, line * 0.45f), MonopolyStyle.WithAlpha(cyan, 0.7f), MonopolyStyle.WithAlpha(cyan, 0.6f));
                }
                if (data.kind == SpaceKind.Jail)
                {
                    Rect cell = pxRect(geometry.Part(space, 0.3f, 1f, 0f, 0.7f));
                    layers.Main.FillRect(cell, MonopolyStyle.Hex(0x1A1030));
                    Color bars = MonopolyStyle.Hex(0xFF4FD8);
                    for (int bar = 1; bar <= 5; bar++)
                    {
                        float x = Mathf.Lerp(cell.xMin, cell.xMax, bar / 6f);
                        var strip = new Rect(x - line * 0.7f, cell.yMin + cell.height * 0.1f, line * 1.4f, cell.height * 0.8f);
                        layers.FillRect(strip, MonopolyStyle.WithAlpha(bars, 0.9f), MonopolyStyle.WithAlpha(bars, 0.8f));
                    }
                    layers.Outline(cell, line, MonopolyStyle.WithAlpha(bars, 0.9f), MonopolyStyle.WithAlpha(bars, 0.7f));
                }
                if (data.kind == SpaceKind.Go)
                {
                    Vector2 c = bounds.center;
                    float radius = bounds.width * 0.36f;
                    layers.Fill(p => Sd.Ring(p, c, radius, line * 2f), MonopolyStyle.WithAlpha(spec.Print.go, 0.35f), MonopolyStyle.WithAlpha(spec.Print.go, 0.45f), Sd.Around(c, radius + line * 3f));
                }
                layers.Outline(bounds, line * 0.9f, MonopolyStyle.WithAlpha(cyan, 0.85f), MonopolyStyle.WithAlpha(cyan, 0.85f));
            }

            // The lanes: the inner and outer edges of the track, with a node where spaces meet.
            layers.Outline(centre, line * 1.6f, cyan, cyan);
            layers.Outline(new Rect(0, 0, size, size), line * 2.4f, cyan, cyan);
            for (int space = 0; space < spaces.Count; space++)
            {
                Rect bounds = pxRect(geometry.Bounds(space));
                foreach (Vector2 corner in new[] { new Vector2(bounds.xMin, bounds.yMin), new Vector2(bounds.xMax, bounds.yMin), new Vector2(bounds.xMin, bounds.yMax), new Vector2(bounds.xMax, bounds.yMax) })
                {
                    bool onInner = Mathf.Abs(corner.x - centre.xMin) < 1f || Mathf.Abs(corner.x - centre.xMax) < 1f || Mathf.Abs(corner.y - centre.yMin) < 1f || Mathf.Abs(corner.y - centre.yMax) < 1f;
                    bool onInnerSquare = onInner && corner.x >= centre.xMin - 1f && corner.x <= centre.xMax + 1f && corner.y >= centre.yMin - 1f && corner.y <= centre.yMax + 1f;
                    if (onInnerSquare)
                    {
                        Vector2 node = corner;
                        layers.Fill(p => Sd.Circle(p, node, line * 1.9f), cyan, cyan, Sd.Around(node, line * 3f));
                        layers.Main.Fill(p => Sd.Circle(p, node, line * 0.8f), panel, Sd.Around(node, line * 2f));
                    }
                }
            }
        }

        /// <summary>A circuit trace: two segments with a right angle and a dot at the end.</summary>
        private static void Trace(Layers layers, Vector2 start, Vector2 bend, Vector2 end, float width, Color color, Color dot)
        {
            Rect bounds = Sd.Bounds(new[] { start, bend, end });
            bounds = Grow(bounds, width * 4f);
            Sdf shape = p => Sd.Union(Sd.Segment(p, start, bend, width), Sd.Segment(p, bend, end, width));
            layers.Fill(shape, MonopolyStyle.WithAlpha(color, 0.4f), MonopolyStyle.WithAlpha(color, 0.45f), bounds);
            layers.Fill(p => Sd.Circle(p, end, width * 2.2f), MonopolyStyle.WithAlpha(dot, 0.8f), MonopolyStyle.WithAlpha(dot, 0.8f), Sd.Around(end, width * 3f));
            layers.Fill(p => Sd.Circle(p, start, width * 1.6f), MonopolyStyle.WithAlpha(color, 0.7f), MonopolyStyle.WithAlpha(color, 0.6f), Sd.Around(start, width * 3f));
        }

        /// <summary>Random stars inside <paramref name="area"/>: small dots, a few bigger ones with a soft halo.</summary>
        private static void Starfield(Raster raster, Rect area, int count, int seed, bool glowLayer)
        {
            if (raster == null)
            {
                return;
            }
            var random = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                var p = new Vector2(area.xMin + (float)random.NextDouble() * area.width, area.yMin + (float)random.NextDouble() * area.height);
                float radius = 0.6f + (float)random.NextDouble() * 1.4f;
                float brightness = 0.35f + (float)random.NextDouble() * 0.65f;
                bool big = i % 23 == 0;
                if (big)
                {
                    radius *= 1.8f;
                    raster.Fill(q => Sd.Circle(q, p, radius * 3.5f), new Color(0.6f, 0.9f, 1f, (glowLayer ? 0.25f : 0.14f) * brightness), Sd.Around(p, radius * 4f));
                }
                Color star = new Color(0.85f, 0.95f, 1f, glowLayer ? (big ? brightness : brightness * 0.35f) : brightness);
                raster.Fill(q => Sd.Circle(q, p, radius), star, Sd.Around(p, radius + 2f));
            }
        }

        /// <summary>A card spot as a holographic panel: a dark rounded box with a rim in the deck colour and a hex grid inside.</summary>
        private static void HoloSpot(Layers layers, Vector2 center, float halfWidth, float halfHeight, float angle, Color color, float line, Color panel)
        {
            var half = new Vector2(halfWidth, halfHeight);
            float radius = halfWidth * 0.12f;
            Sdf box = p => Sd.Box(Sd.Rotate(p, center, angle), center, half, radius);
            Rect bounds = Sd.Around(center, halfHeight * 1.5f);
            layers.Main.Fill(box, MonopolyStyle.WithAlpha(panel, 0.92f), bounds);
            HexGrid(layers.Main, p => Sd.Box(Sd.Rotate(p, center, angle), center, half - Vector2.one * line * 3f, radius), bounds, halfWidth * 0.16f, MonopolyStyle.WithAlpha(color, 0.16f), line * 0.35f);
            layers.Fill(p => Sd.Outline(box(p), line * 1.4f), MonopolyStyle.WithAlpha(color, 0.95f), MonopolyStyle.WithAlpha(color, 0.9f), bounds);
            layers.Fill(p => Sd.Outline(Sd.Box(Sd.Rotate(p, center, angle), center, half - Vector2.one * line * 4f, radius), line * 0.6f), MonopolyStyle.WithAlpha(color, 0.45f), MonopolyStyle.WithAlpha(color, 0.35f), bounds);
        }

        /// <summary>A grid of hexagon outlines clipped to <paramref name="clip"/>.</summary>
        private static void HexGrid(Raster raster, Sdf clip, Rect bounds, float cell, Color color, float width)
        {
            float w = cell * 1.732f;
            float h = cell * 1.5f;
            int columns = Mathf.CeilToInt(bounds.width / w) + 2;
            int rows = Mathf.CeilToInt(bounds.height / h) + 2;
            for (int row = -1; row < rows; row++)
            {
                for (int column = -1; column < columns; column++)
                {
                    var c = new Vector2(bounds.xMin + column * w + (row % 2 == 0 ? 0f : w * 0.5f), bounds.yMin + row * h);
                    var hexagon = new List<Vector2>();
                    for (int i = 0; i < 6; i++)
                    {
                        float a = (60f * i + 30f) * Mathf.Deg2Rad;
                        hexagon.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * cell);
                    }
                    raster.Fill(p => Sd.Intersect(Sd.Outline(Sd.Polygon(p, hexagon), width), clip(p)), color, Sd.Around(c, cell + width * 2f));
                }
            }
        }

        private static void CardSpot(Raster raster, Vector2 center, float halfWidth, float halfHeight, float angle, Color color, float line)
        {
            var half = new Vector2(halfWidth, halfHeight);
            float radius = halfWidth * 0.12f;
            Sdf box = p => Sd.Box(Sd.Rotate(p, center, angle), center, half, radius);
            Rect bounds = Sd.Around(center, halfHeight * 1.5f);
            raster.Fill(box, MonopolyStyle.WithAlpha(color, 0.14f), bounds);
            raster.Fill(p => Sd.Outline(box(p), line * 1.4f), MonopolyStyle.WithAlpha(color, 0.9f), bounds);
            raster.Fill(p => Sd.Outline(Sd.Box(Sd.Rotate(p, center, angle), center, half - Vector2.one * line * 4f, radius), line * 0.7f), MonopolyStyle.WithAlpha(color, 0.5f), bounds);
        }

        private static void Outline(Raster raster, Rect rect, float width, Color color)
        {
            float h = width * 0.5f;
            raster.FillRect(Rect.MinMaxRect(rect.xMin - h, rect.yMin - h, rect.xMax + h, rect.yMin + h), color);
            raster.FillRect(Rect.MinMaxRect(rect.xMin - h, rect.yMax - h, rect.xMax + h, rect.yMax + h), color);
            raster.FillRect(Rect.MinMaxRect(rect.xMin - h, rect.yMin + h, rect.xMin + h, rect.yMax - h), color);
            raster.FillRect(Rect.MinMaxRect(rect.xMax - h, rect.yMin + h, rect.xMax + h, rect.yMax - h), color);
        }

        private static Rect Grow(Rect rect, float amount)
        {
            return Rect.MinMaxRect(rect.xMin - amount, rect.yMin - amount, rect.xMax + amount, rect.yMax + amount);
        }

        // ------------------------------------------------------------------ table

        /// <summary>A tileable tabletop: four wooden planks, or the starfield beyond a space dock.</summary>
        public static Texture2D Table(int size, MonopolyThemeSpec spec)
        {
            return spec.Style == ArtStyle.Galactic ? SpaceDock(size) : WoodenTable(size);
        }

        private static Texture2D WoodenTable(int size)
        {
            var raster = new Raster(size, size, Color.black);
            Color dark = MonopolyStyle.Hex(0x3E2416);
            Color light = MonopolyStyle.Hex(0x7A4A2B);
            int planks = 4;
            int plankWidth = size / planks;
            var random = new System.Random(11);
            var tints = Enumerable.Range(0, planks).Select(_ => 0.85f + (float)random.NextDouble() * 0.3f).ToArray();
            var joints = Enumerable.Range(0, planks).Select(_ => random.Next(size)).ToArray();
            for (int y = 0; y < size; y++)
            {
                float v = y / (float)size;
                for (int x = 0; x < size; x++)
                {
                    int plank = x / plankWidth;
                    float u = x / (float)size;
                    float warp = 0.25f * Mathf.Sin(2f * Mathf.PI * (v * 2f + plank * 0.37f)) + 0.06f * Mathf.Sin(2f * Mathf.PI * (v * 9f + plank));
                    float grain = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (u * 52f + warp * 3f));
                    float fine = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (u * 190f + warp * 7f + v * 3f));
                    float t = Mathf.Clamp01(0.35f + 0.45f * grain * grain + 0.12f * fine);
                    Color c = Color.Lerp(dark, light, t) * tints[plank];
                    int inPlank = x - plank * plankWidth;
                    if (inPlank < 3 || inPlank > plankWidth - 3)
                    {
                        c *= 0.55f;
                    }
                    int joint = (y - joints[plank] + size) % size;
                    if (joint < 3)
                    {
                        c *= 0.6f;
                    }
                    c.a = 1f;
                    raster.Set(x, y, c);
                }
            }
            raster.Grain(new Rect(0, 0, size, size), 0.035f, 5);
            return raster.ToTexture(false);
        }

        /// <summary>Deep space seen past the dock: a faint nebula (periodic, so the tile is seamless), stars and a dock grid.</summary>
        private static Texture2D SpaceDock(int size)
        {
            var raster = new Raster(size, size, Color.black);
            Color deep = MonopolyStyle.Hex(0x03050A);
            Color violet = MonopolyStyle.Hex(0x1A1440);
            Color teal = MonopolyStyle.Hex(0x0A2A33);
            for (int y = 0; y < size; y++)
            {
                float v = y / (float)size;
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float a = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (u * 2f + 0.3f * Mathf.Sin(2f * Mathf.PI * v)));
                    float b = 0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * (v * 1f + 0.25f * Mathf.Sin(2f * Mathf.PI * u * 3f)));
                    float c = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (u * 1f - v * 2f));
                    float nebula = Mathf.Pow(a * b, 1.6f);
                    float wisp = Mathf.Pow(b * c, 2.2f);
                    Color color = Color.Lerp(deep, violet, nebula * 0.7f);
                    color = Color.Lerp(color, teal, wisp * 0.6f);
                    color.a = 1f;
                    raster.Set(x, y, color);
                }
            }
            // The dock grid: faint lines eight times across the tile.
            Color grid = new Color(0.16f, 0.3f, 0.45f, 0.22f);
            for (int i = 0; i < 8; i++)
            {
                float at = i * size / 8f;
                raster.FillRect(new Rect(at - 0.75f, 0f, 1.5f, size), grid);
                raster.FillRect(new Rect(0f, at - 0.75f, size, 1.5f), grid);
            }
            // Stars, drawn again across the edge so the tile repeats without a seam.
            var random = new System.Random(19);
            for (int i = 0; i < 1100; i++)
            {
                var p = new Vector2((float)random.NextDouble() * size, (float)random.NextDouble() * size);
                float radius = 0.5f + (float)random.NextDouble() * 1.3f;
                float brightness = 0.3f + (float)random.NextDouble() * 0.7f;
                bool big = i % 37 == 0;
                foreach (Vector2 offset in new[] { Vector2.zero, new Vector2(size, 0f), new Vector2(-size, 0f), new Vector2(0f, size), new Vector2(0f, -size) })
                {
                    Vector2 at = p + offset;
                    if (at.x < -8f || at.y < -8f || at.x > size + 8f || at.y > size + 8f)
                    {
                        continue;
                    }
                    if (big)
                    {
                        raster.Fill(q => Sd.Circle(q, at, radius * 5f), new Color(0.6f, 0.85f, 1f, 0.18f * brightness), Sd.Around(at, radius * 6f));
                        raster.Fill(q => Sd.Union(Sd.Segment(q, at + new Vector2(-radius * 6f, 0f), at + new Vector2(radius * 6f, 0f), 0.6f),
                            Sd.Segment(q, at + new Vector2(0f, -radius * 6f), at + new Vector2(0f, radius * 6f), 0.6f)), new Color(0.85f, 0.95f, 1f, 0.5f * brightness), Sd.Around(at, radius * 7f));
                        raster.Fill(q => Sd.Circle(q, at, radius * 1.6f), new Color(0.9f, 0.97f, 1f, brightness), Sd.Around(at, radius * 3f));
                    }
                    else
                    {
                        raster.Fill(q => Sd.Circle(q, at, radius), new Color(0.85f, 0.93f, 1f, brightness), Sd.Around(at, radius + 2f));
                    }
                }
            }
            return raster.ToTexture(false);
        }

        // ------------------------------------------------------------------ dice

        /// <summary>A 3 by 2 atlas of die faces (1 to 6 as the die mesh maps them); the speed die has 1, 2, 3, the bus and Mr. Monopoly twice.</summary>
        public static Texture2D DiceAtlas(bool speed, int cell, MonopolyThemeSpec spec)
        {
            Color body = speed ? spec.SpeedDieBody : spec.DieBody;
            Color pip = speed ? spec.SpeedDiePip : spec.DiePip;
            bool glowing = spec.Emissive;
            var raster = new Raster(cell * 3, cell * 2, body);
            for (int face = 0; face < 6; face++)
            {
                var origin = new Vector2((face % 3) * cell, face < 3 ? cell : 0);
                Vector2 center = origin + new Vector2(cell * 0.5f, cell * 0.5f);
                // A little shading towards the edges hints at the rounding.
                raster.Fill(p => Sd.Box(p, center, new Vector2(cell * 0.5f, cell * 0.5f), 0f),
                    p => new Color(0f, 0f, 0f, Mathf.Clamp01(((p - center).magnitude / (cell * 0.5f) - 0.75f) * 0.25f)), new Rect(origin, new Vector2(cell, cell)));
                if (glowing)
                {
                    // A thin frame around each face.
                    raster.Fill(p => Sd.Outline(Sd.Box(p, center, new Vector2(cell * 0.42f, cell * 0.42f), cell * 0.06f), cell * 0.012f), MonopolyStyle.WithAlpha(pip, 0.45f), new Rect(origin, new Vector2(cell, cell)));
                }
                int value = face + 1;
                if (speed && value >= 4)
                {
                    if (value == 4)
                    {
                        Bus(raster, center, cell * 0.36f, pip, glowing);
                    }
                    else
                    {
                        TopHat(raster, center, cell * 0.34f, pip);
                    }
                    continue;
                }
                foreach (Vector2 offset in Pips(value))
                {
                    Vector2 c = center + offset * cell * 0.27f;
                    float r = cell * 0.085f;
                    if (glowing)
                    {
                        raster.Fill(p => Sd.Circle(p, c, r * 2.2f), MonopolyStyle.WithAlpha(pip, 0.2f), Sd.Around(c, r * 2.5f));
                    }
                    raster.Fill(p => Sd.Circle(p, c, r), pip, Sd.Around(c, r + 2f));
                    if (!glowing)
                    {
                        // The pips are drilled: a soft highlight on their lower edge.
                        raster.Fill(p => Sd.Outline(Sd.Circle(p, c, r * 0.82f), r * 0.18f), p => new Color(1f, 1f, 1f, p.y < c.y ? 0.18f : 0f), Sd.Around(c, r));
                    }
                }
            }
            return raster.ToTexture(false);
        }

        private static IEnumerable<Vector2> Pips(int value)
        {
            switch (value)
            {
                case 1: return new[] { Vector2.zero };
                case 2: return new[] { new Vector2(-1, 1), new Vector2(1, -1) };
                case 3: return new[] { new Vector2(-1, 1), Vector2.zero, new Vector2(1, -1) };
                case 4: return new[] { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1) };
                case 5: return new[] { new Vector2(-1, 1), new Vector2(1, 1), Vector2.zero, new Vector2(-1, -1), new Vector2(1, -1) };
                default: return new[] { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, 0), new Vector2(1, 0), new Vector2(-1, -1), new Vector2(1, -1) };
            }
        }

        /// <summary>Mr. Monopoly's top hat, a silhouette.</summary>
        public static void TopHat(Raster raster, Vector2 center, float size, Color color)
        {
            Vector2 brim = center + new Vector2(0f, -size * 0.55f);
            Rect bounds = Sd.Around(center, size * 1.2f);
            Sdf hat = p => Sd.Union(
                Sd.Ellipse(p, brim, new Vector2(size * 0.95f, size * 0.2f)),
                Sd.Box(p, center + new Vector2(0f, size * 0.12f), new Vector2(size * 0.52f, size * 0.68f), size * 0.08f));
            raster.Fill(hat, color, bounds);
            // The band.
            raster.Fill(p => Sd.Box(p, center + new Vector2(0f, -size * 0.3f), new Vector2(size * 0.53f, size * 0.09f)), MonopolyStyle.WithAlpha(Color.black, 0.35f), bounds);
        }

        /// <summary>The bus of the speed die (a shuttle in the glowing look).</summary>
        public static void Bus(Raster raster, Vector2 center, float size, Color color, bool shuttle = false)
        {
            Rect bounds = Sd.Around(center, size * 1.4f);
            Vector2 body = center + new Vector2(0f, size * 0.12f);
            raster.Fill(p => Sd.Box(p, body, new Vector2(size * 1.1f, size * 0.62f), size * (shuttle ? 0.3f : 0.18f)), color, bounds);
            Color window = shuttle ? MonopolyStyle.WithAlpha(MonopolyStyle.Hex(0x1A1030), 0.95f) : MonopolyStyle.WithAlpha(MonopolyStyle.Hex(0x7A0F1C), 0.95f);
            for (int i = 0; i < 4; i++)
            {
                Vector2 w = body + new Vector2(-size * 0.72f + i * size * 0.44f, size * 0.18f);
                raster.Fill(p => Sd.Box(p, w, new Vector2(size * 0.16f, size * 0.17f), size * 0.04f), window, bounds);
            }
            if (shuttle)
            {
                // Thrusters instead of wheels.
                foreach (float x in new[] { -0.75f, 0.75f })
                {
                    Vector2 thruster = center + new Vector2(size * x, -size * 0.5f);
                    raster.Fill(p => Sd.Box(p, thruster, new Vector2(size * 0.2f, size * 0.16f), size * 0.05f), color, bounds);
                }
                return;
            }
            foreach (float x in new[] { -0.62f, 0.62f })
            {
                Vector2 wheel = center + new Vector2(size * x, -size * 0.55f);
                raster.Fill(p => Sd.Circle(p, wheel, size * 0.2f), color, bounds);
                raster.Fill(p => Sd.Circle(p, wheel, size * 0.09f), window, bounds);
            }
        }

        // ------------------------------------------------------------------ cards

        /// <summary>The back of a Chance or Community Chest card (the icon and name are TextMesh Pro on top).</summary>
        public static Texture2D CardBack(Color color, int width, int height, MonopolyThemeSpec spec)
        {
            var center = new Vector2(width * 0.5f, height * 0.5f);
            var half = new Vector2(width * 0.5f - width * 0.06f, height * 0.5f - width * 0.06f);
            Rect all = new Rect(0, 0, width, height);
            if (spec.Style == ArtStyle.Galactic)
            {
                Color panel = MonopolyStyle.Hex(0x0B1220);
                var raster = new Raster(width, height, MonopolyStyle.Hex(0x060A12));
                Sdf box = p => Sd.Box(p, center, half, width * 0.06f);
                raster.Fill(box, panel, all);
                HexGrid(raster, p => Sd.Box(p, center, half - Vector2.one * width * 0.05f, width * 0.04f), all, width * 0.07f, MonopolyStyle.WithAlpha(color, 0.14f), 1.2f);
                // A holographic sheen across the panel.
                raster.Fill(box, p => new Color(color.r, color.g, color.b, Mathf.Clamp01(0.5f - Mathf.Abs((p.x + p.y) / (width + height) - 0.5f) * 3f) * 0.12f), all);
                raster.Fill(p => Sd.Outline(box(p), width * 0.018f), MonopolyStyle.WithAlpha(color, 0.95f), all);
                raster.Fill(p => Sd.Outline(Sd.Box(p, center, half - Vector2.one * width * 0.04f, width * 0.04f), width * 0.007f), MonopolyStyle.WithAlpha(color, 0.45f), all);
                return raster.ToTexture(false);
            }
            var classic = new Raster(width, height, Color.white);
            classic.Fill(p => Sd.Box(p, center, half, width * 0.06f), p =>
            {
                float stripe = Mathf.Repeat((p.x + p.y) / (width * 0.12f), 1f) < 0.5f ? 0.04f : 0f;
                return Color.Lerp(color, Color.white, stripe);
            }, all);
            classic.Fill(p => Sd.Outline(Sd.Box(p, center, half - Vector2.one * width * 0.04f, width * 0.04f), width * 0.012f), new Color(1f, 1f, 1f, 0.85f), all);
            return classic.ToTexture(false);
        }

        // ------------------------------------------------------------------ interface

        public static Texture2D Rounded(int size, float radius)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            raster.Fill(p => Sd.Box(p, center, center, radius), Color.white, new Rect(0, 0, size, size));
            return raster.ToTexture();
        }

        /// <summary>A soft shadow of a rounded rectangle, for nine slicing (<paramref name="blur"/> pixels of fade).</summary>
        public static Texture2D Shadow(int size, float radius, float blur)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            var half = center - Vector2.one * blur;
            raster.Shadow(p => Sd.Box(p, center, half, radius), new Color(0f, 0f, 0f, 1f), blur, Vector2.zero, new Rect(0, 0, size, size));
            return raster.ToTexture();
        }

        /// <summary>A halo around a rounded rectangle: a glow that is strongest at the edge, for nine slicing.</summary>
        public static Texture2D Halo(int size, float radius, float blur)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            var half = center - Vector2.one * blur;
            raster.Shadow(p => Sd.Box(p, center, half, radius), new Color(1f, 1f, 1f, 1f), blur, Vector2.zero, new Rect(0, 0, size, size));
            // Hollow, so the glow rims the card instead of tinting it.
            raster.Fill(p => Sd.Box(p, center, half - Vector2.one * 2f, radius), new Color(1f, 1f, 1f, 0.35f), new Rect(0, 0, size, size));
            return raster.ToTexture();
        }

        public static Texture2D Circle(int size)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            raster.Fill(p => Sd.Circle(p, center, size * 0.5f - 1f), Color.white, new Rect(0, 0, size, size));
            return raster.ToTexture();
        }

        public static Texture2D Ring(int size, float thickness)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            raster.Fill(p => Sd.Ring(p, center, size * 0.5f - thickness * 0.5f - 1f, thickness), Color.white, new Rect(0, 0, size, size));
            return raster.ToTexture();
        }

        /// <summary>A radial glow, opaque in the middle and fading out.</summary>
        public static Texture2D Glow(int size)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = (new Vector2(x + 0.5f, y + 0.5f) - center).magnitude / (size * 0.5f);
                    float a = Mathf.Clamp01(1f - d);
                    raster.Set(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }
            return raster.ToTexture();
        }

        /// <summary>A vertical gradient from white to transparent (a glossy highlight on buttons); a fainter one with scanlines in the glowing look.</summary>
        public static Texture2D Sheen(int width, int height, MonopolyThemeSpec spec)
        {
            var raster = new Raster(width, height, Color.clear);
            bool scan = spec.Style == ArtStyle.Galactic;
            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1);
                float a = scan ? Mathf.Clamp01((t - 0.35f) * 1.2f) * 0.3f * (y % 4 < 2 ? 1f : 0.6f) : Mathf.Clamp01((t - 0.45f) * 1.4f) * 0.55f;
                for (int x = 0; x < width; x++)
                {
                    raster.Set(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            return raster.ToTexture();
        }

        /// <summary>A banknote in the classic pastel green, or a credit chip.</summary>
        public static Texture2D Bill(int width, int height, MonopolyThemeSpec spec)
        {
            var raster = new Raster(width, height, Color.clear);
            var center = new Vector2(width * 0.5f, height * 0.5f);
            var half = center - Vector2.one * 3f;
            Rect all = new Rect(0, 0, width, height);
            if (spec.Style == ArtStyle.Galactic)
            {
                Color cyan = spec.Palette.chestBlue;
                raster.Fill(p => Sd.Box(p, center, half, 8f), MonopolyStyle.Hex(0x0E1A2E), all);
                raster.Fill(p => Sd.Outline(Sd.Box(p, center, half - Vector2.one * 2f, 7f), 2.5f), cyan, all);
                var hexagon = new List<Vector2>();
                for (int i = 0; i < 6; i++)
                {
                    float a = (60f * i + 30f) * Mathf.Deg2Rad;
                    hexagon.Add(center + new Vector2(-width * 0.18f, 0f) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * height * 0.28f);
                }
                raster.Fill(p => Sd.Polygon(p, hexagon), cyan, all);
                raster.Fill(p => Sd.Polygon(p, hexagon.Select(v => Vector2.Lerp(center + new Vector2(-width * 0.18f, 0f), v, 0.55f)).ToList()), MonopolyStyle.Hex(0x0E1A2E), all);
                for (int i = 0; i < 3; i++)
                {
                    float y = center.y + (i - 1) * height * 0.22f;
                    raster.Fill(p => Sd.Box(p, new Vector2(center.x + width * 0.16f, y), new Vector2(width * 0.16f, height * 0.05f), 2f), MonopolyStyle.WithAlpha(cyan, i == 1 ? 0.95f : 0.55f), all);
                }
                return raster.ToTexture();
            }
            raster.Fill(p => Sd.Box(p, center, half, 6f), MonopolyStyle.Hex(0xBFE3B4), all);
            raster.Fill(p => Sd.Outline(Sd.Box(p, center, half - Vector2.one * 7f, 4f), 3f), MonopolyStyle.Hex(0x3C8A4B), all);
            raster.Fill(p => Sd.Ellipse(p, center, new Vector2(height * 0.3f, height * 0.3f)), MonopolyStyle.Hex(0x3C8A4B), all);
            raster.Fill(p => Sd.Ellipse(p, center, new Vector2(height * 0.22f, height * 0.22f)), MonopolyStyle.Hex(0xE8F5E2), all);
            return raster.ToTexture();
        }

        /// <summary>The glowing frame around a highlighted space.</summary>
        public static Texture2D HighlightFrame(int size)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            float edge = size * 0.5f - size * 0.08f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Sd.Box(new Vector2(x + 0.5f, y + 0.5f), center, new Vector2(edge, edge), size * 0.06f);
                    float line = Mathf.Clamp01(1f - Mathf.Abs(d) / (size * 0.018f));
                    float glow = Mathf.Clamp01(1f - Mathf.Abs(d) / (size * 0.075f));
                    float inside = d < 0f ? 0.16f : 0f;
                    float a = Mathf.Max(line, glow * glow * 0.75f, inside);
                    raster.Set(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            return raster.ToTexture();
        }

        /// <summary>The banner of the logo (the word itself is TextMesh Pro): the red plate with a white keyline, or a dark holographic plate rimmed in cyan.</summary>
        public static Texture2D LogoBanner(int width, int height, MonopolyThemeSpec spec)
        {
            var raster = new Raster(width, height, Color.clear);
            var center = new Vector2(width * 0.5f, height * 0.5f);
            Rect all = new Rect(0, 0, width, height);
            var half = center - Vector2.one * height * 0.08f;
            if (spec.Style == ArtStyle.Galactic)
            {
                Color cyan = spec.Palette.chestBlue;
                Color violet = MonopolyStyle.Hex(0x8A6BFF);
                raster.Shadow(p => Sd.Box(p, center, half, height * 0.08f), MonopolyStyle.WithAlpha(cyan, 0.55f), height * 0.08f, Vector2.zero, all);
                raster.Fill(p => Sd.Box(p, center, half, height * 0.08f), MonopolyStyle.WithAlpha(MonopolyStyle.Hex(0x0A1224), 0.97f), all);
                for (int y = 0; y < height; y += 6)
                {
                    raster.Fill(p => Sd.Intersect(Sd.Box(p, center, half - Vector2.one * height * 0.02f, height * 0.06f), Sd.Box(p, new Vector2(center.x, y + 0.5f), new Vector2(width, 0.5f))), new Color(1f, 1f, 1f, 0.035f), new Rect(0, y - 1, width, 3));
                }
                raster.Fill(p => Sd.Outline(Sd.Box(p, center, half - Vector2.one * height * 0.03f, height * 0.06f), height * 0.028f), MonopolyStyle.WithAlpha(cyan, 0.95f), all);
                raster.Fill(p => Sd.Outline(Sd.Box(p, center, half - Vector2.one * height * 0.09f, height * 0.04f), height * 0.012f), MonopolyStyle.WithAlpha(violet, 0.6f), all);
                return raster.ToTexture();
            }
            raster.Shadow(p => Sd.Box(p, center, half, height * 0.06f), new Color(0f, 0f, 0f, 0.35f), height * 0.06f, new Vector2(0f, -height * 0.02f), all);
            raster.Fill(p => Sd.Box(p, center, half, height * 0.06f), spec.Palette.red, all);
            raster.Fill(p => Sd.Outline(Sd.Box(p, center, half - Vector2.one * height * 0.06f, height * 0.03f), height * 0.035f), Color.white, all);
            return raster.ToTexture();
        }

        /// <summary>A soft round shadow for the feet of tokens and dice.</summary>
        public static Texture2D ContactShadow(int size)
        {
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = (new Vector2(x + 0.5f, y + 0.5f) - center).magnitude / (size * 0.5f);
                    float a = Mathf.Clamp01(1f - d);
                    raster.Set(x, y, new Color(0f, 0f, 0f, a * a * 0.7f));
                }
            }
            return raster.ToTexture();
        }

        // ------------------------------------------------------------------ buildings

        /// <summary>
        /// The skin of a dome (a house) or a tower (a hotel) of the station: a dark hull with rows of lit windows, mapped
        /// around the lathe (u around, v from the foot to the top). Doubles as the emission map: only the windows are bright.
        /// </summary>
        public static Texture2D BuildingSkin(bool tower, int size, MonopolyThemeSpec spec)
        {
            Color hull = MonopolyStyle.Hex(0x1C2740);
            Color window = spec.Palette.chestBlue;
            Color warm = spec.Palette.gold;
            var raster = new Raster(size, size, hull);
            Rect all = new Rect(0, 0, size, size);
            // A slightly lighter band at the foot.
            raster.FillRect(new Rect(0f, 0f, size, size * 0.1f), MonopolyStyle.Hex(0x2A3854));
            if (tower)
            {
                int columns = 8;
                int rows = 7;
                for (int row = 0; row < rows; row++)
                {
                    for (int column = 0; column < columns; column++)
                    {
                        var c = new Vector2((column + 0.5f) * size / columns, size * (0.16f + row * 0.1f));
                        var half = new Vector2(size / columns * 0.26f, size * 0.032f);
                        bool dark = (row * 3 + column * 5) % 7 == 0;
                        raster.Fill(p => Sd.Box(p, c, half, 1.5f), dark ? MonopolyStyle.Hex(0x101828) : (row + column) % 5 == 0 ? warm : window, all);
                    }
                }
                raster.FillRect(new Rect(0f, size * 0.9f, size, size * 0.1f), MonopolyStyle.Hex(0x2A3854));
                raster.FillRect(new Rect(0f, size * 0.955f, size, size * 0.02f), window);
            }
            else
            {
                int columns = 12;
                for (int column = 0; column < columns; column++)
                {
                    var c = new Vector2((column + 0.5f) * size / columns, size * 0.33f);
                    var half = new Vector2(size / columns * 0.3f, size * 0.05f);
                    raster.Fill(p => Sd.Box(p, c, half, 2f), column % 4 == 2 ? warm : window, all);
                }
                // The beacon on top.
                raster.FillRect(new Rect(0f, size * 0.93f, size, size * 0.07f), window);
            }
            return raster.ToTexture(false);
        }

        // ------------------------------------------------------------------ token badges

        /// <summary>A white silhouette of a token for the interface badges, in <paramref name="size"/> pixels.</summary>
        public static Texture2D TokenBadge(int token, int size, MonopolyThemeSpec spec)
        {
            var raster = new Raster(size, size, Color.clear);
            Color white = Color.white;
            if (spec.Style == ArtStyle.Galactic)
            {
                SpaceBadge(raster, token, size, white);
                return raster.ToTexture();
            }
            switch (token)
            {
                case 0:
                    DrawCar(raster, size, white);
                    break;
                case 1:
                {
                    var c = new Vector2(size * 0.5f, size * 0.52f);
                    TopHat(raster, c, size * 0.34f, white);
                    break;
                }
                case 2:
                    FillOutline(raster, TokenOutlines.Outline(TokenOutlines.Dog), size, white);
                    break;
                case 3:
                    DrawShip(raster, size, white);
                    break;
                case 4:
                    FillOutline(raster, TokenOutlines.Outline(TokenOutlines.Cat), size, white);
                    break;
                case 5:
                    DrawDuck(raster, size, white);
                    break;
                case 6:
                    DrawPenguin(raster, size, white);
                    break;
                default:
                    FillOutline(raster, TokenOutlines.Outline(TokenOutlines.Dino), size, white);
                    break;
            }
            return raster.ToTexture();
        }

        /// <summary>The silhouettes of the spacecraft tokens: rocket, satellite, robot, UFO, comet, ringed planet, helmet, station.</summary>
        private static void SpaceBadge(Raster raster, int token, int size, Color color)
        {
            float s = size / 256f;
            Rect all = new Rect(0, 0, size, size);
            Vector2 P(float x, float y) => new Vector2(x, y) * s;
            switch (token)
            {
                case 0:
                {
                    var body = new[] { P(128, 238), P(152, 196), P(158, 130), P(150, 70), P(106, 70), P(98, 130), P(104, 196) };
                    raster.Fill(p => Sd.Polygon(p, body), color, all);
                    raster.Fill(p => Sd.Polygon(p, new[] { P(104, 120), P(60, 52), P(108, 66) }), color, all);
                    raster.Fill(p => Sd.Polygon(p, new[] { P(152, 120), P(196, 52), P(148, 66) }), color, all);
                    raster.Fill(p => Sd.Polygon(p, new[] { P(112, 66), P(128, 22), P(144, 66) }), color, all);
                    raster.Erase(p => Sd.Circle(p, P(128, 156), 13f * s), all);
                    break;
                }
                case 1:
                {
                    raster.Fill(p => Sd.Box(p, P(128, 122), P(26, 26), 4f * s), color, all);
                    raster.Fill(p => Sd.Box(p, P(58, 122), P(42, 20), 3f * s), color, all);
                    raster.Fill(p => Sd.Box(p, P(198, 122), P(42, 20), 3f * s), color, all);
                    raster.Erase(p => Sd.Box(p, P(58, 122), P(3, 20)), all);
                    raster.Erase(p => Sd.Box(p, P(198, 122), P(3, 20)), all);
                    raster.Fill(p => Sd.Segment(p, P(128, 148), P(128, 176), 4f * s), color, all);
                    raster.Fill(p => Sd.Circle(p, P(128, 196), 28f * s), color, all);
                    raster.Erase(p => Sd.Circle(p, P(128, 204), 16f * s), all);
                    raster.Fill(p => Sd.Circle(p, P(128, 200), 5f * s), color, all);
                    break;
                }
                case 2:
                {
                    raster.Fill(p => Sd.Box(p, P(128, 176), P(36, 28), 6f * s), color, all);
                    raster.Erase(p => Sd.Circle(p, P(114, 180), 7f * s), all);
                    raster.Erase(p => Sd.Circle(p, P(142, 180), 7f * s), all);
                    raster.Fill(p => Sd.Segment(p, P(128, 204), P(128, 224), 3f * s), color, all);
                    raster.Fill(p => Sd.Circle(p, P(128, 230), 8f * s), color, all);
                    raster.Fill(p => Sd.Box(p, P(128, 108), P(40, 36), 6f * s), color, all);
                    raster.Erase(p => Sd.Box(p, P(128, 112), P(16, 8), 2f * s), all);
                    raster.Fill(p => Sd.Box(p, P(74, 112), P(12, 32), 5f * s), color, all);
                    raster.Fill(p => Sd.Box(p, P(182, 112), P(12, 32), 5f * s), color, all);
                    raster.Fill(p => Sd.Box(p, P(110, 48), P(13, 26), 4f * s), color, all);
                    raster.Fill(p => Sd.Box(p, P(146, 48), P(13, 26), 4f * s), color, all);
                    break;
                }
                case 3:
                {
                    raster.Fill(p => Sd.Ellipse(p, P(128, 112), P(108, 28)), color, all);
                    raster.Fill(p => Sd.Ellipse(p, P(128, 140), P(46, 34)), color, all);
                    raster.Erase(p => Sd.Ellipse(p, P(128, 146), P(30, 18)), all);
                    foreach (float x in new[] { 76f, 128f, 180f })
                    {
                        raster.Erase(p => Sd.Circle(p, P(x, 106), 7f * s), all);
                    }
                    foreach (float x in new[] { 84f, 128f, 172f })
                    {
                        raster.Fill(p => Sd.Segment(p, P(x, 88), P(x + (x - 128f) * 0.2f, 62), 4f * s), color, all);
                    }
                    break;
                }
                case 4:
                {
                    raster.Fill(p => Sd.Polygon(p, new[] { P(122, 190), P(186, 128), P(24, 30) }), color, all);
                    raster.Fill(p => Sd.Polygon(p, new[] { P(176, 112), P(190, 140), P(60, 74) }), MonopolyStyle.WithAlpha(color, 0.6f), all);
                    raster.Fill(p => Sd.Circle(p, P(156, 158), 42f * s), color, all);
                    raster.Erase(p => Sd.Circle(p, P(168, 168), 10f * s), all);
                    break;
                }
                case 5:
                {
                    Vector2 c = P(128, 132);
                    raster.Fill(p => Sd.Circle(p, c, 62f * s), color, all);
                    raster.Erase(p => Sd.Outline(Sd.Ellipse(Sd.Rotate(p, c, -20f), c, P(116, 30)), 16f * s), all);
                    raster.Fill(p => Sd.Outline(Sd.Ellipse(Sd.Rotate(p, c, -20f), c, P(116, 30)), 10f * s), color, all);
                    break;
                }
                case 6:
                {
                    raster.Fill(p => Sd.Circle(p, P(128, 136), 84f * s), color, all);
                    raster.Erase(p => Sd.Ellipse(p, P(128, 142), P(58, 46)), all);
                    raster.Fill(p => Sd.Ellipse(p, P(128, 142), P(46, 34)), MonopolyStyle.WithAlpha(color, 0.35f), all);
                    raster.Fill(p => Sd.Box(p, P(128, 44), P(44, 12), 5f * s), color, all);
                    break;
                }
                default:
                {
                    Vector2 c = P(128, 128);
                    raster.Fill(p => Sd.Ring(p, c, 90f * s, 18f * s), color, all);
                    raster.Fill(p => Sd.Circle(p, c, 32f * s), color, all);
                    for (int i = 0; i < 4; i++)
                    {
                        float a = (45f + 90f * i) * Mathf.Deg2Rad;
                        Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        raster.Fill(p => Sd.Segment(p, c + d * 30f * s, c + d * 82f * s, 7f * s), color, all);
                    }
                    raster.Fill(p => Sd.Box(p, P(128, 128), P(6, 116), 2f * s), color, all);
                    raster.Erase(p => Sd.Circle(p, c, 12f * s), all);
                    break;
                }
            }
        }

        /// <summary>Fills an outline scaled to fit the badge with a margin.</summary>
        private static void FillOutline(Raster raster, List<Vector2> outline, int size, Color color)
        {
            List<Vector2> fitted = Fit(outline, size, 0.1f);
            raster.Fill(p => Sd.Polygon(p, fitted), color, Sd.Bounds(fitted));
        }

        private static List<Vector2> Fit(IList<Vector2> points, int size, float margin)
        {
            Rect bounds = Sd.Bounds(points);
            float scale = size * (1f - margin * 2f) / Mathf.Max(bounds.width, bounds.height);
            Vector2 offset = new Vector2(size * 0.5f, size * 0.5f) - bounds.center * scale;
            return points.Select(p => p * scale + offset).ToList();
        }

        private static void DrawCar(Raster raster, int size, Color color)
        {
            List<Vector2> body = TokenOutlines.Outline(TokenOutlines.CarBody);
            // The car with its wheels spans x 0..108 and y 0..44 in design units.
            float scale = size * 0.8f / 108f;
            Vector2 offset = new Vector2(size * 0.1f, size * 0.32f);
            List<Vector2> fitted = body.Select(p => p * scale + offset).ToList();
            raster.Fill(p => Sd.Polygon(p, fitted), color, Sd.Bounds(fitted));
            foreach (float x in new[] { 22f, 88f })
            {
                Vector2 wheel = new Vector2(x, 13f) * scale + offset;
                raster.Fill(p => Sd.Circle(p, wheel, 14f * scale), color, Sd.Around(wheel, 15f * scale));
                raster.Erase(p => Sd.Circle(p, wheel, 5f * scale), Sd.Around(wheel, 6f * scale));
            }
            Vector2 head = new Vector2(40f, 43f) * scale + offset;
            raster.Fill(p => Sd.Circle(p, head, 7.5f * scale), color, Sd.Around(head, 8f * scale));
        }

        private static void DrawShip(Raster raster, int size, Color color)
        {
            float s = size / 256f;
            var hull = new List<Vector2>
            {
                new Vector2(20, 118), new Vector2(236, 118), new Vector2(214, 82), new Vector2(40, 82)
            }.Select(p => p * s).ToList();
            raster.Fill(p => Sd.Polygon(p, hull), color, Sd.Bounds(hull));
            Rect all = new Rect(0, 0, size, size);
            raster.Fill(p => Sd.Box(p, new Vector2(128, 128) * s, new Vector2(56, 14) * s, 3f * s), color, all);
            raster.Fill(p => Sd.Box(p, new Vector2(118, 150) * s, new Vector2(24, 16) * s, 3f * s), color, all);
            raster.Fill(p => Sd.Box(p, new Vector2(96, 150) * s, new Vector2(8, 24) * s, 2f * s), color, all);
            raster.Fill(p => Sd.Segment(p, new Vector2(128, 166) * s, new Vector2(128, 200) * s, 3f * s), color, all);
            foreach (float x in new[] { 64f, 192f })
            {
                Vector2 turret = new Vector2(x, 124) * s;
                raster.Fill(p => Sd.Box(p, turret, new Vector2(14, 9) * s, 5f * s), color, all);
                Vector2 tip = turret + new Vector2(x < 128 ? -34f : 34f, 6f) * s;
                raster.Fill(p => Sd.Segment(p, turret + new Vector2(0f, 4f) * s, tip, 3f * s), color, all);
            }
        }

        private static void DrawDuck(Raster raster, int size, Color color)
        {
            float s = size / 256f;
            Rect all = new Rect(0, 0, size, size);
            Sdf duck = p => Sd.Union(Sd.Union(
                    Sd.Ellipse(p, new Vector2(118, 100) * s, new Vector2(86, 56) * s),
                    Sd.Circle(p, new Vector2(160, 164) * s, 42f * s)),
                Sd.Union(Sd.Ellipse(p, new Vector2(212, 156) * s, new Vector2(30, 12) * s),
                    Sd.Polygon(p, new[] { new Vector2(34, 120) * s, new Vector2(46, 172) * s, new Vector2(72, 132) * s })));
            raster.Fill(duck, color, all);
            raster.Erase(p => Sd.Circle(p, new Vector2(172, 176) * s, 7f * s), all);
        }

        private static void DrawPenguin(Raster raster, int size, Color color)
        {
            float s = size / 256f;
            Rect all = new Rect(0, 0, size, size);
            Sdf penguin = p => Sd.Union(Sd.Union(
                    Sd.Ellipse(p, new Vector2(124, 102) * s, new Vector2(58, 80) * s),
                    Sd.Circle(p, new Vector2(128, 190) * s, 40f * s)),
                Sd.Union(Sd.Polygon(p, new[] { new Vector2(160, 196) * s, new Vector2(198, 186) * s, new Vector2(160, 178) * s }),
                    Sd.Union(Sd.Ellipse(p, new Vector2(150, 24) * s, new Vector2(26, 9) * s), Sd.Ellipse(p, new Vector2(92, 24) * s, new Vector2(24, 9) * s))));
            raster.Fill(penguin, color, all);
            raster.Erase(p => Sd.Outline(Sd.Ellipse(Sd.Rotate(p, new Vector2(88, 108) * s, -18f), new Vector2(88, 108) * s, new Vector2(14, 46) * s), 4f * s), all);
            raster.Erase(p => Sd.Circle(p, new Vector2(142, 198) * s, 6f * s), all);
        }
    }
}
