package mocap;

import java.util.LinkedList;
import java.util.List;

/**
 * Class for holding information about a single bone.
 * 
 * @author  Stefan Marks
 */
public class Bone 
{
    public String name;
    public int    id;
    public float  px, py, pz;     // position
    public float  qx, qy, qz, qw; // rotation
    
    public Bone   parent;   // parent bone
    public float  ox;       // offset to parent
    public float  oy;
    public float  oz;

    public final List<Bone> children; // children of this bone
    public final List<Bone> chain;    // chain from root bone to this bone
    
    public Bone()
    {
        name = "";
        id   = 0;
        px = py = pz = 0;         // origin position
        qx = qy = qz = 0; qw = 1; // no rotation
        ox = oy = oz = 0;         // no offset
        parent = null;            
        children = new LinkedList<>(); 
        chain    = new LinkedList<>();
        chain.add(this);
    }
    
    
    /**
     * Builds the chain list from the root bone to this bone.
     */
    protected void buildChain()
    {
        if ( parent != null )
        {
            chain.addAll(0, parent.chain);
        }
    }
}
