/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MobileAgent : MonoBehaviour
{
    public static Vector3[] TextEffectOffset = new Vector3[]
        {
            new Vector3 (0, 60, 0),
            new Vector3 (-150, 0, 0),
            new Vector3 (150, 0, 0),
            new Vector3 (0, -60, 0)
        };

    public List<Transform> buffs = new List<Transform>(); // active buffs

    public ushort currentTextEffect;

    public Transform panel; // ui panel of this agent

    public void CreateTextEffect(string txt, Color c)
    {
        Transform t = Instantiate(GameManager.singleton.textEffect, panel);
        t.transform.position = Camera.main.WorldToScreenPoint(transform.position);
        t.GetComponent<TextEffect>().v = t.transform.localPosition + TextEffectOffset[currentTextEffect];

        nextCText = Time.time + 2;
        currentTextEffect++;
        if (currentTextEffect >= TextEffectOffset.Length)
            currentTextEffect = 0;

        Text tText = t.GetComponentInChildren<Text>();
        tText.text = txt;
        tText.color = c;
    }

    private void OnDestroy()
    {
        list.Remove(this);
    }

    public static float agentStepDifferency = 3.5f;

    public static List<MobileAgent> list = new List<MobileAgent>();
    public static MobileAgent user;
    public int id;

    public ushort level;
    public ushort exp;

	float _moveSpeed;
    public float moveSpeed
	{
		get
		{
			return _moveSpeed;
		}

		set
		{
			_moveSpeed = value;

			if (anim == null)
				anim = GetComponent<Animator> ();
			anim.SetFloat ("MoveSpeed", _moveSpeed);
		}
	}

    public string clientPrefab;
    public bool isController = false; // client controller
    public bool isCasting = false;
    public string[] skills;
    public ushort lastSkill;
    public ushort team;
    public bool isCreature;

    bool _isDead;
    public bool isDead
    {
        get
        {
            return _isDead;
        }

        set
        {
            _isDead = value;

            if (GameManager.sessionStarted && !isCreature)
            GameManager.singleton.score_grid.Find (team.ToString ()).Find (id.ToString ()).Find ("isDead").GetComponent<UIVisibility>().Open (_isDead);
        }
    }

	public string alias;

    public UICastingItem castingItem;
    public ushort currentHealth;
    public Filler health;
    public UnityEngine.UI.Text healthText;

    public Animator anim;
    public float nextMove;
    // Use this for initialization

    Cloth[] cloths = null;
    Vector3 _tPos;
    bool fixClothes = false;
    public void Fix (Vector3 tPos)
    {
        _tPos = tPos;

        if (GameManager.GetDistance(tPos, transform.position) > 4)
        { // Cloth fixer at that distance
            if (cloths == null)
                cloths = GetComponentsInChildren<Cloth>(true);

            foreach (Cloth c in cloths)
                c.gameObject.SetActive(false);

            fixClothes = true;
        }

        Invoke("EnableGO", Time.deltaTime*3);
    }

    void EnableGO()
    {
        if (_tPos != Vector3.up)
            transform.position = _tPos;

        if (fixClothes)
        {
            foreach (Cloth c in cloths)
            {
                c.enabled = false;
                c.gameObject.SetActive(true);
            }

            Invoke("EnableCloth", Time.deltaTime);

            fixClothes = false;
        }
    }

    void EnableCloth()
    {
        foreach (Cloth c in cloths)
        {
            c.enabled = true;
        }
    }

	void Awake ()
	{
		list.Add (this);
	}

    AgentSoundData asd;
	void Start ()
    {
        DontDestroyOnLoad(this);
        DontDestroyOnLoad(gameObject);

        asd = GetComponent<AgentSoundData>();

        Invoke("Hail", 0.1f);
    }

    void Hail()
    {
        if (asd != null)
            SoundPlayer.PlaySound(asd.born, gameObject, true, 1, 0.7f);
    }

    public float nextMoveRequest;
    // Update is called once per frame

    public bool stopRequest = false;

    float nextCText;
	void Update ()
    {
        if (nextCText < Time.time)
        {
            nextCText = Time.time + 2;
            if (currentTextEffect > 0)
                currentTextEffect--;
        }

        if (isDead)
            return;

        if (!GameManager.sessionStarted)
        {
            int myIndex = list.FindIndex(x => x == this);

            Vector3 v = Vector3.zero;

            v.x = myIndex %4f * agentStepDifferency - (agentStepDifferency * 1.5f);
            v.z = 2 + (myIndex / 4) * agentStepDifferency;

            transform.position = Vector3.Lerp(transform.position, CameraScript.sessionPosition - CameraScript.defaultOffsetToZero + v, 0.1f);

            aimPoint = transform.position + new Vector3(0, 0, -2);
        }

        if (pointed)
        {
            if (GameManager.GetDistance(targetPoint, transform.position) < 0.1f || GameManager.singleton.sessionUpdate.isKilled)
            {
                Stop();
            }
            else if (!isCasting && nextMove < Time.time)
            {
                 Vector3 v = transform.position;
                 v += (targetPoint - transform.position).normalized * moveSpeed * Time.deltaTime;
                 v.y = MapLoader.GetHeight(transform.position);
                 transform.position = v;
            }
        }

        if (aimPoint != transform.position && (pointed || isCasting || !GameManager.sessionStarted))
        {
            Quaternion look = Quaternion.LookRotation(aimPoint - transform.position);
            look.eulerAngles = new Vector3(0, look.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 0.15f);
        }
    }

    public Vector3 aimPoint;
    public float nextAimRequest;
    public float cannotMove;

    public Vector3 targetPoint = Vector3.up;
    public bool pointed = false;

    public void Stop ()
    {
        if (isController)
        {
            AgentInput.axis = false;
        }

        pointed = false;
        anim.SetBool("isMoving", false);
        targetPoint = Vector3.up;

        if (isController) // will work for only on keyboard
        {
            GameManager.singleton.clickEffect.transform.position = Vector3.up * -5000;
        }
    }

    public void StartMove(Vector3 v)
    {
        if (isController && MapLoader.isBlocked(GameManager.currentMapId, transform.position, v, false))
        {
            v = MapLoader.latestPoint(GameManager.currentMapId, transform.position, v);
        }

        if (GameManager.GetDistance(transform.position, v) < 0.5f)
        {
            Stop();
            return;
        }

        targetPoint = v;
        aimPoint = v;
        anim.SetBool("isMoving", !isCasting);

        if (isCasting)
            return;

        pointed = true;

        if (isController && KeyController.current.currentController == 0 && !AgentInput.axis) // only for keyboard
        {
            GameManager.singleton.clickEffect.transform.position = v + Vector3.up * 0.25f;
            GameManager.singleton.clickEffect.Play();
        }
    }
}
