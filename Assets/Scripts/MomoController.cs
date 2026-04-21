using UnityEngine;

public class MomoController : MonoBehaviour
{
    [Header("Falling Settings")]
    [SerializeField] private float fallSpeed = 5f;
    [SerializeField] private bool useGravity = true;

    [Header("Collision Settings")]
    [SerializeField] private string playerTag = "Player"; // Tag on player object
    [SerializeField] private float boundsCheckInterval = 0.5f; // Check bounds periodically for efficiency

    private Rigidbody2D rb;
    private Camera mainCam;
    private float screenBottomY;
    private float nextBoundsCheckTime;
    private GameManager gameManager;
    private bool isActive = true;

    private void OnEnable()
    {
        isActive = true;
        nextBoundsCheckTime = Time.time + boundsCheckInterval;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
        gameManager = Object.FindFirstObjectByType<GameManager>();

        if (rb == null)
        {
            Debug.LogWarning("MomoController: No Rigidbody2D found! Adding one.");
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        // Setup physics
        rb.gravityScale = useGravity ? 1f : 0f;
        rb.linearVelocity = new Vector2(0, -fallSpeed);

        // Cache screen bounds
        float distance = 10f;
        screenBottomY = mainCam.ViewportToWorldPoint(new Vector3(0, -0.5f, distance)).y;
    }

    private void Update()
    {
        // Efficient bounds checking at intervals instead of every frame
        if (isActive && Time.time >= nextBoundsCheckTime)
        {
            CheckScreenBounds();
            nextBoundsCheckTime = Time.time + boundsCheckInterval;
        }
    }

    private void CheckScreenBounds()
    {
        // Destroy if below screen
        if (transform.position.y < screenBottomY)
        {
            RemoveObject();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if collided with player - Game Over!
        if (collision.CompareTag(playerTag))
        {
            isActive = false;
            if (gameManager != null)
            {
                gameManager.EndGame();
            }
            else
            {
                Debug.LogWarning("GameManager not found for game over!");
            }
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
