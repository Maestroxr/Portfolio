using Gamebox.Editor;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// The city: buildings of several heights whose windows light at night, parked cars, cones, barricades, a bus stop,
    /// street trees, a kiosk, a water tower and billboards; an office tower, an apartment block and a neon hotel.
    /// </summary>
    internal static partial class PropMeshes
    {
        private static readonly string[] CarPaints = { "CarRed", "CarBlue", "CarWhite", "CarBlack", "CarYellow", "CarGreen", "CarSilver" };
        private static readonly string[] CityWindows = { "WindowLit", "WindowWarm", "WindowCool" };

        public static LowPolyBuilder ParkedCar(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            Car(m, CarPaints[random.Next(CarPaints.Length)], false, false, seed);
            return m;
        }

        private static void Cone(LowPolyBuilder m, Vector3 foot, bool fallen, Vector3 direction)
        {
            if (fallen)
            {
                m.Push(foot + Vector3.up * 0.18f, Quaternion.FromToRotation(Vector3.up, direction));
            }
            else
            {
                m.Push(foot, Quaternion.identity);
            }
            m.Color(P("Black")).Box(new Vector3(0f, 0.02f, 0f), new Vector3(0.5f, 0.04f, 0.5f));
            m.Color(P("ConeOrange")).Cylinder(new Vector3(0f, 0.04f, 0f), 0.2f, 0.03f, 0.72f, 8);
            m.Color(P("White")).Cylinder(new Vector3(0f, 0.32f, 0f), 0.135f, 0.11f, 0.12f, 8);
            m.Pop();
        }

        public static LowPolyBuilder TrafficCones(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            for (int i = 0; i < 5; i++)
            {
                Cone(m, new Vector3(-1.6f + i * 0.8f, 0f, Rand(random, -0.15f, 0.15f)), false, Vector3.up);
            }
            Cone(m, new Vector3(0.4f, 0f, 1f), true, new Vector3(0.7f, 0f, 0.7f));
            return m;
        }

        /// <summary>A red and white police barricade on two trestles, a lamp blinking on it.</summary>
        public static LowPolyBuilder Barricade(int seed)
        {
            var m = new LowPolyBuilder();
            foreach (float x in new[] { -1.3f, 1.3f })
            {
                m.Color(P("SteelDark")).Rod(new Vector3(x, 0f, -0.35f), new Vector3(x, 1.05f, 0f), 0.04f, 4, false);
                m.Color(P("SteelDark")).Rod(new Vector3(x, 0f, 0.35f), new Vector3(x, 1.05f, 0f), 0.04f, 4, false);
            }
            for (int i = 0; i < 6; i++)
            {
                float x = -1.4f + i * 0.47f + 0.235f;
                m.Color(P(i % 2 == 0 ? "Red" : "White")).Box(new Vector3(x, 0.95f, 0f), new Vector3(0.47f, 0.24f, 0.06f));
                m.Color(P(i % 2 == 0 ? "White" : "Red")).Box(new Vector3(x, 0.55f, 0f), new Vector3(0.47f, 0.18f, 0.06f));
            }
            m.Color(P("NeonYellow")).Box(new Vector3(1.2f, 1.17f, 0f), new Vector3(0.14f, 0.16f, 0.1f));
            return m;
        }

        /// <summary>A bus shelter: glass walls, a bench and a lit advert at its end.</summary>
        public static LowPolyBuilder BusStop(int seed)
        {
            var m = new LowPolyBuilder();
            m.Color(P("Concrete")).Box(new Vector3(0f, 0.08f, 0f), new Vector3(4.4f, 0.16f, 2f));
            foreach (float x in new[] { -1.9f, 1.9f })
            {
                m.Color(P("SteelDark")).Box(new Vector3(x, 1.35f, -0.7f), new Vector3(0.1f, 2.4f, 0.1f));
                m.Color(P("SteelDark")).Box(new Vector3(x, 1.35f, 0.6f), new Vector3(0.1f, 2.4f, 0.1f));
            }
            m.Color(P("SteelDark")).Box(new Vector3(0f, 2.6f, -0.05f), new Vector3(4.2f, 0.12f, 1.8f));
            m.Color(P("Glass")).Box(new Vector3(0f, 1.3f, -0.72f), new Vector3(3.7f, 2f, 0.04f));
            m.Color(P("Glass")).Box(new Vector3(-1.9f, 1.3f, 0f), new Vector3(0.04f, 2f, 1.2f));
            m.Color(P("SteelDark")).Box(new Vector3(1.9f, 1.3f, -0.05f), new Vector3(0.14f, 2.2f, 1.3f));
            m.Color(P("WindowCool")).Box(new Vector3(1.98f, 1.35f, -0.05f), new Vector3(0.03f, 1.8f, 1.1f));
            m.Color(P("NeonPink")).Box(new Vector3(1.99f, 1.9f, -0.05f), new Vector3(0.03f, 0.3f, 0.9f));
            m.Color(P("Wood")).Box(new Vector3(-0.3f, 0.6f, -0.45f), new Vector3(2.6f, 0.08f, 0.4f));
            return m;
        }

        /// <summary>A street tree in a square planter.</summary>
        public static LowPolyBuilder StreetTree(int seed)
        {
            var m = new LowPolyBuilder();
            m.Color(P("ConcreteDark")).Box(new Vector3(0f, 0.25f, 0f), new Vector3(1.6f, 0.5f, 1.6f));
            m.Color(P("WoodDark")).Box(new Vector3(0f, 0.48f, 0f), new Vector3(1.4f, 0.06f, 1.4f));
            m.Color(P("Bark")).Cylinder(new Vector3(0f, 0.5f, 0f), 0.16f, 0.12f, 2.6f, 6);
            m.Color(P("Leaf")).Blob(new Vector3(0f, 3.6f, 0f), new Vector3(1.8f, 1.4f, 1.8f), 1, 0.2f, seed);
            m.Color(P("LeafDark")).Blob(new Vector3(0.8f, 3.1f, 0.4f), new Vector3(1.1f, 0.9f, 1.1f), 1, 0.2f, seed + 1);
            m.Color(P("Leaf")).Blob(new Vector3(-0.6f, 4.3f, -0.3f), new Vector3(1f, 0.8f, 1f), 1, 0.2f, seed + 2);
            return m;
        }

        /// <summary>A news kiosk with an awning and a glowing sign.</summary>
        public static LowPolyBuilder Kiosk(int seed)
        {
            var m = new LowPolyBuilder();
            m.Color(P("GreenDark")).BeveledBox(new Vector3(0f, 1.2f, 0f), new Vector3(2.4f, 2.4f, 1.8f), 0.06f);
            m.Color(P("WindowWarm")).Box(new Vector3(0f, 1.3f, 0.92f), new Vector3(1.9f, 0.9f, 0.04f));
            m.Color(P("Cream")).Box(new Vector3(0f, 0.6f, 1.05f), new Vector3(2f, 0.5f, 0.3f));
            m.Push(new Vector3(0f, 2.15f, 1.3f), Quaternion.Euler(-25f, 0f, 0f));
            for (int i = 0; i < 6; i++)
            {
                m.Color(P(i % 2 == 0 ? "Red" : "White")).Box(new Vector3(-1.1f + i * 0.44f, 0f, 0f), new Vector3(0.44f, 0.04f, 0.9f));
            }
            m.Pop();
            m.Color(P("Charcoal")).Box(new Vector3(0f, 2.62f, 0f), new Vector3(2.6f, 0.16f, 2f));
            m.Color(P("NeonCyan")).Box(new Vector3(0f, 2.82f, 0f), new Vector3(1.8f, 0.24f, 0.1f));
            return m;
        }

        /// <summary>A two storey corner shop with a lit shop front and a neon sign; 8 x 8 m, 7.5 m tall.</summary>
        public static LowPolyBuilder Shop(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            string[] walls = { "BuildingBrick", "Building", "Cream", "BuildingLight" };
            string wall = walls[random.Next(walls.Length)];
            WindowedBlock(m, Vector3.zero, new Vector3(8f, 7f, 8f), wall, seed, CityWindows, 0.5f);
            m.Color(P("RoofDark")).Box(new Vector3(0f, 7.1f, 0f), new Vector3(8.3f, 0.2f, 8.3f));
            m.Color(P("Roof")).Box(new Vector3(1.6f, 7.6f, -1.4f), new Vector3(2f, 0.8f, 1.6f));
            m.Color(P("SteelDark")).Box(new Vector3(-2.2f, 7.5f, 1.8f), new Vector3(1.2f, 0.6f, 1.2f));
            m.Color(P("WindowWarm")).Box(new Vector3(0f, 1.4f, -4.03f), new Vector3(6.6f, 2.2f, 0.06f));
            m.Color(P("Charcoal")).Box(new Vector3(0f, 2.75f, -4.1f), new Vector3(7.2f, 0.5f, 0.2f));
            string[] neon = { "NeonPink", "NeonCyan", "NeonYellow", "NeonGreen", "NeonRed" };
            m.Color(P(neon[random.Next(neon.Length)])).Box(new Vector3(0f, 2.75f, -4.22f), new Vector3(5f, 0.3f, 0.04f));
            m.Push(new Vector3(0f, 2.55f, -4.6f), Quaternion.Euler(-20f, 0f, 0f));
            m.Color(P(random.NextDouble() < 0.5 ? "Red" : "Blue")).Box(Vector3.zero, new Vector3(6.8f, 0.06f, 1f));
            m.Pop();
            return m;
        }

        /// <summary>A narrow three storey town house with a stoop and roof boxes; 6 x 10 m, 10 m tall.</summary>
        public static LowPolyBuilder Townhouse(int seed)
        {
            var m = new LowPolyBuilder();
            var random = new System.Random(seed);
            string[] walls = { "BuildingBrick", "Brick", "Building", "BuildingDark" };
            WindowedBlock(m, Vector3.zero, new Vector3(6f, 10f, 10f), walls[random.Next(walls.Length)], seed, CityWindows, 0.55f);
            m.Color(P("ConcreteLight")).Box(new Vector3(0f, 10.15f, 0f), new Vector3(6.3f, 0.3f, 10.3f));
            m.Color(P("Roof")).Box(new Vector3(0f, 10.3f, 0f), new Vector3(5.6f, 0.1f, 9.6f));
            m.Color(P("SteelDark")).Box(new Vector3(1.2f, 10.8f, 2f), new Vector3(1.4f, 0.9f, 1.4f));
            m.Color(P("Steel")).Cylinder(new Vector3(-1.5f, 10.3f, -3f), 0.35f, 0.35f, 1.2f, 8);
            m.Color(P("ConcreteLight")).Box(new Vector3(0f, 0.5f, -5.6f), new Vector3(2.4f, 1f, 1.2f));
            m.Color(P("SteelDark")).Box(new Vector3(-2.6f, 5f, -5.06f), new Vector3(0.1f, 9.4f, 0.1f));
            return m;
        }

        /// <summary>A wooden water tank on a steel stand with a conical roof, 10 m tall.</summary>
        public static LowPolyBuilder WaterTower(int seed)
        {
            var m = new LowPolyBuilder();
            AddWaterTower(m, Vector3.zero, 1f);
            return m;
        }

        private static void AddWaterTower(LowPolyBuilder m, Vector3 foot, float scale)
        {
            m.Push(foot, Quaternion.identity, Vector3.one * scale);
            foreach (Vector2 leg in new[] { new Vector2(1.4f, 1.4f), new Vector2(-1.4f, 1.4f), new Vector2(1.4f, -1.4f), new Vector2(-1.4f, -1.4f) })
            {
                m.Color(P("SteelDark")).Rod(new Vector3(leg.x * 1.15f, 0f, leg.y * 1.15f), new Vector3(leg.x, 5.2f, leg.y), 0.12f, 5, false);
            }
            m.Color(P("SteelDark")).Box(new Vector3(0f, 2.6f, 0f), new Vector3(3.4f, 0.1f, 0.1f));
            m.Color(P("SteelDark")).Box(new Vector3(0f, 2.6f, 0f), new Vector3(0.1f, 0.1f, 3.4f));
            m.Color(P("SteelDark")).Cylinder(new Vector3(0f, 5.1f, 0f), 2.3f, 2.3f, 0.2f, 12);
            m.Color(P("WaterTower")).Cylinder(new Vector3(0f, 5.3f, 0f), 2f, 2f, 3.2f, 12);
            for (int i = 0; i < 3; i++)
            {
                m.Color(P("SteelDark")).Cylinder(new Vector3(0f, 5.8f + i * 1f, 0f), 2.04f, 2.04f, 0.08f, 12);
            }
            m.Color(P("WaterTowerDark")).Cylinder(new Vector3(0f, 8.5f, 0f), 2.15f, 0.15f, 1.6f, 12);
            m.Pop();
        }

        public static LowPolyBuilder CityBillboard(int seed)
        {
            var m = new LowPolyBuilder();
            Billboard(m, new[] { "White", "NeonPink", "BlueDark", "NeonCyan" }, seed, false);
            return m;
        }

        // ------------------------------------------------------------------------------------------------
        // Landmarks
        // ------------------------------------------------------------------------------------------------

        /// <summary>A glass office tower with a setback and a mast: 12 x 12 m, 26 m tall.</summary>
        public static LowPolyBuilder OfficeTower(int seed)
        {
            var m = new LowPolyBuilder();
            WindowedBlock(m, Vector3.zero, new Vector3(12f, 4.5f, 12f), "BuildingDark", seed, CityWindows, 0.9f);
            WindowedBlock(m, new Vector3(0f, 4.5f, 0f), new Vector3(11f, 15f, 11f), "BuildingBlue", seed + 1, CityWindows, 0.5f);
            m.Color(P("BuildingDark")).Box(new Vector3(0f, 19.6f, 0f), new Vector3(11.4f, 0.3f, 11.4f));
            WindowedBlock(m, new Vector3(0f, 19.7f, 0f), new Vector3(8f, 5.5f, 8f), "BuildingTeal", seed + 2, CityWindows, 0.6f);
            m.Color(P("RoofDark")).Box(new Vector3(0f, 25.3f, 0f), new Vector3(8.3f, 0.3f, 8.3f));
            m.Color(P("SteelDark")).Box(new Vector3(-2f, 26.1f, 1.5f), new Vector3(2.5f, 1.3f, 2f));
            m.Color(P("Steel")).Cylinder(new Vector3(1.8f, 25.4f, -1.8f), 0.12f, 0.05f, 6f, 5);
            m.Color(P("Beacon")).Sphere(new Vector3(1.8f, 31.5f, -1.8f), 0.25f, 3, 6, false);
            m.Color(P("NeonCyan")).Box(new Vector3(0f, 19.4f, -5.75f), new Vector3(9f, 0.25f, 0.06f));
            return m;
        }

        /// <summary>A brick apartment block with fire escapes and a water tower on its roof: 16 x 10 m, 15 m tall.</summary>
        public static LowPolyBuilder Apartments(int seed)
        {
            var m = new LowPolyBuilder();
            WindowedBlock(m, Vector3.zero, new Vector3(16f, 15f, 10f), "BuildingBrick", seed, CityWindows, 0.6f);
            m.Color(P("ConcreteLight")).Box(new Vector3(0f, 15.2f, 0f), new Vector3(16.4f, 0.4f, 10.4f));
            m.Color(P("Roof")).Box(new Vector3(0f, 15.42f, 0f), new Vector3(15.6f, 0.1f, 9.6f));
            for (int floor = 0; floor < 4; floor++)
            {
                float y = 3.6f + floor * 3f;
                m.Color(P("SteelDark")).Box(new Vector3(-3f, y, -5.5f), new Vector3(3.2f, 0.08f, 1f));
                m.Color(P("SteelDark")).Box(new Vector3(-3f, y + 0.5f, -6f), new Vector3(3.2f, 0.06f, 0.06f));
                m.Color(P("SteelDark")).Rod(new Vector3(-4.4f, y, -5.6f), new Vector3(-1.6f, y + 3f, -5.6f), 0.05f, 4, false);
            }
            AddWaterTower(m, new Vector3(4f, 15.45f, 1f), 0.8f);
            m.Color(P("SteelDark")).Box(new Vector3(-4.5f, 16f, 2f), new Vector3(2f, 1.1f, 1.6f));
            return m;
        }

        /// <summary>A hotel with a tall vertical neon sign and a sign on its roof: 14 x 9 m, 13 m tall.</summary>
        public static LowPolyBuilder NeonHotel(int seed)
        {
            var m = new LowPolyBuilder();
            WindowedBlock(m, Vector3.zero, new Vector3(14f, 12.5f, 9f), "Cream", seed, CityWindows, 0.7f);
            m.Color(P("RedDark")).Box(new Vector3(0f, 12.7f, 0f), new Vector3(14.4f, 0.4f, 9.4f));
            m.Color(P("WindowWarm")).Box(new Vector3(0f, 1.5f, -4.53f), new Vector3(5f, 2.4f, 0.06f));
            m.Color(P("RedDark")).Box(new Vector3(0f, 3.1f, -5.2f), new Vector3(6f, 0.2f, 1.6f));
            // The vertical sign at the corner.
            m.Color(P("Charcoal")).Box(new Vector3(-7.4f, 7f, -4f), new Vector3(0.5f, 9f, 1.6f));
            for (int i = 0; i < 5; i++)
            {
                m.Color(P(i % 2 == 0 ? "NeonPink" : "NeonCyan")).Box(new Vector3(-7.68f, 3.6f + i * 1.65f, -4f), new Vector3(0.06f, 1.2f, 1.2f));
            }
            m.Color(P("NeonPink")).Box(new Vector3(-7.4f, 11.6f, -4f), new Vector3(0.56f, 0.12f, 1.66f));
            // The roof sign on its frame.
            m.Color(P("SteelDark")).Box(new Vector3(0f, 13.6f, 2f), new Vector3(9f, 1.4f, 0.12f));
            for (int i = 0; i < 5; i++)
            {
                m.Color(P(i == 2 ? "NeonYellow" : "NeonRed")).Box(new Vector3(-3.6f + i * 1.8f, 14.9f, 2f), new Vector3(1.3f, 1.2f, 0.2f));
            }
            return m;
        }

        /// <summary>A steel street lamp: a pole 8 m tall whose arm curves out along +Z to the head.</summary>
        public static LowPolyBuilder StreetLamp(int seed, out Vector3 head)
        {
            var m = new LowPolyBuilder();
            m.Color(P("LampPole")).Cylinder(Vector3.zero, 0.22f, 0.2f, 0.8f, 8);
            m.Color(P("LampPole")).Cylinder(new Vector3(0f, 0.8f, 0f), 0.12f, 0.09f, 7.2f, 8);
            var arc = new[] { new Vector3(0f, 7.8f, 0f), new Vector3(0f, 8.3f, 0.4f), new Vector3(0f, 8.45f, 1.1f), new Vector3(0f, 8.4f, 2f), new Vector3(0f, 8.25f, 2.7f) };
            m.Color(P("LampPole"));
            m.Tube(arc, 0.07f, 6);
            m.Color(P("LampPole")).BeveledBox(new Vector3(0f, 8.2f, 2.9f), new Vector3(0.5f, 0.2f, 0.9f), 0.06f);
            m.Color(P("LampHead")).Box(new Vector3(0f, 8.09f, 2.92f), new Vector3(0.42f, 0.05f, 0.76f));
            head = new Vector3(0f, 8.05f, 2.92f);
            return m;
        }
    }
}
