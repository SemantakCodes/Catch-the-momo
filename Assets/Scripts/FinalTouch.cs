using UnityEngine;
using System.Collections;

public class FinalTouch : MonoBehaviour
{
    [Header("Screen Shake Settings")]
    [SerializeField] private float loseLifeShakeMagnitude = 0.15f;
    [SerializeField] private float loseLifeShakeDuration = 0.3f;
    [SerializeField] private float catchMomoShakeMagnitude = 0.08f;
    [SerializeField] private float catchMomoShakeDuration = 0.15f;

    [Header("Camera Movement Settings")]
    [SerializeField] private float cameraBobSpeed = 2f;
    [SerializeField] private float cameraBobAmount = 0.05f;

    [Header("Catch Feedback")]
    [SerializeField] private float catchZoomScale = 1.1f;
    [SerializeField] private float catchZoomDuration = 0.2f;

    [Header("Death Shake Settings")]
    [SerializeField] private float deathShakeMagnitude = 0.5f;
    [SerializeField] private float deathShakeDuration = 0.6f;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip catchMomoSound;
    [SerializeField] private AudioClip catchGuffSound;
    [SerializeField] private AudioClip gameOverSound;
    [SerializeField] private float soundVolume = 0.8f;

    private Camera mainCam;
    private Vector3 originalCamPos;
    private float originalCamSize;
    private float cameraBobOffset = 0f;
    private Coroutine currentShakeCoroutine;
    private GameManager gameManager;
    private AudioSource audioSource;

    private static FinalTouch instance;

    private void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        mainCam = Camera.main;
        if (mainCam != null)
        {
            originalCamPos = mainCam.transform.position;
            originalCamSize = mainCam.orthographicSize;
        }

        gameManager = FindFirstObjectByType<GameManager>();
        
        // Setup AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.volume = soundVolume;
    }

    private void Update()
    {
        // Apply dynamic camera bobbing during gameplay
        if (mainCam != null)
        {
            cameraBobOffset = Mathf.Sin(Time.time * cameraBobSpeed) * cameraBobAmount;
            mainCam.transform.position = originalCamPos + Vector3.up * cameraBobOffset;
        }
    }

    /// <summary>
    /// Triggers screen shake when player loses a life
    /// </summary>
    public void OnLoseLife()
    {
        ScreenShake(loseLifeShakeMagnitude, loseLifeShakeDuration);
    }

    /// <summary>
    /// Triggers visual feedback when catching Momo
    /// </summary>
    public void OnCatchMomo(Vector3 catchPosition)
    {
        if (mainCam == null) return; // Guard against null camera
        
        ScreenShake(catchMomoShakeMagnitude, catchMomoShakeDuration);
        StartCoroutine(CatchZoomEffect());
        PlaySound(catchMomoSound);
    }

    /// <summary>
    /// Triggers visual feedback when catching Guff
    /// </summary>
    public void OnCatchGuff(Vector3 catchPosition)
    {
        PlaySound(catchGuffSound);
    }

    /// <summary>
    /// Triggers violent shake when game ends
    /// </summary>
    public void OnGameOver()
    {
        ScreenShake(deathShakeMagnitude, deathShakeDuration);
        PlaySound(gameOverSound);
    }

    /// <summary>
    /// Screen shake effect
    /// </summary>
    private void ScreenShake(float magnitude, float duration)
    {
        if (mainCam == null) return;

        // Stop any existing shake
        if (currentShakeCoroutine != null)
        {
            StopCoroutine(currentShakeCoroutine);
        }

        currentShakeCoroutine = StartCoroutine(ShakeCoroutine(magnitude, duration));
    }

    private IEnumerator ShakeCoroutine(float magnitude, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            mainCam.transform.position = originalCamPos + new Vector3(x, y + cameraBobOffset, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset to original position
        mainCam.transform.position = originalCamPos + Vector3.up * cameraBobOffset;
    }

    /// <summary>
    /// Brief camera zoom when catching Momo
    /// </summary>
    private IEnumerator CatchZoomEffect()
    {
        float elapsed = 0f;

        while (elapsed < catchZoomDuration)
        {
            float progress = elapsed / catchZoomDuration;
            // Zoom in then back out
            float zoomScale = Mathf.Lerp(1f, catchZoomScale, Mathf.Sin(progress * Mathf.PI));
            mainCam.orthographicSize = originalCamSize / zoomScale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset zoom
        mainCam.orthographicSize = originalCamSize;
    }

    /// <summary>
    /// Add visual "juice" - can be extended for particles, sounds, etc.
    /// </summary>
    public void AddCatchJuice()
    {
        // This method can be extended with particle effects, sound effects, etc.
        // For now it's a placeholder for future enhancements
    }

    /// <summary>
    /// Play a sound effect
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip, soundVolume);
    }
}
