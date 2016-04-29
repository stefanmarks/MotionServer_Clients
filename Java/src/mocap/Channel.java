package mocap;

/**
 * Class for holding information about a single Interaction Device channel.
 * 
 * @author  Stefan Marks
 */
public class Channel 
{
    public final Device device; // associated device
    public final String name;   // channel name
    
    public       float  value;  // channel value

    
    /**
     * Creates a channel instance.
     * 
     * @param device associated device
     * @param name   the name of the channel
     */
    public Channel(Device device, String name)
    {
        this.device = device;
        this.name   = name;
        
        value = 0.0f;
    }
}
