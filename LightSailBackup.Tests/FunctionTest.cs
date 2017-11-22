using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;

using LightSailBackup;

namespace LightSailBackup.Tests
{
    public class FunctionTest
    {
        [Fact]
        public async Task TestBackupSingaporeQuantityLimit7()
        {

            // Invoke the lambda function and confirm the string was upper cased.
            var _function = new Function();
            var _context = new TestLambdaContext();
            _context.AwsRequestId = Guid.NewGuid().ToString ();

            Configuration _config = new Configuration();
            _config.Name = "DailyBackupQuantity";
            _config.Region = "ap-southeast-1";
            _config.RetentionPolicy = Configuration.RetentionPolicyType.Quantity;
            _config.RetentionLimit = 1;

            var result = await _function.FunctionHandler(_config, _context);

            Assert.Equal(_config.Name + " completed successfully - " + _context.AwsRequestId, result);
        }

        [Fact]
        public async Task TestBackupTokyoPeriodLimit7()
        {

            // Invoke the lambda function and confirm the string was upper cased.
            var _function = new Function();
            var _context = new TestLambdaContext();
            _context.AwsRequestId = Guid.NewGuid().ToString();

            Configuration _config = new Configuration();
            _config.Name = "DailyBackupPeriod";
            _config.Region = "ap-northeast-1";
            _config.RetentionPolicy = Configuration.RetentionPolicyType.Period;
            _config.RetentionLimit = 1;

            var result = await _function.FunctionHandler(_config, _context);

            Assert.Equal(_config.Name + " completed successfully - " + _context.AwsRequestId, result);
        }
    }
}
