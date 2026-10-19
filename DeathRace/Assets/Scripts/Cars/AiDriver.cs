using System;
using System.Collections.Generic;
using Gamebox.Racing;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>What a computer driver knows of another car this frame.</summary>
    public struct AiOther
    {
        /// <summary>The car's seat (to remember which side it is being passed on).</summary>
        public int Id;
        public Vector2 Position;
        public Vector2 Velocity;
        /// <summary>Where it is along the track's centre line and how far off it (+ = left).</summary>
        public float Distance;
        public float Offset;
        public float HalfWidth;
        public bool Wrecked;
        public bool Finished;
        public float Health01;
    }

    /// <summary>What a computer driver sees and knows this frame: the input of its decisions.</summary>
    public struct AiSense
    {
        public Vector2 Position;
        /// <summary>Unit vector of the car's nose.</summary>
        public Vector2 Forward;
        public Vector2 Velocity;
        /// <summary>Speed along the nose (negative in reverse).</summary>
        public float Speed;
        public float Distance;
        public float Offset;
        /// <summary>The road's half width here.</summary>
        public float HalfWidth;
        public float CarHalfWidth;
        public float CarHalfLength;
        /// <summary>The green light was given (the race runs).</summary>
        public bool Racing;
        public bool Countdown;
        public int Ammo;
        public int Mines;
        public int Rockets;
        public int NitroCharges;
        public bool NitroActive;
        /// <summary>A barrier right in front of the nose.</summary>
        public bool WallAhead;
        /// <summary>The rubber band: below 1 eases off (far ahead of the people), above 1 pushes (far behind).</summary>
        public float Pace;
        /// <summary>The weapons answer (not after the finish).</summary>
        public bool Armed;
        public List<AiOther> Others;
        /// <summary>Mines and other hazards on the road ahead.</summary>
        public List<Vector2> Hazards;
    }

    /// <summary>
    /// The computer's driver. It follows the track's racing line (a blend of it towards the middle of the road the less
    /// skilled it is) with pure pursuit steering, and holds the speed its <see cref="SpeedProfile"/> allows, so it brakes
    /// for the bends it sees coming. It passes slower cars on the side with room, keeps off cars beside it, steers round
    /// wrecks and mines, and when it is stuck against a barrier or facing the wrong way it backs out and turns. It fights:
    /// the machine gun when a car sits in a narrow cone ahead, a mine when one is close behind, a rocket at a car ahead,
    /// nitro on the straights. Skill (0 to 1) scales the line, the cornering and braking, the reaction time and the aim,
    /// and a light rubber band keeps the people in the race without anyone noticing.
    ///
    /// <see cref="Think"/> works on an <see cref="AiSense"/> alone, so its decisions can be tested on plain geometry;
    /// <see cref="Drive"/> gathers that sense from the car and its race.
    /// </summary>
    public sealed class AiDriver : ICarDriver
    {
        /// <summary>Slower than this while trying to go is being stuck.</summary>
        public const float StuckSpeed = 1.2f;

        /// <summary>No progress along the track for this long and the car is put back on the road.</summary>
        public const float HopelessSeconds = 9f;

        /// <summary>The look-ahead of the steering: metres, plus seconds of speed.</summary>
        private const float LookBase = 6f;
        private const float LookTime = 0.42f;

        private const float NitroTap = 0.12f;
        private const float HazardScanInterval = 0.15f;
        private const int HazardMask = 1 << DeathRaceLayers.Hazard;

        private readonly System.Random random;
        private readonly Dictionary<int, float> passSides = new Dictionary<int, float>();
        private readonly List<AiOther> others = new List<AiOther>();
        private readonly List<Vector2> hazards = new List<Vector2>();
        private readonly Collider[] hazardHits = new Collider[16];

        // What skill makes of the driver.
        private readonly float gripShare;
        private readonly float brakeShare;
        private readonly float topShare;
        private readonly float lineShare;
        private readonly float reaction;
        private readonly float aimTolerance;
        private readonly float wanderAmplitude;
        private readonly float weaponPatience;
        private readonly float noticeChance;
        private readonly float wanderPhase;

        private Race preparedFor;
        private RacingLine line;
        private SpeedProfile profile;
        private CarHandling handling;
        private Func<float, float> halfWidthAt;
        private RacePath path;

        private float clock;
        private float launchDelay = -1f;
        private float stuckTime;
        private float reverseTime;
        private float reverseSteer;
        private float reversedFor;
        private float targetSeen;
        private bool engaging;
        private bool declined;
        private float burstEnd;
        private float pauseEnd;
        private float lastInSights = -10f;
        private float racingClock;
        private float chasedFor;
        private float mineReady;
        private float rocketReady;
        private float nitroReady;
        private float nitroUntil;
        private float lastThrottle;
        private float bestProgress = float.MinValue;
        private float progressClock;
        private float hazardClock;
        private bool respawnWanted;
        private string pendingStuck;

        /// <param name="skill">0 (a beginner) to 1 (the champion).</param>
        /// <param name="seed">Seeds the driver's own whims (when it chooses to fire, how it wanders).</param>
        public AiDriver(float skill, int seed)
        {
            Skill = Mathf.Clamp01(skill);
            random = new System.Random(seed);
            float s = Skill;
            gripShare = Mathf.Lerp(0.6f, 0.97f, s);
            brakeShare = Mathf.Lerp(0.5f, 0.93f, s);
            topShare = Mathf.Lerp(0.82f, 1f, s);
            lineShare = Mathf.Lerp(0.3f, 1f, s);
            reaction = Mathf.Lerp(0.75f, 0.12f, s);
            aimTolerance = Mathf.Lerp(2.6f, 0.7f, s);
            wanderAmplitude = Mathf.Lerp(1.6f, 0.15f, s);
            weaponPatience = Mathf.Lerp(1.8f, 0.8f, s);
            noticeChance = Mathf.Lerp(0.3f, 0.95f, s);
            wanderPhase = (float)random.NextDouble() * 100f;
        }

        public float Skill { get; }

        /// <summary>After the finish: an easy lap at a cruise, no weapons, no hurry.</summary>
        public bool Cruise { get; set; }

        /// <summary>How often the driver got stuck and had to back out.</summary>
        public int StuckCount { get; private set; }

        /// <summary>How often the driver gave up and was put back on the road.</summary>
        public int Respawns { get; private set; }

        /// <summary>What the driver is doing (for development logs).</summary>
        public string State { get; private set; } = "grid";

        /// <summary>The driver got stuck (the car, and what it does about it).</summary>
        public event Action<Car, string> Stuck;

        /// <summary>
        /// Gets ready to drive <paramref name="racingLine"/> in a car that handles like <paramref name="carHandling"/> on a
        /// road of <paramref name="roadHalfWidth"/> (a half width at a distance). <see cref="Drive"/> does it by itself.
        /// </summary>
        public void Prepare(RacingLine racingLine, CarHandling carHandling, Func<float, float> roadHalfWidth)
        {
            path = racingLine.Path;
            line = racingLine.Blend(lineShare);
            handling = carHandling;
            halfWidthAt = roadHalfWidth;
            profile = new SpeedProfile(line, handling, gripShare, brakeShare);
        }

        /// <summary>The speed the driver aims for at a distance along the path (its profile, for tests and tuning).</summary>
        public float PlannedSpeed(float distance)
        {
            return profile != null ? Mathf.Min(handling.TopSpeed * topShare, profile.SpeedAt(distance)) : 0f;
        }

        /// <summary>The lap time the driver plans for (its line, its speeds, its share of the top speed): for tests and tuning.</summary>
        public float PlannedLapTime()
        {
            return profile != null ? profile.LapTime(handling, topShare) : 0f;
        }

        public CarInput Drive(Car car, float deltaTime)
        {
            Race race = car.Race;
            if (race == null || race.Track == null || race.Track.Path == null || car.Body == null)
            {
                return CarInput.Idle;
            }
            if (preparedFor != race || profile == null)
            {
                preparedFor = race;
                Track track = race.Track;
                Prepare(race.Line ?? RacingLine.Build(track.Path, track.HalfWidthAt), car.Body.Handling, track.HalfWidthAt);
            }
            AiSense sense = Sense(car, deltaTime);
            CarInput input = Think(sense, deltaTime);
            if (pendingStuck != null)
            {
                Stuck?.Invoke(car, pendingStuck);
                pendingStuck = null;
            }
            if (respawnWanted)
            {
                respawnWanted = false;
                PutBack(car);
            }
            return input;
        }

        /// <summary>The decisions of one frame from what the driver sees.</summary>
        public CarInput Think(in AiSense sense, float deltaTime)
        {
            clock += deltaTime;
            var input = new CarInput();
            if (!sense.Racing)
            {
                State = sense.Countdown ? "countdown" : "grid";
                // Revving on the grid, a little restless.
                input.Throttle = sense.Countdown ? 0.25f + 0.35f * Mathf.Abs(Mathf.Sin(clock * (2.2f + wanderPhase % 1f))) : 0f;
                launchDelay = Mathf.Lerp(0.35f, 0.04f, Skill) * (0.6f + 0.8f * (float)random.NextDouble());
                return input;
            }
            if (launchDelay > 0f)
            {
                launchDelay -= deltaTime;
                input.Throttle = 0.4f;
                return input;
            }
            WatchProgress(sense, deltaTime);
            if (racingClock <= 0f)
            {
                // The green light: the weapons' first chances come a while into the race, each driver at its own moment.
                mineReady = clock + 8f + (float)random.NextDouble() * 8f;
                rocketReady = clock + 6f + (float)random.NextDouble() * 8f;
                nitroReady = clock + 1f + (float)random.NextDouble() * 5f;
            }
            racingClock += deltaTime;

            float speed = sense.Speed;
            float look = Mathf.Clamp(LookBase + Mathf.Max(0f, speed) * LookTime, 7f, 28f);
            float aheadDistance = sense.Distance + look;
            float plannedOffset = PlanOffset(sense, aheadDistance, out float followSpeed);
            Vector2 aim = path.PointAt(aheadDistance) + path.NormalAt(aheadDistance) * plannedOffset;
            Vector2 toAim = aim - sense.Position;
            float alpha = AngleRight(sense.Forward, toAim);

            if (Recovering(sense, alpha, deltaTime, ref input))
            {
                lastThrottle = input.Throttle;
                return input;
            }

            // Steering: pure pursuit, the yaw rate that puts the car on the aim point, as a share of what the wheel can do.
            float steer;
            float maxYaw = handling.MaxYawRate(speed);
            if (speed > 3f && maxYaw > 0.05f)
            {
                float desiredYaw = 2f * Mathf.Sin(alpha) * speed / Mathf.Max(1f, toAim.magnitude);
                steer = desiredYaw / maxYaw;
            }
            else
            {
                steer = alpha * 2.5f;
            }
            steer = Mathf.Clamp(steer, -1f, 1f);

            // Speed: what the profile allows a little ahead (the time it takes to react), eased by the rubber band.
            bool nitro = sense.NitroActive;
            float top = handling.TopSpeedWith(nitro) * topShare * (Cruise ? 0.62f : 1f);
            float lead = Mathf.Max(0f, speed) * 0.15f;
            float allowed = profile.SpeedAt(sense.Distance + lead) * (Cruise ? 0.8f : Mathf.Clamp(sense.Pace, 0.9f, 1.05f));
            float target = Mathf.Min(top, allowed, followSpeed);
            float heading = Mathf.Abs(alpha) * Mathf.Rad2Deg;
            if (heading > 100f)
            {
                // Facing the wrong way: slow down, then turn round as tight as the car goes (backing up if a barrier is in
                // the way: steering flips in reverse).
                State = "turning";
                bool backUp = sense.WallAhead && speed < 4f;
                input.Throttle = speed > 9f ? -0.7f : backUp ? -1f : 0.55f;
                input.Steer = backUp ? -Mathf.Sign(alpha) : Mathf.Sign(alpha);
                lastThrottle = input.Throttle;
                return input;
            }
            if (heading > 45f)
            {
                target = Mathf.Min(target, 13f);
            }
            float error = target - speed;
            float throttle;
            if (error > 0.3f)
            {
                throttle = 1f;
            }
            else if (error > -0.8f)
            {
                throttle = Mathf.Clamp01(0.35f + error);
            }
            else
            {
                throttle = -Mathf.Clamp((-error - 0.8f) / 4f + 0.2f, 0.2f, 1f);
            }
            input.Throttle = throttle;
            input.Steer = steer;
            State = Cruise ? "cruise" : followSpeed < SpeedProfile.Unlimited ? "following" : throttle < 0f ? "braking" : "racing";
            lastThrottle = throttle;

            // A clean start: nobody shoots in the first moments off the grid (the cars are nose to tail there).
            if (sense.Armed && !Cruise && racingClock >= Mathf.Lerp(3.5f, 2f, Skill))
            {
                Fight(sense, deltaTime, steer, top, ref input);
            }
            return input;
        }

        /// <summary>
        /// Where across the road to aim at <paramref name="aheadDistance"/>: the line (with a beginner's wander), moved
        /// aside to pass a slower car or a wreck on the side with room, away from a car alongside, round a mine, and a
        /// little towards a car worth shooting. <paramref name="followSpeed"/> is the speed of a car that cannot be passed.
        /// </summary>
        private float PlanOffset(in AiSense sense, float aheadDistance, out float followSpeed)
        {
            followSpeed = SpeedProfile.Unlimited;
            float half = halfWidthAt != null ? halfWidthAt(aheadDistance) : sense.HalfWidth;
            float limit = Mathf.Max(0.5f, half - sense.CarHalfWidth - 0.7f);
            float wander = wanderAmplitude * (Mathf.Sin(clock * 0.37f + wanderPhase) + 0.5f * Mathf.Sin(clock * 0.93f + wanderPhase * 1.7f));
            float offset = line.OffsetAt(aheadDistance) + wander;
            float speed = Mathf.Max(0f, sense.Speed);
            float avoidRange = Mathf.Clamp(8f + speed * 0.9f, 12f, 42f);

            if (sense.Others != null && sense.Armed && !Cruise && sense.Ammo > 0)
            {
                // Lining up a shot: drift towards a car a little way ahead that is not being caught (one that is, is passed).
                foreach (AiOther other in sense.Others)
                {
                    float delta = path.Delta(sense.Distance, other.Distance);
                    if (other.Wrecked || other.Finished || delta < 10f || delta > 40f || Mathf.Abs(other.Offset - offset) > 6f)
                    {
                        continue;
                    }
                    float closing = speed - Vector2.Dot(other.Velocity, path.TangentAt(other.Distance));
                    if (closing < 3f)
                    {
                        offset = Mathf.Lerp(offset, other.Offset, 0.45f * Skill * (1f - Mathf.Clamp01((delta - 25f) / 15f)));
                        break;
                    }
                }
            }

            if (sense.Others != null)
            {
                float nearest = float.MaxValue;
                foreach (AiOther other in sense.Others)
                {
                    float delta = path.Delta(sense.Distance, other.Distance);
                    float clearance = sense.CarHalfWidth + other.HalfWidth + 1.3f;
                    float apart = sense.Offset - other.Offset;
                    if (Mathf.Abs(delta) < sense.CarHalfLength * 2f + 1f && Mathf.Abs(apart) < clearance + 0.5f)
                    {
                        // Alongside: keep off its doors.
                        offset += Mathf.Sign(apart == 0f ? 1f : apart) * (clearance + 0.5f - Mathf.Abs(apart)) * 0.8f;
                        continue;
                    }
                    if (delta <= 0f || delta > avoidRange)
                    {
                        continue;
                    }
                    float otherSpeed = other.Wrecked ? 0f : Vector2.Dot(other.Velocity, path.TangentAt(other.Distance));
                    float closing = speed - otherSpeed;
                    if (closing < 0.5f && delta > 9f)
                    {
                        continue;
                    }
                    if (Mathf.Abs(offset - other.Offset) >= clearance || delta >= nearest)
                    {
                        continue;
                    }
                    nearest = delta;
                    float left = other.Offset + clearance;
                    float right = other.Offset - clearance;
                    bool leftFits = left <= limit;
                    bool rightFits = right >= -limit;
                    float side;
                    if (!passSides.TryGetValue(other.Id, out side) || (side > 0f && !leftFits) || (side < 0f && !rightFits))
                    {
                        side = leftFits && (!rightFits || Mathf.Abs(left - offset) <= Mathf.Abs(right - offset)) ? 1f : rightFits ? -1f : 0f;
                        passSides[other.Id] = side;
                    }
                    float weight = 1f - Mathf.Clamp01((delta - 8f) / Mathf.Max(1f, avoidRange - 8f));
                    if (side == 0f)
                    {
                        // No way past: sit behind it (and shoot it).
                        if (delta < 14f && !other.Wrecked)
                        {
                            followSpeed = Mathf.Max(4f, otherSpeed - (delta < 7f ? 2f : 0f));
                        }
                        else if (other.Wrecked && delta < 18f)
                        {
                            followSpeed = 6f;
                        }
                        continue;
                    }
                    offset = Mathf.Lerp(offset, side > 0f ? left : right, weight);
                }
            }

            if (sense.Hazards != null)
            {
                foreach (Vector2 hazard in sense.Hazards)
                {
                    float hazardDistance = path.Project(hazard, sense.Distance, 50f);
                    float delta = path.Delta(sense.Distance, hazardDistance);
                    if (delta <= 1f || delta > avoidRange || !Notices(hazard))
                    {
                        continue;
                    }
                    float lateral = path.SignedOffset(hazard, hazardDistance);
                    float clearance = sense.CarHalfWidth + 2.2f;
                    if (Mathf.Abs(offset - lateral) < clearance)
                    {
                        float left = lateral + clearance;
                        float right = lateral - clearance;
                        float target = left <= limit && (Mathf.Abs(left - offset) < Mathf.Abs(right - offset) || right < -limit) ? left : right;
                        offset = Mathf.Lerp(offset, target, 1f - Mathf.Clamp01((delta - 6f) / Mathf.Max(1f, avoidRange - 6f)));
                    }
                }
            }
            return Mathf.Clamp(offset, -limit, limit);
        }

        /// <summary>Stuck against something, or backing out of it: true while the recovery drives.</summary>
        private bool Recovering(in AiSense sense, float alpha, float deltaTime, ref CarInput input)
        {
            if (reverseTime > 0f)
            {
                reverseTime -= deltaTime;
                reversedFor += deltaTime;
                bool pointed = Mathf.Abs(alpha) < 25f * Mathf.Deg2Rad && reversedFor > 0.6f;
                bool wedged = reversedFor > 0.8f && sense.Speed > -0.6f;
                if (reverseTime <= 0f || pointed || wedged)
                {
                    reverseTime = 0f;
                    stuckTime = 0f;
                }
                else
                {
                    State = "reversing";
                    input.Throttle = -1f;
                    input.Steer = reverseSteer;
                    return true;
                }
            }
            bool trying = lastThrottle > 0.3f;
            if (trying && Mathf.Abs(sense.Speed) < StuckSpeed)
            {
                stuckTime += deltaTime * (sense.WallAhead ? 1.6f : 1f);
            }
            else
            {
                stuckTime = Mathf.Max(0f, stuckTime - deltaTime * 2f);
            }
            float patience = Mathf.Lerp(1.4f, 0.7f, Skill);
            if (stuckTime < patience)
            {
                return false;
            }
            // Back out, turning the nose towards the road: steering flips in reverse.
            StuckCount++;
            stuckTime = 0f;
            reversedFor = 0f;
            reverseTime = 1.1f + (float)random.NextDouble() * 0.5f;
            float towards = Mathf.Abs(alpha) > 0.05f ? Mathf.Sign(alpha) : (random.Next(2) == 0 ? 1f : -1f);
            reverseSteer = -towards;
            State = "reversing";
            input.Throttle = -1f;
            input.Steer = reverseSteer;
            pendingStuck = "backing out";
            return true;
        }

        /// <summary>The guns, the mines, the rockets and the nitro.</summary>
        private void Fight(in AiSense sense, float deltaTime, float steer, float top, ref CarInput input)
        {
            Vector2 right = new Vector2(sense.Forward.y, -sense.Forward.x);
            Vector2 muzzle = sense.Position + sense.Forward * sense.CarHalfLength;
            float gunRange = CombatRules.GunRange * Mathf.Lerp(0.45f, 0.85f, Skill);
            bool inSights = false;
            bool rocketShot = false;
            bool mineShot = false;
            if (sense.Others != null)
            {
                foreach (AiOther other in sense.Others)
                {
                    if (other.Wrecked || other.Finished)
                    {
                        continue;
                    }
                    Vector2 to = other.Position - muzzle;
                    float distance = to.magnitude;
                    if (distance < 0.5f)
                    {
                        continue;
                    }
                    float angle = Vector2.Angle(sense.Forward, to);
                    float cone = Mathf.Atan((other.HalfWidth * 0.8f + aimTolerance) / distance) * Mathf.Rad2Deg;
                    if (distance < gunRange && angle < cone)
                    {
                        inSights = true;
                    }
                    float rocketCone = Mathf.Lerp(13f, 7f, Skill);
                    if (distance > 14f && distance < 65f && angle < rocketCone && (other.Health01 < 0.45f || Skill > 0.35f))
                    {
                        rocketShot = true;
                    }
                    Vector2 rel = other.Position - sense.Position;
                    float along = Vector2.Dot(rel, sense.Forward);
                    float side = Vector2.Dot(rel, right);
                    if (along < -2.5f && along > -(9f + Mathf.Max(0f, sense.Speed) * 0.3f) && Mathf.Abs(side) < 2.4f)
                    {
                        mineShot = true;
                    }
                }
            }

            // The gun: a target has to sit in the sights for the reaction time; a beginner lets some chances go.
            if (inSights)
            {
                targetSeen += deltaTime;
                if (!engaging && !declined && targetSeen >= reaction)
                {
                    engaging = random.NextDouble() < Mathf.Lerp(0.55f, 1f, Skill);
                    declined = !engaging;
                }
            }
            else
            {
                targetSeen = 0f;
                engaging = false;
                declined = false;
            }
            // Bursts with breaks between them (shorter breaks the better the driver), stopping soon after the target slips away.
            if (inSights)
            {
                lastInSights = clock;
            }
            if (engaging && sense.Ammo > 0 && clock >= pauseEnd)
            {
                float burst = Mathf.Lerp(0.35f, 0.8f, Skill) * (0.7f + 0.6f * (float)random.NextDouble());
                burstEnd = clock + burst;
                pauseEnd = burstEnd + Mathf.Lerp(1.8f, 0.6f, Skill) * (0.7f + 0.6f * (float)random.NextDouble());
            }
            input.Fire = sense.Ammo > 0 && clock < burstEnd && clock - lastInSights < 0.2f;

            // A mine for a car that has sat on this one's tail for a moment, not for any car that happens to be behind.
            chasedFor = mineShot ? chasedFor + deltaTime : 0f;
            if (chasedFor > 0.6f && sense.Mines > 0 && clock >= mineReady)
            {
                mineReady = clock + Mathf.Lerp(6f, 14f, (float)random.NextDouble()) * weaponPatience;
                input.DropMine = random.NextDouble() < Mathf.Lerp(0.5f, 1f, Skill);
            }
            if (rocketShot && sense.Rockets > 0 && clock >= rocketReady)
            {
                rocketReady = clock + Mathf.Lerp(5f, 12f, (float)random.NextDouble()) * weaponPatience;
                input.FireRocket = random.NextDouble() < Mathf.Lerp(0.5f, 1f, Skill);
            }

            // Nitro: on a straight long enough for a whole charge, when it is not already burning.
            if (clock < nitroUntil)
            {
                input.Nitro = true;
            }
            else if (sense.NitroCharges > 0 && !sense.NitroActive && clock >= nitroReady && Mathf.Abs(steer) < 0.35f && sense.Speed > top * 0.5f)
            {
                float burn = Mathf.Max(0f, sense.Speed) * 1.15f * CombatRules.NitroSeconds;
                float needed = handling.TopSpeedWith(true) * topShare * (sense.Pace > 1.01f ? 0.85f : 0.95f);
                if (profile.MinAhead(sense.Distance, burn) >= needed)
                {
                    nitroUntil = clock + NitroTap;
                    nitroReady = clock + CombatRules.NitroSeconds + Mathf.Lerp(7f, 2f, Skill) * (0.7f + 0.6f * (float)random.NextDouble());
                    input.Nitro = true;
                }
            }
        }

        /// <summary>Gives up on a car that has made no progress for a long time: it is put back on the road.</summary>
        private void WatchProgress(in AiSense sense, float deltaTime)
        {
            float progress = sense.Distance;
            if (bestProgress == float.MinValue)
            {
                bestProgress = progress;
            }
            float gained = path.Delta(bestProgress, progress);
            if (gained > 8f)
            {
                bestProgress = progress;
                progressClock = 0f;
                return;
            }
            progressClock += deltaTime;
            if (progressClock > HopelessSeconds)
            {
                progressClock = 0f;
                respawnWanted = true;
            }
        }

        /// <summary>The car went nowhere for too long: back on the road a little behind where it is, at rest.</summary>
        private void PutBack(Car car)
        {
            Track track = car.Race.Track;
            float distance = path.Project(car.PhysicsPosition2D, car.TrackDistance, 40f) - 4f;
            Pose pose = track.Respawn(distance);
            car.Body.Teleport(pose);
            Respawns++;
            reverseTime = 0f;
            stuckTime = 0f;
            bestProgress = distance;
            Stuck?.Invoke(car, "put back on the road");
        }

        /// <summary>Gathers what the driver sees from its car and the race.</summary>
        private AiSense Sense(Car car, float deltaTime)
        {
            Race race = car.Race;
            Track track = race.Track;
            Transform body = car.transform;
            Vector3 forward3 = body.forward;
            Vector2 forward = new Vector2(forward3.x, forward3.z);
            forward = forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector2.up;
            Vector3 velocity3 = car.Body.Velocity;
            Vector2 position = car.Position2D;
            float distance = car.TrackDistance;
            CarSpec spec = car.Loadout != null ? car.Loadout.Spec : CarSpecs.All[0];
            var sense = new AiSense
            {
                Position = position,
                Forward = forward,
                Velocity = new Vector2(velocity3.x, velocity3.z),
                Speed = car.Body.ForwardSpeed,
                Distance = distance,
                Offset = car.TrackOffset,
                HalfWidth = track.HalfWidthAt(distance),
                CarHalfWidth = spec.Size.x * 0.5f,
                CarHalfLength = spec.Size.z * 0.5f,
                Racing = race.IsRunning || race.Phase == RacePhase.Over,
                Countdown = race.Phase == RacePhase.Countdown,
                Armed = car.WeaponsLive,
                Pace = Pace(car),
                Others = others,
                Hazards = hazards
            };
            if (car.Weapons != null)
            {
                sense.Ammo = car.Weapons.Ammo;
                sense.Mines = car.Weapons.Mines;
                sense.Rockets = car.Weapons.Rockets;
                sense.NitroCharges = car.Weapons.NitroCharges;
                sense.NitroActive = car.Weapons.NitroActive;
            }
            if (Mathf.Abs(sense.Speed) < 8f)
            {
                Vector3 origin = body.position + Vector3.up * 0.7f;
                // The ray starts inside the car's own box, which a ray never hits: only a barrier or another car counts.
                sense.WallAhead = Physics.Raycast(origin, forward3, sense.CarHalfLength + 2.2f, DeathRaceLayers.ShotMask, QueryTriggerInteraction.Ignore);
            }

            others.Clear();
            foreach (Car other in race.Cars)
            {
                if (other == null || other == car)
                {
                    continue;
                }
                Vector3 otherVelocity = other.Body != null ? other.Body.Velocity : Vector3.zero;
                CarSpec otherSpec = other.Loadout != null ? other.Loadout.Spec : CarSpecs.All[0];
                // The room it takes across the road: a car turned sideways (a spin, a wreck) blocks its whole length.
                Vector2 across = path.NormalAt(other.TrackDistance);
                Vector2 nose = other.Forward2D;
                float halfAcross = Mathf.Abs(Vector2.Dot(across, nose)) * otherSpec.Size.z * 0.5f
                    + Mathf.Abs(Vector2.Dot(across, new Vector2(nose.y, -nose.x))) * otherSpec.Size.x * 0.5f;
                others.Add(new AiOther
                {
                    Id = other.Entrant != null ? other.Entrant.Seat : other.GetHashCode(),
                    Position = other.Position2D,
                    Velocity = new Vector2(otherVelocity.x, otherVelocity.z),
                    Distance = other.TrackDistance,
                    Offset = other.TrackOffset,
                    HalfWidth = halfAcross,
                    Wrecked = other.IsWrecked,
                    Finished = other.HasFinished,
                    Health01 = other.Health != null ? other.Health.Health01 : 1f
                });
            }

            hazardClock -= deltaTime;
            if (hazardClock <= 0f)
            {
                hazardClock = HazardScanInterval;
                hazards.Clear();
                float reach = Mathf.Clamp(10f + Mathf.Max(0f, sense.Speed) * 0.6f, 12f, 30f);
                Vector3 centre = body.position + forward3 * reach;
                int count = Physics.OverlapSphereNonAlloc(centre, reach + 2f, hazardHits, HazardMask, QueryTriggerInteraction.Collide);
                for (int i = 0; i < count; i++)
                {
                    Vector3 point = hazardHits[i].transform.position;
                    hazards.Add(new Vector2(point.x, point.z));
                }
            }
            return sense;
        }

        /// <summary>
        /// The rubber band, subtle: a computer car far ahead of the best person eases off a few per cent, one far behind
        /// pushes a few per cent. Nothing in a race without people.
        /// </summary>
        private static float Pace(Car car)
        {
            Race race = car.Race;
            if (race.Setup == null || race.Setup.Mode == RaceMode.Attract || car.Racer == null)
            {
                return 1f;
            }
            float best = float.MinValue;
            foreach (Car other in race.Cars)
            {
                if (other != null && other.IsHuman && !other.IsWrecked && other.Racer != null)
                {
                    best = Mathf.Max(best, other.Racer.Progress);
                }
            }
            return best == float.MinValue ? 1f : RubberBand(car.Racer.Progress - best);
        }

        /// <summary>The pace for a car <paramref name="gap"/> metres ahead (positive) of the best person.</summary>
        public static float RubberBand(float gap)
        {
            if (gap > 60f)
            {
                return 1f - Mathf.Min(0.07f, (gap - 60f) / 1300f);
            }
            if (gap < -60f)
            {
                return 1f + Mathf.Min(0.04f, (-gap - 60f) / 2000f);
            }
            return 1f;
        }

        /// <summary>Whether this driver sees a mine (a beginner misses some): the same answer for the same mine every frame.</summary>
        private bool Notices(Vector2 hazard)
        {
            int hash = Mathf.RoundToInt(hazard.x * 7.3f) * 73856093 ^ Mathf.RoundToInt(hazard.y * 7.3f) * 19349663 ^ (int)(wanderPhase * 1000f);
            return (hash & 0xffff) / 65535f < noticeChance;
        }

        /// <summary>The angle from <paramref name="forward"/> to <paramref name="to"/> in radians, positive to the right.</summary>
        public static float AngleRight(Vector2 forward, Vector2 to)
        {
            float cross = forward.x * to.y - forward.y * to.x;
            return -Mathf.Atan2(cross, Vector2.Dot(forward, to));
        }
    }
}
