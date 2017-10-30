//
//	  UnityOSC - Open Sound Control interface for the Unity3d game engine	  
//
//	  Copyright (c) 2012 Jorge Garcia Martin
//
// 	  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// 	  documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// 	  the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// 	  and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// 	  The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// 	  of the Software.
//
// 	  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// 	  TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// 	  THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// 	  CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// 	  IN THE SOFTWARE.
//
//	  Inspired by http://www.unifycommunity.com/wiki/index.php?title=AManagerClass

using SentienceLab.OSC;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityOSC;

/// <summary>
/// Handles all the OSC servers and clients of the current Unity game/application.
/// Tracks incoming and outgoing messages.
/// </summary>
/// 
[AddComponentMenu("OSC/Manager")]
[DisallowMultipleComponent]
public class OSC_Manager : MonoBehaviour
{
	public int      portIncoming    = 57110;
	public int      portOutgoing    = 57111;
	public string[] startClientList = { "127.0.0.1" };


	/// <summary>
	/// Initializes the OSC Handler.
	/// Here you can create the OSC servers and clientes.
	/// </summary>
	public void Start()
	{
		// start server
		server = new OSCServer(portIncoming);
		server.PacketReceivedEvent += OnPacketReceived;

		// prepare clients
		clients = new Dictionary<string, OSCClient>();
		foreach (string addr in startClientList)
		{
			clients.Add(addr, new OSCClient(IPAddress.Parse(addr), portOutgoing));
		}

		// do the variable gathering in the first Update call
		// because some Components might not have had Start() called until now.
		variableList = null;
		clientToExclude = null;
	}


	/// <summary>
	/// Ensure that the instance is destroyed when the game is stopped in the Unity editor
	/// Close all the OSC clients and servers
	/// </summary>
	void OnApplicationQuit() 
	{
		if ( server != null )
		{
			server.Close();
			server = null;
		}

		foreach (OSCClient client in clients.Values)
		{
			client.Close();
		}
		clients.Clear();
	}


	public void Update()
	{
		if (variableList == null)
		{
			GatherOSC_Variables();
		}
		else
		{
			// run Update on each variable
			foreach (OSC_Variable variable in variableList)
			{
				variable.Update();
			}
		}
	}


	protected void GatherOSC_Variables()
	{
		// gather all OSC variables in the scene
		variableList = new List<OSC_Variable>();
		ICollection<IOSCVariableContainer> containers = SentienceLab.ClassUtils.FindAll<IOSCVariableContainer>();
		foreach (IOSCVariableContainer container in containers)
		{
			variableList.AddRange(container.GetOSC_Variables());
		}
		// register this manager with all OSC variables
		string varNames = "";
		foreach (OSC_Variable variable in variableList)
		{
			varNames += ((varNames.Length == 0) ? "" : ", ") + variable.Name;
			variable.SetManager(this);
		}
		Debug.Log("OSC Variables: " + varNames);
		UpdateAllClients();
	}


	protected void UpdateAllClients()
	{
		foreach (OSC_Variable variable in variableList)
		{
			variable.SendUpdate();
		}
	}


	public void SendPacket(OSCPacket packet)
	{
		if (clients == null)
			return;
		foreach (OSCClient client in clients.Values)
		{
			if (client != clientToExclude)
			{
				client.Send(packet);
			}
		}
	}


	/// <summary>
	/// Raises the packet received event.
	/// </summary>
	/// <param name="server">the server that needs to process a packet.</param>
	/// <param name="packet">the packet to process.</param>
	/// 
	void OnPacketReceived(OSCServer server, OSCPacket packet)
	{
		// check if we have a new client
		string clientAddr = server.LastEndPoint.Address.ToString();
		if (!clients.ContainsKey(clientAddr))
		{
			// Yes: add to the list of addresses to send updates back to
			clients.Add(clientAddr, new OSCClient(IPAddress.Parse(clientAddr), portOutgoing));
			Debug.Log("Added OSC client " + clientAddr);
			UpdateAllClients();
		}
		else
		{
			// exclude client from receiving its own value
			clientToExclude = clients[clientAddr];
		}

		// check which variable will accept the packet
		foreach (OSC_Variable var in variableList)
		{
			if (var.CanAccept(packet))
			{
				var.Accept(packet);
				var.SendUpdate();
				break;
			}
		}

		clientToExclude = null;
	}


	private OSCServer                     server;
	private List<OSC_Variable>            variableList;
	private Dictionary<string, OSCClient> clients;
	private OSCClient                     clientToExclude;
}	

