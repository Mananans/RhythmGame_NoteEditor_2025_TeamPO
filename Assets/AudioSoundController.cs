using UnityEngine;
using UnityEngine.UI;

public class AudioSoundController : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;
    [SerializeField] Slider slider;
    [SerializeField] Button PauseButton;
    [SerializeField] Button PlayButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        slider.onValueChanged.AddListener(delegate { setAudioVolum(); });
        PauseButton.onClick.AddListener(delegate { pauseButton(); });
        PlayButton.onClick.AddListener(delegate { playButton(); });
    }

    void setAudioVolum()
    {
        audioSource.volume = slider.value;
    }

    void pauseButton()
    {
        audioSource.Pause();
    }

    void playButton() 
    { 
        audioSource.Play();
    }

    // Update is called once per frame
    void Update()
    {   
    }
}
