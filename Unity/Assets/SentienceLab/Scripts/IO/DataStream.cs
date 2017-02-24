#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information
using System.IO;
using UnityEngine;

namespace SentienceLab.IO
{
	/// <summary>
	/// Interface for a data stream from CSV text assets or streaming assets.
	/// </summary>
	/// 
	public interface DataStream
	{
		/// <summary>
		/// Gets the name of the stream.
		/// </summary>
		/// <returns>the name of the stream</returns>
		/// 
		string GetName();

		/// <summary>
		/// Opens the stream.
		/// </summary>
		/// <returns><c>true</c> on success, <c>false</c> otherwise</returns>
		/// 
		bool Open();

		/// <summary>
		/// Closes the file
		/// </summary>
		/// 
		void Close();

		/// <summary>
		/// Reads the next line of data.
		/// </summary>
		/// <returns>the amount of characters read</returns>
		/// 
		int ReadNextLine();

		/// <summary>
		/// Checks if the end of the stream has been reached.
		/// </summary>
		/// <returns><c>true</c> if the end of the stream has been reached,
		/// <c>false</c> otherwise</returns>
		/// 
		bool EndOfStream();

		/// <summary>
		/// Marks the position within the stream to be able to rewind to.
		/// </summary>
		/// 
		void MarkPosition();

		/// <summary>
		/// Rewinds the stream to a previously marked position.
		/// </summary>
		void Rewind();

		/// <summary>
		/// Gets the raw line read from the stream.
		/// </summary>
		/// <returns>the raw line read from the stream</returns>
		/// 
		string GetRawDataString();

		/// <summary>
		/// Gets the separated parts of the line.
		/// </summary>
		/// <returns>an array with the searated parts of the line</returns>
		/// 
		string[] GetRawData();

		/// <summary>
		/// Gets the number of data items in the current line.
		/// </summary>
		/// <returns>the number fo data items in the current line</returns>
		/// 
		int GetDataCount();

		/// <summary>
		/// Checks if there is a "next" data item.
		/// </summary>
		/// <returns><c>true</c> if there is more data, <c>false</c> if not</returns>
		/// 
		bool HasNext();

		/// <summary>
		/// Gets a specific value as a string.
		/// This does not move or advance the GetNext... pointer
		/// </summary>
		/// <param name="parameterIdx">the index of the parameter to retrieve</param>
		/// <returns>the next string value</returns>
		/// 
		string GetString(int parameterIdx);

		/// <summary>
		/// Gets the next value as a string.
		/// </summary>
		/// <returns>the next string value</returns>
		/// 
		string GetNextString();

		/// <summary>
		/// Gets a specific value as an integer.
		/// This does not move or advance the GetNext... pointer
		/// </summary>
		/// <param name="parameterIdx">the index of the parameter to retrieve</param>
		/// <returns>the next integer value</returns>
		/// 
		int GetInt(int idx);

		/// <summary>
		/// Gets the next value as an integer.
		/// </summary>
		/// <returns>the next integer value</returns>
		/// 
		int GetNextInt();

		/// <summary>
		/// Gets a specific value as a float.
		/// This does not move or advance the GetNext... pointer
		/// </summary>
		/// <param name="parameterIdx">the index of the parameter to retrieve</param>
		/// <returns>the next float value</returns>
		/// 
		float GetFloat(int idx);

		/// <summary>
		/// Gets the next value as a float.
		/// </summary>
		/// <returns>the next float value</returns>
		/// 
		float GetNexFloat();
	}


	/// <summary>
	/// Abstract base class for a data stream.
	/// It uses a stream reader that does most fo the work.
	/// </summary>
	public abstract class AbstractDataStream : DataStream
	{
		public AbstractDataStream(char separator)
		{
			this.stream         = null;
			this.filePosition   = 0;
			this.markerPosition = 0;
			this.separator      = separator;
		}

		public abstract string GetName();

		public abstract bool Open();

		public int ReadNextLine()
		{
			try
			{
				do
				{
					dataLine = stream.ReadLine();
					if (dataLine != null)
					{
						dataLine = dataLine.TrimStart();
						filePosition++;
					}
					else
					{
						dataLine = "";
					}
				}
				while (dataLine.StartsWith("#") && !stream.EndOfStream);
			}
			catch (IOException)
			{
				// ignore
				dataLine = "";
			}

			data    = dataLine.Split(separator);
			dataIdx = 0;
			return data.Length;
		}

		public void MarkPosition()
		{
			markerPosition = filePosition;
		}

		public bool EndOfStream()
		{
			return stream.EndOfStream;
		}

		abstract public void Rewind();

		public string GetRawDataString()
		{
			return dataLine;
		}

		public string[] GetRawData()
		{
			return data;
		}

		public int GetDataCount()
		{
			return data.Length;
		}

		public bool HasNext()
		{
			return dataIdx < data.Length;
		}

		public string GetString(int parameterIdx)
		{
			return ((parameterIdx >= 0) && (parameterIdx < data.Length)) ?
				data[parameterIdx].Trim().Trim('"') :
				"";
		}

		public string GetNextString()
		{
			return data[dataIdx++].Trim().Trim('"');
		}

		public int GetInt(int parameterIdx)
		{
			return ((parameterIdx >= 0) && (parameterIdx < data.Length)) ?
				int.Parse(data[parameterIdx]) :
				0;
		}

		public int GetNextInt()
		{
			return int.Parse(data[dataIdx++]);
		}

		public float GetFloat(int parameterIdx)
		{
			return ((parameterIdx >= 0) && (parameterIdx < data.Length)) ?
				float.Parse(data[parameterIdx]) :
				0.0f;
		}

		public float GetNexFloat()
		{
			return float.Parse(data[dataIdx++]);
		}

		public void Close()
		{
			if (stream != null)
			{
				stream.Close();
				stream = null;
			}
		}

		protected StreamReader stream;
		protected string       dataLine;
		protected string[]     data;
		protected char         separator;
		protected int          dataIdx;
		protected int          filePosition, markerPosition;
	}


	/// <summary>
	/// Implementation of a DataStream using a file.
	/// </summary>
	public class DataStream_File : AbstractDataStream
	{
		public DataStream_File(string filename, char separator = '\t') : base(separator)
		{
			this.filename  = filename;
		}

		public override string GetName()
		{
			return filename;
		}

		public override bool Open()
		{
			if (File.Exists(filename))
			{
				Close();
				stream = new StreamReader(filename);
				filePosition = 0;
			}
			return (stream != null);
		}

		public override void Rewind()
		{ 
			Open();
			while (filePosition < markerPosition)
			{
				ReadNextLine();
			}
		}

		private string filename;
	}


	/// <summary>
	/// Implementation of a DataStream using a Unity text asset.
	/// </summary>
	public class DataStream_TextAsset : AbstractDataStream
	{
		public DataStream_TextAsset(TextAsset asset, char separator = '\t') : base(separator)
		{
			this.asset = asset;
		}

		public override string GetName()
		{
			return asset.name;
		}

		public override bool Open()
		{
			Close();
			memoryStream = new MemoryStream(asset.bytes);
			stream       = new StreamReader(memoryStream);
			filePosition = 0;
			return true;
		}

		public override void Rewind()
		{
			memoryStream.Seek(0, SeekOrigin.Begin);
			stream       = new StreamReader(memoryStream);
			filePosition = 0;
			while (filePosition < markerPosition)
			{
				ReadNextLine();
			}
		}

		private TextAsset    asset;
		private MemoryStream memoryStream;
	}
}

