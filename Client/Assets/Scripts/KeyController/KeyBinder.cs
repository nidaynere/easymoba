/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class KeyBinder : MonoBehaviour
{
    public static List<KeyBinder> list = new List<KeyBinder>();

    [HideInInspector]
    public KeyCode keyCode;

    public int targetKey; // target key no in KeyController

    public GameObject targetPointer;

    public bool autoInit;

    bool inited = false;

    public bool alwaysInit = false;

    private void OnEnable()
    {
        if (!list.Contains (this))
        list.Add(this);

        if (autoInit && !inited)
        {
            inited = true;
            Init();
        }
    }

    void OnDestroy()
    {
        list.Remove(this);
    }

    Transform kk;

    public Vector3 keyCodeOffset;

    public void Init(int tKey = -1) // init the key binder, can be called more than once
    {
        if (KeyController.current.currentController == 3)
            return; // MobileCheck

        if (!alwaysInit && KeyController.current.currentController == 0)
            return; // StandaloneCheck
             
        if (tKey != -1)
            targetKey = tKey;

        if (kk != null)
            Destroy(kk.gameObject);

        KeyMap km = KeyController.current.keyMaps[KeyController.current.currentController];

        kk = Instantiate (GameManager.singleton.keyCode, transform);
        kk.localPosition = keyCodeOffset;
        kk.gameObject.SetActive(true);
        keyCode = km.keys[targetKey].key;
        Text kt = kk.GetComponentInChildren<Text>();
        kt.text = km.keys[targetKey].keyName.ToString();
        kt.color = km.keys[targetKey].keyColor;
        Image ki = kk.GetComponent<Image>();
        ki.color = km.keys[targetKey].color;
        ki.sprite = km.keys[targetKey].sprite;
    }

    bool pressed;
    float pressTime;

    public bool targetPointerHasButton = false;
    void Update()
    {
        if (Input.GetKeyUp(keyCode))
        {
            KeyExit();

            pressTime = 0;
            pressed = false;
        }

        if (Input.GetKeyDown(keyCode))
        {
            KeyClick();
            pressed = true;
            pressTime = Time.time + 0.2f;
        }

        if (Input.GetKey(keyCode) && pressed && pressTime < Time.time)
        {
            KeyEnter();
        }
    }

    public void AlternateUpdate()
    {
        Update();
    }

    public void KeyClick()
    {
        if (targetPointer == null)
            return;

        if (!targetPointerHasButton)
            targetPointer.SendMessage("KeyClicked");
        else targetPointer.GetComponent<Button>().OnPointerClick(new PointerEventData(EventSystem.current));
    }

    public void KeyEnter()
    {
        if (targetPointer == null)
            return;

        if (!targetPointerHasButton)
            targetPointer.SendMessage("KeyEntered");
        else targetPointer.GetComponent<Button>().OnPointerEnter(new PointerEventData(EventSystem.current));
    }

    public void KeyExit()
    {
        if (targetPointer == null)
            return;

        if (!targetPointerHasButton)
            targetPointer.SendMessage("KeyExited");
        else targetPointer.GetComponent<Button>().OnPointerExit(new PointerEventData(EventSystem.current));
    }
}
