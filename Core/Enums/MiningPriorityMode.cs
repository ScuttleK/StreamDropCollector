namespace Core.Enums
{
    public enum MiningPriorityMode
    {
        AvailabilityThenProgress = 0,
        EndingSoonest = 1,
        LeastTimeToNextReward = 2,
        HighestCompletion = 3
    }
}