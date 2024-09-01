
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;

using System.Collections.Generic;

namespace UMol {
public class CenterOfGravBurst {

	public static NativeArray<float3> curRes = new NativeArray<float3>(1, Allocator.Persistent);
	public static NativeArray<float3> curMin = new NativeArray<float3>(1, Allocator.Persistent);
	public static NativeArray<float3> curMax = new NativeArray<float3>(1, Allocator.Persistent);

	public static Vector3 computeCOG(List<UnityMolAtom> atoms, ref Vector3 mmin, ref Vector3 mmax) {
		int N = atoms.Count;

		if (N <= 0) {
			return Vector3.zero;
		}

		if (!curRes.IsCreated) {
			curRes = new NativeArray<float3>(1, Allocator.Persistent);
			curMin = new NativeArray<float3>(1, Allocator.Persistent);
			curMax = new NativeArray<float3>(1, Allocator.Persistent);
		}

		NativeArray<float3> atomPos = new NativeArray<float3>(N, Allocator.TempJob);
		for (int i = 0; i < N; i++) {
			atomPos[i] = atoms[i].position;
		}

		var cogJob = new COGJob() {
			pos = atomPos,
			res = curRes,
			cmin = curMin,
			cmax = curMax
		};

		var cogJobHandle = cogJob.Schedule();

		cogJobHandle.Complete();

		mmin = cogJob.cmin[0];
		mmax = cogJob.cmax[0];

		Vector3 result = cogJob.res[0];
		atomPos.Dispose();

		return result;
	}

	public static Vector3 computeCOG(Vector3[] pos, ref Vector3 mmin, ref Vector3 mmax) {
		if (pos.Length <= 0) {
			return Vector3.zero;
		}

		if (!curRes.IsCreated) {
			curRes = new NativeArray<float3>(1, Allocator.Persistent);
			curMin = new NativeArray<float3>(1, Allocator.Persistent);
			curMax = new NativeArray<float3>(1, Allocator.Persistent);
		}

		NativeArray<float3> atomPos = new NativeArray<float3>(pos.Length, Allocator.TempJob);
		GetNativeArray(atomPos, pos);

		var cogJob = new COGJob() {
			pos = atomPos,
			res = curRes,
			cmin = curMin,
			cmax = curMax
		};

		var cogJobHandle = cogJob.Schedule();
		cogJobHandle.Complete();


		mmin = cogJob.cmin[0];
		mmax = cogJob.cmax[0];

		Vector3 result = cogJob.res[0];

		atomPos.Dispose();

		return result;
	}

	//From https://gist.github.com/LotteMakesStuff/c2f9b764b15f74d14c00ceb4214356b4
	static unsafe void GetNativeArray(NativeArray<float3> posNativ, Vector3[] posArray)
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

	[BurstCompile]
	struct COGJob : IJob
	{
		public NativeArray<float3> res;
		public NativeArray<float3> cmin;
		public NativeArray<float3> cmax;
		[ReadOnly] public NativeArray<float3> pos;


		public void Execute()
		{
			res[0] = float3.zero;
			cmin[0] = pos[0];
			cmax[0] = pos[0];
			for (int i = 0; i < pos.Length; i++) {
				res[0] += pos[i];
				cmin[0] = math.min(cmin[0], pos[i]);
				cmax[0] = math.max(cmax[0], pos[i]);
			}
			res[0] /= pos.Length;
		}
	}
}
}