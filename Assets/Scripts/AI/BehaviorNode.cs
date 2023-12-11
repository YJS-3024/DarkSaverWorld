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

    public abstract class BTNode
    {
        protected List<BTNode> childNode;

        protected void AddChild(BTNode node)
        {
            childNode.Add(node);
        }

        protected virtual BTNodeState Evaluate()
        {
            return BTNodeState.Failure;
        }
    }

    public abstract class BTAction
    {
        private System.Func<BTNodeState> action;

        public BTAction(System.Func<BTNodeState> action)
        {
            this.action = action;
        }

        public virtual BTNodeState Evaluate()
        {
            return this.action();
        }
    }

    public abstract class ActionTask
    {
        protected abstract bool OnStart();
        protected abstract bool OnUpdate();
        protected abstract bool OnEnd();
    }
}
