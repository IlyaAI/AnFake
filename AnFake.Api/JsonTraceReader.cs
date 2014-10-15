using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace AnFake.Api
{
	public sealed class JsonTraceReader
	{
		private readonly Stream _stream;
		private readonly XmlObjectSerializer _serializer;
		private readonly byte[] _buffer;
		private int _bufferOffset;
		private bool _eof;

		public JsonTraceReader(Stream stream)
		{
			_stream = stream;
			_serializer = new DataContractJsonSerializer(typeof (TraceMessage));
			_buffer = new byte[100*1024];
			_bufferOffset = 0;
			_eof = false;
		}

		public TraceMessage Read()
		{
			var read = 0;
			if (!_eof)
			{
				read = _stream.Read(_buffer, _bufferOffset, _buffer.Length - _bufferOffset);
				if (read <= 0)
				{
					read = 0;
					_eof = true;
				}
			}

			if (_bufferOffset + read == 0)
				return null;

			TraceMessage message;
			var parsed = DoReadMessage(0, _bufferOffset + read, out message);
			Array.Copy(_buffer, parsed, _buffer, 0, _bufferOffset + read - parsed);
			_bufferOffset += read - parsed;

			return message;
		}

		private int DoReadMessage(int start, int length, out TraceMessage message)
		{
			var end = start + length;
			var index = start;

			while (index < end && _buffer[index] != '\x0A') index++;

			if (index >= end)
				throw new FormatException("Unable to locate end-of-object marker. Might be trace message too large.");

			using (var mem = new MemoryStream(_buffer, start, index - start, false))
			{
				message = (TraceMessage) _serializer.ReadObject(mem);
			}

			return ++index;
		}
	}
}