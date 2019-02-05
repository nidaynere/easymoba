/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 24 December 2017
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ServerManager : MonoBehaviour
{
    // DON't MODIFY THE SERVER TO BE RESTARTED VIA SCENEMANAGER.LOADSCENE, YOU SHOULD CLOSE/OPEN AGAIN, IF YOU WANT TO RESTART THE SERVER
    public static ServerManager current;
    public static Callipso.ServerSettings setting;
    public static List<Callipso.Hero> playerHeroes = new List<Callipso.Hero>(); // player heroes list.
	public static List<Callipso.Hero> creatureHeroes = new List<Callipso.Hero>(); // creature heroes list
    public static List<Callipso.GameSession> sessions = new List<Callipso.GameSession>(); // created game sessions

    // Use this for initialization
    void Start ()
    {
        Application.targetFrameRate = 60;
        current = this;

        LoadSettings();
        LoadHeroes();
		LoadBotNames ();

        UNETServer.StartHost(); // Listen the server
    }

    public float nextUpdate = 0;
    List<Callipso.GameSession> willRemove = new List<Callipso.GameSession>();
    void Update()
    {
		if (nextUpdate > Time.time)
            return;

        nextUpdate = Time.time + 1;

        willRemove = new List<Callipso.GameSession>();

        int c = sessions.Count;
        for (int i = 0; i < c; i++)
        {
            if (sessions[i].isStarted && !sessions[i].killed)
            {
                List<Map.CreatureSpawn> cSpawns = MapLoader.maps[sessions[i].map].creatureSpawns;
                ushort lSpawns = (ushort)cSpawns.Count;

                for (ushort a = 0; a < lSpawns; a++)
                { // Spawn creatures
                    if (sessions[i].creatureSpawns[a] < Time.time)
                    {
                        sessions[i].creatureSpawns[a] = Time.time + cSpawns[a].spawnTime;

                        if (sessions[i].creatures.FindAll (x=>x.spawner == a).Count < cSpawns [a].spawnCount)
                        {
                            MobileAgent creature = AISpawner.SpawnCreature(cSpawns[a].GetSpawnObject(), sessions[i]);
                            creature.spawner = a;
                            sessions[i].creatures.Add(creature);

                            /*
                             *  FIND A NON BLOCKED POSITION
                             * */

                            bool found = false;
                            for (int s = 0; s < 10; s++)
                            {
                                Vector3 tPos = cSpawns [a].spawnPoint + new Vector3 (Random.Range (-cSpawns[a].spawnRange/2, cSpawns[a].spawnRange / 2), 0, Random.Range(-cSpawns[a].spawnRange / 2, cSpawns[a].spawnRange / 2));
                                if (tPos.x <= 0 || tPos.x >= MapLoader.maps[sessions[i].map].mapSize || tPos.z <= 0 || tPos.z >= MapLoader.maps[sessions[i].map].mapSize)
                                    continue;

                                if (MapLoader.isPositionBlocked(sessions[i].map, tPos))
                                    continue;

                                creature.transform.position = tPos;
                                found = true;
                            }

                            if (!found)
                                creature.transform.position = cSpawns[a].spawnPoint;
                        }
                    }
                }
            }

            if (sessions[i].time < Time.time)
            {
                if (sessions[i].killed)
                {
                    willRemove.Add(sessions[i]);
                }

                if (sessions[i].isStarted)
                {
                    sessions[i].round ++;

                    if (sessions[i].round > MapLoader.maps[sessions[i].map].roundCount)
                    {
                        // Game is over
                        sessions[i].Kill( !sessions [i].killed );
                    }
                    else sessions[i].Start();
                }
                else
                {
                    if (sessions[i].agents.Count < MapLoader.maps [sessions[i].map].minPlayers)
                    {
                        sessions[i].time = Time.time + MapLoader.maps[sessions[i].map].lobbyTime;
                        sessions[i].canAddBots = true;
                    } else
                    
					sessions[i].Start();
                }
            }
        }

        c = willRemove.Count;
        for (int i = 0; i < c; i++)
            sessions.Remove(willRemove[i]); // Remove the session from list;

        c = sessions.Count;
        for (int i = 0; i < c; i++)
            sessions[i].Update();
    }

    public static MObjects.HeroInfo heroInfo;

    void HeroLoadFromFolder(string folderName)
    {
        List<string> content = new List<string>();
        DataReader.readJson(folderName, out content);

        int c = content.Count;
        for (int i = 0; i < c; i++)
        {
            Callipso.Hero _hero = JsonUtility.FromJson<Callipso.Hero>(content[i]);
            if (_hero.heroType == Callipso.HeroType.Creature)
                creatureHeroes.Add(_hero);
            else
            {
                playerHeroes.Add(_hero);
            }
        }
    }

    void LoadHeroes()
    {
        HeroLoadFromFolder("Heroes");
        HeroLoadFromFolder("Creatures");

        heroInfo = new MObjects.HeroInfo();
        ushort c = (ushort) playerHeroes.Count;
        heroInfo.clientPrefab = new string[c];
        heroInfo.status = new bool[c];

        for (int i = 0; i < c; i++)
        {
            heroInfo.clientPrefab[i] = playerHeroes[i].clientPrefab;
            // locked status will be filled while player sending info
        }
    }

    void LoadSettings()
    {
        string readed = "";
        DataReader.readSingleJson("settings.json", out readed);
        setting = JsonUtility.FromJson<Callipso.ServerSettings>(readed); // Loading settings is done (You can reach it from anywhere by ServerManager.setting)
    }

	void LoadBotNames ()
	{
        string readed = "";
        DataReader.readSingleJson("playerBot_Aliases.json", out readed);

		AISpawner._botNames = JsonUtility.FromJson<AISpawner.botNames>(readed);
	}

    public void ClientConnected(NetworkMessage netMsg) // Registered on UNETServer
    {
        Debug.Log("Client connected.");
        // All clients Will receive maps info per second

        netMsg.conn.SetChannelOption(0, ChannelOption.MaxPendingBuffers, 64);
    }

    public void SendHeroInfo (NetworkConnection conn)
    {
        NetworkServer.SendToClient(conn.connectionId, MTypes.HeroInfo, heroInfo);
    }

    public void ClientDisconnected(NetworkMessage netMsg)
    {
        Debug.Log("Client disconnected. " + netMsg.conn.connectionId);
		Callipso.GameSession _s = sessions.Find(x => (x.agents.Find(e => e.user != null && e.user.connectionId == netMsg.conn.connectionId))); // currently in session

        if (_s!=null)_s.KickPlayer(netMsg.conn.connectionId); // remove the connection from the session
    }

    public static ushort createdSessions;

    public void FindGameRequest(NetworkMessage netMsg) // Registered on UNETServer
    {
        Debug.Log("Find game request received");
        /*
         * FIND THE CURRENT SESSION
         * */
        MObjects.FindGameRequest mObject = netMsg.ReadMessage<MObjects.FindGameRequest>();

        Callipso.GameSession _currentSession = sessions.Find(x => x.agents.Find(e => e.user != null && e.user.connectionId == netMsg.conn.connectionId)); // currently in session
        if (_currentSession != null)
        {
            netMsg.conn.Disconnect();
            return;
            // kicked
        }

        string clientPrefab = playerHeroes[Random.Range (0, playerHeroes.Count)].clientPrefab; // Random hero for first connection

        Callipso.GameSession _gameSession = sessions.Find(x => x.map == mObject.mapId && !x.isStarted && !x.killed && x.agents.Count < MapLoader.maps [x.map].maxPlayers);
		JoinGame (netMsg.conn, _gameSession, mObject.alias, clientPrefab, true, mObject.mapId);
    }

	public MobileAgent JoinGame (NetworkConnection conn, Callipso.GameSession _gameSession, string alias, string clientPrefab = null, bool isHero = false, ushort mapId = 0)
	{
        if (conn != null)
            SendHeroInfo(conn); // Send hero list 

        if (_gameSession == null)
		{ // No game found creating session
			Debug.Log("Creating game session");
			_gameSession = new Callipso.GameSession();
			_gameSession.id = createdSessions++;
			_gameSession.map = mapId;

            int cSpawns = MapLoader.maps[_gameSession.map].creatureSpawns.Count;
            _gameSession.creatureSpawns = new float[cSpawns];

            _gameSession.round = 1;
			_gameSession.time = Time.time + MapLoader.maps[_gameSession.map].lobbyTime;
            _gameSession.teamsize = MapLoader.maps[_gameSession.map].teamsize;

            sessions.Add(_gameSession);
		}
		else Debug.Log("Joining game session");

		GameObject _player = new GameObject("Player");
		MobileAgent _ma = _player.AddComponent<MobileAgent>();
        _ma.agentLevel = _player.AddComponent<MobileAgent_Leveling>();
        _ma.agentLevel.myAgent = _ma;
        _ma.agentBuff = _player.AddComponent<MobileAgent_Buffs>();
        _ma.agentBuff.myAgent = _ma;

        _ma.session = _gameSession;

		_ma.user = conn;

		if (_ma.user != null)
			_ma.session.users.Add (conn);

		if (_ma.user == null)
			_ma.customId = (short) (-(_gameSession.agentCreated++) - 1);
		_ma.alias = alias; // set user alias

		SpamController.obj.SpamFor(_ma, 10); // client can send this per two seconds or the spammer will be increased (10/5)

		_gameSession.agents.Add(_ma);

		_ma.LoadHero(clientPrefab, isHero); // for bots

		if (_ma._hero.heroType == Callipso.HeroType.Player && _gameSession.time < Time.time + 10) 
		{ // New player joined
			_gameSession.time = Time.time + Mathf.Clamp (10, 0, MapLoader.maps [_gameSession.map].lobbyTime);
		}

		_gameSession.Update(true);
        _ma.agentLevel.UpdateLevel(); // send the level info

        return _ma;
	}
    
    public void MoveRequest(NetworkMessage netMsg)
    {
		Callipso.GameSession _currentSession = sessions.Find(x => x.agents.Find(e => e.user != null && e.user.connectionId == netMsg.conn.connectionId)); // currently in session
        if (_currentSession == null || !_currentSession.isStarted)
        { // Not in a session or session is not started
            netMsg.conn.Disconnect();
            return;
        }

        MObjects.MoveRequest mObject = netMsg.ReadMessage<MObjects.MoveRequest>();
        _currentSession.MoveAgent(netMsg.conn.connectionId, mObject.value);
    }

    public void AimRequest(NetworkMessage netMsg)
    {
		Callipso.GameSession _currentSession = sessions.Find(x => x.agents.Find(e => e.user != null && e.user.connectionId == netMsg.conn.connectionId)); // currently in session
        if (_currentSession == null || !_currentSession.isStarted)
        { // Not in a session or session is not started
            netMsg.conn.Disconnect();
            return;
        }

        MObjects.AimRequest mObject = netMsg.ReadMessage<MObjects.AimRequest>();
        _currentSession.AimAgent(netMsg.conn.connectionId, mObject.y, mObject.pos);
    }

    public void HeroChangeRequest (NetworkMessage netMsg)
    {
		Callipso.GameSession _currentSession = sessions.Find(x => x.agents.Find(e => e.user != null && e.user.connectionId == netMsg.conn.connectionId)); // currently in session
        if (_currentSession == null || _currentSession.isStarted)
        { // Not in a session or session is not started
            netMsg.conn.Disconnect();
            return;
        }

        MObjects.HeroChangeRequest mObject = netMsg.ReadMessage<MObjects.HeroChangeRequest>();
        _currentSession.HeroChange (netMsg.conn.connectionId, mObject.val);
    }

	public void BotRequest (NetworkMessage netMsg)
	{
		Callipso.GameSession _currentSession = sessions.Find(x => x.agents.Find(e => e.user != null && e.user.connectionId == netMsg.conn.connectionId)); // currently in session
		if (_currentSession == null || _currentSession.isStarted)
		{ // Not in a session or session is not started
			netMsg.conn.Disconnect();
			return;
		}

		_currentSession.BotRequest (netMsg.conn.connectionId);
	}

    public void LastAim (NetworkMessage netMsg)
    {
		Callipso.GameSession _currentSession = sessions.Find(x => x.agents.Find(e => e.user != null && e.user.connectionId == netMsg.conn.connectionId)); // currently in session
        if (_currentSession == null || !_currentSession.isStarted)
        { // Not in a session or session is not started
            netMsg.conn.Disconnect();
            return;
        }

        MObjects.LastAim mObject = netMsg.ReadMessage<MObjects.LastAim>();
        _currentSession.LastAim(netMsg.conn.connectionId, mObject.y, mObject.pos);
    }

    public void SkillRequest (NetworkMessage netMsg)
    {
		Callipso.GameSession _currentSession = sessions.Find(x => x.agents.Find(e => e.user != null && e.user.connectionId == netMsg.conn.connectionId)); // currently in session
        if (_currentSession == null || !_currentSession.isStarted)
        { // Not in a session or session is not started
            netMsg.conn.Disconnect();
            return;
        }

        _currentSession.SkillRequest(netMsg.conn.connectionId, netMsg.ReadMessage<MObjects.SkillRequest>().skillId);
    }
}
