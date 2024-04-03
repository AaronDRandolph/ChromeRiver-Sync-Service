using ChromeRiverService.Classes.DTOs;
using ChromeRiverService.Classes.DTOs.Subclasses;
using System.Reflection;
using System.Text;

namespace ChromeRiverService.Classes.Helpers
{
    public class NullChecker
    {
        private static ICollection<string> nulls = [];

        public static string GetNullPropertiesLog<T>(T obj, string uid) where T : class
        {
            StringBuilder log = new($"Type: {typeof(T)} with uid {uid} not sent due to null attributes => ");
            AddNulls(obj);
            string logString = nulls.Count > 0 ? log.Append(string.Join(", ", nulls)).ToString() : string.Empty;
            nulls.Clear();

            return logString;
        }

        private static void AddNulls(object obj, string? parentAtribute = null)
        {
            foreach (PropertyInfo prop in obj.GetType().GetProperties())
            {
                string propName = prop.Name;

                if (prop.GetValue(obj) is null)
                {
                    nulls.Add(parentAtribute is not null ? $"{parentAtribute} : {prop.Name}" : prop.Name);
                }

                // handle list of subclasses
                if ((prop.ReflectedType?.Name.Equals(nameof(EntityDto)) ?? false) && propName == "EntityNames")
                {
                    ICollection<EntityName>? entityNameObjs = (ICollection<EntityName>?)prop.GetValue(obj);
                    if (entityNameObjs is not null)
                    {
                        foreach (EntityName entityNameObj in entityNameObjs)
                        {
                            AddNulls(entityNameObj, propName);
                        }
                    }
                }

                if ((prop.ReflectedType?.Name.Equals(nameof(PersonDto)) ?? false) && propName == "PersonEntities")
                {
                    ICollection<PersonEntity>? personEntityObjects = (ICollection<PersonEntity>?)prop.GetValue(obj);
                    if (personEntityObjects is not null)
                    {
                        foreach (PersonEntity personEntityObject in personEntityObjects)
                        {
                            AddNulls(personEntityObject, propName);
                        }
                    }
                }
            }
        }

    }

}


