package mocap;


import java.net.InetAddress;
import java.net.UnknownHostException;
import java.util.logging.Logger;

public class MoCapTest_Console 
{
    public static void main(String[] args)
    {
        Logger logger = Logger.getLogger("");
        logger.removeHandler(logger.getHandlers()[0]);
        logger.addHandler(new CompactLogHandler());     
        //Logger.getLogger(NatNetClient.class.getName()).setLevel(Level.FINE);
        
        MoCapClient client = new NatNetClient("MoCap Java Test Client", new byte[] {1, 2, 3, 4});
        
        String[] servers = new String[] {"127.0.0.1", "156.62.159.85", "10.1.1.199"};
        for ( String server : servers )
        {
            try
            {
                InetAddress addr = InetAddress.getByName(server);
                System.out.println("Attempting to connect to MoCap server at " + addr);
                if ( client.connect(addr) )
                {
                    break;
                }
            }
            catch (UnknownHostException e)
            {
                System.err.println("Could not resolve server address " + server);
            }
        }
        
        if ( client.isConnected() )
        {
            System.out.println("Connected to " + client.getServerName());
        
            // test listeners
            Scene scene = client.getScene();
            client.addSceneListener(new Listener());
            
            for ( int i = 0 ; i < 10 ; i++)
            {
                client.update();
                synchronized(scene)
                {
                    System.out.println("Frame " + scene.frameNumber);
                    System.out.println("Actors count: " + scene.actors.length);
                    for (Actor actor : scene.actors)
                    {
                        System.out.println("Actor " + actor.id + " '" + actor.name + "'");
                        for (Marker m : actor.markers)
                        {
                            System.out.println("\t" + m.name + 
                                    " X=" + m.pz + ", Y=" + m.py + ", Z=" + m.pz +
                                    " " + (m.tracked ? "Tracked" : "Not Tracked") );
                        }
                        System.out.println("Bone count: " + actor.bones.length);
                        for (Bone b : actor.bones)
                        {
                            float[] aa = b.getAxisAngle();
                            System.out.println("Bone " + b.id + " '" + b.name + 
                                    "': Offset X=" + b.ox + ", Y=" + b.oy + ", Z=" + b.oz + 
                                    " / Length=" + b.length +
                                    " / " + (b.tracked ? "Tracked" : "Not Tracked") +
                                    " / Pos X=" + b.px + ", Y=" + b.py + ", Z=" + b.pz + 
                                    " / Rot X=" + b.qx + ", Y=" + b.qy + ", Z=" + b.qz + ", W=" + b.qw +
                                    " / RotAA X=" + aa[0] + ", Y=" + aa[1] + ", Z=" + aa[2] + ", A=" + aa[3] +
                                    " / Parent = " + ((b.parent == null) ? "---" : b.parent.name));
                        }
                    }
                    System.out.println("Device count: " + scene.devices.length);
                    for (Device d : scene.devices)
                    {
                        System.out.println("Device " + d.name);
                        for (Channel c : d.channels)
                        {
                            System.out.println("\t" + c.name + "=" + c.value);
                        }
                    }
                    System.out.println("Latency : " + scene.latency + "ms");
                }
            }

            client.disconnect();
        }
    }


    private static class Listener implements SceneListener
    {
        @Override
        public void sceneUpdated(Scene scene)
        {
            System.out.println("Scene updated.");
        }

        @Override
        public void sceneChanged(Scene scene)
        {
            System.out.println("Scene definition changed");
        }
    }
}
