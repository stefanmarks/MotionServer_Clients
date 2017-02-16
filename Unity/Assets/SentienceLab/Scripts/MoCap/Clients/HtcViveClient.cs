using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using Valve.VR;

namespace SentienceLab.MoCap
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
				system = OpenVR.System;
				if (system == null)
				{
					connected = false;
					Debug.LogWarning("Could not find OpenVR System instance.");
				}
				compositor = OpenVR.Compositor;
				if (compositor == null)
				{
					connected = false;
					Debug.LogWarning("Could not find OpenVR Compositor instance.");
				}
			}

			if (connected)
			{
				poses     = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
				gamePoses = new TrackedDevicePose_t[0];

				FindControllerIndices();
				scene.actors  = new Actor[controllerIndices.Length];
				scene.devices = new Device[controllerIndices.Length];
				states        = new VRControllerState_t[controllerIndices.Length];

				for (int idx = 0; idx < controllerIndices.Length; idx++)
				{
					string name = "Controller" + (idx + 1);

					Actor actor        = new Actor(scene, name, idx);
					actor.bones        = new Bone[1];
					actor.bones[0]     = new Bone(actor, "root", 0);
					scene.actors[idx]  = actor;

					Device device      = new Device(scene, name, idx);
					device.channels     = new Channel[11];
					device.channels[0] = new Channel(device, "button1");  // fire
					device.channels[1] = new Channel(device, "button2");  // menu
					device.channels[2] = new Channel(device, "button3");  // grip
					device.channels[3] = new Channel(device, "axis1");    // touchpad + press
					device.channels[4] = new Channel(device, "axis2");
					device.channels[5] = new Channel(device, "axis1raw"); // touchpad touch
					device.channels[6] = new Channel(device, "axis2raw");
					device.channels[7]  = new Channel(device, "right");    // touchpad as buttons
					device.channels[8]  = new Channel(device, "left");
					device.channels[9]  = new Channel(device, "up");
					device.channels[10] = new Channel(device, "down");

					scene.devices[idx] = device;
				}
			}
			return connected;
		}


		/// <summary>
		/// Finds all the OpenVR device indices for hand controllers.
		/// </summary>
		/// 
		private void FindControllerIndices()
		{
			List<int> indices = new List<int>();
			for (int index = 0; index < OpenVR.k_unMaxTrackedDeviceCount; index++)
			{
				if (system.GetTrackedDeviceClass((uint) index) == ETrackedDeviceClass.Controller)
				{
					indices.Add(index);
				}
				// limit to 2 controllers
				if (indices.Count == 2) break;
			}
			controllerIndices = indices.ToArray();
		}


		public bool IsConnected()
		{
			return connected;
		}


		public void Disconnect()
		{
			connected = false; 
			system     = null;
			compositor = null;
			sceneListeners.Clear();
		}
		
		
		public String GetDataSourceName()
		{
			return "HTC Vive";
		}


		public void SetPaused(bool pause)
		{
			// ignored
		}


		public void Update()
		{
			// TODO: is this necessary?
			compositor.GetLastPoses(poses, gamePoses);

			for (int idx = 0; idx < controllerIndices.Length; idx++)
			{
				// update position, orientation, and tracking state
				int controller = controllerIndices[idx];
				Bone bone = scene.actors[idx].bones[0];

				HmdMatrix34_t pose = poses[controller].mDeviceToAbsoluteTracking;
				Matrix4x4     m    = Matrix4x4.identity;
				m[0,0] = pose.m0; m[0,1] = pose.m1; m[0,2] = pose.m2;  m[0,3] = pose.m3;
				m[1,0] = pose.m4; m[1,1] = pose.m5; m[1,2] = pose.m6;  m[1,3] = pose.m7;
				m[2,0] = pose.m8; m[2,1] = pose.m9; m[2,2] = pose.m10; m[2,3] = pose.m11;
				MathUtil.ToggleLeftRightHandedMatrix(ref m);
				bone.CopyTransform(MathUtil.GetTranslation(m), MathUtil.GetRotation(m));

				bone.tracked = poses[controller].bDeviceIsConnected && poses[controller].bPoseIsValid;

				// update inputs
				system.GetControllerStateWithPose(
					ETrackingUniverseOrigin.TrackingUniverseStanding, 
					(uint) controller, ref states[idx], ref poses[idx]);
				Device device = scene.devices[idx];
				// trigger button
				device.channels[0].value = states[idx].rAxis1.x;
				// menu button
				device.channels[1].value = (states[idx].ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_ApplicationMenu)) != 0 ? 1 : 0;
				// grip button
				device.channels[2].value = (states[idx].ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_Grip)) != 0 ? 1 : 0;
				// touchpad (axis1/2 and axis1/2raw)
				float touchpadPressed = (states[idx].ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_SteamVR_Touchpad)) != 0 ? 1 : 0;
				device.channels[3].value = states[idx].rAxis0.x * touchpadPressed;
				device.channels[4].value = states[idx].rAxis0.y * touchpadPressed;
				device.channels[5].value = states[idx].rAxis0.x;
				device.channels[6].value = states[idx].rAxis0.y;
				// touchpad as buttons
				device.channels[7].value  = (states[idx].rAxis0.x > +0.5) ? touchpadPressed : 0;
				device.channels[8].value  = (states[idx].rAxis0.x < -0.5) ? touchpadPressed : 0;
                device.channels[9].value  = (states[idx].rAxis0.y > +0.5) ? touchpadPressed : 0;
                device.channels[10].value = (states[idx].rAxis0.y < -0.5) ? touchpadPressed : 0;
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
		private CVRCompositor               compositor;
		private CVRSystem              system;
		private int[]                  controllerIndices;
		private TrackedDevicePose_t[]       poses, gamePoses;
		private VRControllerState_t[]  states;
	}

}
