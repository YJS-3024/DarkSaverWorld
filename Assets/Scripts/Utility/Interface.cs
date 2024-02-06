using UnityEngine;

namespace Utility
{
    public interface iInputData
    {
        public void OnClick_Ground(RaycastHit2D[] hit);
        public void OnClick_Player(RaycastHit2D[] hit);
        public void OnClick_Enemy(RaycastHit2D[] hit);
        public void OnClick_ActionPlate(RaycastHit2D[] hit);
    }
}