using UnityEngine;
using TMPro;
using DanielLochner.Assets.SimpleScrollSnap;

public class AlphaScroll : MonoBehaviour
{
  [SerializeField] private RectTransform[] letters;
  [SerializeField] private SimpleScrollSnap scrollSnap;
  [SerializeField] private GameObject navElements;
  private float activationTime;
  private bool isActive;

  public bool IsActive
  {
    get => isActive;
    set
    {
      isActive = value;
      if (value) {
        activationTime = Time.time;
      }
    }
  }

  void Update()
  {
    if (!IsActive) {
      navElements.SetActive(false);
      return;
    }

    navElements.SetActive(true);

    // Prevent input processing for first 0.1 seconds after activation to avoid instant selection
    if (Time.time - activationTime < 0.5f) return;

    if (Input.GetKeyDown(KeyCode.A)) {
      scrollSnap.GoToPreviousPanel();
    } else if (Input.GetKeyDown(KeyCode.Q)) {
      scrollSnap.GoToNextPanel();
    } else if (Input.GetKeyDown(KeyCode.S)) {
      SelectLetter();
    }
  }

  private void SelectLetter()
  {
    NameEntryController.Instance.ConfirmSelection();
  }

  public string GetSelectedLetter()
  {
    int panelIndex = scrollSnap.CenteredPanel;
    Transform panelTransform = scrollSnap.Panels[panelIndex].transform;

    var text = panelTransform.GetComponentInChildren<TextMeshProUGUI>();
    if (text != null) return text.text;

    // Fallback
    return "A";
  }
}
