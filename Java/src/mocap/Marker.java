package mocap;

/**
 * Class for holding information about a single MoCap marker.
 * 
 * @author  Stefan Marks
 */
public class Marker 
{
    public final String  name;       // name
    public       float   px, py, pz; // position
    public       boolean tracked;    // tracking state

    /**
     * Creates a marker instance.
     * 
     * @param name the name of the marker
     */
    public Marker(String name)
    {
        this.name = name;
        px = py = pz = 0.0f;
        tracked = false;
    }
}
