using UnityEngine;
using System.Net;
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
	public byte[] clientAppVersion = new byte[] { 1, 0, 1, 0 };

	[Tooltip("Scale factor for all translation units coming from the MoCap system")]
	public float unitScaleFactor = 1.0f;


	/// <summary>
	/// Gets the internal NatNet client instance singelton.
	/// </summary>
	/// <returns>The NatNet singleton</returns>
	/// 
	private NatNetClient GetClient()
	{
		if ( client == null )
		{
			client = new NatNetClient(clientAppName, clientAppVersion);
		}
		return client;
	}


	/// <summary>
	/// Called at the start of the scene. 
	/// Tries to connect to the remote (and if not found, a local) MoCap server.
	/// </summary>
	/// 
	public void Start () 
	{
		// test a local server first
		if ( GetClient().Connect(IPAddress.Loopback) )
		{
			Debug.Log("MoCap client connected to local server " + GetClient().GetServerName());
		}
		// if not local, is it running remotely?
		else if ( GetClient().Connect(IPAddress.Parse(serverAddress)) )
		{
			Debug.Log("MoCap client connected to server " + GetClient().GetServerName());
		}
		// nope, can't find it
		else
		{
			Debug.LogWarning("Could not connect to MoCap server at " + serverAddress + ".");
		}
	}

	 
	/// <summary>
	/// Called once per frame.
	/// Polls a new frame description.
	/// TODO: This will have to be replaced by Multicast mechanisms later
	/// </summary>
	/// 
	public void Update ()
	{
		if ( GetClient().IsConnected() )
		{
			GetClient().Update();
		}
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


	private NatNetClient client = null;
}
