using System;
using System.Collections.Generic;

namespace AnFake.Integration.Tfs2012
{
	internal sealed class ProcessOutputBuffer
	{
		private const int DefaultCapacity = 48; // lines

		private readonly int _capacity;
		private readonly Queue<string> _buffer;

		public ProcessOutputBuffer(int capacity)
		{
			_capacity = capacity;
			_buffer = new Queue<string>(capacity);
		}

		public ProcessOutputBuffer()
			: this(DefaultCapacity)
		{			
		}
		
		public int Append(string data)
		{
			if (String.IsNullOrWhiteSpace(data))
				return _buffer.Count;

			while (_buffer.Count >= _capacity)
				_buffer.Dequeue();

			_buffer.Enqueue(data);

			return _buffer.Count;
		}

		public override string ToString()
		{
			return String.Join("\n", _buffer);
		}
	}
}