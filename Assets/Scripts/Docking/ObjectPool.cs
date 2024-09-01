using UnityEngine;
using System.Collections.Generic;

public class ObjectPool : MonoBehaviour {

	public PooledObject prefab;
	public int activeCount = 0;

	List<PooledObject> availableObjects = new List<PooledObject>();

	// public static ObjectPool GetPool (PooledObject prefab) {
	// 	GameObject obj;
	// 	ObjectPool pool;
	// 	if (Application.isEditor) {
	// 		obj = GameObject.Find(prefab.name + " Pool");
	// 		if (obj) {
	// 			pool = obj.GetComponent<ObjectPool>();
	// 			if (pool) {
	// 				return pool;
	// 			}
	// 		}
	// 	}
	// 	obj = new GameObject(prefab.name + " Pool");
	// 	pool = obj.AddComponent<ObjectPool>();
	// 	pool.prefab = prefab;
	// 	return pool;
	// }

	public PooledObject GetObject () {
		PooledObject obj;
		int lastAvailableIndex = availableObjects.Count - 1;
		if (lastAvailableIndex >= 0) {
			obj = availableObjects[lastAvailableIndex];
			availableObjects.RemoveAt(lastAvailableIndex);
			obj.gameObject.SetActive(true);
		}
		else {
			obj = Instantiate<PooledObject>(prefab);
			// obj.transform.SetParent(transform, false);
			obj.Pool = this;
			obj.transform.position = new Vector3(0.0f,0.0f,10.0f);
		}
		activeCount++;
		return obj;
	}

	public void AddObject (PooledObject obj) {
		obj.gameObject.SetActive(false);
		availableObjects.Add(obj);
		activeCount--;
	}
}