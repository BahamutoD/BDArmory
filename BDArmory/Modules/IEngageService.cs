namespace BDArmory.Modules
{
    public interface IEngageService
    {
        float GetEngagementRangeMax();

        bool GetEngageAirTargets();

        bool GetEngageMissileTargets();

        bool GetEngageGroundTargets();

        bool GetEngageSLWTargets();
    }
}
