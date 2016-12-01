using UnityEngine;
using System.Collections;


public class ParticleOnCollision : MonoBehaviour{

	public GameObject hit;
	public GameObject playerHit;

	void OnCollisionEnter (Collider other)
	{
		if (other.tag == "Boundary") {
			return;
		}
		Instantiate (hit, transform.position, transform.rotation);
		if (other.tag == "Player") {
			Instantiate (playerHit, other.transform.position, other.transform.rotation);
		}


	}



}