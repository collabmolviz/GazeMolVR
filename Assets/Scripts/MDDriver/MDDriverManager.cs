using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine.XR;
using UnityEngine.UI;
using System.Runtime.InteropServices;

using MDDriverClientPointer = System.IntPtr;

namespace UMol {

public class MDDriverManager : MonoBehaviour {

    MDDriverThreadedClient threadedClient;

    public IntPtr mddriverInstance;
    GraphManager gm;

    //Scale the applied forces using this factor
    public float appliedForceFactor = 1.0f;
    Thread oThread;

    bool initialized = false;

    public int protocol_version = 0;

    public Vector3[] positions;
    public UnityMolStructure structure;
    public bool needsUpdate = false;

    private Transform clref;
    private Transform crref;

    private List<float> coordinatesForces = new List<float>();
    private List<int> indicesForces = new List<int>();


    private Vector3 initCoG = Vector3.zero;
    private Vector3 initCoGIMD = Vector3.zero;
    public Vector3 translation = Vector3.zero;
    bool first = true;
    private bool forceStop = false;

    public static string namePlot1 = "Total Energy (kcal/mol)";
    public static string namePlot2 = "Hydrogen Bonds Energy (kcal/mol)";
    public static string namePlot3 = "Stacking Energy (kcal/mol)";

    public string curNamePlot1;
    public string curNamePlot2;
    public string curNamePlot3;



    public bool isConnected()
    {
        if (mddriverInstance != IntPtr.Zero) {
            return MDDriverWrapper.MDDriver_isConnected(mddriverInstance);
        }
        return false;
    }

    public bool connect(string host, int port)
    {
        indicesForces.Clear();
        coordinatesForces.Clear();
        first = true;

        if (structure.trajectoryLoaded) {
            Debug.LogError("A trajectory is loaded for this structure");
            return false;
        }

        if (isConnected()) {
            Debug.Log("Already connected to an IMD session, disconnect first");
            return true;
        }


        int capacity = structure.Count;
        positions = new Vector3[capacity];

        structure.trajAtomPositions = new Vector3[capacity];

        if (isConnected()) {
            Clear();
        }

        mddriverInstance = MDDriverWrapper.createMDDriverInstance();

        MDDriverWrapper.MDDriver_init(mddriverInstance, host, port);
        Debug.Log("IMD connection to: " + host + " : " + port);


        if (!isConnected()) {
            Debug.LogError("Failed to connect to IMD");
            MDDriverWrapper.MDDriver_disconnect(mddriverInstance);
            positions = null;
            structure.trajAtomPositions = null;

            if (mddriverInstance != IntPtr.Zero)
                MDDriverWrapper.deleteMDDriverInstance(mddriverInstance);
            mddriverInstance = IntPtr.Zero;
            return false;
        }

        threadedClient = new MDDriverThreadedClient();
        threadedClient.MDDM = this;
        threadedClient.targetFPS = Mathf.Max(30, Screen.currentResolution.refreshRate);

        initialized = true;

        initCoG = structure.currentModel.centroid;

        oThread = new Thread(() => threadedClient.mainProc(capacity));
        oThread.Start();
        while (!oThread.IsAlive);

        switchControllerMode(true);

        gm = UnityMolMain.getGraphManager();
        curNamePlot1 = structure.name + namePlot1;
        curNamePlot2 = structure.name + namePlot2;
        curNamePlot3 = structure.name + namePlot3;

        gm.CreatePlot(curNamePlot1, Color.white, -200.0f, 300.0f);
        gm.CreatePlot(curNamePlot2, new Color(0.75f, 0.75f, 0.75f, 1.0f));
        gm.CreatePlot(curNamePlot3, new Color(0.75f, 0.5f, 0.5f, 1.0f));


        UnityMolMain.IMDRunning = true;
        return true;
    }

    // What to do when the connection is closed/lost
    // (either from disconnect, stop, or something else)
    public void clearConnection()
    {
        if (isConnected()) {
            threadedClient.stop();
            oThread.Join();
            initialized = false;
            Debug.Log ("Thread joined");
        }


        UnityMolMain.IMDRunning = isIMDRunning();
        first = true;
        coordinatesForces.Clear();
        indicesForces.Clear();
        destroyMDDInstance();

        gm = UnityMolMain.getGraphManager();
        gm.DeletePlot(curNamePlot1);
        gm.DeletePlot(curNamePlot2);
        gm.DeletePlot(curNamePlot3);

    }

    void destroyMDDInstance() {
        if (mddriverInstance != IntPtr.Zero) {
            MDDriverWrapper.deleteMDDriverInstance(mddriverInstance);
        }
        mddriverInstance = IntPtr.Zero;
    }

    //Directly call the structure.disconnect in the Update loop => main Unity thread
    public void forceDisconnect() {
        forceStop = true;
    }


    public void stop_simulation()
    {
        if (isConnected())
        {
            threadedClient.set_stop_simulation();
            clearConnection();
        }
        switchControllerMode(false);
    }

    void switchControllerMode(bool IMDOn) {

        if (UnityMolMain.inVR()) {
            GameObject clrefgo = UnityMolMain.getLeftController();
            GameObject crrefgo = UnityMolMain.getRightController();

            if (clrefgo != null)
                clref = clrefgo.transform;
            if (crrefgo != null)
                crref = crrefgo.transform;

            if (clref != null) {
                clref.gameObject.GetComponent<PointerAtomSelection>().enabled = !IMDOn;
                clref.gameObject.GetComponent<PointerIMD>().enabled = IMDOn;
            }
            if (crref != null) {
                crref.gameObject.GetComponent<PointerAtomSelection>().enabled = !IMDOn;
                crref.gameObject.GetComponent<PointerIMD>().enabled = IMDOn;
            }
        }
    }

    void Update() {

        if (forceStop) {
            structure.disconnectIMD();
            return;
        }
        if (needsUpdate) {
            if (first) {
                initCoGIMD = UnityMolModel.ComputeCentroid(positions);
                translation = initCoGIMD - initCoG;
                first = false;
            }

            // for (int i = 0; i < positions.Length; i++) {
            //     positions[i] = positions[i] - translation;
            // }


            structure.trajAtomPositions = positions;
            structure.trajUpdateAtomPositions();
            structure.updateRepresentations(trajectory: true);

            if (gm != null) {
                var e = threadedClient.energies;

                gm.AddToPlot(curNamePlot1, e.Etot);
                gm.AddToPlot(curNamePlot2, e.Evdw);
                gm.AddToPlot(curNamePlot3, e.Eelec);

            }



            needsUpdate = false;
        }
        if (indicesForces.Count != 0) {
            threadedClient.sendForcesToThread(indicesForces.Count, indicesForces, coordinatesForces);
            coordinatesForces.Clear();
            indicesForces.Clear();
        }
    }

    //Scaling is done here
    public void addForce(int id, float[] coords) {
        indicesForces.Add(id);
        coordinatesForces.Add(coords[0] * appliedForceFactor);
        coordinatesForces.Add(coords[1] * appliedForceFactor);
        coordinatesForces.Add(coords[2] * appliedForceFactor);
    }

    public void disconnect() {
        Clear();
    }

    public void Clear() {
        clearConnection();
        switchControllerMode(false);
    }

    public static bool isIMDRunning() {
        UnityMolStructureManager sm = UnityMolMain.getStructureManager();
        foreach (UnityMolStructure s in sm.loadedStructures) {
            if (s.mddriverM != null) {
                return true;
            }
        }
        return false;
    }
    void OnDestroy() {
        Clear();
    }
}

public class MDDriverThreadedClient {

    public MDDriverManager MDDM;
    public int targetFPS;

    private float[] pos2;

    private bool run = false;
    public bool kill_simulation = false;


    public MDDriverWrapper.IMDEnergies energies = new MDDriverWrapper.IMDEnergies();

    private ConcurrentQueue<float> threadForces = new ConcurrentQueue<float>();
    private ConcurrentQueue<int> threadIndicesForces = new ConcurrentQueue<int>();
    //Indices of atoms for which we applied forces but we have to reset them
    private HashSet<int> prevThreadIndices = new HashSet<int>();

    public void set_stop_simulation()
    {
        kill_simulation = true;
    }

    public void disconnect()
    {
        Debug.Log("Sending disconnect");
        MDDriverWrapper.MDDriver_disconnect(MDDM.mddriverInstance);
    }

    public void mainProc(int capacity)
    {
        kill_simulation = false;
        Vector3 dummyPos = Vector3.one * -100;

        pos2 = new float[capacity * 3];
        run = true;
        int readParticles = 0;
        int sleepTimems = Mathf.CeilToInt(1000.0f / targetFPS);

        while (run)
        {
            if (MDDriverWrapper.MDDriver_loop(MDDM.mddriverInstance) == 1) {

                // if (firstPass)
                readParticles = MDDriverWrapper.MDDriver_getNbParticles(MDDM.mddriverInstance);

                if (capacity >= readParticles && readParticles > 0) {

                    //Get positions
                    MDDriverWrapper.MDDriver_getPositions(MDDM.mddriverInstance, pos2, readParticles);
                    //Get energies
                    MDDriverWrapper.MDDriver_getEnergies(MDDM.mddriverInstance, ref energies);

                    for (int i = 0; i < readParticles; i++) {
                        if (float.IsNaN(pos2[i * 3]) || float.IsInfinity(pos2[i * 3]) || Mathf.Abs(pos2[i * 3]) > 10000.0f ) {
                            MDDM.positions[i] = dummyPos;
                            continue;
                        }
                        if (float.IsNaN(pos2[i * 3 + 1]) || float.IsInfinity(pos2[i * 3 + 1]) || Mathf.Abs(pos2[i * 3 + 1]) > 10000.0f ) {
                            MDDM.positions[i] = dummyPos;
                            continue;
                        }
                        if (float.IsNaN(pos2[i * 3 + 2]) || float.IsInfinity(pos2[i * 3 + 2]) || Mathf.Abs(pos2[i * 3 + 2]) > 10000.0f ) {
                            MDDM.positions[i] = dummyPos;
                            continue;
                        }

                        //Invert X !
                        MDDM.positions[i].x = -pos2[i * 3] - MDDM.translation.x;
                        MDDM.positions[i].y = pos2[i * 3 + 1] - MDDM.translation.y;
                        MDDM.positions[i].z = pos2[i * 3 + 2] - MDDM.translation.z;
                    }

                    MDDM.needsUpdate = true;
                }



                sendForcesToSimulation();

                //Don't go faster than the Unity framerate
                System.Threading.Thread.Sleep(sleepTimems);
                // System.Threading.Thread.Sleep(100);
            }
            else { // Server connection was shut.
                Debug.Log("Connection stopped");
                stop();
                MDDM.forceDisconnect();
            }
        }

        // Debug.Log("Stopping");
        // //kill and disconnect calls the same function
        // if (kill_simulation) {
        //     MDDriverWrapper.MDDriver_stop(MDDM.mddriverInstance);
        // }
        // else {
        //     disconnect();
        // }
    }

    public void sendForcesToThread(int nb_forces, List<int> indices, List<float> coordinates) {
        for (int i = 0; i < nb_forces; i++) {
            threadForces.Enqueue(-coordinates[i * 3]);
            threadForces.Enqueue(coordinates[i * 3 + 1]);
            threadForces.Enqueue(coordinates[i * 3 + 2]);
            threadIndicesForces.Enqueue(indices[i]);
        }
    }
    private void sendForcesToSimulation() {

        if (prevThreadIndices.Count != 0) { //If previous forces need to be reset
            //Check if we continue applying forces to the same atoms
            foreach (int idf in threadIndicesForces) {
                if (prevThreadIndices.Contains(idf)) {
                    prevThreadIndices.Remove(idf);
                }
            }
            if (prevThreadIndices.Count != 0) {
                float[] zeros = new float[prevThreadIndices.Count * 3];
                MDDriverWrapper.MDDriver_setForces(MDDM.mddriverInstance, prevThreadIndices.Count, prevThreadIndices.ToArray(), zeros);
            }
            prevThreadIndices.Clear();
        }

        if (threadIndicesForces.Count != 0) {
            MDDriverWrapper.MDDriver_setForces(MDDM.mddriverInstance, threadIndicesForces.Count, threadIndicesForces.ToArray(), threadForces.ToArray());
        }

        //Record atom indices to reset forces when done
        foreach (int idf in threadIndicesForces) {
            prevThreadIndices.Add(idf);
        }

        clearConcurrentQueue(threadIndicesForces);
        clearConcurrentQueue(threadForces);
    }

    public void stop() {
        run = false;
        if (kill_simulation)
            MDDriverWrapper.MDDriver_stop(MDDM.mddriverInstance);
        else
            disconnect();
    }

    private void clearConcurrentQueue<T>(ConcurrentQueue<T> queue) {
        T item;
        while (queue.TryDequeue(out item))
        {
        }
    }
}
}
