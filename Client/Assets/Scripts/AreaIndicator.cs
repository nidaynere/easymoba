/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using UnityEngine;

public class AreaIndicator : MonoBehaviour
{
    public ParticleSystem indicator;

    bool lastValue = false;

    float nextUpdate;

    // Update is called once per frame
    void FixedUpdate ()
    {
        if (nextUpdate > Time.time)
            return;

        nextUpdate = Time.time + 0.1f;

        if (MobileAgent.user != null)
        {
            bool blockValue = MapLoader.isBlocked(GameManager.currentMapId, MobileAgent.user.transform.position, transform.position, false);
            if (blockValue != lastValue)
            {
                lastValue = blockValue;

                ParticleSystem.MainModule mm = indicator.main;
                mm.startColor = blockValue ? Color.red : Color.green;
            }
        }
    }
}
