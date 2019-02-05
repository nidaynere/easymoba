/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 24 December 2017
*/

using System.Collections.Generic;
using UnityEngine;
using EpPathFinding.cs;
using UnityEngine.Networking;

[System.Serializable]
public class Map
{
    public BaseGrid aStar;

    [HideInInspector]
    public int mapSize;

    public int clientLanguageId;
    public string clientSceneName;

    public List<CreatureSpawn> creatureSpawns = new List<CreatureSpawn>();

    [Tooltip ("This is a float value. If you make this 1.5, for example: agents get 3 exp, not 2.")]
    public float experienceModifier = 1f;

    [System.Serializable]
    public class SpawnObject
    {
        public string clientPrefab;
        public ushort spawnRate;
    }

    [System.Serializable]
    public class CreatureSpawn
    {
        public SpawnObject [] spawnObject;
        public Vector3 spawnPoint;
        public float spawnTime;
        public ushort spawnCount;
        public float spawnRange;

        public ProportionValue<ushort>[] clist;

        public string GetSpawnObject()
        {
            Debug.Log("Creature spawn request with cList: " + clist.Length);
            if (clist.Length < 2)
                return spawnObject[0].clientPrefab;

            else return spawnObject[clist.ChooseByRandom()].clientPrefab;
        }
    }

    [System.Serializable]
    public class TeamData
    {
        public Vector3[] spawnPoints;
    }

    public Texture2D data;

    public ushort teamsize = 0; // how many teams in this map?, 0 means deathmatch
    public ushort roundCount = 4; // how many rounds will be played?
    public ushort roundTime = 120; // how many seconds a round will be played?
    public ushort lobbyTime = 12; // how many seconds the player will wait in the lobby
    public ushort maxPlayers = 8;
    public ushort minPlayers = 2;

    public TeamData[] teamData;

    public Vector3 Request_SpawnPoint(Callipso.GameSession session, ushort team)
    {
        if (teamsize == 0) // 0 = deathmath
        {
            team = 0;
        }

        session.spawnPointRequester[team]++;

        if (session.spawnPointRequester[team] >= teamData[team].spawnPoints.Length)
            session.spawnPointRequester[team] = 0;
        return teamData[team].spawnPoints [session.spawnPointRequester[team]];
    }
}

public class MapLoader : MonoBehaviour
{
    public static List<Map> maps = new List<Map>(); // loaded maps

    private void Start()
    {
		// Load maps and nodes
        DataReader.readMaps(out maps);

        ushort mCount = (ushort)maps.Count;

        for (ushort i = 0; i < mCount; i++)
        {
            /*
             * CREATE PROPORTION RANDOM
             * */

            ushort cSpawns = (ushort) maps[i].creatureSpawns.Count;
            for (ushort b = 0; b < cSpawns; b++)
            {
                maps[i].creatureSpawns[b].clist = new ProportionValue<ushort>[maps[i].creatureSpawns[b].spawnObject.Length];
                ushort rVal = (ushort)maps[i].creatureSpawns[b].spawnObject.Length;
                for (ushort a = 0; a < rVal; a++)
                    maps[i].creatureSpawns[b].clist[a] = ProportionValue.Create(maps[i].creatureSpawns[b].spawnObject[a].spawnRate, a);
            }

            /*
             * END
             * */
            int sizeX = maps[i].data.width;
            int sizeY = maps[i].data.height;

            bool[][] movableMatrix = new bool[sizeX][];
            for (int widthTrav = 0; widthTrav < sizeX; widthTrav++)
            {
                movableMatrix[widthTrav] = new bool[sizeY];
                for (int heightTrav = 0; heightTrav < sizeY; heightTrav++)
                {
                    movableMatrix[widthTrav][heightTrav] = Mathf.RoundToInt(maps[i].data.GetPixel(widthTrav, heightTrav).r) < 0.1f;
                }
            }

            maps[i].aStar = new StaticGrid(sizeX, sizeY, movableMatrix);
        }
    }

    public static bool isBlocked(ushort mapId, Vector3 cPosition, Vector3 tPosition, bool fixedMagnitude =true)
    {
        Vector3 direction = (tPosition - cPosition).normalized;

        int magnitude = (fixedMagnitude) ? 2 : Mathf.RoundToInt (PhysikEngine.GetDistance (cPosition, tPosition));

        if (magnitude < 1) magnitude = 1;
        for (int i = 1; i < magnitude; i++)
        {
            Vector3 position = cPosition + direction * i;
            if (position.x <= 0 || position.x > maps[mapId].mapSize || position.z <= 0 || position.z >= maps[mapId].mapSize)
                return true; // Out of bounds

            bool c = maps[mapId].aStar.IsWalkableAt(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z));

            if (!c)
                return true;
        }

        return false;
    }

    public static Vector3 latestPoint(ushort mapId, Vector3 position1, Vector3 position2)
    {
        position1.y = 0;
        position2.y = 0;

        Vector3 normal = (position2 - position1).normalized;
        float currentDistance = 100;
        while (currentDistance > 1)
        {
            currentDistance = PhysikEngine.GetDistance(position2, position1);
            position1 += normal;

            if (isPositionBlocked(mapId, position1))
                return position1 - normal;
        }

        return position2;
    }

    public static bool isPositionBlocked(ushort mapId, Vector3 position)
    {
        position = new Vector3(Mathf.RoundToInt(position.x), 0, Mathf.RoundToInt(position.z));
        if (position.x <= 0 || position.x >= maps[mapId].mapSize || position.z >= maps[mapId].mapSize || position.z <= 0)
            return true;

        return !maps[mapId].aStar.IsWalkableAt(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z));
    }

    float nextUpdate = 0;

    MObjects.MapInfo mInfo = null;
    private void Update()
    {
        if (nextUpdate > Time.time)
            return;

        nextUpdate = Time.time + 2;

        int mCount = maps.Count;

        if (mInfo == null)
        {
            mInfo = new MObjects.MapInfo();
            mInfo.langId = new int[mCount];
            mInfo.players = new int[mCount];

            for (int i = 0; i < mCount; i++)
            {
                mInfo.langId[i] = maps[i].clientLanguageId;
            }
        }

        for (int i = 0; i < mCount; i++)
        {
            // update player count
            mInfo.players[i] = ServerManager.sessions.FindAll(x => x.map == i).Count;
        }

        int uCount = NetworkServer.connections.Count;

        NetworkServer.SendToAll(MTypes.MapInfo, mInfo);
    } 
}
