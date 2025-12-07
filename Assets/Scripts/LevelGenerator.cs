using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
  [Header("Settings")]
  public int numberOfColors = 4;
  public int emptyBottles = 4;
  public int shuffleSteps = 20;

  [Header("Assets")]
  public BottleController bottlePrefab;
  public Color[] colorPalette;
  public float distanceBetweenBottles = 2f;

  private void Start()
  {
    GenerateLevel();
  }

  public void GenerateLevel()
  {
    // 1. Create initial solved State
    int totalBottles = numberOfColors + emptyBottles;
    List<List<Color>> logicalBottles = new List<List<Color>>();

    // Validate Palette
    if (colorPalette.Length < numberOfColors)
    {
      Debug.LogError("Not enough colors in Palette to generate level!");
      return;
    }

    // Initialize Bottles
    for (int i = 0; i < totalBottles; i++)
    {
      List<Color> currentBottle = new List<Color>();
      // The first 'numberOfColors' bottles get filled completely with a distinct color
      if (i < numberOfColors)
      {
        for (int layer = 0; layer < 4; layer++)
        {
          currentBottle.Add(colorPalette[i]);
        }
      }
      // The rest remain empty (count 0)
      logicalBottles.Add(currentBottle);
    }

    // Shuffle the Board (The Reverse Move Logic)
    for (int i = 0; i < shuffleSteps; i++)
    {
      int srcIndex = Random.Range(0, totalBottles);
      int dstIndex = Random.Range(0, totalBottles);

      // Constraints for a valid shuffle move:
      // 1. Source and Dest must be different
      // 2. Source must have liquid to give
      // 3. Dest must have space to receive (max 4 layers)
      if (srcIndex != dstIndex &&
          logicalBottles[srcIndex].Count > 0 &&
          logicalBottles[dstIndex].Count < 4)
      {
        // Perform the move in our logical list
        Color colorToMove = logicalBottles[srcIndex][logicalBottles[srcIndex].Count - 1];
        logicalBottles[srcIndex].RemoveAt(logicalBottles[srcIndex].Count - 1);
        logicalBottles[dstIndex].Add(colorToMove);
      }
      else
      {
        // If move failed, try again in this iteration or just skip
        // Decrementing i ensures ensure 'shuffleSteps' valid moves occur
        i--;
      }
    }

    // Spawn Bottles
    SpawnBottles(logicalBottles);
  }

  private void SpawnBottles(List<List<Color>> logicalBottles)
  {
    float startX = -((logicalBottles.Count - 1) * distanceBetweenBottles) / 2f;
    for (int i = 0; i < logicalBottles.Count; i++)
    {
      // Calculate spawn position
      Vector3 spawnPos = new Vector3(startX + (i * distanceBetweenBottles), 0, 0);
      BottleController newBottle = Instantiate(bottlePrefab, spawnPos, Quaternion.identity);
      List<Color> layers = logicalBottles[i];
      for (int j = 0; j < 4; j++)
      {
        if (j < layers.Count)
        {
          newBottle.bottleColors[j] = layers[j];
        }
        else
        {
          newBottle.bottleColors[j] = Color.clear;
        }
      }
      newBottle.numberOfColorsInBottle = layers.Count;
      newBottle.name = "Bottle_" + i;
    }
  }
}
