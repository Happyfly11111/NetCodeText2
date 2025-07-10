using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyCtrl : MonoBehaviour
{
    public static BodyCtrl Instance;
    private Transform playerModel;
    private float rotationSpeed = 100f;
    private bool isRotating = false;
    private float targetRotation = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SwitchGender(int gender)
    {
        transform.Find("Player").GetChild(gender).gameObject.SetActive(true);
        transform.Find("Player").GetChild(1 - gender).gameObject.SetActive(false);
        playerModel = transform.Find("Player").GetChild(gender);
    }

    public void RotateModel(float deltaX)
    {
        // 根据拖动的距离计算旋转角度，可以调整系数来控制旋转速度
        float rotationAmount = deltaX * rotationSpeed * Time.deltaTime;
        Debug.Log($"RotateModel called - deltaX: {deltaX}, rotationAmount: {rotationAmount}");
        playerModel.Rotate(Vector3.up, -rotationAmount);
    }

    public void StopRotation()
    {
        Debug.Log("StopRotation called");
        isRotating = false;
    }
}
