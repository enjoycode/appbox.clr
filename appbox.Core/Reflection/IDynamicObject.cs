using System;
using System.Threading.Tasks;
using appbox.Expressions;

namespace appbox.Reflection
{
    public interface IDynamicObject
    {
        object GetBoxedPropertyValue(string propertyName);
        void SetBoxedPropertyValue(string propertyName, object value);
        T GetPropertyValue<T>(string propertyName);
        void SetPropertyValue<T>(string propertyName, T value);
        object InvokeMethod(string methodName, object[] args);
        Task<object> InvokeMethodAsync(string methodName, object[] args);
        void AddEventHandler(string eventName, EventAction action);
    }
}

