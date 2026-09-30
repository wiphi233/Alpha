using UnityEngine;
using UnityEngine.VFX;
using System.Runtime.InteropServices;

/// <summary>
/// 程序化草地数据绑定器
/// 地形生成、草点计算完成后，调用一次 Initialize 即可
/// </summary>
[RequireComponent(typeof(VisualEffect))]
public class GrassVFXDataBinder : MonoBehaviour
{
    //[Header("目标草地VFX组件")]
    private VisualEffect grassVFX;

    // GPU缓冲区
    private GraphicsBuffer _grassDataBuffer;

    // VFX参数名（与Blackboard中名称完全一致）
    private const string PARAM_GRASS_BUFFER = "_GrassDataBuffer";
    private const string PARAM_GRASS_COUNT = "_GrassCount";

    /// <summary>
    /// 初始化草地数据
    /// </summary>
    /// <param name="grassDataArray">CPU多线程计算完成的草数据数组</param>
    public void InitializeGrassData(GrassParticleData[] grassDataArray)
    {
        grassVFX = GetComponent<VisualEffect>();
        if (grassVFX == null)
        {
            Debug.LogError("未分配 Grass VFX 组件！");
            return;
        }

        int grassCount = grassDataArray.Length;
        if (grassCount == 0)
        {
            // 草数据数组为空！
            return;
        }

        // 1. 释放旧缓冲区
        ReleaseBuffer();

        // 2. 创建结构化缓冲区
        // 计算单条数据的字节步长，自动匹配结构体大小
        int stride = Marshal.SizeOf<GrassParticleData>();
        _grassDataBuffer = new GraphicsBuffer(
            GraphicsBuffer.Target.Structured,
            grassCount,
            stride
        );

        // 3. 将CPU数组数据上传到GPU缓冲区
        _grassDataBuffer.SetData(grassDataArray);

        // 4. 将缓冲区和数量传入VFX Graph
        grassVFX.SetGraphicsBuffer(PARAM_GRASS_BUFFER, _grassDataBuffer);
        grassVFX.SetInt(PARAM_GRASS_COUNT, grassCount);

        // 5. 设置粒子系统容量（可选，也可以在VFX里固定）
        // 注意：VFX中Initialize上下文的Capacity必须 >= grassCount

        // 6. 触发一次生成
        grassVFX.Reinit();
        grassVFX.Play();

        transform.position = Vector3.zero;
        // 注意这里的 500，如需修改，Visual Effect Graph 中也要跟着修改
        //transform.position = new Vector3(transform.position.x, 500, transform.position.z);
    }

    /// <summary>
    /// 释放GPU缓冲区，避免内存泄漏
    /// </summary>
    private void ReleaseBuffer()
    {
        if (_grassDataBuffer != null)
        {
            _grassDataBuffer.Release();
            _grassDataBuffer = null;
        }
    }

    private void OnDestroy()
    {
        ReleaseBuffer();
    }

    private void OnDisable()
    {
        ReleaseBuffer();
    }
}