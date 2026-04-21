using UnityEngine;

public class MomoController : MonoBehaviour
{
    [Header("Falling Settings")]
    [SerializeField] private float fallSpeed = 5f;
    [SerializeField] private bool useGravity = true;

    [Header("Collision Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float boundsCheckInterval = 0.5f;

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

        // Setup physics for falling
        rb.gravityScale = useGravity ? 1f : 0f;
        rb.linearVelocity = new Vector2(0, -fallSpeed);

        // Cache screen bounds
        float distance = 10f;
        screenBottomY = mainCam.ViewportToWorldPoint(new Vector3(0, -0.5f, distance)).y;
    }

    private void Update()
    {
        // Check bounds periodically for efficiency
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
        // Check if collided with player
        if (collision.CompareTag(playerTag))
        {
            isActive = false;

            // Increase score through GameManager
            if (gameManager != null)
            {
                gameManager.AddScore(1); // Add 1 point for catching Momo
            }

            // Drop and destroy Momo
            RemoveObject();
        }
    }

    private void RemoveObject()
    {
        if (!isActive) return;

        isActive = false;

        // Call back to GameManager for pooling
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
}
