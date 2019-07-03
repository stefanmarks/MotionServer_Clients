#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Class for a MoCap client that uses Unity's XR classes.
	/// </summary>
	/// 
	class UnityXR_Client : IMoCapClient
	{
		/// <summary>
		/// Constructs a MoCap client that uses Unity XR devices.
		/// </summary>
		///
		public UnityXR_Client()
		{
			nodeStates = new List<XRNodeState>();
			scene      = new Scene();
			connected  = false;
		}


		public bool Connect(IMoCapClient_ConnectionInfo connectionInfo)
		{
			connected = XRDevice.isPresent;

			if (connected)
			{
				InputTracking.GetNodeStates(nodeStates);
				actors = new Dictionary<ulong, Actor>();

				// construct scene description
				scene.actors.Clear();
				foreach (XRNodeState state in nodeStates)
				{
					CreateActor(state);
				}
			}
			return connected;
		}


		private Actor CreateActor(XRNodeState state)
		{
			// some names are a bit too complex
			String name = InputTracking.GetNodeName(state.uniqueID);
			name = name.Replace("Windows Mixed Reality", "WMR");
			name = name.Replace(" ", "").Replace("-", "");

			// create actor
			Actor actor = new Actor(scene, name);
			actor.bones = new Bone[1];
			actor.bones[0] = new Bone(actor, "root", 0);
			scene.actors.Add(actor);
			actors.Add(state.uniqueID, actor);
			return actor;
		}


		public bool IsConnected()
		{
			return connected;
		}


		public void Disconnect()
		{
			connected = false; 
		}
		
		
		public String GetDataSourceName()
		{
			return "UnityXR/" + XRDevice.model;
		}


		public float GetFramerate()
		{
			return XRDevice.refreshRate;
		}


		public void SetPaused(bool pause)
		{
			// ignored
		}


		public void Update(ref bool dataChanged, ref bool sceneChanged)
		{
			// frame number and timestamp
			scene.frameNumber = Time.frameCount;
			scene.timestamp   = Time.time;

			// poll hand controllers to force detection 
			// InputTracking.GetLocalPosition(XRNode.LeftHand);
			// InputTracking.GetLocalPosition(XRNode.RightHand);

			// get new node data
			InputTracking.GetNodeStates(nodeStates);

			foreach (XRNodeState state in nodeStates)
			{
				Actor actor = null;
				if (actors.TryGetValue(state.uniqueID, out actor))
				{
					// update position, orientation, and tracking state
					Bone bone = actor.bones[0];
					bone.tracked = state.tracked;
					if (bone.tracked)
					{
						Vector3 pos;
						if (state.TryGetPosition(out pos)) bone.CopyFrom(pos);
						else bone.tracked = false;
						Quaternion rot;
						if (state.TryGetRotation(out rot)) bone.CopyFrom(rot);
						else bone.tracked = false;
					}
					dataChanged = true;
				}
				else
				{
					actor = CreateActor(state);
					Debug.Log("New actor added: " + actor.name);
					sceneChanged = true;
				}
			}

			dataChanged = true;
		}


		public Scene GetScene()
		{
			return scene;
		}


		private bool                      connected;
		private Scene                     scene;
		private Dictionary<ulong, Actor>  actors;
		private List<XRNodeState>         nodeStates;
	}

}
