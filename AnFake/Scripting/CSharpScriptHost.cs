using ScriptCs;
using ScriptCs.Contracts;

namespace AnFake.Scripting
{
	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	///		This class must be public.
	/// </remarks>
	public class CSharpScriptHost : ScriptHost
	{
		public CSharpScriptHost(IScriptPackManager scriptPackManager, ScriptEnvironment environment)
			: base(scriptPackManager, environment)
		{
		}

	}
}