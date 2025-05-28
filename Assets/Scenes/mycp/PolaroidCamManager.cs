using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PolaroidCamManager : MonoBehaviour
{
    [Header("Polaroid Components")]
    public GameObject polaroidCam;
    public Animator polaroidAnimator;
    public string animationName = "PolaroidStart";

    [Header("UI Panels")]
    public GameObject polaroidUI; //cam view ui
    public GameObject polaroidCollectionUI;
    public RawImage photoDisplay;
    public Image flashOverlay; // fullscreen white image

    [Header("Reveal Settings")]
    public float revealDuration = 10f;       // seconds to reveal
    public Color startTint = new Color(0.7f, 0.8f, 1f, 0.3f); // faint blue transparent
    public Color endTint = new Color(1f, 1f, 1f, 1f);       // white opaque

    [Header("Timing")]
    public float fallbackAnimDuration = 1.5f;

    private bool isReadyToShoot = false;
    private bool isProcessing = false;
    private bool photoTaken = false;

    [Header("Post Processing")]
    public Volume polaroidDOFVolume;  // drag in your LocalVolume (child of polaroidUI)


    void Start()
    {
        polaroidUI.SetActive(false);
        polaroidCollectionUI.SetActive(false);
        SetPolaroidVisible(false);
        flashOverlay.color = new Color(1f, 1f, 1f, 0f);
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.O) && Input.GetKeyDown(KeyCode.P) && !isProcessing)
        {
            StartCoroutine(ActivatePolaroidSequence());
        }

        if (isReadyToShoot && Input.GetMouseButtonDown(0))
        {
            StartCoroutine(CapturePhoto());
        }

        // Toggle photo view
        if (Input.GetKeyDown(KeyCode.C) && photoTaken)
        {
            polaroidCollectionUI.SetActive(!polaroidCollectionUI.activeSelf);
        }
    }

    IEnumerator ActivatePolaroidSequence()
    {
        isProcessing = true;

        polaroidAnimator.Play(animationName, -1, 0f);

        polaroidCam.SetActive(true);
        SetPolaroidVisible(true);
        polaroidAnimator.Play(animationName);

        float duration = fallbackAnimDuration;
        AnimatorStateInfo state = polaroidAnimator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName(animationName)) duration = state.length;
        yield return new WaitForSeconds(duration);

        SetPolaroidVisible(false);
        polaroidUI.SetActive(true);
        StartCoroutine(FocusAndExposureLerp(0.1f, 10f, -3f, -0.15f, 1f)); //PP adjust
        isReadyToShoot = true;
    }


    IEnumerator CapturePhoto()
    {
        isReadyToShoot = false;

        polaroidUI.SetActive(false);  // Hide UI first
        yield return new WaitForEndOfFrame();

        // Capture
        /*        Texture2D photo = new(Screen.width, Screen.height, TextureFormat.RGB24, false);
                photo.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                photo.Apply();*/ //if i need to adjust bounds
        Texture2D photo = ScreenCapture.CaptureScreenshotAsTexture();


        RenderTexture.active = null;

        // Flash
        StartCoroutine(FlashEffect());

        // Apply to photoDisplay
        photoDisplay.texture = photo;
        photoDisplay.color = startTint;

        // Reveal animation
        StartCoroutine(RevealPhoto(photoDisplay));

        polaroidCollectionUI.SetActive(true);
        photoTaken = true;
        isProcessing = false;
    }

    IEnumerator RevealPhoto(RawImage img)
    {
        float elapsed = 0f;
        while (elapsed < revealDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / revealDuration);
            img.color = Color.Lerp(startTint, endTint, t);
            yield return null;
        }
    }

    IEnumerator FlashEffect()
    {
        float flashTime = 1f;
        float elapsed = 0f;

        // Start fully white
        flashOverlay.color = new Color(1f, 1f, 1f, 1f);

        while (elapsed < flashTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / flashTime);
            flashOverlay.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        flashOverlay.color = new Color(1f, 1f, 1f, 0f);
    }

    IEnumerator FocusAndExposureLerp(float focusFrom, float focusTo, float exposureFrom, float exposureTo, float duration)
    {
        if (polaroidDOFVolume == null) yield break;

        VolumeProfile profile = polaroidDOFVolume.profile;

        bool hasDOF = profile.TryGet<DepthOfField>(out var dof);
        bool hasExposure = profile.TryGet<ColorAdjustments>(out var exposure);

        if (!hasDOF && !hasExposure) yield break;

        float elapsed = 0f;

        if (hasDOF)
        {
            dof.active = true;
            dof.mode.value = DepthOfFieldMode.Bokeh;
        }

        if (hasExposure)
        {
            exposure.active = true;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (hasDOF)
                dof.focusDistance.value = Mathf.Lerp(focusFrom, focusTo, t);

            if (hasExposure)
                exposure.postExposure.value = Mathf.Lerp(exposureFrom, exposureTo, t);

            yield return null;
        }

        if (hasDOF)
            dof.focusDistance.value = focusTo;

        if (hasExposure)
            exposure.postExposure.value = exposureTo;
    }

    void SetPolaroidVisible(bool visible)
    {
        foreach (var renderer in polaroidCam.GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = visible;
        }
    }
}