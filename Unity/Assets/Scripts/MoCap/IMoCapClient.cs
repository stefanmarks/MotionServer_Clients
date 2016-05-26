using System;

namespace MoCap
{
	interface IMoCapClient_ConnectionInfo
	{
		// no methods, just a base interface
	};

	/// <summary>
	/// Generic interface for MoCap clients.
	/// </summary>
	/// 
	interface IMoCapClient
	{
		/// <summary>
		/// Tries to establish a connection to the data source, e.g., a server.
		/// </summary>
		/// <param name="connectionInfo">Information necessary connect to the source</param>
		/// <returns><c>true</c> if the connection is established</returns>
		/// 
		bool Connect(IMoCapClient_ConnectionInfo connectionInfo);


		/// <summary>
		/// Checks if the client is connected to the data source.
		/// </summary>
		/// <returns><c>true</c> if the client is connected</returns>
		/// 
		bool IsConnected();


		/// <summary>
		/// Disconnects the client from the data source.
		/// </summary>
		/// 
		void Disconnect();


		/// <summary>
		/// Gets the name of the data source, e.g., the MotionServer.
		/// </summary>
		/// <returns>the name of the data source</returns>
		/// 
		String GetDataSourceName();


		/// <summary>
		/// Gets the latest frame data.
		/// </summary>
		/// 
		void Update();


		/// <summary>
		/// Adds a scene data listener.
		/// </summary>
		/// <param name="listener">The listener to add</param>
		/// <returns><c>true</c>, if the scene listener was added, <c>false</c> otherwise.</returns>
		/// 
		bool AddSceneListener(SceneListener listener);


		/// <summary>
		/// Removes a scene data listener.
		/// </summary>
		/// <param name="listener">The listener to remove</param>
		/// <returns><c>true</c>, if the scene listener was removed, <c>false</c> otherwise.</returns>
		///
		bool RemoveSceneListener(SceneListener listener);
	}

}
