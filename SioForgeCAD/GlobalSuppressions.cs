// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Usage", "CA2211:Les champs non constants ne doivent pas être visibles", Justification = "", Scope = "module")]
[assembly: SuppressMessage("Performance", "CA1861:Éviter les tableaux constants en tant qu’arguments", Justification = "", Scope = "module")]
[assembly: SuppressMessage("Roslynator", "RCS1246:Use element access", Justification = "", Scope = "module")]
[assembly: SuppressMessage("Roslynator", "RCS1033:Remove redundant boolean literal", Justification = "", Scope = "module")]
[assembly: SuppressMessage("Roslynator", "RCS1102:Make class static", Justification = "Needed for autocad to not be static", Scope = "type", Target = "~T:SioForgeCAD.Commands")]
[assembly: SuppressMessage("Style", "IDE0180:Utilisez le tuple pour échanger des valeurs", Justification = "", Scope = "module")]
