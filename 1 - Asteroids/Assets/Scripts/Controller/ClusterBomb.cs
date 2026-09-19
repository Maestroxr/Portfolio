using TMPro;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A cluster bomb warps in with a countdown and a ring showing how far its blast reaches. When the countdown runs
    /// out it explodes and sprays shrapnel in every direction. Shooting it detonates it on the spot, so the safe play is
    /// to set it off from far away - or to be elsewhere when it blows.
    /// </summary>
    public class ClusterBomb : Explodable
    {
        [SerializeField] internal float fuseTime = 4.5f;
        [SerializeField] internal int shrapnel = 14;
        [SerializeField] internal float shrapnelSpeed = 9f;
        [SerializeField] internal TMP_Text countdown;
        [SerializeField] internal Transform rangeRing;
        [SerializeField] internal Renderer core;
        [SerializeField, ColorUsage(false, true)] internal Color coreColor = new Color(3f, 0.6f, 0.1f);

        private static MaterialPropertyBlock block;
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        private float fuse;
        private int shownSeconds = -1;

        public float FuseLeft => fuse;


        public override void OnSpawned()
        {
            base.OnSpawned();
            fuse = fuseTime;
            shownSeconds = -1;
            if (rangeRing != null)
            {
                rangeRing.gameObject.SetActive(true);
            }
        }


        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (!InPlay)
            {
                return;
            }
            fuse -= deltaTime;
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, fuse));
            if (seconds != shownSeconds)
            {
                shownSeconds = seconds;
                if (countdown != null)
                {
                    countdown.text = seconds.ToString();
                }
                if (Field != null && Field.Sounds != null && seconds > 0)
                {
                    Field.Sounds.BombTick();
                }
            }
            float urgency = 1f - Mathf.Clamp01(fuse / fuseTime);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Age * Mathf.Lerp(6f, 30f, urgency));
            if (core != null)
            {
                block ??= new MaterialPropertyBlock();
                block.Clear();
                block.SetColor(ColorId, coreColor * (0.4f + pulse));
                core.SetPropertyBlock(block);
            }
            if (rangeRing != null)
            {
                float size = BlastRadius * 2f;
                rangeRing.localScale = new Vector3(size, size, 1f) * (1f + 0.03f * pulse);
                rangeRing.rotation = Quaternion.Euler(0f, 0f, Age * 40f);
            }
            if (countdown != null)
            {
                countdown.transform.rotation = Quaternion.identity;
            }
            if (fuse <= 0f)
            {
                Detonate(false);
            }
        }


        protected override void Blow(bool byPlayer)
        {
            base.Blow(byPlayer);
            SpawnService spawner = Field != null ? Field.Spawner : null;
            if (spawner == null)
            {
                return;
            }
            float offset = Random.Range(0f, 360f);
            for (int i = 0; i < shrapnel; i++)
            {
                float angle = (offset + 360f * i / shrapnel) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                spawner.FireEnemyShot(EnemyShotKind.Shrapnel, Position + direction * radius, direction * shrapnelSpeed * Random.Range(0.85f, 1.15f));
            }
        }
    }
}
