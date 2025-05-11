// Warning: Some assembly references could not be resolved automatically. This might lead to incorrect decompilation of some parts,
// for ex. property getter/setter access. To get optimal decompilation results, please manually add the missing references to the list of loaded assemblies.
// Sandbox.Game, Version=0.1.1.0, Culture=neutral, PublicKeyToken=null
// Sandbox.Game.AI.Autopilots.MyAutopilotTypeAttribute
using System;
using VRage.Game.Common;

internal class MyAutopilotTypeAttribute : MyFactoryTagAttribute
{
	public MyAutopilotTypeAttribute(Type objectBuilderType)
		: base(objectBuilderType)
	{
	}
}
