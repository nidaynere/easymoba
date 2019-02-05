/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 24 December 2017
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Leveling
{
    [System.Serializable]
    public class Level
    {
        public ushort level;
        /*
         * LEVELING
         * */

        public float Percent_health; // increases the health
        public float Percent_effect; // increases the skill effects
        public float Percent_fastercast; // faster casting
        public float Percent_movespeed; // faster move
        public float Percent_cooldown; // reduced cooldowns
    }
}

public class MobileAgent_Leveling : MonoBehaviour
{
    public MobileAgent myAgent;

    /*
     * LEVEL SYSTEM
     * */
    public Leveling.Level level;
    public ushort exp;
    public ushort requiredExp;

    public void getExp(ushort val)
    {
        val = (ushort)Mathf.RoundToInt(val * MapLoader.maps[myAgent.session.map].experienceModifier);
        exp += val;

        int lVal = exp - requiredExp;
        if (lVal >= 0)
        {
            /*
             * LEVEL UP
             * */
            exp = (ushort)lVal;
            level.level++;
            requiredExp = (ushort)(myAgent._hero.experience * Mathf.Pow(level.level, 2));

            /*
             * SEARCH MY HERO FOR MY LEVEL ADDITIONS
             * */

            Leveling.Level lvl = myAgent._hero.levels.Find(x => x.level == level.level);
            if (lvl != null)
            {
                level.Percent_health += lvl.Percent_health;
                level.Percent_cooldown += lvl.Percent_cooldown;
                level.Percent_effect += lvl.Percent_effect;
                level.Percent_fastercast += lvl.Percent_fastercast;
                level.Percent_movespeed += lvl.Percent_movespeed;

                myAgent.maxHealth += Mathf.RoundToInt(myAgent.maxHealth * lvl.Percent_health / 100f);
                myAgent._moveSpeed += myAgent._moveSpeed * lvl.Percent_movespeed / 100f;
            }
        }

        LevelInfo();
    }

    public void LevelInfo()
    {
        MObjects.AgentLevel mObject = new MObjects.AgentLevel();
        mObject.id = (myAgent.user != null) ? myAgent.user.connectionId : myAgent.customId;
        mObject.level = level.level;
        mObject.exp = exp;
        mObject.requiredExp = requiredExp;
        ushort _c = (ushort)myAgent.session.users.Count;
        for (ushort i = 0; i < _c; i++)
        {
            if (NetworkServer.connections.Contains(myAgent.session.users[i]))
            {
                NetworkServer.SendToClient(myAgent.session.users[i].connectionId, MTypes.AgentLevel, mObject);
            }
        }
    }

    public void UpdateLevel()
    {
        if (myAgent.user == null)
            return; // only for players

        MObjects.LevelInfo mObject = new MObjects.LevelInfo();
        mObject.levels = myAgent._hero.levels.ToArray();
        NetworkServer.SendToClient(myAgent.user.connectionId, MTypes.LevelInfo, mObject);
    }
}
