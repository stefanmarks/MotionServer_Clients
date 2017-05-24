#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Base class for providing connection information to the client instance.
	/// </summary>
	///
	public abstract class IMoCapClient_ConnectionInfo
	{
		// no methods, just a base class
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
		/// Gets the amount of frames per second that the Mocap system provides.
		/// </summary>
		/// <returns>the amount of frames per second that the moCap system provides</returns>
		/// 
		float GetFramerate();


		/// <summary>
		/// Pauses or continues the data stream.
		/// </summary>
		/// <param name="paused"><c>true</c> to pause the stream</param>
		/// 
		void SetPaused(bool paused);


		/// <summary>
		/// Retrieves the latest frame data.
		/// </summary>
		/// 
		void Update();


		/// <summary>
		/// Gets the latest frame.
		/// </summary>
		/// 
		Scene GetScene();
	}

}
