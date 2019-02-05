/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 24 December 2017
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

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
    Teleport, // 7
    Hook // 8
}

public class Skill : MonoBehaviour
{
    public Callipso.GameSession session;
    public string id;
    public SkillType skillType;
    public string clientPrefab;
    public MobileAgent caster;
    public float moveSpeed = 0;
    public ushort effectTime;

    float _life;
    float _defaultLife;
    public float life
    {
        get
        {
            return _life;
        }

        set
        {
            _life = value;
            if (_defaultLife == 0)
            _defaultLife = value;
        }
    }
    public bool hitAndDestroy = false;
    public int effect;
    public EffectType effectType;
    public float collision;
    public bool hitContinous = false;
    public float activeAfter;

    bool started = false;
    void Start()
    {
        Physik physik = gameObject.AddComponent<Physik>();
        physik.radius = collision;
        physik.SearchForCollision = true;
        physik.agent = caster;
        physik.session = session;
        physik.hitContinous = hitContinous;
        physik.updateTime = (skillType == SkillType.Mover) ? 0.05f : 1;
        physik.nextControl = Time.time + activeAfter;
        Inform();
        started = true;
    }

    bool dead;

    // Update is called once per frame
    void Update()
    {
        if (dead)
            return;

        if (caster == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        life -= Time.deltaTime;

        if (life <= 0)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Inform()
    {
        MObjects.SkillSpawn mObject = new MObjects.SkillSpawn();
        mObject.id = id;
        mObject.clientPrefab = clientPrefab;
        mObject.speed = moveSpeed;
        mObject.position = transform.position;
        mObject.rotation = transform.eulerAngles.y;

        if (skillType == SkillType.Mover)
        {
            mObject.casterId = (caster.user != null) ? caster.user.connectionId : caster.customId;
        }

        int _c = session.agents.Count;
        for (int i = 0; i < _c; i++)
        {
            if (session.agents[i].user != null && NetworkServer.connections.Contains(session.agents[i].user))
            {
                // send the mobject
                NetworkServer.SendToClient(session.agents[i].user.connectionId, MTypes.SkillSpawn, mObject);
            }
        }
    }

    void OnDestroy()
    {
        session.createdSkills.Remove(gameObject);

        if (!started)
            return;

        MObjects.SkillDestroy mObject = new MObjects.SkillDestroy();
        mObject.id = id;

        int _c = session.agents.Count;
        for (int i = 0; i < _c; i++)
        {
            if (session.agents[i].user != null && NetworkServer.connections.Contains(session.agents[i].user))
            {
                // send the mobject
                NetworkServer.SendToClient(session.agents[i].user.connectionId, MTypes.SkillDestroy, mObject);
            }
        }
    }

    public void PhysikHit(List<MobileAgent> hit)
    {
        if (dead)
            return;
        /*
         * POSSIBLE TARGETS
         * */
        switch (effectType)
        {
            case EffectType.Damage:
            case EffectType.Poison:
            case EffectType.Hook:
            case EffectType.Stun:
                {
                    if (session.teamsize > 0)
                    { // If the session is deathmatch, hits all, else remove teammates
                        List<MobileAgent> ma = hit.FindAll(x => x.team == caster.team); // remove my teammates
                        int mCount = ma.Count;
                        for (int i = 0; i < mCount; i++)
                        {
                            hit.Remove(ma[i]);
                        }
                    }

                    hit.Remove(caster); // remove also the caster
                }
                break;
            case EffectType.Heal:
            case EffectType.DamageShield:
            case EffectType.DamagerObject:
            case EffectType.Speed:
                { // Only to my teammates, if the game has multiple teams
                    if (session.teamsize > 0)
                    {
                        hit = hit.FindAll(x => x.team == caster.team);
                    }
                    else hit = hit.FindAll(x => x == caster);
                }
                break;
        }
        /*
         * */
         
        int c = hit.Count;
        for (int i = 0; i < c; i++)
        {
			hit [i].lastHitter = (caster.user == null) ? caster.customId : (short) caster.user.connectionId;
			hit [i].lastHitterSkill = clientPrefab;

            switch (effectType) // Effect TYPES
            {
                case EffectType.Damage:
                    hit[i].health -= effect;
                    break;

                case EffectType.Heal:
                    hit[i].health += effect;
                    break;

                case EffectType.Stun:
                    // This is a buff (actually debuff)
                    Buffing.Buff currentStun = hit[i].agentBuff.buff.buffs.Find(x => x.buffType == Buffing.buffTypes.Stun);
                    if (currentStun == null)
                    {
                        currentStun = new Buffing.Buff();
                        currentStun.buffType = Buffing.buffTypes.Stun;
                        hit[i].agentBuff.buff.buffs.Add(currentStun);
                    }

                    currentStun.buffEffect += (short) effect;
                    if (currentStun.buffEffect > 100)
                        currentStun.buffEffect = 100;

                    currentStun.buffTime += effectTime;

                    caster.agentBuff.nextUpdate = 0;
                    hit[i].AgentInfo();
                    break;

                case EffectType.Poison:
                    // This is a buff (actually debuff)
                    Buffing.Buff currentPoison = hit[i].agentBuff.buff.buffs.Find(x => x.buffType == Buffing.buffTypes.Poison);

                    if (currentPoison == null)
                    {
                        currentPoison = new Buffing.Buff();
                        currentPoison.buffType = Buffing.buffTypes.Poison;
                        hit[i].agentBuff.buff.buffs.Add(currentPoison);
                    }

                    currentPoison.buffEffect += (short)effect;
                    currentPoison.buffTime += effectTime;

                    caster.agentBuff.nextUpdate = 0;
                    break;

                case EffectType.Hook:
                    MObjects.Hook hook = new MObjects.Hook();
                    hook.from = (hit[i].user == null) ? hit[i].customId : hit[i].user.connectionId;
                    hook.to = (caster.user == null) ? caster.customId : caster.user.connectionId;

                    ushort e = (ushort)session.users.Count;
                    for (ushort a = 0; a < e; a++)
                    {
                        if (NetworkServer.connections.Contains(session.users[a]))
                        {
                            NetworkServer.SendToClient(session.users[a].connectionId, MTypes.Hook, hook);
                        }
                    }

                    hit[i].transform.position = caster.transform.position + transform.forward;
                    hit[i].nextSync = Time.time + 1;
                    break;

                case EffectType.DamageShield:
                    // This is a buff
                    Buffing.Buff currentShield = hit[i].agentBuff.buff.buffs.Find(x => x.buffType == Buffing.buffTypes.DamageShield);

                    if (currentShield == null)
                    {
                        currentShield = new Buffing.Buff();
                        currentShield.buffType = Buffing.buffTypes.DamageShield;
                        hit[i].agentBuff.buff.buffs.Add(currentShield);
                    }

                    currentShield.buffEffect += (short)effect;
                    currentShield.buffTime += effectTime;

                    caster.agentBuff.nextUpdate = 0;
                    break;

                case EffectType.DamagerObject:
                    // This is a buff
                    Buffing.Buff currentDamager = hit[i].agentBuff.buff.buffs.Find(x => x.buffType == Buffing.buffTypes.DamagerObject);

                    if (currentDamager == null)
                    {
                        currentDamager = new Buffing.Buff();
                        currentDamager.buffType = Buffing.buffTypes.DamagerObject;
                        hit[i].agentBuff.buff.buffs.Add(currentDamager);
                    }

                    currentDamager.buffEffect += (short)effect;
                    currentDamager.buffTime += effectTime;

                    caster.agentBuff.nextUpdate = 0;
                    break;

                case EffectType.Speed:
                    Buffing.Buff currentSpeed = hit[i].agentBuff.buff.buffs.Find(x => x.buffType == Buffing.buffTypes.Speed);

                    if (currentSpeed == null)
                    {
                        currentSpeed = new Buffing.Buff();
                        currentSpeed.buffType = Buffing.buffTypes.Speed;
                        hit[i].agentBuff.buff.buffs.Add(currentSpeed);
                    }

                    currentSpeed.buffEffect += (short)effect;
                    currentSpeed.buffTime += effectTime;

                    caster.agentBuff.nextUpdate = 0;
                    hit[i].AgentInfo();
                    break;
            }

            MObjects.SkillEffect mObject = new MObjects.SkillEffect();
			mObject.id = (hit[i].user != null) ? hit[i].user.connectionId : hit[i].customId;
            mObject.clientPrefab = clientPrefab;

            int _c = session.agents.Count;
            for (int e = 0; e < _c; e++)
            {
                if (session.agents[e].user != null && NetworkServer.connections.Contains(session.agents[e].user))
                {
                    // send the mobject
                    NetworkServer.SendToClient(session.agents[e].user.connectionId, MTypes.SkillEffect, mObject);
                }
            }
        }

        if (hitAndDestroy && hit.Count > 0)
        {
            Kill();
        }
    }

    public void Kill()
    {
        Destroy(gameObject, 0.02f);
        dead = true;
    }
}
