using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class LightSphereControl : MonoBehaviour
{
    public ColorType color;
    public ToneType tone;

    private Vector3 EndPoint;
    private bool StartMove = false;
    private bool canDestory = false;
    private Tween tween;
    [SerializeField] private float moveSpeed = 2;//移动速度，或许需要动态调整
    private float moveTime;//t=s/v

    private void Awake()
    {
        EndPoint = Vector3.zero;
    }

    void Start()
    {

    }


    void Update()
    {
        if (StartMove)
        {
            MoveAtTarget();
            StartMove = false;
            canDestory = true;
        }
        if (canDestory)
        {
            DestroyObject();
        }
    }

    public void SetEndPointTarget(Transform target, float Speed, bool StM)
    {
        EndPoint=target.position;
        moveSpeed = Speed;
        StartMove = StM;
    }

    private void MoveAtTarget()
    {
        moveTime= Vector3.Distance(EndPoint, transform.position)/moveSpeed;
        //Debug.Log(moveTime);
        tween =transform.DOMove(EndPoint, moveTime).SetEase(Ease.Linear);
    }

    private void DestroyObject()
    {
        float moveDirection = Vector3.Distance(EndPoint,transform.position);
        if (moveDirection<=0.1f)
        {
            DoTweenKill();
            LightSphereGeneration.Instance.DestroyCurrent(this.gameObject);
        }
    }

    public void DoTweenKill()
    {
        tween.Kill();
    }

}
