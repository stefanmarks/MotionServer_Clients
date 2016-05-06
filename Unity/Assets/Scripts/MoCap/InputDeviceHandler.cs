using UnityEngine;

namespace MoCap
{
	/// <summary>
	/// Class for treating a MoCap input device similar to key strokes of the Input class,
	/// e.g., GetButton(), GetButtonDown
	/// </summary>
	///
	public class InputDeviceHandler : DeviceListener
	{
		/// <summary>
		/// Creates a new input device handler instance for a specific device.
		/// </summary>
		/// <param name="deviceName">the device to attach to</param>
		/// <param name="channelName">the channel to attach to</param>
		/// <param name="inputManagerProxy">name of the channel from the Unity input manager to simulate the device (optional)</param>
		/// 
		public InputDeviceHandler(string deviceName, string channelName, string inputManagerProxy = "")
		{
			this.deviceName  = deviceName;
			this.channelName = channelName;
			this.proxyName   = inputManagerProxy;

			MoCapClient.GetInstance().AddDeviceListener(this);
		}


		/// <summary>
		/// Checks if the button is currently pressed/down.
		/// </summary>
		/// <returns><c>true</c> when the button is pressed/down</returns>
		///
		public bool GetButton()
		{
			bool returnValue = (value > 0);
			if (proxyName.Length > 0)
			{
				returnValue |= Input.GetButton(proxyName);
			}
			return returnValue;
		}


		/// <summary>
		/// Checks if the button has been pressed at least once since the last check.
		/// This method resets the "down" flag.
		/// </summary>
		/// <returns><c>true</c> when the button has been pressed</returns>
		///
		public bool GetButtonDown()
		{
			bool returnValue = pressed;
			if (proxyName.Length > 0)
			{
				returnValue |= Input.GetButtonDown(proxyName);
			}
			pressed = false; // reset flag
			return returnValue;
		}


		/// <summary>
		/// Checks if the button has been released at least once since the last check.
		/// This method resets the "up" flag.
		/// </summary>
		/// <returns><c>true</c> when the button has been released</returns>
		///
		public bool GetButtonUp()
		{
			bool returnValue = released;
			if (proxyName.Length > 0)
			{
				returnValue |= Input.GetButtonUp(proxyName);
			}
			released = false; // reset flag
			return returnValue;
		}


		/// <summary>
		/// Gets the raw value of the channel.
		/// </summary>
		/// <returns>raw channel/axis value</returns>
		///
		public float GetAxis()
		{
			float returnValue = value;
			if (proxyName.Length > 0)
			{
				returnValue += Input.GetAxis(proxyName);
			}
			return returnValue;
		}


		public string GetDeviceName()
		{
			return deviceName;
		}


		public void DeviceChanged(Device device)
		{
			// scene description has changed > search for channel again
			oldValue = 0;
			pressed = false;
			released = false;

			channel = null;
			if (device != null)
			{
				channel = device.FindChannel(channelName);
			}
			if (channel != null)
			{
				Debug.Log("Handler registered for input device '" + deviceName + "' channel '" + channelName + "'");
			}
			else
			{
				Debug.LogWarning("Could not register handler for input device '" + deviceName + "' channel '" + channelName + "'");
			}
		}


		public void DeviceUpdated(Device device)
		{
			// store value for queries
			if (channel != null)
			{
				value = channel.value;
				if (value != oldValue)
				{
					// changed value: set booleans accordingly
					if (value <= 0) { released = true; }
					if (value > 0) { pressed = true; }

					oldValue = value;
				}
			}
		}


		private string  deviceName, channelName, proxyName;
		private Channel channel;
		private float   value, oldValue;
		private bool    pressed, released;
	}
}
