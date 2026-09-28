using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Portfolio.Monopoly.EditorTools
{
    /// <summary>
    /// The 3D models of the game, built from <see cref="MeshFactory"/> shapes: the eight tokens (cut-out side views
    /// extruded with rounded edges, or turned and modelled from ellipsoids; spacecraft and planets in the galactic
    /// look), their bases and turn ring, houses and hotels (domes and towers in the galactic look), the dice and the
    /// pieces of the board. Sizes are in board units (a space is one unit wide).
    /// </summary>
    internal static class MonopolyModels
    {
        /// <summary>Height of the coloured base the figures stand on.</summary>
        public const float BaseHeight = 0.06f;

        public static Mesh Token(MonopolyThemeSpec spec, int index)
        {
            MeshBuilder figure = spec.Style == ArtStyle.Galactic ? SpaceFigure(index) : ClassicFigure(index);
            // Stand the figure on the base, centred over it.
            Bounds bounds = figure.Bounds();
            var placed = new MeshBuilder().Append(figure, Matrix4x4.Translate(new Vector3(-bounds.center.x, BaseHeight - bounds.min.y, -bounds.center.z)));
            return placed.ToMesh(spec.Tokens[index].name);
        }

        private static MeshBuilder ClassicFigure(int index)
        {
            switch (index)
            {
                case 0: return RaceCar();
                case 1: return TopHat();
                case 2: return Extruded(TokenOutlines.Dog, 0.0049f, 0.17f);
                case 3: return Battleship();
                case 4: return Extruded(TokenOutlines.Cat, 0.0045f, 0.16f);
                case 5: return Duck();
                case 6: return Penguin();
                default: return Extruded(TokenOutlines.Dino, 0.0049f, 0.16f);
            }
        }

        private static MeshBuilder SpaceFigure(int index)
        {
            switch (index)
            {
                case 0: return Rocket();
                case 1: return Satellite();
                case 2: return Robot();
                case 3: return Ufo();
                case 4: return Comet();
                case 5: return RingedPlanet();
                case 6: return Helmet();
                default: return SpaceStation();
            }
        }

        /// <summary>A side view cut out and extruded: <paramref name="scale"/> board units per design unit, <paramref name="depth"/> thick.</summary>
        private static MeshBuilder Extruded((float x, float y, bool sharp)[] anchors, float scale, float depth)
        {
            List<Vector2> outline = TokenOutlines.Outline(anchors).Select(p => p * scale).ToList();
            return MeshFactory.Extrude(outline, depth, depth * 0.16f, 3);
        }

        // ------------------------------------------------------------------ the classic figures

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

        // ------------------------------------------------------------------ the spacecraft of the galactic look

        /// <summary>A rocket standing on three fins: a turned body with a nose cone, fins, a porthole and a nozzle.</summary>
        private static MeshBuilder Rocket()
        {
            var rocket = new MeshBuilder();
            var profile = new List<Vector2>
            {
                new Vector2(0f, 0.04f), new Vector2(0.05f, 0.04f), new Vector2(0.06f, 0.07f), new Vector2(0.085f, 0.09f),
                new Vector2(0.095f, 0.16f), new Vector2(0.095f, 0.26f), new Vector2(0.08f, 0.32f), new Vector2(0.045f, 0.37f),
                new Vector2(0.012f, 0.4f), new Vector2(0f, 0.41f)
            };
            rocket.Append(MeshFactory.Lathe(profile, 28));
            // The nozzle.
            rocket.Append(MeshFactory.Cylinder(0.045f, 0.05f, 0.008f, 16), Matrix4x4.Translate(new Vector3(0f, 0f, 0f)));
            // Three fins around the foot.
            var fin = new List<Vector2> { new Vector2(0.06f, 0.02f), new Vector2(0.15f, 0f), new Vector2(0.15f, 0.03f), new Vector2(0.09f, 0.14f), new Vector2(0.075f, 0.16f) };
            MeshBuilder finMesh = MeshFactory.Extrude(fin, 0.016f, 0.004f, 2);
            for (int i = 0; i < 3; i++)
            {
                rocket.Append(finMesh, Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 120f * i, 0f), Vector3.one));
            }
            // A porthole and a band.
            rocket.Append(MeshFactory.Torus(0.03f, 0.008f, 16, 8), Matrix4x4.TRS(new Vector3(0f, 0.25f, -0.09f), Quaternion.Euler(90f, 0f, 0f), Vector3.one));
            rocket.Append(MeshFactory.Torus(0.094f, 0.006f, 28, 6), Matrix4x4.Translate(new Vector3(0f, 0.15f, 0f)));
            return rocket;
        }

        /// <summary>A satellite on a thin stand: a body with two solar panels, a dish and an antenna.</summary>
        private static MeshBuilder Satellite()
        {
            var satellite = new MeshBuilder();
            satellite.Append(MeshFactory.Cylinder(0.02f, 0.14f, 0.004f, 10));
            satellite.Append(MeshFactory.Cylinder(0.07f, 0.02f, 0.006f, 20));
            satellite.Append(MeshFactory.Box(new Vector3(0f, 0.2f, 0f), new Vector3(0.11f, 0.11f, 0.11f)));
            foreach (float side in new[] { -1f, 1f })
            {
                satellite.Append(MeshFactory.Box(new Vector3(side * 0.2f, 0.2f, 0f), new Vector3(0.24f, 0.012f, 0.09f)));
                satellite.Append(MeshFactory.Cylinder(0.008f, 0.09f, 0.002f, 8), Matrix4x4.TRS(new Vector3(side * 0.055f, 0.2f, 0f), Quaternion.Euler(0f, 0f, side * 90f), Vector3.one));
            }
            // The dish, a shallow cone turned upside down, on a short mast.
            satellite.Append(MeshFactory.Cylinder(0.008f, 0.06f, 0.002f, 8), Matrix4x4.Translate(new Vector3(0f, 0.255f, 0f)));
            var dish = new List<Vector2> { new Vector2(0f, 0f), new Vector2(0.075f, 0.03f), new Vector2(0.07f, 0.036f), new Vector2(0.01f, 0.012f), new Vector2(0f, 0.012f) };
            satellite.Append(MeshFactory.Lathe(dish, 24), Matrix4x4.Translate(new Vector3(0f, 0.31f, 0f)));
            satellite.Append(MeshFactory.Cylinder(0.005f, 0.05f, 0.001f, 6), Matrix4x4.Translate(new Vector3(0f, 0.32f, 0f)));
            satellite.Append(MeshFactory.Ellipsoid(new Vector3(0.012f, 0.012f, 0.012f), 8, 6), Matrix4x4.Translate(new Vector3(0f, 0.375f, 0f)));
            return satellite;
        }

        /// <summary>A little robot: boxy body and head, round eyes, arms, legs and an antenna.</summary>
        private static MeshBuilder Robot()
        {
            var robot = new MeshBuilder();
            foreach (float z in new[] { -0.05f, 0.05f })
            {
                robot.Append(MeshFactory.Box(new Vector3(0f, 0.035f, z), new Vector3(0.07f, 0.07f, 0.06f)));
            }
            robot.Append(MeshFactory.Box(new Vector3(0f, 0.16f, 0f), new Vector3(0.14f, 0.18f, 0.2f)));
            robot.Append(MeshFactory.Box(new Vector3(0f, 0.31f, 0f), new Vector3(0.12f, 0.12f, 0.16f)));
            foreach (float z in new[] { -0.045f, 0.045f })
            {
                robot.Append(MeshFactory.Ellipsoid(new Vector3(0.02f, 0.02f, 0.012f), 10, 8), Matrix4x4.Translate(new Vector3(0.06f, 0.325f, z)));
                robot.Append(MeshFactory.Cylinder(0.02f, 0.13f, 0.006f, 10), Matrix4x4.TRS(new Vector3(0f, 0.16f, z * 2.6f), Quaternion.Euler(z < 0f ? 15f : -15f, 0f, 0f), Vector3.one));
            }
            robot.Append(MeshFactory.Box(new Vector3(0.062f, 0.15f, 0f), new Vector3(0.01f, 0.03f, 0.08f)));
            robot.Append(MeshFactory.Cylinder(0.006f, 0.06f, 0.001f, 6), Matrix4x4.Translate(new Vector3(0f, 0.37f, 0f)));
            robot.Append(MeshFactory.Ellipsoid(new Vector3(0.016f, 0.016f, 0.016f), 10, 8), Matrix4x4.Translate(new Vector3(0f, 0.435f, 0f)));
            return robot;
        }

        /// <summary>A flying saucer on three landing legs, with a dome and a ring of lights.</summary>
        private static MeshBuilder Ufo()
        {
            var ufo = new MeshBuilder();
            var saucer = new List<Vector2>
            {
                new Vector2(0f, 0.1f), new Vector2(0.09f, 0.1f), new Vector2(0.2f, 0.135f), new Vector2(0.26f, 0.165f),
                new Vector2(0.2f, 0.2f), new Vector2(0.1f, 0.215f), new Vector2(0f, 0.22f)
            };
            ufo.Append(MeshFactory.Lathe(saucer, 32));
            ufo.Append(MeshFactory.Ellipsoid(new Vector3(0.1f, 0.075f, 0.1f), 24, 12), Matrix4x4.Translate(new Vector3(0f, 0.22f, 0f)));
            ufo.Append(MeshFactory.Torus(0.17f, 0.012f, 32, 8), Matrix4x4.Translate(new Vector3(0f, 0.15f, 0f)));
            for (int i = 0; i < 3; i++)
            {
                Quaternion turn = Quaternion.Euler(0f, 120f * i + 60f, 0f);
                ufo.Append(MeshFactory.Cylinder(0.01f, 0.12f, 0.002f, 8), Matrix4x4.TRS(turn * new Vector3(0.12f, 0f, 0f), turn * Quaternion.Euler(0f, 0f, 20f), Vector3.one));
                ufo.Append(MeshFactory.Cylinder(0.03f, 0.012f, 0.004f, 12), Matrix4x4.Translate(turn * new Vector3(0.16f, 0f, 0f)));
            }
            return ufo;
        }

        /// <summary>A comet: a bright head with a tail streaming back, propped on a small stand.</summary>
        private static MeshBuilder Comet()
        {
            var comet = new MeshBuilder();
            comet.Append(MeshFactory.Cylinder(0.06f, 0.04f, 0.008f, 16), Matrix4x4.Translate(new Vector3(0.05f, 0f, 0f)));
            comet.Append(MeshFactory.Ellipsoid(new Vector3(0.1f, 0.1f, 0.1f), 24, 16), Matrix4x4.Translate(new Vector3(0.12f, 0.2f, 0f)));
            // The tail: two cones pointing away from the head, one long and one short beside it.
            comet.Append(MeshFactory.Cone(0.075f, 0.34f, 18), Matrix4x4.TRS(new Vector3(0.08f, 0.17f, 0f), Quaternion.Euler(0f, 0f, 118f), Vector3.one));
            comet.Append(MeshFactory.Cone(0.04f, 0.24f, 14), Matrix4x4.TRS(new Vector3(0.09f, 0.26f, 0.04f), Quaternion.Euler(-10f, 0f, 108f), Vector3.one));
            comet.Append(MeshFactory.Cylinder(0.014f, 0.16f, 0.004f, 8), Matrix4x4.Translate(new Vector3(0.05f, 0.03f, 0f)));
            return comet;
        }

        /// <summary>A ringed planet on a slim stand.</summary>
        private static MeshBuilder RingedPlanet()
        {
            var planet = new MeshBuilder();
            planet.Append(MeshFactory.Cylinder(0.07f, 0.03f, 0.008f, 20));
            planet.Append(MeshFactory.Cylinder(0.012f, 0.1f, 0.003f, 8), Matrix4x4.Translate(new Vector3(0f, 0.02f, 0f)));
            planet.Append(MeshFactory.Ellipsoid(new Vector3(0.13f, 0.12f, 0.13f), 28, 18), Matrix4x4.Translate(new Vector3(0f, 0.25f, 0f)));
            var ring = new List<Vector2>
            {
                new Vector2(0.16f, -0.006f), new Vector2(0.26f, -0.004f), new Vector2(0.26f, 0.004f), new Vector2(0.16f, 0.006f), new Vector2(0.16f, -0.006f)
            };
            planet.Append(MeshFactory.Lathe(ring, 40), Matrix4x4.TRS(new Vector3(0f, 0.25f, 0f), Quaternion.Euler(18f, 0f, -12f), Vector3.one));
            return planet;
        }

        /// <summary>An astronaut's helmet: a sphere with a visor and a neck ring.</summary>
        private static MeshBuilder Helmet()
        {
            var helmet = new MeshBuilder();
            helmet.Append(MeshFactory.Cylinder(0.11f, 0.05f, 0.012f, 24));
            helmet.Append(MeshFactory.Torus(0.1f, 0.016f, 24, 8), Matrix4x4.Translate(new Vector3(0f, 0.05f, 0f)));
            helmet.Append(MeshFactory.Ellipsoid(new Vector3(0.15f, 0.15f, 0.15f), 28, 18), Matrix4x4.Translate(new Vector3(0f, 0.2f, 0f)));
            // The visor, a flattened dome set into the front.
            helmet.Append(MeshFactory.Ellipsoid(new Vector3(0.1f, 0.085f, 0.06f), 20, 12), Matrix4x4.Translate(new Vector3(0.11f, 0.21f, 0f)));
            helmet.Append(MeshFactory.Torus(0.1f, 0.012f, 24, 8), Matrix4x4.TRS(new Vector3(0.115f, 0.21f, 0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(1f, 1f, 0.6f)));
            // A small antenna.
            helmet.Append(MeshFactory.Cylinder(0.007f, 0.07f, 0.002f, 6), Matrix4x4.Translate(new Vector3(-0.06f, 0.32f, 0.06f)));
            helmet.Append(MeshFactory.Ellipsoid(new Vector3(0.012f, 0.012f, 0.012f), 8, 6), Matrix4x4.Translate(new Vector3(-0.06f, 0.39f, 0.06f)));
            return helmet;
        }

        /// <summary>A space station: a hub on a mast with a ring around it and four spokes.</summary>
        private static MeshBuilder SpaceStation()
        {
            var station = new MeshBuilder();
            station.Append(MeshFactory.Cylinder(0.07f, 0.03f, 0.008f, 20));
            station.Append(MeshFactory.Cylinder(0.014f, 0.14f, 0.003f, 8), Matrix4x4.Translate(new Vector3(0f, 0.02f, 0f)));
            station.Append(MeshFactory.Cylinder(0.05f, 0.1f, 0.01f, 16), Matrix4x4.Translate(new Vector3(0f, 0.16f, 0f)));
            station.Append(MeshFactory.Torus(0.17f, 0.024f, 40, 10), Matrix4x4.TRS(new Vector3(0f, 0.21f, 0f), Quaternion.Euler(0f, 0f, 0f), Vector3.one));
            for (int i = 0; i < 4; i++)
            {
                Quaternion turn = Quaternion.Euler(0f, 90f * i + 45f, 0f);
                station.Append(MeshFactory.Cylinder(0.008f, 0.15f, 0.002f, 8), Matrix4x4.TRS(turn * new Vector3(0.04f, 0.21f, 0f), turn * Quaternion.Euler(0f, 0f, -90f), Vector3.one));
            }
            station.Append(MeshFactory.Cylinder(0.006f, 0.1f, 0.001f, 6), Matrix4x4.Translate(new Vector3(0f, 0.26f, 0f)));
            station.Append(MeshFactory.Box(new Vector3(0f, 0.34f, 0f), new Vector3(0.09f, 0.008f, 0.03f)));
            return station;
        }

        // ------------------------------------------------------------------ pieces

        /// <summary>The base a token stands on: a round disc, or a hexagonal plate in the galactic look.</summary>
        public static Mesh TokenBase(MonopolyThemeSpec spec)
        {
            return spec.Style == ArtStyle.Galactic
                ? MeshFactory.Cylinder(0.27f, BaseHeight, 0.01f, 6).ToMesh("TokenBase")
                : MeshFactory.Cylinder(0.25f, BaseHeight, 0.018f, 36).ToMesh("TokenBase");
        }

        public static Mesh TurnRing()
        {
            return MeshFactory.Torus(0.33f, 0.02f, 48, 10).ToMesh("TurnRing");
        }

        /// <summary>A house: a small gabled house, or a dome with a beacon in the galactic look.</summary>
        public static Mesh House(MonopolyThemeSpec spec)
        {
            if (spec.Style == ArtStyle.Galactic)
            {
                return Dome().ToMesh("House");
            }
            var house = new MeshBuilder();
            house.Append(MeshFactory.Box(new Vector3(0f, 0.065f, 0f), new Vector3(0.18f, 0.13f, 0.15f)));
            house.Append(MeshFactory.Roof(0.18f, 0.15f, 0.085f, 0.012f), Matrix4x4.Translate(new Vector3(0f, 0.13f, 0f)));
            house.Append(MeshFactory.Box(new Vector3(0.045f, 0.18f, 0.03f), new Vector3(0.028f, 0.06f, 0.028f)));
            return house.ToMesh("House");
        }

        /// <summary>A hotel: a long gabled block, or a tower in the galactic look.</summary>
        public static Mesh Hotel(MonopolyThemeSpec spec)
        {
            if (spec.Style == ArtStyle.Galactic)
            {
                return Tower().ToMesh("Hotel");
            }
            var hotel = new MeshBuilder();
            hotel.Append(MeshFactory.Box(new Vector3(0f, 0.09f, 0f), new Vector3(0.42f, 0.18f, 0.2f)));
            hotel.Append(MeshFactory.Roof(0.42f, 0.2f, 0.1f, 0.012f), Matrix4x4.Translate(new Vector3(0f, 0.18f, 0f)));
            return hotel.ToMesh("Hotel");
        }

        /// <summary>A habitat dome: a foot ring, a wall with the windows and a dome with a beacon (v runs from the foot to the top).</summary>
        private static MeshBuilder Dome()
        {
            var profile = new List<Vector2> { new Vector2(0f, 0f), new Vector2(0.1f, 0f), new Vector2(0.1f, 0.02f), new Vector2(0.088f, 0.03f), new Vector2(0.088f, 0.09f) };
            for (int i = 1; i <= 8; i++)
            {
                float angle = i / 8f * Mathf.PI * 0.5f;
                profile.Add(new Vector2(0.088f * Mathf.Cos(angle), 0.09f + 0.085f * Mathf.Sin(angle)));
            }
            profile.Add(new Vector2(0.012f, 0.175f));
            profile.Add(new Vector2(0.012f, 0.205f));
            profile.Add(new Vector2(0f, 0.208f));
            return MeshFactory.Lathe(profile, 24);
        }

        /// <summary>A trade tower: an octagonal shaft on a wider foot, a ring near the top and a spire.</summary>
        private static MeshBuilder Tower()
        {
            var profile = new List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(0.17f, 0f), new Vector2(0.17f, 0.03f), new Vector2(0.13f, 0.04f),
                new Vector2(0.13f, 0.3f), new Vector2(0.15f, 0.31f), new Vector2(0.15f, 0.335f), new Vector2(0.11f, 0.345f),
                new Vector2(0.11f, 0.37f), new Vector2(0.02f, 0.39f), new Vector2(0.02f, 0.45f), new Vector2(0f, 0.46f)
            };
            var tower = new MeshBuilder();
            tower.Append(MeshFactory.Lathe(profile, 8), Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 22.5f, 0f), new Vector3(1.3f, 1f, 0.8f)));
            return tower;
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
