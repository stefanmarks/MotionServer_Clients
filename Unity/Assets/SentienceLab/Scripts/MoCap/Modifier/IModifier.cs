namespace MoCap
{
	/// <summary>
	/// Interface for components that influence MoCap data, e.g., scaling, mirroring
	/// </summary>
	/// 
	public interface IModifier
	{
		/// <summary>
		/// Modifies a MoCap data point.
		/// </summary>
		/// <param name="data">data point to be modified</param>
		/// 
		void Process(ref MoCapData data);
	}
}
