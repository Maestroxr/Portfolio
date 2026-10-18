using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// The campaign bar of the mission select (<see cref="CampaignSwitcher"/>): right of the mode tabs, above the sector
    /// map, on both tabs. It names the campaign the tab shows and its place among the mode's campaigns, tells its story
    /// (or what opens it), and has an arrow to the campaign before and after it at each end.
    /// </summary>
    internal static partial class AsteroidsInterfaceBuilder
    {
        /// <summary>Where the bar starts: right of the mode tabs (60 + 660), with a gap.</summary>
        private const float CampaignBarLeft = 736f;

        /// <summary>The bar's size: it ends before the language flag, and is as tall as the space above the map.</summary>
        private static readonly Vector2 CampaignBarSize = new Vector2(684f, 92f);

        /// <summary>How far the details and the sector maps sit below the middle of the screen, under the campaign bar.</summary>
        private const float MapOffset = -38f;

        private static void BuildCampaignBar(Transform safe, AsteroidsUI ui)
        {
            var fieldColor = new Color(0.3f, 0.6f, 0.9f, 0.9f);
            RectTransform bar = Panel(safe, "Campaign", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(CampaignBarLeft, -148f), CampaignBarSize, fieldColor);
            var switcher = bar.gameObject.AddComponent<CampaignSwitcher>();
            switcher.background = bar.GetComponent<Image>();
            switcher.fieldColor = fieldColor;
            switcher.strikeColor = new Color(0.9f, 0.55f, 0.3f, 0.9f);
            switcher.previous = CampaignArrow(bar, "Previous", false);
            switcher.next = CampaignArrow(bar, "Next", true);

            const float left = 72f;
            Image lockIcon = Image(bar, "Lock", AsteroidsArtBuilder.Icon("Lock"), Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(left, -10f), new Vector2(32f, 32f));
            lockIcon.raycastTarget = false;
            lockIcon.gameObject.SetActive(false);
            switcher.lockIcon = lockIcon.gameObject;
            switcher.title = Text(bar, "Title", "THE RAIDER WAR", 30f, Cyan, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(left, -6f),
                new Vector2(370f, 38f), titleFont);
            switcher.title.characterSpacing = 4f;
            switcher.count = Text(bar, "Count", "CAMPAIGN 1 / 3", 15f, Dim, TextAlignmentOptions.Right, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-left, -14f),
                new Vector2(150f, 24f), hudFont);
            switcher.count.characterSpacing = 4f;
            switcher.count.textWrappingMode = TextWrappingModes.NoWrap;
            AutoSize(switcher.count, 11f, 15f);
            switcher.description = Text(bar, "Description", "Description", 17f, Soft, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(left, -42f), new Vector2(CampaignBarSize.x - left * 2f, 48f), null, false);
            AutoSize(switcher.description, 12f, 17f);
            ui.campaignSwitcher = switcher;
        }


        /// <summary>An arrow button at one end of the campaign bar: the "play" triangle, turned round for the one on the left.</summary>
        private static Button CampaignArrow(Transform bar, string name, bool forward)
        {
            var anchor = new Vector2(forward ? 1f : 0f, 0.5f);
            Image background = Image(bar, name, button, Blue, anchor, anchor, new Vector2(forward ? -10f : 10f, 0f), new Vector2(52f, 74f), true);
            var component = background.gameObject.AddComponent<Button>();
            component.targetGraphic = background;
            component.colors = ButtonColors();
            // A disabled arrow fades as a whole (the tint greys only the button).
            background.gameObject.AddComponent<CanvasGroup>();
            Image icon = Image(background.transform, "Icon", AsteroidsArtBuilder.Icon("Play"), Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(28f, 28f));
            icon.raycastTarget = false;
            if (!forward)
            {
                icon.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            }
            return component;
        }
    }
}
