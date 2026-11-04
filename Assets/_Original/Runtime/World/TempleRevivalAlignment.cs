using System;
namespace DarkSaver.Original.World
{
    // Explicit local quote envelope; not a recovered incoming network layout.
    [Serializable] public sealed class TempleAlignmentOffer
    {
        public string id,token,character,session;[NonSerialized] public uint? fee;[NonSerialized] public int? target;
        public TempleAlignmentOffer Copy()=>(TempleAlignmentOffer)MemberwiseClone();
        public string Text=>!fee.HasValue?"":fee.Value==0?"당신의 성향을 악으로 바꾸시겠습니까?":"당신의 성향을 "+(target==1?"선":"악")+"으로 바꾸시겠습니까?\n 바꾸는데는 "+unchecked((int)fee.Value).ToString(System.Globalization.CultureInfo.InvariantCulture)+"원이 듭니다.";
        public byte[] Packet()
        {if(!fee.HasValue)return null;uint value=fee.Value;return new[]{(byte)0xac,(byte)value,(byte)(value>>8),(byte)(value>>16),(byte)(value>>24)};}
    }
}
