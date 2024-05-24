using Microsoft.PowerFx.Types;

namespace PowerFxWasm.Model
{
	public class TypeGenerator
	{
		public static RecordType GetTypeFromJson(string json) {
			var record = (RecordValue)FormulaValueJSON.FromJson(json);
			return record.Type;
		}
	}
}