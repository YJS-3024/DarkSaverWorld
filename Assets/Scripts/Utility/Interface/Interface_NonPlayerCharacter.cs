using System.Collections.Generic;

/// <summary>
/// 플레이어용 인터페이스
/// </summary>
public interface i_PlayerChar
{   
    public void Attack(PlanePathNode node);
    
    /// <summary>
    /// 마법 공격 시작
    /// </summary>
    public void MagicSkill(int skillId) { }
    public void MagicSkill(int skillId, List<PlanePathNode> node) { }
    
    /// <summary>
    /// 이동
    /// </summary>
    public void Move(List<PlanePathNode> nodes = null);
    
    public void Dead();
    
    
    /// <summary>
    /// 휴식
    /// </summary>
    public void Rest();

    public void HitDamage(int damage);
}

/// <summary>
/// 비전투용 Npc 인터페이스
/// </summary>
public interface i_Npc_NoBattle
{
    public void Move(List<PlanePathNode> nodes = null);
}

/// <summary>
/// 전투용 Npc 인터페이스
/// </summary>
public interface i_Npc_Battle
{
    public void Attack(PlanePathNode node = null);
    public void Move(List<PlanePathNode> nodes = null);
    public void Dead();
    public void Rest();
    
    public void HitDamage(int damage);
    public bool OnSearchPlayer(int range);
}

/// <summary>
/// 공격불가 적군 인터페이스
/// </summary>
public interface i_Enemy_NoBattle
{
    public void Dead();
    public void Rest();
    
    public void HitDamage(int damage);
}

/// <summary>
/// 공격가능 적군 인터페이스
/// </summary>
public interface i_Enemy_Battle
{
    public void Attack(PlanePathNode node = null);
    public void Move(List<PlanePathNode> nodes = null);
    public void Dead();
    public void Rest();
    
    public void HitDamage(int damage);
    public bool OnSearchPlayer(int range);
}