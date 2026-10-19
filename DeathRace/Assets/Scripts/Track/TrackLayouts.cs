using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The twelve tracks of the game, in campaign order (the campaign's level i is track i). Names, lands and laps are the
    /// design's; the shapes were drawn for armed cars seen from above: fast straights for nitro and rockets, hairpins and
    /// chicanes where the field bunches up and the guns come out, a narrow gorge or two. The first nine are the
    /// Wasteland's (the Crucible, 8, is every league's Showdown arena); the last three came with the harder leagues and
    /// sit after it, so the indices saves keep never move. Every layout keeps the rules of
    /// <see cref="TrackGeometry.Problems"/> (the tests check them all).
    ///
    /// The control points are metres (x east, y north) in the order of travel; the centre line is a centripetal
    /// Catmull-Rom curve through them, so a run of points along a straight keeps it straight and points round an arc make
    /// a corner of that radius.
    /// </summary>
    public static class TrackLayouts
    {
        public static readonly TrackLayout[] All =
        {
            new TrackLayout
            {
                Name = "Dust Bowl",
                Description = "A wide, flowing loop through the dunes with one tight hairpin. Learn your car here.",
                Biome = Biome.Desert,
                Laps = 4,
                Width = 15f,
                Seed = 1101,
                StartDistance = 60f,
                PickupDistances = new[] { 208f, 353f, 563f, 706f, 888f },
                SetPieces = new[]
                {
                    new TrackLayout.SetPiece(SetPieceKind.Tyres, 497f, -1f)
                },
                Points = Points(
                    0, 0, 24.5f, 0, 49, 0, 73, 0, 97.5f, 0, 122, 0,
                    136.5f, 2, 149.5f, 7.5f, 161, 16, 169.5f, 27.5f, 175, 41, 177, 55,
                    177, 85, 178, 97, 181.5f, 109, 186.5f, 120, 191.5f, 131, 195, 143,
                    196, 155, 193.5f, 168.5f, 186.5f, 180.5f, 176, 189.5f, 150, 204.5f, 138, 214.5f,
                    130, 228, 127.5f, 243.5f, 127.5f, 273.5f, 124.5f, 285, 116, 293.5f, 104.5f, 296.5f,
                    93, 293.5f, 84.5f, 285, 81.5f, 273.5f, 81.5f, 243.5f, 79.5f, 232, 75, 221.5f,
                    67, 213, 49.5f, 198, 31.5f, 183, 13.5f, 168, 4.5f, 159, -3.5f, 149,
                    -10, 137.5f, -18.5f, 119, -25, 108, -33, 97.5f, -42, 88.5f, -52, 77,
                    -58.5f, 63, -60, 48, -57, 33, -49.5f, 19.5f, -38.5f, 9, -25, 2.5f,
                    -10, 0)
            },
            new TrackLayout
            {
                Name = "Scrapyard Loop",
                Description = "Four hard corners round the junkyard and a chicane between stacks of rusting containers.",
                Biome = Biome.Industrial,
                Laps = 4,
                Width = 14f,
                Seed = 2202,
                StartDistance = 50f,
                PickupDistances = new[] { 211f, 423f, 575f, 725f, 830f },
                SetPieces = new[]
                {
                    new TrackLayout.SetPiece(SetPieceKind.Tyres, 191f, 1f),
                    new TrackLayout.SetPiece(SetPieceKind.Containers, 261f, 1f),
                    new TrackLayout.SetPiece(SetPieceKind.Containers, 302f, -1f),
                    new TrackLayout.SetPiece(SetPieceKind.Containers, 344f, 1f),
                    new TrackLayout.SetPiece(SetPieceKind.Containers, 302f, 1f, 6f)
                },
                Points = Points(
                    0, 0, 28.5f, 0, 57, 0, 85.5f, 0, 114.5f, 0, 143, 0,
                    171.5f, 0, 181, -2, 189, -7.5f, 194.5f, -15.5f, 196.5f, -25, 196.5f, -45,
                    196.5f, -65, 197, -70, 199, -75, 202.5f, -79, 206.5f, -82.5f, 215, -87.5f,
                    222.5f, -94.5f, 225, -104.5f, 222.5f, -114.5f, 215, -122, 206.5f, -127, 202.5f, -130,
                    199, -134.5f, 197, -139, 196.5f, -144.5f, 196.5f, -165.5f, 192.5f, -180.5f, 181.5f, -191.5f,
                    166.5f, -195.5f, 136.5f, -195.5f, 106.5f, -195.5f, 93.5f, -197.5f, 81.5f, -202.5f, 69.5f, -207.5f,
                    56.5f, -209, 36.5f, -209, 16.5f, -209, 5, -207, -5, -200.5f, -22.5f, -182.5f,
                    -29, -173, -31.5f, -161.5f, -31.5f, -136.5f, -31.5f, -111.5f, -33.5f, -97.5f, -40.5f, -85.5f,
                    -47.5f, -73.5f, -50, -60, -50, -30, -46, -15, -35, -4, -20, 0)
            },
            new TrackLayout
            {
                Name = "Frostbite Pass",
                Description = "The longest straight in the land, then S-bends over an icy mountain pass.",
                Biome = Biome.Snow,
                Laps = 4,
                Width = 14f,
                Seed = 3303,
                StartDistance = 60f,
                PickupDistances = new[] { 160f, 294f, 470f, 588f, 696f, 828f },
                SetPieces = new[]
                {
                    new TrackLayout.SetPiece(SetPieceKind.Tyres, 270f, -1f)
                },
                Points = Points(
                    0, 0, 27.5f, 0, 55, 0, 82.5f, 0, 109.5f, 0, 137, 0,
                    164.5f, 0, 192, 0, 219.5f, 0, 247, 0, 258.5f, 2.5f, 268, 9,
                    274.5f, 18.5f, 277, 30, 277, 50.5f, 277, 71.5f, 277, 92, 274, 105.5f,
                    266.5f, 117, 255, 124.5f, 242, 127, 222, 127, 209, 125.5f, 197, 119.5f,
                    187.5f, 111, 175.5f, 101, 160.5f, 95.5f, 145, 95.5f, 130.5f, 101, 118.5f, 111,
                    108.5f, 119.5f, 97, 125.5f, 84, 127, 71, 125.5f, 59, 119.5f, 49.5f, 111,
                    39.5f, 102.5f, 28, 97, 15, 95, -15, 95, -28.5f, 92.5f, -39.5f, 84.5f,
                    -47.5f, 73.5f, -50, 60, -50, 30, -46, 15, -35, 4, -20, 0)
            },
            new TrackLayout
            {
                Name = "Neon Mile",
                Description = "Right-angle turns between city blocks, then a flat-out run down the neon-lit back straight.",
                Biome = Biome.City,
                Laps = 4,
                Width = 14f,
                Seed = 4404,
                StartDistance = 50f,
                PickupDistances = new[] { 229f, 408f, 558f, 753f, 1002f },
                SetPieces = new[]
                {
                    new TrackLayout.SetPiece(SetPieceKind.Tyres, 732f, -1f)
                },
                Points = Points(
                    0, 0, 27.5f, 0, 55, 0, 82.5f, 0, 110, 0, 121, 3,
                    129, 11, 132, 22, 132, 47, 132, 72, 135, 83, 143, 91,
                    154, 94, 179, 94, 204, 94, 215, 97, 223, 105, 226, 116,
                    226, 146, 226, 176, 223, 187, 215, 195, 204, 198, 174, 198,
                    144, 198, 114, 198, 84, 198, 54, 198, 28.5f, 198, 2.5f, 198,
                    -23, 198, -48.5f, 198, -74.5f, 198, -100, 198, -110, 196, -118.5f, 190.5f,
                    -124, 182, -126, 172, -126, 150, -126, 128, -126, 106, -123, 95,
                    -115, 87, -104, 84, -84, 84, -64, 84, -53, 81, -45, 73,
                    -42, 62, -42, 42, -42, 22, -39, 11, -31, 3, -20, 0)
            },
            new TrackLayout
            {
                Name = "Canyon Run",
                Description = "A long, twisting road through the red canyons, with a narrow gorge where passing is a gamble.",
                Biome = Biome.Desert,
                Laps = 5,
                Width = 15f,
                Seed = 5505,
                StartDistance = 60f,
                PickupDistances = new[] { 209f, 387f, 628f, 863f, 986f },
                Stretches = new[] { new TrackLayout.Stretch(477f, 628f, 12f) },
                SetPieces = new[]
                {
                    new TrackLayout.SetPiece(SetPieceKind.Tyres, 745f, 1f),
                    new TrackLayout.SetPiece(SetPieceKind.Tyres, 832f, -1f)
                },
                Points = Points(
                    0, 0, 25, 0, 50, 0, 75, 0, 100, 0, 115, -4,
                    126, -15, 130, -30, 130, -50, 132.5f, -63.5f, 139.5f, -75.5f, 150, -84.5f,
                    161, -95.5f, 165, -110.5f, 161, -125.5f, 150, -136.5f, 139.5f, -145.5f, 132.5f, -157.5f,
                    130, -171, 130, -201, 126, -215, 116, -225.5f, 102, -229, 72, -229,
                    42, -229, 12, -229, -1.5f, -231.5f, -13.5f, -238.5f, -25.5f, -245.5f, -39.5f, -248,
                    -53, -245.5f, -65, -238.5f, -77, -231.5f, -91, -229, -111, -229, -131, -229,
                    -146, -225, -156.5f, -214, -161, -199, -161, -179, -161, -159, -156, -145,
                    -144.5f, -136, -130.5f, -135, -118, -141.5f, -97, -163, -84.5f, -169.5f, -70.5f, -168.5f,
                    -58.5f, -159.5f, -54, -145, -54, -118, -54, -91, -56, -79, -61.5f, -68,
                    -67.5f, -54, -68, -39, -63.5f, -25, -55.5f, -14.5f, -45, -6.5f, -33, -1.5f,
                    -20, 0)
            },
            new TrackLayout
            {
                Name = "Smelter",
                Description = "Tight and technical: corner after corner between the furnaces. Brake early, shoot often.",
                Biome = Biome.Industrial,
                Laps = 5,
                Width = 13f,
                Seed = 6606,
                StartDistance = 65f,
                PickupDistances = new[] { 162f, 267f, 450f, 555f, 657f },
                SetPieces = new[]
                {
                    new TrackLayout.SetPiece(SetPieceKind.Tyres, 237f, -1f),
                    new TrackLayout.SetPiece(SetPieceKind.Containers, 473f, 1f),
                    new TrackLayout.SetPiece(SetPieceKind.Containers, 473f, -1f)
                },
                Points = Points(
                    0, 0, 28, 0, 56, 0, 84.5f, 0, 112.5f, 0, 122, 2.5f,
                    129, 9.5f, 131.5f, 19, 131.5f, 39, 134, 48.5f, 141, 55.5f, 150.5f, 58,
                    165.5f, 58, 175, 60.5f, 182, 67.5f, 184.5f, 77, 182, 86.5f, 175, 93.5f,
                    165.5f, 96, 145.5f, 96, 125.5f, 96, 115.5f, 98.5f, 108, 106, 105.5f, 116,
                    105.5f, 136, 102.5f, 146, 95.5f, 153.5f, 85.5f, 156, 55.5f, 156, 25.5f, 156,
                    14.5f, 153, 6.5f, 145, -1.5f, 137, -12.5f, 134, -42.5f, 134, -52, 131.5f,
                    -59, 124.5f, -61.5f, 115, -61.5f, 98.5f, -61.5f, 82.5f, -60, 73, -54.5f, 64.5f,
                    -47.5f, 57.5f, -42, 49.5f, -40, 40, -40, 20, -37.5f, 10, -30, 2.5f,
                    -20, 0)
            },
            new TrackLayout
            {
                Name = "Glacier Ring",
                Description = "Fast sweeping curves on a wide road of packed snow. Keep your foot down.",
                Biome = Biome.Snow,
                Laps = 5,
                Width = 16f,
                Seed = 7707,
                StartDistance = 60f,
                PickupDistances = new[] { 269f, 492f, 658f, 847f, 1039f },
                SetPieces = new[]
                {
                    new TrackLayout.SetPiece(SetPieceKind.Tyres, 606f, -1f)
                },
                Points = Points(
                    0, 0, 30, 0, 59.5f, 0, 89.5f, 0, 119, 0, 133.5f, 1.5f,
                    147.5f, 6, 160.5f, 13.5f, 171, 23, 179.5f, 35, 188.5f, 47.5f, 198.5f, 59,
                    210.5f, 69, 223.5f, 77, 237.5f, 83.5f, 250, 90, 261, 99, 269.5f, 111,
                    275, 124, 277, 138.5f, 275.5f, 152.5f, 270.5f, 166.5f, 263, 178.5f, 250, 193.5f,
                    237, 209, 228, 219, 217.5f, 228, 206.5f, 235.5f, 194.5f, 242, 182, 247.5f,
                    168.5f, 254.5f, 156.5f, 264, 147, 275.5f, 140, 289.5f, 134, 301, 125.5f, 310.5f,
                    114.5f, 317.5f, 102, 321.5f, 89, 322, 76, 319.5f, 64.5f, 313, 55, 304.5f,
                    38.5f, 285, 22.5f, 266, 6, 246.5f, -10, 227, -26, 208, -35, 195,
                    -42, 181, -46, 165.5f, -47, 150, -48.5f, 136, -52, 122.5f, -58, 110,
                    -66, 98.5f, -74, 86, -79, 71.5f, -80, 56.5f, -77, 41.5f, -71, 28,
                    -61, 16.5f, -49, 7.5f, -35, 2, -20, 0)
            },
            new TrackLayout
            {
                Name = "Downtown Carnage",
                Description = "A brutal city circuit with a double hairpin that turns the field into a demolition derby.",
                Biome = Biome.City,
                Laps = 5,
                Width = 14f,
                Seed = 8808,
                StartDistance = 55f,
                PickupDistances = new[] { 189f, 409f, 579f, 639f, 909f, 1054f },
                Stretches = new[] { new TrackLayout.Stretch(604f, 694f, 12f) },
                SetPieces = new[]
                {
                    new TrackLayout.SetPiece(SetPieceKind.Tyres, 219f, 1f),
                    new TrackLayout.SetPiece(SetPieceKind.Tyres, 319f, -1f)
                },
                Points = Points(
                    0, 0, 26, 0, 52.5f, 0, 79, 0, 105, 0, 116, 3,
                    124, 11, 127, 22, 127, 47, 127, 72, 129.5f, 81.5f, 136.5f, 88.5f,
                    146, 91, 155.5f, 88.5f, 162.5f, 81.5f, 165, 72, 165, 52, 165, 32,
                    167.5f, 22.5f, 174.5f, 15.5f, 184, 13, 193.5f, 15.5f, 200.5f, 22.5f, 203, 32,
                    203, 62, 203, 92, 206, 103, 214, 111, 225, 114, 255, 114,
                    267, 117, 276, 126, 279, 138, 279, 168, 276, 180, 267, 189,
                    255, 192, 230, 192, 212.5f, 192, 195, 192, 167.5f, 192, 140, 192,
                    114.5f, 192, 89.5f, 192, 64, 192, 53, 189, 45, 181, 42, 170,
                    42, 140, 39, 129, 31, 121, 20, 118, 0, 118, -20, 118,
                    -32, 115, -41, 106, -44, 94, -44, 70.5f, -44, 47.5f, -44, 24,
                    -41, 12, -32, 3, -20, 0)
            },
            new TrackLayout
            {
                Name = "The Crucible",
                Description = "The Warden's arena: a short, wide loop ringed by fire pits. Only one driver leaves as champion.",
                Biome = Biome.Desert,
                Laps = 3,
                Width = 16f,
                Seed = 9909,
                Showdown = true,
                StartDistance = 55f,
                PickupDistances = new[] { 150f, 245f, 333f, 452f },
                SetPieces = new[]
                {
                    new TrackLayout.SetPiece(SetPieceKind.FirePit, 123f, 1f),
                    new TrackLayout.SetPiece(SetPieceKind.FirePit, 271f, 1f),
                    new TrackLayout.SetPiece(SetPieceKind.FirePit, 426f, 1f),
                    new TrackLayout.SetPiece(SetPieceKind.FirePit, 195f, -1f),
                    new TrackLayout.SetPiece(SetPieceKind.FirePit, 354f, -1f),
                    new TrackLayout.SetPiece(SetPieceKind.FirePit, 40f, -1f)
                },
                Points = Points(
                    0, 0, 24.5f, 0, 48.5f, 0, 73, 0, 97.5f, 0, 110, 3.5f,
                    119, 12.5f, 122.5f, 25, 119, 37.5f, 108, 57, 96.5f, 76.5f, 84, 98,
                    71.5f, 120, 62.5f, 129, 50, 132.5f, 37.5f, 129, 28, 120, 20.5f, 107,
                    14.5f, 98.5f, 6, 92, -2, 86, -8.5f, 77.5f, -20, 57.5f, -31.5f, 37.5f,
                    -35, 25, -31.5f, 12.5f, -22.5f, 3.5f, -10, 0)
            },

            // The tracks of the harder leagues (after the Crucible, so saves keep their indices): wider and faster, for
            // cars that pass sixty metres a second on nitro, each with a narrow stretch where the field has to queue up.
            new TrackLayout
            {
                Name = "Refinery Row",
                Description = "A flat-out sweep round the storage tanks, then a narrow box of corners through the pipe racks.",
                Biome = Biome.Industrial,
                Laps = 5,
                Width = 16f,
                Seed = 11010,
                StartDistance = 60f,
                PickupDistances = new[] { 235f, 425f, 590f, 725f, 910f, 1050f },
                Stretches = new[] { new TrackLayout.Stretch(640f, 780f, 13f) },
                SetPieces = new[]
                {
                    new TrackLayout.SetPiece(SetPieceKind.StorageTank, 355f, 1f, 6f),
                    new TrackLayout.SetPiece(SetPieceKind.StorageTank, 445f, 1f, 6f),
                    new TrackLayout.SetPiece(SetPieceKind.StorageTank, 510f, 1f, 9f),
                    new TrackLayout.SetPiece(SetPieceKind.StorageTank, 385f, 1f, 30f),
                    new TrackLayout.SetPiece(SetPieceKind.StorageTank, 475f, 1f, 30f),
                    new TrackLayout.SetPiece(SetPieceKind.PipeRack, 670f, -1f),
                    new TrackLayout.SetPiece(SetPieceKind.PipeRack, 730f, -1f),
                    new TrackLayout.SetPiece(SetPieceKind.PipeRack, 730f, 1f, 14f),
                    new TrackLayout.SetPiece(SetPieceKind.PipeRack, 785f, -1f),
                    new TrackLayout.SetPiece(SetPieceKind.Containers, 900f, -1f),
                    new TrackLayout.SetPiece(SetPieceKind.Containers, 930f, 1f),
                    new TrackLayout.SetPiece(SetPieceKind.Tyres, 640f, 1f),
                    new TrackLayout.SetPiece(SetPieceKind.Tyres, 1090f, -1f)
                },
                Points = Points(
                    0, 0, 28, 0, 55.5f, 0, 83.5f, 0, 111.5f, 0, 139, 0,
                    167, 0, 195, 0, 222.5f, 0, 250.5f, 0, 278.5f, 0, 318.5f, 10.5f,
                    347.5f, 40, 358.5f, 80, 358.5f, 95, 347.5f, 135, 318.5f, 164.5f, 278.5f, 175,
                    248, 175, 217.5f, 175, 205.5f, 178, 196.5f, 187, 193.5f, 199, 193.5f, 221,
                    190, 233, 181.5f, 242, 169.5f, 245, 153.5f, 245, 141.5f, 242, 132.5f, 233,
                    129.5f, 221, 129.5f, 208, 125.5f, 194, 115.5f, 184, 101.5f, 180, 67, 180,
                    53.5f, 178.5f, 41.5f, 173, 21, 161.5f, 0.5f, 149.5f, -20, 137.5f, -40, 125.5f,
                    -52, 114, -57, 98.5f, -54, 82, -44, 60, -34, 37.5f, -23.5f, 15,
                    -18, 7, -9.5f, 2)
            },
            new TrackLayout
            {
                Name = "Avalanche Ridge",
                Description = "Two hairpins up the mountain, a narrow ice shelf along the ridge and a long sweeping run back down.",
                Biome = Biome.Snow,
                Laps = 5,
                Width = 16f,
                Seed = 12111,
                StartDistance = 60f,
                PickupDistances = new[] { 245f, 385f, 495f, 665f, 860f, 1180f },
                Stretches = new[] { new TrackLayout.Stretch(755f, 950f, 12f) },
                SetPieces = new[]
                {
                    new TrackLayout.SetPiece(SetPieceKind.Tyres, 425f, -1f),
                    new TrackLayout.SetPiece(SetPieceKind.Tyres, 570f, 1f),
                    new TrackLayout.SetPiece(SetPieceKind.Boulders, 800f, -1f),
                    new TrackLayout.SetPiece(SetPieceKind.Boulders, 900f, -1f),
                    new TrackLayout.SetPiece(SetPieceKind.Boulders, 1050f, -1f)
                },
                Points = Points(
                    0, 0, 27, 0, 53.5f, 0, 80.5f, 0, 107, 0, 134, 0,
                    161, 0, 187.5f, 0, 214.5f, 0, 241, 0, 257.5f, 3.5f, 271, 13.5f,
                    279.5f, 28, 287.5f, 54.5f, 296, 81, 304, 107.5f, 312.5f, 133.5f, 313.5f, 144,
                    310, 153.5f, 303, 161, 293.5f, 164.5f, 281, 164, 271.5f, 159, 265.5f, 150,
                    254, 121, 242.5f, 92, 236.5f, 83.5f, 227.5f, 78.5f, 217, 77.5f, 211, 78,
                    199.5f, 82.5f, 192, 92, 190, 104, 192.5f, 134, 195.5f, 163.5f, 193.5f, 183,
                    184, 200.5f, 168, 213, 149, 218, 123, 219.5f, 97, 221, 71.5f, 222.5f,
                    45.5f, 224, 19.5f, 225.5f, -6, 227, -32, 228.5f, -52.5f, 225.5f, -70.5f, 214,
                    -95, 189.5f, -111.5f, 163, -115, 132, -105, 103, -95, 87, -88, 69.5f,
                    -88.5f, 50.5f, -87.5f, 33.5f, -79.5f, 20.5f, -65.5f, 13.5f, -37, 7.5f, -8.5f, 1)
            },
            new TrackLayout
            {
                Name = "Gridlock Boulevard",
                Description = "The longest circuit in the city: a flat-out boulevard, a tight chicane between the blocks and tyre walls at the stops.",
                Biome = Biome.City,
                Laps = 4,
                Width = 16f,
                Seed = 13212,
                StartDistance = 60f,
                PickupDistances = new[] { 260f, 600f, 800f, 960f, 1130f, 1290f },
                Stretches = new[] { new TrackLayout.Stretch(500f, 580f, 13f) },
                SetPieces = new[]
                {
                    new TrackLayout.SetPiece(SetPieceKind.Tyres, 440f, 1f),
                    new TrackLayout.SetPiece(SetPieceKind.Tyres, 540f, -1f),
                    new TrackLayout.SetPiece(SetPieceKind.Tyres, 1030f, 1f),
                    new TrackLayout.SetPiece(SetPieceKind.Wrecks, 150f, 1f),
                    new TrackLayout.SetPiece(SetPieceKind.Wrecks, 330f, -1f),
                    new TrackLayout.SetPiece(SetPieceKind.Wrecks, 850f, -1f),
                    new TrackLayout.SetPiece(SetPieceKind.Wrecks, 1200f, 1f)
                },
                Points = Points(
                    0, 0, 28, 0, 55.5f, 0, 83.5f, 0, 111.5f, 0, 139.5f, 0,
                    167, 0, 195, 0, 223, 0, 251, 0, 278.5f, 0, 306.5f, 0,
                    334.5f, 0, 362.5f, 0, 390, 0, 418, 0, 429, -3, 437, -11,
                    440, -22, 440, -51.5f, 440, -81.5f, 441.5f, -89.5f, 445.5f, -96.5f, 452, -101.5f,
                    470, -111.5f, 476.5f, -116.5f, 480.5f, -123.5f, 482, -131.5f, 482, -158, 482, -184,
                    482, -210, 478, -225, 467, -236, 452, -240, 424, -240, 395.5f, -240,
                    367.5f, -240, 339.5f, -240, 311, -240, 283, -240, 254.5f, -240, 226.5f, -240,
                    198.5f, -240, 170, -240, 142, -240, 131, -237, 123, -229, 120, -218,
                    120, -192, 117, -181, 109, -173, 98, -170, 75, -170, 52, -170,
                    41, -167, 33, -159, 30, -148, 30, -112, 26, -97.5f, 15, -86.5f,
                    -15, -68.5f, -26, -57.5f, -30, -43, -30, -30, -26, -15, -15, -4)
            }
        };

        public static int Count => All.Length;

        /// <summary>The track at <paramref name="index"/>, clamped into the table (a stale index never throws).</summary>
        public static TrackLayout Of(int index)
        {
            return All[Mathf.Clamp(index, 0, All.Length - 1)];
        }

        /// <summary>Control points from pairs of numbers (x, y), which keeps the table readable.</summary>
        private static Vector2[] Points(params float[] xy)
        {
            var points = new Vector2[xy.Length / 2];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
            }
            return points;
        }
    }
}
