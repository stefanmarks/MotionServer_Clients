using System;
using System.Collections.Generic;
using UnityEngine;

namespace VR.Input
{
	/// <summary>
	/// Class for managing actions via a variety of sources, e.g., keypress, mouse click, etc.
	/// </summary>
	///
	public class ActionHandler
	{
		public readonly string Name;


		/// <summary>
		/// Shortcut to find an action handler through the InputManager singelton.
		/// </summary>
		/// <param name="actionName">the action handler to search for</param>
		/// <returns>the action handler</returns>
		///
		public static ActionHandler Find(String actionName)
		{
			return InputManager.GetActionHandler(actionName);
		}


		/// <summary>
		/// Creates a new action handler instance.
		/// </summary>
		/// 
		public ActionHandler(string name)
		{
			Name    = name;
			devices = new List<IDevice>();
		}


		public bool AddMapping(InputManager.InputType type, string inputName)
		{
			IDevice device = null;
			// create specific device subtype
			switch (type)
			{
				case InputManager.InputType.Keyboard:    device = new Device_Keyboard(); break;
				case InputManager.InputType.MoCapDevice: device = new Device_MoCap(); break;
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
		/// Checks if the action is currently active, e.g., a button is down.
		/// </summary>
		/// <returns><c>true</c> when the action is active</returns>
		///
		public bool IsActive()
		{
			bool returnValue = false;
			foreach (IDevice d in devices)
			{
				returnValue |= (d.GetValue() > 0);
			}
			return returnValue;
		}


		/// <summary>
		/// Checks if the action has just been activated, e.g., a button has been pressed.
		/// </summary>
		/// <returns><c>true</c> when the action has been activated</returns>
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
		/// Checks if the action has been deactivated, e.g., a button has been released.
		/// </summary>
		/// <returns><c>true</c> when the action has been deactivated</returns>
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
		/// Gets the raw "value" of the action.
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


		/// <summary>
		/// Interface for a generic input device.
		/// </summary>
		///
		private interface IDevice
		{
			bool  Initialise(string inputName);
			bool  IsActivated();
			bool  IsDeactivated();
			float GetValue();
		}

		/// <summary>
		/// Class for a keyboard button input device.
		/// It can be a singel key, or a +/- combination of two keys, e.g., for zoom.
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
						keyName1  = parts[0];
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
						        + (UnityEngine.Input.GetKey(keyName2) ?  1 : 0);
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

			private string  keyName1, keyName2;
			private Mode    mode;
		}


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
				if (parts.Length >= 2)
				{
					device = new MoCap.InputDeviceHandler(parts[0], parts[1]);
				}
				return true;
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

			public override string ToString()
			{
				return device.ToString();
			}

			private MoCap.InputDeviceHandler device;
		}
	}
}
