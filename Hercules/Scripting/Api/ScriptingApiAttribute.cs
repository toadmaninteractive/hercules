using System;

namespace Hercules.Scripting
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class ScriptingApiAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }

        public string? Example { get; init; }
        public string? AiHint { get; init; }

        public ScriptingApiAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
