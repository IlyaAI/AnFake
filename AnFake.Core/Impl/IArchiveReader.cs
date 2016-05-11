using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnFake.Core.Impl
{
	internal interface IArchiveReader : IDisposable
	{
		IArchiveEntry NextEntry();
	}
}
