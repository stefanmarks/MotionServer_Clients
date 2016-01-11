package mocap;

/**
 * Static helper methods, e.g, quaternion conversion operations
 *
 * @author  Stefan Marks
 */
public class MathUtil 
{
    public static final float EPSILON = 0.000000001f;
    
    /**
     * Converts the bone quaternion into a rotation axis/angle representation.
     * 
     * @param bone the bone to use for getting the orientation as axis/angle 
     * 
     * @return float array with the axis [0, 1, 2] and angle [3, radians] information.
     *         This array is static and will be overwritten by the next call to this function.
     */
    public static float[] getAxisAngle(Bone bone)
    {
        final float sqrLength =
                bone.qx * bone.qx + 
                bone.qy * bone.qy + 
                bone.qz * bone.qz;
        
        if ( Math.abs(sqrLength) < EPSILON ) 
        {
            tmpAxisAngle[0] = 1.0f; // X
            tmpAxisAngle[1] = 0.0f; // Y
            tmpAxisAngle[2] = 0.0f; // Z
            tmpAxisAngle[3] = 0.0f; // Angle
        } 
        else 
        {
            final float invLength = (1.0f / (float) Math.sqrt(sqrLength));
            tmpAxisAngle[0] = bone.qx * invLength; // X
            tmpAxisAngle[1] = bone.qy * invLength; // Y
            tmpAxisAngle[2] = bone.qz * invLength; // Z
            tmpAxisAngle[3] = (2.0f * (float) Math.acos(bone.qw)); // Angle
        } 
        return tmpAxisAngle;
    }
    
    
    private static final float[] tmpAxisAngle = new float[4];
}
