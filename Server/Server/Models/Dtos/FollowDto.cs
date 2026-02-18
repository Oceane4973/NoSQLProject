namespace Server.Models.Dtos
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="FollowerId"></param>
    /// <param name="FollowingId"></param>
    public record FollowDto(Guid FollowerId, Guid FollowingId);

}
