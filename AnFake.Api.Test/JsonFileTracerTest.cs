using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Api.Test
{
	[TestClass]
	public class JsonFileTracerTest
	{
		class MessageWriter
		{		
			public string Message;
			public int Count;
			public JsonFileTracer Tracer;
			public Exception LastError;

			public void Run()
			{
				try
				{
					for (var i = 0; i < Count; i++)
					{
						Tracer.Write(new TraceMessage(TraceMessageLevel.Info, Message));
					}
				}
				catch (Exception e)
				{
					LastError = e;
				}				
			}
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void JsonTraceReader_should_read_all_messages_at_once()
		{
			// arrange
			const int msgsCount = 1000;
			int i;

			var tracer = new JsonFileTracer("messages.jsx", false);
			for (i = 0; i < msgsCount; i++)
			{
				tracer.Write(new TraceMessage(TraceMessageLevel.Info, i.ToString(CultureInfo.InvariantCulture)));
			}						
			
			// act
			var length = 0L;
			var reader = new JsonTraceReader();
			using (var log = new FileStream("messages.jsx", FileMode.Open, FileAccess.Read))
			{
				length = reader.ReadFrom(log, 0);
			}

			var msgs = new TraceMessage[msgsCount + 1];
			for (i = 0; i <= msgsCount && (msgs[i] = reader.Next()) != null; i++) {}
			
			// assert
			Assert.AreEqual(new FileInfo("messages.jsx").Length, length);

			Assert.AreEqual("0", msgs[0].Message);
			Assert.AreEqual((msgsCount - 1).ToString(CultureInfo.InvariantCulture), msgs[msgsCount - 1].Message);			
			Assert.IsNull(msgs[msgsCount]);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void JsonTraceReader_should_read_messages_from_specified_position()
		{
			// arrange
			var tracer = new JsonFileTracer("messages.jsx", false);
			tracer.Write(new TraceMessage(TraceMessageLevel.Info, "A"));

			var startPosition = new FileInfo("messages.jsx").Length;

			tracer.Write(new TraceMessage(TraceMessageLevel.Info, "B"));			

			// act			
			var reader = new JsonTraceReader();
			using (var log = new FileStream("messages.jsx", FileMode.Open, FileAccess.Read))
			{
				reader.ReadFrom(log, startPosition);
			}

			var msgB = reader.Next();
			var eof = reader.Next();

			// assert
			Assert.AreEqual("B", msgB.Message);			
			Assert.IsNull(eof);
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void JsonFileTracer_should_support_unicode_messages()
		{
			// arrange
			var tracer = new JsonFileTracer("unicode.jsx", false);
			tracer.Write(new TraceMessage(TraceMessageLevel.Error, "ОШИБКА"));
			
			// act
			var reader = new JsonTraceReader();
			using (var log = new FileStream("unicode.jsx", FileMode.Open, FileAccess.Read))
			{
				reader.ReadFrom(log, 0);
			}
			var msg = reader.Next();

			// assert
			Assert.IsNotNull(msg);
			Assert.AreEqual("ОШИБКА", msg.Message);			
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void JsonFileTracer_should_work_in_concurrent_env()
		{
			// arrange
			var writer1 = new MessageWriter
			{
				Count = 1000,
				Message = "Message A",
				Tracer = new JsonFileTracer("concurrent.log.jsx", false) { RetryInterval = TimeSpan.FromMilliseconds(10) }
			};

			var writer2 = new MessageWriter
			{
				Count = 1000,
				Message = "Message B",
				Tracer = new JsonFileTracer("concurrent.log.jsx", true) { RetryInterval = TimeSpan.FromMilliseconds(10) }
			};

			var th1 = new Thread(writer1.Run);
			var th2 = new Thread(writer2.Run);

			// act
			th1.Start();
			th2.Start();

			// assert
			Assert.IsTrue(th1.Join(TimeSpan.FromSeconds(5)));
			Assert.IsTrue(th2.Join(TimeSpan.FromSeconds(1)));

			Assert.IsNull(writer1.LastError);
			Assert.IsNull(writer2.LastError);

			var reader = new JsonTraceReader();
			using (var log = new FileStream("concurrent.log.jsx", FileMode.Open, FileAccess.Read))
			{
				reader.ReadFrom(log, 0);
			}
			
			var count = 0;
			TraceMessage msg;
			while ((msg = reader.Next()) != null)
			{
				Assert.AreEqual(TraceMessageLevel.Info, msg.Level);
				Assert.IsTrue(msg.Message == writer1.Message || msg.Message == writer2.Message);

				count++;
			}

			Assert.AreEqual(writer1.Count + writer2.Count, count);			
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void JsonFileTracer_should_track_external_activity()
		{
			// arrange
			var errors = 0;
			var warnings = 0;
			var tracer = new JsonFileTracer("external.log.jsx", false)
			{
				TrackingInterval = TimeSpan.FromMilliseconds(25),
				RetryInterval = TimeSpan.FromMilliseconds(10)
			};
			tracer.MessageReceived += (sender, message) =>
			{
				switch (message.Level)
				{
					case TraceMessageLevel.Warning:
						warnings++;
						break;
					case TraceMessageLevel.Error:
						errors++;
						break;
				}
			};
			var external = new JsonFileTracer("external.log.jsx", true);
			
			// act
			tracer.TrackExternal(
				() => { },
				t => 
				{
					external.Write(new TraceMessage(TraceMessageLevel.Warning, "Warning"));
					external.Write(new TraceMessage(TraceMessageLevel.Error, "Error"));
					external.Write(new TraceMessage(TraceMessageLevel.Info, "Info"));

					return true;
				}, 
				TimeSpan.FromMilliseconds(1000));
			
			// assert
			Assert.AreEqual(1, warnings);
			Assert.AreEqual(1, errors);						
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void JsonFileTracer_should_return_false_after_timeout()
		{
			// arrange
			var tracer = new JsonFileTracer("external.log.jsx", false)
			{
				TrackingInterval = TimeSpan.FromMilliseconds(25),
				RetryInterval = TimeSpan.FromMilliseconds(10)
			};
			
			// act
			var ret = tracer.TrackExternal(
				() => { },
				t =>
				{
					Thread.Sleep(t);
					return false;
				},
				TimeSpan.FromMilliseconds(10));

			// assert
			Assert.IsFalse(ret);			
		}
	}
}
