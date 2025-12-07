using System.Collections;
using System.ComponentModel;
using UnityEngine;
using System;

public class BottleController : MonoBehaviour
{
  public Action onTransferComplete;

  private const int maxLayers = 4;

  public Color[] bottleColors;
  public SpriteRenderer bottleMaskSR;

  public float rotationDuration = 2f;
  public AnimationCurve SARMCurve;
  public AnimationCurve FillAmountCurve;
  public AnimationCurve RotationSpeedMultiplier;

  public float[] fillAmounts;
  public float[] rotationValues;

  private int rotationIndex = 0;

  [Range(0, maxLayers)]
  public int numberOfColorsInBottle = maxLayers;

  public Color topColor;
  public int numberOfTopColorLayers = 1;

  private bool isRotating = false;

  public BottleController bottleControllerRef;
  private int numberOfColorsToTransfer = 0;

  [Header("Rotation Points")]
  public Transform leftRotationPoint;
  public Transform rightRotationPoint;
  private Transform chosenRotationPoint;
  private float directionMultiplier = 1f;

  Vector3 originalPosition;
  Vector3 startPosition;
  Vector3 endPosition;

  [Header("Selection Settings")]
  public float selectionHeightOffset = 0.5f;
  public float selectionMoveSpeed = 5f;
  private bool isSelected = false;
  private Vector3 targetPosition;


  void Start()
  {
    // Initialize shader fill amount based on current liquid level
    bottleMaskSR.material.SetFloat("_FillAmout", fillAmounts[numberOfColorsInBottle]);

    originalPosition = transform.position;
    targetPosition = originalPosition;

    UpdateColorsOnShader();
    UpdateTopColorValues();
  }

  void Update()
  {
    if (!isRotating) // Only allow selection movement when not rotating
    {
      if (transform.position != targetPosition)
      {
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * selectionMoveSpeed);
      }
    }
  }

  private void UpdateColorsOnShader()
  {
    // Efficiently set shader colors based on the bottleColors array
    for (int i = 0; i < maxLayers; i++)
    {
        bottleMaskSR.material.SetColor($"_C{i + 1}", bottleColors[i]);
    }
  }

  IEnumerator MoveBottle()
  {
    startPosition = transform.position;
    if (chosenRotationPoint == rightRotationPoint)
      endPosition =  bottleControllerRef.leftRotationPoint.position;
    else
      endPosition = bottleControllerRef.rightRotationPoint.position;
    float t = 0f;
    while (t<=1)
    {
      transform.position = Vector3.Lerp(startPosition, endPosition, t);
      t += Time.deltaTime * 2f;

      yield return new WaitForEndOfFrame();
    }
    transform.position = endPosition;
    StartCoroutine(RotateBottle());
  }

  IEnumerator RotateBottle()
  {
    float t = 0f;
    float lerpValue = 0f;
    float angleValue = 0f;

    float lastAngleValue = 0f;

    isRotating = true;

    while (t< rotationDuration)
    {
      t += Time.deltaTime;

      lerpValue = t / rotationDuration;
      angleValue = Mathf.Lerp(0f, directionMultiplier * rotationValues[rotationIndex], lerpValue);
      // lerp value goes from 0 to 1 over rotationDuration seconds
      transform.RotateAround(chosenRotationPoint.position, Vector3.forward, lastAngleValue - angleValue);


      bottleMaskSR.material.SetFloat("_SARM", SARMCurve.Evaluate(angleValue)); // Set SARM based on current angle

      if (fillAmounts[numberOfColorsInBottle] > FillAmountCurve.Evaluate(angleValue)+0.005f) // Only fill if there's enough liquid to pour
      {
        bottleMaskSR.material.SetFloat("_FillAmout", FillAmountCurve.Evaluate(angleValue));

        bottleControllerRef.FillUp(FillAmountCurve.Evaluate(lastAngleValue) - FillAmountCurve.Evaluate(angleValue));
      }

      t += Time.deltaTime * RotationSpeedMultiplier.Evaluate(angleValue);
      lastAngleValue = angleValue;
      yield return new WaitForEndOfFrame();
    }
    angleValue = directionMultiplier * rotationValues[rotationIndex];
    bottleMaskSR.material.SetFloat("_SARM", SARMCurve.Evaluate(SARMCurve.Evaluate(angleValue)));
    bottleMaskSR.material.SetFloat("_FillAmout", FillAmountCurve.Evaluate(angleValue));

    numberOfColorsInBottle -= numberOfColorsToTransfer;
    bottleControllerRef.numberOfColorsInBottle += numberOfColorsToTransfer;

    StartCoroutine(RotateBottleBack());
  }
  IEnumerator RotateBottleBack()
  {
    float t = 0;
    float lerpValue;
    float angleValue;

    float lastAngleValue = directionMultiplier * rotationValues[rotationIndex];
    while (t < rotationDuration)
    {
      t += Time.deltaTime;
      lerpValue = t / rotationDuration;
      angleValue = Mathf.Lerp(directionMultiplier * rotationValues[rotationIndex], 0f, lerpValue);

      //transform.eulerAngles = new Vector3(0f, 0f, angleValue);

      transform.RotateAround(chosenRotationPoint.position, Vector3.forward, (lastAngleValue - angleValue));

      bottleMaskSR.material.SetFloat("_SARM", SARMCurve.Evaluate(angleValue));

      lastAngleValue = angleValue;

      t += Time.deltaTime;

      yield return new WaitForEndOfFrame();
    }
    UpdateTopColorValues();
    angleValue = 0f;
    transform.eulerAngles = new Vector3(0f, 0f, angleValue);
    bottleMaskSR.material.SetFloat("_SARM", SARMCurve.Evaluate(SARMCurve.Evaluate(angleValue)));
    isRotating = false;

    StartCoroutine(MoveBottleBack());
  }
  IEnumerator MoveBottleBack()
  {
    startPosition = transform.position;
    endPosition = originalPosition;

    float t = 0f;

    while (t <= 1)
    {
      transform.position = Vector3.Lerp(startPosition, endPosition, t);
      t += Time.deltaTime * 2f;

      yield return new WaitForEndOfFrame();
    }
    transform.position = endPosition;

    transform.GetComponent<SpriteRenderer>().sortingOrder -= 2;
    bottleMaskSR.sortingOrder -= 2;

    onTransferComplete?.Invoke();
    onTransferComplete = null;

  }
  public void StartColorTransfer()
  {

    ChooseRotationPointAndDirection();

    // Calculate how many layers can be moved 
    // It's the minimum of (Layers available to move) AND (Empty space in target bottle)
    int emptySpaceInTarget = maxLayers - bottleControllerRef.numberOfColorsInBottle;
    numberOfColorsToTransfer = Mathf.Min(numberOfTopColorLayers, emptySpaceInTarget);

    // Update the target bottle's data
    for (int i = 0; i < numberOfColorsToTransfer; i++)
    {
      bottleControllerRef.bottleColors[bottleControllerRef.numberOfColorsInBottle + i] = topColor;
    }
    bottleControllerRef.UpdateColorsOnShader();

    // Prepare for Animation
    CalculateRotationIndex(emptySpaceInTarget);

    // Bring sorting order to the front
    transform.GetComponent<SpriteRenderer>().sortingOrder += 2;
    bottleMaskSR.sortingOrder += 2;

    StartCoroutine(MoveBottle());
  }

  public void UpdateTopColorValues()
  {
    if (numberOfColorsInBottle <= 0)
    {
        numberOfTopColorLayers = 0;
        return;
    }

    // Prevent crossing limit
    int topIndex = Mathf.Clamp(numberOfColorsInBottle - 1, 0, bottleColors.Length - 1);
    topColor = bottleColors[topIndex];

    // Count how many consecutive layers of the same color exist at the top
    numberOfTopColorLayers = 1;
    for (int i = topIndex - 1; i >= 0; i--)
    {
      if (bottleColors[i].Equals(topColor))
      {
          numberOfTopColorLayers++;
      }
      else
      {
          break;
      }
    }

    // Determine the rotation required based on how full the bottle is vs how much we are pouring
    rotationIndex = 3 - (numberOfColorsInBottle - numberOfTopColorLayers);
  }
  public bool FillBottleCheck(Color colorToCheck)
  {
    if (numberOfColorsInBottle == 0) return true;
    if (numberOfColorsInBottle >= maxLayers) return false;
    return topColor.Equals(colorToCheck);
  }

  private void CalculateRotationIndex(int numberOfEmptySpacesInSecondBottle)
  {
    rotationIndex = 3 - (numberOfColorsInBottle - Mathf.Min(numberOfEmptySpacesInSecondBottle, numberOfTopColorLayers));
  }

  private void FillUp(float fillAmountToAdd)
  {
    bottleMaskSR.material.SetFloat("_FillAmout", bottleMaskSR.material.GetFloat("_FillAmout") + fillAmountToAdd);
  }
  private void ChooseRotationPointAndDirection() // Decide which side to rotate around based on relative position to target bottle
  {
    if (transform.position.x < bottleControllerRef.transform.position.x)
    {
      chosenRotationPoint = rightRotationPoint;
      directionMultiplier = 1f;
    }
    else
    {
      chosenRotationPoint = leftRotationPoint;
      directionMultiplier = -1f;
    }
  }
  public bool IsSolved()
  {
    if (numberOfColorsInBottle == 0)
      return true;
    if (numberOfColorsInBottle < maxLayers)
      return false;
    Color baseColor = bottleColors[0];
    for (int i = 1; i < numberOfColorsInBottle; i++)
    {
      if (bottleColors[i] != baseColor)
        return false;
    }
    return true;
  }
  public void SetSelected(bool selected)
  {
    if (isRotating) return;

    isSelected = selected;

    if (isSelected)
    {
      targetPosition = originalPosition + Vector3.up * selectionHeightOffset;
    }
    else
    {
      targetPosition = originalPosition;
    }
  }
}
