#region Copyright Information
// Sentience Lab - Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System;
using System.IO;
using UnityEngine;

namespace SentienceLab
{
	public interface IDataLogger
	{
		void Log(string _event, params object[] _data);
	}


	public class DataLogger : MonoBehaviour, IDataLogger
	{
		
		[Tooltip("Filename of the logfile\n(The string \"{TIMESTAMP}\" will be replaced by an actual timestamp)")]
		public string LogFilename = "Logfile_{TIMESTAMP}.txt";

		public bool AlsoLogToConsole = false;


		public static IDataLogger Instance
		{
			get
			{
				if (m_instance == null && !m_instanceSearched)
				{
					m_instanceSearched = true;
					m_consoleLogger = new ConsoleLogger();
					m_instance = GameObject.FindObjectOfType<DataLogger>();
					if (m_instance != null)
					{
						m_instance.OpenLogfile();
					}
					else
					{
						Debug.LogWarning("Could not find an instance of DataLogger. Using console logger only.");
					}
				}
				return (m_instance != null) ? m_instance : m_consoleLogger;
			}
		}


		public void Awake()
		{
			Start();
		}


		public void Start()
		{
			// force creation of instance and opening of file
			IDataLogger l = Instance;
			if (l != null) OpenLogfile();
		}


		private void OpenLogfile()
		{
			if (m_writer == null && this.enabled)
			{
				try
				{
					// if required, build timestamped filename
					string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
					LogFilename = LogFilename.Replace("{TIMESTAMP}", timestamp);

					// open logfile and append
					m_writer = new StreamWriter(LogFilename, true);
					Log("start", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
				}
				catch (Exception e)
				{
					Debug.LogWarningFormat("Could not open logfile (Reason: {0})", e.ToString());
					this.enabled = false;
				}
			}
		}


		private void CloseLogfile()
		{
			// close logfile
			try
			{
				if (m_writer != null)
				{
					Log("end", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
					m_writer.WriteLine();
					m_writer.Close();
					m_writer = null;
				}
			}
			catch (Exception e)
			{
				Debug.LogWarningFormat("Could not close logfile (Reason: {0})", e.ToString());
			}
		}


		public void Log(string _event, params object[] _data)
		{
			if (m_writer != null)
			{
				m_writer.Write(string.Format("{0:0.000}", Time.unscaledTime));
				m_writer.Write("\t");
				m_writer.Write(_event);
				foreach (object obj in _data)
				{
					m_writer.Write("\t");
					m_writer.Write(obj.ToString());
				}
				m_writer.WriteLine();
				m_writer.Flush();
			}

			if (AlsoLogToConsole)
			{
				m_consoleLogger.Log(_event, _data);
			}
		}


		public void OnApplicationQuit()
		{
			CloseLogfile();
		}


		protected class ConsoleLogger : IDataLogger
		{
			public void Log(string _event, params object[] _data)
			{
				string output = string.Format("{0:0.000}", Time.unscaledTime) + '\t' + _event;
				foreach (object obj in _data)
				{
					output += '\t';
					output += obj.ToString();
				}
				Debug.Log(output);
			}
		}


		private static DataLogger   m_instance         = null;
		private static IDataLogger  m_consoleLogger    = null;
		private static bool         m_instanceSearched = false;
		private        StreamWriter m_writer;
	}
}