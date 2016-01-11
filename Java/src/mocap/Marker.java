package mocap;

/**
 * Class for holding information about a single MoCap marker.
 * 
 * @author  Stefan Marks
 */
public class Marker 
{
    public String name;
    public float  px, py, pz; // position

    public Marker()
    {
        name = "";
        px = py = pz = 0.0f;
    }
}
