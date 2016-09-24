using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoMapper.Extensions
{
	public static class AutoMapperExtensions
	{
		public static AutoMapper.IMapper mapper;
		public static AutoMapper.Configuration.MapperConfigurationExpression config;

		private static HashSet<Type> primitiveTypes = new HashSet<Type>(new List<Type>() { typeof(int), typeof(decimal), typeof(string), typeof(Guid), typeof(DateTime), typeof(Enum), typeof(bool) });
		private static HashSet<Tuple<Type, Type>> mappingsCache = new HashSet<Tuple<Type, Type>>();

		private static MethodInfo mi = typeof(AutoMapper.Configuration.MapperConfigurationExpression).GetMethod("CreateMap", new Type[2] { typeof(Type), typeof(Type) });

		private static object locker = new object();

		public static void Configure(AutoMapper.IMapper mapper, AutoMapper.Configuration.MapperConfigurationExpression config)
		{
			AutoMapperExtensions.mapper = mapper;
			AutoMapperExtensions.config = config;
		}

		public static TDestination DeepCopyTo<TDestination>(this object source)
			 where TDestination : class
		{
			if (source == null)
				return default(TDestination);

			Type sourceType = source.GetType();
			Type destinationType = typeof(TDestination);

			CreateMap(sourceType, destinationType);

			return (TDestination)mapper.Map(source, sourceType, destinationType);
		}

		public static void Map<TSource, TDestination>(TSource source, TDestination destination)
			where TSource : class
			where TDestination : class
		{
			if (source == default(TSource) || destination == default(TDestination))
				return;

			Type sourceType = source.GetType();
			Type destinationType = destination.GetType();

			CreateMap(sourceType, destinationType);

			mapper.Map(source, destination);
		}

		internal static void CreateMap(Type sourceType, Type destinationType)
		{
			Tuple<Type, Type> mappingKey = new Tuple<Type, Type>(sourceType, destinationType);

			if (!mappingsCache.Contains(mappingKey))
			{
				lock (locker)
				{
					if (!mappingsCache.Contains(mappingKey) && mappingsCache.Add(mappingKey))
					{
						MappingAction(sourceType, destinationType);
					}
				}
			}
		}

		private static void MappingAction(Type sourceType, Type destinationType)
		{
			Type actualSourceType = sourceType;
			Type actualDestinationType = destinationType;

			if (sourceType.IsArray)
				actualSourceType = sourceType.GetElementType();

			if (destinationType.IsArray)
				actualDestinationType = destinationType.GetElementType();

			if (sourceType.IsGenericType && sourceType.GetGenericTypeDefinition() == typeof(List<>))
				actualSourceType = sourceType.GetGenericArguments()[0];

			if (destinationType.IsGenericType && destinationType.GetGenericTypeDefinition() == typeof(List<>))
				actualDestinationType = destinationType.GetGenericArguments()[0];

			MapProperties(actualSourceType, actualDestinationType);

			mi.Invoke(config, new object[] { actualSourceType, actualDestinationType });

			TypePair types = new TypePair(actualSourceType, actualDestinationType);

			typeof(IConfigurationProvider)
				.GetMethod("GetMapperFunc", new Type[1] { typeof(TypePair) })
				.MakeGenericMethod(new Type[] { actualSourceType, actualDestinationType })
				.Invoke(mapper.ConfigurationProvider, new object[] { types });

			//TypeMap typeMap = new TypeMap(new TypeDetails(actualSourceType, config), new TypeDetails(actualDestinationType, config), MemberList.None, config);

			//TypeMapRegistry registry = new TypeMapRegistry();

			//((AutoMapper.IProfileConfiguration)config).CreateTypeDetails(actualSourceType);
			//((AutoMapper.IProfileConfiguration)config).CreateTypeDetails(actualDestinationType);

			//mapper.ConfigurationProvider.ResolveTypeMap(actualSourceType, actualDestinationType);
		}

		private static void MapProperties(Type sourceType, Type destinationType)
		{
			PropertyInfo[] destinationProperties = GetProperties(destinationType);

			if (destinationProperties.Length > 0)
			{
				PropertyInfo[] sourceProperties = GetProperties(sourceType);

				string[] commonPropertyNames = destinationProperties.Where(dp => sourceProperties.Any(sp => sp.Name == dp.Name)).Select(property => property.Name).ToArray();

				foreach (string propertyName in commonPropertyNames)
				{
					Type sourcePropertyType = sourceProperties.First(sourceProperty => sourceProperty.Name == propertyName).PropertyType;
					Type destinationPropertyType = destinationProperties.First(destinationProperty => destinationProperty.Name == propertyName).PropertyType;

					CreateMap(sourcePropertyType, destinationPropertyType);
				}
			}
		}

		private static PropertyInfo[] GetProperties(Type type)
		{
			return type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(property => !primitiveTypes.Contains(property.PropertyType)).ToArray();
		}
	}
}