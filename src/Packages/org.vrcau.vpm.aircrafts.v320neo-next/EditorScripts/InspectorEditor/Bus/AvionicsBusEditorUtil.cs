using System;
using System.Collections.Generic;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Editor.InspectorEditor.Bus
{
    internal static class AvionicsBusEditorUtil
    {
        internal sealed class DataIdInfo
        {
            public string Name;
            public int DataType;
            public int Id;
        }

        internal static readonly string[] TypeFilterLabels = { "全部", "Float", "Int", "Bool", "String", "Vector3", "Byte" };
        internal static readonly string[] TypeNames = { "Float", "Int", "Bool", "String", "Vector3", "Byte" };

        private static List<DataIdInfo> _catalog;

        internal static List<DataIdInfo> GetCatalog()
        {
            if (_catalog != null) return _catalog;

            _catalog = new List<DataIdInfo>();

            AddEnumToCatalog(typeof(AvionicsBusFloatDataIds), (int)AvionicsBusDataType.Float);
            AddEnumToCatalog(typeof(AvionicsBusByteDataIds), (int)AvionicsBusDataType.Byte);
            AddEnumToCatalog(typeof(AvionicsBusIntDataIds), (int)AvionicsBusDataType.Int);
            AddEnumToCatalog(typeof(AvionicsBusBoolDataIds), (int)AvionicsBusDataType.Bool);
            AddEnumToCatalog(typeof(AvionicsBusStringDataIds), (int)AvionicsBusDataType.String);
            AddEnumToCatalog(typeof(AvionicsBusVector3DataIds), (int)AvionicsBusDataType.Vector3);

            return _catalog;
        }

        internal static void AddEnumToCatalog(Type enumType, int dataType)
        {
            foreach (string name in Enum.GetNames(enumType))
            {
                if (name == "Count") continue;

                _catalog.Add(new DataIdInfo
                {
                    Name = name,
                    DataType = dataType,
                    Id = (int)Enum.Parse(enumType, name)
                });
            }
        }

        internal static string TypeName(int dataType)
        {
            if (dataType < 0 || dataType >= TypeNames.Length) return "?";

            return TypeNames[dataType];
        }

        internal static string GetDataIdName(int dataType, int id)
        {
            foreach (DataIdInfo info in GetCatalog())
            {
                if (info.DataType == dataType && info.Id == id) return info.Name;
            }

            return "<未知：类型 " + dataType + "，id " + id + ">";
        }

        internal static bool TryGetDataIdInfo(int dataType, int id, out DataIdInfo result)
        {
            foreach (DataIdInfo info in GetCatalog())
            {
                if (info.DataType == dataType && info.Id == id)
                {
                    result = info;
                    return true;
                }
            }

            result = null;
            return false;
        }

        internal static int[] AppendInt(int[] source, int value)
        {
            if (source == null) source = new int[0];

            int[] result = new int[source.Length + 1];
            Array.Copy(source, result, source.Length);
            result[source.Length] = value;

            return result;
        }

        internal static int[] RemoveIntAt(int[] source, int index)
        {
            if (source == null) return new int[0];
            if (index < 0 || index >= source.Length) return source;

            int[] result = new int[source.Length - 1];

            for (int i = 0, j = 0; i < source.Length; i++)
            {
                if (i == index) continue;

                result[j] = source[i];
                j++;
            }

            return result;
        }

        internal static int[] MoveInt(int[] source, int index, int offset)
        {
            if (source == null) return new int[0];

            int target = index + offset;
            if (index < 0 || index >= source.Length || target < 0 || target >= source.Length) return source;

            int[] result = new int[source.Length];
            Array.Copy(source, result, source.Length);

            int value = result[index];
            result[index] = result[target];
            result[target] = value;

            return result;
        }

        internal static string[] AppendString(string[] source, string value)
        {
            if (source == null) source = new string[0];

            string[] result = new string[source.Length + 1];
            Array.Copy(source, result, source.Length);
            result[source.Length] = value;

            return result;
        }

        internal static string[] RemoveStringAt(string[] source, int index)
        {
            if (source == null) return new string[0];
            if (index < 0 || index >= source.Length) return source;

            string[] result = new string[source.Length - 1];

            for (int i = 0, j = 0; i < source.Length; i++)
            {
                if (i == index) continue;

                result[j] = source[i];
                j++;
            }

            return result;
        }

        internal static string[] MoveString(string[] source, int index, int offset)
        {
            if (source == null) return new string[0];

            int target = index + offset;
            if (index < 0 || index >= source.Length || target < 0 || target >= source.Length) return source;

            string[] result = new string[source.Length];
            Array.Copy(source, result, source.Length);

            string value = result[index];
            result[index] = result[target];
            result[target] = value;

            return result;
        }
    }
}
