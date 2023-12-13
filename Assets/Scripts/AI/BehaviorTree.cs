using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviorTree
{
    public enum BTNodeState
    {
        Success,
        Failure,
        Running
    }

    public interface IAction
    {
        protected void OnAction();
    }
    public interface ICondition
    {
        protected bool OnCheck();
    }

    public abstract class BTNode
    {
        protected readonly List<BTNode> childNode = new();

        public virtual void AddChild(BTNode node)
        {
            childNode.Add(node);
        }

        public abstract BTNodeState Evaluate();

        public virtual void Initialize() { }

        public virtual void Update() { }
    }

    /// <summary>
    /// 액션을 실행용 노드
    /// </summary>
    public class BTAction : BTNode, IAction
    {
        private readonly Action _onCall;

        public BTAction(Action action)
        {
            _onCall += action;
        }

        public override BTNodeState Evaluate()
        {
            _onCall();
            return BTNodeState.Success;
        }

        void IAction.OnAction()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ActionNode를 실행시키기 위한 조건 체크 노드
    /// </summary>
    public class BTCondition : BTNode, ICondition
    {
        private Func<bool> _onCheck;

        public BTCondition(Func<bool> onCheck)
        {
            _onCheck = onCheck;
        }

        public override BTNodeState Evaluate()
        {
            return _onCheck.Invoke() ? BTNodeState.Success : BTNodeState.Failure;
        }

        bool ICondition.OnCheck()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 최상위 노드
    /// </summary>
    public class BTRoot : BTNode
    {
        public override BTNodeState Evaluate()
        {
            if (childNode.Count == 0)
            {
                return BTNodeState.Failure;
            }
            else
            {
                foreach (var node in childNode)
                {
                    var state = node.Evaluate();
                    if (state == BTNodeState.Failure)
                    {
                        return state;
                    }
                }

                return BTNodeState.Success;
            }
        }
    }

    /// <summary>
    /// 왼쪽에서 오른쪽 평가중 하나라도 성공이 아니면 실패
    /// </summary>
    public class BTSequence : BTNode
    {
        public override BTNodeState Evaluate()
        {
            foreach (var node in childNode)
            {
                if (node.Evaluate() == BTNodeState.Failure)
                {
                    return BTNodeState.Failure;
                }
            }

            return BTNodeState.Success;
        }
    }

    /// <summary>
    /// 왼쪽에서 오른쪽으로 모든 평가중 하나라도 성공이면 성공
    /// </summary>
    public class BTSelector : BTNode
    {
        public override BTNodeState Evaluate()
        {
            foreach (var node in childNode)
            {
                if (node.Evaluate() == BTNodeState.Success)
                {
                    return BTNodeState.Success;
                }
            }

            return BTNodeState.Failure;
        }
    }
}
