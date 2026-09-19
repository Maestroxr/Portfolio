using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// The backdrop behind the board and the menus: a sky gradient in the colours of the world, a slowly scrolling
    /// pattern and shapes floating across (clouds, leaves, snowflakes, balloons). Changing worlds fades everything to the
    /// new look. Runs on unscaled time so the backdrop keeps moving behind the pause menu.
    /// </summary>
    public class AmbientBackdrop : MonoBehaviour
    {
        private sealed class Shape
        {
            public RectTransform rect;
            public Image image;
            public Vector2 position;
            public float speed;
            public float size;
            public float phase;
            public float spin;
            public float angle;
            public float depth;
        }

        [SerializeField] internal GradientGraphic gradient;
        [SerializeField] internal RawImage pattern;
        [SerializeField] internal Vector2 patternScroll = new Vector2(0.012f, 0.008f);
        [SerializeField] internal float patternTileSize = 256f;
        [SerializeField] internal RectTransform shapesRoot;
        [SerializeField] internal Image shapeTemplate;
        [SerializeField] internal int shapeCount = 16;
        [SerializeField] internal float fadeTime = 0.8f;

        private readonly List<Shape> shapes = new List<Shape>();
        private Color top = new Color(0.45f, 0.75f, 1f);
        private Color bottom = new Color(0.7f, 0.9f, 0.6f);
        private Color topFrom;
        private Color bottomFrom;
        private Color topTarget;
        private Color bottomTarget;
        private float blend = 1f;
        private Sprite shapeSprite;
        private Sprite pendingSprite;
        private Color shapeColor = new Color(1f, 1f, 1f, 0.5f);
        private Color pendingColor;
        private AmbientMotion motion = AmbientMotion.Drift;
        private AmbientMotion pendingMotion;
        private float shapesAlpha = 1f;
        private bool swapping;
        private Vector2 patternOffset;

        public CardWorld World { get; private set; }

        private void Awake()
        {
            if (shapeTemplate != null)
            {
                shapeTemplate.gameObject.SetActive(false);
                shapeTemplate.raycastTarget = false;
            }
            topTarget = topFrom = top;
            bottomTarget = bottomFrom = bottom;
        }

        /// <summary>Changes to the look of <paramref name="world"/>, at once or with a fade.</summary>
        public void Apply(CardWorld world, bool instant)
        {
            if (world == null)
            {
                return;
            }
            bool sameWorld = world == World;
            World = world;
            topFrom = top;
            bottomFrom = bottom;
            topTarget = world.skyTop;
            bottomTarget = world.skyBottom;
            blend = instant ? 1f : 0f;
            if (instant)
            {
                top = topTarget;
                bottom = bottomTarget;
                shapeSprite = world.ambient;
                shapeColor = world.ambientColor;
                motion = world.motion;
                shapesAlpha = 1f;
                swapping = false;
                EnsureShapes(true);
            }
            else if (!sameWorld)
            {
                pendingSprite = world.ambient;
                pendingColor = world.ambientColor;
                pendingMotion = world.motion;
                swapping = true;
            }
            ApplyColors();
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            if (blend < 1f)
            {
                blend = Mathf.MoveTowards(blend, 1f, dt / Mathf.Max(0.01f, fadeTime));
                float e = Mathf.SmoothStep(0f, 1f, blend);
                top = Color.Lerp(topFrom, topTarget, e);
                bottom = Color.Lerp(bottomFrom, bottomTarget, e);
                ApplyColors();
            }
            if (swapping)
            {
                shapesAlpha = Mathf.MoveTowards(shapesAlpha, 0f, dt / (fadeTime * 0.5f));
                if (shapesAlpha <= 0f)
                {
                    shapeSprite = pendingSprite;
                    shapeColor = pendingColor;
                    motion = pendingMotion;
                    swapping = false;
                    EnsureShapes(true);
                }
            }
            else if (shapesAlpha < 1f)
            {
                shapesAlpha = Mathf.MoveTowards(shapesAlpha, 1f, dt / (fadeTime * 0.5f));
            }
            if (pattern != null)
            {
                patternOffset += patternScroll * dt;
                patternOffset.x %= 1f;
                patternOffset.y %= 1f;
                Rect area = pattern.rectTransform.rect;
                float tiles = Mathf.Max(1f, area.width / Mathf.Max(16f, patternTileSize));
                pattern.uvRect = new Rect(patternOffset.x, patternOffset.y, tiles, tiles * area.height / Mathf.Max(1f, area.width));
            }
            MoveShapes(dt);
        }

        private void ApplyColors()
        {
            if (gradient != null)
            {
                gradient.Top = top;
                gradient.Bottom = bottom;
            }
        }

        private void EnsureShapes(bool scatter)
        {
            if (shapesRoot == null || shapeTemplate == null)
            {
                return;
            }
            while (shapes.Count < shapeCount)
            {
                Image image = Instantiate(shapeTemplate, shapesRoot);
                image.gameObject.SetActive(true);
                image.raycastTarget = false;
                var shapeRect = image.rectTransform;
                shapeRect.anchorMin = shapeRect.anchorMax = new Vector2(0.5f, 0.5f);
                shapes.Add(new Shape { rect = shapeRect, image = image });
            }
            Rect area = shapesRoot.rect;
            foreach (Shape shape in shapes)
            {
                shape.image.sprite = shapeSprite;
                shape.image.enabled = shapeSprite != null;
                shape.depth = Random.Range(0.4f, 1f);
                shape.size = Mathf.Lerp(40f, motion == AmbientMotion.Drift ? 260f : 90f, shape.depth * shape.depth);
                shape.speed = Mathf.Lerp(12f, motion == AmbientMotion.Drift ? 40f : 70f, shape.depth);
                shape.phase = Random.Range(0f, Mathf.PI * 2f);
                // Clouds stay level, balloons only sway; leaves and snowflakes tumble.
                shape.spin = motion == AmbientMotion.Fall ? Random.Range(-40f, 40f) : 0f;
                shape.angle = motion == AmbientMotion.Fall ? Random.Range(0f, 360f) : 0f;
                if (scatter)
                {
                    shape.position = new Vector2(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax));
                }
                shape.rect.sizeDelta = new Vector2(shape.size, shape.size * AspectOf(shapeSprite));
            }
            // Far shapes behind near ones.
            shapes.Sort((a, b) => a.depth.CompareTo(b.depth));
            for (int i = 0; i < shapes.Count; i++)
            {
                shapes[i].rect.SetSiblingIndex(i);
            }
        }

        private static float AspectOf(Sprite sprite)
        {
            if (sprite == null || sprite.rect.width <= 0f)
            {
                return 1f;
            }
            return sprite.rect.height / sprite.rect.width;
        }

        private void MoveShapes(float dt)
        {
            if (shapesRoot == null || shapes.Count == 0)
            {
                return;
            }
            Rect area = shapesRoot.rect;
            float margin = 160f;
            foreach (Shape shape in shapes)
            {
                shape.phase += dt;
                Vector2 offset = Vector2.zero;
                switch (motion)
                {
                    case AmbientMotion.Fall:
                        shape.position.y -= shape.speed * dt;
                        offset.x = Mathf.Sin(shape.phase * 1.3f) * 30f * shape.depth;
                        if (shape.position.y < area.yMin - margin)
                        {
                            shape.position = new Vector2(Random.Range(area.xMin, area.xMax), area.yMax + margin);
                        }
                        break;
                    case AmbientMotion.Rise:
                        shape.position.y += shape.speed * dt;
                        offset.x = Mathf.Sin(shape.phase * 1.1f) * 22f * shape.depth;
                        shape.angle = Mathf.Sin(shape.phase * 1.1f + 1f) * 8f;
                        if (shape.position.y > area.yMax + margin)
                        {
                            shape.position = new Vector2(Random.Range(area.xMin, area.xMax), area.yMin - margin);
                        }
                        break;
                    default:
                        shape.position.x += shape.speed * dt;
                        offset.y = Mathf.Sin(shape.phase * 0.6f) * 10f;
                        if (shape.position.x > area.xMax + margin * 1.5f)
                        {
                            shape.position = new Vector2(area.xMin - margin * 1.5f, Random.Range(area.yMin, area.yMax));
                        }
                        break;
                }
                shape.angle += shape.spin * dt;
                shape.rect.anchoredPosition = shape.position + offset;
                shape.rect.localRotation = Quaternion.Euler(0f, 0f, shape.angle);
                Color color = shapeColor;
                color.a *= shapesAlpha * Mathf.Lerp(0.55f, 1f, shape.depth);
                shape.image.color = color;
            }
        }
    }
}
