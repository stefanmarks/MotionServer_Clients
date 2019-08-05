#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using SentienceLab.Input;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

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
		[System.Serializable]
		public class Configuration : ConfigFileBase
		{
			public Configuration() : base("MoCapConfig.txt", "MoCap") { }

			public List<string> Sources;
		}

		[ContextMenuItem("Load configuration from config file", "LoadConfiguration")]
		[ContextMenuItem("Save configuration to config file", "SaveConfiguration")]
		public Configuration configuration;

		[Tooltip("Action name for pausing/running the client")]
		public string pauseAction = "pause";

		[Tooltip("Name of the MoCap Client\n(The string \"{SCENE}\" will be replaced by the active scene name)")]
		public string clientName = "{SCENE}";


		private byte[] clientAppVersion = new byte[] { 1, 4, 2, 0 };


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

			lastUpdateFrame    = -1;
			lastPreRenderFrame = -1;
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

				sceneListeners.Clear();
			}
			clientMutex.ReleaseMutex();
			// MoCap Objects might have registered Coroutines here
			StopAllCoroutines();
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
		/// </summary>
		///
		public void Update()
		{
			if (lastUpdateFrame < Time.frameCount)
			{
				UpdateScene();
				lastUpdateFrame = Time.frameCount;
			}

			if ((pauseHandler != null) && pauseHandler.IsActivated())
			{
				pauseClient = !pauseClient;
				OnApplicationPause(pauseClient);
			}
		}


		/// <summary>
		/// Called just before the scene renders. 
		/// </summary>
		///
		public void OnPreRender()
		{
			if (lastPreRenderFrame < Time.frameCount)
			{
				UpdateScene();
				lastPreRenderFrame = Time.frameCount;
			}
		}


		/// <summary>
		/// Get new scene data now.
		/// </summary>
		///
		public void UpdateScene()
		{
			if (client != null)
			{
				if (client.IsConnected())
				{
					bool dataChanged  = false;
					bool sceneChanged = false;
					client.Update(ref dataChanged, ref sceneChanged);

					if (sceneChanged) NotifyListeners_Change(Scene);
					if (dataChanged ) NotifyListeners_Update(Scene);
				}
			}
		}


		/// <summary>
		/// Checks if the client is connected to the MotionServer.
		/// </summary>
		/// <returns><c>true</c> if the client is connected</returns>
		/// 
		public bool IsConnected
		{
			get
			{
				return (client != null) && client.IsConnected();
			}
		}

		/// <summary>
		/// Gets the name of the connected data source.
		/// </summary>
		/// <returns>Name of the connected data source</returns>
		/// 
		public string DataSourceName
		{
			get
			{
				return (client != null) ? client.GetDataSourceName() : "";
			}
		}


		/// <summary>
		/// Gets the amount of frames per second that the MoCap system provides.
		/// </summary>
		/// <returns>Update rate of the Mocap system in frames per second</returns>
		/// 
		public float Framerate
		{
			get
			{
				return (client != null) ? client.GetFramerate() : 0.0f;
			}
		}

		/// <summary>
		/// Gets the latest scene data structure.
		/// </summary>
		/// <returns>Scene data or <c>null</c> if client is not connected</returns>
		/// 
		public Scene Scene {
			get
			{
				return (client != null) ? client.GetScene() : null;
			}
		}


		/// <summary>
		/// Adds a scene data listener.
		/// </summary>
		/// <param name="listener">The listener to add</param>
		/// <returns><c>true</c>, if the scene listener was added, <c>false</c> otherwise.</returns>
		/// 
		public bool AddSceneListener(SceneListener listener)
		{
			bool added = false;
			if (!sceneListeners.Contains(listener))
			{
				sceneListeners.Add(listener);
				added = true;
				// immediately trigger callback
				if (client != null)
				{
					Scene scene = Scene;
					scene.mutex.WaitOne();
					listener.SceneDefinitionChanged(scene);
					scene.mutex.ReleaseMutex();
				}
			}
			return added;
		}


		/// <summary>
		/// Removes a scene data listener.
		/// </summary>
		/// <param name="listener">The listener to remove</param>
		/// <returns><c>true</c>, if the scene listener was removed, <c>false</c> otherwise.</returns>
		/// 
		public bool RemoveSceneListener(SceneListener listener)
		{
			return sceneListeners.Remove(listener);
		}


		/// <summary>
		/// Notifies scene listeners of an update.
		/// </summary>
		/// <param name="scene"> the scene has been updated</param>
		/// 
		private void NotifyListeners_Update(Scene scene)
		{
			scene.mutex.WaitOne();
			// pump latest data through the buffers before calling listeners
			foreach (Actor a in scene.actors)
			{
				foreach (Marker m in a.markers) { m.buffer.Push(); }
				foreach (Bone   b in a.bones)   { b.buffer.Push(); }
			}

			// call listeners
			foreach (SceneListener listener in sceneListeners)
			{
				listener.SceneDataUpdated(scene);
			}
			scene.mutex.ReleaseMutex();
		}


		/// <summary>
		/// Notifies scene listeners of a description change.
		/// </summary>
		/// <param name="scene"> the scene has been changed</param>
		/// 
		private void NotifyListeners_Change(Scene scene)
		{
			scene.mutex.WaitOne();
			foreach (SceneListener listener in sceneListeners)
			{
				listener.SceneDefinitionChanged(scene);
			}
			scene.mutex.ReleaseMutex();
		}


		/// <summary>
		/// Gets the internal NatNet client instance singelton.
		/// When creating the singleton for the first time, 
		/// tries to connect to a local MoCap server, and if not successful, a remote MoCap server.
		/// </summary>
		/// 
		private void CreateManager()
		{
			sceneListeners = new List<SceneListener>();

			clientMutex.WaitOne();
			if (client == null)
			{
				// only connect when this script is actually enabled
				if (this.isActiveAndEnabled)
				{
					// construct client name
					string appName = clientName;
					appName = appName.Replace("{SCENE}", SceneManager.GetActiveScene().name);

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

					// no client yet > try OpenVR
					if (((client == null) || !client.IsConnected()) && UnityEngine.XR.XRDevice.isPresent)
					{
						client = new OpenVR_Client();
						client.Connect(null);
						
						// did OpenVR work? If not, try he more generic Unity XR client
						if (!client.IsConnected())
						{
							client = new UnityXR_Client();
							client.Connect(null);
						}
					}

					if ((client != null) && client.IsConnected())
					{
						Debug.Log("MoCap client connected to " + client.GetDataSourceName() + ".\n" +
						          "Framerate: " + client.GetFramerate() + " fps");

						// print list of actor and device names
						Scene scene = client.GetScene();
						if (scene.actors.Count > 0)
						{
							string actorNames = "";
							foreach (Actor a in scene.actors)
							{
								if (actorNames.Length > 0) { actorNames += ", "; }
								actorNames += a.name;
							}
							Debug.Log("Actors (" + scene.actors.Count + "): " + actorNames);
						}
						if (scene.devices.Count > 0)
						{
							string deviceNames = "";
							foreach (Device d in scene.devices)
							{
								if (deviceNames.Length > 0) { deviceNames += ", "; }
								deviceNames += d.name;
							}
							Debug.Log("Devices (" + scene.devices.Count + "): " + deviceNames);
						}
					}
				}

				if ((client == null) || !client.IsConnected())
				{
					// not active or not able to connect to any data source: create dummy singleton 
					client = new DummyClient();
				}

				// all fine, notify listeners of scene change
				if ((client != null) && client.IsConnected())
				{
					NotifyListeners_Change(Scene);
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

					// construct sources list with connection data structures
			foreach (string source in configuration.Sources)
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
			return sources;
		}


		/// <summary>
		/// Searches for the MoCapManager instance in the scene and returns it
		/// or quits if it is not defined.
		/// </summary>
		/// <returns>the MoCapManager instance</returns>
		/// 
		public static MoCapManager Instance
		{
			get
			{
				if (instance == null)
				{
					if (!warningIssued)
					{
						Debug.LogWarning("No MoCapManager in scene");
						warningIssued = true;
					}
				}

				return instance;
			}
		}


		public void LoadConfiguration()
		{
			configuration.LoadConfiguration();
		}


		public void SaveConfiguration()
		{
			configuration.SaveConfiguration();
		}


		private static MoCapManager        instance       = null;
		private static List<SceneListener> sceneListeners = null;

		private static bool         warningIssued = false;
		private static IMoCapClient client        = null;
		private static Mutex        clientMutex   = new Mutex();

		private long lastUpdateFrame, lastPreRenderFrame;

		private InputHandler pauseHandler;
		private bool         pauseClient; 
	}

}
