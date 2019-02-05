/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using System.Collections.Generic;
using UnityEngine;

public class UIMapItem : MonoBehaviour
{
    public static List<UIMapItem> list = new List<UIMapItem>();
    public int online;
	// Use this for initialization
	void Awake ()
    {
        list.Add(this);
	}

    public void KeyClicked ()
    {
        GameManager.singleton.SelectMap(transform.GetSiblingIndex());
    }
}
