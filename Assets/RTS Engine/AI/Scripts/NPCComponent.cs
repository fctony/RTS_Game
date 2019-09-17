using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    public abstract class NPCComponent : MonoBehaviour {

        protected NPCManager npcMgr;
        protected FactionManager factionMgr;

        public virtual void InitManagers(NPCManager npcMgr, FactionManager factionMgr)
        {
            this.npcMgr = npcMgr;
            this.factionMgr = factionMgr;
        }
    }
}
