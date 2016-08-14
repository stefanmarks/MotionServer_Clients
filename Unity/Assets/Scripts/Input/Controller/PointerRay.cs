using UnityEngine;

/// <summary>
/// Component for controlling a laser-like ray to point at objects in the scene.
/// This component can be queried as to what it is pointing at.
/// </summary>
///

[RequireComponent(typeof(LineRenderer))]

public class PointerRay : MonoBehaviour
{
	[Tooltip("Maximum range of the ray")]
	public float rayRange = 100.0f;

	[Tooltip("List of tags that the pointer reacts to (e.g., 'floor')")]
	public string[] tagList = { };

	[Tooltip("Object to render at the point where the ray meets another game object (optional)")]
	public Transform activeEndPoint = null;


	void Start()
	{
		line = GetComponent<LineRenderer>();
		line.SetVertexCount(2);
		line.useWorldSpace = true;
		rayEnabled = line.enabled;
		overrideTarget = false;
	}


	void LateUpdate()
	{
		// assume nothing is hit at first
		rayTarget.distance = 0;

		// change in enabled flag
		if (line.enabled != rayEnabled)
		{
			line.enabled = rayEnabled;
			if ((activeEndPoint != null) && !rayEnabled)
			{
				activeEndPoint.gameObject.SetActive(false);
			}
		}

		if (!line.enabled) return; // if ray is disabled, bail out right now

		bool hit = false;
		// construct ray
		Ray ray = new Ray(transform.position, transform.forward);
		Vector3 end = ray.origin + ray.direction * rayRange;
		line.SetPosition(0, ray.origin);
		Debug.DrawLine(ray.origin, end, Color.red);

		if (!overrideTarget)
		{
			// do raycast
			hit = Physics.Raycast(ray, out rayTarget, rayRange);

			// test tags
			if (hit && (tagList.Length > 0))
			{
				hit = false;
				foreach (string tag in tagList)
				{
					if (rayTarget.transform.tag.CompareTo(tag) == 0)
					{
						hit = true;
						break;
					}
				}
				if (!hit)
				{
					// tag test negative > reset raycast structure
					Physics.Raycast(ray, out rayTarget, 0);
				}
			}
		}
		else
		{
			hit = true;
		}

		if (hit)
		{
			// hit something > draw ray to there and render end point object
			line.SetPosition(1, rayTarget.point);
			if (activeEndPoint != null)
			{
				activeEndPoint.position = rayTarget.point;
				activeEndPoint.gameObject.SetActive(true);
			}
		}
		else
		{
			// hit nothing > draw ray to end and disable end point object
			line.SetPosition(1, end);
			if (activeEndPoint != null)
			{
				activeEndPoint.gameObject.SetActive(false);
			}
		}
	}


	/// <summary>
	/// Returns the current target of the ray.
	/// </summary>
	/// <returns>the last raycastHit result</returns>
	/// 
	public RaycastHit GetRayTarget()
	{
		return rayTarget;
	}


	/// <summary>
	/// Sets the current target of the ray.
	/// </summary>
	/// 
	public void OverrideRayTarget(Vector3 pos)
	{
		if (pos.Equals(Vector3.zero))
		{
			overrideTarget = false;
		}
		else
		{
			overrideTarget = true;
			rayTarget.point = pos;
		}
	}


	/// <summary>
	/// Enables or disables the ray.
	/// </summary>
	/// <param name="enabled"><code>true</code> to enable the ray functionality</param>
	/// 
	public void SetEnabled(bool enabled)
	{
		if (line == null) Start();

		rayEnabled = enabled;
	}


	private LineRenderer line;
	private bool         rayEnabled;
	private RaycastHit   rayTarget;
	private bool         overrideTarget;
}