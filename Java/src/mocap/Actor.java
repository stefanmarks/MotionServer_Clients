package mocap;

/**
 * Class for holding information about a single actor.
 * 
 * @author  Stefan Marks
 */
public class Actor 
{
    public final Scene    scene;    // Scene this actor belongs to
    public final String   name;     // Actor name
    public       int      id;       // Actor ID (not final > can be changed by skeleton description)
    
    public       Marker[] markers;  // makers
    public       Bone[]   bones;    // bones

    /**
     * Default constructor.
     * 
     * @param scene  scene this actor belongs to
     * @param name   name of the actor
     * @param id     id of the actor
     */
    public Actor(Scene scene, String name, int id)
    {
        this.scene = scene;
        this.name  = name;
        this.id    = id;
        
        markers = new Marker[0];
        bones   = new Bone[0];
    }

    
    /**
     * Returns the marker with a given name.
     *
     * @param name  the marker name to search for (Regular Expressions are possible)
     *
     * @return the marker with that name
     *         or <code>null</code> if the name doesn't exist
     */
    public Marker findMarker(String name)
    {
        for ( Marker marker : markers )
        {
            if ( marker.name.matches(name) )
            {
                return marker;
            }
        }
        return null;
    }

    
    /**
     * Returns the bone with a given name.
     *
     * @param name  the bone name to search for (Regular Expressions are possible)
     *
     * @return the bone with that name
     *         or <code>null</code> if the bone doesn't exist
     */
    public Bone findBone(String name)
    {
        for ( Bone bone : bones )
        {
            if ( bone.name.matches(name) )
            {
                return bone;
            }
        }
        return null;
    }
    
    
    /**
     * Returns the bone with a given ID.
     *
     * @param id  the bone id to search for
     *
     * @return the bone with that ID
     *         or <code>null</code> if the bone doesn't exist
     */
    public Bone findBone(int id)
    {
        for ( Bone bone : bones )
        {
            if ( (bone != null) && (bone.id == id) )
            {
                return bone;
            }
        }
        return null;
    }
}
