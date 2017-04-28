#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SentienceLab.IO;
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
				dataStream = new DataStream_File(Path.Combine(Application.streamingAssetsPath, filename));
			}

			public DataStream dataStream;
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
			paused              = false;
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


		public float GetFramerate()
		{
			return updateRate;
		}


		public void SetPaused(bool pause)
		{
			paused = pause;
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
				!dataStream.GetNextString().Equals("MotionServer Data File"))
			{
				throw new FileLoadException("Invalid MOT file header");
			}

			fileVersion = dataStream.GetNextInt();
			if ((fileVersion < 1) || (fileVersion > 1))
			{
				throw new FileLoadException("Invalid MOT file version number");
			}

			updateRate = dataStream.GetNextInt();

			int descriptionParts = dataStream.ReadNextLine();
			if ((descriptionParts < 2) ||
				!dataStream.GetNextString().Equals("Descriptions"))
			{
				throw new FileLoadException("Missing description section");
			}
			int descriptionCount = dataStream.GetNextInt();

			List<Actor>  actors  = new List<Actor>();
			List<Device> devices = new List<Device>();
			for (int descrIdx = 0; descrIdx <  descriptionCount; descrIdx++)
			{
				int descrParts = dataStream.ReadNextLine();
				if ((descrParts < 2) ||
					(dataStream.GetNextInt() != descrIdx))
				{
					throw new FileLoadException("Invalid description block #" + descrIdx);
				}

				switch (dataStream.GetNextString()[0])
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
				!dataStream.GetNextString().Equals("Frames"))
			{
				throw new FileLoadException("Missing frame section");
			}

			// remember that this is the end of the header for rewinding/looping
			dataStream.MarkPosition();
		}


		private void ReadMarkersetDescription(ref List<Actor> actors)
		{
			int    id   = 0;                      // no ID for markersets
			string name = dataStream.GetNextString(); // markerset name
			Actor actor = new Actor(scene, name, id);

			int nMarkers  = dataStream.GetNextInt(); // marker count
			actor.markers = new Marker[nMarkers];
			for (int markerIdx = 0; markerIdx < nMarkers; markerIdx++)
			{
				name = dataStream.GetNextString();
				Marker marker = new Marker(actor, name);
				actor.markers[markerIdx] = marker;
			}
			actors.Add(actor);
		}


		private void ReadRigidBodyDescription(ref List<Actor> actors)
		{
			int    id   = dataStream.GetNextInt();    // ID
			string name = dataStream.GetNextString(); // name

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

			dataStream.GetNextInt();            // Parent ID (ignore for rigid body)
			bone.parent = null;                 // rigid bodies should not have a parent
			bone.ox = dataStream.GetNexFloat(); // X offset
			bone.oy = dataStream.GetNexFloat(); // Y offset
			bone.oz = dataStream.GetNexFloat(); // Z offset

			actor.bones = new Bone[1];
			actor.bones[0] = bone;
		}


		private void ReadSkeletonDescription(ref List<Actor> actors)
		{
			int    skeletonId   = dataStream.GetNextInt();    // ID
			string skeletonName = dataStream.GetNextString(); // name

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

			int nBones  = dataStream.GetNextInt(); // Skeleton bone count
			actor.bones = new Bone[nBones];
			for (int boneIdx = 0; boneIdx < nBones; boneIdx++)
			{
				int    id   = dataStream.GetNextInt();    // bone ID
				String name = dataStream.GetNextString(); // bone name 
				Bone   bone = new Bone(actor, name, id);

				int parentId = dataStream.GetNextInt(); // Skeleton parent ID
				bone.parent = actor.FindBone(parentId); 
				if (bone.parent != null)
				{
					// if bone has a parent, update child list of parent
					bone.parent.children.Add(bone);
				}
				bone.BuildChain(); // build chain from root to this bone

				bone.ox = dataStream.GetNexFloat(); // X offset
				bone.oy = dataStream.GetNexFloat(); // Y offset
				bone.oz = dataStream.GetNexFloat(); // Z offset

				actor.bones[boneIdx] = bone;
			}
		}


		private void ReadForcePlateDescription(ref List<Device> devices)
		{
			int    id   = dataStream.GetNextInt();    // ID
			string name = dataStream.GetNextString(); // name
			Device device = new Device(scene, name, id); // create device

			int nChannels   = dataStream.GetNextInt(); // channel count
			device.channels = new Channel[nChannels];
			for (int channelIdx = 0; channelIdx < nChannels; channelIdx++)
			{
				name = dataStream.GetNextString();
				Channel channel = new Channel(device, name);
				device.channels[channelIdx] = channel;
			}
			devices.Add(device);
		}


		private void GetFrameData()
		{
			if (dataStream.EndOfStream())
			{
				Debug.Log("End of MOT file reached > looping");
				// end of file > start from beginning
				dataStream.Rewind();
			}

			dataStream.ReadNextLine();
			scene.frameNumber = dataStream.GetNextInt();   // frame number
			scene.latency     = dataStream.GetNexFloat(); // latency in s

			if (!dataStream.GetNextString().Equals("M"))
			{
				throw new FileLoadException("Invalid marker frame block");
			}
			// Read actor data
			int nActors = dataStream.GetNextInt(); // actor count
			for (int actorIdx = 0; actorIdx < nActors; actorIdx++)
			{
				Actor actor    = scene.actors[actorIdx];
				int   nMarkers = dataStream.GetNextInt();
				for (int markerIdx = 0; markerIdx < nMarkers; markerIdx++)
				{
					Marker marker = actor.markers[markerIdx];
					// Read position
					marker.px = dataStream.GetNexFloat();
					marker.py = dataStream.GetNexFloat();
					marker.pz = dataStream.GetNexFloat();
					TransformToUnity(ref marker);

					// marker is tracked when at least one coordinate is not 0
					marker.tracked = (marker.px != 0) ||
									 (marker.py != 0) ||
									 (marker.pz != 0);
				}
			}

			// Read rigid body data
			if (!dataStream.GetNextString().Equals("R"))
			{
				throw new FileLoadException("Invalid rigid body frame block");
			}
			int nRigidBodies = dataStream.GetNextInt(); // rigid body count
			for (int rigidBodyIdx = 0; rigidBodyIdx < nRigidBodies; rigidBodyIdx++)
			{
				int rigidBodyID = dataStream.GetNextInt(); // get rigid body ID
				Bone bone = scene.FindActor(rigidBodyID).bones[0];

				// Read position/rotation 
				bone.px = dataStream.GetNexFloat(); // position
				bone.py = dataStream.GetNexFloat();
				bone.pz = dataStream.GetNexFloat();
				bone.qx = dataStream.GetNexFloat(); // rotation
				bone.qy = dataStream.GetNexFloat();
				bone.qz = dataStream.GetNexFloat();
				bone.qw = dataStream.GetNexFloat();
				TransformToUnity(ref bone);

				bone.length = dataStream.GetNexFloat(); // Mean error, used as length
				int state = dataStream.GetNextInt();     // state
				bone.tracked = (state & 0x01) != 0;
			}

			// Read skeleton data
			if (!dataStream.GetNextString().Equals("S"))
			{
				throw new FileLoadException("Invalid skeleton frame block");
			}
			int nSkeletons = dataStream.GetNextInt(); // skeleton count
			for (int skeletonIdx = 0; skeletonIdx < nSkeletons; skeletonIdx++)
			{
				int   skeletonId = dataStream.GetNextInt();
				Actor actor      = scene.FindActor(skeletonId);

				// # of bones in skeleton
				int nBones = dataStream.GetNextInt();
				for (int nBodyIdx = 0; nBodyIdx < nBones; nBodyIdx++)
				{
					int boneId = dataStream.GetNextInt();
					Bone bone = actor.bones[boneId];

					// Read position/rotation 
					bone.px = dataStream.GetNexFloat(); // position
					bone.py = dataStream.GetNexFloat();
					bone.pz = dataStream.GetNexFloat();
					bone.qx = dataStream.GetNexFloat(); // rotation
					bone.qy = dataStream.GetNexFloat();
					bone.qz = dataStream.GetNexFloat();
					bone.qw = dataStream.GetNexFloat();
					TransformToUnity(ref bone);

					bone.length = dataStream.GetNexFloat(); // Mean error, used as length
					int state = dataStream.GetNextInt();     // state
					bone.tracked = (state & 0x01) != 0;
				}
			} // next skeleton 

			// Read force plate data
			if (!dataStream.GetNextString().Equals("F"))
			{
				throw new FileLoadException("Invalid force plate frame block");
			}
			int nForcePlates = dataStream.GetNextInt(); // force plate count
			for (int forcePlateIdx = 0; forcePlateIdx < nForcePlates; forcePlateIdx++)
			{
				// read force plate ID and find corresponding device
				int    forcePlateId = dataStream.GetNextInt();
				Device device       = scene.FindDevice(forcePlateId);
				// channel count
				int nChannels = dataStream.GetNextInt();
				// channel data
				for (int chn = 0; chn < nChannels; chn++)
				{
					float value = dataStream.GetNexFloat();
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
			bone.qx *= -1; // flip X/Y quaternion component
			bone.qy *= -1;
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
			if (paused) return;

			sceneMutex.WaitOne();
			GetFrameData();
			notifyListeners = true;
			sceneMutex.ReleaseMutex();
		}


		private DataStream  dataStream;
		private int         fileVersion, updateRate;
		private Timer       streamingTimer;
		private bool        paused;

		private Scene               scene;
		private Mutex               sceneMutex;
		private List<SceneListener> sceneListeners;
		private bool                notifyListeners;
	}
}
