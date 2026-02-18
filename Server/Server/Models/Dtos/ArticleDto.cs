namespace Server.Models.Dtos
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="Name"></param>
    /// <param name="Price"></param>
    public record ArticleDto(Guid Id, string Name, decimal Price);
}
