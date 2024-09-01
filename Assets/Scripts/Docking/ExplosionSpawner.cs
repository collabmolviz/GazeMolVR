using UnityEngine;
using System.Linq;

public class ExplosionSpawner : MonoBehaviour {
	public static ExplosionPooled[] explosions;

	public static int maxCount = 100;
	private ObjectPool myPool;

	public PooledObject exploPrefab;

	void Start(){
		myPool = gameObject.AddComponent<ObjectPool>();
		if(exploPrefab == null)
			exploPrefab = (PooledObject) Resources.Load("Prefabs/PooledExplosion");

		myPool.prefab = exploPrefab;
	}

	public void SpawnExplosion (Transform t, Vector3 position) {
		if(myPool.activeCount >= maxCount){
			return;
		}

		// ExplosionPooled prefab = explosions.Last();
		// ExplosionPooled spawn = prefab.GetPooledInstance<ExplosionPooled>();
		ExplosionPooled spawn = (ExplosionPooled)myPool.GetObject();
		spawn.transform.SetParent(t);
		spawn.transform.position = position;
		spawn.ps.GetComponent<Renderer>().enabled = true;
	}
	public void testPool(){
		SpawnExplosion(transform, Vector3.zero);
	}

}