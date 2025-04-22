using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Vector2 moveBoundaryMin = new Vector2(-10f, -10f);
    [SerializeField] private Vector2 moveBoundaryMax = new Vector2(10f, 10f);

    private NewInput inputActions;
    private void Awake()
    {
        inputActions=new NewInput();
    }
    void Start()
    {
        
    }

    void Update()
    {
        Movement();
    }

    private void OnEnable()
    {
        inputActions.PC.Enable();
    }

    private void OnDisable()
    {
        inputActions.PC.Disable();
    }

    private void Movement()
    {
        //获取inputSystem的输出
        Vector2 moveVector2=inputActions.PC.Move.ReadValue<Vector2>();  
        float htal = moveVector2.x;
        float vtal = moveVector2.y;

#if UNITY_EDITOR
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical"); 
        //Debug.Log("x:" + htal + "z:" + vtal);
#endif

        Vector3 movement = new Vector3(htal, 0.0f, vtal).normalized;
        transform.position += movement * Time.deltaTime * moveSpeed;

        // 限制移动范围
        float clampedX = Mathf.Clamp(transform.position.x, moveBoundaryMin.x, moveBoundaryMax.x);
        float clampedZ = Mathf.Clamp(transform.position.z, moveBoundaryMin.y, moveBoundaryMax.y);
        transform.position = new Vector3(clampedX, transform.position.y, clampedZ);

    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 绘制移动边界
        Gizmos.color = Color.blue;
        Vector3 center = new Vector3(
            (moveBoundaryMin.x + moveBoundaryMax.x) / 2,
            1f,
            (moveBoundaryMin.y + moveBoundaryMax.y) / 2
        );
        Vector3 size = new Vector3(
            moveBoundaryMax.x - moveBoundaryMin.x,
            0.1f,
            moveBoundaryMax.y - moveBoundaryMin.y
        );
        Gizmos.DrawWireCube(center, size);

    }
#endif
}
