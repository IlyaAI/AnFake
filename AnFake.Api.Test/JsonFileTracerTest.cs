using System;
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
		public void JsonFileTracer_should_work_in_concurrent_env()
		{
			// arrange
			var writer1 = new MessageWriter
			{
				Count = 1000,
				Message = "Message A",
				Tracer = new JsonFileTracer("concurrent.log.jsx", false)
			};

			var writer2 = new MessageWriter
			{
				Count = 1000,
				Message = "Message B",
				Tracer = new JsonFileTracer("concurrent.log.jsx", true)
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

			using (var log = new FileStream("concurrent.log.jsx", FileMode.Open, FileAccess.Read))
			{
				var reader = new JsonTraceReader(log);

				var count = 0;
				TraceMessage msg;
				while ((msg = reader.Read()) != null)
				{
					Assert.AreEqual(TraceMessageLevel.Info, msg.Level);
					Assert.IsTrue(msg.Message == writer1.Message || msg.Message == writer2.Message);

					count++;
				}

				Assert.AreEqual(writer1.Count + writer2.Count, count);
			}
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
				TrackingInterval = TimeSpan.FromMilliseconds(25)
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
			
			// act & assert
			IToolExecutionResult result;
			tracer.StartTrackExternal();
			try
			{
				external.Write(new TraceMessage(TraceMessageLevel.Warning, "Warning"));
				Thread.Sleep(50);
				
				Assert.AreEqual(1, warnings);

				external.Write(new TraceMessage(TraceMessageLevel.Error, "Error"));
				Thread.Sleep(50);
				
				Assert.AreEqual(1, errors);

				external.Write(new TraceMessage(TraceMessageLevel.Info, "Info"));
				Thread.Sleep(50);

				Assert.AreEqual(1, warnings);
				Assert.AreEqual(1, errors);
			}
			finally
			{
				result = tracer.StopTrackExternal();
			}
			
			// final assert
			Assert.AreEqual(1, result.WarningsCount);
			Assert.AreEqual(1, result.ErrorsCount);
		}		
	}
}
