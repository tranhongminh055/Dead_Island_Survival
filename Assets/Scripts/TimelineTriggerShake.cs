using UnityEngine;

public class TimelineTriggerShake : MonoBehaviour
{
    public CinematicEffects effects;
    public float duration = 5f;
    public float magnitude = 1f;

    void OnEnable()
    {
        if (effects != null)
        {
            effects.TriggerCameraShake(duration, magnitude);
        }
    }
}
