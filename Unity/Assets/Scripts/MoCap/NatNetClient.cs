using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace MoCap
{
	/// <summary>
	/// Class for a NatNet compatible MoCap client.
	/// </summary>
	/// 
	class NatNetClient
	{
		/// <summary>
		/// Structure for storing the MotionServer name, version and protocol version information.
		/// </summary>
		/// 
		public struct ServerInfo
		{
			public string serverName;
			public byte[] versionServer;
			public byte[] versionNatNet;
		}

		// Portnumbers: Default is 1510/1511, but that seems to collide with Cortex.
		// 1503 is taken by Windows messenger, 1512 is taken by WINS
		// -> so let's use 1508, 1509
		public const int PORT_COMMAND = 1508;
		public const int PORT_DATA    = 1509;
		
		// Magic Packet numbers
		private const short NAT_PING                  = 0;
		private const short NAT_PINGRESPONSE          = 1;
		private const short NAT_REQUEST               = 2;
		private const short NAT_RESPONSE              = 3;
		private const short NAT_REQUEST_MODELDEF      = 4;
		private const short NAT_MODELDEF              = 5;
		private const short NAT_REQUEST_FRAMEOFDATA   = 6;
		private const short NAT_FRAMEOFDATA           = 7;
		private const short NAT_MESSAGESTRING         = 8;
		private const short NAT_UNRECOGNIZED_REQUEST  = 100;

		private const short DATASET_TYPE_MARKERSET  = 0;
		private const short DATASET_TYPE_RIGIDBODY  = 1;
		private const short DATASET_TYPE_SKELETON   = 2;
		private const short DATASET_TYPE_FORCEPLATE = 3;

		// some limits
		private const int MAX_NAMELENGTH    = 256;
		private const int MAX_RETRY_COMMAND = 10;
		private const int MAX_RETRY_DATA    = 3;

		private const int TIMEOUT_COMMAND = 1000;
		private const int TIMEOUT_FRAME   = 100;


		/// <summary>
		/// Constructs a NatNet client instance.
		/// This does not yet attempt to actually connect to the server.
		/// </summary>
		/// <param name="clientAppName">Name of the client to report to the server</param>
		/// <param name="clientAppVersion">Version number of the client to report to the server</param>
		/// 
		public NatNetClient(string clientAppName, byte[] clientAppVersion)
		{
			this.clientAppName       = clientAppName;
			this.clientAppVersion    = clientAppVersion;
			this.clientNatNetVersion = new byte[] {2, 9, 0, 0};

			this.sceneListeners  = new List<SceneListener>();
			this.actorListeners  = new Dictionary<ActorListener, Actor>();
			this.deviceListeners = new Dictionary<DeviceListener, Device>();

			serverInfo.serverName = "";
			multicastAddress      = null;
			scene                 = new Scene();
			connected             = false;
		}


		/// <summary>
		/// Tries to establish a connection to the server.
		/// </summary>
		/// <param name="serverAddress">IP address of the MotionServer to connect to</param>
		/// <returns><c>true</c> if the connection is established</returns>
		/// 
		public bool Connect(IPAddress serverAddress)
		{
			try
			{
				IPEndPoint commandEndpoint = new IPEndPoint(serverAddress, PORT_COMMAND);

				packetOut = new NatNetPacket_Out(commandEndpoint);
				packetIn  = new NatNetPacket_In();
				
				commandClient = new UdpClient(); // connect to server's command port

				if ( PingServer() )
				{
					GetSceneDescription();

					Debug.Log("Connected to NatNet server '" + serverInfo.serverName + "'.");
					// print list of actor and device names
					if (scene.actors.Length > 0)
					{
						string actorNames = "";
						foreach (Actor a in scene.actors)
						{
							if (actorNames.Length > 0) { actorNames += ", "; }
							actorNames += a.name;
						}
						Debug.Log("Actors (" + scene.actors.Length + "): " + actorNames);
					}
					if (scene.devices.Length > 0)
					{
						string deviceNames = "";
						foreach (Device d in scene.devices)
						{
							if (deviceNames.Length > 0) { deviceNames += ", "; }
							deviceNames += d.name;
						}
						Debug.Log("Devices (" + scene.devices.Length + "): " + deviceNames);
					}

					// immediately get first packet of frame data
					GetFrameData();
					connected = true;
					streamingEnabled = false;
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning("Could not connect to NatNet server " + serverAddress + " (" + e.Message + ").");
			}

			// request streaming IP address
			if (connected)
			{
				try
				{
					if (SendRequest("getDataStreamAddress") && (serverResponse.Length > 0))
					{
						multicastAddress = IPAddress.Parse(serverResponse);
						Debug.Log("Data stream address: " + multicastAddress);

						// Prepare multicast data reception
						dataClient = new UdpClient();
						dataClient.ExclusiveAddressUse = false;
						dataClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
						dataClient.Client.Bind(new IPEndPoint(IPAddress.Any, PORT_DATA));
						dataClient.JoinMulticastGroup(multicastAddress);

						dataClient.Client.ReceiveTimeout = 100;
					}
				}
				catch (Exception e)
				{
					Debug.LogWarning("Could not establish data streaming (" + e.Message + ").");
					if (dataClient != null)
					{
						dataClient.Close();
						dataClient = null;
					}
				}
			}

			return connected;
		}


		/// <summary>
		/// Checks if the client is connected to the MotionServer.
		/// </summary>
		/// <returns><c>true</c> if the client is connected</returns>
		/// 
		public bool IsConnected()
		{
			return connected;
		}


		/// <summary>
		/// Disconnects the client from the server.
		/// </summary>
		/// 
		public void Disconnect()
		{
			RemoveAllListeners();

			if (dataClient != null)
			{
				dataClient.DropMulticastGroup(multicastAddress);
				dataClient.Close();
				dataClient = null;
			}

			if (commandClient != null)
			{
				commandClient.Close();
				commandClient = null;
			}

			connected = false;
		}
		
		
		/// <summary>
		/// Gets the name of the MotionServer.
		/// </summary>
		/// <returns>the name of the MotionServer</returns>
		/// 
		public String GetServerName()
		{
			if ( !connected ) return "";
			return serverInfo.serverName + " v" +
				   serverInfo.versionServer[0] + "." + serverInfo.versionServer[1] + "." +
				   serverInfo.versionServer[2] + "." + serverInfo.versionServer[3];
		}


		/// <summary>
		/// Gets the latest frame data either via the multicast channel
		/// or via polling.
		/// </summary>
		public void Update()
		{
			if (connected)
			{
				if (dataClient != null)
				{
					int maxIterations = 5;
					while ( (dataClient.Available > 0) && 
							(maxIterations-- > 0) )
					{
						// data streaming > just see what has arrived, no polling necessary
						if (packetIn.Receive(dataClient) > 0)
						{
							ParsePacket(packetIn, NAT_FRAMEOFDATA);
							if (!streamingEnabled)
							{
								Debug.Log("Data streaming enabled.");
								streamingEnabled = true;
							}
						}
					}
				}

				if ( !streamingEnabled )
				{
					// no data streaming > get frame per polling
					GetFrameData();
				}
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
				listener.SceneChanged(scene);
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
		/// Adds an actor data listener.
		/// </summary>
		/// <param name="listener">The listener to add</param>
		/// <returns><c>true</c>, if the actor listener was added, <c>false</c> otherwise.</returns>
		/// 
		public bool AddActorListener(ActorListener listener)
		{
			bool added = false;
			if ( !actorListeners.ContainsKey(listener) )
			{
				Actor actor = scene.FindActor(listener.GetActorName());
				actorListeners.Add(listener, actor);
				added = true;
				// immediately trigger callback
				listener.ActorChanged(actor);
			}
			return added;
		}


		/// <summary>
		/// Removes an actor data listener.
		/// </summary>
		/// <param name="listener">The listener to remove</param>
		/// <returns><c>true</c>, if the actor listener was removed, <c>false</c> otherwise.</returns>
		///
		public bool RemoveActorListener(ActorListener listener)
		{
			return actorListeners.Remove(listener);
		}


		/// <summary>
		/// Adds a device data listener.
		/// </summary>
		/// <param name="listener">The listener to add</param>
		/// <returns><c>true</c>, if the device listener was added, <c>false</c> otherwise.</returns>
		/// 
		public bool AddDeviceListener(DeviceListener listener)
		{
			bool added = false;
			if (!deviceListeners.ContainsKey(listener))
			{
				Device device = scene.FindDevice(listener.GetDeviceName());
				deviceListeners.Add(listener, device);
				added = true;
				// immediately trigger callback
				listener.DeviceChanged(device);
			}
			return added;
		}


		/// <summary>
		/// Removes a device data listener.
		/// </summary>
		/// <param name="listener">The listener to remove</param>
		/// <returns><c>true</c>, if the device listener was removed, <c>false</c> otherwise.</returns>
		///
		public bool RemoveDeviceListener(DeviceListener listener)
		{
			return deviceListeners.Remove(listener);
		}


		/// <summary>
		/// Removes all listeners.
		/// </summary>
		///
		public void RemoveAllListeners()
		{
			sceneListeners.Clear();
			actorListeners.Clear();
			deviceListeners.Clear();
		}


		private bool PingServer()
		{
			bool success = false;

			// prepare packet to server
			packetOut.Initialise(NAT_PING);
			// send client name (padded to maximum string length
			packetOut.PutFixedLengthString(clientAppName, MAX_NAMELENGTH);
			// add version numbers
			packetOut.PutBytes(clientAppVersion);
			packetOut.PutBytes(clientNatNetVersion);

			// and send
			if ( !packetOut.Send(commandClient) )
			{
				Debug.LogWarning("Could not send ping request to NatNet server.");
				return false;
			}

			// wait for answer
			commandClient.Client.ReceiveTimeout = TIMEOUT_COMMAND;
			success = WaitForSpecificPacket(NAT_PINGRESPONSE, MAX_RETRY_COMMAND);

			return success;
		}


		private bool GetSceneDescription()
		{
			bool success = false;

			// prepare packet to server
			packetOut.Initialise(NAT_REQUEST_MODELDEF);

			// and send
			if ( !packetOut.Send(commandClient) )
			{
				Debug.LogWarning("Could not send definitions request to NatNet server.");
				return false;
			}

			// wait for answer
			commandClient.Client.ReceiveTimeout = TIMEOUT_COMMAND;
			success = WaitForSpecificPacket(NAT_MODELDEF, MAX_RETRY_COMMAND);

			return success;
		}


		private bool GetFrameData()
		{
			bool success = false;

			// prepare packet to server
			packetOut.Initialise(NAT_REQUEST_FRAMEOFDATA);

			// and send
			if ( !packetOut.Send(commandClient) )
			{
				Debug.LogWarning("Could not send frame data request to NatNet server.");
				return false;
			}

			// wait for answer
			commandClient.Client.ReceiveTimeout = TIMEOUT_FRAME;
			success = WaitForSpecificPacket(NAT_FRAMEOFDATA, MAX_RETRY_DATA);

			return success;
		}


		private bool SendRequest(String command)
		{
			bool success = false;

			// prepare packet to server
			packetOut.Initialise(NAT_REQUEST);
			packetOut.PutString(command);

			// and send
			if (!packetOut.Send(commandClient))
			{
				Debug.LogWarning("Could not send request to NatNet server.");
				return false;
			}

			// wait for answer
			commandClient.Client.ReceiveTimeout = TIMEOUT_COMMAND;
			success = WaitForSpecificPacket(NAT_RESPONSE, MAX_RETRY_COMMAND);

			return success;
		}


		private bool WaitForSpecificPacket(int expectedId, int maxRetries)
		{
			int retries  = 0;
			bool success = false;
			while (!success && (retries < maxRetries))
			{
				if (packetIn.Receive(commandClient) > 0)
				{
					if (packetIn.GetID() == expectedId)
					{
						success = ParsePacket(packetIn, expectedId);
					}
					else
					{
						// received something, but wrong ID, try again
						retries++;
					}
				}
				else
				{
					// not received anything > get out here
					break;
				}
			}
			if (!success)
			{
				Debug.LogWarning("Did not receive expected response " + expectedId + " from NatNet server.");
			}
			return success;
		}


		private bool ParsePacket(NatNetPacket_In packet, int expectedId)
		{
			bool success = false;

			int id = packet.GetID();
			if ( id != expectedId )
			{
				Debug.LogWarning("Unexpected response received from NatNet server (expected " + expectedId + ", received " + id + ").");
				return false;
			}
			else
			{
				switch ( id )
				{
					case NAT_PINGRESPONSE : success = ParsePing(packet); break;
					case NAT_RESPONSE     : success = ParseResponse(packet); break;
					case NAT_MODELDEF     : success = ParseModelDefinition(packet); break;
					case NAT_FRAMEOFDATA  : success = ParseFrameOfData(packet); break;
					case NAT_UNRECOGNIZED_REQUEST:
						Debug.LogWarning("Unrecognized request.");
						break;
					default:
						Debug.LogWarning("Received unknown response packet ID " + id + ".)");
						break;
				}
			}
			return success;
		}


		private bool ParsePing(NatNetPacket_In packet)
		{
			serverInfo = new ServerInfo();
			serverInfo.serverName = packet.GetFixedLengthString(MAX_NAMELENGTH);
			// read server version numbers
			serverInfo.versionServer = new byte[4];
			packet.GetBytes(ref serverInfo.versionServer);
			serverInfo.versionNatNet = new byte[4];
			packet.GetBytes(ref serverInfo.versionNatNet);

			Debug.Log("Received ping response from NatNet server '" + serverInfo.serverName + "' v" +
			          serverInfo.versionServer[0] + "." + serverInfo.versionServer[1] + "." +
			          serverInfo.versionServer[2] + "." + serverInfo.versionServer[3] + " (NatNet version " +
			          serverInfo.versionNatNet[0] + "." + serverInfo.versionNatNet[1] + "." +
			          serverInfo.versionNatNet[2] + "." + serverInfo.versionNatNet[3] + ")");
			return true;
		}


		private bool ParseResponse(NatNetPacket_In packet)
		{
			// response from server > unpack into variable
			serverResponse = packet.GetString();
			return true;
		}


		private bool ParseModelDefinition(NatNetPacket_In packet)
		{
			int numDescriptions = packet.GetInt32();

			List<Actor>  actors  = new List<Actor>();
			List<Device> devices = new List<Device>();
			for ( int dIdx = 0 ; dIdx < numDescriptions ; dIdx++ )
			{
				int datasetType = packet.GetInt32();
				switch ( datasetType )
				{
					case DATASET_TYPE_MARKERSET  : ParseMarkerset(packet, actors); break;
					case DATASET_TYPE_RIGIDBODY  : ParseRigidBody(packet, actors); break;
					case DATASET_TYPE_SKELETON   : ParseSkeleton(packet, actors); break;
					case DATASET_TYPE_FORCEPLATE : ParseForcePlate(packet, devices); break;
					default: 
					{
						Debug.LogWarning("Invalid dataset type " + datasetType + " in model definition respose.");
						break;
					}
				}
			}

			scene.actors  = actors.ToArray();
			scene.devices = devices.ToArray();

			// scene description has possibly changed > update device and actor listeners
			RefreshListeners();

			return true;
		}


		private void ParseMarkerset(NatNetPacket_In packet, List<Actor> actors)
		{
			int    id   = 0;                  // no ID for markersets
			string name = packet.GetString(); // markerset name
			Actor actor = new Actor(scene, id, name);

			int nMarkers = packet.GetInt32();  // marker count
			// TODO: Sanity check on the number before allocating that much space
			actor.markers = new Marker[nMarkers];
			for ( int markerIdx = 0 ; markerIdx < nMarkers ; markerIdx++ )
			{
				name = packet.GetString();
				Marker marker = new Marker(actor, name);
				actor.markers[markerIdx] = marker;
			}
			actors.Add(actor);
		}


		private void ParseRigidBody(NatNetPacket_In packet, List<Actor> actors)
		{
			string name = packet.GetString(); // name, TODO: No name in major version < 2
			int    id   = packet.GetInt32();  // ID

			// rigid body name should be equal to actor name: search
			Actor actor = null;
			foreach (Actor a in actors)
			{
				if ( a.name.CompareTo(name) == 0 )
				{
					actor = a;
				}
			}
			if ( actor == null )
			{
				Debug.LogWarning("Rigid Body " + name + " could not be matched to an actor.");
				actor = new Actor(scene, id, name);
				actors.Add(actor);
			}

			Bone bone = new Bone(actor, name, id);

			packet.GetInt32();    // Parent ID (ignore for rigid body)
			bone.parent  =  null;                 // rigid bodies should not have a parent
			bone.ox      =  packet.GetFloat();    // X offset
			bone.oy      =  packet.GetFloat();    // Y offset
			bone.oz      = -packet.GetFloat();    // Z offset
			
			actor.bones    = new Bone[1];
			actor.bones[0] = bone;
		}


		private void ParseSkeleton(NatNetPacket_In packet, List<Actor> actors)
		{
			bool includesBoneNames = // starting at v2.0
					(serverInfo.versionNatNet[0] >= 2);

			String skeletonName = packet.GetString(); // name
			int    skeletonId   = packet.GetInt32();  // ID

			// rigid body name should be equal to actor name: search
			Actor actor = null;
			foreach (Actor a in actors)
			{
				if (a.name.CompareTo(skeletonName) == 0)
				{
					actor = a;
					actor.id = skeletonId; // associate actor and skeleton 
				}
			}
			if (actor == null)
			{
				// names don't match > try IDs
				if ((skeletonId >= 0) && (skeletonId < actors.Count))
				{
					actor = actors[skeletonId];
				}
			}
			if (actor == null)
			{
				Debug.LogWarning("Skeleton " + skeletonName + " could not be matched to an actor.");
				actor = new Actor(scene, skeletonId, skeletonName);
				actors.Add(actor);
			}

			int nBones = packet.GetInt32(); // Skeleton bone count
			// TODO: Sanity check on the number before allocating that much space
			actor.bones = new Bone[nBones];
			for ( int boneIdx = 0 ; boneIdx < nBones ; boneIdx++ )
			{
				String name = "";
				if (includesBoneNames)
				{
					name = packet.GetString(); // bone name 
				}
				int id = packet.GetInt32(); // bone ID
				Bone bone = new Bone(actor, name, id);

				bone.parent = actor.FindBone(packet.GetInt32()); // Skeleton parent ID
				if (bone.parent != null)
				{
					// if bone has a parent, update child list of parent
					bone.parent.children.Add(bone);
				}
				bone.BuildChain(); // build chain from root to this bone

				bone.ox =  packet.GetFloat(); // X offset
				bone.oy =  packet.GetFloat(); // Y offset
				bone.oz = -packet.GetFloat(); // Z offset

				actor.bones[boneIdx] = bone;
			}
		}


		private void ParseForcePlate(NatNetPacket_In packet, List<Device> devices)
		{
			int    id     = packet.GetInt32();           // force plate ID
			String name   = packet.GetString();          // force plate serial #
			Device device = new Device(scene, name, id); // create device

			// skip next 652 bytes 
			// (SDK 2.9 sample code does not explain what this is about)
			packet.Skip(652);

			int nChannels = packet.GetInt32(); // channel count
			device.channels = new Channel[nChannels];
			for (int channelIdx = 0; channelIdx < nChannels; channelIdx++)
			{
				name = packet.GetString();
				Channel channel = new Channel(device, name);
				device.channels[channelIdx] = channel;
			}
			devices.Add(device);
		}


		private bool ParseFrameOfData(NatNetPacket_In packet)
		{
			// determine special datasets depending on NatNet version
			bool includesMarkerIDsAndSizes = // starting at v2.0
					(serverInfo.versionNatNet[0] >= 2);
			bool includesSkeletonData = // starting at v2.1
					((serverInfo.versionNatNet[0] == 2) &&
					 (serverInfo.versionNatNet[1] >= 1)) ||
					(serverInfo.versionNatNet[0] > 2);
			bool includesTrackingState = //  starting at v2.6
					((serverInfo.versionNatNet[0] == 2) &&
					 (serverInfo.versionNatNet[1] >= 6)) ||
					(serverInfo.versionNatNet[0] > 2);
			bool includesLabelledMarkers = //  starting at v2.3
					((serverInfo.versionNatNet[0] == 2) &&
					  (serverInfo.versionNatNet[1] >= 3)) ||
					(serverInfo.versionNatNet[0] > 2);
			bool includesLabelledMarkerFlags = //  starting at v2.6
					((serverInfo.versionNatNet[0] == 2) &&
					  (serverInfo.versionNatNet[1] >= 6)) ||
					(serverInfo.versionNatNet[0] > 2);
			bool includesForcePlateData = // starting at v2.9
					((serverInfo.versionNatNet[0] == 2) &&
					  (serverInfo.versionNatNet[1] >= 9)) ||
					(serverInfo.versionNatNet[0] > 2);

			int frameNumber = packet.GetInt32(); // frame number

			// is this an actual update? Or did we receive an older frame
			// delta < 10: but do consider looping playback 
			// when frame numbers suddenly differ significantly
			int deltaFrame = frameNumber - scene.frameNumber;
			if ( (deltaFrame < 0) && (deltaFrame > -10) ) return true; // old frame, get out

			scene.frameNumber = frameNumber;

			// Read actor data
			int nActors = packet.GetInt32(); // actor count
			for ( int actorIdx = 0 ; actorIdx < nActors ; actorIdx++ )
			{
				string actorName = packet.GetString();
				// find the corresponding actor
				Actor actor = scene.FindActor(actorName);

				int nMarkers = packet.GetInt32();
				for ( int markerIdx = 0 ; markerIdx < nMarkers ; markerIdx++ )
				{
					Marker marker = (actor != null) ? actor.markers[markerIdx] : DUMMY_MARKER;
					// Read position
					marker.px = packet.GetFloat();
					marker.py = packet.GetFloat();
					marker.pz = packet.GetFloat();
					TransformToUnity(ref marker);

					// marker is tracked when at least one coordinate is not 0
					marker.tracked = (marker.px != 0) ||
									 (marker.py != 0) ||
									 (marker.pz != 0);
				}
			}

			// skip unidentified markers
			int nUnidentifiedMarkers = packet.GetInt32();
			int unidentifiedMarkerDataSize = 3 * 4; // 3 floats
			packet.Skip(unidentifiedMarkerDataSize * nUnidentifiedMarkers);

			// Read rigid body data
			int nRigidBodies = packet.GetInt32(); // bone count
			for ( int rigidBodyIdx = 0 ; rigidBodyIdx < nRigidBodies ; rigidBodyIdx++ )
			{
				int rigidBodyID = packet.GetInt32(); // get rigid body ID

				// find the corresponding actor
				Bone bone = DUMMY_BONE;
				if ( (rigidBodyID >=0) && (rigidBodyID < scene.actors.Length) )
				{
					//TODO: What if there is no bone in that actor?
					bone = scene.actors[rigidBodyID].bones[0];
				}

				// Read position/rotation 
				bone.px = packet.GetFloat(); // position
				bone.py = packet.GetFloat();
				bone.pz = packet.GetFloat(); 
				bone.qx = packet.GetFloat(); // rotation
				bone.qy = packet.GetFloat();
				bone.qz = packet.GetFloat();
				bone.qw = packet.GetFloat();
				TransformToUnity(ref bone);

				int nMarkers = packet.GetInt32();
				for ( int i = 0 ; i < nMarkers ; i++ )
				{
					packet.GetFloat(); // Marker X
					packet.GetFloat(); // Marker Y
					packet.GetFloat(); // Marker Z
				}

				if ( includesMarkerIDsAndSizes )
				{
					// also, marker IDs and sizes
					for ( int i = 0 ; i < nMarkers ; i++ )
					{
						packet.GetInt32(); // Marker ID
					}
					// and sizes
					for ( int i = 0 ; i < nMarkers ; i++ )
					{
						packet.GetFloat(); // Marker size
					}
					
					packet.GetFloat(); // Mean error
				}

				// Tracking state
				if (includesTrackingState)
				{
					int state = packet.GetInt16();
					// 0x01 : rigid body was successfully tracked in this frame
					bone.tracked = (state & 0x01) != 0;
				}
				else
				{
					// tracking state not sent separately,
					// but position = (0,0,0) used as "not tracked" indicator
					bone.tracked = (bone.px != 0) ||
								   (bone.py != 0) ||
								   (bone.pz != 0);
				}
			}

			// Read skeleton data
			if (includesSkeletonData)
			{
				int nSkeletons = packet.GetInt32();
				for ( int skeletonIdx = 0 ; skeletonIdx < nSkeletons ; skeletonIdx++ )
				{
					int skeletonId = packet.GetInt32();

					// match skeleton and actor ID
					Actor actor = scene.FindActor(skeletonId);
					if (actor == null)
					{
						Debug.LogWarning("Could not find actor " + skeletonId);
						return false;
					}

					// # of bones in skeleton
					int nBones = packet.GetInt32(); 
					// TODO: Number sanity check
					for ( int nBodyIdx = 0 ; nBodyIdx < nBones ; nBodyIdx++ ) 
					{
						int boneId = packet.GetInt32();
						Bone bone = actor.FindBone(boneId);
						if ( bone == null ) bone = DUMMY_BONE;

						// Read position/rotation
						bone.px = packet.GetFloat(); // position
						bone.py = packet.GetFloat();
						bone.pz = packet.GetFloat();
						bone.qx = packet.GetFloat(); // rotation
						bone.qy = packet.GetFloat();
						bone.qz = packet.GetFloat();
						bone.qw = packet.GetFloat();
						TransformToUnity(ref bone);

						// read/skip rigid marker data
						int nMarkers = packet.GetInt32();
						for ( int i = 0 ; i < nMarkers ; i++ )
						{
							packet.GetFloat(); // X/Y/Z position
							packet.GetFloat(); 
							packet.GetFloat();
						}
						for ( int i = 0 ; i < nMarkers ; i++ )
						{
							packet.GetInt32(); // Marker IDs
						}
						for ( int i = 0 ; i < nMarkers ; i++ )
						{
							packet.GetFloat(); // Marker size
						}

						// ATTENTION: actually "Mean marker error", but used as bone length
						bone.length = packet.GetFloat(); 

						// Tracking state
						if (includesTrackingState)
						{
							int state = packet.GetInt16();
							// 0x01 : rigid body was successfully tracked in this frame
							bone.tracked = (state & 0x01) != 0;
						}
						else
						{
							// tracking state not sent separately,
							// but position = (0,0,0) used as "not tracked" indicator
							bone.tracked = (bone.px != 0) || 
										   (bone.py != 0) || 
										   (bone.pz != 0);
						}
					} // next rigid body
				} // next skeleton 

				// skip labelled markers 
				if (includesLabelledMarkers)
				{
					int nLabelledMarkers = packet.GetInt32();
					int labelledMarkerDataSize =
							includesLabelledMarkerFlags ?
								5 * 4 + 1 * 2 : // 1 int, 4 floats, 1 short
								5 * 4; // 1 int, 4 floats
					packet.Skip(nLabelledMarkers * labelledMarkerDataSize);
					// without skipping:
					// for ( int markerIdx = 0; markerIdx  < nLabeledMarkers; markerIdx++ )
					// {
					//     int   id   =  buf.getInt();
					//     float x    =  buf.getFloat();
					//     float y    =  buf.getFloat();
					//     float z    = -buf.getFloat();
					//     float size =  buf.getFloat();

					//     if ( includesLabelledMarkerFlags ) 
					//     {
					//         short params = buf.getShort();
					//     }
					// }
				}

				// read force plate data
				if (includesForcePlateData)
				{
					int nForcePlates = packet.GetInt32();
					for (int forcePlateIdx = 0; forcePlateIdx < nForcePlates; forcePlateIdx++)
					{
						// read force plate ID and find corresponding device
						int forcePlateId = packet.GetInt32();
						Device device = scene.FindDevice(forcePlateId);
						if (device == null) device = DUMMY_DEVICE;

						// channel count
						int nChannels = packet.GetInt32();
						// channel data
						for (int chn = 0; chn < nChannels; chn++)
						{
							int   nFrames = packet.GetInt32();
							float value   = 0;
							for (int frameIdx = 0; frameIdx < nFrames; frameIdx++)
							{
								value = packet.GetFloat();
							}
							if (chn < device.channels.Length)
							{
								// effectively only keep the last (or only) value
								device.channels[chn].value = value;
							}
						}
					}
				}

				// read latency and convert from s to ms
				scene.latency = (int)(packet.GetFloat() * 1000);
			}

			NotifyListeners_Update();

			return true;
		}


		/// <summary>
		/// Converts a marker position from a right handed coordinate to a left handed (Unity).
		/// </summary>
		/// <param name="pos">the marker to convert</param>
		/// 
		private void TransformToUnity(ref Marker marker)
		{
			marker.pz *= -1; // flip Z
		}


		/// <summary>
		/// Converts a bone from a right handed rotation to a left handed (Unity).
		/// </summary>
		/// <param name="bone">the bone to convert</param>
		/// 
		private void TransformToUnity(ref Bone bone)
		{
			bone.pz *= -1; // flip Z pos

			/*
			Quaternion q = new Quaternion(bone.qx, bone.qy, bone.qz, bone.qw);
			Vector3 e = q.eulerAngles;
			Quaternion x = Quaternion.AngleAxis( e.x, Vector3.right);
			Quaternion y = Quaternion.AngleAxis(-e.y + 180, Vector3.up);
			Quaternion z = Quaternion.AngleAxis( e.z, Vector3.forward);
			q = (z * y * x);

			bone.qx = q.x;
			bone.qy = q.y;
			bone.qz = q.z;
			bone.qw = q.w;
			*/
			
			bone.qx *= -1;
			bone.qy *= -1;
			
			/*
			Quaternion q = new Quaternion(bone.qx, bone.qy, bone.qz, bone.qw);
			float   angle = 0.0f;
			Vector3 axis = Vector3.zero;
			q.ToAngleAxis(out angle, out axis);
			axis.z = -axis.z;
			q = Quaternion.AngleAxis(-angle, axis);

			Debug.Log(
				"from X=" + bone.qx + ",Y=" + bone.qy + ",Z=" + bone.qz + ",W=" + bone.qw +
				"  to  X=" + q.x + ",Y=" + q.y + ",Z=" + q.z + ",W=" + q.w);				

			bone.qx = q.x;
			bone.qy = q.y;
			bone.qz = q.z;
			bone.qw = q.w;
			*/
		}


		public Scene GetScene()
		{
			return scene;
		}


		private void NotifyListeners_Update()
		{
			foreach (SceneListener listener in sceneListeners)
			{
				listener.SceneUpdated(scene);
			}
			foreach (KeyValuePair<ActorListener, Actor> entry in actorListeners)
			{
				// which actor is that?
				ActorListener listener = entry.Key;
				Actor         actor    = entry.Value;
				if ( actor != null )
				{
					listener.ActorUpdated(actor);
				}
			}
			foreach (KeyValuePair<DeviceListener, Device> entry in deviceListeners)
			{
				// which device is that?
				DeviceListener listener = entry.Key;
				Device         device = entry.Value;
				if (device != null)
				{
					listener.DeviceUpdated(device);
				}
			}
		}


		private void RefreshListeners()
		{
			foreach (SceneListener listener in sceneListeners)
			{
				listener.SceneChanged(scene);
			}
			List<ActorListener> actorKeys = new List<ActorListener>(actorListeners.Keys);
			foreach (ActorListener listener in actorKeys)
			{
				Actor actor = scene.FindActor(listener.GetActorName());
				actorListeners[listener] = actor;
				listener.ActorChanged(actor);
			}

			List<DeviceListener> deviceKeys = new List<DeviceListener>(deviceListeners.Keys);
			foreach (DeviceListener listener in deviceKeys)
			{
				Device device = scene.FindDevice(listener.GetDeviceName());
				deviceListeners[listener] = device;
				listener.DeviceChanged(device);
			}
		}


		private string           clientAppName;
		private byte[]           clientAppVersion;
		private byte[]           clientNatNetVersion;
		private UdpClient        commandClient, dataClient;
		private NatNetPacket_In  packetIn;
		private NatNetPacket_Out packetOut;
		private ServerInfo       serverInfo;
		private IPAddress        multicastAddress;
		private string           serverResponse;
		private bool             connected, streamingEnabled;
		private Scene            scene;

		private static Marker DUMMY_MARKER  = new Marker(null, "dummy");
		private static Bone   DUMMY_BONE    = new Bone(null, "dummy", 0);
		private static Device DUMMY_DEVICE  = new Device(null, "dummy", 0);

		private List<SceneListener>                sceneListeners;
		private Dictionary<ActorListener, Actor>   actorListeners;
		private Dictionary<DeviceListener, Device> deviceListeners;

	}

}
