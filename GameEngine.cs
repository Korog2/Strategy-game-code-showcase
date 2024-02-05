using GameProj.Map;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using GameProj.Generators;
using static GameProj.Generators.MapGenerator;
using GameProj.Collections;
using GameProj.Entities;
using System;
using UnityEngine.EventSystems;
using System.IO;
using System.Xml.Serialization;
using GameProj.Options;
using GameProj.UI;
using GameProj.Items;
using GameProj.Area;
using System.Threading;
using GameProj.Events;
using Assets.Scripts.Collections;
using Assets.Scripts.Map;
using GameProj.ScenarioS;

#if UNITY_EDITOR
using UnityEditor;
#endif
//struct MapLayer
//{
//    public const int ground = 0;
//    public const int playerFlag = 1;
//    public const int army = 2;
//    public const int combatIndicator = 3;
//    public const int selection = 4;
//}

public class GameEngine : MonoBehaviour
{
 
    //this class = GameEngine, Map = ActiveGame
    public static void QuitApplication()
    {
        GameEngine.ActiveGame.DisconnectProtocol();
        //if (GameEngine.ActiveGame.clientManager.multiplayer != null)
        //{
        //    GameEngine.ActiveGame.clientManager.multiplayer.CloseSockets(MultiplayerMessage.Pressed_Disconnect);
        //    GameEngine.ActiveGame.clientManager.multiplayer = null;
        //}
        //if (GameEngine.ActiveGame.optionPanel.mpServer != null)
        //{
        //    GameEngine.ActiveGame.optionPanel.mpServer.serverOn = false;
        //    Thread.Sleep(100); //give a bit of time for the server
        //    GameEngine.ActiveGame.optionPanel.mpServer = null;
        //}
        if (Application.isEditor)
        {
            //UnityEditor.EditorApplication.isPlaying = false;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
        else
        {
            Application.Quit();
        }
    }
    public static bool IsUIInitilized = false;
    public Text taskStatusOutput;
    internal bool started = false; //using this bool in taskstatusoutput controller to indicate when to debug log chatToChannels, set to true when option panel is gone
    internal bool serverON = true;
    private System.Diagnostics.Stopwatch gameStopwatch;
    private System.Diagnostics.Stopwatch scenarioStopWatch;
    /// <summary>
    /// chat method
    /// </summary>
    /// <param name="v"></param>
    /// <param name="chatText"></param>
    internal void ProcessChatCommand(string v, Text chatText)
    {
        string[] args = v.Split(' ');
        string command = args[0];
        switch (command)
        {
            case "/mpUI":
                chatText.text += Environment.NewLine +"MP UI commands count: " + GameEngine.ActiveGame.MultiplayerUICommands.Count;
                break;
            case "/testmsg":
           
                foreach (Player plr in scenario.GetLocalPlayers())
                {
                    PlayerController playerController = FindPlayerController(plr.PlayerID);
                    GameEngine.ActiveGame.DisplayOverlandMessageToPlayersOnSquare("test", new List<string>(), 255, 255, 255, new List<SkillParticle>(), plr.CapitalLocation.XCoordinate, plr.CapitalLocation.YCoordinate, plr, playerController, plr.MapMemory);
                }
                break;
            case "/threads":
                GameEngine.ActiveGame.threadController.DisplayThreadInfo();
                break;
            case "/disconnect":
                GameEngine.ActiveGame.DisconnectProtocol();
                GameEngine.ActiveGame.menuController.EscapePressInsideTheGame();
                break;
            case "/mptest":
                MultiplayerMessage multiplayerMessage2 = new MultiplayerMessage(MultiplayerMessage.StartAfterBattleProcessingForPlayer, "test", "");
                GameEngine.ActiveGame.clientManager.Push(multiplayerMessage2);
                break;
            case "/defeat":
                try
                {
                    string plrID = args[1];
                    
                    scenario.SetPlayerAsDefeated(plrID);
                }
                catch
                {

                    chatText.text += Environment.NewLine + "no player with id: " + args[1];
                }
              

                break;
            case "/addunit":
                int armyid = 0;
                try
                {
                     armyid = Int32.Parse(args[1]);
                }
                catch
                {
                    chatText.text += Environment.NewLine + "no army with id: " + armyid;
                    return;
                }
                
                string unitkw = args[2];
                CharacterTemplate characterTemplate = GameEngine.Data.CharacterTemplateCollection.findByKeyword(unitkw);
                if (characterTemplate == null)
                {
                    chatText.text += Environment.NewLine + "no unit with kw: " + unitkw;
                    return;
                }
                Army army2 = scenario.FindArmyByID(armyid);
                Entity newEntity = Entity.CreateTemplateChar(unitkw,new MyRandom(100,32),army2.OwnerPlayerID);
                army2.Units.Add(newEntity);
                chatText.text += Environment.NewLine + "added " + unitkw + " " + newEntity.UnitID + " to army: " + army2.ArmyID;
                break;
            case "/clear":
                chatText.text = "";
                break;
            case "/showarmies":
                bool noArmies = true;
                lock (scenario.Armies)
                {
                    foreach (Army army in scenario.GetAllArmies())
                    {
                        chatText.text += Environment.NewLine + army.GetInformation() + " " + army.GetHostileListInformation();
                        noArmies = false;
                    }
 
                }
                if (noArmies)
                {
                    chatText.text += Environment.NewLine + "no armies";
                }
                break;
            case "/showactivebattles":
                bool noActivaBattles = true;
                lock (scenario.ActiveBattles.Battlefields)
                {
                    foreach (BattlefieldOld battlefield in scenario.ActiveBattles.Battlefields)
                    {
                        chatText.text += Environment.NewLine + " " + battlefield.GetInformation();
                        noActivaBattles = false;
                    }
                }
                if (noActivaBattles)
                {
                    chatText.text += Environment.NewLine + "no active battles";
                }
                break;
            case "/showallbattles":
                bool noActivaBattles1 = true;
                lock (scenario.ActiveBattles.Battlefields)
                {
                    foreach (BattlefieldOld battlefield in scenario.ActiveBattles.Battlefields)
                    {
                        chatText.text += Environment.NewLine + " " + battlefield.GetInformation();
                        noActivaBattles = false;
                    }
                }
                if (noActivaBattles1)
                {
                    chatText.text += Environment.NewLine + "no active battles";
                }
                bool noContiniousBattles = true;
                bool noQueuedBattles = true;
                lock (scenario.BattlesToBeContinued.Battlefields)
                {
                    foreach (BattlefieldOld battlefield in scenario.BattlesToBeContinued.Battlefields)
                    {
                        chatText.text += Environment.NewLine + " " + battlefield.GetInformation();
                        noContiniousBattles = false;
                    }
                }
                if (noContiniousBattles)
                {
                    chatText.text += Environment.NewLine + "no continious battles";
                }
                lock (scenario.QueuedUpBattles.Battlefields)
                {
                    foreach (BattlefieldOld battlefield in scenario.QueuedUpBattles.Battlefields)
                    {
                        chatText.text += Environment.NewLine + " " + battlefield.GetInformation();
                        noQueuedBattles = false;
                    }
                }
                if (noQueuedBattles)
                {
                    chatText.text += Environment.NewLine + "no queued battles";
                }
                break;
            default:
                if (command.Length > 0)
                {
                    if (command[0] == '/')
                    {
                        chatText.text += "Unknown command: " + command;
                    }
                }
                break;
        }
        chatText.text += Environment.NewLine;
    }

    public MenuScript menuController;

    public GameObject playersGameObject; //object in scene, where players are put
    public GameObject playerPrefab; //instantiated prefab
    internal List<GameObject> playerControllersList = new List<GameObject>();
    public PlayerControllerSwitcher playerControllerSwitcher;
    public GameObject combatMapGeneratorPanel;
    public OptionPanelController optionPanel;
    public FlashWindowInUnity flashWindowController;
    public static Server Server { get => GameEngine.ActiveGame.optionPanel.server; set => GameEngine.ActiveGame.optionPanel.server = value; }
    //this static uses internal because public makes this go mad
    internal static Multiplayer Multiplayer { get => GameEngine.ActiveGame.optionPanel.multiplayer; set => GameEngine.ActiveGame.optionPanel.multiplayer = value; }
    //internal static Multiplayer Multiplayer { get => GameEngine.ActiveGame.optionPanel.multiplayer; set => GameEngine.ActiveGame.optionPanel.multiplayer = value; }

    public static CollectionMix Data { get => GameEngine.ActiveGame.CollectionMixData; set => GameEngine.ActiveGame.CollectionMixData = value; }

    public static MyRandom random = new MyRandom();

    public ClientManager clientManager;

    public ThreadManager threadController;

    internal List<TaskStatus> hostTasks = new List<TaskStatus>();

    public static readonly int GAME_PORT = 5321;

    public static readonly string PLAYER_IDENTITY = Environment.MachineName;

    internal string PasswordUsed = "";

 
    public List<MultiplayerUICommand> MultiplayerUICommands = new List<MultiplayerUICommand>();
   

    public OurMapSquareList path = null;

    internal List<string> clientsReadyToStartTurn = new List<string>();

    MapSquare targetMovementSquare;
    bool isSelected;
    internal bool blink = false;
    bool canMoveArmy;

    internal int amountOfPlayers = 3;
    internal ModManager modManager;
    // public Tilemap flags;
    internal Scenario scenario;
    private CollectionMix collectionMix;
    private float waitTime = 0.3f;
    internal float timer = 0.0f;
    private float visualTime = 0.0f;
 
    private string scenarioFolderPath = "/Scenarios/"; // Different Scenarios (Game modes), we will have 1 for sure, we might have more
    private string dataFolderPath = "/Data/";
    private string optionGeneralFolderPath = "/OPTIONS/General/";
    private string optionFolderPath = "/OPTIONS/";
    private string playerProfilesPath = "/PlayerProfiles/"; // all saves should go under profiles in future
    private string saveFolderPath = "/Saves/";

    internal string scenariosPath = "";
    internal string generalOptionsPath = "";
    internal string optionsPath = "";
    internal string profilesPath = "";
    internal string modDataPath = "";
    internal string baseDataPath = "";
    internal string baseDataOptionsPath = "";
    internal string savesPath = "";
    internal string battlefieldSetupsPath = "";
    internal string scenario_savegamesPath = "";
    internal string battlefield_savegamesPath = "";
    internal string randomMapGamemodePath = "";
    internal string campaignGamemodePath = "";
    internal string savedScenarioOptionName = "Options";
    GeneralOptions generalOptions;
    public GeneralOptions GeneralOptions
    {
        get { return generalOptions; }
        set { generalOptions = value; }
    }
    public static Player ActivePlayer {

        get {
            return GameEngine.ActiveGame.scenario.FindPlayerByID(GameEngine.ActiveGame.scenario.ActivePlayerID);
        }
    }

    public static WorldMap Map
    {

        get
        {
            return GameEngine.ActiveGame.scenario.Worldmap;
        }
    }

    public static GameEngine ActiveGame;

    public CollectionMix CollectionMixData { get => collectionMix; set => collectionMix = value; }
    public System.Diagnostics.Stopwatch GameStopwatch { get => gameStopwatch; set => gameStopwatch = value; }
    public System.Diagnostics.Stopwatch ScenarioStopWatch { get => scenarioStopWatch; set => scenarioStopWatch = value; }
 
    public bool isHost
    {

        get
        {
            return GameEngine.ActiveGame.optionPanel.isHost;
        }
    }


    private void Awake()
    {
   
        string rootPath = Application.persistentDataPath;

        ActiveGame = this;
        
        gameStopwatch = new System.Diagnostics.Stopwatch();
        scenarioStopWatch = new System.Diagnostics.Stopwatch();
        Debug.Log(rootPath);
        CreateFileDirectories(rootPath);

        SetData();
        ApplyGeneralOptions();
        //collectionMix = new CollectionMix(1, rootPath + dataFolderPath, optionsPath, this.modManager);
        //collectionMix.LoadOptions(scenariosPath);
        ////collectionMix.saveTemplateCollections(rootPath + dataFolderPath);
        //collectionMix.saveTemplateCollections(baseDataPath, optionsPath);
    }

    /// <summary>
    /// this method will set things from default to whatever was in general options(doesnt apply to all the options)
    /// </summary>
    public void ApplyGeneralOptions()
    {
        ChangeLanguage(this.GeneralOptions.Language.StringValue);
        SetFrameRate(this.GeneralOptions.TargetFrameRate.IntValue);
        SetIpDropdowns(this.GeneralOptions.SavedIPAdresses);
    }

    void SetIpDropdowns(List<string> savedipadress)
    {
        if (menuController != null)
        {
            ConnectToHostPanelController connectToHost = menuController.connectToServerPanel.GetComponent<ConnectToHostPanelController>();
            connectToHost.dropdown.ClearOptions();
            connectToHost.dropdown.AddOptions(savedipadress);
        }
    
    }

    public void SetFrameRate(int frameRate)
    {
        Application.targetFrameRate = frameRate;
    }
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="argument"></param>
    /// <exception cref="NotImplementedException"></exception>
    internal void ResolveEventCommand(string argument)
    {
        try
        {
            string[] args = argument.Split('*');
            Debug.Log("args count: " + args.Length + " full argument " + argument);
            string eventKeyword = args[0];
            string playerID = args[1];
            bool automatically = bool.Parse(args[2]);
            Player player = scenario.FindPlayerByID(playerID);
            if (player == null)
            {
                Debug.LogError("ResolveEventCommand player is null: "+ playerID);
            }
            EventChain eventChain = player.EventChains[0];
            if (eventChain == null)
            {
                Debug.LogError("ResolveEventCommand eventchain is null: " + playerID);
            }
            GameProj.Events.Event currentEvent = eventChain.Events.getCurrentEvent();
            if (currentEvent == null)
            {
                Debug.LogError("ResolveEventCommand currentEvent is null: " + playerID + " events count " + eventChain.Events.Count());
            }
            EventCommand command = currentEvent.returnCurrentCommand();
            if (command == null)
            {
                Debug.LogError("ResolveEventCommand command is null: " + playerID + " event commands count, nextcommand: " + currentEvent.NextCommand + " currentevent kw " + currentEvent.TemplateKeyword);
            }
            EventTemplate template = GameEngine.Data.EventTemplateCollection.findByKeyword(eventKeyword);
            if (template == null)
            {
                Debug.LogError("ResolveEventCommand template is null: " + playerID + " with keyword " + eventKeyword);


            }
            EventChain.resolveEventCommands(command, template, player, currentEvent, automatically, null, null, eventChain, null, null, true, null, false);

            //safe bet to refresh specific player
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.COMMAND_REFRESH_UI_SPECIFIC_PLAYER, playerID));

        }
        catch (Exception e)
        {

            Debug.LogError("ResolveEventCommand" + e.Message + " " + e.StackTrace);
        }
    }

    public void SetData()
    {
        string rootPath = Application.persistentDataPath;
       
       
        collectionMix = new CollectionMix(1, rootPath + dataFolderPath, optionsPath, this.modManager);
        collectionMix.LoadOptions(scenariosPath);
        //collectionMix.saveTemplateCollections(baseDataPath, optionsPath);
    }
    /// <summary>
    /// Multiplayer method
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="battlefieldIDStr"></param>
    /// <param name="isHost"></param>
    internal void SetPlayerAutoBattle(string playerID, string battlefieldIDStr,bool isHost)
    {
        try
        {
            int battlefieldID = Int32.Parse(battlefieldIDStr);
            BattlefieldOld battlefieldOld = scenario.FindBattleByID(battlefieldID);
            CombatMapMemory playerMapMemory = battlefieldOld.GetPlayerCombatMapMemory(playerID);
            playerMapMemory.isAutoBattle = true;
            if (isHost)
            {
                if (battlefieldOld.IsPlayersTurn(playerID))
                {
                    Thread newStartAI = new Thread(() => battlefieldOld.NewStartAI(playerID));
                    newStartAI.IsBackground = true;
                    newStartAI.Name = "New Start AI from Auto battle";
                    newStartAI.Start();
                }

            }
        }
        catch (Exception e)
        {

            Debug.LogError("SetPlayerAutoBattle error: " + e.Message + " " + e.StackTrace);
        }

    }

    /// <summary>
    /// multiplayer method
    /// host recieves this first, then sends to others
    /// </summary>
    /// <param name="argument"></param>
    /// <exception cref="NotImplementedException"></exception>
    internal void ResolveEventClaim(string playerID, string args,bool host)
    {
        try
        {
            //restoring functionality for player, if the player is doing event(right after these lines) then we simply dont cares
            Player player = scenario.FindPlayerByID(playerID);
            player.GameState.GetUIPermissionByObject(GameState.Object.OVERLAND_EVENT_CHOICE_BUTTON).Permission = GameState.Permission.OVERLAND_EVENT_CHOICE_BUTTON_ENABLED;
            player.GameState.GetUIPermissionByObject(GameState.Object.OVERLAND_RIGHT_CLICK).Permission = GameState.Permission.OVERLAND_RIGHTCLICK_ALLOW;

            int gameSquareID = Int32.Parse(args.Split('*')[0]);
            int overlandEventID = Int32.Parse(args.Split('*')[1]);
            GameSquare gameSquare = scenario.Worldmap.FindGameSquareByID(gameSquareID);
            if (gameSquare.GetOverlandEventByID(overlandEventID) != null) //if is null, means someone has claimed this event already
            {
                if (host) //send mp message for others to call this with host = false
                {
                    //send mp message for ResolveEventClaim host = false(using playerID)
                    MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.ResolvedEventClaim,playerID, args);
                    GameEngine.ActiveGame.clientManager.Push(multiplayerMessage);
                }
                scenario.TriggerGameSquareEvent(gameSquare,overlandEventID, playerID, false);
            }

            scenario.RemoveArmyEventFlag(new MapCoordinates(gameSquare.X_cord,gameSquare.Y_cord));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_EVENTS_ON_SQUARE,gameSquare.X_cord.ToString(),gameSquare.Y_cord.ToString()));
        }
        catch (Exception e)
        {

            Debug.LogError("ResolveEventClaim " + e.Message + " " + e.StackTrace);
        }
    }

    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="playerID"></param>
    /// <exception cref="NotImplementedException"></exception>
    internal void ContinuePlayerDungeon(string playerID)
    {
        try
        {
            Player player = scenario.FindPlayerByID(playerID);
            player.DungeonQueue.RemoveAt(0);
            //GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand());
        }
        catch (Exception e)
        {

            Debug.LogError("ContinuePlayerDungeon " + e.Message + " " + e.StackTrace);
        }
       
    }



    /// <summary>
    /// using data.optioncollection as it is already correctly load one(file or constructor)
    /// </summary>
    public void SaveOptionsAfterStartGame()
    {
        // using optioncollection keyword as folder, because the folder have that name
        string optionpath = scenariosPath + Data.OptionCollection.Keyword + "/" + OptionCollection.XMLTEXTFILE;
        Debug.Log("saving last collection: " + optionpath);
        OptionsMyValue optionsMyValue = Data.OptionCollection.FindByKeyword(OptionCollection.PlayerCount).Values.findDefaultMyValue();
        optionsMyValue.Value = this.amountOfPlayers.ToString();
      
        Debug.Log("player amount saved: " + Data.OptionCollection.FindByKeyword(OptionCollection.PlayerCount).Values.findDefaultMyValue().Value);
        OptionCollection.save(Data.OptionCollection, optionpath);
        

    }


    public void AwakeMethod()
    {
        //collectionMix = new CollectionMix();
       
    }
    public delegate void LanguageChange();
    public static event LanguageChange OnLanguageChange;
    internal string selectedLanguage = LanguageBase.ENGLISH;

    public void ChangeLanguage()
    {
        if (OnLanguageChange != null)
            OnLanguageChange();
    }

    public string GetSelectedLanguage()
    {
        return selectedLanguage;
    }

    public void SetSelectedLanguage(string lang)
    {
        Debug.Log("setting language to: " + lang);
        selectedLanguage = lang;
    }
    /// <summary>
    /// host recieves player id
    /// </summary>
    /// <param name="playerID"></param>
    public void SubmitEndTurn(string playerID)
    {
        bool startCountdownTask = false;
        bool cancelCountdown = false;
        lock (scenario.PlayersWhoEndedTurn)
        {
            if (scenario.PlayersWhoEndedTurn.Contains(playerID))
            {
                scenario.PlayersWhoEndedTurn.Remove(playerID); //cancel end turn
                Debug.Log("1 glib + " + scenario.PlayersWhoEndedTurn.Count + " plr id " + playerID);
                cancelCountdown = true; 
            }
            else
            {
                scenario.PlayersWhoEndedTurn.Add(playerID);
            }
            if (scenario.PlayersWhoEndedTurn.Count == scenario.GetPlayerCount()) //all players pressed end turn, start countdown
            {
                startCountdownTask = true;
                Debug.Log("2 glib + " + scenario.PlayersWhoEndedTurn.Count);
            }
        }

        if (startCountdownTask)
        {
            AddHostTask("", TaskStatus.TYPE_END_TURN_COUNTDOWN);
            Debug.Log("starting countdown ");
        }

        if (cancelCountdown)
        {
            SetHostTaskSet("",TaskStatus.TYPE_END_TURN_COUNTDOWN,TaskSet.SET_CANCEL);
            Debug.Log("cancelling countdown ");
        }
       
    }

    public void SetHostTaskSet(string incClientName, string taskName, string taskSet)
    {
    
        lock (hostTasks)
        {
            foreach (TaskStatus task in hostTasks)
            {
                if (task.computerID == incClientName && task.taskType == taskName)
                {
                    task.taskSetSelection = taskSet;
                    return;
                }
            }
        
        }
        Debug.LogError("could not find task: " + taskName + " with client name: " + incClientName);
    }

    public void AddHostTask(string incClientName, string taskName)
    {
        TaskStatus taskStatus = TaskStatus.StartTask(incClientName, taskName);
        lock (hostTasks)
        {
            hostTasks.Add(taskStatus);
        }
    }
    public List<string> GetLanguages()
    {
        List<string> languages = new List<string>();
        languages.Add(LanguageBase.ENGLISH);
        languages.Add(LanguageBase.ESTONIAN);
        languages.Add(LanguageBase.RUSSIAN);


        return languages;
    }
 

    /// <summary>
    /// creates AI clients, that connect from host side to the server
    /// 8.02.2023: now this method just says which players are AI and which are not
    /// </summary>
    /// <param name="playerSetups"></param>
    public void CreateAIClients(List<Player> playerSetups)
    {
        int aiNameCounter = 1;
        bool debug = true;
        if (debug)
        {
            Debug.Log("CreateAIClients start");
        }

        foreach (Player player in playerSetups)
        {
            PlayerSetup playerSetup = scenario.GetPlayerSetupByPlayerID(player.PlayerID);

            MySocket playerSocket = Multiplayer.FindSocketByPlayerID(player.PlayerID);

            if (playerSetup == null) //special player(neutral etc)
            {
                player.isAI = true;
                continue;
            }
            else
            {
                //wasnt assigned slot, therefore AI
                if (playerSetup.ComputerName == "")
                {
                    player.isAI = true;
                }
            }
  
        }
        //return on the older code for now
        return;

        foreach (Player player in playerSetups)
        {
            //getting the existing serverClient. If client exists, that means it is a player that connected
            //earlier in options menu
            if (debug)
            {
                PlayerSetup playerSetup = scenario.GetPlayerSetupByPlayerID(player.PlayerID);
                if (playerSetup != null)
                {
                    Debug.Log("checking setup for computer: " + playerSetup.ComputerName + " player " + playerSetup.PlayerName);
                }
                else //means that the played wasn't created with player setups, but after(neutrals, for example)
                {
                    Debug.Log("checking for non-setup player: " + player.PlayerID);
                }
               
            }
            if (Server == null)
            {
                Debug.LogError("wtf");
            }
            ServerClient existingPlayerClient = Server.GetClientByPlayerName(player.PlayerID);
            //ServerClient existingPlayerClient = Server.GetClientByName(setup.ComputerName);
            //if doesnt exist, then it shall be AI
            if (existingPlayerClient == null)
            {
                if (debug)
                {
                    Debug.Log("no client found, creating AI client");
                }
                Client client;
                client = Instantiate(optionPanel.clientPrefab).GetComponent<Client>();
                //client.optionsPanelText = serverIncomingText;
                client.isHost = false;
                client.isAI = true;
                client.playerName = player.PlayerID; //g
                //connecting the client to the server
                if (client.ConnectToServer(Client.GetLocalIPAddress(), 6321))
                {
                    //if connection is successful, then we need to get serverClient by playername and change it's clientName,
                    //because by default its the host machine, so here we override it with ai name
                    //actually idk if serverclient clientname and playername are relevant outside optionspanel(as it seems)
                    //ServerClient thisClient = Server.GetClientByPlayerName(player.PlayerID);
                    //thisClient.isAI = true;

                    ServerClient aiClient = Server.clients[Server.clients.Count-1];
                    Debug.Log("potential AIclient?: plr name " + aiClient.playerName + " comp " + aiClient.clientName + " to plr " + player.PlayerID );
                    aiClient.playerName = player.PlayerID;
                    aiClient.isAI = true;
                    //aiNameCounter++;

                    //if (debug)
                    //{
                    //    Debug.Log("AI client created for player " + thisClient.playerName + " " + thisClient.clientName);
                    //}
                    if (debug)
                    {
                        Debug.Log("AI client created&connected for player " + client.playerName);
                    }
                }
                else
                {
                    Debug.LogError("failed to connect ai player");
                }

            }
            else
            {
                if (debug)
                {
                    Debug.Log("matched serverClient found: " + existingPlayerClient.clientName + " player " + existingPlayerClient.playerName);
                }
            }


        }
    }
    /// <summary>
    /// multiplayer method
    /// </summary>
    internal void ProceedToMainPhase()
    {
        bool debug = false;
        foreach (Player player in scenario.Players)
        {
            player.GameState = GameEngine.Data.GameStateCollection.findByKeyword(GameState.State.MAIN_PHASE);
            //!!! refresh UI here!!!, will implement multiplayer on this a bit later, should be ez
            //GameObject controllerObject = GameEngine.ActiveGame.FindPlayerControllerGameObject(player.PlayerID);
            //if (controllerObject != null)
            //{
            //    PlayerController playerController = controllerObject.GetComponent<PlayerController>();
            //    playerController.RefreshUI();
            //}

            if (debug)
            {
                Debug.Log(player.PlayerID + " gamestate: " + player.GameState.Keyword);
            }

        }
        if (GameEngine.ActiveGame.isHost)
        {
            GameEngine.ActiveGame.StartAI("");
        }
    }

    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="message"></param>
    internal void SkillClick(string playerID, string message)
    {
        try
        {
            string[] args = message.Split('*');
            int entityID = Int32.Parse(args[0]);
            string skillKeyword = args[1];
            int battlefieldID = Int32.Parse(args[2]);
            BattlefieldOld battlefield = scenario.ActiveBattles.FindBattleByID(battlefieldID);
            battlefield.SetEntityPointer(playerID, entityID, skillKeyword);

            //doing this here as this is a click, and error checks would prevent skill registration
            battlefield.RegisterSkillTreeUsed(entityID, skillKeyword, 1);

            battlefield.nextResolve(playerID, false, true, false,battlefield.BattlefieldRandom,null);
         
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_COMBAT_UI, battlefieldID.ToString()));
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
     
    }
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="playerIDs"></param>
    /// <param name="battlefieldid"></param>
    internal void AcceptPeace(string playerIDs, string battlefieldid)
    {
        try
        {
            string playerID = playerIDs.Split('*')[0];
            string otherPlayerID = playerIDs.Split('*')[1];

            int battlefieldID = Int32.Parse(battlefieldid);
            BattlefieldOld battlefield = scenario.ActiveBattles.FindBattleByID(battlefieldID);
            battlefield.AcceptPeace(playerID,otherPlayerID);
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_COMBAT_UI_PLAYER, battlefieldid, playerID));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_COMBAT_UI_PLAYER, battlefieldid, otherPlayerID));
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
    }
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="playerIDs"></param>
    /// <param name="battlefieldid"></param>
    internal void OfferPeace(string playerIDs, string battlefieldid)
    {
        try
        {
            string playerID = playerIDs.Split('*')[0];
            string otherPlayerID = playerIDs.Split('*')[1];

            int battlefieldID = Int32.Parse(battlefieldid);
            BattlefieldOld battlefield = scenario.ActiveBattles.FindBattleByID(battlefieldID);
            battlefield.OfferPeace(playerID, otherPlayerID);
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_COMBAT_UI_PLAYER, battlefieldid, playerID));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_COMBAT_UI_PLAYER, battlefieldid, otherPlayerID));

        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
    }
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="playerIDs"></param>
    /// <param name="battlefieldid"></param>
    internal void DeclareEnemy(string playerIDs, string battlefieldid)
    {
        try
        {
            string playerID = playerIDs.Split('*')[0];
            string otherPlayerID = playerIDs.Split('*')[1];

            int battlefieldID = Int32.Parse(battlefieldid);

            BattlefieldOld battlefield = scenario.ActiveBattles.FindBattleByID(battlefieldID);

            battlefield.DeclareEnemy(playerID, otherPlayerID);
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_COMBAT_UI_PLAYER, battlefieldid, playerID));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_COMBAT_UI_PLAYER, battlefieldid, otherPlayerID));
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
    }
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="unitID"></param>
    /// <param name="battleid"></param>
    internal void Retreat(string unitID,string battleid)
    {
        try
        {
            int entityID = Int32.Parse(unitID);
            int battleID = Int32.Parse(battleid);
            BattlefieldOld battlefield = scenario.ActiveBattles.FindBattleByID(battleID);
            Entity unit = battlefield.FindUnitByID(entityID, false);
            battlefield.Retreat(unit, true);
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_COMBAT_UI, battleid)); //maybe replace with more precise refresh
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }

    }

    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="message"></param>
    /// <exception cref="NotImplementedException"></exception>
    internal void SkillTargetClick(string playerID, string message)
    {
        try
        {
            string[] args = message.Split('*');
            int battlefieldID = Int32.Parse(args[0]);
            int combatSquareID = Int32.Parse(args[1]);
            BattlefieldOld battlefield = scenario.ActiveBattles.FindBattleByID(battlefieldID);
            CombatSquare sqr = (CombatSquare)battlefield.CombatMap.FindMapSquareByID(combatSquareID);
            battlefield.TargetClick(playerID, sqr);
            battlefield.UpdateVisions(); //maybe not optimal solution, but since this isnt main thread, might as well update all visions for all players

            //not sure what UI to refresh here as some UI calls are made within targetclick
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
    }

    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="argument"></param>
    /// <param name="message"></param>
    /// <exception cref="NotImplementedException"></exception>
    internal void CancelSkillButtonClick(string playerID, string battleID)
    {
        try
        {
            int battlefieldID = Int32.Parse(battleID);
            BattlefieldOld battlefield = scenario.ActiveBattles.FindBattleByID(battlefieldID);
            battlefield.CancelButtonClick(playerID, false);
            //this refresh basically only for observer
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_COMBAT_UI_PLAYER,battleID,playerID));
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
    
        
    }
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="message"></param>
    internal void ClickSkill(string playerID, string message)
    {
        try
        {
            string[] args = message.Split('*');
            int entityID = Int32.Parse(args[0]);
            string skillKeyword = args[1];
            int battleID = Int32.Parse(args[2]);
            BattlefieldOld battlefield = scenario.ActiveBattles.FindBattleByID(battleID);
            battlefield.SetEntityPointer(playerID, entityID, skillKeyword);

            //doing this here as this is a click, and error checks would prevent skill registration
            battlefield.RegisterSkillTreeUsed(entityID, skillKeyword, 1);

            battlefield.nextResolve(playerID, false, true, false,battlefield.BattlefieldRandom,null);
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }

    }
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="battlefieldIDString"></param>
    /// <param name="message"></param>
    internal void CombatUnitMove(string battlefieldIDString, string message)
    {
        try
        {
            string[] args = message.Split('*');
            int destinationCoordX = Int32.Parse(args[0]);
            int destonationCoordY = Int32.Parse(args[1]);
            int unitID = Int32.Parse(args[2]);
            double modifier = Double.Parse(args[3]);
            int battlefieldID = Int32.Parse(battlefieldIDString);
            BattlefieldOld battlefield = scenario.ActiveBattles.FindBattleByID(battlefieldID);
            Entity unit = battlefield.FindUnitByID(unitID,false);
            CombatMapMemory totalMemory = battlefield.GetPlayerCombatMapMemory(unit.FindCurrentOwnerID());
            CombatSquare startSquare = (CombatSquare)battlefield.CombatMap.FindMapSquareByCoordinates(unit.BattlefieldCoordinates.XCoordinate,unit.BattlefieldCoordinates.YCoordinate);
            CombatSquare destination = (CombatSquare)battlefield.CombatMap.FindMapSquareByCoordinates(destinationCoordX, destonationCoordY);
            battlefield.Movement(unit, totalMemory, startSquare, destination, modifier, false);
            //unit.ActivateStatusEffects(EffectFormula.TRIGGER_MOVEMENT, battlefield,null);
            //foreach (string plr in battlefield.GetAllParticipantPlayerIDs())
            //{
            //    if (battlefield.IsPlayerSeeingCoordinates(plr,startSquare.X_cord,startSquare.Y_cord,))
            //    {

            //    }
            //}
            battlefield.UpdateVisions(); //maybe not optimal solution, but since this isnt main thread, might as well update all visions for all players, have to do it again due to acti
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.COMBAT_REFRESH_IF_SEEING_MOVEMENT, new FromToCoordinates(startSquare.X_cord,startSquare.Y_cord, destinationCoordX, destonationCoordY),battlefieldIDString));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_COMBAT_UI_PLAYER,battlefieldIDString,unit.FindCurrentOwnerID()));

        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
       

    }
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="battlefieldId"></param>
    internal void RefreshActiveUnits(string battlefieldId)
    {
        try
        {
            int battlefieldID = Int32.Parse(battlefieldId);
            BattlefieldOld battlefield = scenario.ActiveBattles.FindBattleByID(battlefieldID); //has lock inside
                                                                                               //maybe thread this? idk
            Thread refreshActiveUnitsThread = new Thread(() => battlefield.RefreshActiveUnits(true, 0, true));
            refreshActiveUnitsThread.IsBackground = true;
            refreshActiveUnitsThread.Name = "refresh active units thread";
            refreshActiveUnitsThread.Start();
            
            //battlefield.RefreshActiveUnits(true, 0, true); //refreshUI is allowed because its UI commands

            if (battlefield.HasBattleEnded())
            {
                //notification & un-block heroes in 
            }

        }
        catch (Exception e)
        {

            Debug.LogError(e.Message + e.StackTrace);
        }
 
    }

    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="buildingProductionAndProductionLine"></param>
    /// <param name="buildingIDString"></param>
    internal void SetProductionLineValue(string buildingProductionAndProductionLine, string buildingIDString)
    {
        try
        {
            int buildingID = Int32.Parse(buildingIDString);
            string[] args = buildingProductionAndProductionLine.Split('*');
            int buildingProductionID = Int32.Parse(args[0]);
            int productionLineID = Int32.Parse(args[1]);
            int value = Int32.Parse(args[2]);
            bool refreshUI = bool.Parse(args[3]);


            Building building = scenario.FindBuildingByID(buildingID);
            ProductionLine line = building.FindProductionLineByID(productionLineID);
            line.AllocatedPercentage = value;


            //the message is spammy, so using a bool on last one, this is for observer
            if (refreshUI)
            {
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.COMMAND_REFRESH_UI_SPECIFIC_PLAYER, building.OwnerPlayerID)); //instant crafting could be entity, so full refresh is best choice & for notifications
            }

        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
    }

    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="buildingProduction"></param>
    /// <param name="buildingID"></param>
    internal void SetBuildingProductionToAI(string buildingProduction, string buildingID)
    {
        try
        {
            int buildingId = Int32.Parse(buildingID);
            string[] args = buildingProduction.Split('*');
            int buildingProductionID = Int32.Parse(args[0]);
            int productionLineID = Int32.Parse(args[1]);
            Building building = scenario.FindBuildingByID(buildingId);
            BuildingProduction production = building.FindBuildingProductionByID(buildingProductionID);
            production.IsAiControlled = Boolean.Parse(args[1]);
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_BUILDING_PRODUCTION,buildingProductionID.ToString()));
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
    }
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="newMode"></param>
    /// <param name="buildingID"></param>
    internal void SetBuildingMode(string newMode, string buildingID)
    {
        try
        {
            int buildingId = Int32.Parse(buildingID);
            Building building = scenario.FindBuildingByID(buildingId);
            building.Mode = newMode;
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_BUILDING,buildingID));
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
    }

    /// <summary>
    /// multiplayer method
    /// </summary>
    internal void CheckIfAllFinishedCombat()
    {
        try
        {
            foreach (Player player in scenario.Players)
            {
                if (player.GameState.Keyword != GameState.State.AFTER_BATTLE_PHASE)
                {
                    return;
                }
            }
            Debug.Log("we proceed to after battles processing");
            //if didnt return we proceed to after battles
            MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.ProceedToAfterBattles, "", "");
            GameEngine.ActiveGame.clientManager.Push(multiplayerMessage);

            //we proceed to main turn instead?
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }

    }
    /// <summary>
    /// multiplayer method
    /// </summary>
    public void ProceedToAfterBattles()
    {
        Thread afterBattlesThread = new Thread(scenario.AfterBattles);
        afterBattlesThread.IsBackground = true;
        afterBattlesThread.Name = "after battles thread";
        afterBattlesThread.Start();
    }

    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="argument"></param>
    /// <param name="aFTER_BATTLE_PHASE"></param>
    internal void SetPlayerGameState(string playerID, string gameStateKW)
    {
        try
        {
            Player player = scenario.FindPlayerByID(playerID);

            player.GameState = GameEngine.Data.GameStateCollection.findByKeyword(gameStateKW);
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
       
    }

    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="playerID"></param>
    internal void AfterBattleProcessForPlayer(string playerID)
    {
        Debug.Log("AfterBattleProcessForPlayer return");
        return;
        try
        {
            Thread postBattleProcessing = new Thread(() => scenario.PlayerAfterBattles(playerID, false));
            postBattleProcessing.IsBackground = true;
            postBattleProcessing.Name = "Post battle processing from mp";
            postBattleProcessing.Start();
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);  
        }
    }

    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="argument"></param>
    /// <param name="message"></param>
    internal void SetArmyToStopAttacking(string argument, string message)
    {
        try
        {
            string[] args = message.Split('*');
            Army army = scenario.FindArmyByID(Int32.Parse(argument));
            string targetType = args[0];
            int targetArmyID = Int32.Parse(args[1]);
            army.RemoveEnemy(targetArmyID, targetType);

            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_ENTITY_AND_PLAYER_SELECTED, army.LeaderID.ToString(), army.OwnerPlayerID));
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }

    }
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="argument"></param>
    /// <param name="message"></param>
    internal void SetArmyToAttack(string argument, string message)
    {
        try
        {
            string[] args = message.Split('*');
            Army army = scenario.FindArmyByID(Int32.Parse(argument));
            string targetType = args[0];
            int targetArmyID = Int32.Parse(args[1]);
            army.ArmiesYouIntentAttackIds.Add(new HostilityTarget(targetType, targetArmyID));


            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_ENTITY_AND_PLAYER_SELECTED, army.LeaderID.ToString(),army.OwnerPlayerID));
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }

    }

    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="message"></param>
    internal void PlaceBid(string playerID, string message)
    {
        try
        {
            string[] args = message.Split('*');
            int shopItemID = Int32.Parse(args[0]);
            int guildID = Int32.Parse(args[1]);
            int newBid = Int32.Parse(args[2]);
            string shopID = args[3];
            MerchantGuild merchantGuild = scenario.Guilds.FindGuildByID(guildID);
            ShopItem shopItem = merchantGuild.FindShopItemByID(shopItemID);

            GameEngine.ActiveGame.scenario.BidItem(playerID, shopItem, newBid);

            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_SHOP, playerID, shopID));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_PLAYER_INVENTORY, playerID));
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
 
    }
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="unitID"></param>
    /// <param name="message"></param>
    internal void SelectLevelUpChoice(string unitID, string message)
    {
        try
        {
            Entity entity = scenario.FindUnitByUnitID(Int32.Parse(unitID));
            string[] args = message.Split('*');
            string skillTreeKeyword = args[0];
            int level = Int32.Parse(args[1]);
            string levelUpKeyword = args[2];
            entity.SelectLevelUpChoice(levelUpKeyword, skillTreeKeyword, level);

            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_RECIPES, entity.FindCurrentOwnerID(), unitID));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_ENTITY_CHARACTER_WINDOW, unitID));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_ENTITY_SKILLS, unitID));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_ENTITY_SKILL_TREE, unitID));

        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }

    }
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="unitID"></param>
    /// <param name="itemID"></param>
    internal void CancelCraftItem(string unitID, string itemID)
    {
        try
        {
            Entity producer = scenario.FindUnitByUnitID(Int32.Parse(unitID));
            Item item = producer.BackPack.FindItemByID(Int32.Parse(itemID));
            Player player = scenario.FindPlayerByID(producer.FindCurrentOwnerID());
            item.CancelProduction(producer, player.PlayerID);
            producer.BackPack.RemoveItemByID(item.ID);

            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_ENTITY_INVENTORY, producer.FindCurrentOwnerID(), unitID));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_RECIPES, producer.FindCurrentOwnerID(), unitID));

        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
     }
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="unitID"></param>
    /// <param name="args"></param>
    /// <param name="mode"></param>
    internal void RecipeItemPlacement(string unitID, string args, int mode)
    {
        try
        {
            Entity producer = scenario.FindUnitByUnitID(Int32.Parse(unitID));
            switch (mode)
            {
                case 1:
                    int itemTobePlaced1 = Int32.Parse(args);
                    scenario.PlaceItemBeforeFirst(producer, itemTobePlaced1);
                    break;
                case 2:
                    int itemToBePlaced2 = Int32.Parse(args);
                    scenario.PlaceItemLast(producer, itemToBePlaced2);
                    break;
                case 3:
                    string[] splitted = args.Split('*');
                    int leftItem = Int32.Parse(splitted[0]);
                    int rightItem = Int32.Parse(splitted[1]);
                    int itemToBePlaced3 = Int32.Parse(splitted[2]);
                    scenario.PlaceInBetweenItems(producer, leftItem, rightItem, itemToBePlaced3);
                    break;
                default:
                    break;
            }

            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_ENTITY_INVENTORY, producer.FindCurrentOwnerID(), unitID));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_RECIPES, producer.FindCurrentOwnerID(), unitID));

        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
    }
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="message"></param>
    internal void StartRecipe(string playerID, string message)
    {
        try
        {
            string[] args = message.Split('*');
            string recipeKW = args[0];
            int entityID = Int32.Parse(args[1]);
            Entity producer = scenario.FindUnitByUnitID(entityID);
            Player player = scenario.FindPlayerByID(playerID);
            Recipe recipe = GameEngine.Data.RecipeCollection.findByKeyword(recipeKW);

            Item item = ItemGenerator.startProducingItem(producer, player, recipe);
            Debug.Log("client side StartRecipe item id " + item.ID);
            scenario.AttemptInstantCrafting(producer, recipe, item);
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.COMMAND_REFRESH_UI_SPECIFIC_PLAYER, playerID)); //instant crafting could be entity, so full refresh is best choice & for notifications

        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
   }
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="notificationid"></param>
    internal void DismissNotification(string playerID, string notificationid)
    {
        try
        {
            int notificationID = Int32.Parse(notificationid);
            Player player = scenario.FindPlayerByID(playerID);
            Notification notification = player.FindNotificationByID(notificationID);
            if (notification != null) //notification not being found is legal(you have heroes that didnt end turn)
            {
                notification.IsDismissed = true;
            }
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.COMMAND_REFRESH_UI_SPECIFIC_PLAYER, playerID));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.HIDE_EXPANDED_NOTIFICATION_PANEL, playerID));
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }

    }

    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="message"></param>
    internal void AcceptTradeOffer(string playerID, string message)
    {
        try
        {
            string[] args = message.Split('*');
            int shopItemID = Int32.Parse(args[0]);
            int guildID = Int32.Parse(args[1]);
            string bidPlayerID = args[2];
            MerchantGuild guild = scenario.Guilds.FindGuildByID(guildID);

            ShopItem shopItem = guild.FindShopItemByID(shopItemID);
            lock (shopItem.Bids)
            {
                Bid bid = shopItem.FindBidByPlayer(bidPlayerID);
                scenario.AcceptTradeOffer(shopItem, bid, guildID, false);
            }

            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.COMMAND_REFRESH_UI_SPECIFIC_PLAYER, playerID));
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }

    }

    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="playerid"></param>
    /// <param name="questID"></param>
    internal void CancelQuestParty(string playerid, string questID)
    {
        try
        {
            Player plr = scenario.FindPlayerByID(playerid);
            Quest quest = plr.FindQuestByID(Int32.Parse(questID));
            QuestParty questParty = quest.FindQuestPartyByPlayerID(playerid);
            bool refreshForArmies = false;
            bool refreshForMissions = false;
            if (questParty.HasEmbarked)
            {
                refreshForArmies = true;
                quest.DisbandParty(questParty.ID);
                
                GameEngine.ActiveGame.UpdateMemoryMapWithActiveVision(plr);
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.COMMAND_SELECT_OWN_FRIENDLY_ARMY)); //might not be the best?
            }
            else
            {
                refreshForMissions = true;
                quest.Parties.Remove(questParty);
            }

            AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_QUESTS, playerid, questID));
            if (refreshForArmies)
            {
                AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_MAP_IF_SEEING_SQUARE, new MapCoordinates(plr.CapitalLocation.XCoordinate, plr.CapitalLocation.YCoordinate)));
            }
            if (refreshForMissions)
            {
                AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_ENTITIES_SELECTED, questParty.AssignedEntitiesList));
            }

        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }

    }
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="playerid"></param>
    /// <param name="message"></param>
    internal void AuctionItem(string playerid, string message)
    {
        try
        {
            string[] args = message.Split('*');
            int itemID = Int32.Parse(args[0]);
            int shopID = Int32.Parse(args[1]);
            int amount = Int32.Parse(args[2]);
            Stat price = new Stat(args[3], Double.Parse(args[4]));
            int turns = Int32.Parse(args[5]);
            Player player = scenario.FindPlayerByID(playerid);
            Shop selectedShop = player.Shops.FindShopByID(shopID);
            Item item = player.OwnedItems.FindItemByID(itemID);
            scenario.AuctionItem(playerid, item, selectedShop, amount, price, turns);

            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_SHOP, playerid, shopID.ToString()));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_PLAYER_INVENTORY, playerid));

        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
     }
    /// <summary>
    /// mp method
    /// </summary>
    /// <param name="eventBattleData"></param>
    /// <param name="notes"></param>
    /// <exception cref="NotImplementedException"></exception>
    internal void AddEventSavedData(EventSavedData eventSavedData, string notes)
    {
        Player player = scenario.FindPlayerByID(notes);

        EventChain eventChain = player.EventChains[0];
        eventChain.DataList.AddWithOverwrite(eventSavedData);
    }

    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="playerid"></param>
    /// <param name="message"></param>
    internal void CancelAuctionOrTradeItem(string playerid, string message)
    {
        try
        {
            string[] args = message.Split("*");
            int shopItemID = Int32.Parse(args[0]);
            int guildID = Int32.Parse(args[1]);
            int shopID = Int32.Parse(args[2]);
            MerchantGuild guild = GameEngine.ActiveGame.scenario.Guilds.FindGuildByID(guildID);
            ShopItem shopItem = guild.FindShopItemByID(shopItemID);
            Player player = scenario.FindPlayerByID(playerid);
            Shop selectedShop = player.Shops.FindShopByID(shopID);
            shopItem.ObtainShopItem(shopItem.StackQuantity, player);

            shopItem.ReturnBidItems();

            selectedShop.RemoveShopItemByID(shopItem.ID);

            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_SHOP, playerid, shopID.ToString()));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_PLAYER_INVENTORY, playerid));

        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
 
    }
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="entityiD"></param>
    /// <param name="message"></param>
    internal void AddOrRemovePreferredItem(string entityiD, string message, bool add)
    {
        try
        {
            int entityID = Int32.Parse(entityiD);
            string[] args = message.Split('*');
            int index = Int32.Parse(args[0]);
            string preferredKeyword = args[1];
            Entity entity = scenario.FindUnitByUnitID(entityID);
            UpkeepCost upkeepCost = entity.UpKeep.Costs[index];
            if (add)
            {
                upkeepCost.PreferredItems.Add(preferredKeyword);
            }
            else
            {
                upkeepCost.PreferredItems.Remove(preferredKeyword);
            }

            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_ENTITY_UPKEEPS, entity.UnitID.ToString()));

        }
        catch (Exception)
        {

            throw;
        }
    }

    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="playerid"></param>
    /// <param name="message"></param>
    internal void CancelBid(string playerid, string message)
    {
        try
        {
            string[] args = message.Split("*");
            int shopItemID = Int32.Parse(args[0]);
            int guildID = Int32.Parse(args[1]);
            int shopID = Int32.Parse(args[2]); //for refresh ui
            MerchantGuild guild = GameEngine.ActiveGame.scenario.Guilds.FindGuildByID(guildID);
            ShopItem shopItem = guild.FindShopItemByID(shopItemID);
            Player player = scenario.FindPlayerByID(playerid);
            lock (shopItem.Bids)
            {
                Bid bid = shopItem.FindBidByPlayer(playerid);
                player.OwnedItems.AddRangeItems(bid.BidItems);
                shopItem.Bids.Remove(bid);
            }
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_SHOP, playerid, shopID.ToString()));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_PLAYER_INVENTORY, playerid));

        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
  

    }
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="message"></param>
    internal void CreateEmptyOfferBid(string playerID,string message)
    {
        try
        {
            string[] args = message.Split("*");
            int shopItemID = Int32.Parse(args[0]);
            int guildID = Int32.Parse(args[1]);
            string shopID = args[2];
            Bid bid = new Bid();
            bid.IsFresh = true;
            bid.PlayerID = playerID;
            MerchantGuild guild = GameEngine.ActiveGame.scenario.Guilds.FindGuildByID(guildID);
            ShopItem shopItem = guild.FindShopItemByID(shopItemID);
            lock (shopItem.Bids)
            {
                shopItem.Bids.Add(bid);
            }
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_SHOP, playerID, shopID));

        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
       }

    public void EndGlobalTurnThread()
    {
        Thread thread = new Thread(scenario.EndGlobalTurn);
        thread.Name = "end global turn thread";
        thread.IsBackground = true;
        thread.Start();
    }
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="playerid"></param>
    /// <param name="message"></param>
    internal void SetItemForTrade(string playerid, string message)
    {
        try
        {
            string[] args = message.Split('*');
            int itemID = Int32.Parse(args[0]);
            int shopID = Int32.Parse(args[1]);
            int amount = Int32.Parse(args[2]);
            int turns = Int32.Parse(args[3]);
            Player player = scenario.FindPlayerByID(playerid);
            Shop selectedShop = player.Shops.FindShopByID(shopID);
            Item item = player.OwnedItems.FindItemByID(itemID);
            GameEngine.ActiveGame.scenario.SetItemForTrade(playerid, item, selectedShop, amount, turns);
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_SHOP, playerid, shopID.ToString()));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_PLAYER_INVENTORY, playerid));

        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
}
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="mission"></param>
    /// <param name="notes"></param>
    internal void SetEntityMission(Mission mission,string notes)
    {
        try
        {
            string[] split = notes.Split('*');
            int entityID = Int32.Parse(split[0]);

            Entity entity = this.scenario.FindUnitByUnitID(entityID);
            entity.Mission = mission;
            if (split.Length == 1) //crammed in 1 more par in message part, theres a case where refresh is unwarranted(recipe ui), so split len 2 is guranteed from there
            {
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_ENTITY_SELECTED, notes));
            }

        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }

    }
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="argument"></param>
    internal void NullifyEntityMission(string argument)
    {
        try
        {
            int entityId = Int32.Parse(argument);
            Entity entity = this.scenario.FindUnitByUnitID(entityId);
            entity.Mission = null;
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_ENTITY_SELECTED, argument));

        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
     }
    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="upkeepToPay"></param>
    /// <exception cref="NotImplementedException"></exception>
    internal void PayShopUnitUpkeep(List<Stat> upkeepToPay,string playerID)
    {
        try
        {
         
            Player player = scenario.FindPlayerByID(playerID);
            foreach (Stat stat in upkeepToPay)
            {
                if (player.OwnedItems.GetSameItemAmount(stat.Keyword) < stat.Amount) //this is just in case, so if it goes wrong we would know
                {
                    Debug.LogError("MP PayShopUnitUpkeep invalid stat amount: " + stat.Keyword + " " + stat.Amount);
                }
                player.OwnedItems.GetAndRemoveItemsByKeyword(stat.Keyword, (int)stat.Amount, playerID);
            }

 
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_PLAYER_INVENTORY, playerID));
        }
        catch (Exception e)
        {

            Debug.LogError("PayShopUnitUpkeep " + e.Message + " " + e.StackTrace);
        }
    }

    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="playerid"></param>
    /// <param name="message"></param>
    internal void PurchaseItem(string playerid, string message)
    {
        try
        {
            string[] args = message.Split('*');
            int shopItemID = Int32.Parse(args[0]);
            int guildID = Int32.Parse(args[1]);
            int shopID = Int32.Parse(args[2]);
            MerchantGuild guild = scenario.Guilds.FindGuildByID(guildID);
            ShopItem shopItem = guild.FindShopItemByID(shopItemID);
            scenario.BuyItem(playerid, shopItem, guildID, shopID);
            Player player = scenario.FindPlayerByID(playerid);
            GameEngine.ActiveGame.UpdateMemoryMapWithActiveVision(player); //in case if entity is bought, then let know on this
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_SHOP, playerid, shopID.ToString()));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_PLAYER_INVENTORY, playerid));

        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
     }

    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="argument"></param>
    /// <param name="message"></param>
    internal void UpdateQuestPartyProgress(string argument, string message)
    {
        try
        {
            string[] ids = argument.Split('*');
            int questid = Int32.Parse(ids[0]);
            string playerid = ids[1];

            string[] progressAndMovement = message.Split('*');
            double newProgress = Double.Parse(progressAndMovement[0]);
            double newMovement = Double.Parse(progressAndMovement[1]);
            //using player id to get the quests because scenario.findquestbyid is not thread safe
            Player plr = scenario.FindPlayerByID(playerid);
            Quest qst = plr.FindQuestByID(questid);
            QuestParty questParty = qst.FindQuestPartyByPlayerID(playerid);
            questParty.Progress = newProgress;
            questParty.RemainingMovementPoints = newMovement;
            //UI refresh here?
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }

    }
    /// <summary>
    /// multiplayer method
    /// host does this, and then tells other clients which lootclaim won
    /// </summary>
    /// <param name="lootClaim"></param>
    /// <param name="v"></param>
    /// <exception cref="NotImplementedException"></exception>
    internal void ResolveLootClaim(OverlandLootClaim lootClaim, int gameSquareID, bool ishost)
    {
        try
        {
            GameSquare gameSquare = scenario.Worldmap.FindGameSquareByID(gameSquareID);
            if (gameSquare.Inventory.Count > 0)
            {
                if (ishost)
                {
                    //after this, gameSquare inventory is empty, so other claims will not apply anymore
                    MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.SendLootResolveResult, gameSquareID.ToString());
                    GameEngine.ActiveGame.clientManager.PushMultiplayerObject(multiplayerMessage, lootClaim);
                }
               
                scenario.ResolveLootClaim(gameSquare, lootClaim, false);
                Player player = scenario.FindPlayerByID(lootClaim.PlayerID);
                GameEngine.ActiveGame.UpdateMemoryMapWithActiveVision(player);
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.COMMAND_REFRESH_UI_SPECIFIC_PLAYER, lootClaim.PlayerID));
            }
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message + " " + e.StackTrace);
        }
    }

    /// <summary>
    /// multiplayermethod
    /// this is army movement
    /// </summary>
    /// <param name="armyMovement"></param>
    /// <exception cref="NotImplementedException"></exception>
    internal void Movement(ArmyMovementInfo armyMovementInfo)
    {
        try
        {
            Army army = GameEngine.ActiveGame.scenario.FindArmyByID(armyMovementInfo.armyID);
            Player player = scenario.FindPlayerByID(army.OwnerPlayerID);
            MapSquare loc = GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareByCoordinates(army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);
            MapSquare dest = GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareByCoordinates(armyMovementInfo.newCoordX, armyMovementInfo.newCoordY);
            GameEngine.ActiveGame.scenario.Movement(army, loc, dest, armyMovementInfo.modifier, false,false);
            GameEngine.ActiveGame.UpdateMemoryMapWithActiveVision(player);
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_ATTACK_ICONS_IF_SEEING_ARMY, army.ArmyID.ToString()));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_MAP_IF_SEEING_MOVEMENT, new FromToCoordinates(armyMovementInfo.currentCoordX, armyMovementInfo.currentCoordY, armyMovementInfo.newCoordX, armyMovementInfo.newCoordY)));
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
      
    }

    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="argument"></param>
    /// <param name="message"></param>
    internal void DisbandQuestArmies(string argument, string message)
    {
        try
        {
            string playerid = argument;
            int questid = Int32.Parse(message);
            Player plr = scenario.FindPlayerByID(playerid);
            Quest qst = plr.FindQuestByID(questid);
            qst.DisbandAllParties();

            AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_MAP_IF_SEEING_SQUARE, new MapCoordinates(plr.CapitalLocation.XCoordinate, plr.CapitalLocation.YCoordinate)));
            //queue up a command that checks if a refresh of anything is needed, and if it is needed, then refresh

        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
        }

    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="argument"></param>
    /// <param name="message"></param>
    internal void CreateOverlandEntity(string armyID, string arguments)
    {
        try
        {
            Army army = scenario.FindArmyByID(Int32.Parse(armyID));
            string[] args = arguments.Split('8');
            string templateKW = args[0];
            string randSeed = args[1];
            string randIteration = args[2];
            MyRandom rand = new MyRandom(Int32.Parse(randSeed), Int32.Parse(randIteration));
            Entity ent = Entity.CreateTemplateChar(templateKW, rand, army.OwnerPlayerID);
            lock (army.Units)
            {
                army.Units.Add(ent);
            }
            Player player = scenario.FindPlayerByID(army.OwnerPlayerID);
            GameEngine.ActiveGame.UpdateMemoryMapWithActiveVision(player);

            AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_MAP_IF_SEEING_SQUARE, new MapCoordinates(army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate)));

        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
      }

    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="unitID"></param>
    /// <param name="skillKW"></param>
    internal void RemoveEntitySkill(string unitID, string skillKW)
    {
        try
        {
            Entity entity = scenario.FindUnitByUnitID(Int32.Parse(unitID));
            entity.RemoveSkill(skillKW);
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_ENTITY_SKILLS, unitID));
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
        
    }

    internal void AddEntitySkill(string unitID, string skillKW, bool inCombat)
    {
        try
        {
            Entity entity = scenario.FindUnitByUnitID(Int32.Parse(unitID));
            entity.CreateEntitySkill(skillKW, inCombat);
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_SEEING_ENTITY_SKILLS, unitID));
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
      
    }

    /// <summary>
    /// multiplayer method
    /// </summary>
    /// <param name="playerID"></param>
    internal void RemoveEventChain(string playerID)
    {
        try
        {
            Player player = scenario.FindPlayerByID(playerID);
            player.EventChains.RemoveAt(0); //no locks needed, as no one accesses one players event chains in one place at the same time
                                            //we removeat 0 because eventchains are always taken as player.eventchains[0] when processed
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
 
    }

    /// <summary>
    /// multiplayer method when other players add to events
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="eventKW"></param>
    internal void AddToEvents(string playerID, string eventKW, bool initilizeEvents)
    {
        try
        {
            //no locks needed, as everything goes in a strict order here, and eventchain gets accessed only 1 at the time
            Player player = scenario.FindPlayerByID(playerID);
            GameProj.Events.Event playerEvent = new GameProj.Events.Event();
            playerEvent.TemplateKeyword = eventKW;
            if (initilizeEvents)
            {
                playerEvent.Initialized = true;
                playerEvent.Legal = true;
            }

            player.EventChains[0].Events.addEvent(playerEvent);

            //is UI refresh needed?
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message);
        }
      
    }

    internal void SubmitMainPhase(string playerID)
    {
        Debug.Log("SubmitMainPhase call for " + playerID);
        //not sure if the lock is 100% needed, but just in case as this method gets called from a thread
        lock (scenario.PlayersWhoEndedEvents)
        {
           
            scenario.PlayersWhoEndedEvents.Add(playerID); //no need for checks, because players cannot spam SubmitMainPhase message, as it happens automatically only once per turn
            Debug.Log("scenario.PlayersWhoEndedEvents count: " + scenario.PlayersWhoEndedEvents.Count + " player count: " + scenario.Players.Count);
            if (scenario.PlayersWhoEndedEvents.Count == scenario.Players.Count) //all players ended events, time to ProceedToMainPhase
            {
                //scenario.ProceedToMainPhase()
                MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.ProceedToMainPhase, GameEngine.random.Seed.ToString(), GameEngine.random.Iteration.ToString());
                clientManager.Push(multiplayerMessage);
                scenario.PlayersWhoEndedEvents.Clear();
            }
      
        }

    }

    public void StartAI(string specificPlayerKW)
    {
        foreach (Player player in scenario.Players)
        {
            if (specificPlayerKW != "")
            {
                if (specificPlayerKW != player.PlayerID)
                {
                    continue;
                }
            }
            if (player.isAI)
            {
                Thread aiThread = new Thread(() => GameEngine.ActiveGame.AI_METHOD(player.PlayerID));
                aiThread.Name = "AI Thread + " + player.PlayerID;

                aiThread.IsBackground = true;
                aiThread.Start();
            }
         
        }
    }

    /// <summary>
    /// host side method
    /// </summary>
    public void CreateAIThreads()
    {
        bool debug = true;

        if (debug)
        {
            Debug.Log("CreateAIThreads start");
        }
        foreach (Player player in scenario.Players)
        {
            if (player.isAI)
            {
                MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.CreateAIThreads, player.PlayerID, "");
                GameEngine.Multiplayer.SendToPlayerByID(multiplayerMessage,player.PlayerID);
            }
        }
      
       // Server.Broadcast("createAIThreads|txt",Server.clients);


        return;
        foreach (Player player in scenario.Players)
        {
            ServerClient existingPlayerClient = Server.GetClientByPlayerName(player.PlayerID);

            if (existingPlayerClient == null)
            {
                Debug.LogError("existingPlayerClient is null for player: " + player.PlayerID);
                continue;
                  
            }

            if (existingPlayerClient.isAI)
            {
                //IEnumerator<string> answer = AI_METHOD(player.PlayerID);

                //StartCoroutine(answer);
            }

        }


        if (debug)
        {
            Debug.Log("CreateAIThreads end");
        }
    }

    internal void UpdateTask(MultiplayerMessage multiplayerMessage)
    {
        bool debug = true;
        //splitting because multiplayermessage has 2 free variables and i didnt want to change the class
        string[] taskAndName = multiplayerMessage.Argument.Split('*');

        string taskName = taskAndName[0];
        string clientName = taskAndName[1];

        string status = multiplayerMessage.Message;
        if (debug)
        {
            Debug.Log("updating task type " + taskName + " for client " + clientName + " with a new status: " + status);
        }
        lock (hostTasks)
        {
            foreach (TaskStatus task in hostTasks)
            {
                if (task.computerID == clientName && task.taskType == taskName)
                {
                    task.completionStatus = status;
                    return;
                }
            }
        }
        Debug.LogError("No task was found with computerID " + clientName + " task type " + taskName);
    }

    
    /// <summary>
    /// method that does AI stuff depending on gamestate
    /// </summary>
    /// <param name="playerID"></param>
    /// <returns></returns>
    public void AI_METHOD(string playerID)
    {
        bool debug = true;
        Player player = scenario.FindPlayerByID(playerID);
        if (debug)
        {
            Debug.Log("AI_METHOD started for player: " + playerID + " in gamestate: " + player.GameState.Keyword);
        }
  
        try
        {
            //if player is defeated, player will end turn immediately
            if (player.Defeated)
            {
                MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.SubmitEndTurn, playerID, "");
                GameEngine.ActiveGame.clientManager.Push(multiplayerMessage);
                return;
            }
            switch (player.GameState.Keyword)
            {

                case GameState.State.MAIN_PHASE:

                    int secondsToWait = scenario.AiRandom.Next(1000, 1500); //use system random for wait time!!
                    Thread.Sleep(secondsToWait);
                    //yield return new WaitForSeconds(secondsToWait);
                    if (debug)
                    {
                        Debug.Log(playerID + " has slept" + secondsToWait + " ms, ending turn ");
                    }
                    MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.SubmitEndTurn, playerID, "");
                    GameEngine.ActiveGame.clientManager.Push(multiplayerMessage);
                    break;

                default:
                    break;
            }

        }
        catch (Exception e)
        {

            Debug.LogError("AI " + playerID + " " + e.Message);
        }
        

        
    }
    /// <summary>
    /// used by clients to generate own scenario
    /// </summary>
    public void StartScenarioThread(int incRandom)
    {
        GameEngine.Data.OptionCollection = optionPanel.optionCollection;
        Thread scenarioThread = new Thread(() => GameEngine.ActiveGame.CreateScenario(optionPanel.playerSetups, incRandom));
        scenarioThread.Name = " scenario creation ";

        scenarioThread.IsBackground = true;
        scenarioThread.Start();
    }

    // Start is called before the first frame update
    public void StartMethod(List<PlayerSetup> playerSetups, OptionPanelController incOptionPanel)
    {
         //optionPanel = incOptionPanel; //why here? why not link? mystery, linking right now
        //  worldmap = MapGenerator.GenerateMap(GeneratorMode.Square);
        if (!IsUIInitilized)
        {
            //InitializeUI();
            GameEngine.IsUIInitilized = true;
        }
       
        SaveOptionsAfterStartGame();
        int currentTick = Environment.TickCount; //if option is set seed, this wont be used by the clients
        MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.GenerateScenario,currentTick.ToString(),"");
        GameEngine.ActiveGame.clientManager.Push(multiplayerMessage);
      
        //thread scenario here
        Thread scenarioThread = new Thread(() => GameEngine.ActiveGame.CreateScenario(playerSetups, currentTick));
        scenarioThread.Name = " scenario creation ";

        scenarioThread.IsBackground = true;
        scenarioThread.Start();

       // OnScenarioRecieve(); //now using ui command from scenario thread
        //InstantiatePlayerPrefabs(scenario.Players); //singleplayer
      //  AfterInstantiate();//singleplayer
       // Debug.Log("after instantiate objects count " + playerControllersList.Count);

        //InstantiatePlayerPrefabs(scenario.Players);
        //this part is now called seperately due to slight delay after sending players
        //combatMapGeneratorPanel.SetActive(false);
        //scenario.AutoEquipItems();
        //scenario.StartGlobalTurn();
        //CreateAIThreads();


        //Debug.Log("5 size: " + Data.ItemProductionRuleSetCollection.DataList.Count);


        //combatMapGeneratorPanel.SetActive(false);

        //scenario.StartTurn();

        // FindMissingScriptsRecursively.ShowWindow();

        //MapSquare playerSquare = scenario.Worldmap.FindMapSquareByCordinates(3, 3, 0);

        //if (playerSquare != null) {
        //    moverSquareID = playerSquare.ID;
        //    playerSquare.TileGraphicName = MapPointedHex.GrassThreeTrees;
        //    //OurLog.Print("Player asub " + moverSquareID);

        //    //OurLog.Print("W naaber :" + playerSquare.W_NeighbourID);
        //    //OurLog.Print("E naaber :" + playerSquare.E_NeighbourID);
        //    //OurLog.Print("NW naaber :" + playerSquare.Nw_NeighbourID);
        //    //OurLog.Print("SE naaber :" + playerSquare.Se_NeighbourID);

        //}
      

        //AddHostTask("", TaskStatus.TYPE_SEND_OPTION_COLLECTION);

    }
    /// <summary>
    /// this chunk was taken from startmethod, and is to be called after every1's UIs were created
    /// </summary>
    public void AfterInstantiate()
    {
        //scenario.HowLargeIsScenario("after instantiate");
        // combatMapGeneratorPanel.SetActive(false);
        try
        {
            //scenario.AutoEquipItems();
            scenario.StartGlobalTurn(true);
            //if (GameEngine.ActiveGame.isHost)
            //{
            //    CreateAIThreads();
            //}
        }
        catch (Exception e)
        {

            Debug.LogError("After instantiate error: " + e.Message);    
        }
      
       
    }

    public void AfterInstantiateThread()
    {
        Thread afterInstantiateThread = new Thread(AfterInstantiate);
        afterInstantiateThread.Name = "start global turn";
        afterInstantiateThread.IsBackground=true;
        afterInstantiateThread.Start();
    }

    public void CreateScenario(List<PlayerSetup> playerSetups, int incRandomSeed)
    {
        int maxSteps = 15;
        int currentStep = 1;
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Creating scenario " + (currentStep++) + "/" + maxSteps));

        scenario = new Scenario();
        //moved saving of options to be first thing as i setup random right away
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Saving game options " + (currentStep++) + "/" + maxSteps));
        scenario.SaveGameOptions();

        switch (scenario.SeedMode)
        {
            case OptionCollection.Seed_RANDOM:
                GameEngine.random = new MyRandom(incRandomSeed);
                break;
            case OptionCollection.Seed_SPECIFIC:
            
                GameEngine.random = new MyRandom(scenario.SetSeed);
                break;
            default:
                Debug.LogError("incorrect seed mode: " + scenario.SeedMode);
                break;
        }
        
        
        scenario.SavedRandom = GameEngine.random;
        scenario.AiRandom = new MyRandom(incRandomSeed);
        scenario.MapTemplateKeyword = "Example_1";
  
  
        scenario.PlayerSetups = playerSetups;
        //scenario.SetQuestRefreshCounters();        
        //scenario.RandomSeed = UnityEngine.Random.state; //commented out bc cant work in threads
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Generating merchant guilds " + (currentStep++) + "/" + maxSteps));
        //GameEngine.ActiveGame.clientManager.Push(new MultiplayerMessage(MultiplayerMessage.UpdateTaskStatusOutputToAllExceptSender,"", "Generating merchant guilds " + (currentStep++) + "/" + maxSteps));
        scenario.GenerateMerchantGuilds();
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Generating players " + (currentStep++) + "/" + maxSteps));
        scenario.GeneratePlayers();
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Setting AI clients " + (currentStep++) + "/" + maxSteps));
        CreateAIClients(scenario.Players);
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Generating relations " + (currentStep++) + "/" + maxSteps));
        scenario.GenerateRelations();
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Refreshing quest counter " + (currentStep++) + "/" + maxSteps));
        scenario.RefreshQuestCounter();
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Generating world map " + (currentStep++) + "/" + maxSteps));
        scenario.Worldmap = MapGenerator.GenerateMap(MapGenerator.Mode_PointedHex, scenario.Max_Map_Size_X, scenario.Max_Map_Size_Y);

        MapTemplate mapTemplate = GameEngine.Data.MapTemplateCollection.findByKeyword(scenario.MapTemplateKeyword); 
        if (mapTemplate == null) Debug.LogError("maptemplate not found by name :" + mapTemplate);

        List<Zone> zones = scenario.Worldmap.GenerateZones(mapTemplate, scenario.Players);

        //List<string> ids = new List<string>();
        //foreach (Player item in scenario.Players)
        //{
        //    ids.Add(item.PlayerID);
        //}

        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Populating map with structures " + (currentStep++) + "/" + maxSteps));
        //       scenario.Worldmap.PopulateMapWithStructures(ids);
        scenario.Worldmap.GenerateBuildings(zones);

        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Adding overland resources " + (currentStep++) + "/" + maxSteps));
        scenario.Worldmap.SetResources(zones);

        //GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Generating gamesquare inventory treasures " + (currentStep++) + "/" + maxSteps));
        //scenario.Worldmap.GeneratePickUps();


        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Generating neutral armies " + (currentStep++) + "/" + maxSteps));
        scenario.Worldmap.GenerateDifficultyZones();
        scenario.Worldmap.GenerateNeutralArmies();

  



        bool debugFaction = false;
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Creating player starting armies " + (currentStep++) + "/" + maxSteps));
        foreach (Player player in scenario.Players)
        {
            if (player.PlayerID == Player.Neutral)
            {
                continue;
            }
            //Faction faction = GameEngine.Data.FactionCollection.findByKeyword(player.FactionKeyword);
            if (debugFaction)
            {
                Debug.Log("faction of player " + player.FactionKeyword + " id " + player.PlayerID);
            }
       
            scenario.CreateStartingArmies(player, player.StartingHeroes);
        }
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Refreshing future hero counter " + (currentStep++) + "/" + maxSteps));
        scenario.RefreshFutureHeroCounter(true);

        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Generating overland events " + (currentStep++) + "/" + maxSteps));
        if (scenario.Events_Toggle)
        {
            scenario.Worldmap.GenerateOverlandEvents(scenario.Players, mapTemplate, 0);
            scenario.GeneratePlayerStartingEvents();
        }

        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Creating player hidden maps " + (currentStep++) + "/" + maxSteps));
        foreach (Player player in scenario.Players)
        {
            CreateHiddenMap(player);
        }

        scenario.AutoEquipItems();


        //GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.LOBBY_AFTER_INSTANTIATE));

       
        //send scenario before create player UI??
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Sending scenario to server"));
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, ""));
        started = true;

        bool sendObjToServer = false;
        if (sendObjToServer)
        {
            MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.SubmitScenarioToServer, PLAYER_IDENTITY);
            CompressedBytes compressedScenario = new CompressedBytes();
            //GameStopwatch.Reset();
            //GameStopwatch.Start();
            Debug.Log("compressed scenario before debug ");
            compressedScenario.obj = ObjectByteConverter.ObjectToByteArray(scenario);
            //GameStopwatch.Stop();
            Debug.Log("compressed scenario size: " + compressedScenario.obj.Length + " took " + GameStopwatch.ElapsedMilliseconds + " ms to complete");
            //        Debug.Log("not compressed scenario size: " + ObjectByteConverter.ObjectToByteArrayTest(scenario).Length);
            clientManager.PushMultiplayerObject(multiplayerMessage, compressedScenario);
        }
  

        // GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.AFTER_INSTANTIATE, ""));
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Creating player UI"));
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.LOBBY_HIDE_OPTIONS_PANEL_CREATE_PLAYER_CONTROLLER));
        //InstantiateMultiplayerPrefabs(scenario.Players); //multiplayer
        //SendMultiplayerMap(); //multiplayer
        //SendMultiplayerArmies(); //multiplayer
        //SendMultiplayerScenarios(); //multiplayer
    }

    public void DeserializeScenario(byte[] incBytes)
    {
        try
        {
            Scenario desScenario = (Scenario)ObjectByteConverter.ByteArrayToObject(incBytes);
            GameEngine.ActiveGame.scenario = desScenario;
            GameEngine.ActiveGame.scenario.SavedRandom.InitilizeRandom();
            GameEngine.random = new MyRandom(GameEngine.ActiveGame.scenario.SavedRandom.Seed, GameEngine.ActiveGame.scenario.SavedRandom.Iteration);
            if (GameEngine.ActiveGame.optionPanel.isLoading)
            {
                MultiplayerMessage updateTaskMessage = new MultiplayerMessage(MultiplayerMessage.UpdateTask, TaskStatus.TYPE_SEND_SCENARIO_LOAD_GAME + "*" + GameEngine.PLAYER_IDENTITY, "1");
                GameEngine.ActiveGame.clientManager.Push(updateTaskMessage);
            }
            else
            {
                MultiplayerMessage updateTaskMessage = new MultiplayerMessage(MultiplayerMessage.UpdateTask, TaskStatus.TYPE_SEND_SCENARIO + "*" + GameEngine.PLAYER_IDENTITY, "1");
                GameEngine.ActiveGame.clientManager.Push(updateTaskMessage);
            }
          
            Debug.Log("thread end DeserializeScenario");
        }
        catch (Exception e)
        {

            Debug.LogError("thread failed: " + e.Message);
        }
       
    }


    /// <summary>
    /// client method after recieving scenario from server(not for host)
    /// since uses monobehaviour, this should be called using UI commands
    /// </summary>
    public void OnScenarioRecieve()
    {
        //scenario.HowLargeIsScenario(" beginning of on scenario recieve ");
        DeletePlayerPrefabs();
        playerControllersList.Clear();
        optionPanel.HideUI();
    

        menuController.chatPanel.SetActive(true);
        NewInitilizeLocalPlayerPrefabs();


        foreach (Player player in scenario.Players)
        {
            player.PlayerEventRandom = new MyRandom(player.PlayerEventRandom.Seed, player.PlayerEventRandom.Iteration);
        }
        //foreach (string playerID in playersYouPlayAsIDs)
        //{
        //    Debug.Log("OnScenarioRecieve not observer");

        //    InstantiateSinglePlayerPrefab(GameEngine.ActiveGame.scenario.FindPlayerByID(playerID), false, setActive,false);

        //    setActive = false; //keeping on only the first one
        //}

        //scenario.HowLargeIsScenario(" end of OnScenarioRecieve ");
    }

 
    /// <summary>
    /// the latest one, which uses observer modes
    /// </summary>
    public void NewInitilizeLocalPlayerPrefabs()
    {
        bool isObserver = true;
        bool setActive = true;
        //scenario.HowLargeIsScenario(" before player setups ");
        foreach (PlayerSetup setup in scenario.PlayerSetups)
        {
            if (setup.ComputerName == GameEngine.PLAYER_IDENTITY)
            {
                Debug.Log("OnScenarioRecieve not observer");

                InstantiateSinglePlayerPrefab(GameEngine.ActiveGame.scenario.FindPlayerByID(setup.PlayerName), false, setActive, false);

                setActive = false; //keeping on only the first one
                isObserver = false;
            }
        }
        if (isObserver) //spectator
        {
            //observer can go through multiple player controllers
            foreach (Player plr in GameEngine.ActiveGame.scenario.Players)
            {
                if (plr.Defeated)
                {
                    continue;
                }
                InstantiateSinglePlayerPrefab(plr, false, setActive, true);

                setActive = false; //keeping on only the first one
            }
        }
        //if multiple player controllers, then activate switcher
        if (playerControllersList.Count > 1)
        {
            playerControllerSwitcher.Activate();
        }
    }

    internal void AcceptTransactionInfo(TransactionInfo transactionInfo)
    {
        if (transactionInfo == null)
        {
            Debug.LogError("AcceptTransactionInfo transactionInfo is null");
        }
        else
        {
          //  Debug.LogError("AcceptTransactionInfo transactionInfo: player " + transactionInfo.PlayerID + " item id " + transactionInfo.ItemID + " guild id " + transactionInfo.GuildID + " notification type & id " + transactionInfo.Notification.Type + " " + transactionInfo.Notification.ID);
        }
        Player plr = scenario.FindPlayerByID(transactionInfo.PlayerID);
        TransactionItem transactionItem = null;
        if (transactionInfo.GuildID == -1)
        {
            transactionItem = plr.FindExtraItemByID(transactionInfo.ItemID); 
        }
        else
        {
            transactionItem = scenario.FindTransactionItemByID(transactionInfo.GuildID, transactionInfo.ItemID); //search through player extra items if guild id is -1!!!!!!!!!!
        }
         
        if (transactionItem == null)
        {
            Debug.LogError("no transaction item found with guild id & item id: " + transactionItem.GuildID + " ");
        }
   
        plr.Notifications.Remove(plr.FindNotificationByID(transactionInfo.Notification.ID));
        GameEngine.ActiveGame.scenario.AcceptPendingItem(transactionItem, plr,false);
    }

    internal void AddQuestItemsToEventStash(OddsAndRandom oddsAndRandom, string playerID)
    {
        List<List<TemplateOdd>> odds = (List<List<TemplateOdd>>)oddsAndRandom.obj;
        //MyRandom rand = new MyRandom(oddsAndRandom.seed,oddsAndRandom.iteration);
        Player plr = scenario.FindPlayerByID(playerID);
        plr.EventStash.CreateItems(odds, null, plr.PlayerEventRandom, playerID, null,true);

        AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_EVENT_STASH_PANEL, playerID));
    }

    //public void DisplayOverlandOLD()
    //{


    //    foreach (MapSquare currentSquare in scenario.Worldmap.MapSquares)
    //    {
    //        GameSquare gameSquare = currentSquare as GameSquare;
    //        Tile squareTile = GetTileByName(currentSquare.TileGraphicName);
    //        if (gameSquare.building != null)
    //        {
    //            squareTile = GetTileByName(gameSquare.building.Graphics);
    //        }
    //        armyTileMap.SetTile(new Vector3Int(currentSquare.X_cord, currentSquare.Y_cord, 0), null);
    //        selectionTileMap.SetTile(new Vector3Int(currentSquare.X_cord, currentSquare.Y_cord, 0), null);

    //        if (blink)
    //        {
    //            if (selectedGameSquare != null)
    //            {
    //                if (currentSquare.X_cord == selectedGameSquare.X_cord && currentSquare.Y_cord == selectedGameSquare.Y_cord)
    //                {
    //                    squareTile = GetTileByName(MapPointedHex.Red_sand);
    //                }
    //            }


    //        }
    //        groundTileMap.SetTile(new Vector3Int(currentSquare.X_cord, currentSquare.Y_cord, 0), squareTile);

    //        Army overlandarmy = scenario.GetOverlandArmy(currentSquare.X_cord, currentSquare.Y_cord);
    //        if (overlandarmy != null)
    //        {
    //            Tile squareTile2 = GetTileByName(overlandarmy.GetArmyPicture());
    //            armyTileMap.SetTile(new Vector3Int(overlandarmy.WorldMapPositionX, overlandarmy.WorldMapPositionY, 0), squareTile2);
    //        }

    //        switch (currentSquare.Owner_ID)
    //        {
    //            case "":
    //                playerFlagTileMap.SetTile(new Vector3Int(currentSquare.X_cord, currentSquare.Y_cord, 0), null);
    //                break;

    //            case MapPointedHex.Owner_neutral:
    //                squareTile = GetTileByName(MapPointedHex.Owner_neutral);
    //                playerFlagTileMap.SetTile(new Vector3Int(currentSquare.X_cord, currentSquare.Y_cord, 0), squareTile);
    //                break;
    //            case MapPointedHex.Owner_player1:
    //                squareTile = GetTileByName(MapPointedHex.Owner_player1);
    //                playerFlagTileMap.SetTile(new Vector3Int(currentSquare.X_cord, currentSquare.Y_cord, 0), squareTile);
    //                break;
    //            case MapPointedHex.Owner_player2:
    //                squareTile = GetTileByName(MapPointedHex.Owner_player2);
    //                playerFlagTileMap.SetTile(new Vector3Int(currentSquare.X_cord, currentSquare.Y_cord,0), squareTile);
    //                break;
    //            case MapPointedHex.Owner_player3:
    //                squareTile = GetTileByName(MapPointedHex.Owner_player2);
    //                playerFlagTileMap.SetTile(new Vector3Int(currentSquare.X_cord, currentSquare.Y_cord, 0), squareTile);
    //                break;
    //            default:
    //                squareTile = GetTileByName(MapPointedHex.Owner_player2);
    //                playerFlagTileMap.SetTile(new Vector3Int(currentSquare.X_cord, currentSquare.Y_cord, 0), squareTile);
    //                break;
    //        }

    //        if (gameSquare.BattleFieldID > -1) {

    //            squareTile = GetTileByName("tileInBattle");
    //            combatIndicatorTileMap.SetTile(new Vector3Int(currentSquare.X_cord, currentSquare.Y_cord, 0), squareTile);
    //        }


    //    }





    //    Tile selectedSquare = GetTileByName(MapPointedHex.OrangeSelector);
    //    if (selectedGameSquare != null)
    //    {
    //        selectionTileMap.SetTile(new Vector3Int(selectedGameSquare.X_cord, selectedGameSquare.Y_cord, 0), selectedSquare);
    //    }
    //    if (path != null)
    //    {
    //        foreach (MapSquare mapsquare in path)
    //        {
    //            selectionTileMap.SetTile(new Vector3Int(mapsquare.X_cord, mapsquare.Y_cord, 0), selectedSquare);
    //        }
    //    }




    

    //}

    /// <summary>
    /// since overland army doesnt have skill targeting, all particles will be on 1 place only
    /// </summary>
    /// <param name="message"></param>
    /// <param name="audioFiles"></param>
    /// <param name="r"></param>
    /// <param name="g"></param>
    /// <param name="b"></param>
    /// <param name="particles"></param>
    /// <param name="xCord"></param>
    /// <param name="yCord"></param>
    /// <param name="player"></param>
    /// <param name="playerController"></param>
    /// <param name="memoryTiles"></param>
    public void DisplayOverlandMessageToPlayersOnSquare(string message, List<string> audioFiles, byte r, byte g, byte b,
        List<SkillParticle> particles, int xCord, int yCord, Player player, PlayerController controller,MapMemory memoryTiles)
    {
        Color32 color = new Color32(r, g, b, 255);
        MemoryTile memoryTile = memoryTiles.FindMemoryTileByCoordinates(xCord, yCord);
        if (memoryTile.SightOnTile == 0)
        {
            return;
        }
        if (player.GameState.Keyword == GameState.State.BATTLE_PHASE)
        {
            return;
        }
        //CombatMapGenerator controller = playerController.GetCombatMapGenerator();
        GameObject visionObj = Instantiate(controller.combatMessagePrefab, controller.onWorldCanvas.transform, false);



        Vector3Int convertCellPositionToWorld = new Vector3Int(xCord, yCord, 0);
        Vector3 cellPosition = controller.grid.CellToWorld(convertCellPositionToWorld);
        cellPosition = new Vector3(cellPosition.x, cellPosition.y - controller.grid.cellSize.y / 4);

        visionObj.transform.position = cellPosition;
        visionObj.transform.position += new Vector3(0, memoryTile.messageOffset, 0);

        memoryTile.messageOffset -= 1;
        //uhhhhhhhhh what is this and is this legal
        if (!visionObj.activeInHierarchy)
        {
            Destroy(visionObj);
            return;
        }


        //text message
        CombatMessageTextController combatMessageText = visionObj.GetComponent<CombatMessageTextController>();
        combatMessageText.text.text = message;
        combatMessageText.tile = memoryTile;
        combatMessageText.originalColor = color;
        combatMessageText.text.color = color;
        combatMessageText.FadeText();



        if (audioFiles.Count > 0)
        {
            string audioFile = audioFiles[UnityEngine.Random.Range(0, audioFiles.Count)];
            AudioClip audioClip = Resources.Load<AudioClip>(audioFile);
            if (audioClip != null)
            {
                if (controller.audioSource != null)
                {
                    controller.audioSource.PlayOneShot(audioClip);
                }

            }
            else
            {
                Debug.LogError("no sound found: " + audioFile);
            }

        }


        SkillParticleController previousParticle = null;
        foreach (SkillParticle particleStat in particles)
        {
            GameObject particlePrefab = Resources.Load<GameObject>(particleStat.PrefabName);
            Debug.Log("particle: " + particleStat.PrefabName);
            if (particlePrefab == null)
            {
                Debug.LogError("no particle prefab found: " + particleStat.PrefabName + " message: " + message);
                continue;
            }

            GameObject particleObject = Instantiate(particlePrefab, controller.onWorldCanvas.transform, false);

            particleObject.transform.position = cellPosition;
            Debug.Log("particle pos: x " + particleObject.transform.position.x + " y " + particleObject.transform.position.y);

            SkillParticleController skillParticleController = particleObject.GetComponent<SkillParticleController>();

            bool triggerImmediately = true;
            if (particleStat.Wait)
            {

                skillParticleController.previousParticles = previousParticle;
                skillParticleController.waitingForPreviousParticle = true;
                triggerImmediately = false;
                if (previousParticle == null)
                {
                    triggerImmediately = true;
                }
            }


            previousParticle = skillParticleController;
            if (triggerImmediately)
            {
                skillParticleController.skillparticleSystem.Play();
                skillParticleController.startCountdown = true;
            }



        }

    }


    /// <summary>
    /// message should come in pieces for collection lang strings
    /// if battlefield is null, means that message is for overland, and MapMemory is checked instead of combatmapmemory
    /// </summary>
    /// <param name="message"></param>
    /// <param name="color"></param>
    /// <param name="sqr"></param>
    /// <param name="battlefield"></param>
    public void DisplayCombatMessageToPlayersOnSquare(string message, List<string> audioFiles,byte r, byte g, byte b ,
        List<SkillParticle> particles,int xCord,int yCord, int sourceX, int sourceY,BattlefieldOld battlefield, bool showMessageAtSourceSquare,
        Player player, PlayerController playerController,CombatMapMemory memory,List<AnimationStationayInfo> animationStationayInfos,bool showMessageIfNoVision)
    {
        Color32 color = new Color32(r, g, b, 255);
        Debug.Log("DisplayCombatMessageToPlayersOnSquare coords X " + xCord + " Y " + yCord + " for player: " + player.PlayerID);
        //if player not in battle in this, then skip
        //if (!battlefield.GetCurrentParticipantPlayerIDs().Contains(player.PlayerID))
        //{
        //    return;
        //}
   

        //CombatMapMemory memory = battlefield.GetPlayerCombatMapMemory(player.PlayerID);
        int X = 0;
        int Y = 0;
        if (showMessageAtSourceSquare)
        {
            X = sourceX;
            Y = sourceY;
        }
        else
        {
            X = xCord;
            Y = yCord;
        }
        if (memory == null)
        {
            Debug.LogError("map memory null wtf");
        }
        CombatMemoryTile combatMemoryTile = memory.FindMemoryTileByCoordinates(X, Y);
        //sqr if not in vision, then skip


        //after message legality, we can save the message to combat map memory
        //if (saveAsReplay)
        //{
        //    CombatMessageInfo combatMessageInfo = new CombatMessageInfo(message, audioFiles, r, g, b, particles, xCord, yCord, sourceX, sourceY, null, showMessageAtSourceSquare);
        //    //should we save the combat map here?
        //    memory.AddReplay(new CombatReplay(combatMessageInfo,memory));
        //}
        //if player not seeing the battle, then skip(visual legality)
        CombatMapGenerator combatMapGenerator = playerController.GetCombatMapGenerator();
        if (combatMapGenerator.battlefield == null)
        {
            return;
        }
        if (combatMapGenerator.battlefield.ID != battlefield.ID) 
        {
            return;
        }
        if (combatMapGenerator.IsInAutoBattle())
        {
            return;
        }
        PlayerController controller = FindPlayerControllerGameObject(player.PlayerID).GetComponent<PlayerController>();
        Vector3Int convertCellPositionToWorld = new Vector3Int(X, Y, 0);
        Vector3 cellPosition = controller.GetCombatMapGenerator().grid.CellToWorld(convertCellPositionToWorld);
        if (combatMemoryTile.VisionOnTile > 0 || showMessageIfNoVision)
        {
            //GameObject visionObj = Instantiate(controller.GetCombatMapGenerator().CombatSquareIconPrefab, controller.GetCombatMapGenerator().onWorldCanvas.transform, false);
            GameObject visionObj = Instantiate(controller.combatMessagePrefab, controller.GetCombatMapGenerator().onWorldCanvas.transform, false);

    


            cellPosition = new Vector3(cellPosition.x, cellPosition.y - controller.GetCombatMapGenerator().grid.cellSize.y / 4);

            visionObj.transform.position = cellPosition;
            visionObj.transform.position += new Vector3(0, combatMemoryTile.combatMessageOffset, 0);

            combatMemoryTile.combatMessageOffset -= 1;
            //uhhhhhhhhh what is this and is this legal
            if (!visionObj.activeInHierarchy)
            {
                Destroy(visionObj);
                return;
            }


            //text message
            CombatMessageTextController combatMessageText = visionObj.GetComponent<CombatMessageTextController>();
            combatMessageText.text.text = message;
            combatMessageText.sqr = combatMemoryTile;
            combatMessageText.originalColor = color;
            combatMessageText.text.color = color;
            combatMessageText.FadeText();

        }
        if (combatMemoryTile.VisionOnTile > 0)
        {
            Debug.Log("animationStationayInfos count: " + animationStationayInfos.Count);
            //animations
            foreach (AnimationStationayInfo animationInfo in animationStationayInfos)
            {
                controller.GetCombatMapGenerator().StaticUnitAnimation(animationInfo.UnitKW, animationInfo.MapCoordinates, animationInfo.PlayerID, memory, animationInfo.AnimationName, animationInfo.UnitID);
            }



            //sound
            if (audioFiles.Count > 0)
            {
                string audioFile = audioFiles[UnityEngine.Random.Range(0, audioFiles.Count)];
                AudioClip audioClip = Resources.Load<AudioClip>(audioFile);
                if (audioClip != null)
                {
                    if (controller.audioSource != null)
                    {
                        controller.audioSource.PlayOneShot(audioClip);
                    }

                }
                else
                {
                    Debug.LogError("no sound found: " + audioFile);
                }

            }
        }
        //no vision check here?
        Vector3 pointA = controller.GetCombatMapGenerator().grid.CellToWorld(new Vector3Int(sourceX, sourceY));
        Vector3 pointB = controller.GetCombatMapGenerator().grid.CellToWorld(new Vector3Int(xCord, yCord));


        float distance = Mathf.Sqrt(Mathf.Pow((pointB.x - pointA.x), 2) + Mathf.Pow((pointB.y - pointA.y), 2));

        // Debug.Log("rotate source XY " + sourceX + " " + sourceY + " target xy " + xCord + " " + yCord + " distance " + distance);

        SkillParticleController previousParticle = null;
        foreach (SkillParticle particleStat in particles)
        {
            GameObject particlePrefab = Resources.Load<GameObject>(particleStat.PrefabName);
            Debug.Log("particle: " + particleStat.PrefabName);
            if (particlePrefab == null)
            {
                Debug.LogError("no particle prefab found: " + particleStat.PrefabName + " message: " + message);
                continue;
            }

            GameObject particleObject = Instantiate(particlePrefab, controller.GetCombatMapGenerator().onWorldCanvas.transform, false);

            particleObject.transform.position = cellPosition;

            if (particleStat.TriggerAtSource)
            {
                particleObject.transform.position = pointA;

                if (particleStat.SwapSourceTarget)
                {
                    particleObject.transform.position = pointB;
                }
            }
            //1,105
            Debug.Log("particle pos: x " + particleObject.transform.position.x + " y " + particleObject.transform.position.y + " DEBUG LOG DISTANCE " + distance + " radius would be " + distance / 2);

            SkillParticleController skillParticleController = particleObject.GetComponent<SkillParticleController>();

            if (particleStat.Rotate)
            {
                if (particleStat.SwapSourceTarget)
                {
                    skillParticleController.Rotate(pointB, pointA,particleStat.OffsetPositionToRadius,1.105f,particleStat.AdditionalOffset,particleStat.AngleOffset); //1.105 radius is distance between 2 point blank hex / 2
                }
                else
                {
                    skillParticleController.Rotate(pointA, pointB,particleStat.OffsetPositionToRadius, 1.105f, particleStat.AdditionalOffset, particleStat.AngleOffset); //1.105 radius is distance between 2 point blank hex / 2
                }

            }

            if (particleStat.AdjustParticleLifetime)
            {
                skillParticleController.SetParticleLife(distance);
            }
            bool triggerImmediately = true;
            if (particleStat.Wait)
            {

                skillParticleController.previousParticles = previousParticle;
                skillParticleController.waitingForPreviousParticle = true;
                triggerImmediately = false;
                if (previousParticle == null)
                {
                    triggerImmediately = true;
                }
            }


            previousParticle = skillParticleController;
            if (triggerImmediately)
            {
                skillParticleController.skillparticleSystem.Play();
                skillParticleController.startCountdown = true;
            }



        }

    }
    /// <summary>
    /// sets computer name in player setup(Load)
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="computerName"></param>
    internal void AssignPlayerID(string playerID, string computerName)
    {
        PlayerSetup playerSetup = scenario.GetPlayerSetupByPlayerID(playerID);
        playerSetup.ComputerName = computerName;
    }
    /// <summary>
    /// sets computer name in player setup(hosting a new game, for more info check optionPanel.assignedPlayerSetups var)
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="computerName"></param>
    internal void AssignPlayerIDToNewGame(string playerID, string computerName)
    {
        lock (optionPanel.assignedPlayerSetups)
        {
            optionPanel.assignedPlayerSetups.Add(new MyValue(playerID, computerName));
        }
       
        AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.LOBBY_CREATE_PLAYER_SLOTS)); //using UI command, as this function is called from client side
    }

    internal void UnAssignPlayerID(string playerID)
    {
        lock (optionPanel.assignedPlayerSetups)
        {
            MyValue toRemove = null;
            foreach (MyValue assignedSetup in optionPanel.assignedPlayerSetups)
            {
                if (assignedSetup.Keyword == playerID)
                {
                    toRemove = assignedSetup;
                }
            }
            optionPanel.assignedPlayerSetups.Remove(toRemove);

        }
        AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.LOBBY_CREATE_PLAYER_SLOTS)); //using UI command, as this function is called from client side
    }

    internal void AddToUICommands(MultiplayerUICommand incCommand)
    {
        lock (MultiplayerUICommands)
        {
            MultiplayerUICommands.Add(incCommand);
        }
    }

    public void DisplayMessageToPlayer(string message, string playerID, Color color)
    {
        FindPlayerControllerGameObject(playerID).GetComponent<PlayerController>().DisplayMessage(message, color);
    }

    /// <summary>
    /// performs ALL the actions needed before creating new server
    /// </summary>
    public void RemoveServer()
    {
        GameEngine.ActiveGame.DisconnectProtocol();
        GameEngine.ActiveGame.threadController.threadsToExit.Clear();
        GameEngine.ActiveGame.threadController.threadsControl.Clear();
        GameEngine.ActiveGame.clientManager.multiplayerMessages.Clear();
        GameEngine.ActiveGame.clientManager.previousMessages.Clear();
        GameEngine.ActiveGame.MultiplayerUICommands.Clear();
        GameEngine.ActiveGame.hostTasks.Clear();
        GameEngine.ActiveGame.optionPanel.isHost = false;
    }

    public void DisconnectProtocol()
    {
        bool debug = true;
        if (debug)
        {
            Debug.Log("Disconnect Protocol start");
        }
        GameEngine.ActiveGame.DeletePlayerPrefabs();
        GameEngine.ActiveGame.scenario = null;
        if (GameEngine.ActiveGame.clientManager.multiplayer != null)
        {
            GameEngine.ActiveGame.clientManager.multiplayer.CloseSockets(MultiplayerMessage.Pressed_Disconnect);
            GameEngine.ActiveGame.clientManager.multiplayer = null;
            Thread.Sleep(200);  
      
            GameEngine.ActiveGame.optionPanel.multiplayer = null;
            if (debug)
            {
                Debug.Log("Disconnect Protocol closing client");
            }
        }


        if (GameEngine.ActiveGame.optionPanel.mpServer != null)
        {
            GameEngine.ActiveGame.optionPanel.mpServer.stopThread = true;
            Thread.Sleep(30);
       

            //letting go of clients
            GameEngine.ActiveGame.optionPanel.mpServer.serverOn = false;
            GameEngine.ActiveGame.serverON = false;
            GameEngine.ActiveGame.optionPanel.mpServer.CloseSockets(MultiplayerMessage.Pressed_Disconnect);
            //GameEngine.ActiveGame.optionPanel.mpServer.CloseClientSockets();

            if (debug)
            {
                Debug.Log("Disconnect Protocol closing server");
            }
            GameEngine.ActiveGame.optionPanel.mpServer = null;
            //GameEngine.ActiveGame.optionPanel.mpServer.CloseSockets(MultiplayerMessage.Pressed_Disconnect);
            Thread.Sleep(200); //give a bit of time for the server
       
      
        }

        if (debug)
        {
            Debug.Log("Disconnect Protocol end");
        }
    }
    public void DeletePlayerPrefabs()
    {
        foreach (GameObject item in playerControllersList)
        {

            Destroy(item);
        }
        playerControllersList.Clear();
    }
    void SendMultiplayerMap()
    {
        string scenarioToText = "";
        try
        {
            scenarioToText = ConvertDataToText(scenario.Worldmap);
        }
        catch (Exception)
        {

            throw;
        }
        Debug.Log("map size: " + scenarioToText.Length);
        foreach (Player player in scenario.Players)
        {
            ServerClient cl = Server.GetClientByPlayerName(player.PlayerID);
            if (cl == null)
            {
                Debug.LogError(player.PlayerID + "'s client is null, count: " + Server.clients.Count);
            }

            string message = "recieveWorldmap|" + scenarioToText;
            Server.Broadcast(message, cl);
        }
    }

    internal void AddMapToCombatReplay(int v)
    {
        throw new NotImplementedException();
    }

    void SendMultiplayerArmies()
    {
        string scenarioToText = "";
        try
        {
            scenarioToText = ConvertDataToText(scenario.Armies);
        }
        catch (Exception)
        {

            throw;
        }
     
        foreach (Player player in scenario.Players)
        {
            ServerClient cl = Server.GetClientByPlayerName(player.PlayerID);
            if (cl == null)
            {
                Debug.LogError(player.PlayerID + "'s client is null, count: " + Server.clients.Count);
            }

            string message = "recieveArmies|" + scenarioToText;
            Server.Broadcast(message, cl);
        }
    }

    void SendMultiplayerScenarios()
    {
        string scenarioToText = "";
        try
        {
            scenarioToText = ConvertDataToText(scenario);
        }
        catch (Exception)
        {

            throw;
        }
      
        foreach (Player player in scenario.Players)
        {
            ServerClient cl = Server.GetClientByPlayerName(player.PlayerID);
            if (cl == null)
            {
                Debug.LogError(player.PlayerID + "'s client is null, count: " + Server.clients.Count);
            }
     
            string message = "recieveScenarios|" + scenarioToText;
            Server.Broadcast(message, cl);
        }
    }

    /// <summary>
    /// called from host
    /// </summary>
    /// <param name="incPlayers"></param>
    void InstantiateMultiplayerPrefabs(List<Player> incPlayers)
    {
       
        foreach (Player player in incPlayers)
        {
            ServerClient cl = Server.GetClientByPlayerName(player.PlayerID);
            if (cl == null)
            {
                Debug.LogError(player.PlayerID + "'s client is null, count: " + Server.clients.Count);
            }
            string playerToText = ConvertDataToText(player);
            string message = "createPlayerController|" + playerToText;
            Server.Broadcast(message, cl);
        }
    }
    public void InstantiatePlayerPrefabs(List<Player> incPlayers)
    {
        DeletePlayerPrefabs();

        playerControllersList.Clear();

        foreach (Player player in incPlayers)
        {
             

            GameObject playerControllerGameObject = (GameObject)Instantiate(playerPrefab);
            //Grid grid = playerControllerGameObject.GetComponent<Grid>();
            //grid.cellSize = new Vector3(1.16f,1.4f,0);
            playerControllerGameObject.transform.SetParent(playersGameObject.transform);
            playerControllersList.Add(playerControllerGameObject);
            PlayerController playerController = playerControllerGameObject.GetComponent<PlayerController>();
            playerController.PlayerID = player.PlayerID;
            playerController.BackgroundTile = player.PlayerID + "_bg";
            playerController.StartMethod(player,false); //false as this is old method for manual play
        }
    }
    /// <summary>
    /// can be used for MP
    /// removes controller from player
    /// becomes an observer if has no more 
    /// </summary>
    public void SetPlayerAsAI(string playerID)
    {
        if (scenario.Ended)
        {
            return;
        }
        Player player = scenario.FindPlayerByID(playerID);
        player.isAI = true;
        PlayerSetup playerSetup = scenario.GetPlayerSetupByPlayerID(playerID);


        if (playerSetup.ComputerName == GameEngine.PLAYER_IDENTITY)
        {
            playerSetup.ComputerName = ""; //doing it here before re-initilizing
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.RE_INITIATE_PLAYER_CONTROLLERS));
        }

        playerSetup.ComputerName = "";

        if (isHost)
        {
            switch (player.GameState.Keyword)
            {
                case GameState.State.MAIN_PHASE:
                    StartAI(playerID);
                    break;
                case GameState.State.EVENT_PHASE:
                    player.resolveEvents(true); //hopefully previous results is not a problem here
                    break;
                case GameState.State.BATTLE_PHASE:
                    lock (scenario.ActiveBattles.Battlefields)
                    {
                        foreach (BattlefieldOld activeBattlefield in scenario.ActiveBattles.Battlefields)
                        {
                            //if not auto battle, and is the players turn, then start the AI
                            if (activeBattlefield.IsPlayersTurn(playerID) && !activeBattlefield.IsPlayerAutoBattle(playerID))
                            {
                                activeBattlefield.StartPlayerAI(playerID);  
                            }
                        }
                    }
                    
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>
    /// client side
    /// isAI is true if called for AI client, and this should work 100% of the time as AI clients always come after players
    /// and is necessary for host's/other AIs prefabs to not get destroyed
    /// </summary>
    /// <param name="player"></param>
    public void InstantiateSinglePlayerPrefab(Player player,bool isAI, bool setActive, bool isObserver)
    {
        bool debug = true;

        if (debug)
        {
            Debug.Log("InstantiateSinglePlayerPrefab player " + player.PlayerID + " isAI = " + isAI.ToString() + " objects count: " + playerControllersList.Count);
        }

        //if (!isAI)
        //{
        //    DeletePlayerPrefabs();

        //    playerControllersList.Clear();


        //}


        GameObject playerControllerGameObject = (GameObject)Instantiate(playerPrefab);
        //Grid grid = playerControllerGameObject.GetComponent<Grid>();
        //grid.cellSize = new Vector3(1.16f,1.4f,0);
        playerControllerGameObject.transform.SetParent(playersGameObject.transform);
        playerControllersList.Add(playerControllerGameObject);
        PlayerController playerController = playerControllerGameObject.GetComponent<PlayerController>();
        playerController.PlayerID = player.PlayerID;
        playerController.BackgroundTile = player.PlayerID + "_bg";
        playerController.isObserver = isObserver;
        playerController.StartMethod(player,isAI);
        menuController.anyPlayerController = playerController;
        playerController.testStr = player.PlayerID;
        playerController.chatPanel = menuController.chatPanel.GetComponent<ChatPanelController>();
        if (isHost)
        {
            playerController.chatPanel.seePlayersButton.gameObject.SetActive(true);
            if (playerController.chatPanel.seePlayersButton.onClick.GetPersistentEventCount() == 0) //preventing from adding seeplayersclick too many times if a host has multiple player controllers
            {
                playerController.chatPanel.seePlayersButton.onClick.AddListener(playerController.chatPanel.SeePlayersClick);
            }
            
        }
        else
        {
            playerController.chatPanel.seePlayersButton.gameObject.SetActive(false);
            playerController.chatPanel.seePlayersButton.onClick.RemoveAllListeners();
        }
       
        Debug.Log("creating playercontroller for player: " + player.PlayerID);
        Army selectArmy = GameEngine.ActiveGame.scenario.GetFirstAvalibleArmyOfPlayer(playerController.PlayerID, true);
        if (selectArmy != null)
        {
            playerController.Selection.SelectArmy(selectArmy.ArmyID);
        }

        playerController.RefreshUI();

        if (debug)
        {
            Debug.Log("InstantiateSinglePlayerPrefab after add count " + playerControllersList.Count);
        }
        if (!setActive)
        {
            playerControllerGameObject.SetActive(false);
        }

    }
  
    public GameObject FindPlayerControllerGameObject(string playerID)
    {
        foreach (GameObject gameobj in playerControllersList)
        {
            PlayerController playerController = gameobj.GetComponent<PlayerController>();
            if (playerController.PlayerID == playerID)
            {
                return gameobj;
            }
        }
        return null;
    }
    public PlayerController FindPlayerController(string playerID)
    {
        foreach (GameObject gameobj in playerControllersList)
        {
            PlayerController playerController = gameobj.GetComponent<PlayerController>();
            if (playerController.PlayerID == playerID)
            {
                return playerController;
            }
        }
        return null;
    }
    //public void RefreshUI()
    //{
    //    DisplayLocalActivePlayer();
    //    RefreshTurnCounter();
    //    DisplayGemsAndHeroesOnPanel();
    //    RefreshUnitPanel();
    //    RefreshArmyPanel();
    //    RefreshBuildingInfoUI();
    //    RefreshArmyPanel();
    //    RefreshMultipleArmiesPanel();
    //}









    //public void InitializeUI()
    //{
    //    if (nextTurnBtn != null)
    //    {
    //        nextTurnBtn.onClick.AddListener(NextTurn);           
    //    }
    //    if (nextUnitBtn != null)
    //    {
    //        nextUnitBtn.onClick.AddListener(DisplayUnitOnPanel);
    //    }
    //    if (captureButton != null)
    //    {

    //        captureButton.onClick.AddListener(delegate { CaptureButtonClick(SelectedArmy); });
    //    }
    //    if (killHeroButton != null)
    //    {
    //        killHeroButton.onClick.AddListener(delegate { KillHeroButtonClick(SelectedArmy); });
    //    }
    //    if (destroyBuildingButton != null)
    //    {
    //        destroyBuildingButton.onClick.AddListener(delegate { DestroyBuildingButtonClick(selectedGameSquare); });
    //    }
    //}

    public void DestroyBuildingButtonClick(GameSquare gameSquare)
    {
        if (gameSquare != null)
        {
            
                gameSquare.building = null;
                OurLog.Print("building is destroyed");
                
            
            
        }
    }

    public void CaptureButtonClick(Army army)
    {
        Entity armyHero = scenario.FindUnitByUnitID(army.LeaderID);
        if (armyHero != null)
        {
            SetArmyMissionToCapture(army);
           
        }
        
    }
    public void KillHeroButtonClick(Army army)
    {
        Entity armyHero = scenario.FindUnitByUnitID(army.LeaderID);
        if (armyHero != null)
        {
            SetArmyMissionToKillHero(army);
            
        }
      
    }



    public void SetArmyMissionToSurvey(Army army)
    {
        if (army != null)
        {




            //OurLog.Print("Setting plot to kill the army hero");
            Debug.Log("army mission set to survey");
            Entity hero = scenario.FindUnitByUnitID(army.LeaderID);


            //we toggle off the mission // moved this if block above hero.setmission, otherwise this block makes 0 sense
            if (hero.Mission != null)
            {
                if (hero.Mission.MissionName == Mission.mission_Survey)
                {
                    MultiplayerMessage nullifyMission = new MultiplayerMessage(MultiplayerMessage.NullEntityMission, hero.UnitID.ToString(), "");
                    GameEngine.ActiveGame.clientManager.Push(nullifyMission);

                    hero.Mission = null;
                    return;
                }
            }


            hero.SetMission(Mission.mission_Survey, army.LeaderID, 0, army.ArmyID);

            //SelectedArmy.Units.Remove(armyHero);

            
        }
    }
    public void SetArmyMissionToKillHero(Army army)
    {
        if (army != null)
        {
            //OurLog.Print("Setting plot to kill the army hero");
            //army.SetMission(Mission.mission_KillHero, army.LeaderID, army.LeaderID, army.ArmyID);
            Entity armyHero = scenario.FindUnitByUnitID(army.LeaderID);
            army.Units.Remove(armyHero);
           
           
        }
    }

    public void SetArmyMissionToCapture(Army army)
    {
        if (army != null)
        {
           
             
            GameSquare gameSquareToBeCaptured = GameEngine.Map.FindMapSquareByCordinates(army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);

            if (gameSquareToBeCaptured.building == null)
            {
                OurLog.Print("Uncapturable gamesquare");
            }
            else
            {

                OurLog.Print("Capture flag is set");
                Entity leader = scenario.FindUnitByUnitID(army.LeaderID);

                //we toggle off the mission
                if (leader.Mission != null)
                {
                    if (leader.Mission.MissionName == Mission.mission_Capture)
                    {
                        MultiplayerMessage nullifyMission = new MultiplayerMessage(MultiplayerMessage.NullEntityMission, leader.UnitID.ToString(), "");
                        GameEngine.ActiveGame.clientManager.Push(nullifyMission);
                        leader.Mission = null;
                        return;
                    }
                }

                leader.SetMission(Mission.mission_Capture, army.LeaderID, gameSquareToBeCaptured.ID, 0);

                MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.SetEntityMission, leader.UnitID.ToString());
                GameEngine.ActiveGame.clientManager.PushMultiplayerObject(multiplayerMessage, leader.Mission);

                //OurLog.Print("Mission: " + army.Mission.missionName);
                if (army.LeaderID == gameSquareToBeCaptured.building.OwnerHeroID)
                {
                    Debug.LogError("hero is trying to capture the building it already owns");
                }
            }
            //if (gameSquareToBeCaptured.Owner_ID != "")
            //{
            //    OurLog.Print("Capture flag is set");
            //    army.SetMission(Mission.mission_Capture, army.LeaderID, gameSquareToBeCaptured.ID, 0);
            //    OurLog.Print("Mission: " + army.Mission.missionName);
            //}
            //else
            //{
            //    OurLog.Print("Uncapturable gamesquare");
            //}
            
        }
        else
        {
            OurLog.Print("no army was selected for capture"); //make ui for these messages
        }
        
    }

    public List<MemoryArmy> FindPlayerUniqueMemoryArmies(List<MemoryArmy> memoryArmies) {

        List<MemoryArmy> playerUniqueMemoryArmies = new List<MemoryArmy>();

        List<string> differentPlayers = new List<string>();

        foreach (MemoryArmy army in memoryArmies)
        {

            if (!differentPlayers.Contains(army.PlayerID))
            {
                differentPlayers.Add(army.PlayerID);
                playerUniqueMemoryArmies.Add(army);

            }
        }

        return playerUniqueMemoryArmies;

    }

    public void DisplayOverlandArmies(List<MemoryArmy> memoryArmies, List<Army> playersArmies, MemoryTile memoryTile)
    {
        bool debug = false;
        string mode = "";
        List<MemoryArmy> playerUniqueMemoryArmies = this.FindPlayerUniqueMemoryArmies(memoryArmies);
        int memoryPlayersCount = playerUniqueMemoryArmies.Count;
    
        if (playersArmies.Count > 0 && memoryPlayersCount == 0)
        {
            mode = MemoryArmy.SHOW_ONLY_PLAYER_ARMY;       
        }

        if (playersArmies.Count == 0 && memoryPlayersCount == 1) 
        {
            mode = MemoryArmy.SHOW_ONLY_OTHER_PLAYER_ARMY;
        }

        if (playersArmies.Count > 0 && memoryPlayersCount == 1)
        {
            mode = MemoryArmy.SHOW_PLAYER_AND_OTHER_PLAYER_ARMIES;
        }

        if (playersArmies.Count == 0 && memoryPlayersCount == 2)
        {
            mode = MemoryArmy.SHOW_TWO_OTHER_PLAYERS_ARMIES;
        }

        if (playersArmies.Count == 0 && memoryPlayersCount > 2)
        {
            mode = MemoryArmy.SHOW_THREE_OTHER_PLAYERS_ARMIES;
        }

        if (playersArmies.Count > 0 && memoryPlayersCount >= 2)
        {
            mode = MemoryArmy.SHOW_PLAYER_AND_TWO_OTHER_PLAYERS_ARMIES;
        }

       


        string armygraphics = "";
        string flagsGraphics = "";
        //string flag2 = "";
        //string flag3 = "";
        string flagHolder = "";
        string armybgGraphics = "";
        byte backgroundColor1 = 0;
        byte backgroundColor2 = 0;
        byte backgroundColor3 = 0;
        byte backgroundColorA = 0;
        byte flag1Color1 = 0;
        byte flag1Color2 = 0;
        byte flag1Color3 = 0;
        byte flag1ColorA = 0;
        byte flag2Color1 = 0;
        byte flag2Color2 = 0;
        byte flag2Color3 = 0;
        byte flag2ColorA = 0;
        byte flag3Color1 = 0;
        byte flag3Color2 = 0;
        byte flag3Color3 = 0;
        byte flag3ColorA = 0;

        if (debug)
        {
            Debug.Log("flag mode: " + mode);
        }

        switch (mode) {
            
            case MemoryArmy.SHOW_ONLY_PLAYER_ARMY:
                //get first avalible from playerarmies
                armygraphics = playersArmies[0].GetArmyPicture();
                armybgGraphics = MapPointedHex.playerBgDefault;
                backgroundColor1 = playersArmies[0].Color1;
                backgroundColor2 = playersArmies[0].Color2;
                backgroundColor3 = playersArmies[0].Color3;
                backgroundColorA = 255;
                //armybgcolor = MapPointedHex.playerBgDefault + playersArmies[0].OwnerPlayerID + "_bg";
                break;

            case MemoryArmy.SHOW_ONLY_OTHER_PLAYER_ARMY:
                armygraphics = playerUniqueMemoryArmies[0].PriorityUnitTileGraphics;
                armybgGraphics = MapPointedHex.playerBgDefault;
                backgroundColor1 = playerUniqueMemoryArmies[0].Color1;
                backgroundColor2 = playerUniqueMemoryArmies[0].Color2;
                backgroundColor3 = playerUniqueMemoryArmies[0].Color3;
                backgroundColorA = 255;
                //armybgcolor = MapPointedHex.playerBgDefault + playerUniqueMemoryArmies[0].PlayerID + "_bg";


                //Debug.Log("other : " + armybgcolor + " graphics " + armygraphics);
                break;
            case MemoryArmy.SHOW_PLAYER_AND_OTHER_PLAYER_ARMIES:
                //get players armies flag 1, visible armies flag 2
                //flagsGraphics = MapPointedHex.TwoPlayerFlagBase + playersArmies[0].OwnerPlayerID + MapPointedHex.Flag1;
                ////Debug.Log(flag1);
                //flag2 = MapPointedHex.TwoPlayerFlagBase + playerUniqueMemoryArmies[0].PlayerID + MapPointedHex.Flag2;
                //Debug.Log(flag2);
                flagsGraphics = MapPointedHex.TwoPlayerFlagBase;
                flag1Color1 = playersArmies[0].Color1;
                flag1Color2 = playersArmies[0].Color2;
                flag1Color3 = playersArmies[0].Color3;
                flag1ColorA = 255;

                flag2Color1 = playerUniqueMemoryArmies[0].Color1;
                flag2Color2 = playerUniqueMemoryArmies[0].Color2;
                flag2Color3 = playerUniqueMemoryArmies[0].Color3;
                flag2ColorA = 255;

                flagHolder = MapPointedHex.FlagHolder2;
                //Debug.Log("otherAndPlayer");




                armygraphics = "";
                armybgGraphics = "";
                backgroundColor1 = 0;
                backgroundColor2 = 0;
                backgroundColor3 = 0;
                //Debug.Log("otherAndPlayer : " + armybgcolor + " graphics " + armygraphics);
                break;
            case MemoryArmy.SHOW_TWO_OTHER_PLAYERS_ARMIES:
                //get [0] and [1] from visible armies
                //flagsGraphics = MapPointedHex.TwoPlayerFlagBase + playerUniqueMemoryArmies[0].PlayerID + MapPointedHex.Flag1;
                //flag2 = MapPointedHex.TwoPlayerFlagBase + playerUniqueMemoryArmies[1].PlayerID + MapPointedHex.Flag2;

                flagsGraphics = MapPointedHex.TwoPlayerFlagBase;
                flag1Color1 = playerUniqueMemoryArmies[0].Color1;
                flag1Color2 = playerUniqueMemoryArmies[0].Color2;
                flag1Color3 = playerUniqueMemoryArmies[0].Color3;
                flag1ColorA = 255;

                flag2Color1 = playerUniqueMemoryArmies[1].Color1;
                flag2Color2 = playerUniqueMemoryArmies[1].Color2;
                flag2Color3 = playerUniqueMemoryArmies[1].Color3;
                flag2ColorA = 255;


                flagHolder = MapPointedHex.FlagHolder2;
                armygraphics = "";
                armybgGraphics = "";
                backgroundColor1 = 0;
                backgroundColor2 = 0;
                backgroundColor3 = 0;
                break;
            case MemoryArmy.SHOW_THREE_OTHER_PLAYERS_ARMIES:
                //get [0] and [1] from visible armies plus gray one
                //flagsGraphics = MapPointedHex.TwoPlayerFlagBase + playerUniqueMemoryArmies[0].PlayerID + MapPointedHex.Flag1;
                //flag2 = MapPointedHex.TwoPlayerFlagBase + playerUniqueMemoryArmies[1].PlayerID + MapPointedHex.Flag2;
                //flag3 = MapPointedHex.Flag3;


                flagsGraphics = MapPointedHex.TwoPlayerFlagBase;
                flag1Color1 = playerUniqueMemoryArmies[0].Color1;
                flag1Color2 = playerUniqueMemoryArmies[0].Color2;
                flag1Color3 = playerUniqueMemoryArmies[0].Color3;
                flag1ColorA = 255;

                flag2Color1 = playerUniqueMemoryArmies[1].Color1;
                flag2Color2 = playerUniqueMemoryArmies[1].Color2;
                flag2Color3 = playerUniqueMemoryArmies[1].Color3;
                flag2ColorA = 255;

                flag3Color1 = playerUniqueMemoryArmies[2].Color1;
                flag3Color2 = playerUniqueMemoryArmies[2].Color2;
                flag3Color3 = playerUniqueMemoryArmies[2].Color3;
                flag3ColorA = 255;

                flagHolder = MapPointedHex.FlagHolder2Plus;
                backgroundColor1 = 0;
                backgroundColor2 = 0;
                backgroundColor3 = 0;
                break;

            case MemoryArmy.SHOW_PLAYER_AND_TWO_OTHER_PLAYERS_ARMIES:

                //get player flag and visible [0] flag and gray 3rd flag
                //flagsGraphics = MapPointedHex.TwoPlayerFlagBase + playersArmies[0].OwnerPlayerID + MapPointedHex.Flag1;
                //flag2 = MapPointedHex.TwoPlayerFlagBase + playerUniqueMemoryArmies[0].PlayerID + MapPointedHex.Flag2;
                //flag3 = MapPointedHex.Flag3;
                flagHolder = MapPointedHex.FlagHolder2Plus;



                flagsGraphics = MapPointedHex.TwoPlayerFlagBase;
                flag1Color1 = playersArmies[0].Color1;
                flag1Color2 = playersArmies[0].Color2;
                flag1Color3 = playersArmies[0].Color3;
                flag1ColorA = 255;

                flag2Color1 = playerUniqueMemoryArmies[1].Color1;
                flag2Color2 = playerUniqueMemoryArmies[1].Color2;
                flag2Color3 = playerUniqueMemoryArmies[1].Color3;
                flag2ColorA = 255;

                flag3Color1 = playerUniqueMemoryArmies[2].Color1;
                flag3Color2 = playerUniqueMemoryArmies[2].Color2;
                flag3Color3 = playerUniqueMemoryArmies[2].Color3;
                flag3ColorA = 255;


                backgroundColor1 = 0;
                backgroundColor2 = 0;
                backgroundColor3 = 0;
                break;

            default:
                if (memoryArmies.Count > 0) {
                    Debug.LogError("Something super wrong, should show other player army(ies) but passed all the switch cases");
                }

                if (playersArmies.Count > 0)
                {
                    Debug.LogError("Something super wrong, should show player army but passed all the switch cases");
                }

                break;


        }
        if (armygraphics == "")
        {
            //Debug.LogError("missing army graphics " + debugMode);
        }
        memoryTile.Flag1Color1 = flag1Color1;
        memoryTile.Flag1Color2 = flag1Color2;
        memoryTile.Flag1Color3 = flag1Color3;
        memoryTile.Flag1ColorA = flag1ColorA;

        memoryTile.Flag2Color1 = flag2Color1;
        memoryTile.Flag2Color2 = flag2Color2;
        memoryTile.Flag2Color3 = flag2Color3;
        memoryTile.Flag2ColorA = flag2ColorA;

        memoryTile.Flag3Color1 = flag3Color1;
        memoryTile.Flag3Color2 = flag3Color2;
        memoryTile.Flag3Color3 = flag3Color3;
        memoryTile.Flag3ColorA = flag3ColorA;


        memoryTile.ArmyBackgroundColor1 = backgroundColor1;
        memoryTile.ArmyBackgroundColor2 = backgroundColor2;
        memoryTile.ArmyBackgroundColor3 = backgroundColor3;
        memoryTile.ArmyBackgroundColorA = backgroundColorA;


        memoryTile.FlagPoleTileGraphics = flagsGraphics;      
        memoryTile.ArmiesTileGraphics = armygraphics;
        memoryTile.FlagHolderTileGraphics = flagHolder;
        memoryTile.ArmyBackgroundGraphics = armybgGraphics;

    }

   
    // we will reveal (armies, buildings) player sees in their visibility range
    public void RevealSquares(VisibleMapSquareList visibleSquares, Player player) {
        bool timer = false;
        if (timer)
        {
            this.GameStopwatch.Reset();
            this.GameStopwatch.Start();
        }
        foreach (VisibleMapSquare square in visibleSquares)
        {
            
            GameSquare gamesqr = square.mapSquare as GameSquare;
            // WE get the memorytile with previous data and then we overwrite most of the data
            MemoryTile memoryTile = player.MapMemory.FindMemoryTileByCoordinates(square.mapSquare.X_cord, square.mapSquare.Y_cord);
            memoryTile.GroundTileGraphics = gamesqr.Img; //this is graphical image of the terrain, terrain can have many images
            memoryTile.SightOnTile = square.Vision; // we have sight on tile
            memoryTile.KeywordsRequiredForPassing.Clear();
            memoryTile.LeaderRequirementsForPassing.Clear();
            memoryTile.MemoryArmies = new List<MemoryArmy>();

            List<OverlandEventWithColor> playerEvents = gamesqr.GetOverlandEventWithColor(player.PlayerID, true, true);

            if (playerEvents.Count>0) 
            {
                OverlandEventWithColor firstEvent = playerEvents[0];

                memoryTile.EventSymbolGraphicsColor1 = firstEvent.EventBackgroundColor1;
                memoryTile.EventSymbolGraphicsColor2 = firstEvent.EventBackgroundColor2;
                memoryTile.EventSymbolGraphicsColor3 = firstEvent.EventBackgroundColor3;
                memoryTile.EventSymbolGraphicsColorA = firstEvent.EventBackgroundColorA;

                EventTemplate firstEventTemplate = GameEngine.Data.EventTemplateCollection.findByKeyword(firstEvent.OverlandEvent.EventKeyword);

                memoryTile.EventTileBackgroundGraphics = firstEventTemplate.BackgroundGraphics;
                memoryTile.EventTileForegroundGraphics = firstEventTemplate.ForegroundGraphics;

                if (playerEvents.Count > 1)
                {
                    memoryTile.EventTileBackgroundGraphics = MapPointedHex.EventGlowStar;
                }

                if (firstEvent.OverlandEvent.Hidden) {
                    memoryTile.EventTileForegroundGraphics = "";
                    memoryTile.EventTileBackgroundGraphics = MapPointedHex.eventIndicator;
                }

            }
            else
            {
                memoryTile.EventTileBackgroundGraphics = "";
                memoryTile.EventTileForegroundGraphics = "";
            }

            // Rule is, if we see the square, we always see the terrain
            // if terrain changes, the square is no longer mapped
            if (memoryTile.TerrainTemplateKW != gamesqr.TerrainKeyword)
            {
                memoryTile.IsSurveyed = false;
            }

            memoryTile.TerrainTemplateKW = gamesqr.TerrainKeyword;
            TerrainTemplate template = Data.TerrainTemplateCollection.findByKeyword(gamesqr.TerrainKeyword);
            if (template == null) {
                Debug.LogError("Terrain template by keyword: (" + gamesqr.TerrainKeyword + ") not found");
            }

            foreach (List<string> traitKeyword in template.KeywordsRequriedForPassing)
            {
                memoryTile.KeywordsRequiredForPassing.Add(ObjectCopier.Clone(traitKeyword));
            }
            foreach (List<string> leaderKeywordGroup in template.LeaderRequirementsForPassing)
            {
                memoryTile.LeaderRequirementsForPassing.Add(ObjectCopier.Clone(leaderKeywordGroup));
            }

            bool buildingIsVisible = false; // this boolean was to help with treasure graphics (treasure chests on buildings did have different graphics (glow))

            if (gamesqr.building != null)
            {
                if (gamesqr.building.OwnerPlayerID == player.PlayerID) //if building belongs to player, reveal it right away and the square is mapped (surveyed)
                {
                    buildingIsVisible = true;
                    memoryTile.SetBuildingInfo(player, gamesqr.building,square.Vision);
               
                }
                else //if not, visibility check
                {
                    if (gamesqr.building.ID == memoryTile.BuildingID) //if we have seen this building before, we will see it again 100%
                  //  if (memoryTile.BuildingGraphics == gamesqr.building.Graphics)
                    {
                        buildingIsVisible = true;
                        Player buildingOwner = GameEngine.ActiveGame.scenario.FindPlayerByID(gamesqr.building.OwnerPlayerID);
                        
                        memoryTile.SetBuildingInfo(buildingOwner, gamesqr.building,square.Vision);
                       
                        
                    }
                    if (gamesqr.building.Concealment < memoryTile.SightOnTile)
                    {
                        buildingIsVisible = true;
                        Player buildingOwner = GameEngine.ActiveGame.scenario.FindPlayerByID(gamesqr.building.OwnerPlayerID);

                        memoryTile.SetBuildingInfo(buildingOwner, gamesqr.building,square.Vision);
                        memoryTile.BuildingID = gamesqr.building.ID;
                                       
                    }

                    
                    if (gamesqr.building.TraitKeywordsRequiredForPassing.Count > 0 || gamesqr.building.LeaderRequirementsForPassing.Count > 0)
                    {
                        buildingIsVisible = true;
                        Player buildingOwner = GameEngine.ActiveGame.scenario.FindPlayerByID(gamesqr.building.OwnerPlayerID);

                        memoryTile.SetBuildingInfo(buildingOwner, gamesqr.building, square.Vision);
                        memoryTile.BuildingID = gamesqr.building.ID;

                    }

                }


            }
            else //if no building is truly seen, then remove data(otherwise when building is razed, we still see it on overland map
            {
                memoryTile.ClearBuildingInfo();
            }

            //if (gamesqr.Inventory.Count > 0)
            //{
            //    if (buildingIsVisible)
            //    {
            //        memoryTile.EventTileForegroundGraphics = MapPointedHex.itemIndicatorOverLand;
            //    }
            //    else
            //    {
            //        memoryTile.EventTileForegroundGraphics = MapPointedHex.itemIndicator;
            //    }
  

            //}
            //else
            //{
            //    memoryTile.EventTileForegroundGraphics = "";
            //}


            if (memoryTile.GroundTileGraphics == "")
            {
                OurLog.PrintError("GameSquare (x:" + gamesqr.X_cord + ", y:" + gamesqr.Y_cord + ") is missing ground graphics");
            }


            //TEMPORARY
            //if (memoryTile.IsSurveyed)
            //{
            //    memoryTile.GroundTileGraphics = MapPointedHex.DirtCannon;
            //}

            memoryTile.VisibleResources.Clear();

            foreach (Resource resource in gamesqr.Resources) {
                // if there will be hidden/not revealed resources in the future, here is where we could do the check
                MemoryResource memoryResource = new MemoryResource();
                memoryResource.TemplateKeyword = resource.TemplateKeyword;
                memoryResource.Amount = resource.Amount;

                memoryTile.VisibleResources.Add(memoryResource);
            }

            memoryTile.FogOfWarTile = "";

            memoryTile.HiddenMapTile = "";

            //Army armyOnThisSquare = scenario.GetOverlandArmy(gamesqr.X_cord, gamesqr.Y_cord);
            List<Army> allArmiesOnThisSquare = scenario.FindAllOverlandArmiesByCoordinates(gamesqr.X_cord, gamesqr.Y_cord);
            //List<Army> playersArmies = new List<Army>();
            List<Army> playersArmies = scenario.GetAllPlayersArmiesOnCoordinates(player.PlayerID,gamesqr.X_cord, gamesqr.Y_cord);
            //foreach (Army army in playersArmies)
            //{
            //    //Debug.Log(army.WorldMapPositionX + " " + army.WorldMapPositionY.ToString());
            //}
            List<VisibleArmy> visibleArmies = new List<VisibleArmy>();
            foreach (Army army in allArmiesOnThisSquare)
            {
                if (army.OwnerPlayerID != player.PlayerID)
                {
                    //Entity leader = scenario.FindUnitByUnitID(army.LeaderID);
                    //if (leader.UnitName == "Behemoth The Giant Golem Cyclope")
                    //{
                    //    Debug.Log("cyclope id: " + army.LeaderID + " of army " + army.ArmyID + " of player " + army.OwnerPlayerID + " is detected by player " + player.PlayerID);
                    //}
                    VisibleArmy visibleArmy = CheckArmyForVisibility(army,square.Vision,gamesqr, buildingIsVisible);


                    if (visibleArmy.VisibleEntitiesIDs.Count > 0)
                    {
                        visibleArmies.Add(visibleArmy);
                    }
                }
                else
                {
                    //playersArmies.Add(army);
                }
               
            }
            //memoryTile.VisibleArmies = visibleArmies;
          
            foreach (VisibleArmy visibleArmy in visibleArmies)
            {
                //Debug.Log("converting visible army: " + visibleArmy.ArmyID + " " + visibleArmy.PlayerID + " " + visibleArmy.PriorityGraphics);
                MemoryArmy memoryArmy = MemoryArmy.ConvertVisibleArmyToMemoryArmy(visibleArmy);
                //Debug.Log("converted visible army: " + memoryArmy.ArmyID + " " + memoryArmy.PlayerID + " " + memoryArmy.PriorityGraphics);
                memoryTile.MemoryArmies.Add(memoryArmy);
            }
            DisplayOverlandArmies(memoryTile.MemoryArmies, playersArmies, memoryTile);
            //if (flag1 != "" && flag2 != "" && flag3 != "" && flagHolder != "") //make these checks when drawing ui
            //{

            //}

            //if (allArmiesOnThisSquare.Count > 0)
            //{
            //    memoryTile.ArmiesTileGraphics = allArmiesOnThisSquare[0].GetArmyPicture();
            //    memoryTile.ArmyBackgroundColor = allArmiesOnThisSquare[0].OwnerPlayerID;

            //}
            //else
            //{
            //    memoryTile.ArmiesTileGraphics = "";
            //    memoryTile.ArmyBackgroundColor = "";
            //}

            if (gamesqr.BattleFieldID > -1)
            {
                memoryTile.CombatIndicatorsTileGraphics = MapPointedHex.battleIndicator;
              
            }
            else
            {
                memoryTile.CombatIndicatorsTileGraphics = "";
            }
            string buildingVisibilityMode = MapMemory.GetBuildingVisibilityStatus(gamesqr,memoryTile,player);
            switch (buildingVisibilityMode)
            {
                case Player.HAS_NOT_SURVEYED_THIS_SQUARE_THAT_HAS_NO_BUILDINGS:
                case Player.HAS_SURVEYED_THIS_SQUARE_OUTSIDE_VISION_RANGE_THAT_HAS_NO_BUILDINGS:
                case Player.HAS_SURVEYED_THIS_SQUARE_IN_VISION_RANGE_THAT_HAS_NO_BUILDINGS:
                case Player.KNOWS_THERE_IS_SOME_BUILDING_AND_INSIDE_VISION:
                case Player.KNOWS_THERE_WAS_SOME_BUILDING_OUTSIDE_VISION:
                case Player.DOES_NOT_KNOW_ABOUT_ANY_BUILDING_THERE_BUT_THERE_IS_ONE:
                    memoryTile.PlayerFlagTileGraphics = "";
                    break;
                case Player.SEES_ENEMY_BUILDING:
                case Player.IS_OWNING_THIS_BUILDING:
                    Player buildingOwner = scenario.FindPlayerByID(gamesqr.building.OwnerPlayerID);
                    //string debugString = "owner is null";
                    if (buildingOwner != null)
                    {
                       // debugString = "owner not null";
                        memoryTile.PlayerFlagTileGraphics = MapPointedHex.PlayerFlagBase;
                        memoryTile.BuildingFlagPoleGraphics = MapPointedHex.BuildingFlagPole;
                        memoryTile.Color1 = buildingOwner.Color1;
                        memoryTile.Color2 = buildingOwner.Color2;
                        memoryTile.Color3 = buildingOwner.Color3;
                    }
                    //Debug.Log(buildingVisibilityMode + " X: " + gamesqr.X_cord + " Y: " + gamesqr.Y_cord + " RGB: " + memoryTile.Color1 + " " + memoryTile.Color2 + " " + memoryTile.Color3 + " OWNER: " + gamesqr.building.OwnerPlayerID + " " + debugString);
                    //memoryTile.PlayerFlagTileGraphics = MapPointedHex.PlayerFlagBase + gamesqr.building.OwnerPlayerID;
                    break;
                case Player.KNOWS_ENEMY_BUILDING_TYPE_BUT_NOW_OUTSIDE_VISION:
                    Player buildingOwner2 = scenario.FindPlayerByID(memoryTile.BuildingPlayerOwnerID);

                    if (buildingOwner2 != null)
                    {
                        memoryTile.PlayerFlagTileGraphics = MapPointedHex.PlayerFlagBase;
                        memoryTile.BuildingFlagPoleGraphics = MapPointedHex.BuildingFlagPole;
                        memoryTile.Color1 = buildingOwner2.Color1;
                        memoryTile.Color2 = buildingOwner2.Color2;
                        memoryTile.Color3 = buildingOwner2.Color3;
                    }
                   

                    //memoryTile.PlayerFlagTileGraphics = MapPointedHex.PlayerFlagBase + memoryTile.BuildingPlayerOwnerID;
                    break;
                default:
                    break;

            }

        }
        if (timer)
        {
            this.GameStopwatch.Stop();
            Debug.Log("RevealSquares test took " + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
        }
    }


    public VisibleArmy CheckArmyForVisibility(Army army,double vision, GameSquare gamesquare,bool buildingIsVisible)
    {
        VisibleArmy visibleArmy = new VisibleArmy();
        visibleArmy.ArmyID = army.ArmyID;
        visibleArmy.PlayerID = army.OwnerPlayerID; //save it for bg color
        bool showDueToGarrison = false;
        if (scenario.Stealth)
        {
            if (gamesquare.building != null)
            {
                if (gamesquare.building.OwnerPlayerID == army.OwnerPlayerID)
                {
                    switch (gamesquare.building.HideGarissonMode)
                    {
                        case BuildingTemplate.ARMY_HIDE_MODE_HIDE_ALWAYS:
                            return visibleArmy;
                        case BuildingTemplate.ARMY_HIDE_MODE_HIDE_IF_BUILDING_HIDDEN:
                            if (!buildingIsVisible)
                            {
                                return visibleArmy;
                            }
                            break;
                        case BuildingTemplate.ARMY_HIDE_MODE_HIDE_DO_NOT_HIDE:

                            break;
                        default:
                            Debug.LogError("invalid building hide mode: " + gamesquare.building.HideGarissonMode);
                            break;
                    }
                }
                if (army.ArmyID == gamesquare.building.GarissonArmyID && gamesquare.building.TraitKeywordsRequiredForPassing.Count > 0)
                {
                    showDueToGarrison = true;
                }
            }

        }


        double highestVisibility = 0;
        bool foundLeader = false;
        bool foundHero = false;

        foreach (Entity entity in army.Units)
        {
            if (entity.Concealment < vision || army.RevealedDueToArmyActions || showDueToGarrison)
            {
                visibleArmy.VisibleEntitiesIDs.Add(entity.UnitID);
                //if (army.OwnerPlayerID != MapPointedHex.Owner_Neutral)
                //{
                //    Debug.Log(player.PlayerID + " sees entity: " + entity.UnitName + " " + entity.UnitID + " " + army.OwnerPlayerID);
                //}
                if (!foundLeader)
                {
                    //leader takes priority and is shown over other heroes or units
                    if (entity.UnitID == army.LeaderID)
                    {
                        foundLeader = true;
                        visibleArmy.PriorityID = entity.UnitID;
                        visibleArmy.PriorityGraphics = entity.UnitAppearance;
                        visibleArmy.PortraitGraphics = entity.UnitPortrait;
                        visibleArmy.CharacterTemplateKeyword = entity.CharacterTemplateKeyword;
                        continue;
                    }

                    if (!foundHero)
                    {

                        // showing heroes take priority over normal units
                        if (entity.IsHeroFlag)
                        {
                            foundHero = true;
                            visibleArmy.PriorityID = entity.UnitID;
                            visibleArmy.PriorityGraphics = entity.UnitAppearance;
                            visibleArmy.PortraitGraphics = entity.UnitPortrait;
                            visibleArmy.CharacterTemplateKeyword = entity.CharacterTemplateKeyword;
                            continue;
                        }

                        //unless hero/leader already found, when entity with higher visibility is found, it will be prioritized to show
                        if (entity.Concealment > highestVisibility)
                        {
                            highestVisibility = entity.Concealment;
                            visibleArmy.PriorityID = entity.UnitID;
                            visibleArmy.PriorityGraphics = entity.UnitAppearance;
                            visibleArmy.PortraitGraphics = entity.UnitPortrait;
                            visibleArmy.CharacterTemplateKeyword = entity.CharacterTemplateKeyword;
                        }

                    }






                }

            }
        }
        return visibleArmy;

    }

    // Calculates vision range based on sight
    public static int CalculateVisionRange(double vision)
    {
        bool debug = false;

        

        int visionRange = 0;
        int counter = 1;
        for (int i = 0; i < vision; i = i + counter)
        {
            if (debug)
            {
                Debug.Log("i:" + i + " \t counter: " + counter + " \t vision: " + visionRange);
            }

            visionRange++;

            if (i % 2 == 0)
            {
                counter++;
            }

        }

        if (debug)
        {
            Debug.Log("Final visionRange for vision: " + vision.ToString() + " is " + visionRange);
        }

        return visionRange;
    }

    public void UpdateMemoryMapWithActiveVision(Player player)
    {
        Debug.Log("UpdateMemoryMapWithActiveVision for player " + player.PlayerID);
        bool timer = false;
        if (timer)
        {
            this.GameStopwatch.Reset();
            this.GameStopwatch.Start();
        }
 
        //Debug.Log("UpdateMemoryMapWithActiveVision stopwatch started, ms passed " + this.GameStopwatch.ElapsedMilliseconds.ToString());

        foreach (MemoryTile memorytile in player.MapMemory)
        {
            memorytile.FogOfWarTile = MapPointedHex.FogOfWar;
            memorytile.SightOnTile = 0;
            DisplayOverlandArmies(memorytile.MemoryArmies, new List<Army>(), memorytile);

        }
        VisibleMapSquareList visibleMapSquares = new VisibleMapSquareList();

        //Debug.Log("UpdateMemoryMapWithActiveVision before armies check, ms passed " + this.GameStopwatch.ElapsedMilliseconds.ToString());
        List<Army> battleAndOverlandArmies = new List<Army>();
        battleAndOverlandArmies.AddRange(scenario.Armies);
        battleAndOverlandArmies.AddRange(scenario.GetAllOverlandBattleArmies());
        foreach (Army army in battleAndOverlandArmies) //used to be scenario.Armies
        {

            if (army.OwnerPlayerID == player.PlayerID)
            {

                //int radius = (int)army.CombinedVisionRange;
                //double sight = army.Sight;

                visibleMapSquares = ApplySightToMapSquares(visibleMapSquares, army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate, army.GetVisions());


            }


        }

        //Debug.Log("UpdateMemoryMapWithActiveVision after armies check, ms passed " + this.GameStopwatch.ElapsedMilliseconds.ToString());

        foreach (GameSquare gamesqr in scenario.Worldmap.GameSquares)
        {
            if (gamesqr.building != null)
            {
                if (gamesqr.building.OwnerPlayerID == player.PlayerID)
                {
                    //int radius = (int)gamesqr.VisionRange;
                    //double sight = gamesqr.Sight;
                 
                    visibleMapSquares = ApplySightToMapSquares(visibleMapSquares, gamesqr.X_cord, gamesqr.Y_cord, gamesqr.building.GetVisions());

                  


                }
            }
            
        }

        //Debug.Log("Before revealSquares " + this.GameStopwatch.ElapsedMilliseconds.ToString());

        RevealSquares(visibleMapSquares, player);
        if (timer)
        {
            this.GameStopwatch.Stop();
            Debug.Log("UpdateMemoryMapWithActiveVision test took " + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
        }
      

        //Debug.Log(this.GameStopwatch.ElapsedMilliseconds.ToString() + " ms went on GameEngine.UpdateMemorymapWithActiveVision");

    }





    public VisibleMapSquareList ApplySightToMapSquares(VisibleMapSquareList visibleMapSquares,int xCord, int yCord, VisionInfoList radiuses)
    {
        MapSquare source = scenario.Worldmap.FindMapSquareByCordinates(xCord,yCord);
        
        //OurMapSquareList previousMapSquares = new OurMapSquareList();
        for (int radius = 0; radius < radiuses.Count; radius++)
        {
            VisionInfo visionInfo = radiuses[radius];
           
            if (visionInfo.bestVision <= 0 && radius > 0) {

                continue;
            }
            else
            {
                OurMapSquareList newVisibleSquares = scenario.Worldmap.GetMapSquaresInRadius(source, radius);
      
                foreach (MapSquare item in newVisibleSquares)
                {
                    
                    visibleMapSquares.AddHighest(item, visionInfo.bestVision);
                }
            }

           

            //previousMapSquares.AddRange(cleanedUpSquares);

        }
        return visibleMapSquares;
    }

    public void CreateHiddenMap(Player player)
    {
        string hiddenMapstr = MapPointedHex.Hidden;
        foreach (GameSquare gamesqr in scenario.Worldmap.GameSquares)
        {
            MemoryTile memoryTile = new MemoryTile();
            memoryTile.Coord_X = gamesqr.X_cord;
            memoryTile.Coord_Y = gamesqr.Y_cord;
            memoryTile.SquareID = gamesqr.ID;
            memoryTile.HiddenMapTile = hiddenMapstr;
           
          
            player.MapMemory.Add(memoryTile);
        }
        player.MapMemory.SetObjectNeighbours();

        UpdateMemoryMapWithActiveVision(player);
    }
    /// <summary>
    /// simply converts text into an object of specified class
    /// when using this method, use conversion to desired class,for example: Player plr = ConvertTextToData(text,typeof(Player)) as Player;
    /// to be used for server-client communications
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="text"></param>
    /// <param name="classSpecified"></param>
    /// <returns></returns>
    public static object ConvertTextToData(string text,Type type)
    {
        XmlSerializer serializer = new XmlSerializer(type);
        StringReader reader = null;
        bool debug = true;
        if (debug)
        {
            Debug.Log("reader data incoming: \n" + text);
        }
        try
        {
            reader = new StringReader(text);
           
        }
        catch (Exception e)
        {

            Debug.LogError("exeption " + e);
            Debug.LogError("inner exeption");
            Debug.LogError(e.InnerException);
        }

        object answer = null;
        try
        {
            answer = serializer.Deserialize(reader);
        }
        catch (Exception e)
        {

            Debug.LogError("exeption " + e);
            Debug.LogError("inner exeption");
            Debug.LogError(e.InnerException);
        }
        reader.Close();
        return answer;
    }

    public static Scenario getScenarioFromXML(string folder_path)
    {
        StreamReader reader = null;
        XmlSerializer serializer;
        reader = File.OpenText(folder_path);
        if (reader == null)
        {
            Debug.LogError("no file at path " + folder_path);
        }
        serializer = new XmlSerializer(typeof(Scenario));

        Scenario scenario;
        try
        {
            scenario = serializer.Deserialize(reader) as Scenario;
        }
        catch (Exception e)
        {

            throw e;
        }
        
        reader.Close();
      
        return scenario;
    }
    /// <summary>
    /// converts a serializable object into text
    /// to be used for server-client communications
    /// </summary>
    /// <param name="objectToSave"></param>
    /// <returns></returns>
    public static string ConvertDataToText(object objectToSave)
    {
        UTF8stringWriter writer;
        XmlSerializer xmlSerializer;
        bool debug = true;
        writer = new UTF8stringWriter();
        
        try
        {
            xmlSerializer = new XmlSerializer(objectToSave.GetType());
            xmlSerializer.Serialize(writer, objectToSave);

        }
        catch (Exception exception)
        {
            Debug.Log(exception);
            Debug.Log("InnerException : ");
            Debug.Log(exception.InnerException);
        }
        string answer = writer.ToString();
     //   string compactedAnswer = answer.Replace("\n", "").Replace("\r", "");
        //answer.Replace(" ", "");
      //  answer = compactedAnswer;
        if (debug)
        {
      
            
          //  string[] splitted = answer.Split('<');
            //int i = 0;
            //foreach (string str in splitted)
            //{
            //    if (i != 0)
            //    {
            //        g += "<";
            //    }
            //    g += str.TrimEnd();
            //    i++;
            //}
       
           // Debug.Log("gee: " + g);
            Debug.Log("data serialized: " + answer);
            Debug.Log("data serialized from object: " + objectToSave);
            //Player plr = objectToSave as Player;
            //Debug.Log("data serialized from object is: " + plr.PlayerID);
            //answer = g;
        }
        writer.Close();
      
        return answer;
    }

    public static void SaveGame(object objectToSave, string folder_path, string filename)
    {
        StreamWriter writer;
        XmlSerializer xmlSerializer;

        if (!Directory.Exists(folder_path))
        {
            Directory.CreateDirectory(folder_path);
        }

        writer = File.CreateText(folder_path + filename);
        try
        {
            xmlSerializer = new XmlSerializer(objectToSave.GetType());
            xmlSerializer.Serialize(writer, objectToSave);

        }
        catch (Exception exception)
        {
            Debug.Log(exception);
            Debug.Log("InnerException : ");
            Debug.Log(exception.InnerException);
        }
        writer.Close();
    }



    public void NextTurn(string playerID)
    {
       // scenario.SetNextPlayerAsActive("next turn"); //commented out for testing
       scenario.NEWEndTurnProcess(playerID);


    }

 

    public void DisplayLocalActivePlayer(bool allOFF)
    {
        foreach (Player plr in scenario.Players)
        {
            if (plr == ActivePlayer || !allOFF)
            {
                //GameEngine.ActiveGame.ToggleMainCamera(false);
                GameObject playerControllerGameObject = FindPlayerControllerGameObject(plr.PlayerID);
                if (playerControllerGameObject == null)
                {
                    Debug.LogError("failed to find controller obj for player " + plr.PlayerID + " objects count: " + playerControllersList.Count);
                    continue;
                }
                PlayerController playerController = playerControllerGameObject.GetComponent<PlayerController>();
                playerControllerGameObject.SetActive(true);
                 
                Army selectArmy = GameEngine.ActiveGame.scenario.GetFirstAvalibleArmyOfPlayer(playerController.PlayerID,true);
                if (selectArmy != null) {
                    playerController.Selection.SelectArmy(selectArmy.ArmyID);
                }
               
                playerController.RefreshUI();
            }
         

            if (allOFF || plr != ActivePlayer)
            {
                GameObject playerControllerGameObject2 = FindPlayerControllerGameObject(plr.PlayerID);
                PlayerController playerController2 = playerControllerGameObject2.GetComponent<PlayerController>();
                
                playerControllerGameObject2.SetActive(false);
            }

        }

    }
 

   public void ChangeLanguage(string lang)
    {
        GameEngine.ActiveGame.SetSelectedLanguage(lang);
        //GameControl.control.SaveGeneralOptions();
        GameEngine.ActiveGame.ChangeLanguage();
    }

    public void CheckGeneralOptionsIntegrity()
    {
        Debug.Log("OptionsIntegrityList.Count " + this.GeneralOptions.OptionsIntegrityList.Count);
        bool createNew = false;
        if (this.GeneralOptions == null)
        {
            Debug.Log("options were null");
            createNew = true;
        }
        if (!createNew)
        {
            if (this.GeneralOptions.OptionsIntegrityList == null)
            {
                Debug.Log("options list was null");
                createNew = true;
            }
            else
            {
                if (this.GeneralOptions.OptionsIntegrityList.Count == 0)
                {
                    Debug.Log("options list was empty");
                }
            }
        }
        GeneralOptions constructorOptions = new GeneralOptions("temp constructor for CheckGeneralOptionsIntegrity");
        //existing check
        if (!createNew)
        {
            List<GeneralOption> toAdd = new List<GeneralOption>();
            foreach (GeneralOption option in constructorOptions.OptionsIntegrityList)
            {
                GeneralOption existingOption = this.GeneralOptions.FindByKeyword(option.Keyword);
                if (existingOption == null)
                {
                    toAdd.Add(option);
                }
                else
                {
                    //tooltip check(if tooltip update, update the user options
                    if (existingOption.TooltipContent != option.TooltipContent)
                    {
                        toAdd.Add(option);
                    }
                }
            }

            foreach (GeneralOption option in toAdd)
            {
                Debug.Log("missing option: " + option.Keyword);
                GeneralOptions.AddByKeyword(option);
            }
        }
        //values check
        if (!createNew)
        {
            if (GetLanguages().Count == 0)
            {
                Debug.LogError("no languages found");
            }
            if (!GetLanguages().Contains(GeneralOptions.Language.StringValue))
            {
                GeneralOptions.Language.StringValue = LanguageBase.ENGLISH;

                if (!GetLanguages().Contains(LanguageBase.ENGLISH))
                {
                    GeneralOptions.Language.StringValue = GetLanguages()[0];
                }
            }
            if (GeneralOptions.TargetFrameRate.IntValue <= 0)
            {
                GeneralOptions.TargetFrameRate.IntValue = constructorOptions.TargetFrameRate.IntValue;
            }

            if (GeneralOptions.MusicVolume.IntValue < 0)
            {
                GeneralOptions.MusicVolume.IntValue = constructorOptions.MusicVolume.IntValue;
            }

            if (GeneralOptions.NotifyAboutInActiveHeroes.BoolValue != true && GeneralOptions.NotifyAboutInActiveHeroes.BoolValue != false)
            {
                GeneralOptions.NotifyAboutInActiveHeroes.BoolValue = constructorOptions.NotifyAboutInActiveHeroes.BoolValue;
            }

            if (GeneralOptions.PlayerAmount <= 0)
            {
                GeneralOptions.PlayerAmount = constructorOptions.PlayerAmount;
            }
 
             
        }

        if (createNew)
        {
            CreateGeneralOptions();
        }
        else
        {
            GameUtilities.save(this.GeneralOptions, generalOptionsPath, GeneralOptions.XMLTEXTFILE);
        }

    }
    public void CreateGeneralOptions()
    {
        Debug.Log("Creating new options file");
        this.GeneralOptions = new GeneralOptions("CreateGeneralOptions");
        // this.GeneralOption.DisplayHeight = graphics.PreferredBackBufferHeight;
        // this.GeneralOption.DisplayWidth = graphics.PreferredBackBufferWidth;
        // this.GeneralOption.FullScreen = graphics.IsFullScreen;
        GameUtilities.save(this.GeneralOptions, generalOptionsPath, GeneralOptions.XMLTEXTFILE);
    }
    public static string RGBToHex(int r, int g, int b)
    {
        return $"#{r:X2}{g:X2}{b:X2}";
    }
    public void CreateFileDirectories(string rootPath)
    {

        scenariosPath = rootPath + scenarioFolderPath;
        generalOptionsPath = rootPath + optionGeneralFolderPath;
        optionsPath = rootPath + optionFolderPath;
        profilesPath = rootPath + playerProfilesPath;
        modDataPath = rootPath + dataFolderPath;
        baseDataPath = rootPath + dataFolderPath + "/Base/";
       // baseDataOptionsPath = baseDataPath + "/Options/";
        savesPath = rootPath + saveFolderPath;

        battlefieldSetupsPath = savesPath + "Battlefield Setups/";
        scenario_savegamesPath = savesPath + "Scenario Saves/";
        battlefield_savegamesPath = savesPath + "Battle Saves/";
        randomMapGamemodePath = scenariosPath + "Random map/";
        campaignGamemodePath = scenariosPath + "Campaign/";
     



        List<String> folderPaths = new List<string>();
        folderPaths.Add(scenariosPath);
        folderPaths.Add(generalOptionsPath);
        folderPaths.Add(profilesPath);
        folderPaths.Add(modDataPath);
        folderPaths.Add(savesPath);
        folderPaths.Add(baseDataPath);
        //folderPaths.Add(baseDataOptionsPath);
        folderPaths.Add(optionsPath);
      

        folderPaths.Add(battlefieldSetupsPath);
        folderPaths.Add(scenario_savegamesPath);
        folderPaths.Add(battlefield_savegamesPath);
        folderPaths.Add(randomMapGamemodePath);
        folderPaths.Add(campaignGamemodePath);
        FolderCreation(folderPaths);


        // if Options (settings) file exists we load the old one, otherwise we create new one and save it 



        //  Debug.Log("Trying to load from " + generalOptionsPath + GeneralOptions.XMLTEXTFILE);
        if (File.Exists(generalOptionsPath + GeneralOptions.XMLTEXTFILE)) // 
        {

            this.GeneralOptions = GeneralOptions.getGeneralOptionsFromXML(generalOptionsPath);
            // I Used to set grahpical options here (fullscreen, resolution). Now, as they seem to be handled by Unity, I probably do not need to do that. I also could use playerprefs class. 
            CheckGeneralOptionsIntegrity();
        }
        else
        {
            CreateGeneralOptions();

        }
        
        if (File.Exists(optionsPath + ModManager.XMLTEXTFILE))
        {
            // Debug.Log("ModManager file exists");

            ModManager tempManager = ModManager.getCollectionFromXML(optionsPath);
            //ModManager tempManager = ModManager.getCollectionFromXML(modDataPath);
            this.modManager = new ModManager();

            // Why scenariosPath? KStolin 2020
            
            DirectoryInfo dirInfo = new DirectoryInfo(modDataPath);
            //DirectoryInfo dirInfo = new DirectoryInfo(scenariosPath);
            DirectoryInfo[] mods = dirInfo.GetDirectories();

            // this loads new detected mods, if there are existing mods according to modmanager xml file, and updates it
            foreach (DirectoryInfo currentinfo in mods)
            {
                if (tempManager.findByName(currentinfo.Name) == null)
                {

                    ModData newData = new ModData();
                    newData.setExists(true);
                    newData.Name = currentinfo.Name;
                    tempManager.ModDatas.Add(newData);
                    //tempManager.ModDatas.Insert(0, newData);

                }
                else
                {
                    tempManager.findByName(currentinfo.Name).setExists(true);
                }

            } // end foreach

            // removes old deleted ones? KStolin 2020

            foreach (ModData currentData in tempManager.ModDatas)
            {

                if (currentData.getExists())
                {
                    this.modManager.ModDatas.Add(currentData);
                }

            }


            GameUtilities.save(this.modManager, optionsPath, ModManager.XMLTEXTFILE);
        }
        else
        {

            this.modManager = new ModManager();

            DirectoryInfo dirInfo = new DirectoryInfo(scenariosPath);
            DirectoryInfo[] mods = dirInfo.GetDirectories();


            foreach (DirectoryInfo currentinfo in mods)
            {

                ModData newData = new ModData();
                newData.Name = currentinfo.Name;
                this.modManager.ModDatas.Add(newData);



            } // end foreach


            //   Debug.Log("ModManager file puudub");
            GameUtilities.save(this.modManager, optionsPath, ModManager.XMLTEXTFILE);

        }

    }// END method



    /// <summary>
    /// making folders for options, scenarios, etc, if they yet do not exist 
    /// </summary>
    public void FolderCreation(List<string> folderPaths)
    {

        foreach (string currentFolderPath in folderPaths)
        {

            if (!Directory.Exists(currentFolderPath))
            {
                Directory.CreateDirectory(currentFolderPath);
            }
        }
    }// end method

    public void RefreshAllPlayerUIs()
    {
        foreach (Player playerID in scenario.Players)
        {
            GameObject playerControllerObj = FindPlayerControllerGameObject(playerID.PlayerID);
            if (playerControllerObj != null)
            {

                PlayerController playerController = playerControllerObj.GetComponent<PlayerController>();
                playerController.RefreshUI(); //gotta refresh here so that combat grids go away

            }

        }
    }

    /// <summary>
    /// Starts processing active battles or finishes processing current one and starts the next one
    /// </summary>
    /// <param name="processedBattlefield"></param>
    public void ProcessBattlesQueue(BattlefieldOld processedBattlefield)
    {
        bool debug = true;

        if (debug) {
            OurLog.Print("starting after battles method  battles left " + scenario.QueuedUpBattles.Battlefields.Count);
            foreach (BattlefieldOld battlefield in this.scenario.QueuedUpBattles.Battlefields)
            {
                OurLog.Print("Before Battlefield  ID" + battlefield.ID);            
            }
        }

        //foreach (Player player in scenario.Players)
        //{
        //    if (player.GameState.Keyword != GameState.State.BATTLE_PHASE)
        //    {
        //        player.GameState = GameEngine.Data.GameStateCollection.findByKeyword(GameState.State.AFTER_BATTLE_PHASE);
        //    }
        //}

        // not setting gamestate here, as it is individual per player
        //foreach (Player player in scenario.Players)
        //{
        //    player.GameState = GameEngine.Data.GameStateCollection.findByKeyword(GameState.State.BATTLE_PHASE);
        //    if (scenario.GetPlayersActiveBattlesIDs(player.PlayerID).Count == 0) //if player has no battles to do, after battle phase
        //    {
        //        player.GameState = GameEngine.Data.GameStateCollection.findByKeyword(GameState.State.AFTER_BATTLE_PHASE);
        //    }
        //}


        if (processedBattlefield != null) //assign battle as completed or queued up for next turn
        {
            //processedBattlefield.DivideLoot(); //dividing loot BEFORE disengage for sake of event armies //cant do in before disengageArmies, so doing it there
            processedBattlefield.DisengageArmies();
            if (debug)
            {
                OurLog.Print("battlefield was not null with ID:" + processedBattlefield.ID);
            }
            
            //scenario.QueuedUpBattles.RemoveBattlefieldByID(processedBattlefield.ID);
            if (debug)
            {
                OurLog.Print("MIDDLE after battles method  battles left " + scenario.QueuedUpBattles.Battlefields.Count);
                foreach (BattlefieldOld battlefield in this.scenario.QueuedUpBattles.Battlefields)
                {
                    OurLog.Print("MIDDLE Battlefield ID:" + battlefield.ID);
                }
            }
            scenario.ActiveBattles.RemoveBattlefieldByID(processedBattlefield.ID);
            if (processedBattlefield.OnGoing)
            {
                lock (scenario.BattlesToBeContinued.Battlefields)
                {
                    scenario.BattlesToBeContinued.Battlefields.Add(processedBattlefield);
                }
                
            }
            else
            {
                lock (scenario.CompletedBattles.Battlefields)
                {
                    scenario.CompletedBattles.Battlefields.Add(processedBattlefield);
                }
              
                scenario.RemoveBattleZone(processedBattlefield.ID);
               //divideloot used to be here
            }
        }

        if (debug)
        {
            OurLog.Print("After of after battles method battles left:" + scenario.QueuedUpBattles.Battlefields.Count + " scenario armies count " + scenario.Armies.Count);

            foreach (Army army in scenario.Armies)
            {
                Debug.Log("army: " + army.GetInformationWithUnits());
            }
            
            foreach (BattlefieldOld battlefield in this.scenario.QueuedUpBattles.Battlefields)
            {
                OurLog.Print("After Battlefield  ID" + battlefield.ID);
            }
        }

        if (scenario.QueuedUpBattles.Battlefields.Count > 0)
        {
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, ""));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.INITIALIZE_BATTLEFIELD_MANAGMENT_PANEL));
            scenario.ActiveBattles.Battlefields.AddRange(scenario.QueuedUpBattles.Battlefields);
            scenario.QueuedUpBattles.Battlefields.Clear(); //clearing here because of new scenario all battles search function
                                                           //in this loop, for every player that participaes in any battle, we will open battle managment panel in their specific UI
                                                           //doing only for local computer
            foreach (PlayerSetup setup in scenario.GetPlayerSetupsByComputerName(GameEngine.PLAYER_IDENTITY))
            {

                foreach (BattlefieldOld battlefield in scenario.ActiveBattles.Battlefields)
                {
                    if (battlefield.GetCurrentParticipantPlayerIDs().Contains(setup.PlayerName))
                    {
                        //we have to send in list of battle ids because if we dont when we search for active player battles we might get none which gets the UI messed up
                        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.OPEN_BATTLE_MANAGMENT_PANEL, scenario.GetPlayersActiveBattlesIDs(setup.PlayerName), setup.PlayerName));
                        break;
                    }
                }
            }
            //this is an observer, he gets to see all battles
            if (scenario.GetPlayerSetupsByComputerName(GameEngine.PLAYER_IDENTITY).Count == 0)
            {
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.OPEN_BATTLE_MANAGMENT_PANEL_OBSERVER));

            }

            foreach (Player player in scenario.Players)
            {
                //if a player has absolutely no battles, we the gamestate to after battles phase
                //doing it here to minimize mp stuff
                if (scenario.GetPlayersActiveBattlesIDs(player.PlayerID).Count == 0)
                {
                    player.GameState = GameEngine.Data.GameStateCollection.findByKeyword(GameState.State.AFTER_BATTLE_PHASE);
                }
                else
                {
                    player.GameState = GameEngine.Data.GameStateCollection.findByKeyword(GameState.State.BATTLE_PHASE);
                }
            }
            //summon UI that can switch between battles

            //  scenario.ActiveBattles.Battlefields[0].EnablePlayerUIs(true); //TODO remove temporary
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.COMMAND_STOP_OBSERVING));
            foreach (BattlefieldOld battlefieldOld in scenario.ActiveBattles.Battlefields)
            {
                battlefieldOld.StartAIifPlayerIsAI();
            }
        }
        else //this here means that battles were resolved, and all players should be allowed to use continue button
        {
            //if processedBattlefield is null, and active battles count is 0, then that means there were no battles at all, and we proceed to afterbattles immediately
            bool afterBattle = false;
            lock (scenario.ActiveBattles.Battlefields)
            {
                if (scenario.ActiveBattles.Battlefields.Count == 0 && processedBattlefield == null)
                {
                    afterBattle = true;
                    //foreach (Player playerID in scenario.Players)
                    //{
                    //    //GameObject playerControllerObj = FindPlayerControllerGameObject(playerID.PlayerID);
                    //    //if (playerControllerObj != null)
                    //    //{

                    //    //    PlayerController playerController = playerControllerObj.GetComponent<PlayerController>();
                    //    //    playerID.GameState.Keyword = GameState.State.MAIN_PHASE;
                    //    //    playerController.ToggleCombatPanel(false, null);
                    //    //    playerController.RefreshUI(); //gotta refresh here so that combat grids go away

                    //    //}
                    //    playerID.GameState = GameEngine.Data.GameStateCollection.findByKeyword(GameState.State.AFTER_BATTLE_PHASE);
                    //}
                    //refresh for main phase UI
                    //GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.COMMAND_REFRESH_UI)); //it doesnt matter how and who recieves this, but currently active playercontroller must be refreshed
                    OurLog.Print("The battles count is 0");
                    if (debug)
                    {
                        Debug.Log("scenario.ActiveBattles.Battlefields.Count == 0 && processedBattlefield == null");
                    }
                }
                else
                {

                    if (isHost)
                    {
                        foreach (Player player in scenario.Players) //ai players, if they got no battles, auto after battle phase
                        {
                            if (player.isAI)
                            {
                                if (!HasActiveBattles(player.PlayerID)) //? what about events?
                                {

                                    player.GameState = GameEngine.Data.GameStateCollection.findByKeyword(GameState.State.AFTER_BATTLE_PHASE);
                                    if (processedBattlefield != null)
                                    {
                                        Debug.Log("battle event in battlefield not null for AI player: " + player.PlayerID);
                                        if (processedBattlefield.EventBattlePlayerID == player.PlayerID) //wtf how to do this right??
                                        {
                                            Debug.Log("resolve battle event in battlefield AI player: " + player.PlayerID);

                                            player.GameState = Data.GameStateCollection.findByKeyword(GameState.State.EVENT_PHASE);
                                            //player.resolveEvents(player.isAI, null);
                                            afterBattle = false; //!! just in case, but really not needed
                                            player.resolveEvents(true);
                                        }
                                        //else if (processedBattlefield.ResolveEvent) //failed attempt
                                        //{

                                        //}
                                    }
                                }
                            }
                        }
                    }
                    foreach (PlayerSetup setup in scenario.GetPlayerSetupsByComputerName(GameEngine.PLAYER_IDENTITY)) //we check per local client, as they control the uis for next state
                    {

                        if (!HasActiveBattles(setup.PlayerName))
                        {
                            bool showContinueButton = true; //we dont want to enable the continue button for player if 
                                                            //if player no longer has active battles, they get a continue button, which would place them into after battles phase, waiting on others to finish their battles
                            Debug.Log("battle event process start for " + setup.PlayerName);
                            if (processedBattlefield != null)
                            {
                                //if the event battle didnt belong to this player, then we dont want to show the continue button
                                //if without these checks, a bug can happen: AI did event battle, then you select even choice that gives event battle, and suddenly your continue button is avalible
                                if (processedBattlefield.EventBattlePlayerID != "" && processedBattlefield.EventBattlePlayerID != setup.PlayerName)
                                {
                                    showContinueButton = false;
                                }
                                Debug.Log("battle event in battlefield not null");
                                if (processedBattlefield.ResolveEvent)
                                {
                                    Debug.Log("resolve battle event in battlefield");
                                    Player player = scenario.FindPlayerByID(processedBattlefield.EventBattlePlayerID);
                                    player.GameState = Data.GameStateCollection.findByKeyword(GameState.State.EVENT_PHASE);
                                    //player.resolveEvents(player.isAI, null);
                                    afterBattle = false; //!! just in case, but really not needed
                                    processedBattlefield.ResolveEvent = false;
                                }
                            }
                            if (showContinueButton)
                            {
                                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.COMBAT_SHOW_CONTINUE_BUTTON, setup.PlayerName));
                            }



                        }
                    }



                }
            }

            //this means there were 0 battles this turn, so we automatically go afterbattles
            if (afterBattle) //this bool to do afterbattles outside lock
            {
                Debug.Log("after battle gee");
                scenario.AfterBattles(); //this to be threaded?
            }
        }

        //   GameEngine.ActiveGame.RefreshAllPlayerUIs();
    }
    /// <summary>
    /// both mp and sp method
    /// </summary>
    /// <param name="selectedEntityId"></param>
    /// <exception cref="NotImplementedException"></exception>
    internal void ToggleBlockMode(int selectedEntityId, string argsStr)
    {
        try
        {
            Entity unit = GameEngine.ActiveGame.scenario.FindUnitByUnitID(selectedEntityId);
            unit.BlockBlindUnits = !unit.BlockBlindUnits;
            string[] args = argsStr.Split('*');
            string playerID = args[0];
            int battlefieldID = -1;
            if (args.Length == 2)
            {
                battlefieldID = Int32.Parse(args[1]);
            }
           
          
            if (battlefieldID == -1) //called from overland
            {
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_IF_ENTITY_SELECTED_BY_PLAYER, selectedEntityId.ToString(), playerID));
            }
            else
            {
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_COMBAT_UI_PLAYER, battlefieldID.ToString(), playerID));
            }
            
        }
        catch (Exception e)
        {

            Debug.LogError("ToggleBlockMode " + e.Message + " " + e.StackTrace);
        }

    }

    bool HasActiveBattles(string playerID)
    {
        lock (scenario.ActiveBattles.Battlefields)
        {
            foreach (BattlefieldOld battlefield in scenario.ActiveBattles.Battlefields)
            {
                if (battlefield.GetCurrentParticipantPlayerIDs().Contains(playerID))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void RequeueBattlesAndRemoveHistory()
    {



        scenario.QueuedUpBattles.Battlefields.AddRange(scenario.BattlesToBeContinued.Battlefields);

        scenario.BattlesToBeContinued.Battlefields.Clear();




        //TODO serialize specific completed battles into a file
        //when game is saved, we have to load completed battles from the disc into memory, 
        //for history log we might want to consider either saving more important ones into save game 
        //we want to write what game turn battle was started, what game turn battle was completed

        scenario.CompletedBattles.Battlefields.Clear();

    }


    // Update is called once per frame

}
