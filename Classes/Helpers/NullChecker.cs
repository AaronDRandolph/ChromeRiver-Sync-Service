using ChromeRiverService.Classes.DTOs;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ChromeRiverService.Classes.HelperClasses
{
    public class NullChecker
    {
        private static List<string> nulls = [];

         public static string GetNullPropertiesLog<T>(T obj, string uid) where T : class
        {
            StringBuilder log = new($"Type: {typeof(T)} with uid {uid} not sent due to null attributes => ");
            AddNulls(obj);
            string logString = nulls.Count > 0 ? log.Append(string.Join(", ", nulls)).ToString() : string.Empty;
            nulls.Clear();

            return logString;
        }

        private static void AddNulls(Object obj, string? parentAtribute = null)
        {
            foreach (PropertyInfo prop in obj.GetType().GetProperties())
            {
                string propName = prop.Name;

                if (prop.GetValue(obj) is null)
                {
                    nulls.Add( parentAtribute is not null ? $"{parentAtribute} : {prop.Name}" : prop.Name);
                }

                // handle list of subclasses
                if ((prop.ReflectedType?.Name.Equals(nameof(EntityDto)) ?? false) && propName == "EntityNames")
                {
                    List<EntityDto.EntityName>? entityNameObjs = (List<EntityDto.EntityName>?)prop.GetValue(obj);
                    if (entityNameObjs is not null)
                    {
                        foreach(EntityDto.EntityName entityNameObj in entityNameObjs)
                        {
                            AddNulls(entityNameObj,propName);
                        }
                    }
                }

                if ((prop.ReflectedType?.Name.Equals(nameof(PersonDto)) ?? false) && propName == "PersonEntities")
                {
                    List<PersonDto.Entities>? personEntityObjects = (List<PersonDto.Entities>?)prop.GetValue(obj);
                    if (personEntityObjects is not null)
                    {
                        foreach (PersonDto.Entities personEntityObject in personEntityObjects)
                        {
                            AddNulls(personEntityObject,propName);
                        }
                    }
                }
            }
        }
            
    }

}

