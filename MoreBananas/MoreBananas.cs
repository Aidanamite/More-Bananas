using UnityEngine;
using System;
using HarmonyLib;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using HMLLibrary;

namespace MoreBananas
{
    public class Main : Mod
    {
        Harmony harmony;
        static bool started;
        public override bool CanUnload(ref string message)
        {
            if (SceneManager.GetActiveScene().name != Raft_Network.MenuSceneName)
            {
                message = "Mod must be loaded on the main menu";
                return false;
            }
            return base.CanUnload(ref message);
        }
        public void Start()
        {
            if (SceneManager.GetActiveScene().name != Raft_Network.MenuSceneName)
            {
                modlistEntry.modinfo.unloadBtn.GetComponent<Button>().onClick.Invoke();
                Debug.LogError("Mod must be loaded on the main menu");
                return;
            }
            (harmony = new Harmony("com.aidanamite.MoreBananas")).PatchAll();
            Log("Mod has been loaded!");
            started = true;
        }
        public void OnModUnload()
        {
            harmony?.UnpatchAll(harmony.Id);
            if (started)
                Log("Mod has been unloaded!");
        }
    }

    [HarmonyPatch(typeof(Landmark), "Initialize")]
    public class Patch_IslandSpawn
    {
        static GameObject plantPrefab = null;
        static int bananaInd = ItemManager.GetItemByName("Seed_Banana").UniqueIndex;
        static GameObject PlantPrefab
        {
            get
            {
                if (plantPrefab == null)
                    foreach (Plant plant in Resources.FindObjectsOfTypeAll<Plant>())
                        if (plant.item != null && plant.item.UniqueIndex == bananaInd)
                        {
                            plantPrefab = plant.gameObject;
                            break;
                        }
                return plantPrefab;
            }
        }
        static void Prefix(ref Landmark __instance, bool ___initialized)
        {
            if (!___initialized)
            {
                if (PlantPrefab == null)
                {
                    Debug.LogError("Could not find banana plant prefab");
                    return;
                }
                var list = new List<MeshRenderer>();
                __instance.transform.GetComponentsInChildrenRecursively(ref list);
                foreach (MeshRenderer model in list)
                    if (model.name.Contains("Banana_Bush") || model.name.Contains("Banan_Bush"))
                    {
                        GameObject gO = GameObject.Instantiate(PlantPrefab, model.transform.position, model.transform.rotation, model.transform.parent.parent);
                        gO.transform.localScale = model.transform.localScale;
                        gO.name = "MoreBananas_Banana_Bush";
                        GameObject.DestroyImmediate(gO.GetComponent<Plant>());
                        LandmarkItem_PickupItem lmpi = gO.AddComponent<LandmarkItem_PickupItem>();
                        lmpi.localLandmarkPosition = lmpi.transform.localPosition;
                        lmpi.Invoke("OnValidate", 0);
                        NetworkIDManager.AddNetworkID(gO.GetComponent<PickupItem_Networked>());
                        gO.tag = "Untagged";
                        foreach (Collider c in gO.GetComponentsInChildren<Collider>())
                            c.gameObject.tag = "Tree";
                        var pickup = gO.GetComponent<PickupItem>();
                        pickup.canBePickedUp = true;
                        pickup.dropper = gO.GetComponent<RandomDropper>();
                        pickup.yieldHandler = gO.GetComponent<YieldHandler>();
                        Traverse.Create(gO.GetComponent<HarvestableTree>()).Field("hideOnDepleted").SetValue(true);
                        GameObject.Destroy(model.gameObject);
                    }
            }
        }
    }
}