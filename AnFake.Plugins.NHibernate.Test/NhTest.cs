using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using AnFake.Core;
using AnFake.Core.Exceptions;
using AnFake.Core.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;

namespace AnFake.Plugins.NHibernate.Test
{
	[TestClass]
	public class NhTest
	{
		public sealed class NonEntity
		{			
		}

		[DataContract]
		public sealed class MySimpleEntity
		{
			[DataMember]
			public int IntVal { get; set; }

			[DataMember]
			public long LongVal { get; set; }

			[DataMember]
			public double DoubleVal { get; set; }

			[DataMember(IsRequired = true)]
			public string StringVal { get; set; }

			[DataMember]
			public DateTime DateVal { get; set; }

			[DataMember]
			public int? NullableInt { get; set; }

			[DataMember]
			public string NullableString { get; set; }

			public string NonMember { get; set; }
		}

		[DataContract]
		public sealed class MySuperEntity
		{
			[DataMember]
			public string Value { get; set; }			
		}

		[DataContract]
		public sealed class MyEntity
		{
			[DataMember]
			public int Id { get; set; }

			[DataMember]
			public MySuperEntity Parent { get; set; }

			[DataMember]
			public IList<MySubEntity> Children { get; set; }
		}

		[DataContract]
		public sealed class MySetEntity
		{
			[DataMember]
			public int Id { get; set; }

			[DataMember]
			public ISet<MySubEntity> Children { get; set; }
		}

		[DataContract]
		public sealed class MyListEntity
		{
			[DataMember]
			public int Id { get; set; }

			[DataMember]
			[Ordered]
			public IList<MySubEntity> Children { get; set; }
		}

		[DataContract]
		public class MySubEntity
		{
			[DataMember]
			public string Value { get; set; }
		}

		[DataContract]
		public sealed class MyBiEntity
		{
			[DataMember]
			public int Id { get; set; }

			[DataMember]
			public MyBiEntity Parent { get; set; }

			[DataMember]
			public IList<MyBiEntity> Children { get; set; }
		}

		[DataContract]
		public sealed class MyIdEntity
		{
			[DataMember]
			[Id(IsNative = true)]
			public int Id { get; set; }

			[DataMember]
			public string Value { get; set; }
		}		

		public static int NextId = 1;

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
		[ExpectedException(typeof(InvalidConfigurationException))]
		[TestCategory("Integration")]
		public void Nh_should_throw_on_non_data_contract_class()
		{
			// arrange			

			// act
			Nh.MapClass<NonEntity>();
			
			// assert			
		}

		[TestMethod]
		[TestCategory("Integration")]
		public void Nh_should_save_simple_entity()
		{
			// arrange
			Nh.MapClass<MySimpleEntity>();

			// act
			Nh.DoWork(uow =>
			{
				uow.Save(
					new MySimpleEntity
					{
						IntVal = Int32.MaxValue,
						LongVal = Int64.MaxValue,
						DoubleVal = Double.MaxValue,
						StringVal = "string",
						DateVal = new DateTime(2015, 2, 1, 12, 15, 30),
						NullableInt = null,
						NullableString = null,
						NonMember = "non-member"
					});
				uow.Commit();
			});

			MySimpleEntity entity = null;
			Nh.DoWork(uow =>
			{
				entity = uow.Query("from MySimpleEntity where IntVal = :val")
					.SetParameter("val", Int32.MaxValue)
					.UniqueResult<MySimpleEntity>();
			});
			
			// assert
			Assert.IsNotNull(entity);
			Assert.AreEqual(Int32.MaxValue, entity.IntVal);
			Assert.AreEqual(Int64.MaxValue, entity.LongVal);
			Assert.AreEqual(Double.MaxValue, entity.DoubleVal);
			Assert.AreEqual("string", entity.StringVal);
			Assert.AreEqual(new DateTime(2015, 2, 1, 12, 15, 30), entity.DateVal);
			Assert.IsNull(entity.NullableInt);
			Assert.IsNull(entity.NullableString);
			Assert.IsNull(entity.NonMember);
		}

		[TestMethod]
		[ExpectedException(typeof(PropertyValueException))]
		[TestCategory("Integration")]
		public void Nh_should_throw_if_null_in_required_member()
		{
			// arrange
			Nh.MapClass<MySimpleEntity>();

			// act
			Nh.DoWork(uow =>
			{
				uow.Save(
					new MySimpleEntity
					{
						StringVal = null,
						DateVal = DateTime.UtcNow
					});
				uow.Commit();
			});

			// assert			
		}

		[TestMethod]
		[TestCategory("Integration")]
		public void Nh_should_save_one_to_many_relation_as_bag()
		{
			// arrange
			var id = NextId++;
			Nh.MapClass<MyEntity>();

			// act
			Nh.DoWork(uow =>
			{
				uow.Save(
					new MyEntity
					{
						Id = id,
						Children = new List<MySubEntity>
							{
								new MySubEntity {Value = "bag-sub-entity-1"},
								new MySubEntity {Value = "bag-sub-entity-2"}
							}
					});
				uow.Commit();
			});				

			MyEntity entity = null;
			Nh.DoWork(uow =>
			{
				entity = uow.Query("from MyEntity as e left join fetch e.Children where e.Id = :val")
					.SetParameter("val", id)
					.UniqueResult<MyEntity>();
			});				

			// assert
			Assert.IsNotNull(entity);
			Assert.AreEqual(id, entity.Id);
			Assert.AreEqual(2, entity.Children.Count);
			Assert.AreEqual("bag-sub-entity-1", entity.Children[0].Value);
			Assert.AreEqual("bag-sub-entity-2", entity.Children[1].Value);
		}

		[TestMethod]
		[TestCategory("Integration")]
		public void Nh_should_save_one_to_many_relation_as_set()
		{
			// arrange
			var id = NextId++;
			Nh.MapClass<MySetEntity>();

			// act
			Nh.DoWork(uow =>
			{
				uow.Save(
					new MySetEntity
					{
						Id = id,
						Children = new HashSet<MySubEntity>
							{
								new MySubEntity {Value = "set-sub-entity-1"}
							}
					});
				uow.Commit();
			});

			MySetEntity entity = null;
			Nh.DoWork(uow =>
			{
				entity = uow.Query("from MySetEntity as e left join fetch e.Children where e.Id = :val")
					.SetParameter("val", id)
					.UniqueResult<MySetEntity>();
			});

			// assert
			Assert.IsNotNull(entity);
			Assert.AreEqual(id, entity.Id);
			Assert.AreEqual(1, entity.Children.Count);
			Assert.AreEqual("set-sub-entity-1", entity.Children.First().Value);
		}

		[TestMethod]
		[TestCategory("Integration")]
		public void Nh_should_save_one_to_many_relation_as_list()
		{
			// arrange
			var id = NextId++;
			Nh.MapClass<MyListEntity>();
			Nh.DoWork(uow =>
			{
				uow.Save(
					new MyListEntity
					{
						Id = id,
						Children = new List<MySubEntity>
							{
								new MySubEntity {Value = "list-sub-entity-2"}
							}
					});
				uow.Commit();
			});

			// act
			MyListEntity entity = null;
			Nh.DoWork(uow =>
			{
				entity = uow.Query("from MyListEntity where Id = :val")
					.SetParameter("val", id)
					.UniqueResult<MyListEntity>();

				entity.Children.Insert(0, new MySubEntity { Value = "list-sub-entity-1" });

				uow.Save(entity);
				uow.Commit();
			});
			
			Nh.DoWork(uow =>
			{
				entity = uow.Query("from MyListEntity as e left join fetch e.Children where e.Id = :val")
					.SetParameter("val", id)
					.UniqueResult<MyListEntity>();
			});

			// assert
			Assert.IsNotNull(entity);
			Assert.AreEqual(id, entity.Id);
			Assert.AreEqual(2, entity.Children.Count);
			Assert.AreEqual("list-sub-entity-1", entity.Children[0].Value);
			Assert.AreEqual("list-sub-entity-2", entity.Children[1].Value);
		}

		[TestMethod]
		[TestCategory("Integration")]
		public void Nh_should_save_many_to_one_relation()
		{
			// arrange
			Nh.MapClass<MyEntity>();
			Nh.DoWork(uow =>
			{
				uow.Save(new MySuperEntity { Value = "super" });
				uow.Commit();
			});
			var id = NextId++;
			
			// act
			Nh.DoWork(uow =>
			{
				var super = uow.Query("from MySuperEntity where Value = :value")
					.SetParameter("value", "super")
					.UniqueResult<MySuperEntity>();

				uow.Save(
					new MyEntity
					{
						Id = id,
						Parent = super
					});				
				uow.Commit();
			});

			MyEntity entity = null;
			Nh.DoWork(uow =>
			{
				entity = uow.Query("from MyEntity as e join fetch e.Parent where e.Id = :val")
					.SetParameter("val", id)
					.UniqueResult<MyEntity>();
			});			

			// assert
			Assert.IsNotNull(entity);			
			Assert.IsNotNull(entity.Parent);
			Assert.AreEqual("super", entity.Parent.Value);
		}

		[TestMethod]
		[TestCategory("Integration")]
		public void Nh_should_save_bidirectional_one_to_many_relation()
		{
			// arrange
			Nh.MapClass<MyBiEntity>();
			var parentId = NextId++;

			// act
			Nh.DoWork(uow =>
			{
				var parent = new MyBiEntity { Id = parentId, Children = new List<MyBiEntity>() };
				parent.Children.Add(new MyBiEntity { Id = NextId++, Parent = parent });
				parent.Children.Add(new MyBiEntity { Id = NextId++, Parent = parent });

				uow.Save(parent);
				uow.Commit();
			});

			MyBiEntity entity = null;
			Nh.DoWork(uow =>
			{
				entity = uow.Query("from MyBiEntity as e left join fetch e.Children where e.Id = :id")
					.SetParameter("id", parentId)
					.UniqueResult<MyBiEntity>();
			});			

			// assert
			Assert.IsNotNull(entity);
			Assert.AreEqual(2, entity.Children.Count);
			Assert.IsNotNull(entity.Children[0].Parent);
			Assert.AreEqual(parentId, entity.Children[0].Parent.Id);
			Assert.AreSame(entity.Children[0].Parent, entity.Children[1].Parent);
		}

		[TestMethod]
		[TestCategory("Integration")]
		public void Nh_should_save_identified_entity_with_native_generator()
		{
			// arrange
			Nh.MapClass<MyIdEntity>();

			// act I
			MyIdEntity entity = null;

			Nh.DoWork(uow =>
			{
				entity = new MyIdEntity {Value = "value"};
				uow.Save(entity);
				uow.Commit();
			});

			// assert I
			Assert.IsTrue(entity.Id > 0);
				
			// act II
			Nh.DoWork(uow =>
			{
				entity = uow.Query("from MyIdEntity where Id = :id")
					.SetParameter("id", entity.Id)
					.UniqueResult<MyIdEntity>();
			});
			
			// assert II
			Assert.IsNotNull(entity);			
			Assert.AreEqual("value", entity.Value);			
		}		
	}
}