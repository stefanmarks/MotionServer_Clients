/**
 * Convert MoCap Actor bone positions and rotations into OSC packages.
 */
 
import java.util.*;
import java.net.*;
import oscP5.*;
import netP5.*;
import mocap.*;

final static String[] OSC_RECEIVERS = { "127.0.0.1" };
final static int      OSC_PORT      = 57120;

MoCapClient client;
OscP5       osc;

void setup()
{
  size(640, 480);
  frameRate(60);

  textSize(10);
  textAlign(LEFT, TOP);
  
  // disable the logging output
  java.util.logging.LogManager.getLogManager().reset();
  
  // print MoCap client version info
  println("MoCap client: " + NatNetClient.CLIENT_NAME +
    " v" + 
      NatNetClient.CLIENT_VERSION[0] + "." + NatNetClient.CLIENT_VERSION[1] + "." +
      NatNetClient.CLIENT_VERSION[2] + "." + NatNetClient.CLIENT_VERSION[3] +
    ", NatNet Version " + 
      NatNetClient.NATNET_VERSION[0] + "." + NatNetClient.NATNET_VERSION[1] + "." +
      NatNetClient.NATNET_VERSION[2] + "." + NatNetClient.NATNET_VERSION[3]
  );
    
  // create MoCap client
  //client = new NatNetClient(); // simple with default values
  client = new NatNetClient("Processing OSC Converter", new byte[] {1, 0}); // customising the client data
  
  // try to connect to server
  String[] serverAddresses = loadStrings("servers.txt");
  for ( String serverAddress : serverAddresses )
  {
    String address = serverAddress.trim();
    if ( address.startsWith("#") ) continue; // comment line
  
    try
    {
        InetAddress addr = InetAddress.getByName(address);
        println("Attempting to connect to MoCap server at " + addr);
        if ( client.connect(addr) )
        {
            break;
        }
    }
    catch (UnknownHostException e)
    {
        println("Could not resolve server address " + address);
    }
  }
  
  if ( client.isConnected() )
  {
    // create a new instance of oscP5 
    osc = new OscP5("10.1.1.180", OSC_PORT);
  }
  else
  {
    println("Could not connect to any MoCap server.");
    //exit();
  }
}


void draw()
{
  // prepare canvas
  background(0);
  int yPos = 0;
  
  // draw actors
  if ( client != null )
  {
    client.update();
    Scene scene = client.getScene();

    if ( osc != null )
    {
      // iterate through actors and bones
      for ( Actor actor : scene.actors )   // stream all actors
      //Actor actor = scene.actors[0];     // stream only first actor
      {
        //for ( Bone bone : actor.bones )  // stream all bones
        Bone bone = actor.bones[0];        // stream only root bone
        {
          String name = "/" + actor.name + "/" + bone.name + "/";
          OscBundle bundle = new OscBundle();
          
          // form OSC messages by adding XYZ position...
          OscMessage msgX = new OscMessage(name + "x"); msgX.add(bone.px);
          OscMessage msgY = new OscMessage(name + "y"); msgY.add(bone.py);
          OscMessage msgZ = new OscMessage(name + "z"); msgZ.add(bone.pz);
          // ...and bone rotation
          float[] axisAngle = MathUtil.getAxisAngle(bone);
          OscMessage msgA = new OscMessage(name + "a"); msgA.add(axisAngle[3]);
          // wrap messages in a bundle
          bundle.add(msgX); bundle.add(msgY); bundle.add(msgZ); bundle.add(msgA);

          text(name + " > " + bundle.toString(), 10, yPos, 0);
          yPos += 10;

          // and send/broadcast
          for ( String receiver : OSC_RECEIVERS )
          {
            NetAddress addr = new NetAddress(receiver, OSC_PORT);
            osc.send(bundle, addr);     
          }
        }
      }  
    }
  }
}