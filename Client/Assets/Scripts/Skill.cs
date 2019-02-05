/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using System.Collections.Generic;
using UnityEngine;

public class Skill : MonoBehaviour
{
    public static List<Skill> list = new List<Skill>();

    public int casterId;
    public float offset;
    public string id;
    public float moveSpeed;

    private void Start()
    {
        list.Add(this);
    }

    // Update is called once per frame
    void Update ()
    {
        Vector3 v = transform.position;
        Quaternion forward = transform.rotation;
        forward.eulerAngles = new Vector3(0, forward.eulerAngles.y, 0);

        v +=  (forward * Vector3.forward) * moveSpeed * Time.deltaTime;

        SetPosition(v);
    }

    public void SetPosition(Vector3 v)
    {
        v.y = MapLoader.GetHeight(v) + offset;
        transform.position = v;
    }
}
