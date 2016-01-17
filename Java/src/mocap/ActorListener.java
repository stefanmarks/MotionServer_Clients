package mocap;

/**
 * Interface for listening to actor changes.
 * 
 * @author  Stefan Marks
 */
public interface ActorListener 
{
    /**
     * Gets the name of the actor this listener is bound to.
     * 
     * @return the name of the actor associated with this listener
     */
    String getActorName();
    
    
    /**
     * Called when the actor data has been updated.
     * 
     * @param actor the actor that has been updated
     */
    void actorUpdated(Actor actor);


    /**
     * Called when the actor has changed because of a new scene definition.
     * 
     * @param actor the actor that has been changed 
     *              (may be <code>null</code> when the actor no longer exists.
     */
    void actorChanged(Actor actor);
}
