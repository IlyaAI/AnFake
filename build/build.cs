using System;
using AnFake.Api;
using AnFake.Core;
using AnFake.Csx;

namespace build
{
    public class BuildSpec
    {
	    public BuildSpec()
	    {
		    Console.WriteLine("I'm Created!");
	    }

		[Target]
		[FailIfAnyWarning]
		public void Probe()
		{
			var cur = "".AsPath();
			Trace.InfoFormat("Hello! I'm here: {0}", cur.Full);

			//Trace.Warn("ERROR");
		}

		[TargetOnFailure]
		public void ProbeOnFailure()
		{
			Trace.Info("OnFailure");
		}

		[TargetFinally]
		public void ProbeFinally()
		{			
			Trace.Info("Finally");
		}
    }
}
