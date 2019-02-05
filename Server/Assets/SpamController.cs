/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 24 December 2017
*/

using UnityEngine;

public class SpamController : MonoBehaviour 
{
	public static SpamController obj;
	
	void Start ()
	{
        obj = this;
	}

    public bool SpammerEnabled = true;
	public void SpamFor (MobileAgent p, ushort count)
	{
        if (SpammerEnabled)
        p.spammer += count;
	}
}
