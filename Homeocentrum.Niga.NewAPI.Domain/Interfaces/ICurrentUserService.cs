namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces
{
    public interface ICurrentUserService
    {
        #nullable enable
        //string? UserId { get; }
        int getUserId();
        int getCompanyId();
    }

}
