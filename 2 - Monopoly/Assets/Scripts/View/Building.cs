using System.Collections;
using Gamebox;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>A house or hotel on the board, taken from a <see cref="BuildingPool"/>; drops in with a bounce when built.</summary>
    public class Building : MonoBehaviour
    {
        [SerializeField] private bool hotel;
        [SerializeField] private Transform model;

        public bool IsHotel => hotel;

        public void Show()
        {
            gameObject.SetActive(true);
            if (model != null)
            {
                model.localPosition = Vector3.zero;
                model.localScale = Vector3.one;
            }
        }

        public void Drop()
        {
            if (isActiveAndEnabled && model != null)
            {
                StopAllCoroutines();
                StartCoroutine(DropRoutine());
            }
        }

        private IEnumerator DropRoutine()
        {
            const float fall = 0.28f;
            const float settle = 0.35f;
            float time = 0f;
            while (time < fall)
            {
                float t = time / fall;
                model.localPosition = new Vector3(0f, Mathf.Lerp(1.4f, 0f, t * t), 0f);
                model.localScale = Vector3.one;
                yield return null;
                time += Time.deltaTime;
            }
            time = 0f;
            while (time < settle)
            {
                float t = time / settle;
                float squash = Mathf.Sin(t * Mathf.PI * 2.5f) * (1f - t) * 0.22f;
                model.localPosition = Vector3.zero;
                model.localScale = new Vector3(1f + squash, 1f - squash, 1f + squash);
                yield return null;
                time += Time.deltaTime;
            }
            model.localPosition = Vector3.zero;
            model.localScale = Vector3.one;
        }
    }
}
