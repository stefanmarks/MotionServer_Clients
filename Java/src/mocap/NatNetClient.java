package mocap;

import java.io.IOException;
import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.InetAddress;
import java.net.MulticastSocket;
import java.net.SocketException;
import java.net.SocketTimeoutException;
import java.net.UnknownHostException;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.HashMap;
import java.util.LinkedList;
import java.util.List;
import java.util.Map;
import java.util.logging.Level;
import java.util.logging.Logger;

/**
 * Class for connecting to a NatNet compatible MoCap server.
 * 
 * @author  Stefan Marks
 */
public class NatNetClient implements MoCapClient
{
    public static final String CLIENT_NAME      = "Java MoCap Client";
    public static final byte   CLIENT_VERSION[] = { 1, 0, 1, 0 };
    public static final byte   NATNET_VERSION[] = { 2, 9, 0, 0 };
        
    
    /**
     * Class for storing the server information.
     */
    public class ServerInfo
    {
        public String serverName;
        public byte[] versionServer;
        public byte[] versionNatNet;
    }
    
    // Portnumbers: Default is 1510/1511, 
    // but that seems to collide with Cortex.
    // 1503 is taken by Windows messenger, 
    // 1512 is taken by WINS
    // -> so let's use 1508, 1509
    private final static int PORT_COMMAND = 1508; 
    private final static int PORT_DATA    = 1509;

    // timeout values for initial connections and for streaming
    private final int TIMEOUT_INITIAL = 1000;
    private final int TIMEOUT_RUNNING = 100;

    private final static short NAT_PING                 = 0;
    private final static short NAT_PINGRESPONSE         = 1;
    private final static short NAT_REQUEST              = 2;
    private final static short NAT_RESPONSE             = 3;
    private final static short NAT_REQUEST_MODELDEF     = 4;
    private final static short NAT_MODELDEF             = 5;
    private final static short NAT_REQUEST_FRAMEOFDATA  = 6;
    private final static short NAT_FRAMEOFDATA          = 7;
    private final static short NAT_MESSAGESTRING        = 8;
    private final static short NAT_UNRECOGNIZED_REQUEST = 100;
    
    private final static short DATASET_TYPE_MARKERSET  = 0;
    private final static short DATASET_TYPE_RIGIDBODY  = 1;
    private final static short DATASET_TYPE_SKELETON   = 2;
    private final static short DATASET_TYPE_FORCEPLATE = 3;
    
    private final static int   MAX_NAMELENGTH = 256;
    private final static int   MAX_PACKETSIZE = 10000;
    
    private final Command COMMAND_FRAMEOFDATA = new Command_RequestFrameOfData();
    private final Command COMMAND_MODELDEF    = new Command_RequestModelDefinition();
        
    private interface Command  { void marshal(ByteBuffer buffer);   }
    private interface Response {  }
    
    
    /**
     * Class for a command to ping the server.
     */
    private class Command_Ping implements Command
    {
        public Command_Ping(String clientName, byte[] clientVersion)
        {
            this.clientName = clientName;
            this.versionApp = new byte[4];
            for ( int i = 0 ; i < this.versionApp.length ; i++ )
            {
                if ( i < clientVersion.length )
                {
                    versionApp[i] = clientVersion[i];
                }
                else
                {
                    versionApp[i] = 0;
                }
            }
        }

        private final String clientName;
        private final byte   versionApp[];
        
        @Override
        public void marshal(ByteBuffer buf)
        {
            buf.rewind();
            buf.putShort((short) NAT_PING);
            buf.putShort((short) 0); // length to be filled in later
            buf.put(clientName.getBytes()).putChar('\0'); // client name
            for ( int i = clientName.length() + 2 ; i < MAX_NAMELENGTH ; i++ )
            {
                // pad with 0 to full name length
                buf.put((byte) 0);
            }
            // version information
            for (int i = 0; i < 4; i++) { buf.put(versionApp[i]); }
            for (int i = 0; i < 4; i++) { buf.put(NATNET_VERSION[i]); }
        }
    }
    
    
    /**
     * Class for a ping response from the server.
     */
    private class Response_Ping implements Response
    {
        private Response_Ping(ByteBuffer buf, ServerInfo info)
        {
            info.serverName = unmarshalString(buf);
            for ( int i = info.serverName.length() + 1 ; i < MAX_NAMELENGTH ; i++ )
            {
                // skip rest of maximum string length 
                buf.get();
            }
            info.versionServer = new byte[4];
            info.versionNatNet = new byte[4];
            for ( int i = 0 ; i < 4 ; i++ ) { info.versionServer[i] = buf.get(); }
            for ( int i = 0 ; i < 4 ; i++ ) { info.versionNatNet[i] = buf.get(); }
        }
    }
    
    
    /**
     * Class for a command to request the model definition.
     */
    private class Command_RequestModelDefinition implements Command
    {
        private Command_RequestModelDefinition()
        {
            // nothing to do here
        }

        
        @Override
        public void marshal(ByteBuffer buf)
        {
            buf.rewind();
            buf.putShort(NAT_REQUEST_MODELDEF);
            buf.putShort((short) 0);
        }
    }


    /**
     * Class for a response with a model definition.
     */
    private class Response_ModelDefinition implements Response
    {
        private Response_ModelDefinition(ByteBuffer buf, Scene scene)
        {
            logBufferData(buf, buf.remaining());
            int nDatasets = buf.getInt(); // datasets
            List<Actor>  actors = new LinkedList<>();
            List<Device> devices = new LinkedList<>();
            for ( int datasetIdx = 0 ; datasetIdx < nDatasets ; datasetIdx++ )
            {
                int datasetType = buf.getInt();
                switch ( datasetType )
                {
                    case DATASET_TYPE_MARKERSET : 
                    {
                        parseMarkerset(buf, actors);
                        break;
                    }
                    
                    case DATASET_TYPE_RIGIDBODY : 
                    {
                        parseRigidBody(buf, actors); 
                        break;
                    }
                    
                    case DATASET_TYPE_SKELETON : 
                    {
                        parseSkeleton(buf, actors);
                        break;
                    } 
                    
                    case DATASET_TYPE_FORCEPLATE : 
                    {
                        parseForcePlate(buf, devices); 
                        break;
                    }
                    
                    default: 
                    {
                        LOG.log(Level.WARNING, 
                                "Invalid dataset type {0} in model definition respose.",
                                datasetType);
                        break;
                    }                    
                }
            }
            
            synchronized(scene)
            {
                scene.actors  = actors.toArray(new Actor[actors.size()]);
                scene.devices = devices.toArray(new Device[devices.size()]);
            }
            
            // scene might have changed -> update listeners
            invalidateActorListeners();
        }
        
        
        private void parseMarkerset(ByteBuffer buf, List<Actor> actors)
        {
            Actor actor    = new Actor(); 
            actor.id       = 0;                    // no ID for markersets
            actor.name     = unmarshalString(buf); // markerset name
            int nMarkers   = buf.getInt();         // marker count
            actor.markers  = new Marker[nMarkers];
            for ( int markerIdx = 0 ; markerIdx < nMarkers ; markerIdx++ )
            {   
                String name   = unmarshalString(buf);
                Marker marker = new Marker(name); 
                actor.markers[markerIdx] = marker;
            }
            actors.add(actor);
        }
        
                    
        private void parseRigidBody(ByteBuffer buf, List<Actor> actors)
        {
            Bone bone  = new Bone();
            bone.name  = unmarshalString(buf); // name
            bone.id    = buf.getInt();         // ID

            // rigid body name should be equal to actor name: search
            Actor actor = null;
            for ( Actor a : actors )
            {
                if ( a.name.equals(bone.name) )
                {
                    actor = a;
                    break;
                }
            }
            if ( actor == null )
            {
                // names don't match > try IDs
                if ( (bone.id >=0) && (bone.id < actors.size()) )
                {
                    actor = actors.get(bone.id);
                }
            }
            if ( actor == null )
            {
                LOG.log(Level.WARNING, "Rigid Body {0} could not be matched to an actor.", bone.name);
                actor = new Actor();
                actors.add(actor);
                actor.name = bone.name;
            }
            
                           buf.getInt();         // Parent ID (ignore for rigid body)
            bone.parent  = null;                 // rigid bodies should not have a parent
            bone.ox      = buf.getFloat();       // X offset
            bone.oy      = buf.getFloat();       // Y offset
            bone.oz      = buf.getFloat();       // Z offset
            
            actor.bones    = new Bone[1];
            actor.bones[0] = bone;
            actor.id       = bone.id;
        }
        
        
        private void parseSkeleton(ByteBuffer buf, List<Actor> actors)
        {
            final boolean includesBoneNames = // starting at v2.0
                    (serverInfo.versionNatNet[0] >= 2);

            String skeletonName = unmarshalString(buf); // name
            int    skeletonId   = buf.getInt();         // ID
            
            // rigid body name should be equal to actor name: search
            Actor actor = null;
            for ( Actor a : actors )
            {
                if ( a.name.equals(skeletonName) )
                {
                    actor = a;
                }
            }
            if ( actor == null )
            {
                // names don't match > try IDs
                if ( (skeletonId >=0) && (skeletonId < actors.size()) )
                {
                    actor = actors.get(skeletonId);
                }
            }
            if ( actor == null )
            {
                LOG.log(Level.WARNING, "Skeleton {0} could not be matched to an actor.", skeletonName);
                actor = new Actor();
                actors.add(actor);
                actor.name = skeletonName;
            }
            actor.id = skeletonId;

            int nBones  = buf.getInt(); // Skeleton bone count
            actor.bones = new Bone[nBones];
            for ( int boneIdx = 0 ; boneIdx < nBones ; boneIdx++ )
            {
                Bone bone = new Bone();
                
                if ( includesBoneNames )
                {
                    bone.name = unmarshalString(buf); // Bone name
                }

                bone.id     = buf.getInt();                 // Bone ID
                bone.parent = actor.findBone(buf.getInt()); // Skeleton parent ID
                // update child list
                if ( bone.parent != null )
                {
                    bone.parent.children.add(bone);
                }
                
                bone.buildChain(); // build chain from root to this bone
                
                bone.ox = buf.getFloat(); // X offset
                bone.oy = buf.getFloat(); // Y offset
                bone.oz = buf.getFloat(); // Z offset

                actor.bones[boneIdx] = bone;
            }
        }
        
        private void parseForcePlate(ByteBuffer buf, List<Device> devices)
        {
            int    id     = buf.getInt();         // force plate ID
            String name   = unmarshalString(buf); // force plate serial #
            Device device = new Device(id, name); 
            
            // skip next 652 bytes 
            // (SDK 2.9 sample code does not explain what this is about)
            buf.position(buf.position() + 652); 
            
            int nChannels = buf.getInt(); // channel count
            device.channels = new Channel[nChannels];
            for ( int channelIdx = 0 ; channelIdx < nChannels ; channelIdx++ )
            {   
                name = unmarshalString(buf);
                Channel channel = new Channel(name); 
                device.channels[channelIdx] = channel;
            }
            devices.add(device);
        }
    }
    
    
    /**
     * Class for a command to request the latest frame of data.
     */
    private class Command_RequestFrameOfData implements Command
    {
        private Command_RequestFrameOfData()
        {
            // nothing to do here
        }

        
        @Override
        public void marshal(ByteBuffer buf)
        {
            buf.rewind();
            buf.putShort(NAT_REQUEST_FRAMEOFDATA);
            buf.putShort((short) 0);
        }
    }

    
    /**
     * Class for a response with the latest frame of data.
     */
    private class Response_FrameOfData implements Response
    {
        private Response_FrameOfData(ByteBuffer buf, Scene scene)
        {
            // determine special datasets depending on NatNet version
            final boolean includesMarkerIDsAndSizes = // starting at v2.0
                    (serverInfo.versionNatNet[0] >= 2);
            final boolean includesSkeletonData = // starting at v2.1
                    ( (serverInfo.versionNatNet[0] == 2) &&
                      (serverInfo.versionNatNet[1] >= 1) ) ||
                    (serverInfo.versionNatNet[0] > 2);
            final boolean includesTrackingState = //  starting at v2.6
                    ( (serverInfo.versionNatNet[0] == 2) &&
                      (serverInfo.versionNatNet[1] >= 6) ) ||
                    (serverInfo.versionNatNet[0] > 2);
            final boolean includesLabelledMarkers = //  starting at v2.3
                    ( (serverInfo.versionNatNet[0] == 2) &&
                      (serverInfo.versionNatNet[1] >= 3) ) ||
                    (serverInfo.versionNatNet[0] > 2);
            final boolean includesLabelledMarkerFlags = //  starting at v2.6
                    ( (serverInfo.versionNatNet[0] == 2) &&
                      (serverInfo.versionNatNet[1] >= 6) ) ||
                    (serverInfo.versionNatNet[0] > 2);
            final boolean includesForcePlateData = // starting at v2.9
                    ( (serverInfo.versionNatNet[0] == 2) &&
                      (serverInfo.versionNatNet[1] >= 9) ) ||
                    (serverInfo.versionNatNet[0] > 2);

            synchronized(scene)
            {
                int frameNumber = buf.getInt(); // frame number
                // check if this is a newer frame
                // delta < 10: but do consider looping playback 
                // when frame numbers suddenly differ significantly
                int deltaFrame = frameNumber - scene.frameNumber;
                if ( (deltaFrame < 0) && (deltaFrame > -10) ) return; // old frame, get out

                logBufferData(buf, 400);
                scene.frameNumber = frameNumber;

                // Read actor data
                int nActors = buf.getInt(); // actor count
                for ( int actorIdx = 0 ; actorIdx < nActors ; actorIdx++ )
                {
                    String actorName = unmarshalString(buf);
                    // find the corresponding actor
                    Actor actor = scene.findActor(actorName);

                    int nMarkers = buf.getInt();
                    for ( int markerIdx = 0 ; markerIdx < nMarkers ; markerIdx++ )
                    {
                        Marker marker = (actor != null) ? actor.markers[markerIdx] : DUMMY_MARKER;
                        marker.px = buf.getFloat();
                        marker.py = buf.getFloat();
                        marker.pz = buf.getFloat();
                    }        
                }

                // skip unidentified marker data
                int nUnidentifiedMarkers = buf.getInt();
                final int unidentifiedMarkerDataSize = 3 * 4; // 3 floats
                buf.position(buf.position() + unidentifiedMarkerDataSize * nUnidentifiedMarkers);
                // without skipping:
                // for ( int idx = 0 ; idx < nUnknownMarkers ; idx++ )
                // {
                //     buf.getFloat(); // x
                //     buf.getFloat(); // y
                //     buf.getFloat(); // z
                // }
                
                // Read rigid body data
                int nRigidBodies = buf.getInt(); // bone count
                for ( int rigidBodyIdx = 0 ; rigidBodyIdx < nRigidBodies ; rigidBodyIdx++ )
                {
                    int rigidBodyID = buf.getInt(); // get rigid body ID

                    // find the corresponding actor
                    Bone bone = DUMMY_BONE;
                    if ( checkActorId(rigidBodyID) )
                    {
                        Actor actor = scene.actors[rigidBodyID];
                        if ( actor.bones.length == 0 )
                        {
                            // in case there is no bone, create one
                            actor.bones = new Bone[1];
                            actor.bones[0] = new Bone();
                        }
                        bone = actor.bones[0];
                    }

                    bone.px = buf.getFloat(); // position
                    bone.py = buf.getFloat();
                    bone.pz = buf.getFloat();
                    bone.qx = buf.getFloat(); // rotation
                    bone.qy = buf.getFloat();
                    bone.qz = buf.getFloat();
                    bone.qw = buf.getFloat();

                    int nMarkers = buf.getInt();
                    for ( int i = 0 ; i < nMarkers ; i++ )
                    {
                        buf.getFloat(); // Marker X
                        buf.getFloat(); // Marker Y
                        buf.getFloat(); // Marker Z
                    }
                    if ( includesMarkerIDsAndSizes  )
                    {
                        // also, marker IDs and sizes
                        for ( int i = 0 ; i < nMarkers ; i++ )
                        {
                            buf.getInt(); // Marker ID
                        } 
                        // and sizes
                        for ( int i = 0 ; i < nMarkers ; i++ )
                        {
                            buf.getFloat(); // Marker size
                        } 

                        buf.getFloat(); // mean error
                    }
                    
                    // Tracking state
                    if ( includesTrackingState )
                    {
                        buf.getShort();
                        // 0x01 : rigid body was successfully tracked in this frame
                    }
                }

                // Read skeleton data
                if ( includesSkeletonData )
                {
                    int nSkeletons = buf.getInt();
                    for ( int skeletonIdx = 0 ; skeletonIdx < nSkeletons ; skeletonIdx++ )
                    {
                        // read skeleton ID and find actor
                        int skeletonId = buf.getInt();
                        Actor actor = scene.findActor(skeletonId);
                        if ( actor == null ) return;
                        
                        // # of bones in skeleton
                        int nBones = buf.getInt();

                        for ( int nBodyIdx = 0 ; nBodyIdx < nBones ; nBodyIdx++ ) 
                        { 
                            // read bone ID and find bone
                            int boneId = buf.getInt();
                            Bone bone = actor.findBone(boneId);
                            if ( bone == null ) bone = DUMMY_BONE;
                            
                            bone.px = buf.getFloat(); // read position
                            bone.py = buf.getFloat();
                            bone.pz = buf.getFloat();
                            bone.qx = buf.getFloat(); // read orientation
                            bone.qy = buf.getFloat();
                            bone.qz = buf.getFloat();
                            bone.qw = buf.getFloat();

                            // read/skip rigid marker data
                            int nMarkers = buf.getInt();
                            for ( int i = 0 ; i < nMarkers ; i++ )
                            {
                                buf.getFloat(); // X/Y/Z position
                                buf.getFloat();
                                buf.getFloat();
                            }       
                            for ( int i = 0 ; i < nMarkers ; i++ )
                            {
                                buf.getInt(); // Marker IDs
                            }       
                            for ( int i = 0 ; i < nMarkers ; i++ )
                            {
                                buf.getFloat(); // Marker size
                            }       

                            // Mean marker error
                            buf.getFloat();

                            // Tracking state
                            if ( includesTrackingState )
                            {
                                buf.getShort();
                                // 0x01 : rigid body was successfully tracked in this frame
                            }

                        } // next rigid body
                    } // next skeleton 
                }
                
                // skip labelled markers 
		if ( includesLabelledMarkers )
		{
                    int nLabelledMarkers = buf.getInt();
                    final int labelledMarkerDataSize = 
                            includesLabelledMarkerFlags ? 
                                5 * 4 + 1 * 2 : // 1 int, 4 floats, 1 short
                                5 * 4; // 1 int, 4 floats
                    buf.position(buf.position() + nLabelledMarkers * labelledMarkerDataSize);
                    // without skipping:
                    // for ( int markerIdx = 0; markerIdx  < nLabeledMarkers; markerIdx++ )
                    // {
                    //     int   id   = buf.getInt();
                    //     float x    = buf.getFloat();
                    //     float y    = buf.getFloat();
                    //     float z    = buf.getFloat();
                    //     float size = buf.getFloat();

                    //     if ( includesLabelledMarkerFlags ) 
                    //     {
                    //         short params = buf.getShort();
                    //     }
                    // }
		}

                // read force plate data
                if ( includesForcePlateData )
                {   
                    int nForcePlates = buf.getInt();
                    for (int forcePlateIdx = 0; forcePlateIdx < nForcePlates; forcePlateIdx++)
                    {
                        // read force plate ID and find corresponding device
                        int forcePlateId = buf.getInt();
                        Device device = scene.findDevice(forcePlateId);
                        if ( device == null ) device = DUMMY_DEVICE;

                        // channel count
                        int nChannels = buf.getInt();
                        // channel data
                        for (int i = 0; i < nChannels; i++)
                        {
                            int nFrames = buf.getInt(); 
                            for (int frameIdx = 0; frameIdx < nFrames; frameIdx++)
                            {
                                float value = buf.getFloat();
                                if ( frameIdx < device.channels.length )
                                {
                                    device.channels[i].value = value;
                                }
                            }
                        }
                    }
                }
                
                // read latency and convert from s to ms
                scene.latency = (int) (buf.getFloat() * 1000);
            }
            
            notifyActorListeners();
        }
    }
    
    
    /**
     * Class for a custom request command.
     */
    private class Command_Request implements Command
    {
        private Command_Request(String request)
        {
            this.request = request;
        }

        
        @Override
        public void marshal(ByteBuffer buf)
        {
            buf.rewind();
            buf.putShort(NAT_REQUEST);
            buf.putShort((short) 0);
            buf.put(request.getBytes()).putChar('\0');
        }
        
        private final String request;
    }

    
    /**
     * Class for a request response from the server.
     */
    private class Response_Request implements Response
    {
        private Response_Request(ByteBuffer buf)
        {
            returnValue = unmarshalString(buf);
        }
        
        /**
         * Gets the string returned by the request.
         * 
         * @return the request return value
         */
        public String getValue()
        {
            return returnValue;
        }
        
        private final String returnValue;
    }
    
    
    /**
     * Class for a ping response from the server.
     */
    private class Response_UnrecognizedRequest implements Response
    {
        private Response_UnrecognizedRequest()
        {
            // nothing to do here
        }
    }
    
    
    /**
     * Creates a Natnet compatible Motion Capture client.
     */
    public NatNetClient()
    {
        this(CLIENT_NAME, CLIENT_VERSION);
    }

    /**
     * Creates a Natnet compatible Motion Capture client.
     * 
     * @param applicationName     the name of the application
     * @param applicationVersion  the version number of the application (array of max. size 4)
     */
    public NatNetClient(String applicationName, byte[] applicationVersion)
    {
        this.appName    = applicationName.substring(0, Math.min(128, applicationName.length()));
        this.appVersion = applicationVersion;
        
        this.connected      = false;
        this.frameStreaming = false;
        
        this.scene = new Scene();
        
        this.cmdSocket  = null;
        this.packetOut  = new DatagramPacket(new byte[MAX_PACKETSIZE], MAX_PACKETSIZE);
        this.packetIn   = new DatagramPacket(new byte[MAX_PACKETSIZE], MAX_PACKETSIZE);
        this.bufOut     = ByteBuffer.allocate(MAX_PACKETSIZE).order(ByteOrder.LITTLE_ENDIAN);
        this.serverInfo = new ServerInfo();   
        
        this.actorListeners = new HashMap<>();
    }
    
    
    @Override
    public boolean connect(InetAddress host)
    {
        if ( connected )
        {
            disconnect();
        }
        
        try
        {
            cmdSocket = new DatagramSocket();
            cmdSocket.connect(host, PORT_COMMAND);
            cmdSocket.setSoTimeout(100);
            packetOut.setAddress(null); // make packet neutral
            
            Response_Ping ping = pingServer();
            if ( ping != null )
            {
                connected = true;
                LOG.log(Level.INFO, "Connected to server ''{0}'' v{1}.{2}.{3}.{4}, NatNet v{5}.{6}.{7}.{8}", 
                        new Object[]{
                            serverInfo.serverName,
                            serverInfo.versionServer[0], serverInfo.versionServer[1], serverInfo.versionServer[2], serverInfo.versionServer[3],
                            serverInfo.versionNatNet[0], serverInfo.versionNatNet[1], serverInfo.versionNatNet[2], serverInfo.versionNatNet[3]
                        });
                
                // trigger sending of scene description and the first frame
                sendCommandPacket(COMMAND_MODELDEF);
                receiveResponsePacket(Response_ModelDefinition.class);
                sendCommandPacket(COMMAND_FRAMEOFDATA);
                receiveResponsePacket(Response_FrameOfData.class);
                
                // get data stream source address
                InetAddress dataStreamAddr    = host;
                String      strDataStreamAddr = sendCommand("getDataStreamAddress");
                try
                {
                    dataStreamAddr = InetAddress.getByName(strDataStreamAddr);
                }
                catch (UnknownHostException e)
                {
                    LOG.log(Level.WARNING, 
                            "Could not resolve data stream address ''{0}''", 
                            strDataStreamAddr);
                }
                LOG.log(Level.INFO, 
                        "Server data stream address: {0} {1}", 
                        new Object[] {
                            dataStreamAddr, 
                            dataStreamAddr.isMulticastAddress() ? "(multicast)" : ""
                        });

                // request first frame of data
                frameStreaming = false;
                sendCommandPacket(COMMAND_FRAMEOFDATA);
                receiveResponsePacket(Response_FrameOfData.class);
                
                // start stream receiver thread
                receiverThread = new ReceiverThread(dataStreamAddr);
                receiverThread.start();
            }
            else
            {
                cmdSocket.close();
                cmdSocket = null;
            }
        }
        catch (IllegalArgumentException | SocketException e)
        {
            if ( cmdSocket == null )
            {
                LOG.severe("Could not create socket.");
            }
            else
            {
                LOG.log(Level.SEVERE, "Could not connect to server ({0}).", e.getMessage());
            }
            cmdSocket = null;                
        }
        
        return connected;
    }

    
    @Override
    public boolean isConnected()
    {
        return connected;
    }
    
    
    @Override
    public String getServerName()
    {
        return serverInfo.serverName + " v" +
               serverInfo.versionServer[0] + "." + serverInfo.versionServer[1] + "." +
               serverInfo.versionServer[2] + "." + serverInfo.versionServer[3];
    }
    
    
    @Override
    public void update()
    {
        // only poll "manually" when streaming does not work
        // (for whatever reason)
        if ( connected && !frameStreaming ) 
        {
            sendCommandPacket(COMMAND_FRAMEOFDATA);
            receiveResponsePacket(Response_FrameOfData.class);
        }
    }
    
    
    @Override
    public final Scene getScene()
    {
        return scene;
    }
    
    
    @Override
    public String sendCommand(String command)
    {
        String retVal = null;
        sendCommandPacket(new Command_Request(command));
        Response response = receiveResponsePacket(Response_Request.class);
        if ( response != null )
        {
            retVal = ((Response_Request) response).getValue();
        }
        return retVal;
    }
    
    
    @Override
    public boolean disconnect()
    {
        if ( connected )
        {
            if ( receiverThread != null )
            {
                receiverThread.terminate();
                try
                {
                    receiverThread.join(1000);
                }
                catch (InterruptedException e)
                {
                    // ignore
                }
                receiverThread = null;
            }
            
            cmdSocket.disconnect();
            cmdSocket.close();
            cmdSocket = null;
            
            connected = false;
        }
        return !connected;
    }
    
    
    @Override
    public boolean addActorListener(ActorListener listener)
    {
        boolean added = false;
        if ( !actorListeners.containsKey(listener) )
        {
            Actor actor = getScene().findActor(listener.getActorName());
            actorListeners.put(listener, actor);
            added = true;
        }
        return added;
    }
    
    
    @Override
    public boolean removeActorListener(ActorListener listener)
    {
        boolean removed = false;
        if ( actorListeners.containsKey(listener) )
        {
            actorListeners.remove(listener);
            removed = true;
        }
        return removed;
    }
    
    
    private boolean checkActorId(int actorId)
    {
        boolean valid = (actorId >= 0) && (actorId < scene.actors.length);
        if ( !valid ) 
        {
            LOG.log(Level.WARNING, "Invalid actor ID {0}", actorId);
        }
        return valid;
    }
    
    
    private boolean checkBoneId(int actorId, int boneId)
    {
        boolean valid = (boneId >= 0) && (boneId < scene.actors[actorId].bones.length);
        if ( !valid ) 
        {
            LOG.log(Level.WARNING, "Invalid bone ID {0}", boneId);
        }
        return valid;
    }

    
    private Response_Ping pingServer()
    {
        Response_Ping result = null;
        if ( cmdSocket != null )
        {
            if ( sendCommandPacket(new Command_Ping(appName, appVersion)) )
            {
                result = (Response_Ping) receiveResponsePacket(Response_Ping.class);        
            }            
        }
        return result;
    }

    
    private boolean sendCommandPacket(Command cmd)
    {
        boolean success = false;
        
        cmd.marshal(bufOut);
        int len = bufOut.position(); 
        bufOut.putShort(2, (short) (len - 4)); // adapt length of data packet (less id and packet size)
        // dump(bufOut, len);
        packetOut.setLength(len);
        packetOut.setData(bufOut.array(), 0, len);
        try
        {
            cmdSocket.send(packetOut);
            success = true;
        }
        catch (IOException e)
        {
            LOG.log(Level.SEVERE, "Could not send command ({0}).", e.getMessage());
        }
        return success;
    }
    
    
    private Response receiveResponsePacket(Class c)
    {
        Response response = null;
        try
        {
            do
            {
                cmdSocket.receive(packetIn);
                response = parsePacket(packetIn);
                errorCounter = 0;
            } while ( !c.isInstance(response) );
        }
        catch (IOException e)
        {
            if ( errorCounter == 0 )
            {
                LOG.log(Level.SEVERE, "Could not receive command ({0}).", e.getMessage());
            }
            errorCounter++;
            if ( errorCounter > 30 )
            {
                LOG.log(Level.SEVERE, "Too many errors > disconnecting.");
                disconnect();
            }
        }
        return response;
    }
   
    
    private Response parsePacket(DatagramPacket packet)
    {
        Response response = null;
        int rcvLength = packet.getLength();
        if ( rcvLength > 0 )
        {
            final ByteBuffer bufIn = ByteBuffer.wrap(packet.getData(), 0, rcvLength).order(ByteOrder.LITTLE_ENDIAN);
            logBufferData(bufIn, rcvLength);
            int packetId    = bufIn.getShort();
            int packetLen   = bufIn.getShort();
            int receivedLen = rcvLength; 
            if ( packetLen == receivedLen - 4 ) // don't count the 4 bytes id and length
            {
                switch ( packetId )
                {
                    case NAT_PINGRESPONSE :
                    {
                        response = new Response_Ping(bufIn, serverInfo);
                        break;
                    }

                    case NAT_MODELDEF :
                    {
                        response = new Response_ModelDefinition(bufIn, scene);
                        break;
                    }

                    case NAT_FRAMEOFDATA :
                    {
                        response = new Response_FrameOfData(bufIn, scene);
                        break;
                    }

                    case NAT_RESPONSE :
                    {
                        response = new Response_Request(bufIn);
                        break;
                    }
                    
                    case NAT_UNRECOGNIZED_REQUEST :
                    {
                        response = new Response_UnrecognizedRequest();
                        LOG.log(Level.WARNING, "Unrecognized request.");
                        break;
                    }

                    default:
                    {
                        LOG.log(Level.WARNING, "Unknown packet ID {0}.", packetId);
                        break;
                    }
                }
            }
            else
            {
                LOG.log(Level.WARNING, 
                        "Incoming packet length error (ID={0}, Packet Length={1}, Received Length={2}).", 
                        new Object[]{packetId, packetLen, receivedLen});
            }
        }
        return response;
    }
    
    
    private String unmarshalString(ByteBuffer buf)
    {
        StringBuilder s = new StringBuilder();
        char c;
        while ( (c = (char) buf.get()) != '\0' ) { s.append(c); }
        return s.toString();
    }
    
    
    private void notifyActorListeners()
    {
        for ( Map.Entry<ActorListener, Actor> listener : actorListeners.entrySet())
        {
            // which actor is that?
            Actor actor = listener.getValue();
            if ( actor == REFRESH_ACTOR )
            {
                // scene has been refreshed -> seek actor again by name
                actor = scene.findActor(listener.getKey().getActorName());
                listener.setValue(actor);
            }
            if ( actor != null )
            {
                listener.getKey().actorChanged(actor);
            }
        }
    }
    
    
    private void invalidateActorListeners()
    {
        for ( Map.Entry<ActorListener, Actor> listener : actorListeners.entrySet())
        {
            // force all listeners to refresh their actors next time?
            listener.setValue(REFRESH_ACTOR);
        }
    }
    
    
    private void logBufferData(ByteBuffer buf, int len)
    {
        if ( LOG.isLoggable(Level.FINE) )
        {
            String address   = "";
            String hexData   = "";
            String asciiData = "";

            byte[] arr = buf.array();
                  int idx   = 0;
            final int width = 16;
            while ( idx < len )
            {
                if ( idx % width == 0 )
                {
                    address = String.format("%04x", idx);
                    hexData = "";
                    asciiData = "";
                }

                byte d = arr[idx];
                hexData   += String.format("%02x ", d);
                asciiData += (d >= 32) && (d < 127) ? (char) d : ".";
                idx++;

                if ( (idx == len) || (idx % width == 0) )
                {
                    for ( int i = hexData.length() ; i < width * 3 ; i++ ) { hexData += " "; }
                    LOG.log(Level.FINE, "{0} : {1}  |  {2}", 
                            new Object[]{address, hexData, asciiData});
                }
            }
        }
    }
    
    
    private class ReceiverThread extends Thread
    {
        public ReceiverThread(InetAddress dataStreamAddress)
        {
            try
            {
                if ( dataStreamAddress.isMulticastAddress() )
                {
                    MulticastSocket socket = new MulticastSocket(PORT_DATA);
                    socket.joinGroup(dataStreamAddress);
                    dataSocket = socket;
                }
                else
                {
                    dataSocket = new DatagramSocket(PORT_DATA, dataStreamAddress);
                }
                
                dataSocket.setSoTimeout(TIMEOUT_INITIAL);
                packetIn = new DatagramPacket(new byte[MAX_PACKETSIZE], MAX_PACKETSIZE);
            }
            catch (IOException e)
            {
                LOG.log(Level.SEVERE, 
                        "Could not start receiver thread ({0}).", 
                        e.getMessage());
            }
        }

        @Override
        public void run()
        {
            if ( dataSocket == null ) return;
            
            runReceiver = true;
            LOG.info("Receiver thread started");
            
            boolean firstPacketReceived = false;
            int     timeoutCounter      = 0;
            
            while ( runReceiver )
            {
                try
                {
                    dataSocket.receive(packetIn);
                    parsePacket(packetIn);
                    frameStreaming = true;
                    
                    if ( !firstPacketReceived )
                    {
                        LOG.info("Data stream active");
                        firstPacketReceived = true;
                        timeoutCounter = 0;
                        
                        // OK, data is coming in > set timeout to less
                        try
                        {
                            dataSocket.setSoTimeout(TIMEOUT_RUNNING);
                        }
                        catch (SocketException e)
                        {
                            // do nothing
                        }
                    }
                } 
                catch (SocketTimeoutException ex)
                {
                    if ( (timeoutCounter > 10) && frameStreaming )
                    {
                        // data was streaming (or is expected to)
                        frameStreaming = false;
                        if ( firstPacketReceived )
                        {
                            LOG.warning("Data stream stopped unexpectedly");
                        }
                        else
                        {
                            LOG.warning("No data stream detected");
                        }
                        
                        try
                        {
                            // try again with longer timeout
                            dataSocket.setSoTimeout(TIMEOUT_INITIAL);
                            firstPacketReceived = false;
                        }
                        catch (SocketException e)
                        {
                            // ignore
                        }
                    }
                }
                catch (IOException ex)
                {
                    // uh oh, panic, disconnect
                    runReceiver = false;
                    LOG.log(Level.SEVERE, 
                            "Error while receiving data stream ({0})", 
                            ex.getMessage());
                }
            }
            
            dataSocket.disconnect();
            dataSocket.close();
            dataSocket = null;
            LOG.info("Receiver thread stopped");
        }
        
        
        public void terminate()
        {
            if ( runReceiver )
            {
                LOG.info("Stopping receiver thread");
                runReceiver = false;
            }
        }
        
        private DatagramSocket  dataSocket;
        private DatagramPacket  packetIn;
        private boolean         runReceiver;
    }
    
    
    private final String          appName;
    private final byte[]          appVersion;
    private final Scene           scene;
    private       DatagramSocket  cmdSocket;
    private final DatagramPacket  packetIn, packetOut;
    private final ByteBuffer      bufOut;
    private       boolean         connected;
    private       int             errorCounter;
    private       boolean         frameStreaming;
    private final ServerInfo      serverInfo;
    private       ReceiverThread  receiverThread;

    private final HashMap<ActorListener, Actor> actorListeners;
    
    private final static Actor  REFRESH_ACTOR = new Actor();
    private final static Marker DUMMY_MARKER  = new Marker("dummy");
    private final static Bone   DUMMY_BONE    = new Bone();
    private final static Device DUMMY_DEVICE  = new Device(0, "dummy");
    
    private final static Logger LOG = Logger.getLogger(NatNetClient.class.getName());
}
