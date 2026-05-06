using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    public ParticleSystem flash;

    public void PlayFlash()
    {
        if (flash != null)
        {
            flash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            flash.Play();
        }
    }
}
