using System;
using System.Collections.Generic;

namespace appbox.Reflection
{

    //public delegate TValue DynamicPropertyGetter<in TObject, out TValue>(TObject instance) where TObject : IDynamicObject;
    //public delegate void DynamicPropertySetter<in TObject, in TValue>(TObject instance, TValue value) where TObject : IDynamicObject;

    public sealed class DynamicMembers
    {
        private DynamicMembers baseMembers;
        private Dictionary<string, IDynamicProperty> properties;
        private Dictionary<string, DynamicMethod> methods;

        public DynamicMembers() { }

        public DynamicMembers(DynamicMembers baseMembers)
        {
            this.baseMembers = baseMembers;
        }

        public void RegisterProperty<TObject, TValue>(string propName,
                                                      Func<TObject, TValue> getter,
                                                      Action<TObject, TValue> setter = null) where TObject : IDynamicObject
        {
            if (properties == null)
                properties = new Dictionary<string, IDynamicProperty>();
            else if (properties.ContainsKey(propName))
                throw new Exception("Property has registed: " + propName);

            if (setter == null)
                properties.Add(propName, new DynamicProperty<TValue>(t => getter((TObject)t), null));
            else
                properties.Add(propName, new DynamicProperty<TValue>(t => getter((TObject)t), (t, v) => setter((TObject)t, v)));
        }

        public void RegisterMethod<TObject>(string methodName, Func<TObject, object[], object> invoker) where TObject : IDynamicObject
        {
            if (methods == null)
                methods = new Dictionary<string, DynamicMethod>();
            else if (methods.ContainsKey(methodName))
                throw new Exception("Method has registed: " + methodName);

            methods.Add(methodName, new DynamicMethod((obj, args) => invoker((TObject)obj, args)));
        }

        public T GetPropertyValue<T>(string propName, IDynamicObject instance)
        {
            IDynamicProperty prop = null;
            if (properties.TryGetValue(propName, out prop))
            {
                DynamicProperty<T> dprop = prop as DynamicProperty<T>;
                if (dprop != null)
                    return dprop.GetValue(instance);
                else
                    throw new Exception("dynamic property value type not same: " + typeof(T).Name);
            }
            else
            {
                if (baseMembers != null)
                    return baseMembers.GetPropertyValue<T>(propName, instance);
                else
                    throw new Exception("Cannot find dynamic property: " + propName);
            }
        }

        public void SetPropertyValue<T>(string propName, IDynamicObject instance, T value)
        {
            IDynamicProperty prop = null;
            if (properties.TryGetValue(propName, out prop))
            {
                DynamicProperty<T> dprop = prop as DynamicProperty<T>;
                if (dprop != null)
                    dprop.SetValue(instance, value);
                else
                    throw new Exception("dynamic property value type not same: " + typeof(T).Name);
            }
            else
            {
                if (baseMembers != null)
                    baseMembers.SetPropertyValue<T>(propName, instance, value);
                else
                    throw new Exception("Cannot find dynamic property: " + propName);
            }
        }

        public object GetBoxedPropertyValue(string propName, IDynamicObject instance)
        {
            IDynamicProperty prop = null;
            if (properties.TryGetValue(propName, out prop))
            {
                return prop.GetBoxedValue(instance);
            }
            else
            {
                if (baseMembers != null)
                    return baseMembers.GetBoxedPropertyValue(propName, instance);
                else
                    throw new Exception("Cannot find dynamic property: " + propName);
            }
        }

        public void SetBoxedPropertyValue(string propName, IDynamicObject instance, object value)
        {
            IDynamicProperty prop = null;
            if (properties.TryGetValue(propName, out prop))
            {
                prop.SetBoxedValue(instance, value);
            }
            else
            {
                if (baseMembers != null)
                    baseMembers.SetBoxedPropertyValue(propName, instance, value);
                else
                    throw new Exception("Cannot find dynamic property: " + propName);
            }
        }

        public object InvokeMethod(string methodName, IDynamicObject instance, object[] args)
        {
            DynamicMethod method = null;
            if (methods.TryGetValue(methodName, out method))
            {
                return method.Invoke(instance, args);
            }
            else
            {
                if (baseMembers != null)
                    return baseMembers.InvokeMethod(methodName, instance, args);
                else
                    throw new Exception("Cannot find dynamic method: " + methodName);
            }
        }
    }

    public interface IDynamicProperty
    {

        object GetBoxedValue(IDynamicObject instance);

        void SetBoxedValue(IDynamicObject instance, object value);

    }

    public sealed class DynamicProperty<T> : IDynamicProperty
    {
        private Func<IDynamicObject, T> getter;
        private Action<IDynamicObject, T> setter;

        public DynamicProperty(Func<IDynamicObject, T> getter, Action<IDynamicObject, T> setter)
        {
            this.getter = getter;
            this.setter = setter;
        }

        public T GetValue(IDynamicObject instance)
        {
            if (getter == null)
                throw new Exception("DynamicProperty has no getter.");
            return getter(instance);
        }

        public void SetValue(IDynamicObject instance, T value)
        {
            if (setter == null)
                throw new Exception("DynamicProperty has no setter.");
            setter(instance, value);
        }

        public object GetBoxedValue(IDynamicObject instance)
        {
            return GetValue(instance);
        }

        public void SetBoxedValue(IDynamicObject instance, object value)
        {
            SetValue(instance, (T)value);
        }

    }

    public sealed class DynamicMethod
    {
        private Func<IDynamicObject, object[], object> invoker;

        public DynamicMethod(Func<IDynamicObject, object[], object> invoker)
        {
            this.invoker = invoker;
        }

        public object Invoke(IDynamicObject instance, object[] args)
        {
            return this.invoker(instance, args);
        }
    }
}

