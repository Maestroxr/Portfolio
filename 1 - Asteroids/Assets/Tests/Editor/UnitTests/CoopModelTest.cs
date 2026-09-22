using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>The plain rules behind a mission shared with other pilots: net ids, what travels in a row or a pose, distances on a wrapping field, room options.</summary>
    public class CoopModelTest
    {
        [Test]
        public void TheSimulatorCountsNetIdsFromOne()
        {
            var registry = new BodyRegistry<object>();
            var rock = new object();
            var mine = new object();
            Assert.That(registry.Register(rock), Is.EqualTo(1u));
            Assert.That(registry.Register(mine), Is.EqualTo(2u));
            Assert.That(registry.Register(rock), Is.EqualTo(1u), "A body keeps its id.");
            Assert.That(registry.Count, Is.EqualTo(2));
            Assert.IsTrue(registry.TryGet(2u, out object found));
            Assert.AreSame(mine, found);
            Assert.IsTrue(registry.TryGetId(rock, out uint id));
            Assert.That(id, Is.EqualTo(1u));
            Assert.That(registry.Register(null), Is.Zero, "Nothing is not a body.");
        }

        [Test]
        public void ABodyThatLeftIsForgottenAndItsIdIsNotUsedAgain()
        {
            var registry = new BodyRegistry<object>();
            var rock = new object();
            registry.Register(rock);
            Assert.IsTrue(registry.Remove(rock, out uint id));
            Assert.That(id, Is.EqualTo(1u));
            Assert.IsFalse(registry.Remove(rock, out _), "It is gone already.");
            Assert.IsFalse(registry.TryGet(1u, out _));
            Assert.That(registry.Register(new object()), Is.EqualTo(2u), "A pooled body that comes back is another body to the others.");
            registry.Clear();
            Assert.That(registry.Count, Is.Zero);
            Assert.That(registry.Register(new object()), Is.EqualTo(1u), "Another mission counts from one again.");
        }

        [Test]
        public void TheOthersBindTheIdsTheyAreTold()
        {
            var registry = new BodyRegistry<object>();
            var puppet = new object();
            Assert.IsTrue(registry.Bind(7u, puppet));
            Assert.IsTrue(registry.Contains(7u));
            Assert.IsFalse(registry.Bind(7u, new object()), "The id is taken.");
            Assert.IsFalse(registry.Bind(8u, puppet), "The puppet has an id.");
            Assert.IsFalse(registry.Bind(0u, new object()), "Zero is no id.");
            Assert.IsTrue(registry.Remove(7u, out object removed));
            Assert.AreSame(puppet, removed);
            Assert.IsFalse(registry.TryGetId(puppet, out _));
        }

        [Test]
        public void AnAsteroidTravelsAsKindSizeAndShape()
        {
            foreach (AsteroidKind kind in System.Enum.GetValues(typeof(AsteroidKind)))
            {
                foreach (AsteroidSize size in System.Enum.GetValues(typeof(AsteroidSize)))
                {
                    int variant = BodyCodec.PackAsteroid(kind, size, 5);
                    BodyCodec.UnpackAsteroid(variant, out AsteroidKind unpackedKind, out AsteroidSize unpackedSize, out int shape);
                    Assert.That(unpackedKind, Is.EqualTo(kind));
                    Assert.That(unpackedSize, Is.EqualTo(size));
                    Assert.That(shape, Is.EqualTo(5));
                }
            }
            BodyCodec.UnpackAsteroid(BodyCodec.PackAsteroid(AsteroidKind.Ice, AsteroidSize.Small, -1), out _, out _, out int none);
            Assert.That(none, Is.Zero, "A shape that was never picked is the first one.");
        }

        [Test]
        public void TheFlagsOfABossCarryItsPhase()
        {
            BodyFlags flags = BodyCodec.WithPhase(BodyFlags.Shielded | BodyFlags.Entering, 2);
            Assert.That(BodyCodec.Phase(flags), Is.EqualTo(2));
            Assert.IsTrue(BodyCodec.Has(flags, BodyFlags.Shielded));
            Assert.IsTrue(BodyCodec.Has(flags, BodyFlags.Entering));
            Assert.IsFalse(BodyCodec.Has(flags, BodyFlags.Dying));
            flags = BodyCodec.WithPhase(flags, 1);
            Assert.That(BodyCodec.Phase(flags), Is.EqualTo(1), "A phase replaces the one before.");
            Assert.That(BodyCodec.Phase(BodyFlags.Armed), Is.Zero);
        }

        [Test]
        public void ATintTravelsAsThreeBytes()
        {
            Color tint = BodyCodec.UnpackColor(BodyCodec.PackColor(new Color(1f, 0.5f, 0.2f)));
            Assert.That(tint.r, Is.EqualTo(1f).Within(0.01f));
            Assert.That(tint.g, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(tint.b, Is.EqualTo(0.2f).Within(0.01f));
            Assert.That(BodyCodec.PackColor(new Color(3f, -1f, 0f)), Is.EqualTo(0xFF0000u), "Brighter than white is clamped.");
        }

        [Test]
        public void AShipPoseSurvivesTheTrip()
        {
            var pose = new ShipPose
            {
                Alive = true, Thrusting = true, Dashing = false, Invulnerable = true, Magnet = true, Drones = false,
                Hull = 3, Health = 0.42f, Shield = 1f
            };
            ShipPose unpacked = ShipPose.Unpack(pose.PackState(), pose.PackValue());
            Assert.IsTrue(unpacked.Alive);
            Assert.IsTrue(unpacked.Thrusting);
            Assert.IsFalse(unpacked.Dashing);
            Assert.IsTrue(unpacked.Invulnerable);
            Assert.IsTrue(unpacked.Magnet);
            Assert.IsFalse(unpacked.Drones);
            Assert.That(unpacked.Hull, Is.EqualTo(3));
            Assert.That(unpacked.Health, Is.EqualTo(0.42f).Within(0.006f));
            Assert.That(unpacked.Shield, Is.EqualTo(1f).Within(0.006f));
        }

        [Test]
        public void HullAndShieldDoNotChangeTheStateOfAPose()
        {
            // A change of the state is sent at once, so what changes all the time must not be part of it.
            var full = new ShipPose { Alive = true, Health = 1f, Shield = 1f };
            var hurt = new ShipPose { Alive = true, Health = 0.3f, Shield = 0f };
            Assert.That(hurt.PackState(), Is.EqualTo(full.PackState()));
            Assert.That(hurt.PackValue(), Is.Not.EqualTo(full.PackValue()));
            Assert.That(default(ShipPose).PackState(), Is.Zero, "A ship that is down reports no state.");
        }

        [Test]
        public void TheShortWayMayLeadAcrossAnEdge()
        {
            var period = new Vector2(36f, 20f);
            Vector2 across = FieldMath.Delta(new Vector2(17f, 0f), new Vector2(-17f, 0f), period);
            Assert.That(across.x, Is.EqualTo(2f).Within(0.001f), "Two meters across the right edge, not 34 back through the field.");
            Vector2 inside = FieldMath.Delta(new Vector2(-3f, 2f), new Vector2(4f, -1f), period);
            Assert.That(inside.x, Is.EqualTo(7f).Within(0.001f));
            Assert.That(inside.y, Is.EqualTo(-3f).Within(0.001f));
            Vector2 open = FieldMath.Delta(new Vector2(17f, 9f), new Vector2(-17f, -9f), new Vector2(36f, 0f));
            Assert.That(open.y, Is.EqualTo(-18f).Within(0.001f), "An axis without a period does not wrap.");
        }

        [Test]
        public void TheNearestShipMayBeOnTheOtherSideOfTheEdge()
        {
            var period = new Vector2(36f, 20f);
            var ships = new List<Vector2> { new Vector2(0f, 0f), new Vector2(-16f, 8f) };
            Assert.That(FieldMath.Nearest(ships, new Vector2(17f, 9f), period), Is.EqualTo(1), "Three meters across the corner beats seventeen to the middle.");
            Assert.That(FieldMath.Nearest(ships, new Vector2(2f, -1f), period), Is.EqualTo(0));
            Assert.That(FieldMath.Nearest(new List<Vector2>(), Vector2.zero, period), Is.EqualTo(-1));
            Assert.That(FieldMath.Nearest(null, Vector2.zero, period), Is.EqualTo(-1));
        }

        [Test]
        public void PilotsStartAroundTheMiddle()
        {
            Assert.That(FieldMath.StartPoint(0, 1, 3f), Is.EqualTo(Vector2.zero), "One pilot starts in the middle.");
            Vector2 first = FieldMath.StartPoint(0, 2, 3f);
            Vector2 second = FieldMath.StartPoint(1, 2, 3f);
            Assert.That(first.y, Is.EqualTo(3f).Within(0.001f), "The first pilot starts on top.");
            Assert.That((first + second).magnitude, Is.LessThan(0.001f), "Two pilots face each other across the middle.");
            for (int slot = 0; slot < 4; slot++)
            {
                Assert.That(FieldMath.StartPoint(slot, 4, 3f).magnitude, Is.EqualTo(3f).Within(0.001f));
            }
        }

        [Test]
        public void TheShipsOfARoomComeFromItsOptions()
        {
            Assert.That(CoopRules.Lives("5"), Is.EqualTo(5));
            Assert.That(CoopRules.Lives(null), Is.EqualTo(CoopRules.DefaultLives));
            Assert.That(CoopRules.Lives("many"), Is.EqualTo(CoopRules.DefaultLives));
            Assert.That(CoopRules.Lives("0"), Is.EqualTo(CoopRules.MinLives));
            Assert.That(CoopRules.Lives("99"), Is.EqualTo(CoopRules.MaxLives));
            foreach (string choice in CoopRules.LivesChoices)
            {
                Assert.That(CoopRules.Lives(choice).ToString(), Is.EqualTo(choice), "Every choice of the lobby is a number of ships the rules accept.");
            }
        }

        [Test]
        public void EverySeatHasAColourOfItsOwn()
        {
            var colours = new HashSet<Color>();
            for (int seat = 0; seat < CoopRules.MaxPilots; seat++)
            {
                Assert.IsTrue(colours.Add(CoopRules.SeatColor(seat)), $"Seat {seat} shares its colour.");
            }
            Assert.That(CoopRules.SeatColor(CoopRules.MaxPilots), Is.EqualTo(CoopRules.SeatColor(0)), "Seats beyond the colours start over.");
            Assert.That(CoopRules.SeatColor(-1), Is.EqualTo(CoopRules.SeatColor(CoopRules.MaxPilots - 1)));
        }
    }
}
