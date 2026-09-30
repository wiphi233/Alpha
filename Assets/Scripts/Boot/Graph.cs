using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph
{
    public RectTransform rectTransform;
    private Vector2 velocity;
    private float moveSpeed;
    private float changeTimer;
    private float changeInterval;
    private System.Random rng;

    private Vector2 rippleForce;
    private float rippleDecay;

    public Graph(RectTransform rect, float speed, System.Random random)
    {
        rectTransform = rect;
        moveSpeed = speed;
        rng = random;
        changeInterval = Random.Range(1f, 3f); // 每个图形改变方向的间隔不同

        // 随机初始方向
        float angle = Random.Range(0f, Mathf.PI * 2f);
        velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * moveSpeed;
    }

    public void UpdatePosition(Rect canvasRect)
    {
        // 更新计时器
        changeTimer += Time.deltaTime;

        // 定时改变方向
        if (changeTimer >= changeInterval)
        {
            ChangeDirection();
            changeTimer = 0f;
            changeInterval = Random.Range(1f, 3f);
        }

        // 更新位置
        Vector2 pos = rectTransform.anchoredPosition;
        pos += velocity * Time.deltaTime;

        // 边界碰撞检测
        float halfWidth = canvasRect.width / 2;
        float halfHeight = canvasRect.height / 2;
        float halfSize = rectTransform.sizeDelta.x / 2;

        // 左右边界反弹
        if (pos.x + halfSize > halfWidth)
        {
            pos.x = halfWidth - halfSize;
            velocity.x = -velocity.x;
        }
        else if (pos.x - halfSize < -halfWidth)
        {
            pos.x = -halfWidth + halfSize;
            velocity.x = -velocity.x;
        }

        // 上下边界反弹
        if (pos.y + halfSize > halfHeight)
        {
            pos.y = halfHeight - halfSize;
            velocity.y = -velocity.y;
        }
        else if (pos.y - halfSize < -halfHeight)
        {
            pos.y = -halfHeight + halfSize;
            velocity.y = -velocity.y;
        }

        rectTransform.anchoredPosition = pos;

        // 让图形缓慢自转
        rectTransform.eulerAngles += new Vector3(0, 0, velocity.x * 0.1f * Time.deltaTime);
    }

    void ChangeDirection()
    {
        // 随机改变方向（45度范围内）
        float angleChange = Random.Range(-45f, 45f);
        float currentAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        float newAngle = currentAngle + angleChange;

        float angleRad = newAngle * Mathf.Deg2Rad;
        velocity = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * moveSpeed;

        // 偶尔改变速度
        moveSpeed = Random.Range(30f, 80f);
        velocity = velocity.normalized * moveSpeed;
    }

    //public void ApplyRippleForce(float strength, float maxForce = 50000f, float decay = 5f)
    //{
    //    if (strength <= 0f) return;

    //    Vector2 pos = rectTransform.anchoredPosition;
    //    float distance = pos.magnitude;

    //    // ✅ 正确：距离越近，推力越大
    //    float maxDistance = 800f; // 最大影响距离
    //    float forceMultiplier = Mathf.Clamp01(1f - (distance / maxDistance));
    //    forceMultiplier *= strength;

    //    // 可选：让中心效果更集中
    //    forceMultiplier = Mathf.Pow(forceMultiplier, 0.5f); // 平方根让衰减更平缓
    //                                                        // 或者 forceMultiplier = forceMultiplier * forceMultiplier; // 平方让中心更集中

    //    Vector2 direction = pos.normalized;
    //    if (direction == Vector2.zero)
    //    {
    //        float angle = Random.Range(0f, Mathf.PI * 2f);
    //        direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    //    }

    //    float forceMagnitude = maxForce * forceMultiplier;

    //    // 直接施加力（不乘 Time.deltaTime）
    //    velocity += direction * forceMagnitude;

    //    // 放宽速度限制，让效果更明显
    //    velocity = Vector2.ClampMagnitude(velocity, moveSpeed * 10f);

    //    rippleForce = direction * forceMagnitude;
    //    rippleDecay = decay;
    //}
}