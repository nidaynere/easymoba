/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using UnityEngine;
using UnityEngine.EventSystems;

public class HeroButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public bool selected = false;

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

    void KeyClicked ()
    {
        if (!selected)
            GameManager.singleton.HeroChange(transform.GetSiblingIndex());
        else Invoke ("CloseThePanel", Time.deltaTime); // Wait for a frame
    }

    void CloseThePanel()
    {
        GameManager.singleton.heroGrid.parent.gameObject.SetActive(!GameManager.singleton.heroGrid.parent.gameObject.activeSelf);
    }

    private void OnDisable()
    {
        KeyExited();
    }

    void KeyEntered()
    {
        transform.Find("pointer").gameObject.SetActive(true);
    }

    void KeyExited()
    {
        transform.Find("pointer").gameObject.SetActive(false);
    }
}
