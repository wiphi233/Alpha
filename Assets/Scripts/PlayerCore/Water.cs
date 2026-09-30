using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Water : MonoBehaviour
{
    private float waterSpeed;
    public static int waterCount = 0;
    //private float snowSpeed = 0.7f;

    private void Start()
    {
        waterCount = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 判断进入的是不是玩家（可以通过Tag或TryGetComponent）
        if (other.CompareTag("Player"))
        {
            if (waterCount == 0)
            {
                Debug.Log("Player is enter water.");
                PlayerMovement player = other.GetComponent<PlayerMovement>();
                if (player != null)
                {
                    waterSpeed = 0.4f;
                    player.speedMultiplier = waterSpeed;
                }
            }
            waterCount++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            waterCount--;
            waterCount = Mathf.Max(waterCount, 0);
            if (waterCount == 0)
            {
                Debug.Log("Player is exit water.");
                PlayerMovement player = other.GetComponent<PlayerMovement>();
                if (player != null)
                {
                    waterSpeed = 1f;
                    player.speedMultiplier = waterSpeed;
                }
            }
        }
    }
}
