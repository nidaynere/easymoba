using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HookScript : MonoBehaviour
{
    public MobileAgent from;
    public MobileAgent to;

    public Transform hook;
    public LineRenderer liner;

    private void Start()
    {
        if (to == null)
        {
            to = MobileAgent.list.Find (x=>x.id == GetComponent<Skill>().casterId);
        }

        if (to != null && from != null) // It's an after effect
            Destroy(gameObject, 0.5f); // destroy after 0.6 sec
    }

    // Update is called once per frame
    void Update ()
    {
        if (from != null && to != null)
        {
            from.transform.position = Vector3.Lerp (from.transform.position, to.transform.position + to.transform.forward, 0.07f);
            transform.position = to.transform.position;
            transform.rotation = Quaternion.LookRotation(from.transform.position - transform.position);

            hook.position = from.transform.position + Vector3.up;
            liner.SetPosition(0, transform.position + Vector3.up);
            liner.SetPosition(1, hook.position);
        }
        else
        {
            if (to != null)
            {
                liner.SetPosition(0, to.transform.position);
                liner.SetPosition(1, hook.position);
            }
        }
	}
}
