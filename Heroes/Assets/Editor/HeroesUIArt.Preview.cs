using System.IO;
using Portfolio.Heroes.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// A sheet of the whole interface kit put together as the game would use it (a window under its ribbon, buttons in
    /// every state, slots with portraits, bars, a tooltip, a list of cards, parchment, pennants and the pointers), made
    /// with the game's own UIKit and rendered offscreen, to look the kit over without playing.
    /// </summary>
    internal static partial class HeroesUIArt
    {
        private const int PreviewLayer = 31;

        public static void Preview(HeroesArt art, string file)
        {
            HeroesArt previous = UIKit.Art;
            UIKit.Art = art;
            try
            {
                // The widgets are made in a scene of their own that is thrown away afterwards, so the open scene is not
                // marked as changed by objects coming and going in it.
                HeroesArtBuilder.Staged(() => DrawPreview(art, file));
            }
            finally
            {
                UIKit.Art = previous;
            }
        }

        private static void DrawPreview(HeroesArt art, string file)
        {
            var root = new GameObject("Interface Preview") { hideFlags = HideFlags.HideAndDontSave };
            root.transform.position = new Vector3(0f, -7000f, 0f);
            RenderTexture target = null;
            try
            {
                const int width = 1920;
                const int height = 1080;
                var cameraObject = new GameObject("Camera") { hideFlags = HideFlags.HideAndDontSave };
                cameraObject.transform.SetParent(root.transform, false);
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.orthographic = true;
                camera.cullingMask = 1 << PreviewLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 100f;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
                target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 1 };
                camera.targetTexture = target;

                var canvasObject = new GameObject("Canvas", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
                canvasObject.transform.SetParent(root.transform, false);
                var canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10f;
                var sheet = (RectTransform)canvas.transform;
                Compose(art, sheet);

                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.layer = PreviewLayer;
                    t.gameObject.hideFlags = HideFlags.HideAndDontSave;
                }
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(sheet);
                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    text.ForceMeshUpdate(true, true);
                }
                Canvas.ForceUpdateCanvases();
                camera.Render();
                camera.Render();

                RenderTexture active = RenderTexture.active;
                RenderTexture.active = target;
                var picture = new Texture2D(width, height, TextureFormat.RGB24, false);
                picture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                picture.Apply();
                RenderTexture.active = active;
                camera.targetTexture = null;
                Directory.CreateDirectory(Path.GetDirectoryName(file) ?? ".");
                File.WriteAllBytes(file, picture.EncodeToPNG());
                Object.DestroyImmediate(picture);
                Debug.Log($"Heroes: the interface kit is drawn in {file}.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (target != null)
                {
                    target.Release();
                    Object.DestroyImmediate(target);
                }
            }
        }

        private static void Compose(HeroesArt art, RectTransform sheet)
        {
            // The sky of a battle behind it all, as a stand in for the map the interface sits on.
            Texture2D sky = Read("Art/Sky/kloofendal_48d_partly_cloudy_puresky.jpg");
            var backdrop = UIKit.Rect(sheet, "Sky").gameObject.AddComponent<RawImage>();
            backdrop.texture = sky;
            backdrop.uvRect = new Rect(0.2f, 0.45f, 0.35f, 0.4f);
            UIKit.Stretch((RectTransform)backdrop.transform);
            Image ground = UIKit.Sprite(sheet, "Ground", null, new Color(0.24f, 0.33f, 0.16f));
            UIKit.Stretch((RectTransform)ground.transform, 0f, 0f, 0f, 700f);

            // The bar along the top: the resources and the date.
            Image top = UIKit.Strip(sheet, "TopBar");
            UIKit.Pin((RectTransform)top.transform, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(1500f, 64f));
            RectTransform resources = UIKit.Content(top, 4f, "Resources");
            resources.offsetMax = new Vector2(-330f, resources.offsetMax.y);
            UIKit.Layout<HorizontalLayoutGroup>(resources, 28f).childAlignment = TextAnchor.MiddleLeft;
            int[] amounts = { 12500, 20, 18, 5, 4, 7, 3 };
            for (int i = 0; i < amounts.Length; i++)
            {
                TextMeshProUGUI amount = UIKit.Resource(resources, (ResourceKind)i, 40f);
                amount.text = amounts[i].ToString();
                UIKit.Fit((RectTransform)amount.transform.parent, 130f, 40f);
            }
            Image date = UIKit.Recess(top.transform, "Date");
            UIKit.Pin((RectTransform)date.transform, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(300f, 44f));
            TextMeshProUGUI dateLabel = UIKit.Label(date.transform, "Text", "Month 1, Week 2, Day 3", 22f, UIKit.Gold, TextAlignmentOptions.Center);
            UIKit.Stretch((RectTransform)dateLabel.transform);

            // A window with its ribbon, panels, buttons, slots and bars.
            Image window = UIKit.Frame(sheet, "Window");
            UIKit.Pin((RectTransform)window.transform, new Vector2(0f, 0f), new Vector2(40f, 200f), new Vector2(1000f, 640f));
            Image ribbon = UIKit.Ribbon(window.transform, "Ribbon", "Aldermoor Castle", 30f);
            UIKit.Pin((RectTransform)ribbon.transform, new Vector2(0.5f, 1f), new Vector2(0f, 26f), new Vector2(520f, 76f));
            RectTransform body = UIKit.Content(window, 8f);

            Image panel = UIKit.Panel(body, "Buttons");
            UIKit.Pin((RectTransform)panel.transform, new Vector2(0f, 1f), new Vector2(0f, -44f), new Vector2(460f, 290f));
            RectTransform panelBody = UIKit.Content(panel, 8f);
            UIKit.Stretch((RectTransform)UIKit.Heading(panelBody, "Heading", "Buttons", 28f, TextAlignmentOptions.Top).transform);
            string[] states = { "Normal", "Hover", "Pressed", "Disabled" };
            Sprite[] sprites = { art.button, art.buttonHover, art.buttonPressed, art.buttonDisabled };
            for (int i = 0; i < states.Length; i++)
            {
                Button button = UIKit.Push(panelBody, states[i], states[i], null, 24f);
                button.GetComponent<Image>().sprite = sprites[i];
                UIKit.Pin((RectTransform)button.transform, new Vector2(0f, 1f), new Vector2(i % 2 * 212f, -50f - i / 2 * 66f), new Vector2(200f, 56f));
                if (i == 3)
                {
                    UIKit.Enable(button, false);
                }
            }
            Sprite[] rounds = { art.round, art.roundHover, art.roundPressed };
            string[] roundIcons = { "end_turn", "sleep", "spellbook" };
            for (int i = 0; i < rounds.Length; i++)
            {
                Button icon = UIKit.Icon(panelBody, "Round" + i, art.Icon(roundIcons[i]), null);
                icon.GetComponent<Image>().sprite = rounds[i];
                UIKit.Pin((RectTransform)icon.transform, new Vector2(0f, 0f), new Vector2(i * 76f, 0f), new Vector2(68f, 68f));
            }
            Image check = UIKit.Sprite(panelBody, "Check", art.checkBox, Color.white);
            UIKit.Pin((RectTransform)check.transform, new Vector2(1f, 0f), new Vector2(-150f, 12f), new Vector2(40f, 40f));
            Image mark = UIKit.Sprite(check.transform, "Mark", art.checkMark, Color.white);
            UIKit.Stretch((RectTransform)mark.transform, -4f, -4f, -4f, -4f);
            Image knob = UIKit.Sprite(panelBody, "Knob", art.knob, Color.white);
            UIKit.Pin((RectTransform)knob.transform, new Vector2(1f, 0f), new Vector2(-40f, 10f), new Vector2(44f, 44f));

            Image army = UIKit.Panel(body, "Army");
            UIKit.Pin((RectTransform)army.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(952f, 240f));
            RectTransform armyBody = UIKit.Content(army, 8f);
            TextMeshProUGUI armyTitle = UIKit.Label(armyBody, "Title", "<b>Father Anselm</b>, <i>level 3 Cleric</i>  <sprite name=\"attack\"> 2  <sprite name=\"defense\"> 3  <sprite name=\"power\"> 4  <sprite name=\"knowledge\"> 2   → ★★☆", 24f, UIKit.Ink);
            UIKit.Pin((RectTransform)armyTitle.transform, new Vector2(0f, 1f), Vector2.zero, new Vector2(920f, 34f));
            CreatureId[] creatures = { CreatureId.Militia, CreatureId.Archer, CreatureId.Swordsman, CreatureId.Priest, CreatureId.Crusader, CreatureId.Cavalier, CreatureId.GoldDragon };
            for (int i = 0; i < creatures.Length; i++)
            {
                Image slot = UIKit.Slot(armyBody, "Slot" + i);
                slot.sprite = i == 1 ? art.slotHover : i == 2 ? art.slotSelected : art.slot;
                UIKit.Pin((RectTransform)slot.transform, new Vector2(0f, 1f), new Vector2(i * 102f, -42f), new Vector2(92f, 92f));
                Image picture = UIKit.Sprite(slot.transform, "Portrait", art.Portrait(creatures[i]), Color.white);
                picture.preserveAspect = true;
                UIKit.Stretch((RectTransform)picture.transform, 5f, 5f, 5f, 5f);
                TextMeshProUGUI count = UIKit.Label(slot.transform, "Count", (i * 7 + 3).ToString(), 22f, UIKit.Ink, TextAlignmentOptions.BottomRight);
                UIKit.Stretch((RectTransform)count.transform, 4f, 2f, 7f, 2f);
                UIKit.Look(count, TextLook.Outline);
            }
            Image meter = UIKit.Meter(armyBody, "Experience", new Color(0.4f, 0.75f, 1f));
            meter.fillAmount = 0.62f;
            UIKit.Pin((RectTransform)meter.transform.parent, new Vector2(0f, 0f), new Vector2(0f, 12f), new Vector2(500f, 22f));
            Image bar = UIKit.Bar(armyBody, "Move", new Color(0.4f, 0.85f, 0.4f));
            bar.fillAmount = 0.4f;
            UIKit.Pin((RectTransform)bar.transform.parent, new Vector2(0f, 0f), new Vector2(520f, 16f), new Vector2(200f, 12f));
            RectTransform rule = UIKit.Divider(armyBody, "Rule", 22f);
            UIKit.Pin(rule, new Vector2(0f, 0f), new Vector2(0f, 42f), new Vector2(920f, 22f));

            Image heroes = UIKit.Panel(body, "Heroes");
            UIKit.Pin((RectTransform)heroes.transform, new Vector2(1f, 1f), new Vector2(0f, -44f), new Vector2(480f, 290f));
            RectTransform heroesBody = UIKit.Content(heroes, 8f);
            HeroClass[] classes = { HeroClass.Knight, HeroClass.Cleric, HeroClass.Necromancer };
            for (int i = 0; i < classes.Length; i++)
            {
                Image portrait = UIKit.PortraitFrame(heroesBody, "Hero" + i, art.HeroPortrait(classes[i]), i == 2);
                UIKit.Pin((RectTransform)portrait.transform.parent.parent, new Vector2(0f, 1f), new Vector2(i * 150f, 0f), new Vector2(136f, 136f));
                Image pennant = UIKit.Pennant(heroesBody, "Pennant" + i, HeroesArt.PlayerColor(i));
                UIKit.Pin((RectTransform)pennant.transform, new Vector2(0f, 0f), new Vector2(i * 95f + 30f, 0f), new Vector2(48f, 96f));
            }
            Image mounted = UIKit.Sprite(heroesBody, "Mounted", art.MountedPortrait(HeroClass.Barbarian), Color.white);
            mounted.preserveAspect = true;
            UIKit.Pin((RectTransform)mounted.transform, new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(120f, 120f));

            // A list of cards on the right.
            Image list = UIKit.Frame(sheet, "List");
            UIKit.Pin((RectTransform)list.transform, new Vector2(1f, 0f), new Vector2(-40f, 200f), new Vector2(520f, 640f));
            RectTransform rows = UIKit.Content(list, 6f);
            UIKit.Layout<VerticalLayoutGroup>(rows, 8f);
            TextMeshProUGUI listTitle = UIKit.Heading(rows, "Title", "Recruit", 30f);
            UIKit.Fit((RectTransform)listTitle.transform, 0f, 40f, true);
            CreatureId[] recruits = { CreatureId.Skeleton, CreatureId.Zombie, CreatureId.Lich, CreatureId.BoneDragon, CreatureId.RedDragon };
            for (int i = 0; i < recruits.Length; i++)
            {
                Image card = UIKit.Card(rows, "Card" + i);
                card.sprite = i == 1 ? art.cardHover : i == 2 ? art.cardSelected : art.card;
                UIKit.Fit((RectTransform)card.transform, 0f, 96f, true);
                Image picture = UIKit.Sprite(card.transform, "Portrait", art.Portrait(recruits[i]), Color.white);
                picture.preserveAspect = true;
                UIKit.Pin((RectTransform)picture.transform, new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(84f, 84f));
                CreatureDef def = Creatures.Get(recruits[i]);
                TextMeshProUGUI name = UIKit.Label(card.transform, "Name", def.Name, 24f, UIKit.Ink, TextAlignmentOptions.TopLeft, true);
                UIKit.Pin((RectTransform)name.transform, new Vector2(0f, 1f), new Vector2(104f, -10f), new Vector2(330f, 30f));
                TextMeshProUGUI cost = UIKit.Label(card.transform, "Cost", $"<sprite name=\"gold\"> {def.Cost.Gold}   {def.Growth} a week", 20f, UIKit.Dim, TextAlignmentOptions.TopLeft);
                UIKit.Pin((RectTransform)cost.transform, new Vector2(0f, 1f), new Vector2(104f, -46f), new Vector2(330f, 30f));
            }

            // A parchment announcement, a tooltip and the name of the game.
            Image note = UIKit.Parchment(sheet, "Announcement");
            UIKit.Pin((RectTransform)note.transform, new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(620f, 150f));
            RectTransform noteBody = UIKit.Content(note, 2f);
            TextMeshProUGUI noteTitle = UIKit.Title(noteBody, "Title", "Day 1, Week 1", 36f, new Color(0.25f, 0.15f, 0.07f), TextAlignmentOptions.Top);
            UIKit.Stretch((RectTransform)noteTitle.transform);
            TextMeshProUGUI noteText = UIKit.Label(noteBody, "Text", "Drive the Ironjaw raiders out of the valley.", 24f, new Color(0.2f, 0.13f, 0.06f), TextAlignmentOptions.Bottom);
            UIKit.Stretch((RectTransform)noteText.transform);

            Image tip = UIKit.Sprite(sheet, "Tooltip", art.tooltip, Color.white);
            UIKit.Pin((RectTransform)tip.transform, new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(380f, 110f));
            RectTransform tipBody = UIKit.Content(tip, 6f);
            TextMeshProUGUI tipText = UIKit.Label(tipBody, "Text", "<b>Lich</b>\nShoots death clouds. <sprite name=\"damage\"> 11-13  <sprite name=\"health\"> 30", 21f, UIKit.Ink);
            UIKit.Stretch((RectTransform)tipText.transform);

            TextMeshProUGUI logo = UIKit.Logo(sheet, "Logo", "Heroes", 96f);
            UIKit.Pin((RectTransform)logo.transform, new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(700f, 130f));
            TextMeshProUGUI over = UIKit.Label(sheet, "OverMap", "Text over the map, with a shadow", 26f, UIKit.Ink, TextAlignmentOptions.Center);
            UIKit.Pin((RectTransform)over.transform, new Vector2(0f, 0f), new Vector2(1060f, 720f), new Vector2(280f, 80f));
            UIKit.Look(over, TextLook.Shadow);

            // The pointers in rows between the two windows, and the towns.
            for (int i = 0; i < art.cursors.Length; i++)
            {
                if (art.cursors[i] == null)
                {
                    continue;
                }
                var pointer = UIKit.Rect(sheet, "Pointer" + i).gameObject.AddComponent<RawImage>();
                pointer.texture = art.cursors[i];
                UIKit.Pin((RectTransform)pointer.transform, new Vector2(0f, 0f), new Vector2(1062f + i % 7 * 40f, 620f - i / 7 * 44f), new Vector2(32f, 32f));
            }
            for (int i = 0; i < 3 && i < art.towns.Count; i++)
            {
                Image town = UIKit.Sprite(sheet, "Town" + i, art.TownPortrait(art.towns[i].faction, i), Color.white);
                town.preserveAspect = true;
                UIKit.Pin((RectTransform)town.transform, new Vector2(1f, 0f), new Vector2(-40f - i * 125f, 40f), new Vector2(120f, 120f));
            }
        }
    }
}
