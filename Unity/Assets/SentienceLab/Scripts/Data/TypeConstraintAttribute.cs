#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

// Attribute to constrain class parameters that are not derived from GameObject
// https://answers.unity.com/questions/1479756/how-do-i-expose-a-list-of-interfaces-in-the-inspec.html

using UnityEngine;

public class TypeConstraintAttribute : PropertyAttribute
 {
     private System.Type type;
 
     public TypeConstraintAttribute(System.Type _type)
     {
         type = _type;
     }
 
     public System.Type Type
     {
         get { return type; }
     }
 }
 
