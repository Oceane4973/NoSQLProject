namespace Server.Models.Dtos
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Articles"></param>
    /// <param name="Users"></param>
    /// <param name="Follows"></param>
    /// <param name="Orders"></param>
    public record SetupDto(List<ArticleDto> Articles, List<UserDto> Users, List<FollowDto> Follows, List<OrderDto> Orders);

}
