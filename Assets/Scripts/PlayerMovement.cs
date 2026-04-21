using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float smoothTime;

    private Camera mainCam;
    private float minX, maxX;
    private float targetX;
    private float velocityX = 0f;

    private void Start()
    {
        mainCam = Camera.main;
        CalculateScreenBounds();
        targetX = transform.position.x;
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

        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        float spriteHalfWidth = spriteRenderer != null ? spriteRenderer.bounds.extents.x : 0.5f;

        minX = leftEdge.x + spriteHalfWidth;
        maxX = rightEdge.x - spriteHalfWidth;
    }
}
