/**
 * Simple client drawing the MoCap data in a simplified 3D space.
 *
 * @author Stefan Marks, AUT University, Auckland, NZ
 * @date   18.2.2016
 */
 
import java.util.*;
import java.net.*;
import mocap.*;

MoCapClient client;

float cameraRotX   = -30;         // rotation around X axis
float cameraRotY   = -45;         // rotation around Y axis
float cameraRadius = 400;         // distance of camera from centre

boolean displayMarkers   = true;  // flag for showing markers as little grey boxes
boolean displayBones     = true;  // flag for showing bones
boolean displayBoneNames = false; // flag for showing bone names
boolean displayCoordSys  = false; // flag for showing coordinate systems of the bones

final int GRID_SIZE = 5;          // size of the floor grid in units


void setup()
{
  size(800, 600, P3D);
  frameRate(60);

  textSize(32);
  textAlign(CENTER, CENTER);
  
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
  client = new NatNetClient("Simple Processing MoCap Test Client", new byte[] {1, 0}); // customising the client data
  
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
  
  if ( !client.isConnected() )
  {
    println("Could not connect to any MoCap server.");
    //exit();
  }
}


void draw()
{
  // prepare canvas
  background(0);
  
  pushMatrix();
  
  // position camera (mouse acts as turntable control)
  float rotY = radians(cameraRotY);
  float rotX = radians(cameraRotX);
  float r    = cameraRadius;
  float r2   = r * cos(rotX);
  camera(r2 * sin(rotY), r * sin(rotX), r2 * cos(rotY), 
         0, 0, 0, 
         0, 1, 0);
  scale(50, -50, 50);
  
  // draw XY plane grid
  stroke(0, 128, 0);
  for ( int i = -GRID_SIZE ; i <= GRID_SIZE ; i++ )
  {
    strokeWeight((i == 0) ? 0.02 : 0.01);
    line(-GRID_SIZE, 0, i, GRID_SIZE, 0, i);
    line(i, 0, -GRID_SIZE, i, 0, GRID_SIZE);
  }
  
  // draw world origin coordinate axis
  strokeWeight(0.05);
  stroke(255, 0, 0); line(0, 0, 0, 1, 0, 0);
  stroke(0, 255, 0); line(0, 0, 0, 0, 1, 0);
  stroke(0, 0, 255); line(0, 0, 0, 0, 0, 1);
  
  // draw actors
  if ( client != null )
  {
    client.update();
    Scene scene = client.getScene();

    for ( Actor actor : scene.actors )
    {
      if ( displayMarkers )
      {
        // draw markers
        fill(100); stroke(128);
        for ( Marker marker : actor.markers )
        {
          if ( marker.tracked ) 
          {
            pushMatrix();
            translate(marker.px, marker.py, marker.pz);
            box(0.01);
            popMatrix();
          }
        }
      }
      
      textSize(32);
      textAlign(CENTER, CENTER);
  
      // draw skeletons
      for ( Bone bone : actor.bones )
      {
        if ( !bone.tracked ) continue;
        
        // root bone -> line to floor plane 
        if ( bone.parent == null )
        {
          strokeWeight(0.01);
          stroke(160);
          line(bone.px, bone.py, bone.pz,
               bone.px, 0,       bone.pz);
        }
        
        strokeWeight(0.02);
        pushMatrix();
          // prepare transformation chain
          for ( Bone b : bone.chain )
          {
            translate(b.px, b.py, b.pz);
            // get axis/angle rotation representation
            float[] rot = b.getAxisAngle();
            rotate(rot[3], rot[0], rot[1], rot[2]);
          }
          if ( displayBones )
          {
            stroke(200); line(0, 0, 0, 0, bone.length, 0);
          }
          if ( displayCoordSys )
          {
            // draw axes
            stroke(255, 0, 0); line(0, 0, 0, 0.1, 0, 0);
            stroke(0, 255, 0); line(0, 0, 0, 0, 0.1, 0);
            stroke(0, 0, 255); line(0, 0, 0, 0, 0, 0.1);
          }
          if ( displayBoneNames )
          {
            // draw bone name
            stroke(256); scale(1/256.0); scale(1, -1, 1); text(bone.name, 0, 0, 0);
          }
        popMatrix();
      }
    }  
    
    // Example for reading values from interaction devices,
    // in this case button1 and the Y axis of the thumbstick on a joystick
    Device device = scene.findDevice("Joystick.");
    if ( device != null )
    {
      Channel chnButton = device.findChannel("button1"); // button 1 (Fire)
      if ( (chnButton != null) && (chnButton.value > 0) )
      {
        background(255, 0, 0);      
      }
      
      Channel chnY_Axis = device.findChannel("axis2"); // Y axis
      if ( chnY_Axis != null )
      {
        // move in.out
        cameraRadius -= chnY_Axis.value;
      }
    }
    
    popMatrix();
    
    fill(255);
    textSize(16);
    textAlign(LEFT, TOP);
    text("Frame: " + scene.frameNumber, 5, 5); 
    text("Timestamp: " + nf((float) scene.timestamp, 0, 2), 5, 25); 
  }
}


void mouseDragged(MouseEvent event)
{
  // change camera rotation by dragging
  cameraRotX += map(mouseY - pmouseY, 0, height,  0, -180);
  cameraRotX  = constrain(cameraRotX, -89.9, 89.9);
  cameraRotY -= map(mouseX - pmouseX, 0,  width,  0,  180);
}


void mouseWheel(MouseEvent event) 
{
  // zoom with mouse wheel
  cameraRadius = constrain(cameraRadius + event.getCount() * 10, 50, 1000);
}


void keyPressed()
{
  switch ( key )
  {
    case 'm' : displayMarkers   = !displayMarkers;   break;
    case 'n' : displayBoneNames = !displayBoneNames; break;
    case 'b' : displayBones     = !displayBones;     break;
    case 'c' : displayCoordSys  = !displayCoordSys;  break;
    default: break;
  }
}