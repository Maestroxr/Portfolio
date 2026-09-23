using System.Collections.Generic;
using Gamebox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// Where a game is chosen: the chapters of the campaign, each with the stars it was won with and locked until the
    /// one before it is done, and the skirmish maps, which can be set up as the player likes. It stands in for the
    /// shared menu's New Game button.
    /// </summary>
    public sealed class CampaignScreen : Dialog
    {
        private RectTransform list;
        private TextMeshProUGUI blurb;
        private TextMeshProUGUI record;
        private Button play;
        private Button tab;
        private bool skirmish;
        private int picked = -1;
        private readonly List<Button> entries = new List<Button>();

        public static CampaignScreen Make(Transform parent, HeroesGameManager manager)
        {
            CampaignScreen screen = Build<CampaignScreen>(parent, manager, "Campaign", new Vector2(1180f, 780f),
                "The Shattered Crown");

            Image left = UIKit.Panel(screen.Body, "List", 0.85f);
            RectTransform leftRect = UIKit.Pin((RectTransform)left.transform, new Vector2(0f, 1f), new Vector2(0f, 0f),
                new Vector2(620f, 560f));
            screen.list = UIKit.Scroll(leftRect, "Scroll", out ScrollRect scroll);
            UIKit.Stretch((RectTransform)scroll.transform, 16f, 16f, 16f, 16f);
            VerticalLayoutGroup layout = UIKit.Layout<VerticalLayoutGroup>(screen.list, 6f);
            layout.childForceExpandWidth = true;

            Image right = UIKit.Panel(screen.Body, "Detail", 0.85f);
            RectTransform rightRect = UIKit.Pin((RectTransform)right.transform, new Vector2(1f, 1f), new Vector2(0f, 0f),
                new Vector2(450f, 560f));
            screen.blurb = UIKit.Label(rightRect, "Blurb", "", 22f, UIKit.Ink, TextAlignmentOptions.TopLeft);
            UIKit.Pin((RectTransform)screen.blurb.transform, new Vector2(0f, 1f), new Vector2(24f, -28f), new Vector2(402f, 420f));
            screen.record = UIKit.Label(rightRect, "Record", "", 20f, UIKit.Dim, TextAlignmentOptions.BottomLeft);
            UIKit.Pin((RectTransform)screen.record.transform, new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(402f, 100f));

            screen.tab = screen.Answer("Skirmish Maps", screen.Switch);
            screen.play = screen.Answer("Begin", screen.Begin);
            screen.Answer("Back", screen.Close);
            return screen;
        }

        private HeroesCampaign Campaign => Manager.Campaign as HeroesCampaign;

        /// <summary>Opens the list, on the campaign, with the next chapter to play already picked.</summary>
        public void Show()
        {
            skirmish = false;
            picked = -1;
            Open();
            Fill();
        }

        private void Switch()
        {
            skirmish = !skirmish;
            picked = -1;
            Fill();
        }

        /// <summary>Lists either the chapters in their order or the maps that can be played on their own.</summary>
        private void Fill()
        {
            HeroesCampaign campaign = Campaign;
            Heading.text = skirmish ? "Skirmish Maps" : campaign != null ? campaign.Title : "Campaign";
            tab.GetComponentInChildren<TextMeshProUGUI>().text = skirmish ? "Campaign" : "Skirmish Maps";
            for (int i = list.childCount - 1; i >= 0; i--)
            {
                Destroy(list.GetChild(i).gameObject);
            }
            entries.Clear();
            if (campaign == null)
            {
                return;
            }
            HeroesProgress progress = Manager.Progress;
            List<int> indices = skirmish ? campaign.Skirmishes : campaign.Chapters;
            foreach (int index in indices)
            {
                HeroesLevel level = campaign.Scenario(index);
                if (level == null)
                {
                    continue;
                }
                bool open = skirmish || campaign.IsUnlocked(index, progress);
                int stars = progress.Stars(index);
                int which = index;

                Image row = UIKit.Panel(list, level.Title, open ? 0.9f : 0.45f);
                UIKit.Fit((RectTransform)row.transform, 0f, 74f, true);
                var button = row.gameObject.AddComponent<Button>();
                button.targetGraphic = row;
                button.colors = UIKit.Tint();
                button.interactable = open;
                button.onClick.AddListener(() => Pick(which));
                row.gameObject.AddComponent<Clicker>();

                TextMeshProUGUI title = UIKit.Label(row.transform, "Title",
                    skirmish ? level.Title : $"{indices.IndexOf(index) + 1}. {level.Title}", 24f,
                    open ? UIKit.Ink : UIKit.Dim);
                UIKit.Pin((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(18f, -12f), new Vector2(380f, 30f));
                TextMeshProUGUI note = UIKit.Label(row.transform, "Note", open ? level.Goal : "Win the chapter before it.",
                    17f, UIKit.Dim);
                UIKit.Pin((RectTransform)note.transform, new Vector2(0f, 1f), new Vector2(18f, -42f), new Vector2(420f, 24f));

                for (int star = 0; star < 3 && !skirmish; star++)
                {
                    Image mark = UIKit.Sprite(row.transform, $"Star{star}",
                        star < stars ? Manager.Art.star : Manager.Art.starEmpty, Color.white);
                    UIKit.Pin((RectTransform)mark.transform, new Vector2(1f, 0.5f),
                        new Vector2(-18f - star * 38f, 0f), new Vector2(34f, 34f));
                    ((RectTransform)mark.transform).pivot = new Vector2(1f, 0.5f);
                }
                entries.Add(button);
            }
            // The first chapter that is still to be won is the one offered.
            foreach (int index in indices)
            {
                if (skirmish || campaign.IsUnlocked(index, progress) && progress.Stars(index) == 0)
                {
                    Pick(index);
                    break;
                }
            }
            if (picked < 0 && indices.Count > 0)
            {
                Pick(indices[0]);
            }
        }

        private void Pick(int index)
        {
            picked = index;
            HeroesLevel level = Campaign != null ? Campaign.Scenario(index) : null;
            if (level == null)
            {
                return;
            }
            blurb.text = string.IsNullOrEmpty(level.Intro)
                ? $"{level.Title}\n\n{level.Goal}"
                : $"{level.Title}\n\n{level.Intro}\n\n{level.Goal}";
            int stars = Manager.Progress.Stars(index);
            record.text = skirmish
                ? $"A map for {level.Map.players.Count} players."
                : $"Best: {(stars > 0 ? new string('★', stars) : "not yet won")}\n" +
                  $"Three stars in {level.ThreeStarDays} days, two in {level.TwoStarDays}.";
            UIKit.Enable(play, true);
        }

        private void Begin()
        {
            if (picked < 0)
            {
                return;
            }
            Close();
            Manager.BeginScenario(null, picked);
        }
    }
}
