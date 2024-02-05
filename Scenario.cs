using GameProj.Entities;
using GameProj.Map;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Tilemaps;
using GameProj.Options;
using GameProj.Collections;
using GameProj.Items;
using GameProj.Generators;
using GameProj.Area;
using UnityEngine.SocialPlatforms.Impl;
using GameProj.Events;
using System.Runtime.Serialization;
using System.Threading;
using GameProj.UI;
using Assets.Scripts.Collections;
using GameProj.Skills;
using GameProj.ScenarioS;

[Serializable]
public class Scenario
{
    List<Player> players = new List<Player>();
    List<PlayerSetup> playerSetups = new List<PlayerSetup>();
    List<string> playerturnqueue = new List<string>();
    List<string> playersWhoEndedTurn = new List<string>(); //using seperate variable as doing different system than playerturnqueue
    List<string> playersWhoEndedEvents = new List<string>();
    List<Army> armies = new List<Army>();

    MerchantGuildList guilds = new MerchantGuildList();
    string activePlayerID;
    WorldMap worldmap;
    int turncounter = 0;
    BattleManager queuedUpBattles = new BattleManager();
    private int buildingIdCounter = 100000;
    private int shopItemCounter = 200000;
    private int armyIdCounter = 0;
    private int shopIdCounter = 400000;
    private int questPartyIDCounter = 4000;
    private int itemIDCounter = 300000;
    private int effectIDCounter = 50;
    private int questIDCounter = 50000;
    private int battlefieldIdCounter = 1;
    private int merchantGuildIdCounter = 3000;
    private int buildingProductionIdCounter = 1;
    private int transactionItemIDCounter = 1200;
    private int combatObstacleIDCounter = 20; //for structures from buildings only
    int productionLineIDCounter = 0;
    private int eventIDCounter = 0;
    private string mapTemplateKeyword;

    private BattleManager battlesToBeContinued = new BattleManager();
    private BattleManager completedBattles = new BattleManager();
    private BattleManager activeBattles = new BattleManager();
    private int idCounter = 10000; //entity id counter
    int currentGlobalQuestRefreshTurn;
    int currentPlayerQuestRefreshTurn;
    int currentFutureHeroeRefreshTurn = 0;
    double currentInflation = 0;
    string gameType = "";
    MyRandom savedRandom = null;
    MyRandom aiRandom = null; //seperate random for AI only use, in order to keep main random same in all other clients
    public List<Player> Players { get => players; set => players = value; }
    public List<Army> Armies { get => armies; set => armies = value; }
    public WorldMap Worldmap { get => worldmap; set => worldmap = value; }
    public int Turncounter { get => turncounter; set => turncounter = value; }


    public BattleManager QueuedUpBattles { get => queuedUpBattles; set => queuedUpBattles = value; }
    public int BuildingIdCounter { get => buildingIdCounter; set => buildingIdCounter = value; }
    public int ShopItemCounter { get => shopItemCounter; set => shopItemCounter = value; }
    public string ActivePlayerID { get => activePlayerID; set => activePlayerID = value; }


    /// <summary>
    /// active battles that all players did their turns, but battle did not end yet, used to requeue Battles
    /// </summary>
    public BattleManager BattlesToBeContinued { get => battlesToBeContinued; set => battlesToBeContinued = value; }

    /// <summary>
    /// those battles should be logged for replay and this list will be cleared before next turn(we save them into a file)
    /// </summary>
    public BattleManager CompletedBattles { get => completedBattles; set => completedBattles = value; }
    public int BattlefieldIdCounter { get => battlefieldIdCounter; set => battlefieldIdCounter = value; }
    public int ArmyIdCounter { get => armyIdCounter; set => armyIdCounter = value; }
    public List<string> Playerturnqueue { get => playerturnqueue; set => playerturnqueue = value; }
    public int IdCounter { get => idCounter; set => idCounter = value; }
    public string GameType { get => gameType; set => gameType = value; }
    OptionList optionList;
    List<string> usedHeroKeywords = new List<string>();
    Scoreboard scoreboard = new Scoreboard();
    OurMyValueList defeatedPlayerToClient = new OurMyValueList();


    int questRecursionCounter = 0;
    bool ended = false; //if true, UI will show scoreboard screen
    List<string> playersThatWonList = new List<string>();

    public Scenario()
    {

        //if (optionCollection == null)
        //{

        //}
        //else
        //{

        //    this.OptionList = optionCollection.getAsOptionList().OptionList;
        //}
    }

    //public void SetQuestRefreshCounters()
    //{
    //    this.CurrentGlobalQuestRefreshTurn = GlobalQuestRefreshRate;
    //    this.CurrentPlayerQuestRefreshTurn = PlayerQuestRefreshRate;
    //}

    /// <summary>
    /// manages stuff(dead units, summons,retreated units) for disangaged(ONLY DISENGAGED) army
    /// sets location mode to overland
    /// </summary>
    /// <param name="army"></param>
    public void ArmyAfterBattleProcessing(Army army, RetreatedArmyPointer retreatedUnits, BattlefieldOld battlefield)
    {
        bool debug = true;
        if (debug)
        {
            Debug.Log("ArmyAfterBattleProcessing before add/remove army count: " + Armies.Count);
        }

        Player player = FindPlayerByID(army.OwnerPlayerID);

        //retreat first bc otherwise if a summon retreated u get it back
        army.Units.AddRange(retreatedUnits.Units);

        bool armyIsDead = false;
        string name = "";
        List<Entity> unitsToRemoveFromArmy = new List<Entity>();

        //doing for heroes only first
        foreach (Entity unit in army.Units)
        {
            if (unit.IsHeroFlag)
            {
               
                if (debug)
                {
                    Debug.Log("ArmyAfterBattleProcessing(heroes only) triggering effect for unit: " + unit.CharacterTemplateKeyword);
                    foreach (EntityEffect efct in unit.EntityEffects)
                    {
                        Debug.Log("ArmyAfterBattleProcessing(heroes only) current effect: " + efct.Keyword);
                    }

                }
                //dispelling effects, so that the entity doesnt go away with a -2 or +2 to strength bonus/debuff
                unit.ActivateStatusEffects(EffectFormula.TRIGGER_EFFECT_EXPIRED, battlefield,null);
                unit.ActivateStatusEffects(EffectFormula.TRIGGER_EFFECT_REMOVED, battlefield,null);
                if (!unit.IsAlive())
                {
                    if (unit.UnitID == army.LeaderID)
                    {
                        armyIsDead = true;
                        name = unit.CharacterTemplateKeyword;
                    }
                }
                if (debug)
                {
                    Debug.Log("ArmyAfterBattleProcessing(heroes only) after effect count: " + unit.EntityEffects.Count);
                    foreach (EntityEffect efct in unit.EntityEffects) ;

                }
                unit.EntityEffects.Clear();

                unit.CombatLevelUp();

                if (unit.IsAlive())
                {
                    if (unit.Mission != null)
                    {
                        if (unit.Mission.MissionName == Mission.mission_ReturnToArmy)
                        {
                            unitsToRemoveFromArmy.Add(unit);
                            if (debug)
                            {
                                Debug.Log("hero mission check to return to army " + unit.CharacterTemplateKeyword + " " + unit.UnitID + " " + unit.FindCurrentOwnerID());
                            }
                            Army armyToReturnTo = FindArmyByID(unit.Mission.TargetID);
                            if (armyToReturnTo == null) //the army no longer exists, creating new army but with the same ID (doesnt happen with quests?)
                            {
                                if (debug)
                                {
                                    Debug.Log("new army is being added for the hero");
                                }
                                Army newArmy = new Army(-1, player);
                                newArmy.ArmyID = unit.Mission.TargetID;
                                newArmy.Location = new Location(unit.Mission.Location.WorldMapCoordinates.XCoordinate, unit.Mission.Location.WorldMapCoordinates.YCoordinate);
                                newArmy.Location.Mode = Location.MODE_OVERLAND;
                                newArmy.LeaderID = unit.UnitID;
                                newArmy.Units.Add(unit);
                                newArmy.OwnerPlayerID = army.OwnerPlayerID;
                                unit.Mission = null;
                                lock (Armies)
                                {
                                    if (debug)
                                    {
                                        Debug.Log("army is added to scenario.Armies(hero)");
                                        foreach (Army scenarioArmy in Armies)
                                        {
                                            Debug.Log("scenario army1: " + scenarioArmy.GetInformationWithUnits());
                                        }
                                    }
                                    Armies.Add(newArmy);
                                    if (debug)
                                    {
                                        Debug.Log("army is added to scenario.Armies(hero)");
                                        foreach (Army scenarioArmy in Armies)
                                        {
                                            Debug.Log("scenario army2: " + scenarioArmy.GetInformationWithUnits());
                                        }
                                    }
                                }

                                if (debug)
                                {
                                    foreach (Army scenarioArmy in Armies)
                                    {
                                        Debug.Log("scenario army3: " + scenarioArmy.GetInformationWithUnits());
                                    }
                                }
                            }
                            else
                            {
                                if (debug)
                                {
                                    Debug.Log("adding hero to existing army, location mode: " + armyToReturnTo.Location.Mode);
                                }
                                switch (armyToReturnTo.Location.Mode)
                                {
                                    case Location.MODE_BUILDING_GARRISON:
                                    case Location.MODE_BUILDING_STORAGE:
                                    case Location.MODE_OVERLAND:
                                    case Location.MODE_QUEST:
                                        armyToReturnTo.Units.Add(unit);
                                        armyToReturnTo.LeaderID = unit.UnitID;
                                        unit.Mission = null;
                                        break;
                                    case Location.MODE_IN_DUNGEON_BATTLE:
                                    case Location.MODE_IN_OVERLAND_BATTLE:
                                        PendingReinforcement pendingReinforcement = new PendingReinforcement();
                                        pendingReinforcement.ArmyID = armyToReturnTo.ArmyID;
                                        pendingReinforcement.Entity = unit;
                                        if (armyToReturnTo.Location.WorldMapCoordinates != null)
                                        {
                                            pendingReinforcement.WorldMapCoordinates = new MapCoordinates(armyToReturnTo.Location.WorldMapCoordinates.XCoordinate, armyToReturnTo.Location.WorldMapCoordinates.YCoordinate);
                                        }

                                        player.PendingReinforcements.Add(pendingReinforcement);
                                        break;
                                    default:
                                        Debug.LogError("unforseen situation for " + armyToReturnTo.Location.Mode + " location mode for army: " + armyToReturnTo.GetInformation());
                                        break;
                                }

                            }
                        }
                    }

                }
            }

        }

        //now non hero units
        foreach (Entity unit in army.Units)
        {
            if (!unit.IsHeroFlag)
            {
                if (debug)
                {
                    Debug.Log("ArmyAfterBattleProcessing triggering effect for unit: " + unit.CharacterTemplateKeyword);
                    foreach (EntityEffect efct in unit.EntityEffects)
                    {
                        Debug.Log("ArmyAfterBattleProcessing current effect: " + efct.Keyword);
                    }

                }
                //dispelling effects, so that the entity doesnt go away with a -2 or +2 to strength bonus/debuff
                unit.ActivateStatusEffects(EffectFormula.TRIGGER_EFFECT_EXPIRED, battlefield,null);
                unit.ActivateStatusEffects(EffectFormula.TRIGGER_EFFECT_REMOVED, battlefield,null);
                if (!unit.IsAlive())
                {
                    if (unit.UnitID == army.LeaderID)
                    {
                        armyIsDead = true;
                        name = unit.CharacterTemplateKeyword;
                    }
                }
                if (debug)
                {
                    Debug.Log("ArmyAfterBattleProcessing after effect count: " + unit.EntityEffects.Count);
                    foreach (EntityEffect efct in unit.EntityEffects) ;

                }
                unit.EntityEffects.Clear();

                unit.CombatLevelUp();



                if (unit.IsAlive())
                {
                    if (unit.Mission != null)
                    {
                        if (unit.Mission.MissionName == Mission.mission_ReturnToArmy)
                        {
                            unitsToRemoveFromArmy.Add(unit);
                            if (debug)
                            {
                                Debug.Log("unit mission check to return to army " + unit.CharacterTemplateKeyword + " " + unit.UnitID + " " + unit.FindCurrentOwnerID());
                            }
                            Army armyToReturnTo = FindArmyByID(unit.Mission.TargetID);
                            if (armyToReturnTo == null) //the army no longer exists, creating new army but with the same ID (doesnt happen with quests?)
                            {
                                if (debug)
                                {
                                    Debug.Log("new army is being added for unit");
                                }
                                Army newArmy = new Army(++player.LocalArmyIDCounter, player);
                                newArmy.Location = new Location(unit.Mission.Location.WorldMapCoordinates.XCoordinate, unit.Mission.Location.WorldMapCoordinates.YCoordinate);
                                newArmy.Location.Mode = Location.MODE_OVERLAND;
                                newArmy.LeaderID = unit.UnitID;
                                newArmy.Units.Add(unit);
                                unit.Mission = new Mission();
                                unit.Mission.MissionName = Mission.mission_ReturnToBuildingStorage;
                                Building homeBuilding = FindBuildingByID(unit.UpKeep.BuildingID);
                                if (homeBuilding != null) //if no building to return to & no army to return to, then unit is left in limbo(removed)
                                {
                                    unit.Mission.TargetID = homeBuilding.ID;
                                    lock (Armies)
                                    {
                                        if (debug)
                                        {
                                            Debug.Log("army is added to scenario.Armies(unit)");
                                        }
                                        Armies.Add(newArmy);
                                    }
                                }

                            }
                            else
                            {
                                if (debug)
                                {
                                    Debug.Log("adding unit to existing army, location mode: " + armyToReturnTo.Location.Mode);
                                }
                                switch (armyToReturnTo.Location.Mode)
                                {
                                    case Location.MODE_BUILDING_GARRISON:
                                    case Location.MODE_BUILDING_STORAGE:
                                    case Location.MODE_OVERLAND:
                                    case Location.MODE_QUEST:
                                        armyToReturnTo.Units.Add(unit);
                                        unit.Mission = null;
                                        break;
                                    case Location.MODE_IN_DUNGEON_BATTLE:
                                    case Location.MODE_IN_OVERLAND_BATTLE:
                                        PendingReinforcement pendingReinforcement = new PendingReinforcement();
                                        pendingReinforcement.ArmyID = armyToReturnTo.ArmyID;
                                        pendingReinforcement.Entity = unit;
                                        if (armyToReturnTo.Location.WorldMapCoordinates != null)
                                        {
                                            pendingReinforcement.WorldMapCoordinates = new MapCoordinates(armyToReturnTo.Location.WorldMapCoordinates.XCoordinate, armyToReturnTo.Location.WorldMapCoordinates.YCoordinate);
                                        }

                                        player.PendingReinforcements.Add(pendingReinforcement);
                                        break;
                                    default:
                                        Debug.LogError("unforseen situation for " + armyToReturnTo.Location.Mode + " location mode for army: " + armyToReturnTo.GetInformation());
                                        break;
                                }

                            }
                        }
                    }

                }
            }

        }

        battlefield.Armies.Remove(army);




        Debug.Log("unitsToRemoveFromArmy count " + unitsToRemoveFromArmy.Count);
        foreach (Entity unitThatLeft in unitsToRemoveFromArmy)
        {
            if (debug)
            {
                Debug.Log(unitThatLeft.CharacterTemplateKeyword + " " + unitThatLeft.FindCurrentOwnerID() + " " + unitThatLeft.UnitID);
            }
            army.Units.Remove(unitThatLeft);
            if (army.LeaderID == unitThatLeft.UnitID && army.Units.Count > 0) //doing this just in case
            {
                army.AssignRandomUnitAsLeader(player.PlayerEventRandom);
            }
        }
        //leader died from poison as effects were dispelled so the army is gone now
        if (armyIsDead)
        {
            //Player player = FindPlayerByID(army.OwnerPlayerID);

            Notification notification = new Notification();
            notification.ID = ++player.LocalNotificationID;
            notification.Type = Notification.NotificationType.TYPE_ARMY_LEADER_DIED_AFTER_BATTLE;
            notification.PlayerID = army.OwnerPlayerID;
            notification.HeaderText = name + " died";
            notification.ExpandedText = name + " died from ailments after battle";
            notification.Picture = "other/skull2";
            player.Notifications.Add(notification);
            lock (Armies)
            {
                this.RemoveArmyByID(army.ArmyID);
            }

            return;
        }

        RemoveSummonedAndKilledUnits(army);
        //lock (Armies) //we now do it when battlefield starts
        //{
        //    //we remove the army as it probably has changed a lot since combat(also links between armies probably broke anyway)
        //    this.RemoveArmyByID(army.ArmyID);
        //}

        if (debug)
        {
            Debug.Log("ArmyAfterBattleProcessing remove army count: " + Armies.Count);
        }
        //unitsToRemoveFromThisArmy.Add(unitThatsNotFormThisArmy);

        //if this was an event, then units might have different armies to return to
        //foreach (Entity unitThatsNotFormThisArmy in army.Units)
        //{
        //    if (unitThatsNotFormThisArmy.Mission != null)
        //    {
        //        if (unitThatsNotFormThisArmy.Mission.MissionName == Mission.mission_ReturnToArmy)
        //        {
        //            Army armyToReturnTo = GameEngine.ActiveGame.scenario.FindArmyByID(unitThatsNotFormThisArmy.Mission.SourceID);
        //            if (armyToReturnTo != null) //if the army you want to return to still exists, you get placed there immediately
        //            {
        //                armyToReturnTo.Units.Add(unitThatsNotFormThisArmy);
        //                unitsToRemoveFromThisArmy.Add(unitThatsNotFormThisArmy);
        //            }
        //            else //example: hero got taken away for event battle, meanwhile his waiting army died, so he gets placed back into location of that army(that was saved at the time)
        //            {

        //            }
        //        }
        //    }
        //}
        //if (debug)
        //{
        //    Debug.Log("unitsToRemoveFromThisArmy count: " + unitsToRemoveFromThisArmy.Count);
        //}

        //foreach (Entity entity in unitsToRemoveFromThisArmy)
        //{
        //    if (debug)
        //    {
        //        Debug.Log(entity.CharacterTemplateKeyword + " " + entity.FindCurrentOwnerID() + " " + entity.UnitID);
        //    }

        //    army.Units.Remove(entity);
        //}

        //if unit count is 0, means its a compiled event army, and all units have returned to their armies/locations
        if (army.Units.Count > 0)
        {
            if (debug)
            {
                Debug.Log("non event army units:");
                foreach (Entity unit in army.Units)
                {
                    Debug.Log(unit.CharacterTemplateKeyword + " " + unit.FindCurrentOwnerID() + " " + unit.UnitID);
                }
            }
            //army still functions, so we add it back(its different from previous one)
            //we return the retreated units to army
            Location loc = new Location(army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);
            loc.Mode = Location.MODE_OVERLAND;
            army.Location = loc;
            lock (Armies)
            {
                this.Armies.Add(army);
            }

        }
        if (battlefield.QuestID != -1 && battlefield.EventBattlePlayerID == army.OwnerPlayerID) //if tied to quest, manage the questing parties
        {
            Quest quest = player.FindQuestByID(battlefield.QuestID);
            if (quest == null)
            {
                Debug.LogError("quest is null for player " + player.PlayerID + " battlefield quest id " + battlefield.QuestID);
                foreach (Quest item in player.ActiveQuests)
                {
                    Debug.LogError("quest check: " + item.ID + " " + item.TemplateKeyword + " " + item.Parties.Count);
                }
            }
            QuestParty questParty = quest.FindQuestPartyByPlayerID(player.PlayerID);
            if (questParty.Army.Units.Count == 0)
            {
                quest.DisbandParty(questParty.ID);
            }
            else
            {
                questParty.IsProgressing = true; //you finished the battle and now can proceed
            }



        }
        if (debug)
        {
            Debug.Log("ArmyAfterBattleProcessing add army count: " + Armies.Count);
            Debug.Log("ArmyAfterBattleProcessing army info: " + army.GetInformation());
            Debug.Log("ArmyAfterBattleProcessing loc: " + army.Location.GetInformation());
        }
    }

    internal bool FactionIsTaken(string keyword)
    {
        foreach (Player player in Players)
        {
            if (player.FactionKeyword == keyword)
            {
                return true;
            }
        }
        return false;
    }


    /// <summary>
    /// removes all summoned units(in combat) & army if all units were summons
    /// called after army is being disengaged
    /// </summary>
    /// <param name="army"></param>
    public void RemoveSummonedAndKilledUnits(Army army)
    {
        List<Entity> toRemove = new List<Entity>();
        foreach (Entity entity in army.Units)
        {
            if (entity.DestroyedAfterBattle || !entity.IsAlive() || entity.IsHungry)
            {
                toRemove.Add(entity);
            }
        }
        foreach (Entity entity in toRemove)
        {
            army.RemoveEntityByID(entity.UnitID, false);
        }
    }
    /// <summary>
    /// made this method so we could change how we see the amount of players(dead/defeated players??)
    /// </summary>
    /// <returns></returns>
    internal int GetPlayerCount()
    {
        return Players.Count;
    }

    public void RemoveArmyByID(int ID)
    {
        Army army = FindArmyByID(ID);

        this.Armies.Remove(army);
    }


    public void SaveGameOptions()
    {
        this.OptionList = GameEngine.Data.OptionCollection.getAsOptionList().OptionList;
    }

    /// <summary>
    /// removes and adds future heroes after interval, set calledFromWorldCreation to true so inflation wont go up immediately upon game start
    /// called after buildings and players were created
    /// </summary>
    /// <param name="calledFromWorldCreation"></param>
    public void RefreshFutureHeroCounter(bool calledFromWorldCreation)
    {
        if (HeroAcquisitionMode != "Food")
        {
            return;
        }
        CurrentFutureHeroeRefreshTurn--;

        if (CurrentFutureHeroeRefreshTurn <= 0)
        {
            if (!calledFromWorldCreation)
            {
                CurrentInflation += (Future_Heroes_Refresh_Inflation_Rate / 100);
            }
            RefreshFutureHeroes();//call a function to create next batch of future heroes

            CurrentFutureHeroeRefreshTurn = Future_Heroes_Refresh_Interval;
        }
    }



    public void RefreshQuestCounterForPlayer(string playerID)
    {
        Player player = FindPlayerByID(playerID);
        if (player.Defeated)
        {
            return;
        }
        player.CurrentPlayerQuestRefreshTurn--;
        if (player.CurrentPlayerQuestRefreshTurn <= 0)
        {
            CreateNewPlayerQuestSpecific(player);
            player.CurrentPlayerQuestRefreshTurn = PlayerQuestRefreshRate;
        }


        List<Quest> questToRemove = new List<Quest>();


        foreach (var quest in player.FutureQuests)
        {


            if (quest.ActivationTurn <= Turncounter)
            {
                questToRemove.Add(quest);
                player.ActiveQuests.Add(quest);
            }
        }

        foreach (var item in questToRemove)
        {
            player.FutureQuests.Remove(item);
        }

        questToRemove.Clear();

        foreach (var g in player.ActiveQuests)
        {
            if (g.Parties.Count > 0)
            {
                continue;
            }
            if (g.ExpiresOnTurn <= Turncounter)
            {
                questToRemove.Add(g);
            }
        }
        foreach (var k in questToRemove)
        {
            k.DisbandAllParties();
            player.ActiveQuests.Remove(k);
        }

    }

    /// <summary>
    /// removes quests with expired counter(TODO: return armies), creates new quests when timers are out(option ones) 
    /// </summary>
    public void RefreshQuestCounter()
    {

        CurrentPlayerQuestRefreshTurn--;

        if (CurrentPlayerQuestRefreshTurn <= 0)
        {
            CreateNewPlayerQuests();
            CurrentPlayerQuestRefreshTurn = PlayerQuestRefreshRate;
        }

        foreach (Player player in this.Players)
        {
            List<Quest> questToRemove = new List<Quest>();


            foreach (var quest in player.FutureQuests)
            {


                if (quest.ActivationTurn <= Turncounter)
                {
                    questToRemove.Add(quest);
                    player.ActiveQuests.Add(quest);
                }
            }

            foreach (var item in questToRemove)
            {
                player.FutureQuests.Remove(item);
            }

            questToRemove.Clear();

            foreach (var g in player.ActiveQuests)
            {
                if (g.Parties.Count > 0)
                {
                    continue;
                }
                if (g.ExpiresOnTurn <= Turncounter)
                {
                    questToRemove.Add(g);
                }
            }
            foreach (var k in questToRemove)
            {
                k.DisbandAllParties();
                player.ActiveQuests.Remove(k);
            }

        }


    }

    void RefreshQuestPartyMovementPlayer(Player player)
    {
        foreach (Quest quest in player.ActiveQuests)
        {
            foreach (QuestParty questParty in quest.Parties)
            {
                questParty.RemainingMovementPoints = quest.GetPartyPoints(questParty, "movement");
            }
        }
    }
    public void RefreshQuestPartyMovement()
    {
        foreach (Player player in Players)
        {
            foreach (Quest quest in player.ActiveQuests)
            {
                foreach (QuestParty questParty in quest.Parties)
                {
                    questParty.RemainingMovementPoints = quest.GetPartyPoints(questParty, "movement");
                }
            }
        }
    }

    public List<int> GetActiveQuestIDs(string playerID)
    {
        List<int> answer = new List<int>();
        Player player = FindPlayerByID(playerID);

        foreach (Quest quest in player.ActiveQuests)
        {
            QuestParty questParty = quest.FindQuestPartyByPlayerID(playerID);
            if (questParty != null)
            {
                answer.Add(quest.ID);
            }
        }
        return answer;
    }

    void ProcessQuestPartiesPlayerPhase(Player player)
    {
        if (player.Defeated)
        {
            return;
        }
        foreach (Quest quest in player.ActiveQuests)
        {
            List<QuestParty> toRemove = new List<QuestParty>();
            foreach (QuestParty questParty in quest.Parties)
            {
                //that means player assigned some heroes to the slots, and then just went to next turn without confirming or cancelling, so we remove it here
                if (!questParty.Confirmed)
                {
                    toRemove.Add(questParty);
                    continue;
                }

                if (!questParty.HasEmbarked)
                {
                    bool cancelQuestParty = false;
                    //army check
                    foreach (int id in questParty.AssignedEntitiesList)
                    {
                        Army from = FindOverlandArmyByUnit(id);
                        if (from == null) //if army not overland(quest/combat), then cancel quest party
                        {
                            cancelQuestParty = true;
                            break;
                        }
                        //if wrong location, also cancel quest party(might not be necessary check, but just in case)
                        if (from.Location.Mode != Location.MODE_BUILDING_GARRISON || from.Location.Mode != Location.MODE_OVERLAND)
                        {
                            cancelQuestParty = true;
                            break;
                        }
                    }
                    if (cancelQuestParty)
                    {
                        toRemove.Add(questParty);
                        continue;
                    }

                    questParty.Army = new Army(++GameEngine.ActiveGame.scenario.ArmyIdCounter, player);
                    questParty.Army.OwnerPlayerID = questParty.PlayerID;
                    questParty.Army.Location = new Location();
                    questParty.Army.Location.Mode = Location.MODE_QUEST;
                    questParty.Army.Location.DungeonCoordinates = new QuestCoordinate();
                    questParty.Army.Location.DungeonCoordinates.XCoordinate = 0;
                    questParty.Army.Location.DungeonCoordinates.ID = quest.ID;



                    foreach (int id in questParty.AssignedEntitiesList)
                    {
                        Entity entity = FindUnitByUnitID(id);
                        if (entity == null) //entity was killed before it could go onto a quest //not really possible, as quest mission is nulled?
                        {
                            continue;
                        }
                        Army from = FindOverlandArmyByUnit(entity.UnitID);
                        if (from == null)
                        {
                            Debug.LogError("the army of entity " + entity.UnitName + " " + entity.UnitID + " " + questParty.PlayerID + " was not found");
                            return;
                        }
                        from.RemoveEntityByID(id);
                        entity.Mission = new Mission();
                        //this should make the army disband if the entity is a leader
                        entity.Mission.MissionName = Mission.mission_Quest;
                        questParty.Army.Units.Add(entity);

                    }
                    if (questParty.Army.Units.Count > 0)
                    {
                        questParty.Army.AssignRandomUnitAsLeader(GameEngine.random);
                    }

                    questParty.AssignedEntitiesList.Clear();

                    questParty.HasEmbarked = true;


                    if (questParty.Army.Units.Count == 0) //everyone is dead
                    {
                        toRemove.Add(questParty);
                    }



                }

                if (questParty.HasEmbarked)
                {
                    //double oldProgress = questParty.Progress;
                    //questParty.Progress += quest.GetPartyPoints(questParty, "movement"); ;
                    //List<string> questTemplateKeywords = quest.GetEventsInRange(oldProgress, questParty.Progress);
                    //Location location = new Location();
                    //location.DungeonCoordinates = new QuestCoordinate();
                    //location.DungeonCoordinates.ID = quest.ID;
                    //location.DungeonCoordinates.XCoordinate = questParty.Progress;
                    //Debug.Log("party progress: " + questParty.Progress);
                    //foreach (string questKeyword in questTemplateKeywords)
                    //{
                    //    Debug.Log("initilizing quest event: " + questKeyword);
                    //    player.InitQuestEvent(GameEngine.Data.EventTemplateCollection.findByKeyword(questKeyword), location);
                    //}

                }

            }
            foreach (var item in toRemove)
            {
                quest.Parties.Remove(item);
            }
        }
        player.DungeonQueue = GetActiveQuestIDs(player.PlayerID);
        RefreshQuestPartyMovementPlayer(player);
    }

    public void ProcessQuestPartiesPhase()
    {

        bool debug = true;

        //processing parties for player quests
        foreach (Player player in Players)
        {

            foreach (Quest quest in player.ActiveQuests)
            {
                if (debug)
                {
                    Debug.Log("checking quest " + quest.ID + " " + quest.TemplateKeyword);
                }
                List<QuestParty> toRemove = new List<QuestParty>();
                foreach (QuestParty questParty in quest.Parties)
                {
                    if (debug)
                    {
                        Debug.Log("checking questparty: " + questParty.PlayerID);
                    }
                    //that means player assigned some heroes to the slots, and then just went to next turn without confirming or cancelling, so we remove it here
                    if (!questParty.Confirmed)
                    {
                        if (debug)
                        {
                            Debug.Log("questparty not confirmed for player: " + questParty.PlayerID);
                        }
                        toRemove.Add(questParty);
                        continue;
                    }

                    if (!questParty.HasEmbarked)
                    {
                        bool cancelQuestParty = false;
                        //army check
                        foreach (int id in questParty.AssignedEntitiesList)
                        {
                            Army from = FindOverlandArmyByUnit(id);
                            if (from == null) //if army not overland(quest/combat), then cancel quest party
                            {
                                if (debug)
                                {
                                    Debug.Log("from army is null, party is cancelled");
                                }
                                cancelQuestParty = true;
                                break;
                            }
                            //if wrong location, also cancel quest party(might not be necessary check, but just in case)
                            if (from.Location.Mode != Location.MODE_OVERLAND)
                            {
                                if (debug)
                                {
                                    Debug.Log("from army is in location: " + from.Location.Mode + " but must be overland");
                                }
                                cancelQuestParty = true;
                                break;
                            }
                        }
                        if (cancelQuestParty)
                        {
                            if (debug)
                            {
                                Debug.Log("questparty cancelled: " + questParty.PlayerID);
                            }
                            toRemove.Add(questParty);
                            continue;
                        }

                        questParty.Army = new Army(++GameEngine.ActiveGame.scenario.ArmyIdCounter, player);
                        questParty.Army.OwnerPlayerID = questParty.PlayerID;
                        questParty.Army.Location = new Location();
                        questParty.Army.Location.Mode = Location.MODE_QUEST;
                        questParty.Army.Location.DungeonCoordinates = new QuestCoordinate();
                        questParty.Army.Location.DungeonCoordinates.XCoordinate = 0;
                        questParty.Army.Location.DungeonCoordinates.ID = quest.ID;



                        foreach (int id in questParty.AssignedEntitiesList)
                        {
                            Entity entity = FindUnitByUnitID(id);
                            if (entity == null) //entity was killed before it could go onto a quest //not really possible, as quest mission is nulled?
                            {
                                continue;
                            }
                            Army from = FindOverlandArmyByUnit(entity.UnitID);
                            if (from == null)
                            {
                                Debug.LogError("the army of entity " + entity.UnitName + " " + entity.UnitID + " " + questParty.PlayerID + " was not found");
                                return;
                            }
                            from.RemoveEntityByID(id);
                            entity.Mission = new Mission();
                            //this should make the army disband if the entity is a leader
                            entity.Mission.MissionName = Mission.mission_Quest;
                            questParty.Army.Units.Add(entity);

                        }
                        if (questParty.Army.Units.Count > 0)
                        {
                            questParty.Army.AssignRandomUnitAsLeader(GameEngine.random);
                        }

                        questParty.AssignedEntitiesList.Clear();

                        questParty.HasEmbarked = true;


                        if (questParty.Army.Units.Count == 0) //everyone is dead
                        {
                            toRemove.Add(questParty);
                        }



                    }

                    if (questParty.HasEmbarked)
                    {
                        //double oldProgress = questParty.Progress;
                        //questParty.Progress += quest.GetPartyPoints(questParty, "movement"); ;
                        //List<string> questTemplateKeywords = quest.GetEventsInRange(oldProgress, questParty.Progress);
                        //Location location = new Location();
                        //location.DungeonCoordinates = new QuestCoordinate();
                        //location.DungeonCoordinates.ID = quest.ID;
                        //location.DungeonCoordinates.XCoordinate = questParty.Progress;
                        //Debug.Log("party progress: " + questParty.Progress);
                        //foreach (string questKeyword in questTemplateKeywords)
                        //{
                        //    Debug.Log("initilizing quest event: " + questKeyword);
                        //    player.InitQuestEvent(GameEngine.Data.EventTemplateCollection.findByKeyword(questKeyword), location);
                        //}

                    }

                }
                foreach (var item in toRemove)
                {
                    quest.Parties.Remove(item);
                }
            }
            player.DungeonQueue = GetActiveQuestIDs(player.PlayerID);
        }
        RefreshQuestPartyMovement();
    }

    public void CreateInteractableQuest(List<InteractableQuest> interactableQuests, string playerID)
    {
        questRecursionCounter++;
        if (questRecursionCounter >= MaximumQuestRecursion)
        {
            Debug.Log("maximum quest recursion reached");
            return;
        }
        ChanceEngine chanceEngine = new ChanceEngine();
        foreach (InteractableQuest interactableQuest in interactableQuests)
        {
            List<string> legalPlayers = new List<string>();
            switch (interactableQuest.Mode)
            {
                case InteractableQuest.MODE_ALL_PLAYERS:
                    foreach (var item in Players)
                    {
                        legalPlayers.Add(item.PlayerID);
                    }
                    break;
                case InteractableQuest.MODE_THIS_PLAYER:
                    legalPlayers.Add(playerID);
                    break;
                case InteractableQuest.MODE_OTHER_PLAYERS:
                    foreach (var item in Players)
                    {
                        if (item.PlayerID == playerID)
                        {
                            continue;
                        }
                        legalPlayers.Add(item.PlayerID);
                    }

                    break;

                default:
                    break;
            }


            for (int i = 0; i < interactableQuest.NumberOfTimes; i++)
            {
                if (legalPlayers.Count < 1)
                {
                    break;
                }
                chanceEngine.reset();
                int badPercent = 100 - interactableQuest.Odds;
                chanceEngine.next(badPercent);
                chanceEngine.next(interactableQuest.Odds);
                if (chanceEngine.calculate() == 0)
                {
                    continue;
                }
                int index = GameEngine.random.Next(0, legalPlayers.Count);
                Player player = FindPlayerByID(legalPlayers[index]);
                legalPlayers.Remove(legalPlayers[index]);
                QuestTemplate questTemplate = GameEngine.Data.QuestTemplateCollection.findByKeyword(interactableQuest.QuestKeyword);
                Quest newQuest = Quest.GetQuestFromTemplate(questTemplate);
                newQuest.ActivationTurn = interactableQuest.StartingBracket * PlayerQuestRefreshRate;
                newQuest.ExpiresOnTurn = newQuest.ActivationTurn + (questTemplate.ActiveDuration * PlayerQuestRefreshRate);

                player.FutureQuests.Add(newQuest);
                if (questTemplate.InteractableQuests.Count > 0)
                {
                    questRecursionCounter = 0;
                    CreateInteractableQuest(questTemplate.InteractableQuests, playerID);
                }

            }

        }
    }

    void CreateNewPlayerQuestSpecific(Player player)
    {
        List<Quest> toRemove = new List<Quest>();
        int neededNew = PlayerQuestAmount;

        List<QuestTemplate> questTemplates = GameEngine.Data.QuestTemplateCollection.GetRandomQuests(new List<string>(), new List<string>() { Quest.TYPE_TRIGGERED }, Int32.MinValue, Int32.MaxValue, neededNew, false);



        for (int i = 0; i < neededNew; i++)
        {


            //Debug.Log("quest count: "+questTemplates.Count);
            Quest newQuest = Quest.GetQuestFromTemplate(questTemplates[i]);
            newQuest.ActivationTurn = questTemplates[i].StartingTurnBracket * PlayerQuestRefreshRate;
            newQuest.ExpiresOnTurn = newQuest.ActivationTurn + (questTemplates[i].ActiveDuration * PlayerQuestRefreshRate);
            if (questTemplates[i].InteractableQuests.Count > 0)
            {
                CreateInteractableQuest(questTemplates[i].InteractableQuests, player.PlayerID);
            }
            player.FutureQuests.Add(newQuest);
            //player.ActiveQuests.Add(newQuest);
        }

    }

    public void CreateNewPlayerQuests()
    {

        //global quest roll(temporary)

        //player quest roll(temporary)
        foreach (Player player in Players)
        {
            List<Quest> toRemove = new List<Quest>();
            int neededNew = PlayerQuestAmount;





            List<QuestTemplate> questTemplates = GameEngine.Data.QuestTemplateCollection.GetRandomQuests(new List<string>(), new List<string>() { Quest.TYPE_TRIGGERED }, Int32.MinValue, Int32.MaxValue, neededNew, false);



            for (int i = 0; i < neededNew; i++)
            {


                //Debug.Log("quest count: "+questTemplates.Count);
                Quest newQuest = Quest.GetQuestFromTemplate(questTemplates[i]);
                newQuest.ActivationTurn = questTemplates[i].StartingTurnBracket * PlayerQuestRefreshRate;
                newQuest.ExpiresOnTurn = newQuest.ActivationTurn + (questTemplates[i].ActiveDuration * PlayerQuestRefreshRate);
                if (questTemplates[i].InteractableQuests.Count > 0)
                {
                    CreateInteractableQuest(questTemplates[i].InteractableQuests, player.PlayerID);
                }
                player.FutureQuests.Add(newQuest);
                //player.ActiveQuests.Add(newQuest);
            }

        }

    }

    [XmlIgnore]
    public bool UniqueRandomFactions
    {
        get { return Boolean.Parse(this.OptionList.findByNameWithType(OptionCollection.UniqueRandomFactions, "condition")); }
    }


    [XmlIgnore]
    public bool IncognitoMode
    {
        get { return Boolean.Parse(this.OptionList.findByNameWithType(OptionCollection.Incognito, "condition")); }
    }

    [XmlIgnore]
    public string CombatInitiativeMode
    {
        get { return this.OptionList.findByNameWithType(OptionCollection.CombatInitiativeMode, "mode"); }
    }


    [XmlIgnore]
    public string WinConditionMode
    {
        get { return this.OptionList.findByNameWithType(OptionCollection.Victory_Conditions, "mode"); }
    }


    [XmlIgnore]
    public string PlayerDeathCondition
    {
        get { return this.OptionList.findByNameWithType(OptionCollection.Player_Death_Condition, "mode"); }
    }


    [XmlIgnore]
    public int SurviveUntilTurn
    {
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.How_Many_Turns_To_Survive, "nr")); }
    }

    [XmlIgnore]
    public int HowManyPlayersShouldRemain
    {
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.How_Many_Players_Left_Standing, "nr")); }
    }

    [XmlIgnore]
    public string ExpShareMode
    {
        get { return this.OptionList.findByNameWithType(OptionCollection.ExpShare, "mode"); }
    }

    [XmlIgnore]
    public string HeroAcquisitionMode
    {
        get { return this.OptionList.findByNameWithType(OptionCollection.HeroAcquire, "mode"); }
    }
    [XmlIgnore]
    public string SeedMode
    {
        get { return this.OptionList.findByNameWithType(OptionCollection.Seed_Mode, "mode"); }
    }
    [XmlIgnore]
    public int SetSeed
    {
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.Seed_SPECIFIC, "nr")); }
    }
    [XmlIgnore]
    public string BattleProcessingMode
    {
        get { return this.OptionList.findByNameWithType(OptionCollection.BattleProcessingMode, "mode"); }
    }

    [XmlIgnore]
    public int CombatRoundLastTurns
    {
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.Combat_Round_Turns, "nr")); }
    }

    [XmlIgnore]
    public int OverlandBattlefieldRadius
    {
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.BattlefieldRadius, "nr")); }
    }

    [XmlIgnore]
    public int CombatSectorRadius
    {
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.SectorRadius, "nr")); }
    }

    [XmlIgnore]
    public int MaximumQuestRecursion
    {
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.Maximum_recursion_for_quest_generation, "nr")); }
    }
    //[XmlIgnore]
    //public int Number_of_Barons_to_be_Generated
    //{
    //    get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.Number_of_Barons, "nr")); }
    //}

    //[XmlIgnore]
    //public int Number_of_Counts_to_be_Generated
    //{
    //    get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.Number_of_Counts, "nr")); }
    //}

    //[XmlIgnore]
    //public int Number_of_Dukes_to_be_Generated
    //{
    //    get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.Number_of_Dukes, "nr")); }
    //}

    [XmlIgnore]
    public int Max_Map_Size_X
    {
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.MapSizeX, "nr")); }
    }
    [XmlIgnore]
    public int Max_Map_Size_Y
    {
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.MapSizeY, "nr")); }
    }

    [XmlIgnore]
    public int Grassland_Terrain_Odds
    {
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.Grassland_Terrain_Odds, "nr")); }
    }
    [XmlIgnore]
    public int CombatRounds
    {
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.Combat_Round_Turns, "nr")); }
    }

    [XmlIgnore]
    public int Desert_Terrain_Odds
    {
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.Desert_Terrain_Odds, "nr")); }
    }

    [XmlIgnore]
    public int Swamp_Terrain_Odds
    {
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.Swamp_Terrain_Odds, "nr")); }
    }

    [XmlIgnore]
    public int Swamp_Grass_Terrain_Odds
    {
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.Swamp_Grass_Terrain_Odds, "nr")); }
    }

    [XmlIgnore]
    public int Snow_Terrain_Odds
    {
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.Snow_Terrain_Odds, "nr")); }
    }

    [XmlIgnore]
    public int Red_Sand_Anomaly_Terrain_Odds
    {
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.Red_sand_anomaly_terrain_Odds, "nr")); }
    }


    [XmlIgnore]
    public int PlayerQuestAmount
    {
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.Player_Quests_Amount, "nr")); }
    }




    [XmlIgnore]
    public int PlayerQuestRefreshRate
    {
        //get { return 10; }
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.Player_Quests_Refresh_Rate, "nr")); }
    }

    [XmlIgnore]
    public int Future_Heroes_Refresh_Interval
    {
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.Future_Heroes_Refresh_Interval, "nr")); }
    }

    public int Future_Heroes_Refresh_Inflation_Rate
    {
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.Future_Heroes_Inflation_Rate, "nr")); }
    }

    public int Future_Heroes_Batch_Amount
    {
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.Future_Heroes_Batch_Amount, "nr")); }
    }

    [XmlIgnore]
    public int Events_Odds
    {
        get { return Int32.Parse(this.OptionList.findByNameWithType(OptionCollection.Event_Odds, "nr")); }
    }


    [XmlIgnore]
    public bool Events_Toggle
    {
        get { return Boolean.Parse(this.OptionList.findByNameWithType(OptionCollection.Triggered_Events, "condition")); }
    }

    [XmlIgnore]
    public bool ShowEnemySkills
    {

        get { return Boolean.Parse(this.OptionList.findByNameWithType(OptionCollection.ShowEnemySkills, "condition")); }

    }


    [XmlIgnore]
    public bool AllowPickUpFromGameSquareInventory
    {
        get { return Boolean.Parse(this.OptionList.findByNameWithType(OptionCollection.Allow_Picking_Up_From_GameSquare_Inventory, "condition")); }
    }

    [XmlIgnore]
    public bool LootClaimSystem
    {
        get { return Boolean.Parse(this.OptionList.findByNameWithType(OptionCollection.Loot_Claim_System, "condition")); }
    }
    //public struct GameState
    //{
    //    public const string EventPhase = "Event phase";
    //    public const string AfterEventPhase = "After event phase";
    //    public const string MainPhase = "Main phase";
    //    public const string BattlePhase = "Battle phase";
    //    public const string AfterBattlePhase = "After Battle phase";
    //    public const string Bypass = "Bypass"; //override event restrictions
    //}



    [XmlIgnore]
    public string StartMode
    {
        get { return this.OptionList.findByNameWithType(OptionCollection.Startmode, "mode"); }
    }
    /// <summary>
    /// requires rework due to multiplayer
    /// TODO all players in mp should do this?
    /// TODO is there anything else to initialize
    /// </summary>
    public void OnLoad()
    {
        //UnityEngine.Random.state = RandomSeed;


        //Debug.Log(UnityEngine.Random.state);


        //GameEngine.ActiveGame.InstantiatePlayerPrefabs(Players);
        //initializing randoms
        GameEngine.random = new MyRandom(this.SavedRandom.Seed, this.SavedRandom.Iteration);
        this.AiRandom = new MyRandom(this.AiRandom.Seed, this.AiRandom.Iteration);

        //plr random is initialized in OnScenarioRecieve
        //foreach (Player plr in Players)
        //{
        //    plr.PlayerEventRandom = new MyRandom(plr.PlayerEventRandom.Seed,plr.PlayerEventRandom.Iteration);
        //}
        foreach (BattlefieldOld battlefield in ActiveBattles.Battlefields)
        {
            battlefield.BattlefieldRandom = new MyRandom(battlefield.BattlefieldRandom.Seed,battlefield.BattlefieldRandom.Iteration);
        }
        foreach (BattlefieldOld battlefield in QueuedUpBattles.Battlefields)
        {
            battlefield.BattlefieldRandom = new MyRandom(battlefield.BattlefieldRandom.Seed, battlefield.BattlefieldRandom.Iteration);
        }
        foreach (BattlefieldOld battlefield in BattlesToBeContinued.Battlefields)
        {
            battlefield.BattlefieldRandom = new MyRandom(battlefield.BattlefieldRandom.Seed, battlefield.BattlefieldRandom.Iteration);
        }
        //Debug.Log("before links");
        //Worldmap.GetInformation();
        //MapGenerator.CreateTileMapCoordsByObjectLink(Worldmap);
        Worldmap.SetObjectNeighboursByCubeCoordinates(false);
        //Debug.Log("after links");
        //Worldmap.GetInformation();
        foreach (Player player in Players)
        {
            player.MapMemory.SetObjectNeighbours();
        }
        //GameEngine.ActiveGame.UpdateMemoryMapWithActiveVision(GameEngine.ActivePlayer);

        //GameEngine.ActiveGame.DisplayLocalActivePlayer(false);

        foreach (BattlefieldOld battlefield in QueuedUpBattles.Battlefields)
        {
            foreach (CombatMapMemory memoryTiles in battlefield.PlayerMapMemories)
            {
                memoryTiles.battlefield = battlefield;
                memoryTiles.SetObjectNeighbours();
            }
        }

        foreach (BattlefieldOld battlefield in ActiveBattles.Battlefields)
        {
            foreach (CombatMapMemory memoryTiles in battlefield.PlayerMapMemories)
            {
                memoryTiles.battlefield = battlefield;
                memoryTiles.SetObjectNeighbours();
            }
        }

        foreach (BattlefieldOld battlefield in BattlesToBeContinued.Battlefields)
        {
            foreach (CombatMapMemory memoryTiles in battlefield.PlayerMapMemories)
            {
                memoryTiles.battlefield = battlefield;
                memoryTiles.SetObjectNeighbours();
            }
        }
        //TODO mp command goes here?????
        //TODO: waitForPlayers or not here?
        GameEngine.ActiveGame.OnScenarioRecieve();
        //GameEngine.ActiveGame.NewInitilizeLocalPlayerPrefabs();
    }


    /// <summary>
    /// for chanceengine (in worldmap.setterrain), so you dont actually have to order the .next() of chanceengine
    /// </summary>
    /// <param name="keyword"></param>
    /// <returns></returns>
    public int GetTerrainOdds(string keyword)
    {
        if (keyword == TerrainTemplateCollection.GRASSLAND)
        {
            return Grassland_Terrain_Odds;
        }
        if (keyword == TerrainTemplateCollection.RED_SAND)
        {
            return Red_Sand_Anomaly_Terrain_Odds;
        }
        if (keyword == TerrainTemplateCollection.DESERT)
        {
            return Desert_Terrain_Odds;
        }
        if (keyword == TerrainTemplateCollection.SWAMP)
        {
            return Swamp_Terrain_Odds;
        }
        if (keyword == TerrainTemplateCollection.SWAMP_GRASS)
        {
            return Swamp_Grass_Terrain_Odds;
        }
        if (keyword == TerrainTemplateCollection.SNOW)
        {
            return Snow_Terrain_Odds;
        }
        return 0;
    }

    public PlayerSetup GetPlayerSetupByPlayerID(string playerID)
    {
        foreach (PlayerSetup setup in PlayerSetups)
        {
            if (setup.PlayerName == playerID)
            {
                return setup;
            }
        }
        return null;
    }
    public List<Player> GetLocalPlayers()
    {
        List<Player> answer = new List<Player>();

        foreach (PlayerSetup playerSetup in GetPlayerSetupsByComputerName(GameEngine.PLAYER_IDENTITY))
        {
            answer.Add(FindPlayerByID(playerSetup.PlayerName));
        }

        return answer;
    }
    public List<PlayerSetup> GetPlayerSetupsByComputerName(string computer_ID)
    {
        List<PlayerSetup> playerSetups = new List<PlayerSetup>();
        foreach (PlayerSetup setup in PlayerSetups)
        {
            if (setup.ComputerName == computer_ID)
            {
                playerSetups.Add(setup);
            }
        }
        return playerSetups;
    }

    /// <summary>
    /// rolls all the info from faction into player to be used to generate stuff
    /// </summary>
    /// <param name="player"></param>
    /// <param name="faction"></param>
    public void RollFactionDataIntoPlayer(Player player, Faction faction)
    {
        //rolling merchant guilds
        foreach (List<TemplateOdd> merchantGuild in faction.StartingGuilds)
        {
            MerchantGuildTemplate merchantGuildTemplate = GameEngine.Data.MerchantGuildTemplateCollection.GetMerchantGuildTemplateByRequest(merchantGuild,GameEngine.random);
            if (merchantGuildTemplate != null)
            {
                if (!player.StartingGuilds.Contains(merchantGuildTemplate.Keyword))
                {
                    player.StartingGuilds.Add(merchantGuildTemplate.Keyword);
                }
            }
        }
        //rolling terrain
        if (faction.StartingTerrain.Count > 0)
        {
            TerrainTemplate terrainTemplate = GameEngine.Data.TerrainTemplateCollection.GetTerrainTemplateByRequest(faction.StartingTerrain,GameEngine.random);
            if (terrainTemplate != null)
            {
                player.DefaultTerrain = terrainTemplate.Keyword;
            }           
        }

        //rolling starting buildings
        foreach (List<TemplateOdd> building in faction.StartingBuildings)
        {
            Stat buildingTemplate = GameEngine.Data.BuildingTemplateCollection.GetBuildingTemplateAndQuantityByRequest(building, GameEngine.random);
            if (buildingTemplate != null)
            {
                player.StartingBuildings.AddToExistingValue(buildingTemplate.Keyword,buildingTemplate.Amount);
            }
        }

        //rolling items
        foreach (List<TemplateOdd> item in faction.StartingItems)
        {
            Stat itemStat = GameEngine.Data.ItemTemplateCollection.GetItemTemplateAndQuantityByRequest(item, GameEngine.random);
            if (itemStat != null)
            {
                player.StartingItems.AddToExistingValue(itemStat.Keyword, itemStat.Amount);
            }
    
        }

        //doesnt get changed, no need for cloning
        player.StartingHeroes = faction.StartingHeroes;
        player.EventsToBeSpawned = faction.EventsToBeSpawned;


        //race relatins
        foreach (Stat relation in faction.StartingRelations)
        {
            player.RaceRelations.AddToExistingValue(relation.Keyword, relation.Amount);
        }


        foreach (string trait in faction.PlayerTraits)
        {
            player.Traits.AddToExistingValue(trait,1);
        }


        //item income
        foreach (Stat income in faction.ItemIncomes)
        {
            player.ItemIncome.Add(new Stat(income.Keyword, income.Amount));
        }
    }

    /// <summary>
    /// numberofPlayers - amount of players to create 
    /// </summary>
    /// <param name="numberofPlayers"></param>
    /// <param name="playerNames"></param>
    public void GeneratePlayers()
    {
        Player player;


        int index = 0;
        foreach (PlayerSetup setup in PlayerSetups)
        {
            player = new Player();
            Players.Add(player);
            player.GameState = GameEngine.Data.GameStateCollection.findByKeyword(GameState.State.MAIN_PHASE);
            player.PlayerID = setup.PlayerName;
            index++;
            //now creating random here, as it is ok if randoms are as different as they egt
            int valToAdd = GameEngine.random.Seed;
            try
            {
                valToAdd = GameEngine.random.Seed + index;
            }
            catch (Exception)
            {
                valToAdd = Int32.MaxValue;

            }
            player.PlayerEventRandom = new MyRandom(valToAdd, GameEngine.random.Iteration);
            Faction faction;
            if (setup.FactionKeyword == Faction.FACTION_RANDOM)
            {
                faction = GameEngine.Data.FactionCollection.GetRandomFaction(UniqueRandomFactions);
            }
            else
            {
                faction = GameEngine.Data.FactionCollection.findByKeyword(setup.FactionKeyword);
            }
            if (faction != null)
            {
                player.FactionKeyword = faction.Keyword;

                RollFactionDataIntoPlayer(player,faction);
            }//if draft, then rolled draft data has to be set here to be player's
           
            //when starting to do drafts, will have to move these methods somewhere else, or not use GeneratePlayers at all
            CreateBeginningShop(player);
            CreatePlayerStartingItems(player);
            
            //foreach (Stat income in faction.ItemIncomes)
            //{
            //    player.ItemIncome.Add(new Stat(income.Keyword, income.Amount));
            //}

            //foreach (List<TemplateOdd> itemRequest in faction.StartingItems)
            //{
            //    Item item = Item.createItemByRequest(itemRequest, GameEngine.random, "");

            //    if (item != null)
            //    {
            //        if (player.OwnedItems.HasSpaceToTakeItems(new List<Item> { item }, true))
            //        {
            //            player.OwnedItems.AddItem(item);
            //        }
            //        else
            //        {
            //            CreateExtraItem(player.PlayerID, new List<Item> { item }, 5, "You had no inventory space after item transafer", "Take your item:", true);
            //        }

            //    }

            //}
            player.Color1 = setup.Color1;
            player.Color2 = setup.Color2;
            player.Color3 = setup.Color3;

            SetPlayerColor(player); //temporary, no interface to change colors in lobby right now



        }





        #region old
        //for (int i = 0; i < GameEngine.ActiveGame.amountOfPlayers; i++)
        //{

        //    player = new Player();
        //    player.GameState = GameEngine.Data.GameStateCollection.findByKeyword(GameState.State.MAIN_PHASE);
        //    UIPermission g = player.GameState.GetUIPermissionByObject(GameState.Object.FOGOFWAR_TILE_MAP);
        //    //if (g == null)
        //    //{
        //    //    Debug.LogError("WTF");
        //    //}
        //    //foreach (var item in player.GameState.UIPermissions)
        //    //{
        //    //    Debug.LogError(item.TargetObject + " : " + GameState.Object.FOGOFWAR_TILE_MAP);
        //    //}
        //    CreateBeginningShop(player);
        //    //player.Shops.Add(Shop.createShop(Shop.ShopMode.Mixed));
        //    //player.Shops.Add(Shop.createShop(Shop.ShopMode.HeroShop));
        //    //player.PlayerID = playerNames[i] + (i + 1);
        //    player.PlayerID = "player" + (i + 1);
        //    //player.ItemIncome.Add(new Stat(StatCollection.PURPLE_MANA, 40));
        //    //player.ItemIncome.Add(new Stat(StatCollection.GEM_BLACK, 40));
        //    //player.ItemIncome.Add(new Stat(StatCollection.GEM_GREEN, 40));
        //    //player.ItemIncome.Add(new Stat(StatCollection.RED_MANA, 40));
        //    //player.ItemIncome.Add(new Stat(StatCollection.WHITE_MANA, 40));
        //    //player.ItemIncome.Add(new Stat(StatCollection.GEM_IMPERIAL_COIN, 40));
        //    //player.ItemIncome.Add(new Stat(StatCollection.GEM_BLUE, 40));
        //    player.ItemIncome.Add(new Stat(ItemTemplate.BLACK_MANA_CRYSTAL,50));
        //    player.ItemIncome.Add(new Stat(ItemTemplate.BLUE_MANA_CRYSTAL,50));
        //    player.ItemIncome.Add(new Stat(ItemTemplate.GREEN_MANA_CRYSTAL,50));
        //    player.ItemIncome.Add(new Stat(ItemTemplate.PURPLE_MANA_CRYSTAL,50));
        //    player.ItemIncome.Add(new Stat(ItemTemplate.RED_MANA_CRYSTAL,50));
        //    player.ItemIncome.Add(new Stat(ItemTemplate.WHITE_MANA_CRYSTAL,50));
        //    Item startingCash = Item.createItemByKeyword(ItemTemplateCollection.IMPERIAL_COIN);
        //    startingCash.Quantity = 2000;

        //    player.OwnedItems.AddItem(startingCash);

        //    startingCash = Item.createItemByKeyword("Iron Sword");
        //    startingCash.Quantity = 2;

        //    player.OwnedItems.AddItem(startingCash);

        //    startingCash = Item.createItemByKeyword("Iron Longsword");
        //    startingCash.Quantity = 1;

        //    player.OwnedItems.AddItem(startingCash);

        //    startingCash = Item.createItemByKeyword("Goblin fish jerky");
        //    startingCash.Quantity = 5;

        //    player.OwnedItems.AddItem(startingCash);

        //    startingCash = Item.createItemByKeyword("Lumber");
        //    startingCash.Quantity = 2000;

        //    player.OwnedItems.AddItem(startingCash);

        //    startingCash = Item.createItemByKeyword("Stone");
        //    startingCash.Quantity = 2000;

        //    player.OwnedItems.AddItem(startingCash);


        //    startingCash = Item.createItemByKeyword("Goblin spearman upgrade potion");
        //    startingCash.Quantity = 1;

        //    player.OwnedItems.AddItem(startingCash);

        //    //player.OwnedItems.AddItem(Item.createItemByKeyword(ItemTemplateCollection.IMPERIAL_COIN).Qu);

        //    SetPlayerColor(player);
        //    players.Add(player);

        //}
        #endregion
        player = new Player();
        player.GameState = GameEngine.Data.GameStateCollection.findByKeyword(GameState.State.MAIN_PHASE);
        player.PlayerID = "neutral";
        player.FactionKeyword = Faction.FACTION_ORCS;
        player.ItemIncome.Add(new Stat(ItemTemplate.BLACK_MANA_CRYSTAL, 50));
        player.ItemIncome.Add(new Stat(ItemTemplate.BLUE_MANA_CRYSTAL, 50));
        player.ItemIncome.Add(new Stat(ItemTemplate.GREEN_MANA_CRYSTAL, 50));
        player.ItemIncome.Add(new Stat(ItemTemplate.PURPLE_MANA_CRYSTAL, 50));
        player.ItemIncome.Add(new Stat(ItemTemplate.RED_MANA_CRYSTAL, 50));
        player.ItemIncome.Add(new Stat(ItemTemplate.WHITE_MANA_CRYSTAL, 50));
        player.PlayerEventRandom = new MyRandom(GameEngine.random.Seed, GameEngine.random.Iteration);
        SetPlayerColor(player);




        Players.Add(player);

        //setting local id counters to:
        //player 1 = 1m, player 2 = 2m, player 3 = 3m
        for (int i = 1; i < Players.Count + 1; i++)
        {
            Players[i - 1].LocalArmyIDCounter *= i;
            Players[i - 1].LocalEntityIDCounter *= i;
            Players[i - 1].LocalItemIDCounter *= i;
            Players[i - 1].LocalShopItemID *= i;
            Players[i - 1].LocalTransactionItemID *= i;
            Players[i - 1].LocalNotificationID *= i;
            Players[i - 1].LocalBattlefieldIDCounter *= i;
            Players[i - 1].LocalEventIDCounter *= i;
        }
        //after all randoms for players are done, then we add event random
        //int index = 0;
        //foreach (Player plr in Players)
        //{
        //    index++;
        //    int valToAdd = GameEngine.random.Seed;
        //    try
        //    {
        //        valToAdd = GameEngine.random.Seed + index;
        //    }
        //    catch (Exception)
        //    {
        //        valToAdd = Int32.MaxValue;
               
        //    }
        //    plr.PlayerEventRandom = new MyRandom(valToAdd, GameEngine.random.Iteration);
        //}

    }


    public void GeneratePlayerStartingEvents()
    {
        foreach (Player player in Players)
        {
            if (player.PlayerID == Player.Neutral)
            {
                continue;
            }
            foreach (EventCommand command in player.EventsToBeSpawned)
            {
                Debug.Log("GeneratePlayerStartingEvents starting event " + command.SpawnEvent.EventKeywordToCreate + " for " + player.PlayerID);
                EventChain eventChain = new EventChain();
                EventChain.SpawnEvents(command, player, null, eventChain);
            }
        }
       
    }

    void CreatePlayerStartingItems(Player player)
    {
        foreach (Stat itemStat in player.StartingItems)
        {
            Item item = Item.createItemByKeyword(itemStat.Keyword, "");
            item.Quantity = (int)itemStat.Amount;

            if (player.OwnedItems.HasSpaceToTakeItems(new List<Item> { item }, true))
            {
                player.OwnedItems.AddItem(item);
            }
            else
            {
                CreateExtraItem(player.PlayerID, new List<Item> { item }, 5, "You had no inventory space after item transafer", "Take your item:", true);
            }
        }
    }
    void SetPlayerColor(Player player) //to get other players colors, maybe as disguise
    {
        byte color1;
        byte color2;
        byte color3;

        switch (player.PlayerID)
        {
            case "player1":
                color1 = 195;
                color2 = 18;
                color3 = 18;

                break;


            case "player2":
                color1 = 41;
                color2 = 20;
                color3 = 172;
                break;

            case "player3":
                color1 = 196;
                color2 = 107;
                color3 = 28;
                break;


            case "player4":
                color1 = 196;
                color2 = 28;
                color3 = 186;
                break;


            case "player5":
                color1 = 18;
                color2 = 208;
                color3 = 197;
                break;

            case "player6":
                color1 = 86;
                color2 = 174;
                color3 = 249;
                break;

            case "player7":
                color1 = 253;
                color2 = 95;
                color3 = 169;
                break;


            case "player8":
                color1 = 199;
                color2 = 255;
                color3 = 91;
                break;


            case "player9":
                color1 = 130;
                color2 = 127;
                color3 = 57;
                break;

            case "neutral": //was 189 189 189
                color1 = 149;
                color2 = 149;
                color3 = 149;
                break;

            default:
                color1 = 189;
                color2 = 189;
                color3 = 189;
                break;
        }

        player.Color1 = color1;
        player.Color2 = color2;
        player.Color3 = color3;

        if (color1 >= 5) {
            int color = Convert.ToInt32(color1);
            color = (int)(color / (float)1.5);

            player.DarkColor1 = (byte)color;
        }
        if (color2 >= 5)
        {
            int color = Convert.ToInt32(color2);
            color = (int)(color / (float)1.5);

            player.DarkColor2 = (byte)color;
        }
        if (color3 >= 5)
        {
            int color = Convert.ToInt32(color3);
            color = (int)(color / (float)1.5);

            player.DarkColor3 = (byte)color;
        }

        if (color1 <= 170)
        {
            int color = Convert.ToInt32(color1);
            color = (int)(color * (float)1.5);

            player.LightColor1 = (byte)color;
        }
        else
        {
            player.LightColor1 = 255;
        }
        if (color2 <= 170)
        {
            int color = Convert.ToInt32(color2);
            color = (int)(color * (float)1.5);

            player.LightColor2 = (byte)color;
        }
        else
        {
            player.LightColor2 = 255;
        }
        if (color3 <= 170)
        {
            int color = Convert.ToInt32(color3);
            color = (int)(color * (float)1.5);

            player.LightColor3 = (byte)color;
        }
        else
        {
            player.LightColor3 = 255;
        }
    }
    /// <summary>
    /// when accepting a trade offer remove all other trade offers relating to this one
    /// </summary>
    /// <param name="shopItemID"></param>
    /// <param name="guildid"></param>
    /// <param name="playerID"></param>
    public void RemoveSameItemNotifications(int shopItemID, int guildid, string playerID, bool refreshUI)
    {
        Player player = FindPlayerByID(playerID);
        List<Notification> toRemove = new List<Notification>();
        foreach (Notification notification in player.Notifications)
        {
            if (notification.Type == Notification.NotificationType.TYPE_TRADE_OFFER)
            {
                if (notification.TargetID == guildid && notification.TargetID2 == shopItemID)
                {
                    toRemove.Add(notification);
                }
            }
        }
        foreach (Notification notification in toRemove)
        {
            player.Notifications.Remove(notification);
        }

        if (refreshUI)
        {
            GameObject controller = GameEngine.ActiveGame.FindPlayerControllerGameObject(playerID);
            if (controller != null)
            {
                PlayerController playerController = controller.GetComponent<PlayerController>();
                playerController.RefreshUI();
            }
        }

    }
    public void RemovePendingNotification(string playerID, int targetID, int targetID2)
    {

    }
    public void AcceptTradeOffer(ShopItem shopItem, Bid bid, int guildID, bool sendToMP) //sendToMP is false if this is called from multiplayer to prevent loop
    {
        Debug.Log("setting " + bid.PlayerID + " bid as accepted");
        bid.IsAccepted = true;

        GiveOfferedItemsToSeller(shopItem, bid, guildID);


        RemoveSameItemNotifications(shopItem.ID, guildID, shopItem.SourcePlayerID, sendToMP);
        if (sendToMP)
        {
            MultiplayerMessage acceptTradeOffer = new MultiplayerMessage(MultiplayerMessage.AcceptTradeOffer, shopItem.SourcePlayerID, shopItem.ID + "*" + guildID + "*" + bid.PlayerID);
            GameEngine.ActiveGame.clientManager.Push(acceptTradeOffer);
        }

        if (sendToMP)
        {
            GameObject controller = GameEngine.ActiveGame.FindPlayerControllerGameObject(shopItem.SourcePlayerID);
            if (controller != null)
            {
                PlayerController playerController = controller.GetComponent<PlayerController>();
                playerController.RefreshUI();
            }
        }

    }
    /// <summary>
    /// if theres a deal, we exchange items
    /// </summary>
    public void ProcessTradeOffersPhase()
    {
        bool debug = true;
        foreach (MerchantGuild guild in Guilds)
        {
            List<ShopItem> itemsToRemove = new List<ShopItem>();
            foreach (ShopItem shopItem in guild.ItemsUpForTrade)
            {

                Bid acceptedTrade = shopItem.GetWinnerBid();

                if (acceptedTrade == null)
                {
                    Player player = FindPlayerByID(shopItem.SourcePlayerID);
                    if (player != null)
                    {
                        foreach (Bid bid in shopItem.Bids)
                        {
                            Notification notification = new Notification();
                            notification.ID = ++player.LocalNotificationID;
                            notification.Type = Notification.NotificationType.TYPE_TRADE_OFFER;
                            notification.PlayerID = bid.PlayerID;
                            notification.TargetID = guild.ID;
                            notification.TargetID2 = shopItem.ID;
                            notification.HeaderText = "Trade offer";
                            notification.ExpandedText = "Trade offer";

                            //NotificationElement buffer1 = new NotificationElement();
                            //buffer1.Content = "Your item: ";
                            //notification.NotificationElements.Add(buffer1);

                            //NotificationElement yourItem = new NotificationElement();
                            //yourItem.Picture = shopItem.GetPicture;
                            //yourItem.IsShopItem = true;
                            //yourItem.Source = new SourceInfo(SourceInfo.MODE_SHOP_TRADE_ITEM);
                            //yourItem.Source.GuildID = guild.ID;
                            //yourItem.Source.ShopItemID = shopItem.ID;
                            //yourItem.Content = shopItem.Item.TemplateKeyword + " x" + shopItem.StackQuantity;
                            //notification.NotificationElements.Add(yourItem);


                            //NotificationElement buffer2 = new NotificationElement();
                            //buffer2.Content = "For: ";
                            //notification.NotificationElements.Add(buffer2);

                            //foreach (Item item in bid.BidItems)
                            //{
                            //    NotificationElement element = new NotificationElement();
                            //    element.Picture = item.GetPictureString();
                            //    element.ItemID = item.ID;
                            //    element.Content = item.TemplateKeyword + " x" + item.Quantity;
                            //    element.Source = new SourceInfo(SourceInfo.MODE_TRADE_STASH);
                            //    element.Source.ShopItemID = shopItem.ID;
                            //    element.Source.Quantity = 1;
                            //    element.Source.PlayerID = bid.PlayerID;
                            //    element.Source.GuildID = guild.ID;
                            //    notification.NotificationElements.Add(element);
                            //    //notification.ExpandedText += "\n" + item.TemplateKeyword + " x" + item.Quantity;
                            //}
                            player.Notifications.Add(notification);
                        }
                    }
                }
                else
                {

                    if (shopItem.Item != null)
                    {
                        shopItem.Item.Quantity = shopItem.StackQuantity;
                    }
                    foreach (Bid bid in shopItem.Bids)
                    {
                        Notification notification = new Notification();

                        if (bid == acceptedTrade)
                        {
                            TransactionItem transactionItem = CreateTransactionItem(Notification.NotificationType.TYPE_TRADE_OFFER_ACCEPTED, guild.ID, guild.Name, bid.PlayerID, "");
                            if (shopItem.Item != null)
                            {
                                transactionItem.ItemsToRecieve.Add(shopItem.Item);
                            }
                            else
                            {
                                transactionItem.EntitiesToRecieve.Add(shopItem.Entity);
                            }

                            transactionItem.ItemsYouPaid.AddRange(bid.BidItems);


                            notification.Type = Notification.NotificationType.TYPE_TRADE_OFFER_ACCEPTED;
                            notification.HeaderText = "Your trade offer was accepted";

                            guild.TransactionItems.Add(transactionItem);
                            notification.TargetID2 = transactionItem.ID;
                        }
                        else
                        {
                            TransactionItem transactionItem = CreateTransactionItem(Notification.NotificationType.TYPE_TRADE_OFFER_DECLINED, guild.ID, guild.Name, bid.PlayerID, "");
                            //these are fresh bids
                            transactionItem.ItemsToRecieve.AddRange(bid.BidItems);
                            transactionItem.AddToPaid(shopItem.GetObject());
                            transactionItem.OtherPartyID = shopItem.SourcePlayerID;



                            notification.Type = Notification.NotificationType.TYPE_TRADE_OFFER_DECLINED;
                            notification.HeaderText = "Your trade offer was declined";

                            guild.TransactionItems.Add(transactionItem);
                            notification.TargetID2 = transactionItem.ID;
                        }

                        notification.TargetID = guild.ID;

                        Player player = GameEngine.ActiveGame.scenario.FindPlayerByID(bid.PlayerID);
                        if (player != null)
                        {

                            notification.ID = ++player.LocalNotificationID;
                            player.Notifications.Add(notification);
                        }
                    }




                    shopItem.Bids.Clear();
                    if (debug)
                    {
                        Debug.Log("d");
                    }
                    //shopItem.Bids.Remove(acceptedTrade);

                    //Player buyer = FindPlayerByID(acceptedTrade.PlayerID);
                    //shopItem.ObtainShopItem(shopItem.StackQuantity,buyer);
                    //we dont do this anymore due to updated system see concepts for trade
                    //Player seller = FindPlayerByID(shopItem.SourcePlayerID);
                    //if (seller != null)
                    //{
                    //    seller.OwnedItems.AddRangeItems(acceptedTrade.BidItems);
                    //}
                    //else
                    //{
                    //   // guild.
                    //}

                    itemsToRemove.Add(shopItem);
                }




            }
            foreach (ShopItem toRemove in itemsToRemove)
            {
                guild.ItemsUpForTrade.Remove(toRemove);
            }
        }
    }

    public void NotifyPlayersAboutTheirItemsThatAreInGuildThatNeedToBeTakenPhase()
    {
        foreach (MerchantGuild guild in Guilds)
        {
            foreach (TransactionItem transactionItem in guild.TransactionItems)
            {
                if (transactionItem.PlayerID == "")
                {
                    foreach (Item item in transactionItem.ItemsToRecieve)
                    {
                        guild.CreateStockItem(item, ""); //phase, not using local player ids
                    }
                    foreach (Entity entity in transactionItem.EntitiesToRecieve)
                    {
                        guild.CreateStockItem(entity, "");   //phase, not using local player ids
                    }
                    continue;
                }

                Player playerToNotify = FindPlayerByID(transactionItem.PlayerID);
                Notification notification = new Notification();
                notification.ID = ++playerToNotify.LocalNotificationID;
                notification.Type = transactionItem.NotificationType;
                notification.HeaderText = transactionItem.NotificationHeader;
                notification.ExpandedText = "Take the item";
                notification.TargetID = guild.ID;
                notification.TargetID2 = transactionItem.ID;
                playerToNotify.Notifications.Add(notification);
            }
        }
    }
    /// <summary>
    /// if army is performing specific missions(like capturing & razing buildings) then they will be revealed
    /// </summary>
    void RevealArmiesPerformingMissions()
    {

        foreach (Army army in GetAllArmies())
        {
            foreach (Entity ent in army.Units)
            {
                if (ent.UnitID == army.LeaderID)
                {
                    if (ent.Mission == null)
                    {
                        army.RevealedDueToArmyActions = false;
                    }
                    else
                    {
                        switch (ent.Mission.MissionName)
                        {
                            case Mission.mission_Capture:
                            case Mission.mission_Raze:
                                army.RevealedDueToArmyActions = true;

                                GameSquare gameSquare = Worldmap.FindGameSquareByID(ent.Mission.TargetID);
                                //if capturing/razing building, army is revealed and garrison(if it exists) attacks the enemy
                                if (gameSquare.building.GarissonArmyID != -1 && army.OwnerPlayerID != gameSquare.building.OwnerPlayerID)
                                {
                                    Army garrison = FindOverlandArmy(gameSquare.building.GarissonArmyID);
                                    if (garrison != null)
                                    {
                                        if (!garrison.IsInHostileList(army.ArmyID, BattleParticipant.MODE_ARMY))
                                        {
                                            garrison.ArmiesYouIntentAttackIds.Add(new HostilityTarget(BattleParticipant.MODE_ARMY, army.ArmyID));
                                        }
                                    }
                                }
                                break;
                            default:
                                army.RevealedDueToArmyActions = false;
                                break;
                        }
                    }
                }
            }
        }

        //updating memorymap to see the revealed armies(needed for processing)
        foreach (Player player in Players)
        {
            GameEngine.ActiveGame.UpdateMemoryMapWithActiveVision(player);
        }
    }
    /// <param name="transactionItem"></param>
    /// <param name="player"></param>
    public void AcceptPendingItem(TransactionItem transactionItem, Player player, bool refreshUI)
    {
        if (!player.OwnedItems.HasSpaceToTakeItems(transactionItem.ItemsToRecieve, true))
        {
            Debug.LogError("player's inv doesnt have enough space");
            return;
        }

        foreach (Item item in transactionItem.ItemsToRecieve)
        {
            player.OwnedItems.AddItem(item);
        }
        foreach (Entity entity in transactionItem.EntitiesToRecieve)
        {

            entity.UnitID = ++player.LocalEntityIDCounter;

            if (entity.IsHeroFlag)
            {

                entity.OwnerIDs.Add(player.PlayerID);
                Army army = new Army(++player.LocalArmyIDCounter, player);
                army.OwnerPlayerID = player.PlayerID;
                army.Units.Add(entity);
                army.LeaderID = entity.UnitID;
                army.Location = new Location(player.CapitalLocation.XCoordinate, player.CapitalLocation.YCoordinate);
                lock (GameEngine.ActiveGame.scenario.Armies)
                {
                    GameEngine.ActiveGame.scenario.Armies.Add(army);
                }
                
                player.SetBudgets();

            }
            else
            {
                Debug.LogError("unimplemented case for non hero entities!!!!");
            }
        }

        if (transactionItem.GuildID != -1)
        {
            MerchantGuild guild = Guilds.FindGuildByID(transactionItem.GuildID);
            guild.RemoveTransactionItemByID(transactionItem.ID);
        }
        else
        {
            player.ExtraItems.Remove((ExtraItem)transactionItem);
        }


        if (refreshUI)
        {
            GameObject controller = GameEngine.ActiveGame.FindPlayerControllerGameObject(transactionItem.PlayerID);
            if (controller != null)
            {
                PlayerController playerController = controller.GetComponent<PlayerController>();
                playerController.RefreshUI();
            }
        }


    }

    /// <summary>
    /// search doesnt include completed battles
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public BattlefieldOld GetBattlefieldByArmyID(int id)
    {


        lock (this.QueuedUpBattles.Battlefields)
        {
            int battleID = QueuedUpBattles.GetBattlefieldIDbyArmy(id);
            foreach (BattlefieldOld battlefield in this.QueuedUpBattles.Battlefields)
            {
                if (battlefield.ID == battleID)
                {
                    return battlefield;
                }
            }

        }
        lock (this.ActiveBattles.Battlefields)
        {
            int battleID = ActiveBattles.GetBattlefieldIDbyArmy(id);
            foreach (BattlefieldOld battlefield in this.ActiveBattles.Battlefields)
            {
                if (battlefield.ID == battleID)
                {
                    return battlefield;
                }
            }
        }
        lock (this.BattlesToBeContinued.Battlefields)
        {
            int battleID = BattlesToBeContinued.GetBattlefieldIDbyArmy(id);
            foreach (BattlefieldOld battlefield in this.BattlesToBeContinued.Battlefields)
            {
                if (battlefield.ID == battleID)
                {
                    return battlefield;
                }
            }
        }
        //lock (this.CompletedBattles.Battlefields)
        //{
        //    int battleID = CompletedBattles.GetBattlefieldIDbyArmy(id);
        //    foreach (BattlefieldOld battlefield in this.CompletedBattles.Battlefields)
        //    {
        //        if (battlefield.ID == battleID)
        //        {
        //            return battlefield;
        //        }
        //    }
        //}
        return null;
    }

    /// <summary>
    /// checks ActiveBattles, queued up battles, completed battles and so on
    /// </summary>
    /// <param name="id"></param>
    public BattlefieldOld FindBattleByID(int id)
    {
        lock (this.CompletedBattles.Battlefields)
        {
            foreach (BattlefieldOld battlefield in this.CompletedBattles.Battlefields)
            {
                if (battlefield.ID == id)
                {
                    return battlefield;
                }
            }
        }
        lock (this.QueuedUpBattles.Battlefields)
        {
            foreach (BattlefieldOld battlefield in this.QueuedUpBattles.Battlefields)
            {
                if (battlefield.ID == id)
                {
                    return battlefield;
                }
            }
        }
        lock (this.ActiveBattles.Battlefields)
        {
            foreach (BattlefieldOld battlefield in this.ActiveBattles.Battlefields)
            {
                if (battlefield.ID == id)
                {
                    return battlefield;
                }
            }
        }
        lock (this.BattlesToBeContinued.Battlefields)
        {
            foreach (BattlefieldOld battlefield in this.BattlesToBeContinued.Battlefields)
            {
                if (battlefield.ID == id)
                {
                    return battlefield;
                }
            }
        }

        return null;
    }
    public void GiveOfferedItemsToSeller(ShopItem shopItem, Bid acceptedTrade, int guildID)
    {
        //Bid acceptedTrade = shopItem.GetWinnerBid();
        shopItem.OwnerAcceptedTradeOffer = true;
        //shopItem.Bids.Remove(acceptedTrade);

        Player seller = FindPlayerByID(shopItem.SourcePlayerID);
        if (seller != null)
        {
            if (seller.OwnedItems.HasSpaceToTakeItems(acceptedTrade.BidItems, true))
            {
                seller.OwnedItems.AddRangeItems(acceptedTrade.BidItems);
            }
            else
            {
                MerchantGuild guild = Guilds.FindGuildByID(guildID);

                TransactionItem transactionItem = CreateTransactionItem(Notification.NotificationType.TYPE_TRADE_OFFER_ACCEPTED_BUT_INVENTORY_FULL, guildID, guild.Name, seller.PlayerID, "");

                transactionItem.ItemsToRecieve.AddRange(acceptedTrade.BidItems);
                transactionItem.AddToPaid(shopItem.GetObject());


                Notification notification = new Notification();
                notification.Type = Notification.NotificationType.TYPE_TRADE_OFFER_ACCEPTED_BUT_INVENTORY_FULL;
                notification.HeaderText = "Insufficient space in inventory to claim your items";
                notification.TargetID = guildID;
                notification.TargetID2 = transactionItem.ID;

                notification.ID = ++seller.LocalNotificationID;
                seller.Notifications.Add(notification);

                guild.TransactionItems.Add(transactionItem);
            }

        }
        else
        {
            // guild.
        }
    }

    /// <summary>
    /// we return all bids and the item when turns expire
    /// </summary>
    public void ProcessTradingItemsPhase()
    {
        bool debug = false;
        foreach (MerchantGuild guild in Guilds)
        {
            List<ShopItem> itemsToRemove = new List<ShopItem>();



            foreach (ShopItem shopItem in guild.ItemsUpForTrade)
            {

                if (shopItem.OwnerAcceptedTradeOffer)
                {
                    continue;
                }

                if (shopItem.AuctionTurnCounter <= 0)
                {
                    if (debug)
                    {
                        Debug.Log("returning trade offer item " + shopItem.GetTemplate() + " to: " + shopItem.SourcePlayerID + " of shop " + guild.TemplateKeyword + " auction count: " + shopItem.AuctionTurnCounter);
                    }

                    if (shopItem.SourcePlayerID == "")//means belongs to a guild !!! does ever happen in AI??????
                    {
                        itemsToRemove.Add(shopItem);
                        guild.StockItems.Add(shopItem);
                    }
                    else
                    {
                        Player ownerPlayer = FindPlayerByID(shopItem.SourcePlayerID);
                        if (ownerPlayer == null)
                        {
                            Debug.LogError("no player found with id: " + shopItem.SourcePlayerID);

                        }
                        if (shopItem.Item != null)
                        {
                            shopItem.Item.Quantity = shopItem.StackQuantity;
                        }

                        TransactionItem transactionItem = CreateTransactionItem(Notification.NotificationType.TYPE_TRADE_OFFER_EXPIRED, guild.ID, guild.Name, shopItem.SourcePlayerID, "");

                        transactionItem.AddToRecieve(shopItem.GetObject());


                        //no notifcations here bc end of turn


                        guild.TransactionItems.Add(transactionItem);


                        //shopItem.ObtainShopItem(shopItem.StackQuantity, ownerPlayer);
                        itemsToRemove.Add(shopItem);
                    }

                }
                //foreach (Bid bid in shopItem.Bids)
                //{
                //    if (!bid.IsAccepted && !bid.IsFresh)
                //    {

                //    }
                //}

                foreach (Bid bid in shopItem.Bids)
                {
                    if (!bid.IsAccepted && !bid.IsFresh)
                    {
                        TransactionItem transactionItem = CreateTransactionItem(Notification.NotificationType.TYPE_TRADE_OFFER_DECLINED, guild.ID, guild.Name, bid.PlayerID, shopItem.SourcePlayerID);
                        transactionItem.ItemsToRecieve.AddRange(bid.BidItems);
                        transactionItem.AddToPaid(shopItem.GetObject());

                        guild.TransactionItems.Add(transactionItem);
                    }

                }
                shopItem.RemoveDeclinedBids(); //at the end of the turn all not-accepted
                foreach (Bid bid in shopItem.Bids)
                {
                    bid.IsFresh = false;
                    bid.BidItems.CompressInventory(1); //wrong?
                }
                shopItem.AuctionTurnCounter--;
            }


            foreach (ShopItem inBufferPeriod in guild.TradeItemsToBeProcessed)
            {
                inBufferPeriod.IsInAuctionBufferPeriod = false;
                guild.ItemsUpForTrade.Add(inBufferPeriod);
                Debug.Log("TRADE: adding items to guild.ItemsUpForTrade: " + inBufferPeriod.Item.Name + " x" + inBufferPeriod.StackQuantity + " " + inBufferPeriod.Item.ID);
            }
            guild.TradeItemsToBeProcessed.Clear();
            foreach (ShopItem toRemove in itemsToRemove)
            {
                guild.ItemsUpForTrade.Remove(toRemove);
            }
        }
    }

    public void ProcessAuctionItemsPhase()
    {
        bool debug = false;
        foreach (MerchantGuild guild in Guilds)
        {
            List<ShopItem> itemsToRemove = new List<ShopItem>();



            foreach (var shopItem in guild.BidItems)
            {
                if (shopItem.Bids.Count <= 1)
                {
                    shopItem.IsInAuctionBufferPeriod = false;
                }
                else
                {
                    shopItem.IsInAuctionBufferPeriod = true;
                }
                if (shopItem.Item != null)
                {
                    shopItem.Item.Quantity = shopItem.StackQuantity;
                }
                if (shopItem.AuctionTurnCounter <= 0)
                {
                    //todo: check for new bids
                    if (shopItem.Bids.Count > 1) //exclude the players bet
                    {

                        Bid winnerBid = null;
                        int max = 0;
                        List<Bid> toRemove = new List<Bid>();

                        foreach (Bid bid in shopItem.Bids)
                        {
                            if (bid.Amount >= max)
                            {
                                max = bid.Amount;
                                winnerBid = bid;
                            }
                        }
                        shopItem.Bids.Remove(winnerBid);
                        if (debug)
                        {
                            Debug.Log("auction winner: " + winnerBid.PlayerID);
                        }
                        if (shopItem.Item != null)
                        {
                            shopItem.Item.Quantity = shopItem.StackQuantity;
                        }

                        if (winnerBid.PlayerID != "")
                        {
                            TransactionItem transactionItem = CreateTransactionItem(Notification.NotificationType.TYPE_AUCTION_WIN, guild.ID, guild.Name, winnerBid.PlayerID, shopItem.SourcePlayerID);

                            transactionItem.AddToRecieve(shopItem.GetObject());
                            transactionItem.ItemsYouPaid.AddRange(winnerBid.BidItems);



                            transactionItem.NotificationType = Notification.NotificationType.TYPE_AUCTION_WIN;
                            //no notifcations here bc end of turn


                            guild.TransactionItems.Add(transactionItem);
                            //shopItem.ObtainShopItem(shopItem.StackQuantity, FindPlayerByID(winnerBid.PlayerID));
                            itemsToRemove.Add(shopItem);
                        }

                        TransactionItem transactionItem1 = CreateTransactionItem(Notification.NotificationType.TYPE_AUCTION_FINISH, guild.ID, guild.Name, shopItem.SourcePlayerID, winnerBid.PlayerID);

                        transactionItem1.ItemsToRecieve.AddRange(winnerBid.BidItems);
                        transactionItem1.AddToPaid(shopItem.GetObject());

                        guild.TransactionItems.Add(transactionItem1);

                        foreach (Bid bid in shopItem.Bids)
                        {
                            //we skip bc those are empty bids
                            if (bid.PlayerID == "")
                            {
                                continue;
                            }
                            if (bid.PlayerID == shopItem.SourcePlayerID)
                            {
                                continue;
                            }
                            TransactionItem transactionItem = CreateTransactionItem(Notification.NotificationType.TYPE_AUCTION_LOOSE, guild.ID, guild.Name, bid.PlayerID, shopItem.SourcePlayerID);

                            transactionItem.ItemsToRecieve.AddRange(bid.BidItems);
                            transactionItem.AddToPaid(shopItem.GetObject());



                            //no notifcations here bc end of turn


                            guild.TransactionItems.Add(transactionItem);
                        }
                        shopItem.ReturnBidItems();
                        //foreach (Bid bid in shopItem.Bids)
                        //{
                        //    if (bid.PlayerID == "")
                        //    {
                        //        continue;
                        //    }
                        //    if (bid.PlayerID == shopItem.SourcePlayerID)
                        //    {
                        //        continue;
                        //    }
                        //    FindPlayerByID(bid.PlayerID).OwnedItems.AddRangeItems(bid.BidItems);
                        //    //FindPlayerByID(bid.PlayerID).OwnedItems.AddRangeItems(bid.BidItems.GetAndRemoveItemsByKeyword(bid.CurrencyItemKeyword,bid.Amount));
                        //}
                        //shopItem.Bids.Clear();
                    }
                    else
                    {
                        if (debug)
                        {
                            Debug.Log("returning auction item " + shopItem.GetTemplate() + " to: " + shopItem.SourcePlayerID + " of shop " + guild.TemplateKeyword + " auction count: " + shopItem.AuctionTurnCounter);
                        }

                        if (shopItem.SourcePlayerID == "")//means belongs to a guild
                        {
                            itemsToRemove.Add(shopItem);
                            guild.StockItems.Add(shopItem);
                        }
                        else
                        {
                            //Player ownerPlayer = FindPlayerByID(shopItem.SourcePlayerID);
                            //if (ownerPlayer == null)
                            //{
                            //    Debug.LogError("no player found with id: " + shopItem.SourcePlayerID);

                            //}

                            TransactionItem transactionItem = CreateTransactionItem(Notification.NotificationType.TYPE_AUCTION_EXPIRED, guild.ID, guild.Name, shopItem.SourcePlayerID, "");
                            transactionItem.AddToRecieve(shopItem.GetObject());


                            guild.TransactionItems.Add(transactionItem);
                            // shopItem.ObtainShopItem(shopItem.StackQuantity, ownerPlayer);
                            itemsToRemove.Add(shopItem);
                        }

                    }

                }

                shopItem.AuctionTurnCounter--;
            }

            foreach (var newBidItem in guild.AuctionItemsToBeProcessed)
            {
                newBidItem.IsInAuctionBufferPeriod = true;
                guild.BidItems.Add(newBidItem);
            }
            guild.AuctionItemsToBeProcessed.Clear();

            foreach (var item in itemsToRemove)
            {
                guild.BidItems.Remove(item);
            }
        }
    }


    public Shop FindShopByGuildAndID(int guildID, int shopID)
    {
        foreach (var item in Players)
        {
            foreach (var item2 in item.Shops)
            {
                if (item2.ID == shopID && item2.GuildID == guildID)
                {
                    return item2;
                }
            }
        }
        return null;
    }
    public TransactionItem FindTransactionItemByID(int itemID)
    {
        foreach (MerchantGuild guild in Guilds)
        {
            TransactionItem item = FindTransactionItemByID(guild.ID, itemID);
            if (item != null)
            {
                return item;
            }
        }

        return null;
    }
    public TransactionItem FindTransactionItemByID(int guildID, int itemID)
    {
        MerchantGuild guild = Guilds.FindGuildByID(guildID);
        return guild.FindTransactionItemByID(itemID);
    }
    public bool SetItemForTrade(string playerID, Item item, Shop host, int quantity, int turnLength)
    {
        MerchantGuild hostGuild = Guilds.FindGuildByID(host.GuildID);


        Player player = FindPlayerByID(playerID);
        GuildToPlayerRelation relation = hostGuild.FindRelationByPlayerID(playerID);
        //fee check

        ShopItem fee = new ShopItem(false, "", playerID);
        fee.PriceMaterials.Add(new Stat(ItemTemplateCollection.IMPERIAL_COIN, relation.TradeFee));

        if (fee.PriceCheck(1, player.OwnedItems, null))
        {
            Debug.Log(relation.TradeFee + " Imperial coins were paid as a trade fee");
            fee.removeCosts(1, player);


            ShopItem shopItem = new ShopItem(true, ShopItem.MODE_TRADE, playerID);
            shopItem.StackQuantity = quantity;
            shopItem.AuctionTurnCounter = turnLength;
            shopItem.Item = item;
            //shopItem.Item.Quantity = quantity;
            shopItem.SourcePlayerID = playerID;
            shopItem.IsInAuctionBufferPeriod = true;
            lock (hostGuild.TradeItemsToBeProcessed)
            {
                hostGuild.TradeItemsToBeProcessed.Add(shopItem);
            }


            player.OwnedItems.DecreaseItemQuantity(item.TemplateKeyword, quantity, item.ID);



            return true; //fee paid
        }
        else
        {

            Debug.Log("insufficient imperial coins to pay the trading fee");
            return false;
        }

    }

    public bool AuctionItem(string playerID, Item item, Shop buyer, int quantity, Stat price, int turnLenght)
    {

        MerchantGuild hostGuild = Guilds.FindGuildByID(buyer.GuildID);


        Player player = FindPlayerByID(playerID);

        GuildToPlayerRelation relation = hostGuild.FindRelationByPlayerID(player.PlayerID);


        ShopItem fee = new ShopItem(false, "", playerID);
        fee.PriceMaterials.Add(new Stat(ItemTemplateCollection.IMPERIAL_COIN, relation.AuctionFee));

        if (fee.PriceCheck(1, player.OwnedItems, null))
        {
            Debug.Log(relation.TradeFee + " Imperial coins were paid as auction fee");
            fee.removeCosts(1, player);



            ShopItem shopItem = new ShopItem(true, ShopItem.MODE_AUCTION, playerID);
            Bid bid = new Bid();
            bid.PlayerID = playerID;
            bid.Amount = (int)price.Amount;
            bid.CurrencyItemKeyword = price.Keyword;
            shopItem.Bids.Add(bid);
            shopItem.StackQuantity = quantity;
            shopItem.SourcePlayerID = playerID;
            shopItem.Item = item;
            //shopItem.Item.Quantity = quantity; //not getting saved/gets lost?, using stackquantity instead
            shopItem.AuctionTurnCounter = turnLenght;
            shopItem.PriceMaterials.Add(price);
            lock (hostGuild.AuctionItemsToBeProcessed)
            {
                hostGuild.AuctionItemsToBeProcessed.Add(shopItem);
            }

            Debug.Log("auction item: " + shopItem.Item.TemplateKeyword + " x" + shopItem.Item.Quantity);
            player.OwnedItems.DecreaseItemQuantity(item.TemplateKeyword, quantity, item.ID);



            return true; //fee paid
        }
        else
        {

            Debug.Log("insufficient imperial coins to pay the auction fee: " + relation.AuctionFee);
            return false;
        }




    }

    /// <summary>
    /// might need more improvement
    /// </summary>
    public void RemoveManaCrystals()
    {
        bool timer = true;
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Reset();
            GameEngine.ActiveGame.GameStopwatch.Start();
        }

        foreach (Player player in Players)
        {
            if (player.Defeated)
            {
                continue;
            }
            List<int> toRemove = new List<int>();
            foreach (Item item in player.OwnedItems)
            {

                ItemTemplate itemTemplate = GameEngine.Data.ItemTemplateCollection.findByKeyword(item.TemplateKeyword);

                if (itemTemplate.Types.Contains(ItemTemplate.TYPE_MANA))
                {
                    // Debug.Log("itm: " + item.TemplateKeyword + " " + item.ID);
                    toRemove.Add(item.ID);
                }
            }

            foreach (var item in toRemove)
            {
                player.OwnedItems.RemoveItemByID(item);
            }
        }

        Army entities = this.GetAllUnitsInTheGame(true,true);

        foreach (Entity ent in entities.Units)
        {
            List<int> toRemove = new List<int>();
            foreach (Item item in ent.BackPack)
            {
                if (item == null)
                {
                    Debug.LogError("null item in entity: " + ent.CharacterTemplateKeyword + " " + ent.UnitID + " " + ent.FindCurrentOwnerID());
                    Debug.LogError("backpack info: " + ent.BackPack.ReportInventory(ItemCollection.REPORT_MODE_EMPTY));
                }
                ItemTemplate itemTemplate = GameEngine.Data.ItemTemplateCollection.findByKeyword(item.TemplateKeyword);

                if (itemTemplate.Types.Contains(ItemTemplate.TYPE_MANA))
                {
                    toRemove.Add(item.ID);
                }
            }

            foreach (var item in toRemove)
            {
                ent.BackPack.RemoveItemByID(item);
            }



        }

        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Stop();
            Debug.Log("Scenario.RemoveManaCrystals took " + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
        }
        //maybe do shops as well or ban selling mana crystals

    }

    /// <summary>
    /// player sells to shop
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="item"></param>
    /// <param name="buyer"></param>
    /// <param name="quantity"></param>
    /// <returns></returns>
    public bool SellItem(string playerID, Item item, Shop buyer, int quantity)
    {
        ShopItem shopItem = new ShopItem(true, "", playerID);
        Stat itemPrice = new Stat();
        MerchantGuild buyerGuild = Guilds.FindGuildByID(buyer.GuildID);

        itemPrice.Keyword = ItemTemplateCollection.IMPERIAL_COIN;
        itemPrice.Amount = buyerGuild.GetPrice(item.Quantity, GameEngine.Data.ItemTemplateCollection.findByKeyword(item.TemplateKeyword).Value, playerID);
        shopItem.Item = item;
        shopItem.PriceMaterials.Add(itemPrice);



        Player player = FindPlayerByID(playerID);
        List<Item> items = new List<Item>();
        foreach (var i in buyerGuild.StockItems)
        {
            if (i.Item != null)
            {
                items.Add(i.Item.ReturnDeepClone(true));
            }

        }
        if (shopItem.PriceCheck(1, items, null))
        {
            Debug.Log("d1: " + shopItem.ID + " price: " + itemPrice.Amount + " item: " + itemPrice.Keyword);
            ItemCollection transaction = buyer.RemoveGuildAndShopCosts(shopItem, buyerGuild, buyer);

            foreach (var itdem in transaction)
            {
                Debug.Log(itdem.TemplateKeyword + " " + itdem.Quantity);
            }
            //give items to player
            player.OwnedItems.AddRangeItems(transaction);
            player.OwnedItems.RemoveItemByID(item.ID);
            buyerGuild.CreateStockItem(item, player.PlayerID);
            //player.OwnedItems.DecreaseItemQuantity(item.TemplateKeyword, quantity, item.ID);
            Debug.Log("transaction complete");
            return true;
        }
        else
        {
            Debug.Log("transaction failed");
            GameEngine.ActiveGame.FindPlayerControllerGameObject(playerID).GetComponent<PlayerController>().DisplayMessage("The shop could'nt afford to buy your item", Color.red);
            //credit?????
            return false;
        }

    }
    /// <summary>
    /// AI will manage the rest of player's assets, and will do nothing, just ending turns
    /// clears out things, such as overland armies quests and events 
    /// </summary>
    public void SetPlayerAsDefeated(string playerID)
    {
        //not doing neutral check here, as this is not final process?
        Player player = FindPlayerByID(playerID);
        player.Defeated = true;
        player.isAI = true;
        player.ActiveQuests.Clear();
        player.EventChains.Clear();
        player.DelaydEvents.Clear();
        player.FutureQuests.Clear();
        #region removing event battlefields
        //removing all quest/event battlefields
        List<int> toRemove = new List<int>();
        lock (ActiveBattles.Battlefields)
        {
            
            foreach (BattlefieldOld battle in ActiveBattles.Battlefields)
            {
                if (battle.EventBattlePlayerID == playerID)
                {
                    toRemove.Add(battle.ID);
                }
            }

        }
        //removing here, as removebattlefieldbyid uses lock
        foreach (int battleID in toRemove)
        {
            ActiveBattles.RemoveBattlefieldByID(battleID);
        }
        toRemove = new List<int>();
        lock (QueuedUpBattles.Battlefields)
        {

            foreach (BattlefieldOld battle in QueuedUpBattles.Battlefields)
            {
                if (battle.EventBattlePlayerID == playerID)
                {
                    toRemove.Add(battle.ID);
                }
            }

        }
        //removing here, as removebattlefieldbyid uses lock
        foreach (int battleID in toRemove)
        {
            QueuedUpBattles.RemoveBattlefieldByID(battleID);
        }



        toRemove = new List<int>();
        lock (BattlesToBeContinued.Battlefields)
        {

            foreach (BattlefieldOld battle in BattlesToBeContinued.Battlefields)
            {
                if (battle.EventBattlePlayerID == playerID)
                {
                    toRemove.Add(battle.ID);
                }
            }

        }
        //removing here, as removebattlefieldbyid uses lock
        foreach (int battleID in toRemove)
        {
            BattlesToBeContinued.RemoveBattlefieldByID(battleID);
        }
        #endregion

        //unassigning from player setup
        PlayerSetup playerSetup = GetPlayerSetupByPlayerID(playerID);
        if (playerSetup.ComputerName != "")
        {
            DefeatedPlayerToClient.Add(new MyValue(playerID,playerSetup.ComputerName));
            playerSetup.ComputerName = "";
        }
        //setting buildings to belong to neutral
        List<Building> allBuildings = GetPlayerBuildings(playerID);
        foreach (Building building in allBuildings)
        {
            building.OwnerPlayerID = Player.Neutral;
        }
        //removing overland armies
        List<Army> overlandArmiesToRemove = new List<Army>();
        lock (Armies)
        {
            foreach (Army army in Armies)
            {
                //TODO: message & replay when army disappears? but the replays would be cleared? maybe clear before calling this method?
                if (army.OwnerPlayerID == playerID)
                {
                    overlandArmiesToRemove.Add(army);
                }
            }
            foreach (Army army in overlandArmiesToRemove)
            {
                foreach (Player otherPlayer in Players)
                {
                    if (otherPlayer.PlayerID == playerID || otherPlayer.Defeated)
                    {
                        continue;
                    }
                    MemoryArmy memoryArmy = otherPlayer.MapMemory.FindMemoryArmyByArmyIDVisible(army.ArmyID);
                    if (memoryArmy != null)
                    {
                        Notification notification = otherPlayer.GetNotificationByType(Notification.NotificationType.TYPE_ENEMY_ARMY_DISBANDED);
                        if (notification == null)
                        {
                            notification = new Notification();
                            notification.Type = Notification.NotificationType.TYPE_ENEMY_ARMY_DISBANDED;
                            notification.ID = ++otherPlayer.LocalNotificationID;
                            notification.DismissOnRightClick = true;
                            notification.Picture = "Poneti/Skills/Unsorted/skill_128";
                            notification.HeaderText = "Enemy armies are disbanding";
                            notification.ExpandedText = "Players were defeated, and armies were disbanded:";
                            notification.MapCoordinates = ObjectCopier.Clone(army.Location.WorldMapCoordinates);
                            otherPlayer.Notifications.Add(notification);
                        }
                        NotificationElement notificationElement = new NotificationElement();
                        notificationElement.Content = "Army lead by " + memoryArmy.CharacterTemplateKeyword + " of " + army.OwnerPlayerID;
                        notificationElement.AdditionalToolTipContent = "Click to move to location";
                        notificationElement.Picture = memoryArmy.PriorityUnitTileGraphics;
                        notificationElement.XCord = army.Location.WorldMapCoordinates.XCoordinate;
                        notificationElement.YCord = army.Location.WorldMapCoordinates.YCoordinate;
                        notificationElement.IsClickable = true;
                        notification.NotificationElements.Add(notificationElement);
                        CombatMessageInfo combatMessageInfo = new CombatMessageInfo(playerID + " disbanding", new List<string>(),255,255,255,new List<SkillParticle>(),notificationElement.XCord,notificationElement.YCord,0,0,null,false,false);

                        OverlandReplay replay = new OverlandReplay(otherPlayer.MapMemory);
                        replay.CombatMessages.Add(combatMessageInfo);
                        otherPlayer.MapMemory.AddReplay(replay);
                        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SHOW_OVERLAND_MESSAGE,combatMessageInfo,otherPlayer.PlayerID));

                    
                    }
                }
                Armies.Remove(army);
            }
        }

       
        //this is called in main phase, therefore player would be AI already
        //if (GameEngine.ActiveGame.isHost)
        //{
        //    switch (player.GameState.Keyword)
        //    {
        //        case GameState.State.MAIN_PHASE:
        //            //start AI which will end the turn
        //            break;
        //        case GameState.State.BATTLE_PHASE:
        //            //in all active battles, if the player is active, then start combat AI, which will try to flee the battle with minimal resistance
        //            break;
        //        default:
        //            break;
        //    }
        //}

        //what about shops && auctions?
    }

    internal BattlefieldOld FindBattlefieldByUnit(int unitID)
    {
        BattleManager pool = new BattleManager();
        pool.Battlefields.AddRange(QueuedUpBattles.Battlefields);
        pool.Battlefields.AddRange(BattlesToBeContinued.Battlefields);
        pool.Battlefields.AddRange(ActiveBattles.Battlefields);
        foreach (BattlefieldOld battlefield in pool.Battlefields)
        {
            foreach (Army army in battlefield.Armies)
            {
                foreach (Entity unit in army.Units)
                {
                    if (unit.UnitID == unitID)
                    {
                        return battlefield;
                    }
                }
            }
        }
        return null; 
    }
    internal Entity FindUnitInBattlefield(int unitID)
    {
        BattleManager pool = new BattleManager();
        pool.Battlefields.AddRange(QueuedUpBattles.Battlefields);
        pool.Battlefields.AddRange(BattlesToBeContinued.Battlefields);
        pool.Battlefields.AddRange(ActiveBattles.Battlefields);
        foreach (BattlefieldOld battlefield in pool.Battlefields)
        {
            foreach (Army army in battlefield.Armies)
            {
                foreach (Entity unit in army.Units)
                {
                    if (unit.UnitID == unitID)
                    {
                        return unit;
                    }
                }
            }
        }
        return null;
    }

    public void AutoEquipItems()
    {
        Army allEntities = GetAllUnitsInTheGame(false, false);

        foreach (Entity entity in allEntities.Units)
        {
            entity.AutoEquipItems();
        }
    }


    /// <summary>
    /// you pay for the item but its not put into your inventory but should be managed by item transfer
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="shopItem"></param>
    /// <param name="guildID"></param>
    /// <param name="shopID"></param>
    /// <returns></returns>
    public Item BuyItemTransfer(string playerID, ShopItem shopItem, int guildID, int shopID)
    {
        Player player = FindPlayerByID(playerID);
        int amount = 1;

        Shop shop = GameEngine.ActiveGame.scenario.FindShopByGuildAndID(guildID, shopID);

        if (shopItem.PriceCheck(amount, player.OwnedItems, null))
        {
            //if (shopItem.Item != null)
            //{
            //    if (!player.OwnedItems.HasSpaceToTakeItems(new List<Item> { shopItem.Item }))
            //    {
            //        GameEngine.ActiveGame.DisplayMessageToPlayer("Not enough Inventory space");
            //    }
            //}
          
            //if (shopItem.Item != null)
            //{
            //    shopItem.Item.Quantity = shopItem.StackQuantity;
            //}
            shopItem.removeCosts(amount, player);
            Item item = shopItem.ExtractItem(amount,player);
            if (shopItem.StackQuantity == 0)
            {
                shop.RemoveShopItemByID(shopItem.ID);
            }

            Debug.Log("Buy transfer success,item amount: " + amount);

            return item;

        }
        GameEngine.ActiveGame.FindPlayerControllerGameObject(playerID).GetComponent<PlayerController>().DisplayMessage("Insufficient funds",Color.red);
        return null;
    }

    /// <summary>
    /// returns string for UI
    /// player buys from shop
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    public BuyStatus BuyItem(string playerID, ShopItem shopItem, int guildID, int shopID)
    {
        Player player = FindPlayerByID(playerID);
        int amount = 1;
        BuyStatus buyStatus = new BuyStatus();
        Shop shop = GameEngine.ActiveGame.scenario.FindShopByGuildAndID(guildID, shopID);
      
        if (shopItem.PriceCheck(amount, player.OwnedItems, null))
        {
            //if (shopItem.Item != null)
            //{
            //    shopItem.Item.Quantity = shopItem.StackQuantity;
            //}
            shopItem.removeCosts(amount, player);
            bool good = true;
            if (shopItem.Item != null)
            {
                List<Item> list = new List<Item>();
                list.Add(shopItem.Item);
                if (!player.OwnedItems.HasSpaceToTakeItems(list,true))
                {
                    good = false;
                }
            }
            if (good)
            {
                if (shopItem.ObtainShopItem(amount, player))
                {
                    buyStatus.status = "success decrease";
                }
                else
                {
                    buyStatus.status = "success remove";
                    shop.RemoveShopItemByID(shopItem.ID);
                }
            }
            else
            {
                MerchantGuild guild = GameEngine.ActiveGame.scenario.Guilds.FindGuildByID(guildID);
                //guild.RemoveShopItemByID(shopItem.ID,1);
                buyStatus.status = "success remove";
                shop.RemoveShopItemByID(shopItem.ID);

                TransactionItem transactionItem = CreateTransactionItem(Notification.NotificationType.TYPE_BOUGHT_ITEM_BUT_INVENTORY_FULL,guildID,guild.Name,playerID,"");
      
                transactionItem.AddToRecieve(shopItem.Item);

                lock (guild.TransactionItems)
                {
                    guild.TransactionItems.Add(transactionItem);
                }
               

                Notification notification = new Notification();
                notification.Type = Notification.NotificationType.TYPE_BOUGHT_ITEM_BUT_INVENTORY_FULL;
                notification.HeaderText = "Not enough space in inventory when bought item";
                notification.TargetID = guild.ID;
                notification.TargetID2 = transactionItem.ID;

                notification.ID = ++player.LocalNotificationID;
                lock (player.Notifications) //unnecessary lock??
                {
                    player.Notifications.Add(notification);
                }
               
            }

            Debug.Log("Buy success, status: "+ buyStatus.status );
            buyStatus.shopItem = shopItem;
            return buyStatus;

        }

        buyStatus.status = "insufficient funds";
        return buyStatus;
    }
    public int ReturnValidatedBid(string playerID, ShopItem shopItem)
    {
        Bid lastBid = null;
        foreach (var bidd in shopItem.Bids)
        {
            if (bidd.PlayerID == playerID)
            {
                lastBid = bidd;
            }
        }
        //Bid lastBid = shopItem.Bids[shopItem.Bids.Count - 1];
        if (lastBid == null)
        {
            lastBid = shopItem.Bids[shopItem.Bids.Count - 1];

        }
        int bid = lastBid.Amount;

        if (lastBid.PlayerID != "")
        {

            if (lastBid.PlayerID != playerID)
            {
                bid += 1;
            }

        }

        Debug.Log("last bid: " + lastBid.Amount + " new bid " + bid.ToString());
        return bid;
    }
    /// <summary>
    /// creates transaction item with necessary variables and text
    /// the actual items you get & visual items you paid for are to be added manually
    /// </summary>
    /// <param name="mode">necessary</param>
    /// <param name="guildID">necessary</param>
    /// <param name="playerID">necessary</param>
    /// <param name="otherPlayerID">prefers not to be empty</param>
    /// <returns></returns>
    public TransactionItem CreateTransactionItem(string mode,int guildID,string guildName, string playerID, string otherPlayerID)
    {
        TransactionItem transactionItem = new TransactionItem(true,playerID);
        transactionItem.NotificationType = mode;
        transactionItem.GuildID = guildID;
        transactionItem.PlayerID = playerID;
        transactionItem.OtherPartyID = otherPlayerID;


        switch (transactionItem.NotificationType)
        {
            case Notification.NotificationType.TYPE_AUCTION_EXPIRED:
           
                transactionItem.NotificationHeader = "Your auction has expired in " + guildName;
                transactionItem.Message = "Your auction has expired in " + guildName;
                transactionItem.YouPaidLabelText = "";
                transactionItem.ToRecieveLabelText = "You are returned: ";
                break;
            case Notification.NotificationType.TYPE_AUCTION_FINISH:
                transactionItem.NotificationHeader = "Your auction in " + guildName + " ended successfully";

                transactionItem.Message = "Your auction in " + guildName + " ended successfully";

                transactionItem.ToRecieveLabelText = "You recieved: ";
                transactionItem.YouPaidLabelText = "For: ";
                break;
            case Notification.NotificationType.TYPE_AUCTION_LOOSE:
                transactionItem.NotificationHeader = "You were outbidded in an auction in " + guildName;
                transactionItem.Message = "You were outbidded in an auction in " + guildName;
          
                transactionItem.ToRecieveLabelText = "The items you get back: ";
                transactionItem.YouPaidLabelText = "Were for: ";
                
                break;
            case Notification.NotificationType.TYPE_AUCTION_WIN:
                transactionItem.NotificationHeader = "You have won an auction in " + guildName;
                transactionItem.Message = "You have won an auction in " + guildName;

                transactionItem.ToRecieveLabelText = "The item you get: ";
                transactionItem.YouPaidLabelText = "You paid: ";
              
             
                break;
            case Notification.NotificationType.TYPE_TRADE_OFFER_ACCEPTED:
                transactionItem.NotificationHeader = "Your trade offer was accepted";
               
                transactionItem.Message = "Your trade offer that went through " + guildName + " was accepted";

                transactionItem.ToRecieveLabelText = "The items you get: ";
                transactionItem.YouPaidLabelText = "For: ";                     
                break;
            case Notification.NotificationType.TYPE_TRADE_OFFER_DECLINED:
                transactionItem.NotificationHeader = "Your trade offer was declined";
                transactionItem.Message = "Your trade offer you sent into " + guildName + " was declined";
                transactionItem.ToRecieveLabelText = "The items you get back: ";
                transactionItem.YouPaidLabelText = "Were for: ";

                break;
            case Notification.NotificationType.TYPE_TRADE_OFFER_ACCEPTED_BUT_INVENTORY_FULL:
                transactionItem.NotificationHeader = "You haven't taken your items yet";
                transactionItem.Message = "Take your items please";

                transactionItem.ToRecieveLabelText = "The items you get: ";
                transactionItem.YouPaidLabelText = "Were for: ";
                break;
            case Notification.NotificationType.TYPE_TRADE_OFFER_EXPIRED:
                transactionItem.ToRecieveLabelText = "The item you get back: ";
                transactionItem.YouPaidLabelText = "";
                transactionItem.Message = "Your trade item in " + guildName + " has it's turns expired";
                transactionItem.NotificationHeader = "Your trade item turn's have expired";
                break;
            case Notification.NotificationType.TYPE_BOUGHT_ITEM_BUT_INVENTORY_FULL:
                transactionItem.NotificationHeader = "Take the item you bought";
                transactionItem.Message = "You didn't have enough inventory space at the time of buying in " + guildName;
                transactionItem.ToRecieveLabelText = "Take the item";
                break;
            default:
                break;
        }

        return transactionItem;
    }


    /// <summary>
    /// returns true if bid is succesful
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="shopItem"></param>
    /// <param name="newbid"></param>
    /// <returns></returns>
    public bool BidItem(string playerID, ShopItem shopItem, int newbid)
    {
        Player player = FindPlayerByID(playerID);

        //Bid lastBid = shopItem.Bids[shopItem.Bids.Count - 1];

        //int bid = lastBid.Amount;

        //if (lastBid.PlayerID != "")
        //{

        //    if (!lastBid.PlayerID.Equals(playerID))
        //    {
        //        bid += 1;
        //    }

        //}


        lock (shopItem.Bids)
        {
            Bid lastBid = shopItem.Bids[shopItem.Bids.Count - 1];

            Debug.Log("last bid: " + lastBid.Amount + " new bid " + newbid.ToString());

            int playerOldBid = 0;
            Bid existingBid = null;
            foreach (Bid item in shopItem.Bids)
            {
                if (item.PlayerID == playerID)
                {
                    Debug.Log("phase 0.5");
                    existingBid = item;
                    break;
                }
            }

            if (existingBid != null)
            {
                playerOldBid = existingBid.Amount;
                Debug.Log("old existing bid: " + playerOldBid);

                if (newbid > playerOldBid) //cannot bid less than existing
                {

                    int difference = newbid - playerOldBid;
                    if (player.OwnedItems.GetSameItemAmount(lastBid.CurrencyItemKeyword) >= difference)
                    {
                        existingBid.Amount += difference;
                        //shopItem.removeCosts(difference, player, null);
                        existingBid.BidItems.AddRangeItems(player.OwnedItems.GetAndRemoveItemsByKeyword(existingBid.CurrencyItemKeyword, difference, playerID));

                        return true;
                    }
                    return false; //not enough items to fulfill
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (newbid >= lastBid.Amount)
                {
                    // 200 + 600 = 2000
                    Debug.Log("phase 1");
                    //if (playerOldBid + player.OwnedItems.GetSameItemAmount(lastBid.CurrencyItemKeyword) < newbid)
                    if (player.OwnedItems.GetSameItemAmount(lastBid.CurrencyItemKeyword) >= newbid)
                    {
                        Debug.Log("phase 2: " + player.OwnedItems.GetSameItemAmount(lastBid.CurrencyItemKeyword) + " " + lastBid.CurrencyItemKeyword);


                        // newbid = playerOldBid + player.OwnedItems.GetSameItemAmount(lastBid.CurrencyItemKeyword);


                        //if (existingBid != null)
                        //{
                        //    shopItem.Bids.Remove(existingBid);
                        //}

                        Debug.Log("phase 3");
                        Bid bid = new Bid();
                        bid.Amount = newbid;
                        bid.CurrencyItemKeyword = lastBid.CurrencyItemKeyword;
                        bid.PlayerID = playerID;
                        Debug.Log("new bid: " + newbid);
                        bid.BidItems.AddRangeItems(player.OwnedItems.GetAndRemoveItemsByKeyword(bid.CurrencyItemKeyword, bid.Amount, playerID));
                        foreach (var biditm in bid.BidItems)
                        {
                            Debug.Log("1111111111111 returning bid item: " + biditm.TemplateKeyword + " " + biditm.Quantity);
                        }
                        shopItem.Bids.Add(bid);
                        Debug.Log("bids count: " + shopItem.Bids.Count);

                        //bidtextBox.Text = newbid.ToString();
                        return true;


                    }
                    else
                    {
                        return false;
                    }





                }
                else
                {
                    return false;
                }
            }






        }












        //if (bidtextBox.TextColor == Color.White)
        //{
        //    acceptBidButton.Visible = true;
        //}
    }







    public void CreateBeginningShop(Player player)
    {

        foreach (MerchantGuild guild in Guilds)
        {
            if (player.StartingGuilds.Contains(guild.TemplateKeyword))
            {
                Shop newshop = Shop.CreateNewShop(guild.ID, "shop",player.PlayerID);

                player.Shops.Add(newshop);
            }
      
        }
 

    }

    public void CreateNewRelation(Player player, MerchantGuild guild)
    {
        GuildToPlayerRelation guildToPlayerRelation = new GuildToPlayerRelation();
        guildToPlayerRelation.PlayerID = player.PlayerID;

        MerchantGuildTemplate guildTemplate = GameEngine.Data.MerchantGuildTemplateCollection.findByKeyword(guild.TemplateKeyword);

        guildToPlayerRelation.AuctionFee = guildTemplate.AuctionFee;
        guildToPlayerRelation.ItemQuantityPerShop = guildTemplate.MaxItemQuantityPerShop;
        guildToPlayerRelation.SellPriceModifier = guildTemplate.PlayerSellPriceModifier;
        guildToPlayerRelation.TradeFee = guildTemplate.TradeFee;

        guild.GuildToPlayerRelations.Add(guildToPlayerRelation);

    }

    /// <summary>
    /// creates the relations from guilds to players with values from template
    /// </summary>
    public void GenerateRelations()
    {
       
        foreach (MerchantGuild guild in Guilds)
        {
            foreach (Player player in Players)
            {
                CreateNewRelation(player, guild);
            }
        }
    }

    public void GenerateMerchantGuilds()
    {


        foreach (MerchantGuildTemplate currentTemplate in GameEngine.Data.MerchantGuildTemplateCollection.DataList)
        {
            MerchantGuild newGuild = MerchantGuild.GetMerchantGuildFromTemplate(currentTemplate.Keyword);
            //newGuild.Name = currentTemplate.Keyword;
            //newGuild.ID = ++MerchantGuildIdCounter;

            this.Guilds.Add(newGuild);
        }

    }



    public bool Stealth
    {
        get { return Boolean.Parse(this.OptionList.findByNameWithType(OptionCollection.Stealth, "condition")); }
    }

    public bool ActionPointsAffectMoves
    {
        get { return Boolean.Parse(this.OptionList.findByNameWithType(OptionCollection.Action_Points_Affect_Moves, "condition")); }
    }

    public bool ShowPlayerStanding
    {
        get { return Boolean.Parse(this.OptionList.findByNameWithType(OptionCollection.Show_Player_Standing, "condition")); }
    }

    public List<Army> GetAllPlayersArmiesOnCoordinates(string playerID, int xCord, int yCord)
    {
        List<Army> playerArmies = new List<Army>();

        lock (Armies)
        {
            foreach (Army army in Armies)
            {

                if (army.Location.WorldMapCoordinates.XCoordinate == xCord && army.Location.WorldMapCoordinates.YCoordinate == yCord && army.OwnerPlayerID == playerID)
                {
                    playerArmies.Add(army);
                }
            }
        }



        lock (QueuedUpBattles.Battlefields)
        {
            foreach (BattlefieldOld battlefield in QueuedUpBattles.Battlefields)
            {
                if (battlefield.Mode != BattlefieldOld.MODE_OVERLAND)
                {
                    continue;
                }
                foreach (Army army in battlefield.Armies)
                {
                    if (army.Location.WorldMapCoordinates.XCoordinate == xCord && army.Location.WorldMapCoordinates.YCoordinate == yCord && army.OwnerPlayerID == playerID)
                    {
                        playerArmies.Add(army);
                    }
                }
            }
        }


        lock (BattlesToBeContinued.Battlefields)
        {
            foreach (BattlefieldOld battlefield in BattlesToBeContinued.Battlefields)
            {
                if (battlefield.Mode != BattlefieldOld.MODE_OVERLAND)
                {
                    continue;
                }
                foreach (Army army in battlefield.Armies)
                {
                    if (army.Location.WorldMapCoordinates.XCoordinate == xCord && army.Location.WorldMapCoordinates.YCoordinate == yCord && army.OwnerPlayerID == playerID)
                    {
                        playerArmies.Add(army);
                    }
                }
            }
        }


        lock (ActiveBattles.Battlefields)
        {
            foreach (BattlefieldOld battlefield in ActiveBattles.Battlefields)
            {
                if (battlefield.Mode != BattlefieldOld.MODE_OVERLAND)
                {
                    continue;
                }
                foreach (Army army in battlefield.Armies)
                {
                    if (army.Location.WorldMapCoordinates.XCoordinate == xCord && army.Location.WorldMapCoordinates.YCoordinate == yCord && army.OwnerPlayerID == playerID)
                    {
                        playerArmies.Add(army);
                    }
                }
            }
        }

        return playerArmies;
    }

    public MemoryTile FindMemoryTileByCoordinates(string playerID, int xCord, int yCord)
    {
        Player thisPlayer = FindPlayerByID(playerID);
        foreach (MemoryTile memoryTile in thisPlayer.MapMemory)
        {
            if (memoryTile.Coord_X == xCord && memoryTile.Coord_Y == yCord)
            {
                return memoryTile;
            }
        }
        return null;
    }

 


    public OptionList OptionList { get => optionList; set => optionList = value; }
  
 

    public Army GetFirstAvalibleArmyOfPlayer(string playerID, bool heroprefered) {

        Army answer = null;

        foreach (Army army in armies)
        {
            if (army.OwnerPlayerID == playerID)
            {
                if (answer == null) {
                    answer = army;
                }

                if (heroprefered) {
                    
                    foreach (Entity entity in army.Units)
                    {
                        if (entity.IsHeroFlag)
                        {
                            return army;
                        }

                    }


                }

            }
        }
        return answer;
    }

    public void ArmyIntegrityCheckPhase()
    {
        List<int> existing = new List<int>();
        foreach (Army army in Armies)
        {
            if (existing.Contains(army.ArmyID))
            {
                Debug.LogError("repeating id: " + army.ArmyID);
            }
            else
            {
                existing.Add(army.ArmyID);
            }
          
        }

        foreach (Building building in Worldmap.GetAllBuildings())
        {
            if (building.Storage != null)
            {
                if (existing.Contains(building.Storage.ArmyID))
                {
                    Debug.LogError("repeating storage id: " + building.Storage.ArmyID);
                }
                else
                {
                    existing.Add(building.Storage.ArmyID);
                }
            }
        }

    }

    public void RemoveBattleZone(int battlefieldID)
    {
        foreach (GameSquare gamesqr in Worldmap.GameSquares)
        {
            if (gamesqr.BattleFieldID == battlefieldID)
            {
                gamesqr.BattleFieldID = -1;
            }
        }
    }

    /// <summary>
    /// set true to restore, false to restrict non battle UI
    /// </summary>
    /// <param name="onOff"></param>
    //public void ToggleNonBattleUI(bool onOff)
    //{
    //    //GameEngine.ActiveGame.turnCounterPanel.SetActive(onOff);
    //    //GameEngine.ActiveGame.unitPanel.SetActive(onOff);
    //    //GameEngine.ActiveGame.nextTurnBtn.gameObject.SetActive(onOff);
    //    //GameEngine.ActiveGame.playerheromanagmentpanel.SetActive(onOff);
    //    //GameEngine.ActiveGame.nextUnitBtn.gameObject.SetActive(onOff);

    //}

    public Army CreateStartingHeroArmy(Player player, GameSquare startingLocation, StartingArmyRequest request) {
        string mode = request.UpkeepMode;
        //if (player.PlayerID == "neutral")
        //{
        //    mode = UpKeep.MODE_NONE;
        //}
        //else
        //{
        //    mode = UpKeep.MODE_NORMAL;
        //}
        Army army = null;
        if (request.PartyRequestKeyword == "")
        {
            army = new Army(++armyIdCounter, player);
        }
        else
        {
            army = Army.GenerateArmyFromPartyRequest(GameEngine.Data.PartyRequestCollection.findByKeyword(request.PartyRequestKeyword), null, mode, player.PlayerID, GameEngine.random);
        }
     
       
        army.SetColor(player);
        //Army army = new Army(++armyIdCounter, player);
        army.OwnerPlayerID = player.PlayerID;
        army.Location = new Location(startingLocation.X_cord, startingLocation.Y_cord);

        //Entity hero = Entity.GetRandomMobByValueAndTypes(new List<string>() { CharacterTemplate.TYPE_HERO }, new List<string>(), 0, Int32.MaxValue, new List<string>());
        bool rollRandom = false;
        if (UsedHeroKeywords.Contains(request.HeroOdd.TemplateKeyword) || request.HeroOdd.TemplateKeyword == "")
        {
            rollRandom = true;
        }
        Entity hero = null;
        if (rollRandom)
        {
            hero = Entity.GetRandomMobByValueAndTypes(request.HeroOdd.Types, request.HeroOdd.NotWantedTypes, request.HeroOdd.MinValue, request.HeroOdd.MaxValue, this.UsedHeroKeywords, GameEngine.random,player.PlayerID);

        }
        else
        {
            hero = Entity.CreateTemplateChar(request.HeroOdd.TemplateKeyword, GameEngine.random, player.PlayerID);
        }
        //Entity hero = Entity.GetRandomMobByValueAndTypes(new List<string>() { CharacterTemplate.TYPE_HERO }, new List<string>(), 0, Int32.MaxValue, this.UsedHeroKeywords);
       
        if (hero != null)
        {
            this.UsedHeroKeywords.Add(hero.CharacterTemplateKeyword);

            //hero.UpKeep.UpkeepType = UpKeep.UpkeepFromPlayer;


            hero.AddToAttitude(player.PlayerID, 100, 100);
            hero.OwnerIDs.Add(player.PlayerID);

            if (startingLocation.building != null)
            {

                if (request.HeroOwnsTheBuilding)
                {
                    startingLocation.building.OwnerHeroID = hero.UnitID;
                }

            }

            army.LeaderID = hero.UnitID;
            army.Units.Add(hero);




            army.AssignRandomUnitAsLeader(GameEngine.random);


            Armies.Add(army);

            return army;
        }
        else {
            Debug.Log("Run out of Heroes, all heroes already rolled into game, request: " + request.HeroOdd.GetInformation());
            return null;
        }
      
    }
    
    public void CreateStartingArmies(Player player,List<StartingArmyRequest> armyRequests)
    {
        GameSquare gameSquare;
        #region old
        //foreach (Player player in players)
        //{


        //    //int numberOfStartingHeroes = 1;

        //    //for (int currentHero = 1; currentHero <= numberOfStartingHeroes; currentHero++) {

        //    //    bool assignBuildingControl = false;

        //    //    if (currentHero == 1) {
        //    //        assignBuildingControl = true;



        //    //    }

        //    //    Army startingHeroArmy = CreateStartingHeroArmy(player, gameSquare, assignBuildingControl);

        //    //    if (startingHeroArmy != null)
        //    //    {
        //    //        for (int i = 0; i < 4; i++)
        //    //        {
        //    //            Entity soldier = Entity.CreateTemplateChar("Imp");
        //    //            soldier.UpKeep.UpkeepType = UpKeep.NoUpKeep;
        //    //            startingHeroArmy.Units.Add(soldier);
        //    //        }
        //    //        //if (currentHero == 1)
        //    //        //{


        //    //        //    for (int i = 0; i < 2; i++)
        //    //        //    {
        //    //        //        Entity soldier = Entity.CreateTemplateChar("Night Demon");
        //    //        //        soldier.UpKeep.UpkeepType = UpKeep.NoUpKeep;
        //    //        //        startingHeroArmy.Units.Add(soldier);
        //    //        //    }

        //    //        //}

        //    //    }

        //    //}

        //    //Army garisson = homeCastle.GetGarisson();
        //    //if (garisson == null)
        //    //{
        //    //    garisson = CreateOverlandArmy(player.PlayerID,homeCastle.ID,gameSquare.X_cord,gameSquare.Y_cord);
        //    //}
        //    //for (int i = 0; i < 6; i++)
        //    //{
        //    //    Entity ent = Entity.GetRandomMobByValueAndTypes(new List<string>() { }, new List<string>() { CharacterTemplate.TYPE_HERO }, Int32.MinValue, Int32.MaxValue, new List<string>());
        //    //    ent.UpKeep.UpkeepType = UpKeep.NoUpKeep;

        //    //    garisson.Units.Add(ent);


        //    //}
        //    //garisson.AssignRandomUnitAsLeader();



        //    //armyProduction.UnitName = Entity.UnitName_Spearman;

        //    //homeCastle.ArmyProductions.Add(armyProduction);

        //    //armyProduction = new BuildingProduction();



        //    //armyProduction.UnitName = "Demon";
        //    //armyProduction.AddToGarrison = true;

        //    //homeCastle.ArmyProductions.Add(armyProduction);
        //    //ArmyProduction armyProduction2 = new ArmyProduction();
        //    //armyProduction2.AmountToProduce = 12;
        //    //armyProduction2.MaximumAmount = 40;
        //    //armyProduction2.TurnsToProduce = 2;
        //    //armyProduction2.UnitName = Unit.UnitName_Demon;
        //    //armyProduction2.Progress = 0;
        //    //homeCastle.ArmyProductions.Add(armyProduction2);



        //    //create method for finding gamesquares by owner, and add 2 armies - active, and inactive

        //    /*
        //    for (int i = 0; i < 2; i++)
        //    {
        //        //Entity unit = Entity.UnitCreate(Entity.UnitName_Spearman, ++IdCounter);
        //        //Entity unit = Entity.CreateTemplateChar(Entity.UnitName_Spearman);
        //        //unit.UnitID = ++GameEngine.ActiveGame.scenario.IdCounter;
        //        //unit.UpKeep.UpkeepType = UpKeep.BuildingUpKeep;
        //        //unit.UpKeep.Reserves.Add(new GameProj.Entities.Stat(Player.TYPE_FOOD, 2));
        //        //unit.UpKeep.PlayerID = player.PlayerID;
        //        //unit.UpKeep.BuildingID = homeCastle.ID;

        //        //army.Units.Add(unit);
        //    }
        //    */







        //    //hero.UpKeep.Costs.Add(new GameProj.Entities.Stat(Player.GEM_RED, 5));
        //    //hero.UpKeep.Costs.Add(new GameProj.Entities.Stat(Player.GEM_BLUE, 9));


        //    //  hero.UpKeep.Reserves.Add(new GameProj.Entities.Stat(Player.GEM_RED, 10));





        //    //hero = Entity.HeroCreate(Entity.HeroName_Leader, ++IdCounter);
        //    //hero.UpKeep.Costs.Add(new TemplateOdd(Player.GEM_GREEN, 2));
        //    //hero.UpKeep.Reserves.Add(new GameProj.Entities.Stat(Player.GEM_GREEN, 4));

        //    //army.Mission = new Mission();
        //    // army.mission.missionName = Mission.mission_Hide;
        //    //hero.UpKeep.Costs.Add(new TemplateOdd(Player.GEM_GREEN, 2));
        //    /*
        //    army = new Army(armyIdCounter++, player);
        //    army.OwnerPlayerID = player.PlayerID;
        //    army.WorldMapPositionX = gameSquare.X_cord;
        //    army.WorldMapPositionY = gameSquare.Y_cord;
        //    army.Color1 = player.Color1;
        //    army.Color2 = player.Color2;
        //    army.Color3 = player.Color3;
        //    hero = Entity.getRandomMobByValueAndTypes(0, Int32.MaxValue, new List<string>(), new List<string>() { CharacterTemplate.TYPE_HERO }, new List<string>());
        //    hero.KnownBuildingKeywords.Add("Crypt");
        //    hero.KnownBuildingKeywords.Add("Castle");
        //    hero.KnownBuildingKeywords.Add("Water tower");
        //    hero.KnownBuildingKeywords.Add("Church");
        //    hero.UpKeep.UpkeepType = UpKeep.UpkeepFromPlayer;
        //    //hero.UpKeep.Costs.Add(new TemplateOdd(Player.GEM_GREEN, 2));
        //    hero.OwnerIDs.Add(player.PlayerID);
        //    hero.AddToAttitude(player.PlayerID, 114, 114);
        //    army.Units.Add(hero);
        //    army.LeaderID = hero.UnitID;
        //    armies.Add(army);

        //    army = new Army(armyIdCounter++, player);
        //    army.OwnerPlayerID = player.PlayerID;
        //    army.WorldMapPositionX = gameSquare.X_cord;
        //    army.WorldMapPositionY = gameSquare.Y_cord;
        //    army.Color1 = player.Color1;
        //    army.Color2 = player.Color2;
        //    army.Color3 = player.Color3;
        //    hero = Entity.getRandomMobByValueAndTypes(0, Int32.MaxValue, new List<string>(), new List<string>() { CharacterTemplate.TYPE_HERO }, new List<string>());
        //    hero.KnownBuildingKeywords.Add("Crypt");
        //    hero.KnownBuildingKeywords.Add("Castle");
        //    hero.KnownBuildingKeywords.Add("Water tower");
        //    hero.KnownBuildingKeywords.Add("Church");
        //    hero.UpKeep.UpkeepType = UpKeep.UpkeepFromPlayer;
        //    //hero.UpKeep.Costs.Add(new TemplateOdd(Player.GEM_GREEN, 2));
        //    hero.OwnerIDs.Add(player.PlayerID);
        //    hero.AddToAttitude(player.PlayerID, 115, 115);
        //    army.Units.Add(hero);
        //    army.LeaderID = hero.UnitID;
        //    armies.Add(army);

        //    army = new Army(armyIdCounter++, player);
        //    army.OwnerPlayerID = player.PlayerID;
        //    army.WorldMapPositionX = gameSquare.X_cord;
        //    army.WorldMapPositionY = gameSquare.Y_cord;
        //    army.Color1 = player.Color1;
        //    army.Color2 = player.Color2;
        //    army.Color3 = player.Color3;
        //    hero = Entity.getRandomMobByValueAndTypes(0, Int32.MaxValue, new List<string>(), new List<string>() { CharacterTemplate.TYPE_HERO }, new List<string>());
        //    hero.KnownBuildingKeywords.Add("Crypt");
        //    hero.KnownBuildingKeywords.Add("Castle");
        //    hero.KnownBuildingKeywords.Add("Water tower");
        //    hero.KnownBuildingKeywords.Add("Church");
        //    hero.UpKeep.UpkeepType = UpKeep.UpkeepFromPlayer;
        //    //hero.UpKeep.Costs.Add(new TemplateOdd(Player.GEM_GREEN, 2));
        //    hero.OwnerIDs.Add(player.PlayerID);
        //    hero.AddToAttitude(player.PlayerID, 225, 226);
        //    army.Units.Add(hero);
        //    army.LeaderID = hero.UnitID;
        //    armies.Add(army);
        //    */
        //}
        #endregion old
        gameSquare = worldmap.FindStartingGameSquareByOwner(player);
        if (gameSquare == null) {
            // starting coordinates
            Debug.LogError("no Starting position for " + player.PlayerID + " having starting army request of " + armyRequests.Count);
        }

        //Building homeCastle = gameSquare.building;

        //BuildingProduction armyProduction = new BuildingProduction(true);

        foreach (StartingArmyRequest startingArmy in armyRequests)
        {
           CreateStartingHeroArmy(player, gameSquare, startingArmy);
        
        }



    }
    public void RefreshMovementPoints()
    {
        bool timer = false;
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Reset();
            GameEngine.ActiveGame.GameStopwatch.Start();
        }
      
        foreach (Army army in armies)
        {
            foreach (Entity unit in army.Units)
            {
                unit.MovementRemaining = Math.Min(unit.MovementMax, unit.MovementMax + unit.MovementRemaining); //coe4 style
            }
        }
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Stop();
            Debug.Log("Scenario.RefreshMovementPoints took " + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
        }
    }

    public Location FindEntityLocation(int UnitId) {

        // Overland Armies
        foreach (Army army in armies)
        {
            foreach (Entity unit in army.Units)
            {
                if (unit.UnitID == UnitId) {

                    //Location location = new Location();
                    //location.Mode = Location.MODE_OVERLAND;
                    //location.WorldMapCoordinates = new MapCoordinates();
                    //location.WorldMapCoordinates.XCoordinate = army.Location.WorldMapCoordinates.XCoordinate;
                    //location.WorldMapCoordinates.YCoordinate = army.WorldMapPositionY;
                    return ObjectCopier.Clone(army.Location);
                }
            }

        }
        if (this.Worldmap != null)
        {
            //Buildings (Storage, Garrison)
            foreach (GameSquare gameSquare in this.Worldmap.GameSquares)
            {
                if (gameSquare.building != null)
                {
                    foreach (Entity storageunit in gameSquare.building.Storage.Units)
                    {
                        if (storageunit.UnitID == UnitId)
                        {

                            Location location = new Location();
                            location.Mode = Location.MODE_BUILDING_STORAGE;
                            location.WorldMapCoordinates = new MapCoordinates();
                            location.WorldMapCoordinates.XCoordinate = gameSquare.X_cord;
                            location.WorldMapCoordinates.YCoordinate = gameSquare.Y_cord;
                            return location;
                        }
                    }




                }
            }

        }

        // Quest Armies
        foreach (Player player in Players)
        {
            foreach (Quest quest in player.ActiveQuests)
            {
                foreach (QuestParty questParty in quest.Parties)
                {
                    foreach (Entity entity in questParty.Army.Units)
                    {
                        if (entity.UnitID == UnitId)
                        {                   
                            Location location = new Location();
                            location.Mode = Location.MODE_QUEST;
                            location.DungeonCoordinates = new QuestCoordinate();
                            location.DungeonCoordinates.ID = quest.ID;
                            location.DungeonCoordinates.XCoordinate = questParty.Progress;
                            return location;
                        }
                    }
                }
            }
        }



        return null;
    }
 

    public Army CreateOverlandArmy(string playerID,int buildingID, int xCord, int yCord)
    {

        Army newArmy = new Army(++ArmyIdCounter,FindPlayerByID(playerID));
        newArmy.OwnerPlayerID = playerID;
        if (buildingID == -1)
        {
            newArmy.Location = new Location(xCord, yCord);
        }
        else
        {
            newArmy.Location = new Location(buildingID, new MapCoordinates(xCord, yCord));
            Building building = FindBuildingByID(buildingID);
            building.GarissonArmyID = newArmy.ArmyID;
        }
       
        Armies.Add(newArmy);
        return newArmy;
    }

    public Army CreateStorageArmy(string playerID, int buildingID)
    {

        Army newArmy = new Army(++ArmyIdCounter, FindPlayerByID(playerID));
        newArmy.OwnerPlayerID = playerID;
        newArmy.Location = new Location(buildingID, null);

        return newArmy;
    }


    public Entity FindUnitByUnitID(int unitID)
    {
        Army allunitsingame = GetAllUnitsInTheGame(true, true);

        foreach (Entity unit in allunitsingame.Units)
        {
            if (unit.UnitID == unitID)
            {
                return unit;
            }
        }

        return null;
    }
 
    public Army FindOverlandArmyByUnit(int unitID)
    {
        lock (Armies)
        {
            foreach (Army army in Armies)
            {
                foreach (Entity unit in army.Units)
                {
                    if (unitID == unit.UnitID)
                    {
                        return army;
                    }
                }
            }
        }

      //  OurLog.Print("Scenario.FindArmyByUnit: couldn't find army with unit: ");
        return null;
    }


    public Army FindArmyByUnit(int unitID)
    {
        foreach (Army army in armies)
        {
            foreach (Entity unit in army.Units)
            {
                if (unitID == unit.UnitID)
                {
                    return army;
                }
            }
        }

        foreach (Player player in Players)
        {
            foreach (Quest quest in player.ActiveQuests)
            {
                foreach (QuestParty party in quest.Parties)
                {
                    foreach (Entity entity in party.Army.Units)
                    {
                        if (unitID == entity.UnitID)
                        {
                            return party.Army;
                        }
                    }
                }
            }
        }

        foreach (Building building in Worldmap.GetAllBuildings())
        {
            foreach (Entity entity in building.Storage.Units)
            {
                if (entity.UnitID == unitID)
                {
                    return building.Storage;
                }
            }
        }



        lock (QueuedUpBattles.Battlefields)
        {
            foreach (BattlefieldOld battlefieldOld in this.QueuedUpBattles.Battlefields)
            {
                foreach (Army army in battlefieldOld.Armies)
                {
                    foreach (Entity unit in army.Units)
                    {
                        if (unit.UnitID == unitID)
                        {
                            return army;
                        }
                    }
                }
            }
        }



        lock (BattlesToBeContinued.Battlefields)
        {
            foreach (BattlefieldOld battlefieldOld in this.BattlesToBeContinued.Battlefields)
            {
                foreach (Army army in battlefieldOld.Armies)
                {
                    foreach (Entity unit in army.Units)
                    {
                        if (unit.UnitID == unitID)
                        {
                            return army;
                        }
                    }
                }
            }
        }


        lock (ActiveBattles.Battlefields)
        {
            foreach (BattlefieldOld battlefieldOld in this.ActiveBattles.Battlefields)
            {
                foreach (Army army in battlefieldOld.Armies)
                {
                    foreach (Entity unit in army.Units)
                    {
                        if (unit.UnitID == unitID)
                        {
                            return army;
                        }
                    }
                }
            }
        }

        //  OurLog.Print("Scenario.FindArmyByUnit: couldn't find army with unit: ");
        return null;
    }


    public Army FindOverlandArmy(int armyID)
    {
        foreach (Army army in armies)
        {
            if (army.ArmyID == armyID)
            {
                return army;
            }
        }

        //List<Building> buildings = Worldmap.GetAllBuildings();

        //foreach (Building building in buildings)
        //{
        //    if (building.Garisson.ArmyID == armyID)
        //    {
        //        return building.Garisson;
        //    }

        //}

        return null;

    }

    public Army FindArmyInActiveQuests(int armyID) {
        foreach (Player player in Players)
        {
            foreach (Quest quest in player.ActiveQuests)
            {
                foreach (var item in quest.Parties)
                {
                    if (item.Army != null)
                    {
                        if (item.Army.ArmyID == armyID)
                        {
                            return item.Army;
                        }
                    }
                    
                }
            }
        }
        return null;
    }

    public Army FindStorageArmyByID(int armyID)
    {
        foreach (Building building in Worldmap.GetAllBuildings())
        {
            if (building.Storage != null)
            {
                if (building.Storage.ArmyID == armyID)
                {
                    return building.Storage;
                }
            }
        }
        return null;
    }

    Army FindArmyInBattlefields(int armyID)
    {
        lock (QueuedUpBattles.Battlefields)
        {
            foreach (BattlefieldOld battlefieldOld in this.QueuedUpBattles.Battlefields)
            {
                foreach (Army army in battlefieldOld.Armies)
                {
                    if (army.ArmyID == armyID)
                    {
                        return army;
                    }
                }
            }
        }



        lock (BattlesToBeContinued.Battlefields)
        {
            foreach (BattlefieldOld battlefieldOld in this.BattlesToBeContinued.Battlefields)
            {
                foreach (Army army in battlefieldOld.Armies)
                {
                    if (army.ArmyID == armyID)
                    {
                        return army;
                    }
                }
            }
        }


        lock (ActiveBattles.Battlefields)
        {
            foreach (BattlefieldOld battlefieldOld in this.ActiveBattles.Battlefields)
            {
                foreach (Army army in battlefieldOld.Armies)
                {
                    if (army.ArmyID == armyID)
                    {
                        return army;
                    }
                }
            }
        }
        return null;

    }

    public Army FindArmyByID(int armyID)
    {

        lock (Armies)
        {
            Army armyFound = FindOverlandArmy(armyID);

            if (armyFound != null)
            {
                return armyFound;
            }
            armyFound = FindArmyInActiveQuests(armyID);

            if (armyFound != null)
            {
                return armyFound;
            }



            armyFound = FindArmyInBattlefields(armyID);

            if (armyFound != null)
            {
                return armyFound;
            }

            armyFound = FindStorageArmyByID(armyID);
            if (armyFound != null)
            {
                return armyFound;
            }
        }


        return null;
    }


 


    public List<Army> FindAllOverlandArmiesByCoordinates(int coord_X, int coord_Y)
    {

        List<Army> answer = new List<Army>();
        lock (Armies)
        {
            foreach (Army army in armies)
            {
                if (army.Location.WorldMapCoordinates.XCoordinate == coord_X && army.Location.WorldMapCoordinates.YCoordinate == coord_Y)
                {
                    answer.Add(army);
                }

            }
        }
        

        lock (QueuedUpBattles.Battlefields)
        {
            foreach (BattlefieldOld battlefield in QueuedUpBattles.Battlefields)
            {
                if (battlefield.Mode != BattlefieldOld.MODE_OVERLAND)
                {
                    continue;
                }
                foreach (Army army in battlefield.Armies)
                {
                    if (army.Location.WorldMapCoordinates.XCoordinate == coord_X && army.Location.WorldMapCoordinates.YCoordinate == coord_Y)
                    {
                        answer.Add(army);
                    }
                }
            }
        }


        lock (BattlesToBeContinued.Battlefields)
        {
            foreach (BattlefieldOld battlefield in BattlesToBeContinued.Battlefields)
            {
                if (battlefield.Mode != BattlefieldOld.MODE_OVERLAND)
                {
                    continue;
                }
                foreach (Army army in battlefield.Armies)
                {
                    if (army.Location.WorldMapCoordinates.XCoordinate == coord_X && army.Location.WorldMapCoordinates.YCoordinate == coord_Y)
                    {
                        answer.Add(army);
                    }
                }
            }
        }


        lock (ActiveBattles.Battlefields)
        {
            foreach (BattlefieldOld battlefield in ActiveBattles.Battlefields)
            {
                if (battlefield.Mode != BattlefieldOld.MODE_OVERLAND)
                {
                    continue;
                }
                foreach (Army army in battlefield.Armies)
                {
                    if (army.Location.WorldMapCoordinates.XCoordinate == coord_X && army.Location.WorldMapCoordinates.YCoordinate == coord_Y)
                    {
                        answer.Add(army);
                    }
                }
            }
        }
        //Building building = Worldmap.FindMapSquareByCordinates(coord_X, coord_Y).building;
        //if (building != null)
        //{
        //    if (building.Garisson.Units.Count > 0)
        //    {
        //        answer.Add(building.Garisson);
        //    }
        //}
        return answer;

    }

    public List<Army> GetOverlandArmy(int coord_X, int coord_Y)
    {

        List<Army> answer = new List<Army>();
        foreach (Army army in armies)
        {
            if (army.Location.WorldMapCoordinates.XCoordinate == coord_X && army.Location.WorldMapCoordinates.YCoordinate == coord_Y)
            {
                answer.Add(army);
            }

        }

        lock (QueuedUpBattles.Battlefields)
        {
            foreach (BattlefieldOld battlefield in QueuedUpBattles.Battlefields)
            {
                if (battlefield.Mode != BattlefieldOld.MODE_OVERLAND)
                {
                    continue;
                }
                foreach (Army army in battlefield.Armies)
                {
                    if (army.Location.WorldMapCoordinates.XCoordinate == coord_X && army.Location.WorldMapCoordinates.YCoordinate == coord_Y)
                    {
                        answer.Add(army);
                    }
                }
            }
        }


        lock (BattlesToBeContinued.Battlefields)
        {
            foreach (BattlefieldOld battlefield in BattlesToBeContinued.Battlefields)
            {
                if (battlefield.Mode != BattlefieldOld.MODE_OVERLAND)
                {
                    continue;
                }
                foreach (Army army in battlefield.Armies)
                {
                    if (army.Location.WorldMapCoordinates.XCoordinate == coord_X && army.Location.WorldMapCoordinates.YCoordinate == coord_Y)
                    {
                        answer.Add(army);
                    }
                }
            }
        }


        lock (ActiveBattles.Battlefields)
        {
            foreach (BattlefieldOld battlefield in ActiveBattles.Battlefields)
            {
                if (battlefield.Mode != BattlefieldOld.MODE_OVERLAND)
                {
                    continue;
                }
                foreach (Army army in battlefield.Armies)
                {
                    if (army.Location.WorldMapCoordinates.XCoordinate == coord_X && army.Location.WorldMapCoordinates.YCoordinate == coord_Y)
                    {
                        answer.Add(army);
                    }
                }
            }
        }

        return answer;

    }

    public Player FindPlayerByID(string id)
    {
        foreach (Player player in players)
        {
            if (player.PlayerID == id)
            {
                return player;
            }
        }
        //   OurLog.Print("Scenario.FindPlayerByID: can't find player with id: " + id);
        return null;
    }
    public List<Entity> FindAllHeroesByPlayerID(string id)
    {
        List<Entity> heroes = new List<Entity>();
        //foreach (Army army in Armies)
        //{
        //    foreach (Entity unit in army.Units)
        //    {
        //        if (unit.OwnerIDs.Count > 0)
        //        {
        //            if (unit.OwnerIDs[unit.OwnerIDs.Count - 1] == id)
        //            {
        //                heroes.Add(unit);
        //            }
        //        }

        //    }
        //}
        Army allEntities = GetAllUnitsInTheGame(true, true);
        foreach (Entity unit in allEntities.Units)
        {
            if (unit.OwnerIDs.Count > 0)
            {
                if (unit.OwnerIDs[unit.OwnerIDs.Count - 1] == id)
                {
                    heroes.Add(unit);
                }
            }
        }
        return heroes;
    }
    void SaveToScoreboard()
    {
        foreach (Player player in Players)
        {
            if (player.PlayerID == Player.Neutral)
            {
                continue;
            }
            if (player.Defeated)
            {
                continue;
            }
            Scoreboard.SaveScore(Scoreboard.MILITARY_POWER,player.PlayerID,player.GetMilitaryPower(),Turncounter);
            Scoreboard.SaveScore(Scoreboard.BUILDINGS_AMOUNT,player.PlayerID,player.GetAllBuildingsCount(),Turncounter);
            Scoreboard.SaveScore(Scoreboard.WEALTH,player.PlayerID,player.GetAllItemsWorth(""),Turncounter);
            Scoreboard.SaveScore(Scoreboard.IMPERIAL_COIN, player.PlayerID,player.GetAllItemsWorth(ItemTemplateCollection.IMPERIAL_COIN),Turncounter);
        }
    }
    public void StartGlobalTurn(bool waitForPlayers)
    {
        Debug.Log("start global turn call");

        GameEngine.ActiveGame.ScenarioStopWatch.Reset();
        GameEngine.ActiveGame.ScenarioStopWatch.Start();
        try
        {
            Entity whiteshaman = this.FindUnitByUnitID(10005);
            if (whiteshaman != null) {
                Debug.Log("Whiteshaman " + whiteshaman.BackPack.ReportInventory());
            }

            turncounter++;
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Saving scoreboards"));
            SaveToScoreboard();
            //now doing it here
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Checking player death phase"));
            CheckPlayerDeathPhase();
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Checking Victory phase"));
            CheckVictoryPhase();
            if (Ended)
            {
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.COMMAND_REFRESH_UI));
                GameEngine.ActiveGame.clientManager.mode = ClientManager.MODE_IDLE; //we are done processing, so we do mp messages again
                return;
            }

            lock (PlayersWhoEndedTurn)
            {
                PlayersWhoEndedTurn.Clear();
            }
            //UnityEngine.Debug.Log("StartGlobalTurn before random iteration : seed " + GameEngine.random.Iteration + " " + GameEngine.random.Seed);
            if (waitForPlayers) //true if called from afterinstantiate, as we are turned to observers during EndGlobalTurn, which includes calling this from AfterBattles,
            {
                //HowLargeIsScenario("start of first turn");
               
                //HowLargeIsScenario("start of first turn x2");
                //HowLargeIsObject(Armies,"armies","start of first turn");
                //HowLargeIsObject(Players,"players","start of first turn");
                //HowLargeIsObject(Worldmap,"worldmap","start of first turn");
                //HowLargeIsEverythingPlayer(Players[0]);
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.COMMAND_TURN_INTO_OBSERVER));
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Waiting for players..."));
                
            }
            GameEngine.ActiveGame.clientManager.mode = ClientManager.MODE_PROCESSING; //prevent getting mp messages during processing, to avoid things changing during
            Debug.Log("start globla turn start");

            //    RemoveManaCrystals();
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Removing expired gamesquare events"));
            RemoveExpiredGameSquareEvents();
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Processing trading items"));
            ProcessTradingItemsPhase(); //moved from after battles
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Processing auction items"));
            ProcessAuctionItemsPhase(); //moved from after battles

            ResolveLootClaimsPhase();
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Refreshing movement points"));
            RefreshMovementPoints();
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Refreshing stats"));
            RefreshStatsPhase();
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Creating auctions"));
            CreateAuctionsPhase();
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Refreshing shop content"));
            RefreshShopContentPhase();
            //RecieveSupplyPhase();
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Army production phase"));
            ArmyProductionPhase(); //re enable after done debugging
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Producing player items"));
            PlayerItemProductionPhase();
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Sending supplies"));
            SendSuppliesPhase();
            //TODO: events here maybe?
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Sending armies to heroes"));
            SendArmiesToHeroesPhase();
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Building production phase"));
            AllocatePlannedProductionsPhase();
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Setting budgets"));
            SetBudgets(); //hero upkeep
                          //  ResolveAllPlayerEvents();
                          //  PayUnitUpkeeps();//pay hero upkeeps, unit upkeeps
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Paying upkeeps"));
            foreach (Player player in Players)
            {
                if (player.PlayerID == Player.Neutral)
                {
                    continue;
                }
                if (player.Defeated)
                {
                    continue;
                }
                PayUnitUpkeepsNEW(GetAllPlayerNonHungryUnits(player.PlayerID),player,true,"Upkeep results");
                //PayUnitUpkeepsNEW(GetAllPlayerHeroes(player.PlayerID),player,true,"Heroes upkeep results");
            }
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Removing unhappy units"));
            RemoveUnhappyUnits();
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Rolling events"));
            RollEventsPhase(); //moved from end turn into startglobalturn after removeunhappyunits to prevent event with party of entities that left(prevent crash)
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Notifying players about guild items"));
            NotifyPlayersAboutTheirItemsThatAreInGuildThatNeedToBeTakenPhase();
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Notifying players about extra items"));
            CreateNotifcationsForExtraItemsPhase();
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Processing trade offers"));
            ProcessTradeOffersPhase();

            AddPlayersToTurnQueue();


            // SetNextPlayerAsActive("StartGlobalTurn"); //multiplayer will change this
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Army integrity check"));
            ArmyIntegrityCheckPhase();

            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Updating vision"));
            //needed this for sake of replays if you have multiple playercontrollers
            foreach (Player player in Players)
            {
                if (player.Defeated)
                {
                    continue;
                }
                GameEngine.ActiveGame.UpdateMemoryMapWithActiveVision(player);
            }
            //SaveToScoreboard();
            Debug.Log("start globla turn end");
            //HowLargeIsEverythingPlayer(Players[0]);
            //HowLargeIsScenario("end of turn");
            //HowLargeIsObject(Armies, "armies", "end of turn");
            //HowLargeIsObject(Players, "players", "end of turn");
            //HowLargeIsObject(Worldmap, "worldmap", "end of turn");
            //UnityEngine.Debug.Log("StartGlobalTurn after : seed " + GameEngine.random.Iteration + " " + GameEngine.random.Seed);
            //MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.ReadyForStartGlobalTurn, GameEngine.PLAYER_IDENTITY, "");
            //GameEngine.ActiveGame.clientManager.Push(multiplayerMessage);
            GameEngine.ActiveGame.ScenarioStopWatch.Stop();
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SHOW_GRAY_PLAYER_MESSAGE,"Processing " + GameEngine.ActiveGame.ScenarioStopWatch.ElapsedMilliseconds + " ms"));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SHOW_GRAY_PLAYER_MESSAGE,"Event phase"));
          
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.START_TURN)); //we do events & exit observer mode


            GameEngine.ActiveGame.clientManager.mode = ClientManager.MODE_IDLE; //we are done processing, so we do mp messages again
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message + " " + e.StackTrace);
        }


     //   GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.RESOLVE_EVENTS));

        //if (GameEngine.ActiveGame.isHost)
        //{
        //    GameEngine.ActiveGame.StartAI();
        //}
      
        // GameEngine.ActiveGame.RefreshUI();
    }

    public void CheckPlayerDeathPhase()
    {
        foreach (Player player in Players)
        {
            if (player.PlayerID == Player.Neutral)
            {
                continue;
            }
            if (player.Defeated)
            {
                continue;
            }
            switch (PlayerDeathCondition)
            {
                case OptionCollection.Player_Death_No_Buildings:
                    if (!PlayerHasBuildings(player.PlayerID))
                    {
                        SetPlayerAsDefeated(player.PlayerID);
                    }
                    break;
                case OptionCollection.Player_Death_No_Buildings_And_Heroes:
                    if (!PlayerHasBuildings(player.PlayerID) && !PlayerHasHeroes(player.PlayerID))
                    {
                        SetPlayerAsDefeated(player.PlayerID);
                    }
                    break;
                case OptionCollection.Player_Death_No_Capital:
                    if (!PlayerHasCapital(player))
                    {
                        SetPlayerAsDefeated(player.PlayerID);
                    }
                    break;
                case OptionCollection.Player_Death_No_Heroes:
                    if (!PlayerHasHeroes(player.PlayerID))
                    {
                        SetPlayerAsDefeated(player.PlayerID);
                    }
                    break;
                case OptionCollection.Player_Death_No_Capital_And_Heroes:
                    if (!PlayerHasCapital(player) && !PlayerHasHeroes(player.PlayerID))
                    {
                        SetPlayerAsDefeated(player.PlayerID);
                    }
                    break;
                default:
                    break;
            }
        }
     
    }

    public bool PlayerHasCapital(Player player)
    {
        if (this.Worldmap == null)
        {
            return false;
        }
        GameSquare buildingSqr = Worldmap.FindGameSquareByCoordinates(player.CapitalLocation.XCoordinate, player.CapitalLocation.YCoordinate);
        if (buildingSqr.building == null)
        {
            return false;
        }
        if (buildingSqr.building.OwnerPlayerID != player.PlayerID)
        {
            return false;
        }
        return true;
    }
    bool PlayerHasBuildings(string playerID)
    {
        if (GetPlayerBuildings(playerID).Count > 0)
        {
            return true;
        }
        return false;
    }

    bool PlayerHasHeroes(string playerID)
    {
        if (GetAllPlayerHeroes(playerID).Count > 0)
        {
            return true;
        }
        return false;
    }

    public void CheckVictoryPhase()
    {
        List<string> playersThatWon = new List<string>();
        bool gameEnds = false;
   
        switch (WinConditionMode)
        {
            case OptionCollection.Victory_Last_Player_Standing:
                List<string> winnerIDs = new List<string>();
                foreach (Player player in Players)
                {
                    if (player.PlayerID == Player.Neutral)
                    {
                        continue;
                    }
                    if (!player.Defeated)
                    {
                        winnerIDs.Add(player.PlayerID);
                    }
                }
                if (winnerIDs.Count <= HowManyPlayersShouldRemain)
                {
                    playersThatWon.AddRange(winnerIDs);
                    gameEnds = true;
                }
                 
                break;
            case OptionCollection.Victory_Survive_Until_Turn:
                if (Turncounter >= SurviveUntilTurn)
                {
                    foreach (Player player in Players)
                    {
                        if (player.Defeated)
                        {
                            continue;
                        }
                        playersThatWon.Add(player.PlayerID);
                        gameEnds = true;
                    }
                }
           
                break;
            case OptionCollection.Victory_None:

                break;
            default:
                Debug.LogError("no victory condition mode: " + WinConditionMode);
                break;
        }
        //no players to win, everyone looses
        bool noPlayersRemain = true;
        foreach (Player player in Players)
        {
            if (player.PlayerID == Player.Neutral)
            {
                continue;
            }
            if (!player.Defeated)
            {
                noPlayersRemain = false;
                break;
            }
        }
        if (noPlayersRemain)
        {
            playersThatWon.Add("No one");
            gameEnds = true;
        }

        if (gameEnds)
        {
            string message = "";
        
            foreach (string plrID in playersThatWon)
            {
                message += plrID;
                if (playersThatWon[playersThatWon.Count - 1] != plrID)
                {
                    message += ", ";
                }
            }
            message += " won the game";
            PlayersThatWonList.AddRange(playersThatWon);
            Debug.Log("!!!!!!!!! " + message + " !!!!!!!!!!!!");
            Ended = true;
            //TODO ui 
        }

    }

    public void CreateAuctionsPhase()
    {
        bool timer = false;
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Reset();
            GameEngine.ActiveGame.GameStopwatch.Start();
        }

        foreach (MerchantGuild guild in Guilds)
        {
            MerchantGuildTemplate merchantGuildTemplate = GameEngine.Data.MerchantGuildTemplateCollection.findByKeyword(guild.TemplateKeyword);
            if (merchantGuildTemplate == null)
            {
                Debug.Log("no guild template found: " + guild.TemplateKeyword + " might be player created guild: " + guild.Name);
            }
            guild.CreateAuctions(merchantGuildTemplate.MaxAuctionNumber, merchantGuildTemplate.MinAuctionValue, merchantGuildTemplate.MinAuctionTurn, merchantGuildTemplate.MaxAuctionTurn, merchantGuildTemplate.StartingBidPriceDivision);
        }
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Stop();
            Debug.Log("Scenario.CreateAuctionsPhase took " + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
        }
    }


    public void RefreshShopContentPhase()
    {
        bool timer = false;
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Reset();
            GameEngine.ActiveGame.GameStopwatch.Start();
        }

        foreach (MerchantGuild guild in Guilds)
        {
            guild.CurrentRefreshStreak++;
            MerchantGuildTemplate template = GameEngine.Data.MerchantGuildTemplateCollection.findByKeyword(guild.TemplateKeyword);
            if (guild.CurrentRefreshStreak == template.RefreshShopContentInXTurns)
            {
                guild.ResetStockItemsStatus();
                guild.CurrentRefreshStreak = 0;
            }
          
        }

        foreach (Player player in players)
        {
            if (player.Defeated)
            {
                continue;
            }
            foreach (Shop shop in player.Shops)
            {
                foreach (MerchantGuild guild in Guilds)
                {
                    if (guild.CurrentRefreshStreak == 0)
                    {
                        shop.SetShopProducts(shop.GuildID, player.PlayerID);
                    }
                }
              
            }
        }
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Stop();
            Debug.Log("Scenario.RefreshShopContentPhase took " + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
        }
    }

    public void AllocatePlannedProductionsPhase()
    {
        bool timer = true;
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Reset();
            GameEngine.ActiveGame.GameStopwatch.Start();
        }

        foreach (Building building in Worldmap.GetAllBuildings())
        {
            building.AllocatePlannedProductions();
        }
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Stop();
            Debug.Log("Scenario.AllocatePlannedProductionsPhase took " + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
        }
    }

    /// <summary>
    /// right now this gets all battles that involve a player directly
    /// in future we could include battles that the player can see on the map
    /// </summary>
    /// <param name="playerID"></param>
    /// <returns></returns>
    public List<int> GetPlayersActiveBattlesIDs(string playerID)
    {
        List<int> answer = new List<int>();
        lock (this.ActiveBattles.Battlefields)
        {
            foreach (BattlefieldOld battlefield in this.ActiveBattles.Battlefields)
            {
                if (battlefield.GetCurrentParticipantPlayerIDs().Contains(playerID))
                {
                    answer.Add(battlefield.ID);
                }
            }
        }
       
        return answer;
    }
    internal IEnumerable<Army> GetAllOverlandBattleArmies()
    {
        List<Army> answer = new List<Army>();
        lock (QueuedUpBattles.Battlefields)
        {
            foreach (BattlefieldOld battlefieldOld in this.QueuedUpBattles.Battlefields)
            {
                if (battlefieldOld.Mode != BattlefieldOld.MODE_OVERLAND)
                {
                    continue;
                }
                answer.AddRange(battlefieldOld.Armies);
            }
        }



        lock (BattlesToBeContinued.Battlefields)
        {
            foreach (BattlefieldOld battlefieldOld in BattlesToBeContinued.Battlefields)
            {
                if (battlefieldOld.Mode != BattlefieldOld.MODE_OVERLAND)
                {
                    continue;
                }
                answer.AddRange(battlefieldOld.Armies);
            }
        }


        lock (ActiveBattles.Battlefields)
        {
            foreach (BattlefieldOld battlefieldOld in ActiveBattles.Battlefields)
            {
                if (battlefieldOld.Mode != BattlefieldOld.MODE_OVERLAND)
                {
                    continue;
                }
                answer.AddRange(battlefieldOld.Armies);
            }
        }
        return answer;
    }
    internal IEnumerable<Army> GetAllBattlefieldArmies()
    {
        List<Army> answer = new List<Army>();
        lock (QueuedUpBattles.Battlefields)
        {
            foreach (BattlefieldOld battlefieldOld in this.QueuedUpBattles.Battlefields)
            {
                answer.AddRange(battlefieldOld.Armies);
            }
        }



        lock (BattlesToBeContinued.Battlefields)
        {
            foreach (BattlefieldOld battlefieldOld in BattlesToBeContinued.Battlefields)
            {
                answer.AddRange(battlefieldOld.Armies);
            }
        }


        lock (ActiveBattles.Battlefields)
        {
            foreach (BattlefieldOld battlefieldOld in ActiveBattles.Battlefields)
            {
                answer.AddRange(battlefieldOld.Armies);
            }
        }
        return answer;
    }

    public void SetNextPlayerAsActive(string caller)
    {
        Debug.Log("caller: " + caller);
        if (playerturnqueue.Count > 0) //we have some players left in turn-based single comp mode
        {
            Debug.Log("went playerqueue to  DisplayLocalActivePlayer");
            ActivePlayerID = playerturnqueue[0];
            playerturnqueue.RemoveAt(0);
            GameEngine.ActiveGame.DisplayLocalActivePlayer(false);
        }
        else //all players have done their turn and we initiate battles
        {
            Debug.Log("went endglobalturn");
            Debug.Log("before unit upkeeps");
            
            PayUnitUpkeeps();//pay hero upkeeps, unit upkeeps



            RemoveUnhappyUnits();
 
      

            //NeutralsAttackPhase();
            DetectConflicts();
            GameEngine.ActiveGame.RequeueBattlesAndRemoveHistory();
            GameEngine.ActiveGame.ProcessBattlesQueue(null);
            Debug.Log("after process battles queue");
       
        }

    }

    //this is useful only to other clients(host save/load to avoid loading lots of notifications) and observer(stop notification overflow)
    void ClearAllNotifications() 
    {
        foreach (Player player in Players)
        {
            player.Notifications.Clear();
        }
    }

    /// <summary>
    /// after everyone has ended their turn
    /// </summary>
    public void EndGlobalTurn()
    {
        try
        {
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.COMMAND_TURN_INTO_OBSERVER));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Ending turn..."));
            Debug.Log("end global turn");
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Clearing all notifications"));
            ClearAllNotifications();
            //HowLargeIsScenario(" after clearing notifications");
          
            GameEngine.ActiveGame.clientManager.mode = ClientManager.MODE_PROCESSING; //preventing recieving messages here, as it could lead to bad situations
                                                                                      //PayUnitUpkeeps();//pay hero upkeeps, unit upkeeps



            //RemoveUnhappyUnits(); //move to start turn?
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Clearing overland replays"));
            ClearOverlandReplaysPhase(); //so that replays created during phases remain
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Neutrals attacking"));
            NeutralsAttackPhase();
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Revealing armies on missions"));
            RevealArmiesPerformingMissions();
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Detecting conflicts"));
            DetectConflicts();
            GameEngine.ActiveGame.RequeueBattlesAndRemoveHistory();
            //HowLargeIsScenario("after detect conflicts ");
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Processing battles queue"));
            GameEngine.ActiveGame.ProcessBattlesQueue(null);
            GameEngine.ActiveGame.clientManager.mode = ClientManager.MODE_IDLE;
            Debug.Log("after process battles queue");
        }
        catch (Exception e)
        {

            Debug.LogError(e.Message + " " + e.StackTrace);
        }

    }

    public void NEWEndTurnProcess(string incPlayer)
    {
        //Playerturnqueue.Remove(incPlayer);
        //GameEngine.Server.Broadcast("endTurn|" + incPlayer,GameEngine.Server.clients);
        //if (Playerturnqueue.Count == 0)
        //{
        //    Debug.Log("NEWEndTurnProcess all players ended turn");
        //}

        //no null checks here because we call this from this playercontroller
        PlayerController playerController = GameEngine.ActiveGame.FindPlayerControllerGameObject(incPlayer).GetComponent<PlayerController>();
        playerController.isObserver = true; //for the brief period that 
        playerController.isEndingTurn = true;
        List<ProductionLine> changedLines = GetPlayerChangedProductionSlider(incPlayer);
        foreach (Building building in GetPlayerBuildings(incPlayer))
        {
            foreach (BuildingProduction production in building.ArmyProductions)
            {
                foreach (ProductionLine line in production.ProductionLines)
                {
                    if (line.WasChanged)
                    {
                        bool refreshUI = false;
                        if (changedLines[changedLines.Count-1].ID == line.ID) //if this is the last line, then refresh UI
                        {
                            refreshUI = true;
                        }
                        MultiplayerMessage productionLineChangedMessage = new MultiplayerMessage(MultiplayerMessage.SetProductionLineValue, production.ID + "*" + line.ID+"*"+line.AllocatedPercentage+"*"+refreshUI.ToString(), building.ID.ToString());
                        GameEngine.ActiveGame.clientManager.Push(productionLineChangedMessage);
                        line.WasChanged = false;
                        //Debug.Log("line was changed debug");
                    }

                }
            }
        }


        MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.SubmitEndTurn,incPlayer,"");
        GameEngine.ActiveGame.clientManager.Push(multiplayerMessage);

    }

    /// <summary>
    /// no usage
    /// </summary>
    /// <param name="incPhase"></param>
    public void ProceedToEndTurn(GameState incPhase)
    {
        foreach (Player player in Players)
        {
            if (player.GameState == null)
            {
                Debug.LogError("no player gamestate!!!!! in " + player.PlayerID);
                return;
            }
            if (player.GameState.Keyword != incPhase.Keyword)
            {
                return;
            }
        }
        foreach (Player player in Players)
        {
            player.GameState = GameEngine.Data.GameStateCollection.findByKeyword(GameState.State.MAIN_PHASE);
        }
    }

    /// <summary>
    /// when all players have their phase as incPhase, will proceed to main phase
    /// !!!! multiplayer will change this approach, use task for this?
    /// !! new change: this method is depricated
    /// </summary>
    /// <param name="incPhase"></param>
    public void ProceedToMainPhase()
    {
        bool debug = true;
        if (debug)
        {
            Debug.Log("ProceedToMainPhase call");
        }
        //multiplayer in place, no longer using this part
        //foreach (Player player in Players)
        //{
        //    if (player.GameState == null)
        //    {
        //        Debug.LogError("no player gamestate!!!!!");
        //        return;
        //    }
        //    if (player.GameState.Keyword != incPhase.Keyword)
        //    {
        //        return;
        //    }
        //}
        foreach (Player player in Players)
        {
            player.GameState = GameEngine.Data.GameStateCollection.findByKeyword(GameState.State.MAIN_PHASE);
            //!!! refresh UI here!!!, will implement multiplayer on this a bit later, should be ez
            GameObject controllerObject = GameEngine.ActiveGame.FindPlayerControllerGameObject(player.PlayerID);
            if (controllerObject != null)
            {
                PlayerController playerController = controllerObject.GetComponent<PlayerController>();
                playerController.RefreshUI();
            }

            if (debug) {
                Debug.Log(player.PlayerID + " gamestate: " + player.GameState.Keyword);
            }
           
        }
        if (GameEngine.ActiveGame.isHost)
        {
            GameEngine.ActiveGame.StartAI("");
        }
      
    }
    /// <summary>
    /// TODO: option to not clear(or maybe skip option bc it be bad for memory)
    /// </summary>
    public void ClearOverlandReplaysPhase()
    {
        foreach (Player player in Players)
        {
            player.MapMemory.Replays.Clear();
        }
    }

    public bool IsPlayerPlayedByThisMachine(string playerID)
    {
        foreach (PlayerSetup setup in GetPlayerSetupsByComputerName(GameEngine.PLAYER_IDENTITY))
        {
            if (setup.PlayerName == playerID)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// this method used to be in scenario StartGlobalTurn, however due to a LOT of UI being in use, i decided to move it into main thread
    /// in StartGlobalTurn(thread) a multiplayerUICommand is queued up, and in playercontroller this method is called
    /// </summary>
    public void ResolveAllPlayerEvents()
    {
        bool timer = true;
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Reset();
            GameEngine.ActiveGame.GameStopwatch.Start();
        }

        bool debug = true;

        foreach (Player player in Players)
        {
            //player.PlayerEventRandom = new MyRandom(GameEngine.random.Seed, GameEngine.random.Iteration);
            player.GameState = GameEngine.Data.GameStateCollection.findByKeyword(GameState.State.EVENT_PHASE);
      
            if (player.isAI && GameEngine.ActiveGame.isHost) //if this is called host side, then only host launches AIs to resolve their events
            {
                if (debug)
                {
                    Debug.Log(player.PlayerID + " AI events gamestate: " + player.GameState.Keyword);
                }
                player.resolveEvents(true);
            }
            //foreach (var item in GameEngine.Data.GameStateCollection.DataList)
            //{
            //    Debug.LogError(item.Keyword);
            //}
            //GameState test = GameEngine.Data.GameStateCollection.findByKeyword(GameState.State.EVENT_PHASE);
            //if (test == null)
            //{
            //    Debug.LogError("g");
            //}

            //MultiplayerMessage resolveEvent = new MultiplayerMessage(MultiplayerMessage.ResolveEvents, GetPlayerSetupByPlayerID(player.PlayerID).ComputerName, "");
            //GameEngine.ActiveGame.clientManager.Push();
      
           // player.resolveEvents(player.isAI, null);
            //player.resolveEvents(false, null);
        }
        //we go through player setups to get players under control of this computer
        //and then we manually resolve those
        foreach (PlayerSetup setup in GetPlayerSetupsByComputerName(GameEngine.PLAYER_IDENTITY))
        {
            Player player = FindPlayerByID(setup.PlayerName);
            player.resolveEvents(false);
        }

        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Stop();
            Debug.Log("Scenario.ResolveAllPlayerEvents took " + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
        }
    }

    /// <summary>
    /// doesnt affect skills(?)
    /// </summary>
    public void RefreshStatsPhase()
    {
        bool timer = false;
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Reset();
            GameEngine.ActiveGame.GameStopwatch.Start();
        }

        Army allunitsingame = GetAllUnitsInTheGame(true, true);

        foreach (Entity entity in allunitsingame.Units)
        {
            foreach (EntityStat stat in entity.Stats)
            {
                stat.Current += stat.Regen;
                if (stat.Current > stat.Boosted)
                {
                    stat.Current = stat.Boosted;
                }
            }

            foreach (Limb limb in entity.Limbs)
            {
                foreach (EntityStat stat in limb.Stats)
                {
                    stat.Current += stat.Regen;
                    if (stat.Current > stat.Boosted)
                    {
                        stat.Current = stat.Boosted;
                    }
                }
            }

        }
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Stop();
            Debug.Log("Scenario.RefreshStatsPhase took " + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
        }

    }

    public Army FindArmyByAlternateID(string alternateID)
    {
        foreach (Army army in Armies)
        {
            if (army.AlternateID == alternateID)
            {
                return army;
            }
        }

        foreach (Player player in players)
        {
            foreach (Quest quest in player.ActiveQuests)
            {
                foreach (QuestParty party in quest.Parties)
                {
                    if (party.Army.AlternateID == alternateID)
                    {
                        return party.Army;
                    }
                }
            }
        }
        return null;
        
    }
    void RollEventsForPlayerPhase(Player player)
    {
        bool debug = true;
        if (!Events_Toggle)
        {
            return;
        }
        if (player.Defeated)
        {
            return;
        }
        if (player.PlayerID == "neutral") //skipping neutral due to test reasons, as auto pick isnt functional right now 15.02.2023, this gets removed
        {
            return;
        }
        if (!PlayerHasCapital(player)) //not rolling any events(of capital location) if you have no capital
        {
            return;
        }
        if (player.DelaydEvents.Count > 0)
        {
            player.checkDelayedEvents();//Delayd Eventide addimine
        }
        //uncomment this if when you stop debugging
        //List<EventTemplate> eventTemplates = GameEngine.Data.EventTemplateCollection.GetProvinceEvents(Events_Odds);
        List<EventTemplate> eventTemplates = new List<EventTemplate>();

        //EventTemplate t = GameEngine.Data.EventTemplateCollection.findByKeyword("Goblin King appears"); //Some items were found
        EventTemplate t = GameEngine.Data.EventTemplateCollection.findByKeyword("Adventurer selling map (maze)");
        eventTemplates.Add(t);
        //EventTemplate d = GameEngine.Data.EventTemplateCollection.findByKeyword("New copper deposit");
        //eventTemplates.Add(d);
        //        eventTemplates.Clear();
        //Debug.Log("legal events queued: " + eventTemplates.Count);
        foreach (EventTemplate eventTemplate in eventTemplates)
        {
            if (debug)
            {
                Debug.Log(player.PlayerID + " event rolled: " + eventTemplate.Keyword);
            }

            player.InitProvinceCenterEvent(eventTemplate, null,false);
        }
    }
    public void RollEventsPhase()
    {
        if (!Events_Toggle)
        {
            return;
        }
        bool debug = false;
        MapTemplate mapTemplate = GameEngine.Data.MapTemplateCollection.findByKeyword(this.MapTemplateKeyword);
        if (mapTemplate.HasAnyEventsForTurn(this.turncounter)) {
            worldmap.GenerateOverlandEvents(Players, mapTemplate, this.turncounter);
        }

        foreach (Player player in Players)
        {
            if (player.Defeated)
            {
                continue;
            }
            if (player.PlayerID == "neutral") //skipping neutral due to test reasons, as auto pick isnt functional right now 15.02.2023, this gets removed
            {
                continue;
            }
            if (player.DelaydEvents.Count > 0)
            {
                player.checkDelayedEvents();//Delayd Eventide addimine
            }
            //uncomment this if when you stop debugging
            //List<EventTemplate> eventTemplates = GameEngine.Data.EventTemplateCollection.GetProvinceEvents(Events_Odds);
            List<EventTemplate> eventTemplates = new List<EventTemplate>();

//            eventTemplates.Add(GameEngine.Data.EventTemplateCollection.findByKeyword("Goblin King appears")); //Some items were found

            //EventTemplate d = GameEngine.Data.EventTemplateCollection.findByKeyword("New copper deposit");
            //eventTemplates.Add(d);
    //        eventTemplates.Clear();
            //Debug.Log("legal events queued: " + eventTemplates.Count);
            foreach (EventTemplate eventTemplate in eventTemplates)
            {
                if (debug)
                {
                    Debug.Log(player.PlayerID + " event rolled: " + eventTemplate.Keyword);
                }
     
                player.InitProvinceCenterEvent(eventTemplate,null,false);
            }
        }
    }
    public List<Entity> GetAllPlayerNonHungryUnits(string playerID)
    {
        List<Entity> answer = new List<Entity>();
        Army allEntities = GetAllUnitsInTheGame(true, false);

        foreach (var item in allEntities.Units)
        {
            if (item.FindCurrentOwnerID() == playerID && !item.IsHungry)
            {
                answer.Add(item);
            }
        }
        return answer;
    }
    public List<Entity> GetAllPlayerEntities(string playerID)
    {
        List<Entity> answer = new List<Entity>();
        Army allEntities = GetAllUnitsInTheGame(true, true);

        foreach (var item in allEntities.Units)
        {
            if (item.FindCurrentOwnerID() == playerID)
            {
                answer.Add(item);
            }
        }
        return answer;
    }
    public List<Entity> GetAllPlayerHeroes(string playerID)
    {
        List<Entity> answer = new List<Entity>();
        Army allEntities = GetAllUnitsInTheGame(true, false);

        foreach (var item in allEntities.Units)
        {
            if (item.FindCurrentOwnerID() == playerID)
            {
                if (item.IsHeroFlag)
                {
                    answer.Add(item);
                }
              
            }
        }
        return answer;
    }


    public List<int> GetAllPlayerEntitiesIDs(string playerID)
    {
        List<int> answer = new List<int>();
        Army allEntities = GetAllUnitsInTheGame(true, true);

        foreach (var item in allEntities.Units)
        {
            if (item.FindCurrentOwnerID() == playerID)
            {
                answer.Add(item.UnitID);
            }
        }
        return answer;
    }

    public List<int> GetPlayerQuestEntities(string playerID, bool heroesOnly)
    {
        Player player = FindPlayerByID(playerID);
        List<int> answer = new List<int>();
     
        foreach (var item in player.ActiveQuests)
        {
            foreach (var party in item.Parties)
            {
                if (party.Army.OwnerPlayerID == playerID)
                {
                    foreach (var unit in party.Army.Units)
                    {
                        if (heroesOnly)
                        {
                            if (!unit.IsHeroFlag)
                            {
                                continue;
                            }
                        }
                        answer.Add(unit.UnitID);
                    }
                }
            }
        }
       
        return answer;
    }

    public EntityList FindUnitsByIDs(List<int> ids)
    {
        EntityList entities = new EntityList();

        Army allEntities = GetAllUnitsInTheGame(true, true);

        foreach (Entity entity in allEntities.Units)
        {
            if (ids.Contains(entity.UnitID))
            {
                entities.Add(entity);
            }
        }


        return entities;
    }

    public List<int> GetEntitiesByLocation(string playerID,Location location, bool heroesOnly)
    {
        List<int> answer = new List<int>();
        Player player = FindPlayerByID(playerID);
        //get units if specific quest of specific location of the player
        if (location.DungeonCoordinates != null)
        {
            foreach (Quest quest in player.ActiveQuests)
            {
                if (quest.ID == location.DungeonCoordinates.ID)
                {
                    foreach (QuestParty party in quest.Parties)
                    {

                        if (party.Army.OwnerPlayerID == playerID)
                        {
                            if (party.Progress == location.DungeonCoordinates.XCoordinate)
                            {
                                foreach (Entity unit in party.Army.Units)
                                {
                                    if (heroesOnly)
                                    {
                                        if (!unit.IsHeroFlag)
                                        {
                                            continue;
                                        }
                                    }
                                    answer.Add(unit.UnitID);
                                }
                            }
                            
                            
                        }
                    }
                }
                
            }
        }

        if (location.WorldMapCoordinates != null)
        {
            List<Army> armiesOnHex = FindAllOverlandArmiesByCoordinates(location.WorldMapCoordinates.XCoordinate,location.WorldMapCoordinates.YCoordinate);

            foreach (Army army in armiesOnHex)
            {
                if (army.OwnerPlayerID == playerID)
                {
                    foreach (Entity unit in army.Units)
                    {
                        if (heroesOnly)
                        {
                            if (!unit.IsHeroFlag)
                            {
                                continue;
                            }
                        }
                        answer.Add(unit.UnitID);
                    }
                }
            }
        }
        return answer;

    }

    public List<int> GetPlayerEntitiesByMode(string mode, string playerID, Location location)
    {
        List<int> answer = new List<int>();

        switch (mode)
        {
            //this gets all player entities in specific location on overland map or quest
            case EventTemplate.OPTION_ENTITIES_ON_LOCATION:
                answer = GetEntitiesByLocation(playerID,location,false);
                break;
            case EventTemplate.OPTION_HEROES_ON_LOCATION:
                answer = GetEntitiesByLocation(playerID, location, true);
                break;

            case EventTemplate.OPTION_ALL_ENTITIES:
                answer = GetAllPlayerEntitiesIDs(playerID);
                break;
            case EventTemplate.OPTION_ALL_HEROES:
                List<Entity> heroes = FindAllHeroesByPlayerID(playerID);
                foreach (var item in heroes)
                {
                    answer.Add(item.UnitID);
                }
                break;
            case EventTemplate.OPTION_QUEST_HEROES:
                answer = GetPlayerQuestEntities(playerID,true);
                break;
            case EventTemplate.OPTION_QUEST_ENTITIES:
                answer = GetPlayerQuestEntities(playerID, false);
                break;
                //TODO: add more cases
            default:
                break;
        }

        return answer;
    }

    ///// <summary>
    ///// will check if this player has any more events
    ///// </summary>
    ///// <param name="player"></param>
    ///// <param name="previousResults"></param>
    //public void eventStatusCheck(Player player,List<string> previousResults)
    //{
         
    //    //foreach (ScenarioWindow currentScenariowWindow in this.scenarioWindows)
    //    //{
    //    //    if (currentScenariowWindow.OwnerPlayer.IDP.Equals(activePlayer.IDP))
    //    //    {
    //    //        scenarioWindow = currentScenariowWindow;
    //    //    }
    //    //}
    //    player.resolveEvents(false, previousResults);
    //    //this.scenarioWindow.Show();
    //    //this.scenarioWindow.resolveEvents(false, previousResults);

    //}//end eventStatusCheck


    public void PlayerFinishedAfterBattles(string playerID)
    {

        Debug.Log("PlayerFinishedAfterBattles for " + playerID + " return");
        return;
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.COMMAND_TURN_INTO_OBSERVER));
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "After battle processing..."));
        GameEngine.ActiveGame.clientManager.mode = ClientManager.MODE_PROCESSING;
        Thread postBattleProcessing =  new Thread(() => this.PlayerAfterBattles(playerID,true));
        postBattleProcessing.IsBackground = true;
        postBattleProcessing.Name = "Post battle processing";
        postBattleProcessing.Start();
        //PlayerAfterBattles(playerID);
    }

    public void PlayerAfterBattles(string playerID,bool turnOffObserver)
    {
        Player player = FindPlayerByID(playerID);
        RefreshQuestCounterForPlayer(playerID);
        ProcessPlayerHeroActionPhase(player);
        ExtraItemReductionPhasePlayer(player);
        RollEventsForPlayerPhase(player);
        ProcessQuestPartiesPlayerPhase(player);
        SendHeroesAsReinforcementsFromEventBattles(player);
        if (turnOffObserver)
        {
            GameEngine.ActiveGame.clientManager.mode = ClientManager.MODE_IDLE;
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.COMMAND_STOP_OBSERVING));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.COMMAND_REFRESH_UI));
        }
    }


    void SendHeroesAsReinforcementsFromEventBattles(Player player)
    {
        if (player.Defeated)
        {
            return;
        }
        foreach (PendingReinforcement pendingReinforcement in player.PendingReinforcements)
        {
            BattlefieldOld battlefield = GetBattlefieldByArmyID(pendingReinforcement.ArmyID);
            Army reinforcement = new Army(++player.LocalArmyIDCounter, player);
            reinforcement.Units.Add(pendingReinforcement.Entity);
            reinforcement.Location = new Location();
            reinforcement.Location.WorldMapCoordinates = pendingReinforcement.WorldMapCoordinates;
            reinforcement.LeaderID = pendingReinforcement.Entity.UnitID;
            
            BattleParticipant newParticipant = new BattleParticipant(reinforcement);
            if (battlefield != null)
            {
                battlefield.AddOverlandParticipant(newParticipant);
                battlefield.SingleParticipantPlacement(newParticipant);
                if (battlefield.IsEventBattle)
                {
                    reinforcement.Location.Mode = Location.MODE_IN_DUNGEON_BATTLE;
                }
                else
                {
                    reinforcement.Location.Mode = Location.MODE_IN_OVERLAND_BATTLE;
                }
              
            }
            else //the army now just minds it's own business, as there was no battle to add to
            {
                reinforcement.Location.Mode = Location.MODE_OVERLAND;
            }
      
        }
    }

    void SendBattleReinforcementsFromEventBattlesPhase()
    {
        foreach (Player player in Players)
        {
            if (player.Defeated)
            {
                continue;
            }
            SendHeroesAsReinforcementsFromEventBattles(player);
        }
    }

    public void AfterBattles()
    {

        Debug.Log("after battles");
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.COMMAND_TURN_INTO_OBSERVER));
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Processing after battles..."));
        SendBattleReinforcementsFromEventBattlesPhase();
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Refreshing quest counter"));
        RefreshQuestCounter(); //TODO: call when no battles left
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Refreshing future heroes"));
        RefreshFutureHeroCounter(false);
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Processing hero actions"));
        ProcessHeroActionPhase(); //TODO: call when no battles left
        ProcessBuildingPlanPhase(); //lockdown
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Decaying capturing progress"));
        DecayCapturingProgress();
        //ProcessTradingItemsPhase(); //this got moved to startglobalturn
        //ProcessAuctionItemsPhase(); //this got moved to startglobalturn
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Reducing extra items"));
        ExtraItemReductionPhase(); //TODO: call when no battles left
        //RollEventsPhase(); //TODO: call when no battles left
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Processing quest parties"));
        ProcessQuestPartiesPhase(); //TODO: call when no battles left
     
        StartGlobalTurn(false);
        //GameEngine.ActiveGame.RefreshAllPlayerUIs();
   
    }

    public void ArmyProductionPhase()
    {
        bool timer = true;
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Reset();
            GameEngine.ActiveGame.GameStopwatch.Start();
        }

        bool debug = false;

        List<Building> buildings = Worldmap.GetAllBuildings();
        foreach (Building building in buildings)
        {
            if (building.Durability.Current < building.Durability.Original)
            {
                continue; //if buildign is damaged, produce nothing
            }
            if (debug) {
                Debug.Log("TURN " + this.turncounter + " production proccess started for building " + building.ID);
            }
            building.ArmyProductionProcess();
            //  OurLog.Print(building.templateKeyword + " produced " + building.armyProductions.)
        }
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Stop();
            Debug.Log("Scenario.ArmyProductionPhase took " + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
        }
    }
    /// <summary>
    /// not crafting
    /// </summary>
    public void PlayerItemProductionPhase()
    {
        bool timer = true;
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Reset();
            GameEngine.ActiveGame.GameStopwatch.Start();
        }

        foreach (Player player in Players)
        {
            if (player.Defeated)
            {
                continue;
            }
            foreach (Stat stat in player.ItemIncome)
            {
                if (stat.Amount < 1)
                {
                    continue;
                }
                Item newItem = Item.createItemByKeyword(stat.Keyword,"");
                newItem.Quantity = (int)stat.Amount;
                player.OwnedItems.AddItem(newItem);
            }
        }
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Stop();
            Debug.Log("Scenario.PlayerItemProductionPhase took " + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
        }
    }

    public Army GetEntitiesThatAreNotInArmyProductionListsButAreLinkedToBuilding(Building building)
    {
        Army answer = new Army(-1, null);
        foreach (Entity unit in GetAllUnitsInTheGame(false, true).Units)
        {
            if (unit.UpKeep.BuildingID == building.ID)
            {
                bool isInProduction = false;
                foreach (BuildingProduction buildingProduction in building.ArmyProductions)
                {
                    if (buildingProduction.EntityIds.Contains(unit.UnitID))
                    {
                        isInProduction = true;
                        break;
                    }
                }
                if (!isInProduction)
                {
                    answer.Units.Add(unit);
                }
            }
        }
        return answer;
    }

    public Army GetUnitsByIDs(List<int> incids)
    {
        Army entities = new Army(-1,null);

        foreach (Entity entity in GetAllUnitsInTheGame(true, true).Units)
        {
            if (incids.Contains(entity.UnitID))
            {
                entities.Units.Add(entity);
            }
        }

        return entities;
    }

    public Army FindAllUnitsByBuildingID(int buildingID, string armyOwnerPlayerID)
    {
        Army unitsToSupply = new Army(-1, null);
        List<Building> buildings = Worldmap.GetAllBuildings();
        foreach (Army army in armies)
        {
            if (armyOwnerPlayerID != null)
            {
                if (army.OwnerPlayerID != armyOwnerPlayerID)
                {
                    continue;
                }
            }

            foreach (Entity unit in army.Units)
            {
                if (unit.UpKeep != null)
                {
                    if (unit.UpKeep.BuildingID != 0)
                    {
                        if (unit.UpKeep.BuildingID == buildingID)
                        {
                            unitsToSupply.AddSorted(unit);
                        }
                    }

                }

            }
        }

        foreach (Building building in buildings)
        {
            foreach (Entity unit in building.Storage.Units)
            {
                if (unit.UpKeep != null)
                {
                    if (unit.UpKeep.BuildingID != -1)
                    {
                        if (unit.UpKeep.BuildingID == buildingID)
                        {
                            unitsToSupply.AddSorted(unit);
                        }
                    }

                }
            }

        }

        return unitsToSupply;
    }


    public Army FindAllUnitsByTypeAndBuildingID(string keyword, int buildingID, string armyOwnerPlayerID)
    {
        Army unitsToSupply = new Army(-1,null);
        List<Building> buildings = Worldmap.GetAllBuildings();
        foreach (Army army in armies)
        {
            if (armyOwnerPlayerID != null)
            {
                if (army.OwnerPlayerID != armyOwnerPlayerID)
                {
                    continue;
                }
            }

            foreach (Entity unit in army.Units)
            {
                if (unit.UpKeep != null)
                {
                    if (unit.UpKeep.BuildingID != 0)
                    {
                        if (unit.UnitName == keyword && unit.UpKeep.BuildingID == buildingID)
                        {
                            unitsToSupply.AddSorted(unit);
                        }
                    }

                }

            }
        }

        foreach (Building building in buildings)
        {
            foreach (Entity unit in building.Storage.Units)
            {
                if (unit.UpKeep != null)
                {
                    if (unit.UpKeep.BuildingID != 0)
                    {
                        if (unit.UnitName == keyword && unit.UpKeep.BuildingID == buildingID)
                        {
                            unitsToSupply.AddSorted(unit);
                        }
                    }

                }
            }
         
        }

        return unitsToSupply;
    }
    public void SendSuppliesPhase()
    {

        bool timer = false;
        bool debug = false;
        bool debugBuildingsList = false;


        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Reset();
            GameEngine.ActiveGame.GameStopwatch.Start();
        }
 

       
        List<Building> buildings = Worldmap.GetAllBuildings();
      
       
        if (debugBuildingsList)
        {
            Worldmap.GetInformationOnBuildings();
            Debug.Log("GetBuildings() GenerateSuppliesPhase buildings count: " + buildings.Count);
        }
        foreach (Building building in buildings)
        {
            if (debug)
            {
                
                if (building.ArmyProductions.Count > 0)
                {
                    Debug.Log("generating supplies phase for building: " + building.GetInformation());
                }
                else
                {
                    Debug.Log("not generating supplies for building: " + building.GetInformation());
                }
                
            }
            //make deplorables unhappy
            Army deplorables = GetEntitiesThatAreNotInArmyProductionListsButAreLinkedToBuilding(building);
            foreach (Entity entity in deplorables.Units)
            {
                entity.AddToAttitude(entity.FindCurrentOwnerID(), -10, -10);
                if (debug)
                {
                    Debug.Log("missing armyproduction entity: " + entity.UnitName + " " + entity.UnitID + " , lowering attitude");
                }
            }

            
            GameSquare gameSquare = worldmap.FindGameSquareByBuildingID(building.ID);
            foreach (BuildingProduction armyProduction in building.ArmyProductions)
            {
               
                if (debug)
                {
                    if (armyProduction.EntityIds.Count > 0)
                    {
                        Debug.Log("entity ids count in armyproduction: " + armyProduction.EntityIds.Count);

                        //foreach (int id in armyProduction.EntityIds)
                        //{
                        //    Debug.Log(""id);
                        //}
                    }
                    else
                    {
                        Debug.Log("entity ids count is 0");
                    }
                }

                List<int> entitiesToProcess = new List<int>();
                foreach (int id in armyProduction.EntityIds)
                {
                    entitiesToProcess.Add(id);
                }
                //we have to exclude storage units
                foreach (Entity storageUnit in building.Storage.Units)
                {
                    entitiesToProcess.Remove(storageUnit.UnitID);
                }

                //using this list we sort units into high and low priority lists
                Army unitsToSupply = GetUnitsByIDs(entitiesToProcess);

                //priority list are units that match entity types              
                Army priorityOneUnits = new Army(-1, null);

                //lower priority list are units that dont match entity types but are still linked
                Army lowerPriorityUnits = new Army(-1, null);

                //we get the target desire from armyproduction which has entity mode
                ProductionRecipeRequest targetDesire = null;

                //the desire of potion determines if we can upgrade units or not
                ProductionRecipeRequest trainingDesire = null;

                foreach (ProductionRecipeRequest desire in armyProduction.RecipeRequests)
                {
                    switch (desire.Mode)
                    {
                        case ProductionRecipeRequest.TYPE_ENTITY:
                            targetDesire = desire;
                            break;
                        case ProductionRecipeRequest.TYPE_TRAINING:
                            trainingDesire = desire;
                            break;
                        default:
                            break;
                    }
            
                   
                }

                if (debug)
                {
                    if (targetDesire != null)
                    {
                        Debug.Log("entity desire found: " + targetDesire.GetInformation());
                    }
                }
              
                //if the desire is found, we sort units into lists
                if (targetDesire != null)
                {
                    foreach (Entity unit in unitsToSupply.Units)
                    {
                        //checking if types match/dont match
                        CharacterTemplate characterTemplate = GameEngine.Data.CharacterTemplateCollection.findByKeyword(unit.CharacterTemplateKeyword);
                        if (characterTemplate.isCorrectTypes(targetDesire.EntityTypes,targetDesire.NotWantedTypes))
                        {
                            ItemCollection itemsToGive = new ItemCollection();

                            //if we dont have potion to upgrade with, we dont do upgrades at all
                            bool canUpgrade = true;
                            if (trainingDesire == null)
                            {
                                canUpgrade = false;
                            }
                       
                            if (unit.CharacterTemplateKeyword != armyProduction.UnitName)
                            {
                                Location unitLocation = FindEntityLocation(unit.UnitID);


                                //compiling list of items to give for upgrade
                                foreach (ProductionRecipeRequest desire in armyProduction.RecipeRequests)
                                {
                                    switch (desire.Mode)
                                    {
                                        //we skip entity and training potions
                                        case ProductionRecipeRequest.TYPE_ENTITY:
                                        case ProductionRecipeRequest.TYPE_TRAINING:
                                            break;
                                        case ProductionRecipeRequest.TYPE_ITEM:
                                            int amountNeeded = desire.RequiredAmountForEntity;

                                            int entitiesExistingAmount = unit.Inventory.GetCorrectAmountByTypes(desire.Types, desire.NotWantedTypes);


                                            amountNeeded = amountNeeded - entitiesExistingAmount;
                                            //means there are items to give
                                            if (amountNeeded > 0)
                                            {
                                                //we get a copy of items to be used in tranfer
                                                ItemCollection missingDesireItems = armyProduction.Stash.FindCorrectTypeItemsWithAmount(desire.Types, desire.NotWantedTypes, amountNeeded);
                                                int itemsRecieved = missingDesireItems.GetAmountOfItemsInInventory();
                                                //not enough items, abort entire upgrade operation
                                                if (itemsRecieved < amountNeeded)
                                                {
                                                    if (debug)
                                                    {
                                                        Debug.Log("not enough items for upgrade: " + itemsRecieved + "/" + amountNeeded + " " + desire.GetInformation());
                                                    }
                                                    canUpgrade = false;
                                                    break;
                                                }
                                                else
                                                {
                                                    //success
                                                    itemsToGive.AddRangeItems(missingDesireItems);
                                                }


                                            }
                                            else
                                            {
                                                //entity already has items of this type
                                            }

                                            break;
                                        default:
                                            Debug.LogError("missing mode: " + desire.Mode + " of desire: " + desire.GetInformation());
                                            break;
                                    }
                                }



                                if (canUpgrade)
                                {
                                    if (debug)
                                    {
                                        Debug.Log("trying to upgrade unit " + unit.CharacterTemplateKeyword + " " + unit.UnitID);
                                    }
                                    ItemCollection itemsToConsume = new ItemCollection();
                                    //skill potion check
                                    switch (unitLocation.Mode)
                                    {
                                        case Location.MODE_BUILDING_STORAGE:
                                        case Location.MODE_IN_DUNGEON_BATTLE:
                                        case Location.MODE_IN_OVERLAND_BATTLE:
                                            //skip
                                            break;
                                        case Location.MODE_OVERLAND:
                                            //if the unit is not upgraded yet, then we check if we send the potion
                                            bool skillTreesMatch = true;
                                            foreach (Stat requiredSkillTree in armyProduction.UnitTraining.SkillTreeKeywordsAndExp)
                                            {
                                                EntitySkillTreeLevel entitySkillTreeLevel = unit.checkSkillTreeLevel(requiredSkillTree.Keyword);
                                                if (entitySkillTreeLevel == null)
                                                {
                                                    skillTreesMatch = false;
                                                    break;
                                                }
                                                if (entitySkillTreeLevel.Exp_gained_for_tree < requiredSkillTree.Amount)
                                                {
                                                    skillTreesMatch = false;
                                                    break;
                                                }
                                            }
                                            if (!skillTreesMatch)
                                            {
                                                ItemCollection itemToGive = armyProduction.Stash.FindCorrectTypeItemsWithAmount(trainingDesire.Types, trainingDesire.NotWantedTypes, 1);
                                              
                                                

                                                if (itemToGive.Count == 0)
                                                {
                                                    //no potion to upgrade with, cancel upgrade and move on to next unit
                                                    continue;
                                                }
                                                else
                                                {
                                                    itemsToConsume.AddRangeItems(itemToGive);
                                                }

                                            }
                                            break;
                                        default:
                                            break;
                                    }




                                    foreach (Item item in itemsToGive)
                                    {
                                        SourceInfo from = new SourceInfo(SourceInfo.MODE_BUILDING_PRODUCTION_STASH);
                                        from.BuildingID = building.ID;
                                        from.BuildingProductionID = armyProduction.ID;
                                        from.Quantity = item.Quantity;
                                        from.ItemID = item.ID;
                                        from.AllowConsumption = false;
                                        SourceInfo into = new SourceInfo(SourceInfo.MODE_ENTITY_EQUIPMENT);
                                        into.AllowConsumption = false;
                                        into.EntityID = unit.UnitID;
                                        into.PlayerID = building.OwnerPlayerID;
                                        Item.transfer(from, into);

                                    }
                                    foreach (Item item in itemsToConsume)
                                    {
                                        SourceInfo from = new SourceInfo(SourceInfo.MODE_BUILDING_PRODUCTION_STASH);
                                        from.BuildingID = building.ID;
                                        from.BuildingProductionID = armyProduction.ID;
                                        from.ItemID = item.ID;
                                        from.Quantity = item.Quantity;
                                        from.AllowConsumption = true;
                                        SourceInfo into = new SourceInfo(SourceInfo.MODE_ENTITY_EQUIPMENT);
                                        into.AllowConsumption = true;
                                        into.EntityID = unit.UnitID;
                                        into.PlayerID = building.OwnerPlayerID;
                                        Item.transfer(from, into);

                                    }
                                    CharacterTemplate upgradedTemplate = GameEngine.Data.CharacterTemplateCollection.findByKeyword(armyProduction.UnitName);
                                    unit.CharacterTemplateKeyword = upgradedTemplate.Keyword;
                                    unit.BodyTemplateKeyword = upgradedTemplate.BodyTemplateKeyword;
                                    unit.UnitAppearance = upgradedTemplate.CombatPicture;
                                    unit.UnitPortrait = upgradedTemplate.PortraitPicture;
                                    BuildingProductionTemplate buildingProductionTemplate = GameEngine.Data.BuildingProductionTemplateCollection.findByKeyword(armyProduction.Keyword);
                                    if (buildingProductionTemplate.ReplaceOldUpkeep)
                                    {
                                        unit.UpKeep.Costs.Clear();
                                        foreach (TemplateOdd item in upgradedTemplate.UpkeepCosts)
                                        {
                                            unit.UpKeep.Costs.Add(new UpkeepCost(ObjectCopier.Clone(item)));
                                        }
                                        //unit.UpKeep.Costs = ObjectCopier.Clone<List<>>(upgradedTemplate.UpkeepCosts);
                                      
                                    }
                                }


                            }
                            else //entity doesnt need an upgrade
                            {
                                //giving missing items
                                foreach (ProductionRecipeRequest desire in armyProduction.RecipeRequests)
                                {
                                    switch (desire.Mode)
                                    {
                                        case ProductionRecipeRequest.TYPE_ENTITY:
                                        case ProductionRecipeRequest.TYPE_TRAINING:
                                            break;
                                        case ProductionRecipeRequest.TYPE_ITEM:
                                            int amountNeeded = desire.RequiredAmountForEntity;

                                            int existingAmount = unit.Inventory.GetCorrectAmountByTypes(desire.Types, desire.NotWantedTypes);

                                            amountNeeded = amountNeeded - existingAmount;
                                            if (amountNeeded > 0)
                                            {
                                                if (armyProduction == null)
                                                {
                                                    Debug.LogError("armyproduction is null");
                                                }
                                                if (armyProduction.Stash == null)
                                                {
                                                    Debug.LogError("armyproduction stash is null");
                                                }
                                                if (desire == null)
                                                {
                                                    Debug.LogError("desire is null");
                                                }
                                                itemsToGive = armyProduction.Stash.FindCorrectTypeItems(desire.Types, desire.NotWantedTypes);
                                                foreach (Item item in itemsToGive)
                                                {



                                                    SourceInfo from = new SourceInfo(SourceInfo.MODE_BUILDING_PRODUCTION_STASH);
                                                    from.BuildingID = building.ID;
                                                    from.BuildingProductionID = armyProduction.ID;
                                                    from.ItemID = item.ID;
                                                    if (item.Quantity < amountNeeded)
                                                    {
                                                        from.Quantity = item.Quantity;
                                                        amountNeeded -= item.Quantity;
                                                    }
                                                    else
                                                    {
                                                        from.Quantity = amountNeeded;
                                                        amountNeeded = 0;
                                                    }


                                                    SourceInfo into = new SourceInfo(SourceInfo.MODE_ENTITY_EQUIPMENT);
                                                    into.AllowConsumption = true;
                                                    into.EntityID = unit.UnitID;
                                                    into.PlayerID = building.OwnerPlayerID;
                                                    Item.transfer(from, into);
                                                    if (amountNeeded == 0)
                                                    {
                                                        break;
                                                    }
                                                }

                                            }
                                            break;
                                        default:



                                            Debug.LogError("unknown type " + desire.Mode + " of desire: " + desire.GetInformation());
                                     

                                            break;
                                    }
                                }

                            }




                            priorityOneUnits.AddSortedByPower(unit);

                            if (debug)
                            {
                                Debug.Log("entity put into higher priority: " + unit.UnitName + " " + unit.UnitID);
                            }
                        }
                        else
                        {
                            lowerPriorityUnits.AddSortedByPower(unit);

                            if (debug)
                            {
                                Debug.Log("unit put into lower priority: " + unit.UnitName + " " + unit.UnitID);
                            }

                        }

                    }


                }
                else
                {

                    if (debug)
                    {
                        if (unitsToSupply.Units.Count > 0)
                        {
                            Debug.Log("missing desire for units, therefore lowering attitude of entities: ");
                        }
                    }

                    //if we have no desire for any units, decrease attitude
                    foreach (Entity entity in unitsToSupply.Units)
                    {
                        entity.AddToAttitude(entity.FindCurrentOwnerID(), -10, -10);
                        if (debug)
                        {
                            Debug.Log("attitude of entity lowered: " + entity.UnitName + " " + entity.UnitID);
                        }
                    }
                    break;
                }

 













                //we compile the final sorted list
                Army compiledList = new Army(-1, null);
                compiledList.Units.AddRange(priorityOneUnits.Units);
                compiledList.Units.AddRange(lowerPriorityUnits.Units);
                if (debug)
                {
                    Debug.Log("higher priority units count: " + priorityOneUnits.Units.Count);
                    Debug.Log("lower priority units count: " + lowerPriorityUnits.Units.Count);
                    Debug.Log("production stash count: " + armyProduction.Stash.Count);
                }
                //we go through compiled list and try to supply the units               
                for (int i = 0; i < compiledList.Units.Count; i++)
                {
                    Entity unit = compiledList.Units[i];
                    
                    //check if entity within the desired quantity
                    if (i <= targetDesire.DesiredQuantity)
                    {
                       
                        //we go through all the costs and try to match them with items
                        foreach (UpkeepCost stat in unit.UpKeep.Costs)
                        {
                            ItemCollection itemsToGive = new ItemCollection();
                            if (stat.TemplateOdd.TemplateKeyword == "")
                            {
                 
                                itemsToGive = armyProduction.Stash.FindCorrectTypeItems(stat.TemplateOdd.Types, stat.TemplateOdd.NotWantedTypes);
                                itemsToGive.Sort(delegate (Item controller1, Item controller2) { return controller1.CurrentValue.CompareTo(controller2.CurrentValue); });

                            }
                            else
                            {
                                itemsToGive = armyProduction.Stash.GetItemsByKeyword(stat.TemplateOdd.TemplateKeyword);

                            }
                            int amountNeeded = stat.TemplateOdd.MinQuantity;
                            foreach (Item item in itemsToGive)
                            {



                                SourceInfo from = new SourceInfo(SourceInfo.MODE_BUILDING_PRODUCTION_STASH);
                                from.BuildingID = building.ID;
                                from.BuildingProductionID = armyProduction.ID;
                                from.ItemID = item.ID;
                                if (item.Quantity < amountNeeded)
                                {
                                    from.Quantity = item.Quantity;
                                    amountNeeded -= item.Quantity;
                                }
                                else
                                {
                                    from.Quantity = amountNeeded;
                                    amountNeeded = 0;
                                }


                                SourceInfo into = new SourceInfo(SourceInfo.MODE_ENTITY_STASH);
                                into.EntityID = unit.UnitID;

                                Item.transfer(from, into);
                                if (amountNeeded == 0)
                                {
                                    break;
                                }
                            }

                            if (amountNeeded > 0)
                            {
                                unit.AddToAttitude(unit.FindCurrentOwnerID(), -5, -5);
                                if (debug)
                                {
                                    Debug.Log(unit.UnitName + " " + unit.UnitID + " did not recieve desired upkeep: " + unit.UpKeep.GetInformation());
                                }
                            }
                            else
                            {
                                unit.AddToAttitude(unit.FindCurrentOwnerID(), 1, 1);
                                if (debug)
                                {
                                    Debug.Log(unit.UnitName + " " + unit.UnitID + " recieved upkeep: " + unit.UpKeep.GetInformation());
                                }
                            }
  

                        }
                    }
                    else //unit is not within the desired quantity and doesnt get supplied, therefore unhappy
                    {
                        unit.AddToAttitude(unit.FindCurrentOwnerID(), -10, -10);
                        if (debug)
                        {
                            Debug.Log(unit.UnitName + " " + unit.UnitID + " didnt make it into desired quantity , being at " + i);
                        }
                    }
                 
                }

                 
            }

 

        }
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Stop();
            Debug.Log("Scenario.SendSuppliesPhase took " +GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
        }
    }

    //public void RecieveSupplyPhase()
    //{
    //    bool timer = false;
    //    if (timer)
    //    {
    //        GameEngine.ActiveGame.GameStopwatch.Reset();
    //        GameEngine.ActiveGame.GameStopwatch.Start();
    //    }

    //    bool debuglog = false;

    //    Army allUnitsInGame = GetAllUnitsInTheGame(true);

    //    foreach (Entity unit in allUnitsInGame.Units)
    //    {
    //        if (debuglog) {
    //            OurLog.Print("Supplies total: " + unit.UpKeep.Supplies.Count + " of unit: " + unit.UnitID);
    //        }
           
    //        SupplyList suppliesToBeProccessed = new SupplyList();
    //        SupplyList suppliesLeft = new SupplyList();
    //        foreach (Supply supply in unit.UpKeep.Supplies)
    //        {
    //            if (supply.ArriveAtTurn == turncounter)
    //            {
    //                suppliesToBeProccessed.Add(supply);
    //            }
    //            else
    //            {
    //                suppliesLeft.Add(supply);
    //            }
    //        }

    //        if (debuglog)
    //        {
    //            OurLog.Print("Supplies left:" + suppliesLeft.Count);
    //        }
    //        // 
    //        unit.UpKeep.Supplies = suppliesLeft;

    //        if (debuglog)
    //        {
    //            OurLog.Print("supply to be processed count:" + suppliesToBeProccessed.Count);
    //        }

    //        //  
    //        foreach (Supply supply in suppliesToBeProccessed)
    //        {
    //            if (debuglog)
    //            {
    //                OurLog.Print("Processing Supplies for this unit: " + unit.UnitName);
    //            }
             
    //            //unit.UpKeep.Reserves.AddToExistingValueFromList(supply.Cargo);
    //        }

    //    }
    //    if (timer)
    //    {
    //        GameEngine.ActiveGame.GameStopwatch.Stop();
    //        Debug.Log("Scenario.RecieveSupplyPhase took " + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
    //    }
    //}

    public void DisplayArmiesInDebugLog() {
  
        OurLog.Print("Army Count: " + armies.Count);
        foreach (Army army in armies) {
            OurLog.Print("Army: " + army.ArmyID + "(" + army.Units.Count + "u)" + " leaderID: " + army.LeaderID);
        }

    }

    public int CheckUnitDistanceFromBuildingForSupply(int unitID, int coord_X, int coord_Y, int coord_Z,int supplySpeed)
    {

        foreach (Army army in armies)
        {
            //if (army.WorldMapPositionX == coord_X && army.WorldMapPositionY == coord_Y)
            //{
                foreach (Entity unit in army.Units)
                {
                    if (unit.UnitID == unitID)
                    {
                    MapSquare ArmymapSquare = worldmap.FindMapSquareByCordinates(army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);
                    MapSquare supplymapSquare = worldmap.FindMapSquareByCordinates(coord_X, coord_Y);
                    List<MapSquare> mapSquares = worldmap.FindFullPath(army, supplymapSquare, ArmymapSquare, null);
                    if (mapSquares.Count == 0)
                    {
                        return 0;
                    }
                    else
                    {
                        return Math.Max(1, mapSquares.Count / 3);
                    }

                        
                    }
                }
            //}

        }
        GameSquare gameSquare = worldmap.FindMapSquareByCordinates(coord_X, coord_Y, coord_Z) as GameSquare;
        if (gameSquare.building != null)
        {
            foreach (Entity unit in gameSquare.building.Storage.Units)
            {
                if (unit.UnitID == unitID)
                {
                    return 0;
                }
            }

         
        }
       
        return 0;
    }



    public void UnAssignHeroesBuildings(Entity entity) 
    {
        if (entity.IsHeroFlag)
        {
            List<Building> buildings = GetEntityBuildings(entity.UnitID);

            foreach (Building building in buildings)
            {
                building.OwnerHeroID = -1;
            }
        }

    }


    public void SendArmiesToHeroesPhase()
    {
        bool timer = false;
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Reset();
            GameEngine.ActiveGame.GameStopwatch.Start();
        }

        bool debuglog = false;

        List<Army> armiesToAdd = new List<Army>();
        List<Entity> unitsToRemove = new List<Entity>();
        List<Building> allbuildings = worldmap.GetAllBuildings();

        //Decision-making phase
        if (debuglog) {
            OurLog.Print("*************** Beginning of SendArmiesToHeroesPhase ******* turn " + turncounter);
            DisplayArmiesInDebugLog();
        }
        #region oldCode
        //// we should cycle all the armies
        //// we check if army leader has gone missing or dead
        //// we check all the units
        //// We should check if unit's building ownership leader is different than armies leader
        //// we should check if army has leader present or if leader is still alive. If not, we gonna try send reinforcement army back to barracks.
        //// if we need to move units to new leader, we should create/or add to army that will be reinforcement army towards the other leader


        ////foreach (Army army in armies)
        ////{

        ////    bool leaderMissing = false;

        ////    // we check if army has a leader (hero) that has gone missing or dead

        ////    if (army.LeaderID > 0)
        ////    {
        ////        Unit leader = FindUnitByUnitID(army.LeaderID);
        ////        if (leader == null)
        ////        {
        ////            if (debuglog)
        ////            {
        ////                OurLog.Print("Army " + army.ArmyID + " should have leader, (ID: " + army.LeaderID + ") , but leader is gone from game");
        ////            }

        ////            leaderMissing = true;
        ////        }
        ////        else
        ////        {

        ////            if (leader.UnitHp <= 0)
        ////            {

        ////                if (debuglog)
        ////                {
        ////                    OurLog.Print("Army " + army.ArmyID + " with a DEAD leader (ID: " + army.LeaderID + ") , Disbanding");
        ////                }

        ////                leaderMissing = true;
        ////            }

        ////        }

        ////    }


        ////    foreach (Unit unit in army)
        ////    {

        ////        if (unit.UpKeep != null)
        ////        {

        ////            Building building = this.FindBuildingByID(unit.UpKeep.BuildingID);

        ////            if (building != null)
        ////            {

        ////                //  lets not send armies back to enemies - in movement code?
        ////                //  GameSquare gameSquare = Worldmap.FindGameSquareByBuildingID(building.ID);

        ////                Army sendarmy = new Army(armyIdCounter++);
        ////                sendarmy.Add(unit);
        ////                sendarmy.WorldMapPositionX = army.WorldMapPositionX;
        ////                sendarmy.WorldMapPositionY = army.WorldMapPositionY;
        ////                sendarmy.OwnerPlayerID = army.OwnerPlayerID;
        ////                sendarmy.SetMission(Mission.mission_ReturnToBuildingStorage, 0, building.ID, army.ArmyID);
        ////                armiesToAdd.Add(sendarmy);

        ////            }

        ////        }

        ////    }


        ////}



        //foreach (Army army in armies)
        //{

        //    // First we check if army has any units in it, if it doesnt, we set it to disband
        //    if (army.Units.Count == 0)
        //    {
        //        if (debuglog) {
        //            OurLog.Print("Army ID(" + army.ArmyID + ") should'nt exist, it has 0 units");
        //        }

        //        army.Mission = new Mission();
        //        army.SetMission(Mission.mission_Disband, 0, 0, 0);
        //        continue;
        //    }

        //    // we check if army leader still exists. 
        //    // we also check if leader is dead, with hp < 0
        //    // if leader is dead or missing, we gonna flag army for disband
        //    if (army.LeaderID > 0)
        //    {
        //        Entity leader = FindUnitByUnitID(army.LeaderID);
        //        if (leader == null)
        //        {
        //            if (debuglog)
        //            {
        //                OurLog.Print("Army " + army.ArmyID + " does not have a leader ID: " + army.LeaderID + " , Disbanding");
        //            }

        //            army.SetMission(Mission.mission_Disband, 0, 0, 0);
        //        }
        //        else {

        //            if (leader.UnitHp <= 0) {

        //                if (debuglog)
        //                {
        //                    OurLog.Print("Army " + army.ArmyID + " with a DEAD leader (ID: " + army.LeaderID + ") , Disbanding");
        //                }

        //                army.SetMission(Mission.mission_Disband, 0, 0, 0);
        //            }

        //        }

        //    }

        //    // we check Reinforce mission legality, if hero is still alive. If not, we gonna try send reinforcement army back to barracks.
        //    // if barracks is also destroyed, we gonna set army to disband

        //    if (army.Mission != null)
        //    {
        //        if (army.Mission.missionName == Mission.mission_Reinforce)
        //        {
        //            int destination_hero_ID = army.Mission.targetID;

        //            // our first check is if our target hero is still alive
        //            Entity targetHero = this.FindUnitByUnitID(destination_hero_ID);

        //            if (targetHero == null)
        //            {
        //                if (debuglog)
        //                {
        //                    OurLog.Print("Hero for our reinforcement army is missing, Reinforce mission changed to going back to building storage");
        //                }

        //                // Hero is dead or missing from the map.
        //                army.Mission = new Mission();
        //                army.SetMission(Mission.mission_ReturnToBuildingStorage, 0, 0, 0);

        //            }
        //            else {

        //                Building building = this.FindBuildingByID(army.Mission.sourceID);

        //                if (building == null)
        //                {
        //                    if (debuglog)
        //                    {
        //                        OurLog.Print("This building is nowhere to found, it has been razed");
        //                    }

        //                }
        //                else
        //                {

        //                    GameSquare gameSquare = worldmap.FindGameSquareByBuildingID(building.ID);
        //                    int gameSquareOwnerHeroID = gameSquare.building.OwnerHeroID;

        //                    // lets check if the same player has another hero that has captured the building 
        //                    // while reinforcement armies are en-route, so we can change the army destination to that new hero
        //                    if (army.OwnerPlayerID == gameSquare.building.OwnerPlayerID)
        //                    {

        //                        if (gameSquareOwnerHeroID != destination_hero_ID)
        //                        {
        //                            // we will reinforce the other hero instead
        //                            army.Mission.targetID = gameSquareOwnerHeroID;  // we can change the army destination to that new hero

        //                        }

        //                    }
        //                    else
        //                    {
        //                        // Building is probably captured by another player, we do not change reinforcement destination,
        //                        // and they might run out of Supplies if building is not recaptured (and starve and leave when upkeep phase)

        //                    }

        //                }
        //            }

        //        }

        //        if (army.Mission.missionName == Mission.mission_ReturnToBuildingStorage) {

        //            Building building = this.FindBuildingByID(army.Mission.targetID);

        //            if (building == null) {

        //                army.Mission = new Mission();
        //                army.SetMission(Mission.mission_Disband, 0, 0, 0);

        //                if (debuglog)
        //                {
        //                    OurLog.Print("Return to building mission changed to disband");
        //                }

        //            }

        //        }



        //        // we check for each unit, that if we need to send units from one hero to another (same player)
        //        if (army.Mission.missionName != Mission.mission_Disband) {


        //            if (army.LeaderID > 0)
        //            {
        //                Entity leader = FindUnitByUnitID(army.LeaderID);
        //                Army unitsToRemove = new Army(-1,null);

        //                foreach (Entity unit in army.Units)
        //                {
        //                    if (unit.UpKeep != null) {

        //                        if (unit.UpKeep.BuildingID > 0) {

        //                            Building building = FindBuildingByID(unit.UpKeep.BuildingID);

        //                            if (building != null) {

        //                                GameSquare gameSquare = Worldmap.FindGameSquareByBuildingID(building.ID);
        //                                if (gameSquare.building.OwnerHeroID != army.LeaderID && gameSquare.building.OwnerPlayerID == army.OwnerPlayerID) {

        //                                    if (debuglog)
        //                                    {
        //                                        OurLog.Print("Removing Unit ID" + unit.UnitID + " from Army ID:" + army.ArmyID);
        //                                    }

        //                                    unitsToRemove.Units.Add(unit);
        //                                    Player player = FindPlayerByID(army.OwnerPlayerID);
        //                                    Army sendArmy = new Army(armyIdCounter++,player);
        //                                    armiesToAdd.Add(sendArmy);
        //                                    sendArmy.Units.Add(unit);
        //                                    sendArmy.OwnerPlayerID = army.OwnerPlayerID;
        //                                    sendArmy.SetMission(Mission.mission_Reinforce, 0, gameSquare.building.OwnerHeroID, building.ID);

        //                                }

        //                            }

        //                        }

        //                    }
        //                }

        //                foreach (Entity unitToRemove in unitsToRemove.Units) {

        //                    army.Units.Remove(unitToRemove);
        //                }

        //            }


        //        }


        //    }

        //}

        //if (debuglog)
        //{
        //    OurLog.Print(" Before disband check ");
        //    DisplayArmiesInDebugLog();
        //}



        //List<Army> armiesToKeep = new List<Army>();


        //// Disband check
        //foreach (Army army in armies)
        //{
        //    bool keep = true;

        //    if (army.Mission != null)
        //    {
        //        if (army.Mission.missionName == Mission.mission_Disband)
        //        {
        //            if (debuglog)
        //            {
        //                OurLog.Print("Disbanding units in army ID:" + army.ArmyID);
        //            }

        //            keep = false;
        //            foreach (Entity unit in army.Units) {

        //                if (unit.UpKeep != null) {

        //                    Building building = this.FindBuildingByID(unit.UpKeep.BuildingID);

        //                    if (building != null) {

        //                        // TODO lets not send armies back to enemies - in movement code?
        //                        //  GameSquare gameSquare = Worldmap.FindGameSquareByBuildingID(building.ID);

        //                        Army sendarmy = new Army(armyIdCounter++,FindPlayerByID(army.OwnerPlayerID));
        //                        sendarmy.Units.Add(unit);
        //                        sendarmy.WorldMapPositionX = army.WorldMapPositionX;
        //                        sendarmy.WorldMapPositionY = army.WorldMapPositionY;
        //                        sendarmy.OwnerPlayerID = army.OwnerPlayerID; 
        //                        sendarmy.SetMission(Mission.mission_ReturnToBuildingStorage,0, building.ID, army.ArmyID);
        //                        armiesToAdd.Add(sendarmy);

        //                    }

        //                }

        //            }

        //        }

        //    }

        //    if (keep) {
        //        armiesToKeep.Add(army);
        //    }
        //}

        ////OurLog.Print(" After disband check ");
        ////DisplayArmiesInDebugLog();
        //armies.Clear();
        //armies.AddRange(armiesToKeep);

        //if (debuglog)
        //{
        //    OurLog.Print("New armies to add: " + armiesToAdd.Count);
        //}

        //foreach (Army army in armiesToAdd) {
        //    armies.Add(army);
        //}
        ////OurLog.Print(" After adding new armies ");
        ////DisplayArmiesInDebugLog();

        //List<Building> allbuildings = worldmap.GetAllBuildings();

        //foreach (Building building in allbuildings)
        //{
        //    if (building.Storage.Units.Count > 0)
        //    {
        //        GameSquare gameSquare = worldmap.FindGameSquareByBuildingID(building.ID);
        //        if (gameSquare.building.OwnerHeroID > 0)
        //        {
        //            Army sendarmy = new Army(armyIdCounter++,FindPlayerByID(building.OwnerPlayerID));
        //            Entity targethero = FindUnitByUnitID(gameSquare.building.OwnerHeroID);

        //            if (targethero == null) {

        //                if (debuglog)
        //                {
        //                    OurLog.Print("Building owner hero seems to be dead! " + building.TemplateKeyword + " ID:" + building.ID + " Hero ID: " + gameSquare.building.OwnerHeroID);
        //                }

        //                gameSquare.building.OwnerHeroID = 0; //should we really remove hero ownership here?
        //                continue;
        //            }

        //            Army targetarmy = FindArmyByUnit(targethero);
        //            sendarmy.WorldMapPositionX = gameSquare.X_cord;
        //            sendarmy.WorldMapPositionY = gameSquare.Y_cord;
        //            sendarmy.OwnerPlayerID = gameSquare.building.OwnerPlayerID;
        //            if (targetarmy.IsOverland)
        //            {

        //                foreach (Entity unit in building.Storage.Units)
        //                {
        //                    sendarmy.Units.Add(unit);
        //                }

        //                sendarmy.SetMission(Mission.mission_Reinforce, 0, targethero.UnitID, building.ID);
        //                Armies.Add(sendarmy);
        //                building.Storage = new Army(-1,null);
        //            }

        //        }
        //    }
        //}

        ////movement phase

        //if (debuglog)
        //{
        //    OurLog.Print("Armies count before movement");
        //    DisplayArmiesInDebugLog();
        //}



        //armiesToKeep = new List<Army>();

        //foreach (Army army in armies)
        //{
        //    bool keep = true;

        //    if (army.Mission != null)
        //    {
        //        MapSquare armylocationSquare = worldmap.FindMapSquareByCordinates(army.WorldMapPositionX, army.WorldMapPositionY);
        //        MapSquare armyDestinationSquare = null;
        //        Entity destinationHero = null;
        //        Army destinationArmy = null;
        //        Building destinationBuilding = null;

        //        if (army.Mission.missionName == Mission.mission_Reinforce)
        //        {

        //            destinationHero = FindUnitByUnitID(army.Mission.targetID);
        //            if (destinationHero == null)
        //            {
        //                continue;
        //            }
        //            destinationArmy = FindArmyByUnit(destinationHero);
        //            if (destinationArmy == null)
        //            {
        //                continue;
        //            }
        //            armyDestinationSquare = worldmap.FindMapSquareByCordinates(destinationArmy.WorldMapPositionX, destinationArmy.WorldMapPositionY);

        //        }

        //        if (army.Mission.missionName == Mission.mission_ReturnToBuildingStorage)
        //        {
        //            GameSquare gameSquare = Worldmap.FindGameSquareByBuildingID(army.Mission.targetID);

        //            if (gameSquare != null)
        //            {

        //                armyDestinationSquare = gameSquare;

        //            }
        //            else
        //            {
        //                if (debuglog)
        //                {
        //                    OurLog.Print("Building destroyed??? in movement phase, our army will stand still and will disband next turn");
        //                }

        //                army.Mission.missionName = Mission.mission_Disband;
        //            }

        //        }

        //        if (armyDestinationSquare != null)
        //        {
        //            OurMapSquareList mapSquares = worldmap.FindFullPath(army, armylocationSquare, armyDestinationSquare, null);
        //            MoveAlongPath(mapSquares, army, armylocationSquare, 0.5);

        //            if (army.WorldMapPositionX == armyDestinationSquare.X_cord && army.WorldMapPositionY == armyDestinationSquare.Y_cord)
        //            {
        //                if (army.CurrentArmyMovementPoints() * 2 >= army.MaximumArmyMovementPoints())
        //                {


        //                    keep = false;
        //                    if (destinationArmy != null)
        //                    {
        //                        if (debuglog)
        //                        {
        //                            OurLog.Print("units from army " + army.ArmyID + " are joining the army(ID:" + destinationArmy.ArmyID + ")");
        //                        }

        //                        foreach (Entity unit in army.Units)
        //                        {
        //                            unit.RefreshMovementPoints();
        //                            destinationArmy.Units.Add(unit);
        //                        }

        //                    }
        //                    else
        //                    {
        //                        if (destinationBuilding != null)
        //                        {
        //                            if (debuglog)
        //                            {
        //                                OurLog.Print("units are joining the building");
        //                            }

        //                            foreach (Entity unit in army.Units)
        //                            {
        //                                unit.RefreshMovementPoints();
        //                                destinationBuilding.Storage.Units.Add(unit);

        //                            }

        //                        }
        //                    }
        //                }
        //            }


        //            //     OurLog.Print("Army id: " + army.ArmyID + " is moving to hero(id): " + destinationHero.UnitID + " and has " + mapSquares.Count + " squares to move");




        //        }



        //    }

        //    if (keep)
        //    {
        //        armiesToKeep.Add(army);
        //    }
        //}

        //armies.Clear();
        //armies.AddRange(armiesToKeep);

        //if (debuglog)
        //{
        //    OurLog.Print("Armies count after movement");
        //    DisplayArmiesInDebugLog();
        //}
        #endregion
        //// we should cycle all the armies
        //// we check all the units
        //every unit that has a building, we compare if building's owner hero matches leader id, if it doesnt, we reinforce if alive/overland, send to building if dead/not overland
        //if building has lost owner hero id, then it goes back to storage
        //if unit is reinforcement army, if building leader changed to different player's, dont change destination
        //if building owner is different player, then dont send back or reinforce
        //if building is destroyed, then dont send back or reinforce
        //remove empty armies

        foreach (Army army in armies)
        {

            if (army.Location.Mode == Location.MODE_BUILDING_GARRISON)
            {
                continue;
            }
            Entity leader = FindUnitByUnitID(army.LeaderID);
            unitsToRemove = new List<Entity>();
            //setting mission
            foreach (Entity unit in army.Units)
            {
                if (unit.IsHeroFlag)
                {
                    continue;
                }
                Building unitBuilding = FindBuildingByID(unit.UpKeep.BuildingID);
                if (unitBuilding == null) //if no building, stays in army
                {
                    continue; //nothing happens
                }
                if (unitBuilding.OwnerPlayerID != unit.FindCurrentOwnerID()) //if building owner is different player, stays in army or continues with the mission
                {
                    continue; //also nothing happens
                }
                //if building has no hero, or if hero is on a quest,or buildingproduction gone/inactive, return to building
                if (unitBuilding.OwnerHeroID <= 0 || FindUnitByUnitID(unitBuilding.OwnerHeroID) == null || !unitBuilding.IsEntitysBuildingProductionIsSupported(unit.UnitID) || leader.HasMission(Mission.mission_Quest)) 
                {

                    unitsToRemove.Add(unit);
                    Army newArmy = new Army(++ArmyIdCounter, FindPlayerByID(unit.FindCurrentOwnerID()));
                    newArmy.Location = new Location(army.Location.WorldMapCoordinates.XCoordinate,army.Location.WorldMapCoordinates.YCoordinate);
                    newArmy.Units.Add(unit);
                    newArmy.LeaderID = unit.UnitID;
                    newArmy.OwnerPlayerID = unit.FindCurrentOwnerID();
                    armiesToAdd.Add(newArmy);
                    unit.SetMission(Mission.mission_ReturnToBuildingStorage,unit.UnitID,unitBuilding.ID,army.ArmyID);
                    continue;
                }
                if (unitBuilding.OwnerHeroID != leader.UnitID) //if building owner is not the army leader, creates new one with destination to that one
                {
                    if (unit.Mission != null)
                    {
                        if (unit.Mission.MissionName == Mission.mission_Reinforce)
                        {
                            if (unit.Mission.TargetID == unitBuilding.OwnerHeroID)
                            {
                                continue;
                            }
                        }
                    }
                    unitsToRemove.Add(unit);
                    Army newArmy = new Army(++ArmyIdCounter, FindPlayerByID(unit.FindCurrentOwnerID()));
                    newArmy.Location = new Location(army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);
                    newArmy.Units.Add(unit);
                    newArmy.LeaderID = unit.UnitID;
                    newArmy.OwnerPlayerID = unit.FindCurrentOwnerID();
                    armiesToAdd.Add(newArmy);
                    unit.SetMission(Mission.mission_Reinforce, unit.UnitID, unitBuilding.OwnerHeroID, army.ArmyID);
                    continue;
                }
            }
            //removing units from old armies
            foreach (Entity unit in unitsToRemove)
            {
                army.Units.Remove(unit);
            }

        }
        
        //reinforcing from building storages
        foreach (Building building in allbuildings)
        {
            GameSquare gameSquare = worldmap.FindGameSquareByBuildingID(building.ID);

             bool sendtroops = false;
             if (gameSquare.building.OwnerHeroID > 0) //if no hero, do nothing
             {

               
                Entity targethero = FindUnitByUnitID(gameSquare.building.OwnerHeroID);
                
                if (targethero == null)
                {

                    if (debuglog)
                    {
                        OurLog.Print("Building owner hero seems to be dead! " + building.TemplateKeyword + " ID:" + building.ID + " Hero ID: " + gameSquare.building.OwnerHeroID);
                    }

                    gameSquare.building.OwnerHeroID = 0; //TODO should we really remove hero ownership here?
                    continue;
                }
                else
                {
                    sendtroops = true;
                }
                List<Entity> units = building.GetReadyEntities(sendtroops);



                Notification notification = new Notification();
                notification.IsOverland = true;
                notification.TargetID = building.ID;
                notification.Type = Notification.NotificationType.TYPE_BUILDING_UNIT_COMPLETE;
                notification.HeaderText = building.TemplateKeyword + " has produced new units ";
                notification.ExpandedText = " units produced: ";
       


 

                foreach (Entity unit in units)
                {
                    NotificationElement notificationElement = new NotificationElement();
                    notificationElement.EntityID = unit.UnitID;
                    notificationElement.Picture = unit.GetPicture();
                    notificationElement.Content = unit.UnitName;
                    //CharacterTemplate characterTemplate = GameEngine.Data.CharacterTemplateCollection.findByKeyword(unit.CharacterTemplateKeyword);
                    notificationElement.EntityKeyword = unit.CharacterTemplateKeyword;
                    notification.NotificationElements.Add(notificationElement);

                    Army sendarmy = new Army(++armyIdCounter, FindPlayerByID(building.OwnerPlayerID));
                    unit.SetMission(Mission.mission_Reinforce, unit.UnitID, targethero.UnitID, building.ID);
                    sendarmy.Location = new Location(gameSquare.X_cord, gameSquare.Y_cord);
                    sendarmy.OwnerPlayerID = gameSquare.building.OwnerPlayerID;
                    sendarmy.LeaderID = unit.UnitID;
                    sendarmy.Units.Add(unit);
                    armiesToAdd.Add(sendarmy);
                }


                //foreach (Stat stat in stats)
                //{
                //    notification.ExpandedText += stat.Keyword + " " + stat.Amount + Environment.NewLine;
                //}

                if (units.Count > 0)
                {
                    Player player = GameEngine.ActiveGame.scenario.FindPlayerByID(building.OwnerPlayerID);
                    notification.Picture = units[0].GetPicture(); //we might have multiple different units produced by 1 building, but just show whatever is first
                    notification.ID = ++player.LocalNotificationID;
                    player.Notifications.Add(notification);

                }
            }
         
            
        }

        //adding new armies to army pool
        foreach (Army newArmy in armiesToAdd)
        {
            Armies.Add(newArmy);
        }


        //movement phase
        foreach (Army army in Armies)
        {
            Entity leader = FindUnitByUnitID(army.LeaderID);
            Player player = FindPlayerByID(army.OwnerPlayerID);
            if (leader.Mission != null)
            {
                MapSquare armylocationSquare = worldmap.FindMapSquareByCordinates(army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);

                Army destinationArmy = null;
                if (leader.Mission.MissionName == Mission.mission_Reinforce)
                {
                    destinationArmy = FindOverlandArmyByUnit(FindUnitByUnitID(leader.Mission.TargetID).UnitID);
                }
                Building destinationBuilding = null;
                if (leader.Mission.MissionName == Mission.mission_ReturnToBuildingStorage)
                {
                    destinationBuilding = FindBuildingByID(leader.Mission.TargetID);
                }

                if (destinationArmy != null)
                {
                    MapSquare armyDestinationSquare = worldmap.FindMapSquareByCordinates(destinationArmy.Location.WorldMapCoordinates.XCoordinate, destinationArmy.Location.WorldMapCoordinates.YCoordinate);
                    //OurMapSquareList mapSquares = worldmap.FindPathUsingBreadth(armylocationSquare, armyDestinationSquare);
                    MemoryTile source = player.MapMemory.FindMemoryTileByMapSquareID(armylocationSquare.ID);
                    MemoryTile target = player.MapMemory.FindMemoryTileByMapSquareID(armyDestinationSquare.ID);
                    //     OurMapSquareList mapSquares = worldmap.FindFullPath(army, armylocationSquare, armyDestinationSquare, null);
                    MoveAlongPath(player.MapMemory.FindPathUsingBreadthWithObjectLinks(source,target,army), army, armylocationSquare, 0.5,false,false,true);

                    if (destinationArmy.Location.WorldMapCoordinates.XCoordinate == army.Location.WorldMapCoordinates.XCoordinate && destinationArmy.Location.WorldMapCoordinates.YCoordinate == army.Location.WorldMapCoordinates.YCoordinate)
                    {
                        Debug.Log("army on the square");
                        List<Entity> entitiesToRemove = new List<Entity>();
                        foreach (Entity unit in army.Units)
                        {
                            unit.RefreshMovementPoints();
                            destinationArmy.Units.Add(unit);
                            entitiesToRemove.Add(unit);

                        }

                        foreach (Entity item in entitiesToRemove)
                        {
                            army.Units.Remove(item);
                        }

                        continue;

                    }


                }
                else
                {
                    if (destinationBuilding != null)
                    {
                        if (debuglog)
                        {
                            OurLog.Print("units are joining the building");
                        }

                        foreach (Entity unit in army.Units)
                        {
                            unit.RefreshMovementPoints();
                            // Right now, when we switch to peacetime mode, player keeps the items and units, they are just in storage, and switching back to wartime,
                            // means the armies in storage are most likely fully ready to go out and join hero again, making the tactic of produce the armies first, and then switch
                            // to peacetime a dominant tactic (and it is less of a choice between peace and wartime modes)
                         //   if (destinationBuilding.IsEntitysBuildingProductionIsSupported(unit.UnitID))
                         //   {
                                BuildingProduction buildingProduction = destinationBuilding.GetEntitysBuildingProduction(unit.UnitID);
                                foreach (Item item in unit.Inventory)
                                {
                                    buildingProduction.Stash.AddItem(item);
                                }
                          //  }
                            unit.Inventory.Clear();
                            
                            destinationBuilding.Storage.Units.Add(unit);
                            
                        }

                    }
                }

            }
        }


        List<Army> toRemove = new List<Army>();
        //remove empty armies phase
        foreach (Army army in Armies)
        {
            if (army.Units.Count <= 0)
            {
                toRemove.Add(army);
            }
        }
        foreach (Army army in toRemove)
        {
            Armies.Remove(army);
        }

        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Stop();
            Debug.Log("Scenario.SendArmiesToHeroesPhase took " + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
        }

    }




    public Building FindBuildingByID (int id){

        List<Building> allbuildings = worldmap.GetAllBuildings();

        foreach (Building building in allbuildings) {

            if (building.ID == id) return building;

        }

        return null;
    }


    public void RemoveArmyEventFlag(MapCoordinates mapCoordinates)
    {
        lock (Armies)
        {
            foreach (Army army in Armies)
            {
                if (army.triggeredEventLocation != null)
                {
                    if (army.triggeredEventLocation.XCoordinate == mapCoordinates.XCoordinate && army.triggeredEventLocation.YCoordinate == mapCoordinates.YCoordinate)
                    {
                        army.triggeredEventLocation = null;
                        Debug.Log("setting  triggeredEventLocation as null");
                    }
                }
            }
        }
 
    }
    /// <summary>
    /// playersThatSaved - to prevent saving movement twice
    /// since we do the save before and after moving, if you managed to save the movement before army coords changed, then no need to save the same movement twice
    /// </summary>
    /// <param name="army"></param>
    /// <param name="fromToCoordinates"></param>
    /// <returns></returns>
    OurMyValueList SaveMapOnMovement(Army army, FromToCoordinates fromToCoordinates,ref OurMyValueList playersThatMustSave, bool save)
    {
        OurMyValueList answer = new OurMyValueList();
        foreach (Player player in Players)
        {
            //if (player.PlayerID == Player.Neutral)
            //{
            //    continue;
            //}
            //since we do the save before and after moving, if you managed to save the movement before army coords changed, then no need to save the same movement twice
            //if (playersThatSaved.Contains(player.PlayerID))
            //{
            //    continue;
            //}
            bool canSeeMovement = false;
            bool playerWasSavedToList = false;
            string unitKW = "";
            if (army.OwnerPlayerID == player.PlayerID)
            {
                canSeeMovement = true;
                foreach (Entity ent in army.Units)
                {
                    if (ent.UnitID == army.LeaderID)
                    {
                        unitKW = ent.CharacterTemplateKeyword;
                    }
                }

            }
            else
            {
                MemoryArmy memoryArmy = player.MapMemory.FindMemoryArmyByArmyIDVisible(army.ArmyID);
                if (memoryArmy != null)
                {
                    canSeeMovement = true;
                    unitKW = memoryArmy.CharacterTemplateKeyword;
                }
                
            }
            //if you saw an army, which went out of vision, then you still get the movement animation
            MyValue playerAndUnitKW = playersThatMustSave.findMyValueByKeyword(player.PlayerID);
            if (playerAndUnitKW != null)
            {
                playerWasSavedToList = true;
                canSeeMovement = true;
                unitKW = playerAndUnitKW.Value;
            }
    
            if (canSeeMovement)//|| mapMemory.IsSeeingSquare(fromToCoordinates.toX,fromToCoordinates.toY)  && (mapMemory.IsSeeingSquare(fromToCoordinates.fromX,fromToCoordinates.fromY))
            {
                if (save)
                {
                    if (player.PlayerID != army.OwnerPlayerID)
                    {
                        //if not doing this, then update comes at the very end, so replays become inconclusive
                        //army's own player does this in a different place
                        GameEngine.ActiveGame.UpdateMemoryMapWithActiveVision(player);
                    }

                    Debug.Log("adding replay of army: " + army.OwnerPlayerID + " to " + player.PlayerID + " from to coords: " + fromToCoordinates.fromX + " " + fromToCoordinates.fromY + " " + fromToCoordinates.toX + " " + fromToCoordinates.toY);
                    OverlandReplay replay = new OverlandReplay(player.MapMemory);
                    replay.ArmyID = army.ArmyID;
                    replay.ArmyCoordinates = new MapCoordinates(army.Location.WorldMapCoordinates.XCoordinate,army.Location.WorldMapCoordinates.YCoordinate);
                    AnimationMovementInfo animationMovementInfo = new AnimationMovementInfo();
                    animationMovementInfo.FromToCoordinates = fromToCoordinates;
                    animationMovementInfo.UnitKeyword = unitKW;
                    animationMovementInfo.PlayerID = army.OwnerPlayerID;
                    replay.AnimationMovementInfo = animationMovementInfo;
                    player.MapMemory.AddReplay(replay);
                    if (player.PlayerID != army.OwnerPlayerID)
                    {
                        Player armyPlayer = FindPlayerByID(army.OwnerPlayerID);
                        Notification enemyMovementNotification = player.GetEnemyMovementNotification(army.ArmyID);
                        if (enemyMovementNotification != null)
                        {
                         
                            //if notification wasnt removed, then flashing the colors back
                            enemyMovementNotification.BgImageR = armyPlayer.Color1;
                            enemyMovementNotification.BgImageG = armyPlayer.Color2;
                            enemyMovementNotification.BgImageB = armyPlayer.Color3;
                            enemyMovementNotification.BgImageA = 255;
                            enemyMovementNotification.HeaderText = "Army movement of " + army.OwnerPlayerID + " detected (Unseen)";

                            CharacterTemplate characterTemplate = GameEngine.Data.CharacterTemplateCollection.findByKeyword(unitKW);
                            enemyMovementNotification.Picture = characterTemplate.CombatPicture;
                        }
                        else
                        {
                            enemyMovementNotification = new Notification();
                            enemyMovementNotification.ID = ++player.LocalNotificationID;
                            enemyMovementNotification.BgImageR = armyPlayer.Color1;
                            enemyMovementNotification.BgImageG = armyPlayer.Color2;
                            enemyMovementNotification.BgImageB = armyPlayer.Color3;
                            enemyMovementNotification.BgImageA = 255;
                            enemyMovementNotification.Type = Notification.NotificationType.TYPE_ENEMY_ARMY_MOVEMENT_DETECTED;
                            enemyMovementNotification.PlayerID = army.OwnerPlayerID;
                            enemyMovementNotification.TargetID = army.ArmyID;
                            enemyMovementNotification.HeaderText = "Army movement of " + army.OwnerPlayerID + " detected (Unseen)";
                            //enemyMovementNotification.Picture = "Poneti/Skills/Warrior/Warriorskill_22";
                            CharacterTemplate characterTemplate = GameEngine.Data.CharacterTemplateCollection.findByKeyword(unitKW);
                            enemyMovementNotification.Picture = characterTemplate.CombatPicture;
                            lock (player.Notifications)
                            {
                                player.Notifications.Add(enemyMovementNotification);
                            }
                            //refresh ui is called far outside of this function
                        }
                    }
                    save = false;
                }
                //for gathering the list in 1st iteration
                if (playerWasSavedToList) //since now threaded, using the reference variable for this
                {
                    lock (playersThatMustSave)
                    {
                        playersThatMustSave.Add(new MyValue(player.PlayerID, unitKW)); //used to be answer
                    }
                 
                }
              
            }
        }
        return answer;
    }

    public void MoveAlongPath(MapMemory mapSquares, Army army, MapSquare source, double modifier, bool sendMP, bool selectInUI,bool calledFromPhase)
    {
        Debug.Log("army is moving: " + army.GetInformation() + " " + army.GetInformationWithUnits());
        //  GameObject controllerObj = GameEngine.ActiveGame.FindPlayerControllerGameObject(army.OwnerPlayerID);
        // PlayerController playerController = null;
        //if (controllerObj != null)
        //{
        //    playerController = controllerObj.GetComponent<PlayerController>();
        //}
        army.isMoving = true;
        if (army.Location.Mode == Location.MODE_BUILDING_GARRISON)
        {
            Debug.Log("MoveAlongPath army is garisson, not moving");
            //if (playerController != null)
            //{
            //    playerController.Selection.SelectArmy(army.ArmyID);
            //    playerController.RefreshUI();

            //}
            Player playerr = FindPlayerByID(army.OwnerPlayerID);
            if (!playerr.isAI)
            {
          //      GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SELECT_ARMY_AND_REFRESH, army.ArmyID.ToString()));
            }
         
            return;
        }
        MemoryTile targetsquare;
        bool refreshIfCancellingMovement = false;
        Player plr = FindPlayerByID(army.OwnerPlayerID);
   
        while (mapSquares.Count > 0)
        {
            targetsquare = mapSquares[0];
            mapSquares.RemoveAt(0);
            bool success = Movement(army, source, targetsquare.gameSquare, modifier, sendMP,calledFromPhase);
            
            if (!success)
            {
                refreshIfCancellingMovement = true;
                break;
            }
            //if (playerController != null)
            //{
            //    playerController.RefreshUI();
            //}
           
            if (!plr.isAI && selectInUI && !calledFromPhase)
            {
                //Debug.Log("ghee");
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SELECT_ARMY_AND_REFRESH, army.ArmyID.ToString()));
            }
 
            source = targetsquare.gameSquare;
            if (!calledFromPhase && mapSquares.Count > 0)
            {
                Thread.Sleep(600);
            }
       
        }
        //if (playerController != null)
        //{
        //    playerController.Selection.SelectArmy(army.ArmyID);
        //    playerController.RefreshUI();
        //} //no idea why we refresh here again
        army.isMoving = false;
        if (!plr.isAI && selectInUI && !calledFromPhase && refreshIfCancellingMovement)
        {
            Debug.Log("ghee 2");
            //GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.COMMAND_REFRESH_UI));
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SELECT_ARMY_AND_REFRESH, army.ArmyID.ToString()));
        }
    }

    public void RemoveExpiredGameSquareEvents()
    {
        bool timer = false;
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Reset();
            GameEngine.ActiveGame.GameStopwatch.Start();
        }
     
        foreach (GameSquare gamesqr in Worldmap.MapSquares)
        {
            gamesqr.RemoveExpiredEvents(Turncounter);
        }
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Stop();
            Debug.Log("Scenario.RemoveExpiredGameSquareEvents took " + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
        }
    }


    public void TriggerGameSquareEvent(GameSquare incGameSquare,int overlandEventID, string playerID, bool sendToMP)
    {
        if (!Events_Toggle)
        {
            return;
        }
        if (incGameSquare.GetOverlandEventByPlayerID(playerID).Count>0)
        {
            OverlandEvent overlandEvent = incGameSquare.GetOverlandEventByID(overlandEventID);
            Player player = FindPlayerByID(playerID);
            if (sendToMP)
            {
                //stopping the player as player is waiting for response
                player.GameState.GetUIPermissionByObject(GameState.Object.OVERLAND_EVENT_CHOICE_BUTTON).Permission = GameState.Permission.OVERLAND_EVENT_CHOICE_BUTTON_DISABLED;
                player.GameState.GetUIPermissionByObject(GameState.Object.OVERLAND_RIGHT_CLICK).Permission = GameState.Permission.OVERLAND_RIGHTCLICK_BAN;
                if (overlandEvent.PlayerIDs.Count > 0) { //changed from 1 to 0 for now
                    MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.ClaimEvent, playerID, incGameSquare.ID.ToString() + "*" + overlandEventID);
                    GameEngine.ActiveGame.clientManager.Push(multiplayerMessage);
                    return;
                }
                
            }
           
            if (overlandEvent.StartingTurn <= Turncounter && overlandEvent.EndingTurn > Turncounter)
            {
               
                EventTemplate eventTemplate = GameEngine.Data.EventTemplateCollection.findByKeyword(overlandEvent.EventKeyword);
                Location location = new Location();
                location.Mode = Location.MODE_OVERLAND;
                location.WorldMapCoordinates = new MapCoordinates();
                location.WorldMapCoordinates.XCoordinate = incGameSquare.X_cord;
                location.WorldMapCoordinates.YCoordinate = incGameSquare.Y_cord;
                RemoveOverlandEventByID(overlandEventID, incGameSquare);
                player.InitProvinceCenterEvent(eventTemplate, location,true);

           
            
                //incGameSquare.OverlandEvents.Remove(overlandEvent);
            }
        
        }
    }

    public void RemoveOverlandEventByID(int incID, GameSquare gameSquare)
    {
        //GameSquare gameSquare = Worldmap.FindGameSquareByID(gameSquareID);
        lock (gameSquare.OverlandEvents)
        {
            OverlandEvent toRemove = null;
            foreach (OverlandEvent overlandEvent in gameSquare.OverlandEvents)
            {
                if (overlandEvent.Id == incID)
                {
                    toRemove = overlandEvent;
                    break;
                }
            }
            gameSquare.OverlandEvents.Remove(toRemove);
        }
    }

    public void ResolveLootClaimsPhase()
    {
        bool timer = false;
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Reset();
            GameEngine.ActiveGame.GameStopwatch.Start();
        }
 
        foreach (GameSquare gamesquare in this.Worldmap.GameSquares)
        {
            if (gamesquare.LootClaims.Count > 0)
            {
                List<OverlandLootClaim> contested = new List<OverlandLootClaim>();
            
                OverlandLootClaim best = gamesquare.LootClaims[0];
                gamesquare.LootClaims.RemoveAt(0);
                //get the best avalible first
                foreach (OverlandLootClaim overlandLootClaim in gamesquare.LootClaims)
                {
                    if (overlandLootClaim.TimePoint < best.TimePoint)
                    {
                        best = overlandLootClaim;
                    }
                }
                //add to contested if theres more than 1 of best timepoint
                foreach (OverlandLootClaim claim in gamesquare.LootClaims)
                {
                    if (claim.TimePoint == best.TimePoint)
                    {
                        contested.Add(claim);
                    }
                }

                if (contested.Count > 1)
                {
                    ChanceEngine chance = new ChanceEngine();

                    foreach (var item in contested)
                    {
                        chance.next(1);
                    }

                    best = contested[chance.calculate()];
                }

                ResolveLootClaim(gamesquare,best,false);
                gamesquare.LootClaims.Clear();
            }
        }

        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Stop();
            Debug.Log("Scenario.ResolveLootClaimsPhase took " + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
        }
    }

    public void ResolveLootClaim(GameSquare gamesquare,OverlandLootClaim incLootClaim, bool sendToMP)
    {
        //we send our claim to host, which decides who got the treasure first, and would send it for the player that host recieved the claim from first 
        //this only applies if we loot real time
        if (sendToMP) 
        {
            MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.Sending_Object,MultiplayerMessage.ClaimLoot,gamesquare.ID.ToString());
            GameEngine.ActiveGame.clientManager.PushMultiplayerObject(multiplayerMessage,incLootClaim);
            return;
        }
        Notification notification = new Notification();
        notification.HeaderText = "Looting complete";
        notification.IsOverland = true;
        notification.TargetID = gamesquare.ID;
        notification.Type = Notification.NotificationType.TYPE_LOOT;
        
        Player player = FindPlayerByID(incLootClaim.PlayerID);
        ItemCollection loot = new ItemCollection();
        foreach (Item item in gamesquare.Inventory)
        {
            ItemTemplate template = GameEngine.Data.ItemTemplateCollection.findByKeyword(item.TemplateKeyword);
            notification.ExpandedText += "Looted: " + item.TemplateKeyword + " x" + item.Quantity;
            foreach (var lootMultiplier in incLootClaim.LootMultipliers)
            {

                StatTemplate stat = GameEngine.Data.StatCollection.findByKeyword(lootMultiplier.Keyword);
                if (!template.isCorrectTypesAndInValueRange(stat.LootingBonus.Types,stat.LootingBonus.NotWantedTypes,stat.LootingBonus.MinValue,stat.LootingBonus.MaxValue))
                {
                    continue;
                }
                if (stat.LootingBonus.Mode == LootingBonusTemplate.MODE_PERCENTUAL)
                {
                    int bonusQuantity = (int)(item.Quantity * lootMultiplier.Amount);
                    notification.ExpandedText += " + " + bonusQuantity + " bonus of " + lootMultiplier.Amount * 100 + "% because of " + lootMultiplier.Keyword; 
                    item.Quantity += bonusQuantity;
                }
                if (stat.LootingBonus.Mode == LootingBonusTemplate.MODE_FLAT)
                {
                    notification.ExpandedText += " + " + lootMultiplier.Amount + " because of " + lootMultiplier.Keyword;
                    item.Quantity += (int)lootMultiplier.Amount;
                   // Item bonusItem = Item.createRandomItem(stat.LootingBonus.NotWantedItemTypes,stat.LootingBonus.ItemTypes,stat.LootingBonus.MinValue,stat.LootingBonus.MaxValue);
                }

            }

            foreach (MyValue trait in incLootClaim.Traits)
            {
 
                switch (trait.Value) //for sake of multiplayer, we should avoid using random here
                {
                    case "Expert woodcutter":
                        if (template.isCorrectType(ItemTemplate.TYPE_RESOURCE_WOOD))
                        {
                            item.Quantity *= 2;
                            notification.ExpandedText += " and the amount was doubled by " + trait.Value + " of " + trait.Keyword;
                        }
                        break;
                    case "Expert stonemason":
                        if (template.isCorrectType(ItemTemplate.TYPE_RESOURCE_STONE))
                        {
                            item.Quantity *= 2;
                            notification.ExpandedText += " and the amount was doubled by " + trait.Value + " of " + trait.Keyword + $" ({item.Quantity})";
                        }
                        break;
                    default:
                        break;
                }
            }


            notification.ExpandedText += Environment.NewLine;
            loot.AddItem(item);
            //player.OwnedItems.AddItem(item);

        }

        //notification.ID = ++player.LocalNotificationID;
        //lock (player.Notifications)
        //{
        //    player.Notifications.Add(notification);
        //}
        CreateExtraItem(player.PlayerID, loot, 5, "Looting complete", "Take your loot", true);

        gamesquare.Inventory.Clear();
    }

    public void AutoPickUpGameSquareItems(GameSquare incGameSquare, string playerID, Army army, bool sendToMP,bool calledFromPhase)
    {
        if (incGameSquare.Inventory.Count > 0)
        {
            OverlandLootClaim lootClaim = new OverlandLootClaim();
            lootClaim.ArmyID = army.ArmyID;

            lootClaim.TimePoint = 1 - (army.MovementPoints * 100 / army.MaximumArmyMovementPoints());
            lootClaim.PlayerID = playerID;
            Player player = this.FindPlayerByID(playerID);
            lootClaim.LootMultipliers.AddRange(army.GetHighestLootingStats(player));
            lootClaim.Traits.AddRange(army.GetLootingTraits());
            if (LootClaimSystem)
            {

                incGameSquare.LootClaims.Add(lootClaim);
            }
            else
            {
                //if send to mp = false, means its resolved by host machine
                //if called from phase(sendtoMp is false) ,then we resolve, as every client is resolving the same way(because its a phase)
                if (sendToMP || calledFromPhase)
                {
                    ResolveLootClaim(incGameSquare, lootClaim, sendToMP);
                }
               
                //
            }

        }
    }

    void UpdateMapForPlayersExcept(string exceptionPlayer)
    {
        foreach (Player plr in Players)
        {
            if (plr.PlayerID == exceptionPlayer)
            {
                continue;
            }
            GameEngine.ActiveGame.UpdateMemoryMapWithActiveVision(plr);
        }
    }
    void SaveMapAndMovementToOtherPlayers(string playerID, Army army, FromToCoordinates coords, OurMyValueList onMovementTracking, bool save) 
    {

        UpdateMapForPlayersExcept(playerID);

        SaveMapOnMovement(army,coords, ref onMovementTracking,save);
    }
    public bool Movement(Army army, MapSquare startLoc, MapSquare destination, double modifier, bool sendMpMessage, bool calledFromPhase)
    {
        bool timer = false;
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Reset();
            GameEngine.ActiveGame.GameStopwatch.Start();
        }
   
        if (army.MovementPoints > 0)
        {
            GameSquare gameSquare = (GameSquare)destination;
            if (gameSquare.IsPassable(army))
            {
                OurMyValueList onMovementTracking = new OurMyValueList();
                if (!calledFromPhase)
                {
                    Thread saveMapOnmovementThread = new Thread(() => SaveMapOnMovement(army, new FromToCoordinates(startLoc.X_cord, startLoc.Y_cord, destination.X_cord, destination.Y_cord),ref onMovementTracking, false));
                    saveMapOnmovementThread.IsBackground = true;
                    saveMapOnmovementThread.Name = "saveMapOnmovementThread";
                    saveMapOnmovementThread.Start();
                    //onMovementTracking =
                }
                army.Location.Mode = Location.MODE_OVERLAND;
                army.Location.WorldMapCoordinates.XCoordinate = destination.X_cord;
                army.Location.WorldMapCoordinates.YCoordinate = destination.Y_cord;
                army.MovementPoints = army.MovementPoints - destination.MovementPointCost * modifier;


                //AutoPickUpGameSquareItems(gameSquare, army.OwnerPlayerID, army);
                if (sendMpMessage)
                {
                    MultiplayerMessage armyMovementMessage = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.ArmyMove, "");
                    GameEngine.ActiveGame.clientManager.PushMultiplayerObject(armyMovementMessage, new ArmyMovementInfo(destination.X_cord, destination.Y_cord, army.ArmyID,modifier,army.Location.WorldMapCoordinates.XCoordinate,army.Location.WorldMapCoordinates.YCoordinate));
                   
                }
                //if we send to mp message, that means you are moving, and therefore you send game square event claim to server
                //if sendMpMessage = false means that this is called from server
                //if calledFromPhase = true, then sendMpMessage is false, we trigger the event without using mp as it happens in phase for all clients
                bool stopMoving = false;
                if (sendMpMessage || calledFromPhase)
                {
                    if (gameSquare.GetOverlandEventByPlayerID(army.OwnerPlayerID).Count> 0)
                    {
                        army.triggeredEventLocation = new MapCoordinates(gameSquare.X_cord, gameSquare.Y_cord);
                        Debug.Log("setting triggeredEventLocation on X Y " + gameSquare.X_cord + " " + gameSquare.Y_cord);
                        //stopMoving = true;
                    }
                    //TriggerGameSquareEvent(gameSquare, army.OwnerPlayerID, sendMpMessage);

                }

                AutoPickUpGameSquareItems(gameSquare, army.OwnerPlayerID, army, sendMpMessage, calledFromPhase); //doing here, so only when a player moves
                GameEngine.ActiveGame.UpdateMemoryMapWithActiveVision(FindPlayerByID(army.OwnerPlayerID)); //keep it in this thread
                                                                                                           //updating map for other players is thread
                //Thread updateMapThread = new Thread(() => UpdateMapForPlayersExcept(army.OwnerPlayerID));
                //updateMapThread.IsBackground = true;
                //updateMapThread.Name = "updateMapThread";
                //updateMapThread.Start();
                if (!calledFromPhase)
                {
                    Thread saveMapOnmovementThread = new Thread(() => SaveMapAndMovementToOtherPlayers(army.OwnerPlayerID,army, new FromToCoordinates(startLoc.X_cord, startLoc.Y_cord, destination.X_cord, destination.Y_cord), onMovementTracking,false));
                    saveMapOnmovementThread.IsBackground = true;
                    saveMapOnmovementThread.Name = "saveMapOnmovementThread";
                    saveMapOnmovementThread.Start();
                    //SaveMapOnMovement(army, new FromToCoordinates(startLoc.X_cord, startLoc.Y_cord, destination.X_cord, destination.Y_cord), onMovementTracking, true);
                }
                else
                {
                    //why do need to update map during phase anyway???
                    //UpdateMapForPlayersExcept(army.OwnerPlayerID); //dont want to do thread here as its in thread for phase and things go fast that this could overwrite
                }
                if (stopMoving)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
           
        }
        else
        {
            return false;
        }
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Stop();
            Debug.Log("Scenario.Movement test took " + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
        }
        return true;
    }

    /// <summary>
    /// detects if 2 armies can attack each other on overland map
    /// if they can, they will be added to this battlefield
    /// </summary>
    /// <param name="army"></param>
    /// <param name="enemy"></param>
    /// <param name="battlefield"></param>
    /// <param name="player"></param>
    /// <param name="enemyTargeting"></param>
    public BattlefieldOld DetectArmyVSArmyHostilities(BattleParticipant army, BattleParticipant enemy,BattlefieldOld battlefield, Player player,bool enemyTargeting)
    {
        bool debug = true;
        BattleParticipant defender = null;
        BattleParticipant attacker = null;
        //either army targetting enemy with intenttoattacklist or enemy targeting army the same way
        if (debug)
        {
            Debug.Log("DetectArmyVSArmyHostilities player is " + player.PlayerID);
        }
        if (enemyTargeting)
        {
            defender = army;
            attacker = enemy;
        }
        else
        {
            defender = enemy;
            attacker = army;
        }
        int existingAttackerBattleID = this.BattlesToBeContinued.GetBattlefieldIDbyParticipant(attacker);
        int existingDefenderBattleID = this.BattlesToBeContinued.GetBattlefieldIDbyParticipant(defender);
        //we cant pull in armies that are already in battle
        if (existingDefenderBattleID != -1)
        {
            if (debug)
            {
                Debug.Log("DetectArmyVSArmyHostilities we cant pull in armies that are already in battle");
            }
            return battlefield;
        }
        if (defender != null)
        {
            //MapSquare a = Worldmap.FindMapSquareByCordinates(attacker.Location.WorldMapCoordinates.XCoordinate, attacker.Location.WorldMapCoordinates.YCoordinate);
            //MapSquare b = Worldmap.FindMapSquareByCordinates(target.Location.WorldMapCoordinates.XCoordinate, target.Location.WorldMapCoordinates.YCoordinate);
            GameSquare attackerGameSquare = attacker.GetGameSquare();
            GameSquare defenderGameSquare = defender.GetGameSquare();
            //we check if armies are close enough to attack each other
          //  if (existingAttackerBattleID == -1 && existingDefenderBattleID == -1)
            if (existingAttackerBattleID == -1) //distance rule(radius) no active battle
            {
                //if target is out of range, we return without adding attacker and target
                if (MapGenerator.GetDistance(attackerGameSquare, defenderGameSquare) > this.OverlandBattlefieldRadius)
                {
                    if (debug)
                    {
                        Debug.Log("DetectArmyVSArmyHostilities targets are out of range");
                    }
                    return battlefield;
                }
            }
            else 
            {
                //active battle rule, every square in battle is a valid range
                // so, as far as range is concerned, if you defender has stepped into active battle gameSquare and is being pulled in by participant, range wont matter
                if (defenderGameSquare.BattleFieldID != existingAttackerBattleID)
                {
                    // but defender cant be pulled into battle, if he is on neutral or on square with different battle going on
                    if (debug)
                    {
                        Debug.Log("DetectArmyVSArmyHostilities defender was already in battle(ID) " + defenderGameSquare.BattleFieldID + " existing attacker battle id " + existingAttackerBattleID);
                    }
                    return battlefield;
                }

            }

            //we still have to see the defender, to pull it in (sight check)

            MemoryTile tile = player.MapMemory.FindMemoryTileByCoordinates(defenderGameSquare.X_cord, defenderGameSquare.Y_cord);
            //MemoryTile tile = player.MapMemory.FindMemoryTileByCoordinates(target.Location.WorldMapCoordinates.XCoordinate, target.Location.WorldMapCoordinates.YCoordinate);
            bool legal = false;
            if (attacker.Mode == BattleParticipant.MODE_ARMY) // another check making sure only armies can pull into battle
            {
                if (debug)
                {
                    Debug.Log("DetectArmyVSArmyHostilities attacker mode IS mode army");
                }
                //do we see the enemy/building?
                if (tile.SightOnTile > 0)
                {
                    if (debug)
                    {
                        Debug.Log("tile sight is > than 0");
                    }
                    switch (defender.Mode)
                    {
                        case BattleParticipant.MODE_ARMY:
                            if (debug)
                            {
                                Debug.Log("defender is an army, tile count is " + tile.MemoryArmies.Count);
                            }
                            foreach (MemoryArmy memory in tile.MemoryArmies)
                            {
                                if (debug)
                                {
                                    Debug.Log("checking defender's memory armies");
                                }
                                //matching visible army
                                if (memory.ArmyID == defender.Army.ArmyID)
                                {
                                    if (debug)
                                    {
                                        Debug.Log("checking defender's memory army was found: " + memory.ArmyID);
                                    }
                                    legal = true;
                                }
                            }

                            break;
                        case BattleParticipant.MODE_BUILDING:
                          
                            //check if see the building
                            string status = MapMemory.GetBuildingVisibilityStatus(defenderGameSquare, tile,  attacker.GetPlayer());
                            if (debug)
                            {
                                Debug.Log("defender is a building, visibility status is " + status);
                            }
                            switch (status)
                            {
                                case Player.SEES_ENEMY_BUILDING:
                                    if (debug)
                                    {
                                        Debug.Log("defender is legal");
                                    }
                                    legal = true;
                                    break;
                                case Player.KNOWS_ENEMY_BUILDING_TYPE_BUT_NOW_OUTSIDE_VISION:
                                    //for now, we only attack buildings we see, but in future in case of complaints, then implement this
                                    if (debug)
                                    {
                                        Debug.Log("defender is outside vision building");
                                    }
                                    break;
                                default:
                                    break;
                            }
                            break;
                        default:
                            break;
                    }



                }
            }
            if (!legal)
            {
                if (debug)
                {
                    Debug.Log("DetectArmyVSArmyHostilities battle not legal");
                }
                return battlefield;
            }
            //create new battlefield if there is none
            if (battlefield == null)
            {
                //exception: we want to create battlefield for the army(so it kinda stacks)
             //   battlefield = new BattlefieldOld(attacker.GetGameSquare(), this.CombatRounds);
                battlefield = new BattlefieldOld(army.GetGameSquare(), this.CombatRounds);

                //battlefield.AddOverlandParticipant(attacker);
                //battlefield.AddOverlandParticipant(defender);
                battlefield.AddOverlandParticipant(army);
                battlefield.AddOverlandParticipant(enemy);
            }
            else
            {
                bool addedDefender = false;

                //add to existing battlefield
  //              if (!battlefield.HasParticipant(defender)) //instead of enemy
                    if (!battlefield.HasParticipant(enemy))
                {
                    battlefield.AddOverlandParticipant(enemy); // adding participant, assuming his gameSquare will match a battlefield sector
                    addedDefender = true;
                    if (existingAttackerBattleID != -1)
                    {
                        //there is active battle, so we have to actually put the units on the battlefield? We are doing just that
                        battlefield.SingleParticipantPlacement(enemy);
                    }

                }


                if (!battlefield.HasParticipant(army))
                {
                    Debug.LogError("Should this ever trigger, shouldnt it be armies "+ army.GetInformation() + "'s existing battlefield" + battlefield.GetInformation() + ", enemy " + enemy.GetInformation() + " was added here is " + addedDefender.ToString());
                    battlefield.AddOverlandParticipant(army);
                }


            }

        }



        return battlefield;



    }


    public void CancelQuestOfPartiesInCombat(int unitID,string playerID)
    {
        Player player = FindPlayerByID(playerID);
        foreach (Quest quest in player.ActiveQuests)
        {
            List<QuestParty> toDisband = new List<QuestParty>();
            foreach (QuestParty party in quest.Parties)
            {
              
                if (party.PlayerID == playerID)
                {
                    List<Entity> entities = party.GetEntities();

                    foreach (Entity entity in entities)
                    {
                        if (entity.UnitID == unitID)
                        {
                            Army overlandArmy = FindOverlandArmyByUnit(entity.UnitID);

                            Notification notification = new Notification();
                            notification.HeaderText = "Quest party cancelled!";
                            notification.IsOverland = true;
                            notification.BgImageR = 255;
                            notification.BgImageG = 2;
                            notification.BgImageB = 2;

                            notification.IsDismissed = false;
                            
                            notification.Type = Notification.NotificationType.TYPE_WARINING_QUEST_PARTY_CANCELLED;
                            notification.TargetID = Worldmap.FindGameSquareByCoordinates(overlandArmy.Location.WorldMapCoordinates.XCoordinate, overlandArmy.Location.WorldMapCoordinates.YCoordinate).ID;
                            notification.ExpandedText += "Your entity is in a battle: " + entity.UnitName + Environment.NewLine;
                            notification.ExpandedText += "Party disbanded: " + Environment.NewLine;
                            foreach (Entity ent in entities)
                            {
                                //notification.ExpandedText += ent.UnitName + Environment.NewLine;
                                NotificationElement notificationElement = new NotificationElement();
                                notificationElement.EntityID = ent.UnitID;
                                notificationElement.Picture = ent.GetPicture();
                                notificationElement.Content = ent.CharacterTemplateKeyword;
                                notification.NotificationElements.Add(notificationElement);

                            }

                            notification.ID = ++player.LocalNotificationID;
                            player.Notifications.Add(notification);
                            toDisband.Add(party);
                            
                            break;
                        }
                    }
                }
            }
            foreach (QuestParty partyToDisband in toDisband)
            {
                if (partyToDisband.HasEmbarked)
                {
                    quest.DisbandParty(partyToDisband.ID);

                }
                else
                {
                    quest.Parties.Remove(partyToDisband);
                }
            }
        }
    }


    public void CheckForArmiesToAttackOnSquares(Army sourceArmy, OurMapSquareList mapSquares) {

        bool debug = true;

        if (debug) {
            Debug.Log("CheckForArmiesToAttackOnSquares for army ID: " + sourceArmy.ArmyID + " on nr of " + mapSquares.Count + " squares");
        }
       
        Player player = FindPlayerByID(sourceArmy.OwnerPlayerID);

       

        foreach (var item in mapSquares)
        {
            List<Army> armiesOnSquare = this.FindAllOverlandArmiesByCoordinates(item.X_cord, item.Y_cord);
            foreach (Army potentialTarget in armiesOnSquare)
            {

                if (potentialTarget.OwnerPlayerID != sourceArmy.OwnerPlayerID)
                {
                    MemoryTile tile = player.MapMemory.FindMemoryTileByCoordinates(potentialTarget.Location.WorldMapCoordinates.XCoordinate, potentialTarget.Location.WorldMapCoordinates.YCoordinate);

                    //sight check
                    if (tile.SightOnTile > 0)
                    {
                        foreach (MemoryArmy memory in tile.MemoryArmies)
                        {
                            //matching visible army
                            if (memory.ArmyID == potentialTarget.ArmyID)
                            {
                                if (!sourceArmy.IsInHostileList(potentialTarget.ArmyID, BattleParticipant.MODE_ARMY))
                                {
                                    if (debug)
                                    {
                                        Debug.Log("adding army to attack list: " + potentialTarget.ArmyID + " " + potentialTarget.OwnerPlayerID);
                                    }
                                   
                                    sourceArmy.ArmiesYouIntentAttackIds.Add(new HostilityTarget(BattleParticipant.MODE_ARMY,potentialTarget.ArmyID));
                                }

                            }
                        }

                    }


                  
                }
            }
        }

   
    
    }

    public void NeutralsAttackPhase()
    {
        Debug.Log("before neutral attack phase");
       
        foreach (Army army in Armies)
        {
            if (army.OwnerPlayerID == Player.Neutral)
            {
                MapSquare source = this.Worldmap.FindMapSquareByCordinates(army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);

                int existingBattlefieldID = this.QueuedUpBattles.GetBattlefieldIDbyArmy(army.ArmyID);

                if (existingBattlefieldID != -1)
                {
                    // army already in battle, we gonna try to pull all visible armies that are on marked battlefieldSquares

                    OurMapSquareList existingBattlegameSquares = this.Worldmap.GetAllBattlefieldSquares(existingBattlefieldID);

                    CheckForArmiesToAttackOnSquares(army, existingBattlegameSquares);

                }
                else {

                    for (int i = 0; i <= this.OverlandBattlefieldRadius; i++)
                    {
                        OurMapSquareList mapSquares = this.Worldmap.GetMapSquaresInRadius(source, i);

                        CheckForArmiesToAttackOnSquares(army, mapSquares);

                    }



                }

            }
        }
    }

    public List<Army> GetAllArmies()
    {
        List<Army> answer = new List<Army>();
        answer.AddRange(GetAllGarissons());
        answer.AddRange(this.Armies);

        lock (ActiveBattles.Battlefields)
        {
            foreach (BattlefieldOld battlefield in ActiveBattles.Battlefields)
            {
                answer.AddRange(battlefield.Armies);
            }

        }
        lock (BattlesToBeContinued.Battlefields)
        {
            foreach (BattlefieldOld battlefield in BattlesToBeContinued.Battlefields)
            {
                answer.AddRange(battlefield.Armies);
            }
        }

        lock (QueuedUpBattles.Battlefields)
        {
            foreach (BattlefieldOld battlefield in QueuedUpBattles.Battlefields)
            {
                answer.AddRange(battlefield.Armies);
            }
        }
        foreach (Player player in Players)
        {
            foreach (Quest quest in player.ActiveQuests)
            {
                foreach (QuestParty party in quest.Parties)
                {
                    answer.Add(party.Army);
                }
            }
        }

        return answer;
    }

    public List<Army> GetAllGarissons()
    {
        List<Army> answer = new List<Army>();
        foreach (Building building in Worldmap.GetAllBuildings())
        {
            if (building.GarissonArmyID != -1)
            {
                answer.Add(building.GetGarisson());
            }
         
        }
        return answer;
    }

    public void TransferBuildingOwnership(int heroID, string playerID, GameSquare gameSquare)
    {

        gameSquare.building.OwnerPlayerID = playerID;
        gameSquare.building.OwnerHeroID = heroID;
        Army garisson = gameSquare.building.GetGarisson();
        if (garisson != null)
        {
            garisson.OwnerPlayerID = playerID;
            garisson.SetColor(FindPlayerByID(playerID));
        }
 
        gameSquare.building.Storage.OwnerPlayerID = playerID;
  
        gameSquare.building.Storage.SetColor(FindPlayerByID(playerID));

    }


    /// <summary>
    /// Detects conflicts on overland map, creates battlefields for each conflict
    /// </summary>
    public void DetectConflicts()
    {
        
        bool debug = true;
        bool targetingDebug = true;
     
        BattleManager potentialBattlefields = new BattleManager();


        if (debug)
        {
            Debug.Log("detection start");
        }
        //commented out this refresh ui as it makes no sense rn 15.02.2023
        //foreach (Player player in Players)
        //{
        //    PlayerController playerController = GameEngine.ActiveGame.FindPlayerControllerGameObject(player.PlayerID).GetComponent<PlayerController>();
        //    playerController.RefreshUI();

        //}
        Worldmap.GetInformation();
      

        //

        // TODOS
        // TODO while compiling list of armies, how are castle garrisons handled?         
        // TODO reinforcements might be split up during battle phase (we have to check sector size and reinforce - split army if nessesary, keep data inside battlefield?)  
        // TODO Lock in army UI and movement, if they in battle, so they can only disband?

        //we go through each army, and each armies hostile list, then we check 2 things:
        //if hostile army is in range(2 default) & visible,we do a new battlefield with those armies


        //algorithm is based on both hostility list and intend to attack list, and also adding potential auto-reinforcements
        List<BattleParticipant> battleParticipants = new List<BattleParticipant>();
        foreach (Army army in Armies)
        {
            battleParticipants.Add(new BattleParticipant(army));
        }
        foreach (GameSquare square in Worldmap.GameSquares)
        {
            if (square.building != null)
            {
                BuildingTemplate buildingTemplate = GameEngine.Data.BuildingTemplateCollection.findByKeyword(square.building.TemplateKeyword);
                if (buildingTemplate.Types.Contains(BuildingTemplate.TYPE_CAPTURABLE))
                {
                    battleParticipants.Add(new BattleParticipant(square));
                }
            }
        }

        foreach (BattleParticipant battleParticipant in battleParticipants)
        {

            //MapSquare armyMapSquare = Worldmap.FindMapSquareByCordinates(army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);
            GameSquare armyGameSquare = battleParticipant.GetGameSquare();
            if (armyGameSquare == null)
            {
                Debug.LogError("armygamesquare is null: " + battleParticipant.GetInformation());
            }
            Player player = battleParticipant.GetPlayer();
        
            
            BattlefieldOld battlefield = null;
            //if army is already inside battle, he will not create a new battlefield, it will pull armies into his own battlefield
            int batID = this.BattlesToBeContinued.GetBattlefieldIDbyParticipant(battleParticipant);
            if (batID != -1)
            {
                battlefield = this.BattlesToBeContinued.FindBattleByID(batID);
            }

            // we can not attack armies at all, if we are on top of battlefield hex with another battle (that we are not in)  
            if ((armyGameSquare.BattleFieldID != -1) && (armyGameSquare.BattleFieldID != batID))
            {
       
                if (targetingDebug)
                {
                    //TODO 26Nov2020 continue from here. 
                     Debug.Log(battleParticipant.GetInformation() + " is in top of battlefield hex of another battle");
                }

            }
            else {

                //we check if its an army, because buildings cannot attack, buildings can only be attacked
                if (battleParticipant.Mode == BattleParticipant.MODE_ARMY)
                {
                    //we cycle our hostility list to check if they are legal to be added to battlefield 
                    List<HostilityTarget> toRemove = new List<HostilityTarget>();
                    foreach (HostilityTarget hostilityTarget in battleParticipant.Army.ArmiesYouIntentAttackIds)
                    {
                        BattleParticipant targetParticipant = null;
                        //this block cleans up illegal hostile targets
                        switch (hostilityTarget.Mode)
                        {
                            case BattleParticipant.MODE_ARMY:
                                Army target = FindArmyByID(hostilityTarget.ID);
                                if (target == null)
                                {
                                    toRemove.Add(hostilityTarget);
                                    continue;
                                }
                                targetParticipant = new BattleParticipant(target);
                                break;
                            case BattleParticipant.MODE_BUILDING:

                                GameSquare building = Worldmap.FindGameSquareByBuildingID(hostilityTarget.ID);
                                if (building == null)
                                {
                                    toRemove.Add(hostilityTarget);
                                    continue;
                                }

                                if (targetingDebug) {
                                    Debug.Log("We found target on our killlist and it is a building with ID: " + hostilityTarget.ID);
                                    if (battlefield == null) {
                                        Debug.Log("Battlefield with the building is not created yet, it is null, lets head to range detection");
                                    }
                                    else
                                    {
                                        Debug.Log("Battlefield with the building is created already");
                                    }

                                }

                                targetParticipant = new BattleParticipant(building);
                                break;
                            default:
                                break;
                        }
                        //target passes the check, and is existing on overland(has not disappeared), then if the target is in range
                        battlefield = DetectArmyVSArmyHostilities(battleParticipant, targetParticipant, battlefield, player, false);

                        if (targetingDebug) {

                            if (targetParticipant != null)
                            {
                                if (targetParticipant.Mode == BattleParticipant.MODE_BUILDING)
                                {
                                    if (battlefield == null)
                                    {
                                        Debug.Log("Battlefield is still null, building rangedetection Failed");
                                    }
                                    else
                                    {
                                        Debug.Log("Battlefield with building created SUCCESSFULLY!!! ID: " + battlefield.ID);
                                    }
                                }
                            }

                        }
                        

                    }
                    foreach (HostilityTarget id in toRemove)
                    {
                        battleParticipant.Army.ArmiesYouIntentAttackIds.Remove(id);
                        battleParticipant.Army.EnemyArmies.Remove(id);
                    }
                }
           
            }


            //checking others attacking us
            foreach (BattleParticipant otherArmy in battleParticipants)
            {

                //buildings cannot attack, only being attacked
                if (otherArmy.Mode == BattleParticipant.MODE_BUILDING)
                {
                    continue;
                }
                if (otherArmy.Army.ArmyID == battleParticipant.GetID() && otherArmy.Mode == battleParticipant.Mode) {
                    // it is the same army, so we continue
                    continue;
                }

        
                    // First we check if it is potential attacker, so we check if other army is targeting battleparticipant
                    if (otherArmy.Army.IsInHostileList(battleParticipant.GetID(),battleParticipant.Mode))
                {

                    if (targetingDebug)
                    {
                        Debug.Log(battleParticipant.GetInformation() + " is being targeted by " + otherArmy.GetInformation());
                    }

                    Player enemyPlayer = otherArmy.GetPlayer();
                    int enemyBattleID = this.BattlesToBeContinued.GetBattlefieldIDbyArmy(otherArmy.Army.ArmyID);
                    BattlefieldOld attackerBattlefield = null;
                    if (enemyBattleID != -1)
                    {
                        // there is existing overland battle, that has been going on at least one turn. We will be pulled into it, if we are inside battlefield square

                        attackerBattlefield = this.BattlesToBeContinued.FindBattleByID(enemyBattleID);

                        if (targetingDebug)
                        {
                            Debug.Log(battleParticipant.GetInformation() + " attacker is already in existing battle " + attackerBattlefield.GetInformation());
                        }
                        attackerBattlefield = DetectArmyVSArmyHostilities(battleParticipant, otherArmy, attackerBattlefield, enemyPlayer, true);

                        if (attackerBattlefield.HasParticipant(battleParticipant))
                        {
                            if (targetingDebug)
                            {
                                Debug.Log(battleParticipant.GetInformation() + " was added to " + attackerBattlefield.GetInformation());

                            }

                        }
                        else {

                            if (targetingDebug)
                            {
                                Debug.Log(battleParticipant.GetInformation() + " was NOT added to " + attackerBattlefield.GetInformation());
                            }

                        }

                       

                    }
                    else
                    {

                        if (targetingDebug)
                        {
                            if (battlefield == null)
                            {
                                Debug.Log("entering being targeted range check for " + battleParticipant.GetInformation() + " that is being attacked by " + otherArmy.GetInformation() + " with no existing battlefield");
                            }
                            else
                            {
                                Debug.Log("entering being targeted range check for  " + battleParticipant.GetInformation() + " that is being attacked by " + otherArmy.GetInformation() + " with existing " + battlefield.GetInformation());
                            }

                        }

                        battlefield = DetectArmyVSArmyHostilities(battleParticipant, otherArmy, battlefield, enemyPlayer, true);

                        if (targetingDebug)
                        {
                            if (battlefield == null)
                            {
                                Debug.Log("after entering being targeted range check for " + battleParticipant.GetInformation() + " that is being attacked by " + otherArmy.GetInformation() + " resulted with no existing battlefield");
                            }
                            else
                            {
                                Debug.Log("after entering being targeted range check for  " + battleParticipant.GetInformation() + " that is being attacked by " + otherArmy.GetInformation() + " with existing " + battlefield.GetInformation());
                            }

                        }

                    }
                    //int enemyExistingBattleID = this.Battles.GetBattlefieldIDbyArmy(attacker.ArmyID);                              
                }
                else // then we try to find out if it is a potential friendly, willing to autojoin
                {

                    // we check for possibility that this army could be possible reinforcement
                  
                    if (!otherArmy.Army.IsAutomaticallyReinforcing)
                    {
                        continue;
                    }

                  

                    // we have to check the army being controlled by same player (with alliances including different players, one should join battles manually, I guess)

                    if (battleParticipant.GetPlayer().PlayerID == otherArmy.Army.OwnerPlayerID)
                    {

                        //we dont allow armies to reinforce if they are already in battle
                        if (this.BattlesToBeContinued.GetBattlefieldIDbyArmy(otherArmy.Army.ArmyID) != -1)
                        {
                            continue;
                        }

                        GameSquare otherArmyGameSquare = otherArmy.GetGameSquare();
                        //MapSquare otherArmyMapSquare = Worldmap.FindMapSquareByCordinates(otherArmy.Location.WorldMapCoordinates.XCoordinate, otherArmy.Location.WorldMapCoordinates.YCoordinate);
          
                        //if other friendly army is on existing battlesquare, it will not be pulled into new forming battles
                        //in that case it could be pulled into the battle that he has stepped onto
                        if (otherArmyGameSquare.BattleFieldID != -1)
                        {


                            BattlefieldOld onOtherbattlefield = this.BattlesToBeContinued.FindBattleByID(otherArmyGameSquare.BattleFieldID);
                            if (onOtherbattlefield != null)
                            {
                                
                                if (onOtherbattlefield.HasParticipant(otherArmy))
                                {
                                    // this should never happen, but I guess, cant be too careful, if army got already added somehow, we continue
                                    Debug.LogError("Weird stuff in Scenario.DetectConflicts, how is this otherarmy already in battlefield");
                                    continue;
                                }

                                  //if reinforcing army has allied armies in battle, then reinforce
                                if (onOtherbattlefield.GetCurrentParticipantPlayerIDs().Contains(otherArmy.Army.OwnerPlayerID))
                                {
                                    this.BattlesToBeContinued.AddArmyAsReinforcement(otherArmy.Army, otherArmyGameSquare.BattleFieldID);
                                }
                            }
                            else
                            {
                                Debug.LogError("null battlefield with id: " + otherArmyGameSquare.BattleFieldID);
                            }

                            continue;

                        }
                        // no existing battle, lets check for range
                        else
                        {

                            if (MapGenerator.GetDistance(otherArmyGameSquare, armyGameSquare) > this.OverlandBattlefieldRadius)
                            {
                                continue;
                            }

                            if (targetingDebug)
                            {
                                if (battlefield == null)
                                {
                                    Debug.Log("creating new battlefield for allies " + battleParticipant.GetInformation() + " that is being assisted by " + otherArmy.GetInformation());
                                }
                                else {

                                    Debug.Log("existing " + battlefield.GetInformation() + " FOUND for " + battleParticipant.GetInformation() + " that is being assisted by " + otherArmy.GetInformation());
                                }
         
                            }

                            //create new battlefield if there is none
                            if (battlefield == null)
                            {
                                //we add the army and it's reinforcement ... so this battlefield has at moment only same-side armies 
                                //so we are ready for potential enemies to be added later, but if no enemies, we have no conflict, meaning, we remove the battlefield later with 
                                // hostile detection processs
                                
                                battlefield = new BattlefieldOld(armyGameSquare, this.CombatRounds);
                               
                                battlefield.AddOverlandParticipant(battleParticipant);
                                battlefield.AddOverlandParticipant(otherArmy);

                                if (targetingDebug)
                                {
                                    Debug.Log("Created " + battlefield.GetInformation());
                                }

                            }
                            else
                            {

                                if (!battlefield.HasParticipant(battleParticipant))
                                {
                   
                                    Debug.LogError("This should never happen, that we have created a potential battlefield without it's main center army");
                                    if (debug)
                                    {
                                        battlefield.DisplayParticipantInfo();
                                        Debug.Log("So, the participant being added is ");
                                        Debug.Log(battleParticipant.GetInformation());
                                        Debug.Log(" So it ends ");
                                    }
                                   
                                    battlefield.AddOverlandParticipant(battleParticipant);
                                }

                                if (!battlefield.HasParticipant(otherArmy))
                                {
                                    battlefield.AddOverlandParticipant(otherArmy);
                                    if (debug)
                                    {
                                        Debug.Log("assister added for " + battlefield.GetInformation() + " that was assisted by " + otherArmy.GetInformation());

                                    }

                                }

                            }


                        }

                    }

                }
            }


            if (battlefield != null)
            {
                if (debug)
                {
                    Debug.Log("we have a battlefield that goes through hostile detection process: " + battlefield.ID);

                    foreach (Army participant in battlefield.Armies)
                    {
                        // Debug.Log("participating army: " + participant.OwnerPlayerID + " " + participant.ArmyID);
                       // participant.CheckAttackLists();
                        //foreach (HostilityTarget attacking in participant.ArmiesYouIntentAttackIds)
                        //{
                        //    Debug.Log("attacking: " + attacking.ID + " " + attacking.Mode);
                        //}
                    }


                }


                    if (batID == -1) //battlefield is actually new potential one and not existing one from this.battles
                {
                    // We check if battlefield has hostile armies (it is possible to have only friendlies (center army + reinforcement armies))
                    if (debug)
                    {
                        Debug.Log(battlefield.GetInformation() + " is passing HasArmiesHostileToEachOther with " + battlefield.HaveArmiesHostileToEachOther().ToString());

                    }

                    if (battlefield.HaveArmiesHostileToEachOther())
                    {
                        potentialBattlefields.Battlefields.Add(battlefield);
                    }
                    
                }
                
            }
        }

        //if (debug)
        //{
        //    Debug.Log("before sort battlefield count: " + potentialBattlefields.Battlefields.Count);
        //}

            potentialBattlefields.Battlefields.Sort(delegate (BattlefieldOld controller1, BattlefieldOld controller2) { return controller1.BattlefieldImportance().CompareTo(controller2.BattlefieldImportance()); });

        if (debug)
        {
            Debug.Log("potential battlefields: " + potentialBattlefields.Battlefields.Count);

            foreach (BattlefieldOld currentBattlefield in potentialBattlefields.Battlefields)
            {
                Debug.Log(currentBattlefield.GetInformation() + " importance: " + currentBattlefield.BattlefieldImportance());
            }

        }

        potentialBattlefields.Battlefields.Reverse(); // they were in wrong order, the more important ones at the end
        potentialBattlefields.RemoveDuplicateBattles();
        potentialBattlefields.RemoveDuplicateMapSquares();



        if (debug)
        {
            Debug.Log("final battlefields: " + potentialBattlefields.Battlefields.Count);

            foreach (BattlefieldOld currentBattlefield in potentialBattlefields.Battlefields)
            {
                Debug.Log("final Battlefield (ID:" + currentBattlefield.ID + ") importance: " + currentBattlefield.BattlefieldImportance());
            }

        }

        foreach (BattlefieldOld battlefield in potentialBattlefields.Battlefields)
        {
            
            foreach (MapSquare mapsqr in battlefield.OverLandmapSquares)
            {
                GameSquare gameSquare = mapsqr as GameSquare;
                gameSquare.BattleFieldID = battlefield.ID;
            }
           
            battlefield.InitilizeBattlefield(Location.MODE_IN_OVERLAND_BATTLE);
            //restrict other UI
        }

        this.QueuedUpBattles.Battlefields.AddRange(potentialBattlefields.Battlefields);



        if (debug)
        {
            OurLog.Print("END END ################################## ");
        }
        //foreach (Player player in Players)
        //{
        //    if (GameEngine.ActiveGame.FindPlayerControllerGameObject(player.PlayerID) != null)
        //    {
        //        PlayerController playerController = GameEngine.ActiveGame.FindPlayerControllerGameObject(player.PlayerID).GetComponent<PlayerController>();
        //        playerController.RefreshUI();
        //    }
           

        //}
        Worldmap.GetInformation();
    }
    
    public void HowLargeIsEverythingPlayer(Player player)
    {

        HowLargeIsObject(player, player.PlayerID, "");
        HowLargeIsObject(player.ActiveQuests, "active quests ", "");
        HowLargeIsObject(player.Budgets, "budgets ", "");
        HowLargeIsObject(player.CapitalLocation, "capital location ", "");
        HowLargeIsObject(player.ExtraItems, "extra items ", "");
        HowLargeIsObject(player.EventStash, "event stash ", "");
        HowLargeIsObject(player.FutureQuests, "future quests ", "");
        HowLargeIsObject(player.OwnedItems, "owned items ", "");
        HowLargeIsObject(player.Shops, "shops ", "");
        HowLargeIsObject(player.MapMemory, "MapMemory ", "");
        HowLargeIsObject(player.MapMemory.Capacity, "MapMemory capacity", "");
        HowLargeIsObject(player.MapMemory.Count, "MapMemory count", "");
        HowLargeIsObject(player.ItemIncome, "item income ", "");
        GameEngine.SaveGame(Armies, GameEngine.ActiveGame.savesPath, "armies test.xml");
    }
    public void HowLargeIsScenario(string whereFrom)
    {
        byte[] scenarioToBytes = ObjectByteConverter.ObjectToByteArray(this);
        Debug.Log("scenario size: " + scenarioToBytes.Length + " from " + whereFrom);
    }
    public void HowLargeIsScenarioObjects()
    {

    }
    public void HowLargeIsObject(object incObject, string name, string whereFrom)
    {
        byte[] objecToBytes = ObjectByteConverter.ObjectToByteArray(incObject);
        Debug.Log(name + " size: " + objecToBytes.Length + " from " + whereFrom);
    }
    /// <summary>
    /// We are creating automatic hero budgets for all of players 
    /// (Forecasting payrolls on heroes), the budget could be modified by UI
    /// </summary>
    public void SetBudgets()
    {
        bool timer = true;
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Reset();
            GameEngine.ActiveGame.GameStopwatch.Start();
        }

        foreach (Player player in Players)
        {
            if (player.Defeated)
            {
                continue;
            }
            player.SetBudgets();
        }
        if (timer)
        {
            GameEngine.ActiveGame.GameStopwatch.Stop();
            Debug.Log("Scenario.SetBudgets took " + GameEngine.ActiveGame.GameStopwatch.ElapsedMilliseconds + " ms to complete");
        }
    }


    public List<Building> GetEntityBuildings(int unitID)
    {
        List<Building> answer = new List<Building>();
        foreach (Building building in Worldmap.GetAllBuildings())
        {
            if (building.OwnerHeroID == unitID)
            {
                answer.Add(building);
            }
        }
        return answer;
    }
    public List<ProductionLine> GetPlayerChangedProductionSlider(string playerID)
    {
        List<ProductionLine> answer = new List<ProductionLine>();

        foreach (Building building in GetPlayerBuildings(playerID))
        {
            foreach (BuildingProduction production in building.ArmyProductions)
            {
                foreach (ProductionLine line in production.ProductionLines)
                {
                    if (line.WasChanged)
                    {
                        answer.Add(line);
                    }
                }
            }
        }

        return answer;
    }
    public List<Building> GetPlayerBuildings(string playerID)
    {
        List<Building> answer = new List<Building>();
        foreach (Building building in Worldmap.GetAllBuildings())
        {
            if (building.OwnerPlayerID == playerID)
            {
                answer.Add(building);
            }
        }
        return answer;
    }
    public List<Player> getRemainingOpponents(Player player) {
        List<Player> opponents = new List<Player>();
        foreach (Player currentPlayer in this.Players) {

            if (currentPlayer.PlayerID == player.PlayerID) {
                continue;
            }

            if (currentPlayer.PlayerID == Player.Neutral) {
                continue;
            }

            if (currentPlayer.Defeated)
            {
                continue;
            }

            opponents.Add(currentPlayer);
        }
        return opponents;

    }


    public List<GameSquare> GetPlayerBuildingGameSquares(string playerID)
    {
        List<GameSquare> answer = new List<GameSquare>();

        foreach (GameSquare gameSquare in Worldmap.GameSquares) {

            if (gameSquare.building != null) {

                if (gameSquare.building.OwnerPlayerID == playerID)
                {
                    answer.Add(gameSquare);
                }

            }

        }

        return answer;
    }

    public void RefreshFutureHeroes()
    {
        bool debug = false;
     
        foreach (Player player in Players)
        {
            if (debug)
            {
                Debug.Log("creating future heroes for player " + player.PlayerID);
            }
            player.FutureHeroes.Clear();

            List<string> raceKeywords = new List<string>();

            foreach (Building building in GetPlayerBuildings(player.PlayerID))
            {
                raceKeywords.AddRange(building.RaceKeywords);
            }
            if (debug)
            {
                Debug.Log("race keywords are: ");
                foreach (string race in raceKeywords)
                {
                    Debug.Log(race);
                }
            }
            if (raceKeywords.Count == 0)
            {
                Debug.Log("please set some races for buildings in data or the player has no building, adding an orc");
                raceKeywords.Add(RaceTemplate.TYPE_RACE_ORC);
            }

            for (int i = 0; i < GameEngine.ActiveGame.scenario.Future_Heroes_Batch_Amount; i++)
            {
                string keyword = raceKeywords[GameEngine.random.Next(0, raceKeywords.Count)];

                Entity newFutureHero = Entity.createRace(GameEngine.Data.RaceCollection.findByKeyword(keyword),player.PlayerID);

                if (debug)
                {
                    Debug.Log("new future hero is created: " + newFutureHero.CharacterTemplateKeyword);
                }

                player.FutureHeroes.Add(newFutureHero);
            }


        }


    }

    /// <summary>
    /// call this method when you drop the item between 2 items
    /// make sure to refresh the UI after calling
    /// get the items not from direct links but from IDs in the UI
    /// </summary>
    /// <param name="centerItem"></param>
    /// <param name="placedItem"></param>
    public void PlaceInBetweenItems(Entity producer,int leftItemID, int rightItemID, int placedItemID)
    {
        bool debug = false;
        Item leftItem = producer.BackPack.FindItemByID(leftItemID);
        Item rightItem = producer.BackPack.FindItemByID(rightItemID);
        Item placedItem = producer.BackPack.FindItemByID(placedItemID);
        //no point of doing this
        if (leftItem == placedItem || rightItem == placedItem)
        {
            if (debug)
            {
                Debug.Log("adjacent items, not placing");
            }
            return;
        }

        if (debug)
        {
            Debug.Log("left item: " + leftItem.SourceRecipeKeyword + " right item: " + rightItem.SourceRecipeKeyword + " placed Item: " + placedItem.SourceRecipeKeyword);
        }

        //split inv into 2 and shift the item accordingly(use compressinventory)

        ItemCollection leftGroup = producer.BackPack.GetItemsFromPosition(leftItem.InventoryPosition, false);
        ItemCollection rightGroup = producer.BackPack.GetItemsFromPosition(rightItem.InventoryPosition, true);

        if (leftGroup.FindItemByID(placedItem.ID) != null)
        {
            leftGroup.RemoveItemByID(placedItem.ID);
            leftGroup.CompressInventory(1);
            leftGroup.AddItem(placedItem);
            if (debug)
            {
                Debug.Log(placedItem.SourceRecipeKeyword + " was in left group");
            }
        }
        else if (rightGroup.FindItemByID(placedItem.ID) != null)
        {
            rightGroup.RemoveItemByID(placedItem.ID);
            rightGroup.CompressInventory(2);
            rightGroup.AddItem(placedItem, 1);
            if (debug)
            {
                Debug.Log(placedItem.SourceRecipeKeyword + " was in right group");
            }
        }
        else
        {
            Debug.LogError("the placed item isnt in either group");

        }
        leftGroup.SortItemsByInventoryPosition();
        rightGroup.SortItemsByInventoryPosition();
        leftGroup.AddRangeItems(rightGroup);
        producer.BackPack.Clear();
        producer.BackPack.AddRangeItems(leftGroup);
      
    }

    /// <summary>
    /// call this method when you drop the item before the first item
    /// make sure to refresh the UI after calling
    /// get the items not from direct links but from IDs in the UI
    /// </summary>
    /// <param name="firstItem"></param>
    /// <param name="placedItem"></param>
    public void PlaceItemBeforeFirst(Entity producer, int placedItemID)
    {
        Item placedItem = producer.BackPack.FindItemByID(placedItemID);



        producer.BackPack.RemoveItemByID(placedItem.ID);


        producer.BackPack.CompressInventory(2);



        producer.BackPack.AddItem(placedItem, 1);

    

    }

    /// <summary>
    /// call this method when you drop the item after the last item
    /// make sure to refresh the UI after calling
    /// get the items not from direct links but from IDs in the UI
    /// </summary>
    /// <param name="lastItem"></param>
    /// <param name="placedItem"></param>
    public void PlaceItemLast(Entity producer, int placedItemID)
    {
        //Item lastItem = producer.Inventory.GetLastItem();
        Item placedItem = producer.BackPack.FindItemByID(placedItemID);

        producer.BackPack.RemoveItemByID(placedItemID);

        producer.BackPack.CompressInventory(1);

        producer.BackPack.AddItem(placedItem);
        //placedItem.InventoryPosition = lastItem.InventoryPosition++;
      
    }


    public Army GetAllUnitsInTheGame(bool checkShops, bool checkStorage)
    {
        Army AllUnitsInGame = new Army(-1,null);
        lock (Armies)
        {
            foreach (Army army in armies)
            {
                foreach (Entity unit in army.Units)

                {
                    AllUnitsInGame.Units.Add(unit);
                }

            }
        }
        if (checkStorage)
        {
            if (this.Worldmap != null)
            {
                List<Building> buildings = Worldmap.GetAllBuildings();
                foreach (Building building in buildings)
                {


                    foreach (Entity storageunit in building.Storage.Units)
                    {
                        AllUnitsInGame.Units.Add(storageunit);
                    }


                }
            }
        
        }



        foreach (Player player in Players)
        {
            lock (player.ActiveQuests)
            {
                foreach (Quest quest in player.ActiveQuests)
                {
                    foreach (QuestParty questParty in quest.Parties)
                    {
                        if (questParty.Army != null)
                        {
                            foreach (Entity entity in questParty.Army.Units)
                            {
                                AllUnitsInGame.Units.Add(entity);
                            }
                        }

                    }
                }
            }
       
 
        }
        if (checkShops)
        {
            foreach (MerchantGuild guild in Guilds)
            {
                lock (guild.BidItems)
                {
                    foreach (ShopItem item in guild.BidItems)
                    {
                        if (item.Entity != null)
                        {
                            AllUnitsInGame.Units.Add(item.Entity);
                        }
                    }
                }

                lock (guild.AuctionItemsToBeProcessed)
                {
                    foreach (ShopItem item in guild.AuctionItemsToBeProcessed)
                    {
                        if (item.Entity != null)
                        {
                            AllUnitsInGame.Units.Add(item.Entity);
                        }
                    }
                }

                lock (guild.StockItems)
                {
                    foreach (ShopItem item in guild.StockItems)
                    {
                        if (item.Entity != null)
                        {
                            AllUnitsInGame.Units.Add(item.Entity);
                        }
                    }
                }

                lock (guild.ItemsUpForTrade)
                {
                    foreach (ShopItem item in guild.ItemsUpForTrade)
                    {
                        if (item.Entity != null)
                        {
                            AllUnitsInGame.Units.Add(item.Entity);
                        }
                    }

                }

                lock (guild.TradeItemsToBeProcessed)
                {
                    foreach (ShopItem item in guild.TradeItemsToBeProcessed)
                    {
                        if (item.Entity != null)
                        {
                            AllUnitsInGame.Units.Add(item.Entity);
                        }
                    }
                }
      
            }
        }

        foreach (Army army in GetAllBattlefieldArmies())
        {
            foreach (Entity unit in army.Units)
            {
                AllUnitsInGame.Units.Add(unit);
            }
        }

        return AllUnitsInGame;
    }
    /// <summary>
    /// call for heroes first, then for other units
    /// do it for every player
    /// </summary>
    /// <param name="incUnits"></param>
    void PayUnitUpkeepsNEW(List<Entity> incUnits, Player player, bool displayResults, string notificationTitle)
    {
        bool debug = false;
   
        if (debug)
        {
            Debug.Log("starting PayUnitUpkeeps for player: " + player.PlayerID + " inc units count: " + incUnits.Count);
        }
        player.Budgets.Clear();   //temp here, a bit later remove from here
        OurStatList answer = new OurStatList(); //what has been paid for upkeeps
        List<AssignedUpkeepInfo> projectedUpkeeps = new List<AssignedUpkeepInfo>();

        List<UpkeepCommand> upkeepCommands = new List<UpkeepCommand>();

        //sort units to prioritize most valuable ones
        incUnits.Sort(delegate (Entity controller1, Entity controller2) { return controller1.CurrentValue.CompareTo(controller2.CurrentValue); });

        //creating projecting upkeeps & going through items in entity inventory
        foreach (Entity ent in incUnits)
        {
            if (debug)
            {
                Debug.Log(ent.CharacterTemplateKeyword + " upkeep type " + ent.UpKeep.UpkeepType);
            }

            AssignedUpkeepInfo newUpkeepInfo = new AssignedUpkeepInfo();
            newUpkeepInfo.EntityID = ent.UnitID;
            if (ent.UpKeep.UpkeepType == UpKeep.ConsumeFromEntityInventory || ent.UpKeep.UpkeepType == UpKeep.ConsumeFromEntityThenPlayer)
            {
                foreach (UpkeepCost cost in ent.UpKeep.Costs)
                {
                    if (cost.TemplateOdd.TemplateKeyword != "")
                    {
                        int itemQuantity = ent.BackPack.GetSameItemAmount(cost.TemplateOdd.TemplateKeyword);
                        UpkeepStat upkeepStat = new UpkeepStat();
                        upkeepStat.Keyword = cost.TemplateOdd.TemplateKeyword;
                        upkeepStat.Amount = itemQuantity;
                        upkeepStat.Source = UpkeepStat.SOURCE_ENTITY_INVENTORY;
                        upkeepStat.UpkeepIndex = ent.UpKeep.Costs.IndexOf(cost);
                        upkeepStat.Mode = UpkeepStat.MODE_SPECIFIC;
                        upkeepStat.UpkeepRequiredAmount = cost.TemplateOdd.MaxQuantity;
                        newUpkeepInfo.UpkeepStats.Add(upkeepStat);
                     
                    }
                    else
                    {
                        ItemCollection typeItems = new ItemCollection();
                        typeItems.AddRangeItems(ObjectCopier.Clone<ItemCollection>(ent.BackPack.FindCorrectTypeItems(cost.TemplateOdd.Types, cost.TemplateOdd.NotWantedTypes)));
                        foreach (Item item in typeItems)
                        {
                            UpkeepStat upkeepStat = new UpkeepStat();
                            upkeepStat.Keyword = item.TemplateKeyword;
                            upkeepStat.Amount = item.Quantity;
                            upkeepStat.Source = UpkeepStat.SOURCE_ENTITY_INVENTORY;
                            upkeepStat.UpkeepIndex = ent.UpKeep.Costs.IndexOf(cost);
                            upkeepStat.Mode = UpkeepStat.MODE_TYPE;
                            if (cost.PreferredItems.Contains(item.TemplateKeyword))
                            {
                                upkeepStat.Mode = UpkeepStat.MODE_PREFERRED;
                            }
                            upkeepStat.UpkeepRequiredAmount = cost.TemplateOdd.MaxQuantity;
                            newUpkeepInfo.UpkeepStats.Add(upkeepStat);
                        }
                

                    }
                }

            }




            projectedUpkeeps.Add(newUpkeepInfo);
        }



        if (debug)
        {
            Debug.Log("projected upkeeps count: " + projectedUpkeeps.Count);
            foreach (AssignedUpkeepInfo item in projectedUpkeeps)
            {
                Debug.Log("checking assignedupkeepinfo " + item.EntityID + " item count " + item.UpkeepStats.Count);
                foreach (UpkeepStat up in item.UpkeepStats)
                {
                    Debug.Log("checking upkeepStat " + up.Keyword + " x" + up.Amount + " mode " + up.Mode + " source " + up.Source + " UpkeepIndex " + up.UpkeepIndex + " UpkeepRequiredAmount " + up.UpkeepRequiredAmount);
                }
            }
        }
        //creating upkeep commands for the items
        foreach (AssignedUpkeepInfo assignedUpkeepInfo in projectedUpkeeps)
        {
            foreach (Entity entity in incUnits)
            {
                if (entity.UpKeep.UpkeepType == UpKeep.ConsumeFromEntityInventory || entity.UpKeep.UpkeepType == UpKeep.ConsumeFromEntityThenPlayer)
                {
                    if (entity.UnitID == assignedUpkeepInfo.EntityID)
                    {
                        assignedUpkeepInfo.SortByLowestValue();
                        foreach (UpkeepStat upKeepStat in assignedUpkeepInfo.UpkeepStats)
                        {
                            if (upKeepStat.Mode != UpkeepStat.MODE_SPECIFIC || upKeepStat.Source != UpkeepStat.SOURCE_ENTITY_INVENTORY)
                            {
                                continue;
                            }
                            int amountToTake = upKeepStat.UpkeepRequiredAmount;
                            int amountAvalible = (int)upKeepStat.Amount;
                            foreach (UpkeepCommand upkeepCommand in upkeepCommands)
                            {
                                if (upkeepCommand.itemTemplate == upKeepStat.Keyword)
                                {

                                    if (upkeepCommand.entityID == entity.UnitID && upkeepCommand.upkeepCostIndex == upKeepStat.UpkeepIndex)
                                    {
                                        if (upkeepCommand.source != upKeepStat.Source)
                                        {
                                            amountToTake -= upkeepCommand.itemQuantity;
                                        }
                                        else
                                        {
                                            amountToTake += upkeepCommand.itemQuantity;
                                            amountToTake = Math.Min(amountToTake, upKeepStat.UpkeepRequiredAmount);
                                        }

                                    }
                                    if (upkeepCommand.source == upKeepStat.Source)
                                    {
                                        amountAvalible -= upkeepCommand.itemQuantity;
                                    }

                                }
                                else if (upkeepCommand.upkeepCostIndex == upKeepStat.UpkeepIndex && upkeepCommand.entityID == entity.UnitID) //this part matters primaraly due to types
                                {
                                    //if upkeep indexes match, means we are seeing command for same upkeeps
                                    //suppose upKeepStat is cheese, and command is take goblin fish jerky , and they both have index to upkeep for type food
                                    //if the item and command are indeed for same upkeep, decrease amount needed to take
                                    amountToTake -= upkeepCommand.itemQuantity;
                                }
                                //if (upkeepCommand.itemTemplate == upKeepStat.Keyword && upkeepCommand.source == upKeepStat.Source)
                                //{
                                //    amountAvalible -= upkeepCommand.itemQuantity;
                                //}
                            }
                            if (amountAvalible > 0 && amountToTake > 0)
                            {
                                UpkeepCommand newUpkeepCommand = new UpkeepCommand();
                                newUpkeepCommand.entityID = entity.UnitID;
                                newUpkeepCommand.itemQuantity = Math.Min(amountToTake, amountAvalible);
                                newUpkeepCommand.source = upKeepStat.Source;
                                newUpkeepCommand.itemTemplate = upKeepStat.Keyword;
                                newUpkeepCommand.upkeepCostIndex = upKeepStat.UpkeepIndex;
                                upkeepCommands.Add(newUpkeepCommand);
                            }

                        }

                        foreach (UpkeepStat upKeepStat in assignedUpkeepInfo.UpkeepStats)
                        {
                            if (upKeepStat.Mode != UpkeepStat.MODE_PREFERRED || upKeepStat.Source != UpkeepStat.SOURCE_ENTITY_INVENTORY)
                            {
                                continue;
                            }
                            int amountToTake = upKeepStat.UpkeepRequiredAmount;
                            int amountAvalible = (int)upKeepStat.Amount;
                            foreach (UpkeepCommand upkeepCommand in upkeepCommands)
                            {
                                if (upkeepCommand.itemTemplate == upKeepStat.Keyword)
                                {

                                    if (upkeepCommand.entityID == entity.UnitID && upkeepCommand.upkeepCostIndex == upKeepStat.UpkeepIndex)
                                    {
                                        if (upkeepCommand.source != upKeepStat.Source)
                                        {
                                            amountToTake -= upkeepCommand.itemQuantity;
                                        }
                                        else
                                        {
                                            amountToTake += upkeepCommand.itemQuantity;
                                            amountToTake = Math.Min(amountToTake, upKeepStat.UpkeepRequiredAmount);
                                        }

                                    }
                                    if (upkeepCommand.source == upKeepStat.Source)
                                    {
                                        amountAvalible -= upkeepCommand.itemQuantity;
                                    }

                                }
                                else if (upkeepCommand.upkeepCostIndex == upKeepStat.UpkeepIndex && upkeepCommand.entityID == entity.UnitID) //this part matters primaraly due to types
                                {
                                    //if upkeep indexes match, means we are seeing command for same upkeeps
                                    //suppose upKeepStat is cheese, and command is take goblin fish jerky , and they both have index to upkeep for type food
                                    //if the item and command are indeed for same upkeep, decrease amount needed to take
                                    amountToTake -= upkeepCommand.itemQuantity;
                                }
                                //if (upkeepCommand.itemTemplate == upKeepStat.Keyword && upkeepCommand.source == upKeepStat.Source)
                                //{
                                //    amountAvalible -= upkeepCommand.itemQuantity;
                                //}
                            }
                            if (amountAvalible > 0 && amountToTake > 0)
                            {
                                UpkeepCommand newUpkeepCommand = new UpkeepCommand();
                                newUpkeepCommand.entityID = entity.UnitID;
                                newUpkeepCommand.itemQuantity = Math.Min(amountToTake, amountAvalible);
                                newUpkeepCommand.source = upKeepStat.Source;
                                newUpkeepCommand.itemTemplate = upKeepStat.Keyword;
                                newUpkeepCommand.upkeepCostIndex = upKeepStat.UpkeepIndex;
                                upkeepCommands.Add(newUpkeepCommand);
                            }

                        }

                        foreach (UpkeepStat upKeepStat in assignedUpkeepInfo.UpkeepStats)
                        {
                            if (upKeepStat.Mode != UpkeepStat.MODE_TYPE || upKeepStat.Source != UpkeepStat.SOURCE_ENTITY_INVENTORY)
                            {
                                continue;
                            }
                            int amountToTake = upKeepStat.UpkeepRequiredAmount;
                            int amountAvalible = (int)upKeepStat.Amount;
                            foreach (UpkeepCommand upkeepCommand in upkeepCommands)
                            {
                                if (upkeepCommand.itemTemplate == upKeepStat.Keyword)
                                {
                     
                                    if (upkeepCommand.entityID == entity.UnitID && upkeepCommand.upkeepCostIndex == upKeepStat.UpkeepIndex)
                                    {
                                        if (upkeepCommand.source != upKeepStat.Source)
                                        {
                                            amountToTake -= upkeepCommand.itemQuantity;
                                        }
                                        else
                                        {
                                            amountToTake += upkeepCommand.itemQuantity;
                                            amountToTake = Math.Min(amountToTake, upKeepStat.UpkeepRequiredAmount);
                                        }

                                    }
                                    if (upkeepCommand.source == upKeepStat.Source)  
                                    {
                                        amountAvalible -= upkeepCommand.itemQuantity;
                                    }

                                }
                                else if (upkeepCommand.upkeepCostIndex == upKeepStat.UpkeepIndex && upkeepCommand.entityID == entity.UnitID) //this part matters primaraly due to types
                                {
                                    //if upkeep indexes match, means we are seeing command for same upkeeps
                                    //suppose upKeepStat is cheese, and command is take goblin fish jerky , and they both have index to upkeep for type food
                                    //if the item and command are indeed for same upkeep, decrease amount needed to take
                                    amountToTake -= upkeepCommand.itemQuantity;
                                }
                                //if (upkeepCommand.itemTemplate == upKeepStat.Keyword && upkeepCommand.source == upKeepStat.Source)
                                //{
                                //    amountAvalible -= upkeepCommand.itemQuantity;
                                //}
                            }
                            if (amountAvalible > 0 && amountToTake > 0)
                            {
                                UpkeepCommand newUpkeepCommand = new UpkeepCommand();
                                newUpkeepCommand.entityID = entity.UnitID;
                                newUpkeepCommand.itemQuantity = Math.Min(amountToTake, amountAvalible);
                                newUpkeepCommand.source = upKeepStat.Source;
                                newUpkeepCommand.itemTemplate = upKeepStat.Keyword;
                                newUpkeepCommand.upkeepCostIndex = upKeepStat.UpkeepIndex;
                                upkeepCommands.Add(newUpkeepCommand);
                            }

                        }

                    }
                }
           
            }
        }

        if (debug)
        {
            Debug.Log("checking upkeep commands after entity inventory round, command count: " + upkeepCommands.Count);
            foreach (UpkeepCommand upkeepCommandToDebug in upkeepCommands)
            {
                Debug.Log("checking upkeepCommand: unit id: " + upkeepCommandToDebug.entityID + " item: " + upkeepCommandToDebug.itemTemplate + " x" + upkeepCommandToDebug.itemQuantity + " for upkeep(index) " + upkeepCommandToDebug.upkeepCostIndex + " from " + upkeepCommandToDebug.source);
            }
        }
      
        //going through items in player inventory
        foreach (AssignedUpkeepInfo upKeepInfo in projectedUpkeeps)
        {

            foreach (Entity ent in incUnits)
            {
                if (ent.UpKeep.UpkeepType == UpKeep.ConsumeFromEntityThenPlayer || ent.UpKeep.UpkeepType == UpKeep.UpkeepFromPlayer || ent.UpKeep.UpkeepType == UpKeep.ConsumeFromPlayerThenEntity)
                {
                    foreach (UpkeepCost cost in ent.UpKeep.Costs)
                    {
                        if (cost.TemplateOdd.TemplateKeyword != "")
                        {
                            int itemQuantity = player.OwnedItems.GetSameItemAmount(cost.TemplateOdd.TemplateKeyword);
                            UpkeepStat upkeepStat = new UpkeepStat();
                            upkeepStat.Keyword = cost.TemplateOdd.TemplateKeyword;
                            upkeepStat.Amount = itemQuantity;
                            upkeepStat.Source = UpkeepStat.SOURCE_PLAYER_INVENTORY;
                            upkeepStat.UpkeepIndex = ent.UpKeep.Costs.IndexOf(cost);
                            upkeepStat.Mode = UpkeepStat.MODE_SPECIFIC;
                            upkeepStat.UpkeepRequiredAmount = cost.TemplateOdd.MaxQuantity;
                            upKeepInfo.UpkeepStats.Add(upkeepStat);

                        }
                        else
                        {
                            ItemCollection typeItems = new ItemCollection();
                            typeItems.AddRangeItems(ObjectCopier.Clone<ItemCollection>(player.OwnedItems.FindCorrectTypeItems(cost.TemplateOdd.Types, cost.TemplateOdd.NotWantedTypes)));
                            //typeItems.CompressInventory(1); //needed to prevent seperate stacks
                            //typeItems.CompressInventory(1); //needed to prevent seperate stacks
                            foreach (Item item in typeItems)
                            {
                                UpkeepStat upkeepStat = new UpkeepStat();
                                upkeepStat.Keyword = item.TemplateKeyword;
                                upkeepStat.Amount = item.Quantity;
                                upkeepStat.Source = UpkeepStat.SOURCE_PLAYER_INVENTORY;
                                upkeepStat.UpkeepIndex = ent.UpKeep.Costs.IndexOf(cost);
                                upkeepStat.Mode = UpkeepStat.MODE_TYPE;
                                if (cost.PreferredItems.Contains(item.TemplateKeyword))
                                {
                                    upkeepStat.Mode = UpkeepStat.MODE_PREFERRED;
                                }
                                upkeepStat.UpkeepRequiredAmount = cost.TemplateOdd.MaxQuantity;
                                upKeepInfo.UpkeepStats.Add(upkeepStat);
                            }


                        }
                    }

                }

            }


        }

        //Debug.Log("T1 inv check before after...: ");
        //Debug.Log("T1 " + player.OwnedItems.GetInformation());
        if (debug)
        {
            Debug.Log("checking projectedupkeeps after player inventory: " + projectedUpkeeps.Count);
            foreach (AssignedUpkeepInfo item in projectedUpkeeps)
            {
                Debug.Log("checking assignedupkeepinfo " + item.EntityID + " item count " + item.UpkeepStats.Count);
                foreach (UpkeepStat up in item.UpkeepStats)
                {
                    Debug.Log("checking upkeepStat " + up.Keyword + " x" + up.Amount + " mode " + up.Mode + " source " + up.Source + " UpkeepIndex " + up.UpkeepIndex + " UpkeepRequiredAmount " + up.UpkeepRequiredAmount);
                }
            }
        }


        //creating new commands as we get player items
        foreach (AssignedUpkeepInfo assignedUpkeepInfo in projectedUpkeeps)
        {
            foreach (Entity entity in incUnits)
            {
                if (entity.UpKeep.UpkeepType == UpKeep.ConsumeFromEntityThenPlayer || entity.UpKeep.UpkeepType == UpKeep.UpkeepFromPlayer || entity.UpKeep.UpkeepType == UpKeep.ConsumeFromPlayerThenEntity)
                {
                    if (entity.UnitID == assignedUpkeepInfo.EntityID)
                    {
                        assignedUpkeepInfo.SortByLowestValue();
                        foreach (UpkeepStat upKeepStat in assignedUpkeepInfo.UpkeepStats)
                        {
                            if (upKeepStat.Mode != UpkeepStat.MODE_SPECIFIC || upKeepStat.Source != UpkeepStat.SOURCE_PLAYER_INVENTORY)
                            {
                                continue;
                            }
                            int amountToTake = upKeepStat.UpkeepRequiredAmount;
                            int amountAvalible = (int)upKeepStat.Amount;
                            foreach (UpkeepCommand upkeepCommand in upkeepCommands)
                            {
                                //we do unit id check, because then we can count for entity's own items for this
                                if (upkeepCommand.itemTemplate == upKeepStat.Keyword)
                                {
                                    //if items are from entitiy's own backpack, then decrease amount needed by this entity
                                    if (upkeepCommand.entityID == entity.UnitID && upkeepCommand.upkeepCostIndex == upKeepStat.UpkeepIndex)
                                    {
                                        if (upkeepCommand.source != upKeepStat.Source)
                                        {
                                            amountToTake -= upkeepCommand.itemQuantity;
                                        }
                                        else //we add back to amount to take if this is from same source
                                        {
                                            amountToTake += upkeepCommand.itemQuantity;
                                            amountToTake = Math.Min(amountToTake, upKeepStat.UpkeepRequiredAmount);
                                        }
                                     
                                    }
                                    if (upkeepCommand.source == upKeepStat.Source) //means other entity contests player items
                                    {
                                        amountAvalible -= upkeepCommand.itemQuantity;
                                    }
                                    
                                }
                                else if (upkeepCommand.upkeepCostIndex == upKeepStat.UpkeepIndex && upkeepCommand.entityID == entity.UnitID) //this part matters primaraly due to types
                                {
                                    //if upkeep indexes match, means we are seeing command for same upkeeps
                                    //suppose upKeepStat is cheese, and command is take goblin fish jerky , and they both have index to upkeep for type food
                                    //if the item and command are indeed for same upkeep, decrease amount needed to take
                                    amountToTake -= upkeepCommand.itemQuantity;
                                }
                            }
                            //amountToTake is 0 or less, means you got enough from entity inventory, so you dont need to take from player inventory
                            if (amountAvalible > 0 && amountToTake > 0)
                            {
                                UpkeepCommand newUpkeepCommand = new UpkeepCommand();
                                newUpkeepCommand.entityID = entity.UnitID;
                                newUpkeepCommand.itemQuantity = Math.Min(amountToTake, amountAvalible);
                                newUpkeepCommand.source = upKeepStat.Source;
                                newUpkeepCommand.itemTemplate = upKeepStat.Keyword;
                                newUpkeepCommand.upkeepCostIndex = upKeepStat.UpkeepIndex;
                                upkeepCommands.Add(newUpkeepCommand);
                            }

                        }

                        foreach (UpkeepStat upKeepStat in assignedUpkeepInfo.UpkeepStats)
                        {
                            if (upKeepStat.Mode != UpkeepStat.MODE_PREFERRED || upKeepStat.Source != UpkeepStat.SOURCE_PLAYER_INVENTORY)
                            {
                                continue;
                            }
                            int amountToTake = upKeepStat.UpkeepRequiredAmount;
                            int amountAvalible = (int)upKeepStat.Amount;
                            foreach (UpkeepCommand upkeepCommand in upkeepCommands)
                            {
                                //we do unit id check, because then we can count for entity's own items for this
                                if (upkeepCommand.itemTemplate == upKeepStat.Keyword)
                                {
                                    //if items are from entitiy's own backpack, then decrease amount needed by this entity
                                    if (upkeepCommand.entityID == entity.UnitID && upkeepCommand.upkeepCostIndex == upKeepStat.UpkeepIndex)
                                    {
                                        if (upkeepCommand.source != upKeepStat.Source )
                                        {
                                            amountToTake -= upkeepCommand.itemQuantity;
                                        }
                                        else
                                        {
                                            amountToTake += upkeepCommand.itemQuantity;
                                            amountToTake = Math.Min(amountToTake, upKeepStat.UpkeepRequiredAmount);
                                        }

                                    }
                                    if (upkeepCommand.source == upKeepStat.Source) //means other entity contests player items
                                    {
                                        amountAvalible -= upkeepCommand.itemQuantity;
                                    }

                                }
                                else if (upkeepCommand.upkeepCostIndex == upKeepStat.UpkeepIndex && upkeepCommand.entityID == entity.UnitID) //this part matters primaraly due to types
                                {
                                    //if upkeep indexes match, means we are seeing command for same upkeeps
                                    //suppose upKeepStat is cheese, and command is take goblin fish jerky , and they both have index to upkeep for type food
                                    //if the item and command are indeed for same upkeep, decrease amount needed to take
                                    amountToTake -= upkeepCommand.itemQuantity;
                                }
                            }
                            //amountToTake is 0 or less, means you got enough from entity inventory, so you dont need to take from player inventory
                            if (amountAvalible > 0 && amountToTake > 0)
                            {
                                UpkeepCommand newUpkeepCommand = new UpkeepCommand();
                                newUpkeepCommand.entityID = entity.UnitID;
                                newUpkeepCommand.itemQuantity = Math.Min(amountToTake, amountAvalible);
                                newUpkeepCommand.source = upKeepStat.Source;
                                newUpkeepCommand.itemTemplate = upKeepStat.Keyword;
                                newUpkeepCommand.upkeepCostIndex = upKeepStat.UpkeepIndex;
                                upkeepCommands.Add(newUpkeepCommand);
                            }

                        }

                        foreach (UpkeepStat upKeepStat in assignedUpkeepInfo.UpkeepStats)
                        {
                            if (upKeepStat.Mode != UpkeepStat.MODE_TYPE || upKeepStat.Source != UpkeepStat.SOURCE_PLAYER_INVENTORY)
                            {
                                continue;
                            }
                            int amountToTake = upKeepStat.UpkeepRequiredAmount;
                            int amountAvalible = (int)upKeepStat.Amount;
                            foreach (UpkeepCommand upkeepCommand in upkeepCommands)
                            {
                                //we do unit id check, because then we can count for entity's own items for this
                                if (upkeepCommand.itemTemplate == upKeepStat.Keyword)
                                {
                                    //if items are from entitiy's own backpack, then decrease amount needed by this entity
                                    if (upkeepCommand.entityID == entity.UnitID && upkeepCommand.upkeepCostIndex == upKeepStat.UpkeepIndex)
                                    {
                                        if (upkeepCommand.source != upKeepStat.Source)
                                        {
                                            amountToTake -= upkeepCommand.itemQuantity;
                                        }
                                        else
                                        {
                                            amountToTake += upkeepCommand.itemQuantity;
                                            amountToTake = Math.Min(amountToTake, upKeepStat.UpkeepRequiredAmount);
                                        }

                                    }
                                    if (upkeepCommand.source == upKeepStat.Source) //means other entity contests player items
                                    {
                                        amountAvalible -= upkeepCommand.itemQuantity;
                                    }

                                }
                                else if (upkeepCommand.upkeepCostIndex == upKeepStat.UpkeepIndex && upkeepCommand.entityID == entity.UnitID) //this part matters primaraly due to types
                                {
                                    //if upkeep indexes match, means we are seeing command for same upkeeps
                                    //suppose upKeepStat is cheese, and command is take goblin fish jerky , and they both have index to upkeep for type food
                                    //if the item and command are indeed for same upkeep, decrease amount needed to take
                                    amountToTake -= upkeepCommand.itemQuantity;
                                }
                            }
                            //amountToTake is 0 or less, means you got enough from entity inventory, so you dont need to take from player inventory
                            if (amountAvalible > 0 && amountToTake > 0) 
                            {
                                UpkeepCommand newUpkeepCommand = new UpkeepCommand();
                                newUpkeepCommand.entityID = entity.UnitID;
                                newUpkeepCommand.itemQuantity = Math.Min(amountToTake, amountAvalible);
                                newUpkeepCommand.source = upKeepStat.Source;
                                newUpkeepCommand.itemTemplate = upKeepStat.Keyword;
                                newUpkeepCommand.upkeepCostIndex = upKeepStat.UpkeepIndex;
                                upkeepCommands.Add(newUpkeepCommand);
                            }

                        }

                    }
                }
             
            }
        }

        if (debug)
        {
            Debug.Log("checking upkeep commands after player inventory, command count: " + upkeepCommands.Count);
            foreach (UpkeepCommand upkeepCommandToDebug in upkeepCommands)
            {
                Debug.Log("checking upkeepCommand: unit id: " + upkeepCommandToDebug.entityID + " item: " + upkeepCommandToDebug.itemTemplate + " x" + upkeepCommandToDebug.itemQuantity + " for upkeep(index) " + upkeepCommandToDebug.upkeepCostIndex + " from " + upkeepCommandToDebug.source);
            }
        }

        //going through items in entity inventory again(ConsumeFromPlayerThenEntity)
        foreach (AssignedUpkeepInfo upKeepInfo in projectedUpkeeps)
        {

            foreach (Entity ent in incUnits)
            {
                if (ent.UpKeep.UpkeepType == UpKeep.ConsumeFromPlayerThenEntity)
                {
                    foreach (UpkeepCost cost in ent.UpKeep.Costs)
                    {
                        if (cost.TemplateOdd.TemplateKeyword != "")
                        {
                            int itemQuantity = ent.BackPack.GetSameItemAmount(cost.TemplateOdd.TemplateKeyword);
                            UpkeepStat upkeepStat = new UpkeepStat();
                            upkeepStat.Keyword = cost.TemplateOdd.TemplateKeyword;
                            upkeepStat.Amount = itemQuantity;
                            upkeepStat.Source = UpkeepStat.SOURCE_ENTITY_INVENTORY;
                            upkeepStat.UpkeepIndex = ent.UpKeep.Costs.IndexOf(cost);
                            upkeepStat.Mode = UpkeepStat.MODE_SPECIFIC;
                            upkeepStat.UpkeepRequiredAmount = cost.TemplateOdd.MaxQuantity;
                            upKeepInfo.UpkeepStats.Add(upkeepStat);

                        }
                        else
                        {

                            ItemCollection typeItems = new ItemCollection();
                            typeItems.AddRangeItems(ObjectCopier.Clone<ItemCollection>(ent.BackPack.FindCorrectTypeItems(cost.TemplateOdd.Types, cost.TemplateOdd.NotWantedTypes)));
                            foreach (Item item in typeItems)
                            {
                                UpkeepStat upkeepStat = new UpkeepStat();
                                upkeepStat.Keyword = item.TemplateKeyword;
                                upkeepStat.Amount = item.Quantity;
                                upkeepStat.Source = UpkeepStat.SOURCE_ENTITY_INVENTORY;
                                upkeepStat.UpkeepIndex = ent.UpKeep.Costs.IndexOf(cost);
                                upkeepStat.Mode = UpkeepStat.MODE_TYPE;
                                if (cost.PreferredItems.Contains(item.TemplateKeyword))
                                {
                                    upkeepStat.Mode = UpkeepStat.MODE_PREFERRED;
                                }
                                upkeepStat.UpkeepRequiredAmount = cost.TemplateOdd.MaxQuantity;
                                upKeepInfo.UpkeepStats.Add(upkeepStat);
                            }


                        }
                    }

                }

            }


        }


        if (debug)
        {
            Debug.Log("checking projectedupkeeps after entity ConsumeFromPlayerThenEntity: " + projectedUpkeeps.Count);
            foreach (AssignedUpkeepInfo item in projectedUpkeeps)
            {
                Debug.Log("checking assignedupkeepinfo " + item.EntityID + " item count " + item.UpkeepStats.Count);
                foreach (UpkeepStat up in item.UpkeepStats)
                {
                    Debug.Log("checking upkeepStat " + up.Keyword + " x" + up.Amount + " mode " + up.Mode + " source " + up.Source + " UpkeepIndex " + up.UpkeepIndex + " UpkeepRequiredAmount " + up.UpkeepRequiredAmount);
                }
            }
        }


        //creating upkeep commands for the entity items again after player inv
        foreach (AssignedUpkeepInfo assignedUpkeepInfo in projectedUpkeeps)
        {
            foreach (Entity entity in incUnits)
            {
                if (entity.UpKeep.UpkeepType == UpKeep.ConsumeFromPlayerThenEntity)
                {
                    if (entity.UnitID == assignedUpkeepInfo.EntityID)
                    {
                        assignedUpkeepInfo.SortByLowestValue();
                        foreach (UpkeepStat upKeepStat in assignedUpkeepInfo.UpkeepStats)
                        {
                            if (upKeepStat.Mode != UpkeepStat.MODE_SPECIFIC || upKeepStat.Source != UpkeepStat.SOURCE_ENTITY_INVENTORY)
                            {
                                continue;
                            }
                            int amountToTake = upKeepStat.UpkeepRequiredAmount;
                            int amountAvalible = (int)upKeepStat.Amount;
                            foreach (UpkeepCommand upkeepCommand in upkeepCommands)
                            {
                                //we do unit id check, because then we can count for entity's own items for this
                                if (upkeepCommand.itemTemplate == upKeepStat.Keyword)
                                {
                                    //if items are from entitiy's own backpack, then decrease amount needed by this entity
                                    if (upkeepCommand.entityID == entity.UnitID && upkeepCommand.upkeepCostIndex == upKeepStat.UpkeepIndex)
                                    {
                                        if (upkeepCommand.source != upKeepStat.Source)
                                        {
                                            amountToTake -= upkeepCommand.itemQuantity;
                                        }
                                        else //we add back to amount to take if this is from same source
                                        {
                                            amountToTake += upkeepCommand.itemQuantity;
                                            amountToTake = Math.Min(amountToTake, upKeepStat.UpkeepRequiredAmount);
                                        }

                                    }
                                    if (upkeepCommand.source == upKeepStat.Source) //means other entity contests player items
                                    {
                                        amountAvalible -= upkeepCommand.itemQuantity;
                                    }

                                }
                                else if (upkeepCommand.upkeepCostIndex == upKeepStat.UpkeepIndex) //this part matters primaraly due to types
                                {
                                    //if upkeep indexes match, means we are seeing command for same upkeeps
                                    //suppose upKeepStat is cheese, and command is take goblin fish jerky , and they both have index to upkeep for type food
                                    //if the item and command are indeed for same upkeep, decrease amount needed to take
                                    amountToTake -= upkeepCommand.itemQuantity;
                                }
                            }
                            //amountToTake is 0 or less, means you got enough from entity inventory, so you dont need to take from player inventory
                            if (amountAvalible > 0 && amountToTake > 0)
                            {
                                UpkeepCommand newUpkeepCommand = new UpkeepCommand();
                                newUpkeepCommand.entityID = entity.UnitID;
                                newUpkeepCommand.itemQuantity = Math.Min(amountToTake, amountAvalible);
                                newUpkeepCommand.source = upKeepStat.Source;
                                newUpkeepCommand.itemTemplate = upKeepStat.Keyword;
                                newUpkeepCommand.upkeepCostIndex = upKeepStat.UpkeepIndex;
                                upkeepCommands.Add(newUpkeepCommand);
                            }

                        }

                        foreach (UpkeepStat upKeepStat in assignedUpkeepInfo.UpkeepStats)
                        {
                            if (upKeepStat.Mode != UpkeepStat.MODE_PREFERRED || upKeepStat.Source != UpkeepStat.SOURCE_ENTITY_INVENTORY)
                            {
                                continue;
                            }
                            int amountToTake = upKeepStat.UpkeepRequiredAmount;
                            int amountAvalible = (int)upKeepStat.Amount;
                            foreach (UpkeepCommand upkeepCommand in upkeepCommands)
                            {
                                //we do unit id check, because then we can count for entity's own items for this
                                if (upkeepCommand.itemTemplate == upKeepStat.Keyword)
                                {
                                    //if items are from entitiy's own backpack, then decrease amount needed by this entity
                                    if (upkeepCommand.entityID == entity.UnitID && upkeepCommand.upkeepCostIndex == upKeepStat.UpkeepIndex)
                                    {
                                        if (upkeepCommand.source != upKeepStat.Source)
                                        {
                                            amountToTake -= upkeepCommand.itemQuantity;
                                        }
                                        else
                                        {
                                            amountToTake += upkeepCommand.itemQuantity;
                                            amountToTake = Math.Min(amountToTake, upKeepStat.UpkeepRequiredAmount);
                                        }

                                    }
                                    if (upkeepCommand.source == upKeepStat.Source) //means other entity contests player items
                                    {
                                        amountAvalible -= upkeepCommand.itemQuantity;
                                    }

                                }
                                else if (upkeepCommand.upkeepCostIndex == upKeepStat.UpkeepIndex) //this part matters primaraly due to types
                                {
                                    //if upkeep indexes match, means we are seeing command for same upkeeps
                                    //suppose upKeepStat is cheese, and command is take goblin fish jerky , and they both have index to upkeep for type food
                                    //if the item and command are indeed for same upkeep, decrease amount needed to take
                                    amountToTake -= upkeepCommand.itemQuantity;
                                }
                            }
                            //amountToTake is 0 or less, means you got enough from entity inventory, so you dont need to take from player inventory
                            if (amountAvalible > 0 && amountToTake > 0)
                            {
                                UpkeepCommand newUpkeepCommand = new UpkeepCommand();
                                newUpkeepCommand.entityID = entity.UnitID;
                                newUpkeepCommand.itemQuantity = Math.Min(amountToTake, amountAvalible);
                                newUpkeepCommand.source = upKeepStat.Source;
                                newUpkeepCommand.itemTemplate = upKeepStat.Keyword;
                                newUpkeepCommand.upkeepCostIndex = upKeepStat.UpkeepIndex;
                                upkeepCommands.Add(newUpkeepCommand);
                            }

                        }

                        foreach (UpkeepStat upKeepStat in assignedUpkeepInfo.UpkeepStats)
                        {
                            if (upKeepStat.Mode != UpkeepStat.MODE_TYPE || upKeepStat.Source != UpkeepStat.SOURCE_ENTITY_INVENTORY)
                            {
                                continue;
                            }
                            int amountToTake = upKeepStat.UpkeepRequiredAmount;
                            int amountAvalible = (int)upKeepStat.Amount;
                            foreach (UpkeepCommand upkeepCommand in upkeepCommands)
                            {
                                //we do unit id check, because then we can count for entity's own items for this
                                if (upkeepCommand.itemTemplate == upKeepStat.Keyword)
                                {
                                    //if items are from entitiy's own backpack, then decrease amount needed by this entity
                                    if (upkeepCommand.entityID == entity.UnitID && upkeepCommand.upkeepCostIndex == upKeepStat.UpkeepIndex)
                                    {
                                        if (upkeepCommand.source != upKeepStat.Source)
                                        {
                                            amountToTake -= upkeepCommand.itemQuantity;
                                        }
                                        else
                                        {
                                            amountToTake += upkeepCommand.itemQuantity;
                                            amountToTake = Math.Min(amountToTake, upKeepStat.UpkeepRequiredAmount);
                                        }

                                    }
                                    if (upkeepCommand.source == upKeepStat.Source) //means other entity contests player items
                                    {
                                        amountAvalible -= upkeepCommand.itemQuantity;
                                    }

                                }
                                else if (upkeepCommand.upkeepCostIndex == upKeepStat.UpkeepIndex) //this part matters primaraly due to types
                                {
                                    //if upkeep indexes match, means we are seeing command for same upkeeps
                                    //suppose upKeepStat is cheese, and command is take goblin fish jerky , and they both have index to upkeep for type food
                                    //if the item and command are indeed for same upkeep, decrease amount needed to take
                                    amountToTake -= upkeepCommand.itemQuantity;
                                }
                            }
                            //amountToTake is 0 or less, means you got enough from entity inventory, so you dont need to take from player inventory
                            if (amountAvalible > 0 && amountToTake > 0)
                            {
                                UpkeepCommand newUpkeepCommand = new UpkeepCommand();
                                newUpkeepCommand.entityID = entity.UnitID;
                                newUpkeepCommand.itemQuantity = Math.Min(amountToTake, amountAvalible);
                                newUpkeepCommand.source = upKeepStat.Source;
                                newUpkeepCommand.itemTemplate = upKeepStat.Keyword;
                                newUpkeepCommand.upkeepCostIndex = upKeepStat.UpkeepIndex;
                                upkeepCommands.Add(newUpkeepCommand);
                            }

                        }

                    }
                }

            }
        }

        if (debug)
        {
            Debug.Log("checking upkeep commands after the second entity round, command count: " + upkeepCommands.Count);
            foreach (UpkeepCommand upkeepCommandToDebug in upkeepCommands)
            {
                Debug.Log("checking upkeepCommand: unit id: " + upkeepCommandToDebug.entityID + " item: " + upkeepCommandToDebug.itemTemplate + " x" + upkeepCommandToDebug.itemQuantity + " for upkeep(index) " + upkeepCommandToDebug.upkeepCostIndex + " from " + upkeepCommandToDebug.source);
            }
        }
    
        //checking satisfaction of entities
        foreach (Entity entity in incUnits)
        {
            foreach (UpkeepCost upkeepCost in entity.UpKeep.Costs) //going through costs
            {
                int amountGathered = 0;
                foreach (UpkeepCommand command in upkeepCommands) //going through upkeeps
                {
                    if (command.upkeepCostIndex == entity.UpKeep.Costs.IndexOf(upkeepCost))
                    {
                        amountGathered += command.itemQuantity;
                    }
                }
                //we didnt get enough for the upkeep cost
                if (amountGathered < upkeepCost.TemplateOdd.MaxQuantity)
                {
                    for (var i = incUnits.Count - 1; i >= 0; i--) //we process the list in reverse(lowest value entities first)
                    {
                        if (entity.CurrentValue > incUnits[i].CurrentValue) //if an entity has lower value than current entity, we take from it
                        {
                            List<UpkeepCommand> newUpkeepCommands = new List<UpkeepCommand>();
                            List<UpkeepCommand> commandsToRemove = new List<UpkeepCommand>();
                            foreach (UpkeepCommand lowerEntityUpkeepCommand in upkeepCommands) //going through all commands
                            {
                                if (amountGathered == upkeepCost.TemplateOdd.MaxQuantity) //we have gathered enough, so we break out of loop and add range of the new commands
                                {
                                    break;
                                }
                                if (lowerEntityUpkeepCommand.entityID == incUnits[i].UnitID) //command belongs to the lower entity
                                {
                                    if (lowerEntityUpkeepCommand.source == UpkeepStat.SOURCE_PLAYER_INVENTORY) //we take only if its from player inventory
                                    {
                                        if (upkeepCost.TemplateOdd.TemplateKeyword != "") //higher entity wants to take specific item
                                        {
                                            if (lowerEntityUpkeepCommand.itemTemplate == upkeepCost.TemplateOdd.TemplateKeyword) //the item matches
                                            {
                                                UpkeepCommand newUpkeepCommand = new UpkeepCommand();
                                                newUpkeepCommand.entityID = entity.UnitID;
                                                newUpkeepCommand.itemQuantity = 0;
                                                int quantity = lowerEntityUpkeepCommand.itemQuantity;
                                                for (int j = 0; j < quantity; j++)//draining lower entity upkeep in favor of higher entity upkeep
                                                {
                                                    if (amountGathered == upkeepCost.TemplateOdd.MaxQuantity)
                                                    {
                                                        break;
                                                    }
                                                    newUpkeepCommand.itemQuantity++;
                                                    amountGathered++;
                                                    lowerEntityUpkeepCommand.itemQuantity--;
                                                }

                                                newUpkeepCommand.source = UpkeepStat.SOURCE_PLAYER_INVENTORY;
                                                newUpkeepCommand.itemTemplate = lowerEntityUpkeepCommand.itemTemplate;
                                                newUpkeepCommand.upkeepCostIndex = entity.UpKeep.Costs.IndexOf(upkeepCost);
                                                newUpkeepCommands.Add(newUpkeepCommand);

                                                if (lowerEntityUpkeepCommand.itemQuantity == 0) //upkeep was drained entirely, removing
                                                {
                                                    commandsToRemove.Add(lowerEntityUpkeepCommand);
                                                }

                                            }
                                        }
                                        else //higher entity wants to take a type
                                        {
                                            ItemTemplate itemTemplate = GameEngine.Data.ItemTemplateCollection.findByKeyword(lowerEntityUpkeepCommand.itemTemplate);
                                            if (itemTemplate.isCorrectTypes(upkeepCost.TemplateOdd.Types, upkeepCost.TemplateOdd.NotWantedTypes)) //the command matched correct item
                                            {
                                                UpkeepCommand newUpkeepCommand = new UpkeepCommand();
                                                newUpkeepCommand.entityID = entity.UnitID;
                                                newUpkeepCommand.itemQuantity = 0;
                                                int quantity = lowerEntityUpkeepCommand.itemQuantity;
                                                for (int j = 0; j < quantity; j++) //draining lower entity upkeep in favor of higher entity upkeep
                                                {
                                                    if (amountGathered == upkeepCost.TemplateOdd.MaxQuantity)
                                                    {
                                                        break;
                                                    }
                                                    newUpkeepCommand.itemQuantity++;
                                                    amountGathered++;
                                                    lowerEntityUpkeepCommand.itemQuantity--;
                                                }

                                                newUpkeepCommand.source = UpkeepStat.SOURCE_PLAYER_INVENTORY;
                                                newUpkeepCommand.itemTemplate = itemTemplate.Keyword;
                                                newUpkeepCommand.upkeepCostIndex = entity.UpKeep.Costs.IndexOf(upkeepCost);
                                                newUpkeepCommands.Add(newUpkeepCommand);

                                                if (lowerEntityUpkeepCommand.itemQuantity == 0) //upkeep was drained entirely, removing
                                                {
                                                    commandsToRemove.Add(lowerEntityUpkeepCommand);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            foreach (UpkeepCommand commandToRemove in commandsToRemove)
                            {
                                upkeepCommands.Remove(commandToRemove);
                            }
                            upkeepCommands.AddRange(newUpkeepCommands);

                        }
                    }
                }
            }

        }
        UpkeepResults upkeepResults = new UpkeepResults();
        if (debug)
        {
            Debug.Log("starting paying the items");
        }
        //we pay the upkeeps
        foreach (Entity entity in incUnits)
        {
            if (entity.UpKeep.UpkeepType == UpKeep.NoUpKeep)
            {
                continue;
            }
            int totalGathered = 0;
            int totalRequired = 0;
            if (debug)
            {
                Debug.Log("paying to: " + entity.UnitID + " " + entity.CharacterTemplateKeyword);
            }
       
            foreach (UpkeepCost upkeepCost in entity.UpKeep.Costs)
            {
                totalRequired += upkeepCost.TemplateOdd.MaxQuantity;
                int amountGathered = 0;
                foreach (UpkeepCommand command in upkeepCommands) //going through upkeeps
                {
                    if (command.upkeepCostIndex == entity.UpKeep.Costs.IndexOf(upkeepCost) && command.entityID == entity.UnitID)
                    {
                        amountGathered += command.itemQuantity;
                        switch (command.source)
                        {
                            case UpkeepStat.SOURCE_PLAYER_INVENTORY:
                                
                                if (player.OwnedItems.GetSameItemAmount(command.itemTemplate) >= command.itemQuantity)
                                {
                                    player.OwnedItems.GetAndRemoveItemsByKeyword(command.itemTemplate, command.itemQuantity, player.PlayerID);
                                    upkeepResults.AddItem(command.itemTemplate,entity.UnitID,command.itemQuantity,command.source,entity.CharacterTemplateKeyword);
                                    if (debug)
                                    {
                                        Debug.Log("paying: " + command.itemTemplate + " x" + command.itemQuantity + " upkeep index: " + command.upkeepCostIndex + " from " + command.source);
                                    }
                                  
                                }
                                else
                                {
                                    Debug.LogError("PayUnitUpkeeps(new) player owned items didnt have enough of " + command.itemTemplate + " x" + command.itemQuantity + " player has only " + player.OwnedItems.GetSameItemAmount(command.itemTemplate) + " of player " + player.PlayerID);
                                }
                                break;
                            case UpkeepStat.SOURCE_ENTITY_INVENTORY:
                                if (entity.BackPack.GetSameItemAmount(command.itemTemplate) >= command.itemQuantity)
                                {
                                    entity.BackPack.GetAndRemoveItemsByKeyword(command.itemTemplate, command.itemQuantity, player.PlayerID);
                                    upkeepResults.AddItem(command.itemTemplate, entity.UnitID, command.itemQuantity, command.source,entity.CharacterTemplateKeyword);
                                    if (debug)
                                    {
                                        Debug.Log("paying: " + command.itemTemplate + " x" + command.itemQuantity + " upkeep index: " + command.upkeepCostIndex + " from " + command.source);
                                    }
                                }
                                else
                                {
                                    Debug.LogError("PayUnitUpkeeps(new) entity.BackPack items didnt have enough of " + command.itemTemplate + " x" + command.itemQuantity + " entity has only " + player.OwnedItems.GetSameItemAmount(command.itemTemplate) + " of unit " + entity.CharacterTemplateKeyword + " " + entity.UnitID + " " + player.PlayerID);
                                }

                                break;
                            default:
                                break;
                        }
       


                    }
                }
                totalGathered += amountGathered;
            }
            if (debug)
            {
                Debug.Log("total paid: " + totalGathered+"/"+totalRequired);
            }
            int moodChange = 1; 
            if (totalRequired > 0)
            {
                moodChange = ((totalRequired - totalGathered) * 100) / totalRequired * -1;
            }
           
            if (moodChange == 0)
            {
                if (totalGathered == 0 && totalRequired > 0)
                {
                    moodChange = -100; //you didnt upkeep, -100 to relation

                }
                else
                {
                    moodChange = 1; //you upkept, so +1 to relation
                }


            
            }
            int finalAttitude = entity.AddToAttitude(player.PlayerID, moodChange, moodChange);
            //if no building(garrison) then loosing it
            if (entity.UpKeep.BuildingID != -1)
            {
                Building unitBuilding = FindBuildingByID(entity.UpKeep.BuildingID);

                if (unitBuilding == null)
                {
                    entity.AddToAttitude(player.PlayerID, -10, -10);
                }
            }
            if (finalAttitude <= 0) //moodchange = 0 means no upkeep was paid at all
            {
                entity.IsHungry = true;
            
            }
            upkeepResults.AddMoodChange(entity.CharacterTemplateKeyword,entity.UnitID,moodChange,finalAttitude,entity.IsHungry,totalGathered,totalRequired);
        }
        if (upkeepResults.Count > 0)
        {
            Notification notification = new Notification();
            notification.ID = ++player.LocalNotificationID;
            notification.Type = Notification.NotificationType.TYPE_UPKEEP_RESULTS;
            notification.Picture = "Poneti/ResourceItems/Loot_03";
         
            notification.HeaderText = notificationTitle;
            notification.ExpandedText = notificationTitle;
            foreach (UpkeepResult result in upkeepResults)
            {
                NotificationElement notificationElement = new NotificationElement();
                notificationElement.ItemKeyword = result.ItemKeyword;
                notificationElement.Content = result.GenerateInfo(player);
                notificationElement.Picture = GameEngine.Data.ItemTemplateCollection.findByKeyword(result.ItemKeyword).Picture;
                notificationElement.BgImageSprite = "ButtonIcons/bg 1";
                notificationElement.BgImageA = 255;
                notification.NotificationElements.Add(notificationElement);
                foreach (PaidUpkeepInfo paidUpkeepInfo in result.EntitiesAndItemQuantityConsumed)
                {
                    notificationElement = new NotificationElement();
                    notificationElement.EntityKeyword = paidUpkeepInfo.unitKeyword;
                    notificationElement.Content = paidUpkeepInfo.GenerateInfo(result.ItemKeyword);
                    notificationElement.Picture = GameEngine.Data.CharacterTemplateCollection.findByKeyword(paidUpkeepInfo.unitKeyword).PortraitPicture;
                    notificationElement.BgImageSprite = "ButtonIcons/bg 3";
                    notificationElement.BgImageA = 255;
                    notification.NotificationElements.Add(notificationElement);
                }
            }

            foreach (MoodChangeAfterUpkeep moodChangeAfterUpkeep in upkeepResults.relationsList)
            {
                
                NotificationElement notificationElement = new NotificationElement();
                notificationElement.EntityKeyword = moodChangeAfterUpkeep.entKW;
                string number = moodChangeAfterUpkeep.moodChange.ToString();
                
                if (moodChangeAfterUpkeep.moodChange > 0) //no need to do for negative, as '-' is included in the number string
                {
                    number = "+" + moodChangeAfterUpkeep.moodChange;
                }
                string isHungry = "";
                byte localR = 255;
                byte localG = 255;
                byte localB = 255;
                byte localTextR = 255;
                byte localTextG = 255;
                byte localTextB = 255;
                if (moodChangeAfterUpkeep.isHungry)
                {
                    isHungry = ", is deserting";
                    localR = 180;
                    localG = 120;
                    localB = 120;
                    localTextR = 255;
                    localTextG = 200;
                    localTextB = 200;
                }
                notificationElement.Content = moodChangeAfterUpkeep.entID + " " + moodChangeAfterUpkeep.entKW + " opinion of you: " + moodChangeAfterUpkeep.currentRelation + "(" + number + ")" + ","+ moodChangeAfterUpkeep.totalPaid + "/"+moodChangeAfterUpkeep.totalNeeded + " upkept " + isHungry;
                notificationElement.Picture = GameEngine.Data.CharacterTemplateCollection.findByKeyword(moodChangeAfterUpkeep.entKW).PortraitPicture;
                notificationElement.BgImageSprite = "ButtonIcons/bag3";
                notificationElement.BgImageA = 255;
                notificationElement.BgImageR = localR;
                notificationElement.BgImageG = localG;
                notificationElement.BgImageB = localB;
                notificationElement.TextColorR = localTextR;
                notificationElement.TextColorG = localTextG;
                notificationElement.TextColorB = localTextB;
                notification.NotificationElements.Add(notificationElement);
            }

            player.Notifications.Add(notification);
        }
    }

    public void PayUnitUpkeeps()
    {
        //  OurLog.Print("Pay hero upkeeps started");
        Army AllUnitsInGame = GetAllUnitsInTheGame(false, true); //uses even units in storage
      
            foreach (Entity unit in AllUnitsInGame.Units)
            {

            if (unit.UpKeep.BuildingID != -1)
            {
                Building unitBuilding = FindBuildingByID(unit.UpKeep.BuildingID);

                if (unitBuilding == null)
                {
                    unit.AddToAttitude(unit.FindCurrentOwnerID(), -10, -10);
                }
            }

            switch (unit.UpKeep.UpkeepType)
            {
                case UpKeep.UpkeepFromPlayer:
                    Player player = this.FindPlayerByID(unit.FindCurrentOwnerID());
                    MoodChangeInfo moodChange = player.MoodChangeAfterUpkeep(unit.UnitID, true);
                    int mood = moodChange.MoodChange;
                    unit.MoodChange = moodChange;
                    unit.AddToAttitude(player.PlayerID, mood, mood);
                    //Debug.Log("setting moodchange to: " + unit.UnitName + " " + unit.UnitID);
                    if (mood <= 0)
                    {
                        unit.IsHungry = true;
                    }
                    else
                    {
                        unit.IsHungry = false;
                    }

                    break;
                case UpKeep.ConsumeFromEntityInventory:
                    bool isHungry = false;
                    foreach (UpkeepCost cost in unit.UpKeep.Costs)
                    {
                        //Stat reserve = unit.UpKeep.Reserves.findStatByKeyword(cost.Keyword);

                        for (int i = 0; i < cost.TemplateOdd.MaxQuantity; i++)
                        {
                            Item foodItem = unit.BackPack. FindAndRemoveCorrectTypeItem(cost.TemplateOdd.Types, cost.TemplateOdd.NotWantedTypes, true);
                            if (foodItem == null)
                            {
                                isHungry = true;
                                break;
                            }


                        }

                    }

                    if (isHungry) unit.IsHungry = true;
                    break;
                case UpKeep.NoUpKeep:

                    break;
                default:
                    Debug.LogError(unit.UnitName + " unknowntype of upkeep: " + unit.UpKeep.UpkeepType);
                    break;
            }
           
            
        }
        

    }


    void ProcessPlayerHeroCapturePhase(string playerID)
    {
        lock (Armies)
        {
            foreach (Army army in armies)
            {
                if (army.OwnerPlayerID != playerID)
                {
                    continue;
                }
                if (army.Location.Mode != Location.MODE_OVERLAND)
                {
                    continue;
                }
                GameSquare gameSquare = Worldmap.FindGameSquareByCoordinates(army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);
                if (gameSquare.BattleFieldID != -1)
                {
                    if (IsBattleBeingResolvedRightNow(gameSquare.BattleFieldID)) //we wait for the battle to be over/continued, so we capture only when battle round is over
                    {
                        continue;
                    }
                }
                Entity leader = FindUnitByUnitID(army.LeaderID);
                if (leader != null)
                {
                    if (leader.Mission != null)
                    {
                        if (leader.Mission.MissionName == Mission.mission_Capture)
                        {
                            CaptureGameSquare(army);
                        }


                    }



                }



            }
        }

    }

    void ProcessPlayerHeroSurveyPhase(string playerID)
    {
        lock (Armies)
        {
            foreach (Army army in armies)
            {
                if (army.OwnerPlayerID != playerID)
                {
                    continue;
                }
                if (army.Location.Mode != Location.MODE_OVERLAND)
                {
                    continue;
                }
                Entity leader = FindUnitByUnitID(army.LeaderID);
                if (leader != null)
                {
                    if (leader.Mission != null)
                    {
                        if (leader.Mission.MissionName == Mission.mission_Survey)
                        {
                            if (SurveyGameSquare(army))
                            {


                                leader.Mission = null;
                            }



                        }


                    }

                }

            }
        }
       
    }

    void ProcessPlayerHeroCraftingPhase(string playerID)
    {
        lock (Armies)
        {
            foreach (Army army in armies)
            {
                if (army.OwnerPlayerID != playerID)
                {
                    continue;
                }
                if (army.Location.Mode != Location.MODE_OVERLAND)
                {
                    continue;
                }
                Entity leader = FindUnitByUnitID(army.LeaderID);
                if (leader != null)
                {
                    ProcessHeroCrafting(leader, army, playerID); //local phase, use player ids

                }

            }
        }

    }

    bool IsBattleBeingResolvedRightNow(int incID)
    {
        lock (ActiveBattles.Battlefields)
        {
            foreach (BattlefieldOld oldBattlefield in ActiveBattles.Battlefields)
            {
                if (oldBattlefield.ID == incID)
                {
                    return true;
                }
            }
        }

        return false;
    }

    void ProcessPlayerHeroBuildingPhase(string playerID)
    {
        lock (Armies)
        {
            foreach (Army army in Armies)
            {
                if (army.OwnerPlayerID != playerID)
                {
                    continue;
                }
                if (army.Location.Mode != Location.MODE_OVERLAND)
                {
                    continue;
                }
                GameSquare gameSquare = Worldmap.FindGameSquareByCoordinates(army.Location.WorldMapCoordinates.XCoordinate,army.Location.WorldMapCoordinates.YCoordinate);
                if (gameSquare.BattleFieldID != -1)
                {
                    if (IsBattleBeingResolvedRightNow(gameSquare.BattleFieldID)) //we wait for the battle to be over/continued, so we build only when battle round is over
                    {
                        continue;
                    }
                }
                Entity leader = FindUnitByUnitID(army.LeaderID);
                if (leader != null)
                {
                    if (leader.Mission != null)
                    {
                        if (leader.Mission.MissionName == Mission.mission_Build)
                        {
                            StartingBuildingProcess(army.OwnerPlayerID, leader.Mission.TargetString, army.ArmyID);
                            //ProcessBuildingPlan(army.ArmyID);
                            // BuildBuilding(army.ArmyID);
                            //MemoryTile memoryTile = FindMemoryTileByCoordinates(army.OwnerPlayerID, army.WorldMapPositionX, army.WorldMapPositionY);
                            //GameSquare gameSquare = Worldmap.FindMapSquareByCordinates(army.WorldMapPositionX, army.WorldMapPositionY);
                            //memoryTile.IsSurveyed = true;
                            //memoryTile.TerrainTemplateKW = gameSquare.TerrainKeyword;
                            //leader.Mission = null;
                        }


                    }

                }

            }
        }
  
    }
    void ProcessPlayerHeroRazePhase(string playerID)
    {
        lock (Armies)
        {
            foreach (Army army in armies)
            {
                if (army.OwnerPlayerID != playerID)
                {
                    continue;
                }
                if (army.Location.Mode != Location.MODE_OVERLAND)
                {
                    continue;
                }
                GameSquare gameSquare = Worldmap.FindGameSquareByCoordinates(army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);
                if (gameSquare.BattleFieldID != -1)
                {
                    if (IsBattleBeingResolvedRightNow(gameSquare.BattleFieldID)) //we wait for the battle to be over/continued, so we raze only when battle round is over
                    {
                        continue;
                    }
                }
                Entity leader = FindUnitByUnitID(army.LeaderID);
                if (leader != null)
                {
                    if (leader.Mission != null)
                    {
                        if (leader.Mission.MissionName == Mission.mission_Raze)
                        {
                            MemoryTile memoryTile = FindMemoryTileByCoordinates(army.OwnerPlayerID, army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);

                            if (memoryTile.IsSurveyed)
                            {
                                if (RazeBuilding(army, leader.Mission.TargetID))
                                {
                                    Notification notification = new Notification();

                                    notification.IsOverland = true;
                                    notification.TargetID = leader.UnitID;
                                    notification.HeaderText = leader.UnitName + " razed building: " + memoryTile.BuildingKeyword;
                                    notification.Type = Notification.NotificationType.TYPE_HERO_MISSION_RAZE_COMPLETE;
                                    notification.ExpandedText = leader.UnitName + " razed building: " + memoryTile.BuildingKeyword;
                                    notification.Picture = memoryTile.BuildingGraphics;
                                    Player player = FindPlayerByID(army.OwnerPlayerID);

                                    notification.ID = ++player.LocalNotificationID;
                                    player.Notifications.Add(notification);

                                    leader.Mission = null;
                                }
                            }
                            else
                            {
                                SurveyGameSquare(army);
                            }




                        }


                    }



                }

            }
        }
    }
    /// <summary>
    /// same as ProcessHeroActionPhase
    /// </summary>
    /// <param name="player"></param>
    void ProcessPlayerHeroActionPhase(Player player)
    {
        if (player.Defeated)
        {
            return;
        }
        ProcessPlayerHeroCapturePhase(player.PlayerID);
        ProcessPlayerHeroSurveyPhase(player.PlayerID);
        ProcessPlayerHeroCraftingPhase(player.PlayerID);
        ProcessPlayerHeroBuildingPhase(player.PlayerID);
        ProcessPlayerHeroRazePhase(player.PlayerID);
    }
    /// <summary>
    /// capturing progress decreases by 20% if square wasnt being captured this turn
    /// </summary>
    public void DecayCapturingProgress()
    {
        foreach (GameSquare gamesqr in Worldmap.GameSquares)
        {
            if (gamesqr.WasBeingCapturedThisTurn)
            {
                gamesqr.WasBeingCapturedThisTurn = false;
            }
            else
            {
                if (gamesqr.CapturingProgress != null)
                {
                    gamesqr.CapturingProgress.Amount = gamesqr.CapturingProgress.Amount + (-1 - (int)gamesqr.CapturingProgress.Amount/5);

                    if (gamesqr.CapturingProgress.Amount <= 0)
                    {
                        gamesqr.CapturingProgress = null;
                    }
                }
            }
        }
    }
    public void ProcessHeroActionPhase()
    {
        ProcessHeroCapture(); //hero capture, raze, build methods here
        ProcessHeroSurvey();
        ProcessHeroCraftingPhase();
        ProcessHeroBuilding();
        ProcessHeroRaze();
    }


    public bool CanBeCraftedWithLeftOverPoints(Entity smith, Recipe recipe,Item item)
    {
        double requiredAmount = 0;


        requiredAmount = recipe.ProductionCompleted;

        if (item != null)
        {
            requiredAmount -= item.Progress;
        }

        double providedAmount = 0;

        providedAmount = ItemGenerator.calculateProductionSpeed(recipe, smith) * smith.GetOverlandActionPoints();

        if (providedAmount >= requiredAmount)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void ExtraItemReductionPhasePlayer(Player player)
    {
        if (player.Defeated)
        {
            return;
        }
        List<ExtraItem> toRemove = new List<ExtraItem>();
        foreach (ExtraItem item in player.ExtraItems)
        {
            item.TurnsLeft--;
            if (item.TurnsLeft == 0)
            {
                toRemove.Add(item);
            }
        }
        foreach (ExtraItem item in toRemove)
        {
            player.ExtraItems.Remove(item);
        }
    }

    public void ExtraItemReductionPhase()
    {
        foreach (Player player in Players)
        {
            List<ExtraItem> toRemove = new List<ExtraItem>();
            foreach (ExtraItem item in player.ExtraItems)
            {
                item.TurnsLeft--;
                if (item.TurnsLeft == 0)
                {
                    toRemove.Add(item);
                }
            }
            foreach (ExtraItem item in toRemove)
            {
                player.ExtraItems.Remove(item);
            }
        }
    }

    public void CreateNotifcationsForExtraItemsPhase()
    {
        foreach (Player player in Players)
        {
            foreach (ExtraItem item in player.ExtraItems)
            {
                Notification notification = new Notification();
                string formatstring = "turns";
                if (item.TurnsLeft == 1)
                {
                    formatstring = "turn";
                }
                notification.HeaderText = "You have " + item.TurnsLeft + " " + formatstring + "  to claim your item";
                notification.ExpandedText = item.Message;
                //notification.ExpandedText = "You had no inventory space after item transafer";
                notification.Type = item.NotificationType;
                notification.PlayerID = player.PlayerID;
                notification.TargetID = item.ID;
                notification.Picture = item.Picture;
                notification.MapCoordinates = item.MapCoordinates;
                notification.ID = ++player.LocalNotificationID;
                player.Notifications.Add(notification);
            }
        }
    }

    public void CreateExtraItemsForCombat(string playerID, IEnumerable<Item> items, int turns, MapCoordinates mapCoordinates)
    {
        Player player = FindPlayerByID(playerID);
        ExtraItem extraItem = new ExtraItem(true);
        extraItem.PlayerID = playerID;
        foreach (Item item in items)
        {
            extraItem.AddToRecieve(item);
        }
   
        extraItem.TurnsLeft = turns;
        extraItem.ToRecieveLabelText = "Items looted: ";
        extraItem.Message = "You have obtained combat loot";
        extraItem.Picture = "Poneti/Skills/Assassin/Assassinskill_16";
        extraItem.NotificationType = Notification.NotificationType.TYPE_COMBAT_LOOT;
        extraItem.MapCoordinates = mapCoordinates;
        player.ExtraItems.Add(extraItem);
        //no notification as it will be created by next turn
        //Notification notification = new Notification();
        //string formatstring = "turns";
        //if (extraItem.TurnsLeft == 1)
        //{
        //    formatstring = "turn";
        //}
        //notification.HeaderText = "You have " + extraItem.TurnsLeft + " " + formatstring + "  to claim your item";
        //notification.Type = Notification.NotificationType.TYPE_COMBAT_LOOT;
        //notification.PlayerID = player.PlayerID;
        //notification.TargetID = extraItem.ID;
        //notification.UsePicture = true;
        //notification.Picture = extraItem.Picture;
        //player.Notifications.Add(notification);
    }

    public void CreateExtraItem(string playerID, IEnumerable<Item> items, int turns,string message, string labelText,bool createNotification)
    {
        
        Player player = FindPlayerByID(playerID);
        ExtraItem extraItem = new ExtraItem(true);
        extraItem.PlayerID = playerID;
        foreach (Item item in items)
        {
            extraItem.AddToRecieve(item);
            extraItem.Picture = item.GetPictureString();
        }
        extraItem.TurnsLeft = turns;
        extraItem.Message = message;
        extraItem.ToRecieveLabelText = labelText;
        //extraItem.Message = "You had no inventory space after item transafer";
        //extraItem.ToRecieveLabelText = "Take your item:";
       
        extraItem.NotificationType = Notification.NotificationType.TYPE_NOT_ENOUGH_SPACE_AFTER_ITEM_TRANSFER;
        player.ExtraItems.Add(extraItem);
        if (createNotification)
        {
            Notification notification = new Notification();
            string formatstring = "turns";
            if (extraItem.TurnsLeft == 1)
            {
                formatstring = "turn";
            }
            notification.HeaderText = "You have " + extraItem.TurnsLeft + " " + formatstring + "  to claim your item";
            notification.Type = Notification.NotificationType.TYPE_NOT_ENOUGH_SPACE_AFTER_ITEM_TRANSFER;
            notification.PlayerID = player.PlayerID;
            notification.TargetID = extraItem.ID;
            notification.Picture = extraItem.Picture;

            notification.ID = ++player.LocalNotificationID;
            player.Notifications.Add(notification);
        }
     

    }
    public void AttemptInstantCrafting(Entity smith, Recipe recipe, Item item)
    {
        bool debug = true;
        if (CanBeCraftedWithLeftOverPoints(smith,recipe,item))
        {
            double amount1 = recipe.ProductionCompleted;
            amount1 -= item.Progress;
          

            double toSubtractFromMovement = amount1 / (ItemGenerator.calculateProductionSpeed(recipe, smith) * smith.GetOverlandActionPoints());
            if (debug)
            {
                Debug.Log("subtracted amount: " + toSubtractFromMovement);
                Debug.Log(smith.CharacterTemplateKeyword + " movement pts before: " + smith.MovementRemaining);
            }

            ProcessHeroCrafting(smith,FindArmyByUnit(smith.UnitID),smith.FindCurrentOwnerID());





            smith.SubtractMovementByAction(toSubtractFromMovement);

            if (debug)
            {
             
                Debug.Log(smith.CharacterTemplateKeyword + " movement pts after: " + smith.MovementRemaining);
            }
            GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SHOW_RED_PLAYER_MESSAGE,smith.FindCurrentOwnerID(),"Smithing complete"));

        }
    }

    public void ProcessHeroCraftingPhase()
    {
        bool debug = true;
        foreach (Army army in armies)
        {
            Entity leader = FindUnitByUnitID(army.LeaderID);
            if (leader != null)
            {
                ProcessHeroCrafting(leader,army,""); //phase, so can use normal global ids

            }

        }
    }


    public void ProcessHeroCrafting(Entity leader, Army army, string playerid)
    {
        bool debug = true;
        if (leader.Mission != null)
        {
            if (leader.Mission.MissionName == Mission.mission_Craft)
            {
                Item itemToCraft = leader.BackPack.FindItemByID(leader.Mission.TargetID);
                Recipe recipe = GameEngine.Data.RecipeCollection.findByKeyword(itemToCraft.SourceRecipeKeyword);

                if (itemToCraft.Progress + ItemGenerator.calculateProductionSpeed(recipe, leader) * (leader.GetOverlandActionPoints() + 1) >= recipe.ProductionCompleted)
                {
                    itemToCraft.AddProductionProgress(ItemGenerator.calculateProductionSpeed(recipe, leader) * (leader.GetOverlandActionPoints() + 1), recipe, leader);
                    double toSubtract = recipe.ProductionCompleted / ItemGenerator.calculateProductionSpeed(recipe, leader) * (leader.GetOverlandActionPoints() + 1);

                    leader.SubtractMovementByAction(toSubtract);

                }
                else
                {
                    itemToCraft.AddProductionProgress(ItemGenerator.calculateProductionSpeed(recipe, leader) * leader.GetOverlandActionPoints(), recipe, leader);
                }


           
                if (debug)
                {
                    Debug.Log("progress: " + itemToCraft.Progress.ToString());
                }

                if (itemToCraft.Progress >= recipe.ProductionCompleted ||
                  itemToCraft.SmithingQuality >= ItemGenerator.maxQualityAchieved(recipe, recipe.QualityRange))
                {
                    Notification notification = new Notification();
         
                    notification.IsOverland = true;
                    notification.TargetID = leader.UnitID;
                    notification.HeaderText = "Recipe smithing complete: " + recipe.correctLanguageString();
                    notification.Type = Notification.NotificationType.TYPE_HERO_MISSION_CRAFTING_COMPLETE;
                    notification.Picture = "ButtonIcons/newCrafting";
                    notification.BgImageR = 100;
                    notification.BgImageG = 100;
                    notification.BgImageB = 100;
                    //notification.ExpandedText = leader.UnitName + " has gained " + recipe.TrainsSkillTrees + Environment.NewLine;
                    notification.ExpandedText = leader.UnitName + " has finished recipe";
                    
                    if (debug)
                    {
                        Debug.Log("crafting complete: " + recipe.Keyword);
                    }
                    EntityStat expBoost = leader.Stats.findStatByKeyword(StatCollection.LEARNINGBOOST);
                    //making notification elements for skill trees & training leader's skill trees
                    foreach (Stat expStat in recipe.TrainsSkillTrees)
                    {
                        if (debug)
                        {
                            Debug.Log("crafting gave exp: " + expStat.Keyword + " +" + expStat.Amount);
                        }
                        SkillTree skillTree = GameEngine.Data.SkillTreeCollection.findByKeyword(expStat.Keyword);
                        EntitySkillTreeLevel levelAquired = leader.checkSkillTreeLevel(expStat.Keyword);
                        int levelAttained = 0;
                        string youHaveLeveledUpText = "";
                        double amount = expStat.Amount * expBoost.Current;
                        if (levelAquired != null)
                        {
                            levelAttained = levelAquired.LevelAttained;
                        }

                        leader.TrainSkillTree(skillTree, amount);

                        //after training skill tree, checking if have leveled up, and if so, then change the string
                        levelAquired = leader.checkSkillTreeLevel(expStat.Keyword); //no need for null checks, as we create it
                        if (levelAttained != levelAquired.LevelAttained)
                        {
                            youHaveLeveledUpText = Environment.NewLine + "<color=#702963>Level Up</color>";
                        }

                        NotificationElement notificationElement = new NotificationElement();
                       
                        notificationElement.Content = skillTree.correctLanguageString() + " +" + amount + " exp " + youHaveLeveledUpText;
                        notificationElement.Picture = skillTree.Picture;
                        notificationElement.SkillTreeKeyword = expStat.Keyword;
                        notificationElement.BgImageSprite = "ButtonIcons/bg 4";
                        notificationElement.BgImageA = 255;
                        notification.NotificationElements.Add(notificationElement);
                    }

                    List<Item> craftedItems = Item.SmithingComplete(itemToCraft, leader, recipe.QualityRange,playerid);
                    foreach (var item in craftedItems)
                    {
                        ItemTemplate itemTemplate = GameEngine.Data.ItemTemplateCollection.findByKeyword(item.TemplateKeyword);

                        NotificationElement notificationElement = new NotificationElement();
                        notificationElement.ItemKeyword = itemTemplate.Keyword;
                        notificationElement.Content = itemTemplate.correctLanguageString();
                        notificationElement.Picture = itemTemplate.Picture;
                        notificationElement.BgImageSprite = "ButtonIcons/bg 4";
                        notificationElement.BgImageA = 255;
                        notification.NotificationElements.Add(notificationElement);
                 
                    }
                    //leader.BackPack.AddRangeItems(craftedItems);
                    if (recipe.SummoningTemplateKeyword != "")
                    {
                        SummoningTemplate summoningTemplate = GameEngine.Data.SummoningTemplateCollection.findByKeyword(recipe.SummoningTemplateKeyword);
                        if (debug)
                        {
                            Debug.Log("crafted item count: " + craftedItems.Count);

                        }
                        notification.HeaderText = "Entity summon complete: " + recipe.correctLanguageString();
                        notification.ExpandedText = leader.UnitName + " has summoned: " + Environment.NewLine;
                        foreach (Item item in craftedItems)
                        {
                            Debug.Log(item.TemplateKeyword + " has been crafted");
                            if (summoningTemplate.ImmediateHatch)
                            {
                                if (debug)
                                {
                                    Debug.Log("hatching entity ");
                                }

                                //ItemStat stat = item.GlobalStats.findTStatByKeyword(ItemTemplate.TYPE_HATCHABLE);
                                //if (stat == null)
                                //{
                                //    if (debug)
                                //    {
                                //        Debug.Log("summon produced item that is not a hatchable: " + item.TemplateKeyword);
                                //    }
                                //    ItemTemplate itemTemplate = GameEngine.Data.ItemTemplateCollection.findByKeyword(item.TemplateKeyword);
                                //    notification.ExpandedText += "crafted: "+ itemTemplate.correctLanguageString() + Environment.NewLine;
                                //    continue;

                                //}
                                List<string> entities = item.GetEntityKeywordFromSlots();
                                if (debug)
                                {
                                    Debug.Log("entity keyword count: " + entities.Count);
                                }
                                foreach (string entitykw in entities)
                                {
                                    if (debug)
                                    {
                                        Debug.Log("entity created: " + entitykw);
                                    }
                                    Entity newEntity = Entity.CreateTemplateChar(entitykw, GameEngine.random,army.OwnerPlayerID);
                                    notification.ExpandedText += newEntity.UnitName + Environment.NewLine;
                                    ApplySummonBonusesToEntity(newEntity, leader, recipe);
                                    army.Units.Add(newEntity);
                                }
                                notification.Type = Notification.NotificationType.TYPE_HERO_MISSION_SUMMON_COMPLETE;

                                leader.BackPack.RemoveItemByID(item.ID);
                            }
                            else
                            {
                                ItemTemplate itemTemplate = GameEngine.Data.ItemTemplateCollection.findByKeyword(item.TemplateKeyword);
                                notification.ExpandedText += "item: " + itemTemplate.correctLanguageString() + Environment.NewLine;
                            }
                        }
                        if (craftedItems.Count == 0)
                        {
                            notification.ExpandedText = leader.UnitName + " failed to summon";

                        }
                        Debug.Log("summon count " + craftedItems.Count);
                    }




                    Player player = FindPlayerByID(army.OwnerPlayerID);

                    notification.ID = ++player.LocalNotificationID;
                    player.Notifications.Add(notification);

                    leader.BackPack.RemoveItemByID(itemToCraft.ID);

                    Item nextItemInQueue = leader.BackPack.GetNextUnfinishedItem();
                  
                    if (nextItemInQueue != null)
                    {
                        leader.Mission.TargetID = nextItemInQueue.ID;
                        if (debug)
                        {
                            Debug.Log("smithing: nextItemInQueue not null, its " + nextItemInQueue.SourceRecipeKeyword);
                         }
                    }
                    else
                    {
                        leader.Mission = null;
                        if (debug)
                        {
                            Debug.Log("smithing: nextItemInQueue null, stopping crafting ");
                        }
                    }


                    //Debug.Log("item count: " + leader.BackPack.Count);
                    //leader.BackPack.RemoveItemByID(itemToCraft.ID); //commented this out, because we do mission check and will detect this very item
                    //foreach (var item in leader.BackPack)
                    //{
                    //    Debug.Log("item: " + item.TemplateKeyword);
                    //}
                    //Debug.Log("item count: " + leader.BackPack.Count);
                }
            }


        }


    }

    /// <summary>
    /// if not hatched immediately, send in the recipe from item.sourcerecipekeyword
    /// </summary>
    /// <param name="incEntity"></param>
    /// <param name="caster"></param>
    /// <param name="recipe"></param>
    public void ApplySummonBonusesToEntity(Entity incEntity, Entity caster, Recipe recipe)
    {
        Debug.Log("applying bonuses to entity: " + incEntity.CharacterTemplateKeyword);
        if (recipe.SummoningTemplateKeyword == "")
        {
            Debug.Log("attempted to apply summoning bonus with recipe that hasnt got one");
            return;
        }

        SummoningTemplate summoningTemplate = GameEngine.Data.SummoningTemplateCollection.findByKeyword(recipe.SummoningTemplateKeyword);

        foreach (SummoningBonus bonus in summoningTemplate.Bonuses)
        {
            double casterValue = 0;
            switch (bonus.TriggerType)
            {
                case SummoningBonus.TYPE_STAT:
                    TStat casterStat = caster.Stats.findStatByKeyword(bonus.ConditionStat.Keyword);
                    if (casterStat != null)
                    {
                        casterValue = casterStat.Current;
                    }
                    break;
                case SummoningBonus.TYPE_TRAIT:
                    string casterTrait = caster.GetTraitByKeyword(bonus.ConditionStat.Keyword);
                    if (casterTrait != null)
                    {
                        casterValue++;
                    }
                    break;
                case SummoningBonus.TYPE_SKILLTREE:
                    EntitySkillTreeLevel level = caster.checkSkillTreeLevel(bonus.ConditionStat.Keyword);
                    if (level != null)
                    {
                        casterValue = level.LevelAttained;
                    }
                    break;
                default:
                    break;
            }
            if (Item.IsConditionMet(casterValue,bonus.ConditionStat.Amount,bonus.Condition))
            {
                switch (bonus.RewardType)
                {
                    case SummoningBonus.TYPE_TRAIT:
                    case SummoningBonus.TYPE_STAT:
                        EntityStat newStat = caster.Stats.findStatByKeyword(bonus.RewardStat.Keyword);
                        if (newStat != null)
                        {
                            newStat.Current += bonus.RewardStat.Amount;
                        }
                        else
                        {
                            newStat = new EntityStat(bonus.RewardStat.Keyword, bonus.RewardStat.Amount, bonus.RewardStat.Amount, bonus.RewardStat.Amount);
                            incEntity.Stats.Add(newStat);
                        }
                        break;
                    //case SummoningBonus.TYPE_TRAIT:
                    //    string casterTrait = caster.GetTraitByKeyword(bonus.RewardStat.Keyword);
                    //    if (casterTrait == null)
                    //    {
                    //        incEntity.TraitKeywords.Add(bonus.RewardStat.Keyword);
                    //    }
                    //    break;
                    case SummoningBonus.TYPE_SKILLTREE:
                        EntitySkillTreeLevel level = incEntity.checkSkillTreeLevel(bonus.RewardStat.Keyword);
                        if (level != null)
                        {
                            level.Exp_gained_for_tree += bonus.RewardStat.Amount;
                        }
                        else
                        {
                            incEntity.addSkillTree(bonus.RewardStat.Keyword);
                            level = incEntity.checkSkillTreeLevel(bonus.RewardStat.Keyword);
                            level.Exp_gained_for_tree += bonus.RewardStat.Amount;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        if (summoningTemplate.UpkeepType != "")
        {
            incEntity.UpKeep = new UpKeep();
            incEntity.UpKeep.UpkeepType = summoningTemplate.UpkeepType;
            foreach (var item in summoningTemplate.UpkeepCosts)
            {
                incEntity.UpKeep.Costs.Add(new UpkeepCost(item));
            }
            
        }
        
        
    }

    public void ProcessHeroRaze()
    {
        foreach (Army army in armies)
        {
            Entity leader = FindUnitByUnitID(army.LeaderID);
            if (leader != null)
            {
                if (leader.Mission != null)
                {
                    if (leader.Mission.MissionName == Mission.mission_Raze)
                    {
                        MemoryTile memoryTile = FindMemoryTileByCoordinates(army.OwnerPlayerID,army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);

                        if (memoryTile.IsSurveyed)
                        {
                            if (RazeBuilding(army, leader.Mission.TargetID))
                            {
                                Notification notification = new Notification();
                        
                                notification.IsOverland = true;                               
                                notification.TargetID = leader.UnitID;
                                notification.HeaderText = leader.UnitName + " razed building: " + memoryTile.BuildingKeyword;
                                notification.Type = Notification.NotificationType.TYPE_HERO_MISSION_RAZE_COMPLETE;
                                notification.ExpandedText = leader.UnitName + " razed building: " + memoryTile.BuildingKeyword;
                                notification.Picture = memoryTile.BuildingGraphics;
                                Player player = FindPlayerByID(army.OwnerPlayerID);

                                notification.ID = ++player.LocalNotificationID;
                                player.Notifications.Add(notification);

                                leader.Mission = null;
                            }
                        }
                        else
                        {
                            SurveyGameSquare(army);
                        }

                        
                        
                 
                    }


                }



            }

        }
    }


    public void ProcessHeroBuilding()
    {
        foreach (Army army in armies)
        {
            Entity leader = FindUnitByUnitID(army.LeaderID);
            if (leader != null)
            {
                if (leader.Mission != null)
                {
                    if (leader.Mission.MissionName == Mission.mission_Build)
                    {
                        StartingBuildingProcess(army.OwnerPlayerID, leader.Mission.TargetString, army.ArmyID);
                        //ProcessBuildingPlan(army.ArmyID);
                       // BuildBuilding(army.ArmyID);
                        //MemoryTile memoryTile = FindMemoryTileByCoordinates(army.OwnerPlayerID, army.WorldMapPositionX, army.WorldMapPositionY);
                        //GameSquare gameSquare = Worldmap.FindMapSquareByCordinates(army.WorldMapPositionX, army.WorldMapPositionY);
                        //memoryTile.IsSurveyed = true;
                        //memoryTile.TerrainTemplateKW = gameSquare.TerrainKeyword;
                        //leader.Mission = null;
                    }


                }



            }

        }
    }
    /// <summary>
    /// returns true if succeeded in surveying, false if not complete
    /// </summary>
    /// <param name="army"></param>
    /// <returns></returns>
    public bool SurveyGameSquare(Army army)
    {
  
        MemoryTile memoryTile = FindMemoryTileByCoordinates(army.OwnerPlayerID, army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);
        GameSquare gameSquare = Worldmap.FindMapSquareByCordinates(army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);
        if (memoryTile.SurveyProgress + army.GetOverlandActionModifier() >= 1)
        {
            memoryTile.SurveyProgress += army.GetOverlandActionModifier();

            double toSubtract = 1 / (memoryTile.SurveyProgress + army.GetOverlandActionModifier());

            army.SubtractMovementFromActionPoints(toSubtract);
        }
        else
        {
            memoryTile.SurveyProgress = memoryTile.SurveyProgress + army.GetOverlandActionModifier() + 1;
            double toSubtract = 1 / (memoryTile.SurveyProgress + army.GetOverlandActionModifier() + 1);

            army.SubtractMovementFromActionPoints(toSubtract);

        }
 
        if (memoryTile.SurveyProgress >= 1)
        {
            memoryTile.IsSurveyed = true;
            memoryTile.TerrainTemplateKW = gameSquare.TerrainKeyword;

            Entity leader = FindUnitByUnitID(army.LeaderID);
            Notification notification = new Notification();
       
            notification.IsOverland = true;
            notification.TargetID = leader.UnitID;
            notification.Type = Notification.NotificationType.TYPE_HERO_MISSION_SURVEY_COMPLETE;
            notification.HeaderText = leader.UnitName + " has finished surveyeing ";
            notification.ExpandedText = leader.UnitName + " No buildings were found ";
            GameSquare gamesqr = Worldmap.FindMapSquareByCordinates(army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);
            if (gamesqr.building != null)
            {
                notification.ExpandedText = "Building was found ";
                NotificationElement notificationElement = new NotificationElement();
                notificationElement.BuildingID = gamesqr.building.ID;
                notificationElement.Picture = gamesqr.building.PortraitGraphics;
                notificationElement.Content = gamesqr.building.TemplateKeyword;
               
                notification.NotificationElements.Add(notificationElement);
            }
            Player player = FindPlayerByID(army.OwnerPlayerID);
            notification.ID = ++player.LocalNotificationID;
            player.Notifications.Add(notification);
            //if survey came from the building, dont cancel build project with this nulling
            if (leader.Mission.MissionName != Mission.mission_Build)
            {
                leader.Mission = null;
            }
          

            return true;
        }
        else
        {
            return false;
        }

   

        
    }


    public void ProcessHeroSurvey()
    {
        foreach (Army army in armies)
        {
            Entity leader = FindUnitByUnitID(army.LeaderID);
            if (leader != null)
            {
                if (leader.Mission != null)
                {
                    if (leader.Mission.MissionName == Mission.mission_Survey)
                    {
                        if (SurveyGameSquare(army))
                        {
                           

                            leader.Mission = null;
                        }
                      

 
                    }

                   
                }
                


            }

        }
    }

    public void ProcessHeroCapture()
    {
        foreach (Army army in armies)
        {
            Entity leader = FindUnitByUnitID(army.LeaderID);
            if (leader != null)
            {
                if (leader.Mission != null)
                {
                    if (leader.Mission.MissionName == Mission.mission_Capture)
                    {
                        CaptureGameSquare(army);
                    }


                }



            }

             

        }
        // OurLog.Print("Army count: " + armies.Count);
    }


    public void CaptureGameSquare(Army army)
    {
        bool debugLog = false;


        if (army != null)
        {
            if (debugLog) {
                OurLog.Print("Capturing starts");
            }

           
            //TODO: check if already own this with player and hero, do not execute, tooltip should say you already own this, disable capture button
            GameSquare gameSquareToBeCaptured = GameEngine.Map.FindMapSquareByCordinates(army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);
            //TODO: prevent capture in combat area
            //double maxmove = army.CurrentArmyMovementPoints();
            gameSquareToBeCaptured.WasBeingCapturedThisTurn = true;
            //if (maxmove == 0)
            //{
            //    maxmove = 1;
            //}
            //double actionpointsLeft = army.MovementPoints / maxmove;
            double capturePower = army.GetArmyCapPointTotal() * army.GetOverlandActionModifier();

            if (debugLog)
            {
                OurLog.Print("Capture power: " + capturePower);
                //OurLog.Print("action points: " + actionpointsLeft);
            }
           

            if (gameSquareToBeCaptured.building.OwnerPlayerID != "") //gamesquare can be captured
            {
                if (gameSquareToBeCaptured.building.OwnerPlayerID == army.OwnerPlayerID && gameSquareToBeCaptured.building.OwnerHeroID == army.LeaderID)
                {
                    //todo: message to player that hero already owns the square
                    Debug.LogError("square is already owner by this player and this hero");
                    if (debugLog)
                    {
                        OurLog.Print("square is already owner by this player and this hero");
                    }
                }
                else
                {
                    Stat oldProgress = null;
                    if (gameSquareToBeCaptured.CapturingProgress != null)
                    {

                        if (debugLog)
                        {
                            OurLog.Print("This gamesquare was in progress of capture");
                        }
                       
                        oldProgress = gameSquareToBeCaptured.CapturingProgress;

                        int previousCapturingHeroID = Int32.Parse(oldProgress.Keyword);
                        Entity previousCapturingHero = FindUnitByUnitID(previousCapturingHeroID);
                        string previousCapturingPlayerID = previousCapturingHero.FindCurrentOwnerID();
                        if (previousCapturingPlayerID != army.OwnerPlayerID)
                        {
                            if (debugLog)
                            {
                                OurLog.Print("This was being captured by another player");
                            }
                           
                            oldProgress = null;
                            gameSquareToBeCaptured.CapturingProgress = null;
                        }

                    }

                    if (oldProgress == null)
                    {
                        if (debugLog)
                        {
                            OurLog.Print("Creating new process");
                        }
                       
                        gameSquareToBeCaptured.CapturingProgress = new Stat();
                        gameSquareToBeCaptured.CapturingProgress.Keyword = army.LeaderID.ToString();

                    }
                    double requiredAmount = gameSquareToBeCaptured.CapturingCost - gameSquareToBeCaptured.CapturingProgress.Amount;
                    double capturePowerLeft = capturePower - requiredAmount;



                    Player previousOwner = FindPlayerByID(gameSquareToBeCaptured.building.OwnerPlayerID);
                    Player player = FindPlayerByID(army.OwnerPlayerID);
                    //if (player.PlayerID != previousOwner.PlayerID)
                    //{
                    //    if (gameSquareToBeCaptured.building.GarissonArmyID != -1)
                    //    {
                    //        Army garrison = FindOverlandArmy(gameSquareToBeCaptured.building.GarissonArmyID);
                    //        if (garrison != null)
                    //        {
                    //            if (!garrison.IsInHostileList(army.ArmyID,BattleParticipant.MODE_ARMY))
                    //            {
                    //                garrison.ArmiesYouIntentAttackIds.Add(new HostilityTarget(BattleParticipant.MODE_ARMY,army.ArmyID));
                    //            }
                    //        }
                    //    }
                    //    army.get
                       
                    //}
                    if (capturePowerLeft >= 0)
                    {
                        //capturing is complete
                        if (debugLog)
                        {
                            OurLog.Print("Captured: " + gameSquareToBeCaptured.TerrainKeyword);
                        }
                        Entity leader = FindUnitByUnitID(army.LeaderID);


                        Notification notification = new Notification();
                   
                        notification.IsOverland = true;
                        notification.TargetID = leader.UnitID;
                        notification.Type = Notification.NotificationType.TYPE_HERO_MISSION_CAPTURE_COMPLETE;
                        notification.HeaderText = leader.UnitName + " has captured building: " + gameSquareToBeCaptured.building.TemplateKeyword;                        
                        notification.ExpandedText = leader.UnitName + " has captured building: " + gameSquareToBeCaptured.building.TemplateKeyword;
                        notification.Picture = gameSquareToBeCaptured.building.Graphics;
                        notification.ID = ++player.LocalNotificationID;
                        player.Notifications.Add(notification);

                        //notify the previous owner of building
                       
                        notification = new Notification();
                        notification.BgImageR = 255;
                        notification.BgImageG = 2;
                        notification.BgImageB = 2;
                        notification.IsOverland = true;
                        notification.TargetID = gameSquareToBeCaptured.ID;
                        notification.Type = Notification.NotificationType.TYPE_WARINING_BUILDING_LOST;
                        notification.Picture = gameSquareToBeCaptured.building.Graphics;
                        notification.HeaderText = "Building lost to enemy";
                        notification.ExpandedText = gameSquareToBeCaptured.building.TemplateKeyword + " has been captured by " + player.PlayerID;

                        notification.ID = ++previousOwner.LocalNotificationID;
                        previousOwner.Notifications.Add(notification);

                        leader.Mission = null;
                        gameSquareToBeCaptured.CapturingProgress = null;
                        TransferBuildingOwnership( army.LeaderID, army.OwnerPlayerID, gameSquareToBeCaptured);
                        double actionPointsAfterCapture = capturePowerLeft / army.GetArmyCapPointTotal();
                        double suggestedMovementPoints = army.CurrentArmyMovementPoints() * actionPointsAfterCapture;
                        army.MovementPoints = suggestedMovementPoints;
                    }
                    else
                    {
                        if (debugLog)
                        {
                            OurLog.Print("capturing is in progress");
                        }
                        
                        army.MovementPoints = 0;
                        gameSquareToBeCaptured.CapturingProgress.Amount += capturePower;
                        Entity leader = FindUnitByUnitID(army.LeaderID);
                        leader.SetMission(Mission.mission_Capture, army.LeaderID,gameSquareToBeCaptured.ID,0);

                        //notify the previous owner of building
                        Notification notification = new Notification();
                        notification.BgImageR = 255;
                        notification.BgImageG = 2;
                        notification.BgImageB = 2;
                        notification.IsOverland = true;
                        notification.TargetID = gameSquareToBeCaptured.ID;
                        notification.Type = Notification.NotificationType.TYPE_WARINING_BUILDING_IS_BEING_CAPTURED;
                        notification.HeaderText = "Your building is being captured";
                        notification.ExpandedText = gameSquareToBeCaptured.building.TemplateKeyword + " is being captured by " + player.PlayerID + " with hero " + leader.UnitName;
                        notification.Picture = gameSquareToBeCaptured.building.Graphics;
                        //notification.Picture = "ButtonIcons/buildCaptureTEMPORARY";
                        notification.ID = ++previousOwner.LocalNotificationID;
                        previousOwner.Notifications.Add(notification);

                    }

                }//end else
            }
            //  gameSquareToBeCaptured.

        }
       
    }


    public void RemoveUnhappyUnits() //temporary solution
    {
       
        List<Army> armiesToKeep = new List<Army>();
        List<Army> armiesToDisband = new List<Army>();
 
        Army heroesToBeSentBackToPool = new Army(-1,null);

        Debug.Log("before cloning");
 
        foreach (Army army in armies)
        {
            Army unitsWeDecidedToKeep = army.ReturnDeepClone();
         
            unitsWeDecidedToKeep.Units.Clear();
            Army unitsToKill = new Army(-1,null);
            

            bool keepArmy = true;
            GameSquare gamesquare = Worldmap.FindMapSquareByCordinates(army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);
            Notification notification = new Notification();
 
            notification.IsOverland = true;
            notification.TargetID = gamesquare.ID;
            switch (army.Location.Mode)
            {
                case Location.MODE_BUILDING_GARRISON:
                    notification.HeaderText = "garisson units are starving! ";
                    break;
                default:
                    notification.HeaderText = " units are starving! ";
                    break;
            }
           
            notification.Type = Notification.NotificationType.TYPE_ENTITY_STARVE;
            notification.ExpandedText += " units starved: ";
            notification.BgImageR = 255;
            notification.BgImageG = 2;
            notification.BgImageB = 2;
            notification.Picture = "other/skull2";
            bool sendNotification = false;
            bool sendHeroNotification = false;

            Notification notification2 = new Notification();

            notification2.IsOverland = true;
            notification2.TargetID = gamesquare.ID;
            notification2.HeaderText = "Heroes have left";
            notification2.Type = Notification.NotificationType.TYPE_HERO_LEAVE;
            notification2.BgImageR = 255;
            notification2.BgImageG = 2;
            notification2.BgImageB = 2;
            notification2.Picture = "Poneti/Skills/Misc/57_run";
            notification2.ExpandedText = "heroes left: ";

            foreach (Entity unit in army.Units)
            {
                string selectionMode = "Keep unit";
                //bool keepUnit = true;
                //bool kill = false;
                //bool sendBack = false;

                // We check if leader, if yes and unhappy, army will be disbanded. We want to send all other units back 
                // army will be added into armiesToDisband, set keepArmy = false; 

                if (unit.IsHungry)
                {
                    if (!unit.IsHeroFlag)
                    {
                        selectionMode = "Kill";
                        //Debug.Log("killing unit: " + unit.UnitName + " " + unit.UnitID);
                    }
                

                }
                int heroAttitudeShow = unit.FindPretendAttitudeFromCurrentEmployer();

                if (heroAttitudeShow <= 0)
                {

                    if (army.LeaderID == unit.UnitID)
                    {
                        keepArmy = false;
                    }
                    if (unit.IsHeroFlag)
                    {
                        selectionMode = "Send back";
                    }
                    else
                    {
                        selectionMode = "Kill";
                    }
                 


                }
                else
                {
                    //we keep the hero
                }
                // If Unit, and cant eat, will be sent to disband


                // Default unit will be added to unitsWeDecidedToKeep
                // Default army will be addded to armiesToKeep
                switch (selectionMode)
                {
                    default:
                        OurLog.Print("scenario.removeunhappyheroes: selectionMode error " + selectionMode);
                        break;
                    case "Kill":
                        unitsToKill.Units.Add(unit);
                        //Debug.Log("unit dies: " + unit.UnitName + " " + unit.UnitID);

                        sendNotification = true;
                        NotificationElement notificationElement = new NotificationElement();
                        //notificationElement.AdditionalToolTipContent = "Click to show on map";
                     
                        notificationElement.EntityKeyword = unit.CharacterTemplateKeyword;
                        notificationElement.Content = unit.CharacterTemplateKeyword;
                        notificationElement.Picture = unit.GetPicture();
                        if (army.Location.WorldMapCoordinates != null)
                        {
                            notificationElement.AdditionalToolTipContent = "Click to show on map";
                            notificationElement.IsClickable = true;
                            notificationElement.XCord = army.Location.WorldMapCoordinates.XCoordinate;
                            notificationElement.YCord = army.Location.WorldMapCoordinates.YCoordinate;
                        }
                
                        if (unit.MoodChange != null)
                        {
                            foreach (string info in unit.MoodChange.UpkeepInfo)
                            {
                                notificationElement.AdditionalToolTipContent += Environment.NewLine + info;
                            }
                        }
                        else
                        {
                            //foreach (UpkeepCost info in unit.UpKeep.Costs)
                            //{
                            //    notificationElement.AdditionalToolTipContent += Environment.NewLine + info.TemplateOdd.GetUpkeepFormatInformation();
                            //}
                        }
                       
                        //notificationElement.AdditionalToolTipContent += "\n" + unit.UpKeep.UpkeepType;
                        notificationElement.Picture = unit.GetPicture();
                        notification.NotificationElements.Add(notificationElement);

                        break;
                    case "Send back":
                        heroesToBeSentBackToPool.Units.Add(unit);

                        sendHeroNotification = true;
                        //seperate from units because heroes are important and should be grouped together into 1 notification
                        GameSquare gamesquare2 = Worldmap.FindMapSquareByCordinates(army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);

                        //if (unit.UpKeep == null)
                        //{
                        //    Debug.LogError(unit.UnitName + " null upkeep  ");
                        //}
                        //else
                        //{
                        //    Debug.LogError(unit.UnitName + " not null upkeep " + unit.UpKeep.UpkeepType);
                        //}
                        NotificationElement notificationElement2 = new NotificationElement();
                        notificationElement2.EntityKeyword = unit.CharacterTemplateKeyword;
                        notificationElement2.Content = unit.CharacterTemplateKeyword;
                        notificationElement2.Picture = unit.GetPicture();
                        notificationElement2.AdditionalToolTipContent = unit.CharacterTemplateKeyword + "'s upkeep("+unit.UpKeep.UpkeepType+")";
                        if (unit.MoodChange != null)
                        {
                            foreach (string info in unit.MoodChange.UpkeepInfo)
                            {
                                notificationElement2.AdditionalToolTipContent += Environment.NewLine + info;
                            }
                        }
                        else
                        {
                            foreach (UpkeepCost info in unit.UpKeep.Costs)
                            {
                                notificationElement2.AdditionalToolTipContent += Environment.NewLine + info.TemplateOdd.GetUpkeepFormatInformation();
                            }
                        }
                        notificationElement2.Picture = "";
                        notification2.NotificationElements.Add(notificationElement2);



                        Debug.Log("hero leaves: " + unit.CharacterTemplateKeyword + " " + unit.UnitID);
                        break;
                    case "Keep unit":
                        unitsWeDecidedToKeep.Units.Add(unit);
                        break;
                }
                
 
            }

            if (sendNotification)
            {
                Player player = FindPlayerByID(army.OwnerPlayerID);
                notification.ID = ++player.LocalNotificationID;
                player.Notifications.Add(notification);
            }
            if (sendHeroNotification)
            {
               
                Player player2 = FindPlayerByID(army.OwnerPlayerID);
                notification2.ID = ++player2.LocalNotificationID;
                player2.Notifications.Add(notification2);
            }
      

            if (keepArmy)
            {
                if (unitsWeDecidedToKeep.Units.Count == 0)
                {
                    //Debug.LogError("not keeping an empty army");
                    Building building = FindBuildingByGarissonID(unitsWeDecidedToKeep.ArmyID);
                    if (building != null)
                    {
                        building.GarissonArmyID = -1;
                    }
                }
                else
                {
                    if (!unitsWeDecidedToKeep.HasUnit(unitsWeDecidedToKeep.LeaderID))
                    {
                        unitsWeDecidedToKeep.AssignRandomUnitAsLeader(GameEngine.random);
                    }
                    armiesToKeep.Add(unitsWeDecidedToKeep);
                }
              
            }
            else {

                armiesToDisband.Add(unitsWeDecidedToKeep);
            }
        }
      
        armies = armiesToKeep;
        
    }

    public BuildingRequirements CheckSingleBuildingRequirements(string buildingkw, int buildersID, GameSquare gameSquare)
    {
        BuildingRequirements buildingRequirements = new BuildingRequirements();

        BuildingTemplate buildingTemplate = GameEngine.Data.BuildingTemplateCollection.findByKeyword(buildingkw);
        Entity leader = FindUnitByUnitID(buildersID);
        Player plr = FindPlayerByID(leader.FindCurrentOwnerID());

        buildingRequirements.matchesMaterials = ItemGenerator.GetBuildingMaterialLegalStatuses(buildingTemplate.Ingredients,leader,plr);

        //GameSquare gameSquare = Worldmap.FindMapSquareByCordinates(xCord, yCord);
        if (gameSquare.building != null)
        {
            if (gameSquare.building.OwnerPlayerID == plr.PlayerID && (gameSquare.building.TemplateKeyword == buildingkw || gameSquare.building.UnfinishedBuildingTemplateKeyword == buildingkw))
            {
                if (gameSquare.building.Durability.Current < gameSquare.building.Durability.Original)
                {
                    buildingRequirements.isInNeedForRepair = true; //repair needed
                }
                else
                {
                    buildingRequirements.isCompleted = true; //building complete
                }

            }
        }
    


        string upgradeStatus = "no upgrade required";
        if (buildingTemplate.Types.Contains(BuildingTemplate.TYPE_UPGRADE))
        {
            upgradeStatus = "upgrade required";
        }

        if (upgradeStatus == "upgrade required")
        {
            if (gameSquare.building != null)
            {
                BuildingTemplate existingBuilding = GameEngine.Data.BuildingTemplateCollection.findByKeyword(gameSquare.building.TemplateKeyword);
                if (existingBuilding.UpgradeToBuildingTemplateKeywords.Contains(buildingkw))
                {
                    upgradeStatus = "upgrade matched";
                }
                else
                {
                    upgradeStatus = "upgrade not matched";
                }

            }
            else
            {
                upgradeStatus = "upgrade not matched";
            }

        }

        if (upgradeStatus == "upgrade matched")
        {
            BuildingTemplate g = GameEngine.Data.BuildingTemplateCollection.findByKeyword(gameSquare.building.TemplateKeyword);
            buildingRequirements.matchesUpgrade = new BuildingRequirement(-1,-1,true,gameSquare.building.Graphics,g.correctLanguageString());
        }
        else if(upgradeStatus == "upgrade not matched")
        {
            List<BuildingTemplate> requiredBuildingList = GameEngine.Data.BuildingTemplateCollection.GetBuildingTemplatesToUpgradeFrom(buildingkw);
            foreach (BuildingTemplate buildingtemp in requiredBuildingList)
            {
                buildingRequirements.notMatchesUpgrades.Add(new BuildingRequirement(-1,-1,false,buildingtemp.PortraitGraphics,buildingtemp.correctLanguageString()));
            }
        }


        List<List<Stat>> upgradeResources = AreResourcesValid(buildingkw, gameSquare.X_cord, gameSquare.Y_cord, 1);
        foreach (Stat matchedResource in upgradeResources[0])
        {
            ResourceTemplate resourceTemplate = GameEngine.Data.ResourceTemplateCollection.findByKeyword(matchedResource.Keyword);
            buildingRequirements.matchedResources.Add(new BuildingRequirement((int)matchedResource.Amount,(int)matchedResource.Amount,true, resourceTemplate.Graphics, "resource: " + matchedResource.Keyword));
        }

        foreach (Stat missedResource in upgradeResources[1])
        {
            Stat reqResourceStat = buildingTemplate.RequiredResourceForFunctioning.findStatByKeyword(missedResource.Keyword);
            ResourceTemplate resourceTemplate = GameEngine.Data.ResourceTemplateCollection.findByKeyword(missedResource.Keyword);
            buildingRequirements.matchedResources.Add(new BuildingRequirement((int)reqResourceStat.Amount, (int)missedResource.Amount, true, resourceTemplate.Graphics,"resource: "+ missedResource.Keyword));
        }


        return buildingRequirements;
    }
    //
    /// <summary>
    /// returns priority lists of building templates and if they are interactable(if amount == 1, then not interactable) of entity(for UI)
    /// recent change: removed coordinates, sending in gamesquare instead
    /// </summary>
    /// <param name="buildingkw"></param>
    /// <param name="buildersID"></param>
    /// <param name="xCord"></param>
    /// <param name="yCord"></param>
    /// <returns></returns>
    public List<List<Stat>> GetSortedBuildingTemplateKeywords(List<string> buildingkw,int buildersID, GameSquare gameSquare)
    {
        List<List<Stat>> answer = new List<List<Stat>>();
 
        List<Stat> repair = new List<Stat>(); //not full     
        List<Stat> upgradesWithSufficientMaterialsAndResourcesAndBuilding = new List<Stat>();
        List<Stat> buildingsWithSufficientMaterialsAndResources = new List<Stat>();
        List<Stat> upgradesWithSufficientBuilding = new List<Stat>();
        List<Stat> upgradesWithSufficientMaterialsAndBuilding = new List<Stat>();
        List<Stat> upgradesWithSufficientResourcesAndBuilding = new List<Stat>();
        List<Stat> buildingsWithSufficientMaterials = new List<Stat>();
        List<Stat> buildingsWithSufficientResources = new List<Stat>();
        List<Stat> upgradesWithSufficientMaterialsAndResources = new List<Stat>();
        List<Stat> upgradesWithSufficientMaterials = new List<Stat>();
        List<Stat> upgradesWithSufficientResources = new List<Stat>();
        List<Stat> completed = new List<Stat>(); //already built
        List<Stat> insufficientUpgrades = new List<Stat>();
        List<Stat> insufficientBuildings = new List<Stat>();
       // GameSquare gameSquare = Worldmap.FindMapSquareByCordinates(xCord,yCord);
        #region logic
        foreach (string templateKW in buildingkw)
        {
            BuildingTemplate buildingTemplate = GameEngine.Data.BuildingTemplateCollection.findByKeyword(templateKW);
            Entity leader = FindUnitByUnitID(buildersID);
            Player plr = FindPlayerByID(leader.FindCurrentOwnerID());
            //exclusive block for repair/complete
            if (gameSquare.building != null)
            {
                if (gameSquare.building.OwnerPlayerID == plr.PlayerID && (gameSquare.building.TemplateKeyword == templateKW || gameSquare.building.UnfinishedBuildingTemplateKeyword == templateKW))
                {
                    if (gameSquare.building.Durability.Current < gameSquare.building.Durability.Original)
                    {
                        repair.Add(new Stat(templateKW, 0)); //repair needed
                    }
                    else
                    {
                        completed.Add(new Stat(templateKW, 1)); //building complete
                    }
                    continue;
                }
            }
    

            //is matching upgrade
            //if (buildingTemplate.UpgradeToBuildingTemplateKeywords.Contains(templateKW))
            if (buildingTemplate.Types.Contains(BuildingTemplate.TYPE_UPGRADE))
            {
               // Debug.Log("checking building: " + templateKW);
                bool upgradeSufficient = true;
                bool upgradeMaterials = true;
                bool upgradeResource = true;
                bool upgradeBuilding = true;
                if (!ItemGenerator.isRecipeMaterialLegal(buildingTemplate.Ingredients, leader, plr))
                {
                    upgradeMaterials = false;
                    upgradeSufficient = false;
                }
                List<List<Stat>> upgradeResources = AreResourcesValid(templateKW,gameSquare.X_cord,gameSquare.Y_cord,1);
                if (upgradeResources[1].Count > 1)
                {
                    upgradeResource = false;
                    upgradeSufficient = false;
                }

                if (gameSquare.building != null)
                {
                    BuildingTemplate existingBuilding = GameEngine.Data.BuildingTemplateCollection.findByKeyword(gameSquare.building.TemplateKeyword);
                    if (existingBuilding.UpgradeToBuildingTemplateKeywords.Contains(templateKW))
                    {
                        upgradeBuilding = true;
                    }
                    else
                    {
                        upgradeBuilding = false;
                        upgradeSufficient = false;
                    }
                }
                else
                {
                    upgradeBuilding = false;
                    upgradeSufficient = false;
                }



                if (upgradeSufficient)
                {
                    upgradesWithSufficientMaterialsAndResourcesAndBuilding.Add(new Stat(templateKW,0));
                 //   Debug.Log("checking building: " + templateKW);
                }
                if (!upgradeBuilding && upgradeMaterials && upgradeResource)
                {
                    upgradesWithSufficientMaterialsAndResources.Add(new Stat(templateKW, 1));
  
                }
                if (!upgradeBuilding && upgradeMaterials && !upgradeResource)
                {
                    upgradesWithSufficientMaterials.Add(new Stat(templateKW, 1));
                //    Debug.Log("checking building: " + templateKW);
                }
                if (!upgradeBuilding && !upgradeMaterials && upgradeResource)
                {
                    upgradesWithSufficientResources.Add(new Stat(templateKW, 1));
                //    Debug.Log("checking building: " + templateKW);
                }
                if (!upgradeMaterials && !upgradeResource && upgradeBuilding)
                {
                    upgradesWithSufficientBuilding.Add(new Stat(templateKW, 1));
                 //   Debug.Log("checking building: " + templateKW);
                }
                if (!upgradeMaterials && upgradeResource && upgradeBuilding)
                {
                    upgradesWithSufficientResourcesAndBuilding.Add(new Stat(templateKW, 1));
                //    Debug.Log("checking building: " + templateKW);
                }
                if (upgradeMaterials && !upgradeResource && upgradeBuilding)
                {
                    upgradesWithSufficientMaterialsAndBuilding.Add(new Stat(templateKW, 0));
                 //   Debug.Log("checking building: " + templateKW);
                }
                if (!upgradeMaterials && !upgradeResource && !upgradeBuilding)
                {
                    insufficientUpgrades.Add(new Stat(templateKW, 1));
                 //   Debug.Log("checking building: " + templateKW);
                }

                continue;
            }


            //not upgrade building
            bool sufficient = true;
            bool material = true;
            bool resource = true;

            if (!ItemGenerator.isRecipeMaterialLegal(buildingTemplate.Ingredients, leader, plr))
            {
                material = false;
                sufficient = false;
            }
            List<List<Stat>> resources = AreResourcesValid(templateKW, gameSquare.X_cord, gameSquare.Y_cord, 1);
            if (resources[1].Count > 1)
            {
                resource = false;
                sufficient = false;
            }



            if (sufficient)
            {
                buildingsWithSufficientMaterialsAndResources.Add(new Stat(templateKW, 0));
            }
            if (material && !resource)
            {
                buildingsWithSufficientMaterials.Add(new Stat(templateKW, 0));
            }
            if (!material && resource)
            {
                buildingsWithSufficientResources.Add(new Stat(templateKW, 1));
            }
            if (!material && !resource)
            {
                insufficientBuildings.Add(new Stat(templateKW, 1));
            }


        }
        #endregion

        answer.Add(repair);
        answer.Add(upgradesWithSufficientMaterialsAndResourcesAndBuilding);
        answer.Add(buildingsWithSufficientMaterialsAndResources);
        answer.Add(upgradesWithSufficientResourcesAndBuilding);
        answer.Add(upgradesWithSufficientMaterialsAndBuilding);
        answer.Add(upgradesWithSufficientBuilding);
        answer.Add(buildingsWithSufficientResources);
        answer.Add(buildingsWithSufficientMaterials);
        answer.Add(upgradesWithSufficientMaterialsAndResources);
        answer.Add(upgradesWithSufficientResources);
        answer.Add(upgradesWithSufficientMaterials);       
        answer.Add(completed);
        answer.Add(insufficientBuildings);
        answer.Add(insufficientUpgrades);

        return answer;
    }


    /// <summary>
    /// returns the lists of matching and missing resources(for UI)
    /// </summary>
    /// <param name="buildingTemplateKW"></param>
    /// <param name="xCord"></param>
    /// <param name="yCord"></param>
    /// <param name="radius"></param>
    /// <returns></returns>
    public List<List<Stat>> AreResourcesValid(string buildingTemplateKW, int xCord, int yCord, int radius)
    {
        OurStatList requiredResources = GameEngine.Data.BuildingTemplateCollection.findByKeyword(buildingTemplateKW).RequiredResourceForFunctioning;
        OurStatList metRequierements = new OurStatList();
        List<List<Stat>> answer = new List<List<Stat>>();
        List<Stat> matched = new List<Stat>();
        List<Stat> missed = new List<Stat>();
        MapSquare source = Worldmap.FindMapSquareByCordinates(xCord, yCord);
        List<OurMapSquareList> mapsqrs = new List<OurMapSquareList>();
        for (int i = 0; i <= radius; i++)
        {
            mapsqrs.Add(Worldmap.GetMapSquaresInRadius(source, i));
        }
        
        //List<OurMapSquareList> mapsqrs = Worldmap.GetNeighbourSquares(Worldmap.FindMapSquareByCordinates(xCord,yCord), radius);
        foreach (OurMapSquareList list in mapsqrs)
        {
            foreach (MapSquare mapsqr in list)
            {
                GameSquare gamesqr = (GameSquare)mapsqr;
                foreach (Resource res in gamesqr.Resources)
                {
                    metRequierements.AddToExistingValue(res.TemplateKeyword,res.Amount);
                }
            }
        }

        foreach (Stat requiredResource in requiredResources)
        {
            bool keywordmet = false;
            bool amountMet = false;
            foreach (Stat met in metRequierements)
            {
                if (requiredResource.Keyword == met.Keyword)
                {
                    keywordmet = true;
                    if (requiredResource.Amount <= met.Amount)
                    {
                        amountMet = true;
                        matched.Add(new Stat(requiredResource.Keyword,requiredResource.Amount));
                    }
                }
            }

            if (!keywordmet || !amountMet)
            {
                missed.Add(new Stat(requiredResource.Keyword, requiredResource.Amount));
            }

            
        }


        answer.Add(matched);
        answer.Add(missed);
        return answer;
    }



    public bool ShowNontriggeredEventChains
    {
        set
        {
            Option option = this.OptionList.findByKeyword(OptionCollection.Show_NonTriggered_Events);
            option.Values.findOptionsMyValueByKeyword("condition").Value = value.ToString();
        }

        //get { return Boolean.Parse(this.OptionList.findByNameWithType(OptionCollection.Show_NonTriggered_Events, "condition")); }
        get
        {
            return Boolean.Parse(this.OptionList.findByNameWithType(OptionCollection.Show_NonTriggered_Events, "condition"));
        }
    }

    public List<string> UsedHeroKeywords { get => usedHeroKeywords; set => usedHeroKeywords = value; }
    public int MerchantGuildIdCounter { get => merchantGuildIdCounter; set => merchantGuildIdCounter = value; }
    public MerchantGuildList Guilds { get => guilds; set => guilds = value; }
    public int ItemIDCounter { get => itemIDCounter; set => itemIDCounter = value; }
    public int ShopIdCounter { get => shopIdCounter; set => shopIdCounter = value; }

    
    public int CurrentPlayerQuestRefreshTurn { get => currentPlayerQuestRefreshTurn; set => currentPlayerQuestRefreshTurn = value; }
    public int QuestIDCounter { get => questIDCounter; set => questIDCounter = value; }
    public int QuestPartyIDCounter { get => questPartyIDCounter; set => questPartyIDCounter = value; }
    public int BuildingProductionIdCounter { get => buildingProductionIdCounter; set => buildingProductionIdCounter = value; }

    /// <summary>
    /// those are battles that are ongoing, most likely open in some UI, will not let game progress to next phase until they are completed
    /// </summary>
    public BattleManager ActiveBattles { get => activeBattles; set => activeBattles = value; }
    public List<PlayerSetup> PlayerSetups { get => playerSetups; set => playerSetups = value; }
    public int CurrentFutureHeroeRefreshTurn { get => currentFutureHeroeRefreshTurn; set => currentFutureHeroeRefreshTurn = value; }
    public double CurrentInflation { get => currentInflation; set => currentInflation = value; }
    public int TransactionItemIDCounter { get => transactionItemIDCounter; set => transactionItemIDCounter = value; }
    public int CombatObstacleIDCounter { get => combatObstacleIDCounter; set => combatObstacleIDCounter = value; }
    public int EffectIDCounter { get => effectIDCounter; set => effectIDCounter = value; }
    public List<string> PlayersWhoEndedTurn { get => playersWhoEndedTurn; set => playersWhoEndedTurn = value; }
    /// <summary>
    /// when loaded, set  gameengine static random with this
    /// </summary>
    public MyRandom SavedRandom { get => savedRandom; set => savedRandom = value; }
    public MyRandom AiRandom { get => aiRandom; set => aiRandom = value; }
    public List<string> PlayersWhoEndedEvents { get => playersWhoEndedEvents; set => playersWhoEndedEvents = value; }
    public int ProductionLineIDCounter { get => productionLineIDCounter; set => productionLineIDCounter = value; }
    public Scoreboard Scoreboard { get => scoreboard; set => scoreboard = value; }
    public bool Ended { get => ended; set => ended = value; }
    public List<string> PlayersThatWonList { get => playersThatWonList; set => playersThatWonList = value; }
    public OurMyValueList DefeatedPlayerToClient { get => defeatedPlayerToClient; set => defeatedPlayerToClient = value; }
    public string MapTemplateKeyword { get => mapTemplateKeyword; set => mapTemplateKeyword = value; }
    public int EventIDCounter { get => eventIDCounter; set => eventIDCounter = value; }


    void DebugAllBuildignInfo(string bonusMsg)
    {
        if (bonusMsg != "")
        {
            Debug.Log(bonusMsg);
        }
        foreach (GameSquare gameSquare in Worldmap.GameSquares)
        {
            if (gameSquare.building != null)
            {
                Debug.Log("gamesqr at " + gameSquare.X_cord + " " + gameSquare.Y_cord + " building & durability & ID " + gameSquare.building.TemplateKeyword + " " + gameSquare.building.Durability.Current + " " + gameSquare.building.ID);
            }
        }
    }

    /// <summary>
    /// decreases building durability, and if destroyed, gives 50% of ingredients to the destroyer
    /// returns true if building gets destroyed, returns false if its not destroyed
    /// </summary>
    /// <param name="army"></param>
    /// <param name="gameSquareid"></param>
    public bool RazeBuilding(Army army,int gameSquareid)
    {
        bool debug = true;
        GameSquare gameSquare = Worldmap.FindGameSquareByID(gameSquareid);
        if (gameSquare == null)
        {
            Debug.LogError("gamesquare not found");
            return true;
        }
        if (gameSquare.building == null)
        {
            Debug.Log("building is already destroyed");
            return true;
        }
        double razingPower = (army.RazingPower * army.GetOverlandActionModifier());
        //DebugAllBuildignInfo("DebugAllBuildignInfo before");
        gameSquare.building.Durability.Current -= razingPower;
        Player owner = GameEngine.ActiveGame.scenario.FindPlayerByID(gameSquare.building.OwnerPlayerID);
        Player destroyer = FindPlayerByID(army.OwnerPlayerID);
        //DebugAllBuildignInfo("DebugAllBuildignInfo after");
        if (debug)
        {
            Debug.Log("RazeBuilding start, razing is army: " + army.OwnerPlayerID + " " + army.GetInformation() + " razing power " + razingPower);
        }


        if (gameSquare.building.Durability.Current <= 0)
        {
            if (debug)
            {
                Debug.Log("building has been razed!");
            }
         
          
            //if i dont do this memorytile block of code, the building is still visible on overland map TODO fix that so i could remove this from here
            //MemoryTile memoryTile = FindMemoryTileByCoordinates(destroyer.PlayerID,gameSquare.X_cord,gameSquare.Y_cord);
            //memoryTile.BuildingGraphics = "";
            //memoryTile.BuildingID = -1;
            //memoryTile.BuildingKeyword = "";
            List<Item> itemsFromRazing = Building.CancelBuilding(false, gameSquare.building.TemplateKeyword);
            itemsFromRazing.AddRange(gameSquare.building.GetItemsFromProductions());
            if (itemsFromRazing.Count > 0)
            {
                CreateExtraItem(destroyer.PlayerID, itemsFromRazing, 6, "You have gained materials from razing", "Take your items:", false);

            }

            //if (destroyer.OwnedItems.HasSpaceToTakeItems(itemsFromRazing,true))
            //{
            //    destroyer.OwnedItems.AddRangeItems(itemsFromRazing);
            //}
            //else
            //{
            //    CreateExtraItem(destroyer.PlayerID, itemsFromRazing, 6);
            //}
            if (gameSquare.building.RaceRelationChangeOnRaze.Count > 0)
            {
                Notification notification = new Notification();
                notification.ID = ++destroyer.LocalNotificationID;

                notification.Type = Notification.NotificationType.TYPE_BUILDING_RAZE_RELATION_CHANGES;
                notification.HeaderText = "Race relation changes due to razing of building: " + gameSquare.building.TemplateKeyword;
                notification.ExpandedText = "The changes occured due to your army razing a building";
                notification.MapCoordinates = new MapCoordinates(gameSquare.X_cord,gameSquare.Y_cord);
                notification.Picture = "ButtonIcons/relationChange";
                notification.BgImageG = 180;
                notification.BgImageB = 180;
                destroyer.Notifications.Add(notification);



                foreach (Stat raceRelationChange in gameSquare.building.RaceRelationChangeOnRaze)
                {
                    StatTemplate statTemplate = GameEngine.Data.StatCollection.findByKeyword(raceRelationChange.Keyword);
                    destroyer.RaceRelations.AddToExistingValue(raceRelationChange.Keyword, raceRelationChange.Amount);
                    NotificationElement notificationElement = new NotificationElement();
                    string amountStr = "+";
                    if (raceRelationChange.Amount < 0)
                    {
                        amountStr = "";
                    }
                    notificationElement.Content = "Race relation change: " + raceRelationChange.Keyword + " " + amountStr + raceRelationChange.Amount;
                    notificationElement.AdditionalToolTipContent = "Your relation to " + raceRelationChange.Keyword + " changed by " + amountStr + raceRelationChange.Amount;
                    notificationElement.Picture = statTemplate.Image;
                    notification.NotificationElements.Add(notificationElement);

                }
            }

            if (destroyer.PlayerID != owner.PlayerID)
            {
                string canSeeWho = "hidden enemy army";
                MemoryArmy memoryArmy = owner.MapMemory.FindMemoryArmyByArmyIDVisible(army.ArmyID);
                if (memoryArmy != null)
                {
                    canSeeWho = "by army of " + memoryArmy.PlayerID; 
                }
                Notification notificationForBuildingOwner = new Notification();
                notificationForBuildingOwner.BgImageR = 255;
                notificationForBuildingOwner.BgImageG = 0;
                notificationForBuildingOwner.BgImageB = 0;
                notificationForBuildingOwner.IsOverland = true;
                notificationForBuildingOwner.TargetID = gameSquare.ID;
                notificationForBuildingOwner.Type = Notification.NotificationType.TYPE_WARINING_BUILDING_LOST;
                notificationForBuildingOwner.Picture = gameSquare.building.Graphics;
        
                notificationForBuildingOwner.HeaderText = "Building was destroyed by enemy";
                notificationForBuildingOwner.ExpandedText = gameSquare.building.TemplateKeyword + " has been destroyed by " + canSeeWho;

                notificationForBuildingOwner.ID = ++owner.LocalNotificationID;
                owner.Notifications.Add(notificationForBuildingOwner);
            }
 

            gameSquare.building = null;
            return true;
        }
        else
        {
            if (destroyer.PlayerID != owner.PlayerID)
            {
                string canSeeWho = "hidden enemy army";
                MemoryArmy memoryArmy = owner.MapMemory.FindMemoryArmyByArmyIDVisible(army.ArmyID);
                if (memoryArmy != null)
                {
                    canSeeWho = "by army of " + memoryArmy.PlayerID;
                }
                Notification notificationForBuildingOwner = new Notification();
                notificationForBuildingOwner.BgImageR = 255;
                notificationForBuildingOwner.BgImageG = 144;
                notificationForBuildingOwner.BgImageB = 144;
                notificationForBuildingOwner.IsOverland = true;
                notificationForBuildingOwner.TargetID = gameSquare.ID;
                notificationForBuildingOwner.Type = Notification.NotificationType.TYPE_WARINING_BUILDING_IS_BEING_CAPTURED;
                notificationForBuildingOwner.Picture = gameSquare.building.Graphics;
                notificationForBuildingOwner.HeaderText = "Building is being destroyed by enemy";
                notificationForBuildingOwner.ExpandedText = gameSquare.building.TemplateKeyword + " is being destroyed by " + canSeeWho;

                notificationForBuildingOwner.ID = ++owner.LocalNotificationID;
                owner.Notifications.Add(notificationForBuildingOwner);
            }
            else
            {
                Notification notificationForBuildingOwner = new Notification();
                notificationForBuildingOwner.BgImageR = 144;
                notificationForBuildingOwner.BgImageG = 144;
                notificationForBuildingOwner.BgImageB = 144;
                notificationForBuildingOwner.IsOverland = true;
                notificationForBuildingOwner.TargetID = gameSquare.ID;
                notificationForBuildingOwner.Type = Notification.NotificationType.TYPE_WARINING_BUILDING_IS_BEING_CAPTURED;
                notificationForBuildingOwner.Picture = gameSquare.building.Graphics;
                notificationForBuildingOwner.HeaderText = "Building demolish progress: "+ gameSquare.building.Durability.Current + "/" + gameSquare.building.Durability.Boosted;
                notificationForBuildingOwner.ExpandedText = "Building demoliton is in progress";

                notificationForBuildingOwner.ID = ++owner.LocalNotificationID;
                owner.Notifications.Add(notificationForBuildingOwner);
            }
        }


        //destroying combat structures
        BattlefieldBuildingZoneTemplate battlefieldBuildingZoneTemplate = GameEngine.Data.BattlefieldBuildingZoneTemplateCollection.findByKeyword(gameSquare.building.CombatStructureKeyword);
        
        //suppose theres castle with 100 durability and 4 structures, it now has 75 durability
        
        //4 structures
        int structureCount = battlefieldBuildingZoneTemplate.GetMatchingStrucure(GameEngine.ActiveGame.scenario.CombatSectorRadius).Count; //no change for events, because you cant raze overland event building of combat
        //this number means we see how many obstacles there are for the 75 durability remaining
        //heres its 3
        //using math.floor here just to make sure theres something to destroy in combat if building isnt destroyed
        int howManyStructuresToCurrentDurability = (int)Math.Ceiling((gameSquare.building.Durability.Current * structureCount) / gameSquare.building.Durability.Original);
        //get the difference between max durability's structure count and current durability's structure count
        //4 - 3 = 1 structure to destroy
        int howManyStructuresToDestroy = structureCount - howManyStructuresToCurrentDurability;
        //if 1 structure was destroyed, and next time it wasnt, howManyStructuresToDestroy would destroy more, to cancel it i use previously destroyed obstacles
        //that was if 1 obstacle should be destroyed, but 1 obstacle was already destroyed, then no obstacle gets destroyed
        //if 2 obstacles should be destroyed, but 1 obstacle was already destroyed, then 1 obstacle gets destroyed
        howManyStructuresToDestroy = howManyStructuresToDestroy - gameSquare.building.DestroyedObstacles.Count;

 



        // int howManyStructuresToDestroy = (int)Math.Floor(razingPower / durabilityPerObstacle);
      
        for (int i = 0; i < howManyStructuresToDestroy; i++)
        {
            gameSquare.building.DestroyObstacle(battlefieldBuildingZoneTemplate);
        }
        if (debug)
        {
            Debug.Log("razing building: " + gameSquare.building.TemplateKeyword + " razing power: " + razingPower + " durability: " + gameSquare.building.Durability.Current + "/" + gameSquare.building.Durability.Original + " structures to destroy: " + howManyStructuresToDestroy + " current structures destroyed " + gameSquare.building.DestroyedObstacles.Count + "/" + structureCount);
        }

        return false;
    }
    /// <summary>
    /// not giving materials to prevent friendly firing own structures
    /// </summary>
    /// <param name="damage"></param>
    /// <param name="squareid"></param>
    public void RazeBuildingInCombat(double damage, int squareid)
    {
        GameSquare gameSquare = (GameSquare)Worldmap.FindMapSquareByID(squareid);

        gameSquare.building.Durability.Current -= damage;
        if (gameSquare.building.Durability.Current <= 0)
        {
            gameSquare.building = null;
            Debug.Log("building has been razed!");
        }


    }

    public Quest FindQuestByID(int ID)
    {

        foreach (Player player in Players)
        {
            foreach (Quest quest in player.ActiveQuests)
            {
                if (quest.ID == ID)
                {
                    return quest;
                }
            }
            foreach (Quest quest in player.FutureQuests)
            {
                if (quest.ID == ID)
                {
                    return quest;
                }
            }
        }



        return null;
    }


    /// <summary>
    /// PHASE 1: if surveyed: deconstructs other players building if there is one, adds a building plan if there isnt one | if isnt surveyed: surveys
    /// also takes resources from player & entity for planning if theres no building or its an upgrade
    /// </summary>
    /// <param name="playerID"></param>
    /// <param name="buildingKW"></param>
    /// <param name="armyID"></param>
    public void StartingBuildingProcess(string playerID, string buildingKW, int armyID)
    {
        bool paidFor = false;
        Army army = FindArmyByID(armyID);
        MemoryTile memoryTile = GameEngine.ActiveGame.scenario.FindMemoryTileByCoordinates(playerID, army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);
        if (memoryTile.IsSurveyed)
        {
            GameSquare gameSquare = GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareByCordinates(army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);

            //start deconstructing existing building unless its an upgrade or repair
            if (gameSquare.building != null)
            {
                 
                if (gameSquare.building.TemplateKeyword == buildingKW)
                {
                 
                    if (gameSquare.building.OwnerPlayerID == playerID && (gameSquare.building.TemplateKeyword == buildingKW || gameSquare.building.UnfinishedBuildingTemplateKeyword == buildingKW))
                    {
                        Debug.Log("repair detected");
                        //Debug.Log("building: " + gameSquare.building.TemplateKeyword + " unfinished: " + gameSquare.building.UnfinishedBuildingTemplateKeyword);
                        //return;
                        paidFor = true;
                    }
                }
                else if(gameSquare.building.UnfinishedBuildingTemplateKeyword == "")
                {
                    BuildingTemplate template = GameEngine.Data.BuildingTemplateCollection.findByKeyword(gameSquare.building.TemplateKeyword);
                    Debug.Log("upgrade detected");
                    if (template.UpgradeToBuildingTemplateKeywords.Contains(buildingKW))
                    {
                        gameSquare.building.UnfinishedBuildingTemplateKeyword = buildingKW;
                    }
                }else if(gameSquare.building.OwnerPlayerID != army.OwnerPlayerID || gameSquare.building.UnfinishedBuildingTemplateKeyword != buildingKW)
                {
               
                    RazeBuilding(army, gameSquare.ID);
                    if (gameSquare.building == null)
                    {
                        Debug.Log("building has been deconstructed");
                    }
                    else
                    {
                        Debug.Log("destroying building" + gameSquare.building.TemplateKeyword + " has " + gameSquare.building.Durability.Current + " durability left unfinishedKW: " + gameSquare.building.UnfinishedBuildingTemplateKeyword);
                        Debug.Log("intented kw: " + buildingKW);
                        return;
                    }
                    //if (gameSquare.building.Durability > 0)
                    //{
                    //    Debug.Log("building has " + gameSquare.building.Durability + " durability left");
                    //    return;
                    //}
                    //else
                    //{

                    //    Debug.Log("building has been deconstructed");
                    //}
                }

 
               

            }

            // add a buildingplan to gamesquare
            //foreach (BuildingPlan buildplan in gameSquare.BuildingPlans)
            //{
            //    Debug.Log("planned: " + buildplan + " intended: " + buildingKW);
            //    if (buildplan.BuildingKeyword == buildingKW)
            //    {
            //        Debug.Log("no new plan allowed");
            //        return;
            //    }
            //    if (buildplan.BuildersID == army.LeaderID)
            //    {
            //        Debug.Log("same id, returning");
            //        return;
            //    }
            //}





            //if building plan of same player with same planned building exists, dont create & dont add new one
            //foreach (BuildingPlan bp in gameSquare.BuildingPlans)
            //{
            //    if (gameSquare.building != null)
            //    {
            //        Debug.Log("building: " + gameSquare.building.TemplateKeyword + " unfinished: " + gameSquare.building.UnfinishedBuildingTemplateKeyword);
            //        if (bp.PlayerID == playerID && (bp.BuildingKeyword == buildingKW || gameSquare.building.UnfinishedBuildingTemplateKeyword == buildingKW))
            //        {
            //            return;
            //        }
            //    }
            //    else
            //    {
            //        if (bp.PlayerID == playerID && bp.BuildingKeyword == buildingKW)
            //        {
            //            return;
            //        }
            //    }
               
            //}
       

            //subscribing another builder to the existing builder plan
            foreach (BuildingPlan item in gameSquare.BuildingPlans)
            {
                if (item.BuildingKeyword == buildingKW && item.PlayerID == playerID)
                {
                    if (!item.BuildersIDs.Contains(army.LeaderID)) //if you move away from construction, and then go back at it, dont add duplicate
                    {
                        Debug.Log("subscribing leader to plan: " + army.LeaderID);
                       
                        item.BuildersIDs.Add(army.LeaderID);
                        foreach (int d in item.BuildersIDs)
                        {
                            Debug.Log("existing leader: " + d);
                        }
                        return;
                    }
                    
                   
                }
            }

           


            BuildingPlan buildingPlan = new BuildingPlan();
            buildingPlan.PlayerID = playerID;
            buildingPlan.BuildingKeyword = buildingKW;
            buildingPlan.XCord = gameSquare.X_cord;
            buildingPlan.YCord = gameSquare.Y_cord;
            //buildingPlan.BuildersID = army.LeaderID;
            buildingPlan.BuildersIDs.Add(army.LeaderID);
            BuildingTemplate buildingTemplate = GameEngine.Data.BuildingTemplateCollection.findByKeyword(buildingPlan.BuildingKeyword);
            Entity leader = FindUnitByUnitID(army.LeaderID);
            Player plr = FindPlayerByID(playerID);
            //legality check for items
            if (paidFor)
            {
                bool addNew = true;
                foreach (BuildingPlan item in gameSquare.BuildingPlans)
                {
                    if (item.BuildingKeyword == buildingKW && item.PlayerID == playerID)
                    {

                        addNew = false;

                    }
                }

                if (addNew)
                {
                    Debug.Log("no need to pay for the plan due to existing planned building");
                    gameSquare.BuildingPlans.Add(buildingPlan);
                }
                else
                {
                    Debug.Log("planned building and buildingplan exist");
                }

                
                return;
            }
            if (ItemGenerator.isRecipeMaterialLegal(buildingTemplate.Ingredients, leader, plr))
            {
                Debug.Log("removing costs from player: " + plr.PlayerID);
                foreach (Ingredient currentIngredient in buildingTemplate.Ingredients)
                {
                    int amountNeeded = currentIngredient.Amount;

                    ItemTemplate itemTemplate = GameEngine.Data.ItemTemplateCollection.findByKeyword(currentIngredient.Name);
                    Debug.Log("removed : " + currentIngredient.Name + " " + currentIngredient.Amount);
                    ItemGenerator.removeCosts(itemTemplate, amountNeeded, leader, plr);

                } // end foreach (Ingredient currentIngredient in recipe.Ingredients)
                gameSquare.BuildingPlans.Add(buildingPlan);
                buildingPlan.PaidFor = true;
            }
            else
            {
                Debug.Log("insufficient resources: "); //TODO: maybe null the mission??

                foreach (var item in buildingTemplate.Ingredients)
                {
                    Debug.Log("ingridient: " + item.Name + " amount: " + item.Amount);
                }
            }
        }
        else //gamesqr isnt surveyed, so we survey it
        {
            Debug.Log("Surveying gamesquare");
            SurveyGameSquare(army);
        }
    }




    /// <summary>
    /// PHASE 2 from end turn: creates building in progress from buildingplan that has army with highest production power
    /// 
    /// </summary>
    public void ProcessBuildingPlanPhase()
    {
        foreach (GameSquare gamesqr in Worldmap.GameSquares)
        {
            //GameSquare gamesqr = Worldmap.FindMapSquareByCordinates(army.WorldMapPositionX, army.WorldMapPositionY);
            
            if (gamesqr.BuildingPlans.Count > 0)
            {
                BuildingPlan winner = null;
                double best = -1;


                List<BuildingPlan> toRemove = new List<BuildingPlan>();




                foreach (BuildingPlan buildingPlan in gamesqr.BuildingPlans)
                {
                    List<int> idsToRemove = new List<int>();
                    foreach (int id in buildingPlan.BuildersIDs)
                    {
                        Entity leader = FindUnitByUnitID(id);
                        Army builderArmy = FindOverlandArmyByUnit(leader.UnitID);
                        if (builderArmy == null)
                        {
                            Debug.Log("army leader is null: " + id);
                            idsToRemove.Add(id);
                            continue;
                        }
                        if (builderArmy.Location.WorldMapCoordinates.XCoordinate != gamesqr.X_cord || builderArmy.Location.WorldMapCoordinates.YCoordinate != gamesqr.Y_cord)
                        {
                            Debug.Log("army left the plan: " + builderArmy.ArmyID);
                            idsToRemove.Add(id);
                        }
                        if (leader == null)
                        {
                            idsToRemove.Add(id);
                            continue;
                        }
                        if (leader.Mission == null)
                        {
                            idsToRemove.Add(id);
                            continue;
                        }
                        if (leader.Mission.MissionName != Mission.mission_Build)
                        {
                            idsToRemove.Add(id);
                            continue;
                        }

                    }
                    foreach (int item in idsToRemove)
                    {
                        Debug.Log("removing id : " + item + " total ids: " + buildingPlan.BuildersIDs.Count + " from plan: " + buildingPlan.BuildingKeyword + " total plans: " + gamesqr.BuildingPlans.Count + " " + buildingPlan.PlayerID);
                        buildingPlan.BuildersIDs.Remove(item);
                    }
                    if (buildingPlan.BuildersIDs.Count == 0)
                    {
                        Debug.Log("no subscribers to plan, removing the plan from list");
                        toRemove.Add(buildingPlan);
                        continue;
                    }
                }



                if (gamesqr.building == null)
                {
                    //buildingplan with army that has highest productionpower wins
                    foreach (BuildingPlan buildingPlan in gamesqr.BuildingPlans)
                    {
                     
                        double power = buildingPlan.GetTotalBuildingPower();
                        Debug.Log("comparing plan: " + buildingPlan.BuildingKeyword + " plr: " + buildingPlan.PlayerID + " power: " + power + " best: " + best + " plan count: " + gamesqr.BuildingPlans.Count);
                        if (power > best)
                        {
                            best = power;
                            winner = buildingPlan;
                        }
                    }

                }
                else
                {


                    //safe, because its impossible for armies of other players to place a building plan onto existing building
                    winner = gamesqr.BuildingPlans[0];
                    Debug.Log("building exists, taking 1st build plan");
                }
                if (winner == null)
                {
                    Debug.Log("all builder armies have left");
                    gamesqr.BuildingPlans.Clear();
                    return;
                }

                if (winner.BuildersIDs.Count == 0)
                {
                    //Debug.Log("no subscribers in winner, clearing the list");
                    Debug.Log("no subscribers in winner, removing from list");
                    //gamesqr.BuildingPlans.Clear();
                    gamesqr.BuildingPlans.Remove(winner);
                    return;
                }
                
                //remove those building plans that have lesser building power

                foreach (BuildingPlan buildingPlan in gamesqr.BuildingPlans)
                {
                    if (buildingPlan.PlayerID != winner.PlayerID || buildingPlan.BuildingKeyword != winner.BuildingKeyword)
                    {
                        if (!toRemove.Contains(buildingPlan))
                        {
                            Debug.Log("lesser plan removed: " + buildingPlan.BuildingKeyword + " " + buildingPlan.PlayerID);
                            Debug.Log("winner: " + winner.BuildingKeyword + " " + winner.PlayerID);
                            toRemove.Add(buildingPlan);
                        }

                    }
                }
                foreach (BuildingPlan buildingPlan in toRemove)
                {
                    Debug.Log("returning items to : " + buildingPlan.PlayerID);
                    Player looser = FindPlayerByID(buildingPlan.PlayerID);
                    List<Item> cancelledBuildingItems = Building.CancelBuilding(true, buildingPlan.BuildingKeyword);
                    //if (looser.OwnedItems.HasSpaceToTakeItems(cancelledBuildingItems, true))
                    //{
                    //    looser.OwnedItems.AddRangeItems(cancelledBuildingItems);
                    //}
                    //else
                    //{

                    //}
                    CreateExtraItem(looser.PlayerID, cancelledBuildingItems, 10, "A stronger building plan has been enacted on the square", "Your building plan was cancelled", false);
                    gamesqr.BuildingPlans.Remove(buildingPlan);

                }
                Army army = FindOverlandArmyByUnit(winner.BuildersIDs[0]);
                //Army army = FindArmyByUnit(FindUnitByUnitID(winner.BuildersID));

               
                bool startNewOne = false;
                bool upgrade = false;
                
                if (gamesqr.building == null)
                {
                    startNewOne = true;
                    //if (winner.PlayerID != gamesqr.building.OwnerPlayerID)
                    //{
                    //    startNewOne = true;
                    //}

                    //if (gamesqr.building.UnfinishedBuildingTemplateKeyword == "")
                    //{
                    //    //maybe we allow player to rebuild the exact same building?
                    //    if (winner.BuildingKeyword != gamesqr.building.TemplateKeyword)
                    //    {
                    //        startNewOne = true;
                    //    }
                    //}
                    //else
                    //{
                    //    //we make a new building if 
                    //    if (winner.BuildingKeyword != gamesqr.building.UnfinishedBuildingTemplateKeyword)
                    //    {
                    //        startNewOne = true;
                    //    }
                    //}

                }
                else
                {
                    if (GameEngine.Data.BuildingTemplateCollection.findByKeyword(winner.BuildingKeyword).Types.Contains(BuildingTemplate.TYPE_UPGRADE) && gamesqr.building.UnfinishedBuildingTemplateKeyword != winner.BuildingKeyword)
                    {
                        upgrade = true;
                    }
                }



                if (startNewOne)
                {
                    Debug.Log("starting new building");
                    if (GameEngine.Data.BuildingTemplateCollection.findByKeyword(winner.BuildingKeyword).UpgradeToBuildingTemplateKeywords.Contains(BuildingTemplate.TYPE_UPGRADE))
                    {
                        Debug.LogError("cannot build an upgrade: " + winner.BuildingKeyword);
                        return;
                    }
                    BuildingTemplate buildingTemplate = GameEngine.Data.BuildingTemplateCollection.findByKeyword(winner.BuildingKeyword);
                    gamesqr.building = Building.GenerateBuildingFromTemplate(GameEngine.Data.BuildingTemplateCollection.findByKeyword("Building in construction"));
                    gamesqr.building.UnfinishedBuildingTemplateKeyword = winner.BuildingKeyword;
                    gamesqr.building.Durability.Original = buildingTemplate.Durability.Original;
                    TransferBuildingOwnership(army.LeaderID,army.OwnerPlayerID,gamesqr);
                }
                if (upgrade)
                {
                    Debug.Log("building is being upgraded");
                    if (!GameEngine.Data.BuildingTemplateCollection.findByKeyword(gamesqr.building.TemplateKeyword).UpgradeToBuildingTemplateKeywords.Contains(winner.BuildingKeyword))
                    {
                        Debug.LogError("cannot upgrade " + gamesqr.building.TemplateKeyword + " to: " + gamesqr.building.UnfinishedBuildingTemplateKeyword);
                        return;
                    }
                    gamesqr.building.TemplateKeyword = "Building in upgrade";
                    //reduce amount of production points during upgrade
                    foreach (BuildingProduction buildingProduction in gamesqr.building.ArmyProductions)
                    {
                        buildingProduction.ProductionPoints.Boosted++;
                    }
                    gamesqr.building.UnfinishedBuildingTemplateKeyword = winner.BuildingKeyword;
                }
                //gamesqr.building. = army.LeaderID; //builder id exists so that during construction, the builder doesnt kill his own building
                Debug.Log("prephase 3 building: " + gamesqr.building.TemplateKeyword);
                //foreach (var item in winner.BuildersIDs)
                //{
                //   Army builder = FindArmyByUnit(FindUnitByUnitID(winner.BuildersIDs[0]));

                //}
 

                BuildBuilding(winner.BuildersIDs,gamesqr,winner.GetTotalBuildingPower());
            }
        }




         
       
        
    }
    

    /// <summary>
    /// PHASE 3 from end turn: adds to durability, when required durability is met, either nulls mission(repair), or nulls mission and creates a new building(from construction in progress(build))
    /// </summary>
    /// <param name="armyID"></param>
    public void BuildBuilding(List<int> leaderIds, GameSquare gameSquare, double buildingPower)
    {
        List<Army> participatingArmies = new List<Army>();
        foreach (var item in leaderIds)
        {
            participatingArmies.Add(FindOverlandArmyByUnit(item));
        }
        Army army = participatingArmies[0];

         //= Worldmap.FindMapSquareByCordinates(army.WorldMapPositionX,army.WorldMapPositionY);

        if (gameSquare.BuildingPlans.Count > 0) 
        {
            //if 1+ armies are building on same gamesquare, and if player allegiance doesnt allign, return, because we break buildings in phase prior
            if (gameSquare.BuildingPlans[0].PlayerID != army.OwnerPlayerID)
            {
                Debug.LogError("CANNOT BUILD BUILDING, the buildingplan for: " + gameSquare.BuildingPlans[0].BuildingKeyword + " of " + gameSquare.BuildingPlans[0].PlayerID + " should have been removed");
                return;
            }
            else
            {
                //gameSquare.BuildingPlans.Clear();
            }

        }
        if (gameSquare.building != null)
        {
            if (gameSquare.building.OwnerPlayerID == army.OwnerPlayerID)
            {
                bool debug = true;
                
                Debug.Log("building durability: " + gameSquare.building.Durability + " id: " + gameSquare.building.ID);
                double totalPower = buildingPower * army.GetOverlandActionModifier();
                gameSquare.building.Durability.Current += totalPower;
                Debug.Log("building POWER: " + totalPower);
                //gameSquare.building.Durability += army.BuildingPower;
               
                //check if its in construction
                if (gameSquare.building.UnfinishedBuildingTemplateKeyword != "")
                {
                    //construction is complete
                    //Debug.Log(gameSquare.building.UnfinishedBuildingTemplateKeyword);
                    BuildingTemplate newBuildingTemplate = GameEngine.Data.BuildingTemplateCollection.findByKeyword(gameSquare.building.UnfinishedBuildingTemplateKeyword);
                    if (gameSquare.building.Durability.Current >= gameSquare.building.Durability.Original)
                    {
                        //building was upgraded
                        if (newBuildingTemplate.Types.Contains(BuildingTemplate.TYPE_UPGRADE))
                        {
                            gameSquare.building.Modes = newBuildingTemplate.Modes;
                            if (newBuildingTemplate.Modes.Count > 1)
                            {
                                gameSquare.building.Mode = gameSquare.building.Modes[0];
                            }

                            //foreach (string buildingproductionkw in gameSquare.building.PriorityList)
                            //{
                            //    BuildingProduction bp = gameSquare.building.GetBuildingProductionByKeyword(buildingproductionkw);

                            //}
                            ArmyProductionList oldBuildingProductions = new ArmyProductionList();
                            ArmyProductionList newList = new ArmyProductionList();

                            foreach (var item in gameSquare.building.ArmyProductions)
                            {
                                oldBuildingProductions.Add(item);
                            }

                            foreach (ProductionSetup newProductionSetup in newBuildingTemplate.Troops)
                            {
                                bool upgrade = false;
                                BuildingProductionTemplate buildingProductionTemplate = GameEngine.Data.BuildingProductionTemplateCollection.findByKeyword(newProductionSetup.BuildingProductionKeyword);
                                BuildingProduction upgradable = null;
                                foreach (string priorityKeyword in buildingProductionTemplate.PriorityList)
                                {
                                    BuildingProduction buildingProduction = oldBuildingProductions.GetBuildingProductionByKeyword(priorityKeyword);

                                    if (buildingProduction != null)
                                    {
                                        //matches new production
                                        if (buildingProduction.Keyword == newProductionSetup.BuildingProductionKeyword)
                                        {
                                            oldBuildingProductions.Remove(buildingProduction);
                                            newList.Add(buildingProduction);
                                            break;
                                        }
                                        //priority item found
                                        if (buildingProduction.Keyword == priorityKeyword)
                                        {
                                            oldBuildingProductions.Remove(upgradable);
                                            upgradable = buildingProduction;
                                            upgrade = true;
                                            break;
                                        }


                                    }
                                }
                                if (!upgrade)
                                { //no upgrade, so we create new production
                                    BuildingProduction bp = new BuildingProduction(true);
                                    bp.RecipeRequests.Clear();
                                    foreach (ProductionRecipeRequest request in buildingProductionTemplate.RecipeRequests)
                                    {
                                        bp.RecipeRequests.Add(request);
                                    }
                                    bp.ProductionLines.Clear();
                                    foreach (ProductionLineTemplate productionTemplate in buildingProductionTemplate.ProductionLineTemplates)
                                    {

                                        ProductionLine productionLine = new ProductionLine(true);
                                        productionLine.RecipeKeyword = productionTemplate.RecipeKeyword;
                                        productionLine.QualityRange = productionTemplate.QualityRange;
                                        productionLine.Bonus = productionTemplate.Bonus;
                                        productionLine.Cost = productionTemplate.Cost;
                                        productionLine.OddsForPercentageAllocation = productionTemplate.OddsForPercentageAllocation;
                                        productionLine.SendsToPlayerStash = productionTemplate.SendsToPlayerStash;
                                        foreach (Stat currentStat in productionTemplate.Stats)
                                        {
                                            EntityStat newStat = new EntityStat(currentStat.Keyword, currentStat.Amount, currentStat.Amount, currentStat.Amount, currentStat.Amount);

                                            productionLine.Stats.AddWithOverwriting(newStat);
                                        }
                                        bp.ProductionLines.Add(productionLine);
                                    }

                                    newList.Add(bp);
                                    continue;
                                }
                                else
                                { //priority matched, we add it to new list
                                    if (debug)
                                    {
                                        Debug.Log("upgrading production: " + upgradable.GetInformation());
                                    }
                                    ItemCollection temporaryStash = upgradable.Stash.ReturnDeepClone();
                                    List<int> entityIds = upgradable.EntityIds;
                                    upgradable = BuildingProduction.CreateByTemplate(buildingProductionTemplate);
                                    upgradable.EntityIds = entityIds;
                                    upgradable.Stash = temporaryStash;
                                    //upgradable.Keyword = buildingProductionTemplate.Keyword;
                                    //upgradable.UnitName = buildingProductionTemplate.UnitTemplateKeyword;
                                    //upgradable.UnitTraining = buildingProductionTemplate.UnitTraining;
                                    //upgradable.Modes = buildingProductionTemplate.Modes;
                                    //upgradable.ProductionPoints.Boosted--;
                                    //upgradable.RecipeRequests.Clear();
                                    //foreach (ProductionRecipeRequest request in buildingProductionTemplate.RecipeRequests)
                                    //{
                                    //    upgradable.RecipeRequests.Add(request);
                                    //}
                                    //upgradable.ProductionLines.Clear();
                                    //foreach (ProductionLineTemplate productionTemplate in buildingProductionTemplate.ProductionLineTemplates)
                                    //{

                                    //    ProductionLine productionLine = new ProductionLine();
                                    //    productionLine.RecipeKeyword = productionTemplate.RecipeKeyword;
                                    //    productionLine.QualityRange = productionTemplate.QualityRange;
                                    //    productionLine.Bonus = productionTemplate.Bonus;
                                    //    productionLine.Cost = productionTemplate.Cost;
                                    //    productionLine.OddsForPercentageAllocation = productionTemplate.OddsForPercentageAllocation;
                                    //    productionLine.SendsToPlayerStash = productionTemplate.SendsToPlayerStash;
                                    //    foreach (Stat currentStat in productionTemplate.Stats)
                                    //    {
                                    //        EntityStat newStat = new EntityStat(currentStat.Keyword, currentStat.Amount, currentStat.Amount, currentStat.Amount, currentStat.Amount);

                                    //        productionLine.Stats.AddWithOverwriting(newStat);
                                    //    }
                                    //    upgradable.ProductionLines.Add(productionLine);
                                    //}

                                    if (debug)
                                    {
                                        Debug.Log("production upgraded: " + upgradable.GetInformation());
                                    }
                                    newList.Add(upgradable);
                                }
                       
                            }

                            gameSquare.building.ArmyProductions = newList;
                            string oldkw = gameSquare.building.TemplateKeyword;
                            gameSquare.building.TemplateKeyword = newBuildingTemplate.Keyword;
                            gameSquare.building.UnfinishedBuildingTemplateKeyword = "";
                            gameSquare.building.MaxPop = newBuildingTemplate.MaxPop;
                            gameSquare.building.Graphics = newBuildingTemplate.Graphics[GameEngine.random.Next(0, newBuildingTemplate.Graphics.Count)];
                            gameSquare.building.Value = newBuildingTemplate.Value;
                            gameSquare.building.Durability = newBuildingTemplate.Durability;
                            gameSquare.building.CapPoints = newBuildingTemplate.CapPoints;
                            gameSquare.building.Vision = newBuildingTemplate.Sight;
                            gameSquare.building.ConcealmentStat = newBuildingTemplate.Concealment;
                            gameSquare.building.VisionRangeBonus = newBuildingTemplate.VisionRangeBonus;
                            gameSquare.building.RequiredResources = newBuildingTemplate.RequiredResourceForFunctioning;
                            gameSquare.building.RaceRelationChange = newBuildingTemplate.RaceChangeOnBuild;
                         
                            //gameSquare.building.RequiredItems = buildingTemplate.RequiredItemsForBuilding;


                            Notification notification = new Notification();
         
                            notification.IsOverland = true;
                            notification.TargetID = gameSquare.building.ID;
                            notification.Type = Notification.NotificationType.TYPE_BUILDING_UPGRADE_COMPLETE;
                            notification.Picture = gameSquare.building.Graphics;

                            notification.HeaderText = oldkw + " was upgraded into " + gameSquare.building.TemplateKeyword;
                            notification.ExpandedText =  " participating heroes(and their armies) ";
                         

                            int d = 0;
                            foreach (int leaderid in leaderIds)
                            {
                                Entity entity = FindUnitByUnitID(leaderid);
                 
                                NotificationElement notificationElement = new NotificationElement();
                                notificationElement.EntityID = leaderid;
                                notificationElement.Content = entity.UnitName;
                                notificationElement.Picture = entity.GetPicture();
                                notification.NotificationElements.Add(notificationElement);
                            }
                            Player player = FindPlayerByID(army.OwnerPlayerID);
                            foreach (Stat raceRelationChange in gameSquare.building.RaceRelationChange)
                            {
                                StatTemplate statTemplate = GameEngine.Data.StatCollection.findByKeyword(raceRelationChange.Keyword);
                                player.RaceRelations.AddToExistingValue(raceRelationChange.Keyword, raceRelationChange.Amount);
                                NotificationElement notificationElement = new NotificationElement();
                                string amountStr = "+";
                                if (raceRelationChange.Amount < 0)
                                {
                                    amountStr = "";
                                }
                                notificationElement.Content = "Race relation change: " + raceRelationChange.Keyword + " " + amountStr + raceRelationChange.Amount;
                                notificationElement.AdditionalToolTipContent = "Your relation to " + raceRelationChange.Keyword + " changed by " + amountStr + raceRelationChange.Amount;
                                notificationElement.Picture = statTemplate.Image;
                                notification.NotificationElements.Add(notificationElement);
                            }
                                   
                          
                            notification.ID = ++player.LocalNotificationID;
                            player.Notifications.Add(notification);


                            Debug.Log("upgrading is complete");
 
                        }
                        else //building was constructed
                        {
                            gameSquare.building = Building.GenerateBuildingFromTemplate(newBuildingTemplate);
                            TransferBuildingOwnership(army.LeaderID, army.OwnerPlayerID, gameSquare); //doing before to apply the bonuses
                            gameSquare.building.ProcessGenerationCommands(GenerationCommand.TYPE_BUILDING_COMPLETE,army.OwnerPlayerID);
                      
                            //TransferBuildingOwnership(army.LeaderID,army.OwnerPlayerID,gameSquare);
                           





                            Notification notification = new Notification();
               
                            notification.IsOverland = true;
                            notification.TargetID = gameSquare.building.ID;
                            notification.Type = Notification.NotificationType.TYPE_BUILDING_COMPLETE;
                            notification.Picture = gameSquare.building.Graphics;
                            notification.HeaderText = gameSquare.building.TemplateKeyword + " has been built! ";
                            notification.ExpandedText = " participating heroes(and their armies) ";

                            foreach (int leaderid in leaderIds)
                            {
                                Entity entity = FindUnitByUnitID(leaderid);
                                NotificationElement notificationElement = new NotificationElement();
                                notificationElement.EntityID = leaderid;
                                notificationElement.Content = entity.UnitName;
                                notificationElement.Picture = entity.GetPicture();
                                notification.NotificationElements.Add(notificationElement);

                            }

                            Player player = FindPlayerByID(army.OwnerPlayerID);


                            foreach (Stat raceRelationChange in gameSquare.building.RaceRelationChange)
                            {
                                StatTemplate statTemplate = GameEngine.Data.StatCollection.findByKeyword(raceRelationChange.Keyword);
                                player.RaceRelations.AddToExistingValue(raceRelationChange.Keyword, raceRelationChange.Amount);
                                NotificationElement notificationElement = new NotificationElement();
                                string amountStr = "+";
                                if (raceRelationChange.Amount < 0)
                                {
                                    amountStr = "";
                                }
                                notificationElement.Content = "Race relation change: " + raceRelationChange.Keyword + " " + amountStr + raceRelationChange.Amount;
                                notificationElement.AdditionalToolTipContent = "Your relation to " + raceRelationChange.Keyword + " changed by " + amountStr + raceRelationChange.Amount;
                                notificationElement.Picture = statTemplate.Image;
                                notification.NotificationElements.Add(notificationElement);
                            }

                            notification.ID = ++player.LocalNotificationID;
                            player.Notifications.Add(notification);








                            Debug.Log("building is complete : " + newBuildingTemplate.Keyword);
                        }
                        foreach (Army arm in participatingArmies)
                        {
                            FindUnitByUnitID(arm.LeaderID).Mission = null;
                        }
                        
                        gameSquare.BuildingPlans.Clear();
                         
                    }
                    else
                    {
                        Debug.Log("building progress: " + gameSquare.building.Durability + "/" + GameEngine.Data.BuildingTemplateCollection.findByKeyword(gameSquare.building.UnfinishedBuildingTemplateKeyword).Durability);
                    }
                }
                else
                {
                    //we are repairing the building
                    if (gameSquare.building.Durability.Current >= gameSquare.building.Durability.Original)
                    {
                        //the building has been repaired
                        gameSquare.building.Durability.Current = gameSquare.building.Durability.Original;
                        //Entity leader = FindUnitByUnitID(army.LeaderID);
                        //leader.Mission = null;
                        foreach (Army arm in participatingArmies)
                        {
                            FindUnitByUnitID(arm.LeaderID).Mission = null;
                        }
                        Debug.Log("building has been repaired");




                        Notification notification = new Notification();
                
                        notification.IsOverland = true;
                        notification.TargetID = gameSquare.building.ID;
                        notification.Type = Notification.NotificationType.TYPE_BUILDING_REPAIR_COMPLETE;
                        notification.Picture = gameSquare.building.Graphics;

                        notification.HeaderText = gameSquare.building.TemplateKeyword + " has been repaired! ";
                        notification.ExpandedText = " participating heroes(and their armies) ";
                    
                        Debug.Log("leader ids count: " + leaderIds.Count);
                        //int d = 0;
                        foreach (int leaderid in leaderIds)
                        {
                            Entity entity = FindUnitByUnitID(leaderid);
                            NotificationElement notificationElement = new NotificationElement();
                            notificationElement.EntityID = leaderid;
                            notificationElement.Content = entity.UnitName;
                            notificationElement.Picture = entity.GetPicture();
                            notification.NotificationElements.Add(notificationElement);

                           
                            //if (d == 0)
                            //{
                            //    d = 1;
                            //    notification.ExpandedText += entity.UnitName + Environment.NewLine;
                            //}
                            //notification.ExpandedText += ", " + entity.UnitName + Environment.NewLine;
                        }

                        Player player = FindPlayerByID(army.OwnerPlayerID);
                        notification.ID = ++player.LocalNotificationID;
                        player.Notifications.Add(notification);


                    }
                    else
                    {
                        Debug.Log("building repair: " + gameSquare.building.Durability + "/" + GameEngine.Data.BuildingTemplateCollection.findByKeyword(gameSquare.building.TemplateKeyword).Durability);
                    }
                    BattlefieldBuildingZoneTemplate battlefieldBuildingZoneTemplate = GameEngine.Data.BattlefieldBuildingZoneTemplateCollection.findByKeyword(gameSquare.building.CombatStructureKeyword);
                    //restoring combat structures
                   // double durabilityPerObstacle = Math.Ceiling(gameSquare.building.Durability.Original / battlefieldBuildingZoneTemplate.GetMatchingStrucure(GameEngine.ActiveGame.scenario.CombatSectorRadius).Count);
                    //maybe use math ceilling?
                    //int howManyCombatStructuresToRestore = (int)(totalPower / durabilityPerObstacle);
                    //for (int i = 0; i < howManyCombatStructuresToRestore; i++)
                    //{
                    //    gameSquare.building.RestoreRandomObstacle();
                    //}

                    //4
                    int structureCount = battlefieldBuildingZoneTemplate.GetMatchingStrucure(GameEngine.ActiveGame.scenario.CombatSectorRadius).Count; //no change for events, because you cant raze overland event building of combat

                    //this number means we see how many obstacles there are for the 75 durability 
                    //heres its 3
                    int howManyStructuresToCurrentDurability = (int)Math.Ceiling((gameSquare.building.Durability.Current * structureCount) / gameSquare.building.Durability.Original);
                    // 1
                    int currentlyExistingStructures = structureCount - gameSquare.building.DestroyedObstacles.Count;
                    //1
                    int howManyCombatStructuresToRestore = howManyStructuresToCurrentDurability - currentlyExistingStructures;

                    //howManyCombatStructuresToRestore += gameSquare.building.DestroyedObstacles.Count;

                    for (int i = 0; i < howManyCombatStructuresToRestore; i++)
                    {
                        gameSquare.building.RestoreRandomObstacle();
                    }

                    if (debug)
                    {
                        Debug.Log("building repair structure,"+ " max structureCount " + structureCount + " howManyStructuresToCurrentDurability " + howManyStructuresToCurrentDurability + " currentlyExistingStructures " + currentlyExistingStructures + " howManyCombatStructuresToRestore " + howManyCombatStructuresToRestore + " gameSquare.building.DestroyedObstacles.Count " + gameSquare.building.DestroyedObstacles.Count);
                    }
                }

            }
        }
        else //has to have at least 1 building?
        {
            Debug.Log("missing building! ");
        }


    }

    public Building FindBuildingByProductionID(int id)
    {
        foreach (Building building in Worldmap.GetAllBuildings())
        {
            foreach (BuildingProduction production in building.ArmyProductions)
            {
                if (production.ID == id)
                {
                    return building;
                }
            }
        }
        return null;
    }

    public Building FindBuildingByGarissonID(int id)
    {
        foreach (Building building in Worldmap.GetAllBuildings())
        {
            if (building.GarissonArmyID == id)
            {
                return building;
            }
        }
        return null;
    }


    public void AddPlayersToTurnQueue()
    {
     

        foreach (Player player in Players)
        {
            playerturnqueue.Add(player.PlayerID);
        }
    }
  
}
