#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace EModules
{
    class DescriptionHelper : MonoBehaviour, EModules.IHashProperty
    {
        [HideInInspector]
        [SerializeField]
        List<GameObject> Hash1 = new List<GameObject>();
        [HideInInspector]
        [SerializeField]
        List<string> Hash2 = new List<string>();
        [HideInInspector]
        [SerializeField]
        List<Int32List> Hash3 = new List<Int32List>();
        [HideInInspector]
        [SerializeField]
        List<Int32List> Hash4 = new List<Int32List>();

        public List<GameObject> GetHash1()
        {
            return Hash1;
        }

        public void SetHash1(List<GameObject> hash) { Hash1 = hash; }
        public List<string> GetHash2()
        {
            return Hash2;
        }

        public void SetHash2(List<string> hash) { Hash2 = hash; }
        public List<Int32List> GetHash3()
        {
            return Hash3;
        }

        public void SetHash3(List<Int32List> hash) { Hash3 = hash; }
        public List<Int32List> GetHash4()
        {
            return Hash4;
        }

        public void SetHash4(List<Int32List> hash) { Hash4 = hash; }


        public Component component {
            get { return this; }
        }

    }
}
#endif
