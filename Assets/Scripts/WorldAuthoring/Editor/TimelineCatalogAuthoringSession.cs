#if UNITY_EDITOR
using UnityEngine;

namespace Zana.WorldAuthoring
{
    /// <summary>
    /// A partial stub of <see cref="TimelineCatalogAuthoringSession"/> that exists only in
    /// the editor assembly.  The runtime implementation resides in the
    /// Runtime/Sessions folder and provides the full functionality.  Declaring
    /// this partial class in the editor folder avoids duplicate type
    /// definition errors that can occur when a copy of the timeline authoring
    /// session exists under Editor.  No additional members are defined here.
    /// </summary>
    public sealed partial class TimelineCatalogAuthoringSession
    {
        // Intentionally left blank.  All logic is defined in the runtime
        // partial class.
    }
}
#endif