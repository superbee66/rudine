namespace Rudine.Web
{
    /// <summary>
    ///     Properties implemented must round-trip from InfoPath form XML all the way to DAL
    ///     where is will be managed by application code before getting inserted to the database
    /// </summary>
    public interface IDocIdentifiers
    {
        /// <summary>
        ///     null = never submitted, false = submitted, true = approved
        /// </summary>
        bool? DocStatus { get; set; }
    }
}