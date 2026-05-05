using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private string gameScene;
    public GameObject donationBox;
    [SerializeField] private AudioClip buttonClickSfx;
    
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        playButton.onClick.AddListener(OnPlayButtonPressed);
        quitButton.onClick.AddListener(OnQuitButtonPressed);
    }

    private void OnPlayButtonPressed()
    {
        PlayButtonSound();
        SceneManager.LoadScene(gameScene);
    }

    private void OnQuitButtonPressed()
    {
        PlayButtonSound();
        Application.Quit();
    }

    private void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSfx != null)
        {
            audioSource.PlayOneShot(buttonClickSfx);
        }
    }

    private void OnDestroy()
    {
        playButton.onClick.RemoveListener(OnPlayButtonPressed);
        quitButton.onClick.RemoveListener(OnQuitButtonPressed);
    }

    public void OpenDonation()
    {
        PlayButtonSound(); 
        donationBox.SetActive(true);
    }
    public void CloseDonation()
    {
        PlayButtonSound();
        donationBox.SetActive(false);
    }

        
    
}
