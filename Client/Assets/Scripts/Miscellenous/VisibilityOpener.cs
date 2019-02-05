/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using UnityEngine;
using UnityEngine.EventSystems;

public class VisibilityOpener : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public UIVisibility tVisibility;
    public void OnPointerEnter(PointerEventData data)
    {
        tVisibility.Open(true);
    }

    public void OnPointerExit(PointerEventData data)
    {
        tVisibility.Open(false);
    }

    void KeyEntered()
    {
        OnPointerEnter(new PointerEventData(EventSystem.current));
    }

    void KeyExited()
    {
        OnPointerExit(new PointerEventData(EventSystem.current));
    }

    void KeyClicked()
    {
        OnPointerEnter(new PointerEventData(EventSystem.current));
    }
}
