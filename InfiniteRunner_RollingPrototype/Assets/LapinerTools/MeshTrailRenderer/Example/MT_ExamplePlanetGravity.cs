using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MT_MeshTrail;

public class MT_ExamplePlanetGravity : MonoBehaviour
{
	public ConstantForce Target;

	private MT_SnowMeshTrailUpdater[] CustomRaycastAxisTrails = null;

	private void Start()
	{
		Rigidbody body = Target.GetComponentInChildren<Rigidbody>();
		if (body != null)
		{
			body.useGravity = false;
		}

		List<MT_SnowMeshTrailUpdater> trails = new List<MT_SnowMeshTrailUpdater>(Target.GetComponentsInChildren<MT_SnowMeshTrailUpdater>(true));
		for (int i = trails.Count-1; i >= 0; i--)
		{
			if (trails[i].RaycastMode != MT_SnowMeshTrailUpdater.ERaycastMode.CUSTOM_AXIS)
			{
				trails.RemoveAt(i);
			}
		}
		CustomRaycastAxisTrails = trails.ToArray();
	}
	
	private void Update()
	{
		if (Target != null)
		{
			Target.force = (transform.position - Target.transform.position).normalized * Target.force.magnitude;
		}

		if (CustomRaycastAxisTrails != null)
		{
			for (int i = 0; i < CustomRaycastAxisTrails.Length; i++)
			{
				MT_SnowMeshTrailUpdater trail = CustomRaycastAxisTrails[i];
				if (trail != null)
				{
					trail.RaycastCustomAxis = Target.force.normalized;
				}
			}
		}
	}
}
