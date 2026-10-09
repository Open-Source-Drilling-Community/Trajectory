using System.Reflection;
using OSDC.Drilling.Trajectory.Service.Managers;

namespace ServiceTest;

internal static class ServiceTestHost
{
    internal static void ResetManagerSingletons()
    {
        foreach (Type type in typeof(TrajectoryManager).Assembly.GetTypes()
                     .Where(type => type.Namespace == typeof(TrajectoryManager).Namespace))
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                         .Where(field => !field.IsInitOnly && !field.IsLiteral && field.FieldType == type))
            {
                field.SetValue(null, null);
            }
        }
    }
}
