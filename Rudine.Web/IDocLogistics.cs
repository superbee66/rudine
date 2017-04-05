namespace Rudine.Web
{
    /// <summary>
    ///     Properties implemented must round-trip from InfoPath form XML all the way to DAL
    ///     where is will be managed by application code before getting inserted to the database
    /// </summary>
    public interface IDocLogistics
    {
        /// <summary>
        ///     Location the InfoPath XML document may be had. This location will often
        ///     be part of a RelayUrl
        /// </summary>
        string DocSrc { get; set; }
    }
}