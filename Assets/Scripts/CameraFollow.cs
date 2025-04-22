using DG.Tweening;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public Transform cameraTarget;
    public float smoothSpeed = 5f; // 平滑跟随速度
    public Vector3 offset = new Vector3(0, 2, -5);

    private Transform camTransform;
    private Tween currentTween;

    void Start()
    {
        camTransform = GetComponent<Transform>();
        UpdateCameraPosition();
    }

    void LateUpdate() // 改用 LateUpdate 以确保在所有更新后移动相机
    {
        UpdateCameraPosition();
    }

    void UpdateCameraPosition()
    {
        if (cameraTarget == null || player == null)
            return;

        Vector3 targetPosition = cameraTarget.position;
        float distance = Vector3.Distance(targetPosition, transform.position);

        // 只有当距离较大时才使用 DOTween，否则使用普通插值
        if (distance > 5f)
        {
            // 如果已经有正在进行的 Tween，先停止它
            if (currentTween != null && currentTween.IsPlaying())
            {
                currentTween.Kill();
            }
            
            // 创建新的 Tween
            currentTween = camTransform.DOMove(targetPosition, 1f)
                                     .SetEase(Ease.OutQuad);
        }
        else
        {
            // 对于小距离移动使用普通插值
            transform.position = Vector3.Lerp(
                transform.position, 
                targetPosition, 
                smoothSpeed * Time.deltaTime
            );
        }
    }

    private void OnDisable()
    {
        // 确保在禁用时清理 Tween
        if (currentTween != null)
        {
            currentTween.Kill();
        }
    }
}
