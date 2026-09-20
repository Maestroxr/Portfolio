using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Portfolio.Monopoly.EditorTools
{
    /// <summary>
    /// The 3D models of the game, built from <see cref="MeshFactory"/> shapes: the eight pewter tokens (cut-out side
    /// views extruded with rounded edges, or turned and modelled from ellipsoids), their bases and turn ring, houses,
    /// hotels, the dice and the pieces of the board. Sizes are in board units (a space is one unit wide).
    /// </summary>
    internal static class MonopolyModels
    {
        /// <summary>Height of the coloured base the figures stand on.</summary>
        public const float BaseHeight = 0.06f;

        public static Mesh Token(int index)
        {
            MeshBuilder figure;
            switch (index)
            {
                case 0: figure = RaceCar(); break;
                case 1: figure = TopHat(); break;
                case 2: figure = Extruded(TokenOutlines.Dog, 0.0049f, 0.17f); break;
                case 3: figure = Battleship(); break;
                case 4: figure = Extruded(TokenOutlines.Cat, 0.0045f, 0.16f); break;
                case 5: figure = Duck(); break;
                case 6: figure = Penguin(); break;
                default: figure = Extruded(TokenOutlines.Dino, 0.0049f, 0.16f); break;
            }
            // Stand the figure on the base, centred over it.
            Bounds bounds = figure.Bounds();
            var placed = new MeshBuilder().Append(figure, Matrix4x4.Translate(new Vector3(-bounds.center.x, BaseHeight - bounds.min.y, -bounds.center.z)));
            return placed.ToMesh(MonopolyStyle.TokenNames[index]);
        }

        /// <summary>A side view cut out and extruded: <paramref name="scale"/> board units per design unit, <paramref name="depth"/> thick.</summary>
        private static MeshBuilder Extruded((float x, float y, bool sharp)[] anchors, float scale, float depth)
        {
            List<Vector2> outline = TokenOutlines.Outline(anchors).Select(p => p * scale).ToList();
            return MeshFactory.Extrude(outline, depth, depth * 0.16f, 3);
        }

        private static MeshBuilder RaceCar()
        {
            const float scale = 0.0058f;
            List<Vector2> body = TokenOutlines.Outline(TokenOutlines.CarBody).Select(p => p * scale).ToList();
            var car = new MeshBuilder();
            car.Append(MeshFactory.Extrude(body, 0.2f, 0.035f, 3));
            float wheelRadius = 13.5f * scale;
            MeshBuilder wheel = MeshFactory.Cylinder(wheelRadius, 0.055f, 0.015f, 20);
            foreach (float x in new[] { 22f, 88f })
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    // Cylinders stand on y: turn them onto their side, axle across the car.
                    var position = new Vector3(x * scale, wheelRadius, side * 0.1f + (side < 0f ? -0.055f : 0f));
                    car.Append(wheel, Matrix4x4.TRS(position, Quaternion.Euler(90f, 0f, 0f), Vector3.one));
                }
            }
            // The driver's head behind the windscreen and a steering column.
            car.Append(MeshFactory.Ellipsoid(new Vector3(0.042f, 0.046f, 0.042f), 16, 12), Matrix4x4.Translate(new Vector3(40f * scale, 43f * scale, 0f)));
            // Exhaust pipes along the side.
            MeshBuilder pipe = MeshFactory.Cylinder(0.012f, 0.26f, 0.004f, 10);
            car.Append(pipe, Matrix4x4.TRS(new Vector3(0.1f, 17f * scale, 0.115f), Quaternion.Euler(0f, 0f, -90f), Vector3.one));
            return car;
        }

        private static MeshBuilder TopHat()
        {
            var profile = new List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(0.24f, 0f), new Vector2(0.262f, 0.008f), new Vector2(0.27f, 0.022f),
                new Vector2(0.262f, 0.034f), new Vector2(0.24f, 0.036f), new Vector2(0.172f, 0.038f), new Vector2(0.17f, 0.045f),
                new Vector2(0.176f, 0.047f), new Vector2(0.178f, 0.11f), new Vector2(0.172f, 0.113f), new Vector2(0.176f, 0.2f),
                new Vector2(0.184f, 0.31f), new Vector2(0.186f, 0.34f), new Vector2(0.178f, 0.352f), new Vector2(0.15f, 0.358f),
                new Vector2(0f, 0.36f)
            };
            return MeshFactory.Lathe(profile, 40);
        }

        private static MeshBuilder Battleship()
        {
            var ship = new MeshBuilder();
            // The hull: a top view with a pointed bow, extruded upwards and turned so its depth is the height.
            var deck = new List<Vector2>
            {
                new Vector2(0.36f, 0f), new Vector2(0.28f, 0.055f), new Vector2(0.12f, 0.085f), new Vector2(-0.18f, 0.085f),
                new Vector2(-0.3f, 0.07f), new Vector2(-0.34f, 0.035f), new Vector2(-0.345f, 0f), new Vector2(-0.34f, -0.035f),
                new Vector2(-0.3f, -0.07f), new Vector2(-0.18f, -0.085f), new Vector2(0.12f, -0.085f), new Vector2(0.28f, -0.055f)
            };
            MeshBuilder hull = MeshFactory.Extrude(deck, 0.1f, 0.018f, 2);
            ship.Append(hull, Matrix4x4.TRS(new Vector3(0f, 0.05f, 0f), Quaternion.Euler(-90f, 0f, 0f), Vector3.one));
            // Superstructure, bridge, funnel and mast.
            ship.Append(MeshFactory.Box(new Vector3(-0.02f, 0.13f, 0f), new Vector3(0.26f, 0.06f, 0.11f)));
            ship.Append(MeshFactory.Box(new Vector3(0.03f, 0.19f, 0f), new Vector3(0.1f, 0.07f, 0.08f)));
            ship.Append(MeshFactory.Cylinder(0.034f, 0.12f, 0.008f, 16), Matrix4x4.Translate(new Vector3(-0.08f, 0.14f, 0f)));
            ship.Append(MeshFactory.Cylinder(0.007f, 0.2f, 0.002f, 8), Matrix4x4.Translate(new Vector3(0.04f, 0.2f, 0f)));
            // Turrets with their barrels, fore and aft.
            MeshBuilder turret = MeshFactory.Cylinder(0.045f, 0.035f, 0.01f, 18);
            MeshBuilder barrel = MeshFactory.Cylinder(0.009f, 0.12f, 0.003f, 8);
            foreach (float x in new[] { 0.19f, -0.22f })
            {
                float direction = x > 0f ? 1f : -1f;
                ship.Append(turret, Matrix4x4.Translate(new Vector3(x, 0.1f, 0f)));
                foreach (float z in new[] { -0.018f, 0.018f })
                {
                    ship.Append(barrel, Matrix4x4.TRS(new Vector3(x, 0.12f, z), Quaternion.Euler(0f, 0f, direction > 0f ? -84f : 84f), Vector3.one));
                }
            }
            return ship;
        }

        private static MeshBuilder Duck()
        {
            var duck = new MeshBuilder();
            duck.Append(MeshFactory.Ellipsoid(new Vector3(0.2f, 0.13f, 0.15f), 28, 18), Matrix4x4.Translate(new Vector3(0f, 0.13f, 0f)));
            duck.Append(MeshFactory.Ellipsoid(new Vector3(0.105f, 0.105f, 0.1f), 24, 16), Matrix4x4.Translate(new Vector3(0.1f, 0.31f, 0f)));
            duck.Append(MeshFactory.Ellipsoid(new Vector3(0.07f, 0.026f, 0.058f), 16, 10), Matrix4x4.TRS(new Vector3(0.21f, 0.295f, 0f), Quaternion.Euler(0f, 0f, -8f), Vector3.one));
            duck.Append(MeshFactory.Cone(0.06f, 0.11f, 16), Matrix4x4.TRS(new Vector3(-0.17f, 0.17f, 0f), Quaternion.Euler(0f, 0f, 38f), new Vector3(1f, 1f, 0.8f)));
            // The eyes, small bumps on either side of the head.
            foreach (float z in new[] { -0.075f, 0.075f })
            {
                duck.Append(MeshFactory.Ellipsoid(new Vector3(0.018f, 0.018f, 0.012f), 10, 8), Matrix4x4.Translate(new Vector3(0.15f, 0.34f, z)));
            }
            return duck;
        }

        private static MeshBuilder Penguin()
        {
            var penguin = new MeshBuilder();
            penguin.Append(MeshFactory.Ellipsoid(new Vector3(0.135f, 0.2f, 0.13f), 28, 18), Matrix4x4.Translate(new Vector3(0f, 0.21f, 0f)));
            penguin.Append(MeshFactory.Ellipsoid(new Vector3(0.1f, 0.095f, 0.095f), 24, 16), Matrix4x4.Translate(new Vector3(0.015f, 0.41f, 0f)));
            penguin.Append(MeshFactory.Cone(0.024f, 0.075f, 12), Matrix4x4.TRS(new Vector3(0.09f, 0.405f, 0f), Quaternion.Euler(0f, 0f, -90f), Vector3.one));
            foreach (float z in new[] { -1f, 1f })
            {
                penguin.Append(MeshFactory.Ellipsoid(new Vector3(0.045f, 0.13f, 0.02f), 14, 12), Matrix4x4.TRS(new Vector3(-0.01f, 0.23f, z * 0.13f), Quaternion.Euler(z * 14f, 0f, 8f), Vector3.one));
                penguin.Append(MeshFactory.Ellipsoid(new Vector3(0.06f, 0.02f, 0.035f), 12, 8), Matrix4x4.Translate(new Vector3(0.06f, 0.015f, z * 0.06f)));
                penguin.Append(MeshFactory.Ellipsoid(new Vector3(0.014f, 0.016f, 0.01f), 8, 6), Matrix4x4.Translate(new Vector3(0.075f, 0.44f, z * 0.06f)));
            }
            return penguin;
        }

        // ------------------------------------------------------------------ pieces

        public static Mesh TokenBase()
        {
            return MeshFactory.Cylinder(0.25f, BaseHeight, 0.018f, 36).ToMesh("TokenBase");
        }

        public static Mesh TurnRing()
        {
            return MeshFactory.Torus(0.33f, 0.02f, 48, 10).ToMesh("TurnRing");
        }

        public static Mesh House()
        {
            var house = new MeshBuilder();
            house.Append(MeshFactory.Box(new Vector3(0f, 0.065f, 0f), new Vector3(0.18f, 0.13f, 0.15f)));
            house.Append(MeshFactory.Roof(0.18f, 0.15f, 0.085f, 0.012f), Matrix4x4.Translate(new Vector3(0f, 0.13f, 0f)));
            house.Append(MeshFactory.Box(new Vector3(0.045f, 0.18f, 0.03f), new Vector3(0.028f, 0.06f, 0.028f)));
            return house.ToMesh("House");
        }

        public static Mesh Hotel()
        {
            var hotel = new MeshBuilder();
            hotel.Append(MeshFactory.Box(new Vector3(0f, 0.09f, 0f), new Vector3(0.42f, 0.18f, 0.2f)));
            hotel.Append(MeshFactory.Roof(0.42f, 0.2f, 0.1f, 0.012f), Matrix4x4.Translate(new Vector3(0f, 0.18f, 0f)));
            return hotel.ToMesh("Hotel");
        }

        public static Mesh Die()
        {
            return MeshFactory.RoundedCube(0.46f, 0.075f, 10).ToMesh("Die");
        }

        /// <summary>The body of the board under the printed surface: a slab slightly larger than the board.</summary>
        public static Mesh BoardSlab(float side, float thickness, float margin)
        {
            var slab = new MeshBuilder();
            float size = side + margin * 2f;
            slab.Append(MeshFactory.Box(new Vector3(0f, thickness * 0.5f, 0f), new Vector3(size, thickness, size)));
            return slab.ToMesh("BoardSlab");
        }

        public static Mesh BoardTop(float side)
        {
            return MeshFactory.Plane(side, side, new Rect(0f, 0f, 1f, 1f)).ToMesh("BoardTop");
        }

        public static Mesh Table(float size, float tiles)
        {
            return MeshFactory.Plane(size, size, new Rect(0f, 0f, tiles, tiles)).ToMesh("Table");
        }

        /// <summary>A quad lying flat, one unit square (scaled per use: highlights, stamps, shadows, the logo).</summary>
        public static Mesh Quad()
        {
            return MeshFactory.Plane(1f, 1f, new Rect(0f, 0f, 1f, 1f)).ToMesh("FlatQuad");
        }

        /// <summary>A stack of cards: the white block and, separately, the printed top.</summary>
        public static Mesh CardStack(float width, float depth, float height)
        {
            var stack = new MeshBuilder();
            stack.Append(MeshFactory.Box(new Vector3(0f, height * 0.5f, 0f), new Vector3(width, height, depth)));
            return stack.ToMesh("CardStack");
        }

        /// <summary>The owner tag: a thin coloured plate.</summary>
        public static Mesh OwnerTag(float width, float depth)
        {
            var tag = new MeshBuilder();
            tag.Append(MeshFactory.Box(new Vector3(0f, 0.012f, 0f), new Vector3(width, 0.024f, depth)));
            return tag.ToMesh("OwnerTag");
        }
    }
}
