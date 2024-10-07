using UnityEngine;

/// <summary>
/// Contains all the miscellaneous scripts used by the GridMapMaker package
/// </summary>
/// 
namespace GridMapMaker
{
    /// <summary>
    /// This is used to create the ShowOnlyField attribute. Dont delete
    /// Show only attributes show values in the inspector but do not allow them to be edited.
    /// For private fields, you must have both the ShowOnlyField and SerializeField attributes.
    /// </summary>
    /// 

    public class ShowOnlyFieldAttribute : PropertyAttribute
    {

    }
}