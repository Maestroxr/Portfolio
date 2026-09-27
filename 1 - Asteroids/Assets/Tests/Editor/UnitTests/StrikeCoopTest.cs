using System.Collections.Generic;
using Gamebox;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// The plain rules behind a planet strike mission shared with other pilots: what a boss part, a beam and the scroll
    /// look like on the wire, where the pilots start, the difficulty of a room, and how a busy update is split for the
    /// server.
    /// </summary>
    public class StrikeCoopTest
    {
        private readonly TemporaryObjects objects = new TemporaryObjects();

        [TearDown]
        public void TearDown()
        {
            objects.Dispose();
        }

        [Test]
        public void TheNumbersOfTheStrikeKindsTravel()
        {
            // The others read these numbers from the rows: they must never move.
            Assert.That((int)BodyKind.StrikeAir, Is.EqualTo(12));
            Assert.That((int)BodyKind.StrikeGround, Is.EqualTo(13));
            Assert.That((int)BodyKind.BossPart, Is.EqualTo(14));
            Assert.That((int)FieldSignalKind.Scroll, Is.EqualTo(2));
            Assert.That(SpawnService.StrikeGhostBase, Is.EqualTo(20), "Ghost shots of the strike weapons follow the field's weapons.");
        }

        [Test]
        public void ABossPartNamesItsBossAndItsPlace()
        {
            foreach (uint boss in new[] { 1u, 2u, 255u, 256u, 70000u, 0x7FFFFFu })
            {
                foreach (int index in new[] { 0, 1, 7, 255 })
                {
                    int variant = BodyCodec.PackPart(boss, index);
                    Assert.That(variant, Is.GreaterThanOrEqualTo(0), "A variant is never negative.");
                    BodyCodec.UnpackPart(variant, out uint unpackedBoss, out int unpackedIndex);
                    Assert.That(unpackedBoss, Is.EqualTo(boss));
                    Assert.That(unpackedIndex, Is.EqualTo(index));
                }
            }
            Assert.That(BodyCodec.PackPart(3u, 2), Is.EqualTo((3 << 8) | 2), "The layout of the design: (boss net id << 8) | part index.");
        }

        [Test]
        public void ABeamTravelsInThePose()
        {
            var pose = new ShipPose { Alive = true, Thrusting = true, Beam = true, BeamKind = 2, Hull = 5, Health = 0.75f, Shield = 0.4f };
            ShipPose unpacked = ShipPose.Unpack(pose.PackState(), pose.PackValue());
            Assert.IsTrue(unpacked.Beam);
            Assert.That(unpacked.BeamKind, Is.EqualTo(2));
            Assert.That(unpacked.Hull, Is.EqualTo(5), "The beam kind does not touch the hull.");
            Assert.That(unpacked.Health, Is.EqualTo(0.75f).Within(0.006f));
            Assert.That(unpacked.Shield, Is.EqualTo(0.4f).Within(0.006f));
            Assert.IsFalse(unpacked.Magnet);
            Assert.IsFalse(unpacked.Drones);

            var released = new ShipPose { Alive = true, Thrusting = true, Beam = false, BeamKind = 2, Hull = 5 };
            Assert.That(released.PackState(), Is.Not.EqualTo(pose.PackState()), "Taking the beam off is a change of state, sent at once.");
            Assert.IsFalse(ShipPose.Unpack(released.PackState(), released.PackValue()).Beam);
            Assert.That(ShipPose.Unpack(pose.PackState(), new ShipPose { BeamKind = 99 }.PackValue()).BeamKind, Is.EqualTo(15), "The kind is clamped to its four bits.");
        }

        [Test]
        public void ThePilotsOfAStrikeStartInARowAtTheBottom()
        {
            Vector2 half = StrikeRules.HalfSize;
            Vector2 alone = FieldMath.RowPoint(0, 1, half);
            Assert.That(alone.x, Is.EqualTo(0f).Within(0.0001f), "A pilot alone starts at the bottom centre.");
            Assert.That(alone.y, Is.EqualTo(-half.y + CoopRules.RowHeight).Within(0.0001f));
            for (int pilots = 1; pilots <= CoopRules.MaxPilots; pilots++)
            {
                float sum = 0f;
                for (int slot = 0; slot < pilots; slot++)
                {
                    Vector2 start = FieldMath.RowPoint(slot, pilots, half);
                    Assert.That(start.y, Is.EqualTo(alone.y).Within(0.0001f), "Everybody starts in the same row.");
                    Assert.That(Mathf.Abs(start.x), Is.LessThan(half.x - 1f), "Everybody starts inside the playfield.");
                    if (slot > 0)
                    {
                        Assert.That(start.x - FieldMath.RowPoint(slot - 1, pilots, half).x, Is.EqualTo(CoopRules.RowSpacing).Within(0.0001f),
                            "The pilots start side by side by seat, apart by the spacing.");
                    }
                    sum += start.x;
                }
                Assert.That(sum, Is.EqualTo(0f).Within(0.001f), "The row is centred.");
            }
            Assert.That(FieldMath.RowPoint(9, 2, half), Is.EqualTo(FieldMath.RowPoint(1, 2, half)), "A slot past the row takes the last place.");
            Assert.That(FieldMath.RowPoint(0, 0, half), Is.EqualTo(alone), "No pilots counts as one.");
        }

        [Test]
        public void TheDifficultyOfARoomIsReadFromItsOptions()
        {
            Assert.That(CoopRules.Difficulty("lives=3;difficulty=0"), Is.EqualTo(StrikeDifficulty.Rookie));
            Assert.That(CoopRules.Difficulty("difficulty=2;lives=3"), Is.EqualTo(StrikeDifficulty.Elite));
            Assert.That(CoopRules.Difficulty("lives=3"), Is.EqualTo(StrikeDifficulty.Veteran), "An older room without the option plays Veteran.");
            Assert.That(CoopRules.Difficulty("difficulty=7"), Is.EqualTo(StrikeDifficulty.Veteran), "A number that is no difficulty plays Veteran.");
            Assert.That(CoopRules.Difficulty("difficulty=hard"), Is.EqualTo(StrikeDifficulty.Veteran));
            Assert.That(CoopRules.Difficulty(null), Is.EqualTo(StrikeDifficulty.Veteran));

            // What the controller composes for a room: the ships, the difficulty and the number of missions.
            string options = $"{CoopRules.LivesKey}=5;{CoopRules.DifficultyKey}=0;{CoopRules.MissionsKey}=22";
            Assert.That(CoopRules.Difficulty(options), Is.EqualTo(StrikeDifficulty.Rookie));
            Assert.That(CoopRules.Lives(options), Is.EqualTo(5), "The ships of a field mission are still there.");

            Assert.That(CoopRules.DifficultyChoices.Length, Is.EqualTo(CoopRules.DifficultyLabels.Length), "A label for every choice of the lobby.");
            Assert.That(CoopRules.DifficultyChoices[(int)CoopRules.DefaultDifficulty], Is.EqualTo(((int)StrikeDifficulty.Veteran).ToString()),
                "The lobby starts on Veteran.");
            for (int i = 0; i < CoopRules.DifficultyChoices.Length; i++)
            {
                Assert.That(CoopRules.Difficulty($"{CoopRules.DifficultyKey}={CoopRules.DifficultyChoices[i]}"), Is.EqualTo((StrikeDifficulty)i),
                    "Every choice of the lobby is the difficulty of its number.");
                Assert.That(CoopRules.DifficultyTitle((StrikeDifficulty)i), Is.EqualTo(CoopRules.DifficultyLabels[i]));
            }
        }

        [Test]
        public void ABusyUpdateIsSplitForTheServer()
        {
            Assert.That(BodyCodec.BatchCount(0), Is.Zero);
            Assert.That(BodyCodec.BatchCount(1), Is.EqualTo(1));
            Assert.That(BodyCodec.BatchCount(BodyCodec.MaxBatch), Is.EqualTo(1));
            Assert.That(BodyCodec.BatchCount(BodyCodec.MaxBatch + 1), Is.EqualTo(2));
            Assert.That(BodyCodec.MaxBatch, Is.EqualTo(512), "The server rejects a longer list.");

            var items = new List<int>();
            for (int i = 0; i < 1300; i++)
            {
                items.Add(i);
            }
            var joined = new List<int>();
            for (int call = 0; call < BodyCodec.BatchCount(items.Count); call++)
            {
                List<int> batch = BodyCodec.Batch(items, call);
                Assert.That(batch.Count, Is.LessThanOrEqualTo(BodyCodec.MaxBatch));
                joined.AddRange(batch);
            }
            Assert.That(joined, Is.EqualTo(items), "Every item goes once, in order.");
            Assert.That(BodyCodec.Batch(items, 3), Is.Empty, "A call past the end carries nothing.");
            Assert.That(BodyCodec.Batch(new List<int>(), 0), Is.Empty);
            Assert.That(BodyCodec.Batch<int>(null, 0), Is.Empty);
        }

        [Test]
        public void TheLobbyNumbersEachKindOfMissionOnItsOwn()
        {
            AsteroidsLevel field = objects.Asset<AsteroidsLevel>();
            field.objective = LevelObjective.ClearWaves;
            Name(field, "Debris Belt");
            AsteroidsLevel endless = objects.Asset<AsteroidsLevel>();
            endless.objective = LevelObjective.Endless;
            Name(endless, "Deep Space");
            StrikeLevel strike = objects.Asset<StrikeLevel>();
            Name(strike, "Dust Devil");

            Assert.That(CoopRules.LevelTitle(field, 3), Is.EqualTo("3. Debris Belt"));
            Assert.That(CoopRules.LevelTitle(endless, 12), Is.EqualTo("Deep Space (endless)"));
            Assert.That(CoopRules.LevelTitle(strike, 1), Is.EqualTo("Strike 1. Dust Devil"));
        }

        private static void Name(AsteroidsLevel level, string title)
        {
            var serialized = new SerializedObject(level);
            SerializedProperty property = serialized.FindProperty("<Title>k__BackingField");
            Assert.IsNotNull(property, "GameLevel keeps its title in a serialized backing field.");
            property.stringValue = title;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
