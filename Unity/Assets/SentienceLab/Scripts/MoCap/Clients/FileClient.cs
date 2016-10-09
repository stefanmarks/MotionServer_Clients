using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using UnityEngine;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Class for a MoCap client that reads from a .MOT file.
	/// </summary>
	/// 
	public class FileClient : IMoCapClient
	{
		public class ConnectionInfo : IMoCapClient_ConnectionInfo
		{
			public ConnectionInfo(TextAsset asset)
			{
				dataStream = new DataStream_TextAsset(asset);
			}

			public ConnectionInfo(string filename)
			{
				// cut any leading character
				filename = filename.TrimStart('/');
				dataStream = new DataStream_File(System.IO.Path.Combine(Application.streamingAssetsPath, filename));
			}

			public FileClient.DataStream dataStream;
		}


		/// <summary>
		/// Constructs a .MOT file client instance.
		/// </summary>
		///
		public FileClient()
		{
			this.sceneListeners = new List<SceneListener>();
			scene               = new Scene();
			sceneMutex          = new Mutex();
			dataStream          = null;
			streamingTimer      = null;
			notifyListeners     = false;
		}


		public bool Connect(IMoCapClient_ConnectionInfo connectionInfo)
		{
			// extract filename from connection info
			dataStream = (connectionInfo is ConnectionInfo) ?
				((ConnectionInfo)connectionInfo).dataStream :
				null; // fallback > no stream

			// try connecting
			try
			{
				if ((dataStream != null) && dataStream.Open())
				{
					GetSceneDescription();

					Debug.Log("Reading from MOT file '" + dataStream.GetName() + "'.");
					// print list of actor and device names
					if (scene.actors.Length > 0)
					{
						string actorNames = "";
						foreach (Actor a in scene.actors)
						{
							if (actorNames.Length > 0) { actorNames += ", "; }
							actorNames += a.name;
						}
						Debug.Log("Actors (" + scene.actors.Length + "): " + actorNames);
					}
					if (scene.devices.Length > 0)
					{
						string deviceNames = "";
						foreach (Device d in scene.devices)
						{
							if (deviceNames.Length > 0) { deviceNames += ", "; }
							deviceNames += d.name;
						}
						Debug.Log("Devices (" + scene.devices.Length + "): " + deviceNames);
					}

					// immediately get first packet of frame data
					GetFrameData();

					NotifyListeners_Change();

					streamingTimer = new Timer(new TimerCallback(StreamingTimerCallback));
					streamingTimer.Change(0, 1000 / updateRate);
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning("Could not read from MOT file '" + dataStream.GetName() + "' (" + e.Message + ").");
				dataStream.Close();
				dataStream = null;
			}

			return IsConnected();
		}


		public bool IsConnected()
		{
			return (dataStream != null);
		}


		public void Disconnect()
		{
			// stop streaming data
			if (streamingTimer != null)
			{
				streamingTimer.Dispose();
				streamingTimer = null;
			}

			// close file
			if (dataStream != null)
			{
				dataStream.Close();
				dataStream = null;
			}

			// and then stop
			sceneListeners.Clear();
		}


		public String GetDataSourceName()
		{
			return "MOT file '" + dataStream.GetName() + "'";
		}


		public void Update()
		{
			sceneMutex.WaitOne();
			if (notifyListeners)
			{
				// if one or more frames have been read
				NotifyListeners_Update();
				notifyListeners = false;
			}
			sceneMutex.ReleaseMutex();
		}


		public Scene GetScene()
		{
			return scene;
		}


		public bool AddSceneListener(SceneListener listener)
		{
			bool added = false;
			if (!sceneListeners.Contains(listener))
			{
				sceneListeners.Add(listener);
				added = true;
				// immediately trigger callback
				listener.SceneChanged(scene);
			}
			return added;
		}


		public bool RemoveSceneListener(SceneListener listener)
		{
			return sceneListeners.Remove(listener);
		}


		private void GetSceneDescription()
		{
			// read header
			int headerParts = dataStream.ReadNextLine();
			if ((headerParts < 3) ||
				!dataStream.GetString().Equals("MotionServer Data File"))
			{
				throw new FileLoadException("Invalid MOT file header");
			}
			fileVersion = dataStream.GetInt();
			updateRate  = dataStream.GetInt();

			int descriptionParts = dataStream.ReadNextLine();
			if ((descriptionParts < 2) ||
				!dataStream.GetString().Equals("Descriptions"))
			{
				throw new FileLoadException("Missing description section");
			}
			int descriptionCount = dataStream.GetInt();

			List<Actor>  actors  = new List<Actor>();
			List<Device> devices = new List<Device>();
			for (int descrIdx = 0; descrIdx <  descriptionCount; descrIdx++)
			{
				int descrParts = dataStream.ReadNextLine();
				if ((descrParts < 2) ||
					(dataStream.GetInt() != descrIdx))
				{
					throw new FileLoadException("Invalid description block #" + descrIdx);
				}

				switch (dataStream.GetString()[0])
				{
					case 'M': ReadMarkersetDescription( ref actors); break;
					case 'R': ReadRigidBodyDescription( ref actors); break;
					case 'S': ReadSkeletonDescription(  ref actors); break;
					case 'F': ReadForcePlateDescription(ref devices); break;
					default : throw new FileLoadException("Invalid description block #" + descrIdx);
				}
			}
			scene.actors  = actors.ToArray();
			scene.devices = devices.ToArray();
			
			// here comes the frame data
			int frameParts = dataStream.ReadNextLine();
			if ((frameParts < 1) ||
				!dataStream.GetString().Equals("Frames"))
			{
				throw new FileLoadException("Missing frame section");
			}

			// remember that this is the end of the header for rewinding/looping
			dataStream.MarkPosition();
		}


		private void ReadMarkersetDescription(ref List<Actor> actors)
		{
			int    id   = 0;                      // no ID for markersets
			string name = dataStream.GetString(); // markerset name
			Actor actor = new Actor(scene, name, id);

			int nMarkers  = dataStream.GetInt(); // marker count
			actor.markers = new Marker[nMarkers];
			for (int markerIdx = 0; markerIdx < nMarkers; markerIdx++)
			{
				name = dataStream.GetString();
				Marker marker = new Marker(actor, name);
				actor.markers[markerIdx] = marker;
			}
			actors.Add(actor);
		}


		private void ReadRigidBodyDescription(ref List<Actor> actors)
		{
			int    id   = dataStream.GetInt();    // ID
			string name = dataStream.GetString(); // name

			// rigid body name should be equal to actor name: search
			Actor actor = null;
			foreach (Actor a in actors)
			{
				if (a.name.Equals(name))
				{
					actor = a;
				}
			}
			if (actor == null)
			{
				Debug.LogWarning("Rigid Body " + name + " could not be matched to an actor.");
				actor = new Actor(scene, name, id);
				actors.Add(actor);
			}

			Bone bone = new Bone(actor, name, id);

			dataStream.GetInt();             // Parent ID (ignore for rigid body)
			bone.parent = null;              // rigid bodies should not have a parent
			bone.ox = dataStream.GetFloat(); // X offset
			bone.oy = dataStream.GetFloat(); // Y offset
			bone.oz = dataStream.GetFloat(); // Z offset

			actor.bones = new Bone[1];
			actor.bones[0] = bone;
		}


		private void ReadSkeletonDescription(ref List<Actor> actors)
		{
			int    skeletonId   = dataStream.GetInt();    // ID
			string skeletonName = dataStream.GetString(); // name

			// rigid body name should be equal to actor name: search
			Actor actor = null;
			foreach (Actor a in actors)
			{
				if (a.name.CompareTo(skeletonName) == 0)
				{
					actor = a;
					actor.id = skeletonId; // associate actor and skeleton 
				}
			}
			if (actor == null)
			{
				// names don't match > try IDs
				if ((skeletonId >= 0) && (skeletonId < actors.Count))
				{
					actor = actors[skeletonId];
				}
			}
			if (actor == null)
			{
				Debug.LogWarning("Skeleton " + skeletonName + " could not be matched to an actor.");
				actor = new Actor(scene, skeletonName, skeletonId);
				actors.Add(actor);
			}

			int nBones  = dataStream.GetInt(); // Skeleton bone count
			actor.bones = new Bone[nBones];
			for (int boneIdx = 0; boneIdx < nBones; boneIdx++)
			{
				int    id   = dataStream.GetInt();    // bone ID
				String name = dataStream.GetString(); // bone name 
				Bone   bone = new Bone(actor, name, id);

				int parentId = dataStream.GetInt(); // Skeleton parent ID
				bone.parent = actor.FindBone(parentId); 
				if (bone.parent != null)
				{
					// if bone has a parent, update child list of parent
					bone.parent.children.Add(bone);
				}
				bone.BuildChain(); // build chain from root to this bone

				bone.ox = dataStream.GetFloat(); // X offset
				bone.oy = dataStream.GetFloat(); // Y offset
				bone.oz = dataStream.GetFloat(); // Z offset

				actor.bones[boneIdx] = bone;
			}
		}


		private void ReadForcePlateDescription(ref List<Device> devices)
		{
			int    id   = dataStream.GetInt();    // ID
			string name = dataStream.GetString(); // name
			Device device = new Device(scene, name, id); // create device

			int nChannels   = dataStream.GetInt(); // channel count
			device.channels = new Channel[nChannels];
			for (int channelIdx = 0; channelIdx < nChannels; channelIdx++)
			{
				name = dataStream.GetString();
				Channel channel = new Channel(device, name);
				device.channels[channelIdx] = channel;
			}
			devices.Add(device);
		}


		private void GetFrameData()
		{
			if (dataStream.EndReached())
			{
				Debug.Log("End of MOT file reached > looping");
				// end of file > start from beginning
				dataStream.Rewind();
			}

			dataStream.ReadNextLine();
			scene.frameNumber = dataStream.GetInt();             // frame number
			scene.latency     = dataStream.GetFloat(); // latency in s

			if (!dataStream.GetString().Equals("M"))
			{
				throw new FileLoadException("Invalid marker frame block");
			}
			// Read actor data
			int nActors = dataStream.GetInt(); // actor count
			for (int actorIdx = 0; actorIdx < nActors; actorIdx++)
			{
				Actor actor    = scene.actors[actorIdx];
				int   nMarkers = dataStream.GetInt();
				for (int markerIdx = 0; markerIdx < nMarkers; markerIdx++)
				{
					Marker marker = actor.markers[markerIdx];
					// Read position
					marker.px = dataStream.GetFloat();
					marker.py = dataStream.GetFloat();
					marker.pz = dataStream.GetFloat();
					TransformToUnity(ref marker);

					// marker is tracked when at least one coordinate is not 0
					marker.tracked = (marker.px != 0) ||
									 (marker.py != 0) ||
									 (marker.pz != 0);
				}
			}

			// Read rigid body data
			if (!dataStream.GetString().Equals("R"))
			{
				throw new FileLoadException("Invalid rigid body frame block");
			}
			int nRigidBodies = dataStream.GetInt(); // rigid body count
			for (int rigidBodyIdx = 0; rigidBodyIdx < nRigidBodies; rigidBodyIdx++)
			{
				int rigidBodyID = dataStream.GetInt(); // get rigid body ID
				Bone bone = scene.FindActor(rigidBodyID).bones[0];

				// Read position/rotation 
				bone.px = dataStream.GetFloat(); // position
				bone.py = dataStream.GetFloat();
				bone.pz = dataStream.GetFloat();
				bone.qx = dataStream.GetFloat(); // rotation
				bone.qy = dataStream.GetFloat();
				bone.qz = dataStream.GetFloat();
				bone.qw = dataStream.GetFloat();
				TransformToUnity(ref bone);

				bone.length = dataStream.GetFloat(); // Mean error, used as length
				int state = dataStream.GetInt();     // state
				bone.tracked = (state & 0x01) != 0;
			}

			// Read skeleton data
			if (!dataStream.GetString().Equals("S"))
			{
				throw new FileLoadException("Invalid skeleton frame block");
			}
			int nSkeletons = dataStream.GetInt(); // skeleton count
			for (int skeletonIdx = 0; skeletonIdx < nSkeletons; skeletonIdx++)
			{
				int   skeletonId = dataStream.GetInt();
				Actor actor      = scene.FindActor(skeletonId);

				// # of bones in skeleton
				int nBones = dataStream.GetInt();
				for (int nBodyIdx = 0; nBodyIdx < nBones; nBodyIdx++)
				{
					int boneId = dataStream.GetInt();
					Bone bone = actor.bones[boneId];

					// Read position/rotation 
					bone.px = dataStream.GetFloat(); // position
					bone.py = dataStream.GetFloat();
					bone.pz = dataStream.GetFloat();
					bone.qx = dataStream.GetFloat(); // rotation
					bone.qy = dataStream.GetFloat();
					bone.qz = dataStream.GetFloat();
					bone.qw = dataStream.GetFloat();
					TransformToUnity(ref bone);

					bone.length = dataStream.GetFloat(); // Mean error, used as length
					int state = dataStream.GetInt();     // state
					bone.tracked = (state & 0x01) != 0;
				}
			} // next skeleton 

			// Read force plate data
			if (!dataStream.GetString().Equals("F"))
			{
				throw new FileLoadException("Invalid force plate frame block");
			}
			int nForcePlates = dataStream.GetInt(); // force plate count
			for (int forcePlateIdx = 0; forcePlateIdx < nForcePlates; forcePlateIdx++)
			{
				// read force plate ID and find corresponding device
				int    forcePlateId = dataStream.GetInt();
				Device device       = scene.FindDevice(forcePlateId);
				// channel count
				int nChannels = dataStream.GetInt();
				// channel data
				for (int chn = 0; chn < nChannels; chn++)
				{
					float value = dataStream.GetFloat();
					device.channels[chn].value = value;
				}
			}
		}


		/// <summary>
		/// Converts a marker position from a right handed coordinate to a left handed (Unity).
		/// </summary>
		/// <param name="pos">the marker to convert</param>
		/// 
		private void TransformToUnity(ref Marker marker)
		{
			marker.pz *= -1; // flip Z
		}


		/// <summary>
		/// Converts a bone from a right handed rotation to a left handed (Unity).
		/// </summary>
		/// <param name="bone">the bone to convert</param>
		/// 
		private void TransformToUnity(ref Bone bone)
		{
			bone.pz *= -1; // flip Z pos

			/*
			Quaternion q = new Quaternion(bone.qx, bone.qy, bone.qz, bone.qw);
			Vector3 e = q.eulerAngles;
			Quaternion x = Quaternion.AngleAxis( e.x, Vector3.right);
			Quaternion y = Quaternion.AngleAxis(-e.y + 180, Vector3.up);
			Quaternion z = Quaternion.AngleAxis( e.z, Vector3.forward);
			q = (z * y * x);

			bone.qx = q.x;
			bone.qy = q.y;
			bone.qz = q.z;
			bone.qw = q.w;
			*/
			
			bone.qx *= -1;
			bone.qy *= -1;
			
			/*
			Quaternion q = new Quaternion(bone.qx, bone.qy, bone.qz, bone.qw);
			float   angle = 0.0f;
			Vector3 axis = Vector3.zero;
			q.ToAngleAxis(out angle, out axis);
			axis.z = -axis.z;
			q = Quaternion.AngleAxis(-angle, axis);

			Debug.Log(
				"from X=" + bone.qx + ",Y=" + bone.qy + ",Z=" + bone.qz + ",W=" + bone.qw +
				"  to  X=" + q.x + ",Y=" + q.y + ",Z=" + q.z + ",W=" + q.w);				

			bone.qx = q.x;
			bone.qy = q.y;
			bone.qz = q.z;
			bone.qw = q.w;
			*/
		}


		private void NotifyListeners_Update()
		{
			foreach (SceneListener listener in sceneListeners)
			{
				listener.SceneUpdated(scene);
			}
		}


		private void NotifyListeners_Change()
		{
			foreach (SceneListener listener in sceneListeners)
			{
				listener.SceneChanged(scene);
			}
		}


		private void StreamingTimerCallback(object state)
		{
			sceneMutex.WaitOne();
			GetFrameData();
			notifyListeners = true;
			sceneMutex.ReleaseMutex();
		}


		private DataStream          dataStream;
		private int                 fileVersion, updateRate;
		private Timer               streamingTimer;

		private Scene               scene;
		private Mutex               sceneMutex;
		private List<SceneListener> sceneListeners;
		private bool                notifyListeners;


		public interface DataStream
		{
			string GetName();
			bool   Open();
			int    ReadNextLine();
			void   MarkPosition();
			bool   EndReached();
			void   Rewind();
			string GetString();
			int    GetInt();
			float  GetFloat();
			void   Close();
		}


		private abstract class AbstractDataStream : DataStream
		{
			public AbstractDataStream()
			{
				stream         = null;
				filePosition   = 0;
				markerPosition = 0;
			}

			public abstract string GetName();

			public abstract bool Open();

			public int ReadNextLine()
			{
				string line;
				do
				{
					line = stream.ReadLine().TrimStart();
					filePosition++;
				}
				while (line.StartsWith("#") && !stream.EndOfStream);
				data    = line.Split('\t');
				dataIdx = 0;
				return data.Length;
			}

			public void MarkPosition()
			{
				markerPosition = filePosition;
			}

			public bool EndReached()
			{
				return stream.EndOfStream;
			}

			abstract public void Rewind();

			public string GetString()
			{
				return data[dataIdx++].Trim().Trim('"');
			}

			public int GetInt()
			{
				return int.Parse(data[dataIdx++]);
			}

			public float GetFloat()
			{
				return float.Parse(data[dataIdx++]);
			}

			public void Close()
			{
				if (stream != null)
				{
					stream.Close();
					stream = null;
				}
			}

			protected StreamReader stream;
			protected string[]     data;
			protected int          dataIdx;
			protected int          filePosition, markerPosition;
		}


		private class DataStream_File : AbstractDataStream
		{
			public DataStream_File(string filename)
			{
				this.filename = filename;
			}

			public override string GetName()
			{
				return filename;
			}

			public override bool Open()
			{
				if (File.Exists(filename))
				{
					Close();
					stream = new StreamReader(filename);
					filePosition = 0;
				}
				return (stream != null);
			}

			public override void Rewind()
			{ 
				Open();
				while (filePosition < markerPosition)
				{
					ReadNextLine();
				}
			}

			private string filename;
		}


		private class DataStream_TextAsset : AbstractDataStream
		{
			public DataStream_TextAsset(TextAsset asset)
			{
				this.asset = asset;
			}

			public override string GetName()
			{
				return asset.name;
			}

			public override bool Open()
			{
				Close();
				memoryStream = new MemoryStream(asset.bytes);
				stream       = new StreamReader(memoryStream);
				filePosition = 0;
				return true;
			}

			public override void Rewind()
			{
				memoryStream.Seek(0, SeekOrigin.Begin);
				stream       = new StreamReader(memoryStream);
				filePosition = 0;
				while (filePosition < markerPosition)
				{
					ReadNextLine();
				}
			}

			private TextAsset    asset;
			private MemoryStream memoryStream;
		}
	}

}
