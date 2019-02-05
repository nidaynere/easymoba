/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using UnityEngine;

public class UIGridSystem : MonoBehaviour
{
    public Vector2 step = new Vector2(0, 50);
    public Vector2 offset = new Vector2(0, 0);
    public bool UseContentSize;
    public float UpdateTime = 0.1f;
	// Use this for initialization
	void Start ()
    {
        if (UpdateTime < 0.1f)
            UpdateTime = 0.1f;
        rect = GetComponent<RectTransform>();
        lastChild = transform.childCount;
        InvokeRepeating("Updater", 0f, UpdateTime);
    }

    int lastChild = 0;
    float height, width;
    RectTransform rect;
    Vector3 calculated;
    public bool UpdateAnyway = false;

	// Update is called once per frame
	void Updater ()
    {
        if (lastChild != transform.childCount ||UpdateAnyway)
        {
            if (UpdateAnyway)
                UpdateAnyway = false;

            lastChild = transform.childCount;
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).position = new Vector2(transform.position.x + offset.x, transform.position.y + offset.y) + i * step;
            }

            if (UseContentSize)
            {
                height = transform.childCount * step.y + offset.y;
                width = transform.childCount * step.x + offset.x;
                rect.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));
            }
        }
    }
}
