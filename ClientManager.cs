using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientManager : MonoBehaviour
{
    public const string MODE_LISTENING = "MODE_LISTENING";
    public const string MODE_IDLE = "MODE_IDLE"; //this goes idle after listening is done and we have nothing to ask/send(queue must be empty)
    public const string MODE_SENDING = "MODE_SENDING";
    public const string MODE_CONNECT_TO_SERVER = "MODE_CONNECT_TO_SERVER";
    public const string MODE_PROCESSING = "MODE_PROCESSING";
    public string mode = MODE_CONNECT_TO_SERVER;
    float timer = 0.0f; //idle ping
    float waitTime = 0f;
    float taskTimer = 0.0f;
    float taskWaitTime = 0f;
    float listeningTimer = 0.0f;
    internal float disconnectTimer = 12f;
    
    float reSendMessageTimer = 0f;  
    internal float reSendMessageWait = 3f;  
    public int MessageCounter = 0;
    internal List<MultiplayerMessageObject> multiplayerMessages = new List<MultiplayerMessageObject>();
    internal List<MultiplayerMessageObject> previousMessages = new List<MultiplayerMessageObject>(); //if server recieved message 1 and 3 it will request message 2 from here?
    MultiplayerMessageObject previousMessage = null;
    public bool forcePinging = false;
    bool debugUpdate = false;
    internal Multiplayer multiplayer; //link from wherever u doing this(optionpanelcontroller/multiplayertestmenu)
    string lastSentMessage = ""; // for debug purposes
    string previousSentMessage = ""; //message before last one
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (multiplayer == null)
        {
            return;
        }
        if (mode == MODE_IDLE)
        {
            if (multiplayerMessages.Count > 0) //we already have a message to send, so no need to ping
            {
                mode = MODE_SENDING;
            }
            else
            {
                timer += Time.deltaTime;
                if (timer >= waitTime)
                {
                    timer = 0;

                    MultiplayerMessage ping = new MultiplayerMessage(MultiplayerMessage.Ping, "", ""); //queueing ping message
                    Push(ping);
                    if (debugUpdate)
                    {
                        Debug.Log("ping");
                    }

                }
            }
          
      
        }
        if (mode == MODE_LISTENING)
        {
            listeningTimer += Time.deltaTime;
            reSendMessageTimer += Time.deltaTime;
            if (listeningTimer >= disconnectTimer)
            {
                listeningTimer = 0;
                Debug.Log("!! Connection timed out , last sent message: " + lastSentMessage + " previous message: " + previousSentMessage);
                multiplayer.CloseSockets("Connection timed out");
                GameEngine.ActiveGame.taskStatusOutput.text = "Connection timed out";
                multiplayer = null;
                //disconnect?
            }
            else
            {
                //we arent getting a response, so we resend a message until we get it
                //return; //test
                if (reSendMessageTimer >= reSendMessageWait)
                {
                    reSendMessageTimer = 0;
                    
                    SendMessageToServer(previousMessage);
                    string msgTxt = "";
                    if (previousMessage.msg != null)
                    {
                        msgTxt += previousMessage.msg.Command + " " + previousMessage.msg.Argument + " " + previousMessage.msg.Message + " " + previousMessage.msg.Number;
                    }
                    if (previousMessage.obj != null)
                    {
                        msgTxt += " + object";
                    }
                    Debug.Log("resending previous message " + msgTxt + " resend timer: " + reSendMessageTimer + " resendwait " + reSendMessageWait);
                }
            }
        }
        if (mode == MODE_PROCESSING)
        {
            //do nothing, we are processing large file(likely scenario)
        }
        if (mode == MODE_SENDING)
        {
            if (debugUpdate)
            {
                Debug.Log("sending mode");
            }
            listeningTimer = 0; //reset the timer
            reSendMessageTimer = 0;
            //no messages avalible, going idle
            lock (multiplayerMessages)
            {
                if (multiplayerMessages.Count == 0)
                {
                    if (debugUpdate)
                    {
                        Debug.Log("returning to idle");
                    }

                    mode = MODE_IDLE;
                    //if server got more stuff to send(quick back and forth), we send a ping in case we dont have messages of client
                    if (forcePinging)
                    {
                        if (debugUpdate)
                        {
                            Debug.Log("forcePinging");
                        }
                        Debug.Log("forcePinging");
                        mode = MODE_SENDING;
                        MultiplayerMessage ping = new MultiplayerMessage(MultiplayerMessage.Ping, "", ""); //queueing ping message
                        Push(ping);
                    }
                }
                else //we have messages to send
                {
                    MultiplayerMessageObject multiplayerMessageObject = multiplayerMessages[0];
                   
                  
                    previousMessage = multiplayerMessageObject;
                    // MultiplayerMessage msg = multiplayerMessageObject.msg;
                    multiplayerMessages.RemoveAt(0);
                    previousMessages.Add(multiplayerMessageObject);
                    //SendMessageToServer(msg);
                    mode = MODE_LISTENING; //waiting for server response !! this thing is fast, so now listening before sending data into socket
                    SendMessageToServer(multiplayerMessageObject);
                    

                    if (lastSentMessage != previousSentMessage)
                    {
                        previousSentMessage = lastSentMessage;
                    }

                    if (multiplayerMessageObject.msg != null)
                    {
                        lastSentMessage = multiplayerMessageObject.msg.Command + " arg " + multiplayerMessageObject.msg.Argument + " msg " + multiplayerMessageObject.msg.Message;
                    }

                    if (debugUpdate)
                    {
                        Debug.Log("sending multiplayer message: " + multiplayerMessageObject.msg.Command + " arg " + multiplayerMessageObject.msg.Argument + " note " + multiplayerMessageObject.msg.Message + " messages count: " + multiplayerMessages.Count);
                        //to allow collapse, now doing message number for this
                        //Debug.Log("sending multiplayer message: " + multiplayerMessageObject.msg.Command + " arg " + multiplayerMessageObject.msg.Argument + " note " + multiplayerMessageObject.msg.Message + " number " + multiplayerMessageObject.msg.Number);
                        Debug.Log("listening mode");
                    }
                }

            }


        }

        //task timer & loop
        taskTimer += Time.deltaTime;
        if (taskTimer >= taskWaitTime)
        {
            taskTimer = 0;
            
            List<TaskStatus> toRemove = new List<TaskStatus>();
            lock (GameEngine.ActiveGame.hostTasks)
            {
                //taking 1 step from each task avalible, and completing it
                foreach (TaskStatus task in GameEngine.ActiveGame.hostTasks)
                {
                    TaskSet taskSteps = task.GetCurrentTasks();
            
                    TaskStep taskStep = taskSteps[0];

                    if (debugUpdate)
                    {
                        Debug.Log("doing task step: " + taskStep.taskStepName + " data " + taskStep.data + " requiredStatus: " + taskStep.requiredStatus + " task set selected: " + task.taskSetSelection);
                    }
                    //if required status is set & it doesnt match the current task status then continue to next task
                    if (taskStep.requiredStatus != "" && taskStep.requiredStatus != task.completionStatus)
                    {
                        continue;
                    }

                    if (taskStep.timerThreshold > 0) //if timer is set, then we check it
                    {
                        taskStep.timer += Time.deltaTime; //add time
                        if (taskStep.timer < taskStep.timerThreshold) //timer not passed yet, continue to next task
                        {
                            continue;
                        }
                    }

                    taskSteps.RemoveAt(0);
                    if (taskSteps.Count == 0) //task complete(no more steps left)
                    {
                        toRemove.Add(task);
                    }
                    Debug.Log("taskStep.data timer " + taskStep.timer + " " + taskStep.timerThreshold);
                    CompleteTaskStep(taskStep, task.computerID);

                }
                foreach (TaskStatus taskToRemove in toRemove)
                {
                    GameEngine.ActiveGame.hostTasks.Remove(taskToRemove);
                }
            }
        }

        

    }

    void CompleteTaskStep(TaskStep taskStep,string clientName)
    {
        switch (taskStep.taskStepName)
        {
            case TaskStep.TASK_SEND_RANDOM:
               // MultiplayerMessage randomMsg = new MultiplayerMessage(MultiplayerMessage.SendRandom, clientName, GameEngine.random.Seed + "*" + GameEngine.random.Iteration);
                break;
            case TaskStep.TASK_LOCK_OPTION_COL_UI:
                MultiplayerMessage LockUIoptions = new MultiplayerMessage(MultiplayerMessage.DisableOptionPanelUI, clientName, "");
                Push(LockUIoptions);
                break;
            case TaskStep.TASK_DISABLE_END_TURN_BUTTON:
                foreach (PlayerSetup setup in GameEngine.ActiveGame.scenario.PlayerSetups)
                {
                    if (setup.ComputerName != "")
                    {
                        MultiplayerMessage disableEndTurnBtn = new MultiplayerMessage(MultiplayerMessage.DisableEndTurnUI, setup.ComputerName, setup.PlayerName);
                        Push(disableEndTurnBtn);
                    }
                }
                break;
            case TaskStep.TASK_PROCEED_NEXT_TURN:
                // MultiplayerMessage nextTurnMessage = new MultiplayerMessage(); //dont want to do next turn here to not stop this update()
                GameEngine.ActiveGame.EndGlobalTurnThread();
                //   GameEngine.ActiveGame.scenario.StartGlobalTurn();
                MultiplayerMessage proceedNextTurnMsg = new MultiplayerMessage(MultiplayerMessage.ProceedToEndTurn, "","");
                GameEngine.ActiveGame.clientManager.Push(proceedNextTurnMsg);
                if (debugUpdate)
                {
                    Debug.Log("TaskStep.TASK_PROCEED_NEXT_TURN");
                }
                foreach (PlayerSetup setup in GameEngine.ActiveGame.scenario.PlayerSetups)
                {
                    if (setup.ComputerName != "")
                    {
                        MultiplayerMessage disableEndTurnBtn = new MultiplayerMessage(MultiplayerMessage.ReleaseEndTurnUI, setup.ComputerName, setup.PlayerName);
                        Push(disableEndTurnBtn);
                    }
                }
         
                break;
            case TaskStep.TASK_ENTER_OBSERVER_MODE_ALL:
                MultiplayerMessage enterObserverModeMessage = new MultiplayerMessage();
                enterObserverModeMessage.Command = MultiplayerMessage.StartObserving;
                enterObserverModeMessage.Message = taskStep.data;
                Push(enterObserverModeMessage);
                break;
            case TaskStep.TASK_SEND_STATUS_OUTPUT_TO_ALL_PLAYERS:
                MultiplayerMessage statusOutputToAllMessage = new MultiplayerMessage();
                statusOutputToAllMessage.Command = MultiplayerMessage.UpdateTaskStatusOutputToAll;
                statusOutputToAllMessage.Message = taskStep.data;
                Debug.Log("taskStep.data: " + taskStep.data);
                Push(statusOutputToAllMessage);
                break;
            case TaskStep.TASK_SEND_STATUS_OUTPUT:
                MultiplayerMessage statusOutputMessage = new MultiplayerMessage();
                statusOutputMessage.Command = MultiplayerMessage.UpdateTaskStatusOutput;
                statusOutputMessage.Argument = clientName;
                statusOutputMessage.Message = taskStep.data;
                Push(statusOutputMessage);
                break;
            case TaskStep.TASK_SEND_ASSIGNED_PLAYERS:
                lock (GameEngine.ActiveGame.optionPanel.assignedPlayerSetups)
                {
                    foreach (MyValue val in GameEngine.ActiveGame.optionPanel.assignedPlayerSetups)
                    {
                        MultiplayerMessage multiplayerMessage = new MultiplayerMessage(MultiplayerMessage.AssignPlayerID, val.Keyword, val.Value);
                        GameEngine.ActiveGame.clientManager.Push(multiplayerMessage);
                    }
                }
                break;
            case TaskStep.TASK_SEND_OPTION_COLLECTION:
    
                //the message that server will read to send to the argument(the player that requested the collection)
                MultiplayerMessage optionColMessage = new MultiplayerMessage(MultiplayerMessage.Sending_Object, MultiplayerMessage.UPDATE_OPTIONCOLLECTION, clientName);
                //sending in the message(header) and the collection itself
                if (GameEngine.ActiveGame.optionPanel == null)
                {
                    Debug.LogError("TASK_SEND_OPTION_COLLECTION option panel is null");
                }
                if (GameEngine.ActiveGame.optionPanel.optionCollection == null)
                {
                    Debug.LogError("TASK_SEND_OPTION_COLLECTION option collection is null");
                }
                if (GameEngine.ActiveGame.optionPanel.optionCollection.getAsOptionList() == null)
                {
                    Debug.LogError("TASK_SEND_OPTION_COLLECTION optionCollection.getAsOptionList() is null");
                }
                //we send only the data of the collection to avoid sending useless data such as constants, which are created anyway on the recieving end
                PushMultiplayerObject(optionColMessage, GameEngine.ActiveGame.optionPanel.optionCollection.getAsOptionList());
                break;
            case TaskStep.TASK_SEND_SCENARIO:
                MultiplayerMessage scenarioMessage = new MultiplayerMessage(MultiplayerMessage.SendScenario, clientName, ""); //scenario is already saved in server, so we just tell server to send it to specific clients
                
                Push(scenarioMessage);
                break;
            case TaskStep.TASK_SEND_SCENARIO_NULLIFIER:
                bool nullify = true;
                //no need to lock as we are already there
                foreach (TaskStatus taskStatus in GameEngine.ActiveGame.hostTasks)
                {
                    //when all tasks have status "1", that means everyone has receieved the scenario and therefore we can remove it from multiplayer memory
                    //if at least one task has not recieved the scenario yet, then this step does nothing, because the last task will null the scenario
                    if (taskStatus.taskType == TaskStatus.TYPE_SEND_SCENARIO && taskStatus.completionStatus != "1")
                    {
                        return;
                    }
                }
                Debug.Log("scenario nullify!!!");
                if (nullify)
                {
                    MultiplayerMessage nullifierMessage = new MultiplayerMessage(MultiplayerMessage.NullifyScenario, "", "");
                    Push(nullifierMessage);
                }
 
                break;
            case TaskStep.TASK_SEND_UI_COMMAND:

                MultiplayerMessage uiCommandMessage = new MultiplayerMessage(MultiplayerMessage.CreatePlayerController,clientName,"");
                Push(uiCommandMessage);
                break;
            default:
                Debug.LogError("no TaskStep with name: " + taskStep.taskStepName);
                break;
        }
    }
 
    public void InsertToFirst(MultiplayerMessage incMessage)
    {
        lock (multiplayerMessages)
        {
            MultiplayerMessageObject multiplayerMessageObject = new MultiplayerMessageObject();
            multiplayerMessageObject.msg = incMessage;
            if (MessageCounter == Int32.MaxValue)
            {
                MessageCounter = 0;
            }
            multiplayerMessageObject.msg.Number = ++MessageCounter;
            if (multiplayerMessages.Count == 0)
            {
                multiplayerMessages.Add(multiplayerMessageObject);
            }
            else
            {
                multiplayerMessages.Insert(0, multiplayerMessageObject);
            }

        }
        if (mode != MODE_LISTENING && mode != MODE_PROCESSING)
        {
            mode = MODE_SENDING;
        }
    }

    public void Push(MultiplayerMessage incMessage)
    {
        MultiplayerMessageObject msg = new MultiplayerMessageObject();
        msg.msg = incMessage;
        if (MessageCounter == Int32.MaxValue)
        {
            MessageCounter = 0;
        }
        msg.msg.Number = ++MessageCounter;
      
        lock (multiplayerMessages)
        {
            multiplayerMessages.Add(msg);
        }
      
        if (mode != MODE_LISTENING && mode != MODE_PROCESSING)
        {
            mode = MODE_SENDING;
        }
    }
    /// <summary>
    /// use this if sending objects
    /// </summary>
    /// <param name="incMessage"></param>
    /// <param name="obj"></param>
    public void PushMultiplayerObject(MultiplayerMessage incMessage, object obj)
    {
        MultiplayerMessageObject msg = new MultiplayerMessageObject();
        msg.msg = incMessage;
        msg.obj = obj;
        if (MessageCounter == Int32.MaxValue)
        {
            MessageCounter = 0;
        }
        msg.msg.Number = ++MessageCounter;
        lock (multiplayerMessages)
        {
            multiplayerMessages.Add(msg);
        }
     
        if (mode != MODE_LISTENING)
        {
            mode = MODE_SENDING;
        }
    }

    void SendMessageToServer(MultiplayerMessageObject multiplayerMessage)
    {
        if (multiplayerMessage.obj == null) //if no obj, means we not sending an object
        {
            multiplayer.SendToServerSocket(multiplayerMessage.msg);
        }
        else
        {
            multiplayer.SendToServerSocket(multiplayerMessage.msg); //sending header
            multiplayer.SendToServerSocket(multiplayerMessage.obj); //sending object right after
        }
       
    }

    public MultiplayerMessage FindPreviousMessageByID(int id)
    {
        foreach (MultiplayerMessageObject msg in previousMessages)
        {
            if (msg.msg.Number == id)
            {
                return msg.msg;
            }
        }
        return null;
    }
}
