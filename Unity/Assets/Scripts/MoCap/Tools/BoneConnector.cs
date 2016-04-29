using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif 

namespace MoCap
{
	/// <summary>
	/// Class for a single bone name connection entry.
	/// </summary>
	///
	[System.Serializable]
	public class BoneConnectionEntry
	{
		public string name1; // name of the bone in the first actor
		public string name2; // name of the bone in the second actor
	}


	/// <summary>
	/// Class for connecting bones of one or more actors with lines.
	/// </summary>
	/// 

	[AddComponentMenu("Motion Capture/Tools/Bone Connector")]

	public class BoneConnector : MonoBehaviour
	{
		[Tooltip("GameObject to start the connections at")]
		public GameObject startObject;

		[Tooltip("GameObject to end the connections at (optional)")]
		public GameObject endObject;

		[Tooltip("Prefab to use for rendering the lines")]
		public GameObject linePrefab;

		[Tooltip("Amount of time that the lines stay drawn [seconds]")]
		[Range(0, 10)]
		public float duration;

		[Tooltip("List of bone names to connect from/to")]
		public BoneConnectionEntry[] boneConnections;


		public void Start()
		{
			int count = boneConnections.Length;

			if (startObject == null)
			{
				Debug.LogError("No start GameObject defined for BoneConnector script.");
				count = 0;
			}
			if (endObject == null)
			{
				// lines stay within the bones of one object
				endObject = startObject;
			}

			// prepare renderers and data structures
			lines = new LineData[count];
			for (int i = 0; i < count; i++)
			{
				GameObject line = GameObject.Instantiate(linePrefab);
				line.transform.parent = this.transform;
				line.name = "Line " + i;
				lines[i] = new LineData(line.GetComponent<LineRenderer>());
			}
			linePrefab.SetActive(false);

			copyContainer = new GameObject();
			copyContainer.name = "Copies";
			copyContainer.transform.parent = this.transform;
		}


		void Update()
		{
			for (int i = 0; i < lines.Length; i++)
			{
				LineData line = lines[i];
				if (line.start != null)
				{
					// only render line when both other actors are active
					bool enabled = line.start.gameObject.activeInHierarchy &&
								   line.end.gameObject.activeInHierarchy;
					line.renderer.gameObject.SetActive(enabled);
					// update endpoint positions
					if (enabled)
					{
						line.renderer.SetPosition(0, line.start.position);
						line.renderer.SetPosition(1, line.end.position);
					}

					if (duration > 0)
					{
						GameObject copy = GameObject.Instantiate(line.renderer.gameObject);
						copy.transform.parent = copyContainer.transform;
						GameObject.Destroy(copy, duration);
					}
				}
				else
				{
					// haven't found the corresponding bone transforms yet
					// TODO: Add max counter to avoid searching for invalid names the whole time
					line.start = Utilities.FindInHierarchy(boneConnections[i].name1, startObject.transform);
					if (line.start != null)
					{
						// only search end when there is a start
						line.end = Utilities.FindInHierarchy(boneConnections[i].name2, endObject.transform);
					}
					if (line.end == null)
					{
						// if there is no end, then thereis no start
						line.start = null;
					}
				}
			}
		}


		/// <summary>
		/// Structure for managing the start and end points and the associated renderer.
		/// </summary>
		/// 
		private class LineData
		{
			public readonly LineRenderer renderer;
			public Transform start, end;

			public LineData(LineRenderer r)
			{
				this.renderer = r;
				start = null;
				end = null;
			}
		}


		private LineData[] lines;
		private GameObject copyContainer;
	}



#if UNITY_EDITOR

	/// <summary>
	/// Class for rendering a bone connection start/end entry in the editor
	/// </summary>
	///

	[CustomPropertyDrawer(typeof(BoneConnectionEntry))]

	public class BoneConnectionEntryDrawer : PropertyDrawer
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
			float space = (position.width - labelWidth1) / 2;
			float x = position.x;
			float w = space;
			Rect name1Rect = new Rect(x, position.y, w, position.height);
			x += w; w = labelWidth1;
			Rect arrowRect = new Rect(x, position.y, w, position.height);
			x += w; w = space;
			Rect name2Rect = new Rect(x, position.y, w, position.height);

			// Draw fields - passs GUIContent.none to each so they are drawn without labels
			EditorGUI.PropertyField(name1Rect, property.FindPropertyRelative("name1"), GUIContent.none);
			EditorGUI.LabelField(arrowRect, " →");
			EditorGUI.PropertyField(name2Rect, property.FindPropertyRelative("name2"), GUIContent.none);

			// restore indent level
			EditorGUI.indentLevel = oldIndent;

			EditorGUI.EndProperty();
		}
	}

#endif

}
