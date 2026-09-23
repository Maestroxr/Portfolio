using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The ground a battle is shown on, whichever style it is fought in: the hexagons of the map around the place the
    /// armies met (<see cref="MapBattlefield"/>), or a battlefield of its own in a scene of its own
    /// (<see cref="BattlefieldScene"/>). The battle view and the battle bar know the field only through this: where a
    /// cell of the battle lies, which cell the pointer is over, which way a side looks, where its hero stands, and how
    /// cells are painted for what the stack in hand may do.
    /// </summary>
    public interface IBattlefield
    {
        /// <summary>The camera the battle is seen through: numbers and count plates turn to it, the pointer is read with it.</summary>
        Camera Camera { get; }

        /// <summary>What the units, heroes and props of the battle are put under.</summary>
        Transform Root { get; }

        /// <summary>The width of a cell, from one side to the other, for the size of a spell.</summary>
        float CellWidth { get; }

        /// <summary>
        /// How much bigger than on the map the creatures and heroes are shown: a battlefield of its own is seen from
        /// farther away, and its troops are drawn larger to read as well.
        /// </summary>
        float UnitScale { get; }

        /// <summary>The middle of a cell of the battle, on the ground.</summary>
        Vector3 Point(int cell);

        /// <summary>
        /// The cell of the battle under a screen position, or -1 off the field; <paramref name="ground"/> is the point on
        /// the ground it hit, for telling which side of a cell the pointer is on.
        /// </summary>
        int CellAt(Vector3 screen, out Vector3 ground);

        /// <summary>Whether the pointer is dragging the view (a click that ends a drag is no click on the field).</summary>
        bool IsDragging { get; }

        /// <summary>The way a stack looks when its <see cref="BattleStack.facing"/> is <paramref name="side"/> (east or west).</summary>
        Vector3 Facing(HexSide side);

        /// <summary>Where the hero (or the banner) of a side stands while the battle is fought, and which way he looks.</summary>
        void HeroSpot(int side, out Vector3 position, out Vector3 facing);

        /// <summary>Shows the hexagons of the field.</summary>
        void ShowGrid();

        void HideGrid();

        void Paint(int cell, Color color);

        void Unpaint(int cell);

        void ClearPaint();

        /// <summary>Rings the cell under the pointer (or nothing, with -1), over whatever it is painted with.</summary>
        void Mark(int cell, Color color);

        /// <summary>
        /// The arrow tower that stood on <paramref name="cell"/> has fallen: a field with a town's wall of its own leaves
        /// the heap of its stones in the wall there (the wall itself stays standing, as it does in the rules).
        /// </summary>
        void TowerFell(int cell);
    }

    /// <summary>
    /// Works out where a camera of a fixed angle has to stand to show a set of points inside a part of the screen: the
    /// field and its heroes above the battle bar, for instance.
    /// </summary>
    public static class Framing
    {
        /// <summary>
        /// The point on the ground (at <paramref name="groundY"/>) a camera turned by <paramref name="rotation"/> looks at,
        /// and how far back it stands, so that every one of <paramref name="points"/> shows inside
        /// <paramref name="viewport"/> (in viewport units, 0 to 1), filling it as far as the tighter direction allows.
        /// </summary>
        public static void Fit(Camera camera, Quaternion rotation, IList<Vector3> points, Rect viewport, float groundY,
            out Vector3 target, out float distance)
        {
            float fov = camera != null ? camera.fieldOfView : 40f;
            float aspect = camera != null && camera.aspect > 0.01f ? camera.aspect : 16f / 9f;
            float tanV = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            float tanH = tanV * aspect;
            Vector3 forward = rotation * Vector3.forward;
            Vector3 right = rotation * Vector3.right;
            Vector3 along = new Vector3(forward.x, 0f, forward.z);
            along = along.sqrMagnitude > 1e-6f ? along.normalized : Vector3.forward;
            float pitch = Mathf.Max(10f, Vector3.Angle(along, forward));
            Quaternion inverse = Quaternion.Inverse(rotation);

            target = Vector3.zero;
            foreach (Vector3 point in points)
            {
                target += point;
            }
            target /= Mathf.Max(1, points.Count);
            target.y = groundY;
            distance = 40f;
            for (int step = 0; step < 48; step++)
            {
                Vector3 position = target - forward * distance;
                float uMin = float.MaxValue, uMax = float.MinValue, vMin = float.MaxValue, vMax = float.MinValue;
                foreach (Vector3 point in points)
                {
                    Vector3 local = inverse * (point - position);
                    float depth = Mathf.Max(0.1f, local.z);
                    float u = 0.5f + 0.5f * local.x / (depth * tanH);
                    float v = 0.5f + 0.5f * local.y / (depth * tanV);
                    uMin = Mathf.Min(uMin, u);
                    uMax = Mathf.Max(uMax, u);
                    vMin = Mathf.Min(vMin, v);
                    vMax = Mathf.Max(vMax, v);
                }
                float du = (uMin + uMax) * 0.5f - viewport.center.x;
                float dv = (vMin + vMax) * 0.5f - viewport.center.y;
                float scale = Mathf.Max((uMax - uMin) / Mathf.Max(0.01f, viewport.width), (vMax - vMin) / Mathf.Max(0.01f, viewport.height));
                // Sideways the picture moves with the camera; along the ground it moves by the sine of the pitch.
                target += right * (du * 2f * tanH * distance * 0.9f);
                target += along * (dv * 2f * tanV * distance / Mathf.Sin(pitch * Mathf.Deg2Rad) * 0.9f);
                distance = Mathf.Clamp(distance * Mathf.Lerp(1f, scale, 0.85f), 4f, 400f);
            }
        }
    }
}
