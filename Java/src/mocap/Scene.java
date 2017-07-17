package mocap;

/**
 * Class for a complete MoCap scene.
 * 
 * @author  Stefan Marks
 */
public class Scene 
{
    public int      frameNumber; // current frame number
    public double   timestamp;   // current timestamp
    public int      latency;     // delay in milliseconds from camera capture to SDK output
    
    public Actor[]  actors;      // data for the actors
    public Device[] devices;     // data for interaction devices
    
    
    /**
     * Constructs a new scene instance.
     */
    public Scene()
    {
        frameNumber = 0;
        latency     = 0;
        actors      = new Actor[0];
        devices     = new Device[0];
    }
        
    /**
     * Returns the actor with a given name.
     *
     * @param name  the actor name to search for (Regular Expressions are possible)
     *
     * @return the actor with that name
     *         or <code>null</code> if the actor doesn't exist
     */
    public Actor findActor(String name)
    {
        int actorIdx = findActorIdx(name);
        return actorIdx < 0 ? null : actors[actorIdx];
    }

    /**
     * Returns the actor with a given ID.
     *
     * @param id  the actor ID to search for
     *
     * @return the actor with that ID
     *         or <code>null</code> if the actor doesn't exist
     */
    public Actor findActor(int id)
    {
        int actorIdx = findActorIdx(id);
        return actorIdx < 0 ? null : actors[actorIdx];
    }

    /**
     * Returns the index of the actor with a given name.
     *
     * @param name  the actor name to search for (Regular Expressions are possible)
     *
     * @return the array index of the actor
     *         or -1 if the actor doesn't exist
     */
    public int findActorIdx(String name)
    {
        int actorIdx = -1;
        for (int i = 0; i < actors.length; i++)
        {
            if (actors[i].name.matches(name))
            {
                actorIdx = i;
                break;
            }
        }
        return actorIdx;
    }

    /**
     * Returns the index of the actor with a given ID.
     *
     * @param id  the actor ID to search for
     *
     * @return the array index of the actor
     *         or -1 if the actor doesn't exist
     */
    public int findActorIdx(int id)
    {
        int actorIdx = -1;
        for (int i = 0; i < actors.length; i++)
        {
            if (actors[i].id == id)
            {
                actorIdx = i;
                break;
            }
        }
        return actorIdx;
    }
    
    /**
     * Returns the device with a given name.
     *
     * @param name  the device name to search for (Regular Expressions are possible)
     *
     * @return the device with that name
     *         or <code>null</code> if the device doesn't exist
     */
    public Device findDevice(String name)
    {
        int deviceIdx = findDeviceIdx(name);
        return deviceIdx < 0 ? null : devices[deviceIdx];
    }
    
    /**
     * Returns the device with a given name.
     *
     * @param id  the device ID to search for
     *
     * @return the device with that name
     *         or <code>null</code> if the device doesn't exist
     */
    public Device findDevice(int id)
    {
        int deviceIdx = findDeviceIdx(id);
        return deviceIdx < 0 ? null : devices[deviceIdx];
    }
    
    /**
     * Returns the index of the device with a given name.
     *
     * @param name  the device name to search for
     *
     * @return the array index of the device
     *         or -1 if the device doesn't exist
     */
    public int findDeviceIdx(String name)
    {
        int actorIdx = -1;
        for (int i = 0; i < devices.length; i++)
        {
            if (devices[i].name.matches(name))
            {
                actorIdx = i;
                break;
            }
        }
        return actorIdx;
    }
    
    /**
     * Returns the index of the device with a given ID.
     *
     * @param id  the device ID to search for
     *
     * @return the array index of the device
     *         or -1 if the device doesn't exist
     */
    public int findDeviceIdx(int id)
    {
        int actorIdx = -1;
        for (int i = 0; i < devices.length; i++)
        {
            if (devices[i].id == id)
            {
                actorIdx = i;
                break;
            }
        }
        return actorIdx;
    }
}
