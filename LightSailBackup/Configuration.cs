using System;
using System.Collections.Generic;
using System.Text;

namespace LightSailBackup
{
    public class Configuration
    {
        public string Name { get; set; }
        public string Region { get; set; }
        public RetentionPolicyType RetentionPolicy { get; set; }        
        public Int32 RetentionLimit { get; set; }        

        public enum RetentionPolicyType
        {
            Quantity, Period
        }
    }
}
