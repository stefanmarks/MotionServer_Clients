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
    public final Actor  actor;        // Actor this bone belongs to
    public final String name;         // name of the bone
    public final int    id;           // ID of the bone
    
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
     * @param actor  the actor this bone belongs to
     * @param id     the ID of the bone
     * @param name   the name of the bone
     */
    public Bone(Actor actor, String name, int id)
    {
        this.actor = actor;
        this.name  = name;
        this.id    = id;

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
    
        
    /**
     * Converts the bone quaternion into a rotation axis/angle representation
     * (angle will be in radians).
     * 
     * @return a temporary axisAngle float array that will be overwritten 
     *         after the next 4 calls to this method
     */
    public float[] getAxisAngle()
    {
        // use internal temporary buffer
        float[] ret = getAxisAngle(TMP_AXIS_ANGLE[idxAxisAngleBuf]);
        // advance buffer index
        idxAxisAngleBuf = (idxAxisAngleBuf + 1) % TMP_AXIS_ANGLE.length;
        return ret;
    }

    
    /**
     * Converts the bone quaternion into a rotation axis/angle representation
     * (angle will be in radians).
     * 
     * @param axisAngle a 4 element array to store the axis/angle data in
     * 
     * @return the axisAngle float array
     */
    public float[] getAxisAngle(float[] axisAngle)
    {
        final float sqrLength = qx * qx + qy * qy + qz * qz;
        
        // if ( Math.abs(sqrLength) < EPSILON ) // sqrLength is always positive
        if ( sqrLength < EPSILON ) 
        {
            axisAngle[0] = 1.0f; // X
            axisAngle[1] = 0.0f; // Y
            axisAngle[2] = 0.0f; // Z
            axisAngle[3] = 0.0f; // Angle
        } 
        else 
        {
            final float invLength = (1.0f / (float) Math.sqrt(sqrLength));
            axisAngle[0] = qx * invLength; // X
            axisAngle[1] = qy * invLength; // Y
            axisAngle[2] = qz * invLength; // Z
            axisAngle[3] = (2.0f * (float) Math.acos(qw)); // Angle
        } 
        return axisAngle;
    }
    
    
    // Cutoff value for minimal values
    private static final float EPSILON = 0.000000001f;
    
    // temporary buffers for axis/angle values
    private static final float[][] TMP_AXIS_ANGLE = new float[4][4];
    private static       int       idxAxisAngleBuf = 0;

}
