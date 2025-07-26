using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Playables;
using Blue.UI;
using Blue.Input;
using Blue.Game;
using UnityEngine.Timeline;
using System.Collections;
using Blue.UI.Screen;

public class GameEventController : MonoBehaviour
{
    [SerializeField] private List<GameEventData> eventList;
    [SerializeField] private PlayableDirector director;

    private GameEventData currentEvent;
    private int dialogueIndex = 0;
    private Dictionary<EventID, GameEventData> eventDict;

    private void Awake()
    {
        eventDict = new Dictionary<EventID, GameEventData>();
        foreach (GameEventData data in eventList)
        {
            if (!eventDict.ContainsKey(data.ID))
            {
                eventDict.Add(data.ID, data);
            }
        }
    }

    public void TriggerEvent(EventID id)
    {
        if (!eventDict.TryGetValue(id, out GameEventData eventData)) return;

        currentEvent = eventData;
        dialogueIndex = 0;

        ShowNextDialogueLine();
    }

    public void ShowNextDialogueLine()
    {
        if (currentEvent == null || dialogueIndex >= currentEvent.DialogueLines.Count)
        {
            EndEvent();
            return;
        }

        string message = currentEvent.DialogueLines[dialogueIndex].text;
        SubtitleUIController.Instance.ShowMessage(message);

        dialogueIndex++;
    }

    private void EndEvent()
    {
        currentEvent = null;
        UIController.Instance.ShowScreen(ScreenState.Ingame);
        PlayerInputHandler.Instance.SetInputMap(InputMapType.Player);
    }

    private void PlayTimeline(TimelineAsset timeline)
    {
        UIController.Instance.ShowScreen(ScreenState.Movie);
        PlayerInputHandler.Instance.SetInputMap(InputMapType.Movie);

        director.playableAsset = timeline;
        director.Play();
    }
}