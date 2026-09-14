using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

public class TitleSceneController : MonoBehaviour
{
  public GameObject cloudPrefab;
  public GameObject sunPrefab;
  public GameObject sunraysPrefab;
  public GameObject logoPrefab;
  public float themeSongBpm = 100f;
  [Tooltip("Seconds after the scene loads before the theme song starts; the animations follow the music, so they wait with it")]
  public float musicStartDelay = 0.8f;
  [Tooltip("Slides the animations against the music, in seconds (positive = animations later)")]
  public float animationOffset = 0f;

  [Header("Beat Timing (beats of the theme song)")]
  public float sunBeat = 2f;
  [Tooltip("Beats the sun takes to rise; the rays pop in as it lands")]
  public float sunRiseBeats = 4f;
  public float logoBeat = 8f;
  [Tooltip("How long a pop-in lasts, in beats")]
  public float popBeats = 0.5f;

  [Header("Beat Motion")]
  [Tooltip("The logo rocks to this angle and back, reaching each side on a beat")]
  public float logoWobbleDegrees = 5f;
  public float raysDegreesPerBeat = 3f;
  [Tooltip("Scale bump on each beat once the logo and rays are in (1.05 = 5% larger)")]
  public float logoPulseScale = 1.05f;
  public float raysPulseScale = 1.04f;
  [Tooltip("Length of the whole ray pulse, out and back, in seconds; keep it under a beat or pulses start skipping beats")]
  public float raysPulseDuration = 0.3f;

  [Header("Scene Transition")]
  public SceneTransitionManager transitionManager;

  [Header("Attract Mode")]
  private float idleTimer = 0f;
  private bool attractModeTriggered = false;

  private const float SunStartY = -10f;
  private const float SunEndY = 1.2f;
  private const float SunScale = 0.5f;
  private const float RaysScale = 0.5f;
  private const float LogoScale = 0.4f;

  private Sun sun;
  private GameObject logoObject;
  private float logoSpawnTime;
  private bool logoPulsing = false;
  private AudioSource themeSong;
  private BeatManager beatManager;
  private float secPerBeat;

  // public TMP_Text highScoreText;
  // private int highScore;

  public struct Cloud
  {
    public GameObject gameObject;
    public float speed;
    public float scale;
    public float spawnTime;

    public Cloud(GameObject _object, float _speed, float _scale, float _spawnTime)
    {
      gameObject = _object;
      speed = _speed;
      scale = _scale;
      spawnTime = _spawnTime;
    }
  }

  private List<Cloud> clouds = new List<Cloud>();

  // The opening clouds, each popping in on its beat
  private struct IntroCloud
  {
    public float beat;
    public float x;
    public float y;
    public float scale;

    public IntroCloud(float _beat, float _x, float _y, float _scale)
    {
      beat = _beat;
      x = _x;
      y = _y;
      scale = _scale;
    }
  }

  private readonly IntroCloud[] introClouds =
  {
    new IntroCloud(0f, 7.37f, 3.19f, 0.48f),
    new IntroCloud(2f, 8.87f, -0.07f, 0.33f),
    new IntroCloud(4f, -6.12f, 4f, 0.13f),
    new IntroCloud(6f, -6.9843f, 0.5f, 0.388f),
  };
  private int nextIntroCloud = 0;

  void SpawnCloud(float x = 15f)
  {
    float y = Random.Range(0.2f, 0.45f);
    float scale = Random.Range(0.25f, 0.5f);
    SpawnCloud(x, y, scale);
  }

  void SpawnCloud(float x, float y, float scale)
  {
    Vector3 startVector = new Vector3(x, y, 0f);
    GameObject newCloud = Instantiate(cloudPrefab, startVector, Quaternion.identity) as GameObject;
    newCloud.transform.localScale = new Vector3(0, 0, 1);

    if (scale < 0.35f)
    {
      SpriteRenderer cloudRenderer = newCloud.GetComponent<SpriteRenderer>();
      cloudRenderer.sortingLayerName = "Cloud";
      cloudRenderer.color = new Color(237, 237, 237);
    }
    clouds.Add(new Cloud(newCloud, 0.5f - (scale * scale), scale, Time.time));
  }

  void DespawnCloud(Cloud cloud)
  {
    Destroy(cloud.gameObject, 2f);
    clouds.Remove(cloud);
  }

  void UpdateClouds()
  {
    for (int i = 0; i < clouds.Count; i++)
    {
      Cloud cloud = clouds[i];
      float scale = cloud.scale * PopProgress(cloud.spawnTime);
      cloud.gameObject.transform.localScale = new Vector3(scale, scale, 1);
      cloud.gameObject.transform.Translate(Vector2.left * cloud.speed * Time.deltaTime);

      if (cloud.gameObject.transform.position.x < -12)
      {
        DespawnCloud(cloud);
        SpawnCloud();
      }
    }
  }

  /*****************************
   ***** ALL THINGS SUN ******
   *****************************/
  private struct Sun
  {
    public GameObject sunObject;
    public GameObject raysObject;
    public float raysPopTime; // -1 until the sun lands
    public bool raysPulsing;

    public Sun(GameObject _sun, GameObject _rays)
    {
      sunObject = _sun;
      raysObject = _rays;
      raysPopTime = -1f;
      raysPulsing = false;
    }
  }

  void SpawnSun()
  {
    GameObject sunObject = Instantiate(sunPrefab, new Vector3(0, SunStartY, 0), Quaternion.identity) as GameObject;
    SpriteRenderer sunRenderer = sunObject.GetComponent<SpriteRenderer>();
    sunRenderer.sortingOrder = 0;
    sunObject.transform.localScale = new Vector3(SunScale, SunScale, 1);

    GameObject raysObject = Instantiate(sunraysPrefab, new Vector3(0, SunEndY, 0), Quaternion.identity) as GameObject;
    raysObject.transform.localScale = new Vector3(0, 0, 1);

    sun = new Sun(sunObject, raysObject);
  }

  void UpdateSun(float beat)
  {
    if (sun.sunObject == null) return;

    // Rise over a set number of beats so the sun lands on one
    float rise = sunRiseBeats <= 0f ? 1f : Mathf.Clamp01((beat - sunBeat) / sunRiseBeats);
    Vector3 position = sun.sunObject.transform.position;
    position.y = Mathf.Lerp(SunStartY, SunEndY, EaseOutCubic(rise));
    sun.sunObject.transform.position = position;

    if (rise < 1f) return;
    if (sun.raysPopTime < 0f) sun.raysPopTime = Time.time;

    Transform rays = sun.raysObject.transform;
    rays.rotation = Quaternion.Euler(0, 0, (beat - sunBeat - sunRiseBeats) * raysDegreesPerBeat);

    if (sun.raysPulsing) return;
    float scale = RaysScale * PopProgress(sun.raysPopTime);
    rays.localScale = new Vector3(scale, scale, 1);
    if (PopDone(sun.raysPopTime))
    {
      AddBeatPulse(sun.raysObject, raysPulseScale).pulseDuration = raysPulseDuration;
      sun.raysPulsing = true;
    }
  }

  /*****************************
          LOGO METHODS
   *****************************/
  void SpawnLogo()
  {
    logoObject = Instantiate(logoPrefab, new Vector3(0, 0.9f, 0), Quaternion.identity) as GameObject;
    SpriteRenderer logoRenderer = logoObject.GetComponent<SpriteRenderer>();
    logoRenderer.sortingOrder = 10;
    logoObject.transform.localScale = new Vector3(0f, 0f, 1);
    logoSpawnTime = Time.time;
  }

  void UpdateLogo(float beat)
  {
    if (logoObject == null) return;

    // Rock side to side, reaching each end right on a beat
    logoObject.transform.rotation = Quaternion.Euler(0, 0, -logoWobbleDegrees * Mathf.Cos(Mathf.PI * beat));

    if (logoPulsing) return;
    float scale = LogoScale * PopProgress(logoSpawnTime);
    logoObject.transform.localScale = new Vector3(scale, scale, 1);
    if (PopDone(logoSpawnTime))
    {
      AddBeatPulse(logoObject, logoPulseScale);
      logoPulsing = true;
    }
  }

  /*****************************
          BEAT HELPERS
   *****************************/
  // Hand a popped-in object to the shared beat pulse; waiting until the pop settles means the pulse starts from full size
  BeatPulseReceiver AddBeatPulse(GameObject target, float pulseScale)
  {
    BeatPulseReceiver pulse = target.AddComponent<BeatPulseReceiver>();
    pulse.baseScale = pulseScale;
    pulse.intensityBonusScale = 0f;
    return pulse;
  }

  // 0 -> 1 across a pop-in, overshooting a little before settling
  float PopProgress(float startTime)
  {
    float duration = popBeats * secPerBeat;
    float t = duration <= 0f ? 1f : Mathf.Clamp01((Time.time - startTime) / duration);
    return EaseOutBack(t);
  }

  bool PopDone(float startTime) => Time.time - startTime >= popBeats * secPerBeat;

  private static float EaseOutBack(float t)
  {
    const float c1 = 1.70158f;
    const float c3 = c1 + 1f;
    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
  }

  private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

  void Start()
  {
    themeSong = GetComponent<AudioSource>();
    themeSong.Stop();
    secPerBeat = 60f / themeSongBpm;

    // There's no GameManager here, so the shared BeatManager follows the theme song instead
    beatManager = GetComponent<BeatManager>();
    if (beatManager == null)
    {
      beatManager = gameObject.AddComponent<BeatManager>();
    }
    beatManager.musicSource = themeSong;
    beatManager.bpm = themeSongBpm;
    beatManager.firstBeatOffset = animationOffset;

    // The beat clock waits for the music, so delaying it delays the whole intro
    Invoke("PlayThemeSong", musicStartDelay);

    // Reset attract mode state when returning to title
    idleTimer = 0f;
    attractModeTriggered = false;

    // highScore = PlayerPrefs.GetInt("HighScore", 0);
    // highScoreText.text = highScore.ToString();
  }

  // Update is called once per frame
  void Update()
  {
    float beat = beatManager.SongPositionInBeats;

    // Everything arrives on its beat, once the music has started
    if (beatManager.HasStarted)
    {
      while (nextIntroCloud < introClouds.Length && beat >= introClouds[nextIntroCloud].beat)
      {
        IntroCloud introCloud = introClouds[nextIntroCloud++];
        SpawnCloud(introCloud.x, introCloud.y, introCloud.scale);
      }
      if (sun.sunObject == null && beat >= sunBeat) SpawnSun();
      if (logoObject == null && beat >= logoBeat) SpawnLogo();
    }

    UpdateClouds();
    UpdateSun(beat);
    UpdateLogo(beat);

    // Check for any key press - new input system or legacy fallback
    bool keyPressed = false;
    if (InputManager.Instance != null)
    {
      // Gameplay keys (W/Q/A/S, gamepad teat buttons) or Submit (Enter/Space, gamepad A)
      keyPressed = InputManager.Instance.AnyGameplayKeyPressed() ||
                   InputManager.Instance.Submit.WasPressedThisFrame();
    }
    else
    {
      keyPressed = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Q) ||
                   Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.A) ||
                   Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
    }

    // Check touch/mouse input but ignore if over UI elements (like volume slider)
    bool touchBegan = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
    bool mouseClicked = Input.GetMouseButtonDown(0);

    bool pointerOverUI = false;
    if (EventSystem.current != null)
    {
      if (touchBegan)
        pointerOverUI = EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
      else if (mouseClicked)
        pointerOverUI = EventSystem.current.IsPointerOverGameObject();
    }

    if (keyPressed || ((touchBegan || mouseClicked) && !pointerOverUI))
    {
      // Reset idle timer on any input
      idleTimer = 0f;

      if (!attractModeTriggered)
      {
        Invoke("LoadLevelSelect", 0.5f);
      }
    }
    else
    {
      // Increment idle timer when no input
      idleTimer += Time.deltaTime;

      // Trigger attract mode after timeout
      if (AttractModeManager.Instance != null &&
          idleTimer >= AttractModeManager.Instance.titleIdleTimeout &&
          !attractModeTriggered)
      {
        attractModeTriggered = true;
        AttractModeManager.Instance.StartAttractMode();
      }
    }
  }

  void PlayThemeSong()
  {
    themeSong.Play();
  }

  void LoadLevelSelect()
  {
    if (transitionManager != null)
    {
      transitionManager.TransitionToScene("LevelSelect");
    }
    else
    {
      SceneManager.LoadScene("LevelSelect");
    }
  }
}
