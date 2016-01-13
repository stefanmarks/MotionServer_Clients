using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using UnityEngine;

/// <summary>
/// Classes for NatNet data packet and marshalling/unmarshalling methods.
/// </summary>
/// 
namespace MoCap
{
	/// <summary>
	/// Class for a received NatNet data packet and unmarshalling methods.
	/// </summary>
	///
	public class NatNetPacket_In
	{
		public NatNetPacket_In()
		{
			data = null;
			idx  = 0;
		}


		public int Receive(UdpClient client)
		{
			source = new IPEndPoint(IPAddress.Any, 0);

			idx    = 0;
			data   = null;
			id     = -1;
			length = 0;
			try
			{
				data    = client.Receive(ref source);
				length  = data.Length - 4;
				id      = GetInt16();
				int len = GetInt16();
				if ( len != length )
				{
					Debug.LogWarning("Packet length mismatch (" + length + " received, " + len + " announced)");
				}
				errorCounter = 0;
			}
			catch (SocketException e)
			{
				if (errorCounter == 0)
				{
					Debug.LogWarning("Exception while waiting for MoCap server response: " + e.Message);
				}
				errorCounter++;
			}

			return (data == null) ? -1 : length;
		}


		public int GetID()
		{
			return id;
		}


		public byte GetByte()
		{
			byte value = data[idx];
			idx++;
			return value;
		}


		public void GetBytes(ref byte[] values)
		{
			for ( int i = 0 ; i < values.Length ; i++ )
			{
				values[i] = data[idx];
				idx++;
			}
		}


		public int GetInt16()
		{
			int value = BitConverter.ToInt16(data, idx); 
			idx += 2;
			return value;
		}


		public int GetInt32()
		{
			int value = BitConverter.ToInt32(data, idx); 
			idx += 4;
			return value;
		}
		
		
		public float GetFloat()
		{
			float value = BitConverter.ToSingle(data, idx); 
			idx += 4;
			return value;
		}


		public string GetString()
		{
			string value = "";
			char charIn;
			do
			{
				charIn = (char) data[idx]; idx+= 1;
				if ( charIn > 0 ) 
				{
					value += charIn;
				}
			} while ( charIn != 0 );
			return value;
		}


		public string GetFixedLengthString(int length)
		{
			string value = "";
			char   charIn;
			int    endIdx = idx + length;
			do
			{
				charIn = (char) data[idx]; idx+= 1;
				if ( charIn > 0 ) 
				{
					value += charIn;
				}
			} while ( (charIn != 0) && (idx < endIdx) );
			idx = endIdx;
			return value;
		}


		public int Skip(int numberOfBytes)
		{
			idx += numberOfBytes;
			if ( idx >= data.Length ) { idx = data.Length;  }
			return idx;
		}


		private byte[]     data;
		private int        idx;
		private int        id, length;
		private IPEndPoint source;
        private int        errorCounter;
	}


	public class NatNetPacket_Out
	{
		public const int MAX_PACKETSIZE = 10000;
		

		public NatNetPacket_Out(IPEndPoint target)
		{
			this.data   = new byte[MAX_PACKETSIZE];
			this.idx    = 0;
			this.target = target;
		}
		

		public void Initialise(int command)
		{
			idx = 0;
			PutInt16(command);
			PutInt16(0); // length - to be filled in later
		}


		public bool Send(UdpClient client)
		{
			bool success = false;

			// fill in packet length by current index position
			int    len     = idx;
			byte[] lenBuf  = BitConverter.GetBytes((short) len - 4); // don't consider packet ID and length bytes in length
       		System.Array.Copy (lenBuf, 0, data, 2, 2);

			// send
			try
			{
				client.Send(data, len, target);
				success = true;
			}
			catch (SocketException e)
			{
				Debug.LogWarning("Exception " + e + " while sending command to MoCap server.");
			}

			return success;
		}
		

		public void PutByte(byte value)
		{
			data[idx] = value;
			idx++;
		}


		public void PutBytes(byte[] values)
		{
			for ( int i = 0 ; i < values.Length ; i++ )
			{
				data[idx] = values[i];
				idx++;
			}
		}
		
		
		public void PutInt16(int value)
		{
			byte[] valueData = BitConverter.GetBytes((short) value); 
			System.Array.Copy(valueData, 0, data, idx, 2); 
			idx+= 2;
		}
		
		
		public void PutInt32(int value)
		{
			byte[] valueData = BitConverter.GetBytes((long) value); 
			System.Array.Copy(valueData, 0, data, idx, 4); 
			idx+= 4;
		}
		
		
		public void PutFloat(float value)
		{
			byte[] valueData = BitConverter.GetBytes((float) value); 
			System.Array.Copy(valueData, 0, data, idx, 4); 
			idx+= 4;
		}
		
		
		public void PutString(string value)
		{
			char[] chars = value.ToCharArray();
			for ( int i = 0 ; i < chars.Length ; i++ )
			{
				data[idx] = (byte) chars[i];
				idx++;
			}
			// terminate string
			data[idx] = 0;
			idx++;
		}


		public void PutFixedLengthString(string value, int length)
		{
			char[] chars = value.ToCharArray();
			for ( int i = 0 ; i < length - 1 ; i++ ) // -1 to accommodate at least one terminating zero
			{
				data[idx] = (byte) ((i < chars.Length) ? chars[i] : 0);
				idx++;
			}
			// terminate string
			data[idx] = 0;
			idx++;
		}


		private byte[]     data;
		private int        idx;
		private IPEndPoint target;
	}
}

