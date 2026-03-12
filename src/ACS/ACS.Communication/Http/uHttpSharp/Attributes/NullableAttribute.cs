using System;

namespace ACS.Communication.Http.uHttpSharp.Attributes
{
    /// <summary>
    /// Marks a controller method argmuent 
    /// as an argument that may be null.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class NullableAttribute : Attribute
    {
        
    }
}