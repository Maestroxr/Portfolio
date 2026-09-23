using System.Collections.Generic;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// Where a game is chosen: the chapters of the campaign as cards, each with the stars it was won with and locked
    /// until the one before it is won, or the skirmish maps; the story of the one picked on a page of parchment; and at
    /// the bottom where its battles are to be fought and the way in. It opens from the title screen and goes back to it.
    /// </summary>
    public sealed class CampaignScreen : Dialog
    {
        private const float CardHeight = 96f;
        private static readonly string[] Numerals = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII" };
        private static readonly string[] BattleLabels = { "On the map", "On a battlefield" };

        private RectTransform list;
        private ScrollRect scroll;
        private Image chaptersTab;
        private Image skirmishTab;
        private TextMeshProUGUI pageTitle;
        private TextMeshProUGUI story;
        private ScrollRect storyScroll;
        private RectTransform record;
        private RectTransform realms;
        private TextMeshProUGUI recordNote;
        private ChoiceSelector battles;
        private Button play;
        private bool skirmish;
        private int picked = -1;
        private readonly Dictionary<int, Image> cards = new Dictionary<int, Image>();

        public static CampaignScreen Make(Transform parent, HeroesGameManager manager)
        {
            CampaignScreen screen = Build<CampaignScreen>(parent, manager, "Campaign", new Vector2(1500f, 880f), "The Shattered Crown");
            RectTransform body = screen.Body;

            // The list on the left: two tabs over a column of cards.
            Image left = UIKit.Panel(body, "List");
            RectTransform leftRect = (RectTransform)left.transform;
            leftRect.anchorMin = new Vector2(0f, 0f);
            leftRect.anchorMax = new Vector2(0f, 1f);
            leftRect.pivot = new Vector2(0f, 0.5f);
            leftRect.offsetMin = new Vector2(0f, 0f);
            leftRect.offsetMax = new Vector2(700f, 0f);
            RectTransform leftContent = UIKit.Content(left, 6f);

            RectTransform tabs = UIKit.Rect(leftContent, "Tabs");
            tabs.anchorMin = new Vector2(0f, 1f);
            tabs.anchorMax = new Vector2(1f, 1f);
            tabs.pivot = new Vector2(0.5f, 1f);
            tabs.offsetMin = new Vector2(0f, -50f);
            tabs.offsetMax = Vector2.zero;
            HorizontalLayoutGroup tabRow = UIKit.Layout<HorizontalLayoutGroup>(tabs, 10f);
            tabRow.childForceExpandWidth = true;
            tabRow.childForceExpandHeight = true;
            screen.chaptersTab = Tab(tabs, "Chapters", "The Campaign", () => screen.Switch(false));
            screen.skirmishTab = Tab(tabs, "Skirmish", "Skirmish Maps", () => screen.Switch(true));

            screen.list = UIKit.Scroll(leftContent, "Cards", out screen.scroll);
            UIKit.Stretch((RectTransform)screen.scroll.transform, 0f, 0f, 0f, 60f);
            VerticalLayoutGroup layout = UIKit.Layout<VerticalLayoutGroup>(screen.list, 8f, new RectOffset(2, 2, 2, 2));
            layout.childForceExpandWidth = true;

            // The page on the right: the title of the chapter, its story, and the record of it at the bottom.
            Image page = UIKit.Parchment(body, "Page");
            RectTransform pageRect = (RectTransform)page.transform;
            pageRect.anchorMin = new Vector2(0f, 0f);
            pageRect.anchorMax = new Vector2(1f, 1f);
            pageRect.offsetMin = new Vector2(716f, 0f);
            pageRect.offsetMax = Vector2.zero;
            RectTransform pageContent = UIKit.Content(page, 14f);

            screen.pageTitle = UIKit.Title(pageContent, "Title", "", 34f, new Color(0.45f, 0.1f, 0.06f));
            RectTransform titleRect = (RectTransform)screen.pageTitle.transform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(0f, -48f);
            titleRect.offsetMax = Vector2.zero;
            UIKit.FitLine(screen.pageTitle, 34f, 20f);
            screen.pageTitle.characterSpacing = 2f;
            RectTransform rule = UIKit.Divider(pageContent, "Rule", 18f);
            rule.anchorMin = new Vector2(0.12f, 1f);
            rule.anchorMax = new Vector2(0.88f, 1f);
            rule.pivot = new Vector2(0.5f, 1f);
            rule.anchoredPosition = new Vector2(0f, -52f);
            rule.sizeDelta = new Vector2(0f, 18f);

            RectTransform words = UIKit.Scroll(pageContent, "Story", out screen.storyScroll);
            UIKit.Stretch((RectTransform)screen.storyScroll.transform, 6f, 262f, 6f, 82f);

            // The realms of the map: whose town, who plays it.
            TextMeshProUGUI realmsCaption = UIKit.Title(pageContent, "RealmsCaption", "The Realms", 20f, UIKit.DimOnParchment);
            RectTransform captionRect = (RectTransform)realmsCaption.transform;
            captionRect.anchorMin = new Vector2(0f, 0f);
            captionRect.anchorMax = new Vector2(1f, 0f);
            captionRect.pivot = new Vector2(0.5f, 0f);
            captionRect.offsetMin = new Vector2(0f, 222f);
            captionRect.offsetMax = new Vector2(0f, 250f);
            screen.realms = UIKit.Rect(pageContent, "Realms");
            screen.realms.anchorMin = new Vector2(0f, 0f);
            screen.realms.anchorMax = new Vector2(1f, 0f);
            screen.realms.pivot = new Vector2(0.5f, 0f);
            screen.realms.offsetMin = new Vector2(0f, 150f);
            screen.realms.offsetMax = new Vector2(0f, 216f);
            HorizontalLayoutGroup realmRow = UIKit.Layout<HorizontalLayoutGroup>(screen.realms, 10f);
            realmRow.childAlignment = TextAnchor.MiddleCenter;
            UIKit.Layout<VerticalLayoutGroup>(words, 0f).childForceExpandWidth = true;
            screen.story = UIKit.Label(words, "Text", "", 23f, UIKit.InkOnParchment, TextAlignmentOptions.TopLeft);
            screen.story.lineSpacing = 6f;

            RectTransform bottomRule = UIKit.Divider(pageContent, "RecordRule", 16f);
            bottomRule.anchorMin = new Vector2(0.2f, 0f);
            bottomRule.anchorMax = new Vector2(0.8f, 0f);
            bottomRule.pivot = new Vector2(0.5f, 0f);
            bottomRule.anchoredPosition = new Vector2(0f, 126f);
            bottomRule.sizeDelta = new Vector2(0f, 16f);
            screen.record = UIKit.Rect(pageContent, "Record");
            screen.record.anchorMin = new Vector2(0f, 0f);
            screen.record.anchorMax = new Vector2(1f, 0f);
            screen.record.pivot = new Vector2(0.5f, 0f);
            screen.record.offsetMin = new Vector2(0f, 44f);
            screen.record.offsetMax = new Vector2(0f, 118f);
            screen.recordNote = UIKit.Label(pageContent, "Note", "", 20f, UIKit.DimOnParchment, TextAlignmentOptions.Center);
            RectTransform noteRect = (RectTransform)screen.recordNote.transform;
            noteRect.anchorMin = new Vector2(0f, 0f);
            noteRect.anchorMax = new Vector2(1f, 0f);
            noteRect.pivot = new Vector2(0.5f, 0f);
            noteRect.offsetMin = new Vector2(0f, 4f);
            noteRect.offsetMax = new Vector2(0f, 38f);
            UIKit.FitLine(screen.recordNote, 20f, 14f);

            // Along the bottom: back to the title, where the battles are fought, and the way in.
            Button back = screen.Answer("Back", screen.Close);
            UIKit.Fit((RectTransform)back.transform, 200f, ButtonRow - 4f);
            screen.battles = Stepper.Make(screen.Buttons, "Battles", "Battles", BattleLabels, 470f, ButtonRow - 4f, 96f);
            UIKit.Fit((RectTransform)screen.battles.transform, 470f, ButtonRow - 4f);
            Tooltip.Attach(screen.battles.gameObject, "Battles",
                "On a battlefield: every battle is fought on a field of its own, with the armies drawn up on either side.\n" +
                "On the map: battles are fought where the armies meet, on the hexagons of the map.\nA saved game keeps the way it began.");
            screen.battles.OnValueChanged.AddListener(screen.SetBattles);
            screen.play = screen.Answer("Begin", screen.Begin);
            UIKit.Fit((RectTransform)screen.play.transform, 260f, ButtonRow - 4f);
            TextMeshProUGUI playLabel = screen.play.GetComponentInChildren<TextMeshProUGUI>();
            UIKit.FitLine(playLabel, 28f, 16f);
            UIKit.Look(playLabel, TextLook.Gold);
            return screen;
        }

        /// <summary>A tab over the list: a card with a title, bright while it is the one shown.</summary>
        private static Image Tab(RectTransform parent, string name, string text, System.Action onClick)
        {
            Button button = UIKit.CardButton(parent, name, onClick);
            var card = (Image)button.targetGraphic;
            TextMeshProUGUI label = UIKit.Title(card.transform, "Label", text, 22f, UIKit.Ink);
            UIKit.Stretch((RectTransform)label.transform, 12f, 2f, 12f, 2f);
            UIKit.FitLine(label, 22f, 14f);
            label.characterSpacing = 2f;
            return card;
        }

        private HeroesCampaign Campaign => Manager.Campaign as HeroesCampaign;

        /// <summary>Opens on the chapters or the skirmish maps, with the next chapter to play already picked.</summary>
        public void Show(bool skirmishMaps = false)
        {
            skirmish = skirmishMaps;
            picked = -1;
            Open();
            HeroesSettings options = Manager.Options;
            battles.SetValueWithoutNotify(options != null ? options.battleStyle : (int)BattleStyle.Battlefield);
            Fill();
        }

        /// <summary>Back goes to the title screen the campaign was opened from.</summary>
        public override void Close()
        {
            bool was = IsOpen;
            base.Close();
            if (was && Manager.HeroesUI != null)
            {
                Manager.HeroesUI.CampaignClosed();
            }
        }

        private void Switch(bool toSkirmish)
        {
            if (skirmish == toSkirmish)
            {
                return;
            }
            skirmish = toSkirmish;
            picked = -1;
            Fill();
        }

        /// <summary>Lists the chapters in their order, or the maps that can be played on their own.</summary>
        private void Fill()
        {
            HeroesCampaign campaign = Campaign;
            Heading.text = skirmish ? "Skirmish" : campaign != null ? campaign.Title : "Campaign";
            UIKit.Select(chaptersTab, !skirmish);
            UIKit.Select(skirmishTab, skirmish);
            SetTabInk(chaptersTab, !skirmish);
            SetTabInk(skirmishTab, skirmish);
            UIKit.Clear(list);
            cards.Clear();
            if (campaign == null)
            {
                return;
            }
            HeroesProgress progress = Manager.Progress;
            List<int> indices = skirmish ? campaign.Skirmishes : campaign.Chapters;
            for (int i = 0; i < indices.Count; i++)
            {
                int index = indices[i];
                HeroesLevel level = campaign.Scenario(index);
                if (level != null)
                {
                    cards[index] = Card(level, index, i, skirmish || campaign.IsUnlocked(index, progress), progress.Stars(index));
                }
            }
            // The first chapter still to be won is the one offered.
            int offer = -1;
            foreach (int index in indices)
            {
                if (skirmish || campaign.IsUnlocked(index, progress) && progress.Stars(index) == 0)
                {
                    offer = index;
                    break;
                }
            }
            if (offer < 0 && indices.Count > 0)
            {
                offer = indices[0];
            }
            if (offer >= 0)
            {
                Pick(offer);
            }
            scroll.verticalNormalizedPosition = 1f;
        }

        private static void SetTabInk(Image tab, bool on)
        {
            TextMeshProUGUI label = tab.GetComponentInChildren<TextMeshProUGUI>();
            label.color = on ? UIKit.Gold : UIKit.Dim;
        }

        /// <summary>
        /// A chapter or a map as a card: its number (or the town of its hero), its title and goal, and its stars; a
        /// locked chapter is greyed out and says why.
        /// </summary>
        private Image Card(HeroesLevel level, int index, int position, bool open, int stars)
        {
            Button button = UIKit.CardButton(list, level.Title, () => Pick(index));
            var card = (Image)button.targetGraphic;
            UIKit.Fit((RectTransform)card.transform, 0f, CardHeight, true);
            button.interactable = open;
            RectTransform content = UIKit.Content(card, 4f);

            if (skirmish)
            {
                Faction faction = HumanFaction(level);
                Image town = UIKit.PortraitFrame(content, "Town", Manager.Art.TownPortrait(faction, 0));
                UIKit.Pin((RectTransform)town.transform.parent.parent, new Vector2(0f, 0.5f), new Vector2(2f, 0f), new Vector2(72f, 72f));
            }
            else
            {
                Image medal = UIKit.Recess(content, "Number");
                UIKit.Pin((RectTransform)medal.transform, new Vector2(0f, 0.5f), new Vector2(2f, 0f), new Vector2(72f, 72f));
                TextMeshProUGUI numeral = UIKit.Title(medal.transform, "Numeral", position < Numerals.Length ? Numerals[position] : (position + 1).ToString(),
                    30f, open ? UIKit.Gold : UIKit.Dim);
                UIKit.Stretch((RectTransform)numeral.transform, 4f, 0f, 4f, 0f);
                UIKit.FitLine(numeral, 30f, 16f);
                numeral.characterSpacing = 0f;
                UIKit.Look(numeral, TextLook.Gold);
            }

            float right = skirmish ? 150f : 124f;
            TextMeshProUGUI title = UIKit.Title(content, "Title", level.Title, 24f, open ? UIKit.Ink : UIKit.Dim, TextAlignmentOptions.BottomLeft);
            RectTransform titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(90f, 2f);
            titleRect.offsetMax = new Vector2(-right, -4f);
            UIKit.FitLine(title, 24f, 15f);
            title.characterSpacing = 1f;

            string line = open ? level.Goal : "Locked: win the chapter before it.";
            TextMeshProUGUI goal = UIKit.Label(content, "Goal", line, 18f, UIKit.Dim, TextAlignmentOptions.TopLeft);
            goal.fontStyle = FontStyles.Italic;
            RectTransform goalRect = (RectTransform)goal.transform;
            goalRect.anchorMin = new Vector2(0f, 0f);
            goalRect.anchorMax = new Vector2(1f, 0.5f);
            goalRect.offsetMin = new Vector2(90f, 4f);
            goalRect.offsetMax = new Vector2(-right, -2f);
            UIKit.FitLine(goal, 18f, 13f);

            if (skirmish)
            {
                TextMeshProUGUI seats = UIKit.Label(content, "Players",
                    $"{level.Map.players.Count} players\n{Size(level)}", 18f, UIKit.Dim, TextAlignmentOptions.MidlineRight);
                RectTransform seatsRect = (RectTransform)seats.transform;
                seatsRect.anchorMin = new Vector2(1f, 0f);
                seatsRect.anchorMax = new Vector2(1f, 1f);
                seatsRect.pivot = new Vector2(1f, 0.5f);
                seatsRect.offsetMin = new Vector2(-right + 6f, 0f);
                seatsRect.offsetMax = new Vector2(-8f, 0f);
            }
            else if (open)
            {
                RectTransform row = UIKit.Stars(content, "Stars", stars, 32f, 3, 4f);
                UIKit.Pin(row, new Vector2(1f, 0.5f), new Vector2(-8f, 0f), row.sizeDelta);
            }
            if (!open)
            {
                var group = card.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0.6f;
                Tooltip.Attach(card.gameObject, "Locked", "Win the chapter before it to open this one.");
            }
            return card;
        }

        /// <summary>The size of the map a new game on <paramref name="level"/> gets, as columns by rows.</summary>
        private string Size(HeroesLevel level)
        {
            MapSpec map = Manager.NewGameMap(level);
            return $"{map.columns} x {map.rows}";
        }

        private static Faction HumanFaction(HeroesLevel level)
        {
            foreach (PlayerSpec player in level.Map.players)
            {
                if (player.human)
                {
                    return player.faction;
                }
            }
            return level.Map.players.Count > 0 ? level.Map.players[0].faction : Faction.Castle;
        }

        private void Pick(int index)
        {
            picked = index;
            foreach (KeyValuePair<int, Image> entry in cards)
            {
                UIKit.Select(entry.Value, entry.Key == index);
                TextMeshProUGUI title = entry.Value.transform.Find("Content/Title")?.GetComponent<TextMeshProUGUI>();
                if (title != null && entry.Value.GetComponent<Button>().interactable)
                {
                    title.color = entry.Key == index ? UIKit.Gold : UIKit.Ink;
                }
            }
            HeroesLevel level = Campaign != null ? Campaign.Scenario(index) : null;
            if (level == null)
            {
                UIKit.Enable(play, false);
                return;
            }
            pageTitle.text = level.Title;
            // The map as a new game would lay it out: a skirmish map at the size, riches and dangers of the settings.
            MapSpec map = Manager.NewGameMap(level);
            string intro = !string.IsNullOrEmpty(level.Intro) ? level.Intro.Trim() + "\n\n"
                : skirmish ? $"A land of its own for {map.players.Count} realms, with {Amount(map.treasure, "scarce", "fair", "rich")} " +
                             $"treasure and {Amount(map.monsters, "few", "some", "many", "hordes of")} wandering armies. Build up your " +
                             "towns, gather your armies and take the land.\n\n"
                : "";
            story.text = $"{intro}<b>{(skirmish ? "To win" : "Your task")}:</b> {level.Goal}";
            storyScroll.verticalNormalizedPosition = 1f;

            Realms(map);
            UIKit.Clear(record);
            int stars = Manager.Progress.Stars(index);
            if (skirmish)
            {
                TextMeshProUGUI about = UIKit.Label(record, "About",
                    $"{map.players.Count} realms on a map of {map.columns} by {map.rows}", 22f, UIKit.InkOnParchment,
                    TextAlignmentOptions.Center);
                UIKit.Stretch((RectTransform)about.transform);
                UIKit.FitLine(about, 22f, 14f);
                recordNote.text = "A skirmish is played for its own sake: it gives no stars.";
            }
            else
            {
                TextMeshProUGUI best = UIKit.Title(record, "Best", stars > 0 ? "Best" : "Not yet won", 22f, UIKit.DimOnParchment);
                RectTransform bestRect = (RectTransform)best.transform;
                bestRect.anchorMin = new Vector2(0f, 0f);
                bestRect.anchorMax = new Vector2(0.5f, 1f);
                bestRect.offsetMin = Vector2.zero;
                bestRect.offsetMax = new Vector2(-12f, 0f);
                best.alignment = TextAlignmentOptions.MidlineRight;
                RectTransform row = UIKit.Stars(record, "Stars", stars, 56f, 3, 8f);
                UIKit.Pin(row, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), row.sizeDelta);
                row.pivot = new Vector2(0f, 0.5f);
                recordNote.text = $"{UIKit.StarText(3)} within {level.ThreeStarDays} days     {UIKit.StarText(2)} within {level.TwoStarDays} days";
            }
            UIKit.Enable(play, true);
        }

        /// <summary>The realms of a map as chips: the picture of each one's town, its name, and who plays it.</summary>
        private void Realms(MapSpec map)
        {
            UIKit.Clear(realms);
            int count = map.players.Count;
            float width = Mathf.Min(250f, (realms.rect.width > 0f ? realms.rect.width : 640f) / Mathf.Max(1, count) - 10f);
            for (int i = 0; i < count; i++)
            {
                PlayerSpec player = map.players[i];
                Image chip = UIKit.Card(realms, $"Realm{i}");
                UIKit.Fit((RectTransform)chip.transform, width, 66f);
                RectTransform content = UIKit.Content(chip, 2f);
                Image town = UIKit.PortraitFrame(content, "Town", Manager.Art.TownPortrait(player.faction, i));
                UIKit.Pin((RectTransform)town.transform.parent.parent, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(50f, 50f));
                string name = player.human ? "You" : string.IsNullOrEmpty(player.name) ? Land.FactionName(player.faction) : player.name;
                TextMeshProUGUI title = UIKit.Label(content, "Name", name, 18f, player.human ? UIKit.Gold : UIKit.Ink, TextAlignmentOptions.BottomLeft, true);
                RectTransform titleRect = (RectTransform)title.transform;
                titleRect.anchorMin = new Vector2(0f, 0.5f);
                titleRect.anchorMax = new Vector2(1f, 1f);
                titleRect.offsetMin = new Vector2(58f, 0f);
                titleRect.offsetMax = new Vector2(-2f, -2f);
                UIKit.FitLine(title, 18f, 11f);
                string who = player.human ? Land.FactionName(player.faction) : $"{Land.FactionName(player.faction)}, {Skill(player.aiLevel)}";
                TextMeshProUGUI line = UIKit.Label(content, "Who", who, 15f, UIKit.Dim, TextAlignmentOptions.TopLeft);
                RectTransform lineRect = (RectTransform)line.transform;
                lineRect.anchorMin = new Vector2(0f, 0f);
                lineRect.anchorMax = new Vector2(1f, 0.5f);
                lineRect.offsetMin = new Vector2(58f, 2f);
                lineRect.offsetMax = new Vector2(-2f, 0f);
                UIKit.FitLine(line, 15f, 10f);
            }
        }

        /// <summary>The word for a setting of a map counted from 1 (1 is the first word).</summary>
        private static string Amount(int value, params string[] words)
        {
            return words[Mathf.Clamp(value - 1, 0, words.Length - 1)];
        }

        private static string Skill(int level)
        {
            return level <= 0 ? "easy" : level == 1 ? "normal" : "hard";
        }

        /// <summary>
        /// The player picked where battles are fought: the setting of the game changes (as a custom setting, saved
        /// like the others), and every new game is fought that way.
        /// </summary>
        private void SetBattles(int style)
        {
            Manager.HeroesUI?.SetBattleStyle(style);
        }

        private void Begin()
        {
            if (picked < 0)
            {
                return;
            }
            int level = picked;
            base.Close();
            Manager.BeginScenario(null, level);
        }
    }
}
