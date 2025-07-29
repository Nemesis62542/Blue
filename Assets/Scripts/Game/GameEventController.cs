using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Playables;
using Blue.UI;
using Blue.Input;
using Blue.Game;
using UnityEngine.Timeline;
using Blue.UI.Screen;
using Blue.Entity;
using Blue.Player;

public class GameEventController : MonoBehaviour
{
    [SerializeField] private List<GameEventData> eventList;
    [SerializeField] private PlayableDirector director;
    [SerializeField] private MecaSharkController shark;

    private GameEventData currentEvent;
    private int dialogueIndex = 0;
    private Dictionary<EventID, GameEventData> eventDict;
    
    public static GameEventController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

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

        PlayTimeline(eventData.TimelineAsset, eventData.IsMovie);
    }

    public void ShowNextDialogueLine()
    {
        if (currentEvent == null || dialogueIndex >= currentEvent.DialogueLines.Count)
        {
            ForwardIngame();
            EndEvent();
            return;
        }

        string message = currentEvent.DialogueLines[dialogueIndex].text;
        SubtitleUIController.Instance.ShowMessage(message);

        dialogueIndex++;
    }

    public void ForwardIngame()
    {
        PlayerController.Instance.ForwardIngame();
    }

    public void ForwardMovie()
    {
        PlayerController.Instance.ForwardMovie();
    }

    public void EmergencyMessage()
    {
        MessageView.Instance.ShowMessage(new MessageData("周囲に大型の動態反応を検知。危険度：<color=red>高</color>"), 5.0f);
    }

    public void BattleStart()
    {
        shark.StartBattle();
        MessageView.Instance.ShowMessage(new MessageData("ME-G4L0の敵対反応を検知。速やかな対応を推奨"), 8.0f);
    }

    public void FoundEMP()
    {
        MessageView.Instance.ShowMessage(new MessageData("付近にEMP装置を検知。ME-G4L0に対し有効"), 8.0f);
    }

    public void ForwardAquariumScene()
    {
        ForwardIngame();
        EndEvent();
        SceneLoader.LoadScene("Aquarium");
    }

    public void EndEvent()
    {
        currentEvent = null;
    }

    private void PlayTimeline(TimelineAsset timeline, bool is_movie)
    {
        if (is_movie) ForwardMovie();

        director.playableAsset = timeline;
        director.Play();
    }
}