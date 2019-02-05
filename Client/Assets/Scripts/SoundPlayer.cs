/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    public bool initer = true;

    public bool _languageSound = false;
    public bool _isLoop = false;
    public float _mod = 1f;
    public float _blend = 1f;
    public string[] clips;

    private void Start()
    {
        if (initer)
        {
            PlaySound(clips, gameObject, _languageSound, _mod, _blend, _isLoop);
        }
    }

    public static void PlaySound(string[] sound, GameObject t, bool languageSound = false, float mod = 1f, float blend = 1f, bool isLoop = false)
    {
        if (sound == null || sound.Length == 0)
            return;

        AudioSource source = t.GetComponent<AudioSource>();
        if (source == null)
            source = t.AddComponent<AudioSource>();

        source.spatialBlend = blend;

        ushort dSound = (ushort)Random.Range(0, sound.Length);

        string tSound = "Sounds/" + ((languageSound) ? Language.loaded + "/" : "") + sound[dSound];
        AudioClip ac = Resources.Load<AudioClip>(tSound);

        if (!isLoop)
            source.PlayOneShot(ac, mod * Settings.soundVolume);
        else
        {
            SoundPlayer_Frequent spf = source.GetComponent<SoundPlayer_Frequent>();
            if (spf == null)
                spf = t.AddComponent<SoundPlayer_Frequent>();

            source.clip = ac;
            spf.mod = mod;
        } 
    }
}
