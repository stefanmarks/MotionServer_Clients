
namespace MoCap
{
	/// <summary>
	/// Interface for components that have a delay parameter.
	/// </summary>
	/// 
	interface IDelay
	{
		float GetDelay();
		void SetDelay(float value);
	}
}
