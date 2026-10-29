using System.Text.RegularExpressions;

// Offline YAML adapter only. Production uses Unity Tilemap directly, not this reader.
internal static class SavedSceneTerrainReader
{
    public static int[] Read(string scenePath,int width,int height)
    {
        string text=File.ReadAllText(scenePath);
        if(!text.StartsWith("%YAML"))throw new InvalidDataException("Offline terrain audit requires text serialization; use Unity for binary scenes: "+scenePath);
        var docs=Regex.Matches(text,@"^--- !u!(?<type>\d+) &(?<id>-?\d+)\r?\n(?<body>.*?)(?=^--- !u!|\z)",RegexOptions.Multiline|RegexOptions.Singleline);
        string go=docs.Cast<Match>().Single(m=>m.Groups["type"].Value=="1" && Regex.IsMatch(m.Groups["body"].Value,@"^  m_Name: TerrainAttributes\s*$",RegexOptions.Multiline)).Groups["id"].Value;
        string body=docs.Cast<Match>().Single(m=>m.Groups["type"].Value=="1839735485" && Regex.IsMatch(m.Groups["body"].Value,@"m_GameObject: \{fileID: "+go+@"\}")).Groups["body"].Value;
        string Section(string key)=>Regex.Match(body,@"^  "+key+@":\s*\r?\n(?<rows>.*?)(?=^  \w+:|\z)",RegexOptions.Multiline|RegexOptions.Singleline).Groups["rows"].Value;
        var paths=Directory.EnumerateFiles("Assets/_Game/Art/Converted/NonScenarioMaps/Terrain","*.asset.meta",SearchOption.AllDirectories)
            .ToDictionary(p=>Regex.Match(File.ReadAllText(p),@"^guid: (\w+)",RegexOptions.Multiline).Groups[1].Value,p=>p[..^5]);
        var values=Regex.Matches(Section("m_TileAssetArray"),@"m_Data: \{fileID: 11400000, guid: (\w+), type: 2\}").Cast<Match>()
            .Select(m=>int.Parse(Regex.Match(File.ReadAllText(paths[m.Groups[1].Value]),@"m_Name: Terrain_(\d+)").Groups[1].Value)).ToArray();
        var result=new int[width*height];var seen=new HashSet<(int,int)>();
        foreach(Match m in Regex.Matches(Section("m_Tiles"),@"first: \{x: (-?\d+), y: (-?\d+), z: 0\}\s*second: \{[^}]*m_TileIndex: (\d+),"))
        {
            int x=int.Parse(m.Groups[1].Value),y=int.Parse(m.Groups[2].Value),index=int.Parse(m.Groups[3].Value);
            if(x<0 || x>=width || y<0 || y>=height || index>=values.Length || !seen.Add((x,y)))throw new InvalidDataException("Invalid saved terrain cell: "+scenePath);
            result[(height-1-y)*width+x]=values[index];
        }
        if(values.Length>0 && seen.Count==0)throw new InvalidDataException("Saved terrain records were not parsed: "+scenePath);
        return result;
    }
}
