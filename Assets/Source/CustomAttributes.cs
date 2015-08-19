using System;
using UnityEngine;

public class ReadOnlyAttribute : PropertyAttribute { }

namespace Conveyor
{
	[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public sealed class ConveyableAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
	public sealed class ConveyorSerializerAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
	public sealed class ConveyorDeserializerAttribute : Attribute { }
}