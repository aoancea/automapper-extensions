using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoMapper.Extensions.UnitTesting
{
	[TestClass]
	public class Startup
	{
		[AssemblyInitialize]
		public static void AssemblyInitialize(TestContext testContext)
		{
			AutoMapper.Configuration.MapperConfigurationExpression config = new Configuration.MapperConfigurationExpression() { CreateMissingTypeMaps = true };


			//config.CreateMap(typeof(Cow), typeof(Mule));


			AutoMapper.Mapper.Initialize(config);

			AutoMapper.Extensions.AutoMapperExtensions.Configure(AutoMapper.Mapper.Instance, config);
		}
	}
}