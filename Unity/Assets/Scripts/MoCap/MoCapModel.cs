﻿using MoCap;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


[System.Serializable]
public class BoneNameTranslationEntry
{
	public string nameMocap; // name of the bone in the MoCap skeleton
	public string nameModel; // name of the bone in the Model
	public string axisTransformation;  // axis transformation to be applied
}


/// <summary>
/// Class for controlling a rigged model via MoCap data.
/// </summary>
///
[DisallowMultipleComponent]
[AddComponentMenu("Motion Capture/MoCap Model")]
public class MoCapModel : MonoBehaviour, ActorListener
{
	[Tooltip("The name of the MoCap actor to link to this model.")]
	public string actorName;

	[Tooltip("Prefix to add to the bone names before searching in the hierarchy.")]
	public string boneNamePrefix = "";

	[Tooltip("What components of the MoCap data stream to use.")]
	public TrackingUsage trackingUsage = TrackingUsage.RotationOnly;

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
			BoneNameTranslationEntry entry = null;
			string boneName = boneNamePrefix + TranslateBoneName(bone.name, ref entry);
			// find the child in the model with the given bone name
			Transform boneNode = FindInHierarchy(boneName, transform);
			if (boneNode != null)
			{
				dataBuffers[bone] = new MoCapDataBuffer(this.gameObject, boneNode.gameObject, entry);
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
	private string TranslateBoneName(string name, ref BoneNameTranslationEntry actualEntry)
	{
		string translatedName = "undefined";
		foreach (BoneNameTranslationEntry entry in boneNameTranslationTable)
		{
			if (name == entry.nameMocap)
			{
				if (entry.nameModel.Length > 0)
				{
					translatedName = entry.nameModel;
				}
				else
				{
					translatedName = name;
				}
				actualEntry = entry;
				break;
			}
		}
		return translatedName;
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
	/// Called once per frame. Updates the model based on the bone rotations and positions.
	/// </summary>
	/// 
	void Update() 
	{
		if ( (client == null) || (dataBuffers == null) )
			return;

		// update bones
		Quaternion rot = new Quaternion();
		foreach ( KeyValuePair<Bone, MoCapDataBuffer> entry in dataBuffers )
		{
			Bone            bone   = entry.Key;
			MoCapDataBuffer buffer = entry.Value; 
			GameObject      obj    = buffer.GetGameObject();

			// pump bone data through buffer
			MoCapData data = buffer.Process(bone);

			// update bone game object
			if (data.tracked)
			{
				if ( (trackingUsage == TrackingUsage.PositionAndRotation) ||
					 (bone.parent == null) )
				{
					// change position only when desired, or when a root bone
					obj.transform.localRotation = Quaternion.identity;
					obj.transform.localPosition = data.pos;
				}

				rot = Quaternion.identity;

				string transforms = ((BoneNameTranslationEntry) buffer.GetDataObject()).axisTransformation;
				foreach ( char c in transforms )
				{
					switch ( c )
					{
						case 'X': rot *= Quaternion.Euler( 90, 0, 0); break;
						case 'x': rot *= Quaternion.Euler(-90, 0, 0); break;
						case 'Y': rot *= Quaternion.Euler(0,  90, 0); break;
						case 'y': rot *= Quaternion.Euler(0, -90, 0); break;
						case 'Z': rot *= Quaternion.Euler(0, 0,  90); break;
						case 'z': rot *= Quaternion.Euler(0, 0, -90); break;
					}
				}

				obj.transform.localRotation = data.rot * rot;
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


	private MoCapClient                       client;
	private Dictionary<Bone, MoCapDataBuffer> dataBuffers;
}



#if UNITY_EDITOR

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
		float labelWidth1 = 20;
		float labelWidth2 = 5;
		float space = (position.width - labelWidth1 - labelWidth2) / 5;
		float x = position.x;
		float w = space * 2;
		Rect mocapRect = new Rect(x, position.y, w, position.height);
		x += w; w = labelWidth1;
		Rect arrowRect = new Rect(x, position.y, w, position.height);
		x += w; w = space * 2;
		Rect modelRect = new Rect(x, position.y, w, position.height);
		x += w + labelWidth2; w = space; 
		Rect axisRect  = new Rect(x, position.y, w, position.height);

		// Draw fields - passs GUIContent.none to each so they are drawn without labels
		EditorGUI.PropertyField(mocapRect, property.FindPropertyRelative("nameMocap"), GUIContent.none);
		EditorGUI.LabelField(arrowRect, " →");
		EditorGUI.PropertyField(modelRect, property.FindPropertyRelative("nameModel"), GUIContent.none);
		EditorGUI.PropertyField(axisRect, property.FindPropertyRelative("axisTransformation"), GUIContent.none);

		// restore indent level
		EditorGUI.indentLevel = oldIndent;

		EditorGUI.EndProperty();
	}
}

#endif

