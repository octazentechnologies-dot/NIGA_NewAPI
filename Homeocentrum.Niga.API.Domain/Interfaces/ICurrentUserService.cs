namespace Homeocentrum.Niga.API.Domain.Interfaces
{
    public interface ICurrentUserService
    {
        #nullable enable
        //string? UserId { get; }
        int getUserId();
        int getCompanyId();
    }

}
