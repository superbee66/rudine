namespace Rudine
{
    /// <summary>
    ///     Properties implemented must round-trip from InfoPath form XML all the way to DAL
    ///     where is will be managed by application code before getting inserted to the database
    /// </summary>
    public interface IDocFormatting
    {
        /// <summary>
        ///     Used to generate the title when returned as a ListItem &
        ///     adapted to something safe for the XML filename
        /// </summary>
        string DocTitleFormat { get; set; }
    }
}