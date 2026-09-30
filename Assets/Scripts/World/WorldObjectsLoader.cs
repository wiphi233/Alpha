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

    // 注意：此函数只在多线程中执行！
    public void WorldObjectGeneration(ref FastNoiseLite grassNoise, ref System.Random rng, 
        float originHeight, int x, int z, ref List<GrassParticleData> grassList, 
        ref List<WorldObject> worldObjects)
    {
        // 注意：originHeight 属于 [0, 1]

        // TerrainLoader.Instance.GetNoise();
        if (originHeight > WorldConfig.waterLevelHeight)
        {
            // 水上 world object
            if (rng.NextDouble() < Mathf.Pow((grassNoise.GetNoise(x, z) + 1f) * 0.5f, 0.5f) && rng.NextDouble() < 0.1)
            {
                GrassParticleData grassParticleData = new GrassParticleData();
                grassParticleData.position = new Vector4(x, originHeight * WorldConfig.heightMultiplier, z, 0);
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
                    
                    // if (objectsNoise.GetNoise(i, j) * noiseWeight + n > rng.NextDouble())
                    // {
                        // Random World Object for this biome
                    // }
                    break;
                }
            }
        }
        else
        {
            // 水下 world object

        }
    }
}