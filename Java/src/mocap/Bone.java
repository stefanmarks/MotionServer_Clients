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
    public final int    id;           // ID of the bone
    public final String name;         // name of the bone
    
    public Bone    parent;            // parent bone
    public float   ox, oy, oz;        // offset to parent
    
    public float   px, py, pz;        // position
    public float   qx, qy, qz, qw;    // rotation
    public float   length;            // length of bone
    public boolean tracked;           // tracking flag
    
    public final List<Bone> children; // children of this bone
    public final List<Bone> chain;    // chain from root bone to this bone
    
    
    /**
     * Creates a new bone.
     * 
     * @param id   the ID of the bone
     * @param name the name of the bone
     */
    public Bone(int id, String name)
    {
        this.id   = id;
        this.name = name;

        ox = oy = oz = 0;         // no offset
        parent = null;            // no parent

        px = py = pz = 0;         // origin position
        qx = qy = qz = 0; qw = 1; // no rotation
        length = 0;               // no length
        
        tracked = true;
        
        children = new LinkedList<>(); 
        chain    = new LinkedList<>();
        chain.add(this); // this bone is part of the chain
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
