using UnityEngine;
using System.Net;
using System.Threading;
using MoCap;

/// <summary>
/// Component for a MoCap client.
/// One instance of this object is needed that all moCap controlled objects get their data from.
/// </summary>
/// 
[DisallowMultipleComponent]
[AddComponentMenu("Motion Capture/MoCap Client")]
public class MoCapClient : MonoBehaviour
{
	[Tooltip("IP address of the Motion Server")]
	public string serverAddress = "127.0.0.1";

	[Tooltip("The name of this application")]
	public string clientAppName = "Unity MoCap Client";

	[Tooltip("Version number of this client")]
	public byte[] clientAppVersion = new byte[] { 1, 0, 2, 0 };

	[Tooltip("Scale factor for all translation units coming from the MoCap system")]
	public float unitScaleFactor = 1.0f;


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
		if ( client == null )
		{
			client = new NatNetClient(clientAppName, clientAppVersion);

			// test a local server first
			if (client.Connect(IPAddress.Loopback))
			{
				Debug.Log("MoCap client connected to local MotionServer '" + GetClient().GetServerName() + "'.");
			}
			// if not local, is it running remotely?
			else if (client.Connect(IPAddress.Parse(serverAddress)))
			{
				Debug.Log("MoCap client connected to MotionServer '" + GetClient().GetServerName() + "'.");
			}
			// nope, can't find it
			else
			{
				Debug.LogWarning("Could not connect to MoCap server at " + serverAddress + ".");
			}
		}
		clientMutex.ReleaseMutex();
		return client;
	}


	/// <summary>
	/// Called at the start of the scene. 
	/// </summary>
	/// 
	public void Start()
	{
		GetClient(); // trigger creation of singleton (if not already happened)
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


	private static NatNetClient client      = null;
	private static Mutex        clientMutex = new Mutex();
	private        bool         readyForNextFrame;
}
