using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// The book of a hero: his portrait, his calling and his experience; his four primary skills as tiles, his morale,
    /// luck, mana and movement; the eight places of his secondary skills; the artifacts he wears and carries and the
    /// spells in his book; and his army along the bottom, where creatures can be shifted between the places.
    /// </summary>
    public sealed class HeroScreen : Dialog
    {
        private const float ArmyHeight = 168f;
        private const float PortraitSize = 150f;
        private const float SkillRow = 60f;
        private const float ItemSize = 64f;
        private const float SpellSize = 76f;

        private HeroState hero;
        private Image portrait;
        private Image pennant;
        private TextMeshProUGUI heroName;
        private TextMeshProUGUI calling;
        private Image experience;
        private TextMeshProUGUI experienceLabel;
        private readonly TextMeshProUGUI[] stats = new TextMeshProUGUI[4];
        private TextMeshProUGUI morale;
        private TextMeshProUGUI luck;
        private TextMeshProUGUI manaLabel;
        private Image manaBar;
        private TextMeshProUGUI movementLabel;
        private Image movementBar;
        private TextMeshProUGUI biography;
        private RectTransform skills;
        private RectTransform equipped;
        private RectTransform pack;
        private TextMeshProUGUI packCaption;
        private RectTransform spells;
        private TextMeshProUGUI spellCaption;
        private ArmyRow army;
        private TextMeshProUGUI strength;
        private Button dismiss;

        public static HeroScreen Make(Transform parent, HeroesGameManager manager)
        {
            HeroScreen screen = Build<HeroScreen>(parent, manager, "Hero", new Vector2(1660f, UnderBarHeight), "Hero");
            screen.KeepUnderTopBar();
            RectTransform body = screen.Body;
            RectTransform top = UIKit.Rect(body, "Top");
            UIKit.Stretch(top, 0f, ArmyHeight + 12f, 0f, 0f);
            screen.Person(Column(top, "Hero", 0f, 0.29f, 0f, 6f));
            screen.SkillList(Column(top, "Skills", 0.29f, 0.61f, 6f, 6f));
            screen.Gear(Column(top, "Gear", 0.61f, 1f, 6f, 0f));
            screen.ArmyPanel(body);
            Button close = screen.Answer("Close", screen.Close);
            UIKit.Fit((RectTransform)close.transform, 240f, ButtonRow - 4f);
            return screen;
        }

        /// <summary>A panel of the upper part, between two fractions of its width; returns the area inside its frame.</summary>
        private static RectTransform Column(RectTransform parent, string name, float from, float to, float left, float right)
        {
            Image panel = UIKit.Panel(parent, name);
            RectTransform rect = (RectTransform)panel.transform;
            rect.anchorMin = new Vector2(from, 0f);
            rect.anchorMax = new Vector2(to, 1f);
            rect.offsetMin = new Vector2(left, 0f);
            rect.offsetMax = new Vector2(-right, 0f);
            return UIKit.Content(panel, 8f);
        }

        // ------------------------------------------------------------------ building

        /// <summary>The hero himself: portrait, name, level and experience, primary skills, the four figures, his story.</summary>
        private void Person(RectTransform area)
        {
            portrait = UIKit.PortraitFrame(area, "Portrait", null);
            UIKit.Pin((RectTransform)portrait.transform.parent.parent, new Vector2(0f, 1f), Vector2.zero, new Vector2(PortraitSize, PortraitSize));
            pennant = UIKit.Pennant(area, "Pennant", Color.white);
            UIKit.Pin((RectTransform)pennant.transform, new Vector2(0f, 1f), new Vector2(PortraitSize - 30f, 6f), new Vector2(22f, 42f));

            heroName = UIKit.Heading(area, "Name", "", 30f, TextAlignmentOptions.TopLeft);
            Beside((RectTransform)heroName.transform, 2f, 40f);
            UIKit.FitLine(heroName, 30f, 16f);
            calling = UIKit.Label(area, "Calling", "", 20f, UIKit.Dim, TextAlignmentOptions.TopLeft);
            calling.fontStyle = FontStyles.Italic;
            Beside((RectTransform)calling.transform, 44f, 28f);
            UIKit.FitLine(calling, 20f, 13f);

            TextMeshProUGUI expCaption = UIKit.Label(area, "ExperienceCaption", $"{UIKit.Glyph("experience")} Experience", 17f, UIKit.Dim,
                TextAlignmentOptions.BottomLeft);
            Beside((RectTransform)expCaption.transform, 82f, 24f);
            experience = UIKit.Meter(area, "Experience", new Color(0.9f, 0.74f, 0.32f));
            RectTransform meter = (RectTransform)experience.transform.parent;
            meter.anchorMin = new Vector2(0f, 1f);
            meter.anchorMax = new Vector2(1f, 1f);
            meter.pivot = new Vector2(0f, 1f);
            meter.offsetMin = new Vector2(PortraitSize + 14f, -128f);
            meter.offsetMax = new Vector2(0f, -110f);
            experienceLabel = UIKit.Label(area, "ExperienceLabel", "", 16f, UIKit.Dim, TextAlignmentOptions.TopRight);
            RectTransform expRect = (RectTransform)experienceLabel.transform;
            expRect.anchorMin = new Vector2(0f, 1f);
            expRect.anchorMax = new Vector2(1f, 1f);
            expRect.pivot = new Vector2(0f, 1f);
            expRect.offsetMin = new Vector2(PortraitSize + 14f, -152f);
            expRect.offsetMax = new Vector2(0f, -130f);
            UIKit.FitLine(experienceLabel, 16f, 11f);

            // The four primary skills, as tiles with their icons.
            RectTransform tiles = UIKit.Rect(area, "Primary");
            tiles.anchorMin = new Vector2(0f, 1f);
            tiles.anchorMax = new Vector2(1f, 1f);
            tiles.pivot = new Vector2(0.5f, 1f);
            tiles.offsetMin = new Vector2(0f, -PortraitSize - 128f);
            tiles.offsetMax = new Vector2(0f, -PortraitSize - 14f);
            HorizontalLayoutGroup row = UIKit.Layout<HorizontalLayoutGroup>(tiles, 8f);
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;
            for (int i = 0; i < 4; i++)
            {
                stats[i] = StatTile(tiles, (PrimaryStat)i);
            }

            // Morale and luck, mana and movement, in two rows of sunken boxes.
            RectTransform figures = UIKit.Rect(area, "Figures");
            figures.anchorMin = new Vector2(0f, 1f);
            figures.anchorMax = new Vector2(1f, 1f);
            figures.pivot = new Vector2(0.5f, 1f);
            figures.offsetMin = new Vector2(0f, -PortraitSize - 268f);
            figures.offsetMax = new Vector2(0f, -PortraitSize - 140f);
            GridLayoutGroup grid = figures.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.spacing = new Vector2(8f, 8f);
            grid.cellSize = new Vector2(206f, 60f);
            morale = UIKit.Figure(figures, "Morale", UIKit.Art.Icon("morale"), true,
                "Morale\nGood morale may give a creature a second turn; bad morale may make it lose one.", 212f);
            luck = UIKit.Figure(figures, "Luck", UIKit.Art.Icon("luck"), true,
                "Luck\nGood luck may make a blow strike twice as hard.", 212f);
            manaLabel = MeterFigure(figures, "Mana", UIKit.Art.Icon("mana"), new Color(0.35f, 0.6f, 1f), out manaBar,
                "Mana\nWhat the hero's spells cost. It comes back day by day, and in full in a town with a Mage Guild.");
            movementLabel = MeterFigure(figures, "Movement", UIKit.Art.Icon("movement"), new Color(0.45f, 0.85f, 0.35f), out movementBar,
                "Movement\nHow far the hero can still go today.");

            Image page = UIKit.Parchment(area, "Story");
            RectTransform pageRect = (RectTransform)page.transform;
            pageRect.anchorMin = Vector2.zero;
            pageRect.anchorMax = Vector2.one;
            pageRect.offsetMin = Vector2.zero;
            pageRect.offsetMax = new Vector2(0f, -PortraitSize - 282f);
            RectTransform words = UIKit.Content(page, 12f);
            biography = UIKit.Label(words, "Text", "", 19f, UIKit.InkOnParchment, TextAlignmentOptions.TopLeft);
            biography.fontStyle = FontStyles.Italic;
            UIKit.Stretch((RectTransform)biography.transform);
            biography.enableAutoSizing = true;
            biography.fontSizeMax = 19f;
            biography.fontSizeMin = 12f;
            biography.overflowMode = TextOverflowModes.Ellipsis;
        }

        /// <summary>A line of the column to the right of the portrait, <paramref name="top"/> down, as wide as the column.</summary>
        private static void Beside(RectTransform rect, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = new Vector2(PortraitSize + 14f, -top - height);
            rect.offsetMax = new Vector2(0f, -top);
        }

        /// <summary>A primary skill: its icon, its value in gold, its name.</summary>
        private static TextMeshProUGUI StatTile(RectTransform parent, PrimaryStat stat)
        {
            Image tile = UIKit.Card(parent, stat.ToString());
            RectTransform content = UIKit.Content(tile, 2f);
            Sprite sprite = UIKit.Art.statIcons.Length > (int)stat ? UIKit.Art.statIcons[(int)stat] : null;
            Image icon = UIKit.Sprite(content, "Icon", sprite, Color.white);
            icon.preserveAspect = true;
            UIKit.Pin((RectTransform)icon.transform, new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(40f, 40f));
            TextMeshProUGUI value = UIKit.Label(content, "Value", "", 28f, UIKit.Gold, TextAlignmentOptions.Center, true);
            RectTransform valueRect = (RectTransform)value.transform;
            valueRect.anchorMin = new Vector2(0f, 0f);
            valueRect.anchorMax = new Vector2(1f, 1f);
            valueRect.offsetMin = new Vector2(0f, 20f);
            valueRect.offsetMax = new Vector2(0f, -42f);
            UIKit.FitLine(value, 28f, 16f);
            UIKit.Look(value, TextLook.Gold);
            TextMeshProUGUI name = UIKit.Label(content, "Name", Short(stat), 15f, UIKit.Dim, TextAlignmentOptions.Bottom);
            UIKit.Stretch((RectTransform)name.transform, 0f, 2f, 0f, 0f);
            UIKit.FitLine(name, 15f, 10f);
            Tooltip.Attach(tile.gameObject, HeroData.StatName(stat), StatHelp(stat), sprite);
            return value;
        }

        private static string Short(PrimaryStat stat)
        {
            switch (stat)
            {
                case PrimaryStat.Attack: return "Attack";
                case PrimaryStat.Defense: return "Defense";
                case PrimaryStat.Power: return "Power";
                default: return "Knowledge";
            }
        }

        private static string StatHelp(PrimaryStat stat)
        {
            switch (stat)
            {
                case PrimaryStat.Attack: return "Every creature of the army strikes harder.";
                case PrimaryStat.Defense: return "Every creature of the army takes less from a blow.";
                case PrimaryStat.Power: return "The hero's spells do more and last longer.";
                default: return "Ten points of mana for every point.";
            }
        }

        /// <summary>A sunken box with an icon, a line of text and a bar under it.</summary>
        private static TextMeshProUGUI MeterFigure(RectTransform parent, string name, Sprite icon, Color color, out Image bar, string tip)
        {
            TextMeshProUGUI text = UIKit.Figure(parent, name, icon, true, tip, 212f);
            RectTransform textRect = (RectTransform)text.transform;
            textRect.anchorMin = new Vector2(0f, 0.42f);
            textRect.offsetMin = new Vector2(textRect.offsetMin.x, 0f);
            UIKit.FitLine(text, 17f, 11f);
            bar = UIKit.Meter(text.transform.parent, "Bar", color);
            RectTransform meter = (RectTransform)bar.transform.parent;
            meter.anchorMin = new Vector2(0f, 0f);
            meter.anchorMax = new Vector2(1f, 0f);
            meter.pivot = new Vector2(0.5f, 0f);
            meter.offsetMin = new Vector2(textRect.offsetMin.x, 3f);
            meter.offsetMax = new Vector2(-6f, 15f);
            return text;
        }

        /// <summary>The eight places of the secondary skills, the empty ones as faint cards.</summary>
        private void SkillList(RectTransform area)
        {
            UIKit.SectionTitle(area, "Caption", "Skills", 24f);
            skills = UIKit.Rect(area, "List");
            UIKit.Stretch(skills, 0f, 0f, 0f, 54f);
            VerticalLayoutGroup list = UIKit.Layout<VerticalLayoutGroup>(skills, 6f);
            list.childForceExpandWidth = true;
        }

        /// <summary>The artifacts worn in their eight places, those carried in the pack, and the spellbook.</summary>
        private void Gear(RectTransform area)
        {
            UIKit.SectionTitle(area, "Caption", "Artifacts", 24f);
            equipped = UIKit.Rect(area, "Equipped");
            equipped.anchorMin = new Vector2(0f, 1f);
            equipped.anchorMax = new Vector2(1f, 1f);
            equipped.pivot = new Vector2(0.5f, 1f);
            equipped.offsetMin = new Vector2(0f, -54f - ItemSize);
            equipped.offsetMax = new Vector2(0f, -54f);
            GridLayoutGroup worn = UIKit.Grid(equipped, new Vector2(ItemSize, ItemSize), new Vector2(6f, 6f), 8);
            worn.childAlignment = TextAnchor.UpperCenter;

            packCaption = UIKit.Label(area, "PackCaption", "", 17f, UIKit.Dim, TextAlignmentOptions.MidlineLeft);
            RectTransform captionRect = (RectTransform)packCaption.transform;
            captionRect.anchorMin = new Vector2(0f, 1f);
            captionRect.anchorMax = new Vector2(1f, 1f);
            captionRect.pivot = new Vector2(0.5f, 1f);
            captionRect.offsetMin = new Vector2(4f, -54f - ItemSize - 34f);
            captionRect.offsetMax = new Vector2(0f, -54f - ItemSize - 8f);
            pack = UIKit.Scroll(area, "Pack", out ScrollRect packScroll);
            RectTransform packRect = (RectTransform)packScroll.transform;
            packRect.anchorMin = new Vector2(0f, 1f);
            packRect.anchorMax = new Vector2(1f, 1f);
            packRect.pivot = new Vector2(0.5f, 1f);
            packRect.offsetMin = new Vector2(0f, -54f - ItemSize - 36f - 54f);
            packRect.offsetMax = new Vector2(0f, -54f - ItemSize - 36f);
            GridLayoutGroup carried = UIKit.Grid(pack, new Vector2(52f, 52f), new Vector2(6f, 6f), 9);
            carried.childAlignment = TextAnchor.UpperLeft;
            carried.padding = new RectOffset(4, 0, 0, 0);

            float spellTop = 54f + ItemSize + 36f + 54f + 14f;
            RectTransform spellTitle = UIKit.Rect(area, "SpellTitle");
            spellTitle.anchorMin = new Vector2(0f, 1f);
            spellTitle.anchorMax = new Vector2(1f, 1f);
            spellTitle.pivot = new Vector2(0.5f, 1f);
            spellTitle.offsetMin = new Vector2(0f, -spellTop - 54f);
            spellTitle.offsetMax = new Vector2(0f, -spellTop);
            spellCaption = UIKit.SectionTitle(spellTitle, "Caption", "Spellbook", 24f);
            spells = UIKit.Scroll(area, "Spells", out ScrollRect spellScroll);
            UIKit.Stretch((RectTransform)spellScroll.transform, 0f, 0f, 0f, spellTop + 54f);
            GridLayoutGroup book = UIKit.Grid(spells, new Vector2(SpellSize, SpellSize), new Vector2(8f, 8f), 6);
            book.childAlignment = TextAnchor.UpperCenter;
        }

        /// <summary>The army along the bottom, its strength, and the way to send a stack home.</summary>
        private void ArmyPanel(RectTransform body)
        {
            Image panel = UIKit.Panel(body, "Army");
            RectTransform rect = (RectTransform)panel.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(0f, ArmyHeight);
            RectTransform content = UIKit.Content(panel, 6f);

            TextMeshProUGUI caption = UIKit.Heading(content, "Caption", "Army", 26f, TextAlignmentOptions.MidlineLeft);
            UIKit.Pin((RectTransform)caption.transform, new Vector2(0f, 0.5f), new Vector2(8f, 18f), new Vector2(200f, 36f));
            strength = UIKit.Label(content, "Strength", "", 18f, UIKit.Dim, TextAlignmentOptions.TopLeft);
            UIKit.Pin((RectTransform)strength.transform, new Vector2(0f, 0.5f), new Vector2(8f, -16f), new Vector2(220f, 50f));
            float size = 120f;
            army = ArmyRow.Make(content, Manager, size);
            UIKit.Pin((RectTransform)army.transform, new Vector2(0.5f, 0.5f), new Vector2(-40f, 0f),
                new Vector2(7f * size + 6f * Mathf.Round(size * 0.1f), size));
            dismiss = UIKit.Push(content, "Dismiss", "Dismiss Stack", DismissPicked, 21f);
            UIKit.Pin((RectTransform)dismiss.transform, new Vector2(1f, 0.5f), new Vector2(-4f, 0f), new Vector2(210f, 56f));
            Tooltip.Attach(dismiss.gameObject, "Dismiss Stack", "Pick a stack in the army first; it is sent home for good.");
        }

        // ------------------------------------------------------------------ showing a hero

        public void Open(HeroState which)
        {
            hero = which;
            ArmyRow.Drop();
            Open();
            Refresh();
        }

        public override void Close()
        {
            base.Close();
            ArmyRow.Drop();
        }

        public void Refresh()
        {
            if (!IsOpen || hero == null || Manager.Game == null)
            {
                return;
            }
            hero = Manager.Game.State.Hero(hero.id);
            if (hero == null || !hero.alive)
            {
                Close();
                return;
            }
            HeroesGame game = Manager.Game;
            HeroClass classId = hero.Def != null ? hero.Def.Class : HeroClass.Knight;
            HeroClassDef heroClass = HeroData.Class(classId);
            PlayerState owner = game.State.Player(hero.owner);
            Heading.text = hero.Name;
            portrait.sprite = Manager.Art.HeroPortrait(classId);
            pennant.color = HeroesArt.PlayerColor(owner != null ? (int)owner.color : 4);
            heroName.text = hero.Name;
            calling.text = $"Level {hero.level} {heroClass.Name}";

            int now = HeroData.ExperienceFor(hero.level);
            int next = HeroData.ExperienceFor(hero.level + 1);
            experience.fillAmount = next > now ? Mathf.Clamp01((hero.experience - now) / (float)(next - now)) : 1f;
            experienceLabel.text = hero.level >= HeroData.MaxLevel ? $"{hero.experience:N0}, the highest level"
                : $"{hero.experience:N0} of {next:N0} for level {hero.level + 1}";

            for (int i = 0; i < 4; i++)
            {
                stats[i].text = game.Stat(hero, (PrimaryStat)i).ToString();
            }
            int moraleValue = game.Morale(hero);
            int luckValue = game.Luck(hero);
            morale.text = $"Morale {UIKit.Signed(moraleValue)}  <color=#B8AB8F>{Mood(moraleValue)}</color>";
            luck.text = $"Luck {UIKit.Signed(luckValue)}  <color=#B8AB8F>{Fortune(luckValue)}</color>";
            int maxMana = game.MaxMana(hero);
            manaLabel.text = $"Mana {hero.mana} / {maxMana}";
            manaBar.fillAmount = maxMana > 0 ? Mathf.Clamp01(hero.mana / (float)maxMana) : 0f;
            movementLabel.text = $"Moves {hero.movement} / {hero.maxMovement}";
            movementBar.fillAmount = hero.maxMovement > 0 ? Mathf.Clamp01(hero.movement / (float)hero.maxMovement) : 0f;
            biography.text = hero.Def != null ? hero.Def.Biography : "";

            Skills();
            ShowArtifacts();
            Spellbook();
            army.Bind(Holder.Hero(hero.id));
            int power = game.Strength(hero);
            strength.text = $"{UIKit.Glyph("attack")} Strength {power:N0}\n{hero.army.TotalCreatures:N0} creatures";
            UIKit.Enable(dismiss, hero.owner == Manager.Viewer && !Manager.HeroesUI.Busy);
        }

        private static string Mood(int value)
        {
            return value >= 2 ? "Great" : value == 1 ? "Good" : value == 0 ? "Normal" : value == -1 ? "Poor" : "Terrible";
        }

        private static string Fortune(int value)
        {
            return value >= 2 ? "Blessed" : value == 1 ? "Good" : value == 0 ? "Normal" : value == -1 ? "Bad" : "Cursed";
        }

        private void Skills()
        {
            UIKit.Clear(skills);
            for (int i = 0; i < HeroData.MaxSkills; i++)
            {
                SkillEntry entry = i < hero.skills.Count ? hero.skills[i] : null;
                Image row = UIKit.Card(skills, entry != null ? $"Skill{i}" : "Empty");
                UIKit.Fit((RectTransform)row.transform, 0f, SkillRow, true);
                RectTransform content = UIKit.Content(row, 2f);
                if (entry == null)
                {
                    row.color = new Color(1f, 1f, 1f, 0.35f);
                    TextMeshProUGUI empty = UIKit.Label(content, "Name", "An empty place for a skill", 17f, new Color(0.72f, 0.67f, 0.56f, 0.6f),
                        TextAlignmentOptions.Center);
                    empty.fontStyle = FontStyles.Italic;
                    UIKit.Stretch((RectTransform)empty.transform);
                    continue;
                }
                var id = (SkillId)entry.skill;
                SkillDef def = HeroData.Skill(id);
                Sprite sprite = Manager.Art.Skill(id);
                Image icon = UIKit.PortraitFrame(content, "Icon", sprite, true);
                UIKit.Stretch((RectTransform)icon.transform, 6f, 6f, 6f, 6f);
                UIKit.Pin((RectTransform)icon.transform.parent.parent, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(46f, 46f));
                TextMeshProUGUI name = UIKit.Label(content, "Name", $"{HeroData.SkillLevelName(entry.level)} {def.Name}", 20f, UIKit.Ink,
                    TextAlignmentOptions.BottomLeft, true);
                RectTransform nameRect = (RectTransform)name.transform;
                nameRect.anchorMin = new Vector2(0f, 0.5f);
                nameRect.anchorMax = new Vector2(1f, 1f);
                nameRect.offsetMin = new Vector2(58f, 0f);
                nameRect.offsetMax = new Vector2(-4f, -2f);
                UIKit.FitLine(name, 20f, 12f);
                string level = entry.level >= 1 && entry.level <= def.Levels.Length ? def.Levels[entry.level - 1] : def.Description;
                TextMeshProUGUI text = UIKit.Label(content, "Text", level, 16f, UIKit.Dim, TextAlignmentOptions.TopLeft);
                RectTransform textRect = (RectTransform)text.transform;
                textRect.anchorMin = new Vector2(0f, 0f);
                textRect.anchorMax = new Vector2(1f, 0.5f);
                textRect.offsetMin = new Vector2(58f, 0f);
                textRect.offsetMax = new Vector2(-4f, -1f);
                UIKit.FitLine(text, 16f, 11f);
                Tooltip.Attach(row.gameObject, $"{HeroData.SkillLevelName(entry.level)} {def.Name}", $"{def.Description}\n{level}", sprite);
            }
        }

        private void ShowArtifacts()
        {
            UIKit.Clear(equipped);
            for (int slot = 0; slot < hero.equipped.Length; slot++)
            {
                int id = hero.equipped[slot];
                ArtifactDef def = id >= 0 ? Artifacts.Get((ArtifactId)id) : null;
                Image frame = UIKit.Slot(equipped, $"Slot{slot}");
                string place = Artifacts.SlotName((ArtifactSlot)slot);
                if (def != null)
                {
                    Item(frame, (ArtifactId)id, def, place);
                }
                else
                {
                    // An empty place shows a faint picture of what is worn there (a helm, a boot, a ring); its name is in
                    // the tooltip.
                    Image mark = UIKit.Sprite(frame.transform, "Place", Manager.Art.Icon(PlaceIcon((ArtifactSlot)slot)),
                        new Color(0.72f, 0.67f, 0.56f, 0.3f));
                    UIKit.Stretch((RectTransform)mark.transform, 13f, 13f, 13f, 13f);
                    mark.preserveAspect = true;
                    mark.raycastTarget = false;
                    mark.enabled = mark.sprite != null;
                    Tooltip.Attach(frame.gameObject, place, "Nothing is worn here.");
                }
            }
            UIKit.Clear(pack);
            foreach (int id in hero.backpack)
            {
                ArtifactDef def = Artifacts.Get((ArtifactId)id);
                Image frame = UIKit.Slot(pack, "Carried");
                if (def != null)
                {
                    Item(frame, (ArtifactId)id, def, "In the pack");
                }
            }
            packCaption.text = hero.backpack.Count > 0 ? $"In the pack ({hero.backpack.Count})" : "Nothing in the pack";
        }

        /// <summary>The icon of a place an artifact is worn in, drawn faint while nothing is worn there.</summary>
        private static string PlaceIcon(ArtifactSlot slot)
        {
            switch (slot)
            {
                case ArtifactSlot.Weapon: return "weapon";
                case ArtifactSlot.Shield: return "defend";
                case ArtifactSlot.Helm: return "helm";
                case ArtifactSlot.Armor: return "skill_armorer";
                case ArtifactSlot.Cloak: return "cape";
                case ArtifactSlot.Boots: return "boot";
                case ArtifactSlot.Ring: return "ring";
                default: return "pendant";
            }
        }

        private void Item(Image frame, ArtifactId id, ArtifactDef def, string where)
        {
            Sprite sprite = Manager.Art.Artifact(id);
            Image icon = UIKit.Sprite(frame.transform, "Icon", sprite, Color.white);
            UIKit.Stretch((RectTransform)icon.transform, 6f, 6f, 6f, 6f);
            icon.preserveAspect = true;
            Tooltip.Attach(frame.gameObject, def.Name, $"<i>{where}</i>\n{def.Description}", sprite);
        }

        private void Spellbook()
        {
            UIKit.Clear(spells);
            var known = new List<SpellDef>();
            foreach (int id in hero.spells)
            {
                SpellDef def = Spells.Get((SpellId)id);
                if (def != null)
                {
                    known.Add(def);
                }
            }
            known.Sort((a, b) => a.Level != b.Level ? a.Level.CompareTo(b.Level) : string.CompareOrdinal(a.Name, b.Name));
            foreach (SpellDef def in known)
            {
                Sprite sprite = Manager.Art.Spell(def.Id);
                Image frame = UIKit.Slot(spells, def.Name);
                Image icon = UIKit.Sprite(frame.transform, "Icon", sprite, Color.white);
                UIKit.Stretch((RectTransform)icon.transform, 7f, 7f, 7f, 7f);
                icon.preserveAspect = true;
                Image plate = UIKit.Recess(frame.transform, "Cost");
                UIKit.Pin((RectTransform)plate.transform, new Vector2(1f, 0f), new Vector2(-2f, 2f), new Vector2(34f, 24f));
                TextMeshProUGUI cost = UIKit.Label(plate.transform, "Text", def.Cost.ToString(), 16f, new Color(0.6f, 0.78f, 1f),
                    TextAlignmentOptions.Center);
                UIKit.Stretch((RectTransform)cost.transform, 2f, 0f, 2f, 0f);
                UIKit.FitLine(cost, 16f, 10f);
                Tooltip.Attach(frame.gameObject, def.Name, $"Level {def.Level}, {def.Cost} mana\n{def.Description}", sprite);
            }
            spellCaption.text = known.Count > 0 ? $"Spellbook ({known.Count})" : "Spellbook";
            if (known.Count == 0)
            {
                TextMeshProUGUI empty = UIKit.Label(spells, "Empty", "No spells yet. A Mage Guild teaches them.", 18f, UIKit.Dim,
                    TextAlignmentOptions.Center);
                empty.fontStyle = FontStyles.Italic;
                // A grid gives the note a cell of its own; it is wide enough to read across the page.
                ((RectTransform)empty.transform).sizeDelta = new Vector2(SpellSize * 6f, SpellSize);
                spells.GetComponent<GridLayoutGroup>().enabled = false;
                UIKit.Pin((RectTransform)empty.transform, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(460f, 40f));
            }
            else
            {
                spells.GetComponent<GridLayoutGroup>().enabled = true;
            }
        }

        private void DismissPicked()
        {
            (ArmyRow row, int slot) = ArmyRow.Picked;
            if (row != null && slot >= 0 && MayCommand)
            {
                Manager.Commands.Dismiss(row.Holder, slot);
                ArmyRow.Drop();
                Refresh();
            }
        }
    }
}
