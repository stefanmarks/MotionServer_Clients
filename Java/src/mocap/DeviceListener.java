package mocap;

/**
 * Interface for listening to device changes.
 * 
 * @author  Stefan Marks
 */
public interface DeviceListener 
{
    /**
     * Gets the name of the device this listener is bound to.
     * 
     * @return the name of the device associated with this listener
     */
    String getDeviceName();
    
    
    /**
     * Called when the device data has been updated.
     * 
     * @param device the device that has been updated
     */
    void deviceUpdated(Device device);


    /**
     * Called when the device has changed because of a new scene definition.
     * 
     * @param device the device that has been changed 
     *              (may be <code>null</code> when the device no longer exists.
     */
    void deviceChanged(Device device);
}
