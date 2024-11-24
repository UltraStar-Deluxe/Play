using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Jokenizer.Net {

    public static class Helper {

        public static bool IsSuitable(ParameterInfo[] prms, Expression[] availableArgs) {
            if (prms.Length != availableArgs.Length) return false;

            for (var i = 0; i < prms.Length; i++) {
                if (availableArgs[i] != null && prms[i].ParameterType != availableArgs[i].Type)
                    return false;
            }

            return true;
        }
    }
}