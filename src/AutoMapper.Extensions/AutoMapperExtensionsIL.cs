using Mono.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoMapper.Extensions
{
	public static class AutoMapperExtensionsIL
	{
		public static AutoMapper.IMapper mapper;

		private static HashSet<Type> primitiveTypes = new HashSet<Type>(new List<Type>() { typeof(int), typeof(decimal), typeof(string), typeof(Guid), typeof(DateTime), typeof(Enum), typeof(bool) });
		private static HashSet<Tuple<Type, Type>> mappingsCache = new HashSet<Tuple<Type, Type>>();

		private static MethodInfo mi = typeof(AutoMapper.Configuration.MapperConfigurationExpression).GetMethod("CreateMap", new Type[2] { typeof(Type), typeof(Type) });

		private static object locker = new object();

		public static void Configure(AutoMapper.IMapper mapper)
		{
			AutoMapperExtensionsIL.mapper = mapper;
		}

		public static TDestination ILDeepCopyFromTo<TSource, TDestination>(this TSource source)
			 where TDestination : class
		{
			if (source == null)
				return default(TDestination);

			Type sourceType = typeof(TSource);
			Type destinationType = typeof(TDestination);

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

			mapper.Map(source, destination);
		}


		public static void RegisterMaps(this AutoMapper.Configuration.MapperConfigurationExpression config, MethodInfo[] methods)
		{
			foreach (MethodBase methodBase in methods)
			{
				IList<Instruction> instructions = MethodBaseRocks.GetInstructions(methodBase);

				foreach (Instruction instruction in instructions.Where(i => i.Operand != null && i.Operand is MethodInfo))
				{
					MethodInfo methodInfo = instruction.Operand as MethodInfo;

					if (methodInfo != null && methodInfo.Name.StartsWith("ILDeepCopyFromTo"))
					{
						Type sourceType = methodInfo.GetParameters().First().ParameterType;
						Type destinationType = methodInfo.ReturnType;

						CreateMap(config, sourceType, destinationType);
					}
				}
			}
		}

		private static void CreateMap(AutoMapper.Configuration.MapperConfigurationExpression config, Type sourceType, Type destinationType)
		{
			Tuple<Type, Type> mappingKey = new Tuple<Type, Type>(sourceType, destinationType);

			if (!mappingsCache.Contains(mappingKey))
			{
				lock (locker)
				{
					if (!mappingsCache.Contains(mappingKey) && mappingsCache.Add(mappingKey))
					{
						MappingAction(config, sourceType, destinationType);
					}
				}
			}
		}

		private static void MappingAction(AutoMapper.Configuration.MapperConfigurationExpression config, Type sourceType, Type destinationType)
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

			MapProperties(config, actualSourceType, actualDestinationType);

			mi.Invoke(config, new object[] { actualSourceType, actualDestinationType });
		}

		private static void MapProperties(AutoMapper.Configuration.MapperConfigurationExpression config, Type sourceType, Type destinationType)
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

					CreateMap(config, sourcePropertyType, destinationPropertyType);
				}
			}
		}

		private static PropertyInfo[] GetProperties(Type type)
		{
			return type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(property => !primitiveTypes.Contains(property.PropertyType)).ToArray();
		}
	}
}