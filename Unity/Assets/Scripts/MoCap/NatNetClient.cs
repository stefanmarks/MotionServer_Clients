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

		private const int MAX_NAMELENGTH = 256;

		private const int TIMEOUT_COMMAND = 1000;
		private const int TIMEOUT_FRAME   = 100;

		public NatNetClient(string clientAppName, byte[] clientAppVersion)
		{
			this.clientAppName       = clientAppName;
			this.clientAppVersion    = clientAppVersion;
			this.clientNatNetVersion = new byte[] {2, 9, 0, 0};

			this.actorListeners = new Dictionary<ActorListener, Actor>();

			serverInfo.serverName = "";
			scene                 = new Scene();
			connected             = false;
		}


		public bool Connect(IPAddress serverAddress)
		{
			try
			{
				IPEndPoint commandEndpoint = new IPEndPoint(serverAddress, PORT_COMMAND);
				// IPEndPoint dataEndpoint    = new IPEndPoint(serverAddress, PORT_DATA);

				packetOut = new NatNetPacket_Out(commandEndpoint);
				packetIn  = new NatNetPacket_In();
				
				commandClient = new UdpClient(); // connect to server's command port

				if ( PingServer() )
				{
					GetSceneDescription();

					Debug.Log("Connected to MotionServer");
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

					connected = true;
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning("Could not connect to MoCap server " + serverAddress + " (" + e.Message + ").");
			}
			return connected;
		}


		public bool IsConnected()
		{
			return connected;
		}


		public void Disconnect()
		{
			commandClient.Close();
			connected = false;
			//dataClient.Close();
		}
		
		
		public String GetServerName()
		{
			if ( !connected ) return "";
			return serverInfo.serverName + " v" +
			       serverInfo.versionServer[0] + "." + serverInfo.versionServer[1] + "." +
			       serverInfo.versionServer[2] + "." + serverInfo.versionServer[3];
		}


		public void Update()
		{
			if ( connected )
			{
				GetFrameData();
			}
		}


		public bool AddActorListener(ActorListener listener)
		{
			bool added = false;
			if ( !actorListeners.ContainsKey(listener) )
			{
				Actor actor = scene.FindActor(listener.GetActorName());
				actorListeners.Add(listener, actor);
				added = true;
			}
			return added;
		}


		public bool RemoveActorListener(ActorListener listener)
		{
			return actorListeners.Remove(listener);
		}


		private bool PingServer()
		{
			bool success = false;

			packetOut.Initialise(NAT_PING);

			// send client name (padded to maximum string length
			packetOut.PutFixedLengthString(clientAppName, MAX_NAMELENGTH);
			// add version numbers
			packetOut.PutBytes(clientAppVersion);
			packetOut.PutBytes(clientNatNetVersion);
			// and send
			if ( !packetOut.Send(commandClient) )
			{
				Debug.LogWarning("Could not send ping request to MoCap server.");
				return false;
			}

			// wait for answer
			commandClient.Client.ReceiveTimeout = TIMEOUT_COMMAND;
			if ( packetIn.Receive(commandClient) > 0 )
			{
				success = ParsePacket(packetIn, NAT_PINGRESPONSE) ;
			}
			return success;
		}


		private bool GetSceneDescription()
		{
			bool success = false;
			
			packetOut.Initialise(NAT_REQUEST_MODELDEF);
			// and send
			if ( !packetOut.Send(commandClient) )
			{
				Debug.LogWarning("Could not send definitions request to MoCap server.");
				return false;
			}

			// wait for answer
			commandClient.Client.ReceiveTimeout = TIMEOUT_COMMAND;
			if ( packetIn.Receive(commandClient) > 0 )
			{
				success = ParsePacket(packetIn, NAT_MODELDEF) ;
			}
			return success;
		}


		private bool GetFrameData()
		{
			bool success = false;

			packetOut.Initialise(NAT_REQUEST_FRAMEOFDATA);
			// and send
			if ( !packetOut.Send(commandClient) )
			{
				Debug.LogWarning("Could not send frame data request to MoCap server.");
				return false;
			}

			// wait for answer
			commandClient.Client.ReceiveTimeout = TIMEOUT_FRAME;
			if ( packetIn.Receive(commandClient) > 0 )
			{
				success = ParsePacket(packetIn, NAT_FRAMEOFDATA) ;
			}
			return success;
		}


		private bool ParsePacket(NatNetPacket_In packet)
		{
			return ParsePacket(packet, -1);
		}


		private bool ParsePacket(NatNetPacket_In packet, int expectedId)
		{
			bool success = false;

			int id = packet.GetID();
			if ( (expectedId >= 0) && (id != expectedId) )
			{
				Debug.LogWarning("Unexpected response received from MoCap server (expected " + expectedId + ", received " + id + ").");
				return false;
			}
			else
			{
				switch ( id )
				{
					case NAT_PINGRESPONSE : success = ParsePing(packet); break;
					case NAT_MODELDEF     : success = ParseModelDefinition(packet); break;
					case NAT_FRAMEOFDATA  : success = ParseFrameOfData(packet); break;
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

			Debug.Log("Received ping response from MoCap server " + serverInfo.serverName + " v" +
			          serverInfo.versionServer[0] + "." + serverInfo.versionServer[1] + "." +
			          serverInfo.versionServer[2] + "." + serverInfo.versionServer[3] + " (NatNet version " +
			          serverInfo.versionNatNet[0] + "." + serverInfo.versionNatNet[1] + "." +
			          serverInfo.versionNatNet[2] + "." + serverInfo.versionNatNet[3] + ")");
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

			scene.actors = actors.ToArray();
			scene.devices = devices.ToArray();

			InvalidateActorListeners();
			return true;
		}


		private void ParseMarkerset(NatNetPacket_In packet, List<Actor> actors)
		{
			int    id   = 0;                  // no ID for markersets
			string name = packet.GetString(); // markerset name
			Actor actor = new Actor(id, name);

			int nMarkers = packet.GetInt32();  // marker count
			// TODO: Sanity check on the number before allocating that much space
			actor.markers = new Marker[nMarkers];
			for ( int markerIdx = 0 ; markerIdx < nMarkers ; markerIdx++ )
			{
				name = packet.GetString();
				Marker marker = new Marker(name);
				actor.markers[markerIdx] = marker;
			}
			actors.Add(actor);
		}


		private void ParseRigidBody(NatNetPacket_In packet, List<Actor> actors)
		{
			string name = packet.GetString(); // name, TODO: No name in major version < 2
			int    id   = packet.GetInt32();  // ID
			Bone bone = new Bone(id, name);

			// rigid body name should be equal to actor name: search
			Actor actor = null;
			foreach (Actor a in actors)
			{
				if ( a.name.CompareTo(bone.name) == 0 )
				{
					actor = a;
				}
			}
			if ( actor == null )
			{
				Debug.LogWarning("Rigid Body " + bone.name + " could not be matched to an actor.");
				actor = new Actor(bone.id, bone.name);
				actors.Add(actor);
			}

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
				actor = new Actor(skeletonId, skeletonName);
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
				Bone bone = new Bone(id, name);

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
			int    id     = packet.GetInt32();    // force plate ID
			String name   = packet.GetString();   // force plate serial #
			Device device = new Device(id, name); // create device

			// skip next 652 bytes 
			// (SDK 2.9 sample code does not explain what this is about)
			packet.Skip(652);

			int nChannels = packet.GetInt32(); // channel count
			device.channels = new Channel[nChannels];
			for (int channelIdx = 0; channelIdx < nChannels; channelIdx++)
			{
				name = packet.GetString();
				Channel channel = new Channel(name);
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
					// Read position and convert from the MoCap right-handed coordinate system 
					// into Unity's left-handed coordinates
					marker.px =  packet.GetFloat();
					marker.py =  packet.GetFloat();
					marker.pz = -packet.GetFloat();
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

				// Read pos/rot and convert from the MoCap right-handed coordinate system 
				// into Unity's left-handed coordinates
				bone.px =  packet.GetFloat(); // position
				bone.py =  packet.GetFloat();
				bone.pz = -packet.GetFloat(); 
				bone.qx = -packet.GetFloat(); // rotation
				bone.qy = -packet.GetFloat();
				bone.qz =  packet.GetFloat();
				bone.qw =  packet.GetFloat();

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
			if ( includesSkeletonData )
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

						// Read pos/rot and convert from the MoCap right-handed coordinate system 
						// into Unity's left-handed coordinates
						bone.px =  packet.GetFloat(); // position
						bone.py =  packet.GetFloat();
						bone.pz = -packet.GetFloat();
						bone.qx = -packet.GetFloat(); // rotation
						bone.qy = -packet.GetFloat();
						bone.qz =  packet.GetFloat();
						bone.qw =  packet.GetFloat();

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
						for (int i = 0; i < nChannels; i++)
						{
							int nFrames = packet.GetInt32();
							for (int frameIdx = 0; frameIdx < nFrames; frameIdx++)
							{
								float value = packet.GetFloat();
								if (frameIdx < device.channels.Length)
								{
									device.channels[i].value = value;
								}
							}
						}
					}
				}

				// read latency and convert from s to ms
				scene.latency = (int)(packet.GetFloat() * 1000);
			}

			NotifyActorListeners();

			return true;
		}


		public Scene GetScene()
		{
			return scene;
		}


		private void NotifyActorListeners()
		{
			List<ActorListener> keys = new List<ActorListener>(actorListeners.Keys);
			foreach ( ActorListener listener in keys )
			{
				// which actor is that?
				Actor actor = actorListeners[listener];
				if ( actor == REFRESH_ACTOR )
				{
					// scene has been refreshed -> seek actor again by name
					actor = scene.FindActor(listener.GetActorName());
					actorListeners[listener] = actor;
				}
				if ( actor != null )
				{
					listener.ActorChanged(actor);
				}
			}
		}


		private void InvalidateActorListeners()
		{
			List<ActorListener> keys = new List<ActorListener>(actorListeners.Keys);
			foreach ( ActorListener listener in keys )
			{
				// force all listeners to refresh their actors next time?
				actorListeners[listener] = REFRESH_ACTOR;
			}
		}


		private string           clientAppName;
		private byte[]           clientAppVersion;
		private byte[]           clientNatNetVersion;
		private UdpClient        commandClient, dataClient;
		private NatNetPacket_In  packetIn;
		private NatNetPacket_Out packetOut;
		private ServerInfo       serverInfo;
		private bool             connected;
		private Scene            scene;

		private static Actor  REFRESH_ACTOR = new Actor(0, "refresh");
		private static Marker DUMMY_MARKER  = new Marker("dummy");
		private static Bone   DUMMY_BONE    = new Bone(0, "dummy");
		private static Device DUMMY_DEVICE  = new Device(0, "dummy");

		private Dictionary<ActorListener, Actor> actorListeners;

	}
}
