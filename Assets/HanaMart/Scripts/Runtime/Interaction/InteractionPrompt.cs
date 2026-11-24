using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HanaMart.Interaction
{
    [RequireComponent(typeof(Interactor))]
    public sealed class InteractionPrompt : MonoBehaviour
    {
        public Font sourceFont;
        Interactor interactor;
        GameObject canvasObject;
        TextMeshProUGUI label;
        Image bar;
        TMP_FontAsset font;
        void Start()
        {
            interactor = GetComponent<Interactor>();
            canvasObject = new GameObject("Interaction HUD", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720);
            font = TMP_FontAsset.CreateFontAsset(sourceFont);
            label = new GameObject("Prompt", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(canvasObject.transform, false);
            var rect = label.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0); rect.pivot = new Vector2(.5f, 0); rect.anchoredPosition = new Vector2(0, 105); rect.sizeDelta = new Vector2(900, 60);
            label.font = font; label.fontSize = 20; label.alignment = TextAlignmentOptions.Center; label.color = new Color(.83f, .91f, .94f); label.raycastTarget = false;
            bar = new GameObject("Hold", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            bar.transform.SetParent(canvasObject.transform, false); bar.color = new Color(.35f, .8f, .84f); bar.raycastTarget = false;
            var br = bar.rectTransform; br.anchorMin = br.anchorMax = new Vector2(.5f, 0); br.pivot = new Vector2(0, .5f); br.anchoredPosition = new Vector2(-100, 105);
        }
        void LateUpdate()
        {
            if (!label) return;
            bool show = interactor.Enabled && interactor.Current && !string.IsNullOrEmpty(interactor.Label);
            label.text = show ? "[E]  " + interactor.Label : "";
            bar.rectTransform.sizeDelta = new Vector2(show ? 200 * interactor.Progress : 0, 2);
        }
        void OnDestroy()
        {
            if (canvasObject) Destroy(canvasObject);
            if (font) { Destroy(font.material); foreach (var texture in font.atlasTextures) if (texture) Destroy(texture); Destroy(font); }
        }
    }
}
