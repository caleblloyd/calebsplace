using System;
using App.Config;
using Xunit;

namespace App.Unit.Common.Config
{
    public class AppConfigTest
    {
        [Fact]
        public void Test1()
        {
            Assert.Equal(100, Convert.ToInt32(AppConfig.Config["Data:EntityFramework:MaxBatchSize"]));
        }
    }
}
