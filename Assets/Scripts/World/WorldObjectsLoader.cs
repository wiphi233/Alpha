using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using static FastNoiseLite;
using static UnityEditor.PlayerSettings;


public class WorldObjectsLoader : MonoBehaviour
{
    public static WorldObjectsLoader Instance;

    WorldObjectsLoader()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("警告：重复的 WorldObjectsLoader 实例！");
            return;
        }
    }

    // ATTENTION
    // 此函数只在多线程中执行！
    // originHeight 属于 [0, 1]
    // x, z 为世界坐标
    public void WorldObjectGeneration(ref FastNoiseLite grassNoise, ref System.Random rng, 
        float originHeight, int x, int z, ref List<GrassParticleData> grassList, 
        ref List<WorldObject> worldObjects)
    {
        

        // TerrainLoader.Instance.GetNoise();
        if (originHeight > WorldConfig.waterLevelHeight)
        {
            // 水上 world object
            if (rng.NextDouble() < Mathf.Pow((grassNoise.GetNoise(x, z) + 1f) * 0.5f, 0.5f) && rng.NextDouble() < 0.1)
            {
                GrassParticleData grassParticleData = new GrassParticleData
                {
                    position = new Vector4(x, originHeight * WorldConfig.heightMultiplier, z, 0)
                };
                grassList.Add(grassParticleData);
            }
            float[] biomeWeights = TerrainLoader.Instance.GetBiomeWeights(x, z);
            double rngVal = rng.NextDouble();
            for (int idx = 1; idx < biomeWeights.Count(); idx++)
            {
                biomeWeights[idx] += biomeWeights[idx - 1];
                // float density = ;
                if (biomeWeights[idx] <= rngVal)
                {
                    // if (rng.Next() > density)
                    // {

                    // }

                    
                    // 生成 idx 这个群系的 world object
                    if (objectsNoise.GetNoise(i, j) * noiseWeight + n > rng.NextDouble())
                    {
                        // Random World Object for this biome
                        // rng.Next(l = 0, r) 返回 [l, r) 的左闭右开区间
                        if (TerrainLoader.boimeWorldObjectsSet[idx].Count > 0)
                        {
                            // 随机 world object
                            WorldObject wobj = TerrainLoader.boimeWorldObjectsSet[idx][rng.Next(TerrainLoader.boimeWorldObjectsSet[idx].Count)];
                            // 随机位置
                            Vector3 position = new Vector3(i + rng.NextDouble(), originHeight * WorldConfig.heightMultiplier, j + rng.NextDouble());
                            // 随机旋转
                            Quaternion rotation = Quaternion.Euler(0, 0, rng.Next(-180, 180)); // 绕顺序：Z, X, Y 轴旋转
                            // 检查生成条件
                            if (true)
                            {
                                GameObject gameObject = Instantiate(wobj.gameObject, position, rotation); //, objectsGroup, true);
                                // 最后一个参数 instantiateInWorldSpace 表示在世界空间中定位位置，而不是基于父亲。
                                // 等等，好像没有 objectsGroup，啥时候删了awa
                                worldObjects.Add(gameObject);
                                Debug.Log($"New GameObject {wobj.ObjID} on position: ({position.x}, {position.y}, {position.z}) rotation: ({rotation.x}, {rotation.y}, {rotation.z})");
                            }
                        }
                    }
                    break;
                }
            }
        }
        else
        {
            // 水下 world object
            // 未来规划：考虑水草、珊瑚和石头，以及一些遗迹
        }
    }
}
