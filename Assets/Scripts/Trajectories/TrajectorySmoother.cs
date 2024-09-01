
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;

namespace UMol {
public class TrajectorySmoother {


	NativeArray<float3> positionsTm1;
	NativeArray<float3> positionsT1;
	NativeArray<float3> positionsT2;
	NativeArray<float3> positionsT3;
	NativeArray<float3> result;
	public bool cubic = false;

	public void init(Vector3[] postm1, Vector3[] post1, Vector3[] post2, Vector3[] post3) {
		cubic = false;
		if (postm1 != null && post3 != null) {
			cubic = true;
		}

		if (positionsT1 == null || positionsT1.Length == 0) {
			positionsT1 = new NativeArray<float3>(post1.Length, Allocator.Persistent);
			positionsT2 = new NativeArray<float3>(post2.Length, Allocator.Persistent);
			result = new NativeArray<float3>(post1.Length, Allocator.Persistent);
		}
		if (cubic) {
			if (positionsTm1 == null || positionsTm1.Length == 0) {
				positionsTm1 = new NativeArray<float3>(postm1.Length, Allocator.Persistent);
				positionsT3 = new NativeArray<float3>(post3.Length, Allocator.Persistent);
			}
		}


		if (post1.Length == post2.Length && post1.Length == positionsT1.Length) {
			GetNativeArray(positionsT1, post1);
			GetNativeArray(positionsT2, post2);
		}
		if (cubic) {
			GetNativeArray(positionsTm1, postm1);
			GetNativeArray(positionsT3, post3);
		}
	}

	public void clear() {
		if (positionsT1.IsCreated) {
			positionsT1.Dispose();
			positionsT2.Dispose();
		}
		if (positionsTm1.IsCreated) {
			positionsTm1.Dispose();
			positionsT3.Dispose();
		}
		if (result.IsCreated) {
			result.Dispose();
		}
	}

	public void process(Vector3[] outPos, float t) {

		if (positionsT1.Length == positionsT2.Length && outPos.Length == positionsT1.Length) {
			if (cubic) {
				var smoothJob = new SmoothedPositionsCubic() {
					interpolatedPositions = result,
					posT1 = positionsTm1,
					posT2 = positionsT1,
					posT3 = positionsT2,
					posT4 = positionsT3,
					step = t
				};

				var smoothJobHandle = smoothJob.Schedule(positionsT1.Length, 64);
				smoothJobHandle.Complete();
			}
			else {
				var smoothJob = new SmoothedPositions() {
					interpolatedPositions = result,
					posT1 = positionsT1,
					posT2 = positionsT2,
					step = t
				};

				var smoothJobHandle = smoothJob.Schedule(positionsT1.Length, 64);
				smoothJobHandle.Complete();
			}

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
	struct SmoothedPositions : IJobParallelFor
	{
		public NativeArray<float3> interpolatedPositions;
		[ReadOnly] public NativeArray<float3> posT1;
		[ReadOnly] public NativeArray<float3> posT2;
		[ReadOnly] public float step;

		void IJobParallelFor.Execute(int index)
		{
			interpolatedPositions[index] = math.lerp(posT1[index], posT2[index], step);
		}
	}

	[BurstCompile]
	struct SmoothedPositionsCubic : IJobParallelFor
	{
		public NativeArray<float3> interpolatedPositions;
		[ReadOnly] public NativeArray<float3> posT1;//n-1
		[ReadOnly] public NativeArray<float3> posT2;//n
		[ReadOnly] public NativeArray<float3> posT3;//n+1
		[ReadOnly] public NativeArray<float3> posT4;//n+2
		[ReadOnly] public float step;

		void IJobParallelFor.Execute(int index)
		{
			float3 p0 = posT1[index];
			float3 p1 = posT2[index];
			float3 p2 = posT3[index];
			float3 p3 = posT4[index];

			float3 v0 = (p2 - p0) * 0.5f;
			float3 v1 = (p3 - p1) * 0.5f;

			float s2 = step * step;
			float s3 = step * s2;

			interpolatedPositions[index] = (2.0f * p1 - 2.0f * p2 + v0 + v1) * s3 + (-3.0f * p1 + 3.0f * p2 - 2.0f * v0 - v1) * s2 + v0 * step + p1;
		}
	}

}
}