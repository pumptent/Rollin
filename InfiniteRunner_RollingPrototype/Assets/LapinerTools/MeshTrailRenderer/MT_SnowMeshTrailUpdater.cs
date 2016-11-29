using UnityEngine;
using System.Collections;

namespace MT_MeshTrail
{
	/// <summary>
	/// The MT_SnowMeshTrailUpdater is the script that uses the MT_MeshTrailRenderer to draw a snow trail on the Unity Terrain. 
	/// Additionally, this script will instantiate the material of the mesh trail renderer and change its render queue order to 
	/// prevent crossing trails from z-fighting. All trails managed by MT_SnowMeshTrailUpdater scripts will have a defined rendering order. 
	/// For example, the first created trail will always be behind the second created trail.
	/// </summary>
	public class MT_SnowMeshTrailUpdater : MonoBehaviour
	{
		public enum ERaycastMode { WORLD_DOWN_AXIS, LOCAL_DOWN_AXIS, CUSTOM_AXIS }
		public enum EUpdateMode { ONCE_PER_FRAME, ON_FIXED_UPDATE }
		public enum Direction { FORWARD, BACKWARD, RIGHT, LEFT, UP, DOWN }

		private static int s_materialRenderQueueOffset = 0;
		
		[SerializeField]
		private bool IS_DRAWING = true;
		/// <summary>
		/// Set this to false when you need to disable the trail. For example,
		/// if your player is in air and you know that there should be no trail,
		/// then you can hide the trail with "IsDrawing = false;".
		/// </summary>
		public bool IsDrawing
		{
			get{ return m_trail != null ? IS_DRAWING && m_trail.IsDrawing : false; }
			set{ IS_DRAWING = value; }
		}
		[SerializeField]
		private EUpdateMode UPDATE_MODE = EUpdateMode.ONCE_PER_FRAME;
		/// <summary>
		/// Depending on your code it might have a difference when to update the trails state. For example, if you use rigidbodies you 
		/// should select the ON_FIXED_UPDATE mode to let the script work in the FixedUpdate function. This way the result will be more accurate. 
		/// Then again if you move your objects only in the Update function and do not use rigidbodies then you need to let the calculations 
		/// happen in the Update loop (ONCE_PER_FRAME mode). Also, ONCE_PER_FRAME is usually faster. Therefore, if you seek for optimization you 
		/// can use this mode and sacrifice accuracy when used with rigidbodies (useful only on mobile, since the performance implications are very small).
		/// </summary>
		public EUpdateMode UpdateMode
		{
			get{ return UPDATE_MODE; }
			set{ UPDATE_MODE = value; }
		}
		[SerializeField]
		private ERaycastMode RAYCAST_MODE = ERaycastMode.WORLD_DOWN_AXIS;
		/// <summary>
		/// Use one of the following raycast modes.
		/// WORLD_DOWN_AXIS: raycasts will go down on the world Y axis.
		/// LOCAL_DOWN_AXIS: raycasts will go down on the object's local Y axis.
		/// CUSTOM_AXIS: the raycast axis is defined in the RAYCAST_CUSTOM_AXIS property. It can be changed on runtime (e.g. for a planet scenario).
		/// </summary>
		public ERaycastMode RaycastMode
		{
			get{ return RAYCAST_MODE; }
			set{ RAYCAST_MODE = value; }
		}
		[SerializeField]
		private Vector3 RAYCAST_CUSTOM_AXIS = Vector3.down;
		/// <summary>
		/// If RAYCAST_MODE is set to CUSTOM_AXIS, then raycasts will follow the given vector. This property can be changed on runtime (e.g. for a planet scenario).
		/// </summary>
		public Vector3 RaycastCustomAxis
		{
			get{ return RAYCAST_CUSTOM_AXIS; }
			set{ RAYCAST_CUSTOM_AXIS = value; }
		}
		[SerializeField]
		private bool IS_POSITION_BY_RAYCAST = true;
		/// <summary>
		/// If true, then the trail will snap to the raycasted object below the original trail position. 
		/// This way the trail will look more accurate with less bumps in it.
		/// </summary>
		public bool IsPostionByRaycast
		{
			get{ return IS_POSITION_BY_RAYCAST; }
			set{ IS_POSITION_BY_RAYCAST = value; }
		}
		[SerializeField]
		private bool IS_ROTATION_BY_RAYCAST = true;
		/// <summary>
		/// If true, then the normal orientation of the trail will be adapted to the raycasted object below the trail object. 
		/// For example, if you have a sledge that is slipping on the side then you don't want the trail to be on the side too. 
		/// It is more likely that you will want to draw the trail directly on the terrain with its normal pointing up and not to the side.
		/// </summary>
		public bool IsRotationByRaycast
		{
			get{ return IS_ROTATION_BY_RAYCAST; }
			set{ IS_ROTATION_BY_RAYCAST = value; }
		}
		[SerializeField]
		private bool IS_ROTATE_TOWARDS_MOVEMENT_DIRECTION = false;
		/// <summary>
		/// If true, then this game object will be rotated towards move direction in the update function of the selected update mode. 
		/// The up vector will be rotated to match the up vector of the parent as good as possible.
		/// </summary>
		public bool IsRotateTowardsMovementDirection
		{
			get{ return IS_ROTATE_TOWARDS_MOVEMENT_DIRECTION; }
			set
			{
				IS_ROTATE_TOWARDS_MOVEMENT_DIRECTION = value;
				if (IS_ROTATE_TOWARDS_MOVEMENT_DIRECTION)
				{
					m_trail.IsRotateTowardsMovementDirection = false; // this script will handle the rotation
				}
			}
		}
		[SerializeField]
		private LayerMask SNOW_LAYERS = 1;
		/// <summary>
		/// Only this layers will be used for raycasts. A raycast is made in every update loop (depending on update mode) to determine 
		/// if the object is on the ground or in air.
		/// </summary>
		public LayerMask Snowlayers
		{
			get{ return SNOW_LAYERS; }
			set{ SNOW_LAYERS = value; }
		}
		[SerializeField]
		private float MAX_DISTANCE_TO_SNOW = 1.45f;
		/// <summary>
		/// This is the maximal distance that raycasts can have. A raycast is made in every update loop (depending on update mode) to determine 
		/// if the object is on the ground or in air. If there is no object from the SNOW_LAYERS within this given distance, then the object is 
		/// considered to be in air.
		/// </summary>
		public float MaxDistanceToSnow
		{
			get{ return MAX_DISTANCE_TO_SNOW; }
			set{ MAX_DISTANCE_TO_SNOW = value; }
		}

		private MT_MeshTrailRenderer m_trail;
		private Vector3 m_originalLocalPosition;
		private Vector3 m_lastPosition;

		private Vector3 RaycastUp
		{
			get
			{
				if (RAYCAST_MODE == ERaycastMode.WORLD_DOWN_AXIS)
				{
					return Vector3.up;
				}
				else if (RAYCAST_MODE == ERaycastMode.LOCAL_DOWN_AXIS)
				{
					return transform.up;
				}
				else
				{
					return -RAYCAST_CUSTOM_AXIS;
				}
			}
		}
		private Vector3 RaycastDown { get{ return -1f * RaycastUp; } }

		private void Start ()
		{
			// get trail
			m_trail = GetComponent<MT_MeshTrailRenderer>();
			if (m_trail == null)
			{
				Debug.LogError("MT_SnowMeshTrailUpdater: could not find a MT_MeshTrailRenderer component!");
				Destroy(this);
				return;
			}
			else
			{
				if (IS_ROTATE_TOWARDS_MOVEMENT_DIRECTION)
				{
					// this script will handle the rotation
					m_trail.IsRotateTowardsMovementDirection = false;
				}
				// save the trail setup
				m_originalLocalPosition = m_trail.transform.localPosition;
				m_lastPosition = transform.position;
			}
			// check if game object has a parent
			if (transform.parent == null)
			{
				Debug.LogError("MT_SnowMeshTrailUpdater: needs parent!");
				Destroy(this);
				return;
			}
			// increment material render queue -> prevent multiple trails from z-fighting
			Material mat = m_trail.Material;
			if (mat != null)
			{
				mat = (Material)Instantiate(mat);
				mat.renderQueue += s_materialRenderQueueOffset++;
				m_trail.Material = mat;
				if (s_materialRenderQueueOffset > 70)
				{
					s_materialRenderQueueOffset = 0;
				}
			}
		}

		private void Update()
		{
			if (UPDATE_MODE == EUpdateMode.ONCE_PER_FRAME)
			{
				DoUpdate();
			}
		}

		private void FixedUpdate()
		{
			if (UPDATE_MODE == EUpdateMode.ON_FIXED_UPDATE)
			{
				DoUpdate();
			}
		}

		private void DoUpdate()
		{
			// rotate to make the forward vector match the movement direction
			if (IS_ROTATE_TOWARDS_MOVEMENT_DIRECTION && m_trail.LastDrawLocation != MT_MeshTrailRenderer.INIT_LAST_DRAW_LOCATION)
			{
				Vector3 move = transform.position - m_lastPosition;
				if (move.sqrMagnitude > 0.00001f)
				{
					transform.LookAt(transform.position + move, transform.parent.up);
				}
				m_lastPosition = transform.position;
			}
			// check if object touches the snow
			bool isDrawing = UpdateIsDrawing();
			// draw trail if object is on the ground
			m_trail.IsDrawing = isDrawing;
		}

		private bool UpdateIsDrawing()
		{
			// check for collision of the object with the snow by raycasting
			RaycastHit hitInfo;
			if (IS_POSITION_BY_RAYCAST)
			{
				transform.localPosition = m_originalLocalPosition;
			}
			if (IS_DRAWING && Raycast(out hitInfo))
			{
				if (IS_POSITION_BY_RAYCAST)
				{
					transform.position = hitInfo.point + RaycastUp*0.05f;
				}
				if (IS_ROTATION_BY_RAYCAST)
				{
					Vector3 oldForward = transform.forward;
					transform.up = hitInfo.normal;
					oldForward -= transform.up*Vector3.Dot(transform.up, oldForward);
					oldForward.Normalize();
					if (Vector3.Dot(Vector3.Cross(transform.forward, oldForward), hitInfo.normal) > 0)
					{
						float angle = -Vector3.Angle(transform.forward, oldForward);
						if (!float.IsNaN(angle))
						{
							transform.Rotate(Vector3.down, angle);
						}
					}
					else
					{
						float angle = -Vector3.Angle(transform.forward, oldForward);
						if (!float.IsNaN(angle))
						{
							transform.Rotate(Vector3.up, angle);
						}
					}
				}
				return true;
			}
			return false;
		}
		
		private bool Raycast(out RaycastHit o_hitInfo)
		{
			// get all hits to find the closest object Physics.Raycast() does not guarantee that the hit is the closest one
			RaycastHit[] hits = Physics.RaycastAll(new Ray(transform.position+RaycastUp, RaycastDown), MAX_DISTANCE_TO_SNOW+1f);
			RaycastHit? bestHit = null;
			float minDist = Mathf.Infinity;
			for (int i=0; i<hits.Length; i++)
			{	
				if (minDist > hits[i].distance)
				{
					minDist = hits[i].distance;
					bestHit = hits[i];
				}
			}
			// the closest object must be in the snow layers
			if (bestHit.HasValue && (SNOW_LAYERS.value & 1<<bestHit.Value.collider.gameObject.layer) != 0)
			{
				o_hitInfo = bestHit.Value;
				return true;
			}
			else
			{
				o_hitInfo = new RaycastHit();
				return false;
			}
		}
	}
}
