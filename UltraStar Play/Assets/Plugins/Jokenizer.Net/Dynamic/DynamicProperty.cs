using System;

namespace Jokenizer.Net.Dynamic {

    internal class DynamicProperty {

        public DynamicProperty(string name, Type type) {
            this.Name = name;
            this.Type = type;
        }

        public string Name { get; }

        public Type Type { get; }
    }
}
