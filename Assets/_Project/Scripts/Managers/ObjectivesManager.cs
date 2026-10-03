using System;
using UnityEngine;

namespace Project.Managers
{
    public class ObjectivesManager : MonoBehaviour
    {
        public static ObjectivesManager Instance { get; private set; }

        public enum ObjectiveStep
        {
            UseWind = 0,
            BreakFragileFloor = 1,
            UseRubberToCrossGap = 2,
            ReachTheFinish = 3,
            LevelCompleted = 4
        }

        [SerializeField] private ObjectiveStep currentStep = ObjectiveStep.UseWind;

        public event Action<string> OnObjectiveChanged;
        public ObjectiveStep CurrentStep => currentStep;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            BroadcastObjective();
        }

        public void SetStep(ObjectiveStep step)
        {
            if (step > currentStep)
            {
                currentStep = step;
                BroadcastObjective();
            }
        }

        public void BroadcastObjective()
        {
            string desc = GetObjectiveText(currentStep);
            OnObjectiveChanged?.Invoke(desc);
        }

        public static string GetObjectiveText(ObjectiveStep step)
        {
            switch (step)
            {
                case ObjectiveStep.UseWind:
                    return "USE WIND TO REACH THE UPPER PLATFORM";
                case ObjectiveStep.BreakFragileFloor:
                    return "BREAK THE FRAGILE FLOOR";
                case ObjectiveStep.UseRubberToCrossGap:
                    return "CROSS THE GAP";
                case ObjectiveStep.ReachTheFinish:
                    return "REACH THE FINISH";
                case ObjectiveStep.LevelCompleted:
                    return "LEVEL COMPLETE!";
                default:
                    return "";
            }
        }
    }
}
