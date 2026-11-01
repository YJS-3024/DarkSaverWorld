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
        // Missing trigger/size fields in version 1 files retain the NPC, one-tile behavior.
        [DataMember(EmitDefaultValue=false)] public string trigger, replacesPortal;
        [DataMember(EmitDefaultValue=false)] public int width, height, returnX, returnY;
        public bool IsWalk => trigger == "walk";
        public int Width => width == 0 ? 1 : width;
        public int Height => height == 0 ? 1 : height;
        public bool Contains(int px,int py) => px>=x && py>=y && (long)px<x+(long)Width && (long)py<y+(long)Height;
        public WorldPoint[] Cells() => Enumerable.Range(0,Height).SelectMany(dy=>Enumerable.Range(0,Width).Select(dx=>new WorldPoint{x=x+dx,y=y+dy})).ToArray();
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
            var ids=new HashSet<string>();var occupied=new HashSet<string>();var replacements=new HashSet<string>();
            foreach(var g in settings.gates)
            {
                var from=catalog.maps.FirstOrDefault(m=>m.id==g?.fromMap);var to=catalog.maps.FirstOrDefault(m=>m.id==g?.targetMap);
                if(g==null||!Guid.TryParseExact(g.id,"N",out _)||!ids.Add(g.id)||from==null||to==null||from.id==to.id
                    ||(!string.IsNullOrEmpty(g.trigger)&&g.trigger!="npc"&&g.trigger!="walk")
                    ||(!g.IsWalk&&(to.kind!="scenario"||(from.kind!="world"&&from.kind!="town")||!catalog.npcJobs.Contains(g.npcJob)||g.Width!=1||g.Height!=1||!string.IsNullOrEmpty(g.replacesPortal)))
                    ||g.fromHash!=from.hash||g.targetHash!=to.hash||g.Width<=0||g.Height<=0||(g.IsWalk&&(g.width<=0||g.height<=0))
                    ||g.x<0||g.y<0||(long)g.x+g.Width>from.width||(long)g.y+g.Height>from.height||g.spawnX<0||g.spawnY<0||g.spawnX>=to.width||g.spawnY>=to.height
                    ||string.IsNullOrWhiteSpace(g.name))throw new InvalidDataException("입구 방식·영역·도착 좌표·맵 버전을 확인하세요.");
                if(!string.IsNullOrEmpty(g.replacesPortal)&&!replacements.Add(g.fromMap+":"+g.replacesPortal))throw new InvalidDataException("같은 기존 출입구를 두 번 수정할 수 없습니다.");
                foreach(var cell in g.Cells())if(!occupied.Add(g.fromMap+":"+cell.x+":"+cell.y))throw new InvalidDataException("입구 영역이 서로 겹칩니다.");
            }
        }
        public static void Apply(ScenarioGatewaySettings settings,GatewayMapCatalog catalog,WorldDefinition world)
        {
            Validate(settings,catalog);
            // Rebuild from a retained baseline so disabling an override restores its original door.
            // Publish only after all entries pass; rejected authoring cannot partly modify the world.
            var baseline=world.gatewayBaseMaps??world.maps;
            var maps=baseline.Select(m=>m.CopyForPlacement()).ToArray();
            foreach(var g in settings.gates.Where(g=>settings.applyToGame&&g.enabled))
            {
                var from=maps.FirstOrDefault(m=>m.id==g.fromMap);var target=maps.FirstOrDefault(m=>m.id==g.targetMap);
                if(from==null||target==null)throw new InvalidDataException("출발/도착 맵을 찾을 수 없습니다.");
                var replaced=string.IsNullOrEmpty(g.replacesPortal)?null:from.portals.FirstOrDefault(p=>p.id==g.replacesPortal);
                if(!string.IsNullOrEmpty(g.replacesPortal)&&replaced==null)throw new InvalidDataException("수정할 기존 출입구를 찾을 수 없습니다.");
                if(from.npcs.Any(n=>g.Contains(n.x,n.y))||from.portals.Where(p=>p!=replaced).Any(p=>p.cells.Any(c=>g.Contains(c.x,c.y)))
                    ||world.campaign?.Map(g.fromMap)?.units.Any(u=>g.Contains(u.x,u.y))==true)throw new InvalidDataException("입구 영역이 기존 직원·출입구·원본 몬스터와 겹칩니다.");
                var tile=new DarkSaver.Original.Tutorial.TutorialTile(g.spawnX,g.spawnY);int cost=target.map.Terrain(tile,2);
                if(cost<=0||cost==99||cost==-99||world.campaign?.Map(g.targetMap)?.units.Any(u=>u.x==g.spawnX&&u.y==g.spawnY)==true)throw new InvalidDataException("도착 칸이 막혀 있거나 원본 몬스터와 겹칩니다.");
                string id="scenario-gateway-"+g.id;
                if(g.IsWalk)
                {
                    foreach(var cell in g.Cells())
                    {int terrain=from.map.Terrain(cell.Tile,2);if(terrain<=0||terrain==99||terrain==-99)throw new InvalidDataException("입구 영역에 이동할 수 없는 칸이 있습니다.");}
                    if(target.training&&(from.id!=1000||!from.map.Contains(new DarkSaver.Original.Tutorial.TutorialTile(g.returnX,g.returnY))))throw new InvalidDataException("수련장 입구는 카오시아의 복귀 칸을 지정하세요.");
                    from.portals=from.portals.Where(p=>p!=replaced).Concat(new[]{new WorldPortal{id=replaced?.id??id,gateway=g.id,targetMap=g.targetMap,spawnX=g.spawnX,spawnY=g.spawnY,returnX=g.returnX,returnY=g.returnY,cells=g.Cells()}}).ToArray();
                }
                else from.npcs=from.npcs.Concat(new[]{new WorldNpc{id=id,name=g.name,picture=g.npcJob,x=g.x,y=g.y,kind=(int)FacilityKind.ScenarioGateway,counter=Array.Empty<WorldPoint>(),evidence=Provenance,gateway=g.id}}).ToArray();
            }
            foreach(var g in settings.gates.Where(g=>settings.applyToGame&&g.enabled))
            {
                var target=maps.First(m=>m.id==g.targetMap);
                if(target.npcs.Any(n=>n.x==g.spawnX&&n.y==g.spawnY)||target.portals.Any(p=>p.cells.Any(c=>c.x==g.spawnX&&c.y==g.spawnY)))throw new InvalidDataException("도착 칸이 적용 후 직원/출입구와 겹칩니다.");
                if(g.IsWalk&&target.training)
                {
                    var from=maps.First(m=>m.id==g.fromMap);var tile=new DarkSaver.Original.Tutorial.TutorialTile(g.returnX,g.returnY);int terrain=from.map.Terrain(tile,2);
                    if(terrain<=0||terrain==99||terrain==-99||from.npcs.Any(n=>n.x==g.returnX&&n.y==g.returnY)||from.portals.Any(p=>p.cells.Any(c=>c.x==g.returnX&&c.y==g.returnY)))throw new InvalidDataException("수련장 복귀 칸이 막혀 있거나 직원/입구와 겹칩니다.");
                }
            }
            world.gatewayBaseMaps=baseline;world.maps=maps;world.gateways=settings.Copy();
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
