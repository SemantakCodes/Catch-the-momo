using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    //Reference 
    private GameManager gameManager;
    [SerializeField] private Image[] lifeImage;
    private int lifeAmount;

    private void Start()
    {
        gameManager = Object.FindFirstObjectByType<GameManager>();
        
        // Initialize life display
        if (gameManager != null)
        {
            UpdateLife();
        }
    }

    private void UpdateLife()
    {
        if (gameManager == null) return;
        
        lifeAmount = gameManager.GetLives();
        
        // Activate/Deactivate life images based on current lives
        for (int i = 0; i < lifeImage.Length; i++)
        {
            if (i < lifeAmount)
            {
                lifeImage[i].gameObject.SetActive(true);
            }
            else
            {
                lifeImage[i].gameObject.SetActive(false);
            }
        }
    }
    
    // Public method called by GameManager when lives change
    public void OnLivesChanged()
    {
        UpdateLife();
    }
}
