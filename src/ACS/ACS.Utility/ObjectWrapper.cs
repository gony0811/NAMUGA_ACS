using System;
using System.Reflection;

namespace ACS.Utility
{
    /// <summary>
    /// Replacement for Spring.Objects.ObjectWrapper.
    /// Provides reflection-based property access on wrapped objects.
    /// </summary>
    public class ObjectWrapper
    {
        private readonly object _instance;

        public ObjectWrapper(object instance)
        {
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        public Type WrappedType => _instance.GetType();

        public object WrappedInstance => _instance;

        public PropertyInfo GetPropertyInfo(string name)
        {
            return WrappedType.GetProperty(name);
        }

        public object GetPropertyValue(string name)
        {
            var prop = WrappedType.GetProperty(name);
            return prop?.GetValue(_instance);
        }

        public void SetPropertyValue(string name, object value)
        {
            var prop = WrappedType.GetProperty(name);
            if (prop != null && prop.CanWrite)
            {
                object convertedValue = value;
                if (value != null && !prop.PropertyType.IsAssignableFrom(value.GetType()))
                {
                    try
                    {
                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        convertedValue = Convert.ChangeType(value, targetType);
                    }
                    catch
                    {
                        convertedValue = value;
                    }
                }
                prop.SetValue(_instance, convertedValue);
            }
        }
    }
}
