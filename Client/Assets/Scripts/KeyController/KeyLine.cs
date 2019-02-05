/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using System.Collections.Generic;
using UnityEngine;

public class KeyLine : KeyBinder // This is a keybinder at all.
{
    public static List<KeyLine> keyLineList = new List<KeyLine>();
    public Transform lineHolder;
    public string axis; // Horizontal Vertical

    public int direction;

    public float axisDead = 0.05f;

    public string inverseAxis;
    public int inverseDirectionStep = 5;

    public bool alwaysActive = false;
    public bool interactable = false;

    void OnEnable()
    {
        if (interactable)
            Init();
    }

    public Transform targeter; // will be actived when the key line is needed by the KeyController

    public Transform stepPointer; // can be null
    public Vector3 stepPointerOffset;

    public int currentStep;

    float nextCheck = 0;
	// Update is called once per frame
	void Update ()
    {
        if (stepPointer && targetPointer != null)
            stepPointer.position = targetPointer.transform.position + stepPointerOffset;

        if (KeyController.current.savedController == -1 && (KeyController.current.currentController == 1 || KeyController.current.currentController == 2)) // Gamepad or 360 Controller
        {
            if (targeter != null)
            {
                if (!targeter.gameObject.activeSelf)
                    targeter.gameObject.SetActive(true);
            }

            if (lineHolder.childCount == 0)
                return;

            if (targetPointer == null)
                targetPointer = lineHolder.GetChild(0).gameObject;

            if (nextCheck < Time.time)
            {
                float fVal = Input.GetAxis(axis);
                float iVal = !string.IsNullOrEmpty (inverseAxis) ? Input.GetAxis(inverseAxis) : 0;

                if (Mathf.Abs(fVal) > axisDead || Mathf.Abs (iVal) > axisDead)
                {
                    nextCheck = Time.time + 0.3f;

                    Set();
                    KeyExit(); // because the new one on the way

                    if (fVal == 0)
                        currentStep += ((iVal > 0) ? -1 : 1) * direction * inverseDirectionStep;
                    else
                        currentStep += ((fVal > 0) ? 1 : -1) * direction;
                    
                    if (currentStep >= lineHolder.childCount)
                        currentStep = lineHolder.childCount - 1;
                    else if (currentStep < 0)
                            currentStep = 0;

                    Set();

                    if (alwaysActive)
                        KeyEnter();
                }
            }

            AlternateUpdate();
        }

        else if (targeter != null && targeter.gameObject.activeSelf)
            targeter.gameObject.SetActive(false);
	}

    public void Set()
    {
        targetPointer = lineHolder.GetChild(currentStep).gameObject;
    }
}
