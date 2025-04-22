using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ButtonState
{
    both,
    color,
    tone
}

public enum ColorType
{
    Red,
    Green,    
    Blue
}

public enum ToneType
{
    High,
    Middle,
    Low
}

public class ButtonChecker : MonoBehaviour
{
    public ButtonState buttonState;
    public ColorType requiredColor;
    public ToneType requiredTone;
    public bool playerInRange = false;

    private NewInput inputActions;

    private void Start()
    {
        inputActions = new NewInput();
    }

    void Update()
    {
        #region 暂时弃置
        //使用inputSystem面板中设置的
        //if (inputActions.PC.Mouse.ReadValue<float>()==1)
        //{

        //}
        #endregion

        //使用inputSystem设置好的
        if (playerInRange && Mouse.current.leftButton.wasPressedThisFrame)
        {
            LightSphereGeneration.Instance.CheckScore(buttonState, requiredColor, requiredTone);//检测最近

        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
        
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }

    }
}
