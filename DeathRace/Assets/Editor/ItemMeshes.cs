using Gamebox.Editor;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// The things that lie on the track or fly over it: the mine, the rocket, the oil slick's puddle, the mortar shell,
    /// the plasma bolt and the four pickups. They are seen from about sixty degrees above at race distance, so every one
    /// is a bold shape with a colour of its own (the mine's red lamp, the black oil with its rainbow sheen, the olive
    /// shell with its yellow band, the white-hot bolt, the green cash crate with its dollar sign, the white repair case
    /// with the red cross and a wrench, the olive ammunition box with brass rounds, the blue nitro bottle). Origins are on
    /// the ground (the flying ones at their middle); the rocket, the shell and the bolt face +z.
    /// </summary>
    internal static class ItemMeshes
    {
        private static Vector2 UV(string color)
        {
            return CarArt.UV(color);
        }

        // ------------------------------------------------------------------ mine

        /// <summary>A low spiked disc with a pressure plate; its lamp is <see cref="MineBeacon"/>.</summary>
        public static LowPolyBuilder Mine()
        {
            var m = new LowPolyBuilder();
            const float radius = 0.6f;
            m.Color(UV("MineBody")).Cylinder(Vector3.zero, radius, radius, 0.1f, 16);
            m.Color(UV("MineTop")).Cylinder(new Vector3(0f, 0.1f, 0f), radius, radius * 0.72f, 0.09f, 16);
            m.Color(UV("Hazard")).Cylinder(new Vector3(0f, 0.19f, 0f), radius * 0.6f, radius * 0.6f, 0.012f, 16);
            m.Color(UV("HazardDark")).Cylinder(new Vector3(0f, 0.19f, 0f), radius * 0.44f, radius * 0.44f, 0.02f, 16);
            m.Color(UV("MineBody")).Cylinder(new Vector3(0f, 0.2f, 0f), radius * 0.3f, radius * 0.26f, 0.04f, 12);
            const int spikes = 8;
            for (int i = 0; i < spikes; i++)
            {
                float angle = (i + 0.5f) * Mathf.PI * 2f / spikes;
                var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 foot = radial * (radius * 0.86f) + Vector3.up * 0.13f;
                m.Color(UV("Spike")).Rod(foot, foot + radial * 0.26f + Vector3.up * 0.16f, 0.055f, 6, false, 0f);
                m.Color(UV("MineBody")).Box(radial * (radius * 0.98f) + Vector3.up * 0.05f, new Vector3(0.08f, 0.1f, 0.08f));
            }
            return m;
        }

        /// <summary>The mine's lamp (its own renderer, so the race can blink it).</summary>
        public static LowPolyBuilder MineBeacon()
        {
            var m = new LowPolyBuilder();
            m.Color(UV("MineLight")).Sphere(new Vector3(0f, 0.235f, 0f), new Vector3(0.15f, 0.13f, 0.15f), 4, 12, false, 0.5f);
            return m;
        }

        // ------------------------------------------------------------------ rocket

        /// <summary>A rocket along +z, 1.4 m: white body, red nose, dark bands, four fins, a nozzle at the back.</summary>
        public static LowPolyBuilder Rocket()
        {
            var m = new LowPolyBuilder();
            const float r = 0.11f;
            const float y = 0f;
            m.Color(UV("RocketBody")).Rod(new Vector3(0f, y, -0.55f), new Vector3(0f, y, 0.45f), r, 12, true);
            m.Color(UV("RocketNose")).Rod(new Vector3(0f, y, 0.45f), new Vector3(0f, y, 0.82f), r, 12, true, 0.01f);
            m.Color(UV("RocketBand")).Rod(new Vector3(0f, y, 0.3f), new Vector3(0f, y, 0.38f), r * 1.04f, 12, true);
            m.Color(UV("RocketBand")).Rod(new Vector3(0f, y, -0.4f), new Vector3(0f, y, -0.32f), r * 1.04f, 12, true);
            m.Color(UV("GunDark")).Rod(new Vector3(0f, y, -0.66f), new Vector3(0f, y, -0.55f), r * 0.8f, 10, false, r * 0.95f);
            for (int i = 0; i < 4; i++)
            {
                m.Push(new Vector3(0f, y, -0.42f), Quaternion.Euler(0f, 0f, 45f + i * 90f));
                m.Color(UV(i % 2 == 0 ? "RocketNose" : "RocketBand")).Box(new Vector3(0f, r + 0.09f, 0f), new Vector3(0.02f, 0.18f, 0.26f));
                m.Pop();
            }
            return m;
        }

        // ------------------------------------------------------------------ special weapons and plasma

        /// <summary>
        /// The puddle of an oil slick: a flat disc of 1 m radius (the race scales it to the slick) with a lobed, ragged rim,
        /// facing up at the origin; its texture is laid over it from above (u along x, v along z), so the dark body and
        /// the rainbow sheen of <see cref="OilTexture"/> sit on it as painted.
        /// </summary>
        public static Mesh OilPuddle()
        {
            const int sides = 40;
            var random = new System.Random(57);
            float phaseA = (float)random.NextDouble() * Mathf.PI * 2f;
            float phaseB = (float)random.NextDouble() * Mathf.PI * 2f;
            var vertices = new Vector3[sides + 1];
            var uvs = new Vector2[sides + 1];
            var normals = new Vector3[sides + 1];
            var triangles = new int[sides * 3];
            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);
            normals[0] = Vector3.up;
            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;
                float reach = 0.9f + 0.06f * Mathf.Sin(angle * 3f + phaseA) + 0.04f * Mathf.Sin(angle * 5f + phaseB) + 0.03f * (float)random.NextDouble();
                var point = new Vector3(Mathf.Cos(angle) * reach, 0f, Mathf.Sin(angle) * reach);
                vertices[i + 1] = point;
                uvs[i + 1] = new Vector2(0.5f + point.x * 0.5f, 0.5f + point.z * 0.5f);
                normals[i + 1] = Vector3.up;
                // Clockwise seen from above: the disc faces the sky.
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = (i + 1) % sides + 1;
                triangles[i * 3 + 2] = i + 1;
            }
            var mesh = new Mesh { name = "OilPuddle", vertices = vertices, uv = uvs, normals = normals, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// The oil on the puddle: nearly black, a thin film near the rim and in swirls shifting through the colours of
        /// oil on water, a few glints of the sky, soft at the edge (the puddle's lobes cut the rest).
        /// </summary>
        public static Texture2D OilTexture()
        {
            return CarArt.ToTexture(128, 128, (u, v) =>
            {
                var p = new Vector2(u - 0.5f, v - 0.5f) * 2f;
                float r = p.magnitude;
                float a = Mathf.Atan2(p.y, p.x);
                float swirl = Mathf.Sin(a * 3f + r * 11f) * 0.5f + 0.5f;
                float film = Mathf.Clamp01(1f - Mathf.Abs(r - 0.72f) / 0.12f) * 0.8f + Mathf.Pow(swirl, 6f) * 0.5f * Mathf.Clamp01(r * 1.6f);
                float hue = Mathf.Repeat(r * 1.9f + swirl * 0.35f + a * 0.08f, 1f);
                Color sheen = Color.HSVToRGB(hue, 0.6f, 0.6f);
                float glint = Mathf.Pow(Mathf.Clamp01(1f - new Vector2(p.x + 0.32f, p.y - 0.28f).magnitude / 0.16f), 2f) * 0.55f;
                Color body = new Color(0.02f, 0.022f, 0.028f, 1f);
                Color color = Color.Lerp(body, sheen, Mathf.Clamp01(film) * 0.42f) + new Color(glint, glint, glint * 1.1f, 0f);
                color.a = Mathf.Clamp01((0.98f - r) / 0.06f) * 0.94f;
                return color;
            });
        }

        /// <summary>
        /// A mortar shell along +z, 1.15 m (bigger than life, so it reads from above as it flies): an olive body with a
        /// yellow band, a darker ogive nose and a brass fuze, a brass driving band, a tail boom with four fins.
        /// </summary>
        public static LowPolyBuilder MortarShell()
        {
            var m = new LowPolyBuilder();
            const float r = 0.17f;
            m.Color(UV("Olive")).Rod(new Vector3(0f, 0f, -0.2f), new Vector3(0f, 0f, 0.25f), r, 14, true);
            m.Color(UV("OliveDark")).Rod(new Vector3(0f, 0f, 0.25f), new Vector3(0f, 0f, 0.5f), r, 14, true, 0.045f);
            m.Color(UV("Brass")).Rod(new Vector3(0f, 0f, 0.5f), new Vector3(0f, 0f, 0.56f), 0.045f, 8, true, 0.02f);
            m.Color(UV("Brass")).Rod(new Vector3(0f, 0f, 0.17f), new Vector3(0f, 0f, 0.22f), r * 1.05f, 14, true);
            m.Color(UV("Hazard")).Rod(new Vector3(0f, 0f, 0.0f), new Vector3(0f, 0f, 0.06f), r * 1.02f, 14, true);
            m.Color(UV("Olive")).Rod(new Vector3(0f, 0f, -0.36f), new Vector3(0f, 0f, -0.2f), 0.07f, 14, true, r);
            m.Color(UV("Gunmetal")).Rod(new Vector3(0f, 0f, -0.6f), new Vector3(0f, 0f, -0.36f), 0.06f, 8, false);
            for (int i = 0; i < 4; i++)
            {
                m.Push(new Vector3(0f, 0f, -0.52f), Quaternion.Euler(0f, 0f, 45f + i * 90f));
                m.Color(UV("GunDark")).Box(new Vector3(0f, 0.12f, 0f), new Vector3(0.025f, 0.17f, 0.2f));
                m.Pop();
            }
            return m;
        }

        /// <summary>The white-hot core of a plasma bolt along +z (its violet glow is a quad of its own; the race adds the trail).</summary>
        public static LowPolyBuilder PlasmaCore()
        {
            var m = new LowPolyBuilder();
            m.Color(UV("GlowWhite")).Sphere(Vector3.zero, new Vector3(0.26f, 0.26f, 0.56f), 5, 10, true);
            return m;
        }

        // ------------------------------------------------------------------ pickups

        /// <summary>The glowing ring a pickup stands in, in the colour of its kind.</summary>
        public static LowPolyBuilder Ring(string glow)
        {
            var m = new LowPolyBuilder();
            m.Color(UV(glow)).Torus(new Vector3(0f, 0.03f, 0f), 0.95f, 0.055f, 28, 5);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                m.Push(radial * 1.12f + Vector3.up * 0.02f, Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f));
                m.Color(UV(glow)).Box(Vector3.zero, new Vector3(0.16f, 0.03f, 0.08f));
                m.Pop();
            }
            return m;
        }

        /// <summary>The model of a pickup, its base at the origin (it floats above its ring).</summary>
        public static LowPolyBuilder Pickup(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.Cash: return Cash();
                case PickupKind.Repair: return Repair();
                case PickupKind.Ammo: return Ammo();
                default: return Nitro();
            }
        }

        /// <summary>The glow colour (palette name) of a pickup's ring.</summary>
        public static string Glow(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.Cash: return "GlowGreen";
                case PickupKind.Repair: return "GlowRed";
                case PickupKind.Ammo: return "GlowAmber";
                default: return "GlowBlue";
            }
        }

        public static Color GlowColor(PickupKind kind)
        {
            return CarArt.ColorOf(Glow(kind));
        }

        /// <summary>A green crate with a gold dollar sign on top and on the sides, banknotes poking out.</summary>
        private static LowPolyBuilder Cash()
        {
            var m = new LowPolyBuilder();
            var size = new Vector3(0.82f, 0.58f, 0.82f);
            Vector3 center = Vector3.up * (size.y * 0.5f);
            m.Color(UV("CashGreen")).BeveledBox(center, size, 0.05f);
            m.Color(UV("CashDark"));
            Vector3 half = size * 0.5f;
            for (int a = -1; a <= 1; a += 2)
            {
                for (int b = -1; b <= 1; b += 2)
                {
                    m.Box(center + new Vector3(a * (half.x - 0.03f), 0f, b * (half.z - 0.03f)), new Vector3(0.08f, size.y + 0.02f, 0.08f));
                    m.Box(center + new Vector3(0f, a * (half.y - 0.03f), b * (half.z - 0.03f)), new Vector3(size.x + 0.02f, 0.08f, 0.08f));
                    m.Box(center + new Vector3(a * (half.x - 0.03f), b * (half.y - 0.03f), 0f), new Vector3(0.08f, 0.08f, size.z + 0.02f));
                }
            }
            Dollar(m, new Vector3(0f, size.y + 0.012f, 0f), Quaternion.identity, 0.5f);
            for (int side = 0; side < 4; side++)
            {
                Quaternion turn = Quaternion.Euler(0f, side * 90f, 0f);
                Dollar(m, center + turn * new Vector3(0f, 0f, half.z + 0.012f), turn * Quaternion.LookRotation(Vector3.up, Vector3.forward), 0.36f);
            }
            m.Push(new Vector3(0.18f, size.y + 0.02f, -0.24f), Quaternion.Euler(0f, 24f, 8f));
            m.Color(UV("Bill")).Box(Vector3.zero, new Vector3(0.34f, 0.02f, 0.16f));
            m.Color(UV("BillDark")).Box(new Vector3(0f, 0.011f, 0f), new Vector3(0.08f, 0.004f, 0.08f));
            m.Pop();
            return m;
        }

        /// <summary>
        /// A dollar sign lying in the local XZ plane (facing +y) at <paramref name="at"/>: an S of five bars and the
        /// stroke through it, <paramref name="height"/> tall.
        /// </summary>
        private static void Dollar(LowPolyBuilder m, Vector3 at, Quaternion rotation, float height)
        {
            float h = height;
            float w = h * 0.62f;
            float t = h * 0.14f;
            m.Push(at, rotation);
            m.Color(UV("Yellow"));
            const float depth = 0.02f;
            m.Box(new Vector3(0f, 0f, h * 0.5f - t * 0.5f), new Vector3(w, depth, t));
            m.Box(new Vector3(0f, 0f, 0f), new Vector3(w, depth, t));
            m.Box(new Vector3(0f, 0f, -h * 0.5f + t * 0.5f), new Vector3(w, depth, t));
            m.Box(new Vector3(-w * 0.5f + t * 0.5f, 0f, h * 0.25f), new Vector3(t, depth, h * 0.5f));
            m.Box(new Vector3(w * 0.5f - t * 0.5f, 0f, -h * 0.25f), new Vector3(t, depth, h * 0.5f));
            m.Box(new Vector3(w * 0.5f - t * 0.5f, 0f, h * 0.36f), new Vector3(t, depth, h * 0.16f));
            m.Box(new Vector3(-w * 0.5f + t * 0.5f, 0f, -h * 0.36f), new Vector3(t, depth, h * 0.16f));
            m.Color(UV("BrassDark")).Box(new Vector3(0f, -0.004f, 0f), new Vector3(t * 0.55f, depth, h * 1.25f));
            m.Pop();
        }

        /// <summary>A white repair case with red crosses and a big wrench lying across it.</summary>
        private static LowPolyBuilder Repair()
        {
            var m = new LowPolyBuilder();
            var size = new Vector3(0.86f, 0.42f, 0.66f);
            Vector3 center = Vector3.up * (size.y * 0.5f);
            m.Color(UV("MedWhite")).BeveledBox(center, size, 0.06f);
            m.Color(UV("Gray")).Box(center + Vector3.up * 0.02f, new Vector3(size.x + 0.01f, 0.03f, size.z + 0.01f));
            m.Color(UV("CrossRed"));
            float top = size.y + 0.01f;
            m.Box(new Vector3(-0.12f, top, 0f), new Vector3(0.42f, 0.02f, 0.13f));
            m.Box(new Vector3(-0.12f, top, 0f), new Vector3(0.13f, 0.02f, 0.42f));
            for (int side = -1; side <= 1; side += 2)
            {
                m.Box(new Vector3(0f, center.y, side * (size.z * 0.5f + 0.01f)), new Vector3(0.24f, 0.08f, 0.02f));
                m.Box(new Vector3(0f, center.y, side * (size.z * 0.5f + 0.01f)), new Vector3(0.08f, 0.24f, 0.02f));
            }
            m.Color(UV("Charcoal")).BeveledBox(new Vector3(0f, size.y + 0.04f, 0f), new Vector3(0.3f, 0.04f, 0.06f), 0.015f);
            // The wrench, diagonal across the lid.
            m.Push(new Vector3(0.18f, size.y + 0.06f, 0f), Quaternion.Euler(0f, 38f, 0f));
            m.Color(UV("Chrome")).BeveledBox(Vector3.zero, new Vector3(0.1f, 0.04f, 0.72f), 0.015f);
            m.Color(UV("ChromeDark")).BeveledBox(new Vector3(0f, 0.005f, -0.08f), new Vector3(0.06f, 0.04f, 0.36f), 0.01f);
            for (int end = -1; end <= 1; end += 2)
            {
                Vector3 head = new Vector3(0f, 0f, end * 0.38f);
                m.Color(UV("Chrome")).Cylinder(head - Vector3.up * 0.02f, 0.11f, 0.11f, 0.045f, 10);
                // The open jaw.
                m.Color(UV("ChromeDark")).Box(head + new Vector3(0f, 0.0f, end * 0.06f), new Vector3(0.09f, 0.05f, 0.1f));
            }
            m.Pop();
            return m;
        }

        /// <summary>An olive ammunition box with a stencilled stripe and a row of brass rounds standing on it.</summary>
        private static LowPolyBuilder Ammo()
        {
            var m = new LowPolyBuilder();
            var size = new Vector3(0.84f, 0.4f, 0.5f);
            Vector3 center = Vector3.up * (size.y * 0.5f);
            m.Color(UV("Olive")).BeveledBox(center, size, 0.04f);
            m.Color(UV("OliveDark")).Box(center + Vector3.up * (size.y * 0.5f - 0.05f), new Vector3(size.x + 0.02f, 0.04f, size.z + 0.02f));
            m.Color(UV("Hazard")).Box(center + new Vector3(0f, -0.04f, size.z * 0.5f + 0.008f), new Vector3(0.5f, 0.07f, 0.01f));
            m.Color(UV("Hazard")).Box(center + new Vector3(0f, -0.04f, -size.z * 0.5f - 0.008f), new Vector3(0.5f, 0.07f, 0.01f));
            for (int side = -1; side <= 1; side += 2)
            {
                m.Color(UV("OliveDark")).Box(new Vector3(side * (size.x * 0.5f + 0.02f), size.y * 0.6f, 0f), new Vector3(0.03f, 0.06f, 0.22f));
            }
            for (int i = 0; i < 5; i++)
            {
                float x = -0.28f + i * 0.14f;
                Vector3 foot = new Vector3(x, size.y, 0f);
                m.Color(UV("Brass")).Cylinder(foot, 0.05f, 0.05f, 0.24f, 10, true);
                m.Color(UV("BrassDark")).Cylinder(foot, 0.055f, 0.055f, 0.025f, 10);
                m.Color(UV("Copper")).Cylinder(foot + Vector3.up * 0.24f, 0.05f, 0.012f, 0.13f, 10, true);
            }
            return m;
        }

        /// <summary>A blue nitrous bottle lying on its side, with a white label band, a valve and a gauge.</summary>
        private static LowPolyBuilder Nitro()
        {
            var m = new LowPolyBuilder();
            const float r = 0.26f;
            const float length = 0.8f;
            Vector3 axisFrom = new Vector3(-length * 0.5f, r + 0.02f, 0f);
            Vector3 axisTo = new Vector3(length * 0.5f, r + 0.02f, 0f);
            m.Color(UV("NitroBlue")).Rod(axisFrom, axisTo, r, 16, true);
            m.Push(axisFrom, Quaternion.Euler(0f, 0f, 90f));
            m.Color(UV("NitroBlue")).Sphere(Vector3.zero, new Vector3(r, r * 0.55f, r), 6, 16, true, 0.5f);
            m.Pop();
            m.Push(axisTo, Quaternion.Euler(0f, 0f, -90f));
            m.Color(UV("NitroBlue")).Sphere(Vector3.zero, new Vector3(r, r * 0.55f, r), 6, 16, true, 0.5f);
            m.Pop();
            m.Color(UV("White")).Rod(new Vector3(-0.12f, r + 0.02f, 0f), new Vector3(0.14f, r + 0.02f, 0f), r * 1.02f, 16, true);
            m.Color(UV("NitroDeep")).Rod(new Vector3(-0.02f, r + 0.02f, 0f), new Vector3(0.04f, r + 0.02f, 0f), r * 1.03f, 16, true);
            m.Color(UV("NitroLight")).Box(new Vector3(-0.2f, r * 2f + 0.025f, 0f), new Vector3(0.3f, 0.01f, 0.05f));
            m.Color(UV("Chrome")).Rod(axisTo + Vector3.right * (r * 0.5f), axisTo + Vector3.right * (r * 0.5f + 0.12f), 0.05f, 10, false);
            m.Color(UV("Chrome")).Rod(axisTo + new Vector3(r * 0.5f + 0.1f, -0.08f, 0f), axisTo + new Vector3(r * 0.5f + 0.1f, 0.12f, 0f), 0.025f, 8, false);
            m.Color(UV("RocketNose")).Cylinder(axisTo + new Vector3(r * 0.5f + 0.1f, 0.12f, 0f), 0.06f, 0.06f, 0.03f, 10);
            m.Color(UV("White")).Cylinder(new Vector3(0.24f, r * 2f + 0.015f, 0f), 0.07f, 0.07f, 0.02f, 12);
            m.Color(UV("Charcoal")).Box(new Vector3(0.24f, r * 2f + 0.037f, 0.015f), new Vector3(0.012f, 0.005f, 0.05f));
            return m;
        }
    }
}
