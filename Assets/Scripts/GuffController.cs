using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class GuffController : MonoBehaviour
{
    [Header("Falling Settings")]
    [SerializeField] private float fallSpeed = 5f;
    [SerializeField] private bool useGravity = true;
    [Header("Collision Settings")]
    [SerializeField] private string playerTag = "Player"; // Tag on player object
    [SerializeField] private float boundsCheckInterval = 0.5f; // Check bounds periodically for efficiency
    private Rigidbody2D rb;
    private Camera mainCam;
    private GameManager gameManager;
    private bool isActive = true;
    [Header("Guff Settings")]
    [SerializeField] private string[] Guff;
    [SerializeField] private TMPro.TextMeshPro tmp;

    private void OnEnable()
    {
        isActive = true;
        // Get the TextMeshProUGUI component if not already assigned
        if (tmp == null)
        {
            tmp = GetComponentInChildren<TextMeshPro>();
        }
            
        // Select and print a random guff on each spawn
        if (Guff != null && Guff.Length > 0 && tmp != null)
        {
            int randomIndex = Random.Range(0, Guff.Length);
            string selectedGuff = Guff[randomIndex];
            tmp.SetText(selectedGuff);
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
        gameManager = Object.FindFirstObjectByType<GameManager>();
        // Setup physics
        rb.gravityScale = useGravity ? 1f : 0f;
        rb.linearVelocity = new Vector2(0, -fallSpeed);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if collided with player - Lose a life!
        if (collision.CompareTag(playerTag))
        {
            isActive = false;
            // Get player movement component
            PlayerMovement playerMovement = collision.GetComponent<PlayerMovement>();
            // Only deal damage if player is not invincible
            if (playerMovement != null && !playerMovement.IsInvincible())
            {
                playerMovement.Blink();
                playerMovement.ResetPosition();
                if (gameManager != null)
                {
                    gameManager.LoseLife(); // Lose one life
                    
                    // Trigger catch guff sound
                    FinalTouch finalTouch = Object.FindFirstObjectByType<FinalTouch>();
                    if (finalTouch != null)
                    {
                        finalTouch.OnCatchGuff(transform.position);
                    }
                }
            }
            
            RemoveObject(); // Remove the Guff after collision
        }
        // Check if collided with ground
        else if (collision.CompareTag("ground"))
        {
            RemoveObject(); // Destroy when hitting ground
        }
    }

    private void RemoveObject()
    {
        if (!isActive) return;

        isActive = false;

        // Call back to GameManager for pooling (if using the improved GameManager)
        if (gameManager != null)
        {
            gameManager.OnObjectRemoved(gameObject);
        }
        else
        {
            // Fallback if GameManager isn't available
            gameObject.SetActive(false);
        }
    }

    // Public method to reset fall speed (useful for difficulty scaling)
    public void SetFallSpeed(float newSpeed)
    {
        fallSpeed = newSpeed;
        if (rb != null && !useGravity)
        {
            rb.linearVelocity = new Vector2(0, -fallSpeed);
        }
    }
}
