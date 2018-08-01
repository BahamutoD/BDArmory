using System;
using System.Linq;
using System.Collections.Generic;
using KSP.UI.Screens;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace BDArmory.Modules
{
    public class ModuleMissileRearm : PartModule
    {
        private Transform MissileTransform = null;

        [KSPField(guiName = "Ordinance Available", guiActive = true, isPersistant = true)]
        public int ammoCount = 20;

        [KSPField(guiName = "Missile Assign", guiActive = true, isPersistant = true)]
        private string MissileName = "bahaAim120";

        [KSPAction("Resupply", KSPActionGroup.None)]
        private void ActionResupply(KSPActionParam param)
        {
            Resupply();
        }

        [KSPEvent(name = "Resupply", guiName = "Resupply", active = true, guiActive = true)]
        public void Resupply()
        {
            if (this.part.children.Count != 0)
            {
                Debug.Log("Not Empty" + this.part.children.Count);
            }
            else
            {
                List<AvailablePart> availablePart = PartLoader.LoadedPartsList;
                foreach (AvailablePart AP in availablePart)
                {
                    if (AP.partPrefab.name == MissileName)
                    {
                        foreach (PartModule m in AP.partPrefab.Modules)
                        {
                            if (m.moduleName == "MissileLauncher")
                            {
                                var partNode = new ConfigNode();
                                PartSnapshot(AP.partPrefab).CopyTo(partNode);
                                Debug.Log("Node" + AP.partPrefab.srfAttachNode.originalPosition);
                                CreatePart(partNode, MissileTransform.transform.position - MissileTransform.TransformDirection(AP.partPrefab.srfAttachNode.originalPosition), 
                                    this.part.transform.rotation, this.part, this.part, "srfAttach");
                                StartCoroutine(ResetTurret());
                            }
                        }
                    }
                }
            }
        }

        IEnumerator ResetTurret()
        {
            ammoCount -= 1;

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            var turret = part.FindModuleImplementing<MissileTurret>();
            if (turret != null)
            {
                turret.UpdateMissileChildren();
            }
        }

        [KSPEvent(name = "Reassign", guiName = "Reassign", active = true, guiActive = true)]
        public void Reassign()
        {
            if (this.part.children.Count == 1)
            {
                foreach (Part p in this.part.children)
                {
                    foreach (PartModule m in p.Modules)
                    {
                        if (m.moduleName == "MissileLauncher")
                        {
                            MissileName = p.name;
                            Debug.Log(MissileName);
                        }
                    }
                }
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            this.enabled = true;
            this.part.force_activate();
            MissileTransform = base.part.FindModelTransform("MissileTransform");
            Reassign();
        }

        public override void OnFixedUpdate()
        {

        }
        [KSPEvent(name = "Resupply", guiName = "Resupply", active = true, guiActive = false)]

        public static AttachNode GetAttachNodeById(Part p, string id)
        {
            var node = id == "srfAttach" ? p.srfAttachNode : p.FindAttachNode(id);
            if (node == null)
            {
                Debug.Log("Cannot find attach node {0} on part {1}. Using srfAttach" + id + p);
                node = p.srfAttachNode;
            }
            return node;
        }

        public static ModuleDockingNode GetDockingNode(
      Part part, string attachNodeId = null, AttachNode attachNode = null)
        {
            var nodeId = attachNodeId ?? (attachNode != null ? attachNode.id : null);
            return part.FindModulesImplementing<ModuleDockingNode>()
                .FirstOrDefault(x => x.referenceAttachNode == nodeId);
        }

        public static bool CoupleDockingPortWithPart(ModuleDockingNode dockingNode)
        {
            var tgtPart = dockingNode.referenceNode.attachedPart;
            if (tgtPart == null)
            {
                Debug.Log(
                    "Node's part {0} is not attached to anything thru the reference node" + dockingNode.part);
                return false;
            }
            if (dockingNode.state != dockingNode.st_ready.name)
            {
                Debug.Log("Hard reset docking node {0} from state '{1}' to '{2}'" +
                                dockingNode.part + dockingNode.state + dockingNode.st_ready.name);
                dockingNode.dockedPartUId = 0;
                dockingNode.dockingNodeModuleIndex = 0;
                // Target part lived in real world for some time, so its state may be anything.
                // Do a hard reset.
                dockingNode.fsm.StartFSM(dockingNode.st_ready.name);
            }
            var initState = dockingNode.lateFSMStart(PartModule.StartState.None);
            // Make sure part init catched the new state.
            while (initState.MoveNext())
            {
                // Do nothing. Just wait.
            }
            if (dockingNode.fsm.currentStateName != dockingNode.st_preattached.name)
            {
                Debug.Log("Node on {0} is unexpected state '{1}'" +
                                dockingNode.part + dockingNode.fsm.currentStateName);
                return false;
            }
            Debug.Log("Successfully set docking node {0} to state {1} with part {2}" +
                         dockingNode.part + dockingNode.fsm.currentStateName + tgtPart);
            return true;
        }

        static IEnumerator WaitAndMakeLonePart(Part newPart, OnPartReady onPartReady)
        {
            Debug.Log("Create lone part vessel for {0}" + newPart);
            string originatingVesselName = newPart.vessel.vesselName;
            newPart.physicalSignificance = Part.PhysicalSignificance.NONE;
            newPart.PromoteToPhysicalPart();
            newPart.Unpack();
            newPart.disconnect(true);
            Vessel newVessel = newPart.gameObject.AddComponent<Vessel>();
            newVessel.id = Guid.NewGuid();
            if (newVessel.Initialize(false))
            {

                newVessel.vesselName = Vessel.AutoRename(newVessel, originatingVesselName);
                newVessel.IgnoreGForces(10);
                newVessel.currentStage = StageManager.RecalculateVesselStaging(newVessel);
                newPart.setParent(null);
            }
            yield return new WaitWhile(() => !newPart.started && newPart.State != PartStates.DEAD);
            Debug.Log("Part {0} is in state {1}" + newPart + newPart.State);
            if (newPart.State == PartStates.DEAD)
            {
                Debug.Log("Part {0} has died before fully instantiating" + newPart);
                yield break;
            }

            if (onPartReady != null)
            {
                onPartReady(newPart);
            }
        }

        public static void AwakePartModule(PartModule module)
        {
            // Private method can only be accessed via reflection when requested on the class that declares
            // it. So, don't use type of the argument and specify it explicitly. 
            var moduleAwakeMethod = typeof(PartModule).GetMethod(
                "Awake", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (moduleAwakeMethod != null)
            {
                moduleAwakeMethod.Invoke(module, new object[] { });
            }
            else
            {
                Debug.Log("Cannot find Awake() method on {0}. Skip awakening", module);
            }
        }

        public static void ResetPartModule(PartModule module)
        {
            // Private method can only be accessed via reflection when requested on the class that declares
            // it. So, don't use type of the argument and specify it explicitly. 
            var moduleResetMethod = typeof(PartModule).GetMethod(
                "UpdateMissileChildren", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (moduleResetMethod != null)
            {
                moduleResetMethod.Invoke(module, new object[] { });
            }
            else
            {
                Debug.Log("Cannot find Awake() method on {0}. Skip awakening", module);
            }
        }

        public static void CleanupFieldsInModule(PartModule module)
        {
            // HACK: Fix uninitialized fields in science lab module.
            var scienceModule = module as ModuleScienceLab;
            if (scienceModule != null)
            {
                scienceModule.ExperimentData = new List<string>();
                Debug.Log(
                    "WORKAROUND. Fix null field in ModuleScienceLab module on the part prefab: {0}", module);
            }

            // Ensure the module is awaken. Otherwise, any access to base fields list will result in NRE.
            // HACK: Accessing Fields property of a non-awaken module triggers NRE. If it happens then do
            // explicit awakening of the *base* module class.
            try
            {
                module.Fields.GetEnumerator();
            }
            catch
            {
                Debug.Log(
                    "WORKAROUND. Module {0} on part prefab is not awaken. Call Awake on it", module);
                AwakePartModule(module);
            }
            foreach (var field in module.Fields)
            {
                var baseField = field as BaseField;
                if (baseField.isPersistant && baseField.GetValue(module) == null)
                {
                    //var proto = new StandardOrdinaryTypesProto();
                    //var defValue = proto.ParseFromString("", baseField.FieldInfo.FieldType);
                    //Debug.Log("WORKAROUND. Found null field {0} in module prefab {1},"
                    //                + " fixing to default value of type {2}: {3}",
                    //                baseField.name, module, baseField.FieldInfo.FieldType, defValue);
                    //baseField.SetValue(defValue, module);
                }
            }
        }

        public static void CleanupModuleFieldsInPart(Part part)
        {
            var badModules = new List<PartModule>();
            foreach (var moduleObj in part.Modules)
            {
                var module = moduleObj as PartModule;
                try
                {
                    CleanupFieldsInModule(module);
                }
                catch
                {
                    badModules.Add(module);
                }
            }
            // Cleanup modules that block KIS. It's a bad thing to do but not working KIS is worse.
            foreach (var moduleToDrop in badModules)
            {
                Debug.Log(
                    "Module on part prefab {0} is setup improperly: name={1}. Drop it!" + part, moduleToDrop);
                part.RemoveModule(moduleToDrop);
            }
        }

        public static ConfigNode PartSnapshot(Part part)
        {
            if (ReferenceEquals(part, part.partInfo.partPrefab))
            {
                // HACK: Prefab may have fields initialized to "null". Such fields cannot be saved via
                //   BaseFieldList when making a snapshot. So, go thru the persistent fields of all prefab
                //   modules and replace nulls with a default value of the type. It's unlikely we break
                //   something since by design such fields are not assumed to be used until loaded, and it's
                //   impossible to have "null" value read from a config.
                CleanupModuleFieldsInPart(part);
            }

            var node = new ConfigNode("PART");
            var snapshot = new ProtoPartSnapshot(part, null);

            snapshot.attachNodes = new List<AttachNodeSnapshot>();
            snapshot.srfAttachNode = new AttachNodeSnapshot("attach,-1");
            snapshot.symLinks = new List<ProtoPartSnapshot>();
            snapshot.symLinkIdxs = new List<int>();
            snapshot.Save(node);

            // Prune unimportant data
            node.RemoveValues("parent");
            node.RemoveValues("position");
            node.RemoveValues("rotation");
            node.RemoveValues("istg");
            node.RemoveValues("dstg");
            node.RemoveValues("sqor");
            node.RemoveValues("sidx");
            node.RemoveValues("attm");
            node.RemoveValues("srfN");
            node.RemoveValues("attN");
            node.RemoveValues("connected");
            node.RemoveValues("attached");
            node.RemoveValues("flag");

            node.RemoveNodes("ACTIONS");

            // Remove modules that are not in prefab since they won't load anyway
            var module_nodes = node.GetNodes("MODULE");
            var prefab_modules = part.partInfo.partPrefab.GetComponents<PartModule>();
            node.RemoveNodes("MODULE");

            for (int i = 0; i < prefab_modules.Length && i < module_nodes.Length; i++)
            {
                var module = module_nodes[i];
                var name = module.GetValue("name") ?? "";

                node.AddNode(module);

                if (name == "KASModuleContainer")
                {
                    // Containers get to keep their contents
                    module.RemoveNodes("EVENTS");
                }
                else if (name.StartsWith("KASModule"))
                {
                    // Prune the state of the KAS modules completely
                    module.ClearData();
                    module.AddValue("name", name);
                    continue;
                }

                module.RemoveNodes("ACTIONS");
            }

            return node;
        }

        public delegate void OnPartReady(Part affectedPart);

        public static Part CreatePart(AvailablePart avPart, Vector3 position, Quaternion rotation,
                                Part fromPart)
        {
            var partNode = new ConfigNode();
            PartSnapshot(avPart.partPrefab).CopyTo(partNode);
            return CreatePart(partNode, position, rotation, fromPart);
        }

        /// <summary>Creates a new part from the config.</summary>
        /// <param name="partConfig">Config to read part from.</param>
        /// <param name="position">Initial position of the new part.</param>
        /// <param name="rotation">Initial rotation of the new part.</param>
        /// <param name="fromPart"></param>
        /// <param name="coupleToPart">Optional. Part to couple new part to.</param>
        /// <param name="srcAttachNodeId">
        /// Optional. Attach node ID on the new part to use for coupling. It's required if coupling to
        /// part is requested.
        /// </param>
        /// <param name="tgtAttachNode">
        /// Optional. Attach node on the target part to use for coupling. It's required if
        /// <paramref name="srcAttachNodeId"/> specifies a stack node.
        /// </param>
        /// <param name="onPartReady">
        /// Callback to call when new part is fully operational and its joint is created (if any). It's
        /// undetermined how long it may take before the callback is called. The calling code must expect
        /// that there will be several frame updates and at least one fixed frame update.
        /// </param>
        /// <param name="createPhysicsless">
        /// Tells if new part must be created without rigidbody and joint. It's only used to create
        /// equippable parts. Any other use-case is highly unlikely.
        /// </param>
        /// <returns></returns>
        public static Part CreatePart(
            ConfigNode partConfig,
            Vector3 position,
            Quaternion rotation,
            Part fromPart,
            Part coupleToPart = null,
            string srcAttachNodeId = null,
            AttachNode tgtAttachNode = null,
            OnPartReady onPartReady = null,
            bool createPhysicsless = false)
        {
            // Sanity checks for the parameters.
            if (coupleToPart != null)
            {
                if (srcAttachNodeId == null
                    || srcAttachNodeId == "srfAttach" && tgtAttachNode != null
                    || srcAttachNodeId != "srfAttach"
                       && (tgtAttachNode == null || tgtAttachNode.id == "srfAttach"))
                {
                    // Best we can do is falling back to surface attach.
                    srcAttachNodeId = "srfAttach";
                    tgtAttachNode = null;
                }
            }

            var refVessel = coupleToPart != null ? coupleToPart.vessel : fromPart.vessel;
            var partNodeCopy = new ConfigNode();
            partConfig.CopyTo(partNodeCopy);
            var snapshot =
                new ProtoPartSnapshot(partNodeCopy, refVessel.protoVessel, HighLogic.CurrentGame);
            if (HighLogic.CurrentGame.flightState.ContainsFlightID(snapshot.flightID)
                || snapshot.flightID == 0)
            {
                snapshot.flightID = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);
            }
            snapshot.parentIdx = coupleToPart != null ? refVessel.parts.IndexOf(coupleToPart) : 0;
            snapshot.position = position;
            snapshot.rotation = rotation;
            snapshot.stageIndex = 0;
            snapshot.defaultInverseStage = 0;
            snapshot.seqOverride = -1;
            snapshot.inStageIndex = -1;
            snapshot.attachMode = srcAttachNodeId == "srfAttach"
                ? (int)AttachModes.SRF_ATTACH
                : (int)AttachModes.STACK;
            snapshot.attached = true;
            snapshot.flagURL = fromPart.flagURL;

            var newPart = snapshot.Load(refVessel, false);
            refVessel.Parts.Add(newPart);
            newPart.transform.position = position;
            newPart.transform.rotation = rotation;
            newPart.missionID = fromPart.missionID;
            newPart.UpdateOrgPosAndRot(newPart.vessel.rootPart);

            if (coupleToPart != null)
            {
                // Wait for part to initialize and then fire ready event.
                Debug.Log("Ready to error" + newPart + srcAttachNodeId + tgtAttachNode);
                newPart.StartCoroutine(
                    WaitAndCouple(newPart, srcAttachNodeId, tgtAttachNode, onPartReady,
                                  createPhysicsless: createPhysicsless));
            }
            else
            {
                // Create new part as a separate vessel.
                newPart.StartCoroutine(WaitAndMakeLonePart(newPart, onPartReady));
            }
            return newPart;
        }

        static IEnumerator WaitAndCouple(Part newPart, string srcAttachNodeId,
                                         AttachNode tgtAttachNode, OnPartReady onPartReady,
                                         bool createPhysicsless = false)
        {
            var tgtPart = newPart.parent;
            if (createPhysicsless)
            {
                newPart.PhysicsSignificance = 1;  // Disable physics on the part.
            }

            // Create proper attach nodes.
            Debug.Log("Attach new part {0} to {1}: srcNodeId={2}, tgtNode={3}" +
                         newPart + newPart.vessel +
                         srcAttachNodeId);
            var srcAttachNode = GetAttachNodeById(newPart, srcAttachNodeId);
            srcAttachNode.attachedPart = tgtPart;
            srcAttachNode.attachedPartId = tgtPart.flightID;
            if (tgtAttachNode != null)
            {
                tgtAttachNode.attachedPart = newPart;
                tgtAttachNode.attachedPartId = newPart.flightID;
            }

            // When target, source or both are docking ports force them into state PreAttached. It's the
            // most safe state that simulates behavior of parts attached in the editor.
            var srcDockingNode = GetDockingNode(newPart, attachNodeId: srcAttachNodeId);
            if (srcDockingNode != null)
            {
                // Source part is not yet started. It's functionality is very limited.
                srcDockingNode.state = "PreAttached";
                srcDockingNode.dockedPartUId = 0;
                srcDockingNode.dockingNodeModuleIndex = 0;
                Debug.Log("Force new node {0} to state {1}" + newPart + srcDockingNode.state);
            }
            var tgtDockingNode = GetDockingNode(tgtPart, attachNode: tgtAttachNode);
            if (tgtDockingNode != null)
            {
                CoupleDockingPortWithPart(tgtDockingNode);
            }

            // Wait until part is started. Keep it in position till it happen.
            Debug.Log("Wait for part {0} to get alive...", newPart);
            newPart.transform.parent = tgtPart.transform;
            var relPos = newPart.transform.localPosition;
            var relRot = newPart.transform.localRotation;
            if (newPart.PhysicsSignificance != 1)
            {
                // Mangling with colliders on physicsless parts may result in camera effects.
                var childColliders = newPart.GetComponentsInChildren<Collider>(includeInactive: false);
                CollisionManager.IgnoreCollidersOnVessel(tgtPart.vessel, childColliders);
            }
            while (!newPart.started && newPart.State != PartStates.DEAD)
            {
                yield return new WaitForFixedUpdate();
                if (newPart.rb != null)
                {
                    newPart.rb.position = newPart.parent.transform.TransformPoint(relPos);
                    newPart.rb.rotation = newPart.parent.transform.rotation * relRot;
                    newPart.rb.velocity = newPart.parent.Rigidbody.velocity;
                    newPart.rb.angularVelocity = newPart.parent.Rigidbody.angularVelocity;
                }
            }
            newPart.transform.parent = newPart.transform;
            Debug.Log("Part {0} is in state {1}" + newPart + newPart.State);
            if (newPart.State == PartStates.DEAD)
            {
                Debug.Log("Part {0} has died before fully instantiating", newPart);
                yield break;
            }

            // Complete part initialization.
            newPart.Unpack();
            newPart.InitializeModules();

            // Notify game about a new part that has just "coupled".
            GameEvents.onPartCouple.Fire(new GameEvents.FromToAction<Part, Part>(newPart, tgtPart));
            tgtPart.vessel.ClearStaging();
            GameEvents.onVesselPartCountChanged.Fire(tgtPart.vessel);
            newPart.vessel.checkLanded();
            newPart.vessel.currentStage = StageManager.RecalculateVesselStaging(tgtPart.vessel) + 1;
            GameEvents.onVesselWasModified.Fire(tgtPart.vessel);
            newPart.CheckBodyLiftAttachment();

            if (onPartReady != null)
            {
                onPartReady(newPart);
            }
        }
    }
}

