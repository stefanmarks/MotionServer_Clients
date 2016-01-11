package mocap;

/**
 * Class for holding information about a single MoCap marker.
 * 
 * @author  Stefan Marks
 */
public class Channel 
{
    public String name;
    public float  value;

    public Channel()
    {
        name  = "";
        value = 0.0f;
    }
}
