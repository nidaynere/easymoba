/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/
using UnityEngine;

public class TextEffect : MonoBehaviour
{
    public float speed = 10f;

    public Vector3 v;
	// Update is called once per frame
	void Update () {
        transform.localPosition += (v - transform.localPosition) * speed * Time.deltaTime;
	}
}
