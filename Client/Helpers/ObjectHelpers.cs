using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LateToTheParty.Helpers
{
    public static class ObjectHelpers
    {
        public static bool IsSubclassOfType(this object obj, Type type)
        {
            Type objType = obj.GetType();

            if (objType == type)
            {
                return true;
            }

            if (objType.IsSubclassOf(type))
            {
                return true;
            }

            return false;
        }
    }
}
