#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
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
		/// <param name="manager">the MoCapManager instance</param>
		///
		public HtcViveClient(MoCapManager manager)
		{
			this.manager = manager;

			scene          = new Scene();
			trackedDevices = new List<TrackedDevice>();
			connected      = false;
		}


		private class TrackedDevice
		{
			public string name;
			public int    controllerIdx;
			public int    deviceIdx;
		}


		public bool Connect(IMoCapClient_ConnectionInfo connectionInfo)
		{
			connected = XRDevice.isPresent;

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
				else
				{
					/* TODO: This doesn't work. Any other solution?
					Compositor_FrameTiming timing = new Compositor_FrameTiming();
					timing.m_nSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Compositor_FrameTiming));
					compositor.GetFrameTimings(ref timing, 1);
					updateRate = 1000.0f / timing.m_flClientFrameIntervalMs;
					*/
					updateRate = 90; // hardcoded for now...
				}
			}

			if (connected)
			{
				// allocate structures
				state = new VRControllerState_t();
				poses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
				gamePoses = new TrackedDevicePose_t[0];

				// find HMDs and controllers
				trackedDevices.Clear();
				int controllerCount = 0;
				int hmdCount        = 0;
				for (int index = 0; index < OpenVR.k_unMaxTrackedDeviceCount; index++)
				{
					ETrackedDeviceClass deviceClass = system.GetTrackedDeviceClass((uint)index);
					TrackedDevice device = null;
					if ( (deviceClass == ETrackedDeviceClass.Controller) &&
						 (controllerCount < 2) )// no more than 2 controllers
					{
						device = new TrackedDevice();
						device.controllerIdx = index;
						device.deviceIdx     = 0; // read input from this controller
						controllerCount++;
						device.name = "ViveController" + controllerCount;
					}
					else if (deviceClass == ETrackedDeviceClass.HMD)
					{
						device = new TrackedDevice();
						device.controllerIdx = index;
						device.deviceIdx     = -1; // no input from this tracked device
						hmdCount++;
						device.name = "ViveHMD" + hmdCount;
					}

					if ( device != null )
					{
						trackedDevices.Add(device);
					}
				}

				// construct scene description
				scene.actors  = new Actor[trackedDevices.Count];
				scene.devices = new Device[controllerCount];
				int deviceIndex = 0;
				for (int idx = 0; idx < trackedDevices.Count; idx++)
				{
					Actor actor = new Actor(scene, trackedDevices[idx].name, idx);
					actor.bones = new Bone[1];
					actor.bones[0] = new Bone(actor, "root", 0);
					scene.actors[idx] = actor;

					// is this an input device, too?
					if (trackedDevices[idx].deviceIdx == 0)
					{
						Device device   = new Device(scene, actor.name, deviceIndex);
						device.channels = new Channel[11];
						device.channels[0] = new Channel(device, "button1");  // fire
						device.channels[1] = new Channel(device, "button2");  // menu
						device.channels[2] = new Channel(device, "button3");  // grip
						device.channels[3] = new Channel(device, "axis1");    // touchpad + press
						device.channels[4] = new Channel(device, "axis2");
						device.channels[5] = new Channel(device, "axis1raw"); // touchpad touch
						device.channels[6] = new Channel(device, "axis2raw");
						device.channels[7] = new Channel(device, "right");    // touchpad as buttons
						device.channels[8] = new Channel(device, "left");
						device.channels[9] = new Channel(device, "up");
						device.channels[10] = new Channel(device, "down");

						scene.devices[deviceIndex]    = device;
						trackedDevices[idx].deviceIdx = deviceIndex;
						deviceIndex++;
					}
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
			system     = null;
			compositor = null;
		}
		
		
		public String GetDataSourceName()
		{
			return "HTC Vive";
		}


		public float GetFramerate()
		{
			return updateRate;
		}


		public void SetPaused(bool pause)
		{
			// ignored
		}


		public void Update()
		{
			compositor.GetLastPoses(poses, gamePoses);

			// frame number and timestamp
			scene.frameNumber = Time.frameCount;
			scene.timestamp   = Time.time;

			for (int idx = 0; idx < trackedDevices.Count; idx++)
			{
				// update position, orientation, and tracking state
				int controllerIdx = trackedDevices[idx].controllerIdx;
				Bone bone = scene.actors[idx].bones[0];

				HmdMatrix34_t pose = poses[controllerIdx].mDeviceToAbsoluteTracking;
				Matrix4x4     m    = Matrix4x4.identity;
				m[0,0] = pose.m0; m[0,1] = pose.m1; m[0,2] = pose.m2;  m[0,3] = pose.m3;
				m[1,0] = pose.m4; m[1,1] = pose.m5; m[1,2] = pose.m6;  m[1,3] = pose.m7;
				m[2,0] = pose.m8; m[2,1] = pose.m9; m[2,2] = pose.m10; m[2,3] = pose.m11;
				MathUtil.ToggleLeftRightHandedMatrix(ref m);
				bone.CopyFrom(MathUtil.GetTranslation(m));
				bone.CopyFrom(MathUtil.GetRotation(m));

				bone.tracked = poses[controllerIdx].bDeviceIsConnected && poses[controllerIdx].bPoseIsValid;

				// if this is a controller, update inputs as well
				int deviceIdx = trackedDevices[idx].deviceIdx;
				if (deviceIdx >= 0)
				{
					system.GetControllerStateWithPose(
						ETrackingUniverseOrigin.TrackingUniverseStanding,
						(uint)controllerIdx,
						ref state, (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VRControllerState_t)),
						ref poses[controllerIdx]);
					Device device = scene.devices[deviceIdx];
					// trigger button
					device.channels[0].value = state.rAxis1.x;
					// menu button
					device.channels[1].value = (state.ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_ApplicationMenu)) != 0 ? 1 : 0;
					// grip button
					device.channels[2].value = (state.ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_Grip)) != 0 ? 1 : 0;
					// touchpad (axis1/2 and axis1raw/2raw)
					float touchpadPressed = (state.ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_SteamVR_Touchpad)) != 0 ? 1 : 0;
					device.channels[3].value  = state.rAxis0.x * touchpadPressed;
					device.channels[4].value  = state.rAxis0.y * touchpadPressed;
					device.channels[5].value  = state.rAxis0.x;
					device.channels[6].value  = state.rAxis0.y;
					
					// touchpad as buttons
					Vector2 touchpad = new Vector2(state.rAxis0.x, state.rAxis0.y) * touchpadPressed;
					float distance = touchpad.magnitude;
					if (distance < 0.3f) touchpad = Vector2.zero;
					// using angle to determine which circular segment the finger is on
					// instead of purely <> comparisons on coordinates
					float angle = Mathf.Rad2Deg * Mathf.Atan2(touchpad.y, touchpad.x);
					//    +135  +90  +45
					// +180/-180       0   to allow for overlap, angles are 67.5 around a direction
					//    -135  -90  -45
					device.channels[7].value  = ((angle >   0 - 67.5f) && (angle <    0 + 67.5f)) ? touchpadPressed : 0; // right
					device.channels[8].value  = ((angle > 180 - 67.5f) || (angle < -180 + 67.5f)) ? touchpadPressed : 0; // left
					device.channels[9].value  = ((angle >  90 - 67.5f) && (angle <   90 + 67.5f)) ? touchpadPressed : 0; // up
					device.channels[10].value = ((angle > -90 - 67.5f) && (angle <  -90 + 67.5f)) ? touchpadPressed : 0; // down
				}
			}
			manager.NotifyListeners_Update(scene);
		}


		public Scene GetScene()
		{
			return scene;
		}


		private readonly MoCapManager manager;

		private bool                   connected;
		private Scene                  scene;
		private CVRCompositor          compositor;
		private CVRSystem              system;
		private List<TrackedDevice>    trackedDevices;
		private TrackedDevicePose_t[]  poses, gamePoses;
		private VRControllerState_t    state;
		private float                  updateRate;
	}

}
