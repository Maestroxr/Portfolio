using TMPro;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>A number that pops out where points were scored, rises and fades.</summary>
    public class ScorePopup : MonoBehaviour
    {
        [SerializeField] internal TMP_Text label;
        [SerializeField] internal float lifetime = 0.95f;
        [SerializeField] internal float rise = 1.3f;

        private float age;
        private Vector3 start;
        private Color color;
        private float size = 1f;

        internal PopupPool Pool { get; set; }


        public void Show(Vector3 position, string text, Color tint, float scale)
        {
            start = new Vector3(position.x, position.y, -1f);
            transform.position = start;
            transform.rotation = Quaternion.identity;
            age = 0f;
            color = tint;
            size = scale;
            if (label != null)
            {
                label.text = text;
                label.color = tint;
            }
            Update();
        }


        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / lifetime);
            float pop = t < 0.15f ? Mathf.Lerp(0.4f, 1.25f, t / 0.15f) : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((t - 0.15f) / 0.2f));
            transform.localScale = Vector3.one * size * pop;
            transform.position = start + Vector3.up * rise * (1f - (1f - t) * (1f - t));
            if (label != null)
            {
                Color faded = color;
                faded.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                label.color = faded;
            }
            if (age >= lifetime)
            {
                if (Pool != null)
                {
                    Pool.Recycle(this);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
