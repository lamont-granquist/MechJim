using System.Collections.Generic;

namespace MechJim.Manager {
    [Enable(typeof(WarpManager))]
    public class AutoScience: ManagerBase {
        public ModuleScienceContainer activeContainer { get; set; }
        public double autowarpRate { get; set; }

        public AutoScience(Core core): base(core) { }

        private ModuleScienceContainer ActiveContainer() {
            if (activeContainer)
                return activeContainer;
            var list = vessel.FindPartModulesImplementing<ModuleScienceContainer>();
            if (list.Count == 0)
                return null;
            return list[0];
        }

        private List<ModuleScienceExperiment> ExperimentList() {
            return vessel.FindPartModulesImplementing<ModuleScienceExperiment>();
        }

        private List<ModuleScienceContainer> ContainerList() {
            return vessel.FindPartModulesImplementing<ModuleScienceContainer>();
        }

        protected override void OnEnable() {
            autowarpRate = 0;
        }

        protected override void OnDisable() {
            autowarpRate = 0;
        }

        public void AutoWarpRate(double rate) {
            if (rate <= 1) {
                warp.MinimumWarp(this);
            }
            autowarpRate = rate;
        }

        public override void OnFixedUpdate() {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX)
                return;
            if (!ActiveContainer())
                return;
            TransferScience();
            MaybeManageWarp();
            RunScience();
        }

        private void TransferScience() {
            if (ActiveContainer().GetActiveVesselDataCount() != ActiveContainer().GetScienceCount()) {
                /* move all science from experiments to main container */
                ActiveContainer().StoreData(ExperimentList().ConvertAll(x => (IScienceDataContainer)x), true);
                /* move all science stored in other containers to main container */
                var otherContainers = ContainerList();
                otherContainers.Remove(ActiveContainer());
                ActiveContainer().StoreData(otherContainers.ConvertAll(x => (IScienceDataContainer)x), true);
            }
        }

        private bool surfaceSamplesUnlocked() {
            return GameVariables.Instance.UnlockedEVA(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex))
                && GameVariables.Instance.UnlockedFuelTransfer(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.ResearchAndDevelopment));
        }

        private bool runnableExperiment(ModuleScienceExperiment e) {
            if (ActiveContainer().HasData(newScienceData(e)))
                return false;
            if (!surfaceSamplesUnlocked() && e.experiment.id == "surfaceSample")
                return false;
            if (!e.rerunnable && !IsScientistOnBoard()) /* FIXME: if its 100% and no scientist, still take the data */
                return false;
            if (!e.experiment.IsAvailableWhile(currentSituation(), mainBody))
                return false;
            if (currentScienceValue(e) < 0.1)
                return false;
            return true;
        }

        private bool vesselHasRunnableScience() {
            List<ModuleScienceExperiment> experiments = ExperimentList();
            if (experiments.Count == 0)
                return false;
            for(int i=0; i<experiments.Count; i++) {
                ModuleScienceExperiment e = experiments[i];
                if (runnableExperiment(e))
                    return true;
            }
            return false;
        }

        private void MaybeManageWarp() {
            if (autowarpRate <= 1)
                return;
            if (vesselHasRunnableScience()) {
                warp.MinimumWarp(this);
            } else {
                warp.WarpAtRegularRate(this, autowarpRate);
            }
        }

        private void RunScience() {
            List<ModuleScienceExperiment> experiments = ExperimentList();
            if (experiments.Count == 0)
                return;
            for(int i=0; i<experiments.Count; i++) {
                ModuleScienceExperiment e = experiments[i];
                if (!runnableExperiment(e))
                    continue;
                ActiveContainer().AddData(newScienceData(e));
            }
        }

        /* construct a ScienceData for an Experiment */
        ScienceData newScienceData(ModuleScienceExperiment e) {
            return new ScienceData(
                    amount: e.experiment.baseValue * currentScienceSubject(e.experiment).dataScale,
                    xmitValue: e.xmitDataScalar,
                    xmitBonus: 0f,
                    id: currentScienceSubject(e.experiment).id,
                    dataName: currentScienceSubject(e.experiment).title
                    );
        }

        bool IsScientistOnBoard() {
            var crew = vessel.GetVesselCrew();
            for(int i=0; i<crew.Count; i++) {
                var kerbal = crew[i];
                if (kerbal.experienceTrait.Title == "Scientist")
                    return true;
            }
            return false;
        }

        ExperimentSituations currentSituation() {
            return ScienceUtil.GetExperimentSituation(vessel);
        }


        float currentScienceValue(ModuleScienceExperiment e) {
            return ResearchAndDevelopment.GetScienceValue(
                    e.experiment.baseValue * e.experiment.dataScale,
                    currentScienceSubject(e.experiment)
                    );
        }

        ScienceSubject currentScienceSubject(ScienceExperiment experiment) {
            string fixBiome = string.Empty; // some biomes don't have 4th string, so we just put an empty in to compare strings later
            if (experiment.BiomeIsRelevantWhile(currentSituation()))
                // for those that do, we add it to the string
                fixBiome = currentBiome();
            //ikr!, we pretty much did all the work already, jeez
            return ResearchAndDevelopment.GetExperimentSubject(experiment, currentSituation(), mainBody, fixBiome);
        }

        // FIXME: see what MJ + Biomatic do?
        string currentBiome() {
                if (mainBody.BiomeMap != null)
                    return !string.IsNullOrEmpty(vessel.landedAt)
                                    ? Vessel.GetLandedAtString(vessel.landedAt)
                                    : ScienceUtil.GetExperimentBiome(mainBody,
                                                vessel.latitude, vessel.longitude);

            return string.Empty;
        }
    }
}
