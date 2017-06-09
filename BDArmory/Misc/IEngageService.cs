namespace BDArmory.Misc
{
    public interface IEngageService
    {
        float GetEngagementRangeMax();
        bool GetEngageAirTargets();
        bool GetEngageMissileTargets();
        bool GetEngageGroundTargets();
    }
}