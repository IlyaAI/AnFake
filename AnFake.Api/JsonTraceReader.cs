using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace AnFake.Api
{
	/// <summary>
	/// Reads TraceMessage objects from extended JSON formatted stream.
	/// Extended means stream can contain multiple objects separated by \x0A character.
	/// </summary>
	/// <remarks>
	/// Messages are read all at once in ReadFrom method and then parsed separatedly by Next method.
	/// Read once technique is used in order to prevent long-term usage (locking) of a stream.
	/// </remarks>
	public sealed class JsonTraceReader
	{		
		private readonly XmlObjectSerializer _serializer;		
		private byte[] _buffer;
		private int _offset;		

		public JsonTraceReader()
		{			
			_serializer = new DataContractJsonSerializer(typeof(TraceMessage));			
		}

		/// <summary>
		/// Reads all available messages at once starting from specified position and returns position where read has finished.
		/// </summary>
		/// <remarks>
		/// Given stream MUST support Position and Length properties.
		/// </remarks>
		/// <param name="stream">stream for reading from</param>
		/// <param name="startPosition">position to start from</param>
		/// <returns>position where read has finished</returns>
		public long ReadFrom(Stream stream, long startPosition)
		{
			stream.Position = startPosition;

			_buffer = new byte[stream.Length - startPosition];

			var read = 0;
			while (read < _buffer.Length)
			{
				read += stream.Read(_buffer, read, _buffer.Length - read);
			}			

			_offset = 0;

			return stream.Position;
		}

		/// <summary>
		/// Parses and returns the next TraceMessage instance or null if no one more.
		/// </summary>
		/// <returns>TraceMessage instance or null</returns>
		public TraceMessage Next()
		{
			if (_offset >= _buffer.Length)
				return null;

			TraceMessage message;
			_offset = DoReadMessage(_offset, _buffer.Length - _offset, out message);

			return message;
		}

		private int DoReadMessage(int start, int length, out TraceMessage message)
		{
			var end = start + length;
			var index = start;

			while (index < end && _buffer[index] != '\x0A') index++;

			if (index >= end)
				throw new FormatException("Inconsistency in trace stream: unable to locate end-of-object marker.");

			using (var mem = new MemoryStream(_buffer, start, index - start, false))
			{
				message = (TraceMessage)_serializer.ReadObject(mem);
			}

			return ++index;
		}
	}
}