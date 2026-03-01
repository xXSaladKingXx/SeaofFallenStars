// Disable the editor stub for TimelineCatalogAuthoringSession.  The runtime
// implementation is compiled into the default Assembly-CSharp and is usable
// in the editor.  Defining a stub here leads to duplicate type definitions
// and Unity serialization errors.  By wrapping the stub in a false
// preprocessor directive, it is excluded from compilation, letting the
// runtime class take precedence.
#if false
// Editor stub disabled
using UnityEngine;
using Zana.WorldAuthoring;
namespace Zana.WorldAuthoring
{
    public sealed partial class TimelineCatalogAuthoringSession : WorldDataAuthoringSessionBase
    {
        public override WorldDataCategory Category => WorldDataCategory.TimelineCatalog;
        public override string GetDefaultFileBaseName() => "timeline_catalog";
        public override string BuildJson() => "{}";
        public override void ApplyJson(string json) { }
    }
}
#endif