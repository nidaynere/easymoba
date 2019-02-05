/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AgentInput : MonoBehaviour
{
    public MobileAgent myAgent;

    public static AgentInput current;

    void Start()
    {
        current = this;
        myAgent = GetComponent<MobileAgent>();
    }

    public static bool axis;

    bool _autoTarget = false;
    public bool autoTargeter
    {
        get
        {
            return _autoTarget;
        }

        set
        {
            _autoTarget = value;
            autoTarget = null;
            AutoTarget.singleton.icon.Open(value);
            nextTarget = 0;
        }
    }

    public float skillDistance;
    public MobileAgent autoTarget;
    float nextTarget;
    float nextAuto;

    UISkillItem targetSkill;

    void Update()
    {
        if (myAgent.isController && !GameManager.SessionEnd && GameManager.sessionStarted)
        { // only if this is the controller one
            TryToMove();
        }

        if (autoTarget != null)
        {
            AutoTarget.singleton.icon.transform.position = Camera.main.WorldToScreenPoint(autoTarget.transform.position);
        }

        if (nextAuto > Time.time)
            return;

        nextAuto = Time.time + 0.1f;

        if (autoTargeter)
        {
            /*
             * MOVE TO TARGET
             * */

            if (autoTarget != null && (MapLoader.isBlocked (GameManager.currentMapId, transform.position, autoTarget.transform.position, false) || autoTarget.currentHealth == 0))
            {
                autoTarget = null;
                nextTarget = 0;
            }

            if (nextTarget < Time.time && autoTarget == null)
            {
                nextTarget = Time.time + 2; // Search for target per 4 seconds

                List<MobileAgent> possibleTargets = MobileAgent.list.FindAll(x => (x.team != myAgent.team || GameManager.singleton.sessionUpdate.teamSize == 0) && x.currentHealth > 0 && !MapLoader.isBlocked(GameManager.currentMapId, myAgent.transform.position, x.transform.position, false));
                possibleTargets = possibleTargets.OrderBy(x => GameManager.GetDistance(x.transform.position, myAgent.transform.position)).ToList();

                if (possibleTargets.Count > 0)
                {
                    autoTarget = possibleTargets[0];
                    targetSkill = GameManager.singleton.skillsHolder.GetChild(0).GetComponent<UISkillItem>(); // First skill
                    skillDistance = targetSkill.skillInfo.life * targetSkill.skillInfo.moveSpeed; // First skill's distance
                }
                else
                {
                    autoTargeter = false; // Disable auto targeting
                    return;
                }
            }

            /*
             * GOTO TARGET
             * */
            if (autoTarget != null)
            {
                float dist = GameManager.GetDistance(myAgent.transform.position, autoTarget.transform.position);
                if (dist < skillDistance / 1.25f)
                {
                    if (GameManager.singleton.skillsHolder.GetChild(0).GetComponentInChildren<Filler>()._image.fillAmount == 0)
                    {
                        if (myAgent.isCasting)
                        AimRequest(autoTarget.transform.position);
                        targetSkill.Request();
                    }
                    else
                    {
                        MoveRequest(transform.position, true);
                    } 
                }
                else if (dist > skillDistance)
                {
                    MoveRequest(autoTarget.transform.position);
                }
            }
        }
    }

    void LastAim()
    {
        if (autoTarget != null)
            AimRequest(autoTarget.transform.position);
    }

    public void TryToMove()
    {
        if (myAgent.cannotMove > Time.time)
            return;

        Vector3 direction = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        if (direction.magnitude > 0)
        {
            axis = true;
            if (KeyController.current.currentController != 1 && KeyController.current.currentController != 2) // Gamepad and XBOX Controller
            {
                KeyController.current.savedController = KeyController.current.currentController;
                KeyController.current.currentController = 1;
            }
        }
        else if (KeyController.current.savedController != -1)
        {
            KeyController.current.currentController = KeyController.current.savedController;
            KeyController.current.savedController = -1;
        }

        switch (KeyController.current.currentController)
        {
            case 0:
                if (Input.GetMouseButton(0))
                {
                    if (autoTargeter)
                        autoTargeter = false;
                }

                if (Input.GetMouseButton(0) || myAgent.isCasting)
                {
                    if (autoTargeter && myAgent.isCasting)
                    {
                        return;
                    }

                    axis = false;
                    var v3 = Input.mousePosition;
                    v3.z = 10.0f;
                    v3 = Camera.main.ScreenToWorldPoint(v3);

                    RaycastHit h = new RaycastHit();
                    if (Physics.Raycast(Camera.main.transform.position, v3 - Camera.main.transform.position, out h, 1000, MapLoader.layerMask))
                    {
                        if (myAgent.isCasting)
                        {
                            AimRequest(h.point);

                            return; // No simulation here
                        }

                        MoveRequest(h.point);
                    }
                }
                break;

            case 1:
            case 2: // Analog stick

                if (GameManager.singleton.panel_levelTooltip.activeSelf)
                    return; // This is a known keyline. Keylines uses also Horizontal and Vertical axis

                if (direction.magnitude > 0)
                {
                    if (autoTargeter)
                        autoTargeter = false;

                    Vector3 hPoint = transform.position;

                    if (!myAgent.isCasting || !myAgent.castingItem.areaFollower)
                    {
                        hPoint = transform.position + direction.normalized * 8;
                    }

                    else
                    {
                        // this is area spell
                        myAgent.castingItem.castingPoint += direction * Time.deltaTime * 10;
                        hPoint = transform.position + myAgent.castingItem.castingPoint;
                    }

                    if (myAgent.isCasting)
                    {
                        AimRequest(hPoint);

                        return; // No simulation here
                    }

                    MoveRequest(hPoint);
                }
                else if (myAgent.pointed && !autoTarget)
                {
                    MoveRequest(myAgent.transform.position);
                }

                break;

            case 3: // Touch stick
                if (GameManager.singleton.panel_levelTooltip.activeSelf)
                    return; // This is a known keyline. Keylines uses also Horizontal and Vertical axis

                if (TouchStick.touched)
                {
                    if (autoTargeter)
                        autoTargeter = false;
                }

                if (!TouchStick.touched)
                {
                    if (myAgent.pointed)
                        MoveRequest(myAgent.transform.position);
                    return;
                }
                Vector3 tPoint = new Vector3(TouchStick.dir.x, 0, TouchStick.dir.y).normalized;

                if (!myAgent.isCasting || !myAgent.castingItem.areaFollower)
                {
                    tPoint = transform.position + tPoint * 4;
                }

                else
                {
                    // this is area spell
                    myAgent.castingItem.castingPoint += tPoint * Time.deltaTime * 10;
                    tPoint = transform.position + myAgent.castingItem.castingPoint;
                }

                if (myAgent.isCasting)
                {
                    AimRequest(tPoint);

                    return; // No simulation here
                }

                MoveRequest(tPoint);

                break;
        }
    }

    void AimRequest(Vector3 pos, bool simulate = true)
    {
        if (myAgent.nextAimRequest < Time.time && myAgent.nextAimRequest != -1)
        {
            MObjects.AimRequest mObject = new MObjects.AimRequest();
            mObject.y = pos;
            mObject.pos = transform.position;
            GameManager.nc.Send(MTypes.AimRequest, mObject);

            if (simulate)
                myAgent.StartMove(pos);

            myAgent.nextAimRequest = Time.time + 0.06f;
        }
    }

    void MoveRequest(Vector3 pos, bool simulate = true)
    {
        if (myAgent.nextMoveRequest < Time.time)
        {
            myAgent.StartMove(pos);
            MObjects.MoveRequest mObject = new MObjects.MoveRequest();
            mObject.value = pos;
            GameManager.nc.Send(MTypes.MoveRequest, mObject);
            myAgent.nextMoveRequest = Time.time + 0.06f;

            if (simulate)
                myAgent.StartMove(pos);
        }
    }
}
