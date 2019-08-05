#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SentienceLab
{
    [System.Serializable]
	public abstract class ConfigFileBase
    {
		[Tooltip("Filename of the configuration file")]
		public string ConfigFileName;

		protected string ConfigFilePurpose;

        public ConfigFileBase(string _filename, string _purpose)
		{
			ConfigFileName    = _filename;
			ConfigFilePurpose = _purpose;
        }


        public void LoadConfiguration()
        {
            try
            {
				string configFilePath = Path.Combine(Application.dataPath, "../");
				configFilePath = Path.Combine(configFilePath, ConfigFileName);
				configFilePath = Path.GetFullPath(configFilePath);
				Debug.LogFormat("Loading {0} configuration from '{1}'", 
					ConfigFilePurpose, configFilePath);

				string json = File.ReadAllText(configFilePath, Encoding.UTF8);
				JsonUtility.FromJsonOverwrite(json, this);

				json = JsonUtility.ToJson(this);
                Debug.LogFormat("Configuration for {0}: {1}", 
					ConfigFilePurpose, json);
            }
            catch (System.Exception e)
            {
                Debug.LogWarningFormat("Error while loading {0} configuration: ({1})",
					ConfigFilePurpose, e);
            }
        }

		public void SaveConfiguration()
		{
			try
			{
				string configFilePath = ConstructPath();
				Debug.LogFormat("Saving {0} configuration to '{1}'",
					ConfigFilePurpose, configFilePath);

				string json = JsonUtility.ToJson(this, true);
				RemoveFields(ref json);

				using (StreamWriter strm = File.CreateText(configFilePath))
				{
					strm.Write(' ');
				}
				File.WriteAllText(configFilePath, json, Encoding.UTF8);
			}
			catch (System.Exception e)
			{
				Debug.LogWarningFormat("Error while saving {0} configuration: ({1})",
					ConfigFilePurpose, e);
			}
		}

		protected string ConstructPath()
		{
			var configFilePath = Path.Combine(Application.dataPath, "../");
			configFilePath = Path.Combine(configFilePath, ConfigFileName);
			configFilePath = Path.GetFullPath(configFilePath);
			return configFilePath;
		}

		protected void RemoveFields(ref string json)
		{
			// don't serialise ConfigFileName (but show in the inspector)
			json = Regex.Replace(json, "^\\s*\"ConfigFileName.*,$", "", RegexOptions.Multiline);
			json = json.Replace("\n\n", "\n");
		}
	}
}

