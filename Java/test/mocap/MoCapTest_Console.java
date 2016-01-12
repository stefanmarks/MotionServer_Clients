package mocap;


import java.net.InetAddress;
import java.net.UnknownHostException;
import java.util.logging.Level;
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
        
        String[] servers = new String[] {"127.0.0.1", "156.62.159.85"};
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
        
            for ( int i = 0 ; i < 10 ; i++)
            {
                client.update();
                Scene s = client.getScene();
                synchronized(s)
                {
                    System.out.println("Frame " + s.frameNumber);
                    System.out.println("Actors count: " + s.actors.length);
                    for (Actor actor : s.actors)
                    {
                        System.out.println("Actor " + actor.id + " '" + actor.name + "'");
                        for (Marker m : actor.markers)
                        {
                            System.out.println("\t" + m.name + " X=" + m.pz + ", Y=" + m.py + ", Z=" + m.pz);
                        }
                        System.out.println("Bone count: " + actor.bones.length);
                        for (Bone b : actor.bones)
                        {
                            System.out.println("Bone " + b.id + " '" + b.name + 
                                    "': Offset X=" + b.ox + ", Y=" + b.oy + ", Z=" + b.oz + 
                                    " / Pos X=" + b.px + ", Y=" + b.py + ", Z=" + b.pz + 
                                    " / Rot X=" + b.qx + ", Y=" + b.qy + ", Z=" + b.qz + ", W=" + b.qw +
                                    " / Parent = " + ((b.parent == null) ? "---" : b.parent.name));
                        }
                    }
                    System.out.println("Device count: " + s.devices.length);
                    for (Device d : s.devices)
                    {
                        System.out.println("Device " + d.name);
                        for (Channel c : d.channels)
                        {
                            System.out.println("\t" + c.name + "=" + c.value);
                        }
                    }
                    System.out.println("Latency : " + s.latency + "ms");
                }
            }

            client.disconnect();
        }
    }
}
