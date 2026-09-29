using System.Collections;
using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// The picture of a theme for the launcher's main menu (<see cref="GameTheme.Screenshot"/>). The board is interface
    /// here, so the base pose (which hides every screen canvas) would leave only the sky: the first level is dealt, half
    /// of its cards turn face up in a checkerboard, backs and faces of the theme side by side, and only the HUD, the
    /// banners and the tips are hidden.
    /// </summary>
    public partial class MemoryCardsGameManager
    {
        public override IEnumerator PoseForScreenshot()
        {
            controller.PrepareGame(LevelData.Create(0));
            float waited = 0f;
            while (phase != Phase.Playing && waited < 20f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1.2f);
            if (gameUI != null)
            {
                foreach (Component part in new Component[] { gameUI.hudScreen, gameUI.bannerGroup, gameUI.tipGroup })
                {
                    if (part != null)
                    {
                        part.gameObject.SetActive(false);
                    }
                }
                if (gameUI.memorizeRoot != null)
                {
                    gameUI.memorizeRoot.SetActive(false);
                }
            }
            List<float> columns = Distinct(view => view.Position.x);
            List<float> rows = Distinct(view => view.Position.y);
            for (int i = 0; i < views.Count; i++)
            {
                Vector2 at = views[i].Position;
                if ((Nearest(columns, at.x) + Nearest(rows, at.y)) % 2 == 0)
                {
                    views[i].FlipUp(i * 0.04f);
                }
            }
            yield return new WaitForSecondsRealtime(flipTime + views.Count * 0.04f + 0.6f);
        }

        /// <summary>The columns (or rows) of the board: the distinct values of a coordinate of the cards, in order.</summary>
        private List<float> Distinct(System.Func<Flippable, float> coordinate)
        {
            var values = new List<float>();
            foreach (Flippable view in views)
            {
                float value = coordinate(view);
                if (values.TrueForAll(known => Mathf.Abs(known - value) > 1f))
                {
                    values.Add(value);
                }
            }
            values.Sort();
            return values;
        }

        private static int Nearest(List<float> values, float value)
        {
            int best = 0;
            for (int i = 1; i < values.Count; i++)
            {
                if (Mathf.Abs(values[i] - value) < Mathf.Abs(values[best] - value))
                {
                    best = i;
                }
            }
            return best;
        }
    }
}
