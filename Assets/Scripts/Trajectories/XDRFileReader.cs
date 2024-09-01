using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System;

using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;

namespace UMol {

// namespace Trajectories {
enum XDRFileReaderStatus {
    OFFSETFILECREATION = -7,
    OFFSETFILETREAD = -6,
    TRAJECTORYPRESENT = -5,
    FRAMEDOESNOTEXIST = -4,
    ENDOFFILE = -3,
    NUMBEROFATOMSMISMATCH = -2,
    FILENOTFOUND = -1,
    SUCCESS = 0
}

enum SEEK {
    SEEK_SET = 0,   /* set file offset to offset */
    SEEK_CUR = 1,   /* set file offset to current plus offset */
    SEEK_END = 2   /* set file offset to EOF plus offset */
}

//
// Performs I/O on an XTC file.
//
public class XDRFileReader {
    //How many frames we pre-fetch from the file / Bigger value means we need to access the xtc file less often but when we do it takes more time
    private int TRAJBUFFERSIZE = 40;

    public UnityMolStructure structure;
    public string path;
    public int currentFrame = 0;
    public int numberAtoms = 0;
    public int numberFrames = 0;

    public System.IntPtr file_pointer = System.IntPtr.Zero;
    public string offsetFileName;
    public long[] offsets;
    bool is_trr = false;
    public bool cubicInterpolation = false;

    float[,] box = new float[3, 3];

#if UNITY_WEBGL
    List<Vector3[]> fullTrajectory;
#endif

    public struct FrameInfo {
        public int step;
        public float time;
    }

    List<FrameInfo> frames_info;

    /// Buffer of frames containing TRAJBUFFERSIZE/2 frames before the current frame and TRAJBUFFERSIZE/2 after
    private Vector3[][] trajectoryBuffer;
    /// Get position of the frame in the buffer
    private Dictionary<int, int> frameToTrajBuffer = new Dictionary<int, int>();
    int idB = 0;
    private float[] trajectoryBufferF;
    private NativeArray<float> trajBufferProcess;
    private NativeArray<float3> trajBufferProcessOut;
    private TrajectorySmoother trajSmoother;
    private TrajectoryMean trajMean;
    private Vector3[] framesToMean;

    /// Initiate at frame 0 and returns the number of frames
    public int load_trajectory() {

        trajSmoother = new TrajectorySmoother();
        trajMean = new TrajectoryMean();
        sync_scene_with_frame(0);

        return numberFrames;
    }

    /// Opens a trajectory file.
    public int open_trajectory(UnityMolStructure stru, string filePath, bool is_trr = false) {
        if (file_pointer != System.IntPtr.Zero) {
            Debug.LogError("This instance has a trajectory already opened.");
            return (int) XDRFileReaderStatus.TRAJECTORYPRESENT;
        }
        if (stru.trajectoryLoaded) {
            Debug.LogError("This structure has a trajectory already opened.");
            return (int) XDRFileReaderStatus.TRAJECTORYPRESENT;
        }

        structure = stru;
        path = filePath;

#if UNITY_WEBGL
        fullTrajectory = XTCTrajectoryParserCSharp.GetTrajectory(filePath);
        numberAtoms = fullTrajectory[0].Length;
        numberFrames = fullTrajectory.Count;

        if (numberAtoms > structure.Count) {
            Debug.LogWarning("Trajectory has not the same number of atoms than the first model of the structure." + numberAtoms + " vs " + structure.Count);
        }
        this.is_trr = is_trr;

        structure.trajectoryLoaded = true;

        return numberAtoms;
#endif
        XDRStatus result;
        if (is_trr) {
            result = XDRFileWrapper.read_trr_natoms (filePath, ref numberAtoms);
        } else {
            result = XDRFileWrapper.read_xtc_natoms (filePath, ref numberAtoms);
        }

        if (result != XDRStatus.exdrOK) {
            Debug.LogError("Could not get number of atoms from file " + filePath);
            return (int) XDRFileReaderStatus.FILENOTFOUND;
        }

        if (numberAtoms > structure.Count) {
            Debug.LogWarning("Trajectory has not the same number of atoms than the first model of the structure." + numberAtoms + " vs " + structure.Count);
//              numberAtoms = (int)MoleculeModel.atomsnumber;
        }

        file_pointer = XDRFileWrapper.xdrfile_open (filePath, "r");
        if (file_pointer == System.IntPtr.Zero) {
            Debug.LogError("Could not open file " + filePath);
            return (int) XDRFileReaderStatus.FILENOTFOUND;
        }

        int res = updateOffsetFile(filePath);
        if (res != (int) XDRFileReaderStatus.SUCCESS) {
            if (res == (int) XDRFileReaderStatus.OFFSETFILETREAD) {
                Debug.LogError("Could not read offset file " + offsetFileName);
                return (int) XDRFileReaderStatus.OFFSETFILETREAD;
            }

            Debug.LogError("Could not create offset file " + offsetFileName);
            return (int) XDRFileReaderStatus.OFFSETFILECREATION;
        }


        this.is_trr = is_trr;
        structure.trajectoryLoaded = true;
        return numberAtoms;
    }

    bool diffFrames(Vector3[] f1, Vector3[] f2) {
        if (f1.Length != f2.Length) {
            return false;
        }
        for (int i = 0; i < f1.Length; i++) {
            if (!Mathf.Approximately(f1[i].x, f2[i].x) ||
                    !Mathf.Approximately(f1[i].y, f2[i].y) ||
                    !Mathf.Approximately(f1[i].z, f2[i].z)) {
                return false;
            }
        }

        return true;
    }

    public Vector3[] getFrame(int frame_number) {
#if UNITY_WEBGL
        if (fullTrajectory != null) {
            if (frame_number >= 0 && frame_number < numberFrames) {
                return fullTrajectory[frame_number];
            }
        }
#endif

        if (trajectoryBuffer == null || trajectoryBuffer[0] == null) {
            if (numberFrames < TRAJBUFFERSIZE) {
                TRAJBUFFERSIZE = numberFrames;
            }
            trajectoryBuffer = new Vector3[TRAJBUFFERSIZE][];
            for (int i = 0; i < TRAJBUFFERSIZE; i++) {
                trajectoryBuffer[i] = new Vector3[numberAtoms];
            }

            if (trajBufferProcess.IsCreated) {
                trajBufferProcess.Dispose();
                trajBufferProcessOut.Dispose();
            }

            trajectoryBufferF = new float[numberAtoms * 3];
            trajBufferProcess = new NativeArray<float>(numberAtoms * 3, Allocator.Persistent);
            trajBufferProcessOut = new NativeArray<float3>(numberAtoms, Allocator.Persistent);
        }

        if (frameToTrajBuffer.ContainsKey(frame_number)) { //Already in memory
            return trajectoryBuffer[frameToTrajBuffer[frame_number]];
        }

        //Not in memory => load TRAJBUFFERSIZE/4 before and after = fills half of the buffer
        loadBufferFrames(frame_number);

        return trajectoryBuffer[frameToTrajBuffer[frame_number]];
    }

    /// Load TRAJBUFFERSIZE/4 frames before and after frame_number
    private void loadBufferFrames(int frame_number) {
        int i = 0;

        int startF = frame_number - TRAJBUFFERSIZE / 4;
        while (i < TRAJBUFFERSIZE / 2) {//While not filled half of the array
            if (startF + i < 0) {
                startF++;
                continue;
            }
            if (startF + i >= numberFrames) {
                startF = -i;
                continue;
            }
            int idF = startF + i;
            loadOneFrame(idF);
            i++;
        }
    }

    // Load one frame in the buffer and manage the associated dictionary
    private Vector3[] loadOneFrame(int frame_number) {
        int res = (int)XDRFileWrapper.xdr_seek(file_pointer, offsets[frame_number], (int) SEEK.SEEK_SET);

        int step = 0;
        float time = 0f;
        float precision = 0f;
        int status = next_frame(ref step, ref time, trajectoryBufferF, ref precision);

        //Invert x and multiply by 10
        processFrame(trajectoryBufferF, trajectoryBuffer[idB]);
        // for (int i = 0; i < numberAtoms; i++) {
        //     //Should be a minus sign here
        //     trajectoryBuffer[idB][i] = new Vector3(-trajectoryBufferF[i * 3], trajectoryBufferF[i * 3 + 1], trajectoryBufferF[i * 3 + 2]) * 10.0f;
        // }

        //Remove previous value
        int fToDel = -1;
        foreach (var f in frameToTrajBuffer) {
            if (f.Value == idB) {
                fToDel = f.Key;
            }
        }
        if (fToDel != -1)
            frameToTrajBuffer.Remove(fToDel);

        Vector3[] frame = trajectoryBuffer[idB];

        frameToTrajBuffer[frame_number] = idB;

        idB++;
        if (idB == TRAJBUFFERSIZE) {
            idB = 0;
        }
        return frame;
    }


    public int updateOffsetFile(string trajFile, bool forceCreate = false) {
        offsetFileName = trajFile + ".offset";
        bool offsetExists = File.Exists(offsetFileName);

        if (forceCreate) {
            return createOffsetFile(trajFile);
        }
        if (offsetExists) {
            DateTime lastModif = File.GetLastWriteTime(offsetFileName);
            DateTime creationTraj = File.GetLastWriteTime(trajFile);
            if (lastModif > creationTraj) { //Offset file is posterior to creation of traj file
                try {
                    return readOffsetFile();
                }
                catch {//Failed to read offset file => create one
                }
            }
        }

        return createOffsetFile(trajFile);

    }

    int createOffsetFile(string fileName) {
        try {
            if (!is_trr) {//XTC

                IntPtr outOffsets = IntPtr.Zero;
                int res = 0;
                res = (int)XDRFileWrapper.read_xtc_numframes(fileName, ref numberFrames, ref outOffsets);
                if (res != (int) XDRStatus.exdrOK) {
                    return res;
                }
                offsets = new long[numberFrames];
                Marshal.Copy(outOffsets, offsets, 0, numberFrames);
                // Marshal.FreeCoTaskMem(outOffsets);
            }
            else {
                IntPtr outOffsets = IntPtr.Zero;
                int res = 0;
                res = (int)XDRFileWrapper.read_trr_numframes(fileName, ref numberFrames, ref outOffsets);
                if (res != (int) XDRStatus.exdrOK) {
                    return res;
                }
                offsets = new long[numberFrames];
                Marshal.Copy(outOffsets, offsets, 0, numberFrames);
                // Marshal.FreeCoTaskMem(outOffsets);
            }

            BinaryWriter bw = new BinaryWriter(new FileStream(offsetFileName, FileMode.Create));
            bw.Write((Int32)numberFrames);
            for (int i = 0; i < numberFrames; i++) {
                bw.Write(offsets[i]);
            }
            bw.Close();
        }
        catch (System.Exception e) {
            Debug.LogError(e);
            return (int) XDRFileReaderStatus.OFFSETFILECREATION;
        }

        return (int) XDRFileReaderStatus.SUCCESS;
    }
    int readOffsetFile() {
        try {
            BinaryReader br = new BinaryReader(new FileStream(offsetFileName, FileMode.Open));
            numberFrames = br.ReadInt32();
            if (numberFrames > 0 && numberFrames < 2e9) { //2 billion seems like a fair upper limit for trajectories
                offsets = new long[numberFrames];
                for (int i = 0; i < numberFrames; i++) {
                    offsets[i] = br.ReadInt64();
                }
            }
            else {
                return (int) XDRFileReaderStatus.OFFSETFILETREAD;
            }
        }
        catch {
            return (int) XDRFileReaderStatus.OFFSETFILETREAD;
        }

        return (int) XDRFileReaderStatus.SUCCESS;
    }

// Reads the next frame from the trajectory.
// Feeds parameters passed by reference.
// The array of positions is required to have a size == sizeof(float) * numberAtoms * 3 for this to work properly.
    public int next_frame(ref int step, ref float time, [In, Out] float[] positions, ref float precision) {
        if (file_pointer == System.IntPtr.Zero) {
            Debug.LogWarning("Trajectory was not previously opened.");
            return (int) XDRFileReaderStatus.FILENOTFOUND;
        }

        if (is_trr) {
            float lambda = 0.0f;
            int res = 0;
            res = (int) XDRFileWrapper.read_trr (file_pointer, numberAtoms, ref step, ref time, ref lambda, box, positions, null, null);
            return res;
        } else {
            return (int) XDRFileWrapper.read_xtc (file_pointer, numberAtoms, ref step, ref time, box, positions, ref precision);
        }
    }

    public int sync_scene_with_frame(int frame_number, bool windowMean = false, int windowSize = 5, bool windowForward = true) {
        if (frame_number >= numberFrames) {
            Debug.LogError("Frame number " + frame_number + " does not exist.");
            return (int) XDRFileReaderStatus.FRAMEDOESNOTEXIST;
        }


        if (windowMean) {
            windowSize = Mathf.Max(1, windowSize);
            getFramesToMean(frame_number, windowSize, windowForward);
            trajMean.init(framesToMean, numberAtoms, windowSize);
            trajMean.process(structure.trajAtomPositions);
        }

        else {
            Vector3[] f = getFrame(frame_number);
            structure.trajAtomPositions = f;
        }

        structure.trajUpdateAtomPositions();

        currentFrame = frame_number;
        return (int) XDRFileReaderStatus.SUCCESS;
    }

    public int close_trajectory() {

        trajSmoother.clear();
        trajMean.clear();

        if (file_pointer == System.IntPtr.Zero) {
            //Debug.LogWarning("Trajectory was not previously opened.");
            return (int) XDRFileReaderStatus.FILENOTFOUND;
        }

        XDRStatus result = XDRFileWrapper.xdrfile_close(file_pointer);

        offsets = null;
        file_pointer = System.IntPtr.Zero;
        numberAtoms = 0;
        numberFrames = 0;
        frameToTrajBuffer.Clear();
        structure.trajectoryLoaded = false;

        if (trajBufferProcess.IsCreated) {
            trajBufferProcess.Dispose();
            trajBufferProcessOut.Dispose();
        }

        return (int) result;
    }
    public void Clear() {
        close_trajectory();
        //Clear atomsIMDSimulationLocationlist
        if (structure.trajAtomPositions != null) {
            structure.trajAtomPositions = null;
        }
    }
    public int sync_scene_with_frame_smooth(int frame1, int frame2, float t, bool new_frame = false) {
        if (frame1 >= numberFrames || frame1 < 0) {
            Debug.LogError("Frame number " + frame1 + " does not exist.");
            return (int) XDRFileReaderStatus.FRAMEDOESNOTEXIST;
        }
        if (frame2 >= numberFrames || frame2 < 0) {
            Debug.LogError("Frame number " + frame2 + " does not exist.");
            return (int) XDRFileReaderStatus.FRAMEDOESNOTEXIST;
        }

        t = Mathf.Clamp(t, 0.0f, 1.0f);

        if (cubicInterpolation) {

            int ifm1 = frame1 - 1;
            int if2p1 = frame2 + 1;
            if (ifm1 < 0)
                ifm1 = frame1;
            if (if2p1 >= numberFrames)
                if2p1 = numberFrames - 1;

            Vector3[] fm1 = getFrame(ifm1);
            Vector3[] f1 = getFrame(frame1);
            Vector3[] f2 = getFrame(frame2);
            Vector3[] f3 = getFrame(if2p1);


            trajSmoother.init(fm1, f1, f2, f3);
            // trajSmoother.init(f1, f2);
            trajSmoother.process(structure.trajAtomPositions, t);
        }
        else {
            Vector3[] f1 = getFrame(frame1);
            Vector3[] f2 = getFrame(frame2);
            trajSmoother.init(null, f1, f2, null);
            trajSmoother.process(structure.trajAtomPositions, t);

        }

        structure.trajUpdateAtomPositions();

        //Not always updating currentFrame
        if (new_frame)
            currentFrame = frame1;

        return (int) XDRFileReaderStatus.SUCCESS;
    }

    ///Fills framesToMean array of arrays of positions
    void getFramesToMean(int start, int windowSize, bool forward = true) {
        if (framesToMean == null || framesToMean.Length != windowSize * numberAtoms) {
            framesToMean = new Vector3[windowSize * numberAtoms];
        }
        int count = 0;

        if (forward) {
            for (int i = start; i < numberFrames; i++) {
                Vector3[] f = getFrame(i);
                System.Array.Copy(f, 0, framesToMean, count * numberAtoms, numberAtoms);
                count++;
                if (count == windowSize)
                    break;
            }
            //Fill with last frame when not enough frames
            while (count != windowSize) {
                Vector3[] f = getFrame(numberFrames - 1);
                System.Array.Copy(f, 0, framesToMean, count * numberAtoms, numberAtoms);
                count++;
            }
        }
        else {
            for (int i = start; i >= 0; i--) {
                Vector3[] f = getFrame(i);
                System.Array.Copy(f, 0, framesToMean, count * numberAtoms, numberAtoms);
                count++;
                if (count == windowSize)
                    break;
            }
            //Fill with first frame when not enough frames
            while (count != windowSize) {
                Vector3[] f = getFrame(0);
                System.Array.Copy(f, 0, framesToMean, count * numberAtoms, numberAtoms);
                count++;
            }
        }
    }

    private void processFrame(float[] tBufferF, Vector3[] tBuffer) {

        GetNativeArray(trajBufferProcess, tBufferF);
        var processJob = new ProcessFrameJob() {
            pos = trajBufferProcess,
            outP = trajBufferProcessOut
        };

        var processJobHandle = processJob.Schedule(tBufferF.Length / 3, 64);
        processJobHandle.Complete();

        SetNativeArray(tBuffer, trajBufferProcessOut);

    }
    [BurstCompile]
    struct ProcessFrameJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> pos;
        public NativeArray<float3> outP;

        void IJobParallelFor.Execute(int index)
        {
            float x = pos[index * 3];
            float y = pos[index * 3 + 1];
            float z = pos[index * 3 + 2];

            float3 p = new float3(-x, y, z) * 10.0f;

            outP[index] = p;
        }
    }

    //From https://gist.github.com/LotteMakesStuff/c2f9b764b15f74d14c00ceb4214356b4
    static unsafe void GetNativeArray(NativeArray<float> posNativ, float[] posArray)
    {
        // pin the buffer in place...
        fixed (void* bufferPointer = posArray)
        {
            // ...and use memcpy to copy the Vector3[] into a NativeArray<floar3> without casting. whould be fast!
            UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(posNativ),
                                 bufferPointer, posArray.Length * (long) UnsafeUtility.SizeOf<float>());
        }
    }
    unsafe static void SetNativeArray(Vector3[] posArray, NativeArray<float3> posNativ) {
        // pin the target array and get a pointer to it
        fixed (void * posArrayPointer = posArray) {
            // memcopy the native array over the top
            UnsafeUtility.MemCpy(posArrayPointer, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(posNativ), posArray.Length * (long) UnsafeUtility.SizeOf<float3>());
        }
    }

}
}
