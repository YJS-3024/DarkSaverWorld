using UnityEngine;

namespace DarkSaver.Combat
{
    public sealed partial class BattleMapPresentation
    {
        SpriteRenderer levelUpFlash,levelUpFlashCaster;
        Sprite levelUpWhiteSprite;
        public bool IsNativeLevelUpFlashVisible=>levelUpFlash!=null && levelUpFlash.enabled;
        void HideLevelUpFlash(){if(levelUpFlash!=null)levelUpFlash.enabled=false;if(levelUpFlashCaster!=null)levelUpFlashCaster.enabled=false;}
        void UpdateLevelUpFlash()
        {
            HideLevelUpFlash();if(!GameConstants.NativeMagicScreenFlashEnabled || !IsBound || !isActiveAndEnabled || view==null || !view.orthographic || battle.IsBattleFinished)return;
            SpriteRenderer caster=null;
            foreach(var e in levelUpCircles.Values)if(e.Playing && e.Caster!=null && NativeLevelUpCircleEffects.IsFlashCounter(NativeTargetEffects.CounterAt(e.Age))){caster=e.Caster;break;}
            if(caster==null)return;
            if(levelUpFlash==null)
            {
                var go=new GameObject("Native Level Up White Fill");go.transform.SetParent(transform,false);levelUpFlash=go.AddComponent<SpriteRenderer>();
                levelUpWhiteSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
                levelUpFlash.sprite=levelUpWhiteSprite;
                var redraw=new GameObject("Native Level Up Caster Redraw");redraw.transform.SetParent(transform,false);levelUpFlashCaster=redraw.AddComponent<SpriteRenderer>();
            }
            levelUpFlash.gameObject.layer=levelUpFlashCaster.gameObject.layer=caster.gameObject.layer;
            levelUpFlash.sortingLayerID=levelUpFlashCaster.sortingLayerID=caster.sortingLayerID;
            levelUpFlash.sortingOrder=32760;levelUpFlashCaster.sortingOrder=32761;
            float distance=Vector3.Dot(caster.transform.position-view.transform.position,view.transform.forward);
            levelUpFlash.transform.SetPositionAndRotation(view.ViewportToWorldPoint(new Vector3(.5f,.5f,distance)),view.transform.rotation);
            levelUpFlash.transform.localScale=new Vector3(view.orthographicSize*2*view.aspect,view.orthographicSize*2,1);
            levelUpFlash.enabled=true;
            // Native fills the game surface, redraws this caster, then skips the remaining unit pass.
            levelUpFlashCaster.sprite=caster.sprite;levelUpFlashCaster.color=caster.color;
            levelUpFlashCaster.flipX=caster.flipX;levelUpFlashCaster.flipY=caster.flipY;
            levelUpFlashCaster.transform.SetPositionAndRotation(caster.transform.position,caster.transform.rotation);
            levelUpFlashCaster.transform.localScale=caster.transform.localScale;levelUpFlashCaster.enabled=caster.enabled;
        }
        void ReleaseLevelUpFlash()
        {
            HideLevelUpFlash();if(levelUpFlash!=null)ReleasePreview(levelUpFlash.gameObject);if(levelUpFlashCaster!=null)ReleasePreview(levelUpFlashCaster.gameObject);
            if(levelUpWhiteSprite!=null)ReleasePreview(levelUpWhiteSprite);levelUpWhiteSprite=null;levelUpFlash=null;levelUpFlashCaster=null;
        }
    }
}
