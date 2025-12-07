using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class VictoryScreen : MonoBehaviour
{
    public void Setup()
  {
    gameObject.SetActive(true);
  }
  public void PlayAgainButton()
  {
    SceneManager.LoadScene("Project");
  }
    
}
