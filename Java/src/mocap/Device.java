package mocap;

/**
 * Class for holding information about an interaction device, e.g., Joystick
 * that have channel values, e.g., "button1" [0,1], "axisX" [-1...1]
 * 
 * @author  Stefan Marks
 */
public class Device 
{
    public final Scene     scene;     // Associated scene
    public final String    name;      // Device name
    public final int       id;        // Device ID
    public       Channel[] channels;  // Device channels

    
    /**
     * Creates a new interaction device
     *
     * @param scene the associated scene
     * @param name  the name of the device
     * @param id    the ID of the device
     */
    public Device(Scene scene, String name, int id)
    {
        this.scene = scene;
        this.name  = name;
        this.id    = id;

        channels = new Channel[0];
    }

    
    /**
     * Returns the channel with a given name.
     *
     * @param name  the channel name to search for (Regular Expressions are possible)
     *
     * @return the channel with that name
     *         or <code>null</code> if the name doesn't exist
     */
    public Channel findChannel(String name)
    {
        for ( Channel channel : channels )
        {
            if ( channel.name.matches(name) )
            {
                return channel;
            }
        }
        return null;
    }
}
