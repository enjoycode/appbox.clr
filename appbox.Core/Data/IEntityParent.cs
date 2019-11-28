using System;

namespace appbox.Data
{
    /// <summary>
    /// 用于Entity.PropertyChanged后向上传播事件，只有Entity及EntityList实现此接口
    /// </summary>
    /// <example>
    /// 1. Entity -> (EntityRef) -> Entity
    /// 2. Entity -> (EntitySet) -> EntityList -> [Entity1, Entity2]
    /// 3. EntityList -> [Entity1, Entity2]
    /// </example>
    public interface IEntityParent
    {
        IEntityParent Parent { get; }

        /// <summary>
        /// 所拥有的实体实例的属性发生变更
        /// </summary>
        void OnChildPropertyChanged(Entity child, ushort childPropertyId);

        /// <summary>
        /// EntitySet集合发生变更
        /// </summary>
        void OnChildListChanged(EntityList childList);
    }
}
