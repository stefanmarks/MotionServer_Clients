using UnityEngine;
using UnityEngine.Assertions;
using System.Net;
using System.Threading;
using System.Collections.Generic;

namespace MoCap
{
	/// <summary>
	/// Component for a MoCap client.
	/// One instance of this object is needed that all moCap controlled objects get their data from.
	/// </summary>
	///

	[DisallowMultipleComponent]
	[AddComponentMenu("Motion Capture/MoCap Client")]

	public class MoCapClient : MonoBehaviour
	{
		[Tooltip("Name of the file with MotionServer IP Addresses to query")]
		public TextAsset serverAddressFile = null;

		[Tooltip("The name of this application")]
		public string clientAppName = "Unity MoCap Client";

		[Tooltip("Version number of this client")]
		public byte[] clientAppVersion = new byte[] { 1, 0, 8, 0 };


		/// <summary>
		/// Gets the internal NatNet client instance singelton.
		/// When creating the singleton for the first time, 
		/// tries to connect to a local MoCap server, and if not successful, a remote MoCap server.
		/// </summary>
		/// <returns>The NatNet singleton</returns>
		/// 
		private NatNetClient GetClient()
		{
			clientMutex.WaitOne();
			if (client == null)
			{
				// create singleton
				client = new NatNetClient(clientAppName, clientAppVersion);

				if (this.isActiveAndEnabled)
				{
					// build list of server addresses
					ICollection<IPAddress> serverAddresses = GetServerAddresses();

					// run through the list
					foreach (IPAddress address in serverAddresses)
					{
						if (client.Connect(address))
						{
							Debug.Log("MoCap client connected to MotionServer '" + client.GetServerName() + "' on " + address + ".");
							break;
						}
					}

					// nope, can't find it
					if (!client.IsConnected())
					{
						Debug.LogWarning("Could not connect to any MotionServer.");
					}
				}
			}
			clientMutex.ReleaseMutex();
			return client;
		}


		/// <summary>
		/// Called once at the start of the scene. 
		/// </summary>
		/// 
		public void Start()
		{
			GetClient(); // trigger creation of singleton (if not already happened)
		}


		/// <summary>
		/// Called when object is about to be destroyed.
		/// Disconnects from the NatNet server and destroys the NatNet client.
		/// </summary>
		/// 
		void OnDestroy()
		{
			if (client != null)
			{
				clientMutex.WaitOne();
				client.Disconnect();
				client = null;
				clientMutex.ReleaseMutex();
			}
		}


		/// <summary>
		/// Called once per physics engine frame and before Update().
		/// Tries to get new frame data.
		/// </summary>
		/// 
		public void FixedUpdate()
		{
			// ideally, we want the updated scene data before Update()
			if (readyForNextFrame && GetClient().IsConnected())
			{
				GetClient().Update();
			}
			readyForNextFrame = false;
		}


		/// <summary>
		/// Called once per rendered frame. 
		/// If FixedUpdate() somehow wasn't called, tries to get new frame data now.
		/// </summary>
		/// 
		public void Update()
		{
			// If we get the update here, it is not ideal, but better than nothing
			FixedUpdate();
		}


		public void LateUpdate()
		{
			// signal preparedness for next update
			readyForNextFrame = true;
		}


		/// <summary>
		/// Checks if the client is connected to the MotionServer.
		/// </summary>
		/// <returns><c>true</c> if the client is connected</returns>
		/// 
		public bool IsConnected()
		{
			return (client != null) && client.IsConnected();
		}


		/// <summary>
		/// Gets the name of the connected MotionServer.
		/// </summary>
		/// <returns>Name of the connected MotionServer</returns>
		/// 
		public string GetServerName()
		{
			return (client != null) ? client.GetServerName() : "";
		}


		/// <summary>
		/// Adds a scene data listener.
		/// </summary>
		/// <param name="listener">The listener to add</param>
		/// <returns><c>true</c>, if the scene listener was added, <c>false</c> otherwise.</returns>
		/// 
		public bool AddSceneListener(SceneListener listener)
		{
			return GetClient().AddSceneListener(listener);
		}


		/// <summary>
		/// Removes a scene data listener.
		/// </summary>
		/// <param name="listener">The listener to remove</param>
		/// <returns><c>true</c>, if the actor listener was removed, <c>false</c> otherwise.</returns>
		/// 
		public bool RemoveSceneListener(SceneListener listener)
		{
			return GetClient().RemoveSceneListener(listener);
		}


		/// <summary>
		/// Adds an actor data listener.
		/// </summary>
		/// <param name="listener">The listener to add</param>
		/// <returns><c>true</c>, if the actor listener was added, <c>false</c> otherwise.</returns>
		/// 
		public bool AddActorListener(ActorListener listener)
		{
			return GetClient().AddActorListener(listener);
		}


		/// <summary>
		/// Removes an actor data listener.
		/// </summary>
		/// <param name="listener">The listener to remove</param>
		/// <returns><c>true</c>, if the actor listener was removed, <c>false</c> otherwise.</returns>
		/// 
		public bool RemoveActorListener(ActorListener listener)
		{
			return GetClient().RemoveActorListener(listener);
		}


		/// <summary>
		/// Adds an interaction device data listener.
		/// </summary>
		/// <param name="listener">The listener to add</param>
		/// <returns><c>true</c>, if the actor listener was added, <c>false</c> otherwise.</returns>
		/// 
		public bool AddDeviceListener(DeviceListener listener)
		{
			return GetClient().AddDeviceListener(listener);
		}


		/// <summary>
		/// Removes an interaction device data listener.
		/// </summary>
		/// <param name="listener">The listener to remove</param>
		/// <returns><c>true</c>, if the actor listener was removed, <c>false</c> otherwise.</returns>
		/// 
		public bool RemoveDeviceListener(DeviceListener listener)
		{
			return GetClient().RemoveDeviceListener(listener);
		}




		[System.Serializable]
		private class ServerAddressList
		{
			public List<string> ServerAddresses = new List<string>();
		}

		/// <summary>
		/// Reads the MotionServer address file asset and constructs a list of IP addresses to query.
		/// </summary>
		/// <returns>List of IP addresses to query</returns>
		/// 
		private ICollection<IPAddress> GetServerAddresses()
		{
			LinkedList<IPAddress> addresses   = new LinkedList<IPAddress>();
			ServerAddressList     addressList = JsonUtility.FromJson<ServerAddressList>(serverAddressFile.text);

			foreach (string strAddress in addressList.ServerAddresses)
			{
				IPAddress address;
				if (IPAddress.TryParse(strAddress.Trim(), out address))
				{
					// success > add to list
					addresses.AddLast(address);
				}
			}

			// if localhost is not part of the list at all, add it at the beginning
			if (!addresses.Contains(IPAddress.Loopback))
			{
				addresses.AddFirst(IPAddress.Loopback);
			}

			return addresses;
		}


		/// <summary>
		/// Searches for the MoCapClient instance in the scene and returns it
		/// or quits if it is not defined.
		/// </summary>
		/// <returns>the MoCapClient instance</returns>
		/// 
		public static MoCapClient GetInstance()
		{
			// try to find the client instance 
			MoCapClient client = FindObjectOfType<MoCapClient>();
			Assert.IsNotNull(client, "No MoCapClient component defined in the scene.");
			return client;
		}


		private static NatNetClient client      = null;
		private static Mutex        clientMutex = new Mutex();
		private bool                readyForNextFrame;
	}

}
