using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    internal static partial class DeathRacePrefabBuilder
    {
        private static string CarMesh(CarModel model, string part)
        {
            return $"{CarArt.MeshFolder}/{model}{part}.asset";
        }

        /// <summary>The meshes of the six cars: body, paint, head and tail lights, front and rear wheels.</summary>
        private static void BuildCarMeshes()
        {
            for (int i = 0; i < CarSpecs.Count; i++)
            {
                var model = (CarModel)i;
                CarRig rig = CarMeshes.Build(model);
                SaveMesh(rig.Body, CarMesh(model, "Body"));
                SaveMesh(rig.Paint, CarMesh(model, "Paint"));
                SaveMesh(rig.Heads, CarMesh(model, "Headlights"));
                SaveMesh(rig.Tails, CarMesh(model, "Taillights"));
                SaveMesh(rig.FrontWheel, CarMesh(model, "WheelFront"));
                SaveMesh(rig.RearWheel, CarMesh(model, "WheelRear"));
            }
        }

        /// <summary>
        /// The car prefabs: the model's renderers, four wheel objects pivoting at their centres, the muzzle, mine chute,
        /// rocket mount and exhaust points, the contact shadow, the night beams and tail glow, the nitro flames, and a
        /// CarModelView wired to all of them. No colliders or rigidbody: the race adds those.
        /// </summary>
        private static void BuildCars()
        {
            Material palette = CarArt.PaletteMaterial(false);
            Material paint = CarArt.PaintMaterial(false);
            Mesh beam = DeathRaceAssets.Require<Mesh>($"{CarArt.MeshFolder}/Beam.asset");
            Mesh quad = DeathRaceAssets.Require<Mesh>($"{CarArt.MeshFolder}/GroundQuad.asset");
            Mesh flame = DeathRaceAssets.Require<Mesh>($"{CarArt.MeshFolder}/NitroFlame.asset");
            for (int i = 0; i < CarSpecs.Count; i++)
            {
                var model = (CarModel)i;
                CarRig rig = CarMeshes.Build(model);
                CarSpec spec = rig.Spec;
                var root = new GameObject(model.ToString());
                var view = root.AddComponent<CarModelView>();
                view.model = model;
                Transform t = root.transform;

                Mesh bodyMesh = DeathRaceAssets.Require<Mesh>(CarMesh(model, "Body"));
                Mesh paintMesh = DeathRaceAssets.Require<Mesh>(CarMesh(model, "Paint"));
                MeshRenderer body = Part(t, "Body", bodyMesh, palette, Vector3.zero, Quaternion.identity);
                MeshRenderer painted = Part(t, "Paint", paintMesh, paint, Vector3.zero, Quaternion.identity);
                MeshRenderer heads = Part(t, "Headlights", DeathRaceAssets.Require<Mesh>(CarMesh(model, "Headlights")), palette, Vector3.zero, Quaternion.identity, false);
                MeshRenderer tails = Part(t, "Taillights", DeathRaceAssets.Require<Mesh>(CarMesh(model, "Taillights")), palette, Vector3.zero, Quaternion.identity, false);

                Transform wheels = Mount(t, "Wheels", Vector3.zero, Quaternion.identity);
                Mesh front = DeathRaceAssets.Require<Mesh>(CarMesh(model, "WheelFront"));
                Mesh rear = DeathRaceAssets.Require<Mesh>(CarMesh(model, "WheelRear"));
                string[] wheelNames = { "WheelFL", "WheelFR", "WheelRL", "WheelRR" };
                var wheelRenderers = new MeshRenderer[4];
                for (int w = 0; w < 4; w++)
                {
                    wheelRenderers[w] = Part(wheels, wheelNames[w], w < 2 ? front : rear, palette, rig.Wheels[w], Quaternion.identity);
                }
                view.wheelFrontLeft = wheelRenderers[0].transform;
                view.wheelFrontRight = wheelRenderers[1].transform;
                view.wheelRearLeft = wheelRenderers[2].transform;
                view.wheelRearRight = wheelRenderers[3].transform;
                view.frontWheelRadius = rig.FrontRadius;
                view.rearWheelRadius = rig.RearRadius;

                view.body = new Renderer[] { body, wheelRenderers[0], wheelRenderers[1], wheelRenderers[2], wheelRenderers[3] };
                view.paint = new Renderer[] { painted };
                view.headlights = new Renderer[] { heads };
                view.taillights = new Renderer[] { tails };

                view.muzzle = Mount(t, "Muzzle", rig.Muzzle, Quaternion.identity);
                view.mineDrop = Mount(t, "MineDrop", rig.MineDrop, Quaternion.Euler(0f, 180f, 0f));
                view.rocketMount = Mount(t, "RocketMount", rig.RocketMount, Quaternion.identity);
                var exhausts = new List<Transform>();
                var flames = new List<Renderer>();
                for (int e = 0; e < rig.Exhausts.Count; e++)
                {
                    Quaternion blow = Quaternion.LookRotation(rig.ExhaustDirections[e], Vector3.up);
                    exhausts.Add(Mount(t, $"Exhaust{e + 1}", rig.Exhausts[e], blow));
                    MeshRenderer fire = Part(t, $"NitroFlame{e + 1}", flame, CarArt.Shared("NitroFlame"), rig.Exhausts[e] + rig.ExhaustDirections[e] * 0.02f, blow, false);
                    fire.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.3f, (spec.Size.x - 1.9f) / 0.4f);
                    fire.enabled = false;
                    flames.Add(fire);
                }
                view.exhausts = exhausts.ToArray();
                view.nitroFlames = flames.ToArray();

                MeshRenderer shadow = Part(t, "Shadow", quad, CarArt.Shared("Shadow"), new Vector3(0f, 0.025f, 0f), Quaternion.identity, false);
                shadow.transform.localScale = new Vector3(spec.Size.x * 1.25f, 1f, spec.Size.z * 1.12f);
                view.shadow = shadow;
                var beams = new List<Renderer>();
                foreach (Vector3 lamp in rig.Beams)
                {
                    MeshRenderer light = Part(t, "Beam", beam, CarArt.Shared("Beam"), new Vector3(lamp.x * 0.8f, 0.04f, lamp.z - 0.1f), Quaternion.Euler(0f, Mathf.Sign(lamp.x) * 4f, 0f), false);
                    light.enabled = false;
                    beams.Add(light);
                }
                view.beams = beams.ToArray();
                MeshRenderer glow = Part(t, "TailGlow", quad, CarArt.Shared("TailGlow"), new Vector3(0f, 0.035f, -spec.Size.z * 0.5f - 0.55f), Quaternion.identity, false);
                glow.transform.localScale = new Vector3(spec.Size.x * 1.4f, 1f, 1.8f);
                glow.enabled = false;
                view.tailGlows = new Renderer[] { glow };

                var bounds = new Bounds(bodyMesh.bounds.center, bodyMesh.bounds.size);
                bounds.Encapsulate(paintMesh.bounds);
                for (int w = 0; w < 4; w++)
                {
                    float radius = w < 2 ? rig.FrontRadius : rig.RearRadius;
                    bounds.Encapsulate(new Bounds(rig.Wheels[w], new Vector3(0.4f, radius * 2f, radius * 2f)));
                }
                DeathRaceAssets.Set(view, "bounds", p => p.boundsValue = bounds);

                DeathRaceAssets.SavePrefab(root, $"{CarFolder}/{model}.prefab");
            }
        }
    }
}
