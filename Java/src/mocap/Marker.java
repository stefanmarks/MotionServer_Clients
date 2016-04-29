package mocap;

/**
 * Class for holding information about a single MoCap marker.
 * 
 * @author  Stefan Marks
 */
public class Marker 
{
    public final Actor   actor;      // associated actor
    public final String  name;       // name
    
    public       float   px, py, pz; // position
    public       boolean tracked;    // tracking state

    
    /**
     * Creates a marker instance.
     * 
     * @param actor associated actor
     * @param name  the name of the marker
     */
    public Marker(Actor actor, String name)
    {
        this.actor = actor;
        this.name  = name;
        px = py = pz = 0.0f;
        tracked = false;
    }
}
