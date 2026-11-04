# AvionicsBus 数据查看器

用于查看 `AvionicsBusRecorder` 导出的 JSON。纯静态页面，数据只在浏览器本地处理，不会上传。

## 打开方式

直接双击 `index.html`，或用任意静态文件服务器托管本目录。

页面通过 CDN 加载图表库，需要联网：

- Chart.js 4.4.7

如果 CDN 不可用，页面会显示提示；JSON 解析和参数列表仍可工作，但没有图表。

## 载入数据

- 把 `.json`、`.txt` 或 `.log` 文件拖到左侧虚线区域。
- 点击虚线区域选择文件。
- 展开“粘贴 JSON / 日志”，粘贴内容后点击“载入粘贴内容”。会自动剥掉 JSON 前面的日志前缀。
- 点击“载入示例”使用内置示例数据。
- 打开 `index.html?sample=1` 可直接载入示例。

## 多视图对比

- 载入数据后会自动创建一个包含全部数值参数的视图。
- 点工具栏“新增视图”创建更多视图；每个视图有自己的参数集合和 Y 轴模式。
- 左侧每个参数行下方的数字按钮就是视图编号：点一下把该参数加入/移出对应视图。
- 所有视图共享同一个时间范围：在任意一个图上拖拽/缩放，其他图会同步跟随。
- 每个视图可以独立选择“归一化”或“共享轴”，方便把不同量纲的参数放在一起比较。
- “清空”只清空当前视图的参数；“移除”删除当前视图（至少保留一个视图）。

## 参数搜索与名称

- 左侧“数值参数”顶部有搜索框，可按参数名、类型、id 过滤。
- 参数名会完整换行显示，不再省略；鼠标悬停仍会显示完整名称。
- 搜索同时过滤“文本参数”区域。
- 数字按钮上的颜色对应视图颜色，视图标题左侧的圆点也是同一颜色。

## 图表操作

- **Y 轴模式（每个视图独立）**
  - `归一化`：每条曲线按自身 min/max 映射到 0..1，适合对比形状。
  - `共享轴`：当前视图的所有曲线共用一个数值轴，适合对比同一单位的数据。
- **平移**：在图上按住左键拖拽。
- **缩放**：滚轮缩放 X 轴，以鼠标位置为中心。
- **重置**：双击图表，或点工具栏“重置缩放”。
- **悬停高亮**：鼠标移到左侧参数行上，会加粗所有视图中对应的曲线。
- **鼠标十字线**：鼠标在任意一个图上移动时，所有视图都会在同一条时间位置显示虚线。
- **数值读数**：每个视图右上角会显示该时间点下本视图所有曲线的数值；归一化模式下也显示原始值。曲线很多时读数面板可滚动。
- 读数取的是距离鼠标时间最近的采样点；内置 Chart.js tooltip 已关闭，统一使用这个读数面板。

## 参数类型

- `float` / `int` / `byte`：普通折线。
- `bool`：映射为 0/1 的阶梯线，tooltip 显示 `true` / `false`。
- `vector3`：拆成 `.x`、`.y`、`.z` 三条曲线，可以分别加入视图。
- `string`：不画图，在“文本参数”中显示最后值和变化时间线。

## JSON 格式

导出器生成 `schemaVersion: 1` 的对象：

```json
{
  "schemaVersion": 1,
  "source": "AvionicsBusRecorder",
  "recordedAtUtc": "2026-10-29T00:00:00.0000000Z",
  "samplingMethod": "Update",
  "sampleRateHz": 60,
  "sampleCount": 601,
  "durationSeconds": 10,
  "overflowed": false,
  "times": [0.0, 0.0166],
  "parameters": [
    {
      "key": "float:12",
      "type": "float",
      "id": 12,
      "name": "V32NN_Frequent_ADIRS_ADR_1_AltitudeFeet",
      "values": [10000, 10010]
    }
  ]
}
```

详细字段说明见 [docs/avionics-bus-recorder.md](../../docs/avionics-bus-recorder.md)。

## 大文件与性能

- 数据量超过 20000 个样本时自动启用 Chart.js 内置 LTTB 降采样（约 4000 点），保证滚动和缩放流畅。
- 降采样后 tooltip 显示的是被保留的采样点，不一定是鼠标位置最近的原始点。
- 参数很多时，只把需要的参数加入对应视图，避免每个图都画太多曲线。

## 示例数据

`samples/sample-export.js` 是内置示例，覆盖 float、bool、vector3、string 四种参数。

重新生成：

```bash
python samples/generate-sample.py
```

脚本会覆盖 `samples/sample-export.js`。

## 文件结构

```text
index.html
viewer.css
viewer.js
README.md
samples/
  generate-sample.py
  sample-export.js
```

## 限制

- 需要联网加载 Chart.js CDN。
- 只支持 `schemaVersion: 1` 的结构；其他版本会给出提示并尽量解析。
- 非数值参数（string）只在文本面板显示，不参与图表。
- 缩放/平移是查看器自己实现的，不依赖 chartjs-plugin-zoom。
- 所有数据只在当前浏览器标签页中处理，刷新页面后丢失。
