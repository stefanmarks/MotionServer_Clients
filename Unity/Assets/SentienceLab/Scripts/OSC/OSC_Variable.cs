using System.Collections.Generic;
using UnityOSC;


public interface IOSCVariableContainer
{
	List<OSC_Variable> GetOSC_Variables();
}


public abstract class OSC_Variable
{
	/// <summary>
	/// Class for delegates that are notified when an OSC variable has received new data.
	/// </summary>
	/// <param name="var">the variable that has received data</param>
	/// 
	public delegate void DataReceivedEventHandler(OSC_Variable var);


	public string name;
	public event DataReceivedEventHandler DataReceivedEvent;


	public OSC_Variable(string name = "")
	{
		this.name = name;
		manager = null;
		DataReceivedEvent += delegate (OSC_Variable var) { };
	}


	public void SetManager(OSC_Manager manager)
	{
		this.manager = manager;
	}


	public bool CanAccept(OSCPacket packet)
	{
		return packet.Address.CompareTo(name) == 0;
	}


	public void Accept(OSCPacket packet)
	{
		Unpack(packet);
		packetReceived = true;
	}


	public abstract void Unpack(OSCPacket packet);


	public void SendUpdate()
	{
		if (manager != null)
		{
			OSCPacket packet = new OSCMessage(name);
			Pack(packet);
			manager.SendPacket(packet);
		}
	}


	public abstract void Pack(OSCPacket packet);


	public void Update()
	{
		// notify listener within the main Unity update loop
		if (packetReceived)
		{
			DataReceivedEvent(this);
			packetReceived = false;
		}
	}

	private OSC_Manager manager;
	private bool        packetReceived;
}


public class OSC_BoolVariable : OSC_Variable
{
	public bool value;

	public OSC_BoolVariable(string name = "") : base(name)
	{
		value = false;
	}


	public override void Unpack(OSCPacket packet)
	{
		object obj = packet.Data[0];
		System.Type type = obj.GetType();
		if      (type == typeof(byte)  ) { value = ((byte)obj) > 0; }
		else if (type == typeof(int)   ) { value = ((int)obj) > 0; }
		else if (type == typeof(long)  ) { value = ((long)obj) > 0; }
		else if (type == typeof(float) ) { value = ((float)obj) > 0; }
		else if (type == typeof(double)) { value = ((double)obj) > 0; }
	}


	public override void Pack(OSCPacket packet)
	{
		packet.Append<int>(value ? 1 : 0);
	}
}


public class OSC_IntVariable : OSC_Variable
{
	public int value;


	public OSC_IntVariable(string name = "") : base(name)
	{
		value = 0;
	}


	public override void Unpack(OSCPacket packet)
	{
		object obj = packet.Data[0];
		System.Type type = obj.GetType();
		if      (type == typeof(byte)  ) { value = (byte)obj; }
		else if (type == typeof(int)   ) { value = (int)obj; }
		else if (type == typeof(long)  ) { value = (int)((long)obj); }
		else if (type == typeof(float) ) { value = (int)((float)obj); }
		else if (type == typeof(double)) { value = (int)((double)obj); }
	}


	public override void Pack(OSCPacket packet)
	{
		packet.Append<int>(value);
	}
}


public class OSC_FloatVariable : OSC_Variable
{
	public float min, max;
	public float value;


	public OSC_FloatVariable(string name = "", float min = 0, float max = 1) : base(name)
	{
		value = 0;
		this.min = min;
		this.max = max;
	}


	public override void Unpack(OSCPacket packet)
	{
		object obj = packet.Data[0];
		System.Type type = obj.GetType();
		if      (type == typeof(byte)  ) { value = ((byte)obj); }
		else if (type == typeof(int)   ) { value = ((int)obj); }
		else if (type == typeof(long)  ) { value = ((long)obj); }
		else if (type == typeof(float) ) { value = (float)obj; }
		else if (type == typeof(double)) { value = (float)((double)obj); }

		if (value > max) { value = max; }
		if (value < min) { value = min; }
	}


	public override void Pack(OSCPacket packet)
	{
		packet.Append<float>(value);
	}
}


public class OSC_StringVariable : OSC_Variable
{
	public string value;


	public OSC_StringVariable(string name = "") : base(name)
	{
		value = "";
	}


	public override void Unpack(OSCPacket packet)
	{
		object obj = packet.Data[0];
		System.Type type = obj.GetType();
		if      (type == typeof(string)) { value = (string)obj; }
		/*
		else if (type == typeof(byte)  ) { value = (byte)obj; }
		else if (type == typeof(int)   ) { value = (int)obj; }
		else if (type == typeof(long)  ) { value = (int) ((long)obj); }
		else if (type == typeof(float) ) { value = (int) ((float)obj); }
		else if (type == typeof(double)) { value = (int) ((double)obj); }
		*/
	}


	public override void Pack(OSCPacket packet)
	{
		packet.Append<string>(value);
	}
}
