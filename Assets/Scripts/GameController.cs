using Unity.VisualScripting;
using UnityEngine;

public class GameController : MonoBehaviour
{
  [SerializeField]
  private BottleController FirstBottle;
  [SerializeField]
  private BottleController SecondBottle;

  public AudioManager audioManager;
  public VictoryScreen victoryScreen;
  public LevelGenerator levelGenerator;

  private void Awake()
  {
    audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
  }

  // Update is called once per frame
  private void Update()
  {
    if (Input.GetMouseButtonDown(0))
      HandleInput();

  }

  private void HandleInput()
  {
    // Convert mouse position to world point to raycast
    Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);

    RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

    if (hit.collider == null) return;

    BottleController clickedBottle = hit.collider.GetComponent<BottleController>();
        
    if (clickedBottle == null) return;

    if (FirstBottle == null)
    {
      // No bottle selected yet, select the first one
      FirstBottle = clickedBottle;
      audioManager.PlaySFX(audioManager.pickUp);
      FirstBottle.SetSelected(true);
    }
    else
    {
      // A bottle is already selected
      if (FirstBottle == clickedBottle)
      {
        // Clicked the same bottle twice, deselect it
        audioManager.PlaySFX(audioManager.dropDown);
        FirstBottle.SetSelected(false);
        FirstBottle = null;
      }
      else
      {
      // Clicked a different bottle, attempt to transfer
        SecondBottle = clickedBottle;
        AttemptTransfer();
      }
    }
  }

    private void AttemptTransfer() // Transfer liquid from FirstBottle to SecondBottle
  {
      FirstBottle.bottleControllerRef = SecondBottle;

      FirstBottle.UpdateTopColorValues();
      SecondBottle.UpdateTopColorValues();

      if (SecondBottle.FillBottleCheck(FirstBottle.topColor))
      {
      FirstBottle.SetSelected(false);
      FirstBottle.onTransferComplete += CheckLevelComplete; // Subscribe to transfer complete event
      audioManager.PlaySFX(audioManager.pour);
      FirstBottle.StartColorTransfer();
        // Reset selection after successful transfer start
        FirstBottle = null;
        SecondBottle = null;
      }
      else
      {
      // Invalid move, reset selection
        audioManager.PlaySFX(audioManager.dropDown);
        FirstBottle.SetSelected(false);
        FirstBottle = null;
        SecondBottle = null;
      }
    }
    public void CheckLevelComplete()
    {
      BottleController[] allBottles = FindObjectsByType<BottleController>(FindObjectsSortMode.None);
      bool allSolved = true;
      foreach (BottleController bottle in allBottles)
      {
        if (!bottle.IsSolved())
          return;   
      }
      if (allSolved)
      {
        Debug.Log("Level Complete!");
        foreach (BottleController bottle in allBottles)
        {
        Destroy(bottle.gameObject);
        }
      victoryScreen.Setup();

    }
    }
}
