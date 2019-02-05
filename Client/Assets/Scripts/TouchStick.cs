/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using UnityEngine;

public class TouchStick : MonoBehaviour
{
    public Transform stick;
    public Transform bg;
    public UIVisibility panel;

    Rect touchArea;
	// Use this for initialization
	void Start ()
    {
        touchArea = new Rect(0, 0, Screen.width / 2, Screen.height);
	}

    float notTouch;

    public static bool touched = false;
    public static Vector2 dir;

    int tCount = 1;
    Vector2 pos;
    // Update is called once per frame
    void Update()
    {
        if (KeyController.current.currentController != 3) // Only for touch platform
            return;

        tCount = (Application.isMobilePlatform) ? Input.touchCount : (Input.GetMouseButton (0) ? 1 : 0);
        for (int i = 0; i < tCount; i++)
        {
            pos = (Application.isMobilePlatform) ? Input.GetTouch(i).position : new Vector2 (Input.mousePosition.x, Input.mousePosition.y);

            if (touchArea.Contains(pos))
            {
                notTouch = 0;
                if (!panel.activeSelf)
                {
                    panel.transform.position = pos;
                    panel.Open(true);
                }

                dir = pos - new Vector2 (bg.position.x, bg.position.y);

                if (dir.magnitude > 50)
                {
                    dir = dir.normalized * 50;
                }

                stick.position = bg.position + new Vector3 (dir.x, dir.y, 0);

                touched = true;

                return;
            }
        }

        if (touched)
        stick.position = bg.position;

        touched = false;
        notTouch += Time.deltaTime;
        if (notTouch > 0.25f)
        {
            panel.Open(false);
        }
	}
}
