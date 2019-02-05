/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using UnityEngine;

[System.Serializable]
public class Key
{
    public KeyCode key;
    public Color color;
    public Color keyColor;
    public Sprite sprite;
    public string keyName;
}

[System.Serializable]
public class KeyMap
{
    public string name; // Unused
    public Key[] keys;
}

public class KeyController : MonoBehaviour
{
    public static KeyController current;

    public KeyMap[] keyMaps;

    [HideInInspector]
    public int savedController = -1;

    [HideInInspector]
    public int currentController; // 0 Keyboard 1 GamePad 2 OtherGamePads 3 Touch

    public int[] clientSkillKeys; // key ids in the key controller. Do not forget to increase this in editor after you added more than 3 skills for heroes. It's unlimited but joysticks and ui is limited :)

    private void Awake()
    {
        current = this;
        UpdateController();
    }

    void UpdateController()
    {
        bool inMobile = Application.isMobilePlatform;
        string[] js = Input.GetJoystickNames();
        bool connectedJS = (js.Length > 0 && !string.IsNullOrEmpty(js[0]));

        if (!connectedJS && !inMobile)
        {
            currentController = 0; // Keyboard
        }
        else if (inMobile && !connectedJS)
        {
            currentController = 3; // Touch
        }
        else if (connectedJS) // There is joystick
        {
            currentController = (js[0].Contains("Logitech")) ? 1 : 2;
        }
        else
        {
            currentController = 0; // Keyboard;
        }

        Debug.Log("Current controller is " + currentController);
    }
}
