using UnityEngine;
using System.Collections;


public class ParticleOnCollision : MonoBehaviour{

	public ParticleSystem _psystem;

	void Awake() {
		_psystem = GetComponent<ParticleSystem> ();
	}

	void OnTriggerEnter (Collider col)  {


		_psystem.Play();

	}

}