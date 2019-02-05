/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using UnityEngine;

public class UIFollowAgent : MonoBehaviour
{
	public Vector3 offset = Vector3.up;
	public Transform target;
	// Update is called once per frame
	void Update () 
	{
		if (target == null) {
			Destroy (gameObject);
			return;
		}

		Vector3 v = Camera.main.WorldToScreenPoint (target.position);
		transform.position = v + offset * (Screen.height / 720f);
	}
}
