/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 16 February 2018
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

[System.Serializable]
public class Settings
{
    public static float soundVolume;
    public static float musicVolume;
    public static string language;

    public string targetIp;
    public ushort port;
    public string[] languages;
}

public class GameManager : MonoBehaviour
{
    public void LanguageUpdate()
    {
        string selectedLang = settings.languages[languageDropdown.value];
        Language.LoadLanguage(selectedLang);

        PlayerPrefs.SetString("Language", selectedLang);
    }

    public Dropdown languageDropdown;
    public Slider soundSlider;
    public Slider musicSlider;

    public void SetSoundVolume()
    {
        Settings.soundVolume = soundSlider.value;
        PlayerPrefs.SetFloat("SoundVolume", Settings.soundVolume);
    }

    public void SetMusicVolume()
    {
        Settings.musicVolume = musicSlider.value;
        PlayerPrefs.SetFloat("MusicVolume", Settings.musicVolume);
    }

    public Color[] teamColors;
    public Color creatureColor;

    public static float GetDistance(Vector3 pos1, Vector3 pos2)
    { // get distance of two position
        float SettledDist_X = Mathf.Abs(pos1.x - pos2.x);
        float SettledDist_Z = Mathf.Abs(pos1.z - pos2.z);
        float SettledDist = SettledDist_X * SettledDist_X + SettledDist_Z * SettledDist_Z;
        SettledDist = Mathf.Sqrt(SettledDist);
        return SettledDist;
    }

    public void ResetGame()
    {
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        Debug.Log("Game has been reset");

        sessionStarted = false;

        SessionEnd = false;

        isInSession = false;

        NetworkClient.ShutdownAll();

        if (panel_Rankings.myCanvas)
        {
            panel_Rankings.myCanvas.alpha = 0;
            panel_Rankings.GetComponent<UIVisibility>().enabled = true;
        }

        areaFollower.gameObject.SetActive(false);

        currentMap = "";
        round = 1;

        foreach (MobileAgent ma in MobileAgent.list)
            if (ma != null) Destroy(ma.gameObject);

        foreach (Skill s in Skill.list)
            if (s != null) Destroy(s.gameObject);

        int c = heroGrid.childCount;
        for (int i = 0; i < c; i++)
            Destroy(heroGrid.GetChild(i).gameObject);

        c = score_grid.childCount;
        for (int i = 0; i < c; i++)
            Destroy(score_grid.GetChild(i).gameObject);

        SceneManager.LoadScene("Game");

        Start();

        MobileAgent.list.Clear();
        Skill.list.Clear();

        nc = null;
    }

    public void OnDisconnect(NetworkMessage netMsg)
    {
        ResetGame();
    }

    public AreaIndicator areaFollower;

    public ParticleSystem clickEffect;

    public static GameManager singleton;
    public static Settings settings;
    public Transform keyCode; // will be used by KeyBinder.cs

    public GameObject menuEffect;

    public InputField debugIp;

    private void Start()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(this);
        DontDestroyOnLoad(gameObject);

        Application.targetFrameRate = 60;

        singleton = this;

        panel_Main.Open();
        panel_Lobby.Open(false);
        panel_Session.Open(false);
        panel_Game.Open(false);
        panel_levelTooltip.Open(false);
        panel_skillTooltip.Open(false);
        panel_Skills.Open(false);
        panel_Leveling.Open(false);
        panel_Leave.Open(false);
        panel_Counter.Open(false);
        panel_Heroes.Open(false);
        menuEffect.SetActive(true);

        LoadGame();
    }

    bool gameLoaded = false;
    void LoadGame()
    {
        if (gameLoaded)
            return;

        gameLoaded = true;

        LoadSettings();
        Settings.language = PlayerPrefs.GetString("Language", settings.languages[0]);

        ushort lCount = (ushort)settings.languages.Length;

        List<Dropdown.OptionData> oList = new List<Dropdown.OptionData>();
        for (int i = 0; i < lCount; i++)
            oList.Add(new Dropdown.OptionData(settings.languages[i], Resources.Load<Sprite>("Icons/Languages/" + settings.languages[i])));
        languageDropdown.AddOptions(oList);
        languageDropdown.value = settings.languages.ToList().FindIndex(x => x == Settings.language);
        if (languageDropdown.value == 0)
            LanguageUpdate();

        Settings.soundVolume = PlayerPrefs.GetFloat("SoundVolume");
        Settings.musicVolume = PlayerPrefs.GetFloat("MusicVolume");
        soundSlider.value = Settings.soundVolume;
        musicSlider.value = Settings.musicVolume;

        LoadAssets();
    }

    void LoadAssets()
    {
        /*
        * LOAD ASSETS
        * */

        panel_Loading.Open(true);
        panel_Loading.myCanvas.alpha = 1;
        loadingText.text = Language.GetText(43);

        heroes = Resources.LoadAll<Transform>("Heroes").ToList();
        heroes.AddRange(Resources.LoadAll<Transform>("Creatures"));
        skills = Resources.LoadAll<Transform>("Skills").ToList();
        skillEffect = Resources.LoadAll<Transform>("SkillEffects").ToList();
        buffs = Resources.LoadAll<Transform>("Buffs").ToList();
        particles = Resources.LoadAll<Transform>("Particles").ToList();
        icons = Resources.LoadAll<Sprite>("Icons").ToList();
        MapLoader.maps = Resources.LoadAll<Texture2D>("Maps").ToList();

        panel_Loading.Open(false);
        /*
         * */
    }

    public void LoadSettings()
    {
        settings = JsonUtility.FromJson<Settings>(Resources.Load<TextAsset>("settings").text);
    }

    public InputField input_alias;

    public static NetworkClient nc = null;
    public void Connect()
    {
        loadingText.text = Language.GetText(8);
        panel_Loading.Open();

        nc = new NetworkClient();
        nc.Connect(string.IsNullOrEmpty (debugIp.text) ? settings.targetIp : debugIp.text, settings.port);

        nc.RegisterHandler(MsgType.Connect, OnConnected);
        nc.RegisterHandler(MsgType.Disconnect, OnDisconnect);
        nc.RegisterHandler(MTypes.SessionUpdate, OnSessionUpdate);
        nc.RegisterHandler(MTypes.AgentInfo, OnAgentInfo);
        nc.RegisterHandler(MTypes.SyncPosition, OnSyncPosition);
        nc.RegisterHandler(MTypes.AgentMove, OnAgentMove);
        nc.RegisterHandler(MTypes.AgentStop, OnAgentStop);
        nc.RegisterHandler(MTypes.AgentDestroy, OnAgentDestroy);
        nc.RegisterHandler(MTypes.StartSkill, OnStartSkill);
        nc.RegisterHandler(MTypes.EndSkill, OnEndSkill);
        nc.RegisterHandler(MTypes.AgentHealth, OnAgentHealth);
        nc.RegisterHandler(MTypes.SkillDestroy, OnSkillDestroy);
        nc.RegisterHandler(MTypes.SkillSpawn, OnSkillSpawn);
        nc.RegisterHandler(MTypes.AgentAim, OnAgentAim);
        nc.RegisterHandler(MTypes.SkillEffect, OnSkillEffect);
        nc.RegisterHandler(MTypes.HeroInfo, OnHeroInfo);
        nc.RegisterHandler(MTypes.Cooldown, OnCooldown);
        nc.RegisterHandler(MTypes.KillInfo, OnKillInfo);
        nc.RegisterHandler(MTypes.ScoreInfo, OnScoreInfo);
        nc.RegisterHandler(MTypes.SessionEnd, OnSessionEnd);
        nc.RegisterHandler(MTypes.RoundComplete, OnRoundComplete);
        nc.RegisterHandler(MTypes.SkillInfo, OnSkillInfo);
        nc.RegisterHandler(MTypes.AgentLevel, OnAgentLevel);
        nc.RegisterHandler(MTypes.LevelInfo, OnLevelInfo);
        nc.RegisterHandler(MTypes.MapInfo, OnMapInfo);
        nc.RegisterHandler(MTypes.AgentBuff, OnAgentBuff);
        nc.RegisterHandler(MTypes.Teleport, OnTeleport);
        nc.RegisterHandler(MTypes.Hook, OnHook);
    }

    public Text loadingText;
    public UIVisibility panel_Main;
    public UIVisibility panel_Lobby;
    public UIVisibility panel_Loading;
    public UIVisibility panel_Session;
    public UIVisibility panel_Game;
    public UIVisibility panel_Counter;
    public UIVisibility panel_Leave;
    public UIVisibility panel_Heroes;
    public UIVisibility panel_Leveling;
    public UIVisibility panel_Skills;
    public GameObject panel_MapLoading;
    public GameObject button_FindGame;

    public Transform canvas;
    public Transform textEffect;

    private void OnConnected(NetworkMessage netMsg)
    {
        loadingText.text = Language.GetText(9);
        panel_Loading.Open(false);

        panel_Main.Open(false);
        panel_Lobby.Open();
        panel_MapLoading.SetActive(true);
        button_FindGame.SetActive(false);
    }

    public Transform heroGrid;
    public Transform heroPrefab;

    public void OnHeroInfo(NetworkMessage netMsg)
    {
        Debug.Log("Incoming hero info");
        MObjects.HeroInfo mObject = netMsg.ReadMessage<MObjects.HeroInfo>();

        int c = mObject.clientPrefab.Length;

        for (int i = 0; i < c; i++)
        {
            GenerateHero(mObject.clientPrefab[i], mObject.status[i]);
        }

        panel_Heroes.Open();
    }

    void GenerateHero(string clientPrefab, bool status)
    {
        Transform t = heroGrid.Find(clientPrefab);
        if (t == null)
        {
            t = Instantiate(heroPrefab, heroGrid);
            t.Find("Icon").GetComponent<Image>().sprite = icons.Find(x => x.name == clientPrefab);
            t.name = clientPrefab;
        }

        t.Find("StatusLocked").gameObject.SetActive(status);
    }

    public static string currentMap = "";
    public static ushort currentMapId;

    public static bool sessionStarted = false;

    public UIVisibility panel_AddBot;
    public Text Text_Round;

    public static bool isInSession = false;

    public bool lastRound = false;

    public UIVisibility panel_GameStart;
    public UIVisibility panel_RoundStart;

    int round = 1;

    public MObjects.SessionUpdate sessionUpdate;
    
    public void OnSessionUpdate(NetworkMessage netMsg)
    {
        sessionUpdate = netMsg.ReadMessage<MObjects.SessionUpdate>();

        isInSession = true;

        panel_Loading.Open(false);
        panel_Lobby.Open(false);

        if (!sessionStarted && sessionUpdate.isStarted)
        {
            panel_GameStart.Open();
        }

        panel_Session.Open(!sessionUpdate.isStarted);
        panel_Game.Open(sessionUpdate.isStarted);
        panel_AddBot.Open(sessionUpdate.canAddBots);
        panel_Heroes.Open(!sessionUpdate.isStarted);
        menuEffect.SetActive(!sessionUpdate.isStarted);
        panel_Skills.Open(true);
        panel_Leveling.Open(true);

        Text_Round.text = sessionUpdate.round + " / " + sessionUpdate.maxRound;

        if (sessionUpdate.round != round)
        {
            panel_RoundStart.GetComponentInChildren<Text>().text = (sessionUpdate.maxRound == sessionUpdate.round) ? Language.GetText (58) : (Language.GetText(4) + sessionUpdate.round);
            panel_RoundStart.Open();
            round = sessionUpdate.round;
        }

        panel_Leave.Open();
        sessionStarted = sessionUpdate.isStarted;

        /*
         * SESSION INFO RECEIVED
         * */

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (sessionUpdate.isKilled)
        {
            sb.Append("<size=33>" + Language.GetText(42));
        }
        else if (sessionUpdate.isStarted) {
            if (sessionUpdate.round == sessionUpdate.maxRound) {
                sb.Append(Language.GetText(17) + ": <size=22>");
            } else
                sb.Append(Language.GetText(18) + ": <size=18>");
        } else if (!sessionUpdate.isStarting) {
            sb.Append(Language.GetText(19) + ": <size=18>");
        } else
            sb.Append(Language.GetText(20) + "... <size=24>");

        sb.Append(sessionUpdate.seconds + " </size>");

        panel_Counter.Open();
        panel_Counter.GetComponentInChildren<Text>().text = sb.ToString();

        if (currentMap != sessionUpdate.mapId)
        {
            currentMap = sessionUpdate.mapId;
            currentMapId = (ushort) MapLoader.maps.FindIndex(x => x.name.Split ('@')[0] == sessionUpdate.mapId);

            SceneManager.LoadScene(currentMap);
        }
    }

    public Transform agentCanvas;
    public Transform creatureCanvas;

    [HideInInspector]
    public List<Transform> heroes = new List<Transform>();
    [HideInInspector]
    public List<Sprite> icons = new List<Sprite>();

    public void HeroChange(int val)
    {
        Invoke("CloseThePanel", Time.deltaTime);

        if (clientHeroId == val)
            return;

        MObjects.HeroChangeRequest mObject = new MObjects.HeroChangeRequest();
        mObject.val = (ushort)val;
        nc.Send(MTypes.HeroChangeRequest, mObject);

        loadingText.text = Language.GetText(6);
        panel_Loading.Open();
    }

    void CloseThePanel()
    {
        heroGrid.parent.gameObject.SetActive(false);
    }

    public void OnAgentInfo(NetworkMessage netMsg)
    {
        MObjects.AgentInfo mObject = netMsg.ReadMessage<MObjects.AgentInfo>();

        MobileAgent ma = MobileAgent.list.Find(x => x.id == mObject.id);
        if (ma == null) // Create the agent.
        {
            ma = CreateAgent(mObject);
        }
        else if (mObject.clientPrefab != ma.clientPrefab)
        {
            Destroy(ma.gameObject);
            ma = CreateAgent(mObject);
        }

        ma.moveSpeed = mObject.moveSpeed;
    }

    public Filler clientLevelFiller;
    public Text clientLevelText;
    public void OnAgentLevel(NetworkMessage netMsg) // agent level info
    {
        MObjects.AgentLevel mObject = netMsg.ReadMessage<MObjects.AgentLevel>();

        MobileAgent ma = MobileAgent.list.Find(x => x.id == mObject.id);

        if (ma == null)
            return;

        if (ma.level != 0 && ma.level != mObject.level)
        {
            // LEVEL UP PARTICLE
            CreateParticle("LevelUp", ma.transform);
            ma.CreateTextEffect(Language.GetText (59), Color.green);
        }

        else if (ma.exp != 0 && ma.exp != mObject.exp)
        {
            ma.CreateTextEffect((mObject.exp - ma.exp) + " " + Language.GetText(45), Color.yellow);
        }

        ma.level = mObject.level;
        ma.exp = mObject.exp;

        if (MobileAgent.user == ma)
        {
            /*
             * OUR CLIENTS WORK
             * */
            
            clientLevelFiller.fillAmount = mObject.exp / (float)mObject.requiredExp;
            clientLevelText.text = mObject.level.ToString ();

            /*
             * UPDATE USER LEVEL TOOLTIP
             * */

            client_level.myLevelInfo = new MObjects.Level();

            int lCount = levels_Grid.childCount;
            for (int i = mObject.level; i > 0; i--)
            {
                Transform t = levels_Grid.Find(i.ToString ());
                if (t != null)
                {
                    t.Find("reveal").gameObject.SetActive(true);
                    UILevelItem ul = t.GetComponent<UILevelItem>();
                    client_level.myLevelInfo.Percent_cooldown += ul.myLevelInfo.Percent_cooldown;
                    client_level.myLevelInfo.Percent_effect += ul.myLevelInfo.Percent_effect;
                    client_level.myLevelInfo.Percent_health += ul.myLevelInfo.Percent_health;
                    client_level.myLevelInfo.Percent_fastercast += ul.myLevelInfo.Percent_fastercast;
                    client_level.myLevelInfo.Percent_movespeed += ul.myLevelInfo.Percent_movespeed;
                }
            }

            string step = "\r\n";
            client_level.core = new System.Text.StringBuilder();
            client_level.pro = new System.Text.StringBuilder();

            client_level.core.Append(Language.GetText(50));
            client_level.pro.Append("%" + client_level.myLevelInfo.Percent_health);

            client_level.core.Append(step + Language.GetText(51));
            client_level.pro.Append(step + "%" + client_level.myLevelInfo.Percent_effect);

            client_level.core.Append(step + Language.GetText(52));
            client_level.pro.Append(step + "%" + client_level.myLevelInfo.Percent_fastercast);

            client_level.core.Append(step + Language.GetText(53));
            client_level.pro.Append(step + "%" + client_level.myLevelInfo.Percent_cooldown);

            client_level.core.Append(step + Language.GetText(54));
            client_level.pro.Append(step + "%" + client_level.myLevelInfo.Percent_movespeed);
        }

        Transform tGrid = score_grid.Find(ma.team.ToString ());
        if (tGrid == null)
        {
            return;
        }

        Transform p = tGrid.Find(ma.id.ToString());
        if (p == null)
            return;

        p.Find("expFiller").GetComponent<Filler>().fillAmount = mObject.exp / (float)mObject.requiredExp;
        p.Find("level").GetComponent<Text>().text = Language.GetText(44) + " " + mObject.level;
    }

    public Text levelHolder_Text;
    public Transform levels_Grid;
    public Transform level_Prefab;
    public UILevelItem client_level;

    public void OnLevelInfo(NetworkMessage netMsg)
    {
        MObjects.LevelInfo mObject = netMsg.ReadMessage<MObjects.LevelInfo>();

        string step = "\r\n";
        int lCount = mObject.levels.Length;

        /*
         * CLEAR
         * */
        // Clear level panel
        int cCount = levels_Grid.childCount;
        for (int i = 0; i < cCount; i++)
            Destroy(levels_Grid.GetChild(i).gameObject);

        /*
         * BUILD
         * */
        for (int i = 0; i < lCount; i++)
        {
            Transform l = Instantiate(level_Prefab, levels_Grid);
            l.Find("reqLevel").GetComponent<Text>().text = mObject.levels[i].level.ToString();

            l.name = mObject.levels[i].level.ToString();

            UILevelItem ul = l.GetComponent<UILevelItem>();

            ul.myLevelInfo = mObject.levels[i];

            ul.core = new System.Text.StringBuilder();
            ul.pro = new System.Text.StringBuilder();

            ul.core.Append("<color=#b7e0e2>" + Language.GetText(48) + "</color>" + step);
            ul.pro.Append(mObject.levels[i].level + step);

            if (mObject.levels[i].Percent_health > 0)
            {
                ul.core.Append(step + Language.GetText(50));
                ul.pro.Append(step + "%" + mObject.levels[i].Percent_health);
            }

            if (mObject.levels[i].Percent_effect > 0)
            {
                ul.core.Append(step + Language.GetText(51));
                ul.pro.Append(step + "%" + mObject.levels[i].Percent_effect);
            }

            if (mObject.levels[i].Percent_fastercast > 0)
            {
                ul.core.Append(step + Language.GetText(52));
                ul.pro.Append(step + "%" + mObject.levels[i].Percent_fastercast);
            }

            if (mObject.levels[i].Percent_cooldown > 0)
            {
                ul.core.Append(step + Language.GetText(53));
                ul.pro.Append(step + "%" + mObject.levels[i].Percent_cooldown);
            }

            if (mObject.levels[i].Percent_movespeed > 0)
            {
                ul.core.Append(step + Language.GetText(54));
                ul.pro.Append(step + "%" + mObject.levels[i].Percent_movespeed);
            }
        }
    }

    public Transform skillsHolder;
    public Transform skillItem;
    public KeyLine heroKeyLine;

    public Transform selectedHero;

    int clientHeroId;
    public MobileAgent CreateAgent(MObjects.AgentInfo mObject)
    {
        int heroPrefab = heroes.FindIndex(x => x.name == mObject.clientPrefab);
        Transform t = Instantiate(heroes[heroPrefab]);
        MobileAgent ma = t.gameObject.AddComponent<MobileAgent>();
        ma.id = mObject.id;
        t.gameObject.name = ma.id.ToString();
        ma.clientPrefab = mObject.clientPrefab;
        ma.alias = mObject.alias;
        ma.team = mObject.team;
        ma.isCreature = (ma.team == 65535);

        ma.transform.position = (!ma.isCreature) ? CameraScript.sessionPosition - CameraScript.defaultOffsetToZero + new Vector3(0, 10, 0) : new Vector3(0, -5000, 0);

        ma.skills = mObject.skills;

        if (mObject.isController)
        {
            t.gameObject.AddComponent<AgentInput>();

            clientHeroId = heroPrefab;
            int cc = heroGrid.childCount;

            selectedHero.Find("selected").gameObject.SetActive(true);
            selectedHero.Find("Icon").GetComponent<Image>().sprite = heroGrid.Find(mObject.clientPrefab).Find ("Icon").GetComponent<Image>().sprite;
            heroGrid.parent.gameObject.SetActive(false);

            for (int i = 0; i < cc; i++)
            {
                string sName = heroGrid.GetChild(i).name;
                heroGrid.GetChild(i).Find("selected").gameObject.SetActive(mObject.clientPrefab == sName);
            }

            heroKeyLine.currentStep = heroPrefab;
            heroKeyLine.Set();

            ma.isController = true;
            MobileAgent.user = ma;
            panel_Loading.Open(false);

            // Clear skills panel
            int cCount = skillsHolder.childCount;
            for (int i = 0; i < cCount; i++)
                Destroy(skillsHolder.GetChild(i).gameObject);

            int sCount = ma.skills.Length;
            for (int i = 0; i < sCount; i++)
            {
                Transform s = Instantiate(skillItem, skillsHolder);
                s.name = i.ToString();
                s.Find("Icon").GetComponent<Image>().sprite = icons.Find(x => x.name == ma.skills[i]);

                if (KeyController.current.currentController != 3) // not for Touch
                {
                    KeyBinder kb = s.gameObject.AddComponent<KeyBinder>();
                    kb.targetPointer = s.gameObject;
                    kb.alwaysInit = true;
                    kb.keyCodeOffset = new Vector3(-40, 0, 0);
                    kb.Init(KeyController.current.clientSkillKeys [i]);
                }
            }

            /*
             * THIS IS MY CONTROLLER LETS MAKE THIS FIRST AT MOBILE AGENT LIST
             * */

            MobileAgent.list.Insert(0, MobileAgent.list[MobileAgent.list.Count - 1]);
            MobileAgent.list.RemoveAt(MobileAgent.list.Count - 1);
        }

        /*
         * AGENT UI
         * */
        ma.panel = Instantiate((ma.isCreature) ? creatureCanvas : agentCanvas, canvas);
        ma.panel.SetSiblingIndex(0); // always behind from the main ui

        Color tColor = (ma.isCreature) ? creatureColor : teamColors[mObject.team];
        Text alias = ma.panel.Find("Text").GetComponent<Text>();
        alias.text = mObject.alias;
        alias.color = tColor;

        ma.castingItem = ma.panel.GetComponentInChildren<UICastingItem>(true);
        ma.castingItem.mobileAgent = ma;
        ma.health = ma.panel.GetComponentInChildren<Filler>(true);
        ma.health._image.color = tColor;

        ma.healthText = ma.panel.Find("healthText").GetComponent<Text>();

        ma.panel.GetComponent<UIFollowAgent>().target = t;
        ma.panel.GetComponent<UIVisibility>().Open();

        return ma;
    }

    public void OnAgentMove(NetworkMessage netMsg)
    {
        MObjects.AgentMove mObject = netMsg.ReadMessage<MObjects.AgentMove>();

        MobileAgent ma = MobileAgent.list.Find(x => x.id == mObject.id);

        if (ma == null || ma.isController)
            return;

        ma.StartMove(mObject.value);
    }

    public void OnAgentAim(NetworkMessage netMsg)
    {
        MObjects.AgentAim mObject = netMsg.ReadMessage<MObjects.AgentAim>();

        MobileAgent ma = MobileAgent.list.Find(x => x.id == mObject.id);

        if (ma == null)
        {
            return;
        }

        if (!ma.isController)
        {
            ma.aimPoint = mObject.y;
        }
    }

    public void OnAgentStop(NetworkMessage netMsg)
    {
        MObjects.AgentStop mObject = netMsg.ReadMessage<MObjects.AgentStop>();

        MobileAgent ma = MobileAgent.list.Find(x => x.id == mObject.id);

        if (ma == null || (ma.isController && !mObject.includeClient))
            return;

        ma.Stop();
    }

    public void OnAgentHealth(NetworkMessage netMsg)
    {
        MObjects.AgentHealth mObject = netMsg.ReadMessage<MObjects.AgentHealth>();

        MobileAgent ma = MobileAgent.list.Find(x => x.id == mObject.id);

        if (ma == null)
        {
            return;
        }

        if (mObject.hp < ma.currentHealth)
        {
            /*
             * GETHIT
             * */
            ma.CreateTextEffect((ma.currentHealth - mObject.hp).ToString(), Color.red);
        }

        ma.currentHealth = mObject.hp;

        ma.health.fillAmount = mObject.hp / (float) mObject.maxhp;

        if (!ma.isDead && ma.health.fillAmount <= 0)
            CreateParticle("AgentDie", ma.transform);

        ma.isDead = (ma.health.fillAmount <= 0);

        ma.healthText.text = mObject.hp + "/" + mObject.maxhp;
        ma.anim.SetBool("Dead", ma.isDead);
    }

    public void OnAgentDestroy(NetworkMessage netMsg)
    {
        MObjects.AgentDestroy mObject = netMsg.ReadMessage<MObjects.AgentDestroy>();

        MobileAgent ma = MobileAgent.list.Find(x => x.id == mObject.id);

        if (ma != null)
        {
            Destroy(ma.gameObject);
        }
    }

    public void FindGame()
    {
        MObjects.FindGameRequest mObject = new MObjects.FindGameRequest();
        mObject.alias = string.IsNullOrEmpty(input_alias.text) ? Language.GetText(10) : input_alias.text;
        mObject.mapId = (ushort) selectedMap;
        nc.Send(MTypes.FindGameRequest, mObject);

        loadingText.text = Language.GetText(7);
        panel_Loading.Open();
    }

    public void OnSyncPosition(NetworkMessage netMsg)
    {
        MObjects.SyncPosition mObject = netMsg.ReadMessage<MObjects.SyncPosition>();

        MobileAgent ma = MobileAgent.list.Find(x => x.id == mObject.id);
        
        if (ma == null)
        {
            return;
        }

        mObject.pos.y = MapLoader.GetHeight(mObject.pos);

        float distance = GetDistance(ma.transform.position, mObject.pos);

        if (distance > ((ma.isController) ? 2 : 0.5f)) ma.Fix(mObject.pos);
    }

    public void OnStartSkill(NetworkMessage netMsg)
    {
        MObjects.StartSkill mObject = netMsg.ReadMessage<MObjects.StartSkill>();

        MobileAgent ma = MobileAgent.list.Find(x => x.id == mObject.id);

        if (ma == null)
        {
            return;
        }

        if (ma.isController)
        {
            if (mObject.skillType == 1) // area Spell
            {
                ma.castingItem.areaFollower = true;

                ParticleSystem.MainModule mm = areaFollower.indicator.main;
                mm.startSize = mObject.skillSize / 2;
            }
        }

        ma.lastSkill = mObject.skillId;
        ma.castingItem.StartCast(mObject.casttime, ma.skills[mObject.skillId], mObject.skillId);

        ma.Stop();
    }

    public void OnEndSkill(NetworkMessage netMsg)
    {
        MObjects.EndSkill mObject = netMsg.ReadMessage<MObjects.EndSkill>();

        MobileAgent ma = MobileAgent.list.Find(x => x.id == mObject.id);

        if (ma == null)
        {
            return;
        }

        ma.anim.SetTrigger("Skill" + ma.lastSkill);
        ma.castingItem.FinishCast();
    }

    [HideInInspector]
    public List<Transform> skills = new List<Transform>();

    public void OnSkillSpawn(NetworkMessage netMsg)
    {
        MObjects.SkillSpawn mObject = netMsg.ReadMessage<MObjects.SkillSpawn>();

        Skill s = Skill.list.Find(x => x.id == mObject.id);

        if (s == null)
        {
            Transform t = skills.Find(x => x.name == mObject.clientPrefab);
            Transform skill = Instantiate(t, Vector3.zero, t.rotation);
            s = skill.gameObject.AddComponent<Skill>();
            s.id = mObject.id;
            s.offset = t.position.y;
            s.moveSpeed = mObject.speed;
            s.casterId = mObject.casterId;

            MobileAgent ma = MobileAgent.list.Find(x => x.id == mObject.casterId);
            if (ma != null)
                mObject.position = ma.transform.position;

            s.SetPosition(mObject.position);
            s.transform.eulerAngles = new Vector3(s.transform.eulerAngles.x, mObject.rotation, s.transform.eulerAngles.z);
        }
    }

    public void OnSkillDestroy(NetworkMessage netMsg)
    {
        MObjects.SkillDestroy mObject = netMsg.ReadMessage<MObjects.SkillDestroy>();

        Skill s = Skill.list.Find(x => x.id == mObject.id);

        if (s != null)
        {
            ParticleSystem[] ps = s.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem p in ps)
            {
                var m = p.main;
                m.loop = false;
                p.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            SoundPlayer_Frequent sfq = s.GetComponent<SoundPlayer_Frequent>();
            if (sfq != null)
                sfq.down = true;

            Destroy(s.gameObject, s.GetComponent<HookScript>() ? 0: 3f);
            Destroy(s);
        }
    }

    [HideInInspector]
    public List<Transform> skillEffect = new List<Transform>();

    public void OnSkillEffect(NetworkMessage netMsg)
    {
        MObjects.SkillEffect mObject = netMsg.ReadMessage<MObjects.SkillEffect>();

        MobileAgent ma = MobileAgent.list.Find(x => x.id == mObject.id);

        if (ma == null)
        {
            return;
        }

        Transform t = skillEffect.Find(x => x.name == mObject.clientPrefab);
        if (t == null)
            return;
        Transform created = Instantiate(t, ma.transform.position + new Vector3(0, t.position.y, 0), t.rotation);
        created.gameObject.AddComponent<ParticleFollow>().target = ma.transform;
        Destroy (created.gameObject,5);
    }

    [HideInInspector]
    public List<Transform> particles = new List<Transform>();
    public void CreateParticle(string pName, Transform target)
    {
        Transform t = particles.Find(x => x.name == pName);
        Transform created = Instantiate(t, target.position + new Vector3(0, t.position.y, 0), t.rotation);
        created.gameObject.AddComponent<ParticleFollow>().target = target;
    }

    public void BotRequest()
    {
        MObjects.BotRequest mObject = new MObjects.BotRequest();
        nc.Send(MTypes.BotRequest, mObject);
    }

    public void OnCooldown(NetworkMessage netMsg)
    {
        MObjects.Cooldown mObject = netMsg.ReadMessage<MObjects.Cooldown>();

        Filler f = skillsHolder.GetChild(mObject.skillId).GetComponentInChildren<Filler>();
        f._image.fillAmount = 1;
        f.speed = 1 / mObject.time;
        f.fillAmount = 0;
    }

    public Transform score_grid;
    public Transform score_teamPrefab;
    public Transform score_playerPrefab;

    public void OnScoreInfo(NetworkMessage netMsg)
    {
        MObjects.ScoreInfo mObject = netMsg.ReadMessage<MObjects.ScoreInfo>();

        ushort c = (ushort)mObject.ids.Length;

        for (ushort i = 0; i < c; i++)
        {
            // find team grid first
            Transform tGrid = score_grid.Find(mObject.teams[i].ToString());
            if (tGrid == null)
            {
                // team not found
                tGrid = Instantiate(score_teamPrefab, score_grid);
                tGrid.name = mObject.teams[i].ToString();
                Color color = new Color(teamColors[mObject.teams[i]].r, teamColors[mObject.teams[i]].g, teamColors[mObject.teams[i]].b, 0.5f);
                tGrid.GetComponent<Image>().color = color;
            }

            // find player prefab

            MobileAgent ma = MobileAgent.list.Find(x => x.id == mObject.ids[i]);

            Transform p = tGrid.Find(mObject.ids[i].ToString());
            if (p == null) {
                // player not found
                p = Instantiate(score_playerPrefab, tGrid);
                p.name = mObject.ids[i].ToString();

                if (ma != null)
                    p.Find("icon").GetComponent<Image>().sprite = icons.Find(x => x.name == ma.clientPrefab);
            }

            p.Find("kills").GetComponent<Text>().text = Language.GetText (46) + " " + mObject.kills[i].ToString();
            p.Find("death").GetComponent<Text>().text = Language.GetText (47) + " " + mObject.deaths[i].ToString();

            UIScoreItem usi = p.GetComponent<UIScoreItem>();
            usi.kills = mObject.kills[i];
            usi.deaths = mObject.deaths[i];

            if (ma != null)
            {
                p.Find("name").GetComponent<Text>().text = ma.alias;
            }
        }

        /*
         * SORT ALL
         */

        // Sort teams first
        ushort e = (ushort)score_grid.childCount;

        short[] teamScores = new short[e];

        for (int i = 0; i < e; i++)
        {
            ushort teamId = ushort.Parse(score_grid.GetChild(i).name);
            // find my scorers
            for (int a = 0; a < c; a++)
            {
                if (mObject.teams[a] == teamId)
                {
                    teamScores[i] += (short)(mObject.kills[a]);
                }
            }

            score_grid.GetChild(i).GetComponentInChildren<Text>().text = teamScores[i].ToString();
        }

        List<Transform> teams = new List<Transform>();
        for (int i = 0; i < e; i++)
            teams.Add(score_grid.GetChild(i));

        teams = teams.OrderByDescending(x => short.Parse(x.GetComponentInChildren<Text>().text)).ToList();

        for (int i = 0; i < e; i++)
            teams[i].SetSiblingIndex(i);

        /*
         * SORT PLAYERS
         * */

        for (int i = 0; i < e; i++)
        {
            Transform team = score_grid.GetChild(i);
            ushort cc = (ushort)team.childCount;

            List<Transform> players = new List<Transform>();
            for (int a = 1; a < cc; a++)
            { // search every member
                players.Add(team.GetChild(a));
            }

            players = players.OrderByDescending(x => x.GetComponent<UIScoreItem>().kills).ThenBy(x => x.GetComponent<UIScoreItem>().deaths).ToList();

            cc = (ushort)players.Count;
            for (int p = 0; p < cc; p++)
                players[p].SetSiblingIndex(p + 1);
        }
    }

    public Transform killInfo_Grid;
    public Transform killInfo_Prefab;
    public void OnKillInfo(NetworkMessage netMsg)
    {
        MObjects.KillInfo mObject = netMsg.ReadMessage<MObjects.KillInfo>();

        MobileAgent ma_killed = MobileAgent.list.Find(x => x.id == mObject.tId);
        MobileAgent ma_died = MobileAgent.list.Find(x => x.id == mObject.id);

        if (ma_died.isCreature || ma_killed.isCreature)
            return; // Not for creatures

        Transform t = Instantiate(killInfo_Prefab, killInfo_Grid);

        Color tColor = (ma_died.team == MobileAgent.user.team) ? Color.red : Color.green;
        tColor.a = 0.5f;
        t.GetComponent<Image>().color = tColor;
        t.Find("diedIcon").GetComponent<Image>().sprite = icons.Find(x => x.name == ma_died.clientPrefab);
        t.Find("killerIcon").GetComponent<Image>().sprite = icons.Find(x => x.name == ma_killed.clientPrefab);
        t.Find("diedAlias").GetComponent<Text>().text = ma_died.alias;
        t.Find("killerAlias").GetComponent<Text>().text = ma_killed.alias;
    }

    public UIVisibility panel_Rankings;

    public static bool SessionEnd = false;
    public void OnSessionEnd(NetworkMessage netMsg)
    {
        SessionEnd = true;
        panel_Rankings.myCanvas.alpha = 1;
        panel_Rankings.GetComponent<UIVisibility>().enabled = false;
        Debug.Log("Session end");

        foreach (MobileAgent ma in MobileAgent.list)
            ma.Stop();
    }

    public UIVisibility panel_Round_Complete;
    public void OnRoundComplete(NetworkMessage netMsg)
    {
        panel_Round_Complete.Open();
    }

    public UIVisibility panel_skillTooltip;
    public Text skillTooltip_CoreText;
    public Text skillTooltip_ProText;

    public UIVisibility panel_levelTooltip;
    public Text levelTooltip_CoreText;
    public Text levelTooltip_ProText;

    public MObjects.SkillInfo mObject;

    public void OnSkillInfo(NetworkMessage netMsg)
    {
        mObject = netMsg.ReadMessage<MObjects.SkillInfo>();
        Invoke("UpdateSkillTooltip", 0.5f);
    }

    void UpdateSkillTooltip()
    {
        string step = "\r\n";
        int sCount = mObject.skills.Length;
        for (int i = 0; i < sCount; i++)
        {
            UISkillItem usi = skillsHolder.GetChild(i).GetComponent<UISkillItem>();
            System.Text.StringBuilder core = new System.Text.StringBuilder();
            System.Text.StringBuilder pro = new System.Text.StringBuilder();

            usi.skillInfo = mObject.skills[i];

            core.Append(Language.GetText(skills.Find(x => x.name == mObject.skills[i].clientPrefab).GetComponent<Language>().textId) + step);
            pro.Append(step);

            if (mObject.skills[i].effect > 0)
            {
                //Append SKILLTYPE
                core.Append(Language.GetText(32) + step + step);

                switch (mObject.skills[i].skillType)
                {
                    case MObjects.SkillType.Area:
                        pro.Append(Language.GetText(34) + step + step);
                        break;
                    case MObjects.SkillType.Self:
                        pro.Append(Language.GetText(66) + step + step);
                        break;
                    default:
                        pro.Append(Language.GetText(33) + step + step);
                        break;
                }

                core.Append(step + Language.GetText(27));
                pro.Append(step + mObject.skills[i].effect);

            }

            core.Append(step + Language.GetText(28));
            pro.Append(step + ((mObject.skills[i].casttime > 0) ? (mObject.skills[i].casttime + " " + Language.GetText(31)) : Language.GetText(29)));

            core.Append(step + Language.GetText(30));
            pro.Append(step + mObject.skills[i].cooldown + " " + Language.GetText(31));

            if (mObject.skills[i].moveSpeed > 0)
            {
                core.Append(step + Language.GetText(35));
                pro.Append(step + mObject.skills[i].moveSpeed + " " + Language.GetText(36));
            }

            if (mObject.skills[i].life > 0)
            {
                core.Append(step + Language.GetText(37));
                pro.Append(step + mObject.skills[i].life + " " + Language.GetText(31));
            }

            if (mObject.skills[i].effectTime > 0)
            {
                core.Append(step + Language.GetText(67));
                pro.Append(step + mObject.skills[i].effectTime + " " + Language.GetText(31));
            }

            if (mObject.skills[i].collision > 0)
            {
                //Append Hit Radius
                core.Append(step + Language.GetText(38));
                pro.Append(step + mObject.skills[i].collision + " " + Language.GetText(39));
            }

            if (mObject.skills[i].hitAndDestroy)
            {
                core.Append(step+step + Language.GetText(40));
            }

            if (mObject.skills[i].hitContinous)
            {
                core.Append(step + step + Language.GetText(41));
            }

            usi.core = core;
            usi.pro = pro;
        }
    }

    public Transform mapHolder;
    public Transform mapPrefab;
    bool mapsReceived = false;

    public void OnMapInfo(NetworkMessage netMsg)
    {
        MObjects.MapInfo mObject = netMsg.ReadMessage<MObjects.MapInfo>();

        int iCount = mObject.langId.Length;
        for (int i=0; i<iCount; i++)
        {
            string iS = i.ToString();
            Transform tt = mapHolder.Find(iS);
            if (tt == null)
            {
                tt = Instantiate(mapPrefab, mapHolder);
                tt.name = iS;
                tt.Find("Text").GetComponent<Text>().text = Language.GetText(mObject.langId[i]);
            }

            tt.Find("online").GetComponent<Text>().text = mObject.players[i].ToString();
            tt.GetComponent<UIMapItem>().online = mObject.players[i];
        }

        if (!mapsReceived)
        {
            mapsReceived = true;
            /*
            * MAPS ORDER BY
            * */

        UIMapItem.list = UIMapItem.list.OrderByDescending(x => x.online).ToList();
            int f = UIMapItem.list.Count;
            for (int i = 0; i < f; i++)
            {
                UIMapItem.list[i].transform.SetSiblingIndex(i);
            }

            /*
             * */

            SelectMap(0);
        }

        panel_MapLoading.SetActive(false);
        button_FindGame.SetActive(true);
    }

    public static int selectedMap;

    public void SelectMap(int index)
    {
        int f = UIMapItem.list.Count;
        for (int i = 0; i < f; i++)
        {
            UIMapItem.list [i].transform.Find ("selected").gameObject.SetActive ((i == index));
        }

        selectedMap = index;
    }

    public Transform UIBuff;
    public List<Transform> buffs = new List<Transform>();
    public void OnAgentBuff(NetworkMessage netMsg)
    {
        MObjects.AgentBuff mObject = netMsg.ReadMessage<MObjects.AgentBuff>();
        MobileAgent ma = MobileAgent.list.Find(x => x.id == mObject.id);

        if (ma != null)
        {
            int bCount = mObject.buffs.Length;

            List<string> received = new List<string>();
            for (int i = 0; i < bCount; i++)
            {
                string buffName = mObject.buffs[i].buffType.ToString();

                received.Add(buffName);

                Transform icon = null;
                if (ma.buffs.Find(x => x.name == buffName) == null)
                {
                    // buff not found, creating
                    Transform pref = buffs.Find(x => x.name == buffName);
                    if (pref != null)
                    {
                        Transform inst = Instantiate(pref);
                        inst.gameObject.AddComponent<ParticleFollow>().target = ma.transform;
                        ma.buffs.Add(inst);
                        inst.name = buffName;

                        /*
                         * CREATE ICON
                         * */

                        icon = Instantiate(UIBuff, ma.panel.GetChild(0));
                        icon.GetComponent<Image>().sprite = icons.Find(x => x.name == buffName);
                        icon.name = buffName;
                        
                        /*
                         * */
                    }
                }

                if (icon == null)
                    icon = ma.panel.GetChild(0).Find(buffName);

                if (icon != null)
                {
                    Filler f = icon.GetComponentInChildren<Filler>();
                    if (mObject.buffs[i].buffTime > 0)
                    {
                        f.speed = (1f - f._image.fillAmount) / mObject.buffs[i].buffTime;
                    }
                    else f.speed = 0;

                    icon.GetComponentInChildren<Text>().text = mObject.modified[i].ToString ();
                }
            }

            List<Transform> willRemove = new List<Transform>();
            bCount = ma.buffs.Count;

            for (int i = 0; i < bCount; i++)
            {
                if (!received.Contains(ma.buffs[i].name))
                {
                    willRemove.Add(ma.buffs[i]);
                }
            }

            bCount = willRemove.Count;
            for (int i = 0; i < bCount; i++)
            {
                /*
                 * DESTROY ICON
                 * */

                Destroy (ma.panel.GetChild(0).Find(willRemove[i].name).gameObject);
                /*
                 * */
                ma.buffs.Remove(willRemove[i]);
                Destroy(willRemove[i].gameObject);
            }

            /*
             * ANIMATOR DAMAGER OBJECT
             * */

            int parameterCount = ma.anim.parameterCount;

            for (int i = 0; i < parameterCount; i++)
            {
                AnimatorControllerParameter param = ma.anim.GetParameter(i);
                if (param.name == "DamagerObject")
                {
                    // The animator has "DamagerObject". Lets check if DamagerObject buff exist.
                    ma.anim.SetBool("DamagerObject", (mObject.buffs.ToList().Find(x => x.buffType == Buffing.buffTypes.DamagerObject) != null));

                    break;
                }
            }

            /*
             */
        }
    }

    public void OnTeleport(NetworkMessage netMsg)
    {
        MObjects.Teleport mObject = netMsg.ReadMessage<MObjects.Teleport>();
        MobileAgent ma = MobileAgent.list.Find(x => x.id == mObject.id);

        if (ma != null)
        {
            mObject.pos.y = MapLoader.GetHeight(mObject.pos);
            ma.transform.position = mObject.pos;

            /*
             * TELEPORT EFFECT
             * */

            CreateParticle("Teleport", ma.transform);
        }
    }

    public Transform hookEffect;
    public void OnHook(NetworkMessage netMsg)
    {
        MObjects.Hook mObject = netMsg.ReadMessage<MObjects.Hook>();
        MobileAgent from = MobileAgent.list.Find(x => x.id == mObject.from);
        MobileAgent to = MobileAgent.list.Find(x => x.id == mObject.to);

        HookScript hs = Instantiate(hookEffect, to.transform.position, Quaternion.LookRotation(from.transform.position - to.transform.position)).GetComponent<HookScript>();
        hs.to = to;
        hs.from = from;
        /*
         * MOVE FROM TO TO
         * */
    }
}
