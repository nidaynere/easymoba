/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 24 December 2017
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Callipso // Welcome to Callipso!
{
    /*
     * HERE IS THE CLASSES WILL BE LOADED AND USED BY THE SERVER
     * */

    [System.Serializable]
    public class ServerSettings
    {
        public ushort port; // It is the server port.
        public ushort spamprotectionsize; // spam protection size, don'T make it too much lower like 5
    }

    [System.Serializable]
    public class Skill
    {
        public string clientPrefab; // client prefab name and will be used Id
        public float moveSpeed; // moving speed
        public float collision; // collision size will be used by Physik. Physik is the custom light weight physics system for the server.
        public int effect;
        public ushort effectTime;
        public EffectType effectType;
        public float casttime; // casting time of the skill
        public float cooldown; // cooldown time of the skill
        public float life; // how many seconds this skill will be alive
        public bool hitAndDestroy; // it is one time hitter?
        public bool hitContinous; // keeps hit in seconds
        public SkillType skillType; // 0 =mover, 1= area
        public int botsDecideChance; // rate when bots decide
        public float activeAfter; // This skill will be activated after x seconds. Default is 0 means instant active.

        public int GetEffect (MobileAgent agent)
        {
            return effect + Mathf.RoundToInt(effect * agent.agentLevel.level.Percent_effect / 100f);
        }
    }

	public enum HeroType
	{
		Player,
		Creature
	}

    [System.Serializable]
    public class Hero // hero data
    {
        public string clientPrefab; // client prefab name and will be used Id
        public int health;
        public ushort experience = 10; // default 10

        public HeroType heroType;
        public float moveSpeed; // moving speed
        public float collision; // collision size will be used by Physik. Physik is the custom light weight physics system for the server.
        public Skill[] skills;
        public List<Leveling.Level> levels = new List<Leveling.Level>(); // agent levels
        public List<Buffing.Buff> defaultBuffs = new List<Buffing.Buff>();

        public float vision; // used for creatures only
        public string alias; // used for creatures only
    }

    [System.Serializable]
    public class GameSession // running game session
    {
        public ushort[] spawnPointRequester;

        public ushort map;
        public ushort round = 1; // round id

        public List<GameObject> createdSkills = new List<GameObject>();

        public float[] creatureSpawns;
        public List<MobileAgent> creatures = new List<MobileAgent>();

        public void RoundComplete()
        {
            MObjects.RoundComplete mObject = new MObjects.RoundComplete();

            ushort r = (ushort) users.Count;

            for (ushort b = 0; b < r; b++)
            {
                if (NetworkServer.connections.Contains(users[b]))
                {
                    NetworkServer.SendToClient(users[b].connectionId, MTypes.RoundComplete, mObject);
                }
            }

            time = Time.time + 5; // Round will be done in 5 seconds
        }

		public void CreateScoreData ()
		{
			List<MobileAgent> playersList = agents.FindAll (x => x.heroType == Callipso.HeroType.Player);
			int players = playersList.Count;
			score_ids = new int[players];
			score_teams = new ushort[players];
			score_kills = new ushort[players];
			score_deaths = new ushort[players];

			for (int i = 0; i < players; i++) 
			{
				score_ids [i] = ((playersList[i].user == null) ? playersList [i].customId : playersList[i].user.connectionId);
				score_teams [i] = playersList [i].team;
			}
		}

		public int [] score_ids;
		public ushort [] score_teams;
		public ushort [] score_kills;
		public ushort [] score_deaths;

        public ushort teamsize; // team size of the session
        public ushort id; // 0-65536
        public bool isStarted = false;
        public List<MobileAgent> agents = new List<MobileAgent>();
		public List<NetworkConnection> users = new List<NetworkConnection>();
        public float time; // its a time. time value for starting game or ending;
        public bool canAddBots;

		public ushort agentCreated;

		public void BotRequest (int connectionId)
		{
			MobileAgent _ma = agents.Find(x => ((x.user != null) ? x.user.connectionId : x.customId) == connectionId);

			if (_ma != null && agents[0] == _ma && !isStarted && canAddBots && agents.Count < MapLoader.maps[map].minPlayers && agents.FindAll(x => x.heroType == HeroType.Player).Count < MapLoader.maps[_ma.session.map].maxPlayers && !killed)
			{
				AISpawner.SpawnPlayerBot (ServerManager.playerHeroes [Random.Range(0,ServerManager.playerHeroes.Count)].clientPrefab,this);
			}
		}

        public void HeroChange(int connectionId, ushort val)
        {
			MobileAgent _ma = agents.Find(x => ((x.user != null) ? x.user.connectionId : x.customId) == connectionId);

            if (_ma != null)
            {
                if (val < 0 || val >= ServerManager.playerHeroes.Count || isStarted)
                    return;

                /*
                 * IF YOU WANT TO MAKE SOME 'UNLOCK HEROES' THING. THIS IS THE PLACE
                 * */

				_ma.LoadHero(ServerManager.playerHeroes[val].clientPrefab, true);

                Update(true);

                _ma.agentLevel.UpdateLevel();
            }
        }

        public ushort KickPlayer(int connectionId)
        {
            Debug.Log("Player is removing from the session");
			MobileAgent _ma = agents.Find(x => ((x.user != null) ? x.user.connectionId : x.customId) == connectionId);

            ushort savedSpam = _ma.spammer;

            Object.Destroy(_ma.gameObject);
            agents.Remove(_ma);

            if (agents.FindAll (x=> x.user != null).Count == 0)
            {
                Kill();
                ServerManager.sessions.Remove(this); //Close session immediately
            }
            else 
                Update();

            return savedSpam;
        }

        public bool killed;

        public void Kill(bool success = false)
        {
            killed = true;
            time = Time.time + MapLoader.maps[map].roundTime;
            Debug.Log("Closing session at round: "+ round);
            int a = agents.Count;

            if (success)
            {
                if (a != 0)
                {
                    for (int i = 0; i < a; i++)
                    {
                        if (agents[i] != null)
                            agents[i].Stop(true);
                    }
                }

                MObjects.SessionEnd mObject = new MObjects.SessionEnd();

                int c = users.Count;
                for (int i = 0; i < c; i++)
                {
                    if (NetworkServer.connections.Contains(users[i]))
                        NetworkServer.SendToClient(users[i].connectionId, MTypes.SessionEnd, mObject);
                }
            }
            else if (a != 0)
            {
                Debug.Log("Session closing...");
                for (int i = 0; i < a; i++)
                {
                    if (agents[i].user != null && NetworkServer.connections.Contains(agents[i].user))
                        agents[i].user.Disconnect();

                    Object.Destroy(agents[i].gameObject); // Remove the mobile agents
                }
            }
        }

        public void Start()
        { // session started
            time = Time.time + MapLoader.maps[map].roundTime;

            int cS = creatureSpawns.Length;
            for (int i = 0; i < cS; i++)
                creatureSpawns[i] = 0;

			if (!isStarted)
            {
                ushort tSize = MapLoader.maps[map].teamsize;
                spawnPointRequester = new ushort[ (tSize == 0) ? 1 : tSize ]; 
				CreateScoreData ();
				UpdateRankings ();

                isStarted = true;
            }

            List<MobileAgent> heroes = agents.FindAll (x=>x.heroType == HeroType.Player);
            int c = heroes.Count;
            for (int i = 0; i < c; i++)
            {
                heroes[i].agentLevel.LevelInfo();
                heroes[i].health = heroes[i].maxHealth;
                heroes[i].transform.position = MapLoader.maps[map].Request_SpawnPoint (this, heroes[i].team);
                heroes[i].Stop(true);
                heroes[i].SyncPosition();
                heroes[i].agentBuff.buff.buffs.Clear(); // Clear all buffs.
                heroes[i].agentBuff.buff.buffs.AddRange(heroes[i]._hero.defaultBuffs); // Re-add default buffs
            }

            c = createdSkills.Count;
            for (int i = 0; i < c; i++)
            {
                Object.Destroy(createdSkills[i]);
            }

            createdSkills.Clear();

            List<MobileAgent> creatures = agents.FindAll(x => x.heroType == HeroType.Creature);
            c = creatures.Count;

            for (int i = 0; i < c; i++)
            {
                creatures[i].GetComponent<AIAgent>().activeTarget = null;
            }

            Update();
        }

        public void Update(bool refreshAgents = false) // Update the session
        {
            MObjects.SessionUpdate mObject = new MObjects.SessionUpdate();

            int _c = agents.Count;

            mObject.isStarted = isStarted;
            mObject.isKilled = killed;
            mObject.mapId = MapLoader.maps[map].clientSceneName;
            mObject.maxRound = MapLoader.maps[map].roundCount;
            mObject.round = (ushort) Mathf.Clamp(round, 1, mObject.maxRound);
			mObject.isStarting = (!isStarted && agents.Count >= MapLoader.maps [map].minPlayers);
            mObject.teamSize = teamsize;

            mObject.seconds = (ushort)Mathf.RoundToInt(time - Time.time);

            bool playersFull = (agents.FindAll(x => x.heroType == HeroType.Player).Count >= MapLoader.maps[map].maxPlayers);
             
            for (int i = 0; i < _c; i++)
            {
                if (agents[i].user != null && NetworkServer.connections.Contains(agents[i].user))
                {
                    mObject.canAddBots = (i == 0 && !isStarted && canAddBots && agents.Count < MapLoader.maps[map].minPlayers && !playersFull && !killed);
					
                    // send the mobject
                    NetworkServer.SendToClient(agents[i].user.connectionId, MTypes.SessionUpdate, mObject);
                }

                if (refreshAgents)
                    agents [i].AgentInfo();
            }
        }
			
		public void UpdateRankings ()
		{
			MObjects.ScoreInfo mObject = new MObjects.ScoreInfo ();
			mObject.ids = score_ids;
			mObject.teams = score_teams;
			mObject.kills = score_kills;
			mObject.deaths = score_deaths;

			ushort _c = (ushort) users.Count;

			for (ushort i = 0; i < _c; i++) {
				if (NetworkServer.connections.Contains (users[i]))
				{
					NetworkServer.SendToClient (users [i].connectionId, MTypes.ScoreInfo, mObject);
				}
			}
		}

        public void LastAim(int connectionId, Vector3 y, Vector3 agentPos)
        {
			MobileAgent _ma = agents.Find(x => ((x.user != null) ? x.user.connectionId : x.customId) == connectionId);

            if (_ma != null && !_ma.isDead)
            {
                SpamController.obj.SpamFor(_ma, 2); // client can send this per 0.1f seconds or the spammer will be increased

                _ma.Aim(y, agentPos, true);
            }
        }

        public void AimAgent(int connectionId, Vector3 y, Vector3 agentPos)
        {
			MobileAgent _ma = agents.Find(x => ((x.user != null) ? x.user.connectionId : x.customId) == connectionId);

            if (_ma != null && !_ma.isDead)
            {
                SpamController.obj.SpamFor(_ma, 1); // client can send this per 0.05f seconds or the spammer will be increased

                _ma.Aim(y, agentPos);
            }
        }

        public void MoveAgent(int connectionId, Vector3 tPos)
        {
			MobileAgent _ma = agents.Find(x => ((x.user != null) ? x.user.connectionId : x.customId) == connectionId);

            if (_ma != null && _ma.skillStart < Time.time && !_ma.isDead)
            {
                SpamController.obj.SpamFor(_ma, 1); // client can send this per 0.05f seconds or the spammer will be increased

                if (MapLoader.isBlocked(map, _ma.transform.position, tPos, false))
                {
                    tPos = MapLoader.latestPoint(map, _ma.transform.position, tPos);
                }

                _ma.targetPoint = tPos;
            }
        }

        public void SkillRequest(int connectionId, ushort skillId)
        {
			MobileAgent _ma = agents.Find(x => ((x.user != null) ? x.user.connectionId : x.customId) == connectionId);
            if (_ma != null && !_ma.isDead && _ma.skillStart == 0f)
            {
                SpamController.obj.SpamFor(_ma, 3);

                _ma.SkillRequest(skillId);
            }
        }
    }
}
