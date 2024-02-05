using GameProj.Generators;
using GameProj.Map;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using GameProj.Area;
using System.Collections;
using System.IO;
using GameProj.Entities;
using GameProj.Collections;
using GameProj.Items;
using System.Threading;
using GameProj.Skills;
using System.Runtime.InteropServices;

public class PlayerController : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Player")]
    public Camera playerCamera;
    public Canvas playerCanvas;
    public Button nextTurnBtn;
    public Text turncounterText;
    public Text turncounterTextNumb;
    public Text gameStateText;
    public GameObject gameStatePanel;
    public GameObject PlayerOverlandUI;
    public Camera screenshot;
    public GameObject splitItemPanel;
    [Header("Other")]
    public GameObject messagePanel;
    public GameObject warningPanel;
    public GameObject whoEndedTurnPanel;
    public GameObject combatMessagePrefab;
    public GameObject playerOverlandUI;
    internal ChatPanelController chatPanel;
    public AudioSource audioSource;
    public OverlandReplayController overlandReplayController;
    public GameObject endGamePanel;
    public Button infoBtn;
    public PlayerInfoPanelController playerInfoPanelController;
    [Header("Notifications")]
    public GameObject notificationPanel;
    public GameObject notificationPanelParent;
    public GameObject notificationPrefab;
    public GameObject expandedNotificationPanel;
    public GameObject incomingTradeOfferWindowPanel;
    public GameObject pendingItemsPanel;


    List<GameObject> createdNotifications = new List<GameObject>();
    [Header("Tilemaps")]
    public Tilemap groundTileMap;
    public Tilemap buildingTileMap;
    public Tilemap playerFlagTileMap;
    public Tilemap armyTileMap;
    public Tilemap combatIndicatorTileMap;

    public Tilemap selectionTileMap;
    public Tilemap selectionAnimatedTileMap;
    public Tilemap fogOfWarTileMap;
    public Tilemap hiddenMapTileMap;
    public Tilemap heroBackgroundTileMap;
    public Tilemap flagHolderTileMap;
    public Tilemap flag1TileMap;
    public Tilemap flag2TileMap;
    public Tilemap flag3TileMap;
    public Tilemap flagPoleTileMap;
    public Tilemap buildingFlagPoleTileMap;
    public Tilemap eventTileMap;
    public Tilemap itemsTileMap;
    public Grid grid;

    [Header("Shop Menu")]
    public Button ShopButton;


    public GameObject ShopPanel;

    [Header("Quest menu")]
    public Button questButton;
    public GameObject questPanel;
    public GameObject questProgressPanel;

    [Header("Inventory menu")]
    public Button inventoryButton;
    public GameObject inventoryPanel;
    public GameObject eventStashPanel;
    public GameObject entityStashPanel;
    public GameObject gamesquareStashPanel;
    public GameObject buildingProductionStashPanel;


    public GameObject tradeOfferStashPanel;


    [Header("Overland Event")]
    public GameObject overlandEventSelectionPanel;


    [Header("UI settings")]

    public Canvas onWorldCanvas;
    public Button toggleCoords;
    public Button toggleVisionButton;
    public GameObject visionPanelPrefab;
    public GameObject coordsPanelPrefab;
    public HexCellUICoordinateList hexCellUICoordinates = new HexCellUICoordinateList();

    [Header("Hero Action Menu ↓------------------------")]

    [Header("Action buttons")] 
    public Button killHeroButton;
    public Button attackArmyButton;
    public Button craftFromRecipeButton;
    public Button openHeroInventoryButton;


    [Header("Army Interaction")]
    public GameObject armyInteractOverlandButton;
    public Button armyInteractAttackBtn;

    [Header("??? old army interaction?")]
    public GameObject hexPanelPrefab;
    public GameObject attackButtonPrefab;
    public GameObject gameSquareBuildingInfoPanel;
    public GameObject turnCounterPanel;

    //for enemies

    public GameSquarePanelController gameSquirePanel;
  
    [Header("Army&HeroMenu")]

    public GameObject allHeroesPanel;
    public GameObject armyPanel;
    public GameObject multipleArmiesPanel;
    public GameObject armyInteractionPanel;
    public GameObject expandedArmyPanel;
    public GameObject heroesPanel;

    [Header("Hero Action Menu ↑------------------------")]
  
    [Header("Recipe Menu")]
    public GameObject recipePanel;

    [Header("BodyPart UI")]
    public GameObject bodyPartsUI;
    public Button bodyPartsBtn;


    [Header("Roll Menu")]
    public GameObject rollsUI;

    [Header("Events")]
    public GameObject eventsPanel;
    public Button eventButton;

    [Header("Building")]
    public Button openBuildingUIButton;
    public GameObject buildingGarrisonPanel;
    public GameObject buildingStoragePanel;
    public Button BuildingStorageButton;
    public Button BuildingGarrisonButton;
    public GameObject buildingSelectionPanel;

    [Header("PlaceingBuilding")]
    public bool isPlacingBuilding = false;
    public GameObject placementImage; //when you want to place a building overland

    [Header("Hero Inventory")]
    public Button heroInvBtn;
    public GameObject HeroInv;

    [Header("Hero Skills")]
    public Button heroSkillsBtn;
    public GameObject skillsPanel;
    public Button heroSkillTreesBtn;
    public GameObject skillTreesPanel;

    [Header("Hero upkeep")]
    public Button heroUpkeepButton;
    public GameObject heroUpkeepPanel;

    [Header("Battlefield")]
    public GameObject battleManagmentPanel;

    [Header("CameraMovement")]
    internal Vector3 cameraStartPosition;

    [Header("Unassigned")]
    public GameObject toolTip;
    public GameObject playerheromanagmentpanel;
    public GameObject combatMapGeneratorPanel;
    public string testStr = "";

    public Tile selectBg;

    internal Color32 playerColor;
    internal Color32 playerDarkColor;
    internal Color32 playerLightColor;
 
    public GameObject resourcePrefab;
    public Button toggleHexResources;
    int refreshCounter = 0;

    #region variables
    internal bool isInReplay = false;
    internal bool cancelPlayback = false;
    internal float playbackSpeedModifier = 1f;

    byte color1;
    byte color2;
    byte color3;
    byte darkcolor1;
    byte darkcolor2;
    byte darkcolor3;
    byte lightcolor1;
    byte lightcolor2;
    byte lightcolor3;
    // public MemoryArmy selectedMemoryArmy;
    public PlayerSelection Selection = null; //holds info for UI, like selected Square, selected army, selected hero
    MapSquare targetMovementSquare;
    private float waitTime = 0.8f;
    internal float timer = 0.0f;
    private float visualTime = 0.0f;
    bool visionToggle = false;
    bool coordsToggle = false;
    bool attacking = false;
    bool resourceToggle = false;
    internal bool blink = false;
    internal bool theBuildingGarrsionUI = false;
    internal bool theBuildingStorageUI = false;

   // private OurMapSquareList path = null;

    MapMemory memoryTilePath = null;
  //  private Army selectedArmy;
  //  private GameSquare selectedGameSquare;
    private Player thisPlayer;
    private string backgroundTile;
    private string playerID = "";
    bool askAboutInactiveHeroes = true;
    List<GameObject> armyInteractObjList = new List<GameObject>();
    // private string playerColor = "";
    float deltaTime;
    bool checkFps = false;
    #endregion

    #region lists
    HexPanelList hexPanelList = new HexPanelList();
    List<GameObject> attackButtons = new List<GameObject>();

    List<GameObject> ResourceImages = new List<GameObject>();
    List<GameObject> AttackButtons = new List<GameObject>();
    List<GameObject> VisionButtons = new List<GameObject>();
    List<GameObject> CoordsTags = new List<GameObject>();
    List<AnimationPrefabController> animationPrefabControllers = new List<AnimationPrefabController>();
    List<int> unitsToCycle = new List<int>();

    #endregion

    public string PlayerID { get => playerID; set => playerID = value; }
   
   // public Army SelectedArmy { get => selectedArmy; set => selectedArmy = value; }
   // public GameSquare SelectedGameSquare { get => selectedGameSquare; set => selectedGameSquare = value; }
    public Player ThisPlayer { get => thisPlayer; set => thisPlayer = value; }
   // public OurMapSquareList Path { get => path; set => path = value; }
    public List<int> UnitsToCycle { get => unitsToCycle; set => unitsToCycle = value; }
   
    internal string BackgroundTile { get => backgroundTile; set => backgroundTile = value; }

    internal bool isObserver = false;
    internal bool isEndingTurn = false; //need this to allow non-observer client to un-press end turn button
    void Start()
    {
       
        onWorldCanvas.renderMode = RenderMode.WorldSpace;
      
    }


    public void StartMethod(Player player, bool isAI)
    {
        ThisPlayer = player;
        //ThisPlayer = GameEngine.ActiveGame.scenario.FindPlayerByID(this.playerID);
        Selection = new PlayerSelection(this);
        //SetPlayerColor();
        InitilizePlayerUI();

        color1 = ThisPlayer.Color1;
        color2 = ThisPlayer.Color2;
        color3 = ThisPlayer.Color3;
        playerColor = new Color32 (color1, color2, color3, 255);

        darkcolor1 = ThisPlayer.DarkColor1;
        darkcolor2 = ThisPlayer.DarkColor2;
        darkcolor3 = ThisPlayer.DarkColor3;
        playerDarkColor = new Color32(darkcolor1, darkcolor2, darkcolor3, 255);

        lightcolor1 = ThisPlayer.LightColor1;
        lightcolor2 = ThisPlayer.LightColor2;
        lightcolor3 = ThisPlayer.LightColor3;
        playerLightColor = new Color32(lightcolor1, lightcolor2, lightcolor3, 255);

        //  playerCamera = Camera.main;
        playerCamera.gameObject.SetActive(true);
      //  Debug.Log("he"+playerCamera.gameObject.transform.position.z);
        // combatMapGenerator = combatMapGeneratorPanel.GetComponent<CombatMapGenerator>();
        inventoryPanel.GetComponent<Image>().color = new Color32(darkcolor1, darkcolor2, darkcolor3, 255);

        if (isAI)
        {
            RemoveRedundantUIElementsForAI();
        }
        MoveCameraToFirstArmy();
   
    }

    /// <summary>
    /// copies logic from combat replay(idk if needed)
    /// </summary>
    /// <returns></returns>
    internal bool IsMoving()
    {
        return false;
    }


    public void BuildButtonClick(bool openOnly)
    {
        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.HERO_ACTION_BUILD_BUTTON).Permission == GameState.Permission.BUILD_BUTTON_DISABLED)
        {
            GameEngine.ActiveGame.DisplayMessageToPlayer("Cannot interact", PlayerID, Color.red);
            Debug.Log("cannot act during phase: " + ThisPlayer.GameState);
            return;
        }
        if (buildingSelectionPanel.activeSelf && !openOnly)
        {
            buildingSelectionPanel.SetActive(false);
        }
        else
        {
           
            if (Selection.SelectedFriendlyOverlandArmy != null)
            {
                Entity hero = GameEngine.ActiveGame.scenario.FindUnitByUnitID(Selection.SelectedFriendlyOverlandArmy.LeaderID);
                if (hero == null)
                {
                    Debug.Log("PlayerController.BuildButtonClick(): entity wasnt found");
                    return;
                }
                if (!hero.IsHeroFlag)
                {
                    Debug.Log("PlayerController.BuildButtonClick(): no hero found");
                    return;
                }
                //we toggle off the mission
                if (hero.Mission != null)
                {
                    if (hero.Mission.MissionName == Mission.mission_Build)
                    {
                        MultiplayerMessage message = new MultiplayerMessage(MultiplayerMessage.NullEntityMission,hero.UnitID.ToString(),"");
                        GameEngine.ActiveGame.clientManager.Push(message);

                        hero.Mission = null;
                        RefreshUI();
                        return;
                    }
                }
                BuildPanelController buildPanelController = buildingSelectionPanel.GetComponent<BuildPanelController>();
                GameSquare gameSquare = GameEngine.ActiveGame.scenario.Worldmap.FindGameSquareByCoordinates(Selection.SelectedFriendlyOverlandArmy.Location.WorldMapCoordinates.XCoordinate, Selection.SelectedFriendlyOverlandArmy.Location.WorldMapCoordinates.YCoordinate);
                if (hero.KnownBuildingKeywords.Count > 0)
                {
                    //buildingSelectionPanel.SetActive(true);
 
                    //buildPanelController.SetValues(hero.KnownBuildingKeywords);
                    
                }
                else
                {
                    Debug.Log("PlayerController.BuildButtonClick(): entity has no recipes");
                    //return;
                }
                buildingSelectionPanel.SetActive(true);
                List<List<Stat>> recipeList = GameEngine.ActiveGame.scenario.GetSortedBuildingTemplateKeywords(hero.KnownBuildingKeywords, hero.UnitID, gameSquare);
                string str = "recipelist count " + recipeList.Count;
                //foreach (List<Stat> statlist in recipeList)
                //{
                //    str += " statlist count: " + statlist.Count;
                //    foreach (Stat stat in statlist)
                //    {
                //        str += " stat: " + stat.Keyword + " " + stat.Amount;
                //    }
                //}
                //Debug.Log(str);
                buildPanelController.SetValues(recipeList, hero.UnitID, gameSquare, Selection.SelectedFriendlyOverlandArmy);
            }
            else
            {
                Debug.LogError("PlayerController.BuildButtonClick(): army wasnt selected");
            }
        }
 
    }
    /// <summary>
    /// temporary method(most likely), to prevent log spam(removes camera and sound output),
    /// idk if keeping the UI is good idea(but might be good when just observing AI?)
    /// </summary>
    public void RemoveRedundantUIElementsForAI()
    {
       // Destroy(playerCamera);
        this.audioSource = null;
        this.gameObject.SetActive(false);
    }
    private void OnDisable()
    {
        overlandReplayController.LastReplay();
        ClearAnimations();
    }
    void InitilizePlayerUI()
    {
        if (overlandEventSelectionPanel != null)
        {
            SelectOverlandEventPanelController overlandEventChoiceController = overlandEventSelectionPanel.GetComponent<SelectOverlandEventPanelController>();
            overlandEventChoiceController.playerController = this;
        }
        if (overlandReplayController != null)
        {
            overlandReplayController.playerController = this;
        }
        if (heroUpkeepButton != null)
        {
            heroUpkeepButton.onClick.AddListener(delegate { OpenUpkeepPanel(false,null); });
        }
        if (infoBtn != null)
        {
            infoBtn.onClick.AddListener(delegate { OpenInfoPanel(false); });
            TooltipTrigger tooltipTrigger = infoBtn.GetComponent<TooltipTrigger>();
            tooltipTrigger.content = "Click to see player information";
        }
        if (playerInfoPanelController != null)
        {
            playerInfoPanelController.playerController = this;
        }
        if (heroUpkeepPanel != null)
        {
            UpkeepPanelController upkeepPanelController = heroUpkeepPanel.GetComponent<UpkeepPanelController>();
            upkeepPanelController.playerController = this;
        }
        if (incomingTradeOfferWindowPanel != null)
        {
            IncomingTradeOfferPanelController incomingTradeOfferPanelController = incomingTradeOfferWindowPanel.GetComponent<IncomingTradeOfferPanelController>();
            incomingTradeOfferPanelController.playerController = this;
        }
        if (pendingItemsPanel != null)
        {
            PendingItemPanelContorller pendingItemPanelContorller = pendingItemsPanel.GetComponent<PendingItemPanelContorller>();
            pendingItemPanelContorller.playerController = this;
        }
        if (skillTreesPanel != null)
        {
            SkillTreesPanelController skillTreesPanelController = skillTreesPanel.GetComponent<SkillTreesPanelController>();
            skillTreesPanelController.playerController = this;
            skillTreesPanelController.InitilizeUI();
        }
        gameSquirePanel.buildingInfoPanelController.playerController = this;
        if (heroSkillTreesBtn != null)
        {
            heroSkillTreesBtn.onClick.AddListener(delegate { OpenHeroSkillTrees(false); });
        }
        if (bodyPartsBtn != null)
        {
            bodyPartsBtn.onClick.AddListener(delegate { OpenHeroBodyParts(false); });
        }
        if (splitItemPanel != null)
        {
            SplitItemStackPanelController splitItemStackPanelController = splitItemPanel.GetComponent<SplitItemStackPanelController>();
            splitItemStackPanelController.playerController = this;
        }
        if (warningPanel != null)
        {
            WarningPanelController warningPanelController = warningPanel.GetComponent<WarningPanelController>();
            warningPanelController.playerController = this;
        }
        if (gameStatePanel != null)
        {
            TooltipTrigger tooltipTrigger = gameStatePanel.GetComponent<TooltipTrigger>();
            tooltipTrigger.content = "shows current gamestate and FPS and random seed";
        }
        if (combatMapGeneratorPanel != null)
        {
            CombatMapGenerator combatMapGenerator = combatMapGeneratorPanel.GetComponent<CombatMapGenerator>();
            combatMapGenerator.player = ThisPlayer;
            combatMapGenerator.playerController = this;
            combatMapGenerator.playerCamera = playerCamera;
            combatMapGenerator.CombatSelection = new CombatSelection(combatMapGenerator);

            InitiativePanelController initiativePanel = combatMapGenerator.initiativePanel.GetComponent<InitiativePanelController>();
            initiativePanel.mapGenerator = combatMapGenerator;

            PlayerDiplomacyPanelController playerDiplomacyPanelController = combatMapGenerator.diplomacyPanel.GetComponent<PlayerDiplomacyPanelController>();
            playerDiplomacyPanelController.combatMapGenerator = combatMapGenerator;

            Text nextCombatButtonText = combatMapGenerator.nextTurnButton.GetComponentInChildren<Text>();
            if (GameEngine.ActiveGame.scenario.CombatInitiativeMode == OptionCollection.UnitBasedInitiative)
            {
                nextCombatButtonText.text = "Next unit";
            }
            else
            {
                nextCombatButtonText.text = "Next army";
            }
        }
        if (expandedNotificationPanel != null)
        {
            ExpandedNotificationPanelController panelController = expandedNotificationPanel.GetComponent<ExpandedNotificationPanelController>();
            panelController.playerController = this;
            TooltipTrigger tooltipTrigger;
            tooltipTrigger = panelController.okButton.GetComponent<TooltipTrigger>();
            tooltipTrigger.content = "Close this window";

            tooltipTrigger = panelController.dismissButton.GetComponent<TooltipTrigger>();
            tooltipTrigger.content = "Remove the notification";
        }
        if (notificationPanelParent != null)
        {
            TooltipTrigger tooltipTrigger = notificationPanelParent.GetComponent<TooltipTrigger>();
            tooltipTrigger.content = "Notifications will appear here";
        }
        if (questProgressPanel != null)
        {
            DungeonViewPanelController dungeonViewPanelController = questProgressPanel.GetComponent<DungeonViewPanelController>();
            dungeonViewPanelController.player = ThisPlayer;
        }
        if (questPanel != null)
        {
            QuestPanelController questPanelController = questPanel.GetComponent<QuestPanelController>();
            questPanelController.player = ThisPlayer;
            questPanelController.playerController = this;
            questPanelController.InitilizeUI();
        }
        if (questButton != null)
        {
            questButton.onClick.AddListener(OpenQuestPanel);
            TooltipTrigger tooltipTrigger = questButton.gameObject.GetComponent<TooltipTrigger>();
            tooltipTrigger.content = "see quests panel";
        }
        if (openHeroInventoryButton != null)
        {
            openHeroInventoryButton.onClick.AddListener(delegate { OpenHeroBackpack(false,true,true); });
        }
        if (heroSkillsBtn != null)
        {
            heroSkillsBtn.onClick.AddListener(delegate { OpenHeroSkills(false); });
        }
        if (nextTurnBtn != null)
        {
            nextTurnBtn.onClick.AddListener(NextTurn);

            TooltipTrigger tooltipTrigger = nextTurnBtn.gameObject.GetComponent<TooltipTrigger>();
            tooltipTrigger.content = "proceed to next turn";
        }
        //if (surveyButton != null)
        //{
        //    surveyButton.onClick.AddListener(SurveyButtonClick);
        //}
        //if (buildBuildingButton != null)
        //{
        //    buildBuildingButton.onClick.AddListener(delegate { BuildButtonClick(false); });
        //}
        if (recipePanel != null)
        {
            recipePanel.GetComponent<RecipeUIController>().RecipeButtonsInitialize();
        }
        if (gameSquirePanel != null)
        {
            gameSquirePanel.InitilizeCommandButtons();
        }
        if (toggleVisionButton != null)
        {
            toggleVisionButton.onClick.AddListener(ToggleVision);
        }

        if (toggleCoords != null)
        {
            toggleCoords.onClick.AddListener(ToggleCoords);
        }

        if (BuildingGarrisonButton != null)
        {
            BuildingGarrisonButton.onClick.AddListener(ToggleGarrsionMenu);
        }

        if (BuildingStorageButton != null)
        {
            BuildingStorageButton.onClick.AddListener(ToggleStorageMenu);
        }

        //if (captureButton != null)
        //{

        //    captureButton.onClick.AddListener(CaptureButtonClick);
        //}
        if (attackArmyButton != null)
        {
            attackArmyButton.onClick.AddListener(delegate { AttackButtonClick(Selection.SelectedFriendlyOverlandArmy); });
        }
        if (killHeroButton != null)
        {
            killHeroButton.onClick.AddListener(delegate { KillHeroButtonClick(Selection.SelectedFriendlyOverlandArmy); });
        }
        //if (destroyBuildingButton != null)
        //{
        //    destroyBuildingButton.onClick.AddListener(RazeBuildingButtonClick);
        //}
        if (craftFromRecipeButton != null)
        {
            craftFromRecipeButton.onClick.AddListener(delegate { OpenRecipeClick(false); });

            TooltipTrigger tooltipTrigger = craftFromRecipeButton.gameObject.GetComponent<TooltipTrigger>();
            tooltipTrigger.content = "see your hero's craft recipes";
        }
        if (ShopButton != null)
        {
            ShopButton.onClick.AddListener(delegate { OpenShopWindow(false); });

            TooltipTrigger tooltipTrigger = ShopButton.gameObject.GetComponent<TooltipTrigger>();
            tooltipTrigger.content = "see shop";
        }
        if (inventoryButton != null)
        {
            inventoryButton.onClick.AddListener(delegate { OpenInventory(false); });

            TooltipTrigger tooltipTrigger = inventoryButton.gameObject.GetComponent<TooltipTrigger>();
            tooltipTrigger.content = "open your player items";
        }
        if (toggleHexResources != null)
        {
            toggleHexResources.onClick.AddListener(ToggleResource);
        }
        if (ShopPanel != null)
        {
            ShopWindowController shopWindowController = ShopPanel.GetComponent<ShopWindowController>();
            shopWindowController.playerController = this;
            shopWindowController.uIInfo = new SourceInfo(SourceInfo.MODE_SHOP);
            shopWindowController.StartMethod(ThisPlayer.PlayerID);
        }
        /*
        if (multipleArmiesPanel != null)
        {
            MultipleArmiesPanelController multipleArmiesPanelController = multipleArmiesPanel.GetComponent<MultipleArmiesPanelController>();
            multipleArmiesPanelController.playerID = playerID;
        }
        */
        if (buildingSelectionPanel != null)
        {
            BuildPanelController buildPanelController = buildingSelectionPanel.GetComponent<BuildPanelController>();
            buildPanelController.playerController = this;
        }
        if (gamesquareStashPanel != null)
        {
            PlayerInventoryController playerInventoryController = gamesquareStashPanel.GetComponent<PlayerInventoryController>();
            playerInventoryController.UIname = GameState.Object.GAMESQUARE_STASH;
            playerInventoryController.uIInfo = new SourceInfo(SourceInfo.MODE_GAMESQUARE_STASH);
            playerInventoryController.playerController = this;
        }
        if (eventStashPanel != null)
        {
            PlayerInventoryController playerInventoryController = eventStashPanel.GetComponent<PlayerInventoryController>();
            playerInventoryController.UIname = GameState.Object.EVENT_STASH;
            playerInventoryController.uIInfo = new SourceInfo(SourceInfo.MODE_PLAYER_EVENT_STASH);
            playerInventoryController.playerController = this;
        }
        if (inventoryPanel != null)
        {
            PlayerInventoryController playerInventoryController = inventoryPanel.GetComponent<PlayerInventoryController>();
            playerInventoryController.UIname = GameState.Object.INVENTORY;
            playerInventoryController.uIInfo = new SourceInfo(SourceInfo.MODE_PLAYER_STASH);
            playerInventoryController.playerController = this;
        }
        if (buildingProductionStashPanel != null)
        {
            PlayerInventoryController playerInventoryController = buildingProductionStashPanel.GetComponent<PlayerInventoryController>();
            playerInventoryController.UIname = GameState.Object.BUILDING_PRODUCTION_STASH;
            playerInventoryController.uIInfo = new SourceInfo(SourceInfo.MODE_BUILDING_PRODUCTION_STASH);
            playerInventoryController.playerController = this;
        }

        if (tradeOfferStashPanel != null)
        {
            PlayerInventoryController playerInventoryController = tradeOfferStashPanel.GetComponent<PlayerInventoryController>();
            playerInventoryController.UIname = GameState.Object.TRADE_OFFER_STASH;
            playerInventoryController.uIInfo = new SourceInfo(SourceInfo.MODE_TRADE_STASH);
            playerInventoryController.playerController = this;
        }

        if (entityStashPanel != null)
        {
            PlayerInventoryController playerInventoryController = entityStashPanel.GetComponent<PlayerInventoryController>();
            playerInventoryController.UIname = GameState.Object.ENTITY_STASH;
            playerInventoryController.uIInfo = new SourceInfo(SourceInfo.MODE_ENTITY_STASH);
            playerInventoryController.playerController = this;
        }
        if (eventButton != null)
        {
            eventButton.onClick.AddListener(ToggleEventPanel);


            TooltipTrigger tooltipTrigger = eventButton.gameObject.GetComponent<TooltipTrigger>();
            tooltipTrigger.content = "Click to toggle events panel";
        }

        if (turncounterText != null)
        {
            TooltipTrigger tooltipTrigger = turncounterText.gameObject.GetComponent<TooltipTrigger>();
            tooltipTrigger.content = "Counter of turns";
        }

        if (openBuildingUIButton != null) {
            openBuildingUIButton.onClick.AddListener(OpenBuildingUI);
            TooltipTrigger tooltipTrigger = openBuildingUIButton.gameObject.GetComponent<TooltipTrigger>();
            tooltipTrigger.content = "Click to customize the building";
        }
        if (allHeroesPanel != null)
        {
            HeroManagmentPanel heroManagmentPanel = allHeroesPanel.GetComponent<HeroManagmentPanel>();
            heroManagmentPanel.playerController = this;

 

        }
        if (heroesPanel != null)
        {
            TooltipTrigger tooltipTrigger = heroesPanel.GetComponent<TooltipTrigger>();
            tooltipTrigger.content = "selectable buttons of your heroes are placed here";
        }
        if (armyInteractionPanel != null)
        {
            TooltipTrigger tooltipTrigger = armyInteractionPanel.GetComponent<TooltipTrigger>();
            tooltipTrigger.content = "you can set who you want/dont want to attack in this panel";
        }

        if(heroInvBtn != null){
            heroInvBtn.onClick.AddListener(OpenHeroInv);
            //added these 2 lines bc of crash due to unlinked player when i was selling items   -Mark
            CharacterInventoryWindowController characterInventoryWindowController = HeroInv.GetComponent<CharacterInventoryWindowController>();
            characterInventoryWindowController.player = this.ThisPlayer;

            TooltipTrigger tooltipTrigger = heroInvBtn.gameObject.GetComponent<TooltipTrigger>();
            tooltipTrigger.content = "see your hero's inventory";

        }
        if (expandedArmyPanel != null)
        {
            ExpandedArmyPanelController expandedArmyPanelController = expandedArmyPanel.GetComponent<ExpandedArmyPanelController>();

            //expandedArmyPanelController.inventoryWindowController = HeroInv.GetComponent<CharacterInventoryWindowController>();
 
        }
    }

    void RefreshPlayerInfoPanel()
    {
        if (playerInfoPanelController.gameObject.activeSelf)
        {
            OpenInfoPanel(true);
        }
    }

    void OpenInfoPanel(bool openOnly)
    {
        if (playerInfoPanelController.gameObject.activeSelf && !openOnly)
        {
            playerInfoPanelController.gameObject.SetActive(false);
        }
        else
        {
            playerInfoPanelController.gameObject.SetActive(true);
            playerInfoPanelController.DisplayInfo(ThisPlayer);
        }
    }
    void OpenUpkeepPanel(bool openOnly,UpkeepCost upkeepCost)
    {
        if (Selection.SelectedFriendlyHero != null)
        {
            if (heroUpkeepPanel.activeSelf && !openOnly)
            {
                heroUpkeepPanel.SetActive(false);
            }
            else
            {
                heroUpkeepPanel.SetActive(true);
                UpkeepPanelController upkeepPanelController = heroUpkeepPanel.GetComponent<UpkeepPanelController>();
                upkeepPanelController.DisplayUpkeeps(Selection.SelectedFriendlyHero, upkeepCost);

            }
        
        }
    }

    void OpenHeroSkillTrees(bool openOnly)
    {
        if (Selection.SelectedFriendlyHero != null)
        {
            if (skillTreesPanel.activeSelf && !openOnly)
            {
                skillTreesPanel.SetActive(false);
            }
            else
            {
                skillTreesPanel.SetActive(true);
                SkillTreesPanelController skillTreesPanelController = skillTreesPanel.GetComponent<SkillTreesPanelController>();

                skillTreesPanelController.DisplaySkillTrees(Selection.SelectedFriendlyHero);

            }
        }
    }

    void OpenHeroBodyParts(bool openOnly)
    {
        if (Selection.SelectedFriendlyHero != null)
        {
            if (bodyPartsUI.activeSelf && !openOnly)
            {
                bodyPartsUI.SetActive(false);

            }
            else
            {
                bodyPartsUI.SetActive(true);
                BodyPartsController bodyPartsController = bodyPartsUI.GetComponent<BodyPartsController>();
                bodyPartsController.DisplayEntityBodyParts(Selection.SelectedFriendlyHero,0,true);
            }

        }
    }

    void RefreshBodyPartsUI()
    {
        if (bodyPartsUI.activeSelf)
        {
            BodyPartsController bodyPartsController = bodyPartsUI.GetComponent<BodyPartsController>();
            bodyPartsController.DisplayEntityBodyParts(Selection.SelectedFriendlyHero, bodyPartsController.layer, false);
        }
    }

    private void OpenHeroSkills(bool openOnly)
    {
       
        if (Selection.SelectedFriendlyHero != null)
        {
            if (skillsPanel.activeSelf && !openOnly)
            {
                skillsPanel.SetActive(false);

            }
            else
            {
                SkillsPanelController skillsPanelController = skillsPanel.GetComponent<SkillsPanelController>();
                skillsPanelController.DisplaySkills(Selection.SelectedFriendlyHero);
            }

        }
    }

 
    public void OpenBuildingUI() {
        gameSquareBuildingInfoPanel.SetActive(true);
    }

 

    public void OpenTradeOfferStash(ItemCollection bid,int incGuildID,int shopItemID,bool openOnly)
    {
        if (tradeOfferStashPanel.activeSelf && !openOnly)
        {
            tradeOfferStashPanel.SetActive(false);
        }
        else
        {
            tradeOfferStashPanel.SetActive(true);
            PlayerInventoryController playerInventoryController = tradeOfferStashPanel.GetComponent<PlayerInventoryController>();

            SourceInfo uIInfo = new SourceInfo(SourceInfo.MODE_TRADE_STASH);
            uIInfo.PlayerID = ThisPlayer.PlayerID;
            uIInfo.GuildID = incGuildID;
            uIInfo.ShopItemID = shopItemID;
            playerInventoryController.DisplayItems(uIInfo, bid, ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.TRADE_OFFER_STASH).Permission, 8, 12,bid.InventorySize);
        }
    }
    public void OpenBuildingProductionStash(BuildingProduction buildingProduction,bool openOnly, bool canInteract)
    {
        if (inventoryPanel.activeSelf && !openOnly)
        {
            buildingProductionStashPanel.SetActive(false);
        }
        else
        {
            string permission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.BUILDING_PRODUCTION_STASH).Permission;
            //if (permissionOverride != "")
            //{
            //    permission = permissionOverride;
            //}
            buildingProductionStashPanel.SetActive(true);
            PlayerInventoryController playerInventoryController = buildingProductionStashPanel.GetComponent<PlayerInventoryController>();
 
            SourceInfo uIInfo = new SourceInfo(SourceInfo.MODE_BUILDING_PRODUCTION_STASH);
            uIInfo.PlayerID = ThisPlayer.PlayerID;
            playerInventoryController.DisplayItems(uIInfo, buildingProduction.Stash, permission, 3, 16, buildingProduction.Stash.InventorySize);
            playerInventoryController.canInteract = canInteract;
            //RefreshUI();
        }
    }

    public void OpenHeroInv()
    {
        if (Selection.SelectedFriendlyHero != null)
        {
           
            if (!HeroInv.activeSelf)
            {
                CharacterInventoryWindowController characterInventoryWindowController = HeroInv.GetComponent<CharacterInventoryWindowController>();
                characterInventoryWindowController.canInteract = true;
                characterInventoryWindowController.LoadCharacter(Selection.SelectedFriendlyHero, thisPlayer);
      
                OpenHeroBackpack(true,true,true);
                HeroInv.SetActive(true);
            }
            else
            {
                HeroInv.SetActive(false);
                entityStashPanel.SetActive(false);
            }

            
        }
        else {
            HeroInv.SetActive(false);
            entityStashPanel.SetActive(false);
        }

    }


    public void OpenEntityInventory(Entity entity, bool canInteract)
    {

        if (!HeroInv.activeSelf)
        {
        
            CharacterInventoryWindowController characterInventoryWindowController = HeroInv.GetComponent<CharacterInventoryWindowController>();
            characterInventoryWindowController.canInteract = canInteract;
            characterInventoryWindowController.LoadCharacter(entity, thisPlayer);
            PlayerInventoryController playerInventoryController = entityStashPanel.GetComponent<PlayerInventoryController>();
            playerInventoryController.canInteract = canInteract;
            OpenEntityBackpack(entity,true);
            HeroInv.SetActive(true);
        }
        else
        {
            HeroInv.SetActive(false);
            entityStashPanel.SetActive(false);
        }

    }

    

    public void OpenHeroBackpack(bool openOnly, bool canInteract, bool checkForInteraction)
    {
        if (Selection.SelectedFriendlyOverlandArmy != null)
        {
            if (entityStashPanel.activeSelf && !openOnly)
            {
                entityStashPanel.SetActive(false);
                
            }
            else
            {
                //if (Selection.SelectedFriendlyOverlandArmy != null)
                //{
                //    //if (GameEngine.ActiveGame.scenario.AllowPickUpFromGameSquareInventory)
                //    //{

                //    //    OpenGameSquareStashPanel(GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareByCordinates(Selection.SelectedFriendlyOverlandArmy.Location.WorldMapCoordinates.XCoordinate, Selection.SelectedFriendlyOverlandArmy.Location.WorldMapCoordinates.YCoordinate), true);
                        
                //    //}
                    
                //}
                Entity leader = GameEngine.ActiveGame.scenario.FindUnitByUnitID(Selection.SelectedFriendlyOverlandArmy.LeaderID);

                entityStashPanel.SetActive(true);

                PlayerInventoryController playerInventoryController = entityStashPanel.GetComponent<PlayerInventoryController>();
                if (checkForInteraction)
                {
                    playerInventoryController.canInteract = canInteract;
                }
                 
                SourceInfo uIInfo = new SourceInfo(SourceInfo.MODE_ENTITY_STASH);
                uIInfo.EntityID = leader.UnitID;
                playerInventoryController.DisplayItems(uIInfo,leader.BackPack, ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ENTITY_STASH).Permission, 4, 6, leader.BackPack.InventorySize);
            }
  
        }
    }


    public void OpenEntityBackpack(Entity entity,bool openOnly)
    {
        if (entityStashPanel.activeSelf && !openOnly)
        {
            entityStashPanel.SetActive(false);

        }
        else
        {
      
            entityStashPanel.SetActive(true);

            PlayerInventoryController playerInventoryController = entityStashPanel.GetComponent<PlayerInventoryController>();
            SourceInfo uIInfo = new SourceInfo(SourceInfo.MODE_ENTITY_STASH);
            uIInfo.EntityID = entity.UnitID;
            playerInventoryController.DisplayItems(uIInfo, entity.BackPack, ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ENTITY_STASH).Permission, 4, 6, entity.BackPack.InventorySize);
        }

    }

    /// <summary>
    /// used when switching playerControllers
    /// </summary>
    public void GlobalRefresh()
    {
        RefreshUI();
        if (combatMapGeneratorPanel.activeSelf)
        {
            CombatMapGenerator combatMapGenerator = combatMapGeneratorPanel.GetComponent<CombatMapGenerator>();
            combatMapGenerator.RefreshUI();
        }
    }

    void RefreshUpkeepPanel()
    {
        if (heroUpkeepPanel.activeSelf)
        {
            UpkeepPanelController upkeepPanelController = heroUpkeepPanel.GetComponent<UpkeepPanelController>();
            OpenUpkeepPanel(true, upkeepPanelController.selectedUpkeep);
        }
    }

    //for multiplayer refresh(observer)
    void RefreshUpkeepPanelIfViewing(int entID)
    {
        if (heroUpkeepPanel.activeSelf)
        {
            UpkeepPanelController upkeepPanelController = heroUpkeepPanel.GetComponent<UpkeepPanelController>();
            if (upkeepPanelController.entity != null)
            {
                if (upkeepPanelController.entity.UnitID == entID)
                {
                    OpenUpkeepPanel(true, upkeepPanelController.selectedUpkeep);
                }
               
            }
        }
    }


    public void OpenInventory(bool openOnly, GameState gameStateOverride = null) //openOnly is when you dont wanna toggle but to open only
    {
     
        if (inventoryPanel.activeSelf && !openOnly)
        {
            inventoryPanel.SetActive(false);
        }
        else
        {

            //if (Selection.SelectedFriendlyOverlandArmy != null)
            //{
            //    if (GameEngine.ActiveGame.scenario.AllowPickUpFromGameSquareInventory)
            //    {

            //        OpenGameSquareStashPanel(GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareByCordinates(Selection.SelectedFriendlyOverlandArmy.Location.WorldMapCoordinates.XCoordinate, Selection.SelectedFriendlyOverlandArmy.Location.WorldMapCoordinates.YCoordinate), true);

            //    }
            //}
            inventoryPanel.SetActive(true);
            PlayerInventoryController playerInventoryController = inventoryPanel.GetComponent<PlayerInventoryController>();
            if (gameStateOverride == null)
            {
                gameStateOverride = ThisPlayer.GameState;
            }
            SourceInfo uIInfo = new SourceInfo(SourceInfo.MODE_PLAYER_STASH);
            uIInfo.PlayerID = ThisPlayer.PlayerID;
            playerInventoryController.DisplayItems(uIInfo,ThisPlayer.OwnedItems, gameStateOverride.GetUIPermissionByObject(GameState.Object.INVENTORY).Permission, 4,12,ThisPlayer.OwnedItems.InventorySize);
            //RefreshUI();
        }
    }


    void RefreshButtons()
    {

        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.SKILL_TREES_BUTTON).Permission == GameState.Permission.SKILL_TREES_BUTTON_ENABLED)
        {
            heroSkillTreesBtn.interactable = true;
        }
        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.SKILL_TREES_BUTTON).Permission == GameState.Permission.SKILL_TREES_BUTTON_DISABLED)
        {
            heroSkillTreesBtn.interactable = false;
        }


        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.UPKEEP_BUTTON).Permission == GameState.Permission.UPKEEP_BUTTON_ENABLED)
        {
            heroUpkeepButton.interactable = true;
        }
        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.UPKEEP_BUTTON).Permission == GameState.Permission.UPKEEP_BUTTON_DISABLED)
        {
            heroUpkeepButton.interactable = false;
        }



        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.END_TURN_BUTTON).Permission == GameState.Permission.END_TURN_ENABLED)
        {
            nextTurnBtn.interactable = true;
        }
        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.END_TURN_BUTTON).Permission == GameState.Permission.END_TURN_DISABLED)
        {
            nextTurnBtn.interactable = false;
        }

         

        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.RECIPE_BUTTON).Permission == GameState.Permission.RECIPE_BUTTON_ENABLED)
        {
            craftFromRecipeButton.interactable = true;
            craftFromRecipeButton.GetComponentInChildren<Text>().color = new Color32(255, 255, 255, 255);
        }
        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.RECIPE_BUTTON).Permission == GameState.Permission.RECIPE_BUTTON_DISABLED)
        {
            craftFromRecipeButton.interactable = false;
            craftFromRecipeButton.GetComponentInChildren<Text>().color = new Color32(255, 255, 255, 100);
        }



        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.INVENTORY_BUTTON).Permission == GameState.Permission.INVENTORY_BUTTON_ENABLED)
        {
         
            inventoryButton.interactable = true;
            inventoryButton.GetComponentInChildren<Text>().color = new Color32(255,255,255, 255);
        }
        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.INVENTORY_BUTTON).Permission == GameState.Permission.INVENTORY_BUTTON_DISABLED)
        {
       
            inventoryButton.interactable = false;
            inventoryButton.GetComponentInChildren<Text>().color = new Color32(255, 255, 255, 100);
        }



        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.SHOP_BUTTON).Permission == GameState.Permission.SHOP_BUTTON_ENABLED)
        {
            
            ShopButton.interactable = true;
            ShopButton.GetComponentInChildren<Text>().color = new Color32(255, 255, 255, 255);
        }
        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.SHOP_BUTTON).Permission == GameState.Permission.SHOP_BUTTON_DISABLED)
        {
         
            ShopButton.interactable = false;
            ShopButton.GetComponentInChildren<Text>().color = new Color32(255, 255, 255, 100);
        }



        //if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.HERO_ACTION_RAZE_BUTTON).Permission == GameState.Permission.RAZE_ENABLED)
        //{
        //    destroyBuildingButton.interactable = true;
        //}
        //if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.HERO_ACTION_RAZE_BUTTON).Permission == GameState.Permission.RAZE_DISABLED)
        //{
        //    destroyBuildingButton.interactable = false;
        //}



        //if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.HERO_ACTION_SURVEY_BUTTON).Permission == GameState.Permission.SURVEY_ENABLED)
        //{
        //    surveyButton.interactable = true;
        //}
        //if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.HERO_ACTION_SURVEY_BUTTON).Permission == GameState.Permission.SURVEY_DISABLED)
        //{
        //    surveyButton.interactable = false;
        //}



        //if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.HERO_ACTION_CAPTURE_BUTTON).Permission == GameState.Permission.CAPTURE_ENABLED)
        //{
        //    captureButton.interactable = true;
        //}
        //if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.HERO_ACTION_CAPTURE_BUTTON).Permission == GameState.Permission.CAPTURE_DISABLED)
        //{
        //    captureButton.interactable = false;
        //}



        //if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.HERO_ACTION_BUILD_BUTTON).Permission == GameState.Permission.BUILD_BUTTON_ENABLED)
        //{
        //    buildBuildingButton.interactable = true;
        //}
        //if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.HERO_ACTION_BUILD_BUTTON).Permission == GameState.Permission.BUILD_BUTTON_DISABLED)
        //{
        //    buildBuildingButton.interactable = false;
        //}



        //if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ENTITY_STASH).Permission == GameState.Permission.BUILD_BUTTON_ENABLED)
        //{
        //    buildBuildingButton.interactable = true;
        //}
        //if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.HERO_ACTION_BUILD_BUTTON).Permission == GameState.Permission.BUILD_BUTTON_DISABLED)
        //{
        //    buildBuildingButton.interactable = false;
        //}




        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.EVENT_BUTTON).Permission == GameState.Permission.EVENT_BUTTON_ENABLED)
        {
            eventButton.interactable = true;
        }
        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.EVENT_BUTTON).Permission == GameState.Permission.EVENT_BUTTON_DISABLED)
        {
            eventButton.interactable = false;
        }




        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.QUESTS_BUTTON).Permission == GameState.Permission.QUESTS_BUTTON_ENABLED)
        {
            questButton.interactable = true;
        }
        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.QUESTS_BUTTON).Permission == GameState.Permission.QUESTS_BUTTON_DISABLED)
        {
            questButton.interactable = false;
        }

 

    }


    public void RefreshEntityControls()
    {
        if (Selection.SelectedFriendlyHero != null)
        {
            heroInvBtn.interactable = true;
            craftFromRecipeButton.interactable = true;
            //heroInvBtn.gameObject.SetActive(true);
            //craftFromRecipeButton.gameObject.SetActive(true);
        }
        else
        {
            heroInvBtn.interactable = false;
            craftFromRecipeButton.interactable = false;
            //heroInvBtn.gameObject.SetActive(false);
            //craftFromRecipeButton.gameObject.SetActive(false);
        }
    }

    public void OpenGameSquareStashPanel(GameSquare gamesquare,bool openOnly)
    {
        if (!GameEngine.ActiveGame.scenario.AllowPickUpFromGameSquareInventory)
        {
            return;
        }
        if (gamesquareStashPanel.activeSelf && !openOnly)
        {
            gamesquareStashPanel.SetActive(false);
        }
        else
        {
            gamesquareStashPanel.SetActive(true);
            PlayerInventoryController playerInventoryController = gamesquareStashPanel.GetComponent<PlayerInventoryController>();
            SourceInfo uIInfo = new SourceInfo(SourceInfo.MODE_GAMESQUARE_STASH);
            uIInfo.GamesquareID = gamesquare.ID;
            playerInventoryController.DisplayItems(uIInfo,gamesquare.Inventory, ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.INVENTORY).Permission, 4, 12,gamesquare.Inventory.InventorySize);
            //RefreshUI();
        }
    }
    void RefreshEventsPanel()
    {
        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.EVENT_BUTTON).Permission == GameState.Permission.EVENT_BUTTON_ENABLED)
        {
            eventsPanel.SetActive(true);
        }
        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.EVENT_BUTTON).Permission == GameState.Permission.EVENT_BUTTON_DISABLED || isObserver)
        {
            eventsPanel.SetActive(false);
        }
    }
    public void RefreshInventoryPanel()
    {
        if (inventoryPanel.activeSelf)
        {
            SourceInfo uIInfo = new SourceInfo(SourceInfo.MODE_PLAYER_STASH);
            uIInfo.PlayerID = ThisPlayer.PlayerID;
            PlayerInventoryController playerInventoryController = inventoryPanel.GetComponent<PlayerInventoryController>();
            playerInventoryController.DisplayItems(uIInfo, ThisPlayer.OwnedItems, ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.INVENTORY).Permission, 4, 12, ThisPlayer.OwnedItems.InventorySize);
        }
   
    }

    public void RefreshTradePanel()
    {
        if (tradeOfferStashPanel.activeSelf)
        {
            PlayerInventoryController playerInventoryController = tradeOfferStashPanel.GetComponent<PlayerInventoryController>();
            playerInventoryController.mod = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.TRADE_OFFER_STASH).Permission;
            playerInventoryController.RefreshUI();
        }
  
    }

    void ToggleEventPanel()
    {

        if (eventsPanel.activeSelf)
        {
            eventsPanel.SetActive(false);
        }
        else
        {
            eventsPanel.SetActive(true);
        }

    }

    void RefreshPlayerManagmentPanel()
    {
        if (chatPanel != null)
        {
            if (chatPanel.playersPanel.activeSelf)
            {
                chatPanel.SeePlayersClick();
            }
        }
      
    }

    void OpenRecipeClick(bool openOnly)
    {
        if (Selection.SelectedFriendlyHero==null)
        {
            Debug.LogError("OpenRecipeClick() SelectedFriendlyHero is null");
            return;
        }

        if (recipePanel.activeSelf && !openOnly)
        {
            recipePanel.SetActive(false);
        }
        else
        {
            RecipeUIController recipeControl = recipePanel.GetComponent<RecipeUIController>();
            recipeControl.Invoke(Selection.SelectedFriendlyHero, thisPlayer);

        }
        
    }

    public void RefreshShopWindowUI()
    {
        if (!GameEngine.ActiveGame.scenario.PlayerHasCapital(ThisPlayer)) //if lost capital, cant access the shop
        {
            ShopPanel.SetActive(false);
        }
        if (ShopPanel.activeSelf)
        {
    
            ShopWindowController shopWindowController = ShopPanel.GetComponent<ShopWindowController>();
            shopWindowController.SetValues();
            shopWindowController.DisplayShop();
            //shopWindowController.DisplayShop(ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.SHOP).Permission);
        }
        
    }

    public void OpenShopWindow(bool openOnly, GameState gameStateOverride = null)
    {
        Debug.Log("current random iteration: " +GameEngine.random.Iteration + " seed " + GameEngine.random.Seed);
        if (ThisPlayer.Shops.Count < 1)
        {
            Debug.Log("no shops");
            return;
        }


        if (ShopPanel.activeSelf && !openOnly) //make into toggle function for the button
        {
            ShopPanel.SetActive(false);
        }
        else
        {

            ShopPanel.SetActive(true);
            ShopWindowController shopWindowController = ShopPanel.GetComponent<ShopWindowController>();
            shopWindowController.SetValues();

            if (gameStateOverride == null)
            {
                gameStateOverride = ThisPlayer.GameState;
            }
            shopWindowController.DisplayShop();
            //shopWindowController.DisplayShop(gameStateOverride.GetUIPermissionByObject(GameState.Object.SHOP).Permission);
        }

    }

    public void OpenQuestPanel()
    {
        if (questPanel.activeSelf)
        {
            questPanel.SetActive(false);
        }
        else
        {
            questPanel.SetActive(true);
            QuestPanelController questPanelController = questPanel.GetComponent<QuestPanelController>();
            questPanelController.ShowQuests(ThisPlayer.ActiveQuests);
        }
       
    }

    void NextTurn()
    {
      
        if ((isObserver && !isEndingTurn) || ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.END_TURN_BUTTON).Permission == GameState.Permission.END_TURN_DISABLED) //means the UI is observer only
        {
            GameEngine.ActiveGame.DisplayMessageToPlayer("Cannot interact", PlayerID, Color.red);
            return;
        }

        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.END_TURN_BUTTON).Permission == GameState.Permission.END_TURN_DISABLED || (isObserver && !isEndingTurn))
        {
            
            Debug.Log("cannot end turn during phase: " + ThisPlayer.GameState);
            return;
        }

        if (!CheckInactiveHeroes())
        {
            if (askAboutInactiveHeroes && !isEndingTurn)
            {
                askAboutInactiveHeroes = false;
             
                return;

            }
        }
        bool noLongerEndingTurn = false;
        if (isEndingTurn) //clicked end turn after ending turn, so no longer observer & no longer ending turn
        {
            noLongerEndingTurn = true;

        }
        askAboutInactiveHeroes = true;
      //  ThisPlayer.Notifications.Clear(); //no longer clearing because what if player cancels end turn??
        ////to fix ui blink thing when on next turn army blink gets into wrong army
        //selectedGameSquare = null;
        GameEngine.ActiveGame.NextTurn(ThisPlayer.PlayerID);
        if (noLongerEndingTurn) // GameEngine.ActiveGame.NextTurn sets these variable to true, so we cant use a check on them right now but before hand(when we clicked)
        {
            isObserver = false;
            isEndingTurn = false;
            nextTurnBtn.GetComponentInChildren<Text>().text = "End turn";
        }
        if (isEndingTurn)
        {
            nextTurnBtn.GetComponentInChildren<Text>().text = "Cancel end turn";
        }
        RefreshUI();
    }
    public void RepairClick()
    {
        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.HERO_ACTION_REPAIR_BUTTON).Permission == GameState.Permission.REPAIR_DISABLED || isObserver)
        {
            GameEngine.ActiveGame.DisplayMessageToPlayer("Cannot interact", PlayerID, Color.red);
            Debug.Log("cannot act during phase: " + ThisPlayer.GameState);
            return;
        }
        Building building = GameEngine.ActiveGame.scenario.Worldmap.FindGameSquareByCoordinates(Selection.SelectedFriendlyOverlandArmy.Location.WorldMapCoordinates.XCoordinate, Selection.SelectedFriendlyOverlandArmy.Location.WorldMapCoordinates.YCoordinate).building;
        string kw = building.TemplateKeyword;
        if (building.UnfinishedBuildingTemplateKeyword != "")
        {
            kw = building.UnfinishedBuildingTemplateKeyword;
        }
        
        
        StartBuilding(kw);

    }
    public void CancelMissionClick()
    {
        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.HERO_ACTION_CANCEL_MISSION_BUTTON).Permission == GameState.Permission.MISSION_CANCEL_DISABLED || isObserver)
        {
            GameEngine.ActiveGame.DisplayMessageToPlayer("Cannot interact", PlayerID, Color.red);
            Debug.Log("cannot act during phase: " + ThisPlayer.GameState);
            return;
        }
        //we toggle off the mission
        Entity hero = GameEngine.ActiveGame.scenario.FindUnitByUnitID(Selection.SelectedFriendlyOverlandArmy.LeaderID);
        hero.Mission = null;

        MultiplayerMessage nullifyMission = new MultiplayerMessage(MultiplayerMessage.NullEntityMission, hero.UnitID.ToString(), "");
        GameEngine.ActiveGame.clientManager.Push(nullifyMission);
        RefreshUI();
    }

    public void SurveyButtonClick()
    {
        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.HERO_ACTION_SURVEY_BUTTON).Permission == GameState.Permission.SURVEY_DISABLED || isObserver)
        {
            GameEngine.ActiveGame.DisplayMessageToPlayer("Cannot interact", PlayerID, Color.red);
            Debug.Log("cannot act during phase: " + ThisPlayer.GameState);
            return;
        }
        Debug.Log("setting army mission to survey");
        GameEngine.ActiveGame.SetArmyMissionToSurvey(Selection.SelectedFriendlyOverlandArmy);
        GameEngine.ActiveGame.scenario.SurveyGameSquare(Selection.SelectedFriendlyOverlandArmy);
        RefreshUI();
    }

    public void CaptureButtonClick()
    {
        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.HERO_ACTION_CAPTURE_BUTTON).Permission == GameState.Permission.CAPTURE_DISABLED || isObserver)
        {
            GameEngine.ActiveGame.DisplayMessageToPlayer("Cannot interact", PlayerID, Color.red);
            Debug.Log("cannot act during phase: " + ThisPlayer.GameState);
            return;
        }
        if (Selection.SelectedFriendlyOverlandArmy != null)
        {
            GameEngine.ActiveGame.CaptureButtonClick(Selection.SelectedFriendlyOverlandArmy);
        }
        else
        {
            Debug.LogError("attempted to capture without selected friendly overland army");
        }
        
        RefreshUI();
    }
    void AttackButtonClick(Army army)
    {
        attacking = true;
        Debug.Log("attack click");
    }
    void KillHeroButtonClick(Army army)
    {
        GameEngine.ActiveGame.KillHeroButtonClick(army);
        RefreshUI();
    }
    public void RazeBuildingButtonClick()
    {
        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.HERO_ACTION_RAZE_BUTTON).Permission == GameState.Permission.RAZE_DISABLED || isObserver)
        {
            GameEngine.ActiveGame.DisplayMessageToPlayer("Cannot interact", PlayerID, Color.red);
            Debug.Log("cannot act during phase: " + ThisPlayer.GameState);
            return;
        }
        // GameEngine.ActiveGame.DestroyBuildingButtonClick(gamesqr);
        if (Selection.SelectedFriendlyOverlandArmy != null)
        {
            Entity leader = GameEngine.ActiveGame.scenario.FindUnitByUnitID(Selection.SelectedFriendlyOverlandArmy.LeaderID);

            //we toggle off the mission
            if (leader.Mission != null)
            {
                if (leader.Mission.MissionName == Mission.mission_Raze)
                {
                    leader.Mission = null;
                    MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.NullEntityMission,leader.UnitID.ToString(),"");
                    GameEngine.ActiveGame.clientManager.Push(multiplayerMessage);
                    Debug.Log("nullyfing raze mission");
                    RefreshUI();
                    return;
                }
            }

            GameSquare targetGameSquare = GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareByCordinates(Selection.SelectedFriendlyOverlandArmy.Location.WorldMapCoordinates.XCoordinate, Selection.SelectedFriendlyOverlandArmy.Location.WorldMapCoordinates.YCoordinate);

            if (targetGameSquare.building == null)
            {
                Debug.Log("no building to raze!");
                return;
            }
            if (targetGameSquare.building.OwnerPlayerID == playerID)
            {
                WarningPanelController controller = warningPanel.GetComponent<WarningPanelController>();
                controller.Display("Are you sure you want to destroy your own building?",Mission.mission_Raze,leader.UnitID,targetGameSquare.ID);
            }
            else
            {
                leader.Mission = new Mission();
                leader.Mission.MissionName = Mission.mission_Raze;
                leader.Mission.TargetID = targetGameSquare.ID;
                MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.SetEntityMission, leader.UnitID.ToString());
                GameEngine.ActiveGame.clientManager.PushMultiplayerObject(multiplayerMessage, leader.Mission);
                Debug.Log("SetMissionToRazeBuilding sending");
            }
 
        }
        RefreshUI();
    }

    public void MoveOverlandUIRelativeToCamera(float x, float y, float z)
    {
        foreach (GameObject item in hexPanelList)
        {

            item.transform.position = new Vector3(item.transform.position.x, item.transform.position.y + y, item.transform.position.z + z);
        }
    }

    public void ShowGameStateText()
    {
        if (ThisPlayer.GameState == null)
        {
            gameStateText.text = ThisPlayer.PlayerID + " gamestate is null";
        }
        else
        {
            gameStateText.text = ThisPlayer.PlayerID + " gamestate is " + ThisPlayer.GameState.Keyword;
        }
        gameStateText.text += " FPS: " + Application.targetFrameRate + " seed: " + GameEngine.random.Seed;
    }

    public void DisplayEventItems()
    {
        eventStashPanel.SetActive(true);
        SourceInfo uIInfo = new SourceInfo(SourceInfo.MODE_PLAYER_EVENT_STASH);
        uIInfo.PlayerID = ThisPlayer.PlayerID;
        PlayerInventoryController playerInventoryController = eventStashPanel.GetComponent<PlayerInventoryController>();
        playerInventoryController.DisplayItems(uIInfo,ThisPlayer.EventStash,ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.EVENT_STASH).Permission,4,4, ThisPlayer.EventStash.InventorySize);
    }

    public void ShowEventPanel()
    {
        eventsPanel.SetActive(true);
        EventPanelController eventPanelController = eventsPanel.GetComponent<EventPanelController>();
        eventPanelController.player = ThisPlayer;
    }

    void RefreshSkillTreesButton()
    {
        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.SKILL_TREES_BUTTON).Permission == GameState.Permission.SKILL_TREES_BUTTON_DISABLED)
        {
            heroSkillTreesBtn.interactable = false;
        }
        else
        {
            heroSkillTreesBtn.interactable = true;
        }
        bool useDefaultColor = true;
 
        if (Selection.SelectedFriendlyHero != null)
        {
            if (Selection.SelectedFriendlyHero.AreThereAnyUnpickedLevelChoices())
            {
                useDefaultColor = false;
             
            }
        }
        if (useDefaultColor)
        {
            heroSkillTreesBtn.GetComponent<Image>().color = new Color32(220,220,220,255);
            TooltipTrigger tooltipTrigger = heroSkillTreesBtn.GetComponent<TooltipTrigger>();
            tooltipTrigger.content = "Click to see skill trees";
        }
        else
        {
            heroSkillTreesBtn.GetComponent<Image>().color = new Color32(140, 255, 90, 255);
            TooltipTrigger tooltipTrigger = heroSkillTreesBtn.GetComponent<TooltipTrigger>();
            tooltipTrigger.content = "Click to see skill trees + \n There are level ups avalible" ;
        }
      
    }


    public void RefreshSkillTreesPanel()
    {
        //if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.SKILL_TREES_PANEL).Permission == GameState.Permission.SKILL_TREES_PANEL_NO_ACCESS)
        //{
        //   // heroSkillTreesBtn.interactable = false;
        //}
        //else
        //{
        //   // heroSkillTreesBtn.interactable = true;
        //}
        if (skillTreesPanel.activeSelf)
        {
            SkillTreesPanelController skillTreesPanelController = skillTreesPanel.GetComponent<SkillTreesPanelController>();
            skillTreesPanelController.RefreshUI();
        }

       
    }


    public void StartBuilding(string kw)
    {
        if (Selection.SelectedFriendlyOverlandArmy != null)
        {
            Entity leader = GameEngine.ActiveGame.scenario.FindUnitByUnitID(Selection.SelectedFriendlyOverlandArmy.LeaderID);
           //commented out this check for 2 reasons: messes up repair & theres the check during the construction phases
            //if(!ItemGenerator.isRecipeMaterialLegal(GameEngine.Data.BuildingTemplateCollection.findByKeyword(kw).Ingredients, leader, ThisPlayer))
            //{
            //    Debug.Log("insufficient materials!");
            //    return;
            //}
   
            leader.Mission = new Mission();
            leader.Mission.MissionName = Mission.mission_Build;
            leader.Mission.TargetString = kw;
            MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.SetEntityMission, leader.UnitID.ToString());
            GameEngine.ActiveGame.clientManager.PushMultiplayerObject(multiplayerMessage, leader.Mission);
            Debug.Log("setting mission: BUILD");
            RefreshUI(); //refresh needed, as it doesnt change leader mission otherwise
        }
       
    }

    public void SetSquareSelectionAnimation(int X_cord, int Y_cord)
    {
        RemoveOverlandSelectionGraphic();
        //Tile selectedSquare = Resources.Load<Tile>(MapPointedHex.BlackSelector);

        AnimatedTile selectedSquare = Resources.Load<AnimatedTile>(MapPointedHex.BlackSelector);
        Tile selectionBg = selectBg;
        selectionBg.color = new Color32(color1, color2, color3, 255); 
        selectionTileMap.SetTile(new Vector3Int(X_cord, Y_cord, 0), selectionBg);
        selectionAnimatedTileMap.SetTile(new Vector3Int(X_cord, Y_cord, 0), selectedSquare);
    }

    public void OnPointerDown(PointerEventData eventData)
    {

    }

    public void OnPointerUp(PointerEventData eventData)
    {

    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isInReplay)
        {
            return;
        }
        chatPanel.isChatting = false;
        //isChatting = false;
        bool debug = false;
        //as only 1 pointer click event is allowed(cant have child object with click event) i just pass the eventData into click event of combatMapGenerator
        if (ThisPlayer.GameState.Keyword == GameState.State.BATTLE_PHASE)
        {
            CombatMapGenerator combatMapGenerator = combatMapGeneratorPanel.GetComponent<CombatMapGenerator>();
            combatMapGenerator.OnPointerClick(eventData);
            return;
        }
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (eventData.pointerCurrentRaycast.gameObject.GetComponent<Tilemap>() == null) {
                return;
            }


            RemoveToolTip();

            //RemoveOverlandSelectionGraphic();


            memoryTilePath = null;
            //path = null;
            // Debug.Log(EventSystem.current.IsPointerOverGameObject());
            /* must be commented
            if (EventSystem.current.IsPointerOverGameObject())
            {
               
                return;
            }
            */


            GameObject playerControllerGameObject = this.gameObject;


            Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);

            Vector3Int coordinate = groundTileMap.WorldToCell(mouseWorldPos);

            if (coordinate == null)
            {
                OurLog.Print("Weird. Returning!");
                return;
            }

            MapSquare clickedMapSquare = GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareByCordinates(coordinate.x, coordinate.y, coordinate.z);

            if (clickedMapSquare == null)
            {
                OurLog.Print("No square by x:" + coordinate.x + " y:" + coordinate.y + " z:" + coordinate.z);
                return;
            }

            GameSquare gameSquare = clickedMapSquare as GameSquare;


            blink = false;
            //AnimatedTile selectedSquare = Resources.Load<AnimatedTile>(MapPointedHex.BlackSelector);

            //selectionAnimatedTileMap.SetTile(new Vector3Int(gameSquare.X_cord, gameSquare.Y_cord, 0), selectedSquare);
            //Tile selectionBg = selectBg;
            //selectionBg.color = new Color32(color1, color2, color3, 255);
            //selectionTileMap.SetTile(new Vector3Int(gameSquare.X_cord, gameSquare.Y_cord, 0), selectionBg);

            if (gameSquare != null)
            {
                List<Army> armies = GameEngine.ActiveGame.scenario.GetAllPlayersArmiesOnCoordinates(ThisPlayer.PlayerID,gameSquare.X_cord,gameSquare.Y_cord);
                if (armies.Count > 0)
                {
                    Selection.SelectArmy(armies[0].ArmyID);
                }
                else
                {
                    Selection.SelectJustGameSquare(gameSquare);
                }
             
                ActivateGameSquareTooltip(gameSquare);

            }

            timer = 0;
            RefreshUI();

        }
        // Right click
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (eventData.pointerCurrentRaycast.gameObject.GetComponent<Tilemap>() == null)
            {
                
                return;
            }
            Debug.Log("clicker 2");

            bool rightClickTimer = false;

            if (rightClickTimer) {            
                GameEngine.ActiveGame.GameStopwatch.Reset();
                GameEngine.ActiveGame.GameStopwatch.Start();
            }
        
            if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.OVERLAND_RIGHT_CLICK).Permission == GameState.Permission.OVERLAND_RIGHTCLICK_BAN || isObserver)
            {
                if (rightClickTimer)
                {
                    GameEngine.ActiveGame.GameStopwatch.Stop();
                    Debug.Log("PlayerController.OnPointerClick right click took" + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
                }

                Debug.Log("cannot move during phase: " + ThisPlayer.GameState.Keyword);
                GameEngine.ActiveGame.DisplayMessageToPlayer("Cannot move during " + ThisPlayer.GameState.Keyword + " phase", PlayerID, Color.red);
                return;
            }
            isPlacingBuilding = false;
            placementImage.SetActive(false);
            if (Selection.SelectedFriendlyOverlandArmy != null)
            {
                Entity leader = GameEngine.ActiveGame.scenario.FindUnitByUnitID(Selection.SelectedFriendlyOverlandArmy.LeaderID);

                if (leader == null)
                {
                    Debug.Log("Leader is null because overland Army id: " + Selection.SelectedFriendlyOverlandArmy.ArmyID + " leader id :" + Selection.SelectedFriendlyOverlandArmy.LeaderID + " is null");
                }


                if (leader.Mission != null)
                {
                    if (leader.Mission.MissionName == Mission.mission_Build || leader.Mission.MissionName == Mission.mission_Raze)
                    {
                        leader.Mission = null;
                    }
                }
                //Debug.Log("s army");
            }

            blink = false;
            //RemoveOverlandSelectionGraphic();
            memoryTilePath = null;
            //path = null;
            /* must be commented
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            */
            if (Selection.SelectedGameSquare == null)
            {
                
                return;
            }

            Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int coordinate = groundTileMap.WorldToCell(mouseWorldPos);
            MapSquare gameSquare = GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareByCordinates(coordinate.x, coordinate.y, coordinate.z);
       
            if (gameSquare == null)
            {
                
                Debug.LogError("no mapsquare found with coordinates X " + coordinate.x + " Y " + coordinate.y + " Z " + coordinate.z + " this is completely insane");
                return;
            }

            if (Selection.SelectedFriendlyOverlandArmy != null)
            {
                string canMove = Selection.SelectedFriendlyOverlandArmy.CanMove();
                if (canMove != "")
                {
                    memoryTilePath = null;
                    targetMovementSquare = null;
                    GameEngine.ActiveGame.DisplayMessageToPlayer(canMove, PlayerID, Color.red);
                    // should or shouldnt use    RemoveOverlandSelectionGraphic();? does it look worse?
                    return;
                }
                if (targetMovementSquare == null && gameSquare == null)
                {
                    Debug.LogError("both null, no mapsquare found with coordinates X " + coordinate.x + " Y " + coordinate.y + " Z " + coordinate.z);
                }
                if (targetMovementSquare != gameSquare)
                {
                    //path = GameEngine.Map.FindFullPath(Selection.SelectedFriendlyOverlandArmy, Selection.SelectedGameSquare, gameSquare, null);
                    //path = GameEngine.Map.FindFullPathWithObjectLinks(Selection.SelectedFriendlyOverlandArmy, Selection.SelectedGameSquare, gameSquare, null);
                    //path = GameEngine.Map.FindPathUsingBreadth(Selection.SelectedGameSquare, gameSquare);
                    //path = GameEngine.Map.FindPathUsingBreadthWithObjectLinks(Selection.SelectedGameSquare, gameSquare);
                    MemoryTile source = ThisPlayer.MapMemory.FindMemoryTileByMapSquareID(Selection.SelectedGameSquare.ID);
                    MemoryTile target = ThisPlayer.MapMemory.FindMemoryTileByMapSquareID(gameSquare.ID);
                    if (Selection.SelectedFriendlyOverlandArmy.Location.Mode == Location.MODE_OVERLAND)
                    {
                        if (debug)
                        {
                            Debug.Log("not garisson, showing path");
                        }
                        
                        memoryTilePath = ThisPlayer.MapMemory.FindPathUsingBreadthWithObjectLinks(source, target, Selection.SelectedFriendlyOverlandArmy);
                        targetMovementSquare = gameSquare;

                    

                    }
                    else
                    {
                        memoryTilePath = null;
                        targetMovementSquare = null;
                        if (debug)
                        {
                            Debug.Log("attempted to make path for army that is garisson");
                        }
         
                    }

                    
                    //RefreshUI();
                    RemoveOverlandSelectionGraphic();
                    if (memoryTilePath != null)
                    {
                        Tile selectedSquare = Resources.Load<Tile>(MapPointedHex.Path);
                        foreach (MemoryTile mapsquare in memoryTilePath)
                        {
                            selectionTileMap.SetTile(new Vector3Int(mapsquare.Coord_X, mapsquare.Coord_Y, 0), selectedSquare);
                        }
                    }

                    if (rightClickTimer)
                    {
                        GameEngine.ActiveGame.GameStopwatch.Stop();
                        Debug.Log("PlayerController.OnPointerClick right click took" + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
                    }

                    return;
                }
                else
                {

                    //path = GameEngine.Map.FindPathUsingBreadth(Selection.SelectedGameSquare, targetMovementSquare);
                    // path = GameEngine.Map.FindFullPath(Selection.SelectedFriendlyOverlandArmy, Selection.SelectedGameSquare, targetMovementSquare, null);
                    MemoryTile source = ThisPlayer.MapMemory.FindMemoryTileByMapSquareID(Selection.SelectedGameSquare.ID);
                    MemoryTile target = ThisPlayer.MapMemory.FindMemoryTileByMapSquareID(targetMovementSquare.ID);

                    if (debug)
                    {
                        Debug.Log("not garisson, moving");
                    }

                    memoryTilePath = ThisPlayer.MapMemory.FindPathUsingBreadthWithObjectLinks(source, target, Selection.SelectedFriendlyOverlandArmy);
                    //have to use tempMemory here, because memoryTilePath gets nulled which affects the thread
                    MapMemory tempMemory = new MapMemory();
                    tempMemory = memoryTilePath;

                    Thread moveAlongPathThread = new Thread(() => GameEngine.ActiveGame.scenario.MoveAlongPath(tempMemory, Selection.SelectedFriendlyOverlandArmy, Selection.SelectedGameSquare, 1, true, true, false));
                    moveAlongPathThread.IsBackground = true;
                    moveAlongPathThread.Name = "moveAlongPathThread";
                    moveAlongPathThread.Start();

                    //GameEngine.ActiveGame.scenario.MoveAlongPath(memoryTilePath, Selection.SelectedFriendlyOverlandArmy, Selection.SelectedGameSquare, 1,true,true,false);


                    //GameEngine.ActiveGame.scenario.MoveAlongPath(path, Selection.SelectedFriendlyOverlandArmy, Selection.SelectedGameSquare, 1);
                    //    List<Army> allArmiesOnTheGameSquare = GameEngine.ActiveGame.scenario.FindAllOverlandArmiesByCoordinates(gameSquare.X_cord, gameSquare.Y_cord);
                    //path = null;
                    memoryTilePath = null;
                    targetMovementSquare = null;
                    Selection.AddSelectedGameSquare(GameEngine.Map.FindMapSquareByCordinates(Selection.SelectedFriendlyOverlandArmy.Location.WorldMapCoordinates.XCoordinate, Selection.SelectedFriendlyOverlandArmy.Location.WorldMapCoordinates.YCoordinate));
                    //RefreshUI();

                    if (rightClickTimer)
                    {
                        GameEngine.ActiveGame.GameStopwatch.Stop();
                        Debug.Log("PlayerController.OnPointerClick right click took" + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
                    }

                }

            }
    
        }
    }
    void RefreshIfSeeingCoords(int xCord, int yCord)
    {
        if (isInReplay)
        {
            return;
        }
        bool refreshMap = false;
        bool refreshClickedSquare = false;
        if (this.ThisPlayer.MapMemory.IsSeeingSquare(xCord, yCord))
        {
            refreshMap = true;
        }
        GameSquare selectedGameSqr = Selection.SelectedGameSquare;

        if (selectedGameSqr != null)
        {
            if (selectedGameSqr.X_cord == xCord && selectedGameSqr.Y_cord == yCord)
            {
                refreshClickedSquare = true;
                refreshMap = false; //setting this to false, as RefreshUI will do everything else stated here
                RefreshUI();
            }
        }
        if (refreshMap)
        {
            GameEngine.ActiveGame.UpdateMemoryMapWithActiveVision(thisPlayer);
            DisplayMap(thisPlayer);
        }
        if (refreshClickedSquare)
        {
            RefreshUI();
        }
    }

    void RefreshIfSeeingInventory(string incPlayerid)
    {
        PlayerInventoryController playerInventoryController;
        if (inventoryPanel.activeSelf)
        {
            playerInventoryController = inventoryPanel.GetComponent<PlayerInventoryController>();
            if (playerInventoryController.playerController.ThisPlayer.PlayerID == incPlayerid)
            {
                playerInventoryController.RefreshUI();
            }
        }
    }
    void RefreshSourceInfoUI(SourceInfo srcInfo, string playerUIID)
    {
        PlayerInventoryController playerInventoryController;
        switch (srcInfo.Mode)
        {
            case SourceInfo.MODE_PLAYER_STASH:
                RefreshIfSeeingInventory(playerUIID);


                break;
            case SourceInfo.MODE_PLAYER_EVENT_STASH:
                if (eventsPanel.activeSelf)
                {
                    playerInventoryController = eventsPanel.GetComponent<PlayerInventoryController>();
                    if (playerInventoryController.playerController.ThisPlayer.PlayerID == playerUIID)
                    {
                        playerInventoryController.RefreshUI();
                    }
                }
                break;
            case SourceInfo.MODE_ENTITY_STASH:
                RefreshIfSeeingEntityStash(playerUIID, srcInfo.EntityID);
                break;
            case SourceInfo.MODE_ENTITY_EQUIPMENT:
                if (HeroInv.activeSelf)
                {
                    CharacterInventoryWindowController charWindow = HeroInv.GetComponent<CharacterInventoryWindowController>();
                    if (charWindow.entity.UnitID == srcInfo.EntityID)
                    {
                        charWindow.LoadCharacter(charWindow.entity, GameEngine.ActiveGame.scenario.FindPlayerByID(playerUIID));
                    }
                }
                break;
            case SourceInfo.MODE_GAMESQUARE_STASH:
                if (gamesquareStashPanel.activeSelf)
                {
                    playerInventoryController = gamesquareStashPanel.GetComponent<PlayerInventoryController>();
                    if (playerInventoryController.playerController.ThisPlayer.PlayerID == playerUIID)
                    {
                        playerInventoryController.RefreshUI();
                    }
                }
                break;
            case SourceInfo.MODE_SHOP_BUY_ITEM:
                RefreshIfSeeingShop(srcInfo.ShopID, playerUIID);
                break;
            case SourceInfo.MODE_SHOP_TRADE_ITEM:
                //not sure what to do here, this is notification domain
                // RefreshIfSeeingShop(srcInfo.ShopID, playerUIID);
                break;
            case SourceInfo.MODE_BUILDING_PRODUCTION_STASH:
                if (buildingProductionStashPanel.activeSelf)
                {
                    playerInventoryController = buildingProductionStashPanel.GetComponent<PlayerInventoryController>();
                    if (playerInventoryController.playerController.ThisPlayer.PlayerID == playerUIID)
                    {
                        playerInventoryController.RefreshUI();
                    }
                }
                break;
            case SourceInfo.MODE_TRADE_STASH:
                if (tradeOfferStashPanel.activeSelf)
                {
                    playerInventoryController = tradeOfferStashPanel.GetComponent<PlayerInventoryController>();
                    if (playerInventoryController.playerController.ThisPlayer.PlayerID == playerUIID)
                    {
                        playerInventoryController.RefreshUI();
                    }
                }
                //also not sure what to do here
                break;
            case SourceInfo.MODE_SHOP:

                break;
            default:
                Debug.LogError("unknown sourceinfo mode: " + srcInfo.Mode);
                break;
        }
    }
    void RefreshIfSeeingTransferUI(SourceInfo from, SourceInfo Into,string playerUIID)
    {
        //if (isInReplay)
        //{
        //    return;
        //}
        List<SourceInfo> UIs = new List<SourceInfo>();
        UIs.Add(from);
        if (from.Mode != Into.Mode) //preventing double refreshes if move was made in confines of 1(player inv to player inv, hero inv to hero inv etc)
        {
            UIs.Add(Into);
        }
        PlayerInventoryController playerInventoryController;
        foreach (SourceInfo srcInfo in UIs)
        {
            RefreshSourceInfoUI(srcInfo, playerUIID);
        }
    }
    /// <summary>
    /// this only applies to players that are not initially observers
    /// </summary>
    /// <param name="observe"></param>
    void StartObserving(bool observe)
    {
        //Debug.Log("observation");
        //this only affect computers that are playing
        foreach (PlayerSetup setup in GameEngine.ActiveGame.scenario.GetPlayerSetupsByComputerName(GameEngine.PLAYER_IDENTITY))
        {
            PlayerController obj = GameEngine.ActiveGame.FindPlayerController(setup.PlayerName);
            obj.isObserver = observe;
        }
    }

    void RefreshIfSeeingShop(int shopID, string IncplayerID)
    {
        if (ShopPanel.activeSelf)
        {
            ShopWindowController shopWindowController = ShopPanel.GetComponent<ShopWindowController>();
            if (shopWindowController.playerController.PlayerID == IncplayerID)
            {
                if (shopWindowController.selectedShop != null)
                {
                    if (shopWindowController.selectedShop.ID == shopID)
                    {
                        RefreshShopWindowUI();
                    }
                }
            }
        }
    }
    /// <summary>
    /// doesnt refresh selected quests
    /// </summary>
    void RefreshQuestsIfSeeing(string playerid, int questID)
    {
        if (questPanel.activeSelf)
        {
            QuestPanelController questPanelController = questPanel.GetComponent<QuestPanelController>();
            if (questPanelController.player.PlayerID == playerid)
            {
                questPanelController.ShowQuests(questPanelController.player.ActiveQuests);
            }
            

            if (questPanelController.selectedQuestPanel.activeSelf)
            {
                if (questPanelController.selectedQuest.ID == questID)
                {
                    questPanelController.ShowSelectedQuest(questPanelController.selectedQuest);
                }
                
            }
        }
    }

    void RefreshIfSeeingEntityStash(string incPlayerID, int entityID)
    {
        PlayerInventoryController playerInventoryController;
        if (entityStashPanel.activeSelf)
        {
            playerInventoryController = entityStashPanel.GetComponent<PlayerInventoryController>();
            if (playerInventoryController.playerController.ThisPlayer.PlayerID == incPlayerID && playerInventoryController.uIInfo.EntityID == entityID)
            {
                playerInventoryController.RefreshUI();
            }
        }
    }
    void RefreshIfSeeingRecipes(string incPlayerID, int entityID)
    {
        if (recipePanel.activeSelf)
        {
            RecipeUIController recipeUIController = recipePanel.GetComponent<RecipeUIController>();
            if (recipeUIController.producer != null)
            {
                if (recipeUIController.producer.UnitID == entityID && recipeUIController.player.PlayerID == incPlayerID)
                {
                    recipeUIController.Invoke(recipeUIController.producer, recipeUIController.player);
                }
            }
        }
    }

    void RefreshIfSeeingCoordinatesOverland(FromToCoordinates fromTo)
    {

        if (ThisPlayer.MapMemory.IsSeeingSquare(fromTo.toX, fromTo.toY) || ThisPlayer.MapMemory.IsSeeingSquare(fromTo.fromX, fromTo.fromY))
        {
            GameEngine.ActiveGame.UpdateMemoryMapWithActiveVision(thisPlayer);
            DisplayMap(ThisPlayer);
        }
    }

    void RefreshIfSeeingSkills(int entityID)
    {
        SkillsPanelController skillsPanelController = skillsPanel.GetComponent<SkillsPanelController>();
        if (skillsPanelController.ent != null)
        {
            if (skillsPanelController.ent.UnitID == entityID)
            {
                RefreshSkillsPanel();
            }
        }
        
    }

    void ClearAnimations()
    {
        foreach (AnimationPrefabController prefabController in animationPrefabControllers)
        {
            if (prefabController == null)
            {
                continue;
            }
            Destroy(prefabController.gameObject);
        }
        animationPrefabControllers.Clear();
    }

    void DeleteAnimationOnCoordinates(int xCord, int yCord)
    {
        AnimationPrefabController toRemove = null;
        foreach (var animationPrefabController in animationPrefabControllers)
        {
            
            if (animationPrefabController.tile.Coord_X == xCord && animationPrefabController.tile.Coord_Y == yCord)
            {
                toRemove = animationPrefabController;
                if (animationPrefabController != null)
                {
                    if (animationPrefabController.gameObject != null)
                    {
                        Destroy(animationPrefabController.gameObject);
                    }

                }

            }
        }
        animationPrefabControllers.Remove(toRemove);
    }

    public void SetAnimationGraphics()
    {
        if (isInReplay)
        {
            return;
        }

        foreach (AnimationPrefabController animationPrefab in animationPrefabControllers)
        {
            if (animationPrefab == null)
            {
                continue;
            }
            Player plr = GameEngine.ActiveGame.scenario.FindPlayerByID(animationPrefab.playerID);
            animationPrefab.unitActiveEntitySpriteObj.SetActive(false);
            //if (battlefield.IsEntityActive(animationPrefab.unitID))
            //{
            //    animationPrefab.unitActiveEntitySpriteObj.SetActive(true);
            //    animationPrefab.unitActiveEntitySpriteObj.transform.position = animationPrefab.transform.position;
            //    animationPrefab.unitActiveEntitySpriteObj.GetComponent<SpriteRenderer>().color = new Color32(plr.DarkColor1, plr.DarkColor2, plr.DarkColor3, 255);

            //}
            //if (CombatSelection.selectedEntityId == animationPrefab.unitID && CombatSelection.selectedEntityId != -1)
            //{
            //    animationPrefab.unitActiveEntitySpriteObj.SetActive(true);
            //    animationPrefab.unitActiveEntitySpriteObj.transform.position = animationPrefab.transform.position;
            //    animationPrefab.unitActiveEntitySpriteObj.GetComponent<SpriteRenderer>().color = new Color32(plr.LightColor1, plr.LightColor2, plr.LightColor3, 255);
            //}
        }
    }

    void AnimateUnitMovement(string unitKeyword, FromToCoordinates fromToCoordinates, string playerid, MapMemory incMemory, int unitID)
    {
        DeleteAnimationOnCoordinates(fromToCoordinates.fromX, fromToCoordinates.fromY);
        CharacterTemplate characterTemplate = GameEngine.Data.CharacterTemplateCollection.findByKeyword(unitKeyword);

        if (characterTemplate.CombatAnimationPrefab == "" || characterTemplate.CombatMovementAnimation == "")
        {
            DisplayMap(incMemory); //this part is done by animations, but if we have none, then at least display the map
            return;
        }
        //TODO use something else here, sinse those are combat tiles? is it necessary?
        //unitBackground.SetTile(new Vector3Int(fromToCoordinates.toX, fromToCoordinates.toY, 0), null);
        //unitsTileMap.SetTile(new Vector3Int(fromToCoordinates.toX, fromToCoordinates.toY, 0), null);
        //activeEntitiesTileMap.SetTile(new Vector3Int(fromToCoordinates.toX, fromToCoordinates.toY, 0), null); //when flag bearer tilemap is implemented, include here


        //unitBackground.SetTile(new Vector3Int(fromToCoordinates.fromX, fromToCoordinates.fromY, 0), null); //these are needed if doing replay?
        //unitsTileMap.SetTile(new Vector3Int(fromToCoordinates.fromX, fromToCoordinates.fromY, 0), null);
        //activeEntitiesTileMap.SetTile(new Vector3Int(fromToCoordinates.fromX, fromToCoordinates.fromY, 0), null); //when flag bearer tilemap is implemented, include here


        GameObject animationPrefab = Instantiate(Resources.Load<GameObject>(characterTemplate.CombatAnimationPrefab), onWorldCanvas.transform, true);

        Vector3Int convertCellPositionToWorld = new Vector3Int(fromToCoordinates.fromX, fromToCoordinates.fromY, 0);
        Vector3 cellPosition = grid.CellToWorld(convertCellPositionToWorld);
        animationPrefab.transform.position = cellPosition;
        animationPrefab.transform.localScale = new Vector3(42, 42); //measured by eye to match tile size
                                                                    //AnimationClip animationClip = Resources.Load<AnimationClip>(characterTemplate.CombatMovementAnimation);
                                                                    //animationClip.SampleAnimation(animationPrefab, 1);

        AnimationPrefabController animationPrefabController = animationPrefab.GetComponent<AnimationPrefabController>();

        animationPrefabController.playerController = this;
        animationPrefabController.memoryTiles = incMemory;
        animationPrefabController.tile = incMemory.FindMemoryTileByCoordinates(fromToCoordinates.fromX, fromToCoordinates.fromY);
        animationPrefabController.unitID = animationPrefabController.memoryTile.UnitID;
        animationPrefabController.playerID = playerid;
        Vector3Int convertCellPositionToWorldDestination = new Vector3Int(fromToCoordinates.toX, fromToCoordinates.toY, 0);
        Vector3 cellPositionDestination = grid.CellToWorld(convertCellPositionToWorldDestination);
        animationPrefabController.targetPosition = cellPositionDestination;
        animationPrefabController.mode = AnimationPrefabController.MODE_PLAY_UNTIL_FINISHED_MOVING;
        animationPrefabController.animator.Play(characterTemplate.CombatMovementAnimation);
        Player plr = GameEngine.ActiveGame.scenario.FindPlayerByID(playerid);
        animationPrefabController.unitBackgroundSpriteObj.SetActive(true);
        animationPrefabController.unitBackgroundSpriteObj.GetComponent<SpriteRenderer>().color = new Color32(plr.Color1, plr.Color2, plr.Color3, 255);
        animationPrefabController.unitBackgroundSpriteObj.transform.position = cellPosition; //have to do here because the sprite doesnt follow parent for some reason
                                                                                             //unitBackgroundTile.color = new Color32(memoryTile.UnitBackgroundColor1, memoryTile.UnitBackgroundColor2, memoryTile.UnitBackgroundColor3, memoryTile.UnitBackgroundColorA);
                                                                                             //IEnumerator<string> answer = AI_METHOD(player.PlayerID);

        //StartCoroutine(answer);
        animationPrefabControllers.Add(animationPrefabController); //on refresh UI, delete & clear list? yes
        //MoveObjectToCoords(fromToCoordinates.toX, fromToCoordinates.toY, animationPrefab, 1);
        //IEnumerator<bool> glideProcess = GlideObjectFromCoords(fromToCoordinates.toX,fromToCoordinates.toY,animationPrefab,2);

        //StartCoroutine(glideProcess);

        Debug.Log("playing animation: " + characterTemplate.CombatMovementAnimation);
        SetAnimationGraphics();
    }

    

    internal void ShowReplay(OverlandReplay replay)
    {
        ClearAnimations();
        ClearInstantiatedObjects();
        if (replay.MapMemory != null)
        {
            DisplayMap(replay.MapMemory);
            //if (replay.AnimationMovementInfo == null && replay.AnimationStationaryInfo == null) 
            //{
            //    DisplayMap(replay.CombatMapMemory);
            //}

            //if (replay.AnimationStationaryInfo != null)//animations include displaymap at end of them
            //{
            //    StaticUnitAnimation(replay.AnimationStationaryInfo.UnitKW, replay.AnimationStationaryInfo.MapCoordinates, replay.AnimationStationaryInfo.PlayerID, replay.CombatMapMemory, replay.AnimationStationaryInfo.AnimationName, replay.AnimationStationaryInfo.UnitID);
            //}
            if (replay.AnimationMovementInfo != null)
            {
                AnimateUnitMovement(replay.AnimationMovementInfo.UnitKeyword, replay.AnimationMovementInfo.FromToCoordinates, replay.AnimationMovementInfo.PlayerID, replay.MapMemory, replay.AnimationMovementInfo.UnitID);
            }
        }

        if (replay.CombatMessages.Count > 0)
        {
            if (replay.MapMemory == null)
            {
                Debug.LogError("show replay null");
            }
            Debug.Log("replay combatmessage count: " + replay.CombatMessages.Count);
            foreach (CombatMessageInfo info in replay.CombatMessages)
            {
                GameEngine.ActiveGame.DisplayOverlandMessageToPlayersOnSquare(info.TotalMessage, info.AudioFiles, info.R, info.G, info.B, info.ParticleEffects, info.X_cord, info.Y_cord, ThisPlayer, this, replay.MapMemory);
            }

        }
    }

 

    void RefreshIfSeeingSkillTrees(int entityid)
    {
        if (skillTreesPanel.activeSelf)
        {
            SkillTreesPanelController skillTreesPanelController = skillTreesPanel.GetComponent<SkillTreesPanelController>();
            if (skillTreesPanelController.entity != null)
            {
                if (skillTreesPanelController.entity.UnitID == entityid)
                {
                    skillTreesPanelController.RefreshUI();
                }
            }
          
          
        }
    }

    void HideExpandedNotificationPanel(string incPlayerID)
    {
        if (expandedNotificationPanel.activeSelf && ThisPlayer.PlayerID == incPlayerID)
        {
            expandedNotificationPanel.SetActive(false);
        }
    }


    void RefreshIfSeeingBuilding(int buildingID)
    {
        if (gameSquirePanel.buildingInfoPanelController.gameObject.activeSelf)
        {
            if (gameSquirePanel.buildingInfoPanelController.gameSquare != null)
            {
                if (gameSquirePanel.buildingInfoPanelController.gameSquare.building.ID == buildingID)
                {
                    gameSquirePanel.buildingInfoPanelController.DisplayBuildingInfo(ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.BUILDING).Permission);
                }
            }
        }
    }
    void RefreshIfSeeingBuildingProduction(int productionID)
    {
        if (gameSquirePanel.buildingInfoPanelController.gameObject.activeSelf)
        {
            if (gameSquirePanel.buildingInfoPanelController.selectedProduction != null)
            {
                if (gameSquirePanel.buildingInfoPanelController.selectedProduction.ID == productionID)
                {
                    gameSquirePanel.buildingInfoPanelController.ShowProductionLines(gameSquirePanel.buildingInfoPanelController.selectedProduction, "");
                }
            }
        }
    }

    void DeselectArmyForAllControllers(int armyID)
    {
        foreach (GameObject obj in GameEngine.ActiveGame.playerControllersList)
        {
            PlayerController controller = obj.GetComponent<PlayerController>();
            if (controller.Selection.selectedArmyId == armyID)
            {
                controller.Selection.Clear(); 
            }
        }
    }


    void DisplaySkillPrediction(MultiplayerUICommand multiplayerUICommand)
    {
        
        string playerID = multiplayerUICommand.elements[0];
        int battlefieldID = Int32.Parse(multiplayerUICommand.elements[1]);
        SkillPredictionInfo skillPredictionInfo = (SkillPredictionInfo)multiplayerUICommand.obj;
        CombatMapGenerator combatMapGenerator = GetCombatMapGenerator();
        if (combatMapGenerator.player.PlayerID == playerID)
        {
            if (combatMapGenerator.battlefield != null)
            {
                if (combatMapGenerator.battlefield.ID == battlefieldID)
                {
                    combatMapGenerator.DisplayTrackedPredictions(skillPredictionInfo);
                }
            }
        }
    }

    /// <summary>
    /// this is important, so thats how we do it
    /// if we dont find the correct combatmap its ok, it means its on different device
    /// </summary>
    /// <param name="incPlayerID"></param>
    /// <param name="battlefieldID"></param>
    void RemoveTargeting(string incPlayerID, int battlefieldID)
    {
        foreach (GameObject obj in GameEngine.ActiveGame.playerControllersList)
        {
            PlayerController controller = obj.GetComponent<PlayerController>();
            CombatMapGenerator combatMap = controller.combatMapGeneratorPanel.GetComponent<CombatMapGenerator>();
            if (combatMap.player.PlayerID == incPlayerID && combatMap.battlefield != null)
            {
                if (combatMap.battlefield.ID == battlefieldID)
                {
                    combatMap.skillTargetSquares.Clear(); //this is important, so we remove command ONLY in this instance
                    combatMap.skillRangeTileMap.ClearAllTiles(); //doing this instead of refresh due to animations(if doing refresh, theres a tile behind animations)
                    combatMap.skillResolvePanel.gameObject.SetActive(false); //new addition 27.07.2023
                    combatMap.mapIsClickable = true;
                    //combatMap.RefreshUI();
                }
            }
        }
    }

    void SetInfoForSkillResolvePanel(string incPlayerID,string currentComponentKeyword, string info, string battlefieldID,object targetHexes,string cancelStatus)
    {
        foreach (GameObject obj in GameEngine.ActiveGame.playerControllersList)
        {
            PlayerController controller = obj.GetComponent<PlayerController>();
            CombatMapGenerator combatMap = controller.combatMapGeneratorPanel.GetComponent<CombatMapGenerator>();
            if (combatMap.player.PlayerID == incPlayerID && combatMap.battlefield != null)
            {
                if (combatMap.battlefield.ID == Int32.Parse(battlefieldID) && !combatMap.battlefield.IsPlayerAutoBattle(incPlayerID))
                {
                    if (targetHexes != null)
                    {
                        combatMap.skillTargetSquares = (List<CombatSquare>)targetHexes;
                    }
                    
                    SkillResolvePanelController skillResolvePanelController = combatMap.skillResolvePanel.GetComponent<SkillResolvePanelController>();

                    skillResolvePanelController.SetInfo(currentComponentKeyword, info,cancelStatus);
                    combatMap.mapIsClickable = true;
                    combatMap.RefreshUI(); //idk if needed so commented out for sake of animations
                }
            }
        }

    }
    void RefreshCombatMap(string incPlayerID, int battlefieldID)
    {
        CombatMapGenerator combatMap = GetCombatMapGenerator();
        if (combatMap.battlefield != null)
        {
            if (combatMap.battlefield.ID == battlefieldID && combatMap.player.PlayerID == incPlayerID && !combatMap.battlefield.IsPlayerAutoBattleCheck(PlayerID))
            {
                combatMap.DisplayMap(combatMap.mapMemory);
            }
        }
    }

    void CloseSkillResolvePanel(string incPlayerID, int battlefieldID)
    {
        foreach (GameObject obj in GameEngine.ActiveGame.playerControllersList)
        {
            PlayerController controller = obj.GetComponent<PlayerController>();
            CombatMapGenerator combatMap = controller.combatMapGeneratorPanel.GetComponent<CombatMapGenerator>();
            if (combatMap.player.PlayerID == incPlayerID && combatMap.battlefield != null)
            {
                if (combatMap.battlefield.ID == battlefieldID)
                {
                    combatMap.skillResolvePanel.gameObject.SetActive(false);
                    combatMap.RefreshUI();
                }
            }
        }
    }

    void InitializeSpecificPlayerBattlefieldManagmentPanel(string playerID)
    {
        foreach (GameObject obj in GameEngine.ActiveGame.playerControllersList)
        {
            PlayerController controller = obj.GetComponent<PlayerController>();
            if (controller.PlayerID == playerID)
            {
                BattlefieldManagmentPanelController battlefieldManagment = controller.battleManagmentPanel.GetComponent<BattlefieldManagmentPanelController>();
                battlefieldManagment.Initilize(GameEngine.ActiveGame.scenario.GetPlayersActiveBattlesIDs(controller.PlayerID));
            }

        }
    }

    void InitializeBattlefieldManagmentPanel()
    {
        foreach (GameObject obj in GameEngine.ActiveGame.playerControllersList)
        {
            PlayerController controller = obj.GetComponent<PlayerController>();
            BattlefieldManagmentPanelController battlefieldManagment = controller.battleManagmentPanel.GetComponent<BattlefieldManagmentPanelController>();
            battlefieldManagment.Initilize(GameEngine.ActiveGame.scenario.GetPlayersActiveBattlesIDs(controller.PlayerID));
        }
    }

    void RefreshAttackIconsIfSeeingArmy(int armyID)
    {
        MemoryArmy memoryArmy = ThisPlayer.MapMemory.FindMemoryArmyByArmyIDVisible(armyID);
        if (memoryArmy != null)
        {
            if (Selection.SelectedFriendlyOverlandArmy != null)
            {
                Selection.SelectArmy(Selection.selectedArmyId);
            }
            RefreshUI();
        }
    }

    /// <summary>
    /// processing UI commands in update
    /// since this is playercontroller, MAKE SURE that if you send command to specific player's playercontroller, that the command has info on the player
    /// which then must be processed here(checking if this player controllers id matches inc id)
    /// </summary>
    public void CheckMultiPlayerUICommands()
    {

        List<MultiplayerUICommand> commandsToRemove = new List<MultiplayerUICommand>();
        string outsideLoopCommand = "";
        lock (GameEngine.ActiveGame.MultiplayerUICommands)
        {
            foreach (MultiplayerUICommand command in GameEngine.ActiveGame.MultiplayerUICommands)
            {
                switch (command.command)
                {
                    case MultiplayerUICommand.SHOW_RED_PLAYER_MESSAGE:
                        if (command.elements[0] == PlayerID)
                        {
                            DisplayMessage(command.elements[1],Color.red);
                        }
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.SHOW_GRAY_PLAYER_MESSAGE:
                        GameEngine.ActiveGame.DisplayMessageToPlayer(command.elements[0],PlayerID, Color.gray);
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.REFRESH_IF_SEEING_EVENTS_ON_SQUARE: //this command might be entirely unneccesary, maybe used total refresh for sake of map?
                        RefreshOverlandEventChoicePanelIfOnSquare(new MapCoordinates(Int32.Parse(command.elements[0]), Int32.Parse(command.elements[1])));
                        commandsToRemove.Add(command);
                        break;

                    case MultiplayerUICommand.SAVE_ANIMATION: //dead command
                        CombatMapGenerator mapGen5 = combatMapGeneratorPanel.GetComponent<CombatMapGenerator>();
                        if (mapGen5.battlefield != null) //no need for replay check as this happens only when ur not in replay anyway
                        {
                            if (mapGen5.battlefield.ID == Int32.Parse(command.elements[0]))
                            {
                                Debug.Log("Save animation - animationStatic call");
                                AnimationStationayInfo animationStaticInfo = (AnimationStationayInfo)command.obj;
                                if (animationStaticInfo == null)
                                {
                                    Debug.LogError("wtf animationStatic is null");
                                }
                                //mapGen5.animationStationay = animationStaticInfo;
                            }
                        }
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.SELECT_COMBAT_SQUARE:
                        CombatMapGenerator mapGen4 = combatMapGeneratorPanel.GetComponent<CombatMapGenerator>();
                        if (mapGen4.battlefield != null) //no need for replay check as this happens only when ur not in replay anyway
                        {
                            if (mapGen4.battlefield.ID == Int32.Parse(command.elements[0]) && !mapGen4.battlefield.IsPlayerAutoBattleCheck(PlayerID))
                            {
                                MapCoordinates mapCoordinates = (MapCoordinates)command.obj;
                                mapGen4.ClickProcess((CombatSquare)mapGen4.battlefield.CombatMap.FindMapSquareByCoordinates(mapCoordinates.XCoordinate, mapCoordinates.YCoordinate),false);

                            }
                        }

                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.SHOW_ANIMATION:
                        CombatMapGenerator mapGen3 = combatMapGeneratorPanel.GetComponent<CombatMapGenerator>();
                        if (mapGen3.battlefield != null)
                        {
                            if (mapGen3.battlefield.ID == Int32.Parse(command.elements[0]) && !mapGen3.battlefield.IsPlayerAutoBattleCheck(PlayerID))
                            {
                                if (!mapGen3.isInReplay)
                                {
                                    int unitID = Int32.Parse(command.elements[1]);
                                    if (mapGen3.mapMemory.IsSeeingUnit(unitID))
                                    {
                                        Entity ent = mapGen3.battlefield.FindUnitByID((unitID), false);

                                        mapGen3.StaticUnitAnimation(ent.CharacterTemplateKeyword, new MapCoordinates(ent.BattlefieldCoordinates.XCoordinate, ent.BattlefieldCoordinates.YCoordinate), ent.FindCurrentOwnerID(), mapGen3.mapMemory, command.elements[2], unitID);
                                    }
                                }
                                else
                                {
                                    mapGen3.RefreshUI();
                                }
                            
                              
                            }
                        }


                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.SHOW_COMBAT_MOVEMENT:


                        CombatMapGenerator mapGen2 = combatMapGeneratorPanel.GetComponent<CombatMapGenerator>();
                        if (mapGen2.battlefield != null)
                        {
                            if (mapGen2.battlefield.ID == Int32.Parse(command.elements[1]) && !mapGen2.battlefield.IsPlayerAutoBattleCheck(PlayerID))
                            {
                                if (!mapGen2.isInReplay)
                                {
                                    int unitID = Int32.Parse(command.elements[3]);
                                    CombatMapMemory combatMemoryTiles = mapGen2.battlefield.GetPlayerCombatMapMemory(PlayerID);
                                    FromToCoordinates fromToCoordinates = (FromToCoordinates)command.obj;
                                    if (combatMemoryTiles.IsSeeingUnit(unitID))
                                    {
                                        mapGen2.AnimateUnitMovement(command.elements[0], fromToCoordinates, command.elements[2], mapGen2.mapMemory, unitID);
                                    }
                                }
                                else
                                {
                                    //if in replay, and something is going on, then set replays to refresh them
                                    mapGen2.RefreshUI(); //due to isInReplay = true, it will not be heavy on the system
                                }


                            }
                        }
                  
                      

                        commandsToRemove.Add(command);

                        break;
                    case MultiplayerUICommand.COMMAND_SELECT_OWN_FRIENDLY_ARMY:
                        if (Selection.SelectedFriendlyOverlandArmy != null)
                        {
                            Selection.SelectArmy(Selection.selectedArmyId);
                        }
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.REFRESH_ATTACK_ICONS_IF_SEEING_ARMY:
                        RefreshAttackIconsIfSeeingArmy(Int32.Parse(command.elements[0]));
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.CLOSE_SKILL_RESOLVE_PANEL:
                        CloseSkillResolvePanel(command.elements[0],Int32.Parse(command.elements[1]));
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.COMBAT_DISPLAY_MAP:
                        RefreshCombatMap(command.elements[0],Int32.Parse(command.elements[1]));
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.SET_INFO_FOR_SKILL_RESOLVE_PANEL:
                        SetInfoForSkillResolvePanel(command.elements[0], command.elements[1],command.elements[2], command.elements[3],command.obj,command.elements[4]);
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.COMBAT_CANCEL_TARGETING:

                        RemoveTargeting(command.elements[0], Int32.Parse(command.elements[1]));

                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.COMBAT_SHOW_PREDICTION:
                        DisplaySkillPrediction(command);
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.REFRESH_IF_SEEING_BUILDING:
                        RefreshIfSeeingBuilding(Int32.Parse(command.elements[0]));
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.REFRESH_IF_SEEING_BUILDING_PRODUCTION:
                        RefreshIfSeeingBuildingProduction(Int32.Parse(command.elements[0]));
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.COMBAT_SHOW_AFTER_BATTLE_PARTICIPANTS:

                        CombatMapGenerator mapGen1 = combatMapGeneratorPanel.GetComponent<CombatMapGenerator>();
                        if (mapGen1.battlefield != null) //can be null if battle was messed up really bad(battle where you were involved but no armies)
                        {
                            if (mapGen1.battlefield.ID == Int32.Parse(command.elements[0]))
                            {
                                mapGen1.ShowAfterBattleParticipants();
                            }
                        }
                    

                        commandsToRemove.Add(command);

                        break;
                    case MultiplayerUICommand.COMBAT_SHOW_CONTINUE_BUTTON:
                        if (PlayerID == command.elements[0])
                        {
                            Debug.Log("showing combat continue button to player: " + command.elements[0]);
                            CombatMapGenerator mapGen = combatMapGeneratorPanel.GetComponent<CombatMapGenerator>();

                            mapGen.continueButton.gameObject.SetActive(true);


                            commandsToRemove.Add(command); //we resolve this command only in correct UIs
                        }
                        break;
                    case MultiplayerUICommand.DESELECT_IF_SELECTED_ARMY:
                        DeselectArmyForAllControllers(Int32.Parse(command.elements[0]));
                     
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.REFRESH_MAP_IF_SEEING_MOVEMENT:
                        RefreshIfSeeingCoordinatesOverland((FromToCoordinates)command.obj);
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.COMBAT_MOVE_CAMERA_TO_ENTITY:
                        if (PlayerID == command.elements[1]) 
                        {
                            CombatMapGenerator combatMapGenerator33 = combatMapGeneratorPanel.GetComponent<CombatMapGenerator>();
                            if (combatMapGenerator33.CanShowCombatInfo())
                            {
                                if (combatMapGenerator33.battlefield.ID == Int32.Parse(command.elements[0]))
                                {
                                    Entity toMoveCameraTo = combatMapGenerator33.battlefield.GetActiveEntity(ThisPlayer.PlayerID);
                                    if (toMoveCameraTo != null)
                                    {
                                        MoveCameraToCoordinates(toMoveCameraTo.BattlefieldCoordinates.XCoordinate, toMoveCameraTo.BattlefieldCoordinates.YCoordinate);
                                        CombatSquare sqr = (CombatSquare)combatMapGenerator33.battlefield.CombatMap.FindMapSquareByCoordinates(toMoveCameraTo.BattlefieldCoordinates.XCoordinate, toMoveCameraTo.BattlefieldCoordinates.YCoordinate);
                                        combatMapGenerator33.ClickProcess(sqr, false);

                                    }
                                }
                            }
                       
                        }
                        
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.COMBAT_REFRESH_IF_SEEING_MOVEMENT:
                        CombatMapGenerator combatMapGenerator3 = combatMapGeneratorPanel.GetComponent<CombatMapGenerator>();
                       
                        if (combatMapGenerator3.battlefield != null) //null check is necessary, as a refresh could be requested even before player accepted to manually resolve. this shouldnt be a problem when we will have replays
                        {
                            if (combatMapGenerator3.battlefield.ID == Int32.Parse(command.elements[0]) && !combatMapGenerator3.battlefield.IsPlayerAutoBattleCheck(PlayerID))
                            {
                                if (!combatMapGenerator3.isInReplay)
                                {
                                    //combatMapGenerator3.RefreshUI();
                                    FromToCoordinates fromToCoordinates = (FromToCoordinates)command.obj;
                                    if (combatMapGenerator3.IsSeeingCoordinates(fromToCoordinates))
                                    {
                                        //combatMapGenerator3.battlefield.RevealSquares(combatMapGenerator3.mapMemory); //now done in movement itself
                                        //combatMapGenerator3.DisplayMap(combatMapGenerator3.mapMemory);
                                        combatMapGenerator3.RefreshUI(); //now doing full refresh, because we need to refresh on the replay
                                    }
                                }
                                else
                                {
                                    combatMapGenerator3.RefreshUI();
                                }
                            
                                 
                            }
                        }
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.REFRESH_COMBAT_UI_PLAYER:
                        CombatMapGenerator combatMapGenerator4 = combatMapGeneratorPanel.GetComponent<CombatMapGenerator>();
                        if (combatMapGenerator4.player.PlayerID == command.elements[1]) //removed isInReplay check here, as it is not needed now
                        {
                            if (combatMapGenerator4.battlefield != null) //null check is necessary, as a refresh could be requested even before player accepted to manually resolve. this shouldnt be a problem when we will have replays
                            {
                                if (combatMapGenerator4.battlefield.ID == Int32.Parse(command.elements[0]) && !combatMapGenerator4.battlefield.IsPlayerAutoBattleCheck(PlayerID))
                                {
                                    combatMapGenerator4.RefreshUI();
                                }
                            }

                        }

                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.SHOW_COMBAT_SCREEN_START_TURN_MESSAGE:
                        CombatMapGenerator combatMapGen = combatMapGeneratorPanel.GetComponent<CombatMapGenerator>();
                       
                        if (PlayerID == command.elements[1])
                        {
                            bool selectedBattlefield = false;

                            if (combatMapGen.battlefield != null) //no need to check replay, its ok to get the message
                            {
                                if (combatMapGen.battlefield.ID == Int32.Parse(command.elements[0]) && !combatMapGen.battlefield.IsPlayerAutoBattleCheck(PlayerID))
                                {
                                    DisplayMessage("Your turn",Color.green);
                                }
                                else
                                {
                                    selectedBattlefield = true;
                                }

                            }
                            else
                            {
                                selectedBattlefield = true;
                            }
                           
                            if (selectedBattlefield)
                            {
                                DisplayMessage("Your turn in other battlefield", Color.white);
                            }
                        }

                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.REFRESH_COMBAT_UI:
                        CombatMapGenerator combatMapGenerator2 = combatMapGeneratorPanel.GetComponent<CombatMapGenerator>();
                        if (combatMapGenerator2.battlefield != null) //null check is necessary, as a refresh could be requested even before player accepted to manually resolve. this shouldnt be a problem when we will have replays
                        {
                            if (combatMapGenerator2.battlefield.ID == Int32.Parse(command.elements[0]) && !combatMapGenerator2.battlefield.IsPlayerAutoBattleCheck(PlayerID))
                            {
                                combatMapGenerator2.RefreshUI();
                            }
                        }
                   
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.SHOW_COMBAT_MESSAGE:
                        CombatMessageInfo combatMessageInfo = (CombatMessageInfo)command.obj;
                        CombatMapGenerator combatMapGenerator = combatMapGeneratorPanel.GetComponent<CombatMapGenerator>();
                        if (combatMapGenerator.battlefield != null)
                        {
                            if (combatMapGenerator.battlefield.ID == Int32.Parse(command.elements[0]) && !combatMapGenerator.battlefield.IsPlayerAutoBattleCheck(PlayerID))
                            {
                                if (!combatMapGenerator.isInReplay)
                                {
                                    GameEngine.ActiveGame.DisplayCombatMessageToPlayersOnSquare(combatMessageInfo.TotalMessage, combatMessageInfo.AudioFiles, combatMessageInfo.R, combatMessageInfo.G, combatMessageInfo.B, combatMessageInfo.ParticleEffects, combatMessageInfo.X_cord, combatMessageInfo.Y_cord, combatMessageInfo.SourceX, combatMessageInfo.SourceY, combatMessageInfo.Battlefield, combatMessageInfo.ShowAtSourceHex, ThisPlayer, this, combatMessageInfo.Battlefield.GetPlayerCombatMapMemory(PlayerID), combatMessageInfo.AnimationStaticInfo, combatMessageInfo.ShowIfNoVision);
                                }
                                else
                                {
                                    combatMapGenerator.RefreshUI();
                                }
                                
                            }
                        }
                        // GameEngine.ActiveGame.DisplayCombatMessageToPlayersOnSquare(combatMessageInfo.TotalMessage, combatMessageInfo.AudioFiles, combatMessageInfo.R, combatMessageInfo.G, combatMessageInfo.B, combatMessageInfo.ParticleEffects, combatMessageInfo.X_cord, combatMessageInfo.Y_cord, combatMessageInfo.SourceX, combatMessageInfo.SourceY, combatMessageInfo.Battlefield, combatMessageInfo.ShowAtSourceHex, ThisPlayer, this, true);
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.SHOW_OVERLAND_MESSAGE:
                        if (command.elements[0] == PlayerID)
                        {
                            CombatMessageInfo overlandMessageInfo = (CombatMessageInfo)command.obj;
                            if (!isInReplay)
                            {
                                GameEngine.ActiveGame.DisplayOverlandMessageToPlayersOnSquare(overlandMessageInfo.TotalMessage, overlandMessageInfo.AudioFiles, overlandMessageInfo.R, overlandMessageInfo.G, overlandMessageInfo.B, overlandMessageInfo.ParticleEffects, overlandMessageInfo.X_cord, overlandMessageInfo.Y_cord, ThisPlayer, this, ThisPlayer.MapMemory);
                            }
                            else
                            {
                                RefreshUI();
                            }
                        }
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.OPEN_BATTLE_MANAGMENT_PANEL_OBSERVER:
                        ThisPlayer.GameState = GameEngine.Data.GameStateCollection.findByKeyword(GameState.State.BATTLE_PHASE);
                        List<int> battlefieldIDs = new List<int>();
                        lock (GameEngine.ActiveGame.scenario.ActiveBattles.Battlefields)
                        {
                            foreach (BattlefieldOld battlefield in GameEngine.ActiveGame.scenario.ActiveBattles.Battlefields)
                            {
                                battlefieldIDs.Add(battlefield.ID);
                            }
                        }

                        BattlefieldManagmentPanelController battlefieldManagmentPanelController2 = battleManagmentPanel.GetComponent<BattlefieldManagmentPanelController>();

                        
                        battlefieldManagmentPanelController2.Initilize(battlefieldIDs); //we create buttons to switch between player battlefields
                        battlefieldManagmentPanelController2.SelectBattlefield(battlefieldIDs[0]); //auto-select 1 battlefield

                        RefreshUI();

                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.INITIALIZE_BATTLEFIELD_MANAGMENT_PANEL_SPECIFIC_PLAYER:

                        InitializeSpecificPlayerBattlefieldManagmentPanel(command.elements[0]);

                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.INITIALIZE_BATTLEFIELD_MANAGMENT_PANEL: //its imporant to save sides to list BEFORE we get to anything
                        InitializeBattlefieldManagmentPanel(); //has to be done by all UI
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.OPEN_BATTLE_MANAGMENT_PANEL:
                        PlayerController playerController = GameEngine.ActiveGame.FindPlayerController(command.elements[0]);
                        if (playerController != null) //this command MUST be processed by correct player, otherwise ignore
                        {
                            playerController.ThisPlayer.GameState = GameEngine.Data.GameStateCollection.findByKeyword(GameState.State.BATTLE_PHASE);
                            playerController.isObserver = false; //setting this as false, because in endGlobalTurn everyone is observer to stop doing stuff during processing
                            //since now we should be doing stuff, we no longer are observers until we finish our combats
  

                            Debug.Log("opening battle managment panel for player: " + PlayerID);
                            playerController.battleManagmentPanel.gameObject.SetActive(true);
                            BattlefieldManagmentPanelController battlefieldManagmentPanelController = playerController.battleManagmentPanel.GetComponent<BattlefieldManagmentPanelController>();

                            List<int> battlefields = (List<int>)command.obj;
                            //List<int> battlefields = GameEngine.ActiveGame.scenario.GetPlayersActiveBattlesIDs(ThisPlayer.PlayerID); //in future we could also have "visible" battles without participation of the player
                            if (battlefields.Count > 0)
                            {
                                //battlefield tabs
                                //battlefieldManagmentPanelController.Initilize(battlefields); //we create buttons to switch between player battlefields
                                battlefieldManagmentPanelController.SelectBattlefield(battlefields[0]); //auto-select 1 battlefield



                            }
                            else
                            {
                               
                            } //the else part shoudnt be done here
                            //else
                            //{
                            //    //there are literally no battlefields to see, so this player moves on to after battle phase
                            //    ThisPlayer.GameState = GameEngine.Data.GameStateCollection.findByKeyword(GameState.State.AFTER_BATTLE_PHASE);
                            //}
                            //playerController.RefreshUI();

                           
                        }
                        RefreshUI();
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.REFRESH_IF_SEEING_ENTITY_SKILLS:
                        RefreshIfSeeingSkills(Int32.Parse(command.elements[0]));
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.REFRESH_IF_SEEING_ENTITY_SKILL_TREE:
                        RefreshIfSeeingSkillTrees(Int32.Parse(command.elements[0]));
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.REFRESH_IF_SEEING_RECIPES:
                        RefreshIfSeeingRecipes(command.elements[0], Int32.Parse(command.elements[1]));
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.REFRESH_IF_SEEING_ENTITY_INVENTORY:
                        RefreshIfSeeingEntityStash(command.elements[0],Int32.Parse(command.elements[1]));
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.HIDE_EXPANDED_NOTIFICATION_PANEL:
                        HideExpandedNotificationPanel(command.elements[0]);
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.COMMAND_REFRESH_UI_SPECIFIC_PLAYER:
                        if (ThisPlayer.PlayerID == command.elements[0])
                        {
                            RefreshUI();
                        }
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.REFRESH_IF_SEEING_QUESTS:
                        RefreshQuestsIfSeeing(command.elements[0],Int32.Parse(command.elements[1]));
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.PROCEED_TO_END_TURN:
                        GameEngine.ActiveGame.EndGlobalTurnThread();
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.REFRESH_IF_ENTITIES_SELECTED:
                        List<int> ids = (List<int>)command.obj;
                        RefreshIfEntitiesSelected(ids);
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.REFRESH_IF_ENTITY_AND_PLAYER_SELECTED:
                        Debug.Log("goofer " + command.elements[0] + " " + command.elements[1] + " | " + ThisPlayer.PlayerID + " " + Selection.selectedEntityId + " plr id " + PlayerID + " test str " + testStr);
                        if (ThisPlayer.PlayerID == command.elements[1])
                        {
                            RefreshIfEntityIsSelected(command.elements[0]);
                        }
                      
                        commandsToRemove.Add(command);
                        break;

                    case MultiplayerUICommand.REFRESH_IF_ENTITY_SELECTED:
                        RefreshIfEntityIsSelected(command.elements[0]);
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.REFRESH_IF_ENTITY_SELECTED_BY_PLAYER:
                        if (command.elements[1] == PlayerID)
                        {
                            RefreshIfEntityIsSelected(command.elements[0]);
                        }
                       
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.REFRESH_IF_SEEING_PLAYER_INVENTORY:
                        RefreshIfSeeingInventory(command.elements[0]);
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.REFRESH_IF_SEEING_SHOP:
                        RefreshIfSeeingShop(Int32.Parse(command.elements[1]),command.elements[0]);
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.STOP_OBSERVER_MODE_TO_ALL:
                        MultiplayerMessage stopObservingMessage = new MultiplayerMessage(MultiplayerMessage.ProceedToStartTurn,"","");
                        GameEngine.ActiveGame.clientManager.Push(stopObservingMessage);
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.REFRESH_SOURCE_ITEM:
                        SourceInfo sourceInfo = (SourceInfo)command.obj;
                        string sourceInfoPlayerID = command.elements[0];
                        RefreshSourceInfoUI(sourceInfo, sourceInfoPlayerID);
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.ITEM_TRASNFER:
                        MultiplayerItemTransfer itemTransfer = (MultiplayerItemTransfer)command.obj;
                       
                        RefreshIfSeeingTransferUI(itemTransfer.from, itemTransfer.to,command.elements[0]);
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.COMMAND_TURN_INTO_OBSERVER:
                        StartObserving(true);
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.REFRESH_MAP_IF_SEEING_SQUARE_FROM_PLAYER:
                        MapCoordinates locCoords2 = (MapCoordinates)command.obj;
                        if (PlayerID == command.elements[0])
                        {
                            RefreshIfSeeingCoords(locCoords2.XCoordinate, locCoords2.YCoordinate);
                        }
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.REFRESH_MAP_IF_SEEING_SQUARE:
                        MapCoordinates locCoords = (MapCoordinates)command.obj;
                        RefreshIfSeeingCoords(locCoords.XCoordinate,locCoords.YCoordinate);

                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.REFRESH_EVENT_STASH_PANEL:
                        PlayerInventoryController eventStashPanelContr = eventStashPanel.GetComponent<PlayerInventoryController>();
                        if (eventStashPanel.activeSelf) //if panel is active
                        {
                            if (eventStashPanelContr.playerController.playerID == command.elements[0]) //if panel's playercontroller belongs to the plr at elements[0], then refresh as new items have been recieved
                            {
                                eventStashPanelContr.RefreshUI();
                            }
                        }
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.START_TURN:
                        Debug.Log(" MultiplayerUICommand.START_TURN for " + playerID);
                        outsideLoopCommand = command.command;
    
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.COMMAND_STOP_OBSERVING:
                        StartObserving(false);
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.RESOLVE_EVENT_PLAYER:
                        if (ThisPlayer.isAI && GameEngine.ActiveGame.isHost || GameEngine.ActiveGame.scenario.IsPlayerPlayedByThisMachine(PlayerID))
                        {
                            Player plr = GameEngine.ActiveGame.scenario.FindPlayerByID(PlayerID);
                            plr.resolveEvents(ThisPlayer.isAI);
                        }
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.RESOLVE_EVENTS: //doesnt matter which playercontroller resolves this
                        Debug.Log("resolve events called");
                        //  StartObserving(false);
                        outsideLoopCommand = command.command;
                        commandsToRemove.Add(command);
                        break; 
                    case MultiplayerUICommand.PROCEED_TO_MAIN_PHASE: //doesnt matter which playercontroller resolves this
                        //GameState gameState = (GameState)command.obj;
                        //GameEngine.ActiveGame.scenario.ProceedToMainPhase();
                        GameEngine.ActiveGame.DisplayMessageToPlayer("Turn starts",PlayerID,Color.gray);
                        GameEngine.ActiveGame.flashWindowController.FlashWindowInTaskbar(); //tried to do this with AI, didnt work in build
                        RefreshUI();
                         
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.DISABLE_EVENTS_PANEL:
                        eventsPanel.SetActive(false);
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.COMMAND_REFRESH_UI:
                        RefreshUI();
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.AFTER_INSTANTIATE: //only host gets this command, doesnt matter which playercontroller processes it
                        GameEngine.ActiveGame.AfterInstantiateThread();
                        //GameEngine.ActiveGame.AfterInstantiateThread();
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.SELECT_ARMY_AND_REFRESH:
                        int armyid = Int32.Parse(command.elements[0]);
                        if (Selection.selectedArmyId == armyid)
                        {
                            Selection.SelectArmy(armyid); //? gives red on end turn
                        }
                      
                        RefreshUI();
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.COMMAND_MOVE_ARMY:
                        ArmyMovementInfo armyMovementInfo = (ArmyMovementInfo)command.obj;
                        Army army = GameEngine.ActiveGame.scenario.FindArmyByID(armyMovementInfo.armyID);
                        MapSquare loc = GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareByCoordinates(army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);
                        MapSquare dest = GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareByCoordinates(armyMovementInfo.newCoordX,armyMovementInfo.newCoordY);
                        GameEngine.ActiveGame.scenario.Movement(army,loc,dest,armyMovementInfo.modifier,false,false);
                        RefreshIfSeeingCoords(army.Location.WorldMapCoordinates.XCoordinate,army.Location.WorldMapCoordinates.YCoordinate); //should also refresh initial army coordinates(otherwise army stays on other computer that didnt see where army went)
                        //RefreshUI();
                        commandsToRemove.Add(command);
                        break;
                    case MultiplayerUICommand.RELEASE_END_TURN_UI:
                        if (command.elements[0] == playerID)
                        {
                            isEndingTurn = false;
                            isObserver = false;
                            nextTurnBtn.interactable = true;
                            nextTurnBtn.GetComponentInChildren<Text>().text = "End turn";
                            overlandReplayController.gameObject.SetActive(true);
                            commandsToRemove.Add(command);
                        }
                      
                        break;
                    case MultiplayerUICommand.DISABLE_END_TURN_UI:
                        if (command.elements[0] == playerID)
                        {
                            nextTurnBtn.interactable = true;
                            overlandReplayController.gameObject.SetActive(false);
                            commandsToRemove.Add(command);
                        }
                        break;
                    case MultiplayerUICommand.RE_INITIATE_PLAYER_CONTROLLERS:
                        GameEngine.ActiveGame.DeletePlayerPrefabs();
                        GameEngine.ActiveGame.NewInitilizeLocalPlayerPrefabs();
                        GameEngine.ActiveGame.playerControllerSwitcher.OnDeleteAutoSelect();
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
        switch (outsideLoopCommand) //for large commands, using commands outside of lock & foreach loop
        {
            case MultiplayerUICommand.START_TURN:
                Debug.Log("outsideLoopCommand " + outsideLoopCommand);
                GameEngine.ActiveGame.scenario.ResolveAllPlayerEvents();
                StartObserving(false);
                GameEngine.ActiveGame.taskStatusOutput.text = "";
                //if (GameEngine.ActiveGame.isHost)
                //{
                //    GameEngine.ActiveGame.StartAI();
                //}
                RefreshUI();
                break;
            default:
                break;
        }
    }
    void RefreshIfEntitiesSelected(List<int> ids)
    {
        foreach (int id in ids)
        {
            if (this.Selection.selectedEntityId == id)
            {
                RefreshUI();
                return;
            }
        }
    }
    private void RefreshIfEntityIsSelected(string entityIDstring)
    {
        Debug.Log("RefreshIfEntityIsSelected call");
        int entityID = Int32.Parse(entityIDstring);
        if (this.Selection.selectedEntityId == entityID) {
            RefreshUI();
        }
    }

    MapSquare current;
    bool showTooltip = false;
    float tooltipDelay = 0.2f;
    float waitDelay = 0.2f;
    bool showTool = false;
    void Update()
    {
     
        CheckMultiPlayerUICommands();
        timer += Time.deltaTime;
        Army army = Selection.SelectedFriendlyOverlandArmy;
        if (timer > waitTime && army != null)
        {
            if (!army.isMoving)
            {
                visualTime = timer;

                // Remove the recorded 2 seconds.
                timer = 0;
                //timer = timer - waitTime;
                if (blink)
                {
                    blink = false;
                }
                else if (!blink)
                {
                    blink = true;
                }
                ShowSelection();
            }


        }

        if (checkFps)
        {
            deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
            float fps = 1.0f / deltaTime;
            gameStateText.text = fps.ToString();

        }

   
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);
        if (raysastResults.Count > 0 && raysastResults[0].gameObject.name == "Ground")
        {
            Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int coordinate = groundTileMap.WorldToCell(mouseWorldPos);
            MapSquare hoverMapSquare = GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareByCordinates(coordinate.x, coordinate.y, coordinate.z);

            if (current == hoverMapSquare)
            {
                if (showTooltip == false && showTool == false)
                {
                    tooltipDelay -= Time.deltaTime;
                }
            }
            else
            {
                RemoveToolTip();
                current = hoverMapSquare;
                showTool = false;
                tooltipDelay = waitDelay;
            }

            if (tooltipDelay < 0)
            {
                showTooltip = true;
            }

            if (showTooltip)
            {
                showTooltip = false;
                showTool = true;
                tooltipDelay = waitDelay;
                GameSquare gameSquare = hoverMapSquare as GameSquare;
                ActivateGameSquareTooltip(gameSquare);
            }

        }
        else {
            RemoveToolTip();
        }

  


 






        //if (Input.GetKeyDown("[8]")) {
        //    DirectionMover("North");
        //}

        //if (Input.GetKeyDown("[7]"))
        //{
        //    DirectionMover("Northwest");
        //}

        //if (Input.GetKeyDown("[9]"))
        //{
        //    DirectionMover("Northeast");
        //}

        //if (Input.GetKeyDown("[4]"))
        //{
        //    DirectionMover("West");
        //}

        //if (Input.GetKeyDown("[6]"))
        //{
        //    DirectionMover("East");
        //}

        //if (Input.GetKeyDown("[1]"))
        //{
        //    DirectionMover("Southwest");
        //}

        //if (Input.GetKeyDown("[3]"))
        //{
        //    DirectionMover("Southeast");
        //}


    }




    public void RemoveToolTip()
    {
        toolTip.SetActive(false);
    }


    public void ActivateGameSquareTooltip(GameSquare gameSquare) {

        this.toolTip.SetActive(true);
        GameSquareTooltipController gameSquareTooltipController = this.toolTip.GetComponent<GameSquareTooltipController>();
        gameSquareTooltipController.CreateTooltipForGameSquare(gameSquare, this);

    }
    /// <summary>
    /// the most global combat panel UI activation
    /// </summary>
    /// <param name="toggle"></param>
    /// <param name="nextGameState"></param>
    public void ToggleCombatPanel(bool toggle,GameState nextGameState)
    {
        bool debug = true;
        if (toggle)
        {
            this.Selection.Clear();
            if (debug)
            {
                Debug.Log("ToggleCombatPanel toggle on");
            }
            battleManagmentPanel.gameObject.SetActive(true);
            BattlefieldManagmentPanelController battlefieldManagmentPanelController = battleManagmentPanel.GetComponent<BattlefieldManagmentPanelController>();

            List<int> battlefields = GameEngine.ActiveGame.scenario.GetPlayersActiveBattlesIDs(ThisPlayer.PlayerID);
            //battlefield tabs
            battlefieldManagmentPanelController.Initilize(battlefields);



            combatMapGeneratorPanel.SetActive(true);
            CombatMapGenerator combatMapGenerator = combatMapGeneratorPanel.GetComponent<CombatMapGenerator>();
            //combatMapGenerator.Activate(true);
            //combatMapGenerator.battlefieldGrid.gameObject.SetActive(true);
            combatMapGenerator.nextGameState = nextGameState;
            //if (GameEngine.ActiveGame.scenario.QueuedUpBattles.Battlefields[0] == null)
            //{
            //    Debug.LogError("null battlefield in scenario");
            //}
            //combatMapGenerator.test = 1;
            //combatMapGenerator.InitilizeCombatMapWindow(GameEngine.ActiveGame.scenario.QueuedUpBattles.Battlefields[0]);
            //we initilize some battlefield by default
            //combatMapGenerator.InitilizeCombatMapWindow(GameEngine.ActiveGame.scenario.ActiveBattles.FindBattleByID(battlefields[0]));

            //battlefieldManagmentPanelController.SetSelectedTab(battlefields[0]);
            battlefieldManagmentPanelController.SelectBattlefield(battlefields[0]);

         

            if (debug)
            {
                Debug.Log("combat panel toggle for " + ThisPlayer.PlayerID);
            }
      
        }
        else
        {
            combatMapGeneratorPanel.SetActive(false);
            CombatMapGenerator combatMapGenerator = combatMapGeneratorPanel.GetComponent<CombatMapGenerator>();
            combatMapGenerator.mapIsClickable = false;
          //  combatMapGenerator.battlefieldGrid.gameObject.SetActive(false);
            battleManagmentPanel.gameObject.SetActive(false);

            if (debug)
            {
                Debug.Log("ToggleCombatPanel toggle off for " + ThisPlayer.PlayerID);
            }
        }
        RefreshUI();
    }


    public void RefreshAttackButtons()
    {
        foreach (var item in attackButtons)
        {
            Destroy(item);
        }
        attackButtons.Clear();

    }
 
    public void RefreshPlayerCanvases()
    {
        string canvasPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.PLAYER_CANVAS).Permission;
        string worldCanvasPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.PLAYER_WORLD_CANVAS).Permission;
        string overlandGridPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.OVERLAND_GRID).Permission;
        string battlefieldGridPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.BATTLEFIELD_GRID).Permission;
        string overlandPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.PLAYER_OVERLAND_UI).Permission;

        if (overlandPermission == GameState.Permission.PLAYER_OVERLAND_UI_DISABLED)
        {
            PlayerOverlandUI.SetActive(false);
        }
        if (overlandPermission == GameState.Permission.PLAYER_OVERLAND_UI_ENABLED)
        {
            PlayerOverlandUI.SetActive(true);
        }

        if (canvasPermission == GameState.Permission.PLAYER_CANVAS_DISABLED)
        {
            playerCanvas.gameObject.SetActive(false);
        }
        if (canvasPermission == GameState.Permission.PLAYER_CANVAS_ENABLED)
        {
            playerCanvas.gameObject.SetActive(true);
        }

        if (worldCanvasPermission == GameState.Permission.PLAYER_WORLD_CANVAS_DISABLED)
        {
            onWorldCanvas.gameObject.SetActive(false);
        }

        if (worldCanvasPermission == GameState.Permission.PLAYER_WORLD_CANVAS_ENABLED)
        {
            onWorldCanvas.gameObject.SetActive(true);
        }

        if (overlandGridPermission == GameState.Permission.OVERLAND_GRID_DISABLED)
        {
            grid.gameObject.SetActive(false);
        }

        if (overlandGridPermission == GameState.Permission.OVERLAND_GRID_ENABLED)
        {
            grid.gameObject.SetActive(true);
        }
        if (ThisPlayer.GameState.Keyword != GameState.State.BATTLE_PHASE)
        {
            if (battlefieldGridPermission == GameState.Permission.BATTLEFIELD_GRID_ENABLED)
            {
                GetCombatMapGenerator().battlefieldGrid.gameObject.SetActive(true);
                Debug.Log("Enable battlefieldGrid for player using states: " + ThisPlayer.PlayerID);
            }

            if (battlefieldGridPermission == GameState.Permission.BATTLEFIELD_GRID_DISABLED)
            {
                GetCombatMapGenerator().battlefieldGrid.gameObject.SetActive(false);
            }
        }
   
    }

    public void DisplayMessage(string message, Color color)
    {
        MessagePanelController panelController = messagePanel.GetComponent<MessagePanelController>();
        panelController.CreateMessage(message,color);
    }
    public void RefreshplayerCamera()
    {
        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.PLAYER_CAMERA).Permission == GameState.Permission.PLAYER_CAMERA_ENABLED)
        {
            playerCamera.gameObject.SetActive(true);
        }
        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.PLAYER_CAMERA).Permission == GameState.Permission.PLAYER_CAMERA_DISABLED)
        {
            playerCamera.gameObject.SetActive(false);
        }
    }

    void HidePanels()
    {
        Selection.Clear();
        allHeroesPanel.SetActive(false);
        gameSquirePanel.gameObject.SetActive(false);
    }

    public void DismissNotification(Notification notification)
    {
        //MultiplayerMessage dismissNotification = new MultiplayerMessage(MultiplayerMessage.DismissNotification, PlayerID, notification.ID.ToString());
        //GameEngine.ActiveGame.clientManager.Push(dismissNotification);
        notification.IsDismissed = true;
        RefreshUI();
    }

    void RefreshReplayPanel()
    {
        string permission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.OVERLAND_REPLAY_PANEL).Permission;
        if (permission == GameState.Permission.OVERLAND_REPLAY_PANEL_FULL_ACCESS && !isInReplay) //is in repl
        {
            overlandReplayController.gameObject.SetActive(true);
        }
        else if (permission == GameState.Permission.OVERLAND_REPLAY_PANEL_RESTRICTED)
        {
            if (isInReplay)
            {
                overlandReplayController.LastReplay();
            }
            overlandReplayController.gameObject.SetActive(false);
           
        }
    }
    void ShowEndGamePanel()
    {
        //not refreshing, because its over
        if (!endGamePanel.activeSelf)
        {
            endGamePanel.gameObject.SetActive(true);

            EndGamePanelController endGamePanelController = endGamePanel.GetComponent<EndGamePanelController>();
            endGamePanelController.ShowScoreGraphs(Scoreboard.MILITARY_POWER);
        }
        
    }

    void ShowGameLostPanel()
    {
        if (!endGamePanel.activeSelf)
        {
            endGamePanel.gameObject.SetActive(true);

            EndGamePanelController endGamePanelController = endGamePanel.GetComponent<EndGamePanelController>();
            endGamePanelController.ShowDefeatScreen();
        }
    }

    public void RefreshUI()
    {
        if (GameEngine.ActiveGame.scenario.Ended)
        {
            ShowEndGamePanel();
            return;
        }
        if (ThisPlayer.Defeated)
        {
            ShowGameLostPanel();
            return;
        }
        bool debugRefresh = false;
        bool timer = false;
        if (debugRefresh)
        {
            refreshCounter++;
            Debug.Log("RefreshUI counter " + refreshCounter + " " + ThisPlayer.PlayerID);
          
       
        }
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Reset();
            GameEngine.ActiveGame.GameStopwatch.Start();
        }
        RefreshPlayerManagmentPanel();
        RefreshReplayPanel();
        overlandReplayController.SetReplays(ThisPlayer.MapMemory.Replays);
        if (isInReplay)
        {
            HidePanels();
            return;
        }
        allHeroesPanel.SetActive(true);
        gameSquirePanel.gameObject.SetActive(true);
        //Debug.Log("RefreshUI for " + ThisPlayer.PlayerID);
        CheckCombatMapGenerator();
        ClearInstantiatedObjects();
        //DisplayLocalActivePlayer();
        RemoveOverlandSelectionGraphic();
        RefreshOverlandEventChoicePanel();
        //GameEngine.ActiveGame.UpdateMemoryMapWithActiveVision(thisPlayer);
        ShowGameStateText();
        ShowAllHeroes();
        RefreshHeroesButtonSelection();
        RefreshHeroEquippedInventory();
        ShowNotifications();
        DisplayMap(thisPlayer);
     
        RefreshTurnCounter();
        //DisplayGemsAndHeroesOnPanel();
        RefreshVision();
        RefreshRecipeUI();
       // RefreshArmyPanel(); 
        RefreshBuildingInfoUI();
        //      RefreshArmyPanel(); //already called
       
        //RefreshMultipleArmiesPanel();
     
        AssignPanelsToArmy();

        RefreshShopButton();
        RefreshShopWindowUI();
        RefreshExpandedPanel();
        RefreshEventsPanel();




        RefreshQuestsPanel();
        RefreshSkillsPanel();
        RefreshUpkeepPanel();
        RefreshBodyPartsUI();
        RefreshEntityStashPanel();
        RefreshEventStashPanel();
        RefreshButtons();


        RefreshEntityControls();
        RefreshplayerCamera();
  
        RefreshResource();

        RefreshInventoryPanel();


        RefreshTradePanel();
        RefreshTransactionItemPanel();
        RefreshSkillTreesButton();
        RefreshSkillTreesPanel();
        RefreshPlayerCanvases();
        RefreshDungeonPanel();
        RefreshPlayerInfoPanel();


        UpdateSelectionGraphics();
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Stop();
            Debug.Log("PlayerController.RefreshUI took " + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
        }

    }

    public void RefreshRecipeUI()
    {
        if (recipePanel.activeSelf)
        {
            OpenRecipeClick(true);
        }
        
    }

    public void RefreshSkillsPanel()
    {
        if (skillsPanel.activeSelf)
        {
            OpenHeroSkills(true);
            SkillsPanelController skillsPanelController = skillsPanel.GetComponent<SkillsPanelController>();
            if (skillsPanelController.allBonusesPanel.activeSelf)
            {
                skillsPanelController.ShowAllBonuses(true);
            }
        }
    }

    public void RefreshEntityStashPanel()
    {
        if (entityStashPanel.activeSelf)
        {
            PlayerInventoryController playerInventoryController = entityStashPanel.GetComponent<PlayerInventoryController>();
            playerInventoryController.RefreshUI();
        }
    }

    public void RefreshEventStashPanel()
    {
        if (eventStashPanel.activeSelf)
        {
            DisplayEventItems();
        }
    }


    public bool CheckInactiveHeroes()
    {
        if (!GameEngine.ActiveGame.GeneralOptions.NotifyAboutInActiveHeroes.BoolValue)
        {
            return true;
        }
        List<Entity> heroes = GameEngine.ActiveGame.scenario.FindAllHeroesByPlayerID(ThisPlayer.PlayerID);
        Notification notification = new Notification();
        notification.IsOverland = true;
        notification.BgImageR = 255;
        notification.BgImageG = 2;
        notification.BgImageB = 2;
        notification.Type = Notification.NotificationType.TYPE_HERO_INACTIVE;
        notification.ExpandedText = "Inactive heroes: ";
        notification.HeaderText = "Inactive heroes detected!";
  
        int count = 0;
        foreach (Entity hero in heroes)
        {
            if (hero.MovementRemaining > 0 && hero.Mission == null)
            {
                count = 1;
                notification.TargetID = hero.UnitID;
                //notification.ExpandedText += hero.UnitName + Environment.NewLine;
                NotificationElement notificationElement = new NotificationElement();
                notificationElement.EntityID = hero.UnitID;
                notificationElement.Picture = hero.GetPicture();
                notificationElement.Content = hero.UnitName;
                notification.NotificationElements.Add(notificationElement);
            }
        }
        if (count == 0)
        {
            return true;
        }
        else
        {
          
            ThisPlayer.Notifications.Add(notification);
            RefreshUI();
            return false;
        }
        
    }
    public void MoveCameraToFirstArmy()
    {
        //playerController = GetComponentInParent<PlayerController>();

        //if (playerController == null)
        //{
        //    return;
        //}
        // Moving camera to first army, but overland armies with heroes prefered
        Army firstArmy = GameEngine.ActiveGame.scenario.GetFirstAvalibleArmyOfPlayer(PlayerID, true);

        if (firstArmy != null)
        {
            // Debug.Log("first army selected " + firstArmy.GetInformation() + " " + firstArmy.GetInformationWithUnits() + " playerController playerid " + playerController.ThisPlayer.PlayerID);
            int xPos = firstArmy.Location.WorldMapCoordinates.XCoordinate;
            int yPos = firstArmy.Location.WorldMapCoordinates.YCoordinate;

            Selection.SelectArmy(firstArmy.ArmyID);

            MoveCameraToCoordinates(xPos, yPos);
            RefreshUI();
        }
        else
        {
            if (ThisPlayer.CapitalLocation != null) //null check just in case we ever delete a capital location
            {
                MoveCameraToCoordinates(ThisPlayer.CapitalLocation.XCoordinate, ThisPlayer.CapitalLocation.YCoordinate);
            }

        }

    }

    void RefreshTransactionItemPanel()
    {
        if (pendingItemsPanel.activeSelf)
        {
            PendingItemPanelContorller pendingItem = pendingItemsPanel.GetComponent<PendingItemPanelContorller>();
            pendingItem.DisplayTransactionItem(pendingItem.transactionItem, ThisPlayer.OwnedItems.HasSpaceToTakeItems(pendingItem.transactionItem.ItemsToRecieve,true), pendingItem.notification);
        }
    }
    public void OpenExtraItem(Notification notification)
    {
        PendingItemPanelContorller pendingItem = pendingItemsPanel.GetComponent<PendingItemPanelContorller>();
 
        Player player = GameEngine.ActiveGame.scenario.FindPlayerByID(notification.PlayerID);
  
        ExtraItem extraItem = player.FindExtraItemByID(notification.TargetID);
  
        pendingItem.DisplayTransactionItem(extraItem, ThisPlayer.OwnedItems.HasSpaceToTakeItems(extraItem.ItemsToRecieve,true), notification);
    }
    public void OpenPendingItem(Notification notification)
    {
        PendingItemPanelContorller pendingItem = pendingItemsPanel.GetComponent<PendingItemPanelContorller>();
        TransactionItem transactionItem = GameEngine.ActiveGame.scenario.FindTransactionItemByID(notification.TargetID, notification.TargetID2);
        pendingItem.DisplayTransactionItem(transactionItem,ThisPlayer.OwnedItems.HasSpaceToTakeItems(transactionItem.ItemsToRecieve,true), notification);
    }
    public void OpenIncomingTradeOffer(Notification notification)
    {
        MerchantGuild guild = GameEngine.ActiveGame.scenario.Guilds.FindGuildByID(notification.TargetID);
        ShopItem shopItem = guild.FindShopItemByID(notification.TargetID2);
        Bid bid = null;
        lock (shopItem.Bids)
        {
            bid = guild.FindBidByPlayer(notification.TargetID2, notification.PlayerID);
        }
    

        NotificationElement yourItem = new NotificationElement();
        yourItem.Picture = shopItem.GetPicture;
        yourItem.IsShopItem = true;
        yourItem.Source = new SourceInfo(SourceInfo.MODE_SHOP_TRADE_ITEM);
        yourItem.Source.GuildID = guild.ID;
        yourItem.Source.ShopItemID = shopItem.ID;
        yourItem.Content = shopItem.Item.TemplateKeyword + " x" + shopItem.StackQuantity;

        List<NotificationElement> notifcationElements = new List<NotificationElement>();
        foreach (Item item in bid.BidItems)
        {
            NotificationElement element = new NotificationElement();
            element.Picture = item.GetPictureString();
            element.ItemID = item.ID;
            element.Content = item.TemplateKeyword + " x" + item.Quantity;
            element.Source = new SourceInfo(SourceInfo.MODE_TRADE_STASH);
            element.Source.ShopItemID = shopItem.ID;
            element.Source.Quantity = 1;
            element.Source.PlayerID = bid.PlayerID;
            element.Source.GuildID = guild.ID;
            notifcationElements.Add(element);
            //notification.ExpandedText += "\n" + item.TemplateKeyword + " x" + item.Quantity;
        }

        IncomingTradeOfferPanelController incomingTradeOfferPanelController = incomingTradeOfferWindowPanel.GetComponent<IncomingTradeOfferPanelController>();
        incomingTradeOfferPanelController.DisplayOffer(shopItem, bid, notification.TargetID,yourItem, notifcationElements);
    }
    
    public void ShowReplayPlayback(List<OverlandReplay> overlandReplays)
    {
       

        StartCoroutine(Playback(overlandReplays));
    }

    private IEnumerator Playback(List<OverlandReplay> overlandReplays)
    {
        float waitTimeBase = 1f;
        playbackSpeedModifier = 1f; //resetting the speed modifier, TODO: should i make an option not to reset the playback speed?
        float waitTimeTotal = waitTimeBase * playbackSpeedModifier;
        cancelPlayback = false;
        isInReplay = true;
        overlandReplayController.stopPlayback.gameObject.SetActive(true);
        overlandReplayController.fasterPlayback.gameObject.SetActive(true);
        overlandReplayController.slowerPlayback.gameObject.SetActive(true);
        for (int t = 0; t < overlandReplays.Count; t += 1)
        {
            waitTimeTotal = waitTimeBase * playbackSpeedModifier; //have to call as playbackSpeedModifier could have changed
            if (cancelPlayback)
            {
                break;
            }
            yield return new WaitForSeconds(waitTimeTotal);
            ShowReplay(overlandReplays[t]);
            MoveCameraToCoordinates(overlandReplays[t].ArmyCoordinates.XCoordinate, overlandReplays[t].ArmyCoordinates.YCoordinate);
            
        }
        waitTimeTotal = waitTimeBase * playbackSpeedModifier;//have to call as playbackSpeedModifier could have changed
        if (!cancelPlayback)
        {
            yield return new WaitForSeconds(waitTimeTotal);
        }
        isInReplay = false;
        overlandReplayController.stopPlayback.gameObject.SetActive(false);
        overlandReplayController.fasterPlayback.gameObject.SetActive(false);
        overlandReplayController.slowerPlayback.gameObject.SetActive(false);
        overlandReplayController.LastReplay();
    }

    public void ExpandNotification(Notification notification,NotificartionController notificationObj)
    {
        expandedNotificationPanel.SetActive(true);
        ExpandedNotificationPanelController expandedNotificationPanelController = expandedNotificationPanel.GetComponent<ExpandedNotificationPanelController>();
        expandedNotificationPanelController.notification = notification;
        expandedNotificationPanelController.expandedText.text = notification.ExpandedText;

        foreach (GameObject obj in expandedNotificationPanelController.gameObjects)
        {
            Destroy(obj);
        }
        if (notification.NotificationElements.Count == 0)
        {
            expandedNotificationPanelController.elementsPanelGlobal.SetActive(false);
        }
        else
        {
            expandedNotificationPanelController.elementsPanelGlobal.SetActive(true);
        }
 
        expandedNotificationPanelController.gameObjects.Clear();
        foreach (NotificationElement element in notification.NotificationElements)
        {
            GameObject gameObj = Instantiate(expandedNotificationPanelController.notificationElementPrefab, expandedNotificationPanelController.elementsPanel.transform, false);
            expandedNotificationPanelController.gameObjects.Add(gameObj);
            TooltipTrigger elementToolTip = gameObj.GetComponent<TooltipTrigger>();
            elementToolTip.content = element.AdditionalToolTipContent;
            if (element.ItemKeyword != "")
            {
                ItemTemplate itemTemplate = GameEngine.Data.ItemTemplateCollection.findByKeyword(element.ItemKeyword);
                elementToolTip.TooltipInfo = itemTemplate;
            }
            if (element.EntityKeyword != "")
            {
                CharacterTemplate characterTemplate = GameEngine.Data.CharacterTemplateCollection.findByKeyword(element.EntityKeyword);
                elementToolTip.TooltipInfo = characterTemplate;
            }
            if (element.BuildingKeyword != "")
            {
                BuildingTemplate buildingTemplate = GameEngine.Data.BuildingTemplateCollection.findByKeyword(element.BuildingKeyword);
                elementToolTip.TooltipInfo = buildingTemplate;
            }
            if (element.SkillTreeKeyword != "")
            {
                SkillTree skillTree = GameEngine.Data.SkillTreeCollection.findByKeyword(element.SkillTreeKeyword);
                elementToolTip.TooltipInfo = skillTree;
            }
            if (element.EntityID != -1)
            {
                Entity entity = GameEngine.ActiveGame.scenario.FindUnitByUnitID(element.EntityID);
                elementToolTip.TooltipInfo = entity;
            }
            if (element.BuildingID != -1)
            {
                Building building = GameEngine.ActiveGame.scenario.FindBuildingByID(element.BuildingID);
                elementToolTip.TooltipInfo = building;
            }
            if (element.ItemID != -1)
            {
                Item item = element.Source.GetItem(element.ItemID);
                elementToolTip.TooltipInfo = item;
            }


           
            

            NotificationElementController elementController = gameObj.GetComponent<NotificationElementController>();
            elementController.playerController = this;
            elementController.clickable = element.IsClickable;
            elementController.xCord = element.XCord;
            elementController.yCord = element.YCord;
            if (element.Picture == "")
            {
                elementController.img.gameObject.SetActive(false);
            }
            else
            {
                elementController.img.sprite = Resources.Load<Sprite>(element.Picture);
            }

            if (element.BgImageSprite != "")
            {
                elementController.panelBackgroundImage.sprite = Resources.Load<Sprite>(element.BgImageSprite);
            }
            elementController.panelBackgroundImage.color = new Color32(element.BgImageR, element.BgImageG, element.BgImageB, element.BgImageA);
            elementController.text.text = element.Content;
            elementController.text.color = new Color32(element.TextColorR,element.TextColorG,element.TextColorB,element.TextColorA);
         
        }
        TooltipTrigger tooltipTrigger = null;
        //if (notification.UseDismissandConfirmButtonAsDefault)
        //{
        //    expandedNotificationPanelController.dismissButton.onClick.AddListener(expandedNotificationPanelController.DismissNotification);
        //    expandedNotificationPanelController.okButton.onClick.AddListener(expandedNotificationPanelController.OkButtonClick);

        //    expandedNotificationPanelController.okButton.GetComponentInChildren<Text>().text = "Ok";
        //    expandedNotificationPanelController.dismissButton.GetComponentInChildren<Text>().text = "Dismiss";

        //    tooltipTrigger = expandedNotificationPanelController.okButton.GetComponent<TooltipTrigger>();
        //    tooltipTrigger.content = "Close this window";

        //    tooltipTrigger = expandedNotificationPanelController.dismissButton.GetComponent<TooltipTrigger>();
        //    tooltipTrigger.content = "Remove the notification";
        //}
        switch (notification.Type)
        {
            case Notification.NotificationType.TYPE_ENEMY_ARMY_MOVEMENT_DETECTED:
                expandedNotificationPanelController.gameObject.SetActive(false);
                List<OverlandReplay> armyReplays = ThisPlayer.MapMemory.GetReplayOfArmy(notification.TargetID);
                ShowReplayPlayback(armyReplays);
                Player movingPlayer = GameEngine.ActiveGame.scenario.FindPlayerByID(notification.PlayerID);
                notificationObj.image.color = new Color32(movingPlayer.DarkColor1,movingPlayer.DarkColor2,movingPlayer.DarkColor3,notification.BgImageA);
                notification.BgImageR = movingPlayer.DarkColor1;
                notification.BgImageG = movingPlayer.DarkColor2;
                notification.BgImageB = movingPlayer.DarkColor3;
                notification.HeaderText = notification.HeaderText.Split('(')[0]; //removing the (unseen) part
                //notificationObj.topImage.sprite = Resources.Load<Tile>(notification.Picture).sprite;
                break;
            case Notification.NotificationType.TYPE_TRADE_OFFER:
                Debug.LogError("do not use this one here");
                //expandedNotificationPanelController.dismissButton.onClick.AddListener(expandedNotificationPanelController.DismissNotification);
        
                //expandedNotificationPanelController.okButton.onClick.AddListener(expandedNotificationPanelController.DismissNotification);
                //MerchantGuild guild = GameEngine.ActiveGame.scenario.Guilds.FindGuildByID(notification.TargetID);
                //ShopItem shopItem = guild.FindShopItemByID(notification.TargetID2);
                //Bid bid = guild.FindBidByPlayer(notification.TargetID2,notification.PlayerID);
                //expandedNotificationPanelController.okButton.onClick.AddListener(delegate { GameEngine.ActiveGame.scenario.AcceptTradeOffer(shopItem,bid); });
                //expandedNotificationPanelController.okButton.onClick.AddListener(delegate { GameEngine.ActiveGame.scenario.RemoveSameItemNotifications(notification.TargetID2,notification.TargetID,ThisPlayer.PlayerID); });

                //expandedNotificationPanelController.okButton.GetComponentInChildren<Text>().text = "Accept";

                //tooltipTrigger = expandedNotificationPanelController.okButton.GetComponent<TooltipTrigger>();
                //tooltipTrigger.content = "Accept the trade offer";

                //expandedNotificationPanelController.dismissButton.GetComponentInChildren<Text>().text = "Decline";
                //tooltipTrigger = expandedNotificationPanelController.dismissButton.GetComponent<TooltipTrigger>();
                //tooltipTrigger.content = "Decline the trade offer";

                break;
            case Notification.NotificationType.TYPE_LOOT:
                if (notification.IsOverland)
                {
                    GameSquare gamesqr = GameEngine.ActiveGame.scenario.Worldmap.FindGameSquareByID(notification.TargetID);
                    MoveCameraToCoordinates(gamesqr.X_cord, gamesqr.Y_cord);
                }
                break;
            case Notification.NotificationType.TYPE_ENTITY_STARVE:
                if (notification.IsOverland)
                {
                    GameSquare gamesqr = GameEngine.ActiveGame.scenario.Worldmap.FindGameSquareByID(notification.TargetID);
                    MoveCameraToCoordinates(gamesqr.X_cord, gamesqr.Y_cord);
                }
                break;
            case Notification.NotificationType.TYPE_WARINING_QUEST_PARTY_CANCELLED:
                if (notification.IsOverland)
                {
                    GameSquare gamesqr = GameEngine.ActiveGame.scenario.Worldmap.FindGameSquareByID(notification.TargetID);
                    MoveCameraToCoordinates(gamesqr.X_cord, gamesqr.Y_cord);
                }
                break;
            case Notification.NotificationType.TYPE_HERO_LEAVE:
                if (notification.IsOverland)
                {
                    GameSquare gamesqr = GameEngine.ActiveGame.scenario.Worldmap.FindGameSquareByID(notification.TargetID);
                    MoveCameraToCoordinates(gamesqr.X_cord, gamesqr.Y_cord);
                }
                break;
            case Notification.NotificationType.TYPE_ARMY_LEADER_DIED_AFTER_BATTLE:
                if (notification.IsOverland)
                {

                }
                break;


            case Notification.NotificationType.TYPE_HERO_INACTIVE:
                if (notification.IsOverland)
                {
                    Army overLandArmy = GameEngine.ActiveGame.scenario.FindOverlandArmyByUnit(notification.TargetID);
                    MoveCameraToCoordinates(overLandArmy.Location.WorldMapCoordinates.XCoordinate, overLandArmy.Location.WorldMapCoordinates.YCoordinate);
                }
                break;

            case Notification.NotificationType.TYPE_BUILDING_PRODUCTION_RELATION_CHANGES:
                //if (notification.MapCoordinates != null)
                //{
                //    MoveCameraToCoordinates(notification.MapCoordinates.XCoordinate, notification.MapCoordinates.YCoordinate);
                //}
                break;
            case Notification.NotificationType.TYPE_BUILDING_RAZE_RELATION_CHANGES:
                if (notification.MapCoordinates != null)
                {
                    MoveCameraToCoordinates(notification.MapCoordinates.XCoordinate, notification.MapCoordinates.YCoordinate);
                }
                break;
            case Notification.NotificationType.TYPE_BUILDING_UNIT_COMPLETE:
                    if (notification.IsOverland)
                    {
                        GameSquare gamesqr = GameEngine.ActiveGame.scenario.Worldmap.FindGameSquareByBuildingID(notification.TargetID);
                        MoveCameraToCoordinates(gamesqr.X_cord, gamesqr.Y_cord);
                    }
                break;
            case Notification.NotificationType.TYPE_BUILDING_COMPLETE:
                if (notification.IsOverland)
                {
                    GameSquare gamesqr = GameEngine.ActiveGame.scenario.Worldmap.FindGameSquareByBuildingID(notification.TargetID);
                    MoveCameraToCoordinates(gamesqr.X_cord, gamesqr.Y_cord);
                }
                break;
            case Notification.NotificationType.TYPE_BUILDING_REPAIR_COMPLETE:
              
                if (notification.IsOverland)
                {
                    GameSquare gamesqr = GameEngine.ActiveGame.scenario.Worldmap.FindGameSquareByBuildingID(notification.TargetID);
                    MoveCameraToCoordinates(gamesqr.X_cord, gamesqr.Y_cord);
                }
                break;
            case Notification.NotificationType.TYPE_BUILDING_UPGRADE_COMPLETE:
               
                if (notification.IsOverland)
                {
                    GameSquare gamesqr = GameEngine.ActiveGame.scenario.Worldmap.FindGameSquareByBuildingID(notification.TargetID);
                    MoveCameraToCoordinates(gamesqr.X_cord, gamesqr.Y_cord);
                }
                break;



            case Notification.NotificationType.TYPE_HERO_MISSION_RAZE_COMPLETE:
                if (notification.Picture != "")
                {
                    notificationObj.topImage.sprite = Resources.Load<Tile>(notification.Picture).sprite;
                }
                if (notification.IsOverland)
                {
                    Army overLandArmy = GameEngine.ActiveGame.scenario.FindOverlandArmyByUnit(notification.TargetID);
                    MoveCameraToCoordinates(overLandArmy.Location.WorldMapCoordinates.XCoordinate, overLandArmy.Location.WorldMapCoordinates.YCoordinate);
                }
                break;
            case Notification.NotificationType.TYPE_HERO_MISSION_CRAFTING_COMPLETE:
                if (notification.IsOverland)
                {
                    Army overLandArmy = GameEngine.ActiveGame.scenario.FindOverlandArmyByUnit(notification.TargetID);
                    MoveCameraToCoordinates(overLandArmy.Location.WorldMapCoordinates.XCoordinate, overLandArmy.Location.WorldMapCoordinates.YCoordinate);
                }
                break;
            case Notification.NotificationType.TYPE_HERO_MISSION_SUMMON_COMPLETE:
                if (notification.IsOverland)
                {
                    Army overLandArmy = GameEngine.ActiveGame.scenario.FindOverlandArmyByUnit(notification.TargetID);
                    MoveCameraToCoordinates(overLandArmy.Location.WorldMapCoordinates.XCoordinate, overLandArmy.Location.WorldMapCoordinates.YCoordinate);
                }
                
                break;
            case Notification.NotificationType.TYPE_HERO_MISSION_SURVEY_COMPLETE:
                if (notification.IsOverland)
                {
                    Army overLandArmy = GameEngine.ActiveGame.scenario.FindOverlandArmyByUnit(notification.TargetID);
                    MoveCameraToCoordinates(overLandArmy.Location.WorldMapCoordinates.XCoordinate, overLandArmy.Location.WorldMapCoordinates.YCoordinate);
                }

                break;
            case Notification.NotificationType.TYPE_HERO_MISSION_CAPTURE_COMPLETE:
              
               
                if (notification.IsOverland)
                {
                    Army overLandArmy = GameEngine.ActiveGame.scenario.FindOverlandArmyByUnit(notification.TargetID);
                    MoveCameraToCoordinates(overLandArmy.Location.WorldMapCoordinates.XCoordinate, overLandArmy.Location.WorldMapCoordinates.YCoordinate);
                }

                break;

            case Notification.NotificationType.TYPE_WARINING_BUILDING_IS_BEING_CAPTURED:
            
                if (notification.IsOverland)
                {
                    MapSquare square = GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareByID(notification.TargetID);
                    MoveCameraToCoordinates(square.X_cord, square.Y_cord);
                }
                break;

            case Notification.NotificationType.TYPE_WARINING_BUILDING_LOST:
            
                if (notification.IsOverland)
                {
                    MapSquare square = GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareByID(notification.TargetID);
                    MoveCameraToCoordinates(square.X_cord, square.Y_cord);
                }
                break;
            default:
                break;
        }
    }
    public void ShowNotifications()
    {
        foreach (GameObject gameobj in createdNotifications)
        {
            Destroy(gameobj);
        }
        createdNotifications.Clear();
        foreach (Notification notification in ThisPlayer.Notifications)
        {
            if (notification.IsDismissed)
            {
                continue;
            }
            GameObject newNotificationObject = Instantiate(notificationPrefab, notificationPanel.transform, false);

            TooltipTrigger tooltipTrigger = newNotificationObject.GetComponent<TooltipTrigger>();
            tooltipTrigger.content = "Click to view the notification";
            if (notification.DismissOnRightClick)
            {
                tooltipTrigger.content += Environment.NewLine + "Right click to remove notification";
            }

            NotificartionController notificartionController = newNotificationObject.GetComponent<NotificartionController>();
            notificartionController.playerController = this;
            createdNotifications.Add(newNotificationObject);
            notificartionController.image.color = new Color32(notification.BgImageR,notification.BgImageG,notification.BgImageB,notification.BgImageA);

            if (notification.Picture != "")
            {
                notificartionController.topImage.gameObject.SetActive(true);
                Sprite sprite = Resources.Load<Sprite>(notification.Picture);
                if (sprite != null)
                {
                    notificartionController.topImage.sprite = sprite;
                }
                else
                {
                    Tile tile = Resources.Load<Tile>(notification.Picture);
                    notificartionController.topImage.sprite = tile.sprite;
                }
                
            }

            notificartionController.text.text = notification.HeaderText;
            notificartionController.notification = notification;
            //notificartionController.
        }
    }

    public void RefreshShopButton()
    {
        if (ThisPlayer.Shops.Count < 1)
        {
            ShopButton.interactable = false;
        }
        else
        {
            ShopButton.interactable = true;
        }
        if (!GameEngine.ActiveGame.scenario.PlayerHasCapital(ThisPlayer)) //if lost capital, cant access the shop
        {
            ShopButton.interactable = false;
        }
    }

    public void ShowSelection()
    {
 
        Tile bgTile = null;
        Tile flag1Tile = null;
        Tile flag2Tile = null;
        Tile flag3Tile = null;
        Tile flagBGTile = null;
        Tile flagPolesTile = null;
        if (Selection.SelectedFriendlyOverlandArmy != null)
        {
            MemoryTile memoryTile = GameEngine.ActiveGame.scenario.FindMemoryTileByCoordinates(thisPlayer.PlayerID, Selection.SelectedGameSquare.X_cord, Selection.SelectedGameSquare.Y_cord);
            Selection.AddSelectedGameSquare (GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareByCordinates(Selection.SelectedFriendlyOverlandArmy.Location.WorldMapCoordinates.XCoordinate, Selection.SelectedFriendlyOverlandArmy.Location.WorldMapCoordinates.YCoordinate));
            if (blink)
            {
                bgTile = Resources.Load<Tile>(memoryTile.ArmyBackgroundGraphics);
                if (bgTile != null)
                {
                    bgTile.color = new Color32(memoryTile.ArmyBackgroundColor1, memoryTile.ArmyBackgroundColor2, memoryTile.ArmyBackgroundColor3, memoryTile.ArmyBackgroundColorA);
                }
                
                //bgTile = Resources.Load<Tile>(MapPointedHex.playerBgDefault + Selection.SelectedFriendlyOverlandArmy.OwnerPlayerID + "_bg");
                Tile armytile = Resources.Load<Tile>(Selection.SelectedFriendlyOverlandArmy.GetArmyPicture());
                heroBackgroundTileMap.SetTile(new Vector3Int(Selection.SelectedGameSquare.X_cord, Selection.SelectedGameSquare.Y_cord, 0), bgTile);
                armyTileMap.SetTile(new Vector3Int(Selection.SelectedGameSquare.X_cord, Selection.SelectedGameSquare.Y_cord, 0), armytile);
                flag1TileMap.SetTile(new Vector3Int(Selection.SelectedGameSquare.X_cord, Selection.SelectedGameSquare.Y_cord, 0), flag1Tile);
                flag2TileMap.SetTile(new Vector3Int(Selection.SelectedGameSquare.X_cord, Selection.SelectedGameSquare.Y_cord, 0), flag2Tile);
                flag3TileMap.SetTile(new Vector3Int(Selection.SelectedGameSquare.X_cord, Selection.SelectedGameSquare.Y_cord, 0), flag3Tile);
                flagHolderTileMap.SetTile(new Vector3Int(Selection.SelectedGameSquare.X_cord, Selection.SelectedGameSquare.Y_cord, 0), flagBGTile);
                flagPoleTileMap.SetTile(new Vector3Int(Selection.SelectedGameSquare.X_cord, Selection.SelectedGameSquare.Y_cord, 0), flagPolesTile);
            }
            else
            {
                
                if (memoryTile != null)
                {
                    if (memoryTile.FlagPoleTileGraphics != "")
                    {
                        flagPolesTile = Resources.Load<Tile>(memoryTile.FlagPoleTileGraphics);
                        flagPoleTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), flagPolesTile);

                        flag1Tile = Resources.Load<Tile>(MapPointedHex.Flag1);
                        flag1Tile.color = new Color32(memoryTile.Flag1Color1, memoryTile.Flag1Color2, memoryTile.Flag1Color3, memoryTile.Flag1ColorA);
                        flag1TileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), flag1Tile);


                        flag2Tile = Resources.Load<Tile>(MapPointedHex.Flag2);
                        flag2Tile.color = new Color32(memoryTile.Flag2Color1, memoryTile.Flag2Color2, memoryTile.Flag2Color3, memoryTile.Flag2ColorA);
                        flag2TileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), flag2Tile);


                        flag3Tile = Resources.Load<Tile>(MapPointedHex.Flag3);
                        flag3Tile.color = new Color32(memoryTile.Flag3Color1, memoryTile.Flag3Color2, memoryTile.Flag3Color3, memoryTile.Flag3ColorA);
                        flag3TileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), flag3Tile);


                        
                    }
                    //if (memoryTile.Flag2TileGraphics != "")
                    //{
                    //    flag2Tile = Resources.Load<Tile>(memoryTile.Flag2TileGraphics);
                    //}
                    //if (memoryTile.Flag3TileGraphics != "")
                    //{
                    //    flag3Tile = Resources.Load<Tile>(memoryTile.Flag3TileGraphics);
                    //}
                    if (memoryTile.FlagHolderTileGraphics != "")
                    {
                        flagBGTile = Resources.Load<Tile>(memoryTile.FlagHolderTileGraphics);
                    }
                    //if (memoryTile.ArmyBackgroundGraphics != "")
                    //{
                    //    flagBGTile = Resources.Load<Tile>(memoryTile.ArmyBackgroundGraphics);
                    //    flagBGTile.color = new Color32(memoryTile.ArmyBackgroundColor1, memoryTile.ArmyBackgroundColor2, memoryTile.ArmyBackgroundColor3, memoryTile.ArmyBackgroundColorA);
                    //}
                }

                GameSquare gameSquaretoShow = Selection.SelectedGameSquare;
 
                heroBackgroundTileMap.SetTile(new Vector3Int(gameSquaretoShow.X_cord, gameSquaretoShow.Y_cord, 0), null);
                armyTileMap.SetTile(new Vector3Int(gameSquaretoShow.X_cord, gameSquaretoShow.Y_cord, 0), null);
                flag1TileMap.SetTile(new Vector3Int(gameSquaretoShow.X_cord, gameSquaretoShow.Y_cord, 0), flag1Tile);
                flag2TileMap.SetTile(new Vector3Int(gameSquaretoShow.X_cord, gameSquaretoShow.Y_cord, 0), flag2Tile);
                flag3TileMap.SetTile(new Vector3Int(gameSquaretoShow.X_cord, gameSquaretoShow.Y_cord, 0), flag3Tile);
                flagHolderTileMap.SetTile(new Vector3Int(gameSquaretoShow.X_cord, gameSquaretoShow.Y_cord, 0), flagBGTile);
                flagPoleTileMap.SetTile(new Vector3Int(gameSquaretoShow.X_cord, gameSquaretoShow.Y_cord, 0), flagPolesTile);
            }
        }
        //path != null
       

    }
    /// <summary>
    /// DO NOT include this function into general refreshUI, calling this from global UIs(equipment, inventory) for proper refresh
    /// </summary>
    public void RefreshCombatMapGenerator()
    {
        if (ThisPlayer.GameState.Keyword == GameState.State.BATTLE_PHASE)
        {
            combatMapGeneratorPanel.gameObject.GetComponent<CombatMapGenerator>().RefreshUI();
        }
    }
    /// <summary>
    /// convient way of dealing with combatmapgenerator panel by using refresh & game state
    /// </summary>
    public void CheckCombatMapGenerator()
    {
        if (ThisPlayer.GameState.Keyword != GameState.State.BATTLE_PHASE)
        {
            //Debug.Log("CheckCombatMapGenerator turning off combatmapgenerator for player " + ThisPlayer.PlayerID + " current phase is " +ThisPlayer.GameState.Keyword);
            combatMapGeneratorPanel.gameObject.SetActive(false); 
        }
    }

    public CombatMapGenerator GetCombatMapGenerator()
    {
        return combatMapGeneratorPanel.gameObject.GetComponent<CombatMapGenerator>();
    }
    public void UpdateSelectionGraphics()
    {
        if (Selection.SelectedGameSquare != null)
        {
            SetSquareSelectionAnimation(Selection.SelectedGameSquare.X_cord, Selection.SelectedGameSquare.Y_cord);
        }
    }

    public void RemoveOverlandSelectionGraphic()
    {
        if (Selection.SelectedGameSquare != null)
        {

            selectionTileMap.SetTile(new Vector3Int(Selection.SelectedGameSquare.X_cord, Selection.SelectedGameSquare.Y_cord, 0), null);
            selectionAnimatedTileMap.SetTile(new Vector3Int(Selection.SelectedGameSquare.X_cord, Selection.SelectedGameSquare.Y_cord, 0), null);

        }
        if (GameEngine.ActiveGame.scenario.Worldmap == null)
        {
            return;
        }
        foreach (MapSquare mapsquare in GameEngine.ActiveGame.scenario.Worldmap.MapSquares)
        {
            selectionTileMap.SetTile(new Vector3Int(mapsquare.X_cord, mapsquare.Y_cord, 0), null);
            selectionAnimatedTileMap.SetTile(new Vector3Int(mapsquare.X_cord, mapsquare.Y_cord, 0), null);
        }
       
        //if (memoryTilePath != null)
        //{
            
        //    foreach (MemoryTile mapsquare in memoryTilePath)
        //    {
        //        selectionTileMap.SetTile(new Vector3Int(mapsquare.Coord_X, mapsquare.Coord_Y, 0), null);
        //    }
        //}
    }

    public void RefreshExpandedPanel()
    {
        if (expandedArmyPanel.activeSelf)
        {
            expandedArmyPanel.SetActive(false);
            ExpandedArmyPanelController panelController = expandedArmyPanel.GetComponent<ExpandedArmyPanelController>();
            //panelController.DisplayArmy(army);
        }
    }

    public void RefreshHeroEquippedInventory()
    {
        bool debug = false;

        if (HeroInv.activeSelf) {

            if (debug) {
                Debug.Log("RefreshHeroEquippedInventory called and HeroInv is acvtive");
            }
            CharacterInventoryWindowController characterInventoryWindowController = HeroInv.GetComponent<CharacterInventoryWindowController>();
            // We get the previous entity 
            int entityID = characterInventoryWindowController.GetCurrentEntityID();

            if (entityID == -1)
            {
                // if no previous entity, we try to get the selected hero (TODO: selected friendly entity). TODO: Or we might be looking at hostile entity, with no
                // option of moving items and such
                if (Selection.SelectedFriendlyHero != null)
                {
                    if (debug)
                    {
                        Debug.Log("RefreshHeroEquippedInventory refreshing with selectedFriendlyHero");
                    }
                    characterInventoryWindowController.LoadCharacter(Selection.SelectedFriendlyHero, thisPlayer);
                }

            }
            else {
                // Death check? Still legal check? TODO Check if enemy entity and if so, can not move items around
                Entity entity = GameEngine.ActiveGame.scenario.FindUnitByUnitID(entityID);

                if (entity != null) {

                    if (debug)
                    {
                        Debug.Log("RefreshHeroEquippedInventory refreshing with previous entity");
                    }
                    characterInventoryWindowController.LoadCharacter(entity, thisPlayer);
                }
               
            }
      
        }

       
    }

    internal void ToggleBlockMode()
    {
        //no need to check in combat, the checks there are already implemented
        if (ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.HERO_ACTION_TOGGLE_STEALTH_BUTTON).Permission == GameState.Permission.TOGGLE_STEALTH_BUTTON_DISABLED)
        {
            GameEngine.ActiveGame.DisplayMessageToPlayer("Cannot move during " + ThisPlayer.GameState.Keyword + " phase", PlayerID, Color.red);
            Debug.Log("cannot act during phase: " + ThisPlayer.GameState);
            return;
        }
        MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.ToggleBlockMode,Selection.selectedEntityId.ToString(),PlayerID);
        GameEngine.ActiveGame.clientManager.Push(multiplayerMessage);
        GameEngine.ActiveGame.ToggleBlockMode(Selection.selectedEntityId,PlayerID);
      
    }

    internal void OpenOverlandEventChoicePanel()
    {
        Army army = Selection.SelectedFriendlyOverlandArmy;
        if (army != null)
        {
            GameSquare gameSquare = GameEngine.ActiveGame.scenario.Worldmap.FindGameSquareByCoordinates(army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);
            overlandEventSelectionPanel.gameObject.SetActive(true);
            SelectOverlandEventPanelController selectOverlandEventPanelController = overlandEventSelectionPanel.GetComponent<SelectOverlandEventPanelController>();
            selectOverlandEventPanelController.ShowEvents(gameSquare.GetOverlandEventWithColor(PlayerID,false, false),army.Location.WorldMapCoordinates.XCoordinate,army.Location.WorldMapCoordinates.YCoordinate);
        }
  

    }

    void RefreshOverlandEventChoicePanel()
    {
        if (overlandEventSelectionPanel.activeSelf)
        {
            OpenOverlandEventChoicePanel();
        }
    }

    void RefreshOverlandEventChoicePanelIfOnSquare(MapCoordinates mapCoordinates)
    {
        if (overlandEventSelectionPanel.activeSelf)
        {
            Army army = Selection.SelectedFriendlyOverlandArmy;
            if (army != null)
            {
                if (army.Location.WorldMapCoordinates.XCoordinate == mapCoordinates.XCoordinate && army.Location.WorldMapCoordinates.YCoordinate == mapCoordinates.YCoordinate)
                {
                    RefreshUI(); //for sake of action buttons, we do full refresh instead of specific
                }
            }
        }
       
    }

    public void RefreshTurnCounter()
    {
        if (GameEngine.ActiveGame.scenario != null)
        {
            string language = GameEngine.ActiveGame.GetSelectedLanguage();
            this.turncounterText.text = GameEngine.Data.generalUI.findByKeyword("Turn").correctLanguageString()+":";
            this.turncounterTextNumb.text = ""+GameEngine.ActiveGame.scenario.Turncounter;
            //Image colorImage = turnCounterPanel.GetComponent<Image>();

           // colorImage.color = playerLightColor; //use player color
           // allHeroesPanel.GetComponent<Image>().color = playerDarkColor;
            //if (ActivePlayer != null)
            //{
            //    this.turncounterText.text += " current player " + ActivePlayer.PlayerID;
            //    switch (ActivePlayer.PlayerID)
            //    {
            //        default:
            //            break;
            //        case MapPointedHex.Owner_player1:
            //            Image colorImage = turnCounterPanel.GetComponent<Image>();
            //            colorImage.color = Color.magenta;
            //            break;
            //        case MapPointedHex.Owner_player2:
            //            Image colorImage2 = turnCounterPanel.GetComponent<Image>();
            //            colorImage2.color = Color.cyan;

            //            break;
            //    }
            //}
        }


    }











    //public void DipsplayHeroDataOnPanel(Player player)
    //{
    //    players = scenario.Players;
    //    Player currentPlayer = players[0];


    //    HeroInfoPanelController heroInfoPanelController = this.playerheromanagmentpanel.GetComponent<HeroInfoPanelController>();
    //}

    //public void ResetSpecificUI()
    //{
    //    SelectedArmy = null;
    //    armyPanel.SetActive(false);
    //    selectedGameSquare = null;
    //    gameSquareBuildingInfoPanel.SetActive(false);
    //    multipleArmiesPanel.SetActive(false);
    //}
    /*
    public void DisplayGemsAndHeroesOnPanel()
    {

        PlayerHeroManagmentPanelScript playerHeroManagmentPanelScript = this.playerheromanagmentpanel.GetComponent<PlayerHeroManagmentPanelScript>();
        playerHeroManagmentPanelScript.DisplayPlayerData();


    }
    */

        /*
    public void RefreshArmyPanel()
    {
        if (Selection.selectedArmyId > -1)
        {
 
            ArmyPanelController armyPanelController = armyPanel.GetComponent<ArmyPanelController>();
            armyPanel.SetActive(true);
            //Entity armyHero = GameEngine.ActiveGame.scenario.FindUnitByUnitID(SelectedArmy.LeaderID);
            armyPanelController.DisplayArmy(Selection.selectedArmyId);
        }
    }
    */
    public void RefreshBuildingInfoUI()
    {
        if (Selection.SelectedGameSquare != null)
        {
            gameSquirePanel.gameObject.SetActive(true);
            gameSquirePanel.FillInfo(Selection.SelectedGameSquare, thisPlayer);
        }
    
    }
    public void RefreshDungeonPanel()
    {
        if (questProgressPanel.activeSelf && ThisPlayer.DungeonQueue.Count > 0)
        {
            DungeonViewPanelController dungeonViewPanelController = questProgressPanel.GetComponent<DungeonViewPanelController>();
            Quest quest = ThisPlayer.FindQuestByID(ThisPlayer.DungeonQueue[0]);
            if (quest != null)
            {
                QuestParty questParty = quest.FindQuestPartyByPlayerID(PlayerID);

                QuestTemplate questTemplate = GameEngine.Data.QuestTemplateCollection.findByKeyword(quest.TemplateKeyword);

                dungeonViewPanelController.ShowDungeon(questParty.Progress, questParty.Progress, questTemplate.Length, quest.GetPartyPoints(questParty, "movement"),quest.TemplateKeyword, questParty.Army.ArmyID);
            }
            else
            {
                dungeonViewPanelController.speedText.text = "Dungeon finished";
            }

        }
    }
    public void ShowQuest(double oldprog, double newprog, int dungeonLenght, double speed,string questKeyword, int armyID)
    {
        questProgressPanel.SetActive(true);
        DungeonViewPanelController dungeonViewPanelController = questProgressPanel.GetComponent<DungeonViewPanelController>();
        dungeonViewPanelController.ShowDungeon(oldprog,newprog,dungeonLenght,speed,questKeyword,armyID);
    }

    public void RefreshMultipleArmiesPanel()
    {
        /*
        if (selectedGameSquare != null)
        {
            if (SelectedArmy == null && selectedMemoryArmy == null)
            {
                return;
            }
            List<Army> alliedArmies = GameEngine.ActiveGame.scenario.GetAllPlayersArmiesOnCoordinates(thisPlayer.PlayerID, selectedGameSquare.X_cord, selectedGameSquare.Y_cord);
            //List<Army> armiesOnSquare = GameEngine.ActiveGame.scenario.FindAllArmiesByCoordinates(selectedGameSquare.X_cord, selectedGameSquare.Y_cord);
            MemoryTile memoryTile = GameEngine.ActiveGame.scenario.FindMemoryTileByCoordinates(thisPlayer.PlayerID, SelectedGameSquare.X_cord, SelectedGameSquare.Y_cord);

            if ((alliedArmies.Count + memoryTile.MemoryArmies.Count) > 1)
            {
                MultipleArmiesPanelController multipleArmiesPanelController = multipleArmiesPanel.GetComponent<MultipleArmiesPanelController>();
                //TODO
                int id1 = 0;
                int id2 = 0;
                if (SelectedArmy != null)
                {
                    id1 = SelectedArmy.ArmyID;
                }
                else
                {
                    id2 = selectedMemoryArmy.ArmyID;
                }

                foreach (Army item in alliedArmies)
                {
                    //Debug.Log("army on tile: " + item.ArmyID + " " + item.OwnerPlayerID);
                }
                foreach (MemoryArmy item in memoryTile.MemoryArmies)
                {
                    //Debug.Log("memoryArmy on tile: " + item.ArmyID + " " + item.PlayerID);
                }

                multipleArmiesPanelController.DisplayMultipleArmies(alliedArmies, memoryTile.MemoryArmies, id1, id2);
            }
            else
            {
                multipleArmiesPanel.SetActive(false);
            }

        }*/
    }
    public void ClearInstantiatedObjects()
    {
        foreach (GameObject obj in armyInteractObjList)
        {
            Destroy(obj);
        }
        armyInteractObjList.Clear();
    }

    public void MoveCameraToCoordinates(int xcoord, int ycoord)
    {
        Vector3Int cellPosition = new Vector3Int(xcoord, ycoord);
        //playerCamera.transform.position = grid.CellToWorld(cellPosition);
        //playerCamera.transform.position = new Vector3(playerCamera.transform.position.x, playerCamera.transform.position.y, -8);
        Vector2 newPosition = grid.CellToWorld(cellPosition);
       
        cameraStartPosition = playerCamera.transform.position;
        Debug.Log("current pos X " + cameraStartPosition.x + " Y " + cameraStartPosition.y + " Z " + cameraStartPosition.z + " new pos X " + newPosition.x + " Y " + newPosition.y);
        if (newPosition.x == cameraStartPosition.x && newPosition.y == cameraStartPosition.y)
        {
            return; //same pos, not moving
        }
        StartCoroutine(LerpToPosition(1, newPosition, true));
    }

    IEnumerator LerpToPosition(float lerpSpeed, Vector2 newPosition, bool useRelativeSpeed = false)
    {
        if (useRelativeSpeed)
        {
            float totalDistance = Vector2.Distance(cameraStartPosition, newPosition);
            float diff = Vector2.Distance(playerCamera.transform.position, newPosition);
            float multiplier = diff / totalDistance;
            lerpSpeed *= multiplier;
        }

        float t = 0.0f;
        Vector2 startingPos = playerCamera.transform.position;
        while (t < 1.0f)
        {
            t += Time.deltaTime * (Time.timeScale / lerpSpeed);
            Vector2 coords = Vector2.Lerp(startingPos, newPosition, t);
            playerCamera.transform.position = new Vector3(coords.x, coords.y, -8);
            yield return 0;
        }
    }

    public void AssignPanelsToArmy() {
        Army army = Selection.SelectedFriendlyOverlandArmy;
        if (army == null)
        {
            return;
        }
       
        foreach (MemoryTile tile in ThisPlayer.MapMemory)
        {
            if (tile.HiddenMapTile != "") //if cant see anything through tile, then can just skip
            {
                continue;
            }
            List<HostilityTarget> ids = new List<HostilityTarget>();
            foreach (MemoryArmy visible in tile.MemoryArmies)
            {
                ids.Add(new HostilityTarget(BattleParticipant.MODE_ARMY,visible.ArmyID));
            }
            GameSquare square = GameEngine.ActiveGame.scenario.Worldmap.FindGameSquareByCoordinates(tile.Coord_X, tile.Coord_Y);
            if (square == null)
            {
                Debug.LogError("no square on coordinates: " + tile.Coord_X + " " + tile.Coord_Y + " ");
                GameEngine.ActiveGame.scenario.Worldmap.GetInformation();
            }
            string buildingVisibilityStatus = MapMemory.GetBuildingVisibilityStatus(square, tile, ThisPlayer);

            switch (buildingVisibilityStatus)
            {
                case Player.SEES_ENEMY_BUILDING:
                    
                    BuildingTemplate template = GameEngine.Data.BuildingTemplateCollection.findByKeyword(square.building.TemplateKeyword);
                    if (template.Types.Contains(BuildingTemplate.TYPE_CAPTURABLE))
                    {
 
                        ids.Add(new HostilityTarget(BattleParticipant.MODE_BUILDING,square.building.ID));
                    }
                    break;
                default:
                    break;
            }

            //if (tile.Garrison != null || tile.KeywordsRequiredForPassing.Count > 0)
            //{

            //}

            //if (ids.Count ==0)
            //{
            //    return;
            //}

            if (ids.Count == 1 && ThisPlayer.GameState.Keyword != GameState.State.BATTLE_PHASE)
            {
                GameObject armyInteractButton = Instantiate(armyInteractOverlandButton, onWorldCanvas.transform, false);
                ArmyInteractionController armyInteractControll = armyInteractButton.GetComponent<ArmyInteractionController>();
                armyInteractButton.GetComponent<Button>().onClick.AddListener(delegate { AttackOneArmyBtnClick(ids[0]); });
                //int attackerid = Selection.SelectedFriendlyOverlandArmy.ArmyID;
                //Army army = Selection.SelectedFriendlyOverlandArmy; //optimizing...
                //Army army = GameEngine.ActiveGame.scenario.FindArmyByID(attackerid);
                // armyInteractControll.image.sprite = Resources.Load<Sprite>("ButtonIcons/Sword_Icon");
              
                bool changeIcon = false;

                foreach (HostilityTarget target in ids)
                {
                    if (army.IsInHostileList(target.ID, target.Mode))
                    {
                        changeIcon = true;
                        break;
                    }
                }

                if (changeIcon)
                {
                    armyInteractControll.image.sprite = Resources.Load<Sprite>("ButtonIcons/Swords_Crossed_Icon");
                }

                TooltipTrigger tooltipTrigger = armyInteractButton.GetComponent<TooltipTrigger>();
                tooltipTrigger.content = "click to interact with armies/buildings on this square";


                armyInteractObjList.Add(armyInteractButton);
                Vector3Int convertCellPositionToWorld = new Vector3Int(tile.Coord_X, tile.Coord_Y, 0);
                Vector3 cellPosition = grid.CellToWorld(convertCellPositionToWorld);
                cellPosition = new Vector3(cellPosition.x, cellPosition.y - grid.cellSize.y / 3,5);
                armyInteractButton.transform.position = cellPosition;


            }
            if (ids.Count > 1 && ThisPlayer.GameState.Keyword != GameState.State.BATTLE_PHASE) //to prevent from buttons appearing in battlefield world canvas
            {

                GameObject armyInteractButton = Instantiate(armyInteractOverlandButton, onWorldCanvas.transform, false);
                ArmyInteractionController armyInteractControll = armyInteractButton.GetComponent<ArmyInteractionController>();
                armyInteractButton.GetComponent<Button>().onClick.AddListener(delegate { AttackBtnClick(ids); });
                //int attackerid = Selection.SelectedFriendlyOverlandArmy.ArmyID;
                //Army army = Selection.SelectedFriendlyOverlandArmy; //optimizing...
                //Army army = GameEngine.ActiveGame.scenario.FindArmyByID(attackerid);
                // armyInteractControll.image.sprite = Resources.Load<Sprite>("ButtonIcons/Sword_Icon");
                armyInteractControll.image.sprite = Resources.Load<Sprite>("ButtonIcons/Swords_Stacked_Icon");
                bool changeIcon = false;

                foreach (HostilityTarget target in ids)
                {
                    if (army.IsInHostileList(target.ID, target.Mode))
                    {
                        changeIcon = true;
                        break;
                    }
                }

                if (changeIcon)
                {
                    armyInteractControll.image.sprite = Resources.Load<Sprite>("ButtonIcons/Swords_Crossed_Icon");
                }

                TooltipTrigger tooltipTrigger = armyInteractButton.GetComponent<TooltipTrigger>();
                tooltipTrigger.content = "click to interact with armies/buildings on this square";


                armyInteractObjList.Add(armyInteractButton);
                Vector3Int convertCellPositionToWorld = new Vector3Int(tile.Coord_X, tile.Coord_Y, 0);
                Vector3 cellPosition = grid.CellToWorld(convertCellPositionToWorld);
                cellPosition = new Vector3(cellPosition.x, cellPosition.y - grid.cellSize.y / 3,5);
                armyInteractButton.transform.position = cellPosition;
            }

            //GameSquare gameSquare = GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareByCordinates(tile.Coord_X, tile.Coord_Y); why?
            //ArmyInteractionController armyInteractControll = armyAttackButton.GetComponent<ArmyInteractionController>();
            //if (ids.Count > 0)
            //{
            // Button attackBtn = Instantiate(armyInteractAttackBtn, armyInteractControll.container.transform, false);
            //uiObjectToDelete.Add(attackBtn.gameObject);
            //attackBtn.onClick.AddListener(delegate { AttackBtnClick(ids); });
            //}
            //armyInteractControll.Refresh();
        }
    }

    //public void RefreshArmyOverlandIcons() {
    //    foreach (GameObject armyInteract in armyInteractObjList)
    //    {
    //        ArmyInteractionController armyInteractControll = armyInteract.GetComponent<ArmyInteractionController>();
    //    }
    //}

    public void AttackBtnClick(List<HostilityTarget> ids)
    {
        if (Selection.SelectedFriendlyOverlandArmy == null)
        {
            return;
        }
        if (isObserver)
        {
            GameEngine.ActiveGame.DisplayMessageToPlayer("Cannot interact", PlayerID, Color.red);
            return;
        }
        armyInteractionPanel.SetActive(true);
        ArmyInteractionPanelController armyInteractionPanelController = armyInteractionPanel.GetComponent<ArmyInteractionPanelController>();
        Debug.Log(Selection.SelectedFriendlyOverlandArmy.ArmyID + " player " + Selection.SelectedFriendlyOverlandArmy.OwnerPlayerID);
        armyInteractionPanelController.attackerid = Selection.SelectedFriendlyOverlandArmy.ArmyID;
        armyInteractionPanelController.ShowArmies(ids);
    }
    public void AttackOneArmyBtnClick(HostilityTarget target)
    {
        if (Selection.SelectedFriendlyOverlandArmy == null)
        {
            return;
        }
        if (isObserver)
        {
            GameEngine.ActiveGame.DisplayMessageToPlayer("Cannot interact", PlayerID, Color.red);
            return;
        }
        int targetArmyID = target.ID;
        string targetType = target.Mode;
        int attackerID = Selection.SelectedFriendlyOverlandArmy.ArmyID;
        Debug.Log("attacker ID: " + attackerID + " puts target ID: " + targetArmyID + " on attack list");

        Army army = GameEngine.ActiveGame.scenario.FindArmyByID(attackerID);

        Army targetArmy = GameEngine.ActiveGame.scenario.FindArmyByID(targetArmyID);

        if (army == null)
        {
            Debug.LogError("attacking army is not on Overland");
        }
        if (targetArmy == null)
        {
            Debug.Log("target army is not on Overland");
        }

        if (!army.IsInHostileList(targetArmyID, targetType))
        {
            army.ArmiesYouIntentAttackIds.Add(new HostilityTarget(targetType, targetArmyID));

            MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.SetArmyToAttack, army.ArmyID.ToString(), targetType + "*" + targetArmyID);
            GameEngine.ActiveGame.clientManager.Push(multiplayerMessage);

            Debug.Log(attackerID + " adds to its target list: " + targetArmyID);
        }
        else
        {
            army.RemoveEnemy(targetArmyID, targetType);

            MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.SetArmyToStopAttacking, army.ArmyID.ToString(), targetType + "*" + targetArmyID);
            GameEngine.ActiveGame.clientManager.Push(multiplayerMessage);

            Debug.Log(attackerID + " removes : " + targetArmyID);
        }
        AssignPanelsToArmy();

    }

    public void SeeVision()
    {
        foreach (GameObject visBtn in VisionButtons)
        {
            Destroy(visBtn);
        }
        VisionButtons.Clear();
        foreach (MemoryTile tile in this.ThisPlayer.MapMemory)
        {
            if (tile.SightOnTile > 0)
            {
                GameSquare gameSquare = GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareByCordinates(tile.Coord_X, tile.Coord_Y);
                GameObject visionObj = Instantiate(visionPanelPrefab, onWorldCanvas.transform, false);
                Vector3Int convertCellPositionToWorld = new Vector3Int(tile.Coord_X, tile.Coord_Y, 0);
                Vector3 cellPosition = grid.CellToWorld(convertCellPositionToWorld);
                cellPosition = new Vector3(cellPosition.x, cellPosition.y - grid.cellSize.y / 4);
                visionObj.transform.position = cellPosition;
          
                VisionButtons.Add(visionObj);
                visionObj.GetComponentInChildren<Text>().text = tile.SightOnTile.ToString();
            }
        }
    }

    public void SeeCoords() {
        foreach (GameObject CoordTag in CoordsTags)
        {
            Destroy(CoordTag);
        }
        CoordsTags.Clear();
        foreach (MemoryTile tile in this.ThisPlayer.MapMemory)
        {
            GameSquare gameSquare = GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareByCordinates(tile.Coord_X, tile.Coord_Y);
            GameObject seeCoordsObj = Instantiate(coordsPanelPrefab, onWorldCanvas.transform, false);
            Vector3Int convertCellPositionToWorld = new Vector3Int(tile.Coord_X, tile.Coord_Y, 0);
            Vector3 cellPosition = grid.CellToWorld(convertCellPositionToWorld);
            cellPosition = new Vector3(cellPosition.x, cellPosition.y);
            seeCoordsObj.transform.position = cellPosition;

            CoordsPrefabController objControl = seeCoordsObj.GetComponent<CoordsPrefabController>();
            objControl.newCoords.text = "X" + gameSquare.Cube_x.ToString() + " Y" + gameSquare.Cube_y.ToString() + " Z" + gameSquare.Cube_z.ToString();
            objControl.oldCoords.text = "X: " + gameSquare.X_cord.ToString() + " Y: " + gameSquare.Y_cord.ToString();
            CoordsTags.Add(seeCoordsObj);
        }
    }
    public void ToggleGarrsionMenu() {
        if (theBuildingGarrsionUI)
        {
            theBuildingGarrsionUI = false;
            buildingGarrisonPanel.SetActive(false);
        }
        else
        {
            theBuildingGarrsionUI = true;
            buildingGarrisonPanel.SetActive(true);
            theBuildingStorageUI = false;
            buildingStoragePanel.SetActive(false);
        }
    }
    public void ToggleStorageMenu() {
        if (theBuildingStorageUI)
        {
            theBuildingStorageUI = false;
            buildingStoragePanel.SetActive(false);
        }
        else
        {
            theBuildingStorageUI = true;
            buildingStoragePanel.SetActive(true);
            theBuildingGarrsionUI = false;
            buildingGarrisonPanel.SetActive(false);
        }
    }

    public void RefreshVision()
    {
        if (visionToggle)
        {
            SeeVision(); //2nd
        }
    }

    public void AlgorithTest() {
        List<MapSquare> sources = new List<MapSquare>();
        List<MapSquare> targets = new List<MapSquare>();


        float x = GameEngine.ActiveGame.scenario.Worldmap.Width / 2 - 1;
        float y = -GameEngine.ActiveGame.scenario.Worldmap.Width / 2 + 1;
        float height = GameEngine.ActiveGame.scenario.Worldmap.Height;
        int z = 0;
        //right = x > 0 , y < 0
        //up
        // x+, y+, z-even
        for (int i = 0; i < height; i++)
        {
            MapSquare map = GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareBy_CUBE_coordinates((int)x++, (int)y++, z);
            --z; --z;
            if (map != null)
            {
                sources.Add(map);
            }
        }

        x = GameEngine.ActiveGame.scenario.Worldmap.Width / 2 - 1;
        y = -GameEngine.ActiveGame.scenario.Worldmap.Width / 2 + 1;
        z = 0;
        //down
        // x-, y-, z+even
        for (int i = 0; i < height; i++)
        {
            MapSquare map = GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareBy_CUBE_coordinates((int)x--, (int)y--, z);
            ++z; ++z;
            if (map != null)
            {
                sources.Add(map);
            }
        }


        x = GameEngine.ActiveGame.scenario.Worldmap.Width / 2 - 1;
        y = -GameEngine.ActiveGame.scenario.Worldmap.Width / 2 + 1;
        z = 0;

        //left = x < 0 , y > 0
        //up
        // x+, y+, z-even
        for (int i = 0; i < height; i++)
        {
            MapSquare map = GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareBy_CUBE_coordinates((int)y++, (int)x++, z);
            --z; --z;
            if (map != null)
            {
                targets.Add(map);
            }
        }

        x = GameEngine.ActiveGame.scenario.Worldmap.Width / 2 - 1;
        y = -GameEngine.ActiveGame.scenario.Worldmap.Width / 2 + 1;
        z = 0;

        //down
        // x-, y-, z+even
        for (int i = 0; i < height; i++)
        {
            MapSquare map = GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareBy_CUBE_coordinates((int)y--, (int)x--, z);
            ++z; ++z;
            if (map != null)
            {
                targets.Add(map);
            }
        }

        string terrainPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.TERRAIN_TILE_MAP).Permission;
        string buildingPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.BUILDINGS_TILE_MAP).Permission;
        string fogofwarPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.FOGOFWAR_TILE_MAP).Permission;
        string hiddenMapPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.HIDDEN_MAP_TILE_MAP).Permission;
        string flag1Permission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMIES_FLAG_1_TILE_MAP).Permission;
        string flag2Permission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMIES_FLAG_2_TILE_MAP).Permission;
        string flag3Permission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMIES_FLAG_3_TILE_MAP).Permission;
        string flagHolderPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMY_FLAGHOLDER_TILE_MAP).Permission;
        string overlandItemPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.OVERLAND_ITEM_TILEMAP).Permission;
        string overlandEventPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.OVERLAND_EVENT_TILE_MAP).Permission;
        string armyPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMY_TILE_MAP).Permission;
        string backgroundPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.UNIT_BACKGROUND_TILE_MAP).Permission;
        string combatIndicatorsPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.COMBAT_INDICATORS_TILE_MAP).Permission;
        string playerFlagsPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.PLAYER_FLAGS_TILE_MAP).Permission;
        string buildingFlagPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.BUILDING_FLAG_COLOR_TILE_MAP).Permission;
        string armyFlagPolePermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMY_FLAG_POLE_TILE_MAP).Permission;
       // targets.Reverse();
        for (int i = 0; i < sources.Count; i++)
        {
            MapSquare source = sources[i];
            MapSquare target = targets[i];
            MemoryTile sourceTile = thisPlayer.MapMemory.FindMemoryTileByMapSquareID(source.ID);
            // MemoryTile copyTile = ObjectCopier.Clone<MemoryTile>(sourceTile);
            MemoryTile copyTile = ObjectCopier.Clone(sourceTile);
            copyTile.Coord_X = target.X_cord;
            copyTile.Coord_Y = target.Y_cord;

            DisplayMemoryTile(copyTile, playerFlagsPermission, backgroundPermission, armyPermission, overlandEventPermission, overlandItemPermission, combatIndicatorsPermission, buildingPermission, fogofwarPermission, terrainPermission, hiddenMapPermission, flag1Permission, flagHolderPermission);


        }


     
    }

    public void RefreshResource()
    {
        if (resourceToggle)
        {
            ShowResources();
        }
    }

    public void ToggleResource()
    {
        AlgorithTest();
        if (resourceToggle)
        {
            foreach (GameObject res in ResourceImages)
            {
                Destroy(res);
            }
            ResourceImages.Clear();
            resourceToggle = false;
        }
        else
        {
            ShowResources();
            resourceToggle = true;
        }
    }

    public void ShowResources()
    {
        foreach (GameObject res in ResourceImages)
        {
            Destroy(res);
        }
        ResourceImages.Clear();
        foreach (MemoryTile tile in this.ThisPlayer.MapMemory)
        {
            if (tile.SightOnTile > 0)
            {
                GameSquare gameSquare = GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareByCordinates(tile.Coord_X, tile.Coord_Y);
                GameObject resourceObject = Instantiate(resourcePrefab, onWorldCanvas.transform, false);
                Vector3Int convertCellPositionToWorld = new Vector3Int(tile.Coord_X, tile.Coord_Y, 0);
                Vector3 cellPosition = grid.CellToWorld(convertCellPositionToWorld);
                cellPosition = new Vector3(cellPosition.x, cellPosition.y - grid.cellSize.y / 4);
                ResourceHexagonUIController resController = resourceObject.GetComponent<ResourceHexagonUIController>();
                resourceObject.transform.position = cellPosition;

       

                List<MemoryResource> memoryResources = new List<MemoryResource>();
                memoryResources.AddRange(tile.VisibleResources);
                memoryResources.Sort(delegate (MemoryResource resource1, MemoryResource resource2) { return resource1.CombinedValue.CompareTo(resource2.CombinedValue); });
                memoryResources.Reverse();

                int maxNrofTimes = Math.Min(3,memoryResources.Count);
              

                for (int i = 0; i < maxNrofTimes; i++)
                {
                    MemoryResource memoryResource = memoryResources[i];
                
                    ResourceTemplate resource = GameEngine.Data.ResourceTemplateCollection.findByKeyword(memoryResource.TemplateKeyword);
                    if (Resources.Load<Sprite>(resource.Graphics) == null)
                    {
                        Debug.LogError(" NO Sprite " + resource.Graphics + " in " + memoryResource.TemplateKeyword);
                        continue;
                    }

                    switch (i)
                    {
                        case 0:
                            resController.firstImg.gameObject.SetActive(true);
                            resController.firstImgText.text = memoryResource.Amount.ToString();
                            resController.firstImg.sprite = Resources.Load<Sprite>(resource.Graphics);
                            break;
                        case 1:
                            resController.secondImg.gameObject.SetActive(true);
                            resController.secondImgText.text = memoryResource.Amount.ToString();
                            resController.secondImg.sprite = Resources.Load<Sprite>(resource.Graphics);
                            break;
                        case 2:
                            resController.thirdImg.gameObject.SetActive(true);
                            resController.thirdImgText.text = memoryResource.Amount.ToString();
                            resController.thirdImg.sprite = Resources.Load<Sprite>(resource.Graphics);
                            break;
                        default:
                            break;
                    }

                }

              

            

                ResourceImages.Add(resourceObject);

            }
        }
    }

    public void ToggleVision()
    {
        if (visionToggle)
        {
            visionToggle = false;
            RemoveVision();
        }
        else
        {
            visionToggle = true;
            SeeVision(); //2nd
        }
    }

    public void ToggleCoords()
    {
        if (coordsToggle)
        {
            coordsToggle = false;
            RemoveCoords();
        }
        else
        {
            coordsToggle = true;
            SeeCoords(); //2nd
        }
    }

    public void RemoveCoords()
    {

        foreach (GameObject coordTag in CoordsTags)
        {
            Destroy(coordTag);
        }

        CoordsTags.Clear();
    }

    public void RemoveVision()
    {

        foreach (GameObject visBtn in VisionButtons)
        {
            Destroy(visBtn);
        }

        VisionButtons.Clear();
    }
    /// <summary>
    /// gets permission right here
    /// </summary>
    /// <param name="memoryTile"></param>
    public void AutoDisplayTile(MemoryTile memoryTile)
    {
        string terrainPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.TERRAIN_TILE_MAP).Permission;
        string buildingPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.BUILDINGS_TILE_MAP).Permission;
        string fogofwarPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.FOGOFWAR_TILE_MAP).Permission;
        string hiddenMapPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.HIDDEN_MAP_TILE_MAP).Permission;
        string flag1Permission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMIES_FLAG_1_TILE_MAP).Permission;
        string flag2Permission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMIES_FLAG_2_TILE_MAP).Permission;
        string flag3Permission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMIES_FLAG_3_TILE_MAP).Permission;
        string flagHolderPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMY_FLAGHOLDER_TILE_MAP).Permission;
        string overlandItemPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.OVERLAND_ITEM_TILEMAP).Permission;
        string overlandEventPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.OVERLAND_EVENT_TILE_MAP).Permission;
        string armyPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMY_TILE_MAP).Permission;
        string backgroundPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.UNIT_BACKGROUND_TILE_MAP).Permission;
        string combatIndicatorsPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.COMBAT_INDICATORS_TILE_MAP).Permission;
        string playerFlagsPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.PLAYER_FLAGS_TILE_MAP).Permission;
        string buildingFlagPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.BUILDING_FLAG_COLOR_TILE_MAP).Permission;
        string armyFlagPolePermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMY_FLAG_POLE_TILE_MAP).Permission;
        DisplayMemoryTile(memoryTile, playerFlagsPermission, backgroundPermission, armyPermission, overlandEventPermission, overlandItemPermission, combatIndicatorsPermission, buildingPermission, fogofwarPermission, terrainPermission, hiddenMapPermission, flag1Permission, flagHolderPermission);

    }

    public void DisplayMemoryTile(MemoryTile memoryTile,string playerFlagsPermission, string backgroundPermission,string armyPermission,string overlandEventPermission, string overlandItemPermission, string combatIndicatorsPermission, string buildingPermission, string fogofwarPermission, string terrainPermission, string hiddenMapPermission, string flag1Permission, string flagHolderPermission)
    {
        bool debug = false;
        if (debug)
        {
            OurLog.Print("there is a memory tile X: " + memoryTile.Coord_X + " Y: " + memoryTile.Coord_Y);
        }





        if (memoryTile.HiddenMapTile != "")
        {
            if (hiddenMapPermission == GameState.Permission.HIDDEN_MAP_SHOW)
            {
                Tile hiddenMapTile = Resources.Load<Tile>(memoryTile.HiddenMapTile);

                if (hiddenMapTile == null)
                {
                    Debug.LogError("hiddenMapTile tile is null with graphic name " + memoryTile.HiddenMapTile);
                }

                hiddenMapTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), hiddenMapTile);

                return; //since this is the toppest layer, for sake of optimziation returning
            }

        }
        else
        {
            hiddenMapTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), null);
        }
        if (memoryTile.FogOfWarTile != "")
        {

            if (fogofwarPermission == GameState.Permission.FOGOFWAR_MAP_SHOW)
            {
                Tile fogOfWarTile = Resources.Load<Tile>(memoryTile.FogOfWarTile);

                if (fogOfWarTile == null)
                {
                    Debug.LogError("fogOfWarTile tile is null with graphic name " + memoryTile.FogOfWarTile);
                }

                fogOfWarTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), fogOfWarTile);

                
            }


        }
        else
        {
            fogOfWarTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), null);
        }


        if (memoryTile.GroundTileGraphics != "")
        {
            if (terrainPermission == GameState.Permission.TERRAIN_TILE_MAP_SHOW)
            {
                Tile displayGroundTile = Resources.Load<Tile>(memoryTile.GroundTileGraphics);

                if (displayGroundTile == null)
                {
                    Debug.LogError("displayGroundTile tile is null with graphic name " + memoryTile.BuildingGraphics);
                }


                groundTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), displayGroundTile);
            }


        }
        else
        {
            groundTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), null);
        }

        if (memoryTile.BuildingGraphics != "")
        {
            if (buildingPermission == GameState.Permission.BUILDINGS_TILE_MAP_SHOW)
            {
                Tile buildingTile = Resources.Load<Tile>(memoryTile.BuildingGraphics);

                if (buildingTile == null)
                {
                    Debug.LogError("buildingTile tile is null with graphic name " + memoryTile.BuildingGraphics);
                }


                buildingTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), buildingTile);
            }


        }
        else
        {
            buildingTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), null);
        }


        if (memoryTile.FlagPoleTileGraphics != "")
        {
            if (flag1Permission == GameState.Permission.ARMIES_FLAG_1_MAP_SHOW)
            {
                Tile flagPolesTile = Resources.Load<Tile>(memoryTile.FlagPoleTileGraphics);

                if (flagPolesTile == null)
                {
                    Debug.LogError("flagPolesTile tile is null with graphic name " + memoryTile.FlagPoleTileGraphics);
                }

                flagPoleTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), flagPolesTile);

                Tile flag1Tile = Resources.Load<Tile>(MapPointedHex.Flag1);
                flag1Tile.color = new Color32(memoryTile.Flag1Color1, memoryTile.Flag1Color2, memoryTile.Flag1Color3, memoryTile.Flag1ColorA);
                flag1TileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), flag1Tile);


                Tile flag2Tile = Resources.Load<Tile>(MapPointedHex.Flag2);
                flag2Tile.color = new Color32(memoryTile.Flag2Color1, memoryTile.Flag2Color2, memoryTile.Flag2Color3, memoryTile.Flag2ColorA);
                flag2TileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), flag2Tile);


                Tile flag3Tile = Resources.Load<Tile>(MapPointedHex.Flag3);
                flag3Tile.color = new Color32(memoryTile.Flag3Color1, memoryTile.Flag3Color2, memoryTile.Flag3Color3, memoryTile.Flag3ColorA);
                flag3TileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), flag3Tile);
            }

        }
        else
        {
            flag1TileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), null);
            flag2TileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), null);
            flag3TileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), null);
            flagPoleTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), null);
        }

        if (memoryTile.FlagHolderTileGraphics != "")
        {
            if (flagHolderPermission == GameState.Permission.ARMY_FLAGHOLDER_MAP_SHOW)
            {
                Tile flagHolder = Resources.Load<Tile>(memoryTile.FlagHolderTileGraphics);

                if (flagHolder == null)
                {
                    Debug.LogError("flagHolder tile is null with graphic name " + memoryTile.FlagHolderTileGraphics);
                }


                flagHolderTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), flagHolder);
            }

        }
        else
        {
            flagHolderTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), null);
        }

        if (memoryTile.EventTileForegroundGraphics != "")
        {
            if (overlandEventPermission == GameState.Permission.OVERLAND_EVENTS_SHOW)
            {
                Tile itemTileGraphics = Resources.Load<Tile>(memoryTile.EventTileForegroundGraphics);
              
                if (itemTileGraphics == null)
                {
                    Debug.LogError("EventTileForegroundGraphics tile is null with graphic name " + memoryTile.EventTileForegroundGraphics);
                }

                itemsTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), itemTileGraphics);
            }

        }
        else
        {
            itemsTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), null);
           
        }

        // event background
        if (memoryTile.EventTileBackgroundGraphics != "")
        {
            if (overlandEventPermission == GameState.Permission.OVERLAND_EVENTS_SHOW)
            {
                eventTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), null);
                
                Color wantedColor = new Color32(memoryTile.EventSymbolGraphicsColor1, memoryTile.EventSymbolGraphicsColor2, memoryTile.EventSymbolGraphicsColor3, memoryTile.EventSymbolGraphicsColorA);

                //Tile previousTile = (Tile)eventTileMap.GetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0));
                //if (previousTile.color.ToString() != wantedColor.ToString()) {
                //    eventTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), null);
                //}

                Tile eventTileGraphics = Resources.Load<Tile>(memoryTile.EventTileBackgroundGraphics);
                if (eventTileGraphics == null)
                {
                    Debug.LogError("eventTileGraphics tile is null with graphic name " + memoryTile.EventTileBackgroundGraphics);
                }
                eventTileGraphics.color = wantedColor;
             //   Debug.Log("Event color for SQ" + memoryTile.SquareID + " Graphics:" + memoryTile.EventTileBackgroundGraphics + " Memorytile colors are: R:" + memoryTile.EventSymbolGraphicsColor1 + " G: " + memoryTile.EventSymbolGraphicsColor2 + " B: " + memoryTile.EventSymbolGraphicsColor3);
                eventTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), eventTileGraphics);

            }


        }
        else
        {
            eventTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), null);
        }

        if (memoryTile.ArmiesTileGraphics != "" && memoryTile.ArmiesTileGraphics != null)
        {
            if (armyPermission == GameState.Permission.ARMY_MAP_SHOW)
            {
                //Debug.Log("!!!!!!!!!!!!!!!!!showing army!!!!!!!!!!!!!!!!!!!!!!");
                Tile displayArmyTile = Resources.Load<Tile>(memoryTile.ArmiesTileGraphics);
                if (displayArmyTile == null)
                {
                    Debug.LogError("displayArmyTile tile is null with graphic name " + memoryTile.ArmiesTileGraphics);
                }
                armyTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), displayArmyTile);


            }

            if (backgroundPermission == GameState.Permission.UNIT_BACKGROUND_MAP_SHOW)
            {
                Tile armyBG = Resources.Load<Tile>(memoryTile.ArmyBackgroundGraphics);

                if (armyBG == null)
                {
                    Debug.LogError("armyBG tile is null with graphic name " + memoryTile.ArmyBackgroundGraphics);
                }

                armyBG.color = new Color32(memoryTile.ArmyBackgroundColor1, memoryTile.ArmyBackgroundColor2, memoryTile.ArmyBackgroundColor3, memoryTile.ArmyBackgroundColorA);
                heroBackgroundTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), armyBG);
            }
            //GameObject gameObject = Instantiate(attackButtonPrefab);
            //gameObject.transform.SetParent(playerCanvas.transform);




            //var pos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
            //Vector3Int cell = armyTileMap.WorldToCell(new Vector3(pos.x, pos.y));
            //TileBase tile = armyTileMap.GetTile(cell);
            //Vector3 worldPos = grid.CellToWorld(cell); // go from cell-space to world-space
            //var screenPoint = Camera.main.WorldToScreenPoint(worldPos); // go from world-space to screen-space
            //RectTransform tr = (gameObject.transform as RectTransform);
            //tr.position = screenPoint;


            //Vector3Int vector3int = new Vector3Int();
            //vector3int.x = memoryTile.Coord_X;
            //vector3int.y = memoryTile.Coord_Y;
            //// Debug.Log("vector 3 " + vector3int.x.ToString() + " "+ vector3int.y.ToString());
            //Vector3 vector3 = armyTileMap.CellToWorld(vector3int);

            //gameObject.transform.position = vector3;

        }
        else
        {
            armyTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), null);
            heroBackgroundTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), null);
        }
        if (memoryTile.CombatIndicatorsTileGraphics != "")
        {
          
            if (combatIndicatorsPermission == GameState.Permission.COMBAT_INDICATORS_MAP_SHOW)
            {
               
                Tile displayCombatIndicatorTile = Resources.Load<Tile>(memoryTile.CombatIndicatorsTileGraphics);
                if (displayCombatIndicatorTile == null)
                {
                    Debug.LogError("displayCombatIndicatorTile is null with graphic name " + memoryTile.CombatIndicatorsTileGraphics);
                }
                combatIndicatorTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), displayCombatIndicatorTile);
            }


        }
        else
        {
            combatIndicatorTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), null);
        }
        if (memoryTile.PlayerFlagTileGraphics != "")
        {
            if (playerFlagsPermission == GameState.Permission.PLAYER_FLAGS_MAP_SHOW)
            {
                //Debug.Log("SHOWING FLAG : " + memoryTile.PlayerFlagTileGraphics);
                Tile flagTile = Resources.Load<Tile>(memoryTile.PlayerFlagTileGraphics);
                if (flagTile == null)
                {
                    Debug.LogError("no flag tile found with graphics: " + memoryTile.PlayerFlagTileGraphics);
                }
                flagTile.color = new Color32(memoryTile.Color1, memoryTile.Color2, memoryTile.Color3, 255);
                //Debug.Log("PlayerController X: " + memoryTile.Coord_X + " Y: " + memoryTile.Coord_Y + " RGB: " + memoryTile.Color1 + " " + memoryTile.Color2 + " " + memoryTile.Color3 + " OWNER: " + memoryTile.BuildingPlayerOwnerID);
                playerFlagTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), null);
                playerFlagTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), flagTile);

                Tile flagPoleTile = Resources.Load<Tile>(memoryTile.BuildingFlagPoleGraphics);


                if (flagPoleTile == null)
                {
                    Debug.LogError("flagPoleTile tile is null with graphic name " + memoryTile.BuildingFlagPoleGraphics);
                }

                buildingFlagPoleTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), flagPoleTile);
                //buildingFlagPoleTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), flagPoleTile);
            }

        }
        else
        {
            playerFlagTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), null);
            buildingFlagPoleTileMap.SetTile(new Vector3Int(memoryTile.Coord_X, memoryTile.Coord_Y, 0), null);
        }
    }
    /// <summary>
    /// this method is for replays
    /// not saving permissions in replay, just using current ones
    /// </summary>
    /// <param name="memoryTiles"></param>
    public void DisplayMap(MapMemory memoryTiles)
    {

        string terrainPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.TERRAIN_TILE_MAP).Permission;
        string buildingPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.BUILDINGS_TILE_MAP).Permission;
        string fogofwarPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.FOGOFWAR_TILE_MAP).Permission;
        string hiddenMapPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.HIDDEN_MAP_TILE_MAP).Permission;
        string flag1Permission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMIES_FLAG_1_TILE_MAP).Permission;
        string flag2Permission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMIES_FLAG_2_TILE_MAP).Permission;
        string flag3Permission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMIES_FLAG_3_TILE_MAP).Permission;
        string flagHolderPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMY_FLAGHOLDER_TILE_MAP).Permission;
        string overlandItemPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.OVERLAND_ITEM_TILEMAP).Permission;
        string overlandEventPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.OVERLAND_EVENT_TILE_MAP).Permission;
        string armyPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMY_TILE_MAP).Permission;
        string backgroundPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.UNIT_BACKGROUND_TILE_MAP).Permission;
        string combatIndicatorsPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.COMBAT_INDICATORS_TILE_MAP).Permission;
        string playerFlagsPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.PLAYER_FLAGS_TILE_MAP).Permission;
        string buildingFlagPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.BUILDING_FLAG_COLOR_TILE_MAP).Permission;
        string armyFlagPolePermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMY_FLAG_POLE_TILE_MAP).Permission;
        foreach (MemoryTile memoryTile in memoryTiles)
        {
            DisplayMemoryTile(memoryTile, playerFlagsPermission, backgroundPermission, armyPermission, overlandEventPermission, overlandItemPermission, combatIndicatorsPermission, buildingPermission, fogofwarPermission, terrainPermission, hiddenMapPermission, flag1Permission, flagHolderPermission);
        }
    }

    public void DisplayMap(Player player)
    {
        bool timer = false;
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Reset();
            GameEngine.ActiveGame.GameStopwatch.Start();
        }

        string terrainPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.TERRAIN_TILE_MAP).Permission;
        string buildingPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.BUILDINGS_TILE_MAP).Permission;
        string fogofwarPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.FOGOFWAR_TILE_MAP).Permission;
        string hiddenMapPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.HIDDEN_MAP_TILE_MAP).Permission;
        string flag1Permission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMIES_FLAG_1_TILE_MAP).Permission;
        string flag2Permission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMIES_FLAG_2_TILE_MAP).Permission;
        string flag3Permission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMIES_FLAG_3_TILE_MAP).Permission;
        string flagHolderPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMY_FLAGHOLDER_TILE_MAP).Permission;
        string overlandItemPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.OVERLAND_ITEM_TILEMAP).Permission;
        string overlandEventPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.OVERLAND_EVENT_TILE_MAP).Permission;
        string armyPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMY_TILE_MAP).Permission;
        string backgroundPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.UNIT_BACKGROUND_TILE_MAP).Permission;
        string combatIndicatorsPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.COMBAT_INDICATORS_TILE_MAP).Permission;
        string playerFlagsPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.PLAYER_FLAGS_TILE_MAP).Permission;
        string buildingFlagPermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.BUILDING_FLAG_COLOR_TILE_MAP).Permission;
        string armyFlagPolePermission = ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.ARMY_FLAG_POLE_TILE_MAP).Permission;
        foreach (MemoryTile memoryTile in player.MapMemory)
        {
            DisplayMemoryTile(memoryTile, playerFlagsPermission, backgroundPermission, armyPermission, overlandEventPermission, overlandItemPermission, combatIndicatorsPermission, buildingPermission, fogofwarPermission, terrainPermission, hiddenMapPermission, flag1Permission, flagHolderPermission); 
           

          

        }


        if (armyFlagPolePermission == GameState.Permission.ARMY_FLAG_POLE_SHOW)
        {
            flagHolderTileMap.gameObject.SetActive(true);
        }
        if (armyFlagPolePermission == GameState.Permission.ARMY_FLAG_POLE_HIDE)
        {
            flagHolderTileMap.gameObject.SetActive(false);
        }


        if (buildingFlagPermission == GameState.Permission.BUILDING_FLAG_POLE_SHOW)
        {
            buildingFlagPoleTileMap.gameObject.SetActive(true);
        }
        if (buildingFlagPermission == GameState.Permission.BUILDING_FLAG_POLE_HIDE)
        {
            buildingFlagPoleTileMap.gameObject.SetActive(false);
        }
        if (terrainPermission == GameState.Permission.TERRAIN_TILE_MAP_HIDE)
        {
            groundTileMap.gameObject.SetActive(false);
        }
        if (terrainPermission == GameState.Permission.TERRAIN_TILE_MAP_SHOW)
        {
            groundTileMap.gameObject.SetActive(true);
        }

        if (backgroundPermission == GameState.Permission.UNIT_BACKGROUND_MAP_HIDE)
        {
            heroBackgroundTileMap.gameObject.SetActive(false);
        }
        if (backgroundPermission == GameState.Permission.UNIT_BACKGROUND_MAP_SHOW)
        {
            heroBackgroundTileMap.gameObject.SetActive(true);
        }



        if (player.GameState.GetUIPermissionByObject(GameState.Object.SELECTION_TILE_MAP).Permission == GameState.Permission.SELECTION_MAP_HIDE)
        {
            selectionTileMap.gameObject.SetActive(false);
            selectionAnimatedTileMap.gameObject.SetActive(false);
        }
        if (player.GameState.GetUIPermissionByObject(GameState.Object.SELECTION_TILE_MAP).Permission == GameState.Permission.SELECTION_MAP_SHOW)
        {
            selectionTileMap.gameObject.SetActive(true);
            selectionAnimatedTileMap.gameObject.SetActive(true);
        }

        if (buildingPermission == GameState.Permission.BUILDINGS_TILE_MAP_SHOW)
        {
            buildingTileMap.gameObject.SetActive(true);
        }
        if (buildingPermission == GameState.Permission.BUILDINGS_TILE_MAP_HIDE)
        {
            buildingTileMap.gameObject.SetActive(false);
        }


        if (combatIndicatorsPermission == GameState.Permission.COMBAT_INDICATORS_MAP_HIDE)
        {
            combatIndicatorTileMap.gameObject.SetActive(false);
        }
        if (combatIndicatorsPermission == GameState.Permission.COMBAT_INDICATORS_MAP_SHOW)
        {
            combatIndicatorTileMap.gameObject.SetActive(true);
        }
    




        if (armyPermission == GameState.Permission.ARMY_MAP_HIDE)
        {
            armyTileMap.gameObject.SetActive(false);
        }
        if (armyPermission == GameState.Permission.ARMY_MAP_SHOW)
        {
            armyTileMap.gameObject.SetActive(true);
        }

        if (playerFlagsPermission == GameState.Permission.PLAYER_FLAGS_MAP_HIDE)
        {
            playerFlagTileMap.gameObject.SetActive(false);
        }
        if (playerFlagsPermission == GameState.Permission.PLAYER_FLAGS_MAP_SHOW)
        {
            playerFlagTileMap.gameObject.SetActive(true);
        }

        if (flag1Permission == GameState.Permission.ARMIES_FLAG_1_MAP_HIDE)
        {
            flag1TileMap.gameObject.SetActive(false);
        }
        if (flag1Permission == GameState.Permission.ARMIES_FLAG_1_MAP_SHOW)
        {
            flag1TileMap.gameObject.SetActive(true);
        }

        if (flag2Permission == GameState.Permission.ARMIES_FLAG_2_MAP_HIDE)
        {
            flag2TileMap.gameObject.SetActive(false);
        }
        if (flag2Permission == GameState.Permission.ARMIES_FLAG_2_MAP_SHOW)
        {
            flag2TileMap.gameObject.SetActive(true);
        }

        if (flag3Permission == GameState.Permission.ARMIES_FLAG_3_MAP_HIDE)
        {
            flag3TileMap.gameObject.SetActive(false);
        }
        if (flag3Permission == GameState.Permission.ARMIES_FLAG_3_MAP_SHOW)
        {
            flag3TileMap.gameObject.SetActive(true);
        }

        if (flagHolderPermission == GameState.Permission.ARMY_FLAGHOLDER_MAP_HIDE)
        {
            flagHolderTileMap.gameObject.SetActive(false);
        }
        if (flagHolderPermission == GameState.Permission.ARMY_FLAGHOLDER_MAP_SHOW)
        {
            flagHolderTileMap.gameObject.SetActive(true);
        }
        if (fogofwarPermission == GameState.Permission.FOGOFWAR_MAP_HIDE)
        {
            fogOfWarTileMap.gameObject.SetActive(false);
        }
        if (fogofwarPermission == GameState.Permission.FOGOFWAR_MAP_SHOW)
        {
            fogOfWarTileMap.gameObject.SetActive(true);
        }





        if (hiddenMapPermission == GameState.Permission.HIDDEN_MAP_HIDE)
        {
            hiddenMapTileMap.gameObject.SetActive(false);
        }
        if (hiddenMapPermission == GameState.Permission.HIDDEN_MAP_SHOW)
        {
            hiddenMapTileMap.gameObject.SetActive(true);
        }



        if (overlandEventPermission == GameState.Permission.OVERLAND_EVENTS_HIDE)
        {
            eventTileMap.gameObject.SetActive(false);
        }
        if (overlandEventPermission == GameState.Permission.OVERLAND_EVENTS_SHOW)
        {
            eventTileMap.gameObject.SetActive(true);
        }




        if (overlandItemPermission == GameState.Permission.OVERLAND_ITEMS_HIDE)
        {
            itemsTileMap.gameObject.SetActive(false);
        }
        if (overlandItemPermission == GameState.Permission.OVERLAND_ITEMS_SHOW)
        {
            itemsTileMap.gameObject.SetActive(true);
        }
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Stop();
            Debug.Log("PlayerController.DisplayMap test took " + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
        }
    }

    // public void DisplayOverland() //use this as example
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
    //                playerFlagTileMap.SetTile(new Vector3Int(currentSquare.X_cord, currentSquare.Y_cord, 0), squareTile);
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

    //        if (gameSquare.BattleFieldID > -1)
    //        {

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
    public void RefreshQuestsPanel()
    {
        if (questPanel.activeSelf)
        {
            QuestPanelController questPanelController = questPanel.GetComponent<QuestPanelController>();
            questPanelController.ShowQuests(ThisPlayer.ActiveQuests);

            if (questPanelController.selectedQuestPanel.activeSelf)
            {
                questPanelController.ShowSelectedQuest(questPanelController.selectedQuest);
            }
        }
    }


    public void RefreshHeroesButtonSelection()
    {
        if (Selection.SelectedFriendlyHero != null)
        {
            HeroManagmentPanel heroMenu = allHeroesPanel.GetComponent<HeroManagmentPanel>();
            heroMenu.heroPanelController.theheroID = Selection.SelectedFriendlyHero.UnitID;
            heroMenu.heroPanelController.SelectHero();
        }
        
    }

    public void ShowAllHeroes()
    {
        HeroManagmentPanel heroMenu = allHeroesPanel.GetComponent<HeroManagmentPanel>();
        heroMenu.DisplayAllHeroes(ThisPlayer,ThisPlayer.GameState.GetUIPermissionByObject(GameState.Object.HERO_BUTTONS).Permission);
    }

}
