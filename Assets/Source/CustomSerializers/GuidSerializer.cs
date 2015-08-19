using Conveyor;
using Avro.IO;
using System;

public static class GuidSerializer
{
	[ConveyorSerializer]
	public static void Serializer(Guid data, BinaryEncoder encoder)
	{
		encoder.WriteString(data.ToString());
	}

	[ConveyorDeserializer]
	public static Guid Deserializer(BinaryDecoder decoder)
	{
		return new Guid(decoder.ReadString());
	}
}