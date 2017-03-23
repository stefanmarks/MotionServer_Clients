#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using SentienceLab.Input;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Component for a Motion Capture manager.
	/// One instance of this object is needed that all MoCap controlled objects get their data from.
	/// Make sure the MoCapManager script is executed before any other script in "Project Settings/Script Execution Order".
	/// </summary>

	[DisallowMultipleComponent]
	[AddComponentMenu("Motion Capture/MoCap Manager")]

	public class MoCapManager : MonoBehaviour
	{
		[Tooltip("Name of the file with MotionServer IP Addresses or .MOT files to query")]
		public TextAsset dataSourceFile = null;
		
		[Tooltip("Action name for pausing/running the client")]
		public string pauseAction = "pause";

		[Tooltip("Name of the MoCap Client (\"$scene\" will be replaced by the active scene name)")]
		public string clientName = "$scene";

		private byte[] clientAppVersion = new byte[] { 1, 2, 0, 0 };

		/// <summary>
		/// Called once at the start of the scene. 
		/// </summary>
		/// 
		public void Awake()
		{
			CreateManager(); // trigger creation of singleton (if not already happened)

			if (instance == null)
			{
				instance = this;
				Debug.Log("MoCap Manager instance created (" + client.GetDataSourceName() + ")");
			}
			else
			{
				// there can be only one instance
				GameObject.Destroy(this);
			}

			pauseHandler = InputHandler.Find(pauseAction);
			pauseClient  = false;
		}


		/// <summary>
		/// Called when object is about to be destroyed.
		/// Disconnects from the NatNet server and destroys the NatNet client.
		/// </summary>
		///
		public void OnDestroy()
		{
			clientMutex.WaitOne();
			if (client != null)
			{
				client.Disconnect();
				client = null;
			}
			clientMutex.ReleaseMutex();
		}


		/// <summary>
		/// Called when the application is paused or continued.
		/// </summary>
		/// <param name="pause"><c>true</c> when the application is paused</param>
		/// 
		public void OnApplicationPause(bool pause)
		{
			if (client != null)
			{
				client.SetPaused(pause);
			}
		}


		/// <summary>
		/// Called once per rendered frame. 
		/// Get new frame data now.
		/// Make sure the MoCapManager script is executed before any other script in "Project Settings/Script Execution Order".
		/// </summary>
		///
		public void Update()
		{
			if (client != null)
			{
				if (client.IsConnected())
			{
				client.Update();
			}
		}
			if ( (pauseHandler != null) && pauseHandler.IsActivated())
			{
				pauseClient = !pauseClient;
				OnApplicationPause(pauseClient);
			}
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
			return (client != null) ? client.GetDataSourceName() : "";
		}


		/// <summary>
		/// Gets the latest scene data structure.
		/// </summary>
		/// <returns>Scene data or <c>null</c> if client is not connected</returns>
		/// 
		public Scene GetScene()
		{
			return (client != null) ? client.GetScene() : null;
		}


		/// <summary>
		/// Adds a scene data listener.
		/// </summary>
		/// <param name="listener">The listener to add</param>
		/// <returns><c>true</c>, if the scene listener was added, <c>false</c> otherwise.</returns>
		/// 
		public bool AddSceneListener(SceneListener listener)
		{
			return (client != null) ? client.AddSceneListener(listener) : false;
		}


		/// <summary>
		/// Removes a scene data listener.
		/// </summary>
		/// <param name="listener">The listener to remove</param>
		/// <returns><c>true</c>, if the actor listener was removed, <c>false</c> otherwise.</returns>
		/// 
		public bool RemoveSceneListener(SceneListener listener)
		{
			return (client != null) ? client.RemoveSceneListener(listener) : true;
		}



		/// <summary>
		/// Structure for loading sources from a JSON file.
		/// </summary>
		///
		[System.Serializable]
		private class MoCapDataSourceList
		{
			public List<string> sources = new List<string>();
		}


		/// <summary>
		/// Gets the internal NatNet client instance singelton.
		/// When creating the singleton for the first time, 
		/// tries to connect to a local MoCap server, and if not successful, a remote MoCap server.
		/// </summary>
		/// 
		private void CreateManager()
		{
			clientMutex.WaitOne();
			if (client == null)
			{
				// only connect when this script is actually enabled
				if (this.isActiveAndEnabled)
				{
					// build list of data sources
					ICollection<IMoCapClient_ConnectionInfo> sources = GetSourceList();

					// run through the list
					foreach (IMoCapClient_ConnectionInfo info in sources)
					{
						// construct client according to structure (this is ugly...)
						if (info is NatNetClient.ConnectionInfo)
						{
							// is client already the right type?
							if (!(client is NatNetClient))
							{
								// construct client name
								string appName = clientName;
								appName = appName.Replace("$scene", SceneManager.GetActiveScene().name);
								client = new NatNetClient(appName, clientAppVersion);
							}
						}
						else if (info is FileClient.ConnectionInfo) 
						{
							// is client already the right type?
							if (!(client is FileClient))
							{
								client = new FileClient();
							}
						}

						if (client.Connect(info))
						{
							// connection established > that's it
							break;
						}
					}

					// no client yet > try VR
					if (!client.IsConnected() && UnityEngine.VR.VRDevice.isPresent)
					{
						client = new HtcViveClient();
						client.Connect(null);
					}

					if (client.IsConnected())
					{
						Debug.Log("MoCap client connected to " + client.GetDataSourceName() + ".");
					}
				}

				if ((client == null) || !client.IsConnected())
				{
					// not active or not able to connect to any data source: create dummy singleton 
					client = new DummyClient();
				}

			}
			clientMutex.ReleaseMutex();
		}


		/// <summary>
		/// Reads the MoCap data source file asset and constructs a list of the connection information.
		/// </summary>
		/// <returns>List of IP addresses to query</returns>
		/// 
		private ICollection<IMoCapClient_ConnectionInfo> GetSourceList()
		{
			LinkedList<IMoCapClient_ConnectionInfo> sources = new LinkedList<IMoCapClient_ConnectionInfo>();

			if ( dataSourceFile.text.StartsWith("MotionServer Data File") )
			{
				// the source is directly a .MOT file
				sources.AddLast(new FileClient.ConnectionInfo(dataSourceFile));
			}
			else
			{
				// read file
				MoCapDataSourceList sourceList = JsonUtility.FromJson<MoCapDataSourceList>(dataSourceFile.text);
				if (sourceList != null)
				{
					// construct sources list with connection data structures
					foreach (string source in sourceList.sources)
					{
						if (source.Contains("/"))
						{
							// slash can only be in a filename
							sources.AddLast(new FileClient.ConnectionInfo(source));
						}
						else
						{
							// or is it an IP address
							IPAddress address;
							if (IPAddress.TryParse(source.Trim(), out address))
							{
								// success > add to list
								sources.AddLast(new NatNetClient.ConnectionInfo(address));
							}
						}
					}
				}
			}
			return sources;
		}


		/// <summary>
		/// Searches for the MoCapManager instance in the scene and returns it
		/// or quits if it is not defined.
		/// </summary>
		/// <returns>the MoCapManager instance</returns>
		/// 
		public static MoCapManager GetInstance()
		{
			if (instance == null) Debug.LogWarning("Null");
			return instance;
		}


		private static MoCapManager instance    = null;
		private static IMoCapClient client      = null;
		private static Mutex        clientMutex = new Mutex();

		private InputHandler pauseHandler;
		private bool         pauseClient; 
	}

}
