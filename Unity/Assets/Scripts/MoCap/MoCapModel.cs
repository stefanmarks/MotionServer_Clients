using MoCap;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class BoneNameTranslationEntry
{
	public string nameMocap; // name of the bone in the MoCap skeleton
	public string nameModel; // name of the bone in the Model
}


/// <summary>
/// Class for controlling a rigged model via MoCap data.
/// </summary>
///
[DisallowMultipleComponent]
[AddComponentMenu("Motion Capture/MoCap Model")]
public class MoCapModel : MonoBehaviour, ActorListener, IDelay
{
	[Tooltip("The name of the MoCap actor to link to this model.")]
	public string actorName;

	[Tooltip("What components of the MoCap data stream to use.")]
	public TrackingUsage trackingUsage = TrackingUsage.RotationOnly;

	[Tooltip("Delay of the rendering in seconds.")]
	public float delay = 0;

	[Tooltip("Table for translating MoCap bone names to Model bone names.")]
	public BoneNameTranslationEntry[] boneNameTranslationTable = new BoneNameTranslationEntry[0];


	/// <summary>
	/// What data to use from the MoCap system.
	/// </summary>
	/// 
	public enum TrackingUsage
	{
		/// <summary>
		/// Use position and rotation.
		/// </summary>
		PositionAndRotation,

		/// <summary>
		/// Use only rotation
		/// </summary>
		RotationOnly
	};


	/// <summary>
	/// Called at the start of the game.
	/// Tries to find the MoCap client singleton and then 
	/// registers this object as a listener with the client.
	/// </summary>
	/// 
	void Start() 
	{
		dataBuffers = null;

		// try to find the client singleton
		client = FindObjectOfType<MoCapClient>();
		if (client != null)
		{
			client.AddActorListener(this);
		}
		else
		{
			Debug.LogWarning("No MoCapClient Component defined in the scene.");
		}
	}


	/// <summary>
	/// Finds the rigging in the model and matches it to the MoCap bone system.
	/// </summary>
	/// <param name="bones">bone data from the MoCap system</param>
	/// 
	private void MatchBones(Bone[] bones)
	{
		dataBuffers = new Dictionary<Bone, MoCapDataBuffer>();
		string unmatchedBones = "";

		// create copies of the marker template
		foreach (Bone bone in bones)
		{
			string boneName = TranslateBoneName(bone.name);

			// find the child in the model with the given bone name
			Transform boneNode = FindInHierarchy(boneName, transform);
			if (boneNode != null)
			{
				dataBuffers[bone] = new MoCapDataBuffer(boneNode.gameObject, delay);
			}
			else
			{
				if ( unmatchedBones.Length > 0 )
				{
					unmatchedBones += ", ";
				}
				unmatchedBones += bone.name;
			}
		}

		if ( unmatchedBones.Length > 0 )
		{
			Debug.LogWarning("Could not find the following bones in Model '" + this.name + "': " + unmatchedBones);
		}
	}


	/// <summary>
	/// Translates a bone name form the MoCap name into the Model name
	/// if defined in the table.
	/// </summary>
	/// <param name="name">the name in the MoCap scene</param>
	/// <returns>the translated name for the model</returns>
	/// 
	private string TranslateBoneName(string name)
	{
		foreach (BoneNameTranslationEntry entry in boneNameTranslationTable)
		{
			if (name == entry.nameMocap)
			{
				name = entry.nameModel;
				break;
			}
		}
		return name;
	}

	/// <summary>
	/// Finds a transform by name within a hierarchy starting at a specific base transform.
	/// </summary>
	/// <param name="name">the name of the Transform to find</param>
	/// <param name="baseTransform">the Transform instance to start the search at</param>
	/// <returns>the Transform with the given name, or <code>null</code> if if doesn't exist</returns>
	/// 
	private Transform FindInHierarchy(string name, Transform baseTransform)
	{
		Transform result = null;

		if (baseTransform.name == name)
		{
			// it's the baseObject itself
			result = baseTransform;
		}
		else
		{
			// let's look in all the children recursively
			foreach (Transform child in baseTransform)
			{
				result = FindInHierarchy(name, child);
				if (result != null) break; // found it > get me out here
			}
		}

		return result;
	}


	//// <summary>
	/// Called once per frame.
	/// </summary>
	/// 
	void Update() 
	{
		if ( (client == null) || (dataBuffers == null) )
			return;

		// update bones
		foreach ( KeyValuePair<Bone, MoCapDataBuffer> entry in dataBuffers )
		{
			Bone            bone   = entry.Key;
			MoCapDataBuffer buffer = entry.Value; 
			GameObject      obj    = buffer.GetGameObject();

			// pump bone data through buffer
			MoCapDataBuffer.MoCapData data = buffer.Process(bone);

			// update bone game object
			if (data.tracked)
			{
				if ( (trackingUsage == TrackingUsage.PositionAndRotation) ||
					 (bone.parent == null) )
				{
					// change position only when desired, or when a root bone
					obj.transform.localPosition = data.pos;
				}
				obj.transform.localRotation = data.rot;
			}
		}
	}


	/// <summary>
	/// Gets the name of the actor.
	/// </summary>
	/// <returns>The name of the actor</returns>
	/// 
	public string GetActorName()
	{
		return actorName;
	}


	public void ActorUpdated(Actor actor)
	{
		// create marker position array if necessary
		// but only when tracking is OK, otherwise the bone lengths are undefined
		if ( dataBuffers == null )
		{
			MatchBones(actor.bones);
		}
	}


	public void ActorChanged(Actor actor)
	{
		// re-match bone names with the next update
		dataBuffers = null;

		if (actor != null)
		{ 
			Debug.Log("MoCapModel '" + this.name + "' controlled by MoCap actor '" + actorName + "'.");
		}
		else
		{
			Debug.LogWarning("MoCapModel '" + this.name + "' cannot find MoCap actor '" + actorName + "'.");
		}
	}


	public float GetDelay()
	{
		return delay;
	}


	public void SetDelay(float value)
	{
		delay = Mathf.Max(0, value);
	}


	private MoCapClient                       client;
	private Dictionary<Bone, MoCapDataBuffer> dataBuffers;
}



/// <summary>
/// Class for rendering a bone translation entry in the editor
/// </summary>
/// 
[CustomPropertyDrawer(typeof(BoneNameTranslationEntry))]
public class BoneNameTranslationEntryDrawer : PropertyDrawer
{
	// Draw the property inside the given rect
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

		// No indentation
		int oldIndent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		// Calculate field positions
		float labelWidth = 20;
		float w = (position.width - labelWidth) / 2;
		Rect mocapRect = new Rect(position.x + 0, position.y, w, position.height);
		Rect arrowRect = new Rect(position.x + w, position.y, labelWidth, position.height);
		Rect modelRect = new Rect(position.x + w + labelWidth, position.y, w, position.height);

		// Draw fields - passs GUIContent.none to each so they are drawn without labels
		EditorGUI.PropertyField(mocapRect, property.FindPropertyRelative("nameMocap"), GUIContent.none);
		EditorGUI.LabelField(arrowRect, " →");
		EditorGUI.PropertyField(modelRect, property.FindPropertyRelative("nameModel"), GUIContent.none);

		// restore indent level
		EditorGUI.indentLevel = oldIndent;

		EditorGUI.EndProperty();
	}
}


