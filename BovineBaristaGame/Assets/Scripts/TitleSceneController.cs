using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TitleSceneController : MonoBehaviour
{
  public GameObject cloudPrefab;
  public GameObject sunRays;
  // public TMP_Text highScoreText;
  // private int highScore;

  public struct Cloud
  {
    public GameObject gameObject;
    public float speed;

    public Cloud(GameObject _object, float _speed)
    {
      gameObject = _object;
      speed = _speed;
    }
  }

  private List<Cloud> clouds = new List<Cloud>();

  void SpawnCloud(float x = 15f) {
    float y = Random.Range(0.2f, 0.45f);
    float scale = Random.Range(0.25f, 0.5f);

    SpawnCloud(x, y, scale);
  }

  void SpawnCloud(float x, float y, float scale) 
  {
    Vector3 startVector = new Vector3(x, y, 0f);
    GameObject newCloud = Instantiate(cloudPrefab, startVector, Quaternion.identity) as GameObject;    
    newCloud.transform.localScale = new Vector3(scale, scale, 1);
    clouds.Add(new Cloud(newCloud, 0.5f - (scale * scale)));    
  }

  void DespawnCloud(Cloud cloud) {
    Destroy(cloud.gameObject, 2f);
    clouds.Remove(cloud);    
  }

  void Start()
  {
    SpawnCloud(7.37f, 3.19f, 0.48f);
    SpawnCloud(8.87f, -0.07f, 0.33f);
    SpawnCloud(-6.12f, 4f, 0.13f);
    SpawnCloud(-6.9843f, 0.5f, 0.388f);    

    // highScore = PlayerPrefs.GetInt("HighScore", 0);
    // highScoreText.text = highScore.ToString();
  }

  // Update is called once per frame
  void Update()
  {
    for (int i = 0; i < clouds.Count; i++) {
      Cloud cloud = clouds[i];
      cloud.gameObject.transform.Translate(Vector2.left * cloud.speed * Time.deltaTime);

      if (cloud.gameObject.transform.position.x < -12) {
        DespawnCloud(cloud);
        Debug.Log("Destroyed cloud");
        SpawnCloud();
      }
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
