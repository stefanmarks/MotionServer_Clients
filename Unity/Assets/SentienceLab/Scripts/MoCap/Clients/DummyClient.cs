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
		///
		public DummyClient()
		{
			scene     = new Scene();
			connected = false;
		}


		public bool Connect(IMoCapClient_ConnectionInfo connectionInfo)
		{
			// successful every time
			connected = true;
			return connected;
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
			// can't pause this
		}


		public void Update(ref bool dataChanged, ref bool sceneChanged)
		{
			// nothing happening here
		}


		public Scene GetScene()
		{
			return scene;
		}


		private          bool         connected;
		private          Scene        scene;
	}

}
