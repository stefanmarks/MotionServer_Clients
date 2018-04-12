#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Component for writing Motion Capture data to a file that can later be read back or analysed.
	/// </summary>

	[DisallowMultipleComponent]
	[AddComponentMenu("Motion Capture/MoCap Writer")]

	public class MoCapWriter : MonoBehaviour
	{
		[Tooltip("Filename to use for the recorded data ({timestamp} will be replaced by an actual timestamp)")]
		public string OutputFilename = "MoCapFile_{timestamp}.mot";

		[Tooltip("List of transforms to log to the file")]
		public List<Transform> Transforms;


		private const char   SEPARATOR           = '\t';

		private const string MOT_FILE_IDENTIFIER = "MotionServer Data File";
		private const int    MOT_FILE_VERSION    = 2;

		private const string MOT_SECTION_DESCRIPTIONS = "Descriptions";
		private const string MOT_SECTION_FRAMES       = "Frames";

		private const string TAG_MARKERSET   = "M";
		private const string TAG_RIGIDBODY   = "R";
		private const string TAG_SKELETON    = "S";
		private const string TAG_FORCEPLATE  = "F";


		public void Start()
		{
			writer = null;

			writers = new List<IMoCapWriter>();
			CollectMoCapWriters();
		}


		private void CollectMoCapWriters()
		{
			writers.Clear();
			int index = 1;

			Scene s = MoCapManager.GetInstance().GetScene();
			foreach(Actor a in s.actors)
			{
				writers.Add(new MoCapActorWriter(a));
				index++;
			}
			foreach (Device d in s.devices)
			{
				writers.Add(new MoCapDeviceWriter(d));
				index++;
			}

			foreach (Transform t in Transforms)
			{
				writers.Add(new TransformWriter(t, index));
				index++;
			}

			// collect overall stats
			numberOfActors  = 0;
			numberOfDevices = 0;
			foreach(IMoCapWriter writer in writers)
			{
				numberOfActors  += writer.GetNumberOfActors();
				numberOfDevices += writer.GetNumberOfDevices();
			}
		}


		public void Update()
		{
			if (writer == null) 
			{
				if (Time.frameCount > 1) // give it at least one iteration for MoCap objects to be finalised 
				{
					OpenLogfile();
				}
			}
			else
			{
				WriteLogfile();
			}
		}


		public void OnApplicationQuit()
		{
			if (writer != null)
			{
				writer.Close();
				writer = null;
			}
		}


		private void OpenLogfile()
		{
			// save pure data
			string filename = OutputFilename.Replace("{timestamp}", System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
			writer = new MoCapFileWriter(new StreamWriter(filename));
			Debug.Log("Opened MoCap logfile '" + filename + "'");

			foreach(IMoCapWriter w in writers) { w.SetWriter(writer); }

			// write MOT file header
			writer
				.WriteTag(MOT_FILE_IDENTIFIER) // header
				.Write(MOT_FILE_VERSION) // version
				.Write(MoCapManager.GetInstance().GetFramerate()) // framerate
				.EndLine();

			// write description section
			writer
				.WriteTag(MOT_SECTION_DESCRIPTIONS) // keyword
				.Write((numberOfActors * 2) + numberOfDevices) // number of descriptions (actors *2: markerset + rigidbbody)
				.EndLine();
			int index = 0;
			foreach (IMoCapWriter w in writers) { w.WriteMarkersetDescription(ref index); }
			foreach (IMoCapWriter w in writers) { w.WriteRigidBodyDescription(ref index); }
			foreach (IMoCapWriter w in writers) { w.WriteDeviceDescription(ref index); }

			// start data section
			writer.WriteTag(MOT_SECTION_FRAMES).EndLine();
			
			// write data headers
			writer.WriteTag("#frame").WriteTag("timestamp").WriteTag("latency");
			writer.WriteTag("markersetTag").WriteTag("markersetCount");
			foreach (IMoCapWriter w in writers) { w.WriteMarkersetHeader(); }
			writer.WriteTag("rigidbodyTag").WriteTag("rigidbodyCount");
			foreach (IMoCapWriter w in writers) { w.WriteRigidBodyHeader(); }
			writer.WriteTag("skeletonTag").WriteTag("skeletonCount");
			writer.WriteTag("forceplateTag").WriteTag("forceplateCount");
			foreach (IMoCapWriter w in writers) { w.WriteDeviceHeader(); }
			writer.EndLine();
		}


		private void WriteLogfile()
		{
			writer
				.Write(Time.frameCount) // frame#
				.Write(Time.time) // timestamp
				.Write(0); // latency
			writer.WriteTag(TAG_MARKERSET).Write(numberOfActors);
			foreach (IMoCapWriter w in writers) { w.WriteMarkersetData(); }
			writer.WriteTag(TAG_RIGIDBODY).Write(numberOfActors);
			foreach (IMoCapWriter w in writers) { w.WriteRigidBodyData(); }
			writer.WriteTag(TAG_SKELETON).Write(0);
			writer.WriteTag(TAG_FORCEPLATE).Write(numberOfDevices);
			foreach (IMoCapWriter w in writers) { w.WriteDeviceData(); }
			writer.EndLine();
		}


		private interface IMoCapWriter
		{
			void SetWriter(MoCapFileWriter _writer);

			int  GetNumberOfActors();
			void WriteMarkersetDescription(ref int _descriptionIndex);
			void WriteMarkersetHeader();
			void WriteMarkersetData();
			void WriteRigidBodyDescription(ref int _descriptionIndex);
			void WriteRigidBodyHeader();
			void WriteRigidBodyData();

			int  GetNumberOfDevices();
			void WriteDeviceDescription(ref int _descriptionIndex);
			void WriteDeviceHeader();
			void WriteDeviceData();
		}
		

		private class TransformWriter : IMoCapWriter
		{
			public TransformWriter(Transform _t, int _index)
			{
				t     = _t;
				index = _index;
			}

			public void SetWriter(MoCapFileWriter _writer)
			{
				writer = _writer;
			}

			public int GetNumberOfActors()
			{
				return 1;
			}

			public void WriteMarkersetDescription(ref int _descriptionIndex)
			{
				writer
					.Write(_descriptionIndex)
					.WriteTag(TAG_MARKERSET)
					.Write(t.gameObject.name) // name
					.Write(1)                 // number of markers
					.Write("m1")              // marker names
					.EndLine();
				;
				_descriptionIndex++;
			}

			public void WriteMarkersetHeader()
			{
				writer
					.WriteHeader(t.gameObject.name, "markerCount")
					.WriteHeader(t.gameObject.name, "m1", new string[] { "x", "y", "z"});
			}

			public void WriteMarkersetData()
			{
				writer
					.Write(1) // number of markers
					.Write(t.localPosition) // position
				;
			}

			public void WriteRigidBodyDescription(ref int _descriptionIndex)
			{
				writer
					.Write(_descriptionIndex)
					.WriteTag(TAG_RIGIDBODY)
					.Write(index)             // ID
					.Write(t.gameObject.name) // name
					.Write(-1)                // parent ID
					.Write(Vector3.zero)      // bone offset
					.EndLine();
				;
				_descriptionIndex++;
			}

			public void WriteRigidBodyHeader()
			{
				writer
					.WriteHeader(t.gameObject.name, "", 
						new string[] { "id", "x", "y", "z", "qx", "qy", "qz", "qw", "meanError", "params" });
			}

			public void WriteRigidBodyData()
			{
				writer
					.Write(index) // ID
					.Write(t.localPosition) // position
					.Write(t.localRotation) // rotation
					.Write(0) // mean error/bone length
					.Write(t.gameObject.activeInHierarchy ? 0x01 : 0x00) // flags (tracking)
				;
			}

			public int GetNumberOfDevices()
			{
				return 0;
			}

			public void WriteDeviceDescription(ref int _descriptionIndex)
			{
				// nothing to do
			}

			public void WriteDeviceHeader()
			{
				// nothing to do
			}

			public void WriteDeviceData()
			{
				// nothing to do
			}

			private Transform       t;
			private int             index;
			private MoCapFileWriter writer;
		}


		private class MoCapActorWriter : IMoCapWriter
		{
			public MoCapActorWriter(Actor _actor)
			{
				actor = _actor;
			}

			public void SetWriter(MoCapFileWriter _writer)
			{
				writer = _writer;
			}

			public int GetNumberOfActors()
			{
				return 1;
			}

			public void WriteMarkersetDescription(ref int _descriptionIndex)
			{
				writer
					.Write(_descriptionIndex)
					.WriteTag(TAG_MARKERSET)
					.Write(actor.name)           // name
					.Write(actor.markers.Length) // number of markers
				;
				foreach(Marker m in actor.markers)
				{
					writer.Write(m.name);
				}
				writer.EndLine();
				_descriptionIndex++;
			}

			public void WriteMarkersetHeader()
			{
				writer.WriteHeader(actor.name, "markerCount");
				foreach (Marker m in actor.markers)
				{
					writer
						.WriteHeader(actor.name, m.name, new string[] { "x", "y", "z" });
				}
			}

			public void WriteMarkersetData()
			{
				writer.Write(actor.markers.Length); // number of markers
				Vector3 pos = Vector3.zero;
				foreach (Marker m in actor.markers)
				{
					m.CopyTo(ref pos);
					writer.Write(pos);
				}
			}

			public void WriteRigidBodyDescription(ref int _descriptionIndex)
			{
				//TODO: this will fail for skeletons
				writer
					.Write(_descriptionIndex)
					.WriteTag(TAG_RIGIDBODY)
					.Write(actor.id)      // ID
					.Write(actor.name)    // name
					.Write(-1)            // parent ID
					.Write(Vector3.zero)  // bone offset
					.EndLine();
				;
				_descriptionIndex++;
			}

			public void WriteRigidBodyHeader()
			{
				writer
					.WriteHeader(actor.name, "",
						new string[] { "id", "x", "y", "z", "qx", "qy", "qz", "qw", "meanError", "params" });
			}

			public void WriteRigidBodyData()
			{
				Vector3    pos = Vector3.zero;
				Quaternion rot = Quaternion.identity;
				Bone bone = actor.bones[0];
				bone.CopyTo(ref pos);
				bone.CopyTo(ref rot);
				writer
					.Write(actor.id) // ID
					.Write(pos)      // position
					.Write(rot)      // rotation
					.Write(0)        // mean error/bone length
					.Write(bone.tracked ? 0x01 : 0x00) // flags (tracking)
				;
			}

			public int GetNumberOfDevices()
			{
				return 0;
			}

			public void WriteDeviceDescription(ref int _descriptionIndex)
			{
				// nothing to do
			}

			public void WriteDeviceHeader()
			{
				// nothing to do
			}

			public void WriteDeviceData()
			{
				// nothing to do
			}

			private Actor           actor;
			private MoCapFileWriter writer;
		}


		private class MoCapDeviceWriter : IMoCapWriter
		{
			public MoCapDeviceWriter(Device _device)
			{
				device = _device;
			}

			public void SetWriter(MoCapFileWriter _writer)
			{
				writer = _writer;
			}

			public int GetNumberOfActors()
			{
				return 0;
			}

			public void WriteMarkersetDescription(ref int _descriptionIndex)
			{
				// nothing to do
			}

			public void WriteMarkersetHeader()
			{
				// nothing to do
			}

			public void WriteMarkersetData()
			{
				// nothing to do
			}

			public void WriteRigidBodyDescription(ref int _descriptionIndex)
			{
				// nothing
			}

			public void WriteRigidBodyHeader()
			{
				// nothing to do
			}

			public void WriteRigidBodyData()
			{
				// nothing to do
			}

			public int GetNumberOfDevices()
			{
				return 1;
			}

			public void WriteDeviceDescription(ref int _descriptionIndex)
			{
				writer
					.Write(_descriptionIndex)
					.WriteTag(TAG_FORCEPLATE)
					.Write(device.id)              // ID
					.Write(device.name)            // name
					.Write(device.channels.Length) // number of markers
				;
				foreach (Channel c in device.channels)
				{
					writer.Write(c.name);
				}
				writer.EndLine();
				_descriptionIndex++;
			}

			public void WriteDeviceHeader()
			{
				writer.WriteHeader(device.name, "", new string[] { "id", "channelCount" });
				foreach(Channel c in device.channels)
				{
					writer.WriteHeader(device.name, c.name);
				}
			}

			public void WriteDeviceData()
			{
				writer
					.Write(device.id)
					.Write(device.channels.Length)
				;
				foreach (Channel c in device.channels)
				{
					writer.Write(c.value);
				}
			}

			private Device          device;
			private MoCapFileWriter writer;
		}


		private class MoCapFileWriter
		{
			public MoCapFileWriter(StreamWriter _sw)
			{
				writer = _sw;
				lineStarted = true;
			}

			public MoCapFileWriter WriteDelimiter()
			{
				if (!lineStarted)
				{
					writer.Write(SEPARATOR);
				}
				lineStarted = false;
				return this;
			}

			public void EndLine()
			{
				writer.WriteLine();
				lineStarted = true;
			}

			public MoCapFileWriter Write(long value)
			{
				WriteDelimiter();
				writer.Write(value);
				return this;
			}

			public MoCapFileWriter Write(float value)
			{
				WriteDelimiter();
				writer.Write(value);
				return this;
			}

			public MoCapFileWriter Write(string value)
			{
				WriteDelimiter();
				writer.Write('"' + value + '"');
				return this;
			}

			public MoCapFileWriter Write(Vector3 value)
			{
				// flip Unity (left handed) to MOT (right handed)
				Write( value.x);
				Write( value.y);
				Write(-value.z);
				return this;
			}

			public MoCapFileWriter Write(Quaternion value)
			{
				// flip Unity (left handed) to MOT (right handed)
				Write(-value.x); 
				Write(-value.y);
				Write( value.z);
				Write( value.w);
				return this;
			}

			public MoCapFileWriter WriteTag(string value)
			{
				WriteDelimiter();
				writer.Write(value);
				return this;
			}

			public MoCapFileWriter WriteHeader(string _prefix, string _name = "", string[] _postfix = null)
			{
				string str = _prefix;
				if (_name.Length > 0)
				{
					str += "." + _name;
				}

				if (_postfix == null)
				{
					WriteTag(str);
				}
				else
				{
					foreach(string s in _postfix)
					{
						WriteTag(str + "." + s);
					}
				}
				return this;
			}

			public void Close()
			{
				writer.Close();
			}

			private StreamWriter writer;
			bool    lineStarted;
		}


		private MoCapFileWriter    writer;
		private List<IMoCapWriter> writers;
		private int                numberOfActors, numberOfDevices;
	}

}