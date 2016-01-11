package mocap;

/**
 * Class for holding information about an interaction device, e.g., Joystick
 * that have channel values, e.g., "button1" [0,1], "axisX" [-1...1]
 * 
 * @author  Stefan Marks
 */
public class Device 
{
    public int       id;        // Device ID
    public String    name;      // Device name
    public Channel[] channels;  // data channels

    /**
     * Default constructor.
     */
    public Device()
    {
        id       = 0;
        name     = "";
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
