using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Stand-in scenery for a theme that has no props, landmarks or lamp for a land yet: simple flat-shaded shapes in
    /// plain colours (rocks and cacti in the desert, crates and barrels in the yards, pines in the snow, blocks and trees
    /// in the city), so a track never looks bare while the art is being made.
    /// </summary>
    public static partial class TrackBuilder
    {
        /// <summary>The stand-ins of one dressing: hidden templates the scatter copies, destroyed when it is done.</summary>
        private sealed class Templates
        {
            private readonly Dressing d;
            private Transform holder;
            private GameObject[] props;
            private GameObject[] landmarks;
            private GameObject lamp;

            public Templates(Dressing dressing)
            {
                d = dressing;
            }

            public GameObject[] Props => props ?? (props = MakeProps());
            public GameObject[] Landmarks => landmarks ?? (landmarks = MakeLandmarks());
            public GameObject Lamp => lamp != null ? lamp : lamp = MakeLamp();

            public void Dispose()
            {
                if (holder != null)
                {
                    Discard(holder.gameObject);
                }
            }

            private GameObject[] MakeProps()
            {
                TrackMaterials.Warn($"no props {d.Biome}", $"Death Race track: the theme has no props for {d.Biome} yet; using stand-ins.");
                switch (d.Biome)
                {
                    case Biome.Industrial:
                        return new[] { Crate(), Barrels(), Scrap(), Crate() };
                    case Biome.Snow:
                        return new[] { Pine(1f), Pine(1.3f), SnowRock(), Pine(0.8f) };
                    case Biome.City:
                        return new[] { Building(9f), Building(14f), Tree(), Bin(), Tree() };
                    default:
                        return new[] { Rock(), Cactus(), Bush(), Rock() };
                }
            }

            private GameObject[] MakeLandmarks()
            {
                TrackMaterials.Warn($"no landmarks {d.Biome}", $"Death Race track: the theme has no landmarks for {d.Biome} yet; using stand-ins.");
                switch (d.Biome)
                {
                    case Biome.Industrial:
                        return new[] { Chimney(), Tank() };
                    case Biome.Snow:
                        return new[] { Peak() };
                    case Biome.City:
                        return new[] { Tower(28f), Tower(40f) };
                    default:
                        return new[] { Mesa() };
                }
            }

            private GameObject MakeLamp()
            {
                var pole = new TrackMesh();
                pole.Cylinder(Vector3.zero, 0.14f, 0.1f, 6.6f, 8, Quaternion.identity);
                pole.Box(new Vector3(0f, 6.45f, 0.8f), new Vector3(0.12f, 0.12f, 1.7f), Quaternion.identity);
                var head = new TrackMesh();
                head.Box(new Vector3(0f, 6.3f, 1.6f), new Vector3(0.45f, 0.2f, 0.8f), Quaternion.identity);
                Color glow = d.Night ? TrackMaterials.NeonColor(d.Biome) * 2f : default;
                GameObject template = Template("Lamp",
                    (pole, TrackMaterials.Plain("lamp pole", new Color(0.22f, 0.22f, 0.24f, 1f), 0.4f)),
                    (head, TrackMaterials.Plain(d.Night ? $"lamp head {d.Biome}" : "lamp head", new Color(0.9f, 0.88f, 0.8f, 1f), 0.5f, glow)));
                var light = new GameObject("Head");
                light.transform.SetParent(template.transform, false);
                light.transform.localPosition = new Vector3(0f, 6.2f, 1.6f);
                return template;
            }

            private GameObject Rock()
            {
                var mesh = new TrackMesh();
                mesh.Box(Vector3.zero, new Vector3(2.4f, 1.3f, 1.9f), Quaternion.Euler(0f, 20f, 0f));
                mesh.Box(new Vector3(0.9f, 0f, 0.6f), new Vector3(1.3f, 0.8f, 1.2f), Quaternion.Euler(0f, 55f, 0f));
                return Template("Rock", (mesh, Plain("rock", 0.56f, 0.43f, 0.33f)));
            }

            private GameObject Cactus()
            {
                var mesh = new TrackMesh();
                mesh.Cylinder(Vector3.zero, 0.36f, 0.3f, 3.4f, 8, Quaternion.identity);
                mesh.Box(new Vector3(0.45f, 1.3f, 0f), new Vector3(0.6f, 0.4f, 0.4f), Quaternion.identity);
                mesh.Cylinder(new Vector3(0.75f, 1.3f, 0f), 0.24f, 0.2f, 1.3f, 8, Quaternion.identity);
                mesh.Box(new Vector3(-0.4f, 1.8f, 0f), new Vector3(0.5f, 0.35f, 0.35f), Quaternion.identity);
                mesh.Cylinder(new Vector3(-0.65f, 1.8f, 0f), 0.2f, 0.17f, 0.9f, 8, Quaternion.identity);
                return Template("Cactus", (mesh, Plain("cactus", 0.3f, 0.45f, 0.26f)));
            }

            private GameObject Bush()
            {
                var mesh = new TrackMesh();
                mesh.Cylinder(Vector3.zero, 1.1f, 0.2f, 0.9f, 7, Quaternion.identity);
                return Template("Bush", (mesh, Plain("bush", 0.46f, 0.4f, 0.24f)));
            }

            private GameObject Crate()
            {
                var mesh = new TrackMesh();
                mesh.Box(Vector3.zero, new Vector3(1.5f, 1.5f, 1.5f), Quaternion.Euler(0f, 12f, 0f));
                mesh.Box(new Vector3(0.2f, 1.5f, 0.1f), new Vector3(1.1f, 1.1f, 1.1f), Quaternion.Euler(0f, 40f, 0f));
                return Template("Crate", (mesh, Plain("crate", 0.55f, 0.4f, 0.24f)));
            }

            private GameObject Barrels()
            {
                var red = new TrackMesh();
                var blue = new TrackMesh();
                red.Cylinder(Vector3.zero, 0.36f, 0.36f, 1.1f, 10, Quaternion.identity);
                blue.Cylinder(new Vector3(0.8f, 0f, 0.2f), 0.36f, 0.36f, 1.1f, 10, Quaternion.identity);
                red.Cylinder(new Vector3(0.35f, 0f, 0.8f), 0.36f, 0.36f, 1.1f, 10, Quaternion.identity);
                return Template("Barrels", (red, Plain("barrel red", 0.6f, 0.2f, 0.12f)), (blue, Plain("barrel blue", 0.18f, 0.3f, 0.5f)));
            }

            private GameObject Scrap()
            {
                var mesh = new TrackMesh();
                mesh.Box(Vector3.zero, new Vector3(3f, 1f, 2f), Quaternion.Euler(0f, 10f, 0f));
                mesh.Box(new Vector3(0.4f, 0.9f, 0.2f), new Vector3(1.8f, 0.8f, 1.4f), Quaternion.Euler(8f, 50f, 6f));
                mesh.Box(new Vector3(-0.8f, 0f, 1.1f), new Vector3(1.2f, 0.6f, 1.2f), Quaternion.Euler(0f, 30f, 0f));
                return Template("Scrap", (mesh, Plain("scrap", 0.42f, 0.38f, 0.34f)));
            }

            private GameObject Pine(float size)
            {
                var trunk = new TrackMesh();
                trunk.Cylinder(Vector3.zero, 0.28f * size, 0.22f * size, 1.4f * size, 7, Quaternion.identity);
                var needles = new TrackMesh();
                needles.Cylinder(new Vector3(0f, 1f, 0f) * size, 2.1f * size, 0f, 3.2f * size, 8, Quaternion.identity);
                needles.Cylinder(new Vector3(0f, 2.8f, 0f) * size, 1.6f * size, 0f, 2.6f * size, 8, Quaternion.identity);
                needles.Cylinder(new Vector3(0f, 4.4f, 0f) * size, 1.05f * size, 0f, 2f * size, 8, Quaternion.identity);
                return Template("Pine", (trunk, Plain("trunk", 0.33f, 0.23f, 0.16f)), (needles, Plain("pine", 0.16f, 0.31f, 0.24f)));
            }

            private GameObject SnowRock()
            {
                var mesh = new TrackMesh();
                mesh.Box(Vector3.zero, new Vector3(2.2f, 1.1f, 1.7f), Quaternion.Euler(0f, 25f, 0f));
                mesh.Box(new Vector3(-0.7f, 0f, 0.5f), new Vector3(1.2f, 0.7f, 1f), Quaternion.Euler(0f, 70f, 0f));
                return Template("Snow Rock", (mesh, Plain("snow rock", 0.72f, 0.76f, 0.82f)));
            }

            private GameObject Building(float height)
            {
                var mesh = new TrackMesh();
                mesh.Box(Vector3.zero, new Vector3(9f, height, 9f), Quaternion.identity);
                mesh.Box(new Vector3(1.5f, height, -1f), new Vector3(3f, 1.4f, 3f), Quaternion.identity);
                return Template("Block", (mesh, Plain("block", 0.44f, 0.44f, 0.47f)));
            }

            private GameObject Tree()
            {
                var trunk = new TrackMesh();
                trunk.Cylinder(Vector3.zero, 0.22f, 0.18f, 2.2f, 7, Quaternion.identity);
                var crown = new TrackMesh();
                crown.Cylinder(new Vector3(0f, 1.8f, 0f), 1.5f, 0.9f, 2.4f, 8, Quaternion.identity);
                return Template("Tree", (trunk, Plain("trunk", 0.33f, 0.23f, 0.16f)), (crown, Plain("crown", 0.24f, 0.4f, 0.22f)));
            }

            private GameObject Bin()
            {
                var mesh = new TrackMesh();
                mesh.Box(Vector3.zero, new Vector3(1.8f, 1.3f, 1.1f), Quaternion.identity);
                return Template("Bin", (mesh, Plain("bin", 0.22f, 0.34f, 0.28f)));
            }

            private GameObject Mesa()
            {
                var mesh = new TrackMesh();
                mesh.Box(Vector3.zero, new Vector3(26f, 9f, 20f), Quaternion.Euler(0f, 12f, 0f));
                mesh.Box(new Vector3(-2f, 9f, 1f), new Vector3(17f, 6f, 13f), Quaternion.Euler(0f, 20f, 0f));
                mesh.Box(new Vector3(9f, 0f, -6f), new Vector3(9f, 5f, 8f), Quaternion.Euler(0f, 40f, 0f));
                return Template("Mesa", (mesh, Plain("mesa", 0.62f, 0.4f, 0.27f)));
            }

            private GameObject Chimney()
            {
                var mesh = new TrackMesh();
                mesh.Cylinder(Vector3.zero, 3.2f, 2.4f, 30f, 12, Quaternion.identity);
                mesh.Box(new Vector3(6f, 0f, 0f), new Vector3(8f, 7f, 10f), Quaternion.identity);
                var band = new TrackMesh();
                band.Cylinder(new Vector3(0f, 26f, 0f), 2.55f, 2.5f, 2f, 12, Quaternion.identity);
                return Template("Chimney", (mesh, Plain("brick", 0.48f, 0.3f, 0.24f)), (band, Plain("band", 0.85f, 0.75f, 0.2f)));
            }

            private GameObject Tank()
            {
                var mesh = new TrackMesh();
                mesh.Cylinder(Vector3.zero, 7f, 7f, 9f, 16, Quaternion.identity);
                mesh.Cylinder(new Vector3(0f, 9f, 0f), 7f, 1.5f, 2f, 16, Quaternion.identity);
                return Template("Tank", (mesh, Plain("tank", 0.62f, 0.62f, 0.6f)));
            }

            private GameObject Peak()
            {
                var rock = new TrackMesh();
                rock.Cylinder(Vector3.zero, 22f, 9f, 9f, 9, Quaternion.Euler(0f, 15f, 0f));
                var snow = new TrackMesh();
                snow.Cylinder(new Vector3(0f, 9f, 0f), 9f, 0f, 12f, 9, Quaternion.Euler(0f, 15f, 0f));
                return Template("Peak", (rock, Plain("peak rock", 0.42f, 0.44f, 0.48f)), (snow, Plain("peak snow", 0.92f, 0.94f, 0.97f)));
            }

            private GameObject Tower(float height)
            {
                var mesh = new TrackMesh();
                mesh.Box(Vector3.zero, new Vector3(18f, height, 16f), Quaternion.identity);
                mesh.Box(new Vector3(0f, height, 0f), new Vector3(8f, 3f, 8f), Quaternion.identity);
                return Template("Tower", (mesh, Plain("tower", 0.34f, 0.36f, 0.42f)));
            }

            private Material Plain(string key, float r, float g, float b)
            {
                float dim = d.Night ? 0.6f : 1f;
                return TrackMaterials.Plain($"{key} {(d.Night ? "night" : "day")}", new Color(r * dim, g * dim, b * dim * 1.1f, 1f), 0.12f);
            }

            /// <summary>A hidden template object with one child per material.</summary>
            private GameObject Template(string name, params (TrackMesh mesh, Material material)[] parts)
            {
                if (holder == null)
                {
                    holder = new GameObject("Stand-in Templates").transform;
                    holder.SetParent(d.Root, false);
                    holder.gameObject.SetActive(false);
                }
                var template = new GameObject(name);
                template.transform.SetParent(holder, false);
                foreach ((TrackMesh mesh, Material material) in parts)
                {
                    var part = new GameObject(material.name);
                    part.transform.SetParent(template.transform, false);
                    part.AddComponent<MeshFilter>().sharedMesh = d.Owner.Own(mesh.ToMesh(name));
                    part.AddComponent<MeshRenderer>().sharedMaterial = material;
                }
                return template;
            }
        }
    }
}
