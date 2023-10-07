using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TitleSceneController : MonoBehaviour
{
  public GameObject cloudPrefab;
  public GameObject sunPrefab;
  public GameObject sunraysPrefab;
  public GameObject logoPrefab;

  private Sun sun;
  private GameObject logoObject;

  private float dspSongTime;
  private float songPosition = 0f;
  public float songPositionInBeats = 0f;

  // public TMP_Text highScoreText;
  // private int highScore;

  public struct Cloud
  {
    public GameObject gameObject;
    public float speed;
    public float scale;

    public Cloud(GameObject _object, float _speed, float _scale)
    {
      gameObject = _object;
      speed = _speed;
      scale = _scale;
    }
  }

  private List<Cloud> clouds = new List<Cloud>();

  void SpawnCloud(float x = 15f) {    
    float y = Random.Range(0.2f, 0.45f);
    float scale = Random.Range(0.25f, 0.5f);
    Debug.Log("About to spawn cloud at " + y);
    SpawnCloud(x, y, scale);
  }

  void SpawnCloud(float x, float y, float scale) {
    Debug.Log("Spawn Cloud");
    Vector3 startVector = new Vector3(x, y, 0f);
    GameObject newCloud = Instantiate(cloudPrefab, startVector, Quaternion.identity) as GameObject;    
    newCloud.transform.localScale = new Vector3(0, 0, 1);

    if (scale < 0.35f) {
      SpriteRenderer cloudRenderer = newCloud.GetComponent<SpriteRenderer>();
      cloudRenderer.sortingLayerName = "Cloud";
      cloudRenderer.color = new Color(237, 237, 237);
    }
  Debug.Log("Added to list");
    clouds.Add(new Cloud(newCloud, 0.5f - (scale * scale), scale));    
  }

  IEnumerator SpawnCloud(float x, float y, float scale, float waits) 
  {    
    yield return new WaitForSeconds(waits);

    SpawnCloud(x, y, scale);
  }

  void DespawnCloud(Cloud cloud) {
    Destroy(cloud.gameObject, 2f);
    clouds.Remove(cloud);    
  }

  /*****************************
   ***** ALL THINGS SUN ******
   *****************************/
  private struct Sun {
    public GameObject sunObject;
    public GameObject raysObject;

    public Vector3 endSunVector;
    public Vector3 endSunRaysVector;

    public float expandingDirection;

    public Sun(GameObject _sun, GameObject _rays)
    {
      sunObject = _sun;
      raysObject = _rays;
      endSunVector = new Vector3(0, 1.2f, 0);
      endSunRaysVector = new Vector3(0, 1.2f, 0);
      expandingDirection = 1f;
    }
  }

  void SpawnSun() {
    GameObject sunObject = Instantiate(sunPrefab, new Vector3(0, -10, 0), Quaternion.identity) as GameObject;
    SpriteRenderer sunRenderer = sunObject.GetComponent<SpriteRenderer>();
    sunRenderer.sortingOrder = -1;
    sunObject.transform.localScale = new Vector3(0.5f, 0.5f, 1);

    GameObject raysObject = Instantiate(sunraysPrefab, new Vector3(0, 1.2f, 0), Quaternion.identity) as GameObject;;
    raysObject.transform.localScale = new Vector3(0, 0, 1);

    sun = new Sun(sunObject, raysObject);
  }

  /*****************************
          LOGO METHODS 
   *****************************/
  void SpawnLogo() {
    logoObject = Instantiate(logoPrefab, new Vector3(0, 1, 0), Quaternion.identity) as GameObject;
    SpriteRenderer logoRenderer = logoObject.GetComponent<SpriteRenderer>();
    sunRenderer.sortingOrder = 10;
    sunObject.transform.localScale = new Vector3(0f, 0f, 1);
  }

  void Start()
  {
    cowbell = GetComponent<AudioSource>();
    dspSongTime = (float)AudioSettings.dspTime;
    
    StartCoroutine(SpawnCloud(7.37f, 3.19f, 0.48f, 2 * CupConductor.SecPerBeat));
    StartCoroutine(SpawnCloud(8.87f, -0.07f, 0.33f, 4 * CupConductor.SecPerBeat));
    StartCoroutine(SpawnCloud(-6.12f, 4f, 0.13f, 6 * CupConductor.SecPerBeat));
    StartCoroutine(SpawnCloud(-6.9843f, 0.5f, 0.388f, 8 * CupConductor.SecPerBeat));    

    Invoke("SpawnSun", 4 * CupConductor.SecPerBeat);

    // highScore = PlayerPrefs.GetInt("HighScore", 0);
    // highScoreText.text = highScore.ToString();
  }

  // Update is called once per frame
  void Update()
  {
    bool isLogoGrownYet = false;

    if (logoObject != null) {
      isLogoGrownYet = logoObject.transform.localScale.x < 0.5;      
    }
    

    for (int i = 0; i < clouds.Count; i++) {
      Cloud cloud = clouds[i];
      if (cloud.gameObject.transform.localScale.x < cloud.scale) {
        float scale = cloud.gameObject.transform.localScale.x + 0.02f;
        cloud.gameObject.transform.localScale = new Vector3(scale, scale, 1);
      }
      cloud.gameObject.transform.Translate(Vector2.left * cloud.speed * Time.deltaTime);

      if (cloud.gameObject.transform.position.x < -12) {
        DespawnCloud(cloud);
        Debug.Log(clouds.Count);
        SpawnCloud();
      }
    }

    // Sun animations
    if (sun.sunObject) {
      float sunScaleX = sun.raysObject.transform.localScale.x;
      if (Mathf.Round(sun.sunObject.transform.position.y * 100f)/100f < Mathf.Round(sun.endSunVector.y * 100f)/100f) {
        sun.sunObject.transform.Translate(Vector2.up * (sun.endSunVector.y - sun.sunObject.transform.position.y + 0.2f) * Time.deltaTime);
      } else { 
        if (sunScaleX == 0.5) {
          SpawnLogo();
        }
        if (sunScaleX < 0.5) {
          float sunNewScale = sunScaleX + 0.05f;
          sun.raysObject.transform.localScale = new Vector3(sunNewScale, sunNewScale, 1);        
        } else if (sunScaleX == 0.5 && isLogoGrownYet) {
          sun.raysObject.transform.Rotate(new Vector3(0, 0, CupConductor.SecPerBeat/8));
          float sunNewScale = sunScaleX + 0.001f * sun.expandingDirection;
          if (clouds.Count == 3) {
            sun.expandingDirection = -sun.expandingDirection;
          }
          
          sun.raysObject.transform.localScale = new Vector3(sunNewScale, sunNewScale, 1);
        }        
      }
    }

    // Logo animations
    if (isLogoGrownYet) {
      float logoNewScale = logoObject.transform.localScale.x + 0.2f;
      logoObject.transform.localScale = new Vector3(logoNewScale, logoNewScale, 1);        
    }

    if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.A))
    {
      Invoke("LoadMainScene", 0.5f);
    }
  }

  void LoadMainScene()
  {
    SceneManager.LoadScene("Main");
  }
}
