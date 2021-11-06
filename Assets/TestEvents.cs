using System;
using UnityEngine;

public class TestEvents : MonoBehaviour {

    [Header("Количество событий в сек")]
    public int eventsToSec = 3;

    [Header("Тип события")]
    public string eventType = "levelStart";

    [Header("Диапазон нумерации")]
    public int genericNumber = 100;


    private float totalTime = 0;
    private int levelNumber = 1;


    void Update() {
        if (eventsToSec != 0) {
            totalTime += Time.deltaTime;
            eventsToSec = Math.Min(10000, Math.Abs(eventsToSec));
            float eventStepTime = 1.0f / Math.Min(1000, eventsToSec);

            while (totalTime >= eventStepTime) {
                totalTime -= eventStepTime;

                EventService.Instance.TrackEvent(eventType, $"level:{levelNumber}");

                levelNumber++;
                if (levelNumber > genericNumber) {
                    levelNumber = 1;
                }
            }
        }
    }
}
