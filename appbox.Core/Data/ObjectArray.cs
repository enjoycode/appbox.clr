using System;
using System.Linq;
using System.Collections.Generic;
using appbox.Serialization;

namespace appbox.Data
{
    public sealed class ObjectArray : List<object>, /*IDynamicObject,*/ IBinSerializable
    {
        public ObjectArray() { }

        #region ====IDynamicObject====
        //private static readonly DynamicMembers DynamicMembers;

        static ObjectArray()
        {
            //DynamicMembers = new DynamicMembers();
            ////DynamicMembers.RegisterProperty<ObjectArray, string>(nameof(Name), t => t.Name);
            ////DynamicMembers.RegisterProperty<ObjectArray, bool>(nameof(HasPermission), t => t.HasPermission);
            ////DynamicMembers.RegisterProperty<ObjectArray, bool>(nameof(IsFolder), t => t.IsFolder);
            ////DynamicMembers.RegisterProperty<ObjectArray, bool>(nameof(IsInherit), t => t.IsInherit);
            ////DynamicMembers.RegisterProperty<ObjectArray, List<PermissionNode>>(nameof(Childs), t => t.Childs);

            //DynamicMembers.RegisterMethod<ObjectArray>(nameof(Add), (t, args) => { t.Add(args[0]); return null; });
            //DynamicMembers.RegisterMethod<ObjectArray>(nameof(Remove), (t, args) => { t.Remove(args[0]); return null; });
            //DynamicMembers.RegisterMethod<ObjectArray>(nameof(ToArray), (t, args) => { return t.ToArray(); });
        }

        //object IDynamicObject.GetBoxedPropertyValue(string propertyName)
        //{
        //    return DynamicMembers.GetBoxedPropertyValue(propertyName, this);
        //}

        //void IDynamicObject.SetBoxedPropertyValue(string propertyName, object value)
        //{
        //    DynamicMembers.SetBoxedPropertyValue(propertyName, this, value);
        //}

        //T IDynamicObject.GetPropertyValue<T>(string propertyName)
        //{
        //    return DynamicMembers.GetPropertyValue<T>(propertyName, this);
        //}

        //void IDynamicObject.SetPropertyValue<T>(string propertyName, T value)
        //{
        //    DynamicMembers.SetPropertyValue<T>(propertyName, this, value);
        //}

        //object IDynamicObject.InvokeMethod(string methodName, object[] args)
        //{
        //    return DynamicMembers.InvokeMethod(methodName, this, args);
        //}

        //void IDynamicObject.AddEventHandler(string eventName, EventAction action)
        //{
        //    throw ExceptionHelper.NotImplemented();
        //}
        #endregion

        #region ====IBinSerializable====
        void IBinSerializable.WriteObject(BinSerializer writer)
        {
            writer.Write(this.Count);
            for (int i = 0; i < this.Count; i++)
            {
                writer.Serialize(this[i]);
            }
        }

        void IBinSerializable.ReadObject(BinSerializer reader)
        {
            var count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                this.Add(reader.Deserialize());
            }
        }
        #endregion

        #region ====显示类型转换====
        public static explicit operator int[] (ObjectArray value)
        {
            if (value == null) return null;
            var array = new int[value.Count];
            for (int i = 0; i < value.Count; i++)
            {
                array[i] = System.Convert.ToInt32(value[i]);
            }
            return array;
        }

        public static explicit operator string[] (ObjectArray value)
        {
            if (value == null) return null;
            return value.Cast<string>().ToArray();
        }

        public static explicit operator float[] (ObjectArray value)
        {
            if (value == null) return null;
            return value.Cast<float>().ToArray();
        }

        public static explicit operator double[] (ObjectArray value)
        {
            if (value == null) return null;
            return value.Cast<double>().ToArray();
        }

        public static explicit operator decimal[] (ObjectArray value)
        {
            if (value == null) return null;
            return value.Cast<decimal>().ToArray();
        }

        public static explicit operator Guid[] (ObjectArray value)
        {
            if (value == null) return null;
            var array = new Guid[value.Count];
            for (int i = 0; i < value.Count; i++)
            {
                array[i] = Guid.Parse((string)value[i]);
            }
            return array;
        }

        public static explicit operator DateTime[] (ObjectArray value)
        {
            if (value == null) return null;
            return value.Cast<DateTime>().ToArray();
        }

        public static explicit operator byte[] (ObjectArray value)
        {
            if (value == null) return null;
            return value.Cast<byte>().ToArray();
        }
        #endregion
    }
}
