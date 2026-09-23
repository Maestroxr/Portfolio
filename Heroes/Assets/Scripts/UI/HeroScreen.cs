using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// The book of a hero: what he is worth in a fight, what he has learned, what he carries and what he can cast,
    /// with his army along the bottom where creatures can be shifted between the slots.
    /// </summary>
    public sealed class HeroScreen : Dialog
    {
        private HeroState hero;
        private TextMeshProUGUI stats;
        private TextMeshProUGUI biography;
        private RectTransform skills;
        private RectTransform artifacts;
        private RectTransform spells;
        private ArmyRow army;
        private Image experience;
        private TextMeshProUGUI experienceLabel;

        public static HeroScreen Make(Transform parent, HeroesGameManager manager)
        {
            HeroScreen screen = Build<HeroScreen>(parent, manager, "Hero", new Vector2(1380f, 860f), "Hero");

            Image left = UIKit.Panel(screen.Body, "Stats", 0.85f);
            RectTransform leftRect = UIKit.Pin((RectTransform)left.transform, new Vector2(0f, 1f), new Vector2(0f, 0f),
                new Vector2(430f, 500f));
            screen.stats = UIKit.Label(leftRect, "Numbers", "", 24f, UIKit.Ink);
            UIKit.Pin((RectTransform)screen.stats.transform, new Vector2(0f, 1f), new Vector2(26f, -34f), new Vector2(382f, 300f));
            screen.experience = UIKit.Bar(leftRect, "Experience", new Color(0.85f, 0.72f, 0.35f));
            UIKit.Pin((RectTransform)screen.experience.transform.parent, new Vector2(0f, 1f), new Vector2(26f, -344f),
                new Vector2(382f, 16f));
            screen.experienceLabel = UIKit.Label(leftRect, "ExperienceLabel", "", 18f, UIKit.Dim);
            UIKit.Pin((RectTransform)screen.experienceLabel.transform, new Vector2(0f, 1f), new Vector2(26f, -366f),
                new Vector2(382f, 22f));
            screen.biography = UIKit.Label(leftRect, "Biography", "", 20f, UIKit.Dim, TextAlignmentOptions.TopLeft);
            UIKit.Pin((RectTransform)screen.biography.transform, new Vector2(0f, 1f), new Vector2(26f, -396f),
                new Vector2(382f, 92f));

            Image middle = UIKit.Panel(screen.Body, "Skills", 0.85f);
            RectTransform middleRect = UIKit.Pin((RectTransform)middle.transform, new Vector2(0.5f, 1f), new Vector2(0f, 0f),
                new Vector2(440f, 500f));
            Caption(middleRect, "Skills");
            screen.skills = UIKit.Rect(middleRect, "List");
            UIKit.Stretch(screen.skills, 18f, 18f, 18f, 52f);
            UIKit.Layout<VerticalLayoutGroup>(screen.skills, 5f).childForceExpandWidth = true;

            Image right = UIKit.Panel(screen.Body, "Gear", 0.85f);
            RectTransform rightRect = UIKit.Pin((RectTransform)right.transform, new Vector2(1f, 1f), new Vector2(0f, 0f),
                new Vector2(440f, 500f));
            Caption(rightRect, "Artifacts and Spells");
            screen.artifacts = UIKit.Rect(rightRect, "Artifacts");
            UIKit.Pin(screen.artifacts, new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(400f, 180f));
            UIKit.Grid(screen.artifacts, new Vector2(88f, 88f), new Vector2(8f, 8f), 4);
            screen.spells = UIKit.Rect(rightRect, "Spells");
            UIKit.Pin(screen.spells, new Vector2(0.5f, 1f), new Vector2(0f, -248f), new Vector2(400f, 250f));
            UIKit.Grid(screen.spells, new Vector2(74f, 74f), new Vector2(6f, 6f), 5);

            Image bottom = UIKit.Panel(screen.Body, "Army", 0.85f);
            RectTransform bottomRect = UIKit.Pin((RectTransform)bottom.transform, new Vector2(0.5f, 0f), new Vector2(0f, 0f),
                new Vector2(1290f, 140f));
            screen.army = ArmyRow.Make(bottomRect, manager, 96f);
            UIKit.Stretch((RectTransform)screen.army.transform, 20f, 20f, 240f, 20f);
            Button dismiss = UIKit.Push(bottomRect, "Dismiss", "Dismiss Stack", screen.DismissPicked, 22f);
            UIKit.Pin((RectTransform)dismiss.transform, new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(200f, 56f));

            screen.Answer("Close", screen.Close);
            return screen;
        }

        private static void Caption(RectTransform parent, string text)
        {
            TextMeshProUGUI label = UIKit.Title(parent, "Caption", text, 24f, UIKit.Gold);
            UIKit.Pin((RectTransform)label.transform, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(380f, 30f));
        }

        public void Open(HeroState which)
        {
            hero = which;
            ArmyRow.Drop();
            Open();
            Refresh();
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
            HeroClassDef heroClass = HeroData.Class(hero.Def != null ? hero.Def.Class : HeroClass.Knight);
            Heading.text = $"{hero.Name}, {heroClass.Name}";

            var text = new StringBuilder();
            text.Append($"Level {hero.level}\n\n");
            text.Append($"Attack      {game.Stat(hero, PrimaryStat.Attack)}\n");
            text.Append($"Defense     {game.Stat(hero, PrimaryStat.Defense)}\n");
            text.Append($"Spell Power {game.Stat(hero, PrimaryStat.Power)}\n");
            text.Append($"Knowledge   {game.Stat(hero, PrimaryStat.Knowledge)}\n\n");
            text.Append($"Mana        {hero.mana} / {game.MaxMana(hero)}\n");
            text.Append($"Movement    {hero.movement} / {hero.maxMovement}\n");
            text.Append($"Morale      {Signed(game.Morale(hero))}    Luck {Signed(game.Luck(hero))}");
            stats.text = text.ToString();

            int now = HeroData.ExperienceFor(hero.level);
            int next = HeroData.ExperienceFor(hero.level + 1);
            experience.fillAmount = next > now ? Mathf.Clamp01((hero.experience - now) / (float)(next - now)) : 1f;
            experienceLabel.text = $"Experience {hero.experience} / {next}";
            biography.text = hero.Def != null ? hero.Def.Biography : "";

            Skills();
            Gear();
            Spellbook();
            army.Bind(Holder.Hero(hero.id));
        }

        private static string Signed(int value)
        {
            return value > 0 ? $"+{value}" : value.ToString();
        }

        private void Skills()
        {
            Clear(skills);
            foreach (SkillEntry entry in hero.skills)
            {
                SkillDef def = HeroData.Skill((SkillId)entry.skill);
                Image row = UIKit.Panel(skills, def.Name, 0.6f);
                UIKit.Fit((RectTransform)row.transform, 0f, 56f, true);
                Image icon = UIKit.Sprite(row.transform, "Icon", Manager.Art.Skill((SkillId)entry.skill), Color.white);
                UIKit.Pin((RectTransform)icon.transform, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(40f, 40f));
                ((RectTransform)icon.transform).pivot = new Vector2(0f, 0.5f);
                TextMeshProUGUI label = UIKit.Label(row.transform, "Name",
                    $"{HeroData.SkillLevelName(entry.level)} {def.Name}", 20f, UIKit.Ink, TextAlignmentOptions.MidlineLeft);
                UIKit.Stretch((RectTransform)label.transform, 58f, 0f, 10f, 0f);
                Tooltip.Attach(row.gameObject, $"{def.Name}\n{def.Description}");
            }
            if (skills.childCount == 0)
            {
                TextMeshProUGUI empty = UIKit.Label(skills, "Empty", "No skills learned yet.", 20f, UIKit.Dim,
                    TextAlignmentOptions.Center);
                UIKit.Fit((RectTransform)empty.transform, 0f, 40f, true);
            }
        }

        private void Gear()
        {
            Clear(artifacts);
            for (int slot = 0; slot < hero.equipped.Length; slot++)
            {
                Image frame = UIKit.Slot(artifacts, $"Slot{slot}");
                int id = hero.equipped[slot];
                ArtifactDef def = id >= 0 ? Artifacts.Get((ArtifactId)id) : null;
                if (def != null)
                {
                    Image icon = UIKit.Sprite(frame.transform, "Icon", Manager.Art.Artifact((ArtifactId)id), Color.white);
                    UIKit.Stretch((RectTransform)icon.transform, 8f, 8f, 8f, 8f);
                    icon.preserveAspect = true;
                    Tooltip.Attach(frame.gameObject, $"{def.Name}\n{def.Description}");
                }
                else
                {
                    Tooltip.Attach(frame.gameObject, $"{Artifacts.SlotName((ArtifactSlot)slot)}: empty");
                }
            }
            foreach (int id in hero.backpack)
            {
                ArtifactDef def = Artifacts.Get((ArtifactId)id);
                Image frame = UIKit.Slot(artifacts, "Spare");
                Image icon = UIKit.Sprite(frame.transform, "Icon", Manager.Art.Artifact((ArtifactId)id), new Color(1f, 1f, 1f, 0.6f));
                UIKit.Stretch((RectTransform)icon.transform, 10f, 10f, 10f, 10f);
                icon.preserveAspect = true;
                Tooltip.Attach(frame.gameObject, def != null ? $"{def.Name} (in the pack)\n{def.Description}" : "In the pack");
            }
        }

        private void Spellbook()
        {
            Clear(spells);
            foreach (int id in hero.spells)
            {
                SpellDef def = Spells.Get((SpellId)id);
                if (def == null)
                {
                    continue;
                }
                Image frame = UIKit.Slot(spells, def.Name);
                Image icon = UIKit.Sprite(frame.transform, "Icon", Manager.Art.Spell((SpellId)id), Color.white);
                UIKit.Stretch((RectTransform)icon.transform, 8f, 8f, 8f, 14f);
                icon.preserveAspect = true;
                TextMeshProUGUI cost = UIKit.Label(frame.transform, "Cost", def.Cost.ToString(), 15f, UIKit.Gold,
                    TextAlignmentOptions.BottomRight);
                UIKit.Stretch((RectTransform)cost.transform, 4f, 4f, 6f, 4f);
                Tooltip.Attach(frame.gameObject, $"{def.Name} (level {def.Level}, {def.Cost} mana)\n{def.Description}");
            }
            if (spells.childCount == 0)
            {
                TextMeshProUGUI empty = UIKit.Label(spells, "Empty", "No spellbook.", 20f, UIKit.Dim, TextAlignmentOptions.Center);
                UIKit.Fit((RectTransform)empty.transform, 0f, 40f, true);
            }
        }

        private void DismissPicked()
        {
            (ArmyRow row, int slot) = ArmyRow.Picked;
            if (row != null && slot >= 0)
            {
                Manager.Commands.Dismiss(row.Holder, slot);
                ArmyRow.Drop();
                Refresh();
            }
        }

        private static void Clear(RectTransform rect)
        {
            for (int i = rect.childCount - 1; i >= 0; i--)
            {
                Destroy(rect.GetChild(i).gameObject);
            }
        }
    }
}
