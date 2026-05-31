using UnityEngine;
using UnityEngine.UI;

public class AudioVolumeSliderBinder : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        RefreshSlidersFromAudioManager();
        BindSliderEvents();
    }

    private void OnDestroy()
    {
        UnbindSliderEvents();
    }

    public void RefreshSlidersFromAudioManager()
    {
        if (MusicAudioManager.Instance == null)
            return;

        SetSliderValueWithoutNotify(masterSlider, MusicAudioManager.Instance.GetMasterVolume());
        SetSliderValueWithoutNotify(bgmSlider, MusicAudioManager.Instance.GetBgmVolume());
        SetSliderValueWithoutNotify(sfxSlider, MusicAudioManager.Instance.GetSfxVolume());
    }

    private void BindSliderEvents()
    {
        if (masterSlider != null)
            masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);

        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
    }

    private void UnbindSliderEvents()
    {
        if (masterSlider != null)
            masterSlider.onValueChanged.RemoveListener(OnMasterSliderChanged);

        if (bgmSlider != null)
            bgmSlider.onValueChanged.RemoveListener(OnBgmSliderChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
    }

    private void OnMasterSliderChanged(float value)
    {
        if (MusicAudioManager.Instance == null)
            return;

        MusicAudioManager.Instance.SetMasterVolume(value);
    }

    private void OnBgmSliderChanged(float value)
    {
        if (MusicAudioManager.Instance == null)
            return;

        MusicAudioManager.Instance.SetBgmVolume(value);
    }

    private void OnSfxSliderChanged(float value)
    {
        if (MusicAudioManager.Instance == null)
            return;

        MusicAudioManager.Instance.SetSfxVolume(value);
    }

    private static void SetSliderValueWithoutNotify(Slider slider, float value)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(value);
    }
}
