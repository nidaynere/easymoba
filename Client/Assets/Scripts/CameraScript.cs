/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public Vector3 offset;

    public Vector3 gameAngle = new Vector3 (55, 0, 0);

    Vector3 defAngle;

    void Start()
    {
        defAngle = transform.eulerAngles;
        defaultOffsetToZero = transform.position;
    }

    public static Vector3 sessionPosition = new Vector3(-2000, 0, -2000);
    public static Vector3 defaultOffsetToZero;

    public float wanderingSpeed = 16;
	// Update is called once per frame
	void Update ()
    {
        if (MobileAgent.user != null && MobileAgent.user.isDead)
        {
            switch (KeyController.current.currentController)
            {
                case 0:
                    transform.position += new Vector3(Input.GetAxis("Mouse X"), 0, Input.GetAxis("Mouse Y")) * Time.deltaTime * wanderingSpeed;
                break;
                case 1: case 2:
                    transform.position += new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) * Time.deltaTime * wanderingSpeed;
                break;
            }
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, (GameManager.sessionStarted) ? MobileAgent.user.transform.position + offset : sessionPosition, 0.25f);

            transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, (GameManager.sessionStarted) ? gameAngle : defAngle, 0.25f);
        }
    }
}
