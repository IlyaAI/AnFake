using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AnFake.Api;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Core.Test
{
	[TestClass]
	public class MyBuildTest
	{
		public FileSystemPath BuildPath;

		[TestInitialize]
		public void Initialize()
		{
			BuildPath = Directory.GetCurrentDirectory().AsPath();
		}

		[TestCleanup]
		public void Cleanup()
		{
			MyBuild.Reset();
			Target.Reset();
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void MyBuildRun_should_do_nothing_if_target_succeeded()
		{
			// arrange
			MyBuild.Initialize(
				BuildPath,
				new FileItem(BuildPath/"build.log", BuildPath),
				new FileItem(BuildPath/"build.fsx", BuildPath),
				Verbosity.Normal,
				new[] {"a", "b"},
				new Dictionary<string, string>());

			var sb = new StringBuilder();
			"a".AsTarget().Do(() => sb.Append("a"));
			"b".AsTarget()
				.Do(() => sb.Append("b"))
				.DependsOn("a");

			// act
			MyBuild.Run();

			// assert
			Assert.AreEqual("ab", sb.ToString());
		}

		[TestCategory("Functional")]
		[TestMethod]
		public void TargetRun_should_do_nothing_if_target_failed()
		{
			// arrange
			MyBuild.Initialize(
				BuildPath,
				new FileItem(BuildPath/"build.log", BuildPath),
				new FileItem(BuildPath/"build.fsx", BuildPath),
				Verbosity.Normal,
				new[] {"a", "b"},
				new Dictionary<string, string>());

			var sb = new StringBuilder();
			"a".AsTarget().Do(() =>
			{
				sb.Append("a");
				throw new Exception();
			}).SkipErrors();
			"b".AsTarget()
				.Do(() => sb.Append("b"))
				.DependsOn("a");

			// act
			MyBuild.Run();

			// assert
			Assert.AreEqual("ab", sb.ToString());
		}
	}
}