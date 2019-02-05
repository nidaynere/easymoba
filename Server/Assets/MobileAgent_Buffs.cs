/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 24 December 2017
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class Buffing
{ // Buffs and debuffs

    public enum buffTypes
    {
        Stun,
        DamageShield,
        DamagerObject,
        BonusExperience,
        Poison,
        Healer,
        Speed
    };

    /*
    * BUFF TYPES;
    * 0=Stun // Decreases the move speed by percent.
    * 1=DamageShield // Absorbs the received damage by percent
    * 2=Damager object around user // Damages the closest agents around (closer than 2 meters) per second by %20 of your first skill. Agent's level affects this.
    * 3=Bonus Experience // Multiplies the received experience by percent.
    * 4=Poison // Damages you by the effect in per second.
    * 5=Healer object around user // Heals the closest agents around (closer than 4 meters) per second by %20 of your first skill. Agent's level affects this.
    * 6=Speed // Increases the movement speed.
    * * */

    [System.Serializable]
    public class Buff
    {
        public buffTypes buffType;

        public short buffEffect;

        public short GetBuffEffect(MobileAgent agent)
        {
            switch (buffType)
            {
                case buffTypes.DamagerObject: case buffTypes.DamageShield:
                    return (short)Mathf.Round(agent.skills[0].GetEffect(agent) * buffEffect / 100f);

                default:
                    return buffEffect;
            }
        }

        public ushort buffTime;
    }

    public List<Buff> buffs = new List<Buff>();
}

public class MobileAgent_Buffs : MonoBehaviour
{
    public MobileAgent myAgent;
    public Buffing buff = new Buffing();

    public float nextUpdate;

    MObjects.AgentBuff mObject = new MObjects.AgentBuff();

    private void Start()
    {
        mObject.id = (myAgent.user == null) ? myAgent.customId : myAgent.user.connectionId;
    }

    private void Update()
    {
        // Buff controller
        if (nextUpdate > Time.time)
            return;

        nextUpdate = Time.time + 1;

        int bCount = buff.buffs.Count;

        List<Buffing.Buff> willRemove = new List<Buffing.Buff>();

        for (int i = 0; i < bCount; i++)
        {
            Buffing.Buff b = buff.buffs[i];

            if (b.buffTime == 1)
                willRemove.Add(b);
            else if (b.buffTime != 0) // 0 means permanent buff
                b.buffTime--;

            if (myAgent.isDead)
                continue;
			
            List<MobileAgent> targetAgents = new List<MobileAgent>();
            int fCount = 0;
            switch (b.buffType)
            {
                case Buffing.buffTypes.DamagerObject: // Damager object
                    targetAgents = myAgent.session.agents.FindAll(x => (x != myAgent && (x.team != myAgent.team || myAgent.session.teamsize == 0)) && !x.isDead && PhysikEngine.GetDistance (x.transform.position, myAgent.transform.position) <= 4);

                    fCount = targetAgents.Count;
                    for (int f = 0; f < fCount; f++)
                    {
                        targetAgents[f].lastHitter = (myAgent.user == null) ? myAgent.customId : myAgent.user.connectionId;
                        targetAgents[f].health -= b.GetBuffEffect (myAgent);
                    }
                break;

                case Buffing.buffTypes.Healer: // Healer objects
                    targetAgents = myAgent.session.agents.FindAll(x => (x == myAgent || (x.team == myAgent.team && myAgent.session.teamsize != 0)) && !x.isDead && (x == myAgent || PhysikEngine.GetDistance(x.transform.position, myAgent.transform.position) <= 8));

                    fCount = targetAgents.Count;
                    for (int f = 0; f < fCount; f++)
                    {
                        targetAgents[f].health += b.GetBuffEffect(myAgent);
                    }
                 break;

                 case Buffing.buffTypes.Poison: // Self poison
                    myAgent.health -= b.buffEffect;
                    break;
            }
        }

        int wCount = willRemove.Count;
        for (int i = 0; i < wCount; i++)
        {
            buff.buffs.Remove (willRemove[i]);
        }

        if (wCount > 0)
            myAgent.AgentInfo();

        /*
         * UPDATE BUFF
         * */

        mObject.buffs = buff.buffs.ToArray();

        wCount = mObject.buffs.Length;
        mObject.modified = new short[wCount];

        for (int i = 0; i < wCount; i++)
        {
            mObject.modified [i] = mObject.buffs[i].GetBuffEffect(myAgent);
        }

        /*
         * THIS WILL SEND mObject to ALL CLIENTS
         * */

        wCount = myAgent.session.users.Count;
        for (ushort i = 0; i < wCount; i++)
        {
            if (NetworkServer.connections.Contains(myAgent.session.users[i]))
            {
                NetworkServer.SendToClient(myAgent.session.users[i].connectionId, MTypes.AgentBuff, mObject);
            }
        }
    }
}
