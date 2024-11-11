using System;
using System.Collections.Generic;
using System.Linq;

namespace Jokenizer.Net.Dynamic {

    internal class Signature : IEquatable<Signature> {
        private readonly int hashCode;

        public Signature(IEnumerable<DynamicProperty> properties) {
            this.Properties = properties.ToArray();
            hashCode = 0;

            foreach (DynamicProperty p in properties) {
                hashCode ^= p.Name.GetHashCode() ^ p.Type.GetHashCode();
            }
        }

        public DynamicProperty[] Properties { get; }

        public override int GetHashCode() {
            return hashCode;
        }

        public bool Equals(Signature other) {
            if (Properties.Length != other.Properties.Length) return false;

            return Properties.All(p => other.Properties.Any(o => o.Name == p.Name && o.Type == p.Type));
        }
    }
}
