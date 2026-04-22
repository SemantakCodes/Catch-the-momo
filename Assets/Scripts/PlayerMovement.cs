using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float smoothTime;

    [Header("Hit Feedback")]
    [SerializeField] private float blinkDuration = 0.3f; // How long the blink effect lasts
    [SerializeField] private float blinkSpeed = 0.1f; // How fast the player blinks

    private Camera mainCam;
    private float minX, maxX;
    private float targetX;
    private float velocityX = 0f;
    private SpriteRenderer spriteRenderer;
    private Vector3 startPosition; // Store initial position
    private bool isInvincible = false; // Invincibility flag during blink

    private void Start()
    {
        mainCam = Camera.main;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        CalculateScreenBounds();
        targetX = transform.position.x;
        startPosition = transform.position; // Store the starting position
    }
    private void Update()
    {
        HandleInput();
        SmoothMovement();
    }
    private void HandleInput()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3 touchPosition = mainCam.ScreenToWorldPoint(Input.mousePosition);
            targetX = Mathf.Clamp(touchPosition.x, minX, maxX);
        }
    }
    private void SmoothMovement()
    {
        float newX = Mathf.SmoothDamp(transform.position.x, targetX, ref velocityX, smoothTime);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }
    private void CalculateScreenBounds()
    {
        float distance = transform.position.z - mainCam.transform.position.z;
        Vector3 leftEdge = mainCam.ViewportToWorldPoint(new Vector3(0,0, distance));
        Vector3 rightEdge = mainCam.ViewportToWorldPoint(new Vector3 (1f,0f,distance));

        SpriteRenderer spriteRendererCheck = GetComponentInChildren<SpriteRenderer>();
        float spriteHalfWidth = spriteRendererCheck != null ? spriteRendererCheck.bounds.extents.x : 0.5f;

        minX = leftEdge.x + spriteHalfWidth;
        maxX = rightEdge.x - spriteHalfWidth;
    }

    // Blink effect when player gets hit
    public void Blink()
    {
        if (spriteRenderer != null)
        {
            StartCoroutine(BlinkCoroutine());
        }
    }

    // Check if player is currently invincible
    public bool IsInvincible()
    {
        return isInvincible;
    }

    // Reset player position to starting point
    public void ResetPosition()
    {
        transform.position = startPosition;
        targetX = startPosition.x;
        velocityX = 0f;
    }

    private System.Collections.IEnumerator BlinkCoroutine()
    {
        isInvincible = true; // Start invincibility
        float elapsedTime = 0f;

        while (elapsedTime < blinkDuration)
        {
            // Toggle sprite visibility
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(blinkSpeed);
            elapsedTime += blinkSpeed;
        }

        // Ensure sprite is visible at the end
        spriteRenderer.enabled = true;
        isInvincible = false; // End invincibility
    }
}
