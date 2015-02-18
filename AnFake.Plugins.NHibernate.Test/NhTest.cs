using System;
using System.Collections.Generic;
using AnFake.Core;
using AnFake.Core.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnFake.Plugins.NHibernate.Test
{
	[TestClass]
	public class NhTest
	{
		public sealed class MySuperEntity
		{
			public string Value { get; set; }			
		}

		public sealed class MyEntity
		{
			public int IntVal { get; set; }

			public long LongVal { get; set; }

			public double DoubleVal { get; set; }

			public string StringVal { get; set; }

			public DateTime DateVal { get; set; }

			public MySuperEntity Parent { get; set; }

			public IList<MySubEntity> Children { get; set; }
		}

		public class MySubEntity
		{
			public string Value { get; set; }
		}

		public sealed class MyIdEntity
		{
			public int Id { get; set; }

			public string Value { get; set; }
		}

		public sealed class MyBiEntity
		{
			public int Id { get; set; }

			public MyBiEntity Parent { get; set; }

			public IList<MyBiEntity> Children { get; set; }
		}

		[ClassInitialize]
		public static void Initialize(TestContext ctx)
		{
			MyBuildTesting.Initialize();
			MyBuildTesting.ConfigurePlugins(PluginsRegistrator);

			Nh.Configuration.Integrate
				.Schema.Recreating();
			Nh.Configuration.Integrate
				.Connected.Using("Data Source=.;Initial Catalog=AnFake.Test;Integrated Security=True");
		}

		private static void PluginsRegistrator()
		{
			Plugin.Register<NhPlugin>().AsSelf();
		}

		[ClassCleanup]
		public static void Cleanup()
		{
			MyBuildTesting.Finalise();
		}

		[TestMethod]
		[TestCategory("Integration")]
		public void Nh_should_save_simple_entity()
		{
			// arrange
			MyEntity entity = null;

			"Nh1".AsTarget().Do(() =>
			{
				Nh.MapClass<MyEntity>();

				var uow = Nh.BeginWork();
				uow.Save(
					new MyEntity
					{
						IntVal = Int32.MaxValue,
						LongVal = Int64.MaxValue,
						DoubleVal = Double.MaxValue,
						StringVal = "string",
						DateVal = new DateTime(2015, 2, 1, 12, 15, 30)
					});
				uow.Flush();

				entity = uow.Query("from MyEntity where IntVal = :intVal")
					.SetParameter("intVal", Int32.MaxValue)
					.UniqueResult<MyEntity>();
			});

			// act
			MyBuildTesting.RunTarget("Nh1");

			// assert
			Assert.IsNotNull(entity);
			Assert.AreEqual(Int32.MaxValue, entity.IntVal);
			Assert.AreEqual(Int64.MaxValue, entity.LongVal);
			Assert.AreEqual(Double.MaxValue, entity.DoubleVal);
			Assert.AreEqual("string", entity.StringVal);
			Assert.AreEqual(new DateTime(2015, 2, 1, 12, 15, 30), entity.DateVal);
		}

		[TestMethod]
		[TestCategory("Integration")]
		public void Nh_should_save_one_to_many_relation()
		{
			// arrange
			MyEntity entity = null;
			var pseudoId = new Random().Next();

			"Nh2".AsTarget().Do(() =>
			{
				Nh.MapClass<MyEntity>();

				var uow = Nh.BeginWork();
				uow.Save(
					new MyEntity
					{
						IntVal = pseudoId,
						DateVal = DateTime.UtcNow,
						Children = new List<MySubEntity>
						{
							new MySubEntity {Value = "sub-entity-1"},
							new MySubEntity {Value = "sub-entity-2"}
						}
					});
				uow.Flush();

				entity = uow.Query("from MyEntity as e left join fetch e.Children where e.IntVal = :intVal")
					.SetParameter("intVal", pseudoId)
					.UniqueResult<MyEntity>();
			});

			// act
			MyBuildTesting.RunTarget("Nh2");

			// assert
			Assert.IsNotNull(entity);
			Assert.AreEqual(pseudoId, entity.IntVal);			
			Assert.AreEqual(2, entity.Children.Count);
			Assert.AreEqual("sub-entity-1", entity.Children[0].Value);
			Assert.AreEqual("sub-entity-2", entity.Children[1].Value);
		}

		[TestMethod]
		[TestCategory("Integration")]
		public void Nh_should_save_identified_entity()
		{
			// arrange
			MyIdEntity entity = null;
			var id = new Random().Next();

			"Nh3".AsTarget().Do(() =>
			{
				Nh.MapClass<MyIdEntity>();

				var uow = Nh.BeginWork();
				uow.Save(
					new MyIdEntity
					{
						Id = id,
						Value = "value"
					});
				uow.Commit();				
			});

			"Nh4".AsTarget().Do(() =>
			{
				var uow = Nh.BeginWork();
				entity = uow.Query("from MyIdEntity where Id = :id")
					.SetParameter("id", id)
					.UniqueResult<MyIdEntity>();
			});

			// act
			MyBuildTesting.RunTarget("Nh3");
			MyBuildTesting.RunTarget("Nh4");

			// assert
			Assert.IsNotNull(entity);
			Assert.AreEqual(id, entity.Id);
			Assert.AreEqual("value", entity.Value);			
		}

		[TestMethod]
		[TestCategory("Integration")]
		public void Nh_should_save_many_to_one_relation()
		{
			// arrange
			MyEntity entity = null;
			
			"Nh5".AsTarget().Do(() =>
			{
				Nh.MapClass<MyEntity>();

				var uow = Nh.BeginWork();
				
				uow.Save(new MySuperEntity { Value = "super" });
				uow.Flush();

				var super = uow.Query("from MySuperEntity where Value = :value")
					.SetParameter("value", "super")
					.UniqueResult<MySuperEntity>();

				uow.Save(
					new MyEntity
					{
						StringVal = "entity-1",
						DateVal = DateTime.UtcNow,
						Parent = super
					});
				uow.Save(
					new MyEntity
					{
						StringVal = "entity-2",
						DateVal = DateTime.UtcNow,
						Parent = super
					});
				uow.Flush();

				entity = uow.Query("from MyEntity as e join fetch e.Parent where e.StringVal = :val")
					.SetParameter("val", "entity-1")
					.UniqueResult<MyEntity>();
			});

			// act
			MyBuildTesting.RunTarget("Nh5");

			// assert
			Assert.IsNotNull(entity);
			Assert.AreEqual("entity-1", entity.StringVal);
			Assert.IsNotNull(entity.Parent);			
			Assert.AreEqual("super", entity.Parent.Value);			
		}

		[TestMethod]
		[TestCategory("Integration")]
		public void Nh_should_save_bidirectional_one_to_many_relation()
		{
			// arrange
			MyBiEntity entity = null;
			var rnd = new Random();

			"Nh6".AsTarget().Do(() =>
			{
				Nh.MapClass<MyBiEntity>();

				var uow = Nh.BeginWork();

				var parent = new MyBiEntity { Id = rnd.Next(), Children = new List<MyBiEntity>() };
				parent.Children.Add(new MyBiEntity { Id = rnd.Next(), Parent = parent });
				parent.Children.Add(new MyBiEntity { Id = rnd.Next(), Parent = parent });

				uow.Save(parent);
				uow.Flush();

				entity = uow.Query("from MyBiEntity as e left join fetch e.Children where e.Id = :id")
					.SetParameter("id", parent.Id)
					.UniqueResult<MyBiEntity>();				
			});

			// act
			MyBuildTesting.RunTarget("Nh6");

			// assert
			Assert.IsNotNull(entity);
			Assert.AreEqual(2, entity.Children.Count);
			Assert.IsNotNull(entity.Children[0].Parent);
			Assert.IsNotNull(entity.Children[1].Parent);
		}
	}
}