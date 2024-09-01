using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

using KNN;
using KNN.Jobs;

using BurstGridSearch;


namespace UMol {
public class SpatialSearch {

    // ------ Grid based search benchmark (Macbook pro 2016 - i7 2.9Ghz)
    //Closest point-----------------
    //1KX2 : 0.32ms | 0.07ms for reso=1.817
    //1KX2 : 0.35ms | 0.06ms for reso=2.289
    //1BTA : 0.36ms | 0.11ms for reso=1.817
    //1BTA : 0.47ms | 0.08ms for reso=2.289
    //3eam : 1.97ms | 0.26ms for reso=1.817
    //3eam : 1.97ms | 0.21ms for reso=2.289
    //1gru : 13.8ms | 0.75ms for reso=1.817
    //1gru : 6.35ms | 0.71ms for reso=2.289
    //Conclusion -> for large proteins, big resolution = faster to create and to query

    //Search within 4.0 maxK = 20-----------------
    //3eam : 10.7ms for reso=1.0
    //3eam : 8.2ms for reso=1.817
    //3eam : 4.2ms for reso=2.289
    //3eam : 5.6ms for reso=3.0
    //1gru : 45.0ms for reso=1.0
    //1gru : 31.0ms for reso=1.817
    //1gru : 19ms for reso=2.289
    //1gru : 22.2ms for reso=3.0
    //Conclusion -> 2.289 seems to be the sweet spot
    // ------

    static bool useGrid = true;

    public NativeArray<float3> localPositions;

    KnnContainer knnContainer;
    KnnRebuildJob rebuildJob;
    JobHandle tofinish;

    GridSearchBurst gsb;

    public SpatialSearch(List<UnityMolAtom> atoms) {
        localPositions = new NativeArray<float3>(atoms.Count, Allocator.Persistent);
        for (int i = 0; i < atoms.Count; i++)
            localPositions[i] = atoms[i].position;

        if (!useGrid) {
            knnContainer = new KnnContainer(localPositions, false, Allocator.Persistent);

            rebuildJob = new KnnRebuildJob(knnContainer);
            //Build the kdtree
            tofinish = rebuildJob.Schedule();
        }
        else {
            gsb = new GridSearchBurst(2.289f);
            gsb.initGrid(localPositions);
        }
    }

    public SpatialSearch(List<Vector3> pos) {
        localPositions = new NativeArray<float3>(pos.Count, Allocator.Persistent);
        for (int i = 0; i < pos.Count; i++)
            localPositions[i] = pos[i];

        if (!useGrid) {
            knnContainer = new KnnContainer(localPositions, false, Allocator.Persistent);

            rebuildJob = new KnnRebuildJob(knnContainer);
            //Build the kdtree
            tofinish = rebuildJob.Schedule();
        }
        else {
            gsb = new GridSearchBurst(2.289f);
            gsb.initGrid(localPositions);
        }
    }

    public void Recreate(List<Vector3> newPositions) {
        Clean();

        localPositions = new NativeArray<float3>(newPositions.Count, Allocator.Persistent);
        for (int i = 0; i < newPositions.Count; i++)
            localPositions[i] = newPositions[i];


        if (useGrid) {
            gsb.updatePositions(localPositions);
        }
        else {
            knnContainer = new KnnContainer(localPositions, false, Allocator.Persistent);

            rebuildJob = new KnnRebuildJob(knnContainer);
            //Build the kdtree
            tofinish = rebuildJob.Schedule();
        }
    }

    public void Recreate(List<UnityMolAtom> newAtoms) {
        Clean();

        localPositions = new NativeArray<float3>(newAtoms.Count, Allocator.Persistent);
        for (int i = 0; i < newAtoms.Count; i++)
            localPositions[i] = newAtoms[i].position;

        if (useGrid) {
            gsb.updatePositions(localPositions);
        }
        else {
            knnContainer = new KnnContainer(localPositions, false, Allocator.Persistent);

            rebuildJob = new KnnRebuildJob(knnContainer);
            //Build the kdtree
            tofinish = rebuildJob.Schedule();
        }
    }

    public void Clean() {
        if (useGrid)
            gsb.clean();
        else
            knnContainer.Dispose();

        localPositions.Dispose();
    }

    ///Local positions of the structure changed, update them in the spatial structure
    public void UpdatePositions(List<UnityMolAtom> atoms) {
        for (int i = 0; i < atoms.Count; i++)
            localPositions[i] = atoms[i].position;
    }

    ///Local positions of the structure changed, update them in the spatial structure
    public void UpdatePositions(List<Vector3> pos) {
        if (pos.Count == localPositions.Length) {
            for (int i = 0; i < pos.Count; i++)
                localPositions[i] = pos[i];
            if (useGrid)
                gsb.updatePositions(localPositions);
            else
                tofinish = rebuildJob.Schedule();
        }
    }

    ///Local positions of the structure changed, update them in the spatial structure
    public void UpdatePositions(Vector3[] pos) {
        if (pos.Length == localPositions.Length) {
            GetNativeArray(localPositions, pos);
            if (useGrid)
                gsb.updatePositions(localPositions);
            else
                tofinish = rebuildJob.Schedule();
        }
    }

    NativeArray<int> _SearchWithinGrid(NativeArray<float3> qPositions, float radius, int maxNeighbor) {
        return gsb.searchWithin(qPositions, radius, maxNeighbor);
    }

    NativeArray<int> _SearchWithinKD(NativeArray<float3> qPositions, float radius, int maxNeighbor) {

        NativeArray<int> results = new NativeArray<int>(qPositions.Length * maxNeighbor, Allocator.TempJob);

        // Store a list of particles in range
        var rangeResults = new NativeArray<RangeQueryResult>(1, Allocator.TempJob);

        // Unfortunately, for batch range queries we do need to decide upfront the maximum nr. of neighbours we allow
        // This is due to limitation on allocations within a job.
        for (int i = 0; i < rangeResults.Length; ++i) {
            rangeResults[i] = new RangeQueryResult(maxNeighbor, Allocator.TempJob);
        }

        // Fire up job to get results for all points
        var batchRange = new QueryRangeBatchJob(knnContainer, qPositions, radius, rangeResults);

        // And just run immediately now. This will run on multiple threads!
        batchRange.ScheduleBatch(qPositions.Length, Mathf.Max(1, qPositions.Length / 32)).Complete();


        for (int i = 0; i < rangeResults.Length; i++) {
            int j = 0;
            for (; j < rangeResults[i].Length; j++) {
                results[i * maxNeighbor + j] = rangeResults[i][j];
            }
            //Fill the rest with -1
            for (; j < maxNeighbor; j++) {
                results[i * maxNeighbor + j] = -1;
            }
        }

        foreach (var r in rangeResults) {
            r.Dispose();
        }
        rangeResults.Dispose();

        return results;
    }

    public NativeArray<int> SearchWithin(Vector3[] queries, float radius, int maxNeighbor) {
        NativeArray<float3> qPositions = new NativeArray<float3>(queries.Length, Allocator.TempJob);
        GetNativeArray(qPositions, queries);

        NativeArray<int> results;
        if (useGrid) {
            results = _SearchWithinGrid(qPositions, radius, maxNeighbor);
        }
        else {
            //Finish the rebuild job if needed
            tofinish.Complete();

            results = _SearchWithinKD(qPositions, radius, maxNeighbor);
        }
        qPositions.Dispose();
        return results;
    }

    public NativeArray<int> SearchWithin(List<Vector3> queries, float radius, int maxNeighbor) {
        NativeArray<float3> qPositions = new NativeArray<float3>(queries.Count, Allocator.TempJob);
        for (int i = 0; i < queries.Count; i++)
            qPositions[i] = queries[i];

        NativeArray<int> results;

        if (useGrid) {
            results = _SearchWithinGrid(qPositions, radius, maxNeighbor);
        }
        else {
            //Finish the rebuild job if needed
            tofinish.Complete();

            results = _SearchWithinKD(qPositions, radius, maxNeighbor);
        }

        qPositions.Dispose();
        return results;
    }

    ///Query atoms should belong to the same structure than the kdtree
    public NativeArray<int> SearchWithin(List<UnityMolAtom> atomsQ, float radius, int maxNeighbor) {
        NativeArray<int> results;

        NativeArray<float3> qPositions = new NativeArray<float3>(atomsQ.Count, Allocator.TempJob);
        for (int i = 0; i < atomsQ.Count; i++)
            qPositions[i] = atomsQ[i].position;

        if (useGrid) {
            results = _SearchWithinGrid(qPositions, radius, maxNeighbor);
        }
        else {
            //Finish the rebuild job if needed
            tofinish.Complete();

            results = _SearchWithinKD(qPositions, radius, maxNeighbor);
        }

        qPositions.Dispose();
        return results;
    }

    ///Query atoms should belong to the same structure than the kdtree
    public NativeArray<int> SearchWithin(HashSet<UnityMolAtom> atomsQ, float radius, int maxNeighbor) {
        NativeArray<int> results;
        NativeArray<float3> qPositions = new NativeArray<float3>(atomsQ.Count, Allocator.TempJob);
        int ida = 0;
        foreach (UnityMolAtom a in atomsQ) {
            qPositions[ida] = a.position;
            ida++;
        }
        if (useGrid) {
            results = _SearchWithinGrid(qPositions, radius, maxNeighbor);
        }
        else {
            //Finish the rebuild job if needed
            tofinish.Complete();

            results = _SearchWithinKD(qPositions, radius, maxNeighbor);
        }

        qPositions.Dispose();

        return results;
    }

    // ///Query atoms should belong to the same structure than the kdtree
    // public NativeArray<int> SearchKNeighbors(List<UnityMolAtom> atomsQ, int k) {


    //     //Finish the rebuild job if needed
    //     tofinish.Complete();

    //     NativeArray<float3> qPositions = new NativeArray<float3>(atomsQ.Count, Allocator.TempJob);
    //     for (int i = 0; i < atomsQ.Count; i++)
    //         qPositions[i] = atomsQ[i].position;

    //     NativeArray<int> results = new NativeArray<int>(atomsQ.Count * k, Allocator.TempJob);

    //     var batchQueryJob = new QueryKNearestBatchJob(knnContainer, qPositions, results);
    //     var handle = batchQueryJob.ScheduleBatch(qPositions.Length, Mathf.Max(16, qPositions.Length / 32));
    //     handle.Complete();

    //     qPositions.Dispose();

    //     return results;
    // }

    public NativeArray<int> SearchClosestPoint(Vector3[] qpoints, bool checkSelf = false, float epsilon = 0.001f) {
        NativeArray<float3> qPositions = new NativeArray<float3>(qpoints.Length, Allocator.TempJob);
        GetNativeArray(qPositions, qpoints);
        NativeArray<int> results;
        if (useGrid) {
            results = gsb.searchClosestPoint(qPositions, checkSelf, epsilon);
        }
        else {
            //Finish the rebuild job if needed
            tofinish.Complete();

            results = new NativeArray<int>(qpoints.Length, Allocator.TempJob);

            var batchQueryJob = new QueryKNearestBatchJob(knnContainer, qPositions, results);
            var handle = batchQueryJob.ScheduleBatch(qPositions.Length, Mathf.Max(16, qPositions.Length / 32));
            handle.Complete();
        }
        qPositions.Dispose();

        return results;

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

}
}