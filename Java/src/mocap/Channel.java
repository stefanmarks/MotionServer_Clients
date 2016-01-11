package mocap;

/**
 * Class for holding information about a single Interaction Device channel.
 * 
 * @author  Stefan Marks
 */
public class Channel 
{
    public final String name;
    public       float  value;

    
    /**
     * Creates a channel instance.
     * 
     * @param name the name of the channel
     */
    public Channel(String name)
    {
        this.name = name;
        value = 0.0f;
    }
}
