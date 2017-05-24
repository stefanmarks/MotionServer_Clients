#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Class for a dummy MoCap client that does not deliver any data.
	/// </summary>
	/// 
	class DummyClient : IMoCapClient
	{
		/// <summary>
		/// Constructs a dummy MoCap client
		/// </summary>
		/// <param name="manager">the MoCapManager instance</param>
		///
		public DummyClient(MoCapManager manager)
		{
			this.manager = manager;

			scene     = new Scene();
			connected = false;
		}


		public bool Connect(IMoCapClient_ConnectionInfo connectionInfo)
		{
			// successful every time
			connected = true;
			manager.NotifyListeners_Change(scene);
			return true;
		}


		public bool IsConnected()
		{
			return connected;
		}


		public void Disconnect()
		{
			connected = false; 
		}
		
		
		public String GetDataSourceName()
		{
			return "Dummy Data Source";
		}


		public float GetFramerate()
		{
			// there are no tracked objects anyway, so the rate doesn't matter
			return 60.0f; 
		}


		public void SetPaused(bool pause)
		{
		}


		public void Update()
		{
			manager.NotifyListeners_Update(scene);
		}


		public Scene GetScene()
		{
			return scene;
		}


		private readonly MoCapManager manager;
		private          bool         connected;
		private          Scene        scene;
	}

}
