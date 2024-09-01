using UnityEngine;
public class ExplosionPooled : PooledObject {

	public ParticleSystem ps;
	void Awake(){
		ps = GetComponentInChildren<ParticleSystem>();
	}
	void Update(){
		if(ps && !ps.IsAlive()){
			ps.GetComponent<Renderer>().enabled = false;
			ReturnToPool();
		}
	}
}