/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 24 December 2017
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysikEngine
{
    public static float GetDistance(Vector3 pos1, Vector3 pos2)
    {
        float SettledDist_X = Mathf.Abs(pos1.x - pos2.x);
        float SettledDist_Z = Mathf.Abs(pos1.z - pos2.z);
        float SettledDist = SettledDist_X * SettledDist_X + SettledDist_Z * SettledDist_Z;
        SettledDist = Mathf.Sqrt(SettledDist);
        return SettledDist;
    }

    public static void FindCollision(ushort sessionId, Vector3 pos, float radius, out List<MobileAgent> found)
    {
        Callipso.GameSession _gameSession = ServerManager.sessions.Find(x => x.id == sessionId);

        found = new List<MobileAgent>();

        if (_gameSession == null)
            return; // Session not found

		List<MobileAgent> alives = _gameSession.agents.FindAll (x => x.health > 0);
		int agentCount = alives.Count;

        for (int i = 0; i < agentCount; i++)
        {
			if (GetDistance(pos, alives[i].transform.position) - alives[i].physik.radius/2 <= radius)
				found.Add(alives[i]);
        }
    }
}

public class Physik : MonoBehaviour
{
    private void Start()
    {
        lastPos = transform.position;    
    }

    public Callipso.GameSession session;
    public ushort team;
    public float radius;
    public bool SearchForCollision;
    public bool hitContinous;
    public MobileAgent agent;
    public float updateTime;

    public float nextControl;

    public List<MobileAgent> alreadyHit = new List<MobileAgent>();

    Vector3 lastPos = Vector3.zero;
	// Update is called once per frame
	void Update ()
    {
        if (nextControl > Time.time)
            return;

		if (agent == null) 
		{
			Destroy (gameObject);
		}

        nextControl = Time.time + updateTime;

        if (SearchForCollision)
        {
            if (MapLoader.isBlocked(session.map, transform.position, transform.position + transform.forward))
            {
                gameObject.SendMessage("Kill");
                return;
            }

            List<MobileAgent> _list = new List<MobileAgent>();
            PhysikEngine.FindCollision(session.id, transform.position, radius + (transform.position - lastPos).magnitude, out _list);

            lastPos = transform.position;
            if (!hitContinous)
            {
                int c = alreadyHit.Count;
                for (int i = 0; i < c; i++)
                    _list.Remove(alreadyHit[i]);

                alreadyHit.AddRange(_list);
            }

            if (_list.Count == 0)
                return;

            gameObject.SendMessage("PhysikHit", _list);
        }
	}
}
