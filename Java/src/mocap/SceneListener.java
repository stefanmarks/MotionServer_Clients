package mocap;

/**
 * Interface for listening to scene changes (=structure) and updates (=data).
 * 
 * @author  Stefan Marks
 */
public interface SceneListener 
{
    /**
     * Called when the scene data has been updated.
     * 
     * @param scene the scene that has been updated
     */
    void sceneUpdated(Scene scene);


    /**
     * Called when the scene has changed because of a new scene definition.
     * 
     * @param scene the scene that has been changed
     */
    void sceneChanged(Scene scene);
}
