package mocap;

import java.net.InetAddress;

/**
 * Generic interface for Motion Capture clients.
 * 
 * @author  Stefan Marks
 */
public interface MoCapClient 
{
    /**
     * Connects to a MoCap server
     * 
     * @param host the host to connect to
     * 
     * @return <code>true</code> if the connection was successful,
     *         <code>false</code> if not
     */
    boolean connect(InetAddress host);
    
    
    /**
     * Checks is the client is still connected.
     * 
     * @return <code>true</code> if the client is connected,
     *         <code>false</code> if not
     */
    boolean isConnected();
    
    
    /**
     * Gets the name of the MoCap server.
     * 
     * @return the name of the MoCap server
     */
    String getServerName();
    
    
    /**
     * Updates the scene.
     */
    void update();
    
    
    /**
     * Gets the current scene.
     * 
     * @return the current scene data
     */
    Scene getScene();
    
    
    /**
     * Registers a new scene listener.
     * 
     * @param listener the listener to register
     * 
     * @return <code>true</code> if listener was registered,
     *         <code>false</code> if not
     */
    boolean addSceneListener(SceneListener listener);
    
    
    /**
     * Removes a scene listener.
     * 
     * @param listener the listener to remove
     * 
     * @return <code>true</code> if listener was removed,
     *         <code>false</code> if not
     */
    boolean removeSceneListener(SceneListener listener);
    
    
    /**
     * Sends a command string to the MoCap system.
     * 
     * @param command the command string
     * 
     * @return the response from the MoCap system 
     *         (<code>null</code> in case of an error);
     */
    String sendCommand(String command);
    
    
    /**
     * Disconnects the client from the MoCap server.
     * 
     * @return <code>true</code> if the disconnection was successful,
     *         <code>false</code> if not
     */
    boolean disconnect();
}
