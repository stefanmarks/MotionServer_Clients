#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SentienceLab.Input
{
	/// <summary>
	/// Class for managing inputs via a variety of sources, e.g., keypress, mouse click, etc.
	/// </summary>
	///
	public class InputHandler
	{
		public readonly string Name;


		/// <summary>
		/// Shortcut to find an input handler through the InputManager singelton.
		/// </summary>
		/// <param name="inputName">the input handler to search for</param>
		/// <returns>the input handler</returns>
		///
		public static InputHandler Find(String inputName)
		{
			return InputManager.GetInputHandler(inputName);
		}


		/// <summary>
		/// Creates a new input handler instance.
		/// </summary>
		/// 
		public InputHandler(string name)
		{
			Name = name;
			devices = new List<IDevice>();

			// by default, check for existing Unity Input axis names
			try
			{
				UnityEngine.Input.GetAxis(name);
				AddMapping(InputManager.InputType.UnityInput, name);
			}
			catch (Exception)
			{
				// axis doesn't exist > don't bother
			}

			// set default threshold
			SetPressThreshold(1.0f);
		}


		/// <summary>
		/// Adds an input handler.
		/// </summary>
		/// <param name="type">the type of input handler</param>
		/// <param name="inputName">the name of the input handler</param>
		/// <returns><c>true</c>if the handler was added</returns>
		/// 
		public bool AddMapping(InputManager.InputType type, string inputName)
		{
			IDevice device = null;
			// create specific device subtype
			switch (type)
			{
				case InputManager.InputType.UnityInput:  device = new Device_UnityInput(); break;
				case InputManager.InputType.Keyboard:    device = new Device_Keyboard(); break;
#if !NO_MOCAP_INPUT 
				case InputManager.InputType.MoCapDevice: device = new Device_MoCap(); break;
#endif
				default:
					{
						Debug.LogWarning("Input Type " + type.ToString() + " not supported.");
						break;
					}
			}
			// try to initialise given the parameter string
			if ((device != null) && device.Initialise(inputName))
			{
				// success
				devices.Add(device);
			}
			else
			{
				// failed
				device = null;
			}
			return (device != null);
		}


		/// <summary>
		/// Processes all input devices.
		/// </summary>
		/// 
		public void Process()
		{
			foreach (IDevice d in devices)
			{
				d.Process();
			}
		}


		/// <summary>
		/// Checks if the input is currently active, e.g., a button is down.
		/// </summary>
		/// <returns><c>true</c> when the input is active</returns>
		///
		public bool IsActive()
		{
			bool returnValue = false;
			foreach (IDevice d in devices)
			{
				returnValue |= d.IsActive();
			}
			return returnValue;
		}


		/// <summary>
		/// Checks if the input has just been activated, e.g., a button has been pressed.
		/// </summary>
		/// <returns><c>true</c> when the input has been activated</returns>
		///
		public bool IsActivated()
		{
			bool isActivated = false;
			foreach (IDevice d in devices)
			{
				isActivated |= d.IsActivated();
			}
			return isActivated;
		}


		/// <summary>
		/// Checks if the input has been deactivated, e.g., a button has been released.
		/// </summary>
		/// <returns><c>true</c> when the input has been deactivated</returns>
		///
		public bool IsDeactivated()
		{
			bool isDeactivated = false;
			foreach (IDevice d in devices)
			{
				isDeactivated |= d.IsDeactivated();
			}
			return isDeactivated;
		}


		/// <summary>
		/// Gets the raw "value" of the input.
		/// </summary>
		/// <returns>raw channel/axis/button value</returns>
		///
		public float GetValue()
		{
			float returnValue = 0.0f;
			foreach (IDevice d in devices)
			{
				returnValue += d.GetValue();
			}
			return returnValue;
		}


		public float GetPressThreshold()
		{
			return pressThreshold;
		}


		public void SetPressThreshold(float value)
		{
			pressThreshold = value;
			foreach (IDevice device in devices)
			{
				device.SetPressThreshold(pressThreshold);
			}
		}


		public override string ToString()
		{
			string txtDevices = "";
			foreach (IDevice d in devices)
			{
				txtDevices += (txtDevices.Length > 0) ? ", " : "";
				txtDevices += d.ToString();
			}
			return "'" + Name + "' (Devices: " + txtDevices + ")";
		}


		private List<IDevice> devices;
		private float         pressThreshold;


		/// <summary>
		/// Interface for a generic input device.
		/// </summary>
		///
		private interface IDevice
		{
			bool  Initialise(string inputName);
			void  Process();
			bool  IsActive();
			bool  IsActivated();
			bool  IsDeactivated();
			float GetValue();
			void  SetPressThreshold(float value);
		}

		/// <summary>
		/// Class for a Unity input axis device.
		/// </summary>
		/// 
		private class Device_UnityInput : IDevice
		{
			public Device_UnityInput()
			{
				axisName = "";
				oldValue = 0;
			}

			public bool Initialise(string inputName)
			{
				bool success = false;
				try
				{
					UnityEngine.Input.GetAxis(inputName);
					success = true;
					axisName = inputName;
					// get first value
					Process();
					oldValue = value;
				}
				catch (Exception e)
				{
					// the Input.GetAxis call didn't succeed
					// -> the name of the key must have been wrong
					Debug.LogWarning(e.Message);
				}
				return success;
			}

			public void Process()
			{
				oldValue = value;
				value    = UnityEngine.Input.GetAxis(axisName);
			}

			public bool IsActive()
			{
				return (value >= pressThreshold);
			}

			public bool IsActivated()
			{
				return (oldValue < pressThreshold) && (value >= pressThreshold);
			}

			public bool IsDeactivated()
			{
				return (oldValue >= pressThreshold) && (value < pressThreshold);
			}

			public float GetValue()
			{
				return value;
			}

			public void SetPressThreshold(float value)
			{
				pressThreshold = value;
			}

			public override string ToString()
			{
				return "Axis '" + axisName + "'";
			}

			private string axisName;
			private float  value, oldValue;
			private float  pressThreshold;
		}


		/// <summary>
		/// Class for a keyboard button input device.
		/// It can be a single key, or a +/- combination of two keys, e.g., for zoom.
		/// </summary>
		/// 
		private class Device_Keyboard : IDevice
		{
			enum Mode
			{
				SingleKey,
				PlusMinus,
				Combination
			}

			public Device_Keyboard()
			{
				keyName1 = "";
				keyName2 = "";
			}

			public bool Initialise(string inputName)
			{
				bool success = false;
				try
				{
					if ((inputName.Length > 1) && inputName.Contains("/"))
					{
						// input name for a plus/minus combination (with a '/' separating them)
						string[] parts = inputName.Split('/');

						UnityEngine.Input.GetKey(parts[0]);
						keyName1 = parts[0];
						UnityEngine.Input.GetKey(parts[1]);
						keyName2 = parts[1];

						mode = Mode.PlusMinus;
					}
					else if ((inputName.Length > 1) && inputName.Contains("+"))
					{
						// input name for a combo name (with a '+' separating them)
						string[] parts = inputName.Split('+');

						UnityEngine.Input.GetKey(parts[0]);
						keyName1 = parts[0];
						UnityEngine.Input.GetKey(parts[1]);
						keyName2 = parts[1];

						mode = Mode.Combination;
					}
					else
					{
						// only a single key
						UnityEngine.Input.GetKey(inputName);
						keyName1 = inputName;
						mode = Mode.SingleKey;
					}

					success = true;
				}
				catch (Exception e)
				{
					// one of the Input.GetKey calls didn't succeed
					// -> the name of the key must have been wrong
					Debug.LogWarning(e.Message);
				}
				return success;
			}

			public void Process()
			{
				// nothing to do
			}

			public bool IsActive()
			{
				switch (mode)
				{
					case Mode.PlusMinus:
						return UnityEngine.Input.GetKey(keyName1) ||
							   UnityEngine.Input.GetKey(keyName2);

					case Mode.Combination:
						return (UnityEngine.Input.GetKey(keyName1) &&
								UnityEngine.Input.GetKey(keyName2));

					default:
						return UnityEngine.Input.GetKey(keyName1);
				}
			}

			public bool IsActivated()
			{
				switch (mode)
				{
					case Mode.PlusMinus:
						return UnityEngine.Input.GetKeyDown(keyName1) ||
							   UnityEngine.Input.GetKeyDown(keyName2);

					case Mode.Combination:
						return (UnityEngine.Input.GetKeyDown(keyName1) &&
								UnityEngine.Input.GetKey(keyName2)) ||
							   (UnityEngine.Input.GetKey(keyName1) &&
								UnityEngine.Input.GetKeyDown(keyName2));

					default:
						return UnityEngine.Input.GetKeyDown(keyName1);
				}
			}

			public bool IsDeactivated()
			{
				switch (mode)
				{
					case Mode.PlusMinus:
						return UnityEngine.Input.GetKeyUp(keyName1) ||
							   UnityEngine.Input.GetKeyUp(keyName2);

					case Mode.Combination:
						return (UnityEngine.Input.GetKeyUp(keyName1) &&
								UnityEngine.Input.GetKey(keyName2)) ||
							   (UnityEngine.Input.GetKey(keyName1) &&
								UnityEngine.Input.GetKeyUp(keyName2));

					default:
						return UnityEngine.Input.GetKeyUp(keyName1);
				}
			}

			public float GetValue()
			{
				float value;
				switch (mode)
				{
					case Mode.PlusMinus:
						value =   (UnityEngine.Input.GetKey(keyName1) ? -1 : 0)
								+ (UnityEngine.Input.GetKey(keyName2) ? 1 : 0);
						break;

					case Mode.Combination:
						value = (UnityEngine.Input.GetKey(keyName1) &&
								 UnityEngine.Input.GetKey(keyName2)) ? 1 : 0;
						break;

					default:
						value = UnityEngine.Input.GetKey(keyName1) ? 1 : 0;
						break;
				}
				return value;
			}

			public void SetPressThreshold(float value)
			{
				// doesn't apply to keys
			}

			public override string ToString()
			{
				switch (mode)
				{
					case Mode.PlusMinus:
						return "Keys '" + keyName1 + "'/'" + keyName2 + "'";

					case Mode.Combination:
						return "Keys '" + keyName1 + "'+'" + keyName2 + "'";

					default:
						return "Key '" + keyName1 + "'";
				}
			}

			private string keyName1, keyName2;
			private Mode mode;
		}


#if !NO_MOCAP_INPUT
		/// <summary>
		/// Class for a MoCap input device.
		/// </summary>
		///
		private class Device_MoCap : IDevice
		{
			public Device_MoCap()
			{
				device = null;
			}

			public bool Initialise(string inputName)
			{
				string[] parts = inputName.Split('/');
				if ((parts.Length >= 2) && MoCap.MoCapManager.GetInstance() != null)
				{
					device = new MoCap.InputDeviceHandler(parts[0], parts[1]);
				}
				return device != null;
			}

			public void Process()
			{
				device.Process();
			}

			public bool IsActive()
			{
				return device.GetButton();
			}

			public bool IsActivated()
			{
				return device.GetButtonDown();
			}

			public bool IsDeactivated()
			{
				return device.GetButtonUp();
			}

			public float GetValue()
			{
				return device.GetAxis();
			}

			public void SetPressThreshold(float value)
			{
				device.PressThreshold = value;
			}

			public override string ToString()
			{
				return device.ToString();
			}

			private MoCap.InputDeviceHandler device;
		}
#endif

	}
}
