/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 24 December 2017
*/

using System.Collections.Generic;
using UnityEngine;

public class AISpawner
{
	public class botNames
	{
		public List<string> list = new List<string>();	
	}

	public static botNames _botNames;

	public static MobileAgent SpawnPlayerBot (string clientPrefab, Callipso.GameSession session)
	{
		MobileAgent created = ServerManager.current.JoinGame (null, session, _botNames.list [Random.Range (0, _botNames.list.Count)], null, true);
        AIAgent ai = created.gameObject.AddComponent<AIAgent>();
        ai.agent = created;
        ai.vision = 20;
        created.user = null;

        return created;
    }

    public static MobileAgent SpawnCreature(string clientPrefab, Callipso.GameSession session)
    {
        Callipso.Hero creature = ServerManager.creatureHeroes.Find(x => x.clientPrefab == clientPrefab);
        MobileAgent created = ServerManager.current.JoinGame(null, session, creature.alias, clientPrefab);
        AIAgent ai = created.gameObject.AddComponent<AIAgent>();
        ai.agent = created;
        ai.vision = creature.vision; // Bots always can see
        created.user = null;

        return created;
    }
}
