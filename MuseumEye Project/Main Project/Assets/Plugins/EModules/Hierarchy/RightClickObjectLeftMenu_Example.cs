//////// Custom Tree-IGenericMenu Example ////////
/*
    To create a hotkey you can use the following special characters: % (ctrl on Windows, cmd on macOS), # (shift).
    To create a menu with hotkey g and no key modifiers pressed use "MySubItem/MyMenuItem _g".
    Hot keys work only in the Hierarchy Window and do not overlap hot keys in other windows.
*/

#if UNITY_EDITOR

using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


/////////////////////////////////////////////////////MENU ITEM TEMPLATE///////////////////////////////////////////////////////////////////////////////
/*
 
    class MyMenu : HierarchyExtensions.IGenericMenu
    {
        public string Name { get { return "MySubItem/MyMenuItem %K"; } }
        public int PositionInMenu { get { return 0; } }

        public bool IsEnable(GameObject clickedObject) { return true; }
        public bool NeedExcludeFromMenu(GameObject clickedObject) { return false; } // or 'return clickedObject.GetComponent<MyComponent>() == null'

        public void OnClick(GameObject[] affectedObjectsArray)
        {
            throw new System.NotImplementedException();
        }
    }

*/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


namespace MyHierarchyMenu_Example
{



    #region ITEM 100-101 - Group/UnGroup

    class MyMenu_Group : HierarchyExtensions.IGenericMenu
    {
        public bool IsEnable(GameObject clickedObject) { return true; }
        public bool NeedExcludeFromMenu(GameObject clickedObject) { return false; }

        public int PositionInMenu { get { return 100; } }
        public string Name { get { return "Group %G"; } }


        public void OnClick(GameObject[] affectedObjectsArray)
        {
            var groupParent = affectedObjectsArray[0].transform.parent;
            var groupSiblingIndex = affectedObjectsArray[0].transform.GetSiblingIndex();

            var groupRoot = new GameObject("GROUP " + affectedObjectsArray[0].name);
            groupRoot.transform.SetParent(groupParent, false);
            groupRoot.transform.localScale = Vector3.one;
            groupRoot.transform.SetSiblingIndex(groupSiblingIndex);

            MyMenu_Utils.AssignUniqueName(groupRoot); // name
            if (groupRoot.GetComponentsInParent<Canvas>(true).Length != 0) { // canvas
                var rect = groupRoot.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            Undo.RegisterCreatedObjectUndo(groupRoot, groupRoot.name);
            var onlytop = MyMenu_Utils.GetOnlyTopObjects(affectedObjectsArray).OrderBy(go => go.transform.GetSiblingIndex());
            foreach (var gameObject in onlytop) {
                Undo.SetTransformParent(gameObject.transform, groupRoot.transform, groupRoot.name);
            }

            HierarchyExtensions.Utilities.SetExpanded(groupRoot.GetInstanceID(), true);

            Selection.objects = onlytop.ToArray();
            //Selection.objects = new[] { groubObject };
        }

    }


    class MyMenu_UnGroup : HierarchyExtensions.IGenericMenu
    {
        public bool IsEnable(GameObject clickedObject) { return clickedObject.transform.childCount != 0; }
        public bool NeedExcludeFromMenu(GameObject clickedObject) { return false; }

        public int PositionInMenu { get { return 101; } }
        public string Name { get { return "UnGroup %#G"; } }


        public void OnClick(GameObject[] affectedObjectsArray)
        {
            var ungroupedObjects = new List<GameObject>();
            var onlytop = MyMenu_Utils.GetOnlyTopObjects(affectedObjectsArray);
            foreach (var ungroupedRoot in onlytop) {
                var ungroupSiblinkIndex = ungroupedRoot.transform.GetSiblingIndex();
                var ungroupParent = ungroupedRoot.transform.parent;
                var undoName = ungroupedRoot.name;
                for (int i = ungroupedRoot.transform.childCount - 1; i >= 0; i--) {
                    var o = ungroupedRoot.transform.GetChild(i);
                    Undo.SetTransformParent(o.transform, ungroupParent, "Remove " + undoName);

                    Undo.RegisterFullObjectHierarchyUndo(o, "Remove " + undoName);
                    o.SetSiblingIndex(ungroupSiblinkIndex);
                    EditorUtility.SetDirty(o);

                    ungroupedObjects.Add(o.gameObject);
                }
                Undo.DestroyObjectImmediate(ungroupedRoot);
            }
            Selection.objects = ungroupedObjects.ToArray();
        }

    }

    #endregion



    #region ITEM 500 - ExperimentalDublicateNextToObject

    class MyMenu_ExperimentalDublicateNextToObject : HierarchyExtensions.IGenericMenu
    {
        public bool IsEnable(GameObject clickedObject) { return true; }
        public bool NeedExcludeFromMenu(GameObject clickedObject) { return false; }

        public int PositionInMenu { get { return 500; } }
        public string Name { get { return "Duplicate Next To Object %#D"; } }


        public void OnClick(GameObject[] affectedObjectsArray)
        {
            var onlytop = MyMenu_Utils.GetOnlyTopObjects(affectedObjectsArray);
            Selection.objects = onlytop.Select(Dublicate).Where(clone => clone).ToArray();
        }


        GameObject Dublicate(GameObject o)
        {
            if (!o.scene.IsValid()) return null; //If object is not created

            var targetSiblingIndex = o.transform.GetSiblingIndex() + 1;
            var clone = SelectDuplicationMethodAndClone(o); //if o is prefab or no

            clone.transform.SetParent(o.transform.parent, false); //to place next to source object
            clone.transform.SetSiblingIndex(targetSiblingIndex);

            MyMenu_Utils.AssignUniqueName(clone); //change name

            Undo.RegisterCreatedObjectUndo(clone, "Dublicate GameObjects");// create undo

            return clone;
        }


        GameObject SelectDuplicationMethodAndClone(GameObject o)
        {
            GameObject result = null;

            var projectPrefab = PrefabUtility.GetPrefabParent(o) as GameObject;
            if (projectPrefab != null && projectPrefab.transform.root.gameObject == projectPrefab.gameObject) { //Duplicate as prefab

                result = PrefabUtility.InstantiatePrefab(projectPrefab.transform.root.gameObject) as GameObject;
                PrefabUtility.SetPropertyModifications(result, PrefabUtility.GetPropertyModifications(o));

            } else { //Duplicate as normal object

                result = Object.Instantiate(o);
            }

            var trimArray = new[] { "(Clone)" };
            foreach (var s in trimArray) {
                if (result.name.Length == 0 || result.name.Length < s.Length) continue;
                if (result.name.EndsWith(s)) result.name = result.name.Remove(result.name.Length - s.Length);
            }

            return result;

        }


    }

    #endregion



    #region ITEM 1000-1001 - ExpandSelecdedObject/CollapseSelecdedObject

    class MyMenu_ExpandSelecdedObject : HierarchyExtensions.IGenericMenu
    {
        public bool IsEnable(GameObject clickedObject) { return clickedObject.transform.childCount != 0; }
        public bool NeedExcludeFromMenu(GameObject clickedObject) { return false; }

        public int PositionInMenu { get { return 1000; } }
        public string Name { get { return "Expand Selection"; } }


        public void OnClick(GameObject[] affectedObjectsArray)
        {
            foreach (var result in affectedObjectsArray.Select(o => o.GetInstanceID()))
                HierarchyExtensions.Utilities.SetExpandedRecursive(result, true);
        }

    }


    class MyMenu_CollapseSelecdedObject : HierarchyExtensions.IGenericMenu
    {
        public bool IsEnable(GameObject clickedObject) { return clickedObject.transform.childCount != 0; }
        public bool NeedExcludeFromMenu(GameObject clickedObject) { return false; }

        public int PositionInMenu { get { return 1001; } }
        public string Name { get { return "Collapse Selection"; } }


        public void OnClick(GameObject[] affectedObjectsArray)
        {
            foreach (var result in affectedObjectsArray.Select(o => o.GetInstanceID()))
                HierarchyExtensions.Utilities.SetExpandedRecursive(result, false);
        }

    }

    #endregion



    #region ITEM 2000-2001 - SelectOnlyTopObjects/SelectAllChildren

    class MyMenu_SelectOnlyTopObjects : HierarchyExtensions.IGenericMenu
    {
        public bool IsEnable(GameObject clickedObject) { return Selection.gameObjects.Length >= 2; }
        public bool NeedExcludeFromMenu(GameObject clickedObject) { return false; }

        public int PositionInMenu { get { return 2000; } }
        public string Name { get { return "Select Only Top Objects"; } }


        public void OnClick(GameObject[] affectedObjectsArray)
        {
            Selection.objects = Selection.gameObjects.Where(g => g.GetComponentsInParent<Transform>(true).Count(p => Selection.gameObjects.Contains(p.gameObject)) == 1).Select(g => g.gameObject).ToArray();
        }

    }


    class MyMenu_SelectAllChildren : HierarchyExtensions.IGenericMenu
    {
        public bool IsEnable(GameObject clickedObject) { return clickedObject.transform.childCount != 0; }
        public bool NeedExcludeFromMenu(GameObject clickedObject) { return false; }

        public int PositionInMenu { get { return 2001; } }
        public string Name { get { return "Select All Children"; } }


        public void OnClick(GameObject[] affectedObjectsArray)
        {
            Selection.objects = affectedObjectsArray.SelectMany(s => s.GetComponentsInChildren<Transform>(true)).Select(s => s.gameObject).ToArray();
        }

    }

    #endregion






    #region - Utils

    static class MyMenu_Utils
    {
        public static void AssignUniqueName(GameObject o)
        {

            var usedNames = new SortedDictionary<string, string>();
            var childList = o.transform.parent
                ? new Transform[o.transform.parent.childCount].Select((t, i) => o.transform.parent.GetChild(i))
                : o.scene.GetRootGameObjects().Select(go => go.transform);

            foreach (var child in childList.Where(child => child != o.transform)) {
                if (!usedNames.ContainsKey(child.name)) usedNames.Add(child.name, child.name);
            }// existing names

            if (!usedNames.ContainsKey(o.name)) return;



            var number = 1;
            var name = o.name;

            var leftBracket = name.IndexOf('(');
            var rightBracket = name.IndexOf(')');

            if (leftBracket != -1 && rightBracket != -1 && rightBracket - leftBracket > 1) {
                int parseResult;
                if (int.TryParse(name.Substring(leftBracket + 1, rightBracket - leftBracket - 1), out parseResult)) {
                    number = parseResult + 1;
                    name = name.Remove(leftBracket);
                }
            }// previous value



            name = name.TrimEnd();
            while (usedNames.ContainsKey(name + " (" + number + ")")) ++number;
            o.name = name + " (" + number + ")"; //result

        }

        public static GameObject[] GetOnlyTopObjects(GameObject[] affectedObjectsArray)
        {
            return affectedObjectsArray.Where(g => g.GetComponentsInParent<Transform>(true).Count(p => affectedObjectsArray.Contains(p.gameObject)) == 1).Select(g => g.gameObject).ToArray();
        }
    }

    #endregion



}//namespace

#endif