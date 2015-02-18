using System;

namespace AnFake.Core.Integration
{
	public abstract class PluginBase : IDisposable
	{
		public virtual void Dispose()
		{
			if (Disposed != null)
			{
				SafeOp.Try(Disposed);
			}
		}

		public event Action Disposed;
	}
}