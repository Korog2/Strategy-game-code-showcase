using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using GameProj.Options;
using System.IO;
using System.Xml.Serialization;
using GameProj.Collections;
using GameProj.Items;
using GameProj.Entities;
using GameProj.ScenarioS;

class Multiplayer
    {

        IPEndPoint ip;  // Multiplayeri osa serveri ip
        Socket listener;  // Socketid andmete edastuseks ja kättesaamiseks
        MySocket serverSocket; // Socket, mis ühendus serveriga
        List<MySocket> clients = new List<MySocket>();
    
        internal bool serverOn = true;
    internal string testMessage = "";
        List<QueuedServerMessages> queuedServerMessagesList = new List<QueuedServerMessages>(); //used by server
    public int MessageCounter = 0;
    internal bool isHost = false; //setting to true in places where we launch server and launch client(which is the host)
    internal string hostName = ""; //this variable usable ONLY by server instance
    internal List<MultiplayerMessage> multiplayerMessages = new List<MultiplayerMessage>();
    
    internal bool stopThread = false;

    internal object scenarioObj = null;

    List<ObjectInTransfer> incomingObjects = new List<ObjectInTransfer>();

    internal List<MySocket> Clients
        {
            get { return clients; }
            set { clients = value; }
        }

        string serverIp = "0"; // serveri ip, kuhu yhendatakse
        int serverPort = 0; // serveri port, kuhu yhendatakse

        public bool connectionEndedNormally = false;

    
        public Multiplayer()
    {
        GameEngine.ActiveGame.threadController.threadsToExit.Add(this);
    }

        /// <summary>
        /// Ühendab mängija serveriga.
        /// </summary>
        public void JoinToServer(String serverIp, int serverPort)
        {
            this.serverIp = serverIp;
            this.serverPort = serverPort;
            Thread join = new Thread(new ThreadStart(JoinTo));
        GameEngine.ActiveGame.threadController.threadsControl.Add(join);
            join.Name = "JoinTo Thread";        

            join.IsBackground = true;
            join.Start();
       
        }

    public void ChatToChannel(string whoSays, string chatText) {
        testMessage = whoSays + ": " + chatText;
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.LOBBY_CHAT_MESSAGE, whoSays + ": ", chatText));
    }
    /// <summary>
    /// gets existing object in transfer or creates a new one with maxLen
    /// </summary>
    /// <param name="objectName"></param>
    /// <param name="maxLen"></param>
    /// <returns></returns>
    public ObjectInTransfer GetObjectInTransfer(string objectName, int maxLen)
    {
        foreach (ObjectInTransfer objectInTransfer in incomingObjects)
        {
            if (objectInTransfer.whatIsTheObject == objectName)
            {
                return objectInTransfer;
            }
        }
        ObjectInTransfer newObjectInTransfer = new ObjectInTransfer();
        newObjectInTransfer.totalByteLength = maxLen;
        newObjectInTransfer.whatIsTheObject = objectName;
        newObjectInTransfer.objectBytes = new byte[maxLen];
        incomingObjects.Add(newObjectInTransfer);
        return newObjectInTransfer;
    }

    public MySocket FindSocketByPlayerID(string playerID)
    {
       
        foreach (MySocket socket in clients)
        {
            if (socket.PlayerID == playerID)
            {
                return socket;
            }
        }
        return null;
    }

    public void ChatToChannel(string chatText, int channelNr) {
        testMessage = chatText + Environment.NewLine;
        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.LOBBY_CHAT_MESSAGE, testMessage,""));
    }

        /// <summary>
        /// Serveriga ühenduv thread, mis jääb peale edukat ühendamist serverikäske ootama
        /// </summary>
        void JoinTo()
        {
        try
        {
            ip = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
        }
        catch (Exception e)
        {

            GameEngine.ActiveGame.MultiplayerUICommands.Add(new MultiplayerUICommand(MultiplayerUICommand.LOBBY_DISPLAY_CONNECTION_FAIL, e.Message));

            return;
            
        }
         

            if (serverSocket != null) { 
                
            }

            serverSocket = new MySocket();

            serverSocket.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //questionable? comp name would be of the joining
            serverSocket.ComputerName = System.Environment.MachineName;

        ////TODO Korog 4.07.2014 serversocketi info peaks saama serveri käest. 
        //if (game.GeneralOption.MultiplayerName == "")
        //    {
        //        serverSocket.PlayerName = System.Environment.MachineName;
                 
        //    }else {

        //        serverSocket.PlayerName = game.GeneralOption.MultiplayerName;
        //    }

            serverSocket.ID_Code = System.Environment.MachineName;
           

            //this.game.NewGameWindow.ConnectToServersWindow.Close();

            //this.game.NewGameWindow.startToConnect();

            connectionEndedNormally = false;

            bool connectedSuccessfully = false;

            try
            {
                serverSocket.Socket.Connect(ip);

                connectedSuccessfully = true;
                serverOn = false;

                ServerAddress serverAddress = new ServerAddress();
                serverAddress.IpAddress = serverIp;
                serverAddress.Port = serverPort;
                serverAddress.ServerName = "Rock"; //TODO Ajutine lahendus

                // Saving and adding new server address to list of servers
                //if (this.game.GeneralOption.addIfNewServer(serverAddress)) 
                //{
                //    GameUtilities.save(this.game.GeneralOption, this.game.optionFolderPath, GeneralOptions.XMLTEXTFILE);
                //}

                //this.game.NewGameWindow.connectedToServer();
                ChatToChannel("Connected to " + serverAddress.IpAddress,1);
            UnityEngine.Debug.Log("Connected to " + serverAddress.IpAddress);
            // sending handshake
            //message used to be: "game.GeneralOption.MultiplayerName" but now using message as password
            MultiplayerMessage mpMessage = new MultiplayerMessage(MultiplayerMessage.HS_MyNameIs, System.Environment.MachineName, GameEngine.ActiveGame.PasswordUsed.ToString()); //TODO change the multiplayername
            SendToServerSocket(mpMessage);
            //broadCastToAll(mpMessage);


                while (true) // TODO bool
                {
                    connectionEndedNormally = recieveCommandFromServer(serverSocket, serverAddress);

                    if (connectionEndedNormally || stopThread) break; //adding stopThread variable to close the stuff correctly when exiting app
            }

            }
            catch
            {

            }
        UnityEngine.Debug.Log("client closing");
        if (connectedSuccessfully)
            {

                if (!connectionEndedNormally)
                {
                    ChatToChannel("Connection to server ended abruptly", 1);

                }
                else
                {

                    ChatToChannel("Connection closed successfully", 1);

                }
            }
            else {

                ChatToChannel("Could not connect to server, try again..", 1);
            }

            //there can be insignificant errors here that dont impact anything, but are annoying to see nonetheless: ObjectDisposedException Cannot access a disposed object
        try
        {
            this.CloseSockets("Connection closed successfully");

        } catch { }



        } // end  void JoinTo()

        /// <summary>
        /// Loob serveri, ootamaks teist mängijat.
        /// </summary>
        public void CreateServer()
        {
        UnityEngine.Debug.Log("CreateServer call");
            serverOn = true;
        GameEngine.ActiveGame.serverON = true;
            Thread listen = new Thread(new ThreadStart(HandleServer));
            listen.Name = "Server Thread";
        GameEngine.ActiveGame.threadController.threadsControl.Add(listen);
        listen.IsBackground = true;
            listen.Start();
        //didnt work
        //ip = new IPEndPoint(IPAddress.Any,6321);
        ////addition to allow server to be host
        //serverSocket = new MySocket();

        //serverSocket.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        ////questionable? comp name would be of the joining
        //serverSocket.ComputerName = System.Environment.MachineName;
        //serverSocket.Socket.Connect(ip);
    }

       
        public void mySocketCleanUp() {

            List<MySocket> newList = new List<MySocket>();

            foreach (MySocket mySocket in this.clients) {
                if (mySocket.Socket != null) {
                    newList.Add(mySocket);
                }
            }

            this.clients = newList;
        }

        public void sendMessage(MySocket mySocket, byte[] message)
        {

            try
            {
                mySocket.Socket.Send(message, message.Length, SocketFlags.None);
            }
            catch
            {
                if (mySocket.Socket != null) {

                if (mySocket.Socket.Connected)
                {
                    mySocket.Socket.Shutdown(SocketShutdown.Both);
                }
                   
                    mySocket.Socket.Close();
                    mySocket.Socket = null;
                

                }
               
            }
        
        }


        //note, this function does not handle closed connections in the middle of a message...
        static byte[] ReadMessage(Socket socket)
        {
            byte[] sizeinfo = new byte[4];

            //read the size of the message
            int totalread = 0, currentread = 0;

            currentread = totalread = socket.Receive(sizeinfo);

            while (totalread < sizeinfo.Length && currentread > 0)
            {
                currentread = socket.Receive(sizeinfo,
                          totalread, //offset into the buffer
                          sizeinfo.Length - totalread, //max amount to read
                          SocketFlags.None);

                totalread += currentread;
            }

            int messagesize = BitConverter.ToInt32(sizeinfo, 0);

            //create a byte array of the correct size
            //note:  there really should be a size restriction on
            //              messagesize because a user could send
            //              Int32.MaxValue and cause an OutOfMemoryException
            //              on the receiving side.  maybe consider using a short instead
            //              or just limit the size to some reasonable value
            byte[] data = new byte[messagesize];

            //read the first chunk of data
            totalread = 0;
            currentread = totalread = socket.Receive(data,
                         totalread, //offset into the buffer
                        data.Length - totalread, //max amount to read
                        SocketFlags.None);

            //if we didn't get the entire message, read some more until we do
            while (totalread < messagesize && currentread > 0)
            {
                currentread = socket.Receive(data,
                         totalread, //offset into the buffer
                        data.Length - totalread, //max amount to read
                        SocketFlags.None);
                totalread += currentread;
            }

            return data;
        }

        // returns true if connection was closed politely by server
        public bool recieveCommandFromServer(MySocket serverSocket, ServerAddress serverAddress) {
        if (serverSocket == null) //if server socket = null, then means you are disconnected, therefore the client chills out
        {
            return true;
        }
        byte[] commandFromServer = Multiplayer.ReadMessage(serverSocket.Socket);

        //START deserialize
        XmlSerializer xml = new XmlSerializer(typeof(MultiplayerMessage));

        MultiplayerMessage multiplayerMessage = null;
        try
        {
            multiplayerMessage = (MultiplayerMessage)xml.Deserialize(new MemoryStream(commandFromServer));
        }
        catch (Exception e)
        {
            if (stopThread)
            {
                return false;
            }
            //UnityEngine.Debug.Log("client failed to deserialize message from server, error: " + e.Message + ", requesting server to resend the message again");
            UnityEngine.Debug.Log("client resend request inserted into list");
            GameEngine.ActiveGame.clientManager.InsertToFirst(new MultiplayerMessage(MultiplayerMessage.ResendRequest, "", ""));
            return false;
        }
        switch (multiplayerMessage.Command)
        {
            case MultiplayerMessage.ToggleBlockMode:
                GameEngine.ActiveGame.ToggleBlockMode(Int32.Parse(multiplayerMessage.Argument), multiplayerMessage.Message);
                break;
            case MultiplayerMessage.ResolveEventCommand:
                GameEngine.ActiveGame.ResolveEventCommand(multiplayerMessage.Argument);
                break;
            case MultiplayerMessage.SetPlayerAsAI:
                GameEngine.ActiveGame.SetPlayerAsAI(multiplayerMessage.Argument);
                break;
            case MultiplayerMessage.SetPlayerAsAutoBattle:
                GameEngine.ActiveGame.SetPlayerAutoBattle(multiplayerMessage.Argument, multiplayerMessage.Message,isHost);
                break;
            case MultiplayerMessage.LoadGame:
                GameEngine.ActiveGame.optionPanel.isLoading = true; //hopefully can do that without UI commands?
                break;
            case MultiplayerMessage.ResolvedEventClaim:
                GameEngine.ActiveGame.ResolveEventClaim(multiplayerMessage.Argument, multiplayerMessage.Message, false);
                break;
            case MultiplayerMessage.ClaimEvent:
                if (isHost)
                {
                    UnityEngine.Debug.Log("ClaimEvent is processed by isHost");
                    GameEngine.ActiveGame.ResolveEventClaim(multiplayerMessage.Argument, multiplayerMessage.Message,true);
                }
             
                break;
            case MultiplayerMessage.ContinueDungeon:
                GameEngine.ActiveGame.ContinuePlayerDungeon(multiplayerMessage.Argument);
                break;
            case MultiplayerMessage.RemovePreferredItem:
                GameEngine.ActiveGame.AddOrRemovePreferredItem(multiplayerMessage.Argument, multiplayerMessage.Message, false);
                break;
            case MultiplayerMessage.AddPreferredItem:
                GameEngine.ActiveGame.AddOrRemovePreferredItem(multiplayerMessage.Argument, multiplayerMessage.Message,true);
                break;
            case MultiplayerMessage.SkillClick:
                GameEngine.ActiveGame.SkillClick(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.DeclareEnemy:
                GameEngine.ActiveGame.DeclareEnemy(multiplayerMessage.Argument,multiplayerMessage.Message);
                break;
            case MultiplayerMessage.OfferPeace:
                GameEngine.ActiveGame.OfferPeace(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.AcceptPeace:
                GameEngine.ActiveGame.AcceptPeace(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.Retreat:
                GameEngine.ActiveGame.Retreat(multiplayerMessage.Argument,multiplayerMessage.Message);
                break;
            case MultiplayerMessage.SkillTargetClick:
                GameEngine.ActiveGame.SkillTargetClick(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.CancelSkillButtonClick:
                GameEngine.ActiveGame.CancelSkillButtonClick(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.UnitMove:
                GameEngine.ActiveGame.CombatUnitMove(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.NextCombatTurnClick:
                GameEngine.ActiveGame.RefreshActiveUnits(multiplayerMessage.Argument);
                break;
            case MultiplayerMessage.SetBuildingMode:
                GameEngine.ActiveGame.SetBuildingMode(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.SetBuildingProductionToAI:
                GameEngine.ActiveGame.SetBuildingProductionToAI(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.StartObserving:
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.COMMAND_TURN_INTO_OBSERVER));
                break;
            case MultiplayerMessage.ProceedToAfterBattles:
                GameEngine.ActiveGame.ProceedToAfterBattles();
                break;
            case MultiplayerMessage.StartAfterBattleProcessingForPlayer:

                GameEngine.ActiveGame.AfterBattleProcessForPlayer(multiplayerMessage.Argument);
                break;
            case MultiplayerMessage.SetPlayerGameStateToAfterBattle:
                GameEngine.ActiveGame.SetPlayerGameState(multiplayerMessage.Argument,GameState.State.AFTER_BATTLE_PHASE);
                if (isHost)
                {
                    GameEngine.ActiveGame.CheckIfAllFinishedCombat();
                }
                break;
            case MultiplayerMessage.SetArmyToAttack:
                GameEngine.ActiveGame.SetArmyToAttack(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.SetArmyToStopAttacking:
                GameEngine.ActiveGame.SetArmyToStopAttacking(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.PlaceBid:
                GameEngine.ActiveGame.PlaceBid(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.SelectLevelUpChoice:
                GameEngine.ActiveGame.SelectLevelUpChoice(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.CancelCraftItem:
                GameEngine.ActiveGame.CancelCraftItem(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.PlaceItemBeforeFirst:
                GameEngine.ActiveGame.RecipeItemPlacement(multiplayerMessage.Argument, multiplayerMessage.Message,1);
                break;
            case MultiplayerMessage.PlaceInBetweenItems:
                GameEngine.ActiveGame.RecipeItemPlacement(multiplayerMessage.Argument, multiplayerMessage.Message, 3);
                break;
            case MultiplayerMessage.PlaceItemLast:
                GameEngine.ActiveGame.RecipeItemPlacement(multiplayerMessage.Argument, multiplayerMessage.Message, 2);
                break;
            case MultiplayerMessage.StartRecipe:
                GameEngine.ActiveGame.StartRecipe(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.DismissNotification:
                GameEngine.ActiveGame.DismissNotification(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.AcceptTradeOffer:
                GameEngine.ActiveGame.AcceptTradeOffer(multiplayerMessage.Argument,multiplayerMessage.Message);
                break;
            case MultiplayerMessage.CancelQuestParty:
                GameEngine.ActiveGame.CancelQuestParty(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.SetItemForAuction:
                GameEngine.ActiveGame.AuctionItem(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.CancelAuctionOrTradeItem:
                GameEngine.ActiveGame.CancelAuctionOrTradeItem(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.CancelledBid:
                GameEngine.ActiveGame.CancelBid(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.CreateEmptyOfferBid:
                GameEngine.ActiveGame.CreateEmptyOfferBid(multiplayerMessage.Argument,multiplayerMessage.Message);
                break;
            case MultiplayerMessage.ProceedToEndTurn:
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.PROCEED_TO_END_TURN));
                break;
            case MultiplayerMessage.NullEntityMission:
                GameEngine.ActiveGame.NullifyEntityMission(multiplayerMessage.Argument);
                break;
            case MultiplayerMessage.PurchaseItem:
                GameEngine.ActiveGame.PurchaseItem(multiplayerMessage.Argument,multiplayerMessage.Message);
                break;
            case MultiplayerMessage.SetItemForTrade:
                GameEngine.ActiveGame.SetItemForTrade(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.ProceedToStartTurn:
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.START_TURN));

                break;
            case MultiplayerMessage.Test:
               
                break;
            case MultiplayerMessage.ReadyForStartGlobalTurn:
                if (GameEngine.ActiveGame.isHost)
                {
                    GameEngine.ActiveGame.clientsReadyToStartTurn.Add(multiplayerMessage.Argument);
                    int playerCount = Int32.Parse(multiplayerMessage.Message);
                    if (GameEngine.ActiveGame.clientsReadyToStartTurn.Count >= playerCount) //multiplayerMessage.Message = how many clients 
                    {
                        GameEngine.ActiveGame.clientsReadyToStartTurn.Clear();
                        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.STOP_OBSERVER_MODE_TO_ALL));
                    }
                    UnityEngine.Debug.Log("ReadyForStartGlobalTurn plr count " + playerCount + " submitted by " + multiplayerMessage.Argument);
                }
                break;
            case MultiplayerMessage.UpdateQuestPartyProgress:
                UnityEngine.Debug.Log("MP UpdateQuestPartyProgress");
                GameEngine.ActiveGame.UpdateQuestPartyProgress(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.DisbandQuestArmies:
                UnityEngine.Debug.Log("MP DisbandQuestArmies");
                GameEngine.ActiveGame.DisbandQuestArmies(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;

            case MultiplayerMessage.CreateOverlandEntity:
                UnityEngine.Debug.Log("MP CreateOverlandEntity");
                GameEngine.ActiveGame.CreateOverlandEntity(multiplayerMessage.Argument,multiplayerMessage.Message);
                break;
            case MultiplayerMessage.CreateEntitySkill:
                GameEngine.ActiveGame.AddEntitySkill(multiplayerMessage.Argument, multiplayerMessage.Message,false);
                break;
            case MultiplayerMessage.CreateEntitySkillInCombat:
                GameEngine.ActiveGame.AddEntitySkill(multiplayerMessage.Argument, multiplayerMessage.Message, true);
                break;
            case MultiplayerMessage.RemoveEntitySkill:
                UnityEngine.Debug.Log("MP RemoveEntitySkill");
                GameEngine.ActiveGame.RemoveEntitySkill(multiplayerMessage.Argument,multiplayerMessage.Message);
                break;
            case MultiplayerMessage.RemoveEventChain:
                UnityEngine.Debug.Log("MP RemoveEventChain");
                GameEngine.ActiveGame.RemoveEventChain(multiplayerMessage.Argument);
                break;
            case MultiplayerMessage.AddEvent:
                UnityEngine.Debug.Log("MP AddEvent");
                GameEngine.ActiveGame.AddToEvents(multiplayerMessage.Argument, multiplayerMessage.Message, false);
                break;
            case MultiplayerMessage.AddInitializedEvent:
                UnityEngine.Debug.Log("MP AddInitializedEvent");
                GameEngine.ActiveGame.AddToEvents(multiplayerMessage.Argument, multiplayerMessage.Message,true);
                break;
            case MultiplayerMessage.ProceedToMainPhase:
                UnityEngine.Debug.Log("MP ProceedToMainPhase");
                if (!GameEngine.ActiveGame.isHost) //after events we are re-synchronizing the game engine random
                {
                    //GameEngine.random = new MyRandom(Int32.Parse(multiplayerMessage.Argument), Int32.Parse(multiplayerMessage.Message));
                }
                GameEngine.ActiveGame.ProceedToMainPhase();
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.PROCEED_TO_MAIN_PHASE));
                break;
            case MultiplayerMessage.GenerateScenario:
                int seed = Int32.Parse(multiplayerMessage.Argument);
                GameEngine.ActiveGame.StartScenarioThread(seed);
                break;
            case MultiplayerMessage.SetGameState:
                GameEngine.ActiveGame.SetPlayerGameState(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.SubmitMainPhase:
                UnityEngine.Debug.Log("MP SubmitMainPhase");
                if (GameEngine.ActiveGame.isHost)
                {
                    GameEngine.ActiveGame.SubmitMainPhase(multiplayerMessage.Argument);
                }
                break;
            case MultiplayerMessage.SendRandom:
                int randomSeed = Int32.Parse(multiplayerMessage.Argument);
                Random random = new Random(randomSeed);
                UnityEngine.Debug.Log("client random: " + random.Next(15, 65));
             
                break;
            case MultiplayerMessage.SendRandomAndUICommand:
                string[] seedAndIteration = multiplayerMessage.Message.Split('*');
                MultiplayerUICommand randomRelatedUICommand = new MultiplayerUICommand(multiplayerMessage.Argument,seedAndIteration[0], seedAndIteration[1]);
                GameEngine.ActiveGame.AddToUICommands(randomRelatedUICommand);
                UnityEngine.Debug.Log("SendRandomAndUICommand recieved: " + multiplayerMessage.Argument + " seed " + seedAndIteration + " iteration " + seedAndIteration);
                break;
            case MultiplayerMessage.HS_MyNameIs:
                    
                serverAddress.ServerName = multiplayerMessage.Message;
                serverSocket.ComputerName = multiplayerMessage.Message;
                serverSocket.ID_Code = multiplayerMessage.Argument;
                ChatToChannel(multiplayerMessage.Message,1);
                //if (this.game.GeneralOption.addIfNewServer(serverAddress)) 
                //{
                //    GameUtilities.save(this.game.GeneralOption, this.game.optionFolderPath, GeneralOptions.XMLTEXTFILE);
                //}

                break;
            case MultiplayerMessage.DisableOptionPanelUI:
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.LOBBY_DISABLE_OPTION_PANEL_UI));

                break;
            case MultiplayerMessage.DisableEndTurnUI:
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.DISABLE_END_TURN_UI,multiplayerMessage.Message));
                break;
            case MultiplayerMessage.ReleaseEndTurnUI:
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.RELEASE_END_TURN_UI,multiplayerMessage.Message));
                break;
            case MultiplayerMessage.SetProductionLineValue:
                GameEngine.ActiveGame.SetProductionLineValue(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.SubmitEndTurn:
                if (isHost)
                {
                    GameEngine.ActiveGame.SubmitEndTurn(multiplayerMessage.Argument);
                    UnityEngine.Debug.Log("subitendturn host recieve");
                }
                break;
            case MultiplayerMessage.SendScenarioTask: //only host gets this message
                GameEngine.ActiveGame.AddHostTask(multiplayerMessage.Argument, TaskStatus.TYPE_SEND_SCENARIO);
                break;
            case MultiplayerMessage.CreatePlayerController:
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.LOBBY_HIDE_OPTIONS_PANEL_CREATE_PLAYER_CONTROLLER));
                break;
            case MultiplayerMessage.OptionChange:
                //argument = option keyword, message = new input box value
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.LOBBY_CHANGE_OPTION_VALUE,multiplayerMessage.Argument,multiplayerMessage.Message));
                break;
            case MultiplayerMessage.OpenOptionsPanel:
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.LOBBY_SHOW_OPTIONS_PANEL,multiplayerMessage.Argument));
                ChatToChannel("OpenOptionsPanel client side reached", 1);
                break;
            case MultiplayerMessage.PlayersCount: //argument = player count
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.LOBBY_CHANGE_PLAYER_COUNT,multiplayerMessage.Argument));
                break;

            case MultiplayerMessage.Bye:
                ChatToChannel("Server has closed the connection: " + multiplayerMessage.Message, 1);
                return true;

            case MultiplayerMessage.ChatMessage:
                //send the text content to UI related variable
                ChatToChannel(multiplayerMessage.Argument, multiplayerMessage.Message);
                break;
            case MultiplayerMessage.UpdateTaskStatusOutput:
            case MultiplayerMessage.UpdateTaskStatusOutputToAll:
            case MultiplayerMessage.UpdateTaskStatusOutputToAllExceptSender:
                GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT,multiplayerMessage.Message));
                break;       

            case MultiplayerMessage.Ping:
                //nothing, response ping
                break;
            case MultiplayerMessage.UnAssignPlayerID:
                GameEngine.ActiveGame.UnAssignPlayerID(multiplayerMessage.Argument);
                break;
            case MultiplayerMessage.AssignPlayerID:
                //set in UI whos assigned player
                GameEngine.ActiveGame.AssignPlayerIDToNewGame(multiplayerMessage.Argument, multiplayerMessage.Message);


                break;
            case MultiplayerMessage.OptionColRequest:
                string optionColCheck = "req Not host,";
                if (isHost) //if this is the host, we send over the option collection to server
                {
                    optionColCheck = "RequestingOptionCollection ishost reached";
                    ////the message that server will read to send to the argument(the player that requested the collection)
                    //MultiplayerMessage optionColMessage = new MultiplayerMessage(MultiplayerMessage.Sending_Object,MultiplayerMessage.UPDATE_OPTIONCOLLECTION, multiplayerMessage.Argument);
                    ////sending in the message(header) and the collection itself
                    ////we send only the data of the collection to avoid sending useless data such as constants, which are created anyway on the recieving end
                    //GameEngine.ActiveGame.clientManager.PushMultiplayerObject(optionColMessage,GameEngine.ActiveGame.optionPanel.optionCollection.getAsOptionList());
                    GameEngine.ActiveGame.AddHostTask(multiplayerMessage.Argument,TaskStatus.TYPE_SEND_OPTION_COLLECTION);
                }
                ChatToChannel(optionColCheck, 1);
                break;
            case MultiplayerMessage.UpdateTask:
                if (isHost)
                {
                    GameEngine.ActiveGame.UpdateTask(multiplayerMessage);
                }
            
                break;
            // warns that object is coming from the server
            case MultiplayerMessage.Sending_Object:
               // GameEngine.ActiveGame.clientManager.mode = ClientManager.MODE_LISTENING; //probably remove
                string objectType = multiplayerMessage.Argument;
                string notes = multiplayerMessage.Message;
                recieveCommandObjectFromSocket(serverSocket, objectType, notes, true);
                break;

        }
        if (multiplayerMessage.IsThereMoreStuffOnQueue)
        {
            GameEngine.ActiveGame.clientManager.mode = ClientManager.MODE_SENDING; //in order to avoid waiting for nothing, we send stuff back and forth
            GameEngine.ActiveGame.clientManager.forcePinging = true;
        }
        else
        {
            GameEngine.ActiveGame.clientManager.mode = ClientManager.MODE_IDLE; //no more messages queued up, so idle with pings
            GameEngine.ActiveGame.clientManager.forcePinging = false;
        }
     
        return false;
           

        }
    /// <summary>
    /// server function for processing objects sent to server by client
    /// </summary>
    /// <param name="senderMySocket"></param>
    /// <param name="objectTypeAsString"></param>
    /// <param name="notes"></param>
    /// <param name="messageFromWhatClient"></param>
    public void RecieveObjectFromClient(MySocket senderMySocket, string objectTypeAsString, string notes, string messageFromWhatClient)
    {
        Socket senderSocket = senderMySocket.Socket;

        Type dataType = typeof(MultiplayerMessage);

        switch (objectTypeAsString)
        {
            case MultiplayerMessage.EventSavedData:
                dataType = typeof(EventSavedData);
                break;
            case MultiplayerMessage.AddEventBattle:
                dataType = typeof(EventBattleMultiplayerData);
                break;
            case MultiplayerMessage.PayShopUnitUpkeep:
                dataType = typeof(OurStatList);
                break;
            case MultiplayerMessage.SendLootResolveResult:
            case MultiplayerMessage.ClaimLoot:
                dataType = typeof(OverlandLootClaim);
                break;
            case MultiplayerMessage.StackItems:
            case MultiplayerMessage.SplitItemStack:
            case MultiplayerMessage.SpreadItems:
                dataType = typeof(SourceInfo);
                break;
            case MultiplayerMessage.TransactionInfo:
                dataType = typeof(TransactionInfo);
                break;
            case MultiplayerMessage.SetEntityMission:
                dataType = typeof(Mission);
                break;
            case MultiplayerMessage.ItemTransfer:
                dataType = typeof(MultiplayerItemTransfer);
                break;
            case MultiplayerMessage.AddQuestLootToEventsInv:
                dataType = typeof(OddsAndRandom);
                break;
            case MultiplayerMessage.TestScenario:
                dataType = typeof(CompressedBytes);
                break;
            case MultiplayerMessage.UPDATE_OPTIONCOLLECTION:
                dataType = typeof(VersionedOptions);
                break;
            case MultiplayerMessage.UPDATE_OPTIONCOLLECTION_TEST:
                dataType = typeof(OptionCollection);
                break;
            case MultiplayerMessage.SubmitScenarioToServer:
                dataType = typeof(CompressedBytes);
                break;
            case MultiplayerMessage.ArmyMove:
                dataType = typeof(ArmyMovementInfo);
                break;
            case MultiplayerMessage.SendRandom:
                dataType = typeof(string);
                break;
            default:
                System.Console.WriteLine("Unknown command in Multiplayer.recieveCommandObjectFromSocket: " + dataType.ToString());
                break;

        }


        byte[] commandFromServer = Multiplayer.ReadMessage(senderSocket);

        //START deserialize

        XmlSerializer xml = new XmlSerializer(dataType);
        MultiplayerMessage multiplayerMessage = null;
        MultiplayerMessageObject multiplayerMessageObject = null;
        switch (objectTypeAsString)
        {
            case MultiplayerMessage.EventSavedData:
                try
                {
                    EventSavedData eventSavedData = (EventSavedData)xml.Deserialize(new MemoryStream(commandFromServer));
                
                    multiplayerMessageObject = new MultiplayerMessageObject();
                    multiplayerMessageObject.msg = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.EventSavedData, notes);
                    multiplayerMessageObject.obj = eventSavedData;
                    PushMessageToAllExceptOneClientQueues(multiplayerMessageObject, messageFromWhatClient);
                    PushPingToClient(messageFromWhatClient);
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError(e.Message + " EventSavedData error " + e.StackTrace);
                }
                break;
            case MultiplayerMessage.AddEventBattle:
                try
                {
                    EventBattleMultiplayerData eventBattleData = (EventBattleMultiplayerData)xml.Deserialize(new MemoryStream(commandFromServer));
                    multiplayerMessageObject = new MultiplayerMessageObject();
                    multiplayerMessageObject.msg = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.AddEventBattle, notes);
                    multiplayerMessageObject.obj = eventBattleData;
                    PushMessageToAllExceptOneClientQueues(multiplayerMessageObject, messageFromWhatClient);
                    PushPingToClient(messageFromWhatClient);
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError(e.Message + " in server PayShopUnitUpkeep " + e.StackTrace);
                }
                break;
            case MultiplayerMessage.PayShopUnitUpkeep:
                try
                {
                    OurStatList upkeepToPay = (OurStatList)xml.Deserialize(new MemoryStream(commandFromServer));
                    multiplayerMessageObject = new MultiplayerMessageObject();
                    multiplayerMessageObject.msg = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.PayShopUnitUpkeep, notes);
                    multiplayerMessageObject.obj = upkeepToPay;
                    PushMessageToAllExceptOneClientQueues(multiplayerMessageObject, messageFromWhatClient);  
                    PushPingToClient(messageFromWhatClient);  
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError(e.Message + " in server PayShopUnitUpkeep " + e.StackTrace);
                }
                break;
            case MultiplayerMessage.SendLootResolveResult:
                try
                {
                    OverlandLootClaim overlandLootClaim = (OverlandLootClaim)xml.Deserialize(new MemoryStream(commandFromServer));
                    multiplayerMessageObject = new MultiplayerMessageObject();
                    multiplayerMessageObject.msg = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.SendLootResolveResult, notes);
                    multiplayerMessageObject.obj = overlandLootClaim;
                    PushMessageToAllExceptOneClientQueues(multiplayerMessageObject,messageFromWhatClient); //sending to all, because even host must get this
                    PushPingToClient(messageFromWhatClient); //pinging back the host
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError(e.Message + " in server claimloot");
                }
                break;
            case MultiplayerMessage.ClaimLoot:
                try
                {
                    OverlandLootClaim overlandLootClaim = (OverlandLootClaim)xml.Deserialize(new MemoryStream(commandFromServer));
                    multiplayerMessageObject = new MultiplayerMessageObject();
                    multiplayerMessageObject.msg = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.ResolveLoot, notes);
                    multiplayerMessageObject.obj = overlandLootClaim;
                    PushMessageToAllClientQueues(multiplayerMessageObject); //sending to all, because even host must get this
                   
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError(e.Message + " in server claimloot");
                }
                break;
            case MultiplayerMessage.StackItems:
            case MultiplayerMessage.SplitItemStack:
            case MultiplayerMessage.SpreadItems:
                try
                {
                    SourceInfo itemStackingSourceInfo = (SourceInfo)xml.Deserialize(new MemoryStream(commandFromServer));
                    multiplayerMessageObject = new MultiplayerMessageObject();
                    multiplayerMessageObject.msg = new MultiplayerMessage(MultiplayerMessage.Sending_Object, objectTypeAsString, notes);
                    multiplayerMessageObject.obj = itemStackingSourceInfo;
                    PushMessageToAllExceptOneClientQueues(multiplayerMessageObject, messageFromWhatClient);
                    PushPingToClient(messageFromWhatClient);
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError(e.Message + " in server " + objectTypeAsString);
                }
          
                break;
            case MultiplayerMessage.TransactionInfo:
                try
                {
                    TransactionInfo transactionInfo = (TransactionInfo)xml.Deserialize(new MemoryStream(commandFromServer));
                    PushPingToClient(messageFromWhatClient);
                    multiplayerMessageObject = new MultiplayerMessageObject();
                    multiplayerMessageObject.msg = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.TransactionInfo, notes);
                    multiplayerMessageObject.obj = transactionInfo;
                    PushMessageToAllExceptOneClientQueues(multiplayerMessageObject, messageFromWhatClient);
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError("server failed to recieve transaction info: " + e.Message);
                }

                break;
            case MultiplayerMessage.SetEntityMission:
                try
                {
                    Mission mission= (Mission)xml.Deserialize(new MemoryStream(commandFromServer));
                    PushPingToClient(messageFromWhatClient);
                    multiplayerMessageObject = new MultiplayerMessageObject();
                    multiplayerMessageObject.msg = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.SetEntityMission, notes);
                    multiplayerMessageObject.obj = mission;
                    PushMessageToAllExceptOneClientQueues(multiplayerMessageObject, messageFromWhatClient);
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError("server failed to recieve Entity mission: " + e.Message);
                }

                break;

            case MultiplayerMessage.ItemTransfer:
                try
                {
                    MultiplayerItemTransfer itemTransfer = (MultiplayerItemTransfer)xml.Deserialize(new MemoryStream(commandFromServer));
                    PushPingToClient(messageFromWhatClient);
                    multiplayerMessageObject = new MultiplayerMessageObject();
                    multiplayerMessageObject.msg = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.ItemTransfer, notes);
                    multiplayerMessageObject.obj = itemTransfer;
                    PushMessageToAllExceptOneClientQueues(multiplayerMessageObject, messageFromWhatClient);
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError("server failed to recieve AddQuestLootToEventsInv: " + e.Message);
                }

                break;
            case MultiplayerMessage.AddQuestLootToEventsInv:
                try
                {
                    OddsAndRandom oddsAndRandom = (OddsAndRandom)xml.Deserialize(new MemoryStream(commandFromServer));
                    PushPingToClient(messageFromWhatClient);
                    multiplayerMessageObject = new MultiplayerMessageObject();
                    multiplayerMessageObject.msg = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.AddQuestLootToEventsInv, notes);
                    multiplayerMessageObject.obj = oddsAndRandom;
                    PushMessageToAllExceptOneClientQueues(multiplayerMessageObject, messageFromWhatClient);
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError("server failed to recieve AddQuestLootToEventsInv: " + e.Message);
                }

                break;
            case MultiplayerMessage.SendRandom:
                try
                {
                    string Compressedrandom = (string)xml.Deserialize(new MemoryStream(commandFromServer));
                    multiplayerMessageObject = new MultiplayerMessageObject();
                    multiplayerMessageObject.msg = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.SendRandom, "");
                    multiplayerMessageObject.obj = Compressedrandom;
                    PushMessageToAllExceptOneClientQueues(multiplayerMessageObject, messageFromWhatClient);
                    PushPingToClient(messageFromWhatClient);
                    UnityEngine.Debug.Log("random reached server");
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError("server failed to recieve random: " + e.Message);
                }
              
                break;
            case MultiplayerMessage.ArmyMove:
                try
                {
                    ArmyMovementInfo armyMovement = (ArmyMovementInfo)xml.Deserialize(new MemoryStream(commandFromServer));
                    PushPingToClient(messageFromWhatClient);
                    multiplayerMessageObject = new MultiplayerMessageObject();
                    multiplayerMessageObject.msg = new MultiplayerMessage(MultiplayerMessage.Sending_Object,MultiplayerMessage.ArmyMove,"");
                    multiplayerMessageObject.obj = armyMovement;
                    PushMessageToAllExceptOneClientQueues(multiplayerMessageObject,messageFromWhatClient);
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError("server failed to recieve armyMovementInfo: " + e.Message);
                }
              
                break;
            case MultiplayerMessage.SubmitScenarioToServer:
                CompressedBytes scenario = null;

                try
                {
                    UnityEngine.Debug.Log("scenario recieve start");
                    scenario = (CompressedBytes)xml.Deserialize(new MemoryStream(commandFromServer));
                    scenarioObj = scenario; //saving the scenario so we wont have to send it over for each player
                                            //after recieving the object into the multiplayer, start tasks for each player that isnt host(and send the task requests to host)
                   
                    foreach (MySocket client in clients)
                    {
                        if (client.ComputerName == messageFromWhatClient) //skip host
                        {
                            continue;
                        }
                        MultiplayerMessage startScenarioTaskMsg = new MultiplayerMessage(MultiplayerMessage.SendScenarioTask, client.ComputerName,"");
                        PushMessageToClientQueue(messageFromWhatClient,startScenarioTaskMsg);
                    }
                    UnityEngine.Debug.Log("scenario recieve end");
                    ChatToChannel("scenario recieved by server", 0);
                }
                catch
                {

                    ChatToChannel("FAILED  TO SERIALIZE SCENARIO",0);
                }

                break;
            //we recieve test mapcoordinates, which should be re-sent to all players
            case MultiplayerMessage.TestScenario:
                CompressedBytes mapCoordinates = null;

                try
                {
                    mapCoordinates = (CompressedBytes)xml.Deserialize(new MemoryStream(commandFromServer));

                    MapCoordinates decompressedCoords = (MapCoordinates)ObjectByteConverter.ByteArrayToObject(mapCoordinates.obj);

                    ChatToChannel("", "map coordinates recieved by server:  " + decompressedCoords.XCoordinate + " " + decompressedCoords.YCoordinate);

                }
                catch(Exception e)
                {
                    ChatToChannel("", "Error tranfering map coordinates from client " + e.Message);
                }


                if (mapCoordinates != null)
                {
                    multiplayerMessageObject = new MultiplayerMessageObject();

                    multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.TestScenario, "");
                    multiplayerMessageObject.msg = multiplayerMessage;
                    multiplayerMessageObject.obj = mapCoordinates;
                    PushMessageToAllClientQueues(multiplayerMessageObject);
                    //multiplayerMessageObject = new MultiplayerMessageObject();
                    //multiplayerMessageObject.obj = mapCoordinates;
                    //PushMessageToAllClientQueues(multiplayerMessageObject);


                }

                break;
            #region option collection test
            //server recieve the collection object, now sending it to the "notes" which is the computer name that requested the collection
            case MultiplayerMessage.UPDATE_OPTIONCOLLECTION_TEST:
                VersionedOptions optionCollection2 = null; //get to the sending of the option collection interaction for explanation why versioned options

                try
                {
                    optionCollection2 = (VersionedOptions)xml.Deserialize(new MemoryStream(commandFromServer));

                    //ChatToChannel("", "map coordinates recieved by server:  " + mapCoordinates.XCoordinate + mapCoordinates.YCoordinate);

                }
                catch
                {
                    //ChatToChannel("", "Error tranfering map coordinates from client");
                }


                if (optionCollection2 != null)
                {
                    multiplayerMessageObject = new MultiplayerMessageObject();

                    multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.UPDATE_OPTIONCOLLECTION, "");
                    multiplayerMessageObject.msg = multiplayerMessage;
                    multiplayerMessageObject.obj = optionCollection2;
                
                    PushMessageToAllExceptOneClientQueues( multiplayerMessageObject, notes);
                 
                    ChatToChannel("Option collection TEST reached server, sending to  " + notes, 1);
                }

                break;
            #endregion option collection test
            #region option collection
            //server recieve the collection object, now sending it to the "notes" which is the computer name that requested the collection
            case MultiplayerMessage.UPDATE_OPTIONCOLLECTION:
                VersionedOptions optionCollection = null; //get to the sending of the option collection interaction for explanation why versioned options

                try
                {
                    optionCollection = (VersionedOptions)xml.Deserialize(new MemoryStream(commandFromServer));

                    //ChatToChannel("", "map coordinates recieved by server:  " + mapCoordinates.XCoordinate + mapCoordinates.YCoordinate);

                }
                catch
                {
                    //ChatToChannel("", "Error tranfering map coordinates from client");
                }


                if (optionCollection != null)
                {
                    multiplayerMessageObject = new MultiplayerMessageObject();
                 
                    multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.UPDATE_OPTIONCOLLECTION, "");
                    multiplayerMessageObject.msg = multiplayerMessage;
                    multiplayerMessageObject.obj = optionCollection;
                    //notes = player who is getting send the collection
                    PushMessageToClientQueue(notes,multiplayerMessageObject);
                    //multiplayerMessageObject = new MultiplayerMessageObject();
                    //multiplayerMessageObject.obj = mapCoordinates;
                    //PushMessageToAllClientQueues(multiplayerMessageObject);

                    ChatToChannel("Option collection reached server, sending to  " + notes, 1);
                }

                break;
                #endregion option collection
        }

    }

    //client method for receivg objects from server (Deserialize xml)
    public void recieveCommandObjectFromSocket(MySocket senderMySocket, string objectTypeAsString, string notes, bool fromServer)
        {
            Socket senderSocket = senderMySocket.Socket;

            Type dataType = typeof(MultiplayerMessage);

        MultiplayerMessage updateTaskMessage = null;
            switch (objectTypeAsString) {
            case MultiplayerMessage.EventSavedData:
                dataType = typeof(EventSavedData);
                break;
            case MultiplayerMessage.AddEventBattle:
                dataType = typeof(EventBattleMultiplayerData);
                break;
            case MultiplayerMessage.PayShopUnitUpkeep:
                dataType = typeof(OurStatList);
                break;
            case MultiplayerMessage.SendLootResolveResult:
            case MultiplayerMessage.ResolveLoot:
                dataType = typeof(OverlandLootClaim);
                break;
            case MultiplayerMessage.StackItems:
            case MultiplayerMessage.SplitItemStack:
            case MultiplayerMessage.SpreadItems:
                dataType = typeof(SourceInfo);
                break;
            case MultiplayerMessage.TransactionInfo:
                dataType = typeof(TransactionInfo);
                break;
            case MultiplayerMessage.ItemTransfer:
                dataType = typeof(MultiplayerItemTransfer);
                break;
            case MultiplayerMessage.SetEntityMission:
                dataType = typeof(Mission);
                break;
            case MultiplayerMessage.AddQuestLootToEventsInv:
                dataType = typeof(OddsAndRandom);
                break;
            case MultiplayerMessage.SendRandom:
                dataType = typeof(string);
                break;
                case MultiplayerMessage.UPDATE_OPTIONCOLLECTION:
                                
                    dataType = typeof(VersionedOptions);                  
                    break;

                case MultiplayerMessage.UPDATE_NEWGAME_OPTION:
                    dataType = typeof(Option);
                    break;

                case MultiplayerMessage.LAUNCH_NEW_SCENARIO:
                    dataType = typeof(CompressedBytes);
                    break;
            case MultiplayerMessage.TestScenario:
                dataType = typeof(MapCoordinates);
                break;
                //case MultiplayerMessage.UPDATE_LOADGAME_PLAYERCOLLECTION:
                //    dataType = typeof(OurPlayerList);
                //    break;

                case MultiplayerMessage.UPDATE_LOADGAME_PLAYER:
                    dataType = typeof(Player);
                    break;
            case MultiplayerMessage.ArmyMove:
                dataType = typeof(ArmyMovementInfo);
                break;
            //case MultiplayerMessage.UPDATE_PROVINCE:
            //    dataType = typeof(Province);
            //    break;

            //case MultiplayerMessage.UPDATE_PROVINCES:
            //    dataType = typeof(List<Province>);
            //    break;

            case MultiplayerMessage.UPDATE_PLAYER_TURN_STATUS:
                    dataType = typeof(Player);
                    break;
                default:
                    System.Console.WriteLine("Unknown command in Multiplayer.recieveCommandObjectFromSocket: " + dataType.ToString());
                    break;

            }
           
            
            byte[] commandFromServer = Multiplayer.ReadMessage(senderSocket);

            //START deserialize

            XmlSerializer xml = new XmlSerializer(dataType);

            switch (objectTypeAsString) {
            case MultiplayerMessage.EventSavedData:
                try
                {
                    EventSavedData eventSavedData = (EventSavedData)xml.Deserialize(new MemoryStream(commandFromServer));

                    GameEngine.ActiveGame.AddEventSavedData(eventSavedData, notes);
                    //its better be done in this thread to prevent recieving messages during generation
                    //Thread refreshActiveUnitsThread = new Thread(() => EventChain.CreateBattlefield()
                    //refreshActiveUnitsThread.IsBackground = true;
                    //refreshActiveUnitsThread.Name = "refresh active units thread";
                    //refreshActiveUnitsThread.Start();
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError(e.Message + " EventSavedData error " + e.StackTrace);
                }
                break;
            case MultiplayerMessage.AddEventBattle:
                try
                {
                    EventBattleMultiplayerData eventBattleData = (EventBattleMultiplayerData)xml.Deserialize(new MemoryStream(commandFromServer));

                   // EventChain.CreateBattlefield(eventBattleData.EventBattleData.SectorSize, eventBattleData.EventBattleData.CombatRoundsCount, eventBattleData.EventBattleData.GlobalRoundsCount, GameEngine.ActiveGame.scenario.FindPlayerByID(eventBattleData.PlayerID), eventBattleData.EventBattleData.SectorsToCreate, eventBattleData.EventBattleData.EventOnWin, eventBattleData.EventBattleData.EventOnDraw, eventBattleData.EventBattleData.EventOnLost, eventBattleData.QuestID, eventBattleData.EventBattleData.EventParties, eventBattleData.EventBattleData.PlayerPartyPositionOnSector, GameEngine.ActiveGame.scenario.FindUnitsByIDs(eventBattleData.PlayerPartyEntityIDs), null);
                    //its better be done in this thread to prevent recieving messages during generation
                    //Thread refreshActiveUnitsThread = new Thread(() => EventChain.CreateBattlefield()
                    //refreshActiveUnitsThread.IsBackground = true;
                    //refreshActiveUnitsThread.Name = "refresh active units thread";
                    //refreshActiveUnitsThread.Start();
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError(e.Message + " in server PayShopUnitUpkeep " + e.StackTrace);
                }
                break;
            case MultiplayerMessage.PayShopUnitUpkeep:
                try
                {
                    OurStatList upkeepToPay = (OurStatList)xml.Deserialize(new MemoryStream(commandFromServer));
                    GameEngine.ActiveGame.PayShopUnitUpkeep(upkeepToPay,notes);
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError(e.Message + " in client PayShopUnitUpkeep " + e.StackTrace);
                }
                break;
            case MultiplayerMessage.SendLootResolveResult: //only non host clients recieve this
                try
                {
                    OverlandLootClaim overlandLootClaim = (OverlandLootClaim)xml.Deserialize(new MemoryStream(commandFromServer));
                    GameEngine.ActiveGame.ResolveLootClaim(overlandLootClaim,Int32.Parse(notes),false);
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError(e.Message + " in server claimloot");
                }
                break;
            case MultiplayerMessage.ResolveLoot:
                try
                {
                    if (isHost)
                    {
                        UnityEngine.Debug.Log("resolve loot ishost side");
                        OverlandLootClaim lootClaim = (OverlandLootClaim)xml.Deserialize(new MemoryStream(commandFromServer));

                        GameEngine.ActiveGame.ResolveLootClaim(lootClaim, Int32.Parse(notes),true);
                    }
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError(e.Message + " in client side resolveloot");
                }
                break;
            case MultiplayerMessage.StackItems:
                try
                {
                    SourceInfo stackItems = (SourceInfo)xml.Deserialize(new MemoryStream(commandFromServer));
                    string[] args = notes.Split('*');
                    int itemID = Int32.Parse(args[0]);
                    string playerID = args[1];
                    stackItems.StackItems(itemID);
                    GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_SOURCE_ITEM, stackItems, playerID));
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError(e.Message + " failed in client stackItems");
                }
                break;
            case MultiplayerMessage.SplitItemStack:
                try
                {
                    SourceInfo stackItems = (SourceInfo)xml.Deserialize(new MemoryStream(commandFromServer));
                    string[] args = notes.Split('*');
                    int itemID = Int32.Parse(args[0]);
                    string playerID = args[1];
                    int stackAmount = Int32.Parse(args[2]);
                    Item item = stackItems.GetItem(itemID);
                    Item newStack = ObjectCopier.Clone<Item>(item);
                    newStack.ID = ++GameEngine.ActiveGame.scenario.FindPlayerByID(playerID).LocalItemIDCounter;
                    item.Quantity = Convert.ToInt32(stackAmount);
                    newStack.Quantity -= item.Quantity;
                    stackItems.AddUnstackable(newStack, false);

                    GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_SOURCE_ITEM, stackItems, playerID));
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError(e.Message + " failed in client SplitItemStack");
                }
                break;
            case MultiplayerMessage.SpreadItems:
                try
                {
                    SourceInfo uIInfo = (SourceInfo)xml.Deserialize(new MemoryStream(commandFromServer));
                    string[] args = notes.Split('*');
                    int itemID = Int32.Parse(args[0]);
                    string playerID = args[1];
                    Item item = uIInfo.GetItem(itemID);
                    while (uIInfo.HasSpaceToTakeItems(new List<Item> { item }, false))
                    {
                        Item newStack = ObjectCopier.Clone<Item>(item);
                        newStack.ID = ++GameEngine.ActiveGame.scenario.FindPlayerByID(playerID).LocalItemIDCounter;
                        //Item newStack = item.ReturnDeepClone(false); //not using ReturnDeepClone, because has a parameter that would add ID from global ID counter, but we want to use local player id instead
                        newStack.Quantity = 1;
                        item.Quantity--;
                        uIInfo.AddUnstackable(newStack, false);



                        if (item.Quantity == 1)
                        {
                            break;
                        }
                    }
                    GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.REFRESH_SOURCE_ITEM, uIInfo, playerID));
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError(e.Message + " failed in client SpreadItems");
                }
                break;
            case MultiplayerMessage.TransactionInfo: //this gave red
                try
                {
                    TransactionInfo transactionInfo = (TransactionInfo)xml.Deserialize(new MemoryStream(commandFromServer));

                    GameEngine.ActiveGame.AcceptTransactionInfo(transactionInfo);
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError("TransactionInfo failed: " + e.Message + e.StackTrace);
                }

                break;
            case MultiplayerMessage.SetEntityMission:
                try
                {
                    Mission mission = (Mission)xml.Deserialize(new MemoryStream(commandFromServer));

                    GameEngine.ActiveGame.SetEntityMission(mission, notes);
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError("client failed to recieve Entity mission: " + e.Message);
                }

                break;
            case MultiplayerMessage.ItemTransfer:
                try
                {
                    MultiplayerItemTransfer itemTransfer = (MultiplayerItemTransfer)xml.Deserialize(new MemoryStream(commandFromServer));
                    
                    UnityEngine.Debug.Log("MP item transfer from ");
                    itemTransfer.from.DisplayDebugLogInfo();
                    UnityEngine.Debug.Log("MP item transfer to ");
                    itemTransfer.to.DisplayDebugLogInfo();
                    Item.transfer(itemTransfer.from, itemTransfer.to);
                    GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.ITEM_TRASNFER, itemTransfer,notes));
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError("client failed to recieve ItemTransfer: " + e.Message);
                }

                break;
            case MultiplayerMessage.AddQuestLootToEventsInv:
                try
                {
                    UnityEngine.Debug.Log("MP AddQuestLootToEventsInv");
                    OddsAndRandom oddsAndRandom = (OddsAndRandom)xml.Deserialize(new MemoryStream(commandFromServer));
                    GameEngine.ActiveGame.AddQuestItemsToEventStash(oddsAndRandom, notes);
                    
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError("server failed to recieve AddQuestLootToEventsInv: " + e.Message);
                }
                break;
            case MultiplayerMessage.SendRandom:
                string Compressedrandom = (string)xml.Deserialize(new MemoryStream(commandFromServer));
                Random random = GameEngine.ConvertTextToData(Compressedrandom,typeof(string)) as Random;
                UnityEngine.Debug.Log("client random T: " + random.Next(75));
                break;
            case MultiplayerMessage.ArmyMove:
                try
                {
                    ArmyMovementInfo armyMovement = (ArmyMovementInfo)xml.Deserialize(new MemoryStream(commandFromServer));

                    GameEngine.ActiveGame.Movement(armyMovement);
                    //Army army = GameEngine.ActiveGame.scenario.FindArmyByID(armyMovementInfo.armyID);
                    //MapSquare loc = GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareByCoordinates(army.Location.WorldMapCoordinates.XCoordinate, army.Location.WorldMapCoordinates.YCoordinate);
                    //MapSquare dest = GameEngine.ActiveGame.scenario.Worldmap.FindMapSquareByCoordinates(armyMovementInfo.newCoordX, armyMovementInfo.newCoordY);
                    //GameEngine.ActiveGame.scenario.Movement(army, loc, dest, armyMovementInfo.modifier, false);

                    
                }
                catch (Exception e)
                {

                    UnityEngine.Debug.LogError("client failed to recieve armyMovementInfo: " + e.Message);
                }

                break;
            case MultiplayerMessage.UPDATE_OPTIONCOLLECTION:

                     OptionCollection options = null;

                    try
                    {
                        options = OptionCollection.getCollectionFromList((VersionedOptions)xml.Deserialize(new MemoryStream(commandFromServer)));

                        string mess = "Options recieved ";
                    //?
                    //if (fromServer)
                    //{
                    //    ChatToChannel("", mess + " from server");
                    //}
                    //else {

                    //    ChatToChannel("", mess + " from client");
                    //}
                    GameEngine.Data.OptionCollection = new OptionCollection("Random map"); //this has the lang strings!!!!!!!!!!!! otherwise upon recieving the col u have no lang string at all
                       GameEngine.ActiveGame.optionPanel.optionCollection = options; //should work i think
                    GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.LOBBY_REFRESH_OPTIONS));
                    ChatToChannel("options recieved by client", 1);

                    //sending confirmation to host(might not be needed in future but for testing purposes i keep it)
                    updateTaskMessage = new MultiplayerMessage(MultiplayerMessage.UpdateTask, TaskStatus.TYPE_SEND_OPTION_COLLECTION + "*" + GameEngine.PLAYER_IDENTITY, "1");
                    GameEngine.ActiveGame.clientManager.Push(updateTaskMessage);
                }
                    catch {

                        string mess = "Error tranfering Options ";

                        if (fromServer)
                        {
                            ChatToChannel("", mess + " from server");
                        }
                        else
                        {
                            ChatToChannel("", mess + " from client");
                        }
                        
                    }
 
                    //if (options!=null) {

                    //    game.NewGameWindow.recieveServerOptions(options);
                    //}

                    break;

                case MultiplayerMessage.UPDATE_NEWGAME_OPTION:

                      Option option = null;

                        try
                        {
                            option = (Option)xml.Deserialize(new MemoryStream(commandFromServer));
                            ChatToChannel("", "Option (" + notes + ") changed by server");
                      //      game.NewGameWindow.changeOption(option);
                        }
                        catch
                        {
                            ChatToChannel("", "Error tranfering Option ("+ notes + ") from server");
                        }

                    break;
 

                case MultiplayerMessage.LAUNCH_NEW_SCENARIO:

                CompressedBytes scenarioSegment = null;

                    try
                    {
                    scenarioSegment = (CompressedBytes)xml.Deserialize(new MemoryStream(commandFromServer));

                    ObjectInTransfer scenarioInTransfer = GetObjectInTransfer(MultiplayerMessage.LAUNCH_NEW_SCENARIO,Int32.Parse(notes)); //notes = total byte size

                    scenarioInTransfer.AddBytes(scenarioSegment.obj);
                     
                    GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT,"Scenario progress: " + scenarioInTransfer.HowMuchOfObjectIsRecieved() + "% (" + scenarioInTransfer.currentByteLength + "/"+scenarioInTransfer.totalByteLength+")"));
                    //GameEngine.ActiveGame.scenario = (Scenario)ObjectByteConverter.ByteArrayToObject(scenario.obj);
                    // ChatToChannel("", "Scenario recieved by server army id " + scenario.ArmyIdCounter);
                    if (scenarioInTransfer.IsReady())
                    {
                        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, "Deserializing scenario"));
                       // GameEngine.ActiveGame.clientManager.mode = ClientManager.MODE_PROCESSING;

                        Thread aiThread = new Thread(() => GameEngine.ActiveGame.DeserializeScenario(scenarioInTransfer.objectBytes));
                        aiThread.Name = " scenario deserialize ";
                        GameEngine.ActiveGame.threadController.threadsControl.Add(aiThread);
                        aiThread.IsBackground = true;
                        aiThread.Start();

                     //   GameEngine.ActiveGame.scenario = (Scenario)ObjectByteConverter.ByteArrayToObject(scenarioInTransfer.objectBytes);
                        incomingObjects.Remove(scenarioInTransfer);
                        GameEngine.ActiveGame.AddToUICommands(new MultiplayerUICommand(MultiplayerUICommand.SET_TASK_STATUS_OUTPUT, ""));
                        ChatToChannel("scenario ready",0);
                        UnityEngine.Debug.Log("scenario ready");
                        //updating host task for final UI step
                      
                    }

                }
                    catch(Exception e)
                    {
                        ChatToChannel("", "Error tranfering Scenario from server " + e.Message);
                    }
                    // if null, Request Scenario again?
                   // game.newGameLaunchAsClient(scenario);

                    break;
            case MultiplayerMessage.TestScenario:
                MapCoordinates mapCoordinates = null;

                try
                {
                    mapCoordinates = (MapCoordinates)xml.Deserialize(new MemoryStream(commandFromServer));

                    ChatToChannel("", "map coordinates recieved by client:  " + mapCoordinates.XCoordinate + " " + mapCoordinates.YCoordinate);

                }
                catch
                {
                    ChatToChannel("", "Error tranfering map coordinates from server");
                }
                break;
            case MultiplayerMessage.UPDATE_LOADGAME_PLAYERCOLLECTION:
                    // OurPlayerList players = null; ;

                    //try
                    //{
                    //    players = (OurPlayerList)xml.Deserialize(new MemoryStream(commandFromServer));

                    //    ChatToChannel("", "Players recieved by server", 1);

                    //}
                    //catch
                    //{
                    //    ChatToChannel("", "Error tranfering Players from server", 1);
                    //}

                    //game.NewGameWindow.recieveServerLoadGamePlayers(players);
                    break;

                case MultiplayerMessage.UPDATE_LOADGAME_PLAYER:

                    Player loadGamePlayer = null;

                    try
                    {
                        loadGamePlayer  = (Player)xml.Deserialize(new MemoryStream(commandFromServer));
                        ChatToChannel("", "Player changed by server");

                    }
                    catch
                    {
                        ChatToChannel("", "Error tranfering changed player from server");
                    }

                    //if (loadGamePlayer != null)
                    //{
                    //    game.NewGameWindow.changePlayer(loadGamePlayer);
                    //}

                    break;

                case MultiplayerMessage.UPDATE_PROVINCE:

                    //Province updatedProvince = null;

                    //try
                    //{
                    //    updatedProvince = (Province)xml.Deserialize(new MemoryStream(commandFromServer));
 
                    //}
                    //catch
                    //{
                    //    string mess = "Error tranfering province data ";

                    //    if (fromServer)
                    //    {
                    //        ChatToChannel("", mess + " from server", 1);
                    //    }
                    //    else
                    //    {

                    //        ChatToChannel("", mess + " from client", 1);
                    //    }

                    //}

                    //if (updatedProvince != null)
                    //{
                    //    game.ActiveScenario.updateProvince(updatedProvince);
                    //    ChatToChannel("", "Updated province successfully (ID:" + updatedProvince.ID + ")", 1);
                       
                    // }

                    
                    break;

                case MultiplayerMessage.UPDATE_PROVINCES:

                    //List<Province> updatedProvinces = null;

                    //try
                    //{
                    //    updatedProvinces = (List<Province>)xml.Deserialize(new MemoryStream(commandFromServer));

                    //}
                    //catch
                    //{
                    //    string mess = "Error tranfering List of updated provinces ";

                    //    if (fromServer)
                    //    {
                    //        ChatToChannel("", mess + " from server", 1);
                    //    }
                    //    else
                    //    {

                    //        ChatToChannel("", mess + " from client", 1);
                    //    }

                    //}

                    //if (updatedProvinces != null)
                    //{
                    //    foreach (Province currentProvince in updatedProvinces) {
                    //        game.ActiveScenario.updateProvince(currentProvince);
                    //    }

                    //    ChatToChannel("", "Updated all provinces successfully", 1);
                    //    game.afterSyncGameResume();
                    //}


                    break;

                case MultiplayerMessage.UPDATE_PLAYER_TURN_STATUS:

                    //Player updatedPlayer = null;

                    //try
                    //{
                    //    updatedPlayer = (Player)xml.Deserialize(new MemoryStream(commandFromServer));
 
                    //}
                    //catch
                    //{
                    //    string mess = "Error tranfering player data ";

                    //    if (fromServer)
                    //    {
                    //        ChatToChannel("", mess + " from server", 1);
                    //    }
                    //    else
                    //    {

                    //        ChatToChannel("", mess + " from client", 1);
                    //    }

                    //}

                    //if (updatedPlayer != null)
                    //{
                    //    game.NewGameWindow.updatePlayerTurnUI(updatedPlayer);
                    //    game.ActiveScenario.updatePlayer(updatedPlayer);

                    //    if (!fromServer) {
                    //        game.Multiplayer.sendPlayerTurnStatus(updatedPlayer, senderMySocket);
                    //        game.checkAllDone(game.ActiveScenario, false);
                    //    }

                       
                       
                    //}

                    break;
   
            }
           

        }//END method

    void CreateClientMessageQueue(string compName)
    {
        lock (queuedServerMessagesList)
        {
            foreach (QueuedServerMessages existingMessages in queuedServerMessagesList)
            {
                if (existingMessages.clientName == compName) //would mean that its a reconnect, therefore clear the existing queue
                {
                    existingMessages.multiplayerMessages.Clear();
                    existingMessages.previousMessages.Clear();
                    return;
                }
            }


            QueuedServerMessages queuedServerMessages = new QueuedServerMessages();
            queuedServerMessages.clientName = compName;
            queuedServerMessagesList.Add(queuedServerMessages);
        }
       
    }

    void PushMessageToClientQueue(string compName, MultiplayerMessageObject msg)
    {
        foreach (QueuedServerMessages messages in queuedServerMessagesList)
        {
            if (messages.clientName == compName)
            {
                if (msg.msg != null)
                {
                    msg.msg.Number = ++messages.serverMessageNumber;
                }
                messages.multiplayerMessages.Add(msg);
                return;
            }
        }
        ChatToChannel("PROBLEM, no client found with comp name: " + compName, 1);
    }

    MultiplayerMessageObject GetClientsLastMessage(string compName)
    {
        foreach (QueuedServerMessages messages in queuedServerMessagesList)
        {
            if (messages.clientName == compName)
            {
                MultiplayerMessageObject multiplayerMessage = messages.previousMessages[messages.previousMessages.Count - 1];
                if (multiplayerMessage.msg != null)
                {
                    multiplayerMessage.msg.IsThereMoreStuffOnQueue = AreThereMoreMessagesToSendToClient(compName);
                }
                
                return multiplayerMessage;
            }
        }
        return null;
    }

    void PushMessageToClientQueue(string compName, MultiplayerMessage msg)
    {
        MultiplayerMessageObject multiplayerMessageObject = new MultiplayerMessageObject();
        multiplayerMessageObject.msg = msg;
   
        PushMessageToClientQueue(compName, multiplayerMessageObject);
    }

    void PushMessageToClientQueue(string compName, MultiplayerMessage incMsg, object obj)
    {
        MultiplayerMessageObject multiplayerMessageObject = new MultiplayerMessageObject();
        multiplayerMessageObject.obj = obj;
        multiplayerMessageObject.msg = incMsg;
        PushMessageToClientQueue(compName,multiplayerMessageObject);
    }
    /// <summary>
    /// will place the taken message into list of previous messages from current messages
    /// </summary>
    /// <param name="compName"></param>
    /// <returns></returns>
    MultiplayerMessageObject GetAndRemoveFirstMessageInQueue(string compName)
    {
        foreach (QueuedServerMessages messages in queuedServerMessagesList)
        {
            if (messages.clientName == compName && messages.multiplayerMessages.Count > 0)
            {
                MultiplayerMessageObject msg = messages.multiplayerMessages[0];
                messages.multiplayerMessages.Remove(msg);
                messages.previousMessages.Add(msg);
                return msg;
            }
        }
        return null;
    }
    /// <summary>
    /// used to re-send messages
    /// therefore not adding new number to messages to be sent
    /// </summary>
    /// <param name="compName"></param>
    /// <param name="msg"></param>
    void InsertIntoClientList(string compName, MultiplayerMessageObject msg)
    {
        foreach (QueuedServerMessages messages in queuedServerMessagesList)
        {
            if (messages.clientName == compName)
            {
                if (messages.multiplayerMessages.Count > 0)
                {
                    messages.multiplayerMessages.Insert(0,msg);
                }
                else
                {
                    messages.multiplayerMessages.Add(msg);
                }
         
            }
        }
    }

    void PushMessageToAllClientQueues(MultiplayerMessageObject msg)
    {
         
        foreach (QueuedServerMessages messages in queuedServerMessagesList)
        {
            //ChatToChannel(" pushing to client: ", messages.clientName);
            if (msg.msg != null)
            {
                msg.msg.Number = ++messages.serverMessageNumber;
            }
            messages.multiplayerMessages.Add(msg);
        }
    }

    bool HasAlreadyRecievedMessageFromClient(int number, string clientName)
    {

        foreach (QueuedServerMessages messages in queuedServerMessagesList)
        {
            if (messages.clientName == clientName)
            {
                bool answer = messages.HasRecievedMessage(number);
                if (!answer) //all is in order, so clearing recieved messages
                {
                    messages.recievedMessagesNumbers.Clear();
                }
                return answer;
            }
        }
        return false;
    }

    void SaveIncomingMessage(int number, string clientName)
    {
        foreach (QueuedServerMessages messages in queuedServerMessagesList)
        {
            if (messages.clientName == clientName)
            {
                messages.recievedMessagesNumbers.Add(number);
            }
        }
    }

    /// <summary>
    /// convient form of the function if not sending any objects, message only
    /// </summary>
    /// <param name="msg"></param>
    void PushMessageToAllClientQueues(MultiplayerMessage msg)
    {
        MultiplayerMessageObject multiplayerMessageObject = new MultiplayerMessageObject();
        multiplayerMessageObject.msg = msg;
        foreach (QueuedServerMessages messages in queuedServerMessagesList)
        {
             
             multiplayerMessageObject.msg.Number = ++messages.serverMessageNumber;
            
            //ChatToChannel(" pushing to client: ", messages.clientName);
            messages.multiplayerMessages.Add(multiplayerMessageObject);
        }
    }
    void PushMessageToAllExceptOneClientQueues(MultiplayerMessageObject msg, string compName)
    {
        foreach (QueuedServerMessages messages in queuedServerMessagesList)
        {
            if (messages.clientName != compName)
            {
                if (msg.msg != null)
                {
                    msg.msg.Number = ++messages.serverMessageNumber;
                }
                messages.multiplayerMessages.Add(msg);
            }
        }
    }
    /// <summary>
    /// convient form of the function if not sending any objects, message only
    /// </summary>
    /// <param name="msg"></param>
    void PushMessageToAllExceptOneClientQueues(MultiplayerMessage msg, string compName)
    {
        MultiplayerMessageObject multiplayerMessageObject = new MultiplayerMessageObject();
        multiplayerMessageObject.msg = msg;
        string debugmsg = "";
        bool debugQueue = false;
        if (debugQueue)
        {
            debugmsg = "PushMessageToAllExceptOneClientQueues sending messages to all except " + compName + " total queued server messages list count: " + queuedServerMessagesList.Count;
        }
        foreach (QueuedServerMessages messages in queuedServerMessagesList)
        {
            if (messages.clientName != compName)
            {
                
                    multiplayerMessageObject.msg.Number = ++messages.serverMessageNumber;
                 
                messages.multiplayerMessages.Add(multiplayerMessageObject);

                if (debugQueue)
                {
                    debugmsg += " sending msg over to " + messages.clientName ;
                }

            }
        }
        if (debugQueue)
        {
            ChatToChannel(debugmsg, 0);
        }
       
    }
    /// <summary>
    /// false means its the last message in queue
    /// </summary>
    /// <param name="compname"></param>
    /// <returns></returns>
    bool AreThereMoreMessagesToSendToClient(string compname)
    {
        foreach (QueuedServerMessages messages in queuedServerMessagesList)
        {
            if (messages.clientName == compname)
            {
                if (messages.multiplayerMessages.Count > 1)
                {
                    return true;
                }
            }
        }
        return false;
    }
 

        // FROM CLIENT
        // server method for recieving info from client !!!!!!!!! no activegame gameengine stuff here(monobehaviour?)
        private void HandleClientComm(object myClient)// HandleClientComm(MySocket myClientSocket)
        {
            MySocket myClientSocket = (MySocket)myClient;

            // we start listening to client

            while (true)
            {
                // cliendi info vastuvõtja ja dekodeerija
                bool breakFromWhile = false;

                try {

                    byte[] commandFromClient = Multiplayer.ReadMessage(myClientSocket.Socket);

                // this.game.NewGameWindow.joinToChat(playerName);
             //   ChatToChannel(myClientSocket.ComputerName + "command from client recieved size: " + commandFromClient.Length, 1);

                //START deserialize
                XmlSerializer xml = new XmlSerializer(typeof(MultiplayerMessage));
                MultiplayerMessage multiplayerMessage = null;
                MultiplayerMessageObject multiplayerMessageObject = null;
                try
                {
                    multiplayerMessage = (MultiplayerMessage)xml.Deserialize(new MemoryStream(commandFromClient));
              //      ChatToChannel("succeeded deserializing multiplayerMessage from client, size: " + commandFromClient.Length, 1);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.Log("failed deserializing multiplayerMessage from client, size: " + commandFromClient.Length + " error: " + e.Message + ", awaiting resend from client...");
                    //ChatToChannel("failed deserializing multiplayerMessage from client, size: " + commandFromClient.Length, 1);
                    if (!TimeoutIsSet(myClientSocket.ComputerName)) //setting the timeout for the client
                    {
                        SetTimeoutTime(myClientSocket.ComputerName, DateTime.Now.ToString());
                    }
                    if (ClientTimeout(myClientSocket.ComputerName))
                    {
                        UnityEngine.Debug.Log("server isnt recieving messages from " + myClientSocket.ComputerName + " for too long, disconnecting client...");
                        if (myClientSocket.Socket != null)
                        {
                            myClientSocket.Socket.Close();
                            myClientSocket.Socket = null;
                        }
                        breakFromWhile = true;
                        break;
                    }
                    continue; //just continue if message was messed up
                }

                SetTimeoutTime(myClientSocket.ComputerName,"");

                //   ChatToChannel(myClientSocket.ComputerName + " command is: " + multiplayerMessage.Command, 1);

                //server could recieve same message several times over, in case of client re-sending it multiple times while listening
                //therefore we just skip the message

                bool processMessage = true;

                if (HasAlreadyRecievedMessageFromClient(multiplayerMessage.Number,myClientSocket.ComputerName))
                {
                    UnityEngine.Debug.Log("server has already recieved message, continuing, pinging back");
                    PushPingToClient(myClientSocket.ComputerName);
                    processMessage = false;
                }
                SaveIncomingMessage(multiplayerMessage.Number,myClientSocket.ComputerName);
                if (processMessage)
                {
                    //UnityEngine.Debug.Log("processing message: " + multiplayerMessage.Command + " " + multiplayerMessage.Number);
                    switch (multiplayerMessage.Command)
                    {
                        #region handshake
                        // client has sent handShake
                        case MultiplayerMessage.HS_MyNameIs:

                            myClientSocket.ComputerName = multiplayerMessage.Argument; //changed from message to computer name
                            //myClientSocket.ComputerName = multiplayerMessage.Message;
                            myClientSocket.ID_Code = multiplayerMessage.Argument; //idk what this is
                                                                                  //  ChatToChannel(myClientSocket.ComputerName + " has connected.", 1);

                            if (multiplayerMessage.Message != GameEngine.ActiveGame.PasswordUsed) //!! use general option here
                            {
                                ChatToChannel(myClientSocket.ComputerName + " has disconnected: bad password", 1);
                                breakFromWhile = true;
                                break;
                            }
                            // game.NewGameWindow.WhoIsConnectedWindow.fillWithContent(); this method made UI buttons and cycled through this.Clients 

                            string nMessage = "";

                            //switch (game.NewGameWindow.LoadOption) {

                            //    case Scenario.SETUP_NEW_SCENARIO:

                            //        // now as he has sent handshake, and if game has not yet started, we will send him new game options
                            //        // selleks saadame talle kõigepealt teate et object on tulemas, mille tüüp on OptionsCollection
                            CreateClientMessageQueue(myClientSocket.ComputerName);
                            //do whatever we need here, right now its a ping(response needed)
                            // MultiplayerMessage lobbyInfo = new MultiplayerMessage(MultiplayerMessage.PlayersCount, "", GameEngine.ActiveGame.optionPanel.playerCount.ToString(), false); //false just in case
                            // PushMessageToClientQueue(myClientSocket.ComputerName, lobbyInfo);

                            bool isHost = false;
                            if (Environment.MachineName != myClientSocket.ComputerName) //if non host is connecting, tell them to open panel, otherwise just ping
                            {
                                isHost = true;
                                multiplayerMessageObject = new MultiplayerMessageObject();
                                //place time in argument for sake of timing
                                //DateTime time = DateTime.Parse(multiplayerMessage.Argument);
                                UnityEngine.Debug.Log("datetime sending: " + DateTime.Now.ToString());
                                MultiplayerMessage openOptionsMsg = new MultiplayerMessage(MultiplayerMessage.OpenOptionsPanel, DateTime.Now.ToString(), multiplayerMessage.Message, AreThereMoreMessagesToSendToClient(myClientSocket.ComputerName));
                                multiplayerMessageObject.msg = openOptionsMsg;
                                PushMessageToClientQueue(myClientSocket.ComputerName, multiplayerMessageObject);
                            }
                            else
                            {
                                multiplayerMessageObject = new MultiplayerMessageObject();

                                MultiplayerMessage ping2 = new MultiplayerMessage(MultiplayerMessage.Ping, "", multiplayerMessage.Message, AreThereMoreMessagesToSendToClient(myClientSocket.ComputerName));
                                multiplayerMessageObject.msg = ping2;
                                PushMessageToClientQueue(myClientSocket.ComputerName, multiplayerMessageObject);

                            }
                            //if this is a load game, notify the client
                            if (GameEngine.ActiveGame.optionPanel.isLoading && !isHost)
                            {
                                PushMessageToClientQueue(myClientSocket.ComputerName, new MultiplayerMessage(MultiplayerMessage.LoadGame, "", ""));
                            }

                            //if (GameEngine.isHost) //if host, reply with ping to the connection
                            //{
                            //    MultiplayerMessage ping2 = new MultiplayerMessage(MultiplayerMessage.Ping, "", multiplayerMessage.Message, AreThereMoreMessagesToSendToClient(myClientSocket.ComputerName));
                            //    PushMessageToClientQueue(myClientSocket.ComputerName, ping2);
                            //}
                            //else //otherwise open options panel
                            //{
                            //    MultiplayerMessage ping2 = new MultiplayerMessage(MultiplayerMessage.OpenOptionsPanel, "", multiplayerMessage.Message, AreThereMoreMessagesToSendToClient(myClientSocket.ComputerName));
                            //    PushMessageToClientQueue(myClientSocket.ComputerName, ping2);
                            //}

                            //MultiplayerMessage mpMessage = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.LAUNCH_NEW_SCENARIO, "",AreThereMoreMessagesToSendToClient(myClientSocket.ComputerName));
                            //sendObjectToClients(mpMessage, myClientSocket);
                            //string path = @"C:\Users\krist\AppData\LocalLow\DefaultCompany\My project\Saves\transferSave.xml";

                            //GameEngine.ActiveGame.scenario = GameEngine.getScenarioFromXML(path);
                            //sendObjectToClients(GameEngine.ActiveGame.scenario, myClientSocket);
                            //        // ja pärast saadame objecti enda
                            //        VersionedOptions options = game.NewGameWindow.OptionCollection.getAsOptionList();
                            //        sendObjectToClients(options, myClientSocket);

                            //        nMessage = "options sent to ";
                            //        break;

                            //    case Scenario.SETUP_LOAD_SCENARIO:

                            //        mpMessage = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.UPDATE_LOADGAME_PLAYERCOLLECTION, "");
                            //        sendObjectToClients(mpMessage, myClientSocket);

                            //        // ja pärast saadame objecti enda
                            //        if (game.NewGameWindow == null) {
                            //            System.Console.WriteLine("game.NewGameWindow is null in Multiplayer.HandleClientComm()");
                            //        }

                            //        if (game.NewGameWindow.OurPlayerList == null) {

                            //            System.Console.WriteLine("game.NewGameWindow.OurLoadGamePlayerList is null in Multiplayer.HandleClientComm()");
                            //        }

                            //        OurPlayerList players = game.NewGameWindow.OurPlayerList;
                            //        sendObjectToClients(players, myClientSocket);

                            //        nMessage = "loadgame playerinfo sent to ";
                            //        break;
                            //    case Scenario.CONNECTION_OBSERVER:


                            //        break;


                            //}

                            // ChatToChannel("", nMessage + myClientSocket.ComputerName, 1);

                            break;
                        #endregion handshake
                        case MultiplayerMessage.GenerateScenario:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.ToggleBlockMode:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.ResolveEventCommand:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.SetPlayerAsAI:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.ResendRequest:
                            InsertIntoClientList(myClientSocket.ComputerName, GetClientsLastMessage(myClientSocket.ComputerName));
                            break;
                        case MultiplayerMessage.SetPlayerAsAutoBattle:
                            PushMessageToAllClientQueues(multiplayerMessage);
                            break;
                        case MultiplayerMessage.ResolvedEventClaim:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.ClaimEvent:
                            PushMessageToAllClientQueues(multiplayerMessage);
                            break;
                        case MultiplayerMessage.ContinueDungeon:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.AddPreferredItem:
                        case MultiplayerMessage.RemovePreferredItem:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.SkillClick:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.StartObserving:
                            PushMessageToAllClientQueues(multiplayerMessage);
                            break;
                        case MultiplayerMessage.DeclareEnemy:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.OfferPeace:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.AcceptPeace:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.Retreat:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.SkillTargetClick:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.CancelSkillButtonClick:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.UnitMove:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.NextCombatTurnClick:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.SetBuildingMode:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.SetBuildingProductionToAI:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.ProceedToAfterBattles:
                            PushMessageToAllClientQueues(multiplayerMessage);
                            break;
                        case MultiplayerMessage.SetPlayerGameStateToAfterBattle:
                            PushMessageToAllClientQueues(multiplayerMessage);

                            break;
                        case MultiplayerMessage.StartAfterBattleProcessingForPlayer:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.SetArmyToStopAttacking:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.SetArmyToAttack:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.PlaceBid:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.SelectLevelUpChoice:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.CancelCraftItem:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.PlaceItemBeforeFirst:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.PlaceInBetweenItems:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.PlaceItemLast:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.StartRecipe:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.DismissNotification:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.AcceptTradeOffer:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.CancelQuestParty:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.SetItemForAuction:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.CancelAuctionOrTradeItem:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.CancelledBid:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.CreateEmptyOfferBid:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.ProceedToEndTurn:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.NullEntityMission:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.SetItemForTrade:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.PurchaseItem:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.ProceedToStartTurn:
                            PushMessageToAllClientQueues(multiplayerMessage);
                            break;
                        case MultiplayerMessage.ReadyForStartGlobalTurn:
                            PushMessageToAllClientQueues(new MultiplayerMessage(multiplayerMessage.Command, multiplayerMessage.Argument, clients.Count.ToString()));
                            //PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.UpdateQuestPartyProgress:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.DisbandQuestArmies:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.CreateEntitySkill:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.CreateEntitySkillInCombat:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.RemoveEntitySkill:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.RemoveEventChain:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.AddEvent:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.AddInitializedEvent:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.ProceedToMainPhase:
                            PushMessageToAllClientQueues(multiplayerMessage);
                            break;
                        case MultiplayerMessage.SendRandom:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.SendRandomAndUICommand:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.DisableOptionPanelUI:
                            PushMessageToClientQueue(multiplayerMessage.Argument, multiplayerMessage);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.ReleaseEndTurnUI:
                            PushMessageToClientQueue(multiplayerMessage.Argument, multiplayerMessage);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.DisableEndTurnUI:
                            PushMessageToClientQueue(multiplayerMessage.Argument, multiplayerMessage);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.ChatMessage:
                            //relaying chat message to all players' queues
                            multiplayerMessageObject = new MultiplayerMessageObject();
                            MultiplayerMessage chatMsg = new MultiplayerMessage(MultiplayerMessage.ChatMessage, multiplayerMessage.Argument, multiplayerMessage.Message, AreThereMoreMessagesToSendToClient(myClientSocket.ComputerName));
                            multiplayerMessageObject.msg = chatMsg;
                            PushMessageToAllClientQueues(multiplayerMessageObject);
                            UnityEngine.Debug.Log("gi");
                            break;
                        case MultiplayerMessage.SetProductionLineValue:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.SubmitEndTurn:
                            PushMessageToAllClientQueues(multiplayerMessage); //send to host
                            UnityEngine.Debug.Log("subit end turn server side");
                            break;
                        case MultiplayerMessage.SendScenario: //sending saved scenario to player(argument)
                            try
                            {
                                CompressedBytes compressedScenario = (CompressedBytes)scenarioObj;
                                byte[] scenarioIntoCompressedBytes = compressedScenario.obj;
                                List<byte[]> segmentedScenario = ObjectByteConverter.DivideByteArrayIntoSegments(scenarioIntoCompressedBytes, 5000);
                                int ik = 0;
                                string debugStr = " ";
                                foreach (byte[] segmentOfScenario in segmentedScenario) //each scenario segment is a seperate message
                                {
                                    ik++;
                                    multiplayerMessageObject = new MultiplayerMessageObject();
                                    multiplayerMessageObject.msg = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.LAUNCH_NEW_SCENARIO, scenarioIntoCompressedBytes.Length.ToString());

                                    CompressedBytes compressedBytes = new CompressedBytes(); //using this class as a placeholder for byte[]
                                    compressedBytes.obj = segmentOfScenario;

                                    multiplayerMessageObject.obj = compressedBytes;
                                    PushMessageToClientQueue(multiplayerMessage.Argument, multiplayerMessageObject);

                                    debugStr += ik + " size " + segmentOfScenario.Length + " ";
                                }



                                PushPingToClient(myClientSocket.ComputerName);
                                ChatToChannel("sending scenario to " + multiplayerMessage.Argument + " compressed size: " + scenarioIntoCompressedBytes.Length + " segments count: " + segmentedScenario.Count + debugStr, 0);
                            }
                            catch (Exception e)
                            {

                                UnityEngine.Debug.LogError(e.Message);
                            }

                            break;
                        case MultiplayerMessage.NullifyScenario:
                            PushPingToClient(myClientSocket.ComputerName);
                            scenarioObj = null;
                            break;
                        case MultiplayerMessage.PlayersCount:
                            PushPingToClient(myClientSocket.ComputerName);
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.Ping:
                            //multiplayerMessageObject = new MultiplayerMessageObject();
                            //MultiplayerMessage ping = new MultiplayerMessage(MultiplayerMessage.Ping, "", multiplayerMessage.Message, AreThereMoreMessagesToSendToClient(myClientSocket.ComputerName));

                            //multiplayerMessageObject.msg = ping;
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.UnAssignPlayerID:
                            PushMessageToAllClientQueues(multiplayerMessage);
                            break;
                        case MultiplayerMessage.AssignPlayerID:
                            multiplayerMessageObject = new MultiplayerMessageObject();
                            MultiplayerMessage assignPlayerIDmsg = new MultiplayerMessage(MultiplayerMessage.AssignPlayerID, multiplayerMessage.Argument, multiplayerMessage.Message, AreThereMoreMessagesToSendToClient(myClientSocket.ComputerName));
                            multiplayerMessageObject.msg = assignPlayerIDmsg;
                            PushMessageToAllClientQueues(multiplayerMessageObject);
                            //GameEngine.ActiveGame.AssignPlayerID(multiplayerMessage.Argument,multiplayerMessage.Message);
                            //show in UI?
                            break;
                        case MultiplayerMessage.OptionChange: //we send to everyone except host(only host can send this so mySocket is the way to go
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.OptionColRequest: //just relaying the incoming request to other clients to check on
                                                                  //we send this option col request to other clients(one of them is a host), meanwhile multiplayermessage.argument is listening to get the collection
                                                                  //MultiplayerMessage wtfMsg = new MultiplayerMessage(MultiplayerMessage.ChatMessage, "gra", " boston tea party");
                                                                  //PushMessageToAllExceptOneClientQueues(wtfMsg, multiplayerMessage.Argument, true);
                            MultiplayerMessage optionColReqRelayed = new MultiplayerMessage(MultiplayerMessage.OptionColRequest, multiplayerMessage.Argument, "", AreThereMoreMessagesToSendToClient(myClientSocket.ComputerName));
                            PushMessageToAllExceptOneClientQueues(optionColReqRelayed, multiplayerMessage.Argument);
                            //PushMessageToAllExceptOneClientQueues(multiplayerMessage,multiplayerMessage.Argument,true);  
                            // PushMessageToClientQueue(hostName,multiplayerMessage); //host name empty, whaaaaaaat!!
                            //updating status output to player waiting
                            MultiplayerMessage waitingForHostResponseMsg = new MultiplayerMessage(MultiplayerMessage.UpdateTaskStatusOutput, "", "Requesting options from host..."); //not needed, can do client-side?
                            PushMessageToClientQueue(multiplayerMessage.Argument, waitingForHostResponseMsg, null);

                            ChatToChannel("option col request from " + multiplayerMessage.Argument + " to " + hostName, 1);
                            break;
                        case MultiplayerMessage.UpdateTask: //relaying the message that host could read
                            PushMessageToAllClientQueues(multiplayerMessage);
                            break;
                        case MultiplayerMessage.UpdateTaskStatusOutput:
                            PushMessageToClientQueue(multiplayerMessage.Argument, multiplayerMessage, null);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.UpdateTaskStatusOutputToAll:
                            PushMessageToAllClientQueues(multiplayerMessage);
                            break;
                        case MultiplayerMessage.UpdateTaskStatusOutputToAllExceptSender:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage,myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.CreatePlayerController:
                            PushMessageToClientQueue(multiplayerMessage.Argument, multiplayerMessage, null);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.SetGameState:
                            PushMessageToAllExceptOneClientQueues(multiplayerMessage, myClientSocket.ComputerName);
                            PushPingToClient(myClientSocket.ComputerName);
                            break;
                        case MultiplayerMessage.SubmitMainPhase:

                            PushMessageToAllClientQueues(multiplayerMessage); //message = comp ID

                            break;
                        case MultiplayerMessage.CreateAIThreads:
                            Thread aiThread = new Thread(() => GameEngine.ActiveGame.AI_METHOD(multiplayerMessage.Argument));
                            aiThread.Name = "AI Thread + " + multiplayerMessage.Argument;

                            aiThread.IsBackground = true;
                            aiThread.Start();
                            break;
                        case MultiplayerMessage.Bye:
                            UnityEngine.Debug.Log("client: " + myClientSocket.ComputerName + " is disconnecting, message: " + multiplayerMessage.Message);
                            ChatToChannel(myClientSocket.ComputerName + " has disconnected: (" + multiplayerMessage.Message + ")", 1);
                            // myClientSocket.Socket.Shutdown(SocketShutdown.Both);
                            breakFromWhile = true;
                            break;

                        // client wants to send an object
                        case MultiplayerMessage.Sending_Object:



                            string objectType = multiplayerMessage.Argument;
                            string notes = multiplayerMessage.Message;
                            try
                            {
                                RecieveObjectFromClient(myClientSocket, objectType, notes, myClientSocket.ComputerName);

                            }
                            catch (Exception)
                            {
                                ChatToChannel(myClientSocket.ComputerName + " failed sending command object: " + objectType + " notes: " + multiplayerMessage.Message, 1);
                                UnityEngine.Debug.LogError(myClientSocket.ComputerName + " failed sending command object: " + objectType + " notes: " + multiplayerMessage.Message);
                            }
                            //queueing up a ping just in case
                            //multiplayerMessageObject = new MultiplayerMessageObject();
                            //MultiplayerMessage ping3 = new MultiplayerMessage(MultiplayerMessage.Ping, "", multiplayerMessage.Message, AreThereMoreMessagesToSendToClient(myClientSocket.ComputerName));

                            //multiplayerMessageObject.msg = ping3;
                            PushPingToClient(myClientSocket.ComputerName);

                            break;

                    }

                }

                //if theres anything in server's queue, then it will send it as response to client
                if (!breakFromWhile)
                {
                    MultiplayerMessageObject responseMessage = GetAndRemoveFirstMessageInQueue(myClientSocket.ComputerName);
                    if (responseMessage != null)
                    {
                        //ChatToChannel(" server responding",0);
                        sendObjectToClients(responseMessage.msg, myClientSocket);
                        if (responseMessage.obj != null)
                        {
                            sendObjectToClients(responseMessage.obj, myClientSocket);
                        }

                    }
                }
              
            }
                catch { 
                    // Connection closed
                    ChatToChannel(myClientSocket.ComputerName + " has been disconnected (reason unknown)", 1);

                    if (myClientSocket.Socket != null)
                    {
                        if (myClientSocket.Socket.Connected)
                        {
                            myClientSocket.Socket.Shutdown(SocketShutdown.Both);
                            myClientSocket.Socket.Close();
                            myClientSocket.Socket = null;
                        }
               
                    }
                   
                    break;
                }

                if (breakFromWhile || stopThread)
                {
                UnityEngine.Debug.Log("SERVER SIDE: CLOSING CLIENT THREAD");
                if (myClientSocket.Socket != null)
                    {
                        myClientSocket.Socket.Shutdown(SocketShutdown.Both);
                        myClientSocket.Socket.Close();
                        myClientSocket.Socket = null;
                    }
                   
                    break;
                }

            }//END while

        }//END METHOD
    /// <summary>
    /// currently timeout time is 6 seconds for the server
    /// </summary>
    /// <param name="computerName"></param>
    /// <returns></returns>
    private bool ClientTimeout(string computerName)
    {
        foreach (QueuedServerMessages messages in queuedServerMessagesList)
        {
            if (messages.clientName == computerName)
            {
                DateTime setTime = DateTime.Parse(messages.DateTimeString);
                double timeElapsed = DateTime.Now.Subtract(setTime).TotalSeconds;
                if (timeElapsed >= 6)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        return false;
    }

    private bool TimeoutIsSet(string computerName)
    {
        foreach (QueuedServerMessages messages in queuedServerMessagesList)
        {
            if (messages.clientName == computerName)
            {
                if (messages.DateTimeString != "")
                {
                    return true;
                }
                else
                {
                    return false;
                }
                
            }
        }
        return false;
    }

    private void SetTimeoutTime(string computerName,string value)
    {
        foreach (QueuedServerMessages messages in queuedServerMessagesList)
        {
            if (messages.clientName == computerName)
            {
                messages.DateTimeString = value;
                return;
            }
        }
    }

    void PushPingToClient(string clientName)
    {
        //  PushMessageToClientQueue(clientName, new MultiplayerMessage(MultiplayerMessage.Ping, "", "", false));


        if (!AreThereMoreMessagesToSendToClient(clientName)) //we dont have to send the ping back if we have actual messages to send
        {
            PushMessageToClientQueue(clientName, new MultiplayerMessage(MultiplayerMessage.Ping, "", "", false));
        }

    }

        /// <summary>
        /// Serveri thread, mis ootab ühendusi, ja kui ühendus saabub, tekitab sellele eraldi socketi ja launchib eraldi threadi ja hakkab kuulama järgmist
        /// Server thread, that is waiting for connections, and when connection arrives, makes seperate socket for it, launches it in the seperate thread and starts listening for next one
        /// </summary>
        void HandleServer()
        {
        //CloseClientSockets();

        try
        {
                ip = new IPEndPoint(IPAddress.Any, GameEngine.GAME_PORT);
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                
                listener.Bind(ip);
                listener.Listen(10);
        
            while (GameEngine.ActiveGame.serverON) 
                {
                    // kui server ei tohiks töödata
                    //if (!serverOn) break;
               // testMessage = "gi";
                MySocket myClientSocket = new MySocket();
                    
                    Socket client = listener.Accept();
                //testMessages.Add(" Client Accepted");
                if (!GameEngine.ActiveGame.serverON) 
                {
                    UnityEngine.Debug.Log("SERVER SIDE: serverOn = false");
                    break;
                }
                //UnityEngine.Debug.Log("SERVER SIDE: serverOn = " + GameEngine.ActiveGame.serverON.ToString());
                myClientSocket.Socket = client;
               // testMessages.Add(" Client Added");
                IPEndPoint clientep = (IPEndPoint)myClientSocket.Socket.RemoteEndPoint;
                    clients.Add(myClientSocket);
                testMessage += "client count:" + clients.Count  +Environment.NewLine; 
                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                GameEngine.ActiveGame.threadController.threadsControl.Add(clientThread);
                //  testMessages.Add(" Client Thread Added");
                clientThread.Name = "Client Thread";
                    clientThread.IsBackground=true;
                    clientThread.Start(myClientSocket);
          
 
            }
           
            //if (listener != null) {


            //    //listener.Shutdown(SocketShutdown.Both);
            //    if (listener.Connected)
            //    {
            //        listener.Shutdown(SocketShutdown.Both);
            //    }
            //    listener.Close();
            //    //listener.Close();
            //    listener = null;

               
            //    UnityEngine.Debug.Log("SERVER SIDE: SERVER IS CLOSED");
                
            //    }

            }
            catch (SocketException e)
            {
            if (e.SocketErrorCode == SocketError.OperationAborted)
            {
                UnityEngine.Debug.Log("server closing sockets: " + e.Message + " " + e.StackTrace + " " + e.ErrorCode);
            }
            else
            {
                UnityEngine.Debug.Log("server closes: " + e.Message + " " + e.StackTrace + " " + e.ErrorCode);
            }
           
                // System.Console.WriteLine("Server lost");
                //CloseSockets("Server Closed " + e.Message); //commented out this part, because testing starting new game from existing game
                // menu.SetNewState(Menu.MultiplayerIntValue, 0);
            }
        //CloseClientSockets();
        UnityEngine.Debug.Log("SERVER SIDE: HandleServer end");
        } // end  void HandleServer()

    public void CloseClientSockets()
    {
        UnityEngine.Debug.Log("SERVER SIDE: CLOSING ALL SOCKETS");
        //listener.Close();
        if (listener != null)
        {
            if (listener.Connected)
            {
                listener.Shutdown(SocketShutdown.Both);
            }
            listener.Close();
            listener = null;
        }


        foreach (MySocket client in clients)
        {
            if (client.Socket != null)
            {
                if (client.Socket.Connected)
                {
                    client.Socket.Shutdown(SocketShutdown.Both);
                }

                client.Socket.Close();
                client.Socket = null;
            }

        }
        clients.Clear();

        // mingi kuradi teine thread võib ta vahepeal ära nullida
        if (this.serverSocket != null)
        {

            if (this.serverSocket.Socket != null)
            {
                if (this.serverSocket.Socket.Connected)
                {
                    this.serverSocket.Socket.Shutdown(SocketShutdown.Both);
                }
              
                this.serverSocket.Socket.Close();
            }

            this.serverSocket = null;
        }
    }

        /// <summary>
        /// Kui mäng suletakse nurgast, siis kutsutakse see meetod välja, et sulgeda enne seda veel lahtised socketid.
        /// </summary>
        public void CloseSockets(string reason)
        {
        // "Server has closed the connection"
        UnityEngine.Debug.Log("CloseSockets called with reason: " + reason);
        if (this.serverSocket != null) {

                // we warn the server that client has pressed disconnect (probably left of it's own will)
                if (reason == MultiplayerMessage.Pressed_Disconnect)
                {
         
                    MultiplayerMessage mpMessage = new MultiplayerMessage(MultiplayerMessage.Bye, "", MultiplayerMessage.Pressed_Disconnect);
                //  game.Multiplayer.sendMultiPlayerMessage(mpMessage);
                    sendMultiPlayerMessage(mpMessage);
                Thread.Sleep(200);
                }

            if (reason == MultiplayerMessage.Wrong_Password)
            {
                MultiplayerMessage mpMessage = new MultiplayerMessage(MultiplayerMessage.Bye, "", MultiplayerMessage.Wrong_Password);
                //  game.Multiplayer.sendMultiPlayerMessage(mpMessage);
                sendMultiPlayerMessage(mpMessage);
                Thread.Sleep(200);
            }

                if (reason == MultiplayerMessage.Closed_Game)
                {
                    MultiplayerMessage mpMessage = new MultiplayerMessage(MultiplayerMessage.Bye, "", MultiplayerMessage.Closed_Game);
                //    game.Multiplayer.sendMultiPlayerMessage(mpMessage);
                     sendMultiPlayerMessage(mpMessage);
                Thread.Sleep(200);

                }

                if (reason == MultiplayerMessage.Server_Closed_Game) {

                    ChatToChannel(MultiplayerMessage.Server_Closed_Game, 1);
                }

                // mingi kuradi teine thread võib ta vahepeal ära nullida
                if (this.serverSocket != null) {

                    if (this.serverSocket.Socket != null) {
                    this.serverSocket.Socket.Shutdown(SocketShutdown.Both);
                        this.serverSocket.Socket.Close();
                    }
                    
                    this.serverSocket = null;
                }
                
            }

            // vaatame kas on vaja teadata (ise olles server), et server pani mängu kinni
            foreach (MySocket currentSocket in clients) 
            {
                if (currentSocket.Socket != null) {

                    if (reason == MultiplayerMessage.Closed_Game)
                    {
                        MultiplayerMessage mpMessage = new MultiplayerMessage(MultiplayerMessage.Bye, "", MultiplayerMessage.Closed_Game);
                    //  game.Multiplayer.sendMultiPlayerMessage(mpMessage);
                         sendMultiPlayerMessage(mpMessage);
                     }

                }
                    
            }

            // anname serverile aega teadata clientidele et ta "lahkub"
            if (this.clients.Count > 0 && reason == MultiplayerMessage.Closed_Game) {
                Thread.Sleep(300);
            }

            // kustutame ja sulgeme kõik socketid
            foreach (MySocket currentSocket in clients)
            {
                if (currentSocket.Socket != null)
                {
                currentSocket.Socket.Shutdown(SocketShutdown.Both);
                currentSocket.Socket.Close();
                    currentSocket.Socket = null;
                }

            }

            clients.Clear();

            if (listener != null)
            {
                listener.Close();
                listener = null;
            }
        ChatToChannel(reason,1);
        }

        #region Send Methods

        public void sendToAllButThisSocket(Object obj, MySocket doNotSendHere)
        {

            List<MySocket> recievers = new List<MySocket>();
            recievers.AddRange(this.clients);
            
            if (this.serverSocket!=null) {
                recievers.Add(this.serverSocket);
            }
            
            List<MySocket> notRecievers = new List<MySocket>();

            if (doNotSendHere != null) {
                notRecievers.Add(doNotSendHere);
            }
            
            sendObjectToClients(obj, recievers, notRecievers);
        }

        public void SendToPlayerByID(Object obj, string playerID)
    {
        List<MySocket> recievers = new List<MySocket>();
        List<MySocket> notRecievers = new List<MySocket>();
        notRecievers.AddRange(this.clients);
        foreach (MySocket socket in this.clients)
        {
            if (socket.PlayerID == playerID)
            {
                recievers.Add(socket);
                notRecievers.Remove(socket);
                break;
            }
        }
        if (serverSocket != null)
        {
            if (serverSocket.PlayerID == playerID)
            {
                recievers.Add(serverSocket);
            }
        }
      

        sendObjectToClients(obj, recievers, notRecievers);
    }

    public void SendToServerSocket(Object obj)
    {
        List<MySocket> recievers = new List<MySocket>();
        List<MySocket> notRecievers = new List<MySocket>();
        notRecievers.AddRange(this.clients);
        if (serverSocket != null)
        {
            recievers.Add(serverSocket);
        }


        sendObjectToClients(obj, recievers, notRecievers);
    }

    public void SendToPlayerByComputerName(Object obj, string computerName)
    {
        List<MySocket> recievers = new List<MySocket>();
        List<MySocket> notRecievers = new List<MySocket>();
        notRecievers.AddRange(this.clients);
        foreach (MySocket socket in this.clients)
        {
            if (socket.ComputerName == computerName)
            {
                recievers.Add(socket);
                notRecievers.Remove(socket);
                break;
            }
        }
        if (serverSocket != null)
        {
            if (serverSocket.ComputerName == computerName)
            {
                recievers.Add(serverSocket);
            }
        }
      

        sendObjectToClients(obj, recievers, notRecievers);
    }

    public void broadCastToAll(Object obj)
        {

            List<MySocket> recievers = new List<MySocket>();
            recievers.AddRange(this.clients);

            if (this.serverSocket != null)
            {
           // testMessage = "ge";
                recievers.Add(this.serverSocket);
        }
        else
        {
           // testMessage = "hyo + " + recievers.Count;
        }

            List<MySocket> notRecievers = new List<MySocket>();

            sendObjectToClients(obj, recievers, notRecievers);


        }



        // this method sends object (can be command) to all connected clients 
        public void sendObjectToAll(Object obj)
        {
            broadCastToAll(obj);
        }

        // konreetsele socketile asja saatmine
        public void sendObjectToClients(Object obj, MySocket mySocket)
        {
            List<MySocket> recievers = new List<MySocket>();
            recievers.Add(mySocket);
            List<MySocket> notRecievers = new List<MySocket>();

            sendObjectToClients(obj, recievers, notRecievers);
        }


        //method for sending info to clients
        public void sendObjectToClients(Object obj, List<MySocket> recievers, List<MySocket> notRecievers)
        {

            XmlSerializer xmlSerializer = new XmlSerializer(obj.GetType());

            using (MemoryStream memoryStream = new MemoryStream())
            {
                xmlSerializer.Serialize(memoryStream, obj);
                memoryStream.Flush();

                byte[] message = PacketProtocol.WrapMessage(memoryStream.GetBuffer());

                foreach (MySocket mySocket in recievers)
                {
                if (mySocket.Socket == null)
                {
                    continue;
                }
                    if (!notRecievers.Contains(mySocket)) {

                        if (mySocket.Socket != null)
                        {
                       // testMessages.Add("we send the object " + UnityEngine.Time.time);
                            sendMessage(mySocket, message);
                        }
                    
                    }

                }// end foreach

            }// end void

            mySocketCleanUp();

        }//END method

        //// client method for sending current player's province to server
        //public void sendProvince(Province province)
        //{
        //    MultiplayerMessage mpMessage = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.UPDATE_PROVINCE, "");
        //    sendObjectToAll(mpMessage);
        //    sendObjectToAll(province);

        //}

        //public void sendProvinces(List<Province> provinces) {

        //    MultiplayerMessage mpMessage = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.UPDATE_PROVINCES, "");
        //    sendObjectToAll(mpMessage);
        //    sendObjectToAll(provinces);
        //}

        public void sendMultiPlayerMessage(MultiplayerMessage mpMessage)
        {
            broadCastToAll(mpMessage);
        }

        #endregion


    } // end class Multiplayer

