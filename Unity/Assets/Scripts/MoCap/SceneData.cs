﻿using System.Collections.Generic;

/// <summary>
/// Scene description and data.
/// </summary>
/// 
namespace MoCap
{
	/// <summary>
	/// Class for holding the information of a MoCap scene.
	/// </summary>
	/// 
	public class Scene
	{
		public int      frameNumber; // current frame number
		public int      latency;     // latency in milliseconds from camera capture to the SDK sending the data
		public Actor[]  actors;      // actor data 
		public Device[] devices;     // data for interaction devices


		/// <summary>
		/// Initializes a new and empty instance of the <see cref="MoCap.Scene"/> class.
		/// </summary>
		/// 
		public Scene()
		{
			frameNumber = 0;
			latency     = 0;
			actors      = new Actor[0];
			devices     = new Device[0];
		}

		/// <summary>
		/// Finds an actor by name.
		/// </summary>
		/// <returns>The actor with the given name or <code>null</code> if the actor doesn't exist</returns>
		/// <param name="name">The name of the actor to find</param>
		/// 
		public Actor FindActor(string name)
		{
			foreach ( Actor a in actors )
			{
				if ( (a != null) && (a.name.CompareTo(name) == 0) ) return a;
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
			if ( (id >= 0) && (id < actors.Length) && 
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
	/// Class for holding the information of a MoCap actor.
	/// </summary>
	/// 
	public class Actor
	{
		public          int    id;   // ID of the actor (not readonly because skeleton description might change it)
		public readonly string name; // Name of the actor

		public Marker[] markers;     // Marker data
		public Bone[]   bones;       // Bone data


		/// <summary>
		/// Creates a new actor.
		/// </summary>
		/// <param name="id">the ID of the actor</param>
		/// <param name="name">the name of the actor</param>
		/// 
		public Actor(int id, string name)
		{
			this.id   = id;
			this.name = name;

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
		public readonly string name; // name of the marker
		public          float  px, py, pz;   // position of the marker

		/// <summary>
		/// Creates a new marker with a name
		/// </summary>
		/// <param name="name">name of the marker</param>
		/// 
		public Marker(string name)
		{
			this.name = name;
		}
	}


	/// <summary>
	/// Class for information about a single bone.
	/// </summary>
	public class Bone
	{
		public readonly string name;  // name of the bone
		public readonly int    id;    // ID of the bone

		public Bone   parent;         // parent of the bone (or <code>null</code> if there is no parent)
		public float  ox, oy, oz;     // offset of the bone
		public float  px, py, pz;     // position of the bone
		public float  qx, qy, qz, qw; // rotation of the bone
		public bool   tracked;        // true if bone is tracked, false if tracking was lost

		public List<Bone> children;   // children of this bone
		public List<Bone> chain;      // chain from root bone to this bone


		public Bone(int id, string name)
		{
			this.id   = id;
			this.name = name;

			px = py = pz = 0;         // origin position
			qx = qy = qz = 0; qw = 1; // no rotation
			ox = oy = oz = 0;         // no offset
			parent   = null;
			children = new List<Bone>();
			chain    = new List<Bone>();
			chain.Add(this);
		}


		/// <summary>
		/// Builds the chain list from the root bone to this bone.
		/// </summary>
		/// 
		public void BuildChain()
		{
			if (parent != null)
			{
				chain.InsertRange(0, parent.chain);
			}
		}
	}


	/// <summary>
	/// Listener interface for reacting to changes in actor data.
	/// </summary>
	/// 
	public interface ActorListener
	{
		/// <summary>
		/// Gets the name of the actor to monitor.
		/// </summary>
		/// <returns>The name of the actor to monitor</returns>
		string GetActorName();


		/// <summary>
		/// Called when the actor changes.
		/// </summary>
		/// <param name="actor">the actor that has changed</param>
		void ActorChanged(Actor actor);
	}


	/// <summary>
	/// Class for holding the information of an interaction device.
	/// </summary>
	/// 
	public class Device
	{
		public readonly int       id;       // id of the device
		public readonly string    name;     // name of the device
		public          Channel[] channels; // channel data


		/// <summary>
		/// Creates a new device with a name and an ID.
		/// </summary>
		/// <param name="id">ID of the device</param>
		/// <param name="name">name of the device</param>
		/// 
		public Device(int id, string name)
		{
			this.id   = id;
			this.name = name;
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
		public readonly string name;  // name of the channel
		public          float  value; // value of the channel

		/// <summary>
		/// Creates a new device channel with a name
		/// </summary>
		/// <param name="name">name of the channel</param>
		/// 
		public Channel(string name)
		{
			this.name = name;
		}
	}
}
