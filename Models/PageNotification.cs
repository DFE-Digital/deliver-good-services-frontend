namespace ServiceManual.Models
{
    /// <summary>
    /// A page notification banner shown when validFrom &lt;= now &lt;= validTo and enabled is true.
    /// </summary>
    public class PageNotification
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
