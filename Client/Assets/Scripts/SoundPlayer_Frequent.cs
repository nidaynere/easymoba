/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using UnityEngine;

public class SoundPlayer_Frequent : MonoBehaviour
{
    public bool isMusic;
    public bool requireGameStart = true;
    AudioSource source;
    public bool down;
    public float mod = 1;
    // Update is called once per frame
	void Update () 
    {
		if (source == null)
			source = GetComponent<AudioSource>();

		if (source == null)
			return;
		
        if (down && mod > 0)
            mod -= Time.deltaTime * 2;

        if (source != null)
        source.volume = mod * ((isMusic) ? Settings.musicVolume : Settings.soundVolume);
	}
}
