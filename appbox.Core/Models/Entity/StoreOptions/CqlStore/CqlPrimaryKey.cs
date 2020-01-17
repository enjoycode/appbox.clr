using System;
using System.Linq;
using appbox.Serialization;

namespace appbox.Models
{
    /// <summary>
    /// 表存储主键，包含PartitionKeys, ClusteringColumns及其排序
    /// </summary>
    public sealed class CqlPrimaryKey
    {

        public ushort[] PartitionKeys { get; internal set; }
        public FieldWithOrder[] ClusteringColumns { get; internal set; }

        /// <summary>
        /// 判断成员名称是否主键成员
        /// </summary>
        public bool IsPrimaryKey(ushort mid)
        {
            if (PartitionKeys.Contains(mid))
                return true;
            if (ClusteringColumns != null)
            {
                for (int i = 0; i < ClusteringColumns.Length; i++)
                {
                    if (ClusteringColumns[i].MemberId == mid)
                        return true;
                }
            }
            return false;
        }

        internal void Validate()
        {
            if (PartitionKeys == null || PartitionKeys.Length == 0)
                throw new Exception("Partition key is empty.");
        }

        internal void WriteObject(BinSerializer bs)
        {
            bs.Write(PartitionKeys);
            if (ClusteringColumns != null)
            {
                bs.Write(ClusteringColumns.Length);
                for (int i = 0; i < ClusteringColumns.Length; i++)
                {
                    bs.Write(ClusteringColumns[i].MemberId);
                    bs.Write(ClusteringColumns[i].OrderByDesc);
                }
            }
            else
                bs.Write(0);
        }

        internal void ReadObject(BinSerializer bs)
        {
            PartitionKeys = bs.ReadUInt16Array();
            int count = bs.ReadInt32();
            if (count > 0)
            {
                ClusteringColumns = new FieldWithOrder[count];
                for (int i = 0; i < count; i++)
                {
                    ClusteringColumns[i] = new FieldWithOrder()
                    {
                        MemberId = bs.ReadUInt16(),
                        OrderByDesc = bs.ReadBoolean()
                    };
                }
            }
        }

        internal void WriteToJson(System.Text.Json.Utf8JsonWriter writer)
        {
            writer.WritePropertyName(nameof(PartitionKeys));
            writer.WriteStartArray();
            if (PartitionKeys != null)
            {
                for (int i = 0; i < PartitionKeys.Length; i++)
                {
                    writer.WriteNumberValue(PartitionKeys[i]);
                }
            }
            writer.WriteEndArray();

            writer.WritePropertyName(nameof(ClusteringColumns));
            if (ClusteringColumns != null && ClusteringColumns.Length > 0)
            {
                writer.WriteStartArray();
                for (int i = 0; i < ClusteringColumns.Length; i++)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("MemberId", ClusteringColumns[i].MemberId);
                    writer.WriteBoolean("OrderByDesc", ClusteringColumns[i].OrderByDesc);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            else
                writer.WriteEmptyArray();
        }

    }

}
