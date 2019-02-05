/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 24 December 2017
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

public class MobileAgent : MonoBehaviour
{
    ushort _spammer;
    public ushort spammer
    {
        get
        {
            return _spammer;
        }

        set
        {
            _spammer = value;

            if (value >= ServerManager.setting.spamprotectionsize)
            {
				if (user != null && NetworkServer.connections.Contains (user)) {
					Debug.Log(alias + " kicked because spam " + _spammer);
				}
            }
        }
    }

    public MobileAgent_Leveling agentLevel; // agent's level
    public MobileAgent_Buffs agentBuff; // agent's buff

    public NetworkConnection user;
	public short customId; // for bots and creatures

    public bool isDead = false;
    public int maxHealth = 0;
    int _health;

	public int lastHitter;
	public string lastHitterSkill;

    public int health
    {
        get
        {
            return _health;
        }

        set
        {
            value = Mathf.Clamp(value, 0, maxHealth);

            bool justUpdate = (value == _health);

            _health = value;

            MObjects.AgentHealth mObject = new MObjects.AgentHealth();
			mObject.id = (user != null) ? user.connectionId : customId;
            mObject.hp = (ushort) _health;
            mObject.maxhp = (ushort)maxHealth;
	
            isDead = (_health <= 0);

            /*
             * CHECK SESSION TO ROUND FINISH AND SCOREBOARD
             * */
            if (isDead && !justUpdate)
            {
                Stop(true);

                if (heroType == Callipso.HeroType.Player)
                {
                    if (session.teamsize == 0)
                    {
                        if (session.agents.FindAll(x => x.heroType == Callipso.HeroType.Player && !x.isDead).Count <= 1)
                        {
                            session.RoundComplete();
                        }
                    }
                    else
                    {
                        ushort haveAlive = 0;
                        for (int i = 0; i < session.teamsize; i++)
                        {
                            if (session.agents.FindAll(x => x.heroType == Callipso.HeroType.Player && x.team == i && !x.isDead).Count >= 1)
                                haveAlive++;
                        }

                        if (haveAlive <= 1)
                            session.RoundComplete();
                    }

                    MObjects.KillInfo _mObject = new MObjects.KillInfo();
                    _mObject.id = (user != null) ? (short)user.connectionId : customId;
                    _mObject.tId = lastHitter;

                    ushort _c = (ushort)session.users.Count;
                    for (ushort i = 0; i < _c; i++)
                    {
                        if (NetworkServer.connections.Contains(session.users[i]))
                        {
                            NetworkServer.SendToClient(session.users[i].connectionId, MTypes.KillInfo, _mObject);
                        }
                    }

                    /*
                     * UPDATE RANKINGS
                     * */
                    
                }

                if (heroType == Callipso.HeroType.Player || session.teamsize == 1 || MapLoader.maps [session.map].minPlayers <= 1) // or co-op
                {
                    List<int> score_ids_list = session.score_ids.ToList();
                    int myIndex = score_ids_list.ToList().FindIndex(x => x == ((user != null) ? user.connectionId : customId));
                    int killerIndex = score_ids_list.ToList().FindIndex(x => x == lastHitter);

                    if (myIndex != -1)
                        session.score_deaths[myIndex]++;

                    if (killerIndex != -1)
                        session.score_kills[killerIndex]++;

                    session.UpdateRankings();
                }

                /*
                * KILLER GETS EXP
                * */
                MobileAgent killer = session.agents.Find(x => lastHitter == ((x.user == null) ? x.customId : x.user.connectionId));
                if (killer != null)
                    killer.agentLevel.getExp(_hero.experience);
                /*
                 * */
            }

            /*
             * */

            int c = session.users.Count;
            for (int i = 0; i < c; i++)
            {
				if (NetworkServer.connections.Contains(session.users[i]))
                {
                    // send the mobject
					NetworkServer.SendToClient(session.users[i].connectionId, MTypes.AgentHealth, mObject);
                }
            }

            if (isDead && heroType == Callipso.HeroType.Creature)
            {
                session.creatures.Remove(this);
                Destroy(gameObject, 5);
            }
        }
    }

    public string alias;
    public Callipso.GameSession session;
    public string heroId;

    [HideInInspector]
    public float _moveSpeed;
    public float moveSpeed
    {
        get
        {
            float f = 0;
            Buffing.Buff b = agentBuff.buff.buffs.Find(x => x.buffType == Buffing.buffTypes.Stun);
            if (b != null)
                f = b.buffEffect/100f;

            float g = 0;
            Buffing.Buff s = agentBuff.buff.buffs.Find(x => x.buffType == Buffing.buffTypes.Speed);
            if (s != null)
                g = s.buffEffect / 100f;

            f = _moveSpeed - (_moveSpeed * f) + (_moveSpeed * g);

            return f;
        }

        set
        {
            _moveSpeed = value;

            if (session.isStarted && !session.killed && skills != null && !isDead && health > 0)
            AgentInfo();
        }
    }

    public Callipso.Skill[] skills;

    public float[] cooldowns;

    public float tolerance;
    public float cannotSync;

	public Callipso.HeroType heroType;

    public ushort team;

    public Physik physik;

    public Callipso.Hero _hero;

	public void LoadHero(string _heroId, bool isHero = false)
    {
        /*
         * SET TEAM
         * */
        FindTeam(isHero);
        /*
         * */

        if (string.IsNullOrEmpty(_heroId))
        { // AUTO HERO SELECTION
            if (session.teamsize == 0)
            {
                _heroId = ServerManager.playerHeroes[Random.Range(0, ServerManager.playerHeroes.Count)].clientPrefab;
            }
            else
            {
                List<MobileAgent> teamMates = session.agents.FindAll(x => x.team == team);
                List<Callipso.Hero> trg = ServerManager.playerHeroes.FindAll(x => teamMates.Find(e => e.heroId == x.clientPrefab) == null);

                if (trg.Count == 0)
                {
                    _heroId = ServerManager.playerHeroes[Random.Range(0, ServerManager.playerHeroes.Count)].clientPrefab;
                } else
                _heroId = trg[Random.Range(0, trg.Count)].clientPrefab;
            }
        }
        
        maxHealth = 0;
        heroId = _heroId;

		if (isHero)
			_hero = ServerManager.playerHeroes.Find(x => x.clientPrefab == _heroId);
        else
            _hero = ServerManager.creatureHeroes.Find(x => x.clientPrefab == _heroId);
		
        moveSpeed = _hero.moveSpeed;
        skills = _hero.skills;

        cooldowns = new float[skills.Length];
        heroType = _hero.heroType;

        agentLevel.level = new Leveling.Level();
        agentLevel.level.level = 1;
        agentLevel.exp = 0;
        agentLevel.requiredExp = 10;

        /*
         * DEFAULT BUFFS
         * */

        agentBuff.buff.buffs.Clear();
        agentBuff.buff.buffs.AddRange(_hero.defaultBuffs);

        /*
         * */

        if (heroType == Callipso.HeroType.Creature)
            gameObject.name = _heroId;

        if (physik == null)
        {
            physik = gameObject.AddComponent<Physik>();
            physik.agent = this;
        }

        physik.session = session;
		physik.radius = _hero.collision;
        physik.team = team;

        if (user != null)
        {
            // SEND SKILL INFO
            MObjects.SkillInfo mObject = new MObjects.SkillInfo();
            mObject.skills = skills;
            NetworkServer.SendToClient(user.connectionId, MTypes.SkillInfo, mObject);
        }
    }
    /*
     * MOBILEAGENT IS THE BASE CLASS FOR PLAYERS & BOTS and others. 
     * */

    public void FindTeam(bool isHero = false)
    {
        if (!isHero)
        {
            team = 65535;
            return;
        }

        ushort[] teamSizes = new ushort[session.teamsize];
        for (ushort i = 0; i < session.teamsize; i++)
        {
            teamSizes[i] = (ushort)session.agents.FindAll(x => x != this && x.team == i).Count;
        }

        ushort lastCount = 65535;
        for (ushort i = 0; i < teamSizes.Length; i++)
        {
            if (teamSizes[i] < lastCount)
            {
                lastCount = teamSizes[i];
                team = i;
            }
        }
    }

    // Update is called once per frame

    Vector3 _targetPoint = Vector3.up;
    public Vector3 targetPoint
    {
        get
        {
            return _targetPoint;
        }

        set
        {
            bool val = !(value == Vector3.up);
            value.y = 0;
            _targetPoint = value;
            pointed = val;

            if (val)
            {
                MObjects.AgentMove mObject = new MObjects.AgentMove();
                mObject.id = (user != null) ? user.connectionId : customId;
                mObject.value = value;

                int _c = session.users.Count;
                for (int i = 0; i < _c; i++)
                {
                    if (NetworkServer.connections.Contains(session.users[i]))
                    {
                        // send the mobject
                        NetworkServer.SendToClient(session.users[i].connectionId, MTypes.AgentMove, mObject);
                    }
                }
            }
        }
    }

    public bool pointed;

    public void Stop (bool urgent = false)
    {
        targetPoint = Vector3.up;

        MObjects.AgentStop mObject = new MObjects.AgentStop();
        mObject.id = (user != null) ? user.connectionId : customId;
        mObject.includeClient = urgent;

        int _c = session.users.Count;
        for (int i = 0; i < _c; i++)
        {
            if (NetworkServer.connections.Contains(session.users[i]))
            {
                // send the mobject
                NetworkServer.SendToClient(session.users[i].connectionId, MTypes.AgentStop, mObject);
            }
        }
    }

    void OnDestroy()
    {
        if (session != null)
            session.agents.Remove(this);

        MObjects.AgentDestroy mObject = new MObjects.AgentDestroy();
		mObject.id = (user != null) ? user.connectionId: customId;
        int sC = session.users.Count;

        for (int i = 0; i < sC; i++)
        {
			if (NetworkServer.connections.Contains(session.users[i]))
            {
				NetworkServer.SendToClient(session.users[i].connectionId, MTypes.AgentDestroy, mObject);
            }
        }
    }

    public void AgentInfo()
    {
        int sC = skills.Length;
        MObjects.AgentInfo mObject = new MObjects.AgentInfo();
        mObject.alias = alias;
        mObject.clientPrefab = heroId;
        mObject.skills = new string[sC];
        mObject.moveSpeed = moveSpeed;
        
        for (int i = 0; i < sC; i++)
            mObject.skills[i] = skills[i].clientPrefab;

		mObject.id = (user != null) ? user.connectionId: customId;

		sC = session.users.Count;

        mObject.team = team;

        for (int i = 0; i < sC; i++)
        {
			if (NetworkServer.connections.Contains(session.users[i]))
            {
				mObject.isController = (session.users[i].connectionId == mObject.id);
                // send the mobject
				NetworkServer.SendToClient(session.users[i].connectionId, MTypes.AgentInfo, mObject);
            }
        }

        agentLevel.LevelInfo(); // Update user levels

        if (maxHealth == 0) // health is not assigned yet
        {
            maxHealth = _hero.health;
            health = maxHealth;
        }
        else health = _health; // Otherwise update user healths for other clients
    }

    public float nextMove = 0;
    public float nextSync = 0;
    public float nextSpamCounter = 0;

    int castedSkill = 0;
    void Update()
    {
        if (tolerance > 0)
            tolerance -= Time.deltaTime * 2;

        if (nextSpamCounter < Time.time)
        {
            if (spammer > 0) spammer--; // Reduce the spammer
            nextSpamCounter = Time.time + 0.03f;
        }

        if (nextSync < Time.time)
        {
            nextSync = Time.time + 0.5f;
            if (session.isStarted)
            {
                SyncPosition();
            }
        }

        CheckSkill();

        if (nextMove > Time.time || isDead || session.killed)
            return;

        if (pointed)
        {
            if (PhysikEngine.GetDistance(transform.position, targetPoint) < 0.1f)
            {
                targetPoint = Vector3.up;
            }
            else
            {
                if (skillStart < Time.time)
                transform.position += (targetPoint - transform.position).normalized * moveSpeed * Time.deltaTime; // if its casting, just rotate, don't move
                transform.rotation = Quaternion.LookRotation(targetPoint - transform.position);
            }
        }
    }

    public Vector3 aimPoint;

    public void CheckSkill()
    {
        if (skillStart != 0 && (lastAimReceived || skillStart < Time.time - 1 || user == null) && skillStart < Time.time)
        {
            lastAimReceived = false;
            skillStart = 0f;
            nextMove = Time.time + 0.5f;
            /*
             * CREATE SKILL
             * */
            if (!isDead)
            {
                Vector3 tPos = transform.position;

                bool isTeleport = skills[skillId].effectType == EffectType.Teleport;

                if (skills[skillId].skillType == SkillType.Area || isTeleport)
                { // Area Spell
                    Vector3 v = aimPoint;
                    if (PhysikEngine.GetDistance(aimPoint, tPos) > 10)
                    {
                        v = transform.position + (aimPoint - tPos).normalized * 10;
                    }

                    tPos = v;
                }

                if (isTeleport)
                { // TELEPORT SKILL
                    transform.position = MapLoader.latestPoint(session.map, transform.position, MapLoader.latestPoint (session.map, transform.position, tPos));
                    MObjects.Teleport tp = new MObjects.Teleport();
                    tp.id = (user == null) ? customId : user.connectionId;
                    tp.pos = transform.position;

                    int _ca = session.users.Count;
                    for (int i = 0; i < _ca; i++)
                    {
                        if (NetworkServer.connections.Contains(session.users[i]))
                        {
                            // send the mobject
                            NetworkServer.SendToClient(session.users[i].connectionId, MTypes.Teleport, tp);
                        }
                    }
                }
                else if (!MapLoader.isBlocked(session.map, transform.position, tPos, false))
                { // OTHERs
                    GameObject skill = new GameObject("skill");
                    Skill s = skill.AddComponent<Skill>();
                    session.createdSkills.Add(skill);
                    s.id = ((user != null) ? user.connectionId : customId) + "_" + castedSkill++;
                    s.clientPrefab = skills[skillId].clientPrefab;
                    s.moveSpeed = skills[skillId].moveSpeed;
                    s.skillType = skills[skillId].skillType;
                    s.life = skills[skillId].life;
                    s.effectTime = skills[skillId].effectTime;
                    s.session = session;
                    s.hitAndDestroy = skills[skillId].hitAndDestroy;
                    s.collision = skills[skillId].collision;
                    s.effect = skills[skillId].GetEffect(this);
                    s.effectType = skills[skillId].effectType;
                    s.hitContinous = skills[skillId].hitContinous;
                    s.transform.rotation = transform.rotation;
                    s.activeAfter = skills[skillId].activeAfter;
                    s.caster = this;
                    s.transform.position = tPos;

                    if (skills[skillId].skillType == SkillType.Self)
                    {
                        // This is a self type skill. Hit & Destroy Immediately.

                        List<MobileAgent> self = new List<MobileAgent>();
                        self.Add(this);
                        s.PhysikHit(self);

                        Destroy(skill);
                    }
                }

                cooldowns[skillId] = Time.time + skills[skillId].cooldown - (skills[skillId].cooldown * agentLevel.level.Percent_cooldown / 100f);

                if (user != null && NetworkServer.connections.Contains(user))
                {
                    MObjects.Cooldown _mObject = new MObjects.Cooldown();
                    _mObject.skillId = skillId;
                    _mObject.time = cooldowns[skillId] - Time.time;

                    NetworkServer.SendToClient(user.connectionId, MTypes.Cooldown, _mObject);
                }
            }

            /*
             * */

            Stop();

            MObjects.EndSkill mObject = new MObjects.EndSkill();
			mObject.id = (user != null) ? user.connectionId: customId;

            int _c = session.users.Count;
            for (int i = 0; i < _c; i++)
            {
				if (NetworkServer.connections.Contains(session.users[i]))
                {
                    // send the mobject
					NetworkServer.SendToClient(session.users[i].connectionId, MTypes.EndSkill, mObject);
                }
            }
        }
    }

    public void SyncPosition()
    {
        MObjects.SyncPosition mObject = new MObjects.SyncPosition();
        mObject.pos = transform.position;
		mObject.id = (user != null) ? user.connectionId: customId;

		int _c = session.users.Count;
        for (int i = 0; i < _c; i++)
        {
			if (NetworkServer.connections.Contains(session.users[i]))
            {
                // send the mobject
				NetworkServer.SendToClient(session.users[i].connectionId, MTypes.SyncPosition, mObject);
            }
        }
    }

    public ushort skillId;
    public float skillStart;

    public void SkillRequest(ushort id)
    {
        if (session.killed)
            return;

        if (nextMove > Time.time)
            return;

        if (id >= skills.Length)
            return;

        if (cooldowns[id] > Time.time) // not cooldown yet.
            return;

        if (_hero.heroType == Callipso.HeroType.Player && agentBuff.buff.buffs.Find(x => x.buffType == Buffing.buffTypes.DamagerObject) != null)
            return; // Players cannot use skill while damager object active.

        skillId = id;
        skillStart = Time.time + skills[id].casttime - (skills[id].casttime * agentLevel.level.Percent_fastercast / 100f);

        Stop();

        /*
         * INFO OTHERS
         * */

        MObjects.StartSkill mObject = new MObjects.StartSkill();
		mObject.id = (user != null) ? user.connectionId: customId;
        mObject.skillId = id;
        mObject.casttime = skills[id].casttime;
        mObject.skillType = (ushort) skills[id].skillType;
        mObject.skillSize = skills[id].collision;

        int _c = session.users.Count;
        for (int i = 0; i < _c; i++)
        {
			if (NetworkServer.connections.Contains(session.users[i]))
            {
                // send the mobject
				NetworkServer.SendToClient(session.users[i].connectionId, MTypes.StartSkill, mObject);
            }
        }
    }

    bool lastAimReceived = false;

    public void Aim(Vector3 y, Vector3 agentPos, bool lastAim = false)
    {
        if (isDead || y == transform.position)
            return;

        if (skillStart >= Time.time || !lastAimReceived)
        {
            y.y = 0;

            transform.rotation = Quaternion.LookRotation(y - agentPos);
            aimPoint = y;

            MObjects.AgentAim mObject = new MObjects.AgentAim();
            mObject.y = y;
			mObject.id = (user != null) ? user.connectionId: customId;

            int _c = session.users.Count;
            for (int i = 0; i < _c; i++)
            {
				if (NetworkServer.connections.Contains(session.users[i]))
                {
                    // send the mobject
					NetworkServer.SendToClient(session.users[i].connectionId, MTypes.AgentAim, mObject);
                }
            }
        }

        lastAimReceived = lastAim;
    }

    public ushort spawner = 65535; // spawner index, will be used if it's a creature
}
