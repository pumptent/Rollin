using UnityEngine;
using System.Collections;

public class FollowBall2 : MonoBehaviour {
	private const float Y_ANGLE_MIN = -50.0f;
	private const float Y_ANGLE_MAX = 50.0f;

	public Transform lookAt;
	public Transform camTransform;

	private Camera cam;

	private float distance = 5.0f;
	private float currentX = -90.0f;
	private float currentY = 20.0f;
	//private float sensitivityX = 4.0f;
	//private float sensitivityY = 1.0f;

	// Use this for initialization
	void Start () {
		camTransform = transform;
		cam = Camera.main;
	}
	
	// Update is called once per frame
	void Update () {
		//currentX += Input.GetAxis ("Mouse X");
		//currentY += Input.GetAxis ("Mouse Y");

		currentY = Mathf.Clamp (currentY, Y_ANGLE_MIN, Y_ANGLE_MAX);
	}

	private void LateUpdate () {
		Vector3 dir = new Vector3 (0, 0, -distance);
		Quaternion rotation = Quaternion.Euler (currentY, currentX, 0);
		camTransform.position = lookAt.position + rotation * dir;
		camTransform.LookAt (lookAt.position);
	}
}
