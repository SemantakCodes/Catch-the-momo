using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private string gameScene;
    public GameObject donationBox;

    private void Start()
    {
        playButton.onClick.AddListener(OnPlayButtonPressed);
        quitButton.onClick.AddListener(OnQuitButtonPressed);
    }

    private void OnPlayButtonPressed()
    {
        SceneManager.LoadScene(gameScene);
    }

    private void OnQuitButtonPressed()
    {
        Application.Quit();
    }
    public void OpenDonation()
    {
        donationBox.SetActive(true);
    }
    public void CloseDonation()
    {
        donationBox.SetActive(false);
    }

        
    
}
