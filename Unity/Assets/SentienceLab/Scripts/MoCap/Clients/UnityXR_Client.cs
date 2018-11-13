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
		/// <param name="manager">the MoCapManager instance</param>
		///
		public UnityXR_Client(MoCapManager manager)
		{
			this.manager = manager;

			trackedDevices = new List<XRNode>(
				new XRNode[] {
					XRNode.LeftHand, XRNode.RightHand,
					XRNode.Head,
					XRNode.GameController, XRNode.HardwareTracker});

			scene = new Scene();
			connected = false;
		}


		public bool Connect(IMoCapClient_ConnectionInfo connectionInfo)
		{
			connected = XRDevice.isPresent;

			if (connected)
			{
				// construct scene description
				scene.actors = new Actor[trackedDevices.Count];
				for (int idx = 0; idx < trackedDevices.Count; idx++)
				{
					Actor actor = new Actor(scene, trackedDevices[idx].ToString(), idx);
					actor.bones = new Bone[1];
					actor.bones[0] = new Bone(actor, "root", 0);
					scene.actors[idx] = actor;
				}
			}
			return connected;
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
			return "UnityXR";
		}


		public float GetFramerate()
		{
			return XRDevice.refreshRate;
		}


		public void SetPaused(bool pause)
		{
			// ignored
		}


		public void Update()
		{
			// frame number and timestamp
			scene.frameNumber = Time.frameCount;
			scene.timestamp   = Time.time;

			for (int idx = 0; idx < trackedDevices.Count; idx++)
			{
				XRNode device = trackedDevices[idx];
				// update position, orientation, and tracking state
				Bone bone = scene.actors[idx].bones[0];
				bone.CopyTransform(InputTracking.GetLocalPosition(device), InputTracking.GetLocalRotation(device));
				bone.tracked = true;
			}
			manager.NotifyListeners_Update(scene);
		}


		public Scene GetScene()
		{
			return scene;
		}


		private readonly MoCapManager manager;

		private bool         connected;
		private Scene        scene;
		private List<XRNode> trackedDevices;
	}

}
