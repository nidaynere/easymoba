/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using UnityEngine;
using System.Text;
using UnityEngine.EventSystems;

public class UILevelItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public MObjects.Level myLevelInfo;

    public UIVisibility hover;

    public StringBuilder core; // core text
    public StringBuilder pro; // skill info

    public void OnPointerEnter(PointerEventData data)
    {
        Vector3 v1 = GameManager.singleton.panel_levelTooltip.transform.position;
        GameManager.singleton.panel_levelTooltip.transform.position = new Vector3(v1.x, transform.position.y, v1.z);

        GameManager.singleton.levelTooltip_CoreText.text = core.ToString();
        GameManager.singleton.levelTooltip_ProText.text = pro.ToString();
        GameManager.singleton.panel_levelTooltip.Open();
        hover.Open();
    }

    public void OnPointerExit(PointerEventData data)
    {
        hover.Close();
        GameManager.singleton.panel_levelTooltip.Open(false);
    }

    void KeyEntered()
    {
        OnPointerEnter(null);
    }

    void KeyExited()
    {
        OnPointerExit(null);
    }

    void KeyClicked()
    {
        OnPointerEnter(null);
    }
}
