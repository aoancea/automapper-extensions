using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoMapper.Extensions.UnitTesting
{
	[TestClass]
	public class Startup
	{
		[AssemblyInitialize]
		public static void AssemblyInitialize(TestContext testContext)
		{
			//ConfigureRuntimeMapper();

			ConfigureStartupMapper();
		}

		private static void ConfigureRuntimeMapper()
		{
			AutoMapper.Configuration.MapperConfigurationExpression config = new Configuration.MapperConfigurationExpression() { CreateMissingTypeMaps = true };


			//config.CreateMap(typeof(Cow), typeof(Mule));


			AutoMapper.Mapper.Initialize(config);

			AutoMapper.Extensions.AutoMapperExtensions.Configure(AutoMapper.Mapper.Instance, config);
		}


		private static void ConfigureStartupMapper()
		{
			AutoMapper.Configuration.MapperConfigurationExpression config = new Configuration.MapperConfigurationExpression();

			MethodInfo[] testMethods = GetAssemblyMethods();

			AutoMapper.Extensions.AutoMapperExtensionsIL.RegisterMaps(config, testMethods);

			AutoMapper.Mapper.Initialize(config);

			AutoMapper.Extensions.AutoMapperExtensionsIL.Configure(AutoMapper.Mapper.Instance);
		}

		private static MethodInfo[] GetAssemblyMethods()
		{
			return typeof(AutoMapperExtensionsILTest)
			   .GetMethods(BindingFlags.Public | BindingFlags.Instance)
			   .Where(mi => mi.GetCustomAttribute<TestMethodAttribute>() != null)
			   .ToArray();
		}
	}
}