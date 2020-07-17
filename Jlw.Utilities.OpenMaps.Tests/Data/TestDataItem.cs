using System;

namespace Jlw.Standard.Utilities.Data.Tests
{
    public class TestDataItem
    {
        private string _description;
        private string _key;
        public object Value { get; set; }
        public object ExpectedValue { get; set; }

        public string Description
        {
            get => _description ?? "(" + Value?.GetType().Name + ")" + (this.TypeCode == TypeCode.String? "\"": "") + Value + (this.TypeCode == TypeCode.String? "\"": "");
            set => _description = value;
        }

        public string Key
        {
            //get => (_key ?? Value?.GetType().Name + "_" + Value)?.ToString().Replace(".", "_");
            get => Description.Replace("(", "").Replace(").", "_").Replace(")", "_").Replace(".", "_").Replace("\"", "");
            set => _key = value;
        }

        public Type Type => Value?.GetType();

        public TypeCode TypeCode => Type.GetTypeCode(this.Type);

        public TestDataItem(object value, object expectedValue, string desc = null, string key = null)
        {
            Value = value;
            ExpectedValue = expectedValue;
            Description = desc;
            Key = key;
        }
    }
}