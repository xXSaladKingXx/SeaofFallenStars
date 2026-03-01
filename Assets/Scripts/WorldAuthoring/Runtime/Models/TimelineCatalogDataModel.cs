using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// Global registry of all timeline events.  This catalog stores the unified chronology
    /// for the world and is intended to be the single source of truth for historical
    /// events.  Each event carries a date (year/month/day), descriptive metadata,
    /// significance and references to participants and locations.  Other data models
    /// refer to events by their unique identifier rather than embedding the full
    /// timeline entry.
    /// </summary>
    [Serializable]
    public sealed class TimelineCatalogDataModel
    {
        /// <summary>
        /// Unique identifier for this catalog.  This value is written into JSON files
        /// so authoring tools know which catalog is being edited.
        /// </summary>
        [JsonProperty("catalogId")]
        public string catalogId = "timeline_catalog";

        /// <summary>
        /// Human‑readable name for the catalog.  Appears in the authoring UI.
        /// </summary>
        [JsonProperty("displayName")]
        public string displayName = "Timeline Catalog";

        /// <summary>
        /// Optional notes about this catalog.  Authors can use this for freeform
        /// description or guidelines.  Stored as a text area in the inspector.
        /// </summary>
        [JsonProperty("notes")]
        [TextArea(2, 10)]
        public string notes;

        /// <summary>
        /// List of events defined in this catalog.  Each event has a unique id
        /// and associated metadata.  Events are sorted by date when displayed in
        /// the authoring UI but stored unsorted here.
        /// </summary>
        [JsonProperty("events")]
        public List<TimelineEventModel> events = new List<TimelineEventModel>();

        /// <summary>
        /// Backwards‑compatible alias for the event list.  Some editor scripts in the
        /// upstream project expect the timeline catalog to expose an <c>entries</c>
        /// collection rather than <c>events</c>.  This property simply forwards
        /// to <see cref="events"/> so both names refer to the same list.  It is
        /// marked with <see cref="JsonIgnoreAttribute"/> to avoid duplicating
        /// the data when serialising to JSON.
        /// </summary>
        [JsonIgnore]
        public List<TimelineEventModel> entries
        {
            get => events;
            set => events = value;
        }

        /// <summary>
        /// Collection of user‑defined event type templates.  Each custom type
        /// captures the participant categories and directionality used when
        /// saving an "Other" event as a new type.  Authors can select these
        /// custom types in the authoring UI for future events.  If this list
        /// is empty, no custom types have been defined.
        /// </summary>
        [JsonProperty("customEventTypes")]
        public List<CustomEventTypeDefinition> customEventTypes = new List<CustomEventTypeDefinition>();
    }

    /// <summary>
    /// Represents a single event in the timeline catalog.  This structure holds
    /// the date, title, significance, optional description, setting and
    /// participants of the event.  It also specifies the event type which can
    /// influence how participants are interpreted (e.g. directional or bilateral).
    /// </summary>
    [Serializable]
    public sealed class TimelineEventModel
    {
        /// <summary>
        /// Unique identifier for this event.  Other data models reference events
        /// via this id rather than embedding the full event.
        /// </summary>
        [JsonProperty("id")]
        public string id;

        /// <summary>
        /// Name or short title of the event.
        /// </summary>
        [JsonProperty("eventName")]
        public string eventName;

        /// <summary>
        /// Relative significance of the event.  Major events are highlighted in
        /// history views; minor and info events provide supplementary detail.
        /// </summary>
        [JsonProperty("eventSignificance")]
        public EventSignificance eventSignificance = EventSignificance.Info;

        /// <summary>
        /// Optional longer description of the event.  This can include narrative
        /// details, outcomes or context.
        /// </summary>
        [JsonProperty("description")]
        [TextArea(2, 10)]
        public string description;

        /// <summary>
        /// Year component of the event date.  Must be specified.  Month and day
        /// are optional and default to the first month or day when omitted.
        /// </summary>
        [JsonProperty("year")]
        public int year;

        /// <summary>
        /// Optional month component of the event date (1–12).  Null indicates
        /// that only the year is specified; defaults to January when computing
        /// chronological order.
        /// </summary>
        [JsonProperty("month")]
        public int? month;

        /// <summary>
        /// Optional day component of the event date (1–31).  Null indicates that
        /// only the year (and possibly month) is specified; defaults to the first
        /// of the month when computing chronological order.
        /// </summary>
        [JsonProperty("day")]
        public int? day;

        /// <summary>
        /// Identifier of the setting where the event occurred.  This should be the
        /// id of a settlement or unpopulated area.  Leave null for events that
        /// occur in no specific place.
        /// </summary>
        [JsonProperty("settingId")]
        public string settingId;

        /// <summary>
        /// List of participant identifiers involved in this event.  Participants
        /// can be settlements, characters, armies, travel groups or other
        /// entities.  Directional semantics (e.g. A → B vs A ↔ B) are implied
        /// by the chosen event type and participant ordering.
        /// </summary>
        [JsonProperty("participantIds")]
        public List<string> participantIds = new List<string>();

        /// <summary>
        /// When <see cref="eventType"/> is <see cref="EventType.Custom"/>, this field
        /// stores the name of the custom event type.  For built‑in event types
        /// this value should be null.  Custom types are defined in the
        /// <see cref="TimelineCatalogDataModel.customEventTypes"/> list.
        /// </summary>
        [JsonProperty("customTypeName")]
        public string customTypeName;

        /// <summary>
        /// The type of event.  Determines default iconography and participant
        /// semantics.  See <see cref="EventType"/> for supported values.
        /// </summary>
        [JsonProperty("eventType")]
        public EventType eventType = EventType.Other;

        /// <summary>
        /// Optional custom icon resource name (without extension) for this event.
        /// If null or empty the default icon for the event type is used.
        /// </summary>
        [JsonProperty("icon")]
        public string icon;

        /// <summary>
        /// Backwards‑compatible alias for the event identifier.  In older
        /// editor scripts the timeline event id is exposed via an <c>eventId</c>
        /// property.  This getter/setter forwards to <see cref="id"/> so that
        /// existing code can continue to compile without modification.  The
        /// property is ignored during JSON serialization to avoid
        /// duplicating the identifier.
        /// </summary>
        [JsonIgnore]
        public string eventId
        {
            get => id;
            set => id = value;
        }
    }

    /// <summary>
    /// Enumeration describing the perceived importance of an event.  Used for
    /// filtering and visual emphasis in timeline UIs.
    /// </summary>
    public enum EventSignificance
    {
        Major,
        Minor,
        Info
    }

    /// <summary>
    /// Enumeration of supported event types.  Certain types imply directional
    /// relationships between participants (e.g. parent → child for Birth).  The
    /// "Other" type allows arbitrary events not covered by the predefined set.
    /// </summary>
    public enum EventType
    {
        Birth,
        Death,
        Coronation,
        DeclareWar,
        Betrothal,
        Marriage,
        Kill,
        Coitus,
        MakeAlliance,
        MakePeace,
        Battle,
        BeganTraveling,
        Visited,
        EndTraveling,
        StrippedOfTitle,
        Knighted,
        AwardedLand,
        LevelUp,
        Befriend,
        StartRivalry,
        /// <summary>
        /// Denotes an undefined event that does not correspond to any specific
        /// built‑in type.  When using this value, authors may choose to save
        /// the event as a new custom type via the authoring UI.  In that case
        /// the eventType will be updated to <see cref="Custom"/> and
        /// <see cref="TimelineEventModel.customTypeName"/> will hold the
        /// user‑defined type name.
        /// </summary>
        Other,
        /// <summary>
        /// Represents a user‑defined event type.  Custom types are stored in
        /// <see cref="TimelineCatalogDataModel.customEventTypes"/> and
        /// referenced by name via <see cref="TimelineEventModel.customTypeName"/>.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Represents a user‑defined event type template.  Custom types are
    /// created when an "Other" event is saved as a new type.  Each
    /// definition records the name, participant categories and
    /// directionality so that subsequent events of this type can be
    /// initialized consistently.  Icons are optional and can be set
    /// independently per event.
    /// </summary>
    [Serializable]
    public class CustomEventTypeDefinition
    {
        /// <summary>
        /// Name of the custom event type, e.g. "Seduce".  This value is
        /// displayed in the authoring UI and used when assigning the
        /// <see cref="TimelineEventModel.customTypeName"/>.
        /// </summary>
        [JsonProperty("name")]
        public string name;

        /// <summary>
        /// Ordered list describing the expected category of each participant.
        /// For a two‑character event (initiator → target), this would
        /// contain two entries of <see cref="ParticipantCategory.Character"/>.
        /// When the event allows variable numbers of participants, this list
        /// may define a minimum; additional participants can be added on
        /// demand.
        /// </summary>
        [JsonProperty("participantCategories")]
        public List<ParticipantCategory> participantCategories = new List<ParticipantCategory>();

        /// <summary>
        /// Directionality of the relationships between participants.  This
        /// indicates whether the first participant acts upon the second
        /// (<see cref="EventDirection.OneWay"/>), both act mutually
        /// (<see cref="EventDirection.TwoWay"/>), or directionality is not
        /// significant (<see cref="EventDirection.None"/>).
        /// </summary>
        [JsonProperty("directionality")]
        public EventDirection directionality = EventDirection.None;

        /// <summary>
        /// Optional default icon for events of this type.  If set, events
        /// lacking an explicit icon will fall back to this value.  Should
        /// correspond to an icon sprite name in resources.
        /// </summary>
        [JsonProperty("icon")]
        public string icon;
    }

    /// <summary>
    /// Enumeration of participant categories.  Each entry describes
    /// which kind of world entity a participant ID refers to when
    /// defining a custom event type.
    /// </summary>
    public enum ParticipantCategory
    {
        Settlement,
        Unpopulated,
        Character,
        Army,
        TravelGroup
    }

    /// <summary>
    /// Specifies how the participants in a custom event relate to each
    /// other.  Use <see cref="OneWay"/> when the first participant acts
    /// upon the second (e.g. assassin → victim), <see cref="TwoWay"/>
    /// when the relationship is bilateral (e.g. betrothal), and
    /// <see cref="None"/> when directionality is not applicable or
    /// multiple participants interact in ways not captured by a simple
    /// arrow.
    /// </summary>
    public enum EventDirection
    {
        None,
        OneWay,
        TwoWay
    }
}