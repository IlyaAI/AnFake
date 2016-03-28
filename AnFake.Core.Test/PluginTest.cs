using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[TestClass]
	[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
	public class PluginTest
	{
		interface IMyPlugin
		{
		}

		class MyPlugin : IMyPlugin
		{
			public static bool Constructed;
			public static int Instances;

			public MyPlugin()
			{
				Constructed = true;
				Instances++;
			}
		}

		[TestCleanup]
		public void Cleanup()
		{
			Plugin.Finalise();

			MyPlugin.Constructed = false;
			MyPlugin.Instances = 0;
		}

		[TestMethod]
		public void Register_should_allow_multiple_calls_1()
		{
			// arrange
			Plugin.Register<MyPlugin>().AsSelf();

			// act
			Plugin.Register<MyPlugin>().As<IMyPlugin>();
			Plugin.Configure();

			// assert
			var cls = Plugin.Get<MyPlugin>();
			var iface = Plugin.Get<IMyPlugin>();
			Assert.IsNotNull(cls);
			Assert.IsNotNull(iface);
			Assert.AreSame(cls, iface);
		}

		[TestMethod]
		public void Register_should_allow_multiple_calls_2()
		{
			// arrange
			Plugin.Register<MyPlugin>().AsSelf();

			// act
			Plugin.Register<MyPlugin>().AsSelf();
			Plugin.Configure();

			// assert
			Assert.AreEqual(1, MyPlugin.Instances);
		}

		[TestMethod]
		public void Register_should_have_priority_over_register_on_demand()
		{
			// arrange
			Plugin.RegisterOnDemand<MyPlugin>().AsSelf();

			// act
			Plugin.Register<MyPlugin>().AsSelf();
			Plugin.Configure();

			// assert
			Assert.IsTrue(MyPlugin.Constructed);
		}

		[TestMethod]
		public void RegisterOnDemand_should_not_instantiate_on_configure()
		{
			// arrange
			Plugin.RegisterOnDemand<MyPlugin>().AsSelf();

			// act			
			Plugin.Configure();

			// assert
			Assert.IsFalse(MyPlugin.Constructed);
		}
	}
}
