namespace Niga_Domain.Interfaces
{
    public interface ICurrentUserService
    {
        #nullable enable
        //string? UserId { get; }
        int getUserId();
        int getCompanyId();
    }

}
