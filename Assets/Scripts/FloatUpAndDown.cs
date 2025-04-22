using UnityEngine;

public class FloatUpAndDown : MonoBehaviour
{
    // 上下浮动的速度
    public float speed = 1.0f;
    // 上下浮动的幅度（即高度）
    public float magnitude = 1.0f;

    // 初始位置，用于计算偏移量
    private Vector3 startPos;

    void Start()
    {
        // 获取物体的初始位置
        startPos = transform.position;
    }

    void Update()
    {
        // 计算y轴方向上的偏移量，这里使用Mathf.Sin函数来创建一个正弦波形
        float yOffset = Mathf.Sin(Time.time * speed) * magnitude;

        // 更新物体的位置
        transform.position = startPos + new Vector3(0, yOffset, 0);
    }
}
