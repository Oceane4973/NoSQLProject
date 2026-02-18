namespace Server.Models.Dtos
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="UserId"></param>
    /// <param name="ArticleId"></param>
    /// <param name="Quantity"></param>
    /// /// <param name="TotalPrice"></param>
    public record OrderDto(Guid Id, Guid UserId, Guid ArticleId, int Quantity, decimal TotalPrice);

}
