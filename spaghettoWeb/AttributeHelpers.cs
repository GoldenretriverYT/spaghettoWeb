using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace spaghettoWeb
{
    internal static class AttributeHelpers
    {
        public static T GetAttribute<T>(this PropertyInfo prop) where T : Attribute
        {
            object[] attrs = prop.GetCustomAttributes(true);
            foreach (object attr in attrs)
            {
                T castedAttr = attr as T;
                if (castedAttr != null)
                {
                    return castedAttr;
                }
            }

            return default(T);
        }

        public static List<Attribute> GetAttributes(this PropertyInfo prop)
        {
            List<Attribute> list = new List<Attribute>();
            object[] attrs = prop.GetCustomAttributes(true);
            foreach (object attr in attrs)
            {
                list.Add((Attribute)attr);
            }

            return list;
        }

        public static List<T> GetAttributes<T>(this PropertyInfo prop) where T : Attribute
        {
            List<T> list = new List<T>();
            object[] attrs = prop.GetCustomAttributes(true);
            foreach (object attr in attrs)
            {
                if(attr is T) list.Add((T)attr);
            }

            return list;
        }

        public static object GetDefaultValueForProperty(this PropertyInfo property)
        {
            var defaultAttr = property.GetCustomAttribute(typeof(DefaultValueAttribute));
            if (defaultAttr != null)
                return (defaultAttr as DefaultValueAttribute).Value;

            var propertyType = property.PropertyType;
            return propertyType.IsValueType ? Activator.CreateInstance(propertyType) : null;
        }
    }
}
