# AvionicsBus 参数录制与 JSON 导出

`AvionicsBusRecorder` 是 `AvionicsBus` 的一个客户端，用来按固定方式采样选定参数，并在停止时生成 JSON。它同时适用于 VRChat 和 Unity Play mode。

- 运行时脚本：`Aircraft/Scripts/Bus/AvionicsBusRecorder.cs`
- 默认挂载位置：`Aircraft/Prefab/v320neo-next.prefab` 根节点下的 `AvionicsBusRecorder` 子物体
- 网页查看器：[tools/avionics-bus-export-viewer/README.md](../tools/avionics-bus-export-viewer/README.md)

## 快速开始（Unity Editor）

1. 打开 `v320neo-next.prefab`，或在场景中选中飞机实例下的 `AvionicsBusRecorder`。
2. 在 Inspector 的“导出参数”区域搜索并添加要记录的数据 id。可以上移、下移、移除，或点“同步参数名称”刷新 enum 改名后的显示名。
3. 设置采样配置：
   - **采样方式**：FixedUpdate / Update / LateUpdate。
   - **采样率 (Hz)**：`0` 表示每次回调都采样；大于 `0` 时按 `1 / Hz` 的间隔限速。
   - **最大样本数**：默认 36000；录制时缓冲区写满会自动停止并生成已记录的数据。
   - **Bus 启动时自动开始**：Bus 启动/重生时自动开始；重生不会中断正在进行的采样。
   - **自动停止 (秒)**：`0` 表示手动停止。
   - **输出到 Debug.Log**：停止时把 JSON 追加到 Unity/VRChat 日志。
4. 设置“导出路径”：
   - 相对路径基于 Unity 项目根目录，例如 `AvionicsBusExports/flight.json`。
   - 绝对路径也可以。
   - 不建议放在 `Assets/` 下，否则 Unity 会把 JSON 当资源导入。
5. 进入 Play mode。
6. 点“开始采样”。飞行或运行系统一段时间。
7. 点“停止并保存”。JSON 会写入配置的路径；如果路径为空，会弹出保存对话框。
8. 如果启用了“自动停止”，停止后点“保存当前导出”或“复制 JSON”。

> Udon 只在 Play mode 执行，所以编辑器内录制必须进入 Play mode。离开 Play mode 前请先保存；`exportedJson` 在退出 Play mode 后会丢失。

## 游戏内使用

录制完成后，JSON 永远会写入组件的 `exportedJson` 字段。其他脚本可以读取它并展示给用户。

```csharp
AvionicsBusRecorder recorder = GetComponentInChildren<AvionicsBusRecorder>(true);

if (recorder._recordingState == 2)
{
    string json = recorder.exportedJson;
}
```

`_recordingState` 的取值：

| 值 | 含义 |
| --- | --- |
| 0 | Idle |
| 1 | Recording |
| 2 | Finished，`exportedJson` 有效 |
| 3 | Error，`_lastError` 包含原因 |

其他运行时状态：

- `_sampleCount`：已采样数。
- `_recordedDuration`：最后一个样本的相对时间（秒）。
- `_overflowed`：是否因缓冲区写满而提前停止。
- `_lastError`：最近一次错误文本。

### 无代码录制

如果暂时没有其他脚本触发，可以：

- 勾选 `autoStartOnBusStart`。
- 设置 `autoStopAfterSeconds`，例如 `30`。
- 勾选 `logExportedJson`，或让其他脚本读取 `exportedJson`。

这样在 VRChat 中进入飞机后会自动录制 30 秒，并在停止时输出。

### Debug.Log

勾选 `logExportedJson` 后，停止时执行：

```csharp
Debug.Log("[AvionicsBusRecorder] " + exportedJson);
```

大 JSON 可能被 VRChat 日志截断，优先使用 `exportedJson` 字段或编辑器文件导出。

## 运行时 API

无参方法可以用 `SendCustomEvent` 调用；带参方法供 UdonSharp 脚本直接调用。

| 方法 | 说明 |
| --- | --- |
| `_StartRecording()` | 开始录制。已经在录制时只记录 warning。 |
| `_StopRecording()` | 停止并生成 JSON，输出到 `exportedJson`/日志。 |
| `_AbortRecording()` | 停止并丢弃本次数据。 |
| `_ClearExport()` | 停止（如有）、清空 JSON 和状态。 |
| `_ApplyExportParametersFromFields()` | 使用当前 `exportDataTypes`/`exportIds`/`exportNames` 重新校验参数。 |
| `_AddExportParameter(int dataType, int id, string name)` | 追加一个参数。 |
| `_RemoveExportParameter(int dataType, int id)` | 移除一个参数。 |
| `_ClearExportParameters()` | 清空参数列表。 |
| `_SetSamplingMethod(int method)` | `0`=FixedUpdate，`1`=Update，`2`=LateUpdate。 |
| `_SetSampleRateHz(float hz)` | `0` 表示每次回调。 |
| `_SetMaxSamples(int maxSamples)` | 最大样本数。 |
| `_SetAutoStopAfterSeconds(float seconds)` | `0` 表示手动停止。 |
| `_SetLogExportedJson(bool value)` | 是否输出到 `Debug.Log`。 |

录制中不能修改参数列表、采样方式、采样率、最大样本数；这些调用会被忽略并记录 warning。`autoStopAfterSeconds` 和 `logExportedJson` 可以在录制中修改。

### 示例

```csharp
AvionicsBusRecorder recorder = GetComponentInChildren<AvionicsBusRecorder>(true);

recorder.exportDataTypes = new[]
{
    (int)AvionicsBusDataType.Float,
    (int)AvionicsBusDataType.Bool,
    (int)AvionicsBusDataType.Vector3,
};

recorder.exportIds = new[]
{
    (int)AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_ADR_1_AltitudeFeet,
    (int)AvionicsBusBoolDataIds.Sim_Frequent_Grounded,
    (int)AvionicsBusVector3DataIds.V32NN_Frequent_FlightControl_LocalPilotInput,
};

recorder.exportNames = new[]
{
    "AltitudeFeet",
    "Grounded",
    "LocalPilotInput",
};

recorder._ApplyExportParametersFromFields();
recorder._StartRecording();
```

停止后：

```csharp
recorder._StopRecording();

if (recorder._recordingState == 2)
{
    string json = recorder.exportedJson;
}
```

## JSON Schema

`schemaVersion` 当前为 `1`。

```json
{
  "schemaVersion": 1,
  "source": "AvionicsBusRecorder",
  "recordedAtUtc": "2026-10-29T00:00:00.0000000Z",
  "samplingMethod": "Update",
  "sampleRateHz": 0,
  "sampleCount": 3,
  "durationSeconds": 0.0333,
  "overflowed": false,
  "times": [0.0, 0.0166, 0.0333],
  "parameters": [
    {
      "key": "float:12",
      "type": "float",
      "id": 12,
      "name": "V32NN_Frequent_ADIRS_ADR_1_AltitudeFeet",
      "values": [10000.0, 10001.0, 10002.0]
    },
    {
      "key": "vector3:0",
      "type": "vector3",
      "id": 0,
      "name": "V32NN_Frequent_FlightControl_LocalPilotInput",
      "components": 3,
      "values": [[0, 0, 0], [0.1, 0, 0], [0.2, 0, 0]]
    },
    {
      "key": "string:0",
      "type": "string",
      "id": 0,
      "name": "V32NN_Infrequent_ECAM_ActiveMessage",
      "values": [null, "ENG 1 FAIL", null]
    }
  ]
}
```

约定：

- `times` 是从录制开始计算的相对秒数，单调递增。
- 每个 `parameters[].values` 的长度等于 `sampleCount`。
- `float` / `int` / `byte` 输出 JSON 数字；`bool` 输出 JSON bool；`string` 可为 `null`；`vector3` 输出三元数组，分量可为 `null`。
- `sampleRateHz = 0` 表示每次回调；查看器会显示“每次回调”。
- `overflowed = true` 表示缓冲区写满，数据被提前截断。
- 参数名来自 `exportNames`；为空时查看器回退到 `type:id`。

## 采样行为

- `FixedUpdate` 使用 `Time.fixedTimeAsDouble`；`Update`/`LateUpdate` 使用 `Time.timeAsDouble`。
- `sampleRateHz = 0` 时每个选中的回调都采样；否则检查 `now - lastSample >= 1 / Hz`。
- 不做补采样；一帧最多采一个样本。
- `autoStopAfterSeconds > 0` 时在超过阈值后的第一个回调停止。
- 样本写满 `maxSamples` 时设置 `overflowed = true` 并自动停止。
- `maxSamples` 会被限制在 1..1,000,000，并按总字段数限制总缓冲大小。

## 限制与注意事项

- 默认 prefab 中的 recorder 组件是启用状态；空闲时三个采样回调只做 early-return。如果不需要，可以在场景实例上禁用或移除它。
- Udon 不在编辑模式执行；没有 Play mode 录制。
- 游戏内不能写文件；文件导出由 Editor Inspector 完成。
- 每个客户端记录自己的本地总线数据，不做网络同步。
- 同一 prefab 多实例同时录制时，请给每个实例设置不同路径。
- `Debug.Log` 超长 JSON 可能被日志截断。
- 运行时 API 修改参数只在下次 `_StartRecording()` 生效；录制中修改会被拒绝。
- 网页查看器需要联网加载 Chart.js CDN；数据本身不会上传。

## 故障排查

| 现象 | 排查 |
| --- | --- |
| 开始采样后没有样本 | 是否在 Play mode；参数列表是否为空；采样方式是否选对；组件是否启用。 |
| `_recordingState = 3` | 查看 `_lastError`；通常是空参数/非法 id。 |
| 停止后没有文件 | 路径是否为空/非法；`exportedJson` 是否为空；Inspector 的保存提示。 |
| 日志里 JSON 不完整 | 日志截断；改用 `exportedJson` 或文件导出。 |
| 查看器图表空白 | Chart.js CDN 是否可访问；浏览器控制台错误；数据是否有数值参数。 |
| 查看器提示 schemaVersion 不匹配 | 导出器与查看器版本不一致；当前 schema 为 1。 |
