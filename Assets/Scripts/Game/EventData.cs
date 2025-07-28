using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace Blue.Game
{
    [CreateAssetMenu(menuName = "Blue/ScriptableObject/EventData")]
    public class GameEventData : ScriptableObject
    {
        [SerializeField] private EventID id;
        [SerializeField] private List<DialogueLine> dialogueLines;
        [SerializeField] private TimelineAsset timelineAsset;
        [SerializeField] private bool isMovie;

        public EventID ID => id;
        public List<DialogueLine> DialogueLines => dialogueLines;
        public TimelineAsset TimelineAsset => timelineAsset;
        public bool IsMovie => isMovie;
    }

    [System.Serializable]
    public enum EventID
    {
        None,
        IntroCutscene,
        GasCylinder,
        CaptureGun,
        IntoTheCave,
        FallInTheHole,
        BattleStart,
    }

    [System.Serializable]
    public class DialogueLine
    {
        [TextArea]
        public string text;
    }

}
