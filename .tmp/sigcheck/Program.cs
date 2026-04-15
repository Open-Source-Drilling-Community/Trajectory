using System;
using System.Linq;
using System.Reflection;
using OSDC.DotnetLibraries.Drilling.Surveying;

Console.WriteLine("SurveyPoint props:");
foreach (var p in typeof(SurveyPoint).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.Name.Contains("Annot") || p.Name is "MD" or "Abscissa"))
    Console.WriteLine($"  {p.PropertyType} {p.Name}");
Console.WriteLine("SurveyStation props:");
foreach (var p in typeof(SurveyStation).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.Name.Contains("Annot") || p.Name is "MD" or "Abscissa"))
    Console.WriteLine($"  {p.PropertyType} {p.Name}");
