/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using UnityEngine;
using UnityEngine.Networking;  

public class MTypes
{
	public static short AgentStop = 2001;
	public static short AgentMove = 2003;
	public static short MoveRequest = 2004;
	public static short HeroChangeRequest = 2005;
	public static short MapChangeRequest = 2006;
	public static short SessionUpdate = 2007;
	public static short FindGameRequest = 2008;
	public static short AgentInfo = 2009;
    public static short SyncPosition = 2011;
	public static short AgentDestroy = 2012;
	public static short SkillRequest = 2013;
	public static short StartSkill = 2014;
	public static short EndSkill = 2015;
	public static short SkillSpawn = 2016;
	public static short SkillDestroy = 2017;
	public static short AimRequest = 2018;
	public static short AgentHealth = 2019;
	public static short AgentAim = 2020;
	public static short LastAim = 2021;
	public static short SkillEffect = 2022;
	public static short BotRequest = 2023;
    public static short HeroInfo = 2024;
    public static short Cooldown = 2025;
	public static short KillInfo = 2026;
	public static short ScoreInfo = 2027;
    public static short SessionEnd = 2028;
    public static short RoundComplete = 2029;
    public static short SkillInfo = 2030;
    public static short LevelInfo = 2031;
    public static short AgentLevel = 2032;
    public static short MapInfo = 2033;
    public static short AgentBuff = 2034;
    public static short Teleport = 2035;
    public static short Hook = 2036;
}

public class MObjects
{
    public class Hook : MessageBase
    {
        public int to;
        public int from;
    }

    public class Teleport : MessageBase
    {
        public int id;
        public Vector3 pos;
    }

    public class AgentBuff : MessageBase
    {
        public int id;
        public Buffing.Buff[] buffs;
        public short[] modified;
    }

    public class MapInfo : MessageBase
    {
        public int[] langId;
        public int[] players;
    }

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

    public class LevelInfo : MessageBase
    {
        public Level[] levels;
    }

    public enum SkillType
    {
        Mover,
        Area,
        Self
    }

    public enum EffectType
    {
        Damage, // 0
        Heal,   // 1
        Poison, // 2
        Stun,   // 3
        DamageShield, // 4
        DamagerObject, // 5
        Speed, // 6
        Teleport // 7
    }

    [System.Serializable]
    public class Skill
    {
        public string clientPrefab; // client prefab name and will be used Id
        public float moveSpeed; // moving speed
        public float collision; // collision size will be used by Physik. Physik is the custom light weight physics system for the server.
        public int effect; // effect of the skill
        public ushort effectTime; // if it's a buff, this is buff's time. 
        public EffectType effectType;
        public float casttime; // casting time of the skill
        public float cooldown; // cooldown time of the skill
        public float life; // how many seconds this skill will be alive
        public bool hitAndDestroy; // it is one time hitter?
        public bool hitContinous; // keeps hit in seconds
        public SkillType skillType; // 0 =mover, 1= area
        public int botsDecideChance; // rate when bots decide
        public float activeAfter; // This skill will be activated after x seconds. Default is 0 means instant active.
    }

    public class SkillInfo : MessageBase
    {
        public Skill[] skills;
    }

    public class RoundComplete : MessageBase
    {

    }

    public class SessionEnd : MessageBase
    {

    }

	public class ScoreInfo : MessageBase
	{
		public int [] ids;
		public ushort [] teams;
		public ushort [] kills;
		public ushort [] deaths;
	}
	
	public class KillInfo : MessageBase
	{
		public int id;
		public int tId;
		public string clientPrefab;
	}

    public class Cooldown : MessageBase
    {
        public ushort skillId;
        public float time;
    }

    public class HeroInfo : MessageBase
    {
        public string[] clientPrefab;
        public bool[] status; // its locked?
    }

    public class BotRequest : MessageBase
	{

	}

	public class SkillEffect : MessageBase
	{
		public int id;
		public string clientPrefab;
	}

	public class LastAim : MessageBase
	{
		public Vector3 y;
        public Vector3 pos; // agent pos
    }

	public class AgentAim : MessageBase
	{
		public int id;
		public Vector3 y;
	}

	public class AgentHealth : MessageBase
	{
		public int id;
        public ushort hp;
        public ushort maxhp;
    }

	public class AimRequest : MessageBase
	{
		public Vector3 y;
        public Vector3 pos; // agent pos
	}

	public class SkillDestroy : MessageBase
	{
		public string id;

	}

	public class SkillSpawn : MessageBase
	{
		public string id; // skill id
		public string clientPrefab; // skill prefab
		public float speed;
		public Vector3 position;
		public float rotation;
        public int casterId;
    }

	public class EndSkill : MessageBase
	{
		public int id;
	}

	public class StartSkill : MessageBase
	{
		public int id;
		public ushort skillId;
		public float casttime;
		public ushort skillType;
		public float skillSize;
	}

	public class SkillRequest : MessageBase
	{
		public ushort skillId;
	}

	public class AgentDestroy : MessageBase
	{
		public int id;
	}

	public class SyncPosition : MessageBase
	{
		public int id;
		public Vector3 pos;
	}

    public class AgentLevel : MessageBase
    {
        public int id;
        public ushort level;
        public ushort exp;
        public ushort requiredExp;
    }

	public class AgentInfo : MessageBase
	{
		public string alias;
		public string clientPrefab;
		public string[] skills;
		public float moveSpeed;
		public int id;
		public bool isController = false;
        public ushort team;
    }

    public class AgentStop : MessageBase
    {
        public int id;
        public bool includeClient;
    }

	public class AgentMove : MessageBase
	{
		public int id;
		public Vector3 value;
	}

	public class MoveRequest : MessageBase
	{
		public Vector3 value;
	}

	public class SessionUpdate : MessageBase
	{
		public ushort seconds; // if started round seconds, if not started start seconds
		public bool isStarted;
        public bool isKilled;
		public string mapId;
		public bool canAddBots;
        public ushort round;
        public ushort maxRound;
		public bool isStarting;
        public ushort teamSize;
	}

	public class HeroChangeRequest : MessageBase
	{
		public ushort val;
	}

	public class MapChangeRequest : MessageBase
	{
		public ushort value;
	}

	public class FindGameRequest : MessageBase
	{
		public string alias;
        public ushort mapId;
	}
}