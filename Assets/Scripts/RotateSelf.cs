using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateSelf : MonoBehaviour
{
    // 旋转速度（单位：度/秒）
    public Vector3 rotationSpeed = new Vector3(0, 50, 0);

    void Update()
    {
        // 根据时间增量计算旋转角度
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}
