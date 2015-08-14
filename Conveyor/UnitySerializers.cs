//Copyright 2015 David Koontz & Trenton Kennedy Greyoak
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//	limitations under the License.

using Conveyor;
using UnityEngine;
using Avro.IO;

namespace Conveyor.Unity3d
{
	
	public static class UnitySerializers
	{
		[ConveyorSerializer]
		public static void ToByteArray(Vector3 vector, BinaryEncoder encoder)
		{
			encoder.WriteFloat(vector.x);
			encoder.WriteFloat(vector.y);
			encoder.WriteFloat(vector.z);
		}

		[ConveyorSerializer]
		public static Vector3 FromByteArray(BinaryDecoder decoder)
		{
			return new Vector3(
				decoder.ReadFloat(),
				decoder.ReadFloat(),
				decoder.ReadFloat());
		}
	}
}