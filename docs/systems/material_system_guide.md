---
title: Material System Complete Guide
document_type: tutorial
priority: high
target_audience: intermediate
topics:
  - material
  - shader
  - rendering
  - stencil
  - visual-effects
version: 1.0
last_updated: 2025-01-27
related_docs:
  - Rendering_API_Reference.md
  - PhysicsGame_Best_Practices.md
---

# 材质系统完整指南

材质系统是框架中控制物体外观和渲染效果的核心系统。掌握材质系统对于制作优秀的视觉效果至关重要。

> **注意**：本文档是完整教程。如需快速查询 API，请参阅 [渲染 API 参考](./Rendering_API_Reference.md)。

---

## 目录

1. [基础材质操作](#基础材质操作)
2. [Shader 系统](#shader-系统)
3. [渲染管道控制](#渲染管道控制)
4. [Stencil 缓冲区技术](#stencil-缓冲区技术)
5. [材质渲染模式](#材质渲染模式)
6. [动态材质效果](#动态材质效果)
7. [性能优化](#性能优化)
8. [调试技巧](#调试技巧)

---

## 基础材质操作

### 获取物体材质

#### 从 PhysicsActor 获取

```csharp
// 获取 PhysicsActor 的内置材质
PhysicsActor actor = GetSomePhysicsActor();
EngineInterface.Urho3DInterface.Material[]? materials = actor.GetModelMaterials();

if (materials != null && materials.Length > 0)
{
    EngineInterface.Urho3DInterface.Material mainMaterial = materials[0];
    // 对材质进行操作
}
```

#### 从地形获取

```csharp
// 获取地形材质
List<EngineInterface.Urho3DInterface.Material> terrainMaterials = Terrain.GetMaterials();
```

> **重要**：始终使用完整命名空间 `EngineInterface.Urho3DInterface.Material` 避免类名冲突！

### 材质属性设置

#### 颜色属性

```csharp
EngineInterface.Urho3DInterface.Material material = GetMaterial();

// 设置基础颜色（漫反射颜色）
material.SetColor("TintColor", System.Drawing.Color.FromArgb(255, 255, 0, 0)); // 红色

// 设置自发光颜色
material.SetColor("Color_Emissive", System.Drawing.Color.FromArgb(255, 0, 255, 0)); // 绿色自发光
```

#### 数值属性

```csharp
// 金属度（0.0 = 非金属，1.0 = 完全金属）
material.SetFloat("MetallicFactor", 0.8f);

// 粗糙度（0.0 = 完全光滑，1.0 = 完全粗糙）
material.SetFloat("RoughnessFactor", 0.2f);

// 自发光强度倍数
material.SetFloat("Emissive_Mul", 2.0f);
```

#### 向量属性

```csharp
// 设置自定义向量参数
material.SetVector("CustomParam", new Vector4(1.0f, 0.5f, 0.0f, 1.0f));
```

#### 获取材质属性

```csharp
// 获取颜色
Color currentColor = material.GetColor("TintColor");

// 获取数值
float metallic = material.GetFloat("MetallicFactor");

// 获取向量
Vector4 customParam = material.GetVector("CustomParam");
```

### 常用材质属性表

| 属性名 | 类型 | 说明 | 范围 |
|--------|------|------|------|
| TintColor | Color | 基础颜色 | ARGB(0-255) |
| Color_Emissive | Color | 自发光颜色 | ARGB(0-255) |
| MetallicFactor | Float | 金属度 | 0.0 - 1.0 |
| RoughnessFactor | Float | 粗糙度 | 0.0 - 1.0 |
| Emissive_Mul | Float | 自发光强度 | 0.0+ |

---

## Shader 系统

### 设置材质 Shader

```csharp
EngineInterface.Urho3DInterface.Material material = GetMaterial();

// 获取已知的 Shader
Shader pbrShader = Shader.Find("PBR_PackedNormal/DefaultMetallicRoughness");

// 应用 Shader
if (pbrShader != null)
{
    material.shader = pbrShader;
}
```

### Shader 使用注意事项

#### ✅ 正确做法

```csharp
// 正确的 Shader 获取方式 - 检查 null
Shader shader = Shader.Find("PBR_PackedNormal/DefaultMetallicRoughness");
if (shader != null)
{
    material.shader = shader;
}
else
{
    Console.WriteLine("Shader 未找到，请检查名称是否正确");
}
```

#### ❌ 错误做法

```csharp
// 错误：不检查 null，可能导致运行时错误
Shader? shader = Shader.Find("UnknownShader");
material.shader = shader;  // shader 可能为 null！
```

### 常用 Shader 列表

| Shader 名称 | 说明 | 适用场景 |
|------------|------|---------|
| PBR_PackedNormal/DefaultMetallicRoughness | 标准 PBR 材质 | 大多数物体 |
| ... | 其他 Shader | 特殊效果 |

---

## 渲染管道控制

### Shader Pass 管理

#### 什么是 Shader Pass？

Shader Pass 是渲染管道的不同阶段。通过启用/禁用不同的 Pass，可以控制物体在哪些阶段渲染。

#### 启用/禁用 Pass

```csharp
EngineInterface.Urho3DInterface.Material material = GetMaterial();

// 启用/禁用不同的渲染 Pass
material.SetShaderPassEnabled("base", true);      // 基础渲染
material.SetShaderPassEnabled("alpha", false);    // 半透明渲染
material.SetShaderPassEnabled("shadow", true);    // 阴影渲染
material.SetShaderPassEnabled("depth", true);     // 深度渲染
```

#### 控制 Pass 的写入权限

```csharp
// 允许颜色写入
material.SetShaderPassColorWrite("base", true);

// 允许深度写入
material.SetShaderPassDepthWrite("base", true);

// 禁用颜色写入（用于特殊效果，如 Stencil 写入器）
material.SetShaderPassColorWrite("base", false);
```

#### 内置 Pass 列表

| Pass 名称 | 说明 |
|----------|------|
| base | 基础 pass（不透明物体） |
| alpha | 半透 pass（透明物体） |
| litbase | 光照 pass（包含平行光、ClusterLight 等计算） |
| litalpha | 半透光照 pass |
| shadow | 实时投影 pass |
| planershadow | 平面阴影 pass |
| xray | XRay 效果 |
| outstroke | 外描边 pass |
| innerstroke | 内描边 pass |
| depth | 深度 pass |

### 渲染顺序控制

#### 设置渲染优先级

```csharp
EngineInterface.Urho3DInterface.Material material = GetMaterial();

// 设置渲染优先级（0-256，数字越小越早渲染）
material.SetRenderOrder(0);    // 最早渲染（背景）
material.SetRenderOrder(128);  // 中等优先级（普通物体）
material.SetRenderOrder(200);  // 较晚渲染（半透明物体）
material.SetRenderOrder(255);  // 最晚渲染（UI、特效）

// 获取当前渲染优先级
uint currentOrder = material.GetRenderOrder();
```

#### 渲染顺序最佳实践

1. **不透明物体**：使用默认顺序（128 左右）
2. **Stencil 写入器**：较早渲染（0-50）
3. **Stencil 读取器**：晚于写入器（51-200）
4. **半透明物体**：较晚渲染（200-230）
5. **UI 和特效**：最晚渲染（231-255）

---

## Stencil 缓冲区技术

Stencil 缓冲区是实现高级视觉效果的强大工具，如镂空、遮罩、轮廓等。

### 基础 Stencil 操作

#### 创建 Stencil 状态

```csharp
using EngineInterface.Urho3DInterface.Graphics;

EngineInterface.Urho3DInterface.Material material = GetMaterial();

// 创建 Stencil 状态
StencilState stencilState = new StencilState
{
    StencilTest = true,                    // 启用 Stencil 测试
    StencilCompare = CompareMode.Always,   // 比较模式
    PassOp = StencilOp.Ref,               // 通过测试时的操作
    StencilRef = 1,                       // 参考值
    StencilWriteMask = 0xFFFFFFFF,        // 写入掩码
    StencilReadMask = 0xFFFFFFFF          // 读取掩码
};

material.SetStencilState(stencilState);

// 获取当前 Stencil 状态
StencilState currentState = material.GetStencilState();
```

### 实现镂空效果

镂空效果（如黑洞地形镂空）需要两个步骤：

#### 步骤1：创建 Stencil 写入器材质

```csharp
EngineInterface.Urho3DInterface.Material stencilWriter = CreateStencilWriterMaterial();

// 配置 Stencil 写入
StencilState writerState = new StencilState
{
    StencilTest = true,
    StencilCompare = CompareMode.Always,   // 总是通过测试
    PassOp = StencilOp.Ref,               // 写入参考值
    StencilRef = 1,                       // 写入值为 1
    StencilWriteMask = 0xFFFFFFFF
};
stencilWriter.SetStencilState(writerState);

// 禁用颜色和深度写入，只写 Stencil
stencilWriter.SetShaderPassColorWrite("base", false);
stencilWriter.SetShaderPassDepthWrite("base", false);

// 确保优先渲染
stencilWriter.SetRenderOrder(0);
```

#### 步骤2：设置需要被镂空的材质

```csharp
EngineInterface.Urho3DInterface.Material targetMaterial = GetTargetMaterial();

// 配置 Stencil 读取
StencilState readerState = new StencilState
{
    StencilTest = true,
    StencilCompare = CompareMode.NotEqual, // 不等于参考值才渲染
    StencilRef = 1,                        // 参考值为 1
    StencilReadMask = 0xFFFFFFFF
};
targetMaterial.SetStencilState(readerState);

// 确保在写入器之后渲染
targetMaterial.SetRenderOrder(50);
```

#### 工作原理

1. **写入阶段**：写入器在指定区域将 Stencil 缓冲区设置为 1
2. **读取阶段**：读取器只渲染 Stencil 值不等于 1 的区域
3. **结果**：写入器覆盖的区域不会渲染读取器的内容，形成镂空效果

### 实现物体轮廓

```csharp
// 创建轮廓效果
EngineInterface.Urho3DInterface.Material outlineMaterial = CreateOutlineMaterial();

// 启用轮廓 Pass
outlineMaterial.SetShaderPassEnabled("outstroke", true);  // 外描边
outlineMaterial.SetShaderPassEnabled("innerstroke", false); // 内描边

// 设置轮廓颜色和宽度
outlineMaterial.SetColor("OutlineColor", System.Drawing.Color.FromArgb(255, 255, 255, 0)); // 黄色轮廓
outlineMaterial.SetFloat("OutlineWidth", 0.02f);
```

### Stencil 调试技巧

```csharp
public static void ValidateStencilSetup(
    EngineInterface.Urho3DInterface.Material writer, 
    EngineInterface.Urho3DInterface.Material reader)
{
    var writerState = writer.GetStencilState();
    var readerState = reader.GetStencilState();

    Console.WriteLine("Stencil 验证:");
    Console.WriteLine($"  Writer - Test: {writerState.StencilTest}, Ref: {writerState.StencilRef}");
    Console.WriteLine($"  Reader - Test: {readerState.StencilTest}, Ref: {readerState.StencilRef}");
    Console.WriteLine($"  Writer RenderOrder: {writer.GetRenderOrder()}");
    Console.WriteLine($"  Reader RenderOrder: {reader.GetRenderOrder()}");

    if (writer.GetRenderOrder() >= reader.GetRenderOrder())
    {
        Console.WriteLine("  ⚠️ 警告: Writer 应该比 Reader 更早渲染");
    }
}
```

---

## 材质渲染模式

### 裁剪和填充模式

#### 设置面裁剪模式

```csharp
using EngineInterface.Urho3DInterface.Graphics;

EngineInterface.Urho3DInterface.Material material = GetMaterial();

// 设置面裁剪模式
material.SetCullMode(CullMode.None);  // 双面渲染
material.SetCullMode(CullMode.CCW);   // 裁剪逆时针面
material.SetCullMode(CullMode.CW);    // 裁剪顺时针面

// 获取当前模式
CullMode currentCull = material.GetCullMode();
```

#### 设置填充模式

```csharp
// 设置填充模式
material.SetFillMode(FillMode.Solid);     // 实心填充
material.SetFillMode(FillMode.Wireframe); // 线框模式
material.SetFillMode(FillMode.Point);     // 点渲染

// 获取当前模式
FillMode currentFill = material.GetFillMode();
```

#### 应用场景

| 模式 | 说明 | 适用场景 |
|------|------|---------|
| CullMode.None | 双面渲染 | 树叶、窗帘、薄片 |
| CullMode.CCW | 裁剪逆时针面（默认） | 大多数封闭物体 |
| FillMode.Solid | 实心填充（默认） | 正常渲染 |
| FillMode.Wireframe | 线框模式 | 调试、特殊效果 |

---

## 动态材质效果

### 材质动画

#### 颜色渐变动画

```csharp
public class MaterialAnimationComponent : ScriptComponent
{
    private EngineInterface.Urho3DInterface.Material targetMaterial;
    private float animationTime = 0f;

    public MaterialAnimationComponent(EngineInterface.Urho3DInterface.Material material)
    {
        targetMaterial = material;
    }

    public override void OnUpdate(float timeStep)
    {
        animationTime += timeStep;

        // 颜色渐变动画（红色 ↔ 紫色）
        float colorIntensity = (MathF.Sin(animationTime * 2.0f) + 1.0f) * 0.5f;
        Color animatedColor = System.Drawing.Color.FromArgb(
            255,
            (int)(255 * colorIntensity),
            0,
            (int)(255 * (1.0f - colorIntensity))
        );
        targetMaterial.SetColor("TintColor", animatedColor);

        // 自发光强度动画
        float emissiveIntensity = MathF.Sin(animationTime * 3.0f) * 0.5f + 1.0f;
        targetMaterial.SetFloat("Emissive_Mul", emissiveIntensity);
    }
}
```

### 材质状态切换

#### 创建材质状态管理器

```csharp
public class MaterialStateController
{
    private EngineInterface.Urho3DInterface.Material material;
    private Dictionary<string, MaterialState> states;
    private string currentState;

    public class MaterialState
    {
        public Color TintColor;
        public float Metallic;
        public float Roughness;
        public Shader? TargetShader;
    }

    public MaterialStateController(EngineInterface.Urho3DInterface.Material mat)
    {
        material = mat;
        states = new Dictionary<string, MaterialState>();
    }

    public void DefineState(string name, Color color, float metallic, float roughness, Shader? shader)
    {
        states[name] = new MaterialState
        {
            TintColor = color,
            Metallic = metallic,
            Roughness = roughness,
            TargetShader = shader
        };
    }

    public void SetState(string stateName)
    {
        if (states.ContainsKey(stateName))
        {
            var state = states[stateName];
            material.SetColor("TintColor", state.TintColor);
            material.SetFloat("MetallicFactor", state.Metallic);
            material.SetFloat("RoughnessFactor", state.Roughness);
            if (state.TargetShader != null)
            {
                material.shader = state.TargetShader;
            }
            currentState = stateName;
        }
    }
}
```

#### 使用示例

```csharp
var controller = new MaterialStateController(material);

// 定义状态
controller.DefineState("normal", Color.White, 0.0f, 0.5f, normalShader);
controller.DefineState("damaged", Color.Red, 0.2f, 0.8f, normalShader);
controller.DefineState("powered", Color.Cyan, 0.8f, 0.2f, glowShader);

// 切换状态
controller.SetState("normal");
// ... 玩家受伤
controller.SetState("damaged");
// ... 玩家激活能力
controller.SetState("powered");
```

---

## 性能优化

### 缓存材质引用

#### ❌ 低效做法

```csharp
public override void OnUpdate(float timeStep)
{
    // 每帧都获取材质数组，性能差
    var materials = actor.GetModelMaterials();
    if (materials != null && materials.Length > 0)
    {
        materials[0].SetColor("TintColor", Color.Red);
    }
}
```

#### ✅ 高效做法

```csharp
private EngineInterface.Urho3DInterface.Material? cachedMaterial;

public override void OnStart()
{
    // 只在启动时获取一次
    var materials = actor.GetModelMaterials();
    if (materials != null && materials.Length > 0)
    {
        cachedMaterial = materials[0];
    }
}

public override void OnUpdate(float timeStep)
{
    // 使用缓存的引用
    if (cachedMaterial != null)
    {
        cachedMaterial.SetColor("TintColor", Color.Red);
    }
}
```

### 颜色对象复用

#### ❌ 低效做法

```csharp
public override void OnUpdate(float timeStep)
{
    // 每帧都创建新颜色对象，产生大量 GC
    material.SetColor("TintColor", System.Drawing.Color.FromArgb(255, 255, 0, 0));
}
```

#### ✅ 高效做法

```csharp
// 缓存常用颜色
private static readonly Color Red = System.Drawing.Color.FromArgb(255, 255, 0, 0);
private static readonly Color Green = System.Drawing.Color.FromArgb(255, 0, 255, 0);
private static readonly Color Blue = System.Drawing.Color.FromArgb(255, 0, 0, 255);

private bool shouldChangeColor = true;

public override void OnUpdate(float timeStep)
{
    // 只在需要时更新，使用缓存的颜色
    if (shouldChangeColor)
    {
        material.SetColor("TintColor", Red);
        shouldChangeColor = false;
    }
}
```

### Shader 预加载

```csharp
public static class MaterialManager
{
    // 缓存常用 Shader
    private static readonly Dictionary<string, Shader?> CachedShaders = new();

    static MaterialManager()
    {
        // 预加载已知的 Shader
        CachedShaders["PBR"] = Shader.Find("PBR_PackedNormal/DefaultMetallicRoughness");
    }

    public static Shader? GetShader(string name)
    {
        if (CachedShaders.TryGetValue(name, out Shader? shader))
        {
            return shader;
        }
        return null;
    }
}
```

### 批量设置属性

```csharp
// 批量设置材质属性（减少调用次数）
public static void SetMaterialProperties(
    EngineInterface.Urho3DInterface.Material material, 
    Color color, 
    float metallic, 
    float roughness)
{
    material.SetColor("TintColor", color);
    material.SetFloat("MetallicFactor", metallic);
    material.SetFloat("RoughnessFactor", roughness);
}
```

---

## 调试技巧

### 材质信息输出

```csharp
public static class MaterialDebugger
{
    public static void LogMaterialInfo(EngineInterface.Urho3DInterface.Material? material, string name = "Material")
    {
        if (material == null)
        {
            Console.WriteLine($"{name}: null");
            return;
        }

        Console.WriteLine($"{name} 信息:");
        Console.WriteLine($"  Shader: {material.shader?.name ?? "null"}");
        Console.WriteLine($"  RenderOrder: {material.GetRenderOrder()}");
        Console.WriteLine($"  CullMode: {material.GetCullMode()}");
        Console.WriteLine($"  FillMode: {material.GetFillMode()}");

        // 尝试获取常见属性
        try
        {
            Color tintColor = material.GetColor("TintColor");
            Console.WriteLine($"  TintColor: {tintColor}");
        }
        catch { Console.WriteLine("  TintColor: 不支持"); }

        try
        {
            float metallic = material.GetFloat("MetallicFactor");
            Console.WriteLine($"  Metallic: {metallic}");
        }
        catch { Console.WriteLine("  Metallic: 不支持"); }
    }
}
```

### 使用调试工具

```csharp
public class MyMaterialComponent : ScriptComponent
{
    public override void OnStart()
    {
        var materials = actor.GetModelMaterials();
        if (materials != null)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                MaterialDebugger.LogMaterialInfo(materials[i], $"Material[{i}]");
            }
        }
    }
}
```

---

## 常见错误和解决方案

### 错误1：材质数组为空

#### ❌ 错误做法

```csharp
var materials = actor.GetModelMaterials();
var material = materials[0];  // 可能抛出 NullReferenceException
```

#### ✅ 正确做法

```csharp
var materials = actor.GetModelMaterials();
if (materials != null && materials.Length > 0)
{
    var material = materials[0];
    // 安全使用材质
}
```

### 错误2：Shader 未检查 null

#### ❌ 错误做法

```csharp
Shader? shader = Shader.Find("UnknownShader");
material.shader = shader;  // shader 可能为 null
```

#### ✅ 正确做法

```csharp
Shader? shader = Shader.Find("PBR_PackedNormal/DefaultMetallicRoughness");
if (shader != null)
{
    material.shader = shader;
}
else
{
    Console.WriteLine("Shader 未找到");
}
```

### 错误3：每帧修改材质

#### ❌ 错误做法

```csharp
public override void OnUpdate(float timeStep)
{
    // 每帧都修改，性能极差
    material.SetColor("TintColor", System.Drawing.Color.FromArgb(255, 255, 0, 0));
}
```

#### ✅ 正确做法

```csharp
private static readonly Color RedColor = System.Drawing.Color.FromArgb(255, 255, 0, 0);
private bool needsUpdate = true;

public override void OnUpdate(float timeStep)
{
    // 只在需要时更新
    if (needsUpdate)
    {
        material.SetColor("TintColor", RedColor);
        needsUpdate = false;
    }
}
```

---

## 总结

材质系统是实现视觉效果的核心工具：

1. **基础操作**：掌握材质获取、属性设置
2. **Shader 系统**：理解 Shader 的作用和使用方法
3. **渲染管道**：控制 Pass、渲染顺序
4. **Stencil 技术**：实现高级视觉效果（镂空、轮廓）
5. **性能优化**：缓存引用、复用对象、预加载资源
6. **调试工具**：使用日志和调试工具排查问题

---

**相关文档**：
- 📖 [渲染 API 参考](./Rendering_API_Reference.md)
- ✅ [最佳实践](./PhysicsGame_Best_Practices.md)
- 🎮 [黑洞游戏案例](./BlackHole_Game_Case_Study.md)

