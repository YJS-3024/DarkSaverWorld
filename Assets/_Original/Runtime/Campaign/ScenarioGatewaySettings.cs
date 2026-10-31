using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Campaign
{
    [Serializable, DataContract] public sealed class ScenarioGateway
    {
        [DataMember] public string id, fromHash, targetHash, name;
        [DataMember] public int fromMap, targetMap, npcJob, x, y, spawnX, spawnY;
        [DataMember] public bool enabled;
        public ScenarioGateway Copy() => (ScenarioGateway)MemberwiseClone();
    }
    [Serializable, DataContract] public sealed class ScenarioGatewaySettings
    {
        [DataMember] public int version = 1;
        [DataMember] public bool applyToGame;
        [DataMember] public string provenance = "user-authored; NPC interaction recalled by user; not recovered original server placements";
        [DataMember] public ScenarioGateway[] gates = Array.Empty<ScenarioGateway>();
        public ScenarioGatewaySettings Copy()
        {var copy=(ScenarioGatewaySettings)MemberwiseClone();copy.gates=gates.Select(g=>g.Copy()).ToArray();return copy;}
    }
    [Serializable] public sealed class GatewayMapInfo
    { public int id,width,height; public string key,title,kind,hash,nativePath; public bool legacy; }
    [Serializable] public sealed class GatewayMapCatalog { public GatewayMapInfo[] maps; public int[] npcJobs; }
    public static class ScenarioGatewayData
    {
        public const string Provenance = "user-authored; NPC interaction recalled by user; not recovered original server placements";
        public static string Hash(byte[] bytes)
        {using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        public static byte[] Encode(ScenarioGatewaySettings settings)
        {using(var s=new MemoryStream()){new DataContractJsonSerializer(typeof(ScenarioGatewaySettings)).WriteObject(s,settings);return s.ToArray();}}
        public static ScenarioGatewaySettings Decode(byte[] bytes)
        {using(var s=new MemoryStream(bytes))return (ScenarioGatewaySettings)new DataContractJsonSerializer(typeof(ScenarioGatewaySettings)).ReadObject(s);}
        public static void Validate(ScenarioGatewaySettings settings,GatewayMapCatalog catalog)
        {
            if(settings==null||settings.version!=1||settings.provenance!=Provenance||settings.gates==null||catalog?.maps==null||catalog.npcJobs==null)throw new InvalidDataException("입구 설정 형식/출처를 확인하세요.");
            var ids=new HashSet<string>();var targets=new HashSet<int>();var occupied=new HashSet<string>();
            foreach(var g in settings.gates)
            {
                var from=catalog.maps.FirstOrDefault(m=>m.id==g?.fromMap);var to=catalog.maps.FirstOrDefault(m=>m.id==g?.targetMap);
                if(g==null||!Guid.TryParseExact(g.id,"N",out _)||!ids.Add(g.id)||from==null||to==null||to.kind!="scenario"||(from.kind!="world"&&from.kind!="town")||from.id==to.id
                    ||g.fromHash!=from.hash||g.targetHash!=to.hash||!targets.Add(g.targetMap)||!catalog.npcJobs.Contains(g.npcJob)
                    ||g.x<0||g.y<0||g.x>=from.width||g.y>=from.height||g.spawnX<0||g.spawnY<0||g.spawnX>=to.width||g.spawnY>=to.height
                    ||!occupied.Add(g.fromMap+":"+g.x+":"+g.y)||string.IsNullOrWhiteSpace(g.name))throw new InvalidDataException("시나리오당 NPC 1명, 병종/좌표/맵 버전과 중복을 확인하세요.");
            }
        }
        public static void Apply(ScenarioGatewaySettings settings,GatewayMapCatalog catalog,WorldDefinition world)
        {
            Validate(settings,catalog);world.gateways=settings.Copy();
            // Clone maps before applying authoring; never mutate the cached native catalog.
            world.maps=world.maps.Select(m=>m.CopyForPlacement()).ToArray();
            foreach(var map in world.maps)map.npcs=map.npcs.Where(n=>string.IsNullOrEmpty(n.gateway)).ToArray();
            if(!settings.applyToGame)return;
            foreach(var g in settings.gates.Where(g=>g.enabled))
            {
                var from=world.Map(g.fromMap);var target=world.Map(g.targetMap);
                if(from==null||target==null||from.npcs.Any(n=>n.x==g.x&&n.y==g.y)||from.portals.Any(p=>p.cells.Any(c=>c.x==g.x&&c.y==g.y)))throw new InvalidDataException("NPC 위치가 기존 직원/출입구와 겹칩니다.");
                var tile=new DarkSaver.Original.Tutorial.TutorialTile(g.spawnX,g.spawnY);int cost=target.map.Terrain(tile,2);
                if(cost<=0||cost==99||cost==-99||world.campaign?.Map(g.targetMap)?.units.Any(u=>u.x==g.spawnX&&u.y==g.spawnY)==true)throw new InvalidDataException("도착 칸이 막혀 있거나 원본 몬스터와 겹칩니다.");
                string id="scenario-gateway-"+g.id;
                if(from.npcs.Any(n=>n.id==id))continue;
                from.npcs=from.npcs.Concat(new[]{new WorldNpc{id=id,name=g.name,picture=g.npcJob,x=g.x,y=g.y,kind=(int)FacilityKind.ScenarioGateway,counter=Array.Empty<WorldPoint>(),evidence=Provenance,gateway=g.id}}).ToArray();
            }
        }
        public static string Save(string path,ScenarioGatewaySettings settings,GatewayMapCatalog catalog,string expectedHash)
        {
            Validate(settings,catalog);string current=File.Exists(path)?Hash(File.ReadAllBytes(path)):null;
            if(current!=expectedHash)throw new IOException("입구 설정이 외부에서 변경됐습니다. 다시 불러오세요.");
            var bytes=Encode(settings);Validate(Decode(bytes),catalog);Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{File.WriteAllBytes(temp,bytes);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}
            return Hash(bytes);
        }
    }
}
