/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 24 December 2017
*/

using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using EpPathFinding.cs;

public class AIAgent : MonoBehaviour 
{
    public MobileAgent agent;

    Physik _activeTarget;
	public Physik activeTarget
    {
        get
        {
            return _activeTarget;
        }
        set
        {
            _activeTarget = value;

            if (value == this)
                Debug.Log("Selft is target??!1");
        }
    }

    public float distanceToActive;
    public float vision; // targeter vision

    public Callipso.Skill [] skills_defense;
    public Callipso.Skill [] skills_attack;

    float nextAIUpdate;

    Vector3 SpawnPoint;

    JumpPointParam jpParam;
    private void Start()
    {
        SpawnPoint = transform.position;
        jpParam = new JumpPointParam(MapLoader.maps[agent.session.map].aStar);

        skills_defense = agent.skills.ToList().FindAll(x => x.effectType == EffectType.Heal || x.effectType == EffectType.DamageShield).ToArray ();
        skills_attack = agent.skills.ToList().FindAll(x => x.effectType != EffectType.Heal && x.effectType != EffectType.DamageShield).ToArray();
    }

    // Update is called once per frame
    void Update ()
    {
        if (!agent.session.isStarted || agent.session.killed)
            return;
        
        if (activeTarget != null && agent.skillStart != 0f)
        { // If there is target and this is on skill casting, always keep aim
            agent.session.LastAim (agent.customId, activeTarget.transform.position, transform.position);
        }

		if (nextAIUpdate > Time.time)
			nextAIUpdate = Time.time + Random.Range (0.5f, 1f);

        SearchForTarget();
        GetSupport();
        GetAction();
        DoPath();

        if (activeTarget == null && agent.heroType == Callipso.HeroType.Creature && PhysikEngine.GetDistance (transform.position, SpawnPoint) > 1)
        {
            Vector3 export = Vector3.zero;
            GetPath(SpawnPoint); // Return the spawn point
        }
    }

    void SearchForTarget()
    {
        if (activeTarget != null && activeTarget.agent.isDead)
        {
            activeTarget = null;
        }

        List<MobileAgent> agents = agent.session.agents.FindAll(x => x != agent &&
        (agent.session.teamsize == 0 || x.team != agent.team) &&
        !x.isDead &&
        PhysikEngine.GetDistance(x.transform.position, transform.position) < vision &&
        (agent.heroType == Callipso.HeroType.Player || (agent.heroType == Callipso.HeroType.Creature && x.heroType == Callipso.HeroType.Player)));

        if (agents.Count > 0)
        {
            if (activeTarget == null || activeTarget.agent.heroType == Callipso.HeroType.Creature)
                activeTarget = agents[Random.Range(0, agents.Count)].physik;
            return;
        }

        if (activeTarget == null && !agent.pointed && agent.heroType == Callipso.HeroType.Player)
        {
            List<MobileAgent> targetCreatures = agent.session.creatures.FindAll(x => x != null);
            if (targetCreatures.Count > 0)
            {
                activeTarget = targetCreatures[Random.Range(0, targetCreatures.Count)].physik;
                GetPath(activeTarget.transform.position);
                return;
            }
        }

        /*
         * NO TARGET FOUND, LET's PICK A RANDOM Target
         * */

        if (agent._hero.heroType == Callipso.HeroType.Player && (targetPath == null || targetPath.Count == 0))
        {
            agents = agent.session.agents.FindAll(x =>
            (agent.session.teamsize == 0 || x.team != agent.team) &&
            !x.isDead &&
            (agent.heroType == Callipso.HeroType.Player || (agent.heroType == Callipso.HeroType.Creature && x.heroType == Callipso.HeroType.Player)));

            if (agents.Count > 0)
                GetPath(agents[Random.Range(0, agents.Count)].transform.position);
        }
    }

    void GetAction()
    {
        if (skills_attack.Length == 0)
            return;

        if (agent.skillStart != 0f)
            return; // already in action

        if (activeTarget != null)
        {
            distanceToActive = PhysikEngine.GetDistance(activeTarget.transform.position, transform.position);

            if (distanceToActive > vision)
            {
                activeTarget = null;
                return;
            }

            else if (!MapLoader.isBlocked (agent.session.map, transform.position, activeTarget.transform.position, false)) // TARGET IN SIGHT, SEARCH FOR SKILLS OR NOT MOVE TO TARGET
            {
                short tSpell = -1;

                /*
                 * DECIDE WITH PROPORTION RANDOM
                 * */
                /*
                 * */

                ProportionValue<ushort>[] clist = new ProportionValue<ushort>[skills_attack.Length];

                ushort rVal = (ushort)clist.Length;

                for (ushort r = 0; r < rVal; r++)
                {
                    ushort idx = (ushort) agent.skills.ToList().FindIndex(x => x == skills_attack[r]);
                    clist[r] = ProportionValue.Create((agent.cooldowns[idx] < Time.time) ? agent.skills[idx].botsDecideChance : 1, idx);
                }

                if (rVal > 1)
                    rVal = clist.ChooseByRandom();
                else rVal = clist[0].Value;

                if (agent.skills[rVal].skillType != SkillType.Self)
                {
                    float distance = (agent.skills[rVal].skillType == SkillType.Area) ? 10 : (agent.skills[rVal].moveSpeed * agent.skills[rVal].life);

                    if ((distance * 3) / 4 > distanceToActive)
                    {
                        tSpell = (short)rVal;
                    }
                }
                else tSpell = (short)rVal;

                if (tSpell != -1)
                {
                    /*
                        * CAST SPELL
                    * */
                    agent.session.LastAim(agent.customId, activeTarget.transform.position, transform.position);
                    agent.session.SkillRequest(agent.customId, (ushort)tSpell);
                    return;
                }
            }

            GetPath(activeTarget.transform.position);
        }
    }

    void GetSupport()
    {
        if (skills_defense.Length == 0)
            return;

        MobileAgent ally = agent.session.agents.Find(x => (x == agent || (x.team == agent.team && agent.session.teamsize != 0)) && !x.isDead && x.health < x.maxHealth / 2 && (x == agent || PhysikEngine.GetDistance(x.transform.position, agent.transform.position) <= 20));

        if (ally == null)
            return;

        if (ally != agent && agent.session.teamsize == 0)
        {
            ally = agent;
        }

        if (ally == agent || !MapLoader.isBlocked(agent.session.map, transform.position, ally.transform.position, false)) // TARGET IN SIGHT
        {
            short tSpell = -1;

            /*
             * DECIDE WITH PROPORTION RANDOM
             * */
            /*
             * */

            ProportionValue<ushort>[] clist = new ProportionValue<ushort>[skills_defense.Length];

            ushort rVal = (ushort)clist.Length;

            for (ushort r = 0; r < rVal; r++)
            {
                ushort idx = (ushort)agent.skills.ToList().FindIndex(x => x == skills_defense[r]);
                clist[r] = ProportionValue.Create((agent.cooldowns[idx] < Time.time) ? agent.skills[idx].botsDecideChance : 1, idx);
            }

            if (rVal > 1)
                rVal = clist.ChooseByRandom();
            else rVal = clist[0].Value;

            float distance = (agent.skills[rVal].skillType == SkillType.Area) ? 10 : (agent.skills[rVal].moveSpeed * agent.skills[rVal].life);

            if ((distance * 3) / 4 > distanceToActive)
            {
                tSpell = (short)rVal;
            }

            if (tSpell != -1)
            {
                /*
                    * CAST SPELL
                * */
                agent.session.LastAim(agent.customId, ally.transform.position, transform.position);
                agent.session.SkillRequest(agent.customId, (ushort)tSpell);
                return;
            }
        }
        // 
    }

    /*
     * PATH WALKER
     * */
    
    public int targetNode;
    public List<Vector3> targetPath;

    float nextPath = 0;
    public void GetPath(Vector3 targetPos)
    {
        if (nextPath > Time.time)
            return;

        nextPath = Time.time + 0.2f;

        targetNode = 0;

        GridPos startPos = new GridPos(Mathf.RoundToInt (transform.position.x), Mathf.RoundToInt (transform.position.z));
        GridPos endPos = new GridPos(Mathf.RoundToInt(targetPos.x), Mathf.RoundToInt(targetPos.z));
        jpParam.Reset(startPos, endPos);

        JumpPointFinder.FindPath(jpParam, out targetPath);
    }

    void DoPath()
    {
        if (targetPath != null && targetPath.Count > 0)
        {
            if (PhysikEngine.GetDistance(transform.position, targetPath[targetNode]) < 0.5f)
            {
                targetNode++;

                if (targetNode >= targetPath.Count)
                {
                    targetPath = new List<Vector3>();
                    targetNode = 0;
                    return;
                }
            }

            if (agent.skillStart == 0f && targetPath.Count > 0)
            {
                agent.targetPoint = targetPath[targetNode];
            }
        }
    }
}
