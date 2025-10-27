using UnityEngine;

public class Cartridge : MonoBehaviour
{
    [Tooltip("Time in seconds before shell cartridge starts blinking")]
    public float m_BlinkStartTime = 10f;
    [Tooltip("Time in seconds between flash and disappearance of shell cartridge")]
    public float m_LifeTime = 10f;
    [Tooltip("Interval between blinks in seconds")]
    public float m_BlinkInterval = 0.2f;
    private float m_BlinkTimer; // Timer used for blinking effect

    private float m_LifeTimer;   // Timer to track the lifetime of the cartridge
    private Renderer m_Renderer; // Reference to the Renderer component

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        m_LifeTimer = 0f;
        m_BlinkTimer = 0f;
        m_Renderer = GetComponent<Renderer>();
    }

    // Update is called once per frame
    private void Update()
    {
        m_LifeTimer += Time.deltaTime;

        // If lifetime exceeded, destroy the cartridge
        if (m_LifeTimer >= m_BlinkStartTime + m_LifeTime)
        {
            Destroy(gameObject);
            return;
        }

        // If within blinking period, handle blinking effect
        if (m_LifeTimer >= m_BlinkStartTime)
        {
            m_BlinkTimer += Time.deltaTime;
            if (m_BlinkTimer >= m_BlinkInterval)
            {
                m_Renderer.enabled = !m_Renderer.enabled;
                m_BlinkTimer = 0f;
            }
        }
    }
}
