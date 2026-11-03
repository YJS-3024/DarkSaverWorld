using System;
using UnityEngine;
using DarkSaver.Original.Tutorial;

namespace DarkSaver.Original.World
{
    public sealed class NativeWorldArt : ScriptableObject
    {
        public int[] magicSoundIds;
        public AudioClip[] magicSounds;
        public AudioClip MagicSound(int id){int i=magicSoundIds==null?-1:Array.IndexOf(magicSoundIds,id);return i>=0&&i<(magicSounds?.Length??0)?magicSounds[i]:null;}
        public TextAsset definition;
        public int[] nativeMapIds,nativeGraphicIds,legacyGraphicIds;
        public TextAsset[] nativeMaps,nativeGraphics,legacyGraphics;
        public byte[] MapBytes(int id)
        {int i=nativeMapIds==null?-1:Array.IndexOf(nativeMapIds,id);return i>=0&&i<(nativeMaps?.Length??0)?nativeMaps[i]?.bytes:null;}
        public byte[] MapGraphic(int id,int map)
        {
            bool old=map==1000;var ids=old?legacyGraphicIds:nativeGraphicIds;var images=old?legacyGraphics:nativeGraphics;
            int i=ids==null?-1:Array.IndexOf(ids,id);
            if(i<0||images==null||i>=images.Length||images[i]==null)throw new System.IO.InvalidDataException("Native world graphic is missing: "+map+"/"+id);
            return images[i].bytes;
        }
        public TextAsset growth;
        public TextAsset combat;
        public TextAsset magicMenu;
        public TextAsset battleItems;
        public TextAsset magicProjectiles;
        public TextAsset resultLayout;
        public TextAsset classLayout;
        public TextAsset utilityLayout;
        public Texture2D[] utilityWindows;
        public WaitingDefinition UtilityLayout=>utilityLayout==null?null:JsonUtility.FromJson<WaitingDefinition>(utilityLayout.text);
        public TextAsset commanderLayout;
        public TextAsset personalLayout;
        public TextAsset chatLayout;
        public Texture2D chatWindow;
        public WaitingWindowDefinition ChatLayout=>chatLayout==null?null:JsonUtility.FromJson<WaitingWindowDefinition>(chatLayout.text);
        public TextAsset privateChatLayout;
        public Texture2D privateChatWindow;
        public WaitingWindowDefinition PrivateChatLayout=>privateChatLayout==null?null:JsonUtility.FromJson<WaitingWindowDefinition>(privateChatLayout.text);
        public WaitingDefinition PersonalLayout=>personalLayout==null?null:JsonUtility.FromJson<WaitingDefinition>(personalLayout.text);
        public Texture2D commanderWindow,commanderMinimized;
        public WorldWindowDefinition CommanderLayout=>commanderLayout==null?null:JsonUtility.FromJson<WorldWindowDefinition>(commanderLayout.text);
        public Texture2D classWindow,classMinimized;
        public WorldWindowDefinition ClassLayout=>classLayout==null?null:JsonUtility.FromJson<WorldWindowDefinition>(classLayout.text);
        public Texture2D resultWindow,resultButtons;
        public WorldWindowDefinition ResultLayout=>resultLayout==null?null:JsonUtility.FromJson<WorldWindowDefinition>(resultLayout.text);
        public Texture2D[] grounds, decorations, pictures, windows;
        public int[] pictureIds;
        public Texture2D buttons, items;
        public Texture2D attackMarker;
        public int ownPalette=NativeBodyPalette.OriginalOwn;
        public int[] paletteJobs,paletteStyles;
        public Texture2D[] palettePictures;
        public NativeTutorialArt battle;
        public WorldDefinition Definition
        {get{
            if(definition==null)return null;var world=JsonUtility.FromJson<WorldDefinition>(definition.text);
            world.growthCatalog=growth==null?null:JsonUtility.FromJson<CharacterGrowthCatalog>(growth.text);
            var hire=Resources.Load<TextAsset>("OriginalMercenarySkills");world.mercenaryCatalog=hire==null?new NativeMercenaryCatalog():JsonUtility.FromJson<NativeMercenaryCatalog>(hire.text);
            world.magicProjectiles=magicProjectiles==null?null:JsonUtility.FromJson<MagicProjectileCatalog>(magicProjectiles.text);
            world.battleItems=battleItems==null?null:JsonUtility.FromJson<BattleItemCatalog>(battleItems.text);
            world.magicMenu=magicMenu==null?null:JsonUtility.FromJson<MagicMenuCatalog>(magicMenu.text);
            world.combatCatalog=combat==null?null:JsonUtility.FromJson<OriginalCombatCatalog>(combat.text);return world;
        }}
        public bool IsConfigured => definition != null && grounds?.Length == 11 && decorations != null && pictures != null
            && pictures.Length == pictureIds?.Length && windows?.Length == 7 && buttons != null && items != null && battle != null && battle.WaitingConfigured;
        public Texture2D Picture(int id) { int i = Array.IndexOf(pictureIds, id); return i >= 0 ? pictures[i] : null; }
        public Texture2D PalettePicture(int id,int palette)
        {
            if(id>299||palette<1||palette>3)return Picture(id);
            if(paletteJobs!=null&&paletteStyles?.Length==paletteJobs.Length&&palettePictures?.Length==paletteJobs.Length)
                for(int i=0;i<paletteJobs.Length;i++)if(paletteJobs[i]==id&&paletteStyles[i]==palette)return palettePictures[i];
            return Picture(id);
        }
    }
}
