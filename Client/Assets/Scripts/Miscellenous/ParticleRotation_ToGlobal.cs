/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/
using UnityEngine;

public class ParticleRotation_ToGlobal : MonoBehaviour
{
    ParticleSystem ps;
	// Use this for initialization
	void Start () {
        ps = GetComponent<ParticleSystem>();
	}
	
	// Update is called once per frame
	void Update () {
        ParticleSystem.MainModule mm = ps.main;
        mm.startRotation = transform.eulerAngles.y / 20500 * 360;
	}
}
