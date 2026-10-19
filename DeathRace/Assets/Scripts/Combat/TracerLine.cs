using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// One streak of a machine gun round: drawn whole from the muzzle to where the round stopped on its first frame, so
    /// it reads even at a low frame rate, then its tail runs after the round into the hit and it fades.
    /// </summary>
    internal sealed class TracerLine : MonoBehaviour
    {
        private LineRenderer line;
        private Vector3 from;
        private Vector3 to;
        private Color color;
        private float width;
        private float life;
        private float age;

        public bool Busy => gameObject.activeSelf;

        public static TracerLine Create(Transform parent, Material material)
        {
            var go = new GameObject("Tracer");
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 0;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = material;
            TracerLine tracer = go.AddComponent<TracerLine>();
            tracer.line = line;
            go.SetActive(false);
            return tracer;
        }

        public void Play(Vector3 start, Vector3 end, Color tint, float seconds, float lineWidth)
        {
            from = start;
            to = end;
            color = tint;
            life = Mathf.Max(0.01f, seconds);
            width = lineWidth;
            age = 0f;
            gameObject.SetActive(true);
            Draw(0f);
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = age / life;
            if (t >= 1f)
            {
                gameObject.SetActive(false);
                return;
            }
            Draw(t);
        }

        private void Draw(float t)
        {
            Vector3 tail = Vector3.Lerp(from, to, 1f - (1f - t) * (1f - t));
            line.SetPosition(0, tail);
            line.SetPosition(1, to);
            Color head = color;
            head.a = 1f - t * t;
            Color back = color;
            back.a = 0f;
            line.startColor = back;
            line.endColor = head;
            line.startWidth = width * 0.5f;
            line.endWidth = width * (1f - 0.4f * t);
        }
    }
}
