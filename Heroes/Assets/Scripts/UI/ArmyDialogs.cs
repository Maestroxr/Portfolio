using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// Splitting a stack: Ctrl and a click on an empty place (or one of the same creatures) while a stack is picked up
    /// asks how many of it go there, with a slider; the rest stay where they were. A hero keeps at least one creature.
    /// </summary>
    public sealed class SplitBox : Dialog
    {
        private const float SlotSize = 112f;

        private int fromHolder;
        private int fromSlot;
        private int toHolder;
        private int toSlot;
        private int creature;
        private int total;
        private int already;
        private Image leftPicture;
        private Image rightPicture;
        private TextMeshProUGUI leftCount;
        private TextMeshProUGUI rightCount;
        private Slider slider;
        private Button split;

        public static SplitBox Make(Transform parent, HeroesGameManager manager)
        {
            SplitBox box = Build<SplitBox>(parent, manager, "Split", new Vector2(780f, 480f), "Split the Stack");
            RectTransform body = box.Body;
            TextMeshProUGUI note = UIKit.Label(body, "Note", "How many go to the new place?", 23f, UIKit.Ink, TextAlignmentOptions.Center);
            UIKit.Pin((RectTransform)note.transform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(660f, 34f));
            box.leftPicture = box.Stack(body, "Stays", -190f, out box.leftCount);
            box.rightPicture = box.Stack(body, "Goes", 190f, out box.rightCount);
            TextMeshProUGUI arrow = UIKit.Label(body, "Arrow", "→", 44f, UIKit.Gold, TextAlignmentOptions.Center);
            UIKit.Pin((RectTransform)arrow.transform, new Vector2(0.5f, 1f), new Vector2(0f, -44f - SlotSize * 0.5f + 22f), new Vector2(120f, 56f));
            box.slider = UIKit.NumberSlider(body, "Count");
            UIKit.Pin((RectTransform)box.slider.transform, new Vector2(0.5f, 1f), new Vector2(0f, -44f - SlotSize - 64f), new Vector2(560f, 44f));
            box.slider.onValueChanged.AddListener(_ => box.Numbers());
            box.split = box.Answer("Split", box.Confirm);
            box.Answer("Cancel", box.Close);
            return box;
        }

        /// <summary>A stack in its square, with the number below it.</summary>
        private Image Stack(RectTransform body, string name, float x, out TextMeshProUGUI count)
        {
            Image slot = UIKit.Slot(body, name);
            UIKit.Pin((RectTransform)slot.transform, new Vector2(0.5f, 1f), new Vector2(x, -44f), new Vector2(SlotSize, SlotSize));
            Image picture = UIKit.Sprite(slot.transform, "Portrait", null, Color.white);
            picture.preserveAspect = true;
            UIKit.Stretch((RectTransform)picture.transform, 5f, 5f, 5f, 5f);
            count = UIKit.Label(body, name + "Count", "", 30f, UIKit.Gold, TextAlignmentOptions.Center, true);
            UIKit.Pin((RectTransform)count.transform, new Vector2(0.5f, 1f), new Vector2(x, -44f - SlotSize - 4f), new Vector2(200f, 40f));
            return picture;
        }

        /// <summary>
        /// Asks how many of the stack at <paramref name="fromSlot"/> of <paramref name="from"/> go to <paramref name="toSlot"/>
        /// of <paramref name="to"/>. False when there is nothing to split there (the caller then moves the whole stack).
        /// </summary>
        public bool Show(int from, int fromSlot, int to, int toSlot)
        {
            if (Manager.Game == null || from == to && fromSlot == toSlot)
            {
                return false;
            }
            Army source = ArmyRow.ArmyOf(Manager, from);
            Army target = ArmyRow.ArmyOf(Manager, to);
            if (source == null || target == null)
            {
                return false;
            }
            ArmySlot src = source.slots[fromSlot];
            ArmySlot dst = target.slots[toSlot];
            if (src.IsEmpty || !dst.IsEmpty && dst.creature != src.creature)
            {
                return false;
            }
            // Within one army at least one stays behind (else it is a move); a hero never gives his last creature away.
            bool keepOne = from == to || !Holder.IsGarrison(from) && source.StackCount == 1;
            int most = src.count - (keepOne ? 1 : 0);
            if (most < 1)
            {
                return false;
            }
            fromHolder = from;
            this.fromSlot = fromSlot;
            toHolder = to;
            this.toSlot = toSlot;
            creature = src.creature;
            total = src.count;
            already = dst.IsEmpty ? 0 : dst.count;
            slider.minValue = 1f;
            slider.maxValue = most;
            slider.SetValueWithoutNotify(Mathf.Clamp(Mathf.CeilToInt(total * 0.5f), 1, most));
            slider.interactable = most > 1;
            CreatureDef def = src.Def;
            Sprite face = def != null ? Manager.Art.Portrait(def.Id) : null;
            leftPicture.sprite = rightPicture.sprite = face != null ? face : Manager.Art.Icon("monster");
            Heading.text = def != null ? Words.F("Split the {0}", Words.Name(def.Plural)) : Words.T("Split the Stack");
            Open();
            Numbers();
            return true;
        }

        private int Count => Mathf.RoundToInt(slider.value);

        private void Numbers()
        {
            leftCount.text = (total - Count).ToString("N0");
            rightCount.text = (already + Count).ToString("N0");
        }

        /// <summary>The armies changed under the box (a command of the rules, a turn ending): it closes if its stack did.</summary>
        public void Refresh()
        {
            if (!IsOpen || Manager.Game == null)
            {
                return;
            }
            Army source = ArmyRow.ArmyOf(Manager, fromHolder);
            Army target = ArmyRow.ArmyOf(Manager, toHolder);
            ArmySlot src = source != null ? source.slots[fromSlot] : null;
            ArmySlot dst = target != null ? target.slots[toSlot] : null;
            if (src == null || dst == null || src.creature != creature || src.count != total || !dst.IsEmpty && dst.creature != creature)
            {
                Close();
                return;
            }
            UIKit.Enable(split, MayCommand);
        }

        private void Confirm()
        {
            if (MayCommand)
            {
                Manager.Commands.MoveArmy(fromHolder, fromSlot, toHolder, toSlot, Count);
            }
            Close();
        }
    }

    /// <summary>
    /// A dwelling out on the map, visited by a hero of this device: who lives there, how many wait, what they cost, and
    /// a slider for how many join the hero. It opens even when nobody can be paid for, to show what the dwelling holds.
    /// </summary>
    public sealed class DwellingBox : Dialog
    {
        private int objectId = -1;
        private int heroId = -1;
        private Image portrait;
        private TextMeshProUGUI creatureName;
        private TextMeshProUGUI stats;
        private TextMeshProUGUI waiting;
        private TextMeshProUGUI count;
        private TextMeshProUGUI cost;
        private TextMeshProUGUI reason;
        private Slider slider;
        private Button recruit;
        private Button max;
        private int most;

        public static DwellingBox Make(Transform parent, HeroesGameManager manager)
        {
            DwellingBox box = Build<DwellingBox>(parent, manager, "Dwelling", new Vector2(920f, 600f), "Dwelling");
            RectTransform body = box.Body;
            box.portrait = UIKit.PortraitFrame(body, "Portrait", null);
            UIKit.Pin((RectTransform)box.portrait.transform.parent.parent, new Vector2(0f, 1f), new Vector2(10f, 0f), new Vector2(170f, 170f));
            box.creatureName = UIKit.Heading(body, "Name", "", 32f, TextAlignmentOptions.TopLeft);
            UIKit.Pin((RectTransform)box.creatureName.transform, new Vector2(0f, 1f), new Vector2(204f, 0f), new Vector2(600f, 44f));
            UIKit.FitLine(box.creatureName, 32f, 18f);
            box.stats = UIKit.Label(body, "Stats", "", 20f, UIKit.Ink, TextAlignmentOptions.TopLeft);
            UIKit.Pin((RectTransform)box.stats.transform, new Vector2(0f, 1f), new Vector2(204f, -50f), new Vector2(600f, 84f));
            box.stats.textWrappingMode = TextWrappingModes.Normal;
            box.stats.enableAutoSizing = true;
            box.stats.fontSizeMax = 20f;
            box.stats.fontSizeMin = 14f;
            box.waiting = UIKit.Label(body, "Waiting", "", 21f, UIKit.Gold, TextAlignmentOptions.TopLeft);
            UIKit.Pin((RectTransform)box.waiting.transform, new Vector2(0f, 1f), new Vector2(204f, -138f), new Vector2(600f, 32f));
            UIKit.FitLine(box.waiting, 21f, 14f);

            Image plate = UIKit.Recess(body, "Hire");
            UIKit.Pin((RectTransform)plate.transform, new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(800f, 190f));
            TextMeshProUGUI caption = UIKit.Title(plate.transform, "Caption", "Recruit", 22f, UIKit.Gold);
            UIKit.Pin((RectTransform)caption.transform, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(400f, 30f));
            UIKit.Look(caption, TextLook.Gold);
            box.slider = UIKit.NumberSlider(plate.transform, "Count");
            UIKit.Pin((RectTransform)box.slider.transform, new Vector2(0.5f, 1f), new Vector2(-60f, -48f), new Vector2(560f, 44f));
            box.slider.onValueChanged.AddListener(_ => box.Numbers());
            box.count = UIKit.Label(plate.transform, "Number", "0", 34f, UIKit.Gold, TextAlignmentOptions.Center, true);
            UIKit.Pin((RectTransform)box.count.transform, new Vector2(0.5f, 1f), new Vector2(300f, -46f), new Vector2(120f, 48f));
            box.cost = UIKit.Label(plate.transform, "Cost", "", 22f, UIKit.Ink, TextAlignmentOptions.Center);
            UIKit.Pin((RectTransform)box.cost.transform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(760f, 34f));
            UIKit.FitLine(box.cost, 22f, 14f);
            box.reason = UIKit.Label(plate.transform, "Reason", "", 19f, UIKit.Dim, TextAlignmentOptions.Center);
            box.reason.fontStyle = FontStyles.Italic;
            UIKit.Pin((RectTransform)box.reason.transform, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(760f, 30f));
            UIKit.FitLine(box.reason, 19f, 13f);

            box.recruit = box.Answer("Recruit", box.Recruit);
            box.max = box.Answer("Max", box.Max);
            box.Answer("Close", box.Close);
            return box;
        }

        /// <summary>Opens the dwelling <paramref name="dwelling"/> for <paramref name="hero"/>, who stands beside it.</summary>
        public void Show(MapObject dwelling, HeroState hero)
        {
            if (dwelling == null || hero == null)
            {
                return;
            }
            objectId = dwelling.id;
            heroId = hero.id;
            most = -1;
            Open();
            Refresh();
        }

        public void Refresh()
        {
            if (!IsOpen || Manager.Game == null)
            {
                return;
            }
            GameState state = Manager.Game.State;
            MapObject dwelling = state.Object(objectId);
            HeroState hero = state.Hero(heroId);
            CreatureDef def = dwelling != null ? Creatures.Get(dwelling.subtype) : null;
            if (dwelling == null || dwelling.removed || hero == null || !hero.alive || def == null)
            {
                Close();
                return;
            }
            PlayerState owner = state.Player(hero.owner);
            int affordable = owner != null ? owner.resources.Times(def.Cost) : 0;
            bool room = hero.army.CanAdd(dwelling.subtype);
            bool near = Manager.Game.Grid.Distance(hero.cell, dwelling.cell) <= 1;
            int could = room && near ? Mathf.Min(dwelling.amount, affordable) : 0;

            Heading.text = Words.F("Dwelling of the {0}", Words.Name(def.Plural));
            Sprite face = Manager.Art.Portrait(def.Id);
            portrait.sprite = face != null ? face : Manager.Art.Icon("monster");
            creatureName.text = Words.Name(def.Plural);
            stats.text = ArmyRow.Describe(def);
            waiting.text = Words.F("{0} {1} can be recruited.", dwelling.amount, Words.Name(dwelling.amount == 1 ? def.Name : def.Plural)) + "  " +
                           $"<color=#B8AB8F>{UIKit.Cost(def.Cost)} {Words.T("each")}</color>";
            reason.text = dwelling.amount <= 0 ? Words.T("None waiting until next week.")
                : !near ? Words.T("The hero has to stand beside the dwelling.")
                : !room ? Words.T("No room in the hero's army for them.")
                : affordable <= 0 ? Words.T("You cannot afford it yet.")
                : Words.F("They join {0}'s army.", Words.Name(hero.Name));

            if (could != most)
            {
                // The most that can be hired changed (or the box just opened): the slider starts at all of them.
                most = could;
                slider.minValue = 0f;
                slider.maxValue = Mathf.Max(1, most);
                slider.SetValueWithoutNotify(most);
                slider.interactable = most > 0;
            }
            Numbers();
        }

        private int Count => Mathf.Clamp(Mathf.RoundToInt(slider.value), 0, Mathf.Max(0, most));

        private bool Mine
        {
            get
            {
                HeroState hero = Manager.Game != null ? Manager.Game.State.Hero(heroId) : null;
                return hero != null && hero.owner == Manager.Viewer && !Manager.HeroesUI.Busy;
            }
        }

        private void Numbers()
        {
            MapObject dwelling = Manager.Game != null ? Manager.Game.State.Object(objectId) : null;
            CreatureDef def = dwelling != null ? Creatures.Get(dwelling.subtype) : null;
            if (def == null)
            {
                return;
            }
            PlayerState owner = Manager.ViewerState;
            count.text = Count.ToString("N0");
            cost.text = Count > 0 ? Words.F("Costs {0}", UIKit.Cost(def.Cost, owner != null ? owner.resources : null, Count)) : "";
            UIKit.Enable(recruit, Count > 0 && Mine && MayCommand);
            UIKit.Enable(max, most > 0 && Count < most);
        }

        private void Max()
        {
            slider.value = most;
        }

        private void Recruit()
        {
            if (Count > 0 && Mine && MayCommand)
            {
                Manager.Commands.RecruitAt(objectId, heroId, Count);
            }
        }
    }

    /// <summary>
    /// Two heroes of a player who met on the map: each one's portrait, army and artifacts side by side. Creatures move
    /// between the armies as within one (a click on a stack, then on a place; Ctrl splits), and a click on an artifact
    /// hands it to the other hero.
    /// </summary>
    public sealed class MeetingScreen : Dialog
    {
        private const float ArmySize = 84f;
        private const float ItemSize = 58f;

        private readonly Side[] sides = new Side[2];
        private string shown;

        private sealed class Side
        {
            public int Hero = -1;
            public Image Portrait;
            public Image Pennant;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI Line;
            public ArmyRow Army;
            public RectTransform Worn;
            public RectTransform Pack;
            public TextMeshProUGUI PackCaption;
        }

        public static MeetingScreen Make(Transform parent, HeroesGameManager manager)
        {
            MeetingScreen screen = Build<MeetingScreen>(parent, manager, "Meeting", new Vector2(1620f, 700f), "Heroes Meet");
            screen.KeepUnderTopBar();
            for (int i = 0; i < 2; i++)
            {
                screen.sides[i] = screen.MakeSide(screen.Body, i);
            }
            TextMeshProUGUI hint = UIKit.Label(screen.Body, "Hint",
                "Click a stack, then a place on either side. Ctrl and a click on an empty place splits a stack. Click an artifact to hand it over.",
                18f, UIKit.Dim, TextAlignmentOptions.Center);
            hint.fontStyle = FontStyles.Italic;
            UIKit.Pin((RectTransform)hint.transform, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(1500f, 28f));
            UIKit.FitLine(hint, 18f, 12f);
            Button close = screen.Answer("Close", screen.Close);
            UIKit.Fit((RectTransform)close.transform, 240f, ButtonRow - 4f);
            return screen;
        }

        private Side MakeSide(RectTransform body, int index)
        {
            var side = new Side();
            Image panel = UIKit.Panel(body, index == 0 ? "Left" : "Right");
            RectTransform rect = (RectTransform)panel.transform;
            rect.anchorMin = new Vector2(index == 0 ? 0f : 0.5f, 0f);
            rect.anchorMax = new Vector2(index == 0 ? 0.5f : 1f, 1f);
            rect.offsetMin = new Vector2(index == 0 ? 0f : 8f, 34f);
            rect.offsetMax = new Vector2(index == 0 ? -8f : 0f, 0f);
            RectTransform area = UIKit.Content(panel, 8f);

            side.Portrait = UIKit.PortraitFrame(area, "Portrait", null);
            UIKit.Pin((RectTransform)side.Portrait.transform.parent.parent, new Vector2(0f, 1f), Vector2.zero, new Vector2(110f, 110f));
            side.Pennant = UIKit.Pennant(area, "Pennant", Color.white);
            UIKit.Pin((RectTransform)side.Pennant.transform, new Vector2(0f, 1f), new Vector2(88f, 6f), new Vector2(20f, 38f));
            side.Name = UIKit.Heading(area, "Name", "", 30f, TextAlignmentOptions.TopLeft);
            UIKit.Pin((RectTransform)side.Name.transform, new Vector2(0f, 1f), new Vector2(128f, -6f), new Vector2(560f, 40f));
            UIKit.FitLine(side.Name, 30f, 16f);
            side.Line = UIKit.Label(area, "Line", "", 20f, UIKit.Dim, TextAlignmentOptions.TopLeft);
            side.Line.fontStyle = FontStyles.Italic;
            UIKit.Pin((RectTransform)side.Line.transform, new Vector2(0f, 1f), new Vector2(128f, -50f), new Vector2(560f, 30f));
            UIKit.FitLine(side.Line, 20f, 13f);

            Caption(area, "ArmyCaption", "Army", -122f);
            side.Army = ArmyRow.Make(area, Manager, ArmySize);
            UIKit.Pin((RectTransform)side.Army.transform, new Vector2(0.5f, 1f), new Vector2(0f, -158f),
                new Vector2(7f * ArmySize + 6f * Mathf.Round(ArmySize * 0.1f), ArmySize));

            Caption(area, "ArtifactCaption", "Artifacts", -256f);
            side.Worn = UIKit.Rect(area, "Worn");
            UIKit.Pin(side.Worn, new Vector2(0.5f, 1f), new Vector2(0f, -292f), new Vector2(8f * ItemSize + 7f * 6f, ItemSize));
            UIKit.Grid(side.Worn, new Vector2(ItemSize, ItemSize), new Vector2(6f, 6f), 8).childAlignment = TextAnchor.UpperCenter;
            side.PackCaption = UIKit.Label(area, "PackCaption", "", 17f, UIKit.Dim, TextAlignmentOptions.MidlineLeft);
            UIKit.Pin((RectTransform)side.PackCaption.transform, new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(8f * ItemSize + 7f * 6f, 26f));
            side.Pack = UIKit.Scroll(area, "Pack", out ScrollRect scroll);
            UIKit.Pin((RectTransform)scroll.transform, new Vector2(0.5f, 1f), new Vector2(0f, -390f), new Vector2(8f * ItemSize + 7f * 6f + 14f, 104f));
            UIKit.Grid(side.Pack, new Vector2(46f, 46f), new Vector2(6f, 6f), 9).childAlignment = TextAnchor.UpperLeft;
            return side;
        }

        private static void Caption(RectTransform area, string name, string text, float y)
        {
            RectTransform holder = UIKit.Rect(area, name);
            UIKit.Pin(holder, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(600f, 34f));
            UIKit.SectionTitle(holder, "Caption", text, 22f);
        }

        /// <summary>Opens the meeting of <paramref name="visitor"/> (who walked up) and <paramref name="host"/>.</summary>
        public void Show(HeroState visitor, HeroState host)
        {
            if (visitor == null || host == null || visitor.id == host.id)
            {
                return;
            }
            sides[0].Hero = visitor.id;
            sides[1].Hero = host.id;
            shown = null;
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
            if (!IsOpen || Manager.Game == null)
            {
                return;
            }
            GameState state = Manager.Game.State;
            HeroState left = state.Hero(sides[0].Hero);
            HeroState right = state.Hero(sides[1].Hero);
            // Both have to be alive and still side by side, else there is no meeting.
            if (left == null || right == null || !left.alive || !right.alive || Manager.Game.Grid.Distance(left.cell, right.cell) > 1)
            {
                Close();
                return;
            }
            Heading.text = Words.F("{0} meets {1}", Words.Name(left.Name), Words.Name(right.Name));
            Fill(sides[0], left);
            Fill(sides[1], right);
            string signature = $"{string.Join(",", left.equipped)}|{string.Join(",", left.backpack)}|{string.Join(",", right.equipped)}|" +
                               $"{string.Join(",", right.backpack)}|{Manager.HeroesUI.Busy}|{Gamebox.GameLanguages.Code}";
            if (signature != shown)
            {
                shown = signature;
                ShowArtifacts(sides[0], left, right);
                ShowArtifacts(sides[1], right, left);
            }
        }

        private void Fill(Side side, HeroState hero)
        {
            HeroClass heroClass = hero.Def != null ? hero.Def.Class : HeroClass.Knight;
            PlayerState owner = Manager.Game.State.Player(hero.owner);
            side.Portrait.sprite = Manager.Art.HeroPortrait(heroClass);
            side.Pennant.color = HeroesArt.PlayerColor(owner != null ? (int)owner.color : 4);
            side.Name.text = Words.Name(hero.Name);
            side.Line.text = Words.F("Level {0} {1}", hero.level, Words.T(HeroData.Class(heroClass).Name));
            side.Army.Bind(Holder.Hero(hero.id));
        }

        /// <summary>What <paramref name="hero"/> wears and carries; a click on one hands it to <paramref name="other"/>.</summary>
        private void ShowArtifacts(Side side, HeroState hero, HeroState other)
        {
            UIKit.Clear(side.Worn);
            for (int slot = 0; slot < hero.equipped.Length; slot++)
            {
                int id = hero.equipped[slot];
                ArtifactDef def = id >= 0 ? Artifacts.Get((ArtifactId)id) : null;
                if (def != null)
                {
                    Item(side.Worn, def, hero, other, true, Artifacts.SlotName((ArtifactSlot)slot));
                }
                else
                {
                    Image empty = UIKit.Slot(side.Worn, $"Slot{slot}");
                    empty.color = new Color(1f, 1f, 1f, 0.55f);
                    Tooltip.Attach(empty.gameObject, Artifacts.SlotName((ArtifactSlot)slot), "Nothing is worn here.");
                }
            }
            UIKit.Clear(side.Pack);
            foreach (int id in hero.backpack)
            {
                ArtifactDef def = Artifacts.Get((ArtifactId)id);
                if (def != null)
                {
                    Item(side.Pack, def, hero, other, false, "In the pack");
                }
            }
            side.PackCaption.text = hero.backpack.Count > 0 ? Words.F("In the pack ({0})", hero.backpack.Count) : Words.T("Nothing in the pack");
        }

        private void Item(RectTransform parent, ArtifactDef def, HeroState hero, HeroState other, bool worn, string where)
        {
            Image frame = UIKit.Slot(parent, def.Name);
            var button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.colors = UIKit.Tint();
            int artifact = (int)def.Id;
            int from = hero.id;
            int to = other.id;
            button.onClick.AddListener(() => Give(from, artifact, to, worn));
            frame.gameObject.AddComponent<Clicker>();
            Gamebox.UI.PressFeedback.Attach(frame.gameObject, 1.05f, 0.95f);
            Sprite sprite = Manager.Art.Artifact(def.Id);
            Image icon = UIKit.Sprite(frame.transform, "Icon", sprite, Color.white);
            UIKit.Stretch((RectTransform)icon.transform, 6f, 6f, 6f, 6f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Tooltip.Attach(frame.gameObject, def.Name,
                $"<i>{Words.T(where)}</i>\n{Words.T(def.Description)}\n{Words.F("Click to give it to {0}.", Words.Name(other.Name))}", sprite);
            UIKit.Enable(button, hero.owner == Manager.Viewer && !Manager.HeroesUI.Busy);
        }

        private void Give(int from, int artifact, int to, bool worn)
        {
            if (MayCommand)
            {
                Manager.Commands.GiveArtifact(from, artifact, to, worn);
            }
        }
    }
}
