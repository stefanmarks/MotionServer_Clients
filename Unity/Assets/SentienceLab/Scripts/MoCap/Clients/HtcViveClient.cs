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
			public TrackedDevice(string _name)
			{
				this.name          = _name;
				this.controllerIdx = -1;
				this.deviceClass   = ETrackedDeviceClass.Invalid;
				this.device        = null;
			}

			public readonly string     name;
			public int                 controllerIdx;
			public ETrackedDeviceClass deviceClass;
			public Device              device;
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

				// find HMDs, trackers and controllers
				trackedDevices.Clear();
				int controllerCount = 0;
				int trackerCount    = 0;
				int hmdCount        = 0;
				int inputDeviceIdx  = 0;
				for (int index = 0; index < OpenVR.k_unMaxTrackedDeviceCount; index++)
				{
					ETrackedDeviceClass deviceClass = system.GetTrackedDeviceClass((uint)index);
					TrackedDevice trackedDevice = null;
					if (deviceClass == ETrackedDeviceClass.Controller)
					{
						controllerCount++;
						trackedDevice = new TrackedDevice("ViveController" + controllerCount);
						
						// controller has 12 input channels
						Device dev = new Device(scene, trackedDevice.name, inputDeviceIdx);
						dev.channels = new Channel[12];
						dev.channels[0]  = new Channel(dev, "button1");  // fire
						dev.channels[1]  = new Channel(dev, "button2");  // menu
						dev.channels[2]  = new Channel(dev, "button3");  // grip
						dev.channels[3]  = new Channel(dev, "button4");  // touchpad press
						dev.channels[4]  = new Channel(dev, "axis1");    // touchpad + press
						dev.channels[5]  = new Channel(dev, "axis2");
						dev.channels[6]  = new Channel(dev, "axis1raw"); // touchpad touch
						dev.channels[7]  = new Channel(dev, "axis2raw");
						dev.channels[8]  = new Channel(dev, "right");    // touchpad as buttons
						dev.channels[9]  = new Channel(dev, "left");
						dev.channels[10] = new Channel(dev, "up");
						dev.channels[11] = new Channel(dev, "down");
						trackedDevice.device = dev;
					}
					else if (deviceClass == ETrackedDeviceClass.GenericTracker)
					{
						trackerCount++;
						trackedDevice = new TrackedDevice("ViveTracker" + trackerCount);

						// tracker has 4 input channels
						Device dev = new Device(scene, trackedDevice.name, inputDeviceIdx);
						dev.channels = new Channel[4];
						dev.channels[0] = new Channel(dev, "button1"); // pin 3: grip
						dev.channels[1] = new Channel(dev, "button2"); // pin 4: trigger
						dev.channels[2] = new Channel(dev, "button3"); // pin 5: touchpad press
						dev.channels[3] = new Channel(dev, "button4"); // pin 6: menu
						trackedDevice.device = dev; 
					}
					else if (deviceClass == ETrackedDeviceClass.HMD)
					{
						hmdCount++;
						trackedDevice = new TrackedDevice("ViveHMD" + hmdCount);
					}

					if (trackedDevice != null)
					{
						trackedDevice.controllerIdx = index;
						trackedDevice.deviceClass   = deviceClass;
						trackedDevices.Add(trackedDevice);

						if (trackedDevice.device != null)
						{
							inputDeviceIdx++;
						}
					}
				}

				// construct scene description
				scene.actors  = new Actor[trackedDevices.Count];
				scene.devices = new Device[inputDeviceIdx];
				int deviceIndex = 0;
				for (int idx = 0; idx < trackedDevices.Count; idx++)
				{
					Actor actor = new Actor(scene, trackedDevices[idx].name, idx);
					actor.bones = new Bone[1];
					actor.bones[0] = new Bone(actor, "root", 0);
					scene.actors[idx] = actor;

					// is this an input device, too?
					if (trackedDevices[idx].device != null)
					{
						scene.devices[deviceIndex] = trackedDevices[idx].device;
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
				bone.CopyFrom(MathUtil.GetTranslation(m), MathUtil.GetRotation(m));

				bone.tracked = poses[controllerIdx].bDeviceIsConnected && poses[controllerIdx].bPoseIsValid;

				// if this is also an input device, update inputs 
				Device dev = trackedDevices[idx].device;
				if (dev != null)
				{
					system.GetControllerStateWithPose(
						ETrackingUniverseOrigin.TrackingUniverseStanding,
						(uint)controllerIdx,
						ref state, (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VRControllerState_t)),
						ref poses[controllerIdx]);

					if (trackedDevices[idx].deviceClass == ETrackedDeviceClass.Controller)
					{
						// hand controller
						// trigger button
						dev.channels[0].value = state.rAxis1.x;
						// menu button
						dev.channels[1].value = (state.ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_ApplicationMenu)) != 0 ? 1 : 0;
						// grip button
						dev.channels[2].value = (state.ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_Grip)) != 0 ? 1 : 0;
						// touchpad (button4, axis1/2 and axis1raw/2raw)
						float touchpadPressed = (state.ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_SteamVR_Touchpad)) != 0 ? 1 : 0;
						dev.channels[3].value = touchpadPressed;
						dev.channels[4].value = state.rAxis0.x * touchpadPressed;
						dev.channels[5].value = state.rAxis0.y * touchpadPressed;
						dev.channels[6].value = state.rAxis0.x;
						dev.channels[7].value = state.rAxis0.y;

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
						dev.channels[8].value  = ((angle >   0 - 67.5f) && (angle <    0 + 67.5f)) ? touchpadPressed : 0; // right
						dev.channels[9].value  = ((angle > 180 - 67.5f) || (angle < -180 + 67.5f)) ? touchpadPressed : 0; // left
						dev.channels[10].value = ((angle >  90 - 67.5f) && (angle <   90 + 67.5f)) ? touchpadPressed : 0; // up
						dev.channels[11].value = ((angle > -90 - 67.5f) && (angle <  -90 + 67.5f)) ? touchpadPressed : 0; // down
					}
					else if (trackedDevices[idx].deviceClass == ETrackedDeviceClass.GenericTracker)
					{
						// generic tracker
						// grip button
						dev.channels[0].value = (state.ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_Grip)) != 0 ? 1 : 0;
						// trigger button
						dev.channels[1].value = (state.ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_SteamVR_Trigger)) != 0 ? 1 : 0;
						// touchpad 
						dev.channels[2].value = (state.ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_SteamVR_Touchpad)) != 0 ? 1 : 0;
						// menu button
						dev.channels[3].value = (state.ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_ApplicationMenu)) != 0 ? 1 : 0;
						Debug.Log(dev.name + state.rAxis0.x + "=" + dev.channels[0].value + "/"+ dev.channels[1].value);
					}
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
