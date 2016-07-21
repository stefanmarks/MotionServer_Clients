using System;
using System.Net;
using System.Net.Sockets;
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


		/// <summary>
		/// Attempts to receive a packet.
		/// </summary>
		/// <param name="client">the UDP client to receive from</param>
		/// <returns>the length of the received packet or -1 if nothing was received</returns>
		/// 
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
					Debug.LogWarning("Exception while waiting for MoCap server response (Time: " + Time.timeSinceLevelLoad + "s): " + e.Message);
				}
				errorCounter++;
			}

			return (data == null) ? -1 : length;
		}


		/// <summary>
		/// Gets the magic packet ID.
		/// </summary>
		/// <returns>the packet ID</returns>
		/// 
		public int GetID()
		{
			return id;
		}


		/// <summary>
		/// Reads a single byte and advanced the data buffer pointer.
		/// </summary>
		/// <returns>the read byte</returns>
		/// 
		public byte GetByte()
		{
			byte value = data[idx];
			idx++;
			return value;
		}


		/// <summary>
		/// Reads a number of bytes and advanced the data buffer pointer accordingly.
		/// </summary>
		/// <param name="values">the buffer to read into</param>
		/// 
		public void GetBytes(ref byte[] values)
		{
			for ( int i = 0 ; i < values.Length ; i++ )
			{
				values[i] = data[idx];
				idx++;
			}
		}


		/// <summary>
		/// Reads a 16 bit integer and advances the data buffer pointer.
		/// </summary>
		/// <returns>the 16 bit integer</returns>
		/// 
		public int GetInt16()
		{
			int value = BitConverter.ToInt16(data, idx); 
			idx += 2;
			return value;
		}


		/// <summary>
		/// Reads a 32 bit integer and advances the data buffer pointer.
		/// </summary>
		/// <returns>the 32 bit integer</returns>
		/// 
		public int GetInt32()
		{
			int value = BitConverter.ToInt32(data, idx); 
			idx += 4;
			return value;
		}


		/// <summary>
		/// Reads a 32 bit float value and advances the data buffer pointer.
		/// </summary>
		/// <returns>the 32 bit float</returns>
		/// 
		public float GetFloat()
		{
			float value = BitConverter.ToSingle(data, idx); 
			idx += 4;
			return value;
		}


		/// <summary>
		/// Reads a zero terminated string and advances the data buffer pointer accordingly.
		/// </summary>
		/// <returns>the string</returns>
		/// 
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


		/// <summary>
		/// Reads a fixed length string and advances the data buffer pointer.
		/// </summary>
		/// <returns>the string</returns>
		/// 
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


		/// <summary>
		/// Skips a number of bytes in the buffer.
		/// </summary>
		/// <param name="numberOfBytes">the number of bytes to skip</param>
		/// <returns>the new data buffer pointer</returns>
		/// 
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


	/// <summary>
	/// Class for a NatNet data packet to be sent with its marshalling methods.
	/// </summary>
	///
	public class NatNetPacket_Out
	{
		public const int MAX_PACKETSIZE = 10000;
		

		/// <summary>
		/// Creates a packet instance for sending.
		/// </summary>
		/// <param name="target">the UDP End point address to send the packet to</param>
		/// 
		public NatNetPacket_Out(IPEndPoint target)
		{
			this.data   = new byte[MAX_PACKETSIZE];
			this.idx    = 0;
			this.target = target;
		}
		

		/// <summary>
		/// Initialises the packet with a specific command ID.
		/// </summary>
		/// <param name="command">the command ID</param>
		/// 
		public void Initialise(int command)
		{
			idx = 0;
			PutInt16(command);
			PutInt16(0); // length - to be filled in later
		}


		/// <summary>
		/// Sends the packet.
		/// </summary>
		/// <param name="client">the client sending this packet</param>
		/// <returns><c>true</c> if the packet was sent successfully</returns>
		/// 
		public bool Send(UdpClient client)
		{
			bool success = false;

			// fill in packet length by current index position
			int    len     = idx;
			byte[] lenBuf  = BitConverter.GetBytes((short) len - 4); // don't consider packet ID and length bytes in length
			System.Array.Copy(lenBuf, 0, data, 2, 2);

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
		

		/// <summary>
		/// Puts a single byte into the data buffer and advances the buffer pointer.
		/// </summary>
		/// <param name="value">the byte to add</param>
		/// 
		public void PutByte(byte value)
		{
			data[idx] = value;
			idx++;
		}


		/// <summary>
		/// Puts several bytes into the data buffer and advances the buffer pointer.
		/// </summary>
		/// <param name="values">the bytes to add</param>
		/// 
		public void PutBytes(byte[] values)
		{
			for ( int i = 0 ; i < values.Length ; i++ )
			{
				data[idx] = values[i];
				idx++;
			}
		}


		/// <summary>
		/// Puts a 16 bit integer into the data buffer and advances the buffer pointer.
		/// </summary>
		/// <param name="value">the integer to add</param>
		/// 
		public void PutInt16(int value)
		{
			byte[] valueData = BitConverter.GetBytes((short) value); 
			System.Array.Copy(valueData, 0, data, idx, 2); 
			idx+= 2;
		}


		/// <summary>
		/// Puts a 32 bit integer into the data buffer and advances the buffer pointer.
		/// </summary>
		/// <param name="value">the integer to add</param>
		/// 
		public void PutInt32(int value)
		{
			byte[] valueData = BitConverter.GetBytes((long) value); 
			System.Array.Copy(valueData, 0, data, idx, 4); 
			idx+= 4;
		}


		/// <summary>
		/// Puts a 32 bit float into the data buffer and advances the buffer pointer.
		/// </summary>
		/// <param name="value">the float to add</param>
		/// 
		public void PutFloat(float value)
		{
			byte[] valueData = BitConverter.GetBytes((float) value); 
			System.Array.Copy(valueData, 0, data, idx, 4); 
			idx+= 4;
		}


		/// <summary>
		/// Puts a zero terminated string into the data buffer and advances the buffer pointer.
		/// </summary>
		/// <param name="value">the tring to add</param>
		/// 
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


		/// <summary>
		/// Puts a fixed length string into the data buffer and advances the buffer pointer.
		/// The buffer is padded with zeroes.
		/// </summary>
		/// <param name="value">the string to add</param>
		/// <param name="length">the fixed length</param>
		/// 
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
