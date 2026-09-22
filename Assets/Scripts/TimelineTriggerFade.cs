using UnityEngine;

public class TimelineTriggerFade : MonoBehaviour
{
    public CinematicEffects effects;
    public bool fadeToBlack = true; // True = Ngất đi, False = Tỉnh dậy

    void OnEnable()
    {
        if (Time.timeSinceLevelLoad < 1f) 
        {
            return;
        }

        if (effects != null)
        {
            if (fadeToBlack)
            {
                effects.FadeToBlack();
            }
            else
            {
                effects.FadeToClear();
            }
        }
    }
}
