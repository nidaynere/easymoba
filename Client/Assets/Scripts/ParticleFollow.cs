/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using UnityEngine;

public class ParticleFollow : MonoBehaviour
{
    public float offset;
    public Transform target;
	// Use this for initialization
	void Start ()
    {
        offset = transform.position.y;
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 v = target.position;
        v.y += offset;
        transform.position = v;
	}
}
