using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using Valve.VR;

namespace MoCap
{
	/// <summary>
	/// Class for a MoCap client that uses tracking data from an HTC Vive.
	/// </summary>
	/// 
	class HtcViveClient : IMoCapClient
	{
		/// <summary>
		/// Constructs a MoCap client that tracks HTC Vive HMDs and controllers.
		/// </summary>
		///
		public HtcViveClient()
		{
			sceneListeners  = new List<SceneListener>();
			scene           = new Scene();
			connected       = false;
		}


		public bool Connect(IMoCapClient_ConnectionInfo connectionInfo)
		{
			connected = VRDevice.isPresent;

			if (connected)
			{
				compositor = OpenVR.Compositor;
				connected &= compositor != null;
			}

			if ( connected )
			{
				poses     = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
				gamePoses = new TrackedDevicePose_t[0];

				controllers   = new SteamVR_Controller.Device[2];
				scene.actors  = new Actor[controllers.Length];
				scene.devices = new Device[controllers.Length];

				int firstControllerIdx = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.First);
				for (int idx = 0; idx < controllers.Length; idx++)
				{
					controllers[idx] = SteamVR_Controller.Input(idx + firstControllerIdx);

					string name = "Controller" + (idx + 1);

					Actor actor        = new Actor(scene, name, idx);
					actor.bones        = new Bone[1];
					actor.bones[0]     = new Bone(actor, "root", 0);
					scene.actors[idx]  = actor;

					Device device      = new Device(scene, name, idx);
					device.channels    = new Channel[5];
					device.channels[0] = new Channel(device, "trigger");
					device.channels[1] = new Channel(device, "menu");
					device.channels[2] = new Channel(device, "grip");
					device.channels[3] = new Channel(device, "axis1");
					device.channels[4] = new Channel(device, "axis2");

					scene.devices[idx] = device;
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
			sceneListeners.Clear();
		}
		
		
		public String GetDataSourceName()
		{
			return "HTC Vive";
		}


		public void Update()
		{
			compositor.GetLastPoses(poses, gamePoses);
			SteamVR_Controller.Update();
			for (int idx = 0; idx < controllers.Length; idx++)
			{
				// update position and orientation
				SteamVR_Controller.Device controller = controllers[idx];
				Bone bone = scene.actors[idx].bones[0];
				bone.tracked = poses[controller.index].bPoseIsValid;
				HmdMatrix34_t pose = poses[controller.index].mDeviceToAbsoluteTracking;
				Matrix4x4     m    = Matrix4x4.identity;
				m[0, 0] =  pose.m0;
				m[0, 1] =  pose.m1;
				m[0, 2] = -pose.m2;
				m[0, 3] =  pose.m3;

				m[1, 0] =  pose.m4;
				m[1, 1] =  pose.m5;
				m[1, 2] = -pose.m6;
				m[1, 3] =  pose.m7;

				m[2, 0] = -pose.m8;
				m[2, 1] = -pose.m9;
				m[2, 2] =  pose.m10;
				m[2, 3] = -pose.m11;

				bone.CopyTransform(m.GetPosition(), m.GetRotation());

				// update inputs
				Device device = scene.devices[idx];
				// trigger button
				device.channels[0].value = controller.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger).magnitude;
				// menu button
				device.channels[1].value = controller.GetAxis(Valve.VR.EVRButtonId.k_EButton_ApplicationMenu).magnitude;
				// grip button
				device.channels[2].value = controller.GetAxis(Valve.VR.EVRButtonId.k_EButton_Grip).magnitude;
				// touchpad (axis1/2)
				device.channels[3].value = controller.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad).x;
				device.channels[4].value = controller.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad).y;
			}
			NotifyListeners_Update();
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


		private bool                        connected;
		private Scene                       scene;
		private List<SceneListener>         sceneListeners;
		private SteamVR_Controller.Device[] controllers;
		private CVRCompositor               compositor;
		private TrackedDevicePose_t[]       poses, gamePoses;
	}

}
