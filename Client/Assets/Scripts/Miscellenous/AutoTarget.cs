using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AutoTarget : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public static AutoTarget singleton;

    public UIVisibility icon;

    private void Start()
    {
        singleton = this;
    }

    public void OnPointerEnter(PointerEventData data)
    {
        KeyEntered();
    }

    public void OnPointerExit(PointerEventData data)
    {
        KeyExited();
    }

    public void OnPointerClick(PointerEventData data)
    {
        KeyClicked();
    }

    public void KeyEntered()
    {
    }

    public void KeyExited()
    {
    }

    public void KeyClicked()
    {
        AgentInput.current.autoTargeter = true;
    }
}
