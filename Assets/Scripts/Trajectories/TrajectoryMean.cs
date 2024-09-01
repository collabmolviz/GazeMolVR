
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;

namespace UMol {
public class TrajectoryMean {


    int nbAtoms;
    int windowSize;
    NativeArray<float3> positionsInput;

    NativeArray<float3> result;

    public void init(Vector3[] positions, int NAtoms, int window) {
        nbAtoms = NAtoms;

        if (positionsInput == null || positionsInput.Length == 0 || window != windowSize) {
            if (positionsInput.IsCreated) {
                positionsInput.Dispose();
            }
            positionsInput = new NativeArray<float3>(window * nbAtoms, Allocator.Persistent);
        }
        windowSize = window;
        if (result == null || result.Length != nbAtoms) {
            if (result.IsCreated) {
                result.Dispose();
            }
            result = new NativeArray<float3>(nbAtoms, Allocator.Persistent);
        }

        GetNativeArray(positionsInput, positions);
    }

    public void clear() {
        if (positionsInput.IsCreated) {
            positionsInput.Dispose();
        }
        if (result.IsCreated) {
            result.Dispose();
        }
    }

    public void process(Vector3[] outPos) {

        if (outPos.Length == nbAtoms && windowSize > 0) {

            var meanJob = new MeanPositions() {
                meanPos = result,
                pos = positionsInput,
                natoms = nbAtoms,
                window = windowSize
            };

            var meanJobHandle = meanJob.Schedule(nbAtoms, 256);
            meanJobHandle.Complete();

            SetNativeArray(outPos, result);
        }
        else {
            Debug.LogError("Wrong sizes, did you call init ?");
        }

    }

    //From https://gist.github.com/LotteMakesStuff/c2f9b764b15f74d14c00ceb4214356b4
    unsafe void GetNativeArray(NativeArray<float3> posNativ, Vector3[] posArray)
    {

        // pin the buffer in place...
        fixed (void* bufferPointer = posArray)
        {
            // ...and use memcpy to copy the Vector3[] into a NativeArray<floar3> without casting. whould be fast!
            UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(posNativ),
                                 bufferPointer, posArray.Length * (long) UnsafeUtility.SizeOf<float3>());
        }
        // we only have to fix the .net array in place, the NativeArray is allocated in the C++ side of the engine and
        // wont move arround unexpectedly. We have a pointer to it not a reference! thats basically what fixed does,
        // we create a scope where its 'safe' to get a pointer and directly manipulate the array

    }
    unsafe void SetNativeArray(Vector3[] posArray, NativeArray<float3> posNativ)
    {
        // pin the target array and get a pointer to it
        fixed (void* posArrayPointer = posArray)
        {
            // memcopy the native array over the top
            UnsafeUtility.MemCpy(posArrayPointer, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(posNativ), posArray.Length * (long) UnsafeUtility.SizeOf<float3>());
        }
    }

    [BurstCompile]
    struct MeanPositions : IJobParallelFor
    {
        public NativeArray<float3> meanPos;
        [ReadOnly] public NativeArray<float3> pos;
        [ReadOnly] public int window;
        [ReadOnly] public int natoms;

        void IJobParallelFor.Execute(int index)
        {
            meanPos[index] = 0.0f;
            for (int i = 0; i < window; i++) {
                meanPos[index] += pos[i * natoms + index];
            }
            meanPos[index] /= window;
        }
    }

}
}