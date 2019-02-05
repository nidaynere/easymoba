/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using UnityEngine;
using UnityEngine.UI;

public class UICastingItem : MonoBehaviour
{
    public Image filler;
    public Image ico;
    public MobileAgent mobileAgent;
    public UIVisibility visibility;

    bool _areaFollower;
    public bool areaFollower
    {
        get

        {
            return _areaFollower;
        }

        set
        {
            _areaFollower = value;
            GameManager.singleton.areaFollower.gameObject.SetActive (value);
            if (!value)
                GameManager.singleton.areaFollower.transform.position = Vector3.up * -10000;
        }
    }

    public Vector3 castingPoint; // only for gamepads & touch input
    int skillId;
    public void StartCast(float castingTime, string clientPrefab, ushort _skillId)
    {
        skillId = _skillId;
        // load spell ico
        try
        {
            ico.sprite = GameManager.singleton.icons.Find(x => x.name == clientPrefab);
        }
        catch (System.Exception e) { Debug.Log(e.ToString() + " " + clientPrefab); } //

        castingPoint = mobileAgent.transform.forward * 2;
        visibility.Open();
        filler.fillAmount = 0;
        fillSpeed = castingTime + 0.01f;
        mobileAgent.isCasting = true;
    }

    float fillSpeed;

    bool aimIsDone = false;
    void FixedUpdate()
    {
        filler.fillAmount += Time.deltaTime / fillSpeed;

        if (mobileAgent == null || !mobileAgent.isController)
            return;

        if (fillSpeed > 0f && filler.fillAmount >= 1 - (Time.deltaTime / fillSpeed) * 8)
        {
            if (!aimIsDone && mobileAgent.isController)
            {
                aimIsDone = true;

                AgentInput.current.TryToMove();

                MObjects.LastAim mObject = new MObjects.LastAim();
                mObject.y = (AgentInput.current.autoTarget != null && GameManager.singleton.skillsHolder.GetChild (skillId).GetComponent<UISkillItem>().skillInfo.skillType == MObjects.SkillType.Mover) ? AgentInput.current.autoTarget.transform.position : mobileAgent.aimPoint;
                mObject.pos = mobileAgent.transform.position;
                GameManager.nc.Send(MTypes.LastAim, mObject);

                mobileAgent.nextAimRequest = -1;

                // agent cannot aim now.
            }

            mobileAgent.nextMove = Time.time + 0.5f;
        }

        if (areaFollower)
        {
            if (mobileAgent.targetPoint != Vector3.up)
            { // Area Spell
                if (GameManager.GetDistance(mobileAgent.targetPoint, mobileAgent.transform.position) > 10)
                {
                    mobileAgent.targetPoint = mobileAgent.transform.position + (mobileAgent.targetPoint - mobileAgent.transform.position).normalized * 10;
                }

                GameManager.singleton.areaFollower.transform.position = mobileAgent.targetPoint;
            }
        }
	}

	public void FinishCast ()
	{
        fillSpeed = 0f;

        if (mobileAgent.isController)
        {
            aimIsDone = false;
            areaFollower = false;
        }

        mobileAgent.nextMove = Time.time + 0.5f;
        mobileAgent.nextAimRequest = Time.time + 0.5f;
        mobileAgent.cannotMove = Time.time + 0.5f;
        mobileAgent.isCasting = false;
        filler.fillAmount = 0;
        visibility.Open(false);
    }
}
