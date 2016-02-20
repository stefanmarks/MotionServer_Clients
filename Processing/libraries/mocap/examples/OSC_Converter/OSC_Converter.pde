/**
 * Convert MoCap Actor bone positions and rotations into OSC packages.
 *
 * @author Stefan Marks, AUT University, Auckland, NZ
 * @date   18.2.2016
 */
 
import java.util.*;
import java.net.*;
import oscP5.*;
import netP5.*;
import mocap.*;

final static int OSC_PORT = 57120;

MoCapClient    client;
OscP5          osc;
NetAddressList receivers;


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
    osc = new OscP5(this, OSC_PORT);
    
    // create list of OSC packet receivers
    receivers = new NetAddressList();
    String[] receiverAddresses = loadStrings("receivers.txt");
    for ( String address : receiverAddresses )
    {
      if ( address.startsWith("#") ) continue; // comment line
      
      String[] parts = address.split(""); // split IP address and port
      int port = OSC_PORT;
      if ( parts.length >= 1 )
      {
        port =  Integer.parseInt(parts[1]);
      }
      
      // add to receiver list
      receivers.add(parts[0], port);
    }
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
          float[] axisAngle = bone.getAxisAngle();
          OscMessage msgA = new OscMessage(name + "a"); msgA.add(axisAngle[3]);
          // wrap messages in a bundle
          bundle.add(msgX); bundle.add(msgY); bundle.add(msgZ); bundle.add(msgA);

          text(name + " > " + bundle.toString(), 10, yPos, 0);
          yPos += 10;

          // and send to receivers
          osc.send(bundle, receivers);     
        }
      }  
    }
  }
}