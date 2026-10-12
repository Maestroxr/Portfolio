using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// Where every part of the backgammon board is, in the board's own units: x to White's right, z away from White, y up
    /// with the felt at 0. The box is 12 by 9.5: a rim round it, the left and the right half of the field with six
    /// points along each long side, the bar between the halves, and on the right, past a divider, the two bearing off
    /// trays (White's nearer White, Black's farther). Point <c>i</c> is White's (i + 1)-point: White's home board is the
    /// bottom right quarter, Black's the top right. The board view and its builder both read this, so the meshes and the
    /// checkers always agree.
    /// </summary>
    public static class BackgammonLayout
    {
        public const float HalfWidth = 6f;
        public const float HalfDepth = 4.75f;
        /// <summary>The width of the rim round the box.</summary>
        public const float Rim = 0.3f;
        public const float PointWidth = 0.78f;
        public const float PointLength = 3.75f;
        public const float BarWidth = 0.84f;
        /// <summary>The frame between the right half of the field and the trays.</summary>
        public const float Divider = 0.25f;
        public const float TrayWidth = 0.95f;
        /// <summary>The frame between the two trays.</summary>
        public const float TraySplit = 0.3f;
        /// <summary>How far the rim, the bar and the dividers rise over the felt.</summary>
        public const float FrameHeight = 0.14f;
        /// <summary>How far the box reaches under the felt, down to the table.</summary>
        public const float BaseDepth = 0.15f;
        public const float CheckerRadius = 0.355f;
        public const float CheckerHeight = 0.11f;
        /// <summary>From one checker's centre to the next along a point.</summary>
        public const float CheckerStep = 0.72f;
        /// <summary>Checkers in a row along a point; more stack on the row again.</summary>
        public const int RowLength = 5;
        public const float DieSize = 0.48f;
        public const float CubeSize = 0.5f;

        /// <summary>The bar as a place (the model's <see cref="BackgammonGame.Bar"/>).</summary>
        public const int Bar = BackgammonGame.Bar;
        /// <summary>The tray of the borne off checkers as a place (the model's <see cref="BackgammonGame.Off"/>).</summary>
        public const int Off = BackgammonGame.Off;
        /// <summary>Places a checker can be: the 24 points, the bar and the tray.</summary>
        public const int Places = 26;

        private const float OffGap = 0.008f;

        public static float LeftMin => -HalfWidth + Rim;

        public static float LeftMax => LeftMin + 6f * PointWidth;

        public static float BarMin => LeftMax;

        public static float BarMax => BarMin + BarWidth;

        public static float BarX => (BarMin + BarMax) * 0.5f;

        public static float RightMin => BarMax;

        public static float RightMax => RightMin + 6f * PointWidth;

        public static float TrayMin => RightMax + Divider;

        public static float TrayMax => TrayMin + TrayWidth;

        public static float TrayX => (TrayMin + TrayMax) * 0.5f;

        /// <summary>Half the depth of the field inside the rim.</summary>
        public static float FieldDepth => HalfDepth - Rim;

        /// <summary>The centre of the half of the field a side throws its dice into: its right hand half as it sits.</summary>
        public static Vector3 DiceArea(Side side)
        {
            float x = side == Side.Light ? (RightMin + RightMax) * 0.5f : (LeftMin + LeftMax) * 0.5f;
            return new Vector3(x, 0f, 0f);
        }

        /// <summary>The x of the middle of point <paramref name="point"/> (0 to 23).</summary>
        public static float PointX(int point)
        {
            if (point < 6)
            {
                return RightMax - (point + 0.5f) * PointWidth;
            }
            if (point < 12)
            {
                return LeftMax - (point - 6 + 0.5f) * PointWidth;
            }
            if (point < 18)
            {
                return LeftMin + (point - 12 + 0.5f) * PointWidth;
            }
            return RightMin + (point - 18 + 0.5f) * PointWidth;
        }

        /// <summary>Whether the point lies along White's side (its base on the near rim, its tip pointing away).</summary>
        public static bool IsNear(int point)
        {
            return point < 12;
        }

        /// <summary>Where the point's base meets the rim (its middle).</summary>
        public static Vector3 PointBase(int point)
        {
            return new Vector3(PointX(point), 0f, IsNear(point) ? -FieldDepth : FieldDepth);
        }

        /// <summary>The way along the point from its base to its tip.</summary>
        public static Vector3 PointDirection(int point)
        {
            return IsNear(point) ? Vector3.forward : Vector3.back;
        }

        /// <summary>The layer of checker <paramref name="index"/> of a stack: a sixth checker lies on the first, and so on.</summary>
        public static int Layer(int index)
        {
            return Mathf.Max(0, index) / RowLength;
        }

        /// <summary>
        /// Where checker <paramref name="index"/> (0 = the first, nearest the rim) of <paramref name="side"/> lies at
        /// <paramref name="place"/> (a point, the bar or the tray): the middle of its bottom face, and how it is turned
        /// (in the tray the checkers stand on their edge, side by side).
        /// </summary>
        public static Pose Slot(Side side, int place, int index)
        {
            index = Mathf.Max(0, index);
            int layer = Layer(index);
            int row = index % RowLength;
            if (place >= 0 && place < 24)
            {
                Vector3 p = PointBase(place) + PointDirection(place) * (0.06f + CheckerRadius + row * CheckerStep);
                p.y = layer * CheckerHeight;
                return new Pose(p, Quaternion.identity);
            }
            if (place == Bar)
            {
                // White's checkers wait on the far half of the bar, Black's on the near half; the cube sits between them.
                float z = CubeSize * 0.5f + 0.05f + CheckerRadius + row * CheckerStep;
                return new Pose(new Vector3(BarX, FrameHeight + layer * CheckerHeight, side == Side.Light ? z : -z), Quaternion.identity);
            }
            // Off: standing on edge in the side's tray, from the rim towards the middle.
            float along = 0.1f + index * (CheckerHeight + OffGap);
            return side == Side.Light
                ? new Pose(new Vector3(TrayX, CheckerRadius, -FieldDepth + along), Quaternion.Euler(90f, 0f, 0f))
                : new Pose(new Vector3(TrayX, CheckerRadius, FieldDepth - along), Quaternion.Euler(-90f, 0f, 0f));
        }

        /// <summary>The middle of the top of checker <paramref name="index"/> at <paramref name="place"/> (for the marks on it).</summary>
        public static Vector3 SlotTop(Side side, int place, int index)
        {
            Pose slot = Slot(side, place, index);
            if (place == Off)
            {
                return new Vector3(slot.position.x, CheckerRadius * 2f, slot.position.z + (side == Side.Light ? 0.5f : -0.5f) * CheckerHeight);
            }
            return slot.position + Vector3.up * CheckerHeight;
        }

        /// <summary>Where the doubling cube sits: in the middle of the bar, raised over it while offered, or in its owner's tray.</summary>
        public static Vector3 CubePlace(int owner, bool offered)
        {
            float half = CubeSize * 0.5f;
            if (offered)
            {
                return new Vector3(BarX, FrameHeight + half + 0.55f, 0f);
            }
            if (owner == (int)Side.Light)
            {
                return new Vector3(TrayX, half, -TraySplit * 0.5f - 0.15f - half);
            }
            if (owner == (int)Side.Dark)
            {
                return new Vector3(TrayX, half, TraySplit * 0.5f + 0.15f + half);
            }
            return new Vector3(BarX, FrameHeight + half, 0f);
        }

        /// <summary>
        /// The place under a point of the felt's plane (board units): a point (0 to 23) anywhere along its half of the
        /// field, the bar (24), a tray or the divider before it (25), or -1 off the field.
        /// </summary>
        public static int PlaceAt(Vector3 local)
        {
            if (Mathf.Abs(local.z) > FieldDepth + 0.1f)
            {
                return -1;
            }
            float x = local.x;
            bool near = local.z < 0f;
            if (x >= LeftMin && x < LeftMax)
            {
                int col = Mathf.Clamp(Mathf.FloorToInt((x - LeftMin) / PointWidth), 0, 5);
                return near ? 11 - col : 12 + col;
            }
            if (x >= RightMin && x < RightMax)
            {
                int col = Mathf.Clamp(Mathf.FloorToInt((x - RightMin) / PointWidth), 0, 5);
                return near ? 5 - col : 18 + col;
            }
            if (x >= BarMin && x < BarMax)
            {
                return Bar;
            }
            if (x >= RightMax && x <= HalfWidth)
            {
                return Off;
            }
            return -1;
        }
    }
}
