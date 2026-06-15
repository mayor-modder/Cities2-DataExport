using System;

namespace System.Text.Json.Serialization
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class JsonPropertyNameAttribute : Attribute
    {
        public JsonPropertyNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
