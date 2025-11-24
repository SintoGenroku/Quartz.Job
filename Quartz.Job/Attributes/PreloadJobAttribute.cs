namespace Quartz.Job.Attributes;

/// <summary>
/// Attribute for decorating IJob that loads data when the application starts
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class PreloadJobAttribute : Attribute
{
}