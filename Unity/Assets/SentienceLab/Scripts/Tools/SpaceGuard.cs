using UnityEngine;

/// <summary>
/// Script for fading in geometry that signals the limits of the tracking volume.
/// </summary>
/// 
public class SpaceGuard : MonoBehaviour
{
	[Tooltip("The object that needs to be guarded to stay within the walls.")]
	public Transform guardedObject;

	[Tooltip("The minimum distance that the falls should start to fade in.")]
	public float fadeInDistance;

	[Tooltip("The name of the material parameter to modify based on the distance.")]
	public string colourParameterName = "_TintColor";

	[Tooltip("Colour gradient for fading in the walls")]
	public Gradient colourGradient;


	/// <summary>
	/// Called at start of the script.
	/// Gathers all children that have renderers as walls.
	/// </summary>
	/// 
	void Start()
	{
		// all components with a renderer are considered walls
		walls = transform.GetComponentsInChildren<MeshRenderer>();
	}
	

	/// <summary>
	/// Called one per frame.
	/// Updates the visibility of the walls based on the distance of the guarded object from them.
	/// </summary>
	/// 
	void Update()
	{
		foreach (MeshRenderer wall in walls)
		{
			// calculate distance of guarded object against all the walls
			Vector3 pos    = wall.transform.position;
			Vector3 normal = wall.transform.forward;
			float dist = -Vector3.Dot(normal, guardedObject.position - pos);
			Color wallColour = wall.material.GetColor(colourParameterName);

			// can't be further away from the wall than "in" it
			if (dist < 0) { dist = 0; }

			// too far away and wall not visible: don't worry changing the material
			if ( (dist > fadeInDistance * 1.1) && (wallColour.a == 0) )
				continue; 

			// calculate colour to apply to the wall
			float fade = 1 - (dist / fadeInDistance);
			wallColour = colourGradient.Evaluate(fade);
			wall.material.SetColor(colourParameterName, wallColour);
		}
	}

	private MeshRenderer[] walls;
}
