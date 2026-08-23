using System.Runtime.CompilerServices;

// The presentation types are internal: nothing outside this assembly should reach
// into them. Two assemblies are excepted — the Editor tests, so layout maths and
// mesh construction can be checked, and the Editor tools, which assemble scenes and
// render previews from the command line. Both are development-only, so neither
// widens the surface the shipped game exposes.
[assembly: InternalsVisibleTo("Pathweaver.Game.EditorTests")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]
