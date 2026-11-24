using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AnimationTestPlayer : MonoBehaviour
{
    private enum Preview { Walk, Smile, WalkAndSmile, Horror }

    [System.Serializable]
    private struct FacialOption
    {
        public AnimationClip clip;
        public Button button;
    }

    [SerializeField] private GameObject character;
    [SerializeField] private Transform displayRoot;
    [SerializeField] private Transform hips;
    [SerializeField] private Transform head;
    [SerializeField] private SkinnedMeshRenderer face;
    [SerializeField] private AnimationClip walk;
    [SerializeField] private AnimationClip smile;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private Vector3 facing = Vector3.forward;
    [SerializeField] private Button walkButton;
    [SerializeField] private Button smileButton;
    [SerializeField] private Button combinedButton;
    [SerializeField] private Text playbackLabel;
    [SerializeField] private Text cameraLabel;
    [SerializeField] private FacialOption[] horrorOptions = System.Array.Empty<FacialOption>();

    private static readonly Color32 ActiveColor = new(30, 101, 100, 255);
    private static readonly Color32 InactiveColor = new(39, 50, 65, 255);

    private Preview selected;
    private Vector3 displayOrigin;
    private Vector3 walkTravel;
    private SkinnedMeshRenderer[] skinnedMeshes;
    private int horrorIndex;
    [System.Serializable]
    private sealed class FaceControl
    {
        public string blendShape;
        public string negativeBlendShape;
        public Slider slider;
        public Text readout;
        [System.NonSerialized] public int blendIndex = -1;
        [System.NonSerialized] public int negativeBlendIndex = -1;
    }

    [SerializeField] private FaceControl[] faceControls = System.Array.Empty<FaceControl>();
    [SerializeField] private Slider eyeAperture;
    [SerializeField] private Text eyeApertureReadout;

    private readonly System.Collections.Generic.List<(SkinnedMeshRenderer renderer, int left, int right,
        int blinkLeft, int blinkRight, int stare)> eyeTargets = new();

    private AnimationClip OneShotClip => selected == Preview.Smile ? smile :
        selected == Preview.Horror ? horrorOptions[horrorIndex].clip : null;
    private float time;
    private bool playing;
    private bool closeUp;

    private void Awake()
    {
        if (face.sharedMesh.GetBlendShapeIndex("Smile") < 0)
        {
            Debug.LogError("The animation preview requires the Smile blend shape.", this);
            enabled = false;
            return;
        }

        skinnedMeshes = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        BindFaceOptions();
        displayOrigin = displayRoot.position;
        walk.SampleAnimation(character, 0f);
        Vector3 start = hips.position;
        walk.SampleAnimation(character, walk.length);
        walkTravel = Vector3.ProjectOnPlane(hips.position - start, Vector3.up);
        SelectWalk();
    }

    private void Update() => Advance(Time.unscaledDeltaTime);

    private void Advance(float deltaTime)
    {
        if (!playing)
            return;

        time += deltaTime;
        var oneShot = OneShotClip;
        if (oneShot != null && time >= oneShot.length)
        {
            time = oneShot.length;
            playing = false;
            UpdateControls();
        }

        ApplyPose();
    }

    public void SelectWalk() => Select(Preview.Walk);
    public void SelectSmile() => Select(Preview.Smile);
    public void SelectWalkAndSmile() => Select(Preview.WalkAndSmile);

    public void SelectHorror(int index)
    {
        if (index < 0 || index >= horrorOptions.Length || horrorOptions[index].clip == null)
            return;

        horrorIndex = index;
        Select(Preview.Horror);
    }

    public void TogglePlayback()
    {
        var oneShot = OneShotClip;
        if (!playing && oneShot != null && time >= oneShot.length)
        {
            Restart();
            return;
        }

        playing = !playing;
        UpdateControls();
    }

    public void Restart()
    {
        time = 0f;
        playing = true;
        ApplyPose();
        UpdateControls();
    }

    public void ToggleCamera()
    {
        closeUp = !closeUp;
        FrameCharacter();
        UpdateControls();
    }

    private void BindFaceOptions()
    {
        foreach (var control in faceControls)
        {
            control.blendIndex = face.sharedMesh.GetBlendShapeIndex(control.blendShape);
            if (!string.IsNullOrEmpty(control.negativeBlendShape))
                control.negativeBlendIndex = face.sharedMesh.GetBlendShapeIndex(control.negativeBlendShape);
            control.slider.onValueChanged.AddListener(_ => UpdateFaceControl(control.slider, control.readout));
            UpdateReadout(control.slider, control.readout);
        }

        if (eyeAperture == null)
            return;

        foreach (var renderer in skinnedMeshes)
        {
            if (renderer.sharedMesh == null)
                continue;

            Mesh mesh = renderer.sharedMesh;
            int left = mesh.GetBlendShapeIndex("CustomEyeApertureLeft");
            int right = mesh.GetBlendShapeIndex("CustomEyeApertureRight");
            int blinkLeft = mesh.GetBlendShapeIndex("BlinkLeft");
            int blinkRight = mesh.GetBlendShapeIndex("BlinkRight");
            int stare = mesh.GetBlendShapeIndex("StareWide");
            if (left >= 0 && right >= 0 && blinkLeft >= 0 && blinkRight >= 0 && stare >= 0)
                eyeTargets.Add((renderer, left, right, blinkLeft, blinkRight, stare));
        }
        eyeAperture.onValueChanged.AddListener(_ => UpdateFaceControl(eyeAperture, eyeApertureReadout));
        UpdateReadout(eyeAperture, eyeApertureReadout);
    }

    private static void UpdateReadout(Slider slider, Text readout)
    {
        if (readout == null)
            return;

        int value = Mathf.RoundToInt(slider.value);
        readout.text = value > 0 && slider.minValue < 0f ? "+" + value : value.ToString();
    }

    private void UpdateFaceControl(Slider slider, Text readout)
    {
        UpdateReadout(slider, readout);
        PreviewFaceEdit();
    }

    public void ResetFaceOptions()
    {
        foreach (var control in faceControls)
        {
            control.slider.SetValueWithoutNotify(0f);
            UpdateReadout(control.slider, control.readout);
        }
        if (eyeAperture != null)
        {
            eyeAperture.SetValueWithoutNotify(0f);
            UpdateReadout(eyeAperture, eyeApertureReadout);
        }
        PreviewFaceEdit();
    }

    private void PreviewFaceEdit()
    {
        if (skinnedMeshes == null)
            return;

        closeUp = true;
        ApplyFaceOptions();
        FrameCharacter();
        UpdateControls();
    }

    private void ApplyFaceOptions()
    {
        foreach (var control in faceControls)
        {
            if (control.blendIndex < 0)
                continue;

            float value = control.slider.value;
            if (control.negativeBlendIndex >= 0)
            {
                face.SetBlendShapeWeight(control.negativeBlendIndex,
                    100f * Mathf.InverseLerp(0f, control.slider.minValue, value));
                value = 100f * Mathf.InverseLerp(0f, control.slider.maxValue, value);
            }
            face.SetBlendShapeWeight(control.blendIndex, value);
        }
        if (eyeAperture == null)
            return;

        // Let a blink close each eyelid even when the manual aperture is wide.
        foreach (var (renderer, left, right, blinkLeft, blinkRight, stare) in eyeTargets)
        {
            float widening = Mathf.Clamp01(renderer.GetBlendShapeWeight(stare) / 100f);
            float weight = eyeAperture.value * (1f - .65f * widening);
            float openLeft = 1f - Mathf.Clamp01(renderer.GetBlendShapeWeight(blinkLeft) / 100f);
            float openRight = 1f - Mathf.Clamp01(renderer.GetBlendShapeWeight(blinkRight) / 100f);
            renderer.SetBlendShapeWeight(left, weight * openLeft);
            renderer.SetBlendShapeWeight(right, weight * openRight);
        }
    }


    private void Select(Preview selection)
    {
        selected = selection;
        closeUp = OneShotClip != null;
        Restart();
    }

    private void ApplyPose()
    {
        displayRoot.position = displayOrigin;
        var oneShot = OneShotClip;
        float walkTime = oneShot != null ? 0f : Mathf.Repeat(time, walk.length);
        walk.SampleAnimation(character, walkTime);

        // Cancel the clip's travel on the stage while retaining the gait and body sway.
        displayRoot.position -= walkTravel * (walkTime / walk.length);
        foreach (var renderer in skinnedMeshes)
        {
            if (renderer.sharedMesh == null)
                continue;

            for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                renderer.SetBlendShapeWeight(i, 0f);
        }

        if (oneShot != null)
            oneShot.SampleAnimation(character, time);
        else if (selected == Preview.WalkAndSmile)
            smile.SampleAnimation(character, Mathf.Min(Mathf.Repeat(time, smile.length + 0.8f), smile.length));

        ApplyFaceOptions();
        FrameCharacter();
    }

    private void FrameCharacter()
    {
        Vector3 view = closeUp ? facing : Quaternion.Euler(0f, 13f, 0f) * facing;
        Vector3 right = Vector3.Cross(Vector3.up, -view).normalized;
        Vector3 aim = closeUp ? head.position + Vector3.up * 0.07f : displayOrigin + Vector3.up * 0.88f;
        aim -= right * (closeUp ? 0.012f : 0.07f);

        previewCamera.orthographicSize = closeUp ? 0.23f : 1.12f;
        previewCamera.transform.position = aim + view * 4f + Vector3.up * (closeUp ? 0.05f : 0.48f);
        previewCamera.transform.LookAt(aim);
    }

    private void UpdateControls()
    {
        walkButton.image.color = selected == Preview.Walk ? ActiveColor : InactiveColor;
        smileButton.image.color = selected == Preview.Smile ? ActiveColor : InactiveColor;
        combinedButton.image.color = selected == Preview.WalkAndSmile ? ActiveColor : InactiveColor;
        for (int i = 0; i < horrorOptions.Length; i++)
            horrorOptions[i].button.image.color = selected == Preview.Horror && horrorIndex == i ? ActiveColor : InactiveColor;

        playbackLabel.text = playing ? "Tạm dừng" : "Phát";
        cameraLabel.text = closeUp ? "Toàn thân" : "Cận mặt";
    }
}

