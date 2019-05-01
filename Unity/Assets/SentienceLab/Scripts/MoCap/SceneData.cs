#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

/// <summary>
/// Scene description and data.
/// </summary>
/// 
namespace SentienceLab.MoCap
{
	/// <summary>
	/// Class for holding the information of a MoCap scene.
	/// </summary>
	/// 
	public class Scene
	{
		public int      frameNumber; // current frame number
		public double   timestamp;   // current timestamp
		public float    latency;     // latency in seconds from camera capture to the SDK sending the data

		public List<Actor>  actors;  // actor data 
		public List<Device> devices; // data for interaction devices

		public readonly Mutex mutex; // mutex for controlling acceess to the scene data


		/// <summary>
		/// Initializes a new and empty instance of the <see cref="MoCap.Scene"/> class.
		/// </summary>
		/// 
		public Scene()
		{
			frameNumber = 0;
			timestamp   = 0;
			latency     = 0;
			actors      = new List<Actor>();
			devices     = new List<Device>();
			mutex       = new Mutex();
		}


		/// <summary>
		/// Finds an actor by name.
		/// </summary>
		/// <returns>The actor with the given name or <code>null</code> if the actor doesn't exist</returns>
		/// <param name="nameRegEx">The name of the actor to find as a regular expression</param>
		/// 
		public Actor FindActor(string nameRegEx)
		{
			foreach (Actor a in actors)
			{
				if (a != null)
				{
					if (Regex.Matches(a.name, nameRegEx).Count > 0) return a;
				}
			}
			return null;
		}


		/// <summary>
		/// Finds an actor by id.
		/// </summary>
		/// <returns>The actor with the given id or <code>null</code> if the actor doesn't exist</returns>
		/// <param name="id">The id of the actor to find</param>
		/// 
		public Actor FindActor(int id)
		{
			// quick check if id is an array index
			if ( (id >= 0) && (id < actors.Count) && 
				 (actors[id] != null) && (actors[id].id == id) ) return actors[id];

			// no > linear search
			foreach ( Actor a in actors )
			{
				if ( (a != null) && (a.id == id) ) return a;
			}

			// found nothing
			return null;
		}


		/// <summary>
		/// Finds an interaction device by name.
		/// </summary>
		/// <returns>The device with the given name or <code>null</code> if the device doesn't exist</returns>
		/// <param name="name">The name of the device to find</param>
		/// 
		public Device FindDevice(string name)
		{
			foreach (Device d in devices)
			{
				if ((d != null) && (d.name.CompareTo(name) == 0)) return d;
			}
			return null;
		}


		/// <summary>
		/// Finds an interaction device by ID.
		/// </summary>
		/// <returns>The device with the given ID or <code>null</code> if the device doesn't exist</returns>
		/// <param name="id">The ID of the device to find</param>
		/// 
		public Device FindDevice(int id)
		{
			foreach (Device d in devices)
			{
				if ((d != null) && (d.id == id)) return d;
			}
			return null;
		}
	}


	/// <summary>
	/// Listener interface for reacting to changes in scene data.
	/// </summary>
	/// 
	public interface SceneListener
	{
		/// <summary>
		/// Called when the scene data has been updated.
		/// </summary>
		/// <param name="scene">the scene that has been updated</param>
		/// 
		void SceneDataUpdated(Scene scene);


		/// <summary>
		/// Called when the scene definition has changed.
		/// </summary>
		/// <param name="scene">the scene that has been updated</param>
		/// 
		void SceneDefinitionChanged(Scene scene);
	}


	/// <summary>
	/// Class for holding the information of a MoCap actor.
	/// </summary>
	/// 
	public class Actor
	{
		public readonly Scene  scene; // scene this actor belongs to
		public readonly string name;  // Name of the actor
		public          int    id;    // ID of the actor (not readonly because skeleton description might change it)

		public Marker[] markers;      // Marker data
		public Bone[]   bones;        // Bone data


		/// <summary>
		/// Creates a new actor.
		/// </summary>
		/// <param name="scene">the scene the actor belongs to</param>
		/// <param name="name">the name of the actor</param>
		/// <param name="id">the ID of the actor (-1: assign automatic ID based on order in actor list, starting at 1)</param>
		/// 
		public Actor(Scene scene, string name, int id = -1)
		{
			this.scene = scene;
			this.name  = name;
			this.id    = (id < 0) ? scene.actors.Count + 1 : id;

			markers = new Marker[0];
			bones   = new Bone[0]; 
		}


		/// <summary>
		/// Finds a marker by name.
		/// </summary>
		/// <param name="name">the name of the marker to search for</param>
		/// <returns>The marker with the specified name or <code>null</code> if the marker could not be found</returns>
		/// 
		public Marker FindMarker(string name)
		{
			foreach ( Marker marker in markers )
			{
				if ( (marker != null) && (marker.name.CompareTo(name) == 0) ) return marker;
			}
			return null;
		}


		/// <summary>
		/// Finds a bone by name.
		/// </summary>
		/// <param name="name">the name of the bone to search for</param>
		/// <returns>The bone with the specified name or <code>null</code> if the bone could not be found</returns>
		/// 
		public Bone FindBone(string name)
		{
			foreach ( Bone bone in bones )
			{
				if ( (bone != null) && (bone.name.CompareTo(name) == 0) ) return bone;
			}
			return null;
		}


		/// <summary>
		/// Finds a bone by id.
		/// </summary>
		/// <param name="id">the id of the bone to search for</param>
		/// <returns>The bone with the specified id or <code>null</code> if the bone could not be found</returns>
		/// 
		public Bone FindBone(int id)
		{
			foreach ( Bone bone in bones )
			{
				if ( (bone != null) && (bone.id == id) ) return bone;
			}
			return null;
		}
	}


	/// <summary>
	/// Class for information about a single MoCap marker.
	/// </summary>
	/// 
	public class Marker
	{
		public readonly Actor  actor;      // actor this marker belongs to
		public readonly string name;       // name of the marker
		public          float  px, py, pz; // position of the marker
		public          bool   tracked;    // tracking state

		public readonly MoCapDataBuffer buffer; // buffer for data


		/// <summary>
		/// Creates a new marker with a name.
		/// </summary>
		/// <param name="actor">actor this marker is associated with</param>
		/// <param name="name">name of the marker</param>
		/// 
		public Marker(Actor actor, string name)
		{
			this.actor = actor;
			this.name  = name;
			px = py = pz = 0;
			tracked = false;
			buffer  = new MoCapDataBuffer(this);
		}

		/// <summary>
		/// Copies data from a Unity position into the structure
		/// </summary>
		/// <param name="pos">Position data</param>
		/// 
		public void CopyFrom(UnityEngine.Vector3 pos)
		{
			px = pos.x; py = pos.y; pz = pos.z;
		}

		/// <summary>
		/// Copies data from the structure to a Unity position class
		/// </summary>
		/// <param name="pos">Position data to copy into</param>
		/// 
		public void CopyTo(ref UnityEngine.Vector3 pos)
		{
			pos.x = px; pos.y = py; pos.z = pz;
		}
	}


	/// <summary>
	/// Class for information about a single bone.
	/// </summary>
	public class Bone
	{
		public readonly Actor  actor; // actor this bone belongs to
		public readonly string name;  // name of the bone
		public readonly int    id;    // ID of the bone

		public Bone   parent;         // parent of the bone (or <code>null</code> if there is no parent)
		public float  ox, oy, oz;     // offset of the bone

		public float  px, py, pz;     // position of the bone
		public float  qx, qy, qz, qw; // rotation of the bone
		public float  length;         // length of the bone
		public bool   tracked;        // true if bone is tracked, false if tracking was lost

		public List<Bone> children;   // children of this bone
		public List<Bone> chain;      // chain from root bone to this bone

		public readonly MoCapDataBuffer buffer; // buffer for data


		/// <summary>
		/// Creates a new bone with a name and ID.
		/// </summary>
		/// <param name="actor">actor this bonebelongs to</param>
		/// <param name="name">name of the bone</param>
		/// <param name="in">ID of the bone</param>
		/// 
		public Bone(Actor actor, string name, int id)
		{
			this.actor = actor;
			this.name  = name;
			this.id    = id;

			ox = oy = oz = 0;  // no offset
			parent = null;     // no parent

			px = py = pz = 0;         // origin position
			qx = qy = qz = 0; qw = 1; // no rotation
			length = 0;               // no length

			children = new List<Bone>();
			chain    = new List<Bone>();
			chain.Add(this); // this bone is part of the chain

			buffer = new MoCapDataBuffer(this);
		}


		/// <summary>
		/// Builds the chain list from the root bone to this bone.
		/// </summary>
		/// 
		public void BuildChain()
		{
			if (parent != null)
			{
				// simply attach the parent chain to the front
				chain.InsertRange(0, parent.chain);
			}
		}

		/// <summary>
		/// Copies data from a Unity position into the structure
		/// </summary>
		/// <param name="pos">Position data</param>
		/// 
		public void CopyFrom(UnityEngine.Vector3 pos)
		{
			px = pos.x; py = pos.y; pz = pos.z;
		}

		/// <summary>
		/// Copies data from a Unity rotation into the structure
		/// </summary>
		/// <param name="rot">Rotation data</param>
		/// 
		public void CopyFrom(UnityEngine.Quaternion rot)
		{
			qx = rot.x; qy = rot.y; qz = rot.z; qw = rot.w;
		}

		/// <summary>
		/// Copies data from a Unity position and rotation into the structure
		/// </summary>
		/// <param name="pos">Position data</param>
		/// <param name="rot">Rotation data</param>
		/// 
		public void CopyFrom(UnityEngine.Vector3 pos, UnityEngine.Quaternion rot)
		{
			px = pos.x; py = pos.y; pz = pos.z;
			qx = rot.x; qy = rot.y; qz = rot.z; qw = rot.w;
		}

		/// <summary>
		/// Copies data from the structure to a Unity position class
		/// </summary>
		/// <param name="pos">Position data to copy into</param>
		/// 
		public void CopyTo(ref UnityEngine.Vector3 pos)
		{
			pos.x = px; pos.y = py; pos.z = pz;
		}

		/// <summary>
		/// Copies data from the structure to a Unity position class
		/// </summary>
		/// <param name="rot">Rotation data to copy into</param>
		/// 
		public void CopyTo(ref UnityEngine.Quaternion rot)
		{
			rot.x = qx; rot.y = qy; rot.z = qz; rot.w = qw;
		}

		/// <summary>
		/// Copies data from the structure to a Unity position and rotation class
		/// </summary>
		/// <param name="pos">Position data to copy into</param>
		/// <param name="rot">Rotation data to copy into</param>
		/// 
		public void CopyTo(ref UnityEngine.Vector3 pos, ref UnityEngine.Quaternion rot)
		{
			pos.x = px; pos.y = py; pos.z = pz;
			rot.x = qx; rot.y = qy; rot.z = qz; rot.w = qw;
		}
	}


	/// <summary>
	/// Class for holding the information of an interaction device.
	/// </summary>
	/// 
	public class Device
	{
		public readonly Scene     scene;    // scene this device belongs to
		public readonly string    name;     // name of the device
		public readonly int       id;       // id of the device
		public          Channel[] channels; // channel data


		/// <summary>
		/// Creates a new device with a name and an ID.
		/// </summary>
		/// <param name="scene">scene this device belongs to</param>
		/// <param name="name">name of the device</param>
		/// <param name="id">ID of the device</param>
		/// 
		public Device(Scene scene, string name, int id)
		{
			this.scene = scene;
			this.name  = name;
			this.id    = id;
		}


		/// <summary>
		/// Finds a channel by name.
		/// </summary>
		/// <param name="name">the name of the channel to search for</param>
		/// <returns>The marker with the specified name or <code>null</code> if the marker could not be found</returns>
		/// 
		public Channel FindChannel(string name)
		{
			foreach (Channel channel in channels)
			{
				if ((channel != null) && (channel.name.CompareTo(name) == 0)) return channel;
			}
			return null;
		}
	}


	/// <summary>
	/// Class for holding the information of a single interaction device channel.
	/// </summary>
	/// 
	public class Channel
	{
		public readonly Device device; // device this channel belongs to
		public readonly string name;   // name of the channel
		public          float  value;  // value of the channel

		/// <summary>
		/// Creates a new device channel with a name.
		/// </summary>
		/// <param name="device">device this channel belongs to</param>
		/// <param name="name">name of the channel</param>
		/// 
		public Channel(Device device, string name)
		{
			this.device = device;
			this.name   = name;
		}
	}

}
