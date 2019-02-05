/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using UnityEngine;
using System.Text;
using UnityEngine.EventSystems;

public class UISkillItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public UIVisibility hover;

    public StringBuilder core; // core text
    public StringBuilder pro; // skill info

    public MObjects.Skill skillInfo;

    public void OnPointerEnter(PointerEventData data)
    {
        KeyEntered();
    }

    public void OnPointerExit(PointerEventData data)
    {
        KeyExited();
    }

    public void OnPointerClick (PointerEventData data)
    {
        KeyClicked();
    }

    public void KeyEntered()
    {
        if (core == null || pro == null)
            return;
        Vector3 v1 = GameManager.singleton.panel_skillTooltip.transform.position;
        GameManager.singleton.panel_skillTooltip.transform.position = new Vector3(v1.x, transform.position.y, v1.z);

        GameManager.singleton.skillTooltip_CoreText.text = core.ToString();
        GameManager.singleton.skillTooltip_ProText.text = pro.ToString();
        GameManager.singleton.panel_skillTooltip.Open();
        hover.Open();
    }

    public void KeyExited()
    {
        hover.Close();
        GameManager.singleton.panel_skillTooltip.Open(false);
    }

    public void KeyClicked()
    {
        if (GameManager.sessionStarted)
        {
            Request();
        }
        else KeyEntered();
    }

    float nextRequest = 0;
    public void Request()
    {
        if (nextRequest > Time.time || GameManager.singleton.sessionUpdate.isKilled)
            return;

        nextRequest = Time.time + 0.1f;

        MObjects.SkillRequest mObject = new MObjects.SkillRequest();
        mObject.skillId = ushort.Parse(gameObject.name);
        GameManager.nc.Send(MTypes.SkillRequest, mObject);
    }
}
