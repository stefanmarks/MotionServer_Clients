#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Class for treating a MoCap input device similar to key strokes of the Input class,
	/// e.g., GetButton(), GetButtonDown()
	/// </summary>
	///
	public class InputDeviceHandler : SceneListener
	{
		/// <summary>
		/// Threshold beyond which to consider a continuous axis value "pressed".
		/// Default: 1.0
		/// </summary>
		/// 
		public float PressThreshold { get; set; }


		/// <summary>
		/// Creates a new input device handler instance for a specific device.
		/// </summary>
		/// <param name="deviceName">the device to attach to</param>
		/// <param name="channelName">the channel to attach to</param>
		/// 
		public InputDeviceHandler(string deviceName, string channelName)
		{
			this.deviceName  = deviceName;
			this.channelName = channelName;

			value    = 0;
			oldValue = 0;

			PressThreshold = 1.0f;

			MoCapManager.GetInstance().AddSceneListener(this);
		}


		/// <summary>
		/// Checks if the button is currently pressed/down.
		/// </summary>
		/// <returns><c>true</c> when the button is pressed/down</returns>
		///
		public bool GetButton()
		{
			return (value >= PressThreshold);
		}


		/// <summary>
		/// Checks if the button has been pressed at least once since the last update.
		/// </summary>
		/// <returns><c>true</c> when the button has been pressed</returns>
		///
		public bool GetButtonDown()
		{
			return pressed;
		}


		/// <summary>
		/// Checks if the button has been released at least once since the last update.
		/// </summary>
		/// <returns><c>true</c> when the button has been released</returns>
		///
		public bool GetButtonUp()
		{
			return released;
		}


		/// <summary>
		/// Gets the raw value of the channel.
		/// </summary>
		/// <returns>raw channel/axis value</returns>
		///
		public float GetAxis()
		{
			return value;
		}


		public void SceneChanged(Scene scene)
		{
			// scene description has changed > search for channel again
			oldValue = 0;
			pressed  = false;
			released = false;

			channel = null;
			Device device = scene.FindDevice(deviceName);
			if (device != null)
			{
				channel = device.FindChannel(channelName);
			}
			if (channel != null)
			{
				Debug.Log("Handler registered for input device '" + deviceName + "', channel '" + channelName + "'");
			}
			else
			{
				Debug.LogWarning("Could not register handler for input device '" + deviceName + "', channel '" + channelName + "'");
			}
		}


		public void Process()
		{
			released = false;
			pressed  = false;
			if (value != oldValue)
			{
				// changed value: set booleans accordingly
				if ((oldValue >= PressThreshold) && (value < PressThreshold)) { released = true; }
				if ((oldValue < PressThreshold) && (value >= PressThreshold)) { pressed = true; }

				oldValue = value;
			}
		}


		public void SceneUpdated(Scene scene)
		{
			// store values and flags for queries
			if (channel != null)
			{
				value = channel.value;
			}
		}


		public override string ToString()
		{
			return "MoCap.InputDeviceHandler(" +
				deviceName + "/" + channelName + ", " +
				((channel != null) ? "active" : "not found") +
				")";
		}


		private string  deviceName, channelName;
		private Channel channel;
		private float   value, oldValue;
		private bool    pressed, released;
	}
}
