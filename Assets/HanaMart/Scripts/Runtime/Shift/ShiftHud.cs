using HanaMart.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HanaMart.Shift
{
    [RequireComponent(typeof(ShiftGame), typeof(InteractionPrompt))]
    public sealed class ShiftHud : MonoBehaviour
    {
        ShiftGame game;
        GameObject root, dialog;
        TMP_FontAsset font;
        TextMeshProUGUI status, subtitle, greet, lines;

        void Start()
        {
            game = GetComponent<ShiftGame>();
            var source = GetComponent<InteractionPrompt>().sourceFont;
            if (!source) return;
            font = TMP_FontAsset.CreateFontAsset(source);
            root = new GameObject("HanaMart shift HUD", typeof(Canvas), typeof(CanvasScaler));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            status = Text(root.transform, "Giờ và cảnh cáo", new Vector2(0, 1), new Vector2(90, -42), new Vector2(132, 56),
                23, TextAlignmentOptions.Center);
            subtitle = Text(root.transform, "Phụ đề", new Vector2(.5f, 0), new Vector2(0, 52), new Vector2(1020, 78),
                20, TextAlignmentOptions.Center);
            greet = Text(root.transform, "Chào khách", new Vector2(.5f, 0), new Vector2(0, 166), new Vector2(420, 36),
                20, TextAlignmentOptions.Center);
            dialog = new GameObject("Đối thoại", typeof(RectTransform), typeof(Image));
            dialog.transform.SetParent(root.transform, false);
            SetRect(dialog.GetComponent<RectTransform>(), new Vector2(.5f, .35f), Vector2.zero, new Vector2(740, 252));
            dialog.GetComponent<Image>().color = new Color(.025f, .036f, .043f, .94f);
            dialog.GetComponent<Image>().raycastTarget = false;
            lines = Text(dialog.transform, "Lời thoại và lựa chọn", new Vector2(.5f, .5f), Vector2.zero, new Vector2(670, 211),
                21, TextAlignmentOptions.TopLeft);
            dialog.SetActive(false);
        }

        void LateUpdate()
        {
            if (!root || game.Run == null) return;
            status.text = $"{game.Run.Clock.Display}\n{game.Run.Progress.Strikes}/3";
            subtitle.text = game.Subtitle;
            greet.text = game.GreetingTime > 0 && game.Dialogue == null ? "[G] Chào khách" : "";
            dialog.SetActive(game.Dialogue != null);
            if (!dialog.activeSelf) return;
            lines.text = $"<color=#7dffe1><b>{game.Speaker}</b></color>\n{game.Dialogue}\n";
            for (int i = 0; i < game.Options.Length; ++i) lines.text += $"\n[{i + 1}] {game.Options[i].Label}";
        }

        TextMeshProUGUI Text(Transform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size,
            float points, TextAlignmentOptions align)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(parent, false);
            SetRect(label.rectTransform, anchor, offset, size);
            label.font = font;
            label.fontSize = points;
            label.alignment = align;
            label.color = new Color(.85f, .92f, .94f);
            label.raycastTarget = false;
            return label;
        }

        static void SetRect(RectTransform rect, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
        }

        void OnDestroy()
        {
            if (root) Destroy(root);
            if (!font) return;
            Destroy(font.material);
            foreach (var texture in font.atlasTextures) if (texture) Destroy(texture);
            Destroy(font);
        }
    }
}
