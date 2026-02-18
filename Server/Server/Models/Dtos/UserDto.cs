namespace Server.Models.Dtos
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="UserName"></param>
    /// <param name="Email"></param>
    public record UserDto(Guid Id, string UserName, string Email);

}
