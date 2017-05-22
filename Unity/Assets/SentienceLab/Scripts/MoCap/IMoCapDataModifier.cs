#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Interface for components that influence MoCap data, e.g., scaling, mirroring
	/// </summary>
	/// 
	public interface IMoCapDataModifier
	{
		/// <summary>
		/// Modifies a MoCap data point.
		/// </summary>
		/// <param name="data">data point to be modified</param>
		/// 
		void Process(ref MoCapData data);


		/// <summary>
		/// Queries how many buffer elements the modifier needs.
		/// </summary>
		/// <returns>Number of buffer elements for this modifier to function (minimum 1)</returns>
		///  
		int GetRequiredBufferSize();
	}
}
