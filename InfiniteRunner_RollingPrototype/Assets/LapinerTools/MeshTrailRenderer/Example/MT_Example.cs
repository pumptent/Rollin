using UnityEngine;
#if UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using System.Collections;

public class MT_Example : MonoBehaviour
{
	public enum EObjectType {SLEDGE, BALL}
	[System.Serializable]
	public class ObjectEntry { public Transform m_transform; public float m_camDist; public EObjectType m_type; public bool m_fixCamUp; public string m_text; }

	public float m_speed = 10f;
	public float m_torque = 1f;
	public ObjectEntry[] m_objects;
	
	private int m_lookAtIndex = 0;
	private ObjectEntry m_lookAt;
	private bool m_isPlaying = false;
	private Vector3 m_camV = Vector3.zero;
	
	private float m_lastObjectSwitchTime = -999;

	private void Start()
	{
		if (m_objects != null && m_objects.Length > 0)
		{
			m_lookAt = m_objects[m_lookAtIndex];
		}

#if UNITY_5
		// materials don't have lateral friction in Unity 5 -> some custom friction logic
		m_speed *= 0.7f; // Unity 5 physics are faster
		Terrain terrain = Terrain.activeTerrain;
		if(terrain != null)
		{
			TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
			if (collider != null)
			{
				collider.material.staticFriction = 0f;
				collider.material.bounciness = 0f;
				collider.material.dynamicFriction *= 0.75f;
			}
		}
#endif
	}

	private void FixedUpdate ()
	{
		if (m_lookAt != null)
		{
#if UNITY_5
						if (m_lookAt.m_type == EObjectType.SLEDGE)
						{
							// materials don't have lateral friction in Unity 5 -> some custom friction logic
							Rigidbody body = m_lookAt.m_transform.GetComponent<Rigidbody>();
							float lateralSpeed = Vector3.Dot(body.velocity, body.transform.right);
							if (Mathf.Abs(lateralSpeed) > 0.5f)
							{
									body.AddForce(- lateralSpeed * body.transform.right);
							}
						}
#endif

			if (m_isPlaying)
			{
				Rigidbody body = m_lookAt.m_transform.GetComponent<Rigidbody>();
				if (body != null)
				{
					if (m_lookAt.m_type == EObjectType.SLEDGE)
					{
						if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) { body.AddForce(m_speed*m_lookAt.m_transform.forward); }
						if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) { body.AddForce(-m_speed*m_lookAt.m_transform.forward); }
						if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) { body.AddRelativeTorque(0, -m_torque, 0); }
						if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) { body.AddRelativeTorque(0, m_torque, 0); }
					}
					else if (m_lookAt.m_type == EObjectType.BALL)
					{
						if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) { body.AddForce(m_speed*Camera.main.transform.forward); }
						if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) { body.AddForce(-m_speed*Camera.main.transform.forward); }
						if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) { body.AddForce(-m_speed*Camera.main.transform.right); }
						if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) { body.AddForce(m_speed*Camera.main.transform.right); }
					}
					// camera position
					Vector3 camForwardVector;
					if (body.velocity.magnitude>0.15f) { camForwardVector = body.velocity.normalized; }
					else if (Mathf.Abs(body.transform.forward.y) < 0.8f) { camForwardVector = body.transform.forward; }
					else if (Mathf.Abs(body.transform.up.y) < 0.8f) { camForwardVector = body.transform.up; }
					else { camForwardVector = body.transform.right; }
					Camera.main.transform.position = Vector3.SmoothDamp(Camera.main.transform.position, body.transform.position + -camForwardVector*m_lookAt.m_camDist + (m_lookAt.m_fixCamUp ? body.transform.up : Vector3.up), ref m_camV, 0.5f);
				}
			}
			// make the camera look at moving object
			Camera.main.transform.LookAt(m_lookAt.m_transform, m_lookAt.m_fixCamUp ? m_lookAt.m_transform.up : Vector3.up);
		}
	}
	
	private void OnGUI()
	{
		GUI.color = Color.black;
		GUILayout.Label("<b>Snow Mesh Trail Renderer get it from Unity Asset Store (v2.00)</b>");
		GUI.color = Color.white;
		if (m_lookAt != null)
		{
			if (m_isPlaying)
			{
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("Restart"))
				{
#if UNITY_5_3_OR_NEWER
					SceneManager.LoadScene(0);
#else
					Application.LoadLevel(0);
#endif
				}
				if (GUILayout.Button("Turn Around"))
				{
					m_lookAt.m_transform.position += Vector3.up;
					m_lookAt.m_transform.up = Vector3.up;
				}
				if (GUILayout.Button("Next Object"))
				{
					m_lastObjectSwitchTime = Time.realtimeSinceStartup;
					m_lookAtIndex = (m_lookAtIndex+1)%m_objects.Length;
					m_lookAt = m_objects[m_lookAtIndex];
				}
				if (GUILayout.Button("Clear Trail"))
				{
					MT_MeshTrail.MT_MeshTrailRenderer[] trails = m_lookAt.m_transform.GetComponentsInChildren<MT_MeshTrail.MT_MeshTrailRenderer>();
					for (int i = 0; i < trails.Length; i++)
					{
						trails[i].Clear();
					}
				}
				GUILayout.EndHorizontal();
				GUI.color = Color.black;
				GUILayout.Label("Move with AWSD or arrow keys.");
				
				GUI.color = new Color(1, 0, 0, (4f - (Time.realtimeSinceStartup - m_lastObjectSwitchTime)) / 3f);
				GUIStyle textStyle = null;
				if (GUI.skin.GetStyle("label") != null)
				{
					textStyle = new GUIStyle(GUI.skin.GetStyle("label"));
					textStyle.fontSize = 22;
				}
				if (textStyle != null)
				{
     				GUI.Label(new Rect(Screen.width - 375f, Screen.height - 75f, 375f, 75f), m_lookAt.m_transform.name + "\n" +  m_lookAt.m_text, textStyle);
				}
			}
			else
			{
				if (GUILayout.Button("<size=24><b>PLAY</b></size>", GUILayout.Width(80), GUILayout.Height(40)))
				{
					m_isPlaying = true;
					// the first sledge has extra force down to be faster while not under control
					if (m_lookAt.m_transform.GetComponent<ConstantForce>() != null)
					{
						m_lookAt.m_transform.GetComponent<ConstantForce>().force *= 0.5f;
					}
				}
			}
		}
	}
}
