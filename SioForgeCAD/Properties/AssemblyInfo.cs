﻿using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

// Les informations générales relatives à un assembly dépendent de 
// l'ensemble d'attributs suivant. Changez les valeurs de ces attributs pour modifier les informations
// associées à un assembly.
[assembly: AssemblyTitle("SioForgeCAD")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("HOFFMANN François")]
[assembly: AssemblyProduct("SioForgeCAD")]
[assembly: AssemblyCopyright("Copyright © HOFFMANN François")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// L'affectation de la valeur false à ComVisible rend les types invisibles dans cet assembly 
// aux composants COM.  Si vous devez accéder à un type dans cet assembly à partir de 
// COM, affectez la valeur true à l'attribut ComVisible sur ce type.
[assembly: ComVisible(false)]

// Le GUID suivant est pour l'ID de la typelib si ce projet est exposé à COM
[assembly: Guid("2037e9c5-32f1-43d5-8433-45124dd5bee7")]

// Les informations de version pour un assembly se composent des quatre valeurs suivantes :
//
//      Version principale
//      Version secondaire 
//      Numéro de build
//      Révision
//
// Vous pouvez spécifier toutes les valeurs ou indiquer les numéros de build et de révision par défaut 
// en utilisant '*', comme indiqué ci-dessous :
// [assembly: AssemblyVersion("1.0.*")]

//Hot Reload and Edit & Continue are not compatible with using * in AssemblyVersionAttribute value.
//Presence of * in the version means that the compiler generates a new version every build based on the current time.
//The build then produces non-reproducible, non-deterministic outputs
#if DEBUG
[assembly: AssemblyVersion("1.0.0.0")]
#else
[assembly: AssemblyVersion("1.0.*")]
#endif
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: NeutralResourcesLanguage("")]
