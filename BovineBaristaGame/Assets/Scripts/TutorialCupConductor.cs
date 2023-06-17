using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialCupConductor : MonoBehaviour
{
    public GameObject cupPrefab;

    public static int BPM = 110;

    public static float SecPerBeat = 60f / 110;

    public static float TimeToSlideInSeconds = 0.5f;

    public static Dictionary<string, Vector3> CupTagEndVector = new Dictionary<string, Vector3>
    {
        {CupTag.FrontLeft, new Vector3(-1.7798f, -2.6906f, 0f)},
        {CupTag.FrontRight, new Vector3(2.9357f, -2.6906f, 0f)},
        {CupTag.BackLeft, new Vector3(-3.5646f, -1.27f, 0f)},
        {CupTag.BackRight, new Vector3(1.13f, -1.27f, 0f)}
    };

    private Vector2 edgeVector;

    void Start()
    {
        edgeVector = Camera.main.ViewportToWorldPoint(new Vector2(0, 0));
    }

    public void Conduct(float songPositionInBeats)
    {
        if (songPositionInBeats % 1 == 0) {
            // beat 1, back right
            // beat 2, back left
            // beat 3, front right
            // beat 4, front left
            
            float beatInMeasure = (songPositionInBeats % 4) + 1;

            string type;

            switch(beatInMeasure) {
                case 1:
                    type = CupTag.BackRight;
                    break;
                case 2:
                    type = CupTag.BackLeft;
                    break;
                case 3:
                    type = CupTag.FrontRight;
                    break;
                case 4:
                    type = CupTag.FrontLeft;                
                    break;
                default:
                    Debug.Log("Got a beat that isn't in 4/4");
                    return;
            }

            Vector3 endVector = CupTagEndVector[type];
            Vector3 startVector = new Vector3(edgeVector.x - 15, endVector.y, 0f);
        }
}
}