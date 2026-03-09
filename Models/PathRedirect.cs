namespace ServiceManual.Models;

/// <summary>
/// A 301 redirect rule from an old path to a new path, optionally via an interim "content has moved" page.
/// </summary>
public class PathRedirect
{
    public string OldPath { get; set; } = string.Empty;
    public string NewPath { get; set; } = string.Empty;
    public bool UseInterimPage { get; set; }
}
