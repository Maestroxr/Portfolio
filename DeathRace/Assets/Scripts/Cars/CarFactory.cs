using Gamebox;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Builds a race car at run time: the visual prefab of its model (meshes and a <see cref="CarModelView"/>, no physics),
    /// or a stand-in of primitives while the model is missing, and then every gameplay part in code (the Rigidbody and its
    /// box, the car, its body, health, weapons and sound), so no agent's prefab has to know another's components.
    /// </summary>
    public static class CarFactory
    {
        private static bool warnedMissingPrefab;
        private static Material standInMaterial;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        /// <summary>A car of <paramref name="entrant"/> at <paramref name="pose"/>, not yet in a race (tests, previews).</summary>
        public static Car Spawn(RaceEntrant entrant, Pose pose, DeathRaceContent content, DeathRaceTheme theme)
        {
            return Spawn(entrant, pose, content, theme, null, null, null);
        }

        /// <summary>
        /// A car of <paramref name="entrant"/> at <paramref name="pose"/> under <paramref name="parent"/>, racing in
        /// <paramref name="race"/> with <paramref name="driver"/>; <paramref name="volume"/> scales its engine.
        /// </summary>
        public static Car Spawn(RaceEntrant entrant, Pose pose, DeathRaceContent content, DeathRaceTheme theme, Race race, ICarDriver driver, Transform parent, float volume = 1f)
        {
            CarLoadout loadout = entrant.Loadout ?? (entrant.Loadout = new CarLoadout());
            GameObject prefab = content != null ? content.CarPrefab(loadout.model) : null;
            GameObject root;
            if (prefab != null)
            {
                root = Object.Instantiate(prefab, pose.position, pose.rotation, parent);
            }
            else
            {
                if (!warnedMissingPrefab)
                {
                    warnedMissingPrefab = true;
                    Debug.LogWarning($"Death Race: no model for the {loadout.model}; racing a stand-in car.");
                }
                root = StandIn(loadout.Spec, entrant.Color, theme != null ? theme.carPaint : null);
                root.transform.SetParent(parent, false);
                root.transform.SetPositionAndRotation(pose.position, pose.rotation);
            }
            root.name = $"Car {entrant.Seat} {entrant.Name}";
            root.layer = DeathRaceLayers.Car;
            // The visual is meant to have no colliders; any it has would join the car's box and catch on the road.
            foreach (Collider stray in root.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(stray);
            }
            CarModelView view = root.GetComponent<CarModelView>();
            if (view == null)
            {
                view = root.AddComponent<CarModelView>();
                view.model = loadout.model;
            }

            root.AddComponent<Rigidbody>();
            root.AddComponent<BoxCollider>();
            Car car = root.AddComponent<Car>();
            CarBody body = root.AddComponent<CarBody>();
            CarHealth health = root.AddComponent<CarHealth>();
            CarWeapons weapons = root.AddComponent<CarWeapons>();
            CarAudio audio = root.AddComponent<CarAudio>();

            car.Setup(entrant, race, driver, body, health, weapons, view);
            body.Setup(car, car.Stats);
            health.Setup(car, loadout.health);
            weapons.Setup(car, loadout);
            car.AttachAudio(audio);
            audio.Setup(car, content != null ? content.sounds : null, volume);
            view.ApplyTheme(theme, entrant.Color);
            car.Assign(entrant.Seat, ControlOf(entrant), entrant.Name);
            return car;
        }

        /// <summary>Who plays a car, in BaseGame's terms.</summary>
        public static PlayerControl ControlOf(RaceEntrant entrant)
        {
            switch (entrant.Driver)
            {
                case DriverKind.Human: return PlayerControl.Local;
                case DriverKind.Remote: return PlayerControl.Remote;
                default: return PlayerControl.Computer;
            }
        }

        /// <summary>
        /// A car of boxes and cylinders in the size of <paramref name="spec"/>, painted <paramref name="paint"/>, with the
        /// mounts and wheels a <see cref="CarModelView"/> expects: what races while the real models are missing. Every part
        /// is drawn in <paramref name="material"/> (the look's car paint; the primitives' own material without one), tinted
        /// per part, so the stand-in renders in any render pipeline the look was made for.
        /// </summary>
        public static GameObject StandIn(CarSpec spec, Color paint, Material material = null)
        {
            standInMaterial = material;
            Vector3 size = spec.Size;
            var root = new GameObject($"{spec.Name} (stand-in)");
            CarModelView view = root.AddComponent<CarModelView>();
            view.model = spec.Model;
            float wheelRadius = CarBody.WheelRadius;
            float chassisHeight = Mathf.Max(0.4f, size.y * 0.42f);
            float chassisY = wheelRadius + chassisHeight * 0.5f - 0.05f;
            Renderer chassis = Part(root.transform, "Body", PrimitiveType.Cube, new Vector3(0f, chassisY, 0f), new Vector3(size.x, chassisHeight, size.z), paint);
            Renderer nose = Part(root.transform, "Bonnet", PrimitiveType.Cube, new Vector3(0f, chassisY + chassisHeight * 0.5f + 0.06f, size.z * 0.28f),
                new Vector3(size.x * 0.86f, 0.12f, size.z * 0.3f), paint);
            float cabinHeight = Mathf.Max(0.3f, size.y - chassisY - chassisHeight * 0.5f);
            Part(root.transform, "Cabin", PrimitiveType.Cube, new Vector3(0f, chassisY + chassisHeight * 0.5f + cabinHeight * 0.5f, -size.z * 0.1f),
                new Vector3(size.x * 0.8f, cabinHeight, size.z * 0.42f), new Color(0.12f, 0.13f, 0.15f));
            Part(root.transform, "Gun", PrimitiveType.Cube, new Vector3(0f, chassisY + chassisHeight * 0.5f + 0.15f, size.z * 0.42f),
                new Vector3(0.14f, 0.14f, size.z * 0.25f), new Color(0.2f, 0.2f, 0.22f));
            view.paint = new[] { chassis, nose };
            // The cabin and the gun stay out of the palette list: a box's UVs would smear the whole palette over it.
            view.body = new Renderer[0];

            float wheelX = size.x * 0.5f - 0.12f;
            float wheelZ = size.z * 0.5f - 0.8f;
            view.wheelFrontLeft = Wheel(root.transform, "Wheel FL", new Vector3(-wheelX, wheelRadius, wheelZ));
            view.wheelFrontRight = Wheel(root.transform, "Wheel FR", new Vector3(wheelX, wheelRadius, wheelZ));
            view.wheelRearLeft = Wheel(root.transform, "Wheel RL", new Vector3(-wheelX, wheelRadius, -wheelZ));
            view.wheelRearRight = Wheel(root.transform, "Wheel RR", new Vector3(wheelX, wheelRadius, -wheelZ));

            float lightY = chassisY + 0.05f;
            var warm = new Color(1f, 0.95f, 0.75f);
            var red = new Color(0.9f, 0.08f, 0.05f);
            view.headlights = new[]
            {
                Lamp(root.transform, "Headlight L", new Vector3(-size.x * 0.32f, lightY, size.z * 0.5f), warm),
                Lamp(root.transform, "Headlight R", new Vector3(size.x * 0.32f, lightY, size.z * 0.5f), warm)
            };
            view.taillights = new[]
            {
                Lamp(root.transform, "Taillight L", new Vector3(-size.x * 0.34f, lightY, -size.z * 0.5f), red),
                Lamp(root.transform, "Taillight R", new Vector3(size.x * 0.34f, lightY, -size.z * 0.5f), red)
            };
            view.muzzle = Mount(root.transform, "Muzzle", new Vector3(0f, chassisY + chassisHeight * 0.5f + 0.15f, size.z * 0.56f), Quaternion.identity);
            view.mineDrop = Mount(root.transform, "Mine Drop", new Vector3(0f, 0.3f, -size.z * 0.5f - 0.6f), Quaternion.identity);
            view.rocketMount = Mount(root.transform, "Rocket Mount", new Vector3(0f, size.y + 0.1f, 0f), Quaternion.identity);
            view.exhausts = new[]
            {
                Mount(root.transform, "Exhaust L", new Vector3(-size.x * 0.25f, 0.35f, -size.z * 0.5f), Quaternion.Euler(0f, 180f, 0f)),
                Mount(root.transform, "Exhaust R", new Vector3(size.x * 0.25f, 0.35f, -size.z * 0.5f), Quaternion.Euler(0f, 180f, 0f))
            };
            return root;
        }

        private static Renderer Part(Transform parent, string partName, PrimitiveType shape, Vector3 position, Vector3 scale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(shape);
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            Renderer renderer = part.GetComponent<Renderer>();
            if (standInMaterial != null)
            {
                renderer.sharedMaterial = standInMaterial;
            }
            Tint(renderer, color, Color.black);
            return renderer;
        }

        private static Transform Wheel(Transform parent, string wheelName, Vector3 position)
        {
            Transform pivot = Mount(parent, wheelName, position, Quaternion.identity);
            float diameter = CarBody.WheelRadius * 2f;
            Renderer tyre = Part(pivot, "Tyre", PrimitiveType.Cylinder, Vector3.zero, new Vector3(diameter, 0.16f, diameter), new Color(0.07f, 0.07f, 0.08f));
            // A cylinder stands along y: lay it on its side so it rolls about the car's x axis.
            tyre.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Renderer hub = Part(pivot, "Hub", PrimitiveType.Cylinder, Vector3.zero, new Vector3(diameter * 0.55f, 0.17f, diameter * 0.55f), new Color(0.55f, 0.55f, 0.58f));
            hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            return pivot;
        }

        private static Renderer Lamp(Transform parent, string lightName, Vector3 position, Color color)
        {
            Renderer light = Part(parent, lightName, PrimitiveType.Cube, position, new Vector3(0.36f, 0.14f, 0.06f), color);
            Tint(light, color, color * 1.5f);
            return light;
        }

        private static Transform Mount(Transform parent, string mountName, Vector3 position, Quaternion rotation)
        {
            var mount = new GameObject(mountName).transform;
            mount.SetParent(parent, false);
            mount.localPosition = position;
            mount.localRotation = rotation;
            return mount;
        }

        private static void Tint(Renderer renderer, Color color, Color emission)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColor, color);
            block.SetColor(ColorId, color);
            if (emission.maxColorComponent > 0f)
            {
                block.SetColor(EmissionColor, emission);
            }
            renderer.SetPropertyBlock(block);
        }
    }
}
