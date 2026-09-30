using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Ball : MonoBehaviour
{
    [SerializeField] private float cycleSpeed = 0.1f; // 循环速度（每秒转多少圈）
    [SerializeField] private float saturation = 1f;   // 饱和度 (0-1)
    [SerializeField] private float value = 1f;        // 明度 (0-1)

    private Renderer modelRenderer;
    private float hue = 0f;

    [Header("Scripts Instance References 脚本实例引用")]
    public GameObject signalCenterObject;
    private ISignalCenter signalCenter;
    //private MeshRenderer meshRenderer;

    void Start()
    {
        gameObject.SetActive(true);
        modelRenderer = GetComponent<Renderer>();
        if (modelRenderer == null)
        {
            modelRenderer = GetComponentInChildren<Renderer>();
        }
        signalCenter = signalCenterObject.GetComponent<ISignalCenter>();
        signalCenter.Subscribe(SignalType.ChangeAngularBall, OnChangeAngularBall);
    }

    void OnChangeAngularBall(GameObject sender, object data)
    {
        //modelRenderer.enabled = (AngularBallState)data == AngularBallState.Active;
    }

    void FixedUpdate()
    {
        if (modelRenderer.enabled)
        {
            hue += Time.fixedDeltaTime * cycleSpeed;
            if (hue >= 1f)
            {
                hue -= 1f;
            }
            Color currentColor = Color.HSVToRGB(hue, saturation, value);
            modelRenderer.material.color = currentColor;
            Transform transform = GetComponent<Transform>();
            transform.Rotate(Vector3.left, transform.rotation.y + 0.1f);
        }
    }
}