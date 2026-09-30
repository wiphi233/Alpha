using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicalCameraSway : MonoBehaviour
{
    [Header("移动晃动")]
    public float positionSmoothTime = 0.1f;
    public Vector3 moveAmplitude = new Vector3(0.05f, 0.1f, 0.03f);

    [Header("视角晃动")]
    public float rotationSmoothTime = 0.2f;
    public Vector3 rotationAmplitude = new Vector3(1f, 0.5f, 0.8f);

    [Header("着陆冲击")]
    public float landShakeAmount = 0.3f;
    public float landShakeDuration = 0.5f;

    private Vector3 posVelocity;
    private Vector3 rotVelocity;
    private Vector3 originalPos;
    private Quaternion originalRot;
    private float landTimer;

    void Start()
    {
        originalPos = transform.localPosition;
        originalRot = transform.localRotation;
    }

    void Update()
    {
        // 移动输入检测
        float inputX = Input.GetAxis("Horizontal");
        float inputY = Input.GetAxis("Vertical");
        bool isMoving = Mathf.Abs(inputX) > 0.1f || Mathf.Abs(inputY) > 0.1f;

        // 移动晃动
        Vector3 targetPos = originalPos;
        if (isMoving)
        {
            targetPos += new Vector3(
                Mathf.Sin(Time.time * 10) * moveAmplitude.x * inputY,
                Mathf.Sin(Time.time * 20) * moveAmplitude.y,
                Mathf.Sin(Time.time * 10) * moveAmplitude.z * inputX
            );
        }

        // 着陆冲击
        if (landTimer > 0)
        {
            targetPos.y -= landShakeAmount * (landTimer / landShakeDuration);
            landTimer -= Time.deltaTime;
        }

        transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition,
            targetPos,
            ref posVelocity,
            positionSmoothTime
        );

        // 视角旋转
        Quaternion targetRot = originalRot;
        if (isMoving)
        {
            targetRot *= Quaternion.Euler(
                Mathf.Sin(Time.time * 8) * rotationAmplitude.x * inputY,
                Mathf.Sin(Time.time * 4) * rotationAmplitude.y,
                Mathf.Sin(Time.time * 6) * rotationAmplitude.z * inputX
            );
        }

        transform.localRotation = QuaternionUtil.SmoothDamp(
            transform.localRotation,
            targetRot,
            ref rotVelocity,
            rotationSmoothTime
        );
    }

    // 在角色着陆时调用此方法
    public void TriggerLandShake()
    {
        landTimer = landShakeDuration;
    }
}

// 辅助类：四元数平滑阻尼
public static class QuaternionUtil
{
    public static Quaternion SmoothDamp(Quaternion current, Quaternion target, ref Vector3 velocity, float smoothTime)
    {
        Vector3 c = current.eulerAngles;
        Vector3 t = target.eulerAngles;
        return Quaternion.Euler(
            Mathf.SmoothDampAngle(c.x, t.x, ref velocity.x, smoothTime),
            Mathf.SmoothDampAngle(c.y, t.y, ref velocity.y, smoothTime),
            Mathf.SmoothDampAngle(c.z, t.z, ref velocity.z, smoothTime)
        );
    }
}