using System;
using System.Collections.Generic;

namespace MoCap
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
			this.sceneListeners  = new List<SceneListener>();
			scene                = new Scene();
			connected            = false;
		}


		public bool Connect(IMoCapClient_ConnectionInfo connectionInfo)
		{
			// successful every time
			connected = true;
			return true;
		}


		public bool IsConnected()
		{
			return connected;
		}


		public void Disconnect()
		{
			connected = false; 
			sceneListeners.Clear();
		}
		
		
		public String GetDataSourceName()
		{
			return "Dummy Data Source";
		}


		public void Update()
		{
		}


		public bool AddSceneListener(SceneListener listener)
		{
			bool added = false;
			if (!sceneListeners.Contains(listener))
			{
				sceneListeners.Add(listener);
				added = true;
				// immediately trigger callback
				listener.SceneChanged(scene);
			}
			return added;
		}


		public bool RemoveSceneListener(SceneListener listener)
		{
			return sceneListeners.Remove(listener);
		}


		public Scene GetScene()
		{
			return scene;
		}


		private void RefreshListeners()
		{
			foreach (SceneListener listener in sceneListeners)
			{
				listener.SceneChanged(scene);
			}
		}


		private bool                connected;
		private Scene               scene;
		private List<SceneListener> sceneListeners;
	}

}
