using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DarkSaver.Original.World;

static class NullableContractChecks
{
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static void Main()
    {
        var types=typeof(WorldRecord).Assembly.GetTypes().Where(t=>t.GetCustomAttribute<DataContractAttribute>()!=null).OrderBy(t=>t.FullName).ToArray();
        var cases=new List<string>();var fields=new List<string>();
        foreach(var type in types)
        foreach(var field in type.GetFields().Where(f=>Nullable.GetUnderlyingType(f.FieldType)!=null&&f.GetCustomAttribute<DataMemberAttribute>()!=null).OrderBy(f=>f.Name))
        {
            string name=type.FullName+"."+field.Name;fields.Add(name);
#if !BASELINE
            Check(field.GetCustomAttribute<NonSerializedAttribute>()!=null,"Unity must not serialize nullable DataContract field: "+name);
#endif
            Type valueType=Nullable.GetUnderlyingType(field.FieldType);
            object[] values=valueType==typeof(bool)?new object[]{null,false,true}:valueType==typeof(uint)?new object[]{null,0u,17u,uint.MaxValue}:valueType==typeof(ulong)?new object[]{null,0ul,17ul,ulong.MaxValue}:valueType==typeof(int)?new object[]{null,0,17,-1,int.MinValue,int.MaxValue}:throw new Exception("Add explicit nullable cases: "+valueType);
            foreach(object value in values)
            {
                object original=Activator.CreateInstance(type);field.SetValue(original,value);
                var serializer=new DataContractJsonSerializer(type);
                using(var stream=new MemoryStream())
                {
                    serializer.WriteObject(stream,original);string json=Encoding.UTF8.GetString(stream.ToArray());stream.Position=0;
                    object loaded=serializer.ReadObject(stream);Check(Equals(field.GetValue(loaded),value),"DCS loses nullable value: "+name+" "+value);
                    using(var parsed=JsonDocument.Parse(json))
                    {
                        bool present=parsed.RootElement.TryGetProperty(field.GetCustomAttribute<DataMemberAttribute>().Name??field.Name,out _);
                        Check(present==(value!=null),"Omitted unknown differs from explicit zero/false: "+name);
                    }
                    cases.Add(name+"\n"+json);
                }
            }
        }
        Check(fields.Count>30,"Production nullable contract coverage unexpectedly missing");
        string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n",cases)))).Replace("-","").ToLowerInvariant();
        Console.WriteLine(JsonSerializer.Serialize(new{assertions=checks,contractTypes=types.Length,nullableFields=fields.Count,roundtripCases=cases.Count,serializedCasesSha256=hash,fields,unityPlay=false,scope="Actual production DataContract nullable fields: unknown/null, explicit zero/false, nonzero and signed/unsigned extrema. Isolated serializer values are schema tests, not approved gameplay records. Baseline variant removes only NonSerialized; emitted save JSON must be byte-identical."}));
    }
}
