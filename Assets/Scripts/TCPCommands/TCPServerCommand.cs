using System.Diagnostics;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;

using UnityEngine;
using System.Collections;


namespace UMol {


public class NetMqCommandManager {
    private readonly Thread _listenerWorker;

    public bool _listenerCancelled;

    public delegate string MessageDelegate(string message);

    private readonly MessageDelegate _messageDelegate;

    private readonly Stopwatch _contactWatch;

    private const long ContactThreshold = 1000;

    public bool Connected;

    public bool waitingForCommandRes = false;


    private void ListenerWork()
    {
        AsyncIO.ForceDotNet.Force();
        using (var server = new ResponseSocket())
        {
            server.Bind("tcp://*:5555");

            while (!_listenerCancelled)
            {
                Connected = _contactWatch.ElapsedMilliseconds < ContactThreshold;
                string message;
                if (!server.TryReceiveFrameString(out message)) continue;
                _contactWatch.Restart();
                waitingForCommandRes = true;
                var response = _messageDelegate(message);
                server.SendFrame(response);
            }
        }
        NetMQConfig.Cleanup();
    }

    public NetMqCommandManager(MessageDelegate messageDelegate)
    {
        _messageDelegate = messageDelegate;
        _contactWatch = new Stopwatch();
        _contactWatch.Start();
        _listenerWorker = new Thread(ListenerWork);
    }

    public void Start()
    {
        _listenerCancelled = false;
        _listenerWorker.Start();
    }

    public void Stop()
    {
        NetMQConfig.Cleanup();
        _listenerCancelled = true;
        _listenerWorker.Abort();
    }
}

public class TCPServerCommand : MonoBehaviour {
    private NetMqCommandManager _commandManager;
    private string commandReceived = "";
    private string commandResult = "";
    private bool wasRunningBackground = false;
    /// Wait X seconds before restoring the run in background state if no command is called
    public float timeoutRunInBG = 5.0f;
    Coroutine restoreRunInBG;

    void Start() {
        wasRunningBackground = Application.runInBackground;
        _commandManager = new NetMqCommandManager(HandleMessage);
        _commandManager.Start();
    }

    void Update() {

        if (_commandManager.waitingForCommandRes) {//Process the command
            if(restoreRunInBG != null)
                StopCoroutine(restoreRunInBG);
            Application.runInBackground = true;
            
            object res = null;
            try {
                res = UMol.API.APIPython.ExecuteCommand(commandReceived);
            }
            catch {
            }
            commandResult = commandResToString(res);

            _commandManager.waitingForCommandRes = false;
            restoreRunInBG = StartCoroutine(waitAndSetRunInBG());
        }
    }
    private string commandResToString(object res) {
        string comRes;
        if (res != null && res.GetType() == typeof(System.String)) {
            comRes = res as string;
        }
        else if (res != null && res.GetType() == typeof(System.Boolean)) {
            comRes = UMol.API.APIPython.cBoolToPy((bool)res);
        }
        else if (res != null && res.GetType() == typeof(UnityMolStructure) ) {
            comRes = (res as UnityMolStructure).name;
        }
        else if (res != null && res.GetType() == typeof(UnityMolSelection) ) {
            comRes = (res as UnityMolSelection).name;
        }
        else {
            comRes = " ";
        }
        return comRes;
    }

    private string HandleMessage(string message) {
        //Warning : This is not running on the main thread !
        UnityEngine.Debug.Log("Received command '" + message + "'");
        commandReceived = message;
        //Wait for the command to execute and return something
        while (_commandManager.waitingForCommandRes && !_commandManager._listenerCancelled) {
        }

        return commandResult;
    }

    private void OnDestroy()
    {
        _commandManager._listenerCancelled = true;
        _commandManager.Stop();
        Application.runInBackground = wasRunningBackground;
    }
    IEnumerator waitAndSetRunInBG(){
        yield return new WaitForSeconds(timeoutRunInBG);
        Application.runInBackground = wasRunningBackground;
    }


}
}