using System;
using System.Globalization;
using System.Text;
using UdonSharp;
using UnityEngine;

namespace VAU.V320NeoNext.Runtime.Bus
{
    public enum AvionicsBusSamplingMethod
    {
        FixedUpdate = 0,
        Update = 1,
        LateUpdate = 2
    }

    public enum AvionicsBusRecorderState
    {
        Idle = 0,
        Recording = 1,
        Finished = 2,
        Error = 3
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AvionicsBusRecorder : AbstractAvionicsBusClient
    {
        private const int DefaultMaxSamples = 36000;
        private const int MaxSampleCapacity = 1000000;
        private const int MaxSampleSlots = 10000000;
        private const int SchemaVersion = 1;

        [Tooltip("数据类型，与 exportIds、exportNames 一一对应")]
        public int[] exportDataTypes = { };

        [Tooltip("数据 id，与 exportDataTypes、exportNames 一一对应")]
        public int[] exportIds = { };

        [Tooltip("JSON 中显示的参数名；留空时使用 类型:id")]
        public string[] exportNames = { };

        public AvionicsBusSamplingMethod samplingMethod = AvionicsBusSamplingMethod.Update;

        [Tooltip("0 表示每次采样回调都记录")]
        public float sampleRateHz = 0f;

        public int maxSamples = DefaultMaxSamples;

        public bool autoStartOnBusStart = false;

        [Tooltip("0 表示不自动停止")]
        public float autoStopAfterSeconds = 0f;

        public bool logExportedJson = false;

        [HideInInspector] public string editorExportPath = "";

        [NonSerialized] public string exportedJson = "";
        [NonSerialized] public int _recordingState = 0;
        [NonSerialized] public int _sampleCount = 0;
        [NonSerialized] public double _recordedDuration = 0.0;
        [NonSerialized] public bool _overflowed = false;
        [NonSerialized] public string _lastError = "";

        private bool _recording;
        private int _capacity;
        private int _paramCount;
        private int _floatCount;
        private int _byteCount;
        private int _intCount;
        private int _boolCount;
        private int _stringCount;
        private int _vector3Count;
        private int[] _paramTypes = { };
        private int[] _paramIds = { };
        private string[] _paramNames = { };
        private int[] _paramTypeIndex = { };
        private double[] _times = { };
        private float[] _floatSamples = { };
        private byte[] _byteSamples = { };
        private int[] _intSamples = { };
        private bool[] _boolSamples = { };
        private string[] _stringSamples = { };
        private Vector3[] _vector3Samples = { };
        private double _startClock;
        private double _lastSampleClock;

        protected override void _OnAvionicsBusStart()
        {
            if (autoStartOnBusStart && !_recording)
            {
                _StartRecording();
            }
        }

        private void FixedUpdate()
        {
            if (!_recording || samplingMethod != AvionicsBusSamplingMethod.FixedUpdate) return;

            _Tick(Time.fixedTimeAsDouble);
        }

        private void Update()
        {
            if (!_recording || samplingMethod != AvionicsBusSamplingMethod.Update) return;

            _Tick(Time.timeAsDouble);
        }

        private void LateUpdate()
        {
            if (!_recording || samplingMethod != AvionicsBusSamplingMethod.LateUpdate) return;

            _Tick(Time.timeAsDouble);
        }

        private void _Tick(double now)
        {
            if (autoStopAfterSeconds > 0f && now - _startClock >= (double)autoStopAfterSeconds)
            {
                _StopRecording();
                return;
            }

            if (sampleRateHz > 0f)
            {
                double interval = 1.0 / (double)sampleRateHz;

                if (now - _lastSampleClock < interval) return;
            }

            _Sample(now);
        }

        private void _Sample(double now)
        {
            if (_sampleCount >= _capacity)
            {
                _overflowed = true;
                _StopRecording();
                return;
            }

            int sampleIndex = _sampleCount;
            _times[sampleIndex] = now - _startClock;

            for (int i = 0; i < _paramCount; i++)
            {
                int type = _paramTypes[i];
                int id = _paramIds[i];
                int index = _paramTypeIndex[i] * _capacity + sampleIndex;

                if (type == (int)AvionicsBusDataType.Float)
                {
                    _floatSamples[index] = _ReadFloat((AvionicsBusFloatDataIds)id);
                }
                else if (type == (int)AvionicsBusDataType.Byte)
                {
                    _byteSamples[index] = _ReadByte((AvionicsBusByteDataIds)id);
                }
                else if (type == (int)AvionicsBusDataType.Int)
                {
                    _intSamples[index] = _ReadInt((AvionicsBusIntDataIds)id);
                }
                else if (type == (int)AvionicsBusDataType.Bool)
                {
                    _boolSamples[index] = _ReadBool((AvionicsBusBoolDataIds)id);
                }
                else if (type == (int)AvionicsBusDataType.String)
                {
                    _stringSamples[index] = _ReadString((AvionicsBusStringDataIds)id);
                }
                else if (type == (int)AvionicsBusDataType.Vector3)
                {
                    _vector3Samples[index] = _ReadVector3((AvionicsBusVector3DataIds)id);
                }
            }

            _sampleCount = sampleIndex + 1;
            _lastSampleClock = now;

            if (_sampleCount >= _capacity)
            {
                _overflowed = true;
                _StopRecording();
            }
        }

        public void _StartRecording()
        {
            if (_recording)
            {
                Debug.LogWarning("[AvionicsBusRecorder] 已经在采样中，忽略重复的启动请求。");
                return;
            }

            if (samplingMethod != AvionicsBusSamplingMethod.FixedUpdate &&
                samplingMethod != AvionicsBusSamplingMethod.Update &&
                samplingMethod != AvionicsBusSamplingMethod.LateUpdate)
            {
                samplingMethod = AvionicsBusSamplingMethod.Update;
            }

            if (sampleRateHz < 0f) sampleRateHz = 0f;
            if (float.IsNaN(sampleRateHz) || float.IsInfinity(sampleRateHz)) sampleRateHz = 0f;

            if (!_BuildParameterCache())
            {
                _recordingState = (int)AvionicsBusRecorderState.Error;
                Debug.LogWarning("[AvionicsBusRecorder] 参数配置无效：" + _lastError);
                return;
            }

            _AllocateBuffers();

            _recording = true;
            _recordingState = (int)AvionicsBusRecorderState.Recording;
            _sampleCount = 0;
            _recordedDuration = 0.0;
            _overflowed = false;
            _lastError = "";
            exportedJson = "";
            _startClock = _Now();
            _lastSampleClock = -1e30;
        }

        public void _StopRecording()
        {
            if (!_recording) return;

            _recording = false;
            _recordingState = (int)AvionicsBusRecorderState.Finished;

            if (_sampleCount <= 0)
            {
                _recordingState = (int)AvionicsBusRecorderState.Error;
                _lastError = "没有采样到任何数据。";
                exportedJson = "";
                Debug.LogWarning("[AvionicsBusRecorder] " + _lastError);
                return;
            }

            _recordedDuration = _times[_sampleCount - 1];
            exportedJson = _BuildJson();
            _recordingState = (int)AvionicsBusRecorderState.Finished;

            if (logExportedJson)
            {
                Debug.Log("[AvionicsBusRecorder] " + exportedJson);
            }
        }

        public void _AbortRecording()
        {
            if (!_recording) return;

            _recording = false;
            _recordingState = (int)AvionicsBusRecorderState.Idle;
            _sampleCount = 0;
            _recordedDuration = 0.0;
            _overflowed = false;
            _lastError = "";
            exportedJson = "";
            _ReleaseBuffers();
        }

        public void _ClearExport()
        {
            if (_recording) _AbortRecording();

            _recordingState = (int)AvionicsBusRecorderState.Idle;
            _sampleCount = 0;
            _recordedDuration = 0.0;
            _overflowed = false;
            _lastError = "";
            exportedJson = "";
            _ReleaseBuffers();
        }

        public void _SetSamplingMethod(int method)
        {
            if (_recording)
            {
                Debug.LogWarning("[AvionicsBusRecorder] 采样中不能修改采样方式。");
                return;
            }

            if (method < (int)AvionicsBusSamplingMethod.FixedUpdate ||
                method > (int)AvionicsBusSamplingMethod.LateUpdate)
            {
                Debug.LogWarning("[AvionicsBusRecorder] 无效的采样方式：" + method);
                return;
            }

            samplingMethod = (AvionicsBusSamplingMethod)method;
        }

        public void _SetSampleRateHz(float hz)
        {
            if (_recording)
            {
                Debug.LogWarning("[AvionicsBusRecorder] 采样中不能修改采样率。");
                return;
            }

            sampleRateHz = hz < 0f || float.IsNaN(hz) || float.IsInfinity(hz) ? 0f : hz;
        }

        public void _SetMaxSamples(int value)
        {
            if (_recording)
            {
                Debug.LogWarning("[AvionicsBusRecorder] 采样中不能修改最大样本数。");
                return;
            }

            maxSamples = value;
        }

        public void _SetAutoStopAfterSeconds(float seconds)
        {
            autoStopAfterSeconds = seconds < 0f || float.IsNaN(seconds) || float.IsInfinity(seconds) ? 0f : seconds;
        }

        public void _SetLogExportedJson(bool value)
        {
            logExportedJson = value;
        }

        public void _ApplyExportParametersFromFields()
        {
            if (_recording)
            {
                Debug.LogWarning("[AvionicsBusRecorder] 采样中不能修改参数列表。");
                return;
            }

            if (!_BuildParameterCache())
            {
                _recordingState = (int)AvionicsBusRecorderState.Error;
                Debug.LogWarning("[AvionicsBusRecorder] 参数配置无效：" + _lastError);
                return;
            }

            _recordingState = (int)AvionicsBusRecorderState.Idle;
            _lastError = "";
        }

        public void _AddExportParameter(int dataType, int id, string name)
        {
            if (_recording)
            {
                Debug.LogWarning("[AvionicsBusRecorder] 采样中不能修改参数列表。");
                return;
            }

            _EnsureConfigArrays();

            int count = exportDataTypes.Length;
            if (exportIds.Length < count) count = exportIds.Length;

            for (int i = 0; i < count; i++)
            {
                if (exportDataTypes[i] == dataType && exportIds[i] == id) return;
            }

            int[] types = new int[count + 1];
            int[] ids = new int[count + 1];
            string[] names = new string[count + 1];

            for (int i = 0; i < count; i++)
            {
                types[i] = exportDataTypes[i];
                ids[i] = exportIds[i];
                names[i] = i < exportNames.Length ? exportNames[i] : "";
            }

            types[count] = dataType;
            ids[count] = id;
            names[count] = name == null ? "" : name;

            exportDataTypes = types;
            exportIds = ids;
            exportNames = names;
        }

        public void _RemoveExportParameter(int dataType, int id)
        {
            if (_recording)
            {
                Debug.LogWarning("[AvionicsBusRecorder] 采样中不能修改参数列表。");
                return;
            }

            _EnsureConfigArrays();

            int count = exportDataTypes.Length;
            if (exportIds.Length < count) count = exportIds.Length;

            int removeIndex = -1;

            for (int i = 0; i < count; i++)
            {
                if (exportDataTypes[i] == dataType && exportIds[i] == id)
                {
                    removeIndex = i;
                    break;
                }
            }

            if (removeIndex < 0) return;

            int[] types = new int[count - 1];
            int[] ids = new int[count - 1];
            string[] names = new string[count - 1];

            for (int i = 0, j = 0; i < count; i++)
            {
                if (i == removeIndex) continue;

                types[j] = exportDataTypes[i];
                ids[j] = exportIds[i];
                names[j] = i < exportNames.Length ? exportNames[i] : "";
                j++;
            }

            exportDataTypes = types;
            exportIds = ids;
            exportNames = names;
        }

        public void _ClearExportParameters()
        {
            if (_recording)
            {
                Debug.LogWarning("[AvionicsBusRecorder] 采样中不能修改参数列表。");
                return;
            }

            exportDataTypes = new int[0];
            exportIds = new int[0];
            exportNames = new string[0];
        }

        private void _EnsureConfigArrays()
        {
            if (exportDataTypes == null) exportDataTypes = new int[0];
            if (exportIds == null) exportIds = new int[0];
            if (exportNames == null) exportNames = new string[0];
        }

        private bool _BuildParameterCache()
        {
            _EnsureConfigArrays();

            int count = exportDataTypes.Length;
            if (exportIds.Length < count) count = exportIds.Length;

            _paramTypes = new int[count];
            _paramIds = new int[count];
            _paramNames = new string[count];
            _paramTypeIndex = new int[count];

            _paramCount = 0;
            _floatCount = 0;
            _byteCount = 0;
            _intCount = 0;
            _boolCount = 0;
            _stringCount = 0;
            _vector3Count = 0;

            for (int i = 0; i < count; i++)
            {
                int type = exportDataTypes[i];
                int id = exportIds[i];

                if (!_IsTypeValid(type) || !_IsIdInRange(type, id)) continue;
                if (_IsDuplicate(type, id)) continue;

                _paramTypes[_paramCount] = type;
                _paramIds[_paramCount] = id;
                _paramNames[_paramCount] = i < exportNames.Length && exportNames[i] != null ? exportNames[i] : "";

                if (type == (int)AvionicsBusDataType.Float)
                {
                    _paramTypeIndex[_paramCount] = _floatCount;
                    _floatCount++;
                }
                else if (type == (int)AvionicsBusDataType.Byte)
                {
                    _paramTypeIndex[_paramCount] = _byteCount;
                    _byteCount++;
                }
                else if (type == (int)AvionicsBusDataType.Int)
                {
                    _paramTypeIndex[_paramCount] = _intCount;
                    _intCount++;
                }
                else if (type == (int)AvionicsBusDataType.Bool)
                {
                    _paramTypeIndex[_paramCount] = _boolCount;
                    _boolCount++;
                }
                else if (type == (int)AvionicsBusDataType.String)
                {
                    _paramTypeIndex[_paramCount] = _stringCount;
                    _stringCount++;
                }
                else
                {
                    _paramTypeIndex[_paramCount] = _vector3Count;
                    _vector3Count++;
                }

                _paramCount++;
            }

            if (_paramCount == 0)
            {
                _lastError = "没有可导出的有效参数。";
                return false;
            }

            return true;
        }

        private bool _IsDuplicate(int type, int id)
        {
            for (int i = 0; i < _paramCount; i++)
            {
                if (_paramTypes[i] == type && _paramIds[i] == id) return true;
            }

            return false;
        }

        private bool _IsTypeValid(int type)
        {
            return type >= (int)AvionicsBusDataType.Float && type <= (int)AvionicsBusDataType.Byte;
        }

        private bool _IsIdInRange(int type, int id)
        {
            if (id < 0) return false;

            if (type == (int)AvionicsBusDataType.Float) return id < (int)AvionicsBusFloatDataIds.Count;
            if (type == (int)AvionicsBusDataType.Byte) return id < (int)AvionicsBusByteDataIds.Count;
            if (type == (int)AvionicsBusDataType.Int) return id < (int)AvionicsBusIntDataIds.Count;
            if (type == (int)AvionicsBusDataType.Bool) return id < (int)AvionicsBusBoolDataIds.Count;
            if (type == (int)AvionicsBusDataType.String) return id < (int)AvionicsBusStringDataIds.Count;
            if (type == (int)AvionicsBusDataType.Vector3) return id < (int)AvionicsBusVector3DataIds.Count;

            return false;
        }

        private void _AllocateBuffers()
        {
            _capacity = maxSamples;

            if (_capacity <= 0) _capacity = DefaultMaxSamples;
            if (_capacity > MaxSampleCapacity) _capacity = MaxSampleCapacity;

            if (_paramCount > 0 && _capacity > MaxSampleSlots / _paramCount)
            {
                _capacity = MaxSampleSlots / _paramCount;
                if (_capacity < 1) _capacity = 1;
                Debug.LogWarning("[AvionicsBusRecorder] 采样缓冲过大，已自动降低最大样本数到 " + _capacity + "。");
            }

            _times = new double[_capacity];
            _floatSamples = _floatCount > 0 ? new float[_capacity * _floatCount] : new float[0];
            _byteSamples = _byteCount > 0 ? new byte[_capacity * _byteCount] : new byte[0];
            _intSamples = _intCount > 0 ? new int[_capacity * _intCount] : new int[0];
            _boolSamples = _boolCount > 0 ? new bool[_capacity * _boolCount] : new bool[0];
            _stringSamples = _stringCount > 0 ? new string[_capacity * _stringCount] : new string[0];
            _vector3Samples = _vector3Count > 0 ? new Vector3[_capacity * _vector3Count] : new Vector3[0];
        }

        private void _ReleaseBuffers()
        {
            _capacity = 0;
            _times = new double[0];
            _floatSamples = new float[0];
            _byteSamples = new byte[0];
            _intSamples = new int[0];
            _boolSamples = new bool[0];
            _stringSamples = new string[0];
            _vector3Samples = new Vector3[0];
        }

        private double _Now()
        {
            if (samplingMethod == AvionicsBusSamplingMethod.FixedUpdate) return Time.fixedTimeAsDouble;

            return Time.timeAsDouble;
        }

        private string _BuildJson()
        {
            StringBuilder builder = new StringBuilder(_EstimateBuilderCapacity());

            builder.Append("{\"schemaVersion\":");
            _AppendJsonInt(builder, SchemaVersion);
            builder.Append(",\"source\":\"AvionicsBusRecorder\"");
            builder.Append(",\"recordedAtUtc\":");
            _AppendJsonString(builder, DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            builder.Append(",\"samplingMethod\":");
            _AppendJsonString(builder, _SamplingMethodName(samplingMethod));
            builder.Append(",\"sampleRateHz\":");
            _AppendJsonFloat(builder, sampleRateHz > 0f ? sampleRateHz : 0f);
            builder.Append(",\"sampleCount\":");
            _AppendJsonInt(builder, _sampleCount);
            builder.Append(",\"durationSeconds\":");
            _AppendJsonDouble(builder, _recordedDuration);
            builder.Append(",\"overflowed\":");
            _AppendJsonBool(builder, _overflowed);
            builder.Append(",\"times\":[");

            for (int i = 0; i < _sampleCount; i++)
            {
                if (i > 0) builder.Append(',');
                _AppendJsonDouble(builder, _times[i]);
            }

            builder.Append("],\"parameters\":[");

            for (int i = 0; i < _paramCount; i++)
            {
                if (i > 0) builder.Append(',');

                _AppendParameter(builder, i);
            }

            builder.Append("]}");

            return builder.ToString();
        }

        private void _AppendParameter(StringBuilder builder, int parameterIndex)
        {
            int type = _paramTypes[parameterIndex];
            int id = _paramIds[parameterIndex];
            int typeIndex = _paramTypeIndex[parameterIndex];

            builder.Append("{\"key\":");
            _AppendJsonString(builder, _TypeName(type) + ":" + id);
            builder.Append(",\"type\":");
            _AppendJsonString(builder, _TypeName(type));
            builder.Append(",\"id\":");
            _AppendJsonInt(builder, id);
            builder.Append(",\"name\":");
            _AppendJsonString(builder, _paramNames[parameterIndex]);

            if (type == (int)AvionicsBusDataType.Vector3)
            {
                builder.Append(",\"components\":3");
            }

            builder.Append(",\"values\":[");

            for (int sampleIndex = 0; sampleIndex < _sampleCount; sampleIndex++)
            {
                if (sampleIndex > 0) builder.Append(',');

                int index = typeIndex * _capacity + sampleIndex;

                if (type == (int)AvionicsBusDataType.Float)
                {
                    _AppendJsonFloat(builder, _floatSamples[index]);
                }
                else if (type == (int)AvionicsBusDataType.Byte)
                {
                    _AppendJsonInt(builder, _byteSamples[index]);
                }
                else if (type == (int)AvionicsBusDataType.Int)
                {
                    _AppendJsonInt(builder, _intSamples[index]);
                }
                else if (type == (int)AvionicsBusDataType.Bool)
                {
                    _AppendJsonBool(builder, _boolSamples[index]);
                }
                else if (type == (int)AvionicsBusDataType.String)
                {
                    _AppendJsonString(builder, _stringSamples[index]);
                }
                else
                {
                    _AppendJsonVector3(builder, _vector3Samples[index]);
                }
            }

            builder.Append("]}");
        }

        private int _EstimateBuilderCapacity()
        {
            int perSample = _paramCount * 12 + 24;

            if (_sampleCount <= 0 || perSample <= 0) return 1024;
            if (_sampleCount > 10000000 / perSample) return 65536;

            int estimate = _sampleCount * perSample;

            if (estimate < 0 || estimate > 67108864) return 65536;

            return estimate;
        }

        private void _AppendJsonFloat(StringBuilder builder, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                builder.Append("null");
                return;
            }

            builder.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        private void _AppendJsonDouble(StringBuilder builder, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                builder.Append("null");
                return;
            }

            builder.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        private void _AppendJsonInt(StringBuilder builder, int value)
        {
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private void _AppendJsonBool(StringBuilder builder, bool value)
        {
            builder.Append(value ? "true" : "false");
        }

        private void _AppendJsonVector3(StringBuilder builder, Vector3 value)
        {
            builder.Append('[');
            _AppendJsonFloat(builder, value.x);
            builder.Append(',');
            _AppendJsonFloat(builder, value.y);
            builder.Append(',');
            _AppendJsonFloat(builder, value.z);
            builder.Append(']');
        }

        private void _AppendJsonString(StringBuilder builder, string value)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('"');

            char[] chars = value.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                int code = chars[i];

                if (code == 34)
                {
                    builder.Append("\\\"");
                }
                else if (code == 92)
                {
                    builder.Append("\\\\");
                }
                else if (code == 10)
                {
                    builder.Append("\\n");
                }
                else if (code == 13)
                {
                    builder.Append("\\r");
                }
                else if (code == 9)
                {
                    builder.Append("\\t");
                }
                else if (code < 32)
                {
                    builder.Append("\\u");
                    builder.Append(code.ToString("x4", CultureInfo.InvariantCulture));
                }
                else
                {
                    builder.Append((char)code);
                }
            }

            builder.Append('"');
        }

        private string _TypeName(int type)
        {
            if (type == (int)AvionicsBusDataType.Float) return "float";
            if (type == (int)AvionicsBusDataType.Byte) return "byte";
            if (type == (int)AvionicsBusDataType.Int) return "int";
            if (type == (int)AvionicsBusDataType.Bool) return "bool";
            if (type == (int)AvionicsBusDataType.String) return "string";

            return "vector3";
        }

        private string _SamplingMethodName(AvionicsBusSamplingMethod method)
        {
            if (method == AvionicsBusSamplingMethod.FixedUpdate) return "FixedUpdate";
            if (method == AvionicsBusSamplingMethod.LateUpdate) return "LateUpdate";

            return "Update";
        }
    }
}
