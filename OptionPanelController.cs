using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using GameProj.Collections;
using GameProj.Options;
using UnityEngine.EventSystems;
using System.Threading;

public class OptionPanelController : MonoBehaviour //probably should rename this to lobby controller some time
{
    public GameObject bgCamera;
    public Button startBtn;
    public InputField amountOfPlayersGameObject;
    public GameObject lobbyRowObj;
    public InputField lobbyPasswordInputField;


    public InputField playerChatInput;
    public GameObject playerListPanel;
    public GameObject gameEngineObject;
    public GameObject menuGameObject;
    public GameObject playersPanel;
    public GameObject playerSlotPrefab;
    public GameObject optionsPanel;
    public GameObject optionPrefab;
    public List<GameObject> optionsPrefabList = new List<GameObject>();
    public GameObject clientPrefab;
    public GameObject playersListAndChatPanel;
    public GameObject factionPanel;
    List<string> playerNames = new List<string>();
    List<GameObject> playerSlots = new List<GameObject>();
    internal List<PlayerSetup> playerSetups = new List<PlayerSetup>();
    //keyword is playerid(slot), value is computer/user name
    internal OurMyValueList assignedPlayerSetups = new OurMyValueList(); //whenever player slots get refreshed(CreatePlayerSlots function), all playerSetups get wiped and then added again. This variable will apply changes after refresh to assign slots
    public Button sendMessageBtn;
    internal Server server;
    internal Multiplayer mpServer;
    internal Multiplayer multiplayer;
    public bool isHost = true;
    internal OptionCollection optionCollection; //this is the currently operated version of the options(at least should be)
    public Client client; //kinda the operator within optionpanelcontroller

    public GameObject serverPrefab; //might want to move this too

    internal int playerCount;
    internal LockableObject playerAmountCompare;
    public Text howManyPlayersText;

    public Dropdown dropDownButton;
    public Text dropDownOption;
    public Text serverIncomingText;
    public bool isLoading = false;


    bool debug = true;

    // Start is called before the first frame update
    void Start()
    {
        startBtn.onClick.AddListener(SubmitOptionValues);
        sendMessageBtn.onClick.AddListener(SendChatMessage);
        amountOfPlayersGameObject.onValueChanged.AddListener(delegate { CreatePlayerSlots(); });
        lobbyPasswordInputField.onValueChanged.AddListener(delegate { OnPasswordInputChange(); });

        Initialize();
    }

    public void Initialize()
    {

        playerAmountCompare = new LockableObject();

        //amountOfPlayersGameObject.text = "0";
        //dropDownButton.onValueChanged.AddListener(delegate { SingleplayerOrMultiplayer(dropDownOption.text); });

        if (!isHost)
        {
            dropDownButton.interactable = false;
            amountOfPlayersGameObject.interactable = false;
            startBtn.interactable = false;
            //playersListAndChatPanel.SetActive(true);

        }
        else
        {
            try
            {
                playerAmountCompare.intVal = int.Parse(amountOfPlayersGameObject.text) - 1;
            }
            catch (Exception)
            {
                Debug.LogError("amountOfPlayersGameObject.text: " + amountOfPlayersGameObject.text + ", setting to 2 ");
                amountOfPlayersGameObject.text = 2.ToString();
                playerCount = 2;
                // throw;
            }

            // playersListAndChatPanel.SetActive(false);
        }

        CreatePlayerSlots();

        FactionPanelController factionPanelController = factionPanel.GetComponent<FactionPanelController>();

        factionPanelController.optionPanel = this;

    }

    void OnPasswordInputChange()
    {
        GameEngine.ActiveGame.PasswordUsed = lobbyPasswordInputField.text;
    }
    internal void RemovePlayerFromSlot(string playerID)
    {
        PlayerSetup playerSetup = FindPlayerSetupByPlayerName(playerID);
        //empty slot, do nothing
        if (playerSetup.ComputerName == "")
        {
            return;
        }
        bool remove = false;
        //host can remove anyone
        if (isHost)
        {
            remove = true;
        }
        else if(playerSetup.ComputerName == GameEngine.PLAYER_IDENTITY)
        {
            //removing assigned player only if its the player itself doing it
            remove = true;
        }

        if (remove)
        {
            MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.UnAssignPlayerID,playerID,"");
            GameEngine.ActiveGame.clientManager.Push(multiplayerMessage);
        }

        
    }

    public void DisableAll()
    {
        amountOfPlayersGameObject.interactable = false;
        dropDownButton.interactable = false;
    }

    void SendChatMessage()
    {
        if (playerChatInput.text != "")
        {
            MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.ChatMessage, Environment.MachineName, playerChatInput.text);
            GameEngine.ActiveGame.clientManager.Push(multiplayerMessage);
            playerChatInput.text = "";
        }

        //old
        //if (playerChatInput.text != "" && client != null)
        //{
        //    client.Send("msg|" + playerChatInput.text);
        //    playerChatInput.text = "";
        //}
    }

    void SingleplayerOrMultiplayer(string selectedOption)
    {
        if (!isHost)
        {
            return;
        }
        if (selectedOption == "Singleplayer")
        {
            //networkManager.StopHost();
            try
            {
                playersListAndChatPanel.SetActive(false);
                if (debug)
                {
                    Debug.Log(DateTime.Now + " stopping server in optionPanelController");
                  
                }
                //server.StopListening();
                //serverIncomingText.text = "";
                //server = null;

                //client.CloseSocket();
                //client = null;
            }
            catch (Exception e)
            {

                Debug.Log(e.Message);
            }
        }
        else if (selectedOption == "Multiplayer")
        {
            // networkManager.StartHost();
            //CreateServer();
        }
    }
    public void CreateServer()
    {
        UnblockUI(); //doing it every time we create server(enter lobby) to counter the effects of BlockOptionsAndJoinSlots()
        try
        {
            if (debug)
            {
                Debug.Log(DateTime.Now + " starting server in optionPanelController");
            
              
            }
          
            assignedPlayerSetups.Clear(); //clear just in case some still made it after prev game start
                                          //if (server != null)
                                          //{
                                          //    server.StopListening();
                                          //    serverIncomingText.text = "";
                                          //    server = null;
                                          //}

            //if (client != null)
            //{
            //    client.CloseSocket();
            //    client = null;
            //}


            //playersListAndChatPanel.SetActive(true);
            //server = Instantiate(serverPrefab).GetComponent<Server>();
            //server.MessageOutput = serverIncomingText;
            ////server.StopListening();
            //server.Init();

            //client = Instantiate(clientPrefab).GetComponent<Client>();
            //client.optionsPanelText = serverIncomingText;
            //client.isHost = true;
            //client.optionsPanel = this;
            //client.isAI = false;
            //client.ConnectToServer(Client.GetLocalIPAddress(), 6321);
            if (mpServer == null)
            {
                Debug.Log("mpServer = null");
            }
            multiplayer = new Multiplayer();

            multiplayer.CreateServer();
            mpServer = multiplayer;
            Thread.Sleep(15);
            multiplayer = new Multiplayer();
            multiplayer.isHost = true;
            multiplayer.hostName = Environment.MachineName;
            multiplayer.JoinToServer("127.0.0.1", GameEngine.GAME_PORT); //automatically connect host client to server
         
            GameEngine.ActiveGame.clientManager.multiplayer = multiplayer;
            isHost = true;
            //if (isLoading) //if is loading, then by this point scenario from xml has been loaded, therefore send to server mp
            //{
            //    MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.SubmitScenarioToServer, GameEngine.PLAYER_IDENTITY);
            //    CompressedBytes compressedScenario = new CompressedBytes();
            //    compressedScenario.obj = ObjectByteConverter.ObjectToByteArray(GameEngine.ActiveGame.scenario);
            //}
            //  playerCount = multiplayer.Clients.Count;
            //  Debug.Log("!!!!!!!! multiplayer clients count: " + playerCount);

            //#region newCode
            //server2 = Instantiate(newServerPrefab).GetComponent<Server2>();
            //Debug.Log("server 2 instantiated");


            //client2 = Instantiate(newClientPrefab).GetComponent<Client2>();
            //client2.InitSocket(Client.GetLocalIPAddress(), 6321);
            //client2.SocketSend("AYO this worked !!1!");
            //Debug.Log("client 2 instantiated");

            //#endregion

        }
        catch (Exception e)
        {

            Debug.Log(DateTime.Now + " OptionPanelController.cs SingleplayerOrMultiplayer(): " + e.Message);
        }
    }

    //void SendPlayerIDsToPlayers()
    //{
    //    Debug.Log("SendPlayerIDsToPlayers assigned plrs count: " +assignedPlayerSetups.Count);
    //    foreach (PlayerSetup playerSetup in playerSetups)
    //    {
    //        if (playerSetup.ComputerName != "")
    //        {

    //            Debug.Log("SendPlayerIDsToPlayers: " + playerSetup.PlayerName + " " + playerSetup.ComputerName);
    //            MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.AddPlayerID, playerSetup.ComputerName, playerSetup.PlayerName);
    //            GameEngine.ActiveGame.clientManager.Push(multiplayerMessage);
    //        }
    //    }
    //}

    public void SubmitOptionValues()
    {
        if (!isHost)
        {
            return;
        }
        if (multiplayer == null)
        {
            Debug.LogError("server is null wtf, returning"); //strange
            return;
        }
        BlockOptionsAndJoinSlots(false);
        bool debug = false;
        if (isLoading)
        {
            
            GameEngine.ActiveGame.AwakeMethod();
            GameEngine.ActiveGame.scenario.OnLoad(); //deletes any player prefab, creates new ones, gives players vision

            //this placement is temp
            //TODO: send scenario on player connect
            MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.SubmitScenarioToServer, GameEngine.PLAYER_IDENTITY);
            CompressedBytes compressedScenario = new CompressedBytes();
            compressedScenario.obj = ObjectByteConverter.ObjectToByteArray(GameEngine.ActiveGame.scenario);
            Debug.Log("compressed scenario size: " + compressedScenario.obj.Length);
            //        Debug.Log("not compressed scenario size: " + ObjectByteConverter.ObjectToByteArrayTest(scenario).Length);
            GameEngine.ActiveGame.clientManager.PushMultiplayerObject(multiplayerMessage, compressedScenario);
            if (debug)
            {
                Debug.Log("game is loaded");
            }

        }
        else
        {
            //if (!isHost)
            //{
            //    return;
            //}
            //clearing assigned player setups otherwise they could theoretically carry on into next game/menu
            
            assignedPlayerSetups.Clear();

            GameEngine.ActiveGame = gameEngineObject.GetComponent<GameEngine>();
            GameEngine.ActiveGame.AwakeMethod();


            int amountOfPlayers = int.Parse(amountOfPlayersGameObject.text);



            if (amountOfPlayers >= 1) // was 2
            {
                if (debug)
                {
                    OurLog.Print(" OptionPanelController SubmitOptionValues(): Player amount set to: " + amountOfPlayers);
                }

                GameEngine.ActiveGame.amountOfPlayers = amountOfPlayers;
            }
       
            //Option playerCountOption = optionCollection.FindByKeyword(OptionCollection.PlayerCount);
            //playerCountOption.Values.findDefaultMyValue().Value = amountOfPlayersGameObject.text;
            GameEngine.ActiveGame.StartMethod(playerSetups,this);
            //GameEngine.ActiveGame.scenario.GameType = dropDownOption.text;

        }
        if (debug)
        {
            Debug.Log(GameEngine.ActiveGame.scenario.GameType);
        }

        //DefaultState defaultState = playerCountOption.DefaultStates.FindByKeywordAndValue(playerCountOption.Values.findDefaultMyValue().Keyword, playerCountOption.Values.findDefaultMyValue().Value);
        //defaultState.Value.Value = amountOfPlayersGameObject.text;
       // HideUI();
        
    }


    public void HideUI()
    {
        this.gameObject.SetActive(false);
        bgCamera.SetActive(false);
    }

    public void ResetOptionValues()
    {
        InputField inputField = amountOfPlayersGameObject.GetComponent<InputField>();
        inputField.text = "3";
    }

    public PlayerSetup FindPlayerSetupByPlayerName(string playerName)
    {
        foreach (PlayerSetup setup in playerSetups)
        {
            if (setup.PlayerName == playerName)
            {
                return setup;
            }
        }
        return null;
    }

    public void CreatePlayerSlots()
    {
        playerSetups.Clear();
        Debug.Log("CreatePlayerSlots call");
        if (amountOfPlayersGameObject.text == "")
        {
            return;
        }

        foreach (char letter in amountOfPlayersGameObject.text)
        {
            if (!char.IsNumber(letter))
            {
                amountOfPlayersGameObject.text = "2";
                return;
            }
        }
        howManyPlayersText.text = amountOfPlayersGameObject.text;
        if (isHost) //if host, send the new value of the option to others
        {
            MultiplayerMessage howManyPlayersMsg = new MultiplayerMessage(MultiplayerMessage.PlayersCount, amountOfPlayersGameObject.text, "");
            GameEngine.ActiveGame.clientManager.Push(howManyPlayersMsg);


           
        }
        if (client != null && isHost)
        {
            client.Send("playersCount|" + amountOfPlayersGameObject.text);
        }
        //lock redundant?
        // lock (playerAmountCompare)
        //check redundant due to multiplayer?
        //if (playerAmountCompare.intVal != int.Parse(amountOfPlayersGameObject.text))
        RectTransform rectTransform = playersPanel.GetComponent<RectTransform>();
        foreach (GameObject plrSlot in playerSlots)
        {
            Destroy(plrSlot);
        }
        playerSlots.Clear();
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 200);
        if (playerAmountCompare == null)
        {
            Debug.LogError("wtf");
        }
        if (amountOfPlayersGameObject == null)
        {
            Debug.LogError("wtf 2");
        }
        playerAmountCompare.intVal = int.Parse(amountOfPlayersGameObject.text);

        for (int i = 1; i < playerAmountCompare.intVal + 1; i++)
        {
            GameObject playerSlot = Instantiate(playerSlotPrefab);
            playerSlot.transform.SetParent(playersPanel.transform, false);
            playerSlots.Add(playerSlot);
            Text playerNameText = playerSlot.GetComponentInChildren<Text>();
            PlayerOptionSlotController playerOptionSlotController = playerSlot.GetComponent<PlayerOptionSlotController>();
            playerOptionSlotController.playerText.text = "player" + i.ToString();
            playerOptionSlotController.selectFactionButton.onClick.AddListener(delegate { OpenFactionPanel(playerOptionSlotController.playerText.text, playerOptionSlotController); });

            PlayerSetup playerSetup = new PlayerSetup();
            playerSetup.PlayerName = "player" + i.ToString();

            if (playerSetup.FactionKeyword == Faction.FACTION_RANDOM)
            {
                playerOptionSlotController.selectFactionButtonText.text = GameEngine.Data.generalUI.findByKeyword(playerSetup.FactionKeyword).correctLanguageString();
            }
            else
            {
                playerOptionSlotController.selectFactionButtonText.text = GameEngine.Data.FactionCollection.findByKeyword(playerSetup.FactionKeyword).correctLanguageString();
            }


            playerSetups.Add(playerSetup);

            Button joinBtn = playerSlot.GetComponentInChildren<Button>();
            joinBtn.onClick.AddListener(delegate { JoinSlot(playerNameText, playerOptionSlotController.playerText.text, playerSetup); });
            JoinAssignSlotRightClick joinAssignSlotRightClick = joinBtn.GetComponent<JoinAssignSlotRightClick>();
            joinAssignSlotRightClick.controller = this;
            joinAssignSlotRightClick.playerID = playerSetup.PlayerName;
            if (i > 3)
            {

                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectTransform.sizeDelta.y + 100);
            }
        }




        if (isLoading)
        {
            LoadGameSlots();
        }
        else
        {
            ApplyAssignedSlots();
        }

    }

    PlayerOptionSlotController FindPlayerSlotObjectByPlayerID(string playerID)
    {
        foreach (GameObject obj in playerSlots)
        {
            PlayerOptionSlotController playerOptionSlotController = obj.GetComponent<PlayerOptionSlotController>();
            if (playerOptionSlotController.playerText.text == playerID)
            {
                return playerOptionSlotController;
            }
        }
        return null;
    }

    /// <summary>
    ///  whenever player slots get refreshed(CreatePlayerSlots function), all playerSetups get wiped and then added again. This function will apply changes after refresh to assign slots(visual for clients, actual changes for host)
    /// </summary>
    void ApplyAssignedSlots()
    {
        Debug.Log("ApplyAssignedSlots call " + playerSetups.Count + " " + assignedPlayerSetups.Count);
        List<MyValue> toRemove = new List<MyValue>();
        lock (assignedPlayerSetups)
        {
            foreach (MyValue assignedSlot in assignedPlayerSetups)
            {
                //checking if previous players assigned for this slot
                PlayerOptionSlotController playerOptionSlotController = FindPlayerSlotObjectByPlayerID(assignedSlot.Keyword);
                //if this is null, it would for example mean: player assigned himself to slot 7, host reduced slots to 6, now this player doesnt have a slot assigned, so we remove from assignedPlayerSetups
                if (playerOptionSlotController == null)
                {
                    //no longer removing bc of sending the stuff before option col(prevent joing on slot before assigned slots arrive, also bc of reconnects??
                   // toRemove.Add(assignedSlot);
                    continue;
                }

                playerOptionSlotController.btnText.text = assignedSlot.Value; //setting UI to reflect
                PlayerSetup playerSetup = FindPlayerSetupByPlayerName(assignedSlot.Keyword);
                if (playerSetup == null)
                {
                    Debug.Log("no player setup found with playerid " + assignedSlot.Keyword + " setup count " + playerSetups.Count);
                }
                playerSetup.ComputerName = assignedSlot.Value; //setting data(really matters only to host)
            }
            foreach (MyValue slottoRemove in toRemove)
            {
                assignedPlayerSetups.Remove(slottoRemove);
            }
        }
      
    }

    public void OpenFactionPanel(string name, PlayerOptionSlotController playerOptionSlotController)
    {
        FactionPanelController factionPanelController = factionPanel.GetComponent<FactionPanelController>();
        factionPanelController.playerOptionSlotController = playerOptionSlotController;
        factionPanelController.Open(name);
    }
    /// <summary>
    /// call whenever theres a value change of any kind in any option
    /// </summary>
    /// <param name="option"></param>
    public void OnOptionValueChange(string newValue, string type, Option incOption)
    {
        bool debug = false;
        if (debug)
        {
            Debug.Log("incOption kw: " + incOption.Keyword + " new value: " + newValue + " type: " + type);
        }
        foreach (Option option in optionCollection.DataList)
        {
            bool destroy = false;
            if (option.DependantOptionState.Keyword == type && option.DependantOptionState.Value == newValue && option.DependantOptionKeyword == incOption.Keyword)
            {
                if (debug)
                {
                    Debug.Log("matched option: " + option.Keyword);
                }
                //if existing object for the option already exists then we dont need to do anything because otherwise it would be 
                //an annoying refresh thing
                GameObject existingObject = GetOptionObjectByKeyword(option.Keyword);
                if (existingObject != null)
                {
                    if (debug)
                    {
                        Debug.Log("matched option already has gameobject");
                    }
                    continue;
                }



                GameObject optionObject = GetOptionObjectByKeyword(incOption.Keyword);
                GameObject objectOfCurrentOption = null;
                if (optionObject == null)
                {
                    if (debug)
                    {
                        Debug.Log("recursion for next level dependency option: " + incOption.DependantOptionKeyword);
                    }
                    //this means that the incOption is an option with a dependency itself and it's requirements didn't match or it's wasnt first in the collection so current option cannot be
                    //therefore we try going for incOption's dependancy option
                    Option incOptionsOption = optionCollection.FindByKeyword(incOption.DependantOptionKeyword);
                    OnOptionValueChange(incOption.DependantOptionState.Value, incOption.DependantOptionState.Keyword, incOptionsOption);
                    //we try to get it again
                    optionObject = GetOptionObjectByKeyword(incOption.Keyword);
                }
                objectOfCurrentOption = GetOptionObjectByKeyword(option.Keyword);
                //if objectOfCurrentOption is not null means it might have been created within recursion
                if (optionObject != null && objectOfCurrentOption == null)
                {
                    OptionRowController optionRow = optionObject.GetComponent<OptionRowController>();
                    if (debug)
                    {
                        Debug.Log("creating option as a sub-option: " + option.Keyword);
                    }

                    GameObject newGameObject = Instantiate(optionPrefab, optionObject.transform, false);
                  
                  //  newGameObject.transform.SetParent();
                    optionsPrefabList.Add(newGameObject);
                    OptionRowController characterTemplateController = newGameObject.GetComponent<OptionRowController>();
                    //newGameObject.GetComponent<ContentSizeFitter>(). = true;
                    //TODO: change colors of characterTemplateController bg to a different color of optionRow


                    characterTemplateController.optionPanel = this;
                    if (isLoading || !isHost)
                    {
                        characterTemplateController.interactable = false;
                    }

                    if (!option.IncludePlaceHolder)
                    {
                        characterTemplateController.insertValueBoxPanel.gameObject.SetActive(false);
                    }
                    characterTemplateController.RowOption = option;
                    characterTemplateController.Refesh_text();
                    characterTemplateController.Generate_option_choices();
                }
                else
                {
                    //means that conditions of incOption's dependancy were not met after all so this one gets destroyed
                    destroy = true;
                }

            }
            else
            {
                //means this option is dependant but conditions were not met so we destroy the object
                if (option.DependantOptionKeyword == incOption.Keyword) 
                {
                    destroy = true;
                }
            }

           

            if (destroy)
            {
                if (debug)
                {
                    Debug.Log("destroying option object: " + option.Keyword);
                }
                GameObject optionObject = GetOptionObjectByKeyword(option.Keyword);
                if (optionObject != null)
                {
                    OptionRowController optionRowController = optionObject.GetComponent<OptionRowController>();
                    optionRowController.isDestroyed = true;
                }
            
                optionsPrefabList.Remove(optionObject);
                Destroy(optionObject);
            }
        }
        
    }

    public void CheckAllOptionsForDependantOptions(string caller)
    {
        bool debug = false;
        if (debug)
        {
            Debug.Log("options check called from: " + caller);
        }
        foreach (Option option in optionCollection.DataList)
        {
            OnOptionValueChange(option.Values.findDefaultMyValue().Value, option.Values.findDefaultMyValue().Keyword,option);
          
        }
        Canvas.ForceUpdateCanvases();

        foreach (Option option in optionCollection.DataList)
        {
            SetGameObjectSizeForOption(option);

        }

    }


    public void SetGameObjectSizeForOption(Option option)
    {
        bool debug = false;
        GameObject optionObj = GetOptionObjectByKeyword(option.Keyword);
        if (debug)
        {
            Debug.Log("trying to resize option: " + option.Keyword);
        }
        if (optionObj != null)
        {
            if (debug)
            {
                Debug.Log("object was found: " + option.Keyword);
            }

            int baseSize = 100;
            int sizeModifier = 0;
            foreach (OptionRowController subOption in optionObj.GetComponentsInChildren<OptionRowController>())
            {
                if (subOption.RowOption.Keyword == option.Keyword)
                {
                    continue;
                }

                if (subOption.isDestroyed)
                {
                    continue;
                }

                if (debug)
                {
                    Debug.Log("found a suboption gameobject: " + subOption.RowOption.Keyword + " of option " + option.Keyword);
                }

                sizeModifier++;
            }
            int finalSize = baseSize + (baseSize * sizeModifier);
            if (debug)
            {
               Debug.Log(option.Keyword +" final size: " + finalSize);
            }
            RectTransform rectTransform = optionObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, finalSize);

        }
        else
        {
            if (debug)
            {
                Debug.Log("no object for " + option.Keyword);
            }
        }
    }

    public GameObject GetOptionObjectByKeyword(string keyword)
    {
        foreach (GameObject obj in optionsPrefabList)
        {
            if (obj == null)
            {
                continue;
            }
            OptionRowController characterTemplateController = obj.GetComponent<OptionRowController>();

            if (characterTemplateController.RowOption.Keyword == keyword)
            {
                return obj;
            }
        }
        return null;
    }
    /// <summary>
    /// UI command to show options
    /// </summary>
    /// <param name="options"></param>
    public void LoadTemplates(OptionCollection options)
    {
        optionCollection = options;
        foreach (var item in optionsPrefabList)
        {
            Destroy(item);
        }
        optionsPrefabList.Clear();
        foreach (Option option in options.DataList)
        {

            if (option.Changeable && option.Showable && option.DependantOptionState.Keyword == "")
            {
                //       Debug.Log("Option keyword " + option.Keyword);
                GameObject characterTemplateRow = addOptionTemplateRow();
                optionsPrefabList.Add(characterTemplateRow);
                OptionRowController characterTemplateController = characterTemplateRow.GetComponent<OptionRowController>();
                characterTemplateController.optionPanel = this;
                if (isLoading || !isHost)
                {
                    characterTemplateController.interactable = false;
                }
              
                if (!option.IncludePlaceHolder)
                {
                    characterTemplateController.insertValueBoxPanel.gameObject.SetActive(false);
                }
                characterTemplateController.RowOption = option;
                characterTemplateController.Refesh_text();
                //Debug.LogError("option lang string count: " + option.LanguageStrings.Count);
                characterTemplateController.Generate_option_choices();
            }

            if (option.Keyword == OptionCollection.PlayerCount)
            {
                //this.howManyPlayersText.text = option.Values.findDefaultValue();
                //this.playerCount = Int32.Parse(option.Values.findDefaultValue());
                this.amountOfPlayersGameObject.text = option.Values.findDefaultMyValue().Value;
                howManyPlayersText.text = amountOfPlayersGameObject.text;
                Debug.Log("setting player amounts from options: " + option.Values.findDefaultMyValue().Value);
            }

            // characterTemplateController.Refesh_text();

        }

        CheckAllOptionsForDependantOptions("loadTemplates");
         
    }


    public GameObject addOptionTemplateRow()
    {

        GameObject newGameObject = (GameObject)Instantiate(optionPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        // newGameObject.transform.SetParent(contentPanel.transform, false); 
        newGameObject.transform.SetParent(optionsPanel.transform, false); //this could be contentpanel, so it could be scrollable
        return newGameObject;
    }

    public void ActivateLoadUI()
    {
        DisableAll();
        LoadTemplates(GameEngine.Data.OptionCollection);
        LoadGameSlots();
        
       // LoadTemplates(GameEngine.Data.OptionCollection);
        bgCamera.gameObject.SetActive(true);
        lobbyRowObj.gameObject.SetActive(true);
    }

    public void LoadGameSlots() //this needs work similar to apply slots. since the amount of playersetups is limited due to load game, we dont remove them
    {
        bool debug = false;
        RectTransform rectTransform = playersPanel.GetComponent<RectTransform>();
        foreach (GameObject plrSlot in playerSlots)
        {
            Destroy(plrSlot);
        }
        playerSlots.Clear();
 
        amountOfPlayersGameObject.text = GameEngine.ActiveGame.scenario.PlayerSetups.Count.ToString();
        int sizeCounter = 0;
        foreach (PlayerSetup setup in GameEngine.ActiveGame.scenario.PlayerSetups)
        {
            
            GameObject playerSlot = Instantiate(playerSlotPrefab);
            playerSlot.transform.SetParent(playersPanel.transform, false);
            playerSlots.Add(playerSlot);

            PlayerOptionSlotController playerOptionSlotController = playerSlot.GetComponent<PlayerOptionSlotController>();
            playerOptionSlotController.playerText.text = setup.PlayerName;
            playerOptionSlotController.btnText.text = setup.ComputerName;
            playerOptionSlotController.selectFactionButton.interactable = false;
            if (debug)
            {
                Debug.Log(setup.PlayerName);
            }
            playerOptionSlotController.selectFactionButtonText.text = setup.FactionKeyword;
            //cant join in load game, as all slots are there
            //playerOptionSlotController.joinBtn.onClick.AddListener(delegate { JoinSlot(playerOptionSlotController.btnText, playerOptionSlotController.playerText.text,setup); });
            playerOptionSlotController.joinBtn.interactable = false;
            JoinAssignSlotRightClick joinAssignSlotRightClick = playerOptionSlotController.joinBtn.GetComponent<JoinAssignSlotRightClick>();
            joinAssignSlotRightClick.controller = this;
            joinAssignSlotRightClick.playerID = setup.PlayerName;
            if (debug)
            {
                Debug.Log(playerSlots.Count);
            }

            if (sizeCounter > 3)
            {

                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectTransform.sizeDelta.y + 100);
            }
            sizeCounter++;
        }
        #region old
        //for (int i = 0; i < GameEngine.ActiveGame.scenario.Players.Count; i++)
        //{
        //    GameObject playerSlot = Instantiate(playerSlotPrefab);
        //    playerSlot.transform.SetParent(playersPanel.transform, false);
        //    playerSlots.Add(playerSlot);

        //    PlayerOptionSlotController playerOptionSlotController = playerSlot.GetComponent<PlayerOptionSlotController>();
        //    playerOptionSlotController.playerText.text = GameEngine.ActiveGame.scenario.Players[i].PlayerID;
        //    if (debug)
        //    {
        //        Debug.Log(GameEngine.ActiveGame.scenario.Players[i].PlayerID);
        //    }
            

        //    playerOptionSlotController.joinBtn.onClick.AddListener(delegate { JoinSlot(playerOptionSlotController.btnText, playerOptionSlotController.playerText.text); });
        //    if (debug)
        //    {
        //        Debug.Log(playerSlots.Count);
        //    }
            
        //    if (i > 3)
        //    {

        //        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectTransform.sizeDelta.y + 100);
        //    }
        //}
        #endregion
    }


    void JoinSlot(Text incText,string playerName, PlayerSetup playerSetup)
    {
        //do nothing if joining already joined own slot
        if (playerSetup.ComputerName != "" && !isLoading) //if is loading, means computer name is assigned already, but can be overridden
        {
            return;
        }

        playerSetup.ComputerName = GameEngine.PLAYER_IDENTITY;
        incText.text = GameEngine.PLAYER_IDENTITY;
        //if (server != null)
        //{
        //    ServerClient thisClient = server.GetClientByName(incText.text);
        //    thisClient.playerName = playerName;
        //    server.Broadcast("assignPlayerSlot|"+thisClient.clientName+"|"+thisClient.playerName,server.clients);
        //    //server.Broadcast("setComputerNameToSetup|" + thisClient.playerName+"|"+thisClient.clientName,server.clients);
        //}
        
        MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.AssignPlayerID, playerSetup.PlayerName, playerSetup.ComputerName);
        GameEngine.ActiveGame.clientManager.Push(multiplayerMessage);
    
    }
    /// <summary>
    /// processing UI commands in update
    /// </summary>
    void CheckMultiPlayerUICommands()
    {
        List<MultiplayerUICommand> commandsToRemove = new List<MultiplayerUICommand>();
        lock(GameEngine.ActiveGame.MultiplayerUICommands)
        {
            foreach (MultiplayerUICommand command in GameEngine.ActiveGame.MultiplayerUICommands)
            {
                switch (command.command)
                {
                    //chat
                    case MultiplayerUICommand.LOBBY_DISABLE_OPTION_PANEL_UI:
                        BlockOptionsAndJoinSlots(false);
                        if (!isHost)
                        {
                            GameEngine.Data.OptionCollection = optionCollection; //we have to do this otherwise we dont have it?????
                        }
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.LOBBY_CHAT_MESSAGE:
                        serverIncomingText.text += Environment.NewLine + command.elements[0] + command.elements[1] + Environment.NewLine;
                        Debug.Log("LOBBY_CHAT_MESSAGE: " + command.elements[0] + command.elements[1]);
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.LOBBY_CHANGE_OPTION_VALUE:
                        Debug.Log("LOBBY_CHANGE_OPTION_VALUE: " + command.elements[0] + " " + command.elements[1]);
                        ChangeOptionValue(command.elements[0], command.elements[1]);
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.LOBBY_CREATE_PLAYER_SLOTS:
                        CreatePlayerSlots();
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.LOBBY_REFRESH_OPTIONS:
                        LoadTemplates(optionCollection);
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.LOBBY_CHANGE_PLAYER_COUNT:
                        amountOfPlayersGameObject.text = command.elements[0];
                        howManyPlayersText.text = command.elements[0];
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.LOBBY_HIDE_OPTIONS_PANEL_CREATE_PLAYER_CONTROLLER:
                      //  GameEngine.ActiveGame.taskStatusOutput.text = "Initializing UI...";
              
                        GameEngine.ActiveGame.OnScenarioRecieve();
                        GameEngine.ActiveGame.AfterInstantiateThread();
                        //GameEngine.ActiveGame.AfterInstantiateThread();
                       // GameEngine.ActiveGame.taskStatusOutput.text = "";
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.LOBBY_HIDE_OPTIONS_PANEL_LOAD_GAME_PROCESSING:
                        //  GameEngine.ActiveGame.taskStatusOutput.text = "Initializing UI...";

                        GameEngine.ActiveGame.scenario.OnLoad();
                        //GameEngine.ActiveGame.AfterInstantiateThread();
                        // GameEngine.ActiveGame.taskStatusOutput.text = "";
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.LOBBY_AFTER_INSTANTIATE:
                        GameEngine.ActiveGame.AfterInstantiateThread();
                        // GameEngine.ActiveGame.taskStatusOutput.text = "";
                        commandsToRemove.Add(command);
                        break;
                    default:
                        break;
                }
            }
            foreach (MultiplayerUICommand command in commandsToRemove)
            {
                GameEngine.ActiveGame.MultiplayerUICommands.Remove(command);
            }
        }
      
    }

    void UnblockUI()
    {
        startBtn.interactable = true;
        amountOfPlayersGameObject.interactable = true;
    }

    void BlockOptionsAndJoinSlots(bool block)
    {
        startBtn.interactable = false;
        foreach (GameObject slotsObj in playerSlots)
        {
            PlayerOptionSlotController playerOptionSlotController = slotsObj.GetComponent<PlayerOptionSlotController>();
            playerOptionSlotController.selectFactionButton.interactable = block;
            playerOptionSlotController.joinBtn.interactable = block;
        }

    
        foreach (GameObject option in optionsPrefabList)
        {
            OptionRowController characterTemplateController = option.GetComponent<OptionRowController>();
            characterTemplateController.valueInsertBox.GetComponent<InputField>().interactable = block;
            foreach (Button defaultState in characterTemplateController.optionChoiceButtons)
            {
                defaultState.interactable = block;
            }
        }
        amountOfPlayersGameObject.interactable = block;
        
    }
    void ChangeOptionValue(string optionKW, string val)
    {
        GameObject gameobj = GetOptionObjectByKeyword(optionKW);
        OptionRowController row = gameobj.GetComponent<OptionRowController>();
        InputField inputFieldScript = row.valueInsertBox.GetComponent<InputField>();

        if (inputFieldScript == null)
        {

            Debug.Log("Did not find inputField component for valueInsertBox (OptionPanel ChangeOptionValue) " + optionKW + " " + val);
        }
        else
        {

            inputFieldScript.text = val;
        }
        row.AssignSelectedImageForOptionButtons();

    }

    private void Update()
    {
        CheckMultiPlayerUICommands();
        if (Input.GetButtonDown("Submit"))
        {
            SendChatMessage();
        }
        if (multiplayer != null)
        {
            if (!isHost)
            {
                return;
            }
            if (multiplayer.Clients.Count != playerCount)
            {
                howManyPlayersText.text = "";
                playerCount = server.clients.Count;

                MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.PlayersCount, Environment.MachineName, amountOfPlayersGameObject.text);
                multiplayer.broadCastToAll(multiplayerMessage);
            }
        }
        if (server != null)
        {
            if (!isHost)
            {
                return;
            }
            //Debug.Log(server.clients.Count);
            //Debug.Log("displaying players in list, playerCount: " + playerCount + " serverCount: " + server.clients.Count);
            if (server.clients.Count != playerCount)
            {
                bool debug = false;
                if (debug)
                {
                    Debug.Log(DateTime.Now + " displaying players in list, playerCount: " + playerCount + " serverCount: " + server.clients.Count);
                }
                
                howManyPlayersText.text = "";
                playerCount = server.clients.Count;


                //send new info to all players when new player joins
                client.Send("playersCount|" + amountOfPlayersGameObject.text);


                int i=0;
                string name = "";
                foreach (ServerClient serverClient in server.clients)
                {
                    i++;
                    name = serverClient.clientName;
                    playerNames.Add(serverClient.clientName);
                  //  howManyPlayersText.text += "Player: " + serverClient.clientName + "\n";
                  //client.Send("playerList|"+i.ToString() + " "+serverClient.clientName + "\n");
                  // Debug.Log(serverClient.clientName);
                }
               // client.Send("playerList|"+i.ToString() + " "+name + "\n");
            }

        }
       
    }

 
    

}
