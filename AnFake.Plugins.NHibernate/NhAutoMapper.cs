using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using AnFake.Core.Exceptions;
using NHibernate.Cfg.MappingSchema;

namespace AnFake.Plugins.NHibernate
{
	internal sealed class NhAutoMapper
	{
		private readonly ISet<Type> _requestedTypes = new HashSet<Type>();
		private readonly ISet<Type> _mappedTypes = new HashSet<Type>();

		public IEnumerable<Type> MappedTypes
		{
			get { return _mappedTypes; }
		}

		public void Push<T>()
			where T : class, new()
		{
			Push(typeof(T));
		}

		public void Push(Type type)			
		{
			if (!type.IsClass || 
				type.BaseType != typeof(Object) || 
				type.FullName.StartsWith("System.") ||
				type.GetCustomAttribute<DataContractAttribute>() == null)
				throw new InvalidConfigurationException(
					String.Format(
						"NhAutoMapper supports only classes without inheritance and marked by [DataContract].\n" +
						"Type '{0}' doesn't satisfy such criteria.", 
						type.Name));			

			_requestedTypes.Add(type);
		}

		public HbmMapping Pull()
		{
			while (_requestedTypes.Count > 0)
			{
				var type = _requestedTypes.First();
				_requestedTypes.Remove(type);

				if (!_mappedTypes.Add(type))
					continue;

				return BuildMapping(type);				
			}

			return null;
		}

		private HbmMapping BuildMapping(Type type)
		{
			var mapping = new HbmMapping
			{
				assembly = type.Assembly.FullName
			};

			var @class = new HbmClass
			{
				name = type.FullName,
				entityname = NameOf(type),
				lazy = false,
				lazySpecified = true,				
				Items = new object[0]
			};

			var candidates = type.FindMembers(
				MemberTypes.Field | MemberTypes.Property,
				BindingFlags.Public | BindingFlags.Instance,
				(member, criteria) => true,
				null);

			var idMapped = false;

			foreach (var member in candidates)
			{
				var memberAttr = member.GetCustomAttribute<DataMemberAttribute>();
				if (memberAttr == null)
					continue;

				var name = memberAttr.Name ?? member.Name;

				var idAttr = member.GetCustomAttribute<IdAttribute>();
				if (idAttr != null || "id".Equals(name, StringComparison.OrdinalIgnoreCase))
				{
					MapId(name, idAttr != null && idAttr.IsNative, member, @class);
					idMapped = true;
				}
				else
				{
					var retType = ReturnTypeOf(member);
					if (retType.IsValueType || retType == typeof (String))
					{
						MapProperty(name, retType, memberAttr.IsRequired, member, @class);
					}
					else if (typeof (IEnumerable).IsAssignableFrom(retType))
					{
						var elementType = ElementTypeOf(retType);
						Push(elementType);
						MapOneToMany(name, elementType, retType, member, @class);
					}					
					else
					{
						Push(retType);
						MapManyToOne(name, retType, memberAttr.IsRequired, member, @class);
					}
				}
			}

			if (!idMapped)
			{
				MapAutoId(@class);
			}

			mapping.Items = new object[] {@class};

			return mapping;
		}

		private static void MapId(string name, bool isNative, MemberInfo member, HbmClass @class)
		{
			@class.Item = new HbmId
			{
				name = name,
				generator = new HbmGenerator
				{
					@class = isNative ? "native" : "assigned"
				}
			};
		}

		private static void MapAutoId(HbmClass @class)
		{
			@class.Item = new HbmId
			{
				column1 = "__Id",
				type1 = "Int64",
				generator = new HbmGenerator {@class = "native"}
			};
		}

		private static void MapProperty(string name, Type type, bool isRequired, MemberInfo member, HbmClass @class)
		{
			var property = new HbmProperty {name = name};

			if (isRequired || (type.IsValueType && !(type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>))))
			{
				property.notnull = true;
				property.notnullSpecified = true;
			}

			var lengthAttr = member.GetCustomAttribute<LengthAttribute>();
			if (lengthAttr != null)
			{
				property.length = lengthAttr.Value.ToString("D");
			}

			var indexedAttr = member.GetCustomAttribute<IndexedAttribute>();
			if (indexedAttr != null)
			{
				property.index = indexedAttr.Name;
				property.unique = indexedAttr.IsUnique;
			}

			@class.Items = @class.Items
				.Concat(new[] {property})
				.ToArray();
		}

		private static void MapOneToMany(string name, Type subType, Type collectionType, MemberInfo member, HbmClass @class)
		{
			object relation = null;
			var parentName = NameOf(member.DeclaringType);
			var key = new HbmKey
			{
				column1 = String.Format("__{0}", parentName)				
			};
			var index = new HbmIndex
			{
				column1 = String.Format("__{0}Index", parentName)
			};

			var ilist = typeof (IList<>).MakeGenericType(subType);
			if (ilist.IsAssignableFrom(collectionType))
			{
				var orderedAttr = member.GetCustomAttribute<OrderedAttribute>();
				if (orderedAttr != null)
				{
					relation = new HbmList
					{
						name = name,
						cascade = "all",
						key = key,
						Item = index,
						Item1 = new HbmOneToMany
						{
							@class = subType.FullName,
							entityname = NameOf(subType)
						}
					};
				}
				else
				{
					relation = new HbmBag
					{
						name = name,
						cascade = "all",
						key = key,
						Item = new HbmOneToMany
						{
							@class = subType.FullName,
							entityname = NameOf(subType)
						}
					};
				}
			}
			else
			{
				var iset = typeof (ISet<>).MakeGenericType(subType);
				if (iset.IsAssignableFrom(collectionType))
				{
					relation = new HbmSet
					{
						name = name,
						cascade = "all",
						key = key,
						Item = new HbmOneToMany
						{
							@class = subType.FullName,
							entityname = NameOf(subType)
						}
					};
				}
			}

			if (relation == null)
				throw new InvalidConfigurationException(
					String.Format(
						"NhAutoMapper supports only ISet<> and IList<> for one-to-many relation.\n" +
						"Property '{0}.{1}' doesn't satisfy such criteria.", 
						// ReSharper disable once PossibleNullReferenceException
						member.DeclaringType.Name,
						member.Name));

			@class.Items = @class.Items
				.Concat(new[] {relation})
				.ToArray();
		}

		private static void MapManyToOne(string name, Type type, bool isRequired, MemberInfo member, HbmClass @class)
		{
			var manyToOne = new HbmManyToOne
			{
				name = name,
				@class = type.FullName,
				entityname = NameOf(type)
			};

			if (isRequired)
			{
				manyToOne.notnull = true;
				manyToOne.notnullSpecified = true;
			}

			@class.Items = @class.Items
				.Concat(new[] {manyToOne})
				.ToArray();
		}

		private static Type ReturnTypeOf(MemberInfo member)
		{
			var pi = member as PropertyInfo;
			if (pi != null)
				return pi.PropertyType;

			var fi = member as FieldInfo;
			if (fi != null)
				return fi.FieldType;

			throw new InvalidConfigurationException(
				String.Format(
					"NhAutoMapper unable to determine return type of '{0}.{1}'.",
					// ReSharper disable once PossibleNullReferenceException
					member.DeclaringType.Name,
					member.Name));
		}

		private static Type ElementTypeOf(Type collection)
		{
			var ienumerable = collection
				.GetInterfaces()
				.FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof (IEnumerable<>));

			if (ienumerable == null)
				throw new InvalidConfigurationException(
					String.Format(
						"NhAutoMapper supports only generic collections implementing IEnumerable<>.\n" +
						"Type '{0}' doesn't satisfy such criteria.",
						collection.FullName));

			return ienumerable.GetGenericArguments()[0];
		}

		private static string NameOf(Type type)
		{
			return type.GetCustomAttribute<DataContractAttribute>().Name ?? type.Name;
		}
	}
}